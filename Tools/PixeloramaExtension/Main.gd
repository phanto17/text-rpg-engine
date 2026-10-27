extends Node

# Pixelorama 1.2.3 / ExtensionsAPI 9. The only document mutation uses the public
# set_pixelcel_image() method after a fresh target hash and selection check.
const MAX_JOB_BYTES := 1048576
const MAX_PNG_BYTES := 4194304
const MAX_SIDE := 1024
const SCAN_SECONDS := 0.04
const PALETTE := ["0b0d12", "151923", "252b38", "3c4554", "697584", "aab8c5", "e5ddc8", "fff1c9", "2b211f", "63372e", "a84b45", "db8552", "d7b46a", "597465", "7fa89b", "a29bc4"]
var _api
var _panel: VBoxContainer
var _path_input: LineEdit
var _message: Label
var _picture: TextureRect
var _apply_button: Button
var _inbox := ""
var _session_id := ""
var _preview: Dictionary = {}
var _preview_image: Image
var _elapsed := 0.0
var _last_status := ""


func _enter_tree() -> void:
	_api = get_node_or_null("/root/ExtensionsApi")
	if _api == null or _api.get_api_version() != 9:
		push_error("Barrowlight Preview requires Pixelorama ExtensionsAPI 9")
		return
	_session_id = Crypto.new().generate_random_bytes(16).hex_encode()
	_build_panel()
	_api.panel.add_node_as_tab(_panel)


func _exit_tree() -> void:
	_inbox = ""
	_preview.clear()
	if _panel != null and is_instance_valid(_panel):
		_api.panel.remove_node_from_tab(_panel)
		_panel.queue_free()


func _build_panel() -> void:
	_panel = VBoxContainer.new()
	_panel.name = "Barrowlight Preview"
	_path_input = LineEdit.new()
	_path_input.placeholder_text = "Absolute local inbox folder"
	_panel.add_child(_path_input)
	var connect_button := Button.new()
	connect_button.text = "Connect local inbox"
	connect_button.pressed.connect(_connect_inbox)
	_panel.add_child(connect_button)
	_message = Label.new()
	_message.text = "Choose a local inbox. No document edits occur automatically."
	_message.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	_panel.add_child(_message)
	_picture = TextureRect.new()
	_picture.custom_minimum_size = Vector2(100, 100)
	_picture.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	_picture.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	_panel.add_child(_picture)
	_apply_button = Button.new()
	_apply_button.text = "Apply to selected cel"
	_apply_button.disabled = true
	_apply_button.pressed.connect(_apply_preview)
	_panel.add_child(_apply_button)
	var cancel_button := Button.new()
	cancel_button.text = "Cancel preview"
	cancel_button.pressed.connect(_cancel_preview)
	_panel.add_child(cancel_button)
	var disconnect_button := Button.new()
	disconnect_button.text = "Disconnect"
	disconnect_button.pressed.connect(_disconnect_inbox)
	_panel.add_child(disconnect_button)


func _connect_inbox() -> void:
	var requested := _path_input.text.strip_edges()
	if not requested.is_absolute_path() or not DirAccess.dir_exists_absolute(requested):
		_message.text = "Select an existing absolute folder."
		return
	if not DirAccess.dir_exists_absolute(requested.path_join("requests")) or not DirAccess.dir_exists_absolute(requested.path_join("images")):
		_message.text = "Inbox needs requests/ and images/ folders."
		return
	if not DirAccess.dir_exists_absolute(requested.path_join("responses")):
		_message.text = "Inbox needs a responses/ folder."
		return
	_cancel_preview()
	_inbox = requested
	_last_status = ""
	_message.text = "Connected. Waiting for a preview job."
	_write_status()


func _disconnect_inbox() -> void:
	_cancel_preview()
	_inbox = ""
	_message.text = "Disconnected."


func _process(delta: float) -> void:
	if _inbox.is_empty():
		return
	_elapsed += delta
	if _elapsed < SCAN_SECONDS:
		return
	_elapsed = 0.0
	_write_status()
	_scan_requests()


func _target() -> Dictionary:
	var project = _api.project.current_project
	if project == null or project.size.x < 1 or project.size.y < 1 or project.size.x > MAX_SIDE or project.size.y > MAX_SIDE:
		return {}
	var frame: int = project.current_frame
	var layer: int = project.current_layer
	if frame < 0 or frame >= project.frames.size() or layer < 0 or layer >= project.layers.size():
		return {}
	var cel = _api.project.get_cel_at(project, frame, layer)
	if cel == null or cel.get_class_name() != "PixelCel" or cel.link_set != null:
		return {}
	if project.layers[layer].is_locked_in_hierarchy() or project.is_indexed():
		return {}
	if cel.get_image().get_size() != project.size:
		return {}
	return {
		"sessionId": _session_id,
		"projectId": str(project.get_instance_id()),
		"frame": frame,
		"layer": layer,
		"width": project.size.x,
		"height": project.size.y,
		"baseHash": _hash(cel.get_image().get_data())
	}


func _hash(data: PackedByteArray) -> String:
	var context := HashingContext.new()
	context.start(HashingContext.HASH_SHA256)
	context.update(data)
	return context.finish().hex_encode()


func _write_status() -> void:
	var target := _target()
	var status := {"protocol": 1, "canApply": not target.is_empty(), "target": target}
	var encoded := JSON.stringify(status)
	if encoded == _last_status:
		return
	if _write_json(_inbox.path_join("status.json"), status):
		_last_status = encoded


func _write_json(path: String, value: Dictionary) -> bool:
	var temp := path + ".pending"
	var file := FileAccess.open(temp, FileAccess.WRITE)
	if file == null:
		_message.text = "Cannot write inbox status or response."
		return false
	file.store_string(JSON.stringify(value))
	file.flush()
	file.close()
	var err := DirAccess.rename_absolute(temp, path)
	if err != OK:
		_message.text = "Cannot publish inbox status or response: " + str(err)
		return false
	return true


func _ack(id: String, state: String, detail := "") -> void:
	_write_json(_inbox.path_join("responses").path_join(id + ".json"),
		{"protocol": 1, "requestId": id, "state": state, "detail": detail})


func _scan_requests() -> void:
	var files := DirAccess.get_files_at(_inbox.path_join("requests"))
	files.sort()
	var seen := 0
	for file_name in files:
		if not file_name.ends_with(".json"):
			continue
		seen += 1
		if seen > 4:
			break
		var id := file_name.trim_suffix(".json")
		var path := _inbox.path_join("requests").path_join(file_name)
		if not _valid_id(id):
			DirAccess.remove_absolute(path)
			continue
		var content := FileAccess.get_file_as_bytes(path)
		if content.size() > MAX_JOB_BYTES:
			_ack(id, "rejected", "job exceeds 1 MiB")
			DirAccess.remove_absolute(path)
			continue
		var job = JSON.parse_string(content.get_string_from_utf8())
		var error := _inspect_job(id, job)
		if error.is_empty():
			if not _preview.is_empty():
				_ack(_preview["requestId"], "superseded")
			_preview = job
			var image_path := _inbox.path_join("images").path_join(id + ".png")
			_preview_image = Image.new()
			_preview_image.load_png_from_buffer(FileAccess.get_file_as_bytes(image_path))
			_picture.texture = ImageTexture.create_from_image(_preview_image)
			_apply_button.disabled = false
			_message.text = "Preview ready. Apply checks the target again."
			_ack(id, "previewed")
		else:
			_ack(id, "rejected", error)
		DirAccess.remove_absolute(path)


func _inspect_job(id: String, job: Variant) -> String:
	if not job is Dictionary or job.get("protocol") != 1 or job.get("requestId") != id or job.get("kind") != "preview":
		return "invalid job envelope"
	if not job.get("target") is Dictionary or not job.get("image") is Dictionary:
		return "missing target or image"
	var target := _target()
	if target.is_empty() or not _same_target(target, job["target"]):
		return "target or cel revision changed"
	var meta: Dictionary = job["image"]
	if meta.get("file") != id + ".png" or meta.get("width") != target["width"] or meta.get("height") != target["height"]:
		return "image path or dimensions differ"
	var image_path := _inbox.path_join("images").path_join(id + ".png")
	var bytes := FileAccess.get_file_as_bytes(image_path)
	if bytes.is_empty() or bytes.size() > MAX_PNG_BYTES or _hash(bytes) != meta.get("sha256"):
		return "missing, oversized or mismatched PNG"
	var image := Image.new()
	if image.load_png_from_buffer(bytes) != OK or image.get_size() != Vector2i(target["width"], target["height"]):
		return "invalid decoded PNG"
	if image.get_format() != Image.FORMAT_RGBA8:
		image.convert(Image.FORMAT_RGBA8)
	for y in image.get_height():
		for x in image.get_width():
			var color := image.get_pixel(x, y)
			var alpha := color.a8
			if alpha != 0 and alpha != 255:
				return "partial alpha is unsupported"
			if alpha == 255 and not color.to_html(false) in PALETTE:
				return "off-palette color is unsupported"
	return ""


func _valid_id(id: String) -> bool:
	if id.length() != 36:
		return false
	for index in id.length():
		var character := id[index]
		if index in [8, 13, 18, 23]:
			if character != "-":
				return false
		elif not character in "0123456789abcdef":
			return false
	return true


func _apply_preview() -> void:
	if _preview.is_empty():
		return
	if Input.is_mouse_button_pressed(MOUSE_BUTTON_LEFT) or Input.is_mouse_button_pressed(MOUSE_BUTTON_RIGHT):
		_message.text = "Release the drawing tool before applying."
		return
	var target := _target()
	if target.is_empty() or not _same_target(target, _preview["target"]):
		_ack(_preview["requestId"], "rejected", "target or cel revision changed before apply")
		_cancel_preview(false)
		return
	_api.project.set_pixelcel_image(_preview_image.duplicate(), target["frame"], target["layer"])
	_ack(_preview["requestId"], "applied", _target().get("baseHash", ""))
	_cancel_preview(false)
	_message.text = "Applied with Pixelorama undo. Save the project when ready."


func _same_target(current: Dictionary, received: Dictionary) -> bool:
	if current.size() != received.size():
		return false
	for key in ["sessionId", "projectId", "baseHash"]:
		if not received.has(key) or str(current[key]) != str(received[key]):
			return false
	for key in ["frame", "layer", "width", "height"]:
		if not received.has(key) or typeof(received[key]) not in [TYPE_INT, TYPE_FLOAT] or received[key] != current[key]:
			return false
	return true


func _cancel_preview(ack := true) -> void:
	if ack and not _preview.is_empty() and not _inbox.is_empty():
		_ack(_preview["requestId"], "cancelled")
	_preview.clear()
	_preview_image = null
	if _picture != null:
		_picture.texture = null
	if _apply_button != null:
		_apply_button.disabled = true
