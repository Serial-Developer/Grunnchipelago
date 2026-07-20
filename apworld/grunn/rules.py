"""Access rules for the Grunn apworld.

Every rule is traceable to a source line, cited in-comment as:
  - regions_v3: design/regions_v3.md connection / equivalence / "zone Hell" section
  - dump: dump/zone_logic.md pickup zone
  - code: analysis/decompiled/*.cs (endings from TriggerEnding; AtticKey from Owner.cs)
  - FLAG: an assumption to review.
"""

from __future__ import annotations

from typing import TYPE_CHECKING, Callable

from BaseClasses import CollectionState
from worlds.generic.Rules import add_rule, set_rule

from . import constants as c
from . import locations

if TYPE_CHECKING:
    from . import GrunnWorld

Rule = Callable[[CollectionState, "GrunnWorld"], bool]

IDOLS = ("GnomeIdol", "ShyIdol", "ShortIdol", "TallIdol")


# --- Equivalence helpers (design/apworld_design.md section 6) --------------------
def _reach(state: CollectionState, world: "GrunnWorld", region: str) -> bool:
    return state.can_reach_region(region, world.player)


def can_cut_grass(state: CollectionState, world: "GrunnWorld") -> bool:
    # regions_v3: "Couper l'herbe : Shears OU MagicSword"
    return state.has_any(("Shears", "MagicSword"), world.player)


def has_plank(state: CollectionState, world: "GrunnWorld") -> bool:
    # regions_v3: "Planche : Plank OU OldPlank"
    return state.has_any(("Plank", "OldPlank"), world.player)


def can_water(state: CollectionState, world: "GrunnWorld") -> bool:
    # regions_v3: "Arroser : WateringCan OU (Coin + acces Eglise)"
    return state.has("WateringCan", world.player) or (
        state.has("Coin", world.player) and _reach(state, world, c.EGLISE)
    )


def can_get_bone(state: CollectionState, world: "GrunnWorld") -> bool:
    # regions_v3: "Os : Hammer OU MagicSword OU Trowel OU via le Manoir"
    return state.has_any(("Hammer", "MagicSword", "Trowel"), world.player) or _reach(
        state, world, c.MANOIR
    )


def can_afford(state: CollectionState, world: "GrunnWorld", price: int) -> bool:
    # coinsanity: money = received "Gulden" items; else tonte (grass) is renewable income
    # covering every shop cost. regions_v3: "Economie".
    if world.options.coinsanity.value:
        return state.has("Gulden", world.player, price)
    return can_cut_grass(state, world)


def complete_zone(state: CollectionState, world: "GrunnWorld", region: str, full: bool) -> bool:
    """Zone maintenance (20 % unlocks / 100 % completion).

    design section 6: access zone + can_cut_grass + can_water (+ Trowel for molehills).
    Molehills (Trowel) are only required for the 100 %.
    """
    ok = _reach(state, world, region) and can_cut_grass(state, world) and can_water(state, world)
    if full:
        ok = ok and state.has("Trowel", world.player)
    return ok


def can_reach_hell(state: CollectionState, world: "GrunnWorld") -> bool:
    """Access to the endgame 'Hell' scenario (crypt then burning manor).

    regions_v3 "Sequence finale - zone Hell" (V): reach the church + open the interior
    door with ChurchKey + place the FlowerGem on the pupitre + deposit the 4 idols.
    (The after-midnight time window is logically free.)
    """
    p = world.player
    return (
        _reach(state, world, c.EGLISE)
        and state.has("ChurchKey", p)
        and state.has("FlowerGem", p)
        and state.has_all(IDOLS, p)
    )


# --- "Obtain X" logical rules ---------------------------------------------------
OBTAIN_RULES: dict[str, Rule] = {
    # dump: Bijkeuken
    "GardenKey": lambda s, w: _reach(s, w, c.BUANDERIE),
    # dump: AppleSpace
    "Apple": lambda s, w: _reach(s, w, c.APPLE_SPACE),
    # dump: StartGarden (worm0) OR WindyPath
    "Worm": lambda s, w: _reach(s, w, c.JARDIN) or _reach(s, w, c.CABANE_PECHEUR),
    # dump: Bunker (trowel0) OR Toilet (trowel0_demo). The "_demo" suffix is naming only,
    # NOT demo-gated content: the real demo mechanism is hideInDemo + SaveManager.demo
    # (ContentHider.cs:214) and these pickups have hideInDemo:False / startState:Show
    # / no hider [J 2026-07-13].
    "Trowel": lambda s, w: _reach(s, w, c.BUNKER) or _reach(s, w, c.TOILET),
    # dump: PlayerSchuur (scissors)
    "Shears": lambda s, w: _reach(s, w, c.CABANE_JOUEUR),
    # dump: Park OR RoundHallway (wateringCan0_demo - naming only, not demo-gated,
    # see Trowel note) [J 2026-07-13]
    "WateringCan": lambda s, w: _reach(s, w, c.PARC) or _reach(s, w, c.PASSAGE_GNOMES),
    # dump: Road (item_plank0)
    "Plank": lambda s, w: _reach(s, w, c.EXTERIEUR),
    # dump: Tent (blueCoin0)
    "Coin": lambda s, w: _reach(s, w, c.TENTE),
    # dump: StartGarden (hammer0_car) OR HedgeMazeInner
    "Hammer": lambda s, w: _reach(s, w, c.JARDIN) or _reach(s, w, c.LABYRINTHE_COEUR),
    # dump: StartGarden (severedHand0)
    "SeveredHand": lambda s, w: _reach(s, w, c.JARDIN),
    # dump: Park (pizzaBox0)
    "PizzaBox": lambda s, w: _reach(s, w, c.PARC),
    # dump: GasStation (free) OR HooibaalSchuur shop (2)
    "OfficeKey": lambda s, w: _reach(s, w, c.GAS_STATION) or _reach(s, w, c.HOOIBAAL),
    # dump: Park (free) OR Road (free) OR GasStation shop (5)
    "Lighter": lambda s, w: _reach(s, w, c.EXTERIEUR) or _reach(s, w, c.PARC) or _reach(s, w, c.GAS_STATION),
    # dump: HooibaalSchuur shop (5)
    "Cd": lambda s, w: _reach(s, w, c.HOOIBAAL) and can_afford(s, w, c.PRICE_CD),
    # regions_v3 I.1: break the garden gnome (Hammer|MagicSword|Trowel) + enter the gas
    # station, then a portal in the Jardin yields the idol.
    "GnomeIdol": lambda s, w: _reach(s, w, c.JARDIN)
    and _reach(s, w, c.GAS_STATION)
    and s.has_any(("Hammer", "MagicSword", "Trowel"), w.player),
    # dump: BigHouseOffice
    "OldKey": lambda s, w: _reach(s, w, c.MANOIR),
    # dump: PlayerSchuur
    "ToiletKey": lambda s, w: _reach(s, w, c.CABANE_JOUEUR),
    # dump: CornFieldCenter
    "ToiletPaper": lambda s, w: _reach(s, w, c.CHAMP_MAIS),
    # dump: Church (missingDoorknob0)
    "Doorknob": lambda s, w: _reach(s, w, c.EGLISE),
    # dump: PillarSpace (trumpet0)
    "Trumpet": lambda s, w: _reach(s, w, c.PILLAR_SPACE),
    # dump: Road / Manoir / GnomeForest / Void / PillarSpace skeletons
    "Bone": lambda s, w: (
        _reach(s, w, c.EXTERIEUR) or _reach(s, w, c.MANOIR) or _reach(s, w, c.PASSAGE_GNOMES)
        or _reach(s, w, c.VOID) or _reach(s, w, c.PILLAR_SPACE)
    ) and can_get_bone(s, w),
    # dump: StartGarden (paddle0)
    "Paddle": lambda s, w: _reach(s, w, c.JARDIN),
    # regions_v3: THE first key, pickup bridgeKey0 on the road at spawn (before Jardin).
    "BridgeKey": lambda s, w: True,
    # dump: BigHouseOffice (free) OR StartGarden magpie (needs Worm). The magpie pickup
    # is strangeKey0_demo, child of magpieDeadByWorm0 = the canonical magpie key drop
    # ("_demo" is naming only, see Trowel note) [J 2026-07-13].
    "StrangeKey": lambda s, w: _reach(s, w, c.MANOIR) or (_reach(s, w, c.JARDIN) and s.has("Worm", w.player)),
    # regions_v3 I.4: cross the maze gap + Compass (-> maze heart) + hit the TallMan
    # (MagicSword|Hammer). MagicSword itself needs Hell, so Hammer is the real route.
    "TallIdol": lambda s, w: _reach(s, w, c.LABYRINTHE_COEUR) and s.has_any(("MagicSword", "Hammer"), w.player),
    # regions_v3 I.3: ToyBoat given to the Ferry kid (reaching Ferry already needs ToyBoat).
    "ShortIdol": lambda s, w: _reach(s, w, c.FERRY),
    # regions_v3 I.2: play the Trumpet near the moving grasses in the Jardin.
    "ShyIdol": lambda s, w: _reach(s, w, c.JARDIN) and s.has("Trumpet", w.player),
    # regions_v3 II: water all strange flowers across the 4 macro-zones.
    "FlowerGem": lambda s, w: _reach(s, w, c.JARDIN) and _reach(s, w, c.PARC)
    and _reach(s, w, c.EGLISE) and _reach(s, w, c.EXTERIEUR) and can_water(s, w),
    # dump: HooibaalSchuur shop (4)
    "Compass": lambda s, w: _reach(s, w, c.HOOIBAAL) and can_afford(s, w, c.PRICE_COMPASS),
    # dump: CornField
    "Corn": lambda s, w: _reach(s, w, c.CHAMP_MAIS),
    # dump: BigHouseKitchen; popcorn chain needs Corn + Butter (code: MakePopcorn)
    "Popcorn": lambda s, w: _reach(s, w, c.MANOIR) and s.has_all(("Corn", "Butter"), w.player),
    # dump: BigHouseFridge
    "Butter": lambda s, w: _reach(s, w, c.MANOIR),
    # regions_v3 Hell: soul fragments scattered in the Hell-version manor.
    "SoulFragment1": lambda s, w: _reach(s, w, c.HELL),
    # regions_v3 Hell: the bottle/jar-shelf fragment (BottleRoom) needs Hammer.
    "SoulFragment2": lambda s, w: _reach(s, w, c.HELL) and s.has("Hammer", w.player),
    "SoulFragment3": lambda s, w: _reach(s, w, c.HELL),
    # regions_v3 Hell: attic cardboard box; the attic door needs AtticKey (Door.cs:684;
    # dump v0.3 door table: AtticKey unlocks bigHouseAtticDoor0).
    "MagicSword": lambda s, w: _reach(s, w, c.HELL) and s.has("AtticKey", w.player),
    # dump: RummikubSpace; needs Lighter (code: LitRummikubHooibaal)
    "PurifiedStone": lambda s, w: _reach(s, w, c.ZONE_VELO) and s.has("Lighter", w.player),
    # dump: VeerbootHuis (cabin interior; entering needs Bone -> handled by the region rule)
    "ToyBoat": lambda s, w: _reach(s, w, c.CABANE_PECHEUR_INT),
    # dump: WindyPath
    "AbandonedKey": lambda s, w: _reach(s, w, c.CABANE_PECHEUR),
    # code Owner.cs/EndConversation: the Owner gives AtticKey inside the Hell manor.
    "AtticKey": lambda s, w: _reach(s, w, c.HELL),
    # dump: BigHouseHallway (Manoir hall, reached via the disc chain). Opens the church
    # interior door (crypt). regions_v3 2026-07-12 key correction.
    "ChurchKey": lambda s, w: _reach(s, w, c.MANOIR),
    # dump: GlassHouse
    "SpecialSeed": lambda s, w: _reach(s, w, c.GLASS_HOUSE),
    # dump: GnomeForest
    "KidTrumpet": lambda s, w: _reach(s, w, c.PASSAGE_GNOMES),
    # dump: HedgeMaze
    "KidCymbals": lambda s, w: _reach(s, w, c.LABYRINTHE),
    # regions_v3 fanfare: trade an Eggball to the person behind the gas station.
    "KidTriangle": lambda s, w: _reach(s, w, c.GAS_STATION) and s.has("Eggball", w.player),
    # regions_v3 fanfare: Park food truck, 5 gulden (Saturday daytime = free logically).
    "Eggball": lambda s, w: _reach(s, w, c.PARC) and can_afford(s, w, c.PRICE_EGGBALL),
    # dump: StartGarden (prettyFlower)
    "PrettyFlower": lambda s, w: _reach(s, w, c.JARDIN),
    # code: revive GoldFishDead at MagicPond OR RoundHallway fishbowl
    "GoldFishAlive": lambda s, w: (_reach(s, w, c.MAGIC_POND) or _reach(s, w, c.PASSAGE_GNOMES))
    and s.has("GoldFishDead", w.player),
    # dump: Park (goldfishDead0)
    "GoldFishDead": lambda s, w: _reach(s, w, c.PARC),
    # dump: HooibaalSchuur shop (10)
    "Medal": lambda s, w: _reach(s, w, c.HOOIBAAL) and can_afford(s, w, c.PRICE_MEDAL),
    # dump: Hill (Plage, unlocked by Eglise 100 %)
    "Blanket": lambda s, w: _reach(s, w, c.PLAGE),
    # dump: Bakery (Boulangerie, unlocked by Parc 100 %)
    "Sandwich": lambda s, w: _reach(s, w, c.BOULANGERIE),
    # dump: ChurchHallway (item_oldPlank0)
    "OldPlank": lambda s, w: _reach(s, w, c.EGLISE),
    # NOTE: "Cymbals" is unsourced (see items.UNSOURCED_ITEMS) - no rule, no location.
}


# --- Ending rules (derived from analysis/decompiled/*.cs TriggerEnding sites) ----
def _end_good(s: CollectionState, w: "GrunnWorld") -> bool:
    # code EndDemon.GetHit: MagicSword + PurifiedStone, in the Hell confrontation.
    return _reach(s, w, c.HELL) and s.has_all(("MagicSword", "PurifiedStone"), w.player)


ENDING_RULES: dict[str, Rule] = {
    # code GameManager.HandleMist: mist closes in on a normal day (time is free).
    "Mist": lambda s, w: _reach(s, w, c.JARDIN),
    # code Interaction.BusSeat: day > 1 (free) + boughtBusTicket (10 gulden).
    "Bus": lambda s, w: can_afford(s, w, c.PRICE_BUS),
    # code Flower: cut >=5 graveyard flowers (church) with can_cut_grass.
    "SacredFlowers": lambda s, w: _reach(s, w, c.EGLISE) and can_cut_grass(s, w),
    # code PlayerControllerNew: drown underwater (water bodies past the garden).
    "Drown": lambda s, w: _reach(s, w, c.EXTERIEUR),
    # regions_v3 (2026-07-12): Darkness closes in outside after midnight -> free.
    "Darkness": lambda s, w: _reach(s, w, c.JARDIN),
    # code DemonAnimation: small demon reaches you in the final hallway.
    "LongHallway": lambda s, w: _reach(s, w, c.COULOIR_FINAL),
    # code GameManager.InspectStrangeSymbol: in the maze heart (needs Compass).
    "HedgeMaze": lambda s, w: _reach(s, w, c.LABYRINTHE_COEUR),
    # regions_v3 (2026-07-12): World End = reach Hell then die (dying is free).
    "WorldEnd": lambda s, w: _reach(s, w, c.HELL),
    "GoodEnd": _end_good,
    # code Dog.Attack: the angry fisherman's dog attacks. Reaching the cabin approach is
    # enough (dying is free).
    # TODO playtest: confirm the Dog ending still triggers when the player HOLDS Bone in
    # inventory (giving the bone is optional) - critical for the all_endings goal.
    "Dog": lambda s, w: _reach(s, w, c.CABANE_PECHEUR),
    # code Interaction.GoPicnic: place Blanket + Sandwich at the picnic (Jardin 100 %).
    "Picnic": lambda s, w: _reach(s, w, c.PICNIC) and s.has_all(("Blanket", "Sandwich"), w.player),
}


def _has_true_ending(state: CollectionState, world: "GrunnWorld") -> bool:
    # code RestoreOwnerSoul: consumes the 3 Soul Fragments, on top of the good ending.
    return _end_good(state, world) and state.has_all(
        ("SoulFragment1", "SoulFragment2", "SoulFragment3"), world.player
    )


def set_all_rules(world: "GrunnWorld") -> None:
    player = world.player

    # "Obtain X" logical checks.
    for item, rule in OBTAIN_RULES.items():
        set_rule(world.get_location(f"Obtain {item}"), lambda s, r=rule: r(s, world))

    # Ending checks.
    for ending, rule in ENDING_RULES.items():
        set_rule(world.get_location(f"Ending: {ending}"), lambda s, r=rule: r(s, world))

    # Ghost checks REQUIRE the Trumpet [J 2026-07-16, in-game + code]: ghosts are
    # invisible and untouchable until revealed, and Ghost.Show() is only ever called
    # by GameManager.ShowNearbyGhosts(), itself only called by PerformTrumpetAction()
    # (GameManager.cs:5153-5167). Touching a hidden ghost is impossible
    # (Interaction.GhostTouch acts on the ghost object, Ghost.Touch checks
    # activeInHierarchy). Without this rule the generator treated the 8 ghosts as
    # free and could hide progression behind them (seed 7: ChurchKey/Compass/Trowel).
    if world.options.ghost_checks:
        for name in locations.GHOST_LOCS:
            add_rule(world.get_location(name), lambda s: s.has("Trumpet", player))

    # Gulden #8 is inside a pot on the road that must be smashed with the Hammer
    # [J 2026-07-13]. (Gulden locations only exist under coinsanity.)
    if world.options.coinsanity:
        set_rule(
            world.get_location("Gulden #8 (Unknown)"),
            lambda s: s.has("Hammer", player),
        )

    # Completion condition per goal (design/apworld_design.md section 2).
    goal = world.options.goal.value
    if goal == c.GOAL_GOOD:
        world.multiworld.completion_condition[player] = lambda s: _end_good(s, world)
    elif goal == c.GOAL_ALL:
        endings = list(ENDING_RULES.values())
        world.multiworld.completion_condition[player] = (
            lambda s: all(rule(s, world) for rule in endings)
        )
    else:  # GOAL_TRUE (default)
        world.multiworld.completion_condition[player] = lambda s: _has_true_ending(s, world)
