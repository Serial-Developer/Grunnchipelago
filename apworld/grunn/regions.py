"""Region graph for the Grunn apworld.

Each connection mirrors one line of design/regions_v3.md "Connexions et regles
d'acces" (cited in-comment). Scenario/time states (InRedWorld, after-midnight window,
final day, day indices) are logically FREE per design section 6.
"""

from __future__ import annotations

from typing import TYPE_CHECKING

from BaseClasses import Region

from . import constants as c
from . import rules

if TYPE_CHECKING:
    from . import GrunnWorld


def create_all_regions(world: "GrunnWorld") -> None:
    world.multiworld.regions += [
        Region(name, world.player, world.multiworld) for name in c.ALL_REGIONS
    ]


def connect_all_regions(world: "GrunnWorld") -> None:
    def link(src: str, dst: str, rule=None) -> None:
        world.get_region(src).connect(world.get_region(dst), rule=rule)

    r = rules  # shorthand
    p = world.player

    # --- Start & main axis ------------------------------------------------------
    # regions_v3: Spawn (Route, bus) -> Jardin : BridgeKey (the bus blocks every other
    # exit). BridgeKey is picked up at spawn, so its own location sits in Menu.
    link(c.MENU, c.JARDIN, lambda s: s.has("BridgeKey", p))
    # regions_v3: Jardin -> Cour derriere maison (libre)
    link(c.JARDIN, c.COUR)
    # regions_v3: Jardin -> Exterieur (pont) : Plank OU OldPlank
    link(c.JARDIN, c.EXTERIEUR, lambda s: r.has_plank(s, world))
    # regions_v3: Cour -> Exterieur (haie) : Shears OU MagicSword
    link(c.COUR, c.EXTERIEUR, lambda s: r.can_cut_grass(s, world))
    # regions_v3: Cour -> Buanderie (libre)
    link(c.COUR, c.BUANDERIE)
    # regions_v3 (2026-07-12 key correction): Jardin -> Eglise (portail) : GardenKey
    link(c.JARDIN, c.EGLISE, lambda s: s.has("GardenKey", p))
    # regions_v3: Exterieur -> Eglise (libre, portail cote Exterieur)
    link(c.EXTERIEUR, c.EGLISE)
    # regions_v3: Parc <-> Exterieur : Lighter
    link(c.EXTERIEUR, c.PARC, lambda s: s.has("Lighter", p))
    # regions_v3: Eglise <-> Parc (barque) : Paddle
    link(c.EGLISE, c.PARC, lambda s: s.has("Paddle", p))
    # regions_v3: Eglise -> Porte cassee (PillarSpace) : 20 % de l'Eglise
    link(c.EGLISE, c.PILLAR_SPACE, lambda s: r.complete_zone(s, world, c.EGLISE, full=False))
    # dump: Road <-> PillarSpace via the repaired doorknob door (Doorknob)
    link(c.EXTERIEUR, c.PILLAR_SPACE, lambda s: s.has("Doorknob", p))
    # regions_v3: Exterieur -> Champ de mais (libre, confirme 2026-07-12)
    link(c.EXTERIEUR, c.CHAMP_MAIS)
    # regions_v3: Exterieur -> Bunker (libre, confirme 2026-07-12)
    link(c.EXTERIEUR, c.BUNKER)

    # --- Endgame: Hell (crypt sequence) -----------------------------------------
    # regions_v3 "zone Hell" (V): from the church, open the interior door with ChurchKey,
    # place the FlowerGem, and deposit the 4 idols. (After-midnight window is free.)
    link(
        c.EGLISE,
        c.HELL,
        lambda s: s.has("ChurchKey", p) and s.has("FlowerGem", p) and s.has_all(r.IDOLS, p),
    )

    # --- House & disc chain -----------------------------------------------------
    # regions_v3: Exterieur -> Station essence (libre au debut ; Hammer apres fermeture)
    link(c.EXTERIEUR, c.GAS_STATION)
    # regions_v3: Station -> Bureau station : OfficeKey
    link(c.GAS_STATION, c.GAS_OFFICE, lambda s: s.has("OfficeKey", p))
    # regions_v3: Bureau station (+ Cd + PC) -> Zone de la pomme OU Void
    link(c.GAS_OFFICE, c.APPLE_SPACE, lambda s: s.has("Cd", p))
    link(c.GAS_OFFICE, c.VOID, lambda s: s.has("Cd", p))
    # regions_v3: Void -> Bureau du manoir (traversee)
    link(c.VOID, c.MANOIR)
    # regions_v3: Bureau du manoir (+ Cd) -> Serre du disque (SpecialSeed)
    link(c.MANOIR, c.GLASS_HOUSE, lambda s: s.has("Cd", p))

    # --- Shed, toilets, end of week ---------------------------------------------
    # dump: player shed is physically inside the start garden (Hide_PlayerSchuur)
    link(c.JARDIN, c.CABANE_JOUEUR)
    # regions_v3: Toilettes : ToiletKey (in the shed)
    link(c.JARDIN, c.TOILET, lambda s: s.has("ToiletKey", p))
    # regions_v3: Toilettes -> Tente : donner ToiletPaper (before day 1 noon; time free)
    link(c.TOILET, c.TENTE, lambda s: s.has("ToiletPaper", p))
    # regions_v3: Cabane joueur -> Couloir final : IsFinalDay + soir. Confirme libre,
    # aucun item, juste attendre le dimanche soir [J 2026-07-13].
    link(c.JARDIN, c.COULOIR_FINAL)
    # dump: LongHallway <-> SmallChapelOutside <-> MagicPond (free)
    link(c.COULOIR_FINAL, c.MAGIC_POND)

    # --- Zone completions (100 % portals) ---------------------------------------
    # regions_v3: Jardin 100 % -> Picnic
    link(c.JARDIN, c.PICNIC, lambda s: r.complete_zone(s, world, c.JARDIN, full=True))
    # regions_v3: Eglise 100 % -> Plage
    link(c.EGLISE, c.PLAGE, lambda s: r.complete_zone(s, world, c.EGLISE, full=True))
    # regions_v3: Parc 100 % -> Boulangerie
    link(c.PARC, c.BOULANGERIE, lambda s: r.complete_zone(s, world, c.PARC, full=True))

    # --- Special zones ----------------------------------------------------------
    # regions_v3: Jardin -> Labyrinthe : (Plank OU OldPlank) OU (Coin + acces Eglise)
    link(
        c.JARDIN,
        c.LABYRINTHE,
        lambda s: r.has_plank(s, world)
        or (s.has("Coin", p) and s.can_reach_region(c.EGLISE, p)),
    )
    # regions_v3: Labyrinthe -> Coeur : Compass
    link(c.LABYRINTHE, c.LABYRINTHE_COEUR, lambda s: s.has("Compass", p))
    # regions_v3: Parc -> Jardin botte de foin : 20 % du Parc + Lighter
    link(
        c.PARC,
        c.HOOIBAAL,
        lambda s: r.complete_zone(s, world, c.PARC, full=False) and s.has("Lighter", p),
    )
    # regions_v3 (2026-07-12): Passage des Gnomes <-> Parc/Jardin :
    # DestroyedAllJumpscareGnomes = Hammer only.
    link(c.JARDIN, c.PASSAGE_GNOMES, lambda s: s.has("Hammer", p))
    link(c.PARC, c.PASSAGE_GNOMES, lambda s: s.has("Hammer", p))
    # dump (2026-07-12): bike is a round trip Exterieur (OutsideVillage) <-> RummikubSpace
    # (toRummikub0 = out, toPath = return, both preventTypes=[]). The return is implicit
    # via AP's origin-return assumption, so only the outbound edge is modelled.
    link(c.EXTERIEUR, c.ZONE_VELO)
    # regions_v3: Embarcadere Ferry libre a pied ; traversee = DayIndexIs(2) (free) + ToyBoat.
    link(c.EXTERIEUR, c.FERRY, lambda s: s.has("ToyBoat", p))
    # regions_v3: fisherman cabin approach is free; ENTERING (interior) needs Bone for the
    # dog (else the dog kills the player -> Dog ending).
    link(c.COUR, c.CABANE_PECHEUR)
    link(c.EXTERIEUR, c.CABANE_PECHEUR)
    link(c.CABANE_PECHEUR, c.CABANE_PECHEUR_INT, lambda s: s.has("Bone", p))


def create_and_connect_regions(world: "GrunnWorld") -> None:
    create_all_regions(world)
    connect_all_regions(world)
