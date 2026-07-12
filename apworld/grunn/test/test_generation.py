"""Generation tests for the Grunn apworld.

The generic WorldTestBase suite (run for each config below) already exercises:
solo generation, fill, "all state can reach everything", and beatability. On top
of that we assert the item/location parity and multi-world generation.
"""

import unittest

from BaseClasses import CollectionState
from test.general import setup_multiworld

from .bases import GrunnTestBase
from .. import GrunnWorld
from ..items import UNSOURCED_ITEMS


class TestDefaultTemplate(GrunnTestBase):
    """Default YAML template (true_ending, exclude_bridge_key on, polaroids + ghosts on)."""

    options: dict = {}

    def test_sourced_key_item_locations_exist(self) -> None:
        names = {loc.name for loc in self.multiworld.get_locations(self.player)}
        for name in self.world.item_name_to_id:
            if self.world.location_name_to_id.get(f"Obtain {name}") is None:
                continue
            if name in UNSOURCED_ITEMS:
                # unsourced items keep a reserved id but never create a location/item
                self.assertNotIn(f"Obtain {name}", names)
                self.assertEqual(self.get_items_by_name(name), [])
            else:
                self.assertIn(f"Obtain {name}", names)

    def test_item_location_parity(self) -> None:
        pool = len(self.multiworld.itempool)
        unfilled = len(self.multiworld.get_unfilled_locations(self.player))
        self.assertEqual(pool, unfilled, "item pool must exactly fill the unfilled locations")


class TestGoalGoodEnding(GrunnTestBase):
    options = {"goal": "good_ending"}


class TestGoalTrueEnding(GrunnTestBase):
    options = {"goal": "true_ending"}


class TestGoalAllEndings(GrunnTestBase):
    options = {"goal": "all_endings"}


class TestCoinsanityAndAllPools(GrunnTestBase):
    options = {
        "goal": "all_endings",
        "coinsanity": True,
        "polaroid_checks": True,
        "ghost_checks": True,
        "keep_shears": True,
        "exclude_bridge_key": False,
    }


class TestHellChain(GrunnTestBase):
    """The endgame 'Hell' chain: idols/flowergem/churchkey -> Hell -> AtticKey -> Sword.

    exclude_bridge_key is off so BridgeKey is in the pool and collect_all_but() grants
    it (otherwise the locally-locked BridgeKey would make everything unreachable).
    """

    options = {"goal": "true_ending", "exclude_bridge_key": False}

    def test_magicsword_requires_attickey(self) -> None:
        # AtticKey opens the attic door (Door.cs:684); the attic Magic Sword needs it.
        self.collect_all_but("AtticKey")
        sword = self.world.get_location("Obtain MagicSword")
        self.assertFalse(sword.can_reach(self.multiworld.state), "MagicSword must require AtticKey")

    def test_hell_requires_all_four_idols(self) -> None:
        # AtticKey is obtained inside Hell, which needs the 4 idols deposited in the crypt.
        self.collect_all_but("GnomeIdol")
        attickey = self.world.get_location("Obtain AtticKey")
        self.assertFalse(attickey.can_reach(self.multiworld.state), "Hell must require every idol")

    def test_true_ending_needs_soul_fragments(self) -> None:
        self.collect_all_but("SoulFragment1")
        self.assertFalse(self.multiworld.state.has("SoulFragment1", self.player))
        self.assertFalse(self.multiworld.can_beat_game(self.multiworld.state))


class TestMinimalPools(GrunnTestBase):
    options = {
        "goal": "good_ending",
        "polaroid_checks": False,
        "ghost_checks": False,
        "coinsanity": False,
    }


class TestMultiworldGeneration(unittest.TestCase):
    """2+ Grunn worlds must generate and fill without errors (each goal mixed)."""

    def test_two_grunn_worlds(self) -> None:
        multiworld = setup_multiworld([GrunnWorld, GrunnWorld])
        from Fill import distribute_items_restrictive

        distribute_items_restrictive(multiworld)

        for player in multiworld.player_ids:
            state = CollectionState(multiworld)
            for item in multiworld.itempool:
                state.collect(item, prevent_sweep=True)
            # collect locked/event items too by sweeping
            state.sweep_for_advancements()
            self.assertTrue(
                multiworld.has_beaten_game(state, player),
                f"player {player} cannot beat the game after collecting everything",
            )

    def test_mixed_goals(self) -> None:
        multiworld = setup_multiworld(
            [GrunnWorld, GrunnWorld, GrunnWorld],
            options=[
                {"goal": "good_ending"},
                {"goal": "true_ending"},
                {"goal": "all_endings", "coinsanity": True},
            ],
        )
        from Fill import distribute_items_restrictive

        distribute_items_restrictive(multiworld)
        self.assertEqual(len(multiworld.get_unfilled_locations()), 0)


if __name__ == "__main__":
    unittest.main()
