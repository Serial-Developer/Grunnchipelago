# Grunn — Proposition v1 du graphe de régions AP

Candidats régions = composantes connexes du graphe de portails (46 arêtes mesurées).
Les liaisons À PIED entre régions ne sont pas dans les données de scène : à définir manuellement
dans `regions_v1.json` -> `connectionsManuelles`, avec la règle d'accès de chaque liaison.

## 11 régions candidates

### R00 — À NOMMER (AppleSpace...) (37 zone(s), 75 checks)
Zones : AppleSpace, AtticRoom, Bakery, BakeryPassage, BehindHouse, BigHouseAttic, BigHouseFridge, BigHouseHallway, BigHouseKitchen, BigHouseOffice, BigHouseOverloop, BigHouseStairs, Bijkeuken, BottleRoom, Forest, ForestPassage, GasStationOffice, GlassHouse, GnomeForest, HedgeMaze, HedgeMazeInner, HooibaalGarden, HooibaalSchuur, Intratuin, Kelder, LongHallway, MagicPond, OrbRoom, Park (extérieur), PlayerSchuur, RoundHallway, SmallChapelOutside, SnowWorld, StartGarden (extérieur), Tent, Toilet, Void
Conditions rencontrées à l'intérieur : AllPlanksUsed, BraamstruikDestroyed, BraamstruikLit, BranchHoleSearched, CandleLit, CandleNotLit, CarboardBoxIsOpen, CardboardBoxIsClosed, CompletedToiletPaperSequence, ComputerNotOn, CreatedShortcut, DestroyedStartGnome … (+79)

### R02 — À NOMMER (Church (extérieur)...) (5 zone(s), 28 checks)
Zones : Church (extérieur), Hill, HillPassage, PillarSpace, Road
Conditions rencontrées à l'intérieur : AllPlanksUsed, BridgeRepaired, ChurchDoorOpen, ClearedPizzaBox, DontHaveAnyPlank, DoorknobBranchHoleSearched, HasEggballAndNotTradedEggball, HasMedalAndNotAwardedSnail, HasWormAndCanBeUsed, HighBridgeNotDetroyed, KeyItemNotObtained(Coin), KeyItemNotObtained(Doorknob) … (+28)

### R03 — À NOMMER (ChurchBigHall...) (4 zone(s), 3 checks)
Zones : ChurchBigHall, ChurchHallway, Crypt, CryptStairs
Conditions rencontrées à l'intérieur : CandleLit, CandleNotLit, InsertedFlowerGem, KeyItemNotObtained(FlowerGem), KeyItemNotObtained(GnomeIdol), KeyItemNotObtained(Lighter), KeyItemNotObtained(ShortIdol), KeyItemNotObtained(ShyIdol), KeyItemNotObtained(TallIdol), KeyItemObtained(FlowerGem), KeyItemObtained(Lighter), ObtainedGnomeIdolButNotUsed … (+7)

### R06 — À NOMMER (Ferry...) (3 zone(s), 2 checks)
Zones : Ferry, Veerboot, VeerbootHallway
Conditions rencontrées à l'intérieur : GaveToyToFerryKid, KeyItemNotObtained(ToyBoat), KeyItemObtained(ToyBoat), NotGaveToyToFerryKid

### R10 — À NOMMER (VeerbootHuis...) (2 zone(s), 6 checks)
Zones : VeerbootHuis, WindyPath
Conditions rencontrées à l'intérieur : FishermanGaveWorm, FishermanNotGaveWorm, GaveBoneToDog, KeyItemNotObtained(Bone), KeyItemNotObtained(Worm), KeyItemObtained(WateringCan), KeyItemObtained(Worm), NotGaveBoneToDog, ObjectActive, ObjectInactive

### R01 — Bunker (1 zone(s), 3 checks)
Zones : Bunker
Conditions rencontrées à l'intérieur : BunkerDoorDestroyed, BunkerDoorNotClosed, KeyItemNotObtained(SeveredHand), KeyItemObtained(SeveredHand), ReturnedSeveredHand

### R07 — GasStation (1 zone(s), 3 checks)
Zones : GasStation

### R05 — CornFieldCenter (1 zone(s), 2 checks)
Zones : CornFieldCenter

### R04 — CornField (1 zone(s), 1 checks)
Zones : CornField

### R09 — RummikubSpace (1 zone(s), 1 checks)
Zones : RummikubSpace
Conditions rencontrées à l'intérieur : KeyItemNotObtained(Lighter), KeyItemObtained(Lighter), LitRummikubHooibaal, NotLitRummikubHooibaal

### R08 — OutsideVillage (1 zone(s), 0 checks)
Zones : OutsideVillage

## Interactions de voyage détectées (candidats connexions inter-régions)

- **BusSign** depuis (zone à préciser) — libre
- **BikeTravelToRummikub** depuis (zone à préciser) — libre
- **BusDriver** depuis (zone à préciser) — libre
- **BusDriver** depuis (zone à préciser) — libre
- **BoatTravelToPark** depuis Church (extérieur) — conditions : KeyItemNotObtained(Paddle)
- **BoatInspect** depuis Church (extérieur) — conditions : KeyItemObtained(Paddle)
- **BoatTravelToChurch** depuis Park (extérieur) — conditions : KeyItemNotObtained(Paddle)
- **BoatInspect** depuis Park (extérieur) — conditions : KeyItemObtained(Paddle)
- **BikeTravelToPath** depuis RummikubSpace — libre
- **BusSeat** depuis (zone à préciser) — libre
- **BusSeat** depuis (zone à préciser) — libre
- **BusSeat** depuis (zone à préciser) — libre
- **BusSeat** depuis (zone à préciser) — libre
- **BusSeat** depuis (zone à préciser) — libre
- **BusSeat** depuis (zone à préciser) — libre
- **BusSeat** depuis (zone à préciser) — libre
- **BusSeat** depuis (zone à préciser) — libre
- **BusSeat** depuis (zone à préciser) — libre
- **BusSeat** depuis (zone à préciser) — libre
- **BusSeat** depuis (zone à préciser) — libre
- **BusSeat** depuis (zone à préciser) — libre
- **BusSeat** depuis (zone à préciser) — libre
- **BusSeat** depuis (zone à préciser) — conditions : ObjectActive
- **BusSeat** depuis (zone à préciser) — libre
- **BusSeat** depuis (zone à préciser) — libre
- **BusSeat** depuis (zone à préciser) — libre
- **BusSeat** depuis (zone à préciser) — libre

## Travail de révision attendu

1. Nommer les régions multi-zones, fusionner les singletons qui vont ensemble à pied.
2. Remplir `connectionsManuelles` : chaque passage à pied/porte/voyage entre régions, avec sa règle.
3. Signaler les zones de scénario (SnowWorld, Void, LongHallway...) : entrée par événement, pas par déplacement.