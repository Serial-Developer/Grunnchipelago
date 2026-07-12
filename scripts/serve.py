#!/usr/bin/env python3
"""Launch a local Archipelago server (MultiServer.py) for testing the client mod.

    python scripts/serve.py <path-to .archipelago or AP_*.zip> [port]

Defaults to port 38281 (matches the mod's default config). Uses the Archipelago
checkout's own Python if run with it; otherwise pass the checkout venv explicitly.
"""

from __future__ import annotations

import os
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
AP = REPO / "Archipelago"


def main() -> None:
    if len(sys.argv) < 2:
        raise SystemExit(__doc__)
    data = Path(sys.argv[1]).resolve()
    port = sys.argv[2] if len(sys.argv) > 2 else "38281"
    if not data.exists():
        raise SystemExit(f"not found: {data}")
    if not (AP / "MultiServer.py").exists():
        raise SystemExit(f"Archipelago checkout not found at {AP}")

    os.environ["SKIP_REQUIREMENTS_UPDATE"] = "1"
    os.chdir(AP)
    sys.argv = ["MultiServer.py", str(data), "--port", str(port)]
    print(f"[serve] hosting {data.name} on port {port} (Ctrl+C to stop)")
    runpy_path = AP / "MultiServer.py"
    import runpy

    runpy.run_path(str(runpy_path), run_name="__main__")


if __name__ == "__main__":
    main()
