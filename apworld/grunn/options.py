"""Player options for the Grunn apworld.

Defaults match design/apworld_design.md section 7 exactly. Trap/buff quantities
are marked "a calibrer" (to be tuned) once the real location/item counts and the
mod behaviour are validated on-device.
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


class KeepShears(Toggle):
    """
    If enabled, the Shears (secateurs) stay in their vanilla location instead of
    being shuffled into the multiworld. Improves early-game accessibility, since
    cutting grass is the renewable source of income.
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
    If enabled (default), calming the 8 ghosts scattered across the world sends checks.
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
    If enabled, comfort shortcuts (Bijkeuken shortcut, Intratuin, park mazes, etc.)
    are restored after each run reset. Read by the client mod; no logic impact.
    """

    display_name = "Persistent Shortcuts"


class TrapPercentage(Range):
    """
    Percentage of filler items that are replaced by traps. (a calibrer)
    """

    display_name = "Trap Percentage"
    range_start = 0
    range_end = 100
    default = 20


class BuffCount(Range):
    """
    How many copies of each progressive buff (Move Speed / Cutter Range / Cutting
    Rate) are added to the item pool. (a calibrer)
    """

    display_name = "Buff Count (per buff)"
    range_start = 0
    range_end = 10
    default = 3


class DeathLink(Toggle):
    """
    STRICT death link (design decision, no filtering): any death you suffer sends a
    DeathLink, and any received DeathLink triggers your nightmare (death).
    """

    display_name = "Death Link"


@dataclass
class GrunnOptions(PerGameCommonOptions):
    goal: Goal
    keep_shears: KeepShears
    exclude_bridge_key: ExcludeBridgeKey
    polaroid_checks: PolaroidChecks
    ghost_checks: GhostChecks
    coinsanity: Coinsanity
    persistent_shortcuts: PersistentShortcuts
    trap_percentage: TrapPercentage
    buff_count: BuffCount
    death_link: DeathLink


grunn_option_groups = [
    OptionGroup(
        "Goal & Pools",
        [Goal, KeepShears, ExcludeBridgeKey, PolaroidChecks, GhostChecks, Coinsanity],
    ),
    OptionGroup(
        "Extras & Tuning",
        [PersistentShortcuts, TrapPercentage, BuffCount, DeathLink],
    ),
]
