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

# --- Unsourced locations --------------------------------------------------------
# "Calm Ghost #8" does NOT exist in game [2026-07-16, confirmed in dump + code]:
# the scene holds 8 ghost objects but only 7 GhostTouch interactions, and the 8th
# object (*** GHOSTS ***/ghost0_backup) sits at the EXACT position of
# ghost0_pillarspace with no interaction of its own - a spare left in the scene.
# SaveManager.ghostCalmMax = 7 (SaveManager.cs:1351) confirms the real count.
# The id stays reserved in ids.json, but the location is never created: an item
# placed there would be permanently unobtainable (seed 7 put the Compass on it).
#
# "Polaroid: Demon" is DEAD CONTENT [2026-07-22, confirmed in-game + full-assembly
# decompile]: the Demon polaroid is NEVER granted by the shipped game. There is no scene
# object of that type (dump + the mod's Hell/Crypt scene dump both report it absent), no
# hardcoded AddPolaroidSolved(PolaroidType.Demon) anywhere, the only generic collect path
# is Polaroid.Trigger() (scene-object driven, so no Demon), and the sole "collect all 47
# polaroids" loop sits inside `if (Application.isEditor) { if (false) { ... } }` = dead
# code. Its enum value only appears in DefinePolaroidString (the name lookup). An item
# placed here can never be checked (seed #3 stranded SoulFragment2 on it), so the location
# is never created - the id stays reserved in ids.json.
#
# "Polaroid: VoidSkeleton" is ALSO dead content [2026-07-27, confirmed in-game + code].
# Its scene object (Main/Polaroids/polaroid_skeletonVoid0) exists in the dump but is never
# activated: it has NO reveal ContentHider (unlike Tent / GardenGnomes, which are event- or
# day-revealed and collect fine), NO hardcoded grant, and the game explicitly strips it as
# "unused" every load (SaveManager.RemoveAndAddCertainPolaroids ->
# RemoveUnusedPolaroid(PolaroidType.VoidSkeleton)). The client's model-swap pass confirmed
# it: 31/34 polaroids present at connect, the 3 absent being Tent + GardenGnomes (legit,
# revealed later) + VoidSkeleton. seed #4 stranded ShyIdol on it -> Hell unreachable.
# "Polaroid: GardenGnomes" is dead content too [2026-07-27, proven in-game]. Its scene
# object exists in the dump (GnomeForest, inside bigMushroom0) but is NEVER instantiated:
# the client scans GameManager.allPolaroids every frame and never saw it, even while the
# player stood ~3 m away (log: "Granted KidTrumpet" - that pickup is 2.83 m from the
# polaroid - and GnomeForestDoor solved in the same seconds). It has no reveal hider and no
# grant anywhere in the code (only its name case in DefinePolaroidString), exactly like
# VoidSkeleton. Control: AppleAndWorm (also non-euclidian, AppleSpace) IS in allPolaroids
# and collects fine, so the absence is specific to this object, not to non-euclidian zones.
# seed hut stranded GnomeIdol (a REQUIRED idol) on it -> Hell unreachable.
# "Obtain OldKey" and "Obtain AbandonedKey" are NOT reachable checks [2026-07-27]:
# neither key is actually usable/pickable in the shipped game (OldKey was shelved from the
# item pool on my request; AbandonedKey is only used BY US, to lock the player hut -
# it has no vanilla check). Keeping their "Obtain X" locations made them dead checks: seed
# hut_s7655... stranded the Trowel on "Obtain OldKey" -> run blocked.
# NOTE: the AbandonedKey ITEM stays in the pool (lock_player_hut needs it); only its
# location is removed. Their rules are removed from OBTAIN_RULES as well.
UNSOURCED_LOCATIONS = {
    "Calm Ghost #8 (PillarSpace)",
    "Polaroid: Demon",
    "Polaroid: VoidSkeleton",
    "Polaroid: GardenGnomes",
    "Obtain OldKey",
    "Obtain AbandonedKey",
}

KEYITEM_LOCS = [n for n, cat in LOCATION_CATEGORY.items() if cat == "keyitem"]
ENDING_LOCS = [n for n, cat in LOCATION_CATEGORY.items() if cat == "ending"]
POLAROID_LOCS = [
    n for n, cat in LOCATION_CATEGORY.items()
    if cat == "polaroid" and n not in UNSOURCED_LOCATIONS
]
GHOST_LOCS = [
    n for n, cat in LOCATION_CATEGORY.items()
    if cat == "ghost" and n not in UNSOURCED_LOCATIONS
]
GULDEN_LOCS = [n for n, cat in LOCATION_CATEGORY.items() if cat == "gulden"]
# "Deed" checks (demande 2026-07-28): rewarded ACTIONS rather than pickups - use an
# item on the right spot, complete the school band... Like the "Obtain X" and ending checks
# they are LOGICAL (placed in Menu and gated entirely by rules.DEED_RULES), because what
# gates them is holding an item, not standing somewhere. Ids: block 478661500+; the next
# free one is 478661508 (reserved for the potted plants, postponed post-launch - the
# position of the 8 pots is not in the dump; see design/backlog_checks.md).
DEED_LOCS = [n for n, cat in LOCATION_CATEGORY.items() if cat == "deed"]
# "Chore" checks (demande 2026-07-30): the five START-GARDEN maintenance jobs.
# Vanilla pays 2 gulden the first time each is completed in the garden ONLY
# (GameManager.areaCompleteGuldenAdd = 2, guarded by the five
# <verb>All<Thing>InStartGardenArea flags); those payouts become checks, and the pool gets
# five "Golden Gulden" worth 2 each in exchange. Logical checks, like the deeds.
# Ids: block 478661600+.
CHORE_LOCS = [n for n, cat in LOCATION_CATEGORY.items() if cat == "chore"]

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
    # "Polaroid: Demon" is intentionally ABSENT: it is dead content, never granted by the
    # game, so it is excluded via UNSOURCED_LOCATIONS above (never created). Kept out of
    # this map on purpose - resolved 2026-07-22 (was a playtest FLAG).
    "Polaroid: GnomeForestDoor": c.EXTERIEUR,  # Road
    "Polaroid: Van": c.EXTERIEUR,
    # SPAWN SIDE, not the Exterieur [2026-07-31, confirmed in dump]: the polaroid sits
    # against the CLOSED high-bridge door - polaroid_highBridgeKey0 at (-38.12, 10.17, -48.4)
    # vs highBridgeDoor0 at (-39.98, 11.76, -46.39), i.e. 2.7 m apart - on the road side the
    # player starts on. It needs NO key at all. Region Menu, so it joins "Obtain BridgeKey"
    # (and the bus gulden under coinsanity) in the true starting sphere: three checks before
    # the bridge, exactly as described.
    "Polaroid: HighBridgeKey": c.MENU,
    "Polaroid: HangjongPizzaBox": c.EXTERIEUR,
    "Polaroid: OldLadyBackGarden": c.PARC,
    "Polaroid: LighterMolehill": c.PARC,
    "Polaroid: WateringCan": c.PARC,
    "Polaroid: PurifiedStone": c.PARC,
    # "Polaroid: VoidSkeleton": excluded via UNSOURCED_LOCATIONS (dead content, never
    # granted). Mapping kept out on purpose - resolved 2026-07-27.
    "Polaroid: AppleAndWorm": c.APPLE_SPACE,
    # "Polaroid: GardenGnomes": excluded via UNSOURCED_LOCATIONS (dead content, never
    # instantiated in the scene). Mapping kept out on purpose - resolved 2026-07-27.
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
    # "Calm Ghost #8 (PillarSpace)": never created - see UNSOURCED_LOCATIONS.
}

GULDEN_REGION: dict[str, str] = {
    "Gulden #1 (GasStationOffice)": c.GAS_OFFICE,
    "Gulden #2 (Unknown)": c.EXTERIEUR,   # bench in the shelter facing the gas station, free [J in-game 2026-07-13]
    "Gulden #3 (Bunker)": c.BUNKER,
    "Gulden #4 (StartGarden)": c.JARDIN,
    "Gulden #5 (Road)": c.MENU,   # bus-arrival gulden, at spawn (sphere 1, before BridgeKey)
    "Gulden #6 (StartGarden)": c.JARDIN,
    "Gulden #7 (Road)": c.EXTERIEUR,
    "Gulden #8 (Unknown)": c.EXTERIEUR,   # pot to smash on the road; needs Hammer (rule in rules.py) [2026-07-13]
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
    unsourced_locs = {f"Obtain {name}" for name in UNSOURCED_ITEMS} | UNSOURCED_LOCATIONS
    for name in KEYITEM_LOCS:
        if name in unsourced_locs:
            continue  # e.g. "Obtain Cymbals" / "Obtain OldKey" - no in-game source
        _add(world, c.MENU, name)
    # exclude_bad_endings (demande 2026-07-30): drop the checks of the 8 endings
    # that KILL you - exactly the DeathLink set - so nobody is forced to die (and, under
    # DeathLink, to kill everyone else) just to collect a check. Never applied on the
    # all_endings goal, which requires meeting every ending anyway.
    skip_endings: set[str] = set()
    if world.options.exclude_bad_endings and world.options.goal.value != c.GOAL_ALL:
        skip_endings = {f"Ending: {name}" for name in c.DEATH_ENDINGS}
    for name in ENDING_LOCS:
        if name in skip_endings:
            continue
        _add(world, c.MENU, name)
    for name in DEED_LOCS:
        _add(world, c.MENU, name)
    if world.options.chore_checks:
        for name in CHORE_LOCS:
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
