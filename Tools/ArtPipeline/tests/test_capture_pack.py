import hashlib
import json
import sys
import tempfile
import unittest
from pathlib import Path

from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from capture_pack import build
from make_preview import frame_durations

STAGING = Path(__file__).resolve().parents[3] / "ArtBuild"
STAGING.mkdir(exist_ok=True)


class CapturePackTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(dir=STAGING)
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        (self.root / "save.json").write_text('{"saveVersion":2}', encoding="utf-8")
        for name, channel in (("opening", 0), ("ending", 2)):
            directory = self.root / "frames" / name
            directory.mkdir(parents=True)
            for i in range(2):
                color = [0, 0, 0]
                color[channel] = 100 + i * 50
                Image.new("RGB", (480, 270), tuple(color)).save(directory / f"frame_{(i + 1) * 5:06}.png")
        self.manifest = {"version": 1, "baselineCommit": "abc123", "contentVersion": "1.0.1", "seed": 42,
                         "shots": [self.shot("opening", False), self.shot("ending", True)]}
        self.path = self.root / "shots.json"
        self.write_manifest()

    @staticmethod
    def shot(identifier, spoiler):
        return {"id": identifier, "recording": "frames/" + identifier, "start": 0, "stop": 2,
                "spoiler": spoiler, "label": identifier, "script": "look", "fixture": "prepared-save", "save": "save.json"}

    def write_manifest(self):
        self.path.write_text(json.dumps(self.manifest), encoding="utf-8")

    def test_spoiler_is_exported_separately_and_excluded_from_teaser(self):
        output = self.root / "pack"
        receipt = build(self.path, output)
        self.assertTrue(receipt["silent"])
        self.assertEqual(receipt["teaserFrames"], 2)
        self.assertTrue((output / "ending.gif").is_file())
        with Image.open(output / "teaser.gif") as teaser:
            self.assertEqual(teaser.n_frames, 2)
            for index in range(teaser.n_frames):
                teaser.seek(index)
                self.assertEqual(teaser.convert("RGB").getpixel((10, 10))[2], 0)

    def test_receipt_hashes_and_poster_match_selected_source(self):
        output = self.root / "pack"
        receipt = build(self.path, output)
        source = self.root / "frames/opening/frame_000005.png"
        original_hash = hashlib.sha256(source.read_bytes()).hexdigest()
        self.assertEqual(receipt["shots"][0]["sourceFrames"][0]["sha256"], original_hash)
        self.assertEqual(receipt["shots"][0]["saveSha256"], hashlib.sha256((self.root / "save.json").read_bytes()).hexdigest())
        with Image.open(source) as image, Image.open(output / "opening.png") as poster:
            self.assertEqual(image.tobytes(), poster.tobytes())
        self.assertEqual(hashlib.sha256(source.read_bytes()).hexdigest(), original_hash)

    def test_gif_timing_does_not_drift_over_a_whole_second(self):
        for count in (12, 120, 360):
            self.assertEqual(sum(frame_durations(count)), count // 12 * 1000)

    def test_existing_pack_cannot_be_overwritten(self):
        output = self.root / "pack"
        build(self.path, output)
        before = (output / "capture-receipt.json").read_bytes()
        with self.assertRaises(FileExistsError):
            build(self.path, output)
        self.assertEqual((output / "capture-receipt.json").read_bytes(), before)

    def test_invalid_ranges_and_escaping_recordings_are_rejected(self):
        original = self.manifest["shots"][0].copy()
        for change in ({"start": -1}, {"start": True}, {"stop": 3}, {"stop": 0}, {"recording": "../outside"},
                       {"recording": "C:/absolute"}, {"id": "../escape"}):
            with self.subTest(change=change):
                self.manifest["shots"][0] = {**original, **change}
                self.write_manifest()
                with self.assertRaises(ValueError):
                    build(self.path, self.root / "pack")

    def test_missing_spoiler_or_fixture_provenance_is_rejected(self):
        original = self.manifest["shots"][0].copy()
        for field in ("spoiler", "fixture", "script", "label", "save"):
            self.manifest["shots"][0] = {key: value for key, value in original.items() if key != field}
            self.write_manifest()
            with self.subTest(field=field), self.assertRaises(ValueError):
                build(self.path, self.root / "pack")

    def test_scaled_capture_is_rejected_without_completed_receipt(self):
        Image.new("RGB", (960, 540)).save(self.root / "frames/opening/frame_000005.png")
        with self.assertRaisesRegex(ValueError, "native"):
            build(self.path, self.root / "pack")
        self.assertFalse((self.root / "pack/capture-receipt.json").exists())

    def test_all_spoiler_pack_cannot_be_labelled_as_teaser(self):
        self.manifest["shots"][0]["spoiler"] = True
        self.write_manifest()
        with self.assertRaisesRegex(ValueError, "teaser"):
            build(self.path, self.root / "pack")

    def test_missing_frame_cannot_silently_change_recording_timing(self):
        (self.root / "frames/opening/frame_000010.png").rename(self.root / "frames/opening/frame_000015.png")
        with self.assertRaisesRegex(ValueError, "non-contiguous"):
            build(self.path, self.root / "pack")
