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


class TestGhostsNeedTrumpet(GrunnTestBase):
    """Ghosts are invisible until the Trumpet reveals them (Ghost.Show is only called
    by ShowNearbyGhosts <- PerformTrumpetAction, GameManager.cs:5153-5167), so every
    ghost check requires it. Regression: seed 7 put ChurchKey/Compass/Trowel behind
    ghosts in an early sphere and the player was hard-blocked in game [J 2026-07-16].
    """

    options = {"goal": "all_endings", "ghost_checks": True, "exclude_bridge_key": False}

    def test_ghost_checks_require_trumpet(self) -> None:
        from ..locations import GHOST_LOCS

        self.collect_all_but("Trumpet")
        for name in GHOST_LOCS:
            location = self.world.get_location(name)
            self.assertFalse(
                location.can_reach(self.multiworld.state),
                f"{name} must require the Trumpet (ghosts are invisible without it)",
            )


class TestLockPlayerHut(GrunnTestBase):
    """Experimental lock_player_hut: AbandonedKey gates the hut (Shears/ToiletKey spots
    and the Sunday hallway)."""

    options = {"goal": "true_ending", "lock_player_hut": True, "exclude_bridge_key": False}

    def test_hut_locations_require_abandoned_key(self) -> None:
        self.collect_all_but("AbandonedKey")
        for name in ("Obtain Shears", "Obtain ToiletKey", "Ending: LongHallway"):
            location = self.world.get_location(name)
            self.assertFalse(
                location.can_reach(self.multiworld.state),
                f"{name} must require AbandonedKey under lock_player_hut",
            )

    def test_abandoned_key_is_progression(self) -> None:
        from BaseClasses import ItemClassification
        from ..items import classification_for
        self.assertEqual(
            classification_for(self.world, "AbandonedKey"), ItemClassification.progression
        )


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
