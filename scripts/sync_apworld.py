#!/usr/bin/env python3
"""Sync the canonical world source into the local Archipelago checkout for testing.

Copies  apworld/grunn/  ->  Archipelago/worlds/grunn/  (the checkout is NOT versioned).
Run this after editing anything under apworld/grunn/, then run the world's tests with
the checkout's Python, e.g.:

    python scripts/sync_apworld.py
    cd Archipelago && .venv313/Scripts/python -m unittest worlds.grunn.test.test_generation
"""

from __future__ import annotations

import shutil
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
SRC = REPO / "apworld" / "grunn"
DST = REPO / "Archipelago" / "worlds" / "grunn"


def main() -> None:
    if not SRC.is_dir():
        raise SystemExit(f"canonical world not found: {SRC}")
    if not DST.parent.is_dir():
        raise SystemExit(
            f"Archipelago checkout not found at {DST.parent}. Clone Archipelago 0.6.7 "
            f"into {REPO / 'Archipelago'} first."
        )
    if DST.exists():
        shutil.rmtree(DST)
    shutil.copytree(SRC, DST, ignore=shutil.ignore_patterns("__pycache__", "*.pyc"))
    print(f"synced {SRC}  ->  {DST}")


if __name__ == "__main__":
    main()
