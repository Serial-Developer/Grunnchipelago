# Grunnchipelago

Archipelago randomizer support for **Grunn** (Sokpop Collective, 2024): a Python
apworld plus the tooling used to derive its logic from the game.

## Layout

| Path | What |
|---|---|
| `apworld/grunn/` | **Canonical apworld source** (the world package). Edit here. |
| `design/` | Design docs: `apworld_design.md`, `regions_v3.md`, `ids.json` (item/location IDs — source of truth). |
| `dump/` | Scene dump (`grunnchipelago_dump.json`) + derived `zone_logic.*` and the scripts that build them. |
| `Grunnchipelago.Dumper/` | The C# runtime dumper that produced the scene dump. |
| `scripts/` | `sync_apworld.py`, `build_apworld.py`. |
| `Archipelago/` | Local AP 0.6.7 checkout for testing — **not versioned** (see `.gitignore`). |

## Workflow

```sh
# 1. Edit the world under apworld/grunn/
# 2. Push it into the Archipelago checkout and run the tests
python scripts/sync_apworld.py
cd Archipelago && .venv313/Scripts/python -m unittest worlds.grunn.test.test_generation

# 3. Build a distributable apworld
python scripts/build_apworld.py   # -> dist/grunn.apworld
```

The `Archipelago/` checkout is a shallow clone of `ArchipelagoMW/Archipelago` at tag
`0.6.7`, with a Python 3.13 venv (`.venv313`) holding the core generation deps.

## Rules

Everything here is **local**. Do not push to any remote or open a PR against
ArchipelagoMW/Archipelago without explicit sign-off.
