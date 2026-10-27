"""Bounded, deterministic raster jobs for Pixelorama preview exchange.

The editor never executes a recipe. It receives a checked PNG plus a small job envelope.
"""
from __future__ import annotations

import hashlib
import json
import os
from pathlib import Path
import tempfile
import uuid

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
MANIFEST = json.loads((ROOT / "ArtSource/pilots.json").read_text(encoding="utf-8"))
PALETTE = {"#" + value.upper(): tuple(bytes.fromhex(value)) + (255,)
           for value in MANIFEST["palette"]}
MAX_SIDE = 1024
MAX_OPERATIONS = 4096
MAX_RECIPE_BYTES = 1024 * 1024


class JobError(ValueError):
    """A recipe or target violates the preview contract."""


def _integer(value: object, label: str, minimum: int, maximum: int) -> int:
    if type(value) is not int or not minimum <= value <= maximum:
        raise JobError(f"{label} must be an integer in {minimum}..{maximum}")
    return value


def _point(value: object, width: int, height: int, label: str) -> tuple[int, int]:
    if not isinstance(value, list) or len(value) != 2:
        raise JobError(f"{label} must be [x,y]")
    return (_integer(value[0], label + ".x", 0, width - 1),
            _integer(value[1], label + ".y", 0, height - 1))


def _color(value: object) -> tuple[int, int, int, int]:
    if value == "transparent":
        return 0, 0, 0, 0
    if not isinstance(value, str) or value.upper() not in PALETTE:
        raise JobError(f"color must be one of the Barrowlight palette entries: {value!r}")
    return PALETTE[value.upper()]


def render(recipe: dict) -> Image.Image:
    """Render only explicit pixel operations to a fresh RGBA buffer."""
    if not isinstance(recipe, dict) or set(recipe) != {"version", "width", "height", "background", "layers"} or recipe["version"] != 1:
        raise JobError("recipe must use version 1 and the documented fields")
    width = _integer(recipe["width"], "width", 1, MAX_SIDE)
    height = _integer(recipe["height"], "height", 1, MAX_SIDE)
    layers = recipe["layers"]
    if not isinstance(layers, list) or not 1 <= len(layers) <= 32:
        raise JobError("layers must contain 1..32 layers")
    result = Image.new("RGBA", (width, height), _color(recipe["background"]))
    count = 0
    names: set[str] = set()
    for layer in layers:
        if not isinstance(layer, dict) or set(layer) != {"name", "operations"}:
            raise JobError("each layer needs a name and operations")
        name = layer["name"]
        if not isinstance(name, str) or not 1 <= len(name) <= 64 or name in names:
            raise JobError("layer names must be distinct and 1..64 characters")
        names.add(name)
        operations = layer["operations"]
        if not isinstance(operations, list):
            raise JobError("operations must be an array")
        count += len(operations)
        if count > MAX_OPERATIONS:
            raise JobError("too many operations")
        canvas = Image.new("RGBA", (width, height))
        draw = ImageDraw.Draw(canvas)
        for op in operations:
            if not isinstance(op, dict) or not isinstance(op.get("type"), str):
                raise JobError("operation must have a type")
            kind = op["type"]
            if kind == "pixel" and set(op) == {"type", "at", "color"}:
                draw.point(_point(op["at"], width, height, "at"), fill=_color(op["color"]))
            elif kind in ("line", "rectangle", "ellipse") and set(op) == {"type", "from", "to", "color", "filled"}:
                start = _point(op["from"], width, height, "from")
                end = _point(op["to"], width, height, "to")
                if start[0] > end[0] or start[1] > end[1] or type(op["filled"]) is not bool:
                    raise JobError("invalid primitive bounds or filled value")
                box = (*start, *end)
                color = _color(op["color"])
                if kind == "line":
                    if op["filled"]:
                        raise JobError("a line cannot be filled")
                    draw.line(box, fill=color)
                elif kind == "rectangle":
                    draw.rectangle(box, fill=color if op["filled"] else None,
                                   outline=None if op["filled"] else color)
                else:
                    draw.ellipse(box, fill=color if op["filled"] else None,
                                 outline=None if op["filled"] else color)
            else:
                raise JobError(f"unsupported operation or fields: {kind}")
        result.alpha_composite(canvas)
    return result


def atomic_bytes(path: Path, content: bytes) -> None:
    """Publish a complete file in one rename, with no arbitrary output path in a job."""
    path.parent.mkdir(parents=True, exist_ok=True)
    descriptor, temp_name = tempfile.mkstemp(dir=path.parent, prefix=".pending-")
    try:
        with os.fdopen(descriptor, "wb") as stream:
            stream.write(content)
            stream.flush()
            os.fsync(stream.fileno())
        os.replace(temp_name, path)
    finally:
        if os.path.exists(temp_name):
            os.unlink(temp_name)


def publish_recipe(recipe: dict, inbox: Path, status_path: Path) -> Path:
    """Publish a preview for the exact editor snapshot selected by the user."""
    image = render(recipe)
    status = json.loads(status_path.read_text(encoding="utf-8"))
    if status.get("protocol") != 1 or not status.get("canApply"):
        raise JobError("editor target is unavailable")
    target = status.get("target")
    if not isinstance(target, dict) or set(target) != {"sessionId", "projectId", "frame", "layer", "width", "height", "baseHash"}:
        raise JobError("editor target snapshot is malformed")
    if image.size != (target["width"], target["height"]):
        raise JobError("image dimensions differ from selected cel")
    if len(list((inbox / "requests").glob("*.json"))) >= 4:
        raise JobError("inbox already contains four pending jobs")
    request_id = str(uuid.uuid4())
    png_name = request_id + ".png"
    png_path = inbox / "images" / png_name
    from io import BytesIO
    buffer = BytesIO()
    image.save(buffer, format="PNG")
    payload = buffer.getvalue()
    atomic_bytes(png_path, payload)
    job = {"protocol": 1, "requestId": request_id, "kind": "preview", "target": target,
           "image": {"file": png_name, "sha256": hashlib.sha256(payload).hexdigest(),
                     "width": image.width, "height": image.height}}
    job_path = inbox / "requests" / (request_id + ".json")
    atomic_bytes(job_path, json.dumps(job, separators=(",", ":")).encode("utf-8"))
    return job_path


def publish(recipe_path: Path, inbox: Path, status_path: Path) -> Path:
    if recipe_path.stat().st_size > MAX_RECIPE_BYTES:
        raise JobError("recipe exceeds 1 MiB")
    recipe = json.loads(recipe_path.read_text(encoding="utf-8"))
    return publish_recipe(recipe, inbox, status_path)
