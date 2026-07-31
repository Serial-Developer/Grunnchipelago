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
    # regions_v3: "Os : Hammer OU MagicSword OU Trowel OU via le Manoir".
    # VERIFIED, unchanged [J 2026-07-28, in-game]: the bone lies FREE only on the manor
    # hallway table (dump: BigHouse_Hallway/hallwayTable0/bone0); the four other sources
    # are SKELETONS that must be smashed with one of the three tools. The dump does NOT
    # show this - those pickups all carry preventTypes = [] - because the gate is not an
    # interaction condition but the HIT mechanic: SkeletonBone.Smash has a single caller,
    # HitReceiver (HitReceiver.cs:110) fed by the weapon Hurtbox, so it takes a swung tool.
    # Do NOT "simplify" this to a free pickup on the strength of the dump alone.
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


def garden_30(state: CollectionState, world: "GrunnWorld") -> bool:
    """StartGarden at >= 30 %, the threshold that makes the magpie appear.

    code GameManager.cs:3046-3050: entering StartGarden with
    GetAreaCompletedPercentage(StartGarden) >= 30 calls UnlockMagpie(); the magpie stays
    hidden while NotUnlockedMagpie (dump: magpieAliveContentHider0).

    Either MOWING (Shears|MagicSword) or WATERING (WateringCan|Coin) alone clears 30 %
    [J in-game 2026-07-21]. The Trowel is deliberately NOT a route: molehills + litter
    together stay UNDER 30 % [J, correction 2026-07-21], so the Trowel only ever helps on
    top of a route that already suffices. Still a much lower bar than complete_zone, which
    demands mowing AND watering together.
    """
    return _reach(state, world, c.JARDIN) and (
        can_cut_grass(state, world) or can_water(state, world)
    )


def can_advance_days(state: CollectionState, world: "GrunnWorld") -> bool:
    """Can the player reach day 2+ (i.e. SLEEP)?

    Days are normally FREE (design section 6) because sleeping has no prerequisite. But
    the only usable BED is inside the player hut (dump: Hide_PlayerSchuur/interior/bed0,
    type=Bed; the game's only other bed is in the endgame AtticRoom), so with
    lock_player_hut the hut key gates SLEEPING itself, and with it every day-gated check
    [J 2026-07-27, found in playtest: hut locked -> stuck on day 1 forever].
    """
    if not world.options.lock_player_hut:
        return True
    return state.has("AbandonedKey", world.player)


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
    # The ONLY first source of a worm is the garden plate, and it requires the APPLE to be
    # placed on it [J 2026-07-21, confirme au dump + code] : dump Main/Interactions/worm0
    # preventTypes ObjectInactive + NotPlacedApple, flag ProgressData.placedApple pose par
    # GameManager.PlaceApple (GameManager.cs:4690). The two other worm objects are NOT
    # first sources - both are circular:
    #   - wormFisherman0 only appears once you ALREADY gave a worm to the fisherman
    #     (dump: FishermanWormContentHider0, hideConditions NotGaveWormToFisherman);
    #   - wormMagpie0 is the worm you get back after feeding the magpie (and it is a
    #     repeatable pickup, so it is not an AP location either).
    "Worm": lambda s, w: _reach(s, w, c.JARDIN) and s.has("Apple", w.player),
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
    # dump: StartGarden (severedHand0). Requires the TallMan scare at the hut window,
    # which only triggers once the player has been INSIDE the hut (dump:
    # tallManOutsideWindow0, hideCondition PlayerNotInPlayerSchuur) [J 2026-07-27].
    "SeveredHand": lambda s, w: _reach(s, w, c.CABANE_JOUEUR),
    # dump: Park (pizzaBox0), hidden until the "hangjongeren" show up = day 2
    # [J 2026-07-27] (pizzaBoxContentHider0 HangjongerenNotAppeared + the hangjong
    # hiders' DayIndexIsNot).
    "PizzaBox": lambda s, w: _reach(s, w, c.PARC) and can_advance_days(s, w),
    # dump: GasStation (free) OR HooibaalSchuur shop (2)
    "OfficeKey": lambda s, w: _reach(s, w, c.GAS_STATION) or _reach(s, w, c.HOOIBAAL),
    # dump: Park (free) OR Road (free) OR GasStation shop (5)
    "Lighter": lambda s, w: _reach(s, w, c.EXTERIEUR) or _reach(s, w, c.PARC) or _reach(s, w, c.GAS_STATION),
    # dump: HooibaalSchuur shop (5)
    "Cd": lambda s, w: _reach(s, w, c.HOOIBAAL) and can_afford(s, w, c.PRICE_CD),
    # regions_v3 I.1: break the garden gnome + enter the gas station (which fires the
    # jumpscare), then a portal in the Jardin yields the idol.
    # HAMMER ONLY [J 2026-07-27, confirmed in code]: regions_v3 claimed
    # Hammer|MagicSword|Trowel, but Gnome.GetHit (Gnome.cs:182) opens with
    # `if (curEquipmentData.handRightItem != Item.Hammer || curState == Hide) return;`
    # and GameManager.DestroyGnome() has that method as its ONLY caller. Neither the
    # sword nor the trowel can break a gnome, so the old rule was too permissive - it
    # could hide a REQUIRED idol behind a trowel-only route and strand the seed.
    "GnomeIdol": lambda s, w: _reach(s, w, c.JARDIN)
    and _reach(s, w, c.GAS_STATION)
    and s.has("Hammer", w.player),
    # NOTE: "OldKey" has NO rule and NO location - the key is not obtainable in game
    # [J 2026-07-27] (see locations.UNSOURCED_LOCATIONS). Same for "AbandonedKey" below.
    # dump: PlayerSchuur
    "ToiletKey": lambda s, w: _reach(s, w, c.CABANE_JOUEUR),
    # dump: CornFieldCenter
    "ToiletPaper": lambda s, w: _reach(s, w, c.CHAMP_MAIS),
    # dump: Church. FREE on purpose - two independent sources [J 2026-07-21]:
    #   - searching the examinable TREE HOLE (doorknobBranchHoleInteraction0, preventTypes
    #     = DoorknobBranchHoleSearched only, so no prerequisite at all), which is the
    #     fallback when the knob was NOT already found while gardening;
    #   - mowing the church grass, which spawns it there (GameManager.SpawnDoorknob, only
    #     while !searchedDoorknobBranchHole).
    # So no gardening requirement here - but POSSESSING the Doorknob is what opens the
    # PillarSpace (see regions.py), which is a separate matter.
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
    # dump: BigHouseOffice (strangeKey0_old, free) OR StartGarden magpie. The magpie pickup
    # is strangeKey0_demo, child of magpieDeadByWorm0 = the canonical magpie key drop
    # ("_demo" is naming only, see Trowel note) [J 2026-07-13]. The magpie route needs the
    # Worm to feed it AND the garden at >= 30 % for the magpie to exist at all
    # (garden_30) [J 2026-07-21].
    "StrangeKey": lambda s, w: _reach(s, w, c.MANOIR)
    or (_reach(s, w, c.JARDIN) and s.has("Worm", w.player) and garden_30(s, w)),
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
    # NOTE: "AbandonedKey" has NO rule and NO location: there is no vanilla check for it
    # [J 2026-07-27]. The ITEM still exists (lock_player_hut uses it to lock the hut) and
    # is placed elsewhere by fill - only its "Obtain" location is gone.
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
    # dump: StartGarden. Full chain confirmed [J 2026-07-27]: plant the SpecialSeed in the
    # pot (in the start garden, reachable from the start), water it EVERY DAY, and the
    # bloom appears on DAY 3. Hence: SpecialSeed (dump hider prettyFlower_ContentHider0,
    # condition NotSpecialSeedPlanted) + can_advance_days (day 3).
    # NOTE: the WateringCan specifically, NOT can_water - the rain trick (Coin + church)
    # cannot be repeated daily up to day 3 [J].
    "PrettyFlower": lambda s, w: _reach(s, w, c.JARDIN)
    and s.has_all(("SpecialSeed", "WateringCan"), w.player)
    and can_advance_days(s, w),
    # Revive the dead fish at the MAGIC POND ONLY [J 2026-07-27, confirmed in code]. The
    # RoundHallway fishbowl does NOT revive - it only holds/retrieves the DEAD fish
    # (FishbowlRetrieveDeadFish; there is no "retrieve alive"), and reviving is exclusively
    # GameManager.PlaceFishInMagicPond -> the client fires "Obtain GoldFishAlive" on placing
    # the dead fish there. The old PASSAGE_GNOMES alternative was a false route.
    "GoldFishAlive": lambda s, w: _reach(s, w, c.MAGIC_POND) and s.has("GoldFishDead", w.player),
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
    # code: the mist is a scenario day (MistDay) - day 3 [J 2026-07-27, in-game], so it
    # needs sleeping (can_advance_days: free unless lock_player_hut).
    "Mist": lambda s, w: _reach(s, w, c.JARDIN) and can_advance_days(s, w),
    # code Interaction.BusSeat (line 34204): `if (dayIndex <= 1) return;` -> day 2+ needed,
    # plus boughtBusTicket (10 gulden).
    "Bus": lambda s, w: can_afford(s, w, c.PRICE_BUS) and can_advance_days(s, w),
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


# --- "Deed" rules: rewarded ACTIONS (demande Jonath 2026-07-28) ------------------
# Each one is an in-game deed the player performs with an item, hooked client-side on the
# game's own method (see design/backlog_checks.md for the full trace). They are logical
# checks: holding the item is what gates them, plus reaching the spot.
DEED_RULES: dict[str, Rule] = {
    # code GameManager.ClearPizzaBox (GameManager.cs:4346), interaction PizzaBoxClear on the
    # Road (dump: Hide_Road/pizzaClearInteraction0, -52.7/11.64/-50.01).
    "Deed: Throw Away PizzaBox": lambda s, w: _reach(s, w, c.EXTERIEUR)
    and s.has("PizzaBox", w.player),
    # code GameManager.PutPrettyFlowerInVase (GameManager.cs:6058); the vase is at the
    # church (dump: Hide_ChurchMid/PrettyVaseContainer, MACRO:Church).
    # NOTE: PrettyFlower already implies SpecialSeed + WateringCan + day 3, so this is one
    # of the deepest checks of the set.
    "Deed: Place PrettyFlower in Vase": lambda s, w: _reach(s, w, c.EGLISE)
    and s.has("PrettyFlower", w.player),
    # code GameManager.KidReadyTrigger (GameManager.cs:6008) fires once
    # schoolbandCompleteIndex >= 3, fed by KidGiveTrumpet/Cymbals/Triangle (5970/5983/5996).
    "Deed: Complete the School Band": lambda s, w: _reach(s, w, c.PARC)
    and s.has_all(("KidTrumpet", "KidCymbals", "KidTriangle"), w.player),
    # code Fishbowl.PlaceFishAlive (InteractionType.FishbowlPlaceFishAlive, Interaction.cs:370);
    # the bowl is in the RoundHallway = PASSAGE_GNOMES (dump: fishbowl_place_fish_alive0).
    # Separate from "Obtain GoldFishAlive" (which fires when the DEAD fish is dropped in the
    # Magic Pond) - the bowl still does NOT revive anything.
    "Deed: Place GoldFishAlive in Fishbowl": lambda s, w: _reach(s, w, c.PASSAGE_GNOMES)
    and s.has("GoldFishAlive", w.player),
    # code GameManager.ReturnWorm (GameManager.cs:5887), interaction WormReturn on the worm
    # hill. The hill sits in the CHURCH corner geometrically (dump:
    # ChurchArea/Hide_ChurchCorner/WormHillContainer) but is only REACHABLE FROM HELL
    # [J 2026-07-28, in-game] - hence HELL, not EGLISE.
    "Deed: Return Worm to the Worm Hill": lambda s, w: _reach(s, w, c.HELL)
    and s.has("Worm", w.player),
    # code Snail.Award (Snail.cs:177), interaction SnailAward in the PillarSpace. The dump's
    # only prevent is SnailStateIsRacing (the snail must have FINISHED racing) and the race
    # ends around 23:45 on DAY 2 [J 2026-07-28, in-game] - so it needs sleeping, which
    # matters under lock_player_hut.
    "Deed: Award Medal to the Snail": lambda s, w: _reach(s, w, c.PILLAR_SPACE)
    and s.has("Medal", w.player)
    and can_advance_days(s, w),
    # code GameManager.ReturnSeveredHand (GameManager.cs:4242); the corpse is in the BUNKER
    # (dump: Areas/Bunker/.../returnSeveredHand0, areas=[Bunker]).
    # SeveredHand already implies reach(CabaneJoueur) - the TallMan scare only triggers once
    # the player has been inside the hut.
    "Deed: Return SeveredHand": lambda s, w: _reach(s, w, c.BUNKER)
    and s.has("SeveredHand", w.player),
}


# --- "Chore" rules: the five garden maintenance jobs (demande Jonath 2026-07-30) ------
# Vanilla pays 2 gulden the first time each job is finished IN THE START GARDEN
# (GameManager.areaCompleteGuldenAdd = 2, one flag per job:
#   cutAllGrassInStartGardenArea       GameManager.CutGrass:3131
#   trimmedAllHedgesInStartGardenArea  TrimBall.cs:149
#   removedAllMolehillsInStartGardenArea Molehill.cs:184
#   wateredAllFlowersInStartGardenArea Flower.cs:529
#   clearedAllTrashInStartGardenArea   Troepje.cs:100 )
# Those payouts are now CHECKS. Tool requirements are Jonath's [2026-07-30]; each also
# needs the garden itself, which is where the job happens.
CHORE_RULES: dict[str, Rule] = {
    "Chore: Trim Every Hedge (Garden)": lambda s, w: _reach(s, w, c.JARDIN)
    and can_cut_grass(s, w),          # shears OR magic sword
    "Chore: Cut All the Grass (Garden)": lambda s, w: _reach(s, w, c.JARDIN)
    and can_cut_grass(s, w),          # shears OR magic sword
    "Chore: Clear Every Molehill (Garden)": lambda s, w: _reach(s, w, c.JARDIN)
    and s.has("Trowel", w.player),
    "Chore: Water Every Flower (Garden)": lambda s, w: _reach(s, w, c.JARDIN)
    and can_water(s, w),              # watering can OR blue coin (+ church rain)
    # The litter one is NOT free: some of it sits INSIDE the toilets [J 2026-07-30], and
    # the toilet door needs the ToiletKey (regions.py: JARDIN -> TOILET).
    "Chore: Pick Up All the Litter (Garden)": lambda s, w: _reach(s, w, c.JARDIN)
    and _reach(s, w, c.TOILET),
    # The 8 potted plants, trimmed with shears OR the magic sword [J 2026-07-30]. Unlike the
    # five above this one pays NOTHING in vanilla (achievement only) - hence no extra Golden
    # Gulden in the pool. Criterion: pottedPlantTrimmedCur >= trimmedPottedPlantMax = 8
    # (SaveManager.cs:1350). Locations given by Jonath [2026-07-30], 8 pots exactly:
    #   1 player hut (DAY 2 only) - 1 toilets - 2 gas station - 2 gas station office
    #   - 2 manor office
    # ALL of them must be trimmed, so the rule is the INTERSECTION of every access. The gas
    # station and its office are implied by the manor (EXTERIEUR -> GAS_STATION -> GAS_OFFICE
    # -> VOID -> MANOIR is the only chain, regions.py), but they stay spelled out: the rule
    # must survive a future change to that graph.
    "Chore: Trim Every Potted Plant": lambda s, w: can_cut_grass(s, w)
    and _reach(s, w, c.CABANE_JOUEUR)
    and can_advance_days(s, w)          # the hut pot only exists on day 2
    and _reach(s, w, c.TOILET)
    and _reach(s, w, c.GAS_STATION)
    and _reach(s, w, c.GAS_OFFICE)
    and _reach(s, w, c.MANOIR),
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

    # Ending checks. Some may not exist: exclude_bad_endings drops the 8 death endings
    # from create_all_locations, so only rule the locations that were actually created.
    created = {location.name for location in world.multiworld.get_locations(player)}
    for ending, rule in ENDING_RULES.items():
        name = f"Ending: {ending}"
        if name not in created:
            continue
        set_rule(world.get_location(name), lambda s, r=rule: r(s, world))

    # "Deed" checks (rewarded actions).
    for name, rule in DEED_RULES.items():
        set_rule(world.get_location(name), lambda s, r=rule: r(s, world))

    # "Chore" checks (the garden maintenance jobs + the potted plants) - only when the
    # chore_checks option created them.
    for name, rule in CHORE_RULES.items():
        if name not in created:
            continue
        set_rule(world.get_location(name), lambda s, r=rule: r(s, world))

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
        # Two ghosts only exist on DAY 2 [J 2026-07-27] -> they need sleeping:
        #  - #3 = ghost0_redCar0    (dump: ghost0_redCar_ContentHider0, DayIndexIsNot day=2)
        #  - #4 = ghost0_scooterCrash0 (dump: ScooterCrashContentHider0, DayIndexIsNot day=2)
        for name in ("Calm Ghost #3 (Road)", "Calm Ghost #4 (WindyPath)"):
            add_rule(world.get_location(name), lambda s: can_advance_days(s, world))

    # Two polaroids only APPEAR in the start garden after talking to the Orb in the
    # Orb Room [J 2026-07-16, dump: Polaroid_crypt_contentHider0 /
    # Polaroid_gnomeIdol_contentHider0, condition NotTalkedToOrbInOrbRoom]. The Orb
    # Room is first entered through the final hallway (portal_LongHallwayToOrbRoom0;
    # the StartGarden portal stays hidden until that first talk), so they are gated
    # on CouloirFinal - not free garden pickups as their position suggests.
    if world.options.polaroid_checks:
        for name in ("Polaroid: Crypt", "Polaroid: GnomeIdol"):
            add_rule(
                world.get_location(name),
                lambda s: _reach(s, world, c.COULOIR_FINAL),
            )
        # Polaroid: GasStation sits INSIDE the smashed garden gnome (dump: hider
        # objectRef gardengnome_Destroyed, condition GnomeNotDestroyed), so it needs
        # the HAMMER - the same gate the GnomeIdol obtain models (see the OBTAIN_RULES
        # comment: Gnome.GetHit accepts Item.Hammer and nothing else).
        add_rule(
            world.get_location("Polaroid: GasStation"),
            lambda s: s.has("Hammer", player),
        )
        # Two garden polaroids require having been INSIDE the player hut [J 2026-07-27]:
        #  - MagpieNest: hider polaroidMagpieNest_hider0, condition NotEnteredPlayerSchuur;
        #  - TallManWindow: the shot is the TallMan at the window, and he only appears
        #    while the player is inside (hider tallManOutsideWindow0,
        #    condition PlayerNotInPlayerSchuur).
        # Free normally, but gated by the key under lock_player_hut.
        for name in ("Polaroid: MagpieNest", "Polaroid: TallManWindow"):
            add_rule(
                world.get_location(name),
                lambda s: _reach(s, world, c.CABANE_JOUEUR),
            )

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
