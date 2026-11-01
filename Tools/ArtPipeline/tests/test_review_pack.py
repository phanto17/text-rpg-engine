import json
import struct
import sys
import tempfile
import unittest
import wave
from pathlib import Path

from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from review_pack import asset_path, audio_preview, build

STAGING = Path(__file__).resolve().parents[3] / "ArtBuild"
STAGING.mkdir(exist_ok=True)


class ReviewPackTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(dir=STAGING)
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.data = self.root / "Data"
        (self.data / "World").mkdir(parents=True)
        (self.data / "Assets").mkdir()
        for name, size in (("room", (304, 140)), ("monster", (192, 48)), ("portrait", (56, 56))):
            Image.new("RGBA", size, "#151923").save(self.data / f"Assets/{name}.png")
        with wave.open(str(self.data / "Assets/loop.wav"), "wb") as output:
            output.setparams((1, 2, 22050, 0, "NONE", "not compressed"))
            output.writeframes(struct.pack("<4h", 0, 10000, -10000, 0))
        self.presentation = {
            "rooms": {"room": {"backdropImage": "Assets/room.png"}},
            "monsters": {"monster": {"spriteImage": "Assets/monster.png", "animation": {"frames": 4, "frameMilliseconds": 250}}},
            "npcs": {"portrait": {"portraitImage": "Assets/portrait.png"}},
            "audio": {"ambience": {"room": "Assets/loop.wav"}, "effects": {"select": "Assets/loop.wav"}},
        }
        self.write_presentation()

    def write_presentation(self):
        (self.data / "World/presentation.json").write_text(json.dumps(self.presentation), encoding="utf-8")

    def test_native_capture_is_distinguished_from_source_only(self):
        source = build(self.data, self.root / "source")
        self.assertFalse(source["rooms"][0]["nativeCapture"])
        self.assertEqual(source["subjectiveAudioReview"], "pending")
        self.assertIn("native review pending", (self.root / "source/index.html").read_text(encoding="utf-8"))
        captures = self.root / "captures"
        captures.mkdir()
        Image.new("RGB", (960, 540), "#d7b46a").save(captures / "room.png")
        native = build(self.data, self.root / "native", captures)
        self.assertTrue(native["rooms"][0]["nativeCapture"])
        with Image.open(self.root / "native/media/image_000.png") as image:
            self.assertEqual(image.size, (480, 270))
            self.assertEqual(image.getpixel((10, 10))[:3], (215, 180, 106))

    def test_preview_gain_preserves_source_and_does_not_normalize(self):
        source = self.data / "Assets/loop.wav"
        original = source.read_bytes()
        metrics = audio_preview(source, self.root / "preview.wav", 0.2)
        with wave.open(str(self.root / "preview.wav"), "rb") as preview:
            self.assertEqual(struct.unpack("<4h", preview.readframes(4)), (0, 2000, -2000, 0))
        self.assertEqual(source.read_bytes(), original)
        self.assertLess(metrics["previewPeakDbfs"], metrics["peakDbfs"])

    def test_existing_review_cannot_be_overwritten(self):
        output = self.root / "review"
        build(self.data, output)
        original = (output / "review.json").read_bytes()
        with self.assertRaises(FileExistsError):
            build(self.data, output)
        self.assertEqual((output / "review.json").read_bytes(), original)

    def test_asset_paths_reject_missing_files_and_traversal(self):
        for relative in ("../outside.png", "/absolute.png", "C:/absolute.png", "Assets\\room.png", "missing.png"):
            with self.subTest(relative=relative), self.assertRaises(ValueError):
                asset_path(self.data, relative)

    def test_html_escapes_data_labels(self):
        self.presentation["rooms"]["<script>alert(1)</script>"] = self.presentation["rooms"].pop("room")
        self.write_presentation()
        build(self.data, self.root / "escaped")
        document = (self.root / "escaped/index.html").read_text(encoding="utf-8")
        self.assertNotIn("<script>", document)
        self.assertIn("&lt;script&gt;", document)

    def test_invalid_native_capture_is_not_presented_as_verified(self):
        captures = self.root / "captures"
        captures.mkdir()
        Image.new("RGB", (800, 450)).save(captures / "room.png")
        with self.assertRaisesRegex(ValueError, "canvas capture"):
            build(self.data, self.root / "invalid", captures)

    def test_invalid_figure_sheet_is_rejected(self):
        self.presentation["monsters"]["monster"]["animation"]["frames"] = 3
        self.write_presentation()
        with self.assertRaisesRegex(ValueError, "figure layout"):
            build(self.data, self.root / "invalid")

    def test_unsupported_pcm_is_rejected(self):
        with wave.open(str(self.root / "unsupported.wav"), "wb") as output:
            output.setparams((2, 2, 44100, 0, "NONE", "not compressed"))
            output.writeframes(b"\0" * 8)
        with self.assertRaisesRegex(ValueError, "Unsupported"):
            audio_preview(self.root / "unsupported.wav", self.root / "preview.wav", 0.2)
