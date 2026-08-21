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
from .. import constants
from ..items import UNSOURCED_ITEMS
from ..locations import UNSOURCED_LOCATIONS


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
            elif f"Obtain {name}" in UNSOURCED_LOCATIONS:
                # keys with no reachable vanilla pickup (OldKey / AbandonedKey): the
                # LOCATION is never created, but the item may still exist in the pool
                # (AbandonedKey is used by lock_player_hut) [2026-07-27]
                self.assertNotIn(f"Obtain {name}", names)
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
        "keep_vanilla_shears": True,
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
    ghosts in an early sphere and the player was hard-blocked in game [2026-07-16].
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

    # exclude_bad_endings is forced OFF here: it defaults to ON since 2026-07-31, and this
    # test asserts on "Ending: LongHallway" - the Sunday hallway, one of the things the hut
    # key really gates.
    options = {
        "goal": "true_ending",
        "lock_player_hut": True,
        "exclude_bridge_key": False,
        "exclude_bad_endings": False,
    }

    def test_hut_locations_require_abandoned_key(self) -> None:
        self.collect_all_but("AbandonedKey")
        for name in ("Obtain Shears", "Obtain ToiletKey", "Ending: LongHallway"):
            location = self.world.get_location(name)
            self.assertFalse(
                location.can_reach(self.multiworld.state),
                f"{name} must require AbandonedKey under lock_player_hut",
            )

    def test_hut_gated_polaroids_require_abandoned_key(self) -> None:
        # Player report (corgi, 2026-08-06): the hut key looked reachable through the
        # magpie polaroid, which the game only shows once you have been INSIDE the hut
        # (dump: polaroidMagpieNest_hider0 / NotEnteredPlayerSchuur). Under
        # lock_player_hut that means the key gates them - never the other way round.
        self.collect_all_but("AbandonedKey")
        for name in ("Polaroid: MagpieNest", "Polaroid: TallManWindow"):
            location = self.world.get_location(name)
            self.assertFalse(
                location.can_reach(self.multiworld.state),
                f"{name} must require AbandonedKey under lock_player_hut",
            )


class TestMaskItemsOn(GrunnTestBase):
    """mask_items is cosmetic: the client picks the displayed model from the slot data.
    The world must generate identically to a run without it."""

    options = {"mask_items": True}

    def test_option_reaches_the_client(self) -> None:
        self.assertEqual(self.world.fill_slot_data()["mask_items"], 1)

    def test_pool_is_unchanged_by_masking(self) -> None:
        masked = sorted(item.name for item in self.multiworld.itempool)
        plain = setup_multiworld(GrunnWorld, seed=self.multiworld.seed, options={"mask_items": False})
        self.assertEqual(masked, sorted(item.name for item in plain.itempool))
        self.assertEqual(
            sorted(loc.name for loc in self.multiworld.get_locations(self.player)),
            sorted(loc.name for loc in plain.get_locations(1)),
        )


class TestVanillaShearsWithLockedHut(GrunnTestBase):
    """Player report (corgi, 2026-08-06): keep_vanilla_shears + lock_player_hut.

    The combination nests two vanilla placements: the Shears sit in the hut, and the hut
    needs the Abandoned Key - so the key must never end up behind anything the hut gates
    (that would be a self-blocking seed). The generic suite's fill test covers the seed
    as a whole; this asserts the specific loop that was reported.
    """

    options = {
        "keep_vanilla_shears": True,
        "lock_player_hut": True,
        "exclude_bridge_key": False,
    }

    def test_abandoned_key_is_never_behind_the_hut(self) -> None:
        from Fill import distribute_items_restrictive

        distribute_items_restrictive(self.multiworld)
        hut_gated = {
            location.name
            for location in self.multiworld.get_locations(self.player)
            if location.item is not None and location.item.name == "AbandonedKey"
        }
        # Everything but the key: whatever holds it must already be reachable.
        state = CollectionState(self.multiworld)
        for item in self.multiworld.itempool:
            if item.advancement and item.name != "AbandonedKey":
                state.collect(item, prevent_sweep=True)
        state.sweep_for_advancements()
        for name in hut_gated:
            self.assertTrue(
                self.multiworld.get_location(name, self.player).can_reach(state),
                f"AbandonedKey placed at {name}, which needs the key itself",
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


class TestChoreChecksOff(GrunnTestBase):
    """chore_checks off: no chore location, and above all NO Golden Gulden - the jobs still
    pay their 2 vanilla gulden, so adding the coins as items would double that money."""

    options = {"chore_checks": False, "coinsanity": True}

    def test_no_chore_locations_and_no_golden_gulden(self) -> None:
        names = {location.name for location in self.multiworld.get_locations(1)}
        self.assertFalse([n for n in names if n.startswith("Chore: ")])
        self.assertEqual(self.get_items_by_name("Golden Gulden"), [])


class TestChoreChecksEconomy(GrunnTestBase):
    """The chore checks must not change the coinsanity purse: the 10 gulden the garden jobs
    stop paying come back as 5 Golden Gulden worth 2 each."""

    options = {"chore_checks": True, "coinsanity": True}

    def test_money_supply_is_unchanged(self) -> None:
        from ..items import GOLDEN_GULDEN_VALUE, PAID_GARDEN_CHORES

        golden = len(self.get_items_by_name("Golden Gulden"))
        plain = len(self.get_items_by_name("Gulden"))
        self.assertEqual(golden, PAID_GARDEN_CHORES)
        expected = (
            constants.PRICE_BUS + constants.PRICE_CD + constants.PRICE_COMPASS
            + constants.PRICE_OFFICE_KEY + constants.PRICE_MEDAL + constants.PRICE_EGGBALL
        )
        self.assertEqual(
            plain + golden * GOLDEN_GULDEN_VALUE, expected,
            "the total spendable money must match the sum of every shop price",
        )

    def test_every_chore_is_a_location(self) -> None:
        from ..locations import CHORE_LOCS

        names = {location.name for location in self.multiworld.get_locations(1)}
        for name in CHORE_LOCS:
            self.assertIn(name, names)


class TestExcludeBadEndings(GrunnTestBase):
    """exclude_bad_endings drops the checks of the 8 endings that kill you (demande
    2026-07-30) - exactly the DeathLink set."""

    options = {"goal": "true_ending", "exclude_bad_endings": True}

    def test_only_survivable_endings_remain(self) -> None:
        names = {location.name for location in self.multiworld.get_locations(1)}
        endings = {name for name in names if name.startswith("Ending: ")}
        self.assertEqual(
            endings,
            {"Ending: Bus", "Ending: Picnic", "Ending: GoodEnd"},
            "only the endings you survive should keep a check",
        )
        for ending in constants.DEATH_ENDINGS:
            self.assertNotIn(f"Ending: {ending}", names)


class TestExcludeBadEndingsIgnoredOnAllEndings(GrunnTestBase):
    """The option must be IGNORED on the all_endings goal: that goal requires meeting
    every ending, so removing their checks would make no sense."""

    options = {"goal": "all_endings", "exclude_bad_endings": True}

    def test_every_ending_keeps_its_check(self) -> None:
        names = {location.name for location in self.multiworld.get_locations(1)}
        for ending in constants.DEATH_ENDINGS:
            self.assertIn(
                f"Ending: {ending}", names,
                f"Ending: {ending} must stay a check on the all_endings goal",
            )


if __name__ == "__main__":
    unittest.main()
