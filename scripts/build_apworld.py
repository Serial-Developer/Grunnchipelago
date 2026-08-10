#!/usr/bin/env python3
"""Build a distributable grunn.apworld from the canonical world source.

An .apworld is just a zip whose root holds the world package folder (grunn/...).
Output: dist/grunn.apworld

    python scripts/build_apworld.py

The two container fields - "version" and "compatible_version" - describe the APContainer
PACKAGING scheme, not the world. Per `docs/apworld specification.md` ("Do not write these
fields yourself") they must be absent from the source manifest and injected here, at
packaging time; Archipelago's own test suite fails a world whose folder manifest declares
them (test/general/test_world_manifest.py::test_no_container_version). We read them from
the AP checkout so a future bump of the scheme is picked up automatically instead of going
stale in a hand-written file.
"""

from __future__ import annotations

import json
import sys
import zipfile
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
SRC_PARENT = REPO / "apworld"          # zip arcnames become "grunn/..."
WORLD = SRC_PARENT / "grunn"
AP = REPO / "Archipelago"
OUT = REPO / "dist" / "grunn.apworld"

MANIFEST = "archipelago.json"
EXCLUDE_DIRS = {"__pycache__"}
EXCLUDE_SUFFIXES = {".pyc"}


def container_versions() -> tuple[int, int]:
    """(version, compatible_version) for an .apworld, straight from the AP checkout.

    Mirrors worlds/Files.py: APContainer.get_manifest uses the module-level
    `container_version`, and APWorldContainer.get_manifest overrides
    `compatible_version` with 7 (Files.py:213).
    """
    if not (AP / "worlds" / "Files.py").exists():
        raise SystemExit(f"Archipelago checkout not found at {AP} - needed for the manifest.")
    sys.path.insert(0, str(AP))
    from worlds.Files import APWorldContainer, container_version  # noqa: E402

    compatible = APWorldContainer.get_manifest.__defaults__  # unused, kept for clarity
    del compatible
    # APWorldContainer hardcodes its own compatible_version; read it without building a
    # container by calling get_manifest on a bare instance.
    probe = APWorldContainer.__new__(APWorldContainer)
    probe.game = None
    probe.world_version = None
    probe.minimum_ap_version = None
    probe.maximum_ap_version = None
    probe.author = None
    manifest = probe.get_manifest()
    return int(manifest["version"]), int(manifest["compatible_version"])


def main() -> None:
    if not WORLD.is_dir():
        raise SystemExit(f"canonical world not found: {WORLD}")
    OUT.parent.mkdir(parents=True, exist_ok=True)

    version, compatible_version = container_versions()
    manifest = json.loads((WORLD / MANIFEST).read_text(encoding="utf-8"))
    for reserved in ("version", "compatible_version"):
        if reserved in manifest:
            raise SystemExit(
                f"{MANIFEST} must not define '{reserved}' - it is injected at packaging "
                f"time (see docs/apworld specification.md)."
            )
    manifest["version"] = version
    manifest["compatible_version"] = compatible_version

    files = sorted(
        f
        for f in WORLD.rglob("*")
        if f.is_file()
        and not any(part in EXCLUDE_DIRS for part in f.relative_to(SRC_PARENT).parts)
        and f.suffix not in EXCLUDE_SUFFIXES
    )
    with zipfile.ZipFile(OUT, "w", zipfile.ZIP_DEFLATED) as zf:
        for f in files:
            arcname = f.relative_to(SRC_PARENT).as_posix()
            if arcname == f"{WORLD.name}/{MANIFEST}":
                zf.writestr(arcname, json.dumps(manifest, indent="\t") + "\n")
            else:
                zf.write(f, arcname)

    print(f"built {OUT}  ({len(files)} files, {OUT.stat().st_size} bytes)")
    print(f"  manifest: world {manifest['world_version']}, "
          f"container version {version} / compatible {compatible_version}")


if __name__ == "__main__":
    main()
