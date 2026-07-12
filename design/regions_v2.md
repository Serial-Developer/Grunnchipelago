# Grunn — Graphe de régions AP v2

Régions = composantes connexes des portails **libres**. Les portails conditionnés par ContentHider
sont des connexions inter-régions avec règle d'accès dérivée des données (négation De Morgan
des hideConditions — sémantique vérifiée dans ContentHider.Update).

## 42 régions candidates

### R06 — À NOMMER (BigHouseHallway...) (4 zone(s), 26 checks)
Zones : BigHouseHallway, BigHouseKitchen, Intratuin, StartGarden (extérieur)

### R11 — À NOMMER (ChurchBigHall...) (4 zone(s), 3 checks)
Zones : ChurchBigHall, ChurchHallway, Crypt, CryptStairs

### R04 — À NOMMER (BigHouseAttic...) (4 zone(s), 2 checks)
Zones : BigHouseAttic, BigHouseOverloop, BigHouseStairs, BottleRoom

### R10 — À NOMMER (Church (extérieur)...) (2 zone(s), 12 checks)
Zones : Church (extérieur), PillarSpace

### R40 — À NOMMER (VeerbootHuis...) (2 zone(s), 6 checks)
Zones : VeerbootHuis, WindyPath

### R14 — À NOMMER (Ferry...) (2 zone(s), 2 checks)
Zones : Ferry, VeerbootHallway

### R02 — À NOMMER (Bakery...) (2 zone(s), 1 checks)
Zones : Bakery, BakeryPassage

### R22 — À NOMMER (Hill...) (2 zone(s), 1 checks)
Zones : Hill, HillPassage

### R15 — À NOMMER (Forest...) (2 zone(s), 0 checks)
Zones : Forest, ForestPassage

### R32 — Road (1 zone(s), 15 checks)
Zones : Road

### R30 — Park (extérieur) (1 zone(s), 12 checks)
Zones : Park (extérieur)

### R19 — GnomeForest (1 zone(s), 5 checks)
Zones : GnomeForest

### R24 — HooibaalSchuur (1 zone(s), 5 checks)
Zones : HooibaalSchuur

### R09 — Bunker (1 zone(s), 3 checks)
Zones : Bunker

### R16 — GasStation (1 zone(s), 3 checks)
Zones : GasStation

### R00 — AppleSpace (1 zone(s), 2 checks)
Zones : AppleSpace

### R07 — BigHouseOffice (1 zone(s), 2 checks)
Zones : BigHouseOffice

### R13 — CornFieldCenter (1 zone(s), 2 checks)
Zones : CornFieldCenter

### R20 — HedgeMaze (1 zone(s), 2 checks)
Zones : HedgeMaze

### R21 — HedgeMazeInner (1 zone(s), 2 checks)
Zones : HedgeMazeInner

### R31 — PlayerSchuur (1 zone(s), 2 checks)
Zones : PlayerSchuur

### R37 — Tent (1 zone(s), 2 checks)
Zones : Tent

### R38 — Toilet (1 zone(s), 2 checks)
Zones : Toilet

### R41 — Void (1 zone(s), 2 checks)
Zones : Void

### R05 — BigHouseFridge (1 zone(s), 1 checks)
Zones : BigHouseFridge

### R08 — Bijkeuken (1 zone(s), 1 checks)
Zones : Bijkeuken

### R12 — CornField (1 zone(s), 1 checks)
Zones : CornField

### R17 — GasStationOffice (1 zone(s), 1 checks)
Zones : GasStationOffice

### R18 — GlassHouse (1 zone(s), 1 checks)
Zones : GlassHouse

### R23 — HooibaalGarden (1 zone(s), 1 checks)
Zones : HooibaalGarden

### R25 — Kelder (1 zone(s), 1 checks)
Zones : Kelder

### R33 — RoundHallway (1 zone(s), 1 checks)
Zones : RoundHallway

### R34 — RummikubSpace (1 zone(s), 1 checks)
Zones : RummikubSpace

### R36 — SnowWorld (1 zone(s), 1 checks)
Zones : SnowWorld

### R01 — AtticRoom (1 zone(s), 0 checks)
Zones : AtticRoom

### R03 — BehindHouse (1 zone(s), 0 checks)
Zones : BehindHouse

### R26 — LongHallway (1 zone(s), 0 checks)
Zones : LongHallway

### R27 — MagicPond (1 zone(s), 0 checks)
Zones : MagicPond

### R28 — OrbRoom (1 zone(s), 0 checks)
Zones : OrbRoom

### R29 — OutsideVillage (1 zone(s), 0 checks)
Zones : OutsideVillage

### R35 — SmallChapelOutside (1 zone(s), 0 checks)
Zones : SmallChapelOutside

### R39 — Veerboot (1 zone(s), 0 checks)
Zones : Veerboot

## 32 connexions conditionnelles (portails + règles auto)

- **BigHouseKitchen** (R06) <-> **SnowWorld** (R36) via `portal_BigHouseKitchenToSnowWorld`
  - masqué si : [OU] NotInRedWorld
  - règle d'accès : **InRedWorld**
- **LongHallway** (R26) <-> **SmallChapelOutside** (R35) via `portal_LongHallwayToOutsideRedChapel0`
  - masqué si : [OU] SmallChapelNotShowing
  - règle d'accès : **NOT(SmallChapelNotShowing)**
- **RoundHallway** (R33) <-> **StartGarden (extérieur)** (R06) via `portal_RoundHallwayToStartGarden0`
  - masqué si : [OU] NotDestroyedAllJumpscareGnomes, InRedWorld, InGoodEnding
  - règle d'accès : **DestroyedAllJumpscareGnomes ET NOT(InRedWorld) ET NOT(InGoodEnding)**
- **RoundHallway** (R33) <-> **Park (extérieur)** (R30) via `portal_RoundHallwayToPark0`
  - masqué si : [OU] NotDestroyedAllJumpscareGnomes
  - règle d'accès : **DestroyedAllJumpscareGnomes**
- **BigHouseOffice** (R07) <-> **BigHouseHallway** (R06) via `portal_BigHouseOfficeToHall`
  - masqué si : [OU] ComputerOn, InsertedDisc
  - règle d'accès : **NOT(ComputerOn) ET NOT(InsertedDisc)**
- **BigHouseFridge** (R05) <-> **BigHouseKitchen** (R06) via `portal_BigHouseFridgeToBigHouseKitchen`
  - masqué si : [OU] InRedWorld
  - règle d'accès : **NOT(InRedWorld)**
- **Veerboot** (R39) <-> **VeerbootHallway** (R14) via `portal_VeerbootToVeerbootHallway`
  - masqué si : [OU] DayIndexIsNot(2)
  - règle d'accès : **DayIndexIs(2)**
- **GnomeForest** (R19) <-> **RoundHallway** (R33) via `portal_GnomeForestToRoundHallway0`
  - masqué si : [OU] NotDestroyedAllJumpscareGnomes
  - règle d'accès : **DestroyedAllJumpscareGnomes**
- **BigHouseHallway** (R06) <-> **Bijkeuken** (R08) via `portal_BigHouseHallToBijkeuken`
  - masqué si : [OU] InRedWorld ; [OU] NotUnlockedBijkeukenShortcut, IsFinalDay
  - règle d'accès : **NOT(InRedWorld) ET UnlockedBijkeukenShortcut ET NOT(IsFinalDay)**
- **Kelder** (R25) <-> **BigHouseHallway** (R06) via `portal_KelderToBigHouseHall`
  - masqué si : [OU] NotInRedWorld
  - règle d'accès : **InRedWorld**
- **Toilet** (R38) <-> **StartGarden (extérieur)** (R06) via `portal_ToiletToStartGarden0`
  - masqué si : [OU] InGoodEnding
  - règle d'accès : **NOT(InGoodEnding)**
- **LongHallway** (R26) <-> **OrbRoom** (R28) via `portal_LongHallwayToOrbRoom0`
  - masqué si : [OU] TalkedToOrbInOrbRoom
  - règle d'accès : **NOT(TalkedToOrbInOrbRoom)**
- **BigHouseHallway** (R06) <-> **BigHouseStairs** (R04) via `portal_BigHouseHallToBigHouseStairs`
  - masqué si : [OU] NotInRedWorld
  - règle d'accès : **InRedWorld**
- **Park (extérieur)** (R30) <-> **HooibaalGarden** (R23) via `portal_ParkToHooiGarden0`
  - masqué si : [OU] ParkNotUnlockedHooibaalGarden ; [OU] ParkNotUnlockedHooibaalGarden
  - règle d'accès : **NOT(ParkNotUnlockedHooibaalGarden) ET NOT(ParkNotUnlockedHooibaalGarden)**
- **BehindHouse** (R03) <-> **HooibaalSchuur** (R24) via `portal_BehindHouseToHooiSchuur0`
  - masqué si : [OU] NotLitAllHooiGardenCandles ; [OU] NotLitAllHooiGardenCandles
  - règle d'accès : **LitAllHooiGardenCandles ET LitAllHooiGardenCandles**
- **Tent** (R37) <-> **Toilet** (R38) via `portal_TentInsideToToilet`
  - masqué si : [OU] NotCompletedToiletPaperSequence
  - règle d'accès : **CompletedToiletPaperSequence**
- **PlayerSchuur** (R31) <-> **AtticRoom** (R01) via `portal_StartGardenToAtticRoom0`
  - masqué si : [OU] AtticRoomNotActive ; [OU] AtticRoomNotActive
  - règle d'accès : **NOT(AtticRoomNotActive) ET NOT(AtticRoomNotActive)**
- **ForestPassage** (R15) <-> **StartGarden (extérieur)** (R06) via `portal_ForestPassageToStartGarden`
  - masqué si : [OU] ObjectIsActive, NotMaintainedGardenArea
  - règle d'accès : **NOT(ObjectIsActive) ET MaintainedGardenArea**
- **GlassHouse** (R18) <-> **BigHouseOffice** (R07) via `portal_GlassHouseToBigHouseOffice`
  - masqué si : [OU] ComputerNotOn, InsertedDisc
  - règle d'accès : **NOT(ComputerNotOn) ET NOT(InsertedDisc)**
- **HedgeMazeInner** (R21) <-> **StartGarden (extérieur)** (R06) via `portal_HedgeMazeEndToStartGarden`
  - masqué si : [OU] KeyItemNotObtained(TallIdol), NotDestroyedTallMan ; [OU] StartHouseNotEntered, KeyItemNotObtained(TallIdol), InRedWorld, InGoodEnding
  - règle d'accès : **KeyItemObtained(TallIdol) ET DestroyedTallMan ET NOT(StartHouseNotEntered) ET KeyItemObtained(TallIdol) ET NOT(InRedWorld) ET NOT(InGoodEnding)**
- **BigHouseOffice** (R07) <-> **Void** (R41) via `portal_BigHouseOfficeToVoid0`
  - masqué si : [OU] ComputerNotOn, NotInsertedDisc
  - règle d'accès : **NOT(ComputerNotOn) ET InsertedDisc**
- **StartGarden (extérieur)** (R06) <-> **OrbRoom** (R28) via `portal_StartGardenToOrbRoom`
  - masqué si : [OU] NotTalkedToOrbInOrbRoom, DisabledStartGardenToOrbRoomPortal ; [OU] NotTalkedToOrbInOrbRoom
  - règle d'accès : **TalkedToOrbInOrbRoom ET NOT(DisabledStartGardenToOrbRoomPortal) ET TalkedToOrbInOrbRoom**
- **AppleSpace** (R00) <-> **GasStationOffice** (R17) via `portal_AppleSpaceToGasStationOffice0`
  - masqué si : [OU] ComputerNotOn ; [OU] ComputerNotOn, InsertedDisc
  - règle d'accès : **NOT(ComputerNotOn) ET NOT(ComputerNotOn) ET NOT(InsertedDisc)**
- **PillarSpace** (R10) <-> **Road** (R32) via `portal_PillarSpaceToRoadMemorial0`
  - masqué si : [OU] NotRepairedMissingDoorknobDoor
  - règle d'accès : **RepairedMissingDoorknobDoor**
- **HooibaalGarden** (R23) <-> **HooibaalSchuur** (R24) via `portal_HooiGardenToHooiSchuur0`
  - masqué si : [OU] NotLitAllHooiGardenCandles ; [OU] NotLitAllHooiGardenCandles
  - règle d'accès : **LitAllHooiGardenCandles ET LitAllHooiGardenCandles**
- **LongHallway** (R26) <-> **AtticRoom** (R01) via `portal_LongHallwayToAtticRoom0`
  - masqué si : [OU] TalkedToOrbInOrbRoom
  - règle d'accès : **NOT(TalkedToOrbInOrbRoom)**
- **SmallChapelOutside** (R35) <-> **MagicPond** (R27) via `portal_OutsideRedChapelMagicPond`
  - masqué si : [OU] SmallChapelNotShowing
  - règle d'accès : **NOT(SmallChapelNotShowing)**
- **GasStationOffice** (R17) <-> **Void** (R41) via `portal_GasStationOfficeToVoid0`
  - masqué si : [OU] ComputerNotOn, NotInsertedDisc ; [OU] ComputerNotOn, NotInsertedDisc
  - règle d'accès : **NOT(ComputerNotOn) ET InsertedDisc ET NOT(ComputerNotOn) ET InsertedDisc**
- **Church (extérieur)** (R10) <-> **HillPassage** (R22) via `portal_ChurchToHillPassage`
  - masqué si : [OU] NotMaintainedChurchArea
  - règle d'accès : **MaintainedChurchArea**
- **StartGarden (extérieur)** (R06) <-> **HedgeMaze** (R20) via `portal_StartGardenToHedgeMazeA`
  - masqué si : [OU] StartHouseNotEntered, KeyItemObtained(TallIdol), InRedWorld, InGoodEnding ; [OU] StartHouseNotEntered, KeyItemObtained(TallIdol)
  - règle d'accès : **NOT(StartHouseNotEntered) ET KeyItemNotObtained(TallIdol) ET NOT(InRedWorld) ET NOT(InGoodEnding) ET NOT(StartHouseNotEntered) ET KeyItemNotObtained(TallIdol)**
- **BakeryPassage** (R02) <-> **Park (extérieur)** (R30) via `portal_BakeryPassageToPark`
  - masqué si : [OU] NotMaintainedParkArea
  - règle d'accès : **MaintainedParkArea**
- **Void** (R41) <-> **GasStationOffice** (R17) via `void_portal1`
  - masqué si : [OU] ComputerNotOn, NotInsertedDisc
  - règle d'accès : **NOT(ComputerNotOn) ET InsertedDisc**

## Interactions de voyage (règles auto)

- **BusSign** depuis Road — règle : **libre**
- **BikeTravelToRummikub** depuis OutsideVillage — règle : **libre**
- **BusDriver** depuis Road — règle : **libre**
- **BusDriver** depuis Road — règle : **libre**
- **BoatTravelToPark** depuis Church (extérieur) — règle : **KeyItemObtained(Paddle)**
- **BoatInspect** depuis Church (extérieur) — règle : **KeyItemNotObtained(Paddle)**
- **BoatTravelToChurch** depuis Park (extérieur) — règle : **KeyItemObtained(Paddle)**
- **BoatInspect** depuis Park (extérieur) — règle : **KeyItemNotObtained(Paddle)**
- **BikeTravelToPath** depuis RummikubSpace — règle : **libre**
- **BusSeat** depuis Road — règle : **libre**
- **BusSeat** depuis Road — règle : **libre**
- **BusSeat** depuis Road — règle : **libre**
- **BusSeat** depuis Road — règle : **libre**
- **BusSeat** depuis Road — règle : **libre**
- **BusSeat** depuis Road — règle : **libre**
- **BusSeat** depuis Road — règle : **libre**
- **BusSeat** depuis Road — règle : **libre**
- **BusSeat** depuis Road — règle : **libre**
- **BusSeat** depuis Road — règle : **libre**
- **BusSeat** depuis Road — règle : **libre**
- **BusSeat** depuis Road — règle : **libre**
- **BusSeat** depuis Road — règle : **libre**
- **BusSeat** depuis Road — règle : **NOT(ObjectActive)**
- **BusSeat** depuis Road — règle : **libre**
- **BusSeat** depuis Road — règle : **libre**
- **BusSeat** depuis Road — règle : **libre**
- **BusSeat** depuis Road — règle : **libre**

## À compléter manuellement : adjacences à pied entre régions

Remplir `connectionsManuelles` dans regions_v2.json (jardin<->route, route<->station, etc.).