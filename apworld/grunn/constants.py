"""Shared string constants for the Grunn apworld.

Keeping region names in one import-free module avoids circular imports between
``regions.py`` (which builds the graph using rule helpers) and ``rules.py``
(which references region names for ``can_reach_region`` checks).
"""

GAME_NAME = "Grunn"

# --- Regions (map-level, derived from design/regions.md) ---------------------
# One map region can fold several technical scene zones together when the game
# connects them freely (e.g. Intratuin is folded into Jardin). See regions.md
# "Régions (nom carte -> zones techniques)".
MENU = "Menu"                       # origin region = spawn (Route/bus); holds Obtain BridgeKey
JARDIN = "Jardin"                   # StartGarden + Intratuin (+ player shed content)
COUR = "Cour"                       # BehindHouse
BUANDERIE = "Buanderie"             # Bijkeuken (GardenKey)
MANOIR = "Manoir"                   # BigHouse* + ChurchKey in hall (disc chain)
EXTERIEUR = "Exterieur"             # Road + OutsideVillage
EGLISE = "Eglise"                   # Church (ext) + ChurchBigHall + ChurchHallway
HELL = "Hell"                       # ScenarioType.Hell endgame (Owner, attic sword, soul fragments)
PILLAR_SPACE = "PillarSpace"        # broken church door space (Trumpet)
PARC = "Parc"                       # Park
HOOIBAAL = "HooibaalGarden"         # HooibaalGarden + HooibaalSchuur (kid shop)
GAS_STATION = "GasStation"          # GasStation
GAS_OFFICE = "GasStationOffice"     # GasStationOffice
APPLE_SPACE = "AppleSpace"          # AppleSpace (disc face A)
VOID = "Void"                       # Void (disc face B -> manoir office)
GLASS_HOUSE = "GlassHouse"          # GlassHouse (SpecialSeed)
CABANE_JOUEUR = "CabaneJoueur"      # PlayerSchuur (Shears, ToiletKey)
TOILET = "Toilet"                   # Toilet
TENTE = "Tente"                     # Tent (blue Coin)
COULOIR_FINAL = "CouloirFinal"      # LongHallway (final-day hallway ending)
MAGIC_POND = "MagicPond"            # MagicPond (fish revival)
LABYRINTHE = "Labyrinthe"           # HedgeMaze entrance
LABYRINTHE_COEUR = "LabyrintheCoeur"  # HedgeMazeInner (TallIdol, Hammer)
PASSAGE_GNOMES = "PassageGnomes"    # GnomeForest + RoundHallway
CHAMP_MAIS = "ChampMais"            # CornField + CornFieldCenter
PICNIC = "Picnic"                   # Forest (Jardin 100 %)
PLAGE = "Plage"                     # Hill (Eglise 100 %)
BOULANGERIE = "Boulangerie"         # Bakery (Parc 100 %)
ZONE_VELO = "ZoneVelo"              # RummikubSpace (PurifiedStone, via bike)
BUNKER = "Bunker"                   # Bunker (Trowel, SeveredHand return)
CABANE_PECHEUR = "CabanePecheur"    # WindyPath approach (fisherman, dog, AbandonedKey)
CABANE_PECHEUR_INT = "CabanePecheurInt"  # VeerbootHuis interior (ToyBoat) - needs Bone for the dog
FERRY = "Ferry"                     # Ferry + Veerboot + VeerbootHallway (ShortIdol)

ALL_REGIONS = [
    MENU, JARDIN, COUR, BUANDERIE, MANOIR, EXTERIEUR, EGLISE, HELL, PILLAR_SPACE,
    PARC, HOOIBAAL, GAS_STATION, GAS_OFFICE, APPLE_SPACE, VOID, GLASS_HOUSE,
    CABANE_JOUEUR, TOILET, TENTE, COULOIR_FINAL, MAGIC_POND, LABYRINTHE,
    LABYRINTHE_COEUR, PASSAGE_GNOMES, CHAMP_MAIS, PICNIC, PLAGE, BOULANGERIE,
    ZONE_VELO, BUNKER, CABANE_PECHEUR, CABANE_PECHEUR_INT, FERRY,
]

# --- Goal option values (mirrored in options.py Choice) -------------------------
GOAL_GOOD = 0
GOAL_TRUE = 1
GOAL_ALL = 2

# --- Shop prices in gulden (used by coinsanity economy) -------------------------
# Sources: design/regions.md "Économie" + dump shop costs.
PRICE_BUS = 10
PRICE_CD = 5
PRICE_COMPASS = 4
PRICE_OFFICE_KEY = 2
PRICE_MEDAL = 10
PRICE_EGGBALL = 5

# --- "Bad" endings: the ones that KILL the player -------------------------------
# Exactly the DeathLink set (decision Jonath 2026-07-13, mirrored in the client's
# GameIds.DeathLinkEndings): every ending EXCEPT Bus, Picnic and GoodEnd. The
# exclude_bad_endings option removes their checks (locations.py).
DEATH_ENDINGS = (
    "Mist", "SacredFlowers", "Drown", "Darkness",
    "LongHallway", "HedgeMaze", "WorldEnd", "Dog",
)
