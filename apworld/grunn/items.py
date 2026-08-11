"""Item table for the Grunn apworld.

The name->id table and item categories are loaded verbatim from ``data/ids.json``
(the single source of truth generated from the game's enums). IDs and names are
never re-derived here, satisfying the "no divergent IDs" acceptance criterion.

Progression vs. useful classification follows design/apworld_design.md section 4,
refined so that exactly the items referenced by an access rule in rules.py are
marked ``progression``. See PROGRESSION_ITEMS below for the traceable list.
"""

from __future__ import annotations

import json
import pkgutil
from typing import TYPE_CHECKING

from BaseClasses import Item, ItemClassification

from . import constants as c

if TYPE_CHECKING:
    from . import GrunnWorld

# Read through pkgutil rather than open(): a shipped .apworld is a zip, so the file
# has no filesystem path there (FileNotFoundError at import time, and the world fails
# to load). pkgutil.get_data works for both the zipped and the extracted layout.
_IDS_RAW = pkgutil.get_data(__package__, "data/ids.json")
if _IDS_RAW is None:  # pragma: no cover - packaging error
    raise FileNotFoundError("grunn: data/ids.json is missing from the world package")
IDS = json.loads(_IDS_RAW.decode("utf-8"))

# name -> id and name -> category ("keyitem" | "buff" | "trap" | "filler")
ITEM_NAME_TO_ID: dict[str, int] = {name: data["id"] for name, data in IDS["items"].items()}
ITEM_CATEGORY: dict[str, str] = {name: data["category"] for name, data in IDS["items"].items()}

KEYITEMS = [n for n, c in ITEM_CATEGORY.items() if c == "keyitem"]
BUFFS = [n for n, c in ITEM_CATEGORY.items() if c == "buff"]
TRAPS = [n for n, c in ITEM_CATEGORY.items() if c == "trap"]  # filtered below
FILLER = [n for n, c in ITEM_CATEGORY.items() if c == "filler"]  # Gulden, Golden Gulden

# "Golden Gulden" (2026-07-30): the coin the 5 garden chores used to pay out. It is worth
# TWO gulden - exactly GameManager.areaCompleteGuldenAdd - so turning those payouts into
# checks costs the economy nothing. The client grants it as 2 gulden and shows a gilded coin.
GOLDEN_GULDEN_VALUE = 2

# How many Golden Gulden the pool owes. NOT "one per chore location": only the FIVE garden
# jobs ever paid 2 gulden (GameManager.areaCompleteGuldenAdd, guarded by the five
# <verb>All<Thing>InStartGardenArea flags). "Chore: Trim Every Potted Plant" pays NOTHING in
# vanilla - it only unlocks an achievement - so counting the category would mint two gulden
# out of thin air and inflate the coinsanity economy.
PAID_GARDEN_CHORES = 5

# --- Unsourced items ------------------------------------------------------------
# Systematic source audit (dump pickups + ObtainKeyItem() calls in *.cs): every key
# item has a source EXCEPT "Cymbals" (the non-"Kid" enum entry) - no pickup and no
# ObtainKeyItem(KeyItem.Cymbals). It is an unused enum entry. Its id stays reserved in
# ids.json, but neither its item nor its "Obtain Cymbals" location is created.
UNSOURCED_ITEMS = {"Cymbals"}

# --- Unimplementable items ------------------------------------------------------
# EMPTY since 2026-07-27. "Regrow Grass Trap" (now "Park Reset Trap") used to live here:
# the grass is rendered by the DOTS GrassSystem, and the earlier client could only drop
# the zone counter without regrowing anything, stranding zone-completion checks. The
# rebuild path was then found in the game itself - GameManager.ResetWorld (GameManager.cs
# :4064) does GrassManager.ClearEntities() + Reset() + CornManager.Reset() LIVE, and the
# save-file cut positions are replayed through performedLoadOperations/PerformLoadOperations
# (GameManager.cs:874). The client now uses that exact path (Effects.ResetGrassInArea), so
# the grass really does grow back and stays cuttable.
UNIMPLEMENTED_ITEMS: set[str] = set()

TRAPS = [n for n in TRAPS if n not in UNIMPLEMENTED_ITEMS]

# --- Items shelved from the item pool (kept obtainable in-game) ------------------
# OldKey unlocks NOTHING [J 2026-07-27, confirmed: zero doors list it in
# unlockItemNeeded, zero interactions use it, and the only rule mentioning it is its own
# "Obtain OldKey" pickup - never a has("OldKey") requirement]. It is shelved from the
# item pool while its purpose is reconsidered: the ITEM is not created (its slot becomes
# filler, so item/location parity is preserved by create_filler), but the "Obtain OldKey"
# LOCATION stays a normal check. Reversible: drop the name here to put it back.
POOL_SHELVED_ITEMS = {"OldKey"}

# AbandonedKey joins them when lock_player_hut is OFF: the option is the ONLY thing that
# gives that key a use (v0.3 door table - no vanilla door lists it), so without it the key
# is dead weight in the pool [J 2026-08-08]. With the option ON it is a real gate and stays.
# Its "Obtain AbandonedKey" location is a separate matter: it was removed on 2026-07-27
# because the key has no vanilla pickup, which made it a dead check (see
# locations.UNSOURCED_LOCATIONS).


def shelved_items(world: "GrunnWorld") -> set[str]:
    """Items kept out of the pool for this slot. Their slots become filler, so the
    item/location parity in create_all_items is preserved by create_filler."""
    shelved = set(POOL_SHELVED_ITEMS)
    if not world.options.lock_player_hut:
        shelved.add("AbandonedKey")
    return shelved


# --- Key items downgraded to filler ---------------------------------------------
# Popcorn is inert [J 2026-08-08]: it does nothing in game and appears in no access rule.
# Corn and Butter DO gate its cooking (rules.py "Popcorn"), so they stay progression -
# Popcorn itself is the end of that chain and gates nothing further.
FILLER_KEY_ITEMS = {"Popcorn"}

# --- Progression classification -------------------------------------------------
# Items referenced by a region entrance or an "Obtain X" / ending rule in rules.py are
# marked progression (so fill and all-state reachability count them), plus the keys that
# open a vanilla door even when the logic has an alternative route for them (GardenKey,
# StrangeKey - decision Jonath 2026-08-08).
# (design/apworld_design.md section 4 + regions.md 2026-07-12 corrections)
PROGRESSION_ITEMS = {
    # traversal / equivalence helpers
    "Shears", "MagicSword", "WateringCan", "Coin", "Plank", "OldPlank",
    "Lighter", "Paddle", "OfficeKey", "Cd", "Compass", "Hammer",
    "ToiletKey", "ToiletPaper", "Doorknob", "Trowel", "Bone",
    # the very first key: spawn -> Jardin
    "BridgeKey",
    # Keys that open a vanilla door, promoted to progression [J 2026-08-08] even though
    # the logic knows a way around each of them:
    #  - GardenKey opens gardenGate0, the Jardin->Eglise portal (regions.py) - the Eglise
    #    also has the free Exterieur entrance, so it was previously "useful";
    #  - StrangeKey opens door_end in the LongHallway (dump v0.3 door table), the Orb Room
    #    door, which holds no check of its own.
    "GardenKey", "StrangeKey",
    # Hell access chain (crypt): church interior key + flower gem + the 4 idols
    "ChurchKey", "AtticKey", "FlowerGem",
    "GnomeIdol", "ShyIdol", "ShortIdol", "TallIdol",
    # idol / fanfare prerequisites
    "Trumpet", "ToyBoat", "Eggball",
    # goal chain
    "PurifiedStone", "SoulFragment1", "SoulFragment2", "SoulFragment3",
    # endings / gated obtains
    "Blanket", "Sandwich", "GoldFishDead", "Worm", "Corn", "Butter",
    # the apple gates the worm plate (rules.py "Worm"), which gates the magpie StrangeKey
    "Apple",
    # SpecialSeed must be planted (+ watered daily to day 3) for Obtain PrettyFlower
    # [J 2026-07-27] - it therefore gates a check and must be progression.
    "SpecialSeed",
    # "Deed" checks (2026-07-28) turned eight more items into gates: each one is the item
    # you must HOLD to perform the rewarded action (rules.DEED_RULES). Without this they
    # would stay "useful", state.has() would never see them, and the deeds would be
    # unreachable in logic (caught by test_all_state_can_reach_everything).
    "PizzaBox",       # Deed: Throw Away PizzaBox
    "PrettyFlower",   # Deed: Place PrettyFlower in Vase
    "KidTrumpet", "KidCymbals", "KidTriangle",   # Deed: Complete the School Band
    "GoldFishAlive",  # Deed: Place GoldFishAlive in Fishbowl
    "Medal",          # Deed: Award Medal to the Snail
    "SeveredHand",    # Deed: Return SeveredHand
}


class GrunnItem(Item):
    game = "Grunn"


def classification_for(world: "GrunnWorld", name: str) -> ItemClassification:
    """Return the ItemClassification for an item by name for this slot's options."""
    category = ITEM_CATEGORY[name]
    if category == "trap":
        return ItemClassification.trap
    if category == "buff":
        return ItemClassification.useful
    if category == "filler":  # Gulden and Golden Gulden - both are money
        # Under coinsanity, gulden is spendable money that gates shop checks, so it
        # must be progression (otherwise it is ignored by fill / all-state logic).
        # Golden Gulden follows the same rule: the client credits it as 2 gulden.
        return (
            ItemClassification.progression
            if world.options.coinsanity.value
            else ItemClassification.filler
        )
    # key items
    if name == "AbandonedKey" and world.options.lock_player_hut:
        # lock_player_hut (experimental): the mod locks the player hut behind this
        # otherwise-orphan key (v0.3 door table: unused by any vanilla door). With the
        # option OFF the key is not created at all (see shelved_items).
        return ItemClassification.progression
    if name in FILLER_KEY_ITEMS:
        return ItemClassification.filler
    if name in PROGRESSION_ITEMS:
        return ItemClassification.progression
    return ItemClassification.useful


def create_item(world: "GrunnWorld", name: str) -> GrunnItem:
    return GrunnItem(name, classification_for(world, name), ITEM_NAME_TO_ID[name], world.player)


def get_filler_item_name(world: "GrunnWorld") -> str:
    """Infinitely repeatable filler used for item links / start inventory / pool fill.

    Filler is ONLY traps and buffs (decision Jonath 2026-07-16): Gulden is never
    filler. Under coinsanity the money supply is added explicitly by
    create_all_items instead. Trap chance is honoured here so that
    ``create_filler`` naturally injects traps.
    """
    if world.random.randint(0, 99) < world.options.trap_percentage:
        return world.random.choice(TRAPS)
    return world.random.choice(BUFFS)


def create_all_items(world: "GrunnWorld") -> None:
    player = world.player
    itempool: list[Item] = []

    # 1) Key items, minus unsourced ones, shelved ones, and the ones kept vanilla.
    shelved = shelved_items(world)
    local_names: set[str] = set()
    if world.options.exclude_bridge_key:
        local_names.add("BridgeKey")
    if world.options.keep_vanilla_shears:
        local_names.add("Shears")

    # Lock the vanilla items onto their own "Obtain X" location so they never enter
    # the multiworld pool. BridgeKey: design default (first key). Shears: keep_vanilla_shears.
    for name in local_names:
        location = world.get_location(f"Obtain {name}")
        location.place_locked_item(create_item(world, name))

    for name in KEYITEMS:
        if name in local_names or name in UNSOURCED_ITEMS or name in shelved:
            continue
        itempool.append(create_item(world, name))

    # 2) Progressive buffs: a fixed number of copies each (buff_count).
    buff_count = world.options.buff_count.value
    for name in BUFFS:
        for _ in range(buff_count):
            itempool.append(create_item(world, name))

    # 2.5) Coinsanity money supply: enough Gulden to afford every shop purchase in
    # a single run (can_afford checks the CUMULATIVE received count against each
    # price - rules.py:56, prices in constants.py). Added explicitly because
    # Gulden is NEVER filler (decision Jonath 2026-07-16).
    # The 5 garden chores PAY 2 gulden each in vanilla. With chore_checks ON the client
    # turns those payouts into checks, so the money must come back through the pool as
    # "Golden Gulden" (2 gulden each) - the economy is unchanged, the coins are just
    # shuffled. With the option OFF nothing is intercepted: the jobs pay their gulden
    # exactly as vanilla, so adding the coins too would DOUBLE that money.
    golden_gulden = PAID_GARDEN_CHORES if world.options.chore_checks else 0

    if world.options.coinsanity.value:
        gulden_needed = (
            c.PRICE_BUS + c.PRICE_CD + c.PRICE_COMPASS
            + c.PRICE_OFFICE_KEY + c.PRICE_MEDAL + c.PRICE_EGGBALL
        )
        gulden_needed -= golden_gulden * GOLDEN_GULDEN_VALUE
        for _ in range(max(0, gulden_needed)):
            itempool.append(create_item(world, "Gulden"))
    for _ in range(golden_gulden):
        itempool.append(create_item(world, "Golden Gulden"))

    # 3) Fill the rest with traps / filler so items == unfilled locations.
    number_of_unfilled = len(world.multiworld.get_unfilled_locations(player))
    remaining = number_of_unfilled - len(itempool)
    if remaining < 0:
        # More key items + buffs than locations (only possible with extreme options):
        # drop buffs first to keep parity. Should not happen with default pools.
        del itempool[remaining:]
        remaining = 0
    itempool += [world.create_filler() for _ in range(remaining)]

    world.multiworld.itempool += itempool
