"""Authenticated loopback preview broker; editor still applies changes only on a click.

This is Barrowlight's own protocol, not a Pixelorama network API.
"""
from __future__ import annotations

import argparse
import asyncio
import json
import os
from pathlib import Path
import secrets

from websockets.asyncio.server import serve
from websockets.exceptions import ConnectionClosed

from art_jobs import JobError, MAX_RECIPE_BYTES, publish_recipe

MAX_MESSAGE = MAX_RECIPE_BYTES
COALESCE_SECONDS = 0.025


def ensure_token(path: Path) -> str:
    path.parent.mkdir(parents=True, exist_ok=True)
    if not path.exists():
        descriptor = os.open(path, os.O_CREAT | os.O_EXCL | os.O_WRONLY, 0o600)
        with os.fdopen(descriptor, "w", encoding="ascii") as stream:
            stream.write(secrets.token_hex(32))
    token = path.read_text(encoding="ascii").strip()
    if len(token) != 64 or any(char not in "0123456789abcdef" for char in token):
        raise ValueError("invalid broker token file")
    return token


class Broker:
    def __init__(self, inbox: Path, token: str):
        self.inbox = inbox
        self.token = token
        self.client_lock = asyncio.Lock()

    async def handle(self, connection):
        if self.client_lock.locked():
            await connection.close(code=1013, reason="controller already connected")
            return
        async with self.client_lock:
            try:
                raw = await asyncio.wait_for(connection.recv(), 3)
                auth = json.loads(raw)
                if not isinstance(auth, dict) or auth.get("type") != "auth" or not isinstance(auth.get("token"), str) or not secrets.compare_digest(auth["token"], self.token):
                    await connection.close(code=1008, reason="authentication failed")
                    return
                await connection.send('{"type":"authenticated","protocol":1}')
                queued = None
                while True:
                    raw = queued if queued is not None else await connection.recv()
                    queued = None
                    if not isinstance(raw, str) or len(raw.encode("utf-8")) > MAX_MESSAGE:
                        await connection.send(json.dumps({"ok": False, "error": "message too large"}))
                        continue
                    try:
                        request = json.loads(raw)
                    except json.JSONDecodeError:
                        await connection.send(json.dumps({"ok": False, "error": "invalid JSON"}))
                        continue
                    if not isinstance(request, dict) or request.get("protocol") != 1 or not isinstance(request.get("id"), str) or len(request["id"]) > 64:
                        await connection.send(json.dumps({"ok": False, "error": "invalid request envelope"}))
                        continue
                    method = request.get("method")
                    if method == "status":
                        try:
                            result = self.status(request.get("requestId"))
                            await connection.send(json.dumps({"id": request["id"], "ok": True, "result": result}))
                        except (JobError, OSError, json.JSONDecodeError) as error:
                            await connection.send(json.dumps({"id": request["id"], "ok": False, "error": str(error)[:200]}))
                    elif method == "preview":
                        # Keep the newest not-yet-rendered preview in a short burst.
                        newest = request
                        while True:
                            try:
                                next_raw = await asyncio.wait_for(connection.recv(), COALESCE_SECONDS)
                            except TimeoutError:
                                break
                            try:
                                candidate = json.loads(next_raw)
                            except (ValueError, TypeError):
                                queued = next_raw
                                break
                            if isinstance(candidate, dict) and candidate.get("method") == "preview" and candidate.get("protocol") == 1 and isinstance(candidate.get("id"), str) and len(candidate["id"]) <= 64:
                                await connection.send(json.dumps({"id": newest["id"], "ok": False, "error": "superseded"}))
                                newest = candidate
                            else:
                                queued = next_raw
                                break
                        try:
                            job_path = await asyncio.to_thread(publish_recipe, newest.get("recipe"), self.inbox, self.inbox / "status.json")
                            await connection.send(json.dumps({"id": newest["id"], "ok": True,
                                                              "result": {"requestId": job_path.stem}}))
                        except (JobError, ValueError, OSError, json.JSONDecodeError) as error:
                            await connection.send(json.dumps({"id": newest["id"], "ok": False, "error": str(error)[:200]}))
                    else:
                        await connection.send(json.dumps({"id": request["id"], "ok": False, "error": "unknown method"}))
            except (ConnectionClosed, TimeoutError, ValueError):
                return

    def status(self, request_id):
        status_path = self.inbox / "status.json"
        status = json.loads(status_path.read_text(encoding="utf-8")) if status_path.exists() else {"canApply": False}
        result = {"editor": status}
        if request_id is not None:
            if not isinstance(request_id, str) or len(request_id) != 36 or any(char not in "0123456789abcdef-" for char in request_id):
                raise JobError("invalid request ID")
            response = self.inbox / "responses" / (request_id + ".json")
            result["operation"] = json.loads(response.read_text(encoding="utf-8")) if response.exists() else {"state": "pending"}
        return result


async def run(inbox: Path, port: int, token_path: Path) -> None:
    if not 1024 <= port <= 65535:
        raise ValueError("port must be 1024..65535")
    for folder in ("requests", "images", "responses"):
        (inbox / folder).mkdir(parents=True, exist_ok=True)
    token = ensure_token(token_path)
    broker = Broker(inbox, token)
    async with serve(broker.handle, "127.0.0.1", port, origins=[None], compression=None,
                     max_size=MAX_MESSAGE, max_queue=8) as server:
        print(f"Barrowlight preview broker on ws://127.0.0.1:{port}; token file: {token_path}", flush=True)
        await server.serve_forever()


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("inbox", type=Path)
    parser.add_argument("--port", type=int, default=8765)
    parser.add_argument("--token-file", type=Path)
    args = parser.parse_args()
    asyncio.run(run(args.inbox, args.port, args.token_file or args.inbox / "broker.token"))
