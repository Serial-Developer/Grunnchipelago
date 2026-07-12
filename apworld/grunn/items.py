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
import os
from typing import TYPE_CHECKING

from BaseClasses import Item, ItemClassification

if TYPE_CHECKING:
    from . import GrunnWorld

_IDS_PATH = os.path.join(os.path.dirname(__file__), "data", "ids.json")
with open(_IDS_PATH, encoding="utf-8") as _f:
    IDS = json.load(_f)

# name -> id and name -> category ("keyitem" | "buff" | "trap" | "filler")
ITEM_NAME_TO_ID: dict[str, int] = {name: data["id"] for name, data in IDS["items"].items()}
ITEM_CATEGORY: dict[str, str] = {name: data["category"] for name, data in IDS["items"].items()}

KEYITEMS = [n for n, c in ITEM_CATEGORY.items() if c == "keyitem"]
BUFFS = [n for n, c in ITEM_CATEGORY.items() if c == "buff"]
TRAPS = [n for n, c in ITEM_CATEGORY.items() if c == "trap"]
FILLER = [n for n, c in ITEM_CATEGORY.items() if c == "filler"]  # ["Gulden"]

# --- Unsourced items ------------------------------------------------------------
# Systematic source audit (dump pickups + ObtainKeyItem() calls in *.cs): every key
# item has a source EXCEPT "Cymbals" (the non-"Kid" enum entry) - no pickup and no
# ObtainKeyItem(KeyItem.Cymbals). It is an unused enum entry. Its id stays reserved in
# ids.json, but neither its item nor its "Obtain Cymbals" location is created.
UNSOURCED_ITEMS = {"Cymbals"}

# --- Progression classification -------------------------------------------------
# Items referenced by a region entrance or an "Obtain X" / ending rule in rules.py are
# marked progression (so fill and all-state reachability count them). GardenKey is the
# one exception: it only gates the Jardin->Eglise portal, and the Eglise is always
# reachable via the Exterieur, so it is never strictly required -> "useful".
# (design/apworld_design.md section 4 + regions_v3.md 2026-07-12 corrections)
PROGRESSION_ITEMS = {
    # traversal / equivalence helpers
    "Shears", "MagicSword", "WateringCan", "Coin", "Plank", "OldPlank",
    "Lighter", "Paddle", "OfficeKey", "Cd", "Compass", "Hammer",
    "ToiletKey", "ToiletPaper", "Doorknob", "Trowel", "Bone",
    # the very first key: spawn -> Jardin
    "BridgeKey",
    # Hell access chain (crypt): church interior key + flower gem + the 4 idols
    "ChurchKey", "AtticKey", "FlowerGem",
    "GnomeIdol", "ShyIdol", "ShortIdol", "TallIdol",
    # idol / fanfare prerequisites
    "Trumpet", "ToyBoat", "Eggball",
    # goal chain
    "PurifiedStone", "SoulFragment1", "SoulFragment2", "SoulFragment3",
    # endings / gated obtains
    "Blanket", "Sandwich", "GoldFishDead", "Worm", "Corn", "Butter",
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
    if category == "filler":  # "Gulden"
        # Under coinsanity, gulden is spendable money that gates shop checks, so it
        # must be progression (otherwise it is ignored by fill / all-state logic).
        return (
            ItemClassification.progression
            if world.options.coinsanity.value
            else ItemClassification.filler
        )
    # key items
    if name in PROGRESSION_ITEMS:
        return ItemClassification.progression
    return ItemClassification.useful


def create_item(world: "GrunnWorld", name: str) -> GrunnItem:
    return GrunnItem(name, classification_for(world, name), ITEM_NAME_TO_ID[name], world.player)


def get_filler_item_name(world: "GrunnWorld") -> str:
    """Infinitely repeatable filler used for item links / start inventory / pool fill.

    Trap chance is honoured here so that ``create_filler`` naturally injects traps.
    Under coinsanity, non-trap filler is "Gulden" (spendable money); otherwise it is
    still "Gulden" as harmless flavour filler (a calibrer).
    """
    if world.random.randint(0, 99) < world.options.trap_percentage:
        return world.random.choice(TRAPS)
    return "Gulden"


def create_all_items(world: "GrunnWorld") -> None:
    player = world.player
    itempool: list[Item] = []

    # 1) Key items, minus unsourced ones and the ones kept vanilla (placed locally).
    local_names: set[str] = set()
    if world.options.exclude_bridge_key:
        local_names.add("BridgeKey")
    if world.options.keep_shears:
        local_names.add("Shears")

    # Lock the vanilla items onto their own "Obtain X" location so they never enter
    # the multiworld pool. BridgeKey: design default (first key). Shears: keep_shears.
    for name in local_names:
        location = world.get_location(f"Obtain {name}")
        location.place_locked_item(create_item(world, name))

    for name in KEYITEMS:
        if name in local_names or name in UNSOURCED_ITEMS:
            continue
        itempool.append(create_item(world, name))

    # 2) Progressive buffs: a fixed number of copies each (a calibrer).
    buff_count = world.options.buff_count.value
    for name in BUFFS:
        for _ in range(buff_count):
            itempool.append(create_item(world, name))

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
