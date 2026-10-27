"""Measure live preview acknowledgement latency through the broker and Pixelorama inbox."""
import argparse
import asyncio
import json
from pathlib import Path
from statistics import median
from time import perf_counter

from websockets.asyncio.client import connect

COLORS = ["#D7B46A", "#7FA89B", "#A84B45", "#A29BC4"]


async def exchange(client, payload):
    await client.send(json.dumps(payload, separators=(",", ":")))
    return json.loads(await asyncio.wait_for(client.recv(), 3))


async def run(inbox: Path, port: int, count: int, frequency: float):
    token = (inbox / "broker.token").read_text(encoding="ascii").strip()
    async with connect(f"ws://127.0.0.1:{port}") as client:
        await client.send(json.dumps({"type": "auth", "token": token}))
        authenticated = json.loads(await asyncio.wait_for(client.recv(), 3))
        if authenticated.get("type") != "authenticated":
            raise RuntimeError("broker authentication failed")
        latencies = []
        for index in range(count):
            status = await exchange(client, {"protocol": 1, "id": f"status-{index}", "method": "status"})
            target = status["result"]["editor"].get("target", {})
            if not status["result"]["editor"].get("canApply"):
                raise RuntimeError("connect the Pixelorama preview panel to this inbox and select an ordinary pixel cel")
            width, height = target["width"], target["height"]
            color = COLORS[index % len(COLORS)]
            recipe = {"version": 1, "width": width, "height": height, "background": "transparent", "layers": [
                {"name": "live-demo", "operations": [
                    {"type": "rectangle", "from": [2, 2], "to": [width - 3, height - 3], "color": "#151923", "filled": True},
                    {"type": "ellipse", "from": [8, 8], "to": [width - 9, height - 9], "color": color, "filled": False}]}
            ]}
            start = perf_counter()
            submitted = await exchange(client, {"protocol": 1, "id": f"preview-{index}", "method": "preview", "recipe": recipe})
            if not submitted.get("ok"):
                raise RuntimeError(submitted)
            request_id = submitted["result"]["requestId"]
            while perf_counter() - start < 3:
                observed = await exchange(client, {"protocol": 1, "id": f"poll-{index}", "method": "status", "requestId": request_id})
                operation = observed["result"]["operation"]
                if operation["state"] == "previewed":
                    latencies.append((perf_counter() - start) * 1000)
                    break
                if operation["state"] not in ("pending", "previewed"):
                    raise RuntimeError(operation)
                await asyncio.sleep(0.01)
            else:
                raise TimeoutError(f"Pixelorama did not acknowledge {request_id}")
            await asyncio.sleep(max(0, 1 / frequency - (perf_counter() - start)))
        ordered = sorted(latencies)
        p95 = ordered[min(len(ordered) - 1, int(0.95 * len(ordered)))]
        print(json.dumps({"samples": len(latencies), "frequencyHz": frequency,
                          "medianMs": round(median(latencies), 1), "p95Ms": round(p95, 1),
                          "minMs": round(ordered[0], 1), "maxMs": round(ordered[-1], 1)}))


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("inbox", type=Path)
    parser.add_argument("--port", type=int, default=8765)
    parser.add_argument("--count", type=int, default=10)
    parser.add_argument("--frequency", type=float, default=5)
    args = parser.parse_args()
    if not 1 <= args.count <= 100 or not 1 <= args.frequency <= 15:
        parser.error("count must be 1..100 and frequency 1..15 Hz")
    asyncio.run(run(args.inbox, args.port, args.count, args.frequency))
