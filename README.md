# Grunnchipelago

[Archipelago](https://archipelago.gg) randomizer support for **Grunn** (Sokpop Collective, 2024):
a Python apworld, a BepInEx client mod, and the tooling used to derive the logic from the game.

> Unofficial, fan-made project. Not affiliated with or endorsed by Sokpop Collective.
> Grunn is the property of Tom van den Boogaart / Sokpop Collective — **no game asset, binary
> or decompiled source is redistributed here**. You need your own copy of the game.

## For players

| | |
|---|---|
| Install and connect | [`docs/setup_en.md`](apworld/grunn/docs/setup_en.md) — français : [`setup_fr.md`](apworld/grunn/docs/setup_fr.md) |
| Goals, options, what gets randomized | [`docs/en_Grunn.md`](apworld/grunn/docs/en_Grunn.md) |
| Downloads (`grunn.apworld`, client mod) | [Releases](../../releases) |

Only the person generating the seed needs `grunn.apworld`. Every player needs the client mod.

## Repository layout

| Path | What |
|---|---|
| `apworld/grunn/` | **Canonical apworld source** (the world package). Edit here, not in the AP checkout. |
| `Grunnchipelago.Client/` | **The BepInEx client mod** (C#) that players install into Grunn. |
| `Grunnchipelago.Dumper/` | Standalone runtime dumper that produced the scene dump. Not needed to play. |
| `design/` | Design docs: `apworld_design.md`, `regions.md`, and `ids.json` (item/location IDs — source of truth). |
| `dump/` | Scene dump (`grunnchipelago_dump.json`), the derived `zone_logic.*`, and the scripts that build them. |
| `scripts/` | `sync_apworld.py`, `build_apworld.py`, `serve.py`, `send_deathlink.py`. |
| `players/` | Template and example YAMLs. |
| `Archipelago/` | Local AP checkout used for testing — **not versioned**, see `.gitignore`. |

## Building

### The apworld

Needs Python 3.11+ and a checkout of [ArchipelagoMW/Archipelago](https://github.com/ArchipelagoMW/Archipelago)
at tag `0.6.7` placed in `Archipelago/`, with the core generation dependencies installed
(`pip install -r Archipelago/requirements.txt` in a virtualenv).

```sh
python scripts/build_apworld.py      # -> dist/grunn.apworld
```

To run the tests, push the world into the checkout first:

```sh
python scripts/sync_apworld.py
cd Archipelago && python -m unittest worlds.grunn.test.test_generation
```

### The client mod

Needs the .NET SDK (8.0+) and a Grunn installation with BepInEx 5 (x64) already set up:
the project references the game's assemblies and BepInEx's directly, so they must exist locally.

The project defaults to the usual Steam location. If yours differs, override `GameDir`:

```sh
dotnet build Grunnchipelago.Client -c Release -p:GameDir="D:\Games\Grunn"
```

The build drops the DLL into `<GameDir>/BepInEx/plugins/Grunnchipelago/`.

## Contributing

Issues and pull requests are welcome.

Two conventions worth keeping if you touch the world:

- **Traceability.** Every rule in `rules.py` / `regions.py` carries a comment pointing at its
  source (`regions.md:`, `dump:` or `code:`). Nothing in the logic is guesswork, and it should
  stay that way.
- **IDs are permanent.** Never renumber anything in `design/ids.json`, including entries that
  were removed — their ids stay reserved so old seeds keep resolving.

## Credits

- **Grunn** by Tom van den Boogaart / [Sokpop Collective](https://sokpop.co).
- The [Archipelago](https://archipelago.gg) multiworld framework and its community.
- Apworld and client mod by Serial-Developer.

Released under the MIT License — see [`LICENSE`](LICENSE).
