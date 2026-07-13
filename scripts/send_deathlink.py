#!/usr/bin/env python3
"""Send a test DeathLink to a running Archipelago server (for testing the Grunn mod).

    Archipelago/.venv313/Scripts/python scripts/send_deathlink.py [slot] [port] [cause]

Defaults: slot Grunn1, port 38281. Connects as a secondary text-only client on the
same slot (AP allows several clients per slot), tagged DeathLink, sends one Bounce,
then disconnects. The game mod (also tagged DeathLink) receives it.
"""

from __future__ import annotations

import asyncio
import json
import sys
import time
import uuid

import websockets


async def main() -> None:
    slot = sys.argv[1] if len(sys.argv) > 1 else "Grunn1"
    port = int(sys.argv[2]) if len(sys.argv) > 2 else 38281
    cause = sys.argv[3] if len(sys.argv) > 3 else "test deathlink from script"

    async with websockets.connect(f"ws://localhost:{port}", max_size=16 * 1024 * 1024) as ws:
        # RoomInfo arrives first.
        room_info = json.loads(await ws.recv())[0]
        version = room_info["version"]
        print(f"[send_deathlink] server AP {version['major']}.{version['minor']}.{version['build']}")

        await ws.send(json.dumps([{
            "cmd": "Connect",
            "game": "Grunn",
            "name": slot,
            "password": None,
            "uuid": uuid.uuid4().hex,
            "version": {"major": version["major"], "minor": version["minor"],
                        "build": version["build"], "class": "Version"},
            "items_handling": 0b000,          # secondary client: receive nothing
            "tags": ["DeathLink", "TextOnly"],
            "slot_data": False,
        }]))

        while True:
            for msg in json.loads(await ws.recv()):
                if msg["cmd"] == "Connected":
                    print(f"[send_deathlink] connected to slot '{slot}'.")
                    await ws.send(json.dumps([{
                        "cmd": "Bounce",
                        "tags": ["DeathLink"],
                        "data": {"time": time.time(), "source": "TestBot", "cause": cause},
                    }]))
                    print(f"[send_deathlink] DeathLink sent (cause: {cause}).")
                    await asyncio.sleep(0.5)   # let it flush before closing
                    return
                if msg["cmd"] == "ConnectionRefused":
                    raise SystemExit(f"refused: {msg.get('errors')}")


if __name__ == "__main__":
    asyncio.run(main())
