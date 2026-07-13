"""Location table for the Grunn apworld.

The name->id table and the 5 categories (keyitem / ending / polaroid / ghost /
gulden) come verbatim from ``data/ids.json``.

Placement model (design/apworld_design.md section 1 - "1 location per logical item"):
- Key-item "Obtain X" locations and the 11 ending locations are *logical* checks:
  they are placed in the origin (Menu) region and gated entirely by explicit access
  rules in rules.py (so multi-source items become simple OR rules).
- Polaroid / ghost / gulden checks are *physical*: they live in the map region that
  contains them (from the scene dump), so reaching that region is enough. Their
  region assignments are below.
"""

from __future__ import annotations

from typing import TYPE_CHECKING

from BaseClasses import Location

from . import constants as c
from .items import IDS, UNSOURCED_ITEMS

if TYPE_CHECKING:
    from . import GrunnWorld

LOCATION_NAME_TO_ID: dict[str, int] = {name: data["id"] for name, data in IDS["locations"].items()}
LOCATION_CATEGORY: dict[str, str] = {name: data["category"] for name, data in IDS["locations"].items()}

KEYITEM_LOCS = [n for n, cat in LOCATION_CATEGORY.items() if cat == "keyitem"]
ENDING_LOCS = [n for n, cat in LOCATION_CATEGORY.items() if cat == "ending"]
POLAROID_LOCS = [n for n, cat in LOCATION_CATEGORY.items() if cat == "polaroid"]
GHOST_LOCS = [n for n, cat in LOCATION_CATEGORY.items() if cat == "ghost"]
GULDEN_LOCS = [n for n, cat in LOCATION_CATEGORY.items() if cat == "gulden"]

# --- Physical-check region assignments -----------------------------------------
# Derived from the scene dump (polaroid type -> area, ghost/gulden path -> area),
# folded into map regions. Entries flagged FLAG are best guesses to review.
POLAROID_REGION: dict[str, str] = {
    "Polaroid: MagpieNest": c.JARDIN,
    "Polaroid: BackGardenFence": c.JARDIN,
    "Polaroid: BackGardenCarTrunk": c.JARDIN,
    "Polaroid: GasStation": c.JARDIN,          # object physically in StartGarden
    "Polaroid: DeadGardener": c.JARDIN,
    "Polaroid: GnomeIdol": c.JARDIN,
    "Polaroid: PlayerShed": c.JARDIN,
    "Polaroid: TallManWindow": c.JARDIN,
    "Polaroid: Crypt": c.JARDIN,               # object physically in StartGarden
    "Polaroid: BoatPaddle": c.EGLISE,
    "Polaroid: Tent": c.EGLISE,                # object physically at Church
    "Polaroid: RedDoor": c.EGLISE,
    "Polaroid: ChurchOutsideDoor": c.EGLISE,
    "Polaroid: FlowerDoor": c.EGLISE,          # ChurchBigHall
    "Polaroid: ChurchKey": c.EGLISE,           # ChurchHallway
    # Only polaroid with no scene object in the dump (granted by an unfound code path).
    # Placed in Hell = strictest region, no accessibility false positive [J 2026-07-13].
    # FLAG playtest: confirm where it is actually awarded.
    "Polaroid: Demon": c.HELL,
    "Polaroid: GnomeForestDoor": c.EXTERIEUR,  # Road
    "Polaroid: Van": c.EXTERIEUR,
    "Polaroid: HighBridgeKey": c.EXTERIEUR,
    "Polaroid: HangjongPizzaBox": c.EXTERIEUR,
    "Polaroid: OldLadyBackGarden": c.PARC,
    "Polaroid: LighterMolehill": c.PARC,
    "Polaroid: WateringCan": c.PARC,
    "Polaroid: PurifiedStone": c.PARC,
    "Polaroid: VoidSkeleton": c.GAS_STATION,
    "Polaroid: AppleAndWorm": c.APPLE_SPACE,
    "Polaroid: GardenGnomes": c.PASSAGE_GNOMES,
    "Polaroid: Compass": c.LABYRINTHE,
    "Polaroid: HooibaalTuin": c.HOOIBAAL,
    "Polaroid: GasStationComputer": c.HOOIBAAL,
    "Polaroid: ToiletStall": c.CHAMP_MAIS,
    "Polaroid: Gnome": c.TOILET,
    "Polaroid: Well": c.TENTE,
    "Polaroid: Ferry": c.CABANE_PECHEUR_INT,  # inside the fisherman's cabin (VeerbootHuis)
    "Polaroid: Bone": c.CABANE_PECHEUR,        # on the windy path approach
}

GHOST_REGION: dict[str, str] = {
    "Calm Ghost #1 (Road)": c.EXTERIEUR,
    "Calm Ghost #2 (Bunker)": c.BUNKER,
    "Calm Ghost #3 (Road)": c.EXTERIEUR,
    "Calm Ghost #4 (WindyPath)": c.CABANE_PECHEUR,
    "Calm Ghost #5 (Void)": c.VOID,
    "Calm Ghost #6 (GnomeForest)": c.PASSAGE_GNOMES,
    "Calm Ghost #7 (PillarSpace)": c.PILLAR_SPACE,
    "Calm Ghost #8 (PillarSpace)": c.PILLAR_SPACE,
}

GULDEN_REGION: dict[str, str] = {
    "Gulden #1 (GasStationOffice)": c.GAS_OFFICE,
    "Gulden #2 (Unknown)": c.EXTERIEUR,   # bench in the shelter facing the gas station, free [J in-game 2026-07-13]
    "Gulden #3 (Bunker)": c.BUNKER,
    "Gulden #4 (StartGarden)": c.JARDIN,
    "Gulden #5 (Road)": c.MENU,   # bus-arrival gulden, at spawn (sphere 1, before BridgeKey)
    "Gulden #6 (StartGarden)": c.JARDIN,
    "Gulden #7 (Road)": c.EXTERIEUR,
    "Gulden #8 (Unknown)": c.EXTERIEUR,   # pot to smash on the road; needs Hammer (rule in rules.py) [J 2026-07-13]
    "Gulden #9 (Church)": c.EGLISE,
    "Gulden #10 (StartGarden)": c.JARDIN,
    "Gulden #11 (Park)": c.PARC,
    "Gulden #12 (Park)": c.PARC,
    "Gulden #13 (PillarSpace)": c.PILLAR_SPACE,
    "Gulden #14 (Ferry)": c.FERRY,
    "Gulden #15 (Intratuin)": c.JARDIN,   # Intratuin folded into Jardin
}


class GrunnLocation(Location):
    game = "Grunn"


def _add(world: "GrunnWorld", region_name: str, loc_name: str) -> None:
    region = world.get_region(region_name)
    region.add_locations({loc_name: LOCATION_NAME_TO_ID[loc_name]}, GrunnLocation)


def create_all_locations(world: "GrunnWorld") -> None:
    # Logical checks (Menu): every sourced key item + every ending. Always created;
    # the locally-kept ones (BridgeKey/Shears) get their vanilla item locked in later.
    unsourced_locs = {f"Obtain {name}" for name in UNSOURCED_ITEMS}
    for name in KEYITEM_LOCS:
        if name in unsourced_locs:
            continue  # e.g. "Obtain Cymbals" - no in-game source, id kept reserved
        _add(world, c.MENU, name)
    for name in ENDING_LOCS:
        _add(world, c.MENU, name)

    # Physical checks, gated only by reaching their region.
    if world.options.polaroid_checks:
        for name in POLAROID_LOCS:
            _add(world, POLAROID_REGION[name], name)
    if world.options.ghost_checks:
        for name in GHOST_LOCS:
            _add(world, GHOST_REGION[name], name)
    if world.options.coinsanity:
        for name in GULDEN_LOCS:
            _add(world, GULDEN_REGION[name], name)
