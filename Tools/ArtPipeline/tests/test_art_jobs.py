import json
import sys
import tempfile
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from art_jobs import JobError, publish, render

STAGING = Path(__file__).resolve().parents[3] / "ArtBuild"
STAGING.mkdir(exist_ok=True)


def recipe():
    return {"version": 1, "width": 4, "height": 4, "background": "transparent", "layers": [
        {"name": "base", "operations": [
            {"type": "rectangle", "from": [0, 0], "to": [3, 3], "color": "#151923", "filled": True},
            {"type": "line", "from": [0, 0], "to": [3, 3], "color": "#FFF1C9", "filled": False}]},
        {"name": "light", "operations": [
            {"type": "pixel", "at": [1, 1], "color": "#D7B46A"}]}
    ]}


class ArtJobsTests(unittest.TestCase):
    def test_render_layers_and_primitives(self):
        result = render(recipe())
        self.assertEqual(result.size, (4, 4))
        self.assertEqual(result.getpixel((1, 1)), (215, 180, 106, 255))
        self.assertEqual(result.getpixel((3, 0)), (21, 25, 35, 255))

    def test_bad_recipe_is_rejected(self):
        for change in (lambda r: r.update(width=1025),
                       lambda r: r["layers"][0]["operations"][0].update(color="#FFFFFF"),
                       lambda r: r["layers"][0]["operations"][0].update(extra=True),
                       lambda r: r.update(layers=r["layers"] * 17)):
            candidate = recipe()
            change(candidate)
            with self.subTest(candidate=candidate), self.assertRaises(JobError):
                render(candidate)

    def test_publish_matches_selected_editor_snapshot(self):
        with tempfile.TemporaryDirectory(dir=STAGING) as folder:
            root = Path(folder)
            inbox = root / "inbox"
            status = {"protocol": 1, "canApply": True, "target": {
                "sessionId": "session", "projectId": "project", "frame": 0, "layer": 0,
                "width": 4, "height": 4, "baseHash": "revision"}}
            (inbox / "status.json").parent.mkdir()
            (inbox / "status.json").write_text(json.dumps(status))
            path = root / "recipe.json"
            path.write_text(json.dumps(recipe()))
            job_path = publish(path, inbox, inbox / "status.json")
            job = json.loads(job_path.read_text())
            self.assertEqual(job["target"], status["target"])
            self.assertTrue((inbox / "images" / job["image"]["file"]).exists())

    def test_publish_rejects_stale_dimensions(self):
        with tempfile.TemporaryDirectory(dir=STAGING) as folder:
            root = Path(folder)
            inbox = root / "inbox"
            inbox.mkdir()
            (inbox / "status.json").write_text(json.dumps({"protocol": 1, "canApply": True,
                "target": {"sessionId": "s", "projectId": "p", "frame": 0, "layer": 0,
                           "width": 5, "height": 4, "baseHash": "r"}}))
            path = root / "recipe.json"
            path.write_text(json.dumps(recipe()))
            with self.assertRaises(JobError):
                publish(path, inbox, inbox / "status.json")
            self.assertFalse((inbox / "requests").exists())


if __name__ == "__main__":
    unittest.main()
