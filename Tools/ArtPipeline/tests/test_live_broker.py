import asyncio
import json
import sys
import tempfile
import unittest
from pathlib import Path

from websockets.asyncio.client import connect
from websockets.asyncio.server import serve
from websockets.exceptions import ConnectionClosedError, InvalidStatus

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from live_broker import Broker

STAGING = Path(__file__).resolve().parents[3] / "ArtBuild"
STAGING.mkdir(exist_ok=True)


def recipe():
    return {"version": 1, "width": 4, "height": 4, "background": "transparent", "layers": [
        {"name": "test", "operations": [{"type": "pixel", "at": [1, 1], "color": "#FFF1C9"}]}]}


class BrokerTests(unittest.IsolatedAsyncioTestCase):
    async def asyncSetUp(self):
        self.folder = tempfile.TemporaryDirectory(dir=STAGING)
        self.inbox = Path(self.folder.name)
        for subdir in ("requests", "images", "responses"):
            (self.inbox / subdir).mkdir()
        (self.inbox / "status.json").write_text(json.dumps({"protocol": 1, "canApply": True, "target": {
            "sessionId": "s", "projectId": "p", "frame": 0, "layer": 0,
            "width": 4, "height": 4, "baseHash": "r"}}))
        self.server = await serve(Broker(self.inbox, "test-token").handle, "127.0.0.1", 0,
                                  origins=[None], compression=None, max_size=1048576)
        self.url = f"ws://127.0.0.1:{self.server.sockets[0].getsockname()[1]}"

    async def asyncTearDown(self):
        self.server.close()
        await self.server.wait_closed()
        self.folder.cleanup()

    async def test_auth_preview_and_status(self):
        async with connect(self.url) as client:
            await client.send(json.dumps({"type": "auth", "token": "test-token"}))
            self.assertEqual(json.loads(await asyncio.wait_for(client.recv(), 3))["type"], "authenticated")
            await client.send(json.dumps({"protocol": 1, "id": "one", "method": "preview", "recipe": recipe()}))
            result = json.loads(await asyncio.wait_for(client.recv(), 3))
            self.assertTrue(result["ok"], result)
            request_id = result["result"]["requestId"]
            self.assertTrue((self.inbox / "requests" / (request_id + ".json")).exists())
            await client.send(json.dumps({"protocol": 1, "id": "two", "method": "status", "requestId": request_id}))
            status = json.loads(await asyncio.wait_for(client.recv(), 3))
            self.assertEqual(status["result"]["operation"]["state"], "pending")

    async def test_bad_token_and_browser_origin_are_rejected(self):
        async with connect(self.url) as client:
            await client.send(json.dumps({"type": "auth", "token": "wrong"}))
            with self.assertRaises(ConnectionClosedError):
                await asyncio.wait_for(client.recv(), 3)
        with self.assertRaises(InvalidStatus):
            async with connect(self.url, origin="https://untrusted.example"):
                pass

    async def test_preview_burst_keeps_latest(self):
        async with connect(self.url) as client:
            await client.send(json.dumps({"type": "auth", "token": "test-token"}))
            await asyncio.wait_for(client.recv(), 3)
            await client.send(json.dumps({"protocol": 1, "id": "old", "method": "preview", "recipe": recipe()}))
            await client.send(json.dumps({"protocol": 1, "id": "new", "method": "preview", "recipe": recipe()}))
            replies = [json.loads(await asyncio.wait_for(client.recv(), 3)) for _ in range(2)]
            self.assertEqual({reply["id"] for reply in replies}, {"old", "new"})
            self.assertEqual(next(r for r in replies if r["id"] == "old")["error"], "superseded")
            self.assertTrue(next(r for r in replies if r["id"] == "new")["ok"])
            self.assertEqual(len(list((self.inbox / "requests").glob("*.json"))), 1)


if __name__ == "__main__":
    unittest.main()
