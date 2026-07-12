#!/usr/bin/env python3
"""Build a distributable grunn.apworld from the canonical world source.

An .apworld is just a zip whose root holds the world package folder (grunn/...).
Output: dist/grunn.apworld

    python scripts/build_apworld.py
"""

from __future__ import annotations

import zipfile
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
SRC_PARENT = REPO / "apworld"          # zip arcnames become "grunn/..."
WORLD = SRC_PARENT / "grunn"
OUT = REPO / "dist" / "grunn.apworld"

EXCLUDE_DIRS = {"__pycache__"}
EXCLUDE_SUFFIXES = {".pyc"}


def main() -> None:
    if not WORLD.is_dir():
        raise SystemExit(f"canonical world not found: {WORLD}")
    OUT.parent.mkdir(parents=True, exist_ok=True)

    files = sorted(
        f
        for f in WORLD.rglob("*")
        if f.is_file()
        and not any(part in EXCLUDE_DIRS for part in f.relative_to(SRC_PARENT).parts)
        and f.suffix not in EXCLUDE_SUFFIXES
    )
    with zipfile.ZipFile(OUT, "w", zipfile.ZIP_DEFLATED) as zf:
        for f in files:
            zf.write(f, f.relative_to(SRC_PARENT).as_posix())

    print(f"built {OUT}  ({len(files)} files, {OUT.stat().st_size} bytes)")


if __name__ == "__main__":
    main()
