"""Grunn - Archipelago apworld.

Grunn (Sokpop Collective, 2024) modelled as looping in-game weeks with 11 endings.
This world randomizes the ~54 key items and offers polaroid / ghost / gulden checks.
See design/apworld_design.md and design/regions.md for the full design.
"""

from __future__ import annotations

from collections.abc import Mapping
from typing import Any

from BaseClasses import Tutorial
from worlds.AutoWorld import WebWorld, World

from . import items, locations, regions, rules
from . import options as grunn_options
from .constants import GAME_NAME
from .items import GrunnItem
from .options import GrunnOptions, grunn_option_groups


class GrunnWebWorld(WebWorld):
    theme = "dirt"
    option_groups = grunn_option_groups

    setup_en = Tutorial(
        "Multiworld Setup Guide",
        "A guide to setting up Grunn for Archipelago multiworld.",
        "English",
        "setup_en.md",
        "setup/en",
        ["Serial-Developer"],
    )

    setup_fr = Tutorial(
        "Guide de configuration Multimonde",
        "Un guide pour configurer Grunn en multimonde Archipelago.",
        "Français",
        "setup_fr.md",
        "setup/fr",
        ["Serial-Developer"],
    )

    tutorials = [setup_en, setup_fr]


class GrunnWorld(World):
    """Grunn is an eerie gardening-adventure roguelite with 11 endings and one week
    that never quite ends. Tend the garden, follow the disc, and find your way out."""

    game = GAME_NAME
    web = GrunnWebWorld()

    options_dataclass = GrunnOptions
    options: GrunnOptions

    item_name_to_id = items.ITEM_NAME_TO_ID
    location_name_to_id = locations.LOCATION_NAME_TO_ID

    origin_region_name = "Menu"
    topology_present = True

    item_name_groups = {
        "Tools": {"Trowel", "Shears", "WateringCan", "Hammer", "Trumpet", "MagicSword"},
        "Idols": {"GnomeIdol", "TallIdol", "ShortIdol", "ShyIdol"},
        "Instruments": {"Trumpet", "Cymbals", "KidTrumpet", "KidCymbals", "KidTriangle"},
        "Soul Fragments": {"SoulFragment1", "SoulFragment2", "SoulFragment3"},
        "Keys": {
            "GardenKey", "OfficeKey", "OldKey", "ToiletKey", "ChurchKey",
            "BridgeKey", "StrangeKey", "AbandonedKey", "AtticKey",
        },
    }

    # --- Generation steps -------------------------------------------------------
    # NOTE (2026-07-31): declaring AbandonedKey via multiworld.early_items was TRIED and
    # REVERTED - it made things worse (20/20 failures instead of 6/20). Archipelago logged
    # "Ran out of early locations for early items": early_items needs locations reachable
    # with an EMPTY state, and Grunn has almost none - every region sits behind BridgeKey,
    # which is itself locked onto its own location. The fix has to widen the early sphere,
    # not reserve a spot inside it.
    def create_regions(self) -> None:
        regions.create_and_connect_regions(self)
        locations.create_all_locations(self)

    def create_items(self) -> None:
        items.create_all_items(self)

    def create_item(self, name: str) -> GrunnItem:
        return items.create_item(self, name)

    def get_filler_item_name(self) -> str:
        return items.get_filler_item_name(self)

    def set_rules(self) -> None:
        rules.set_all_rules(self)

    def fill_slot_data(self) -> Mapping[str, Any]:
        # Options the client mod needs. (design/apworld_design.md section 1 + 7)
        return self.options.as_dict(
            "goal",
            "keep_vanilla_shears",
            "exclude_bridge_key",
            "polaroid_checks",
            "ghost_checks",
            "coinsanity",
            "persistent_shortcuts",
            "lock_player_hut",
            "death_link",
            "chore_checks",
            "exclude_bad_endings",
        )
