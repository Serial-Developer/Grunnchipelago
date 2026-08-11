"""Player options for the Grunn apworld.

Defaults follow design/apworld_design.md section 7, adjusted where playtesting proved
them wrong.
"""

from dataclasses import dataclass

from Options import (
    Choice,
    DefaultOnToggle,
    OptionGroup,
    PerGameCommonOptions,
    Range,
    Toggle,
)


class Goal(Choice):
    """
    The victory condition for your slot.

    - good_ending: reach the Good Ending (defeat the End Demon with the Magic Sword
      and the Purified Stone, then leave through the front gate).
    - true_ending: reach the Good Ending after restoring the owner's soul
      (requires the 3 Soul Fragments). This is the default.
    - all_endings: see all 11 endings (the demo ending is excluded).
    """

    display_name = "Goal"
    option_good_ending = 0
    option_true_ending = 1
    option_all_endings = 2
    default = 1  # true_ending


class KeepVanillaShears(Toggle):
    """
    If enabled, the Shears (secateurs) stay in their VANILLA location - the player hut -
    instead of being shuffled into the multiworld. Improves early-game accessibility, since
    cutting grass is the renewable source of income.

    Note: with lock_player_hut on (the default), the hut itself is locked, so the vanilla
    shears still sit behind the Abandoned Key.
    """

    display_name = "Keep Shears"


class ExcludeBridgeKey(DefaultOnToggle):
    """
    If enabled (default), the Bridge Key stays in its vanilla location instead of
    being shuffled into the multiworld.

    The Bridge Key is THE first key of the game: it opens spawn (the bus stop on the
    road) into the garden, and the bus blocks every other exit. When it is shuffled
    (this option off), sphere 1 contains only the "Obtain BridgeKey" check itself
    (plus the bus gulden if coinsanity is on) - always at least one check, so no
    special generation handling is needed, but the start is extremely restricted.
    Turning this off is discouraged for async play without coinsanity.
    """

    display_name = "Exclude Bridge Key"


class PolaroidChecks(DefaultOnToggle):
    """
    If enabled (default), collecting the world's polaroids sends checks.
    (Ending polaroids are awarded by the endings themselves and are never shuffled.)
    """

    display_name = "Polaroid Checks"


class GhostChecks(DefaultOnToggle):
    """
    If enabled (default), calming the 7 ghosts scattered across the world sends checks.
    """

    display_name = "Ghost Checks"


class Coinsanity(Toggle):
    """
    If enabled, the 15 placed gulden become checks, and buying things requires
    receiving "Gulden" items from the multiworld instead of cutting grass for money.
    """

    display_name = "Coinsanity"


class PersistentShortcuts(Toggle):
    """
    If enabled, comfort shortcuts are restored after each run reset: the Bijkeuken
    shortcut, the hedge cut out of the back yard, the park's hay-bale garden and maze
    accesses, the bramble burnt between the park and the road, and any lock already
    opened. Read by the client mod; no logic impact.
    """

    display_name = "Persistent Shortcuts"


class TrapPercentage(Range):
    """
    Percentage of filler items that are replaced by traps.
    """

    display_name = "Trap Percentage"
    range_start = 0
    range_end = 100
    default = 20


class BuffCount(Range):
    """
    How many copies of each progressive buff (Move Speed / Cutter Range / Cutting
    Rate) are added to the item pool.
    """

    display_name = "Buff Count (per buff)"
    range_start = 0
    range_end = 10
    default = 3


class LockPlayerHut(DefaultOnToggle):
    """
    Enabled by default. The player hut door is locked and requires the Abandoned Key (an
    orphan key in vanilla: the v0.3 door table shows it unlocks nothing - it most likely
    opened this very hut originally).

    IMPORTANT - what the key really gates: the only usable BED is inside the hut (dump:
    Hide_PlayerSchuur/interior/bed0; the game's only other bed is in the endgame
    AtticRoom), so the key also gates SLEEPING, i.e. reaching day 2+. The logic models
    this via rules.can_advance_days: the Mist ending (day 3), the Bus ending
    (dayIndex >= 2), the Ferry crossing (day 2) and Calm Ghost #3 (redCar, day 2) all
    require the key when this option is on [J 2026-07-27, playtest]. It also gates the
    vanilla Shears / Toilet Key spots and the Sunday-evening hallway.

    The Abandoned Key therefore becomes an early, high-value progression item - especially
    interesting in multiworld, where it can come from another player's world. Turn this off
    for a looser run where the hut (and sleeping) is free from the start.
    """

    display_name = "Lock Player Hut"


class MaskItems(Toggle):
    """
    Hide what every location holds: instead of showing the item it contains, each spot
    displays an Archipelago model coloured by its class - progression, useful or filler.
    This covers Grunn's own items too, which is the point of the mode: you can tell how
    valuable a check is, never what it is.

    Traps borrow one of the three models, picked per location and always the same one, so
    a relaunch never gives them away.

    Purely cosmetic: it changes no logic, no item placement, and the pickup message still
    names what you actually got.
    """

    display_name = "Mask Items With Archipelago Model"


class DeathLink(Toggle):
    """
    Death link. Every death ending you meet (any ending except Bus, Picnic and the
    good/true ending) sends a DeathLink to the multiworld. Receiving a DeathLink shows
    a nightmare jumpscare and resets your current run (week) - no ending is triggered
    and no check is granted.
    """

    display_name = "Death Link"


class ChoreChecks(DefaultOnToggle):
    """
    If enabled (default), finishing a maintenance job sends a check: trimming every hedge,
    cutting all the grass, clearing every molehill, watering every flower and picking up all
    the litter IN THE START GARDEN, plus trimming the 8 potted plants scattered around the
    world.

    The five garden jobs normally pay 2 gulden each the first time they are completed; with
    this option on, that money is replaced by five "Golden Gulden" items (worth 2 gulden
    each) shuffled into the multiworld, so the economy is unchanged - you simply receive the
    coins instead of earning them on the spot. Trimming the potted plants pays nothing in
    vanilla, so it adds no coin.

    Independent of Coinsanity: these checks exist either way.
    """

    display_name = "Chore Checks"


class ExcludeBadEndings(DefaultOnToggle):
    """
    Enabled by default. Removes the checks of the "bad" endings - the 8 endings that KILL
    you, i.e. exactly the ones that fire a DeathLink: Mist, SacredFlowers, Drown, Darkness,
    LongHallway, HedgeMaze, WorldEnd and Dog.

    Only Bus, Picnic and the good/true ending keep an ending check, so you are never forced
    to die - and, under DeathLink, to kill everyone else - just to collect a check. Turn it
    off to put those 8 checks back in the pool. The endings themselves are still reachable in game, they simply stop
    being locations.

    Ignored when the goal is "all_endings": that goal requires meeting every ending, so
    removing their checks would make no sense.
    """

    display_name = "Exclude Bad Endings"


@dataclass
class GrunnOptions(PerGameCommonOptions):
    goal: Goal
    keep_vanilla_shears: KeepVanillaShears
    exclude_bridge_key: ExcludeBridgeKey
    polaroid_checks: PolaroidChecks
    ghost_checks: GhostChecks
    coinsanity: Coinsanity
    persistent_shortcuts: PersistentShortcuts
    lock_player_hut: LockPlayerHut
    trap_percentage: TrapPercentage
    buff_count: BuffCount
    death_link: DeathLink
    chore_checks: ChoreChecks
    exclude_bad_endings: ExcludeBadEndings
    mask_items: MaskItems


grunn_option_groups = [
    OptionGroup(
        "Goal & Pools",
        [Goal, KeepVanillaShears, ExcludeBridgeKey, PolaroidChecks, GhostChecks, ChoreChecks,
         Coinsanity],
    ),
    OptionGroup(
        "Extras & Tuning",
        [PersistentShortcuts, LockPlayerHut, TrapPercentage, BuffCount, DeathLink,
         ExcludeBadEndings, MaskItems],
    ),
]
