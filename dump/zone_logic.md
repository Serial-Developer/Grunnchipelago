# Grunn — Table de logique par zone

Générée le 2026-07-31T11:49:54.704Z depuis grunnchipelago_dump.json (grunnchipelago.dumper 0.3.0).
Source : données de scène extraites du jeu en runtime. Rien n'est inféré hors des heuristiques signalées.

## Résumé global

- Zones avec contenu : 57
- Connexions par portails (dédupliquées) : 46
- KeyItems posés dans le monde : 50
- KeyItems référencés par la logique mais donnés par événement/PNJ : 2

### KeyItems donnés par événement/PNJ (pas de pickup posé)

- GoldFishAlive
- KidTriangle

### Connexions par portails

- AppleSpace <-> GasStationOffice
- AtticRoom <-> LongHallway
- AtticRoom <-> PlayerSchuur
- Bakery <-> BakeryPassage
- BakeryPassage <-> Park (extérieur)
- BehindHouse <-> HooibaalSchuur
- BigHouseAttic <-> BigHouseOverloop
- BigHouseFridge <-> BigHouseKitchen
- BigHouseHallway <-> BigHouseKitchen
- BigHouseHallway <-> BigHouseOffice
- BigHouseHallway <-> BigHouseStairs
- BigHouseHallway <-> Bijkeuken
- BigHouseHallway <-> Kelder
- BigHouseHallway <-> StartGarden (extérieur)
- BigHouseKitchen <-> SnowWorld
- BigHouseOffice <-> GlassHouse
- BigHouseOffice <-> Void
- BigHouseOverloop <-> BigHouseStairs
- BigHouseOverloop <-> BottleRoom
- Church (extérieur) <-> HillPassage
- Church (extérieur) <-> PillarSpace
- ChurchBigHall <-> ChurchHallway
- ChurchBigHall <-> CryptStairs
- Crypt <-> CryptStairs
- Ferry <-> VeerbootHallway
- Forest <-> ForestPassage
- ForestPassage <-> StartGarden (extérieur)
- GasStationOffice <-> Void
- GnomeForest <-> RoundHallway
- HedgeMaze <-> StartGarden (extérieur)
- HedgeMazeInner <-> StartGarden (extérieur)
- Hill <-> HillPassage
- HooibaalGarden <-> HooibaalSchuur
- HooibaalGarden <-> Park (extérieur)
- Intratuin <-> StartGarden (extérieur)
- LongHallway <-> OrbRoom
- LongHallway <-> SmallChapelOutside
- MagicPond <-> SmallChapelOutside
- OrbRoom <-> StartGarden (extérieur)
- Park (extérieur) <-> RoundHallway
- PillarSpace <-> Road
- RoundHallway <-> StartGarden (extérieur)
- StartGarden (extérieur) <-> Toilet
- Tent <-> Toilet
- Veerboot <-> VeerbootHallway
- VeerbootHuis <-> WindyPath

## Zone : AppleSpace

Candidats checks : 2 (pickups 1, polaroids 1, fantômes 0, gulden 0)

### Pickups
- Apple — `NonEuclidian/AppleSpace/Hide_appleSpace/AppleSpaceExtraHiderContainer/apple0`

### Polaroids
- AppleAndWorm — `NonEuclidian/AppleSpace/Hide_appleSpace/AppleSpaceExtraHiderContainer/appleTree0 (3)/polaroid_appleAndWorm0`

### Visibilité conditionnée (ContentHiders)
- [OU] ComputerNotOn, InsertedDisc — objet: AppleSpaceExtraHiderContainer

### Portails sortants
- AppleSpace -> GasStationOffice

## Zone : AtticRoom

Candidats checks : 0 (pickups 0, polaroids 0, fantômes 0, gulden 0)

### Visibilité conditionnée (ContentHiders)
- [OU] NotTalkedToOrbInOrbRoom — objet: NormalCornerContainer
- [OU] TalkedToOrbInOrbRoom — objet: JazzCornerContainer

### Portails sortants
- AtticRoom -> LongHallway
- AtticRoom -> PlayerSchuur

## Zone : Bakery

Candidats checks : 1 (pickups 1, polaroids 0, fantômes 0, gulden 0)

### Pickups
- Sandwich — `NonEuclidian/Bakery/Hide_Bakery/Bakery_content/sandwich_itemPickup0`

### Portails sortants
- Bakery -> BakeryPassage

## Zone : BakeryPassage

Candidats checks : 0 (pickups 0, polaroids 0, fantômes 0, gulden 0)

### Portails sortants
- BakeryPassage -> Bakery
- BakeryPassage -> Park (extérieur)

## Zone : BehindHouse

Candidats checks : 0 (pickups 0, polaroids 0, fantômes 0, gulden 0)

### Interactions conditionnées
- ShortcutInspect [OU] CreatedShortcut
- FlowerInspect [OU] KeyItemObtained(WateringCan)

### Portails sortants
- BehindHouse -> HooibaalSchuur

## Zone : BigHouseAttic

Candidats checks : 1 (pickups 1, polaroids 0, fantômes 0, gulden 0)

### Pickups
- OUTIL MagicSword — `NonEuclidian/BigHouse_Attic/Hide_BigHouseAttic/cardboardBoxes/attic_cardboardBox0_magicSword/openObject0/content/itemPickup_magicSword0`

### Interactions conditionnées
- BoxInspect [OU] CardboardBoxIsClosed — refs: {"cardboardBox":"Empty"}
- BoxOpen [OU] CarboardBoxIsOpen — refs: {"cardboardBox":"Empty"}
- BoxOpen [OU] CarboardBoxIsOpen — refs: {"cardboardBox":"MagicSword"}
- BoxInspect [OU] CardboardBoxIsClosed — refs: {"cardboardBox":"Balls"}
- BoxOpen [OU] CarboardBoxIsOpen — refs: {"cardboardBox":"Bones"}
- BoxOpen [OU] CarboardBoxIsOpen — refs: {"cardboardBox":"Clothing"}
- BoxInspect [OU] CardboardBoxIsClosed — refs: {"cardboardBox":"Chickens"}
- BoxOpen [OU] CarboardBoxIsOpen — refs: {"cardboardBox":"Balls"}
- BoxInspect [OU] CardboardBoxIsClosed — refs: {"cardboardBox":"Bones"}
- BoxInspect [OU] CardboardBoxIsClosed — refs: {"cardboardBox":"Clothing"}
- BoxOpen [OU] CarboardBoxIsOpen — refs: {"cardboardBox":"Chickens"}
- BoxInspect [OU] CardboardBoxIsClosed — refs: {"cardboardBox":"Mushrooms"}
- BoxInspect [OU] CardboardBoxIsClosed — refs: {"cardboardBox":"Empty"}
- BoxOpen [OU] CarboardBoxIsOpen — refs: {"cardboardBox":"Snacks"}
- BoxOpen [OU] CarboardBoxIsOpen — refs: {"cardboardBox":"Mushrooms"}
- BoxOpen [OU] CarboardBoxIsOpen — refs: {"cardboardBox":"Empty"}
- BoxInspect [OU] CardboardBoxIsClosed — refs: {"cardboardBox":"Snacks"}

### Portails sortants
- BigHouseAttic -> BigHouseOverloop

## Zone : BigHouseFridge

Candidats checks : 1 (pickups 1, polaroids 0, fantômes 0, gulden 0)

### Pickups
- Butter — `NonEuclidian/BigHouseFridge/Hide_BigHouseFridge/butter0`

### Portails sortants
- BigHouseFridge -> BigHouseKitchen

## Zone : BigHouseHallway

Candidats checks : 2 (pickups 2, polaroids 0, fantômes 0, gulden 0)

### Pickups
- Bone — `NonEuclidian/BigHouse_Hallway/Hide_BigHouseHallway/hallwayTable0/bone0`
- ChurchKey — `NonEuclidian/BigHouse_Hallway/Hide_BigHouseHallway/churchKeyContainer/content/churchKey0`

### Portes verrouillées/barrées
- Default — locked:true barred:false — `NonEuclidian/BigHouse_Hallway/Hide_BigHouseHallway/entranceDoor0_inside/bigHouseFrontEntranceDoor0/bigHouseFrontDoorMain0`
- Default — locked:true barred:false — `NonEuclidian/BigHouse_Hallway/Hide_BigHouseHallway/entranceDoor0_inside/bigHouseFrontEntranceDoor0 (1)/bigHouseFrontDoorOther0`

### Interactions conditionnées
- Door [OU] IsBigHouseFrontDoorAndInRedWorld — refs: {"door":{"path":"NonEuclidian/BigHouse_Hallway/Hide_BigHouseHallway/entranceDoor0_inside/bigHouseFrontEntranceDoor0/bigHouseFrontDoorMain0","locked":true,"barred":false,"type":"Default"}}

### Visibilité conditionnée (ContentHiders)
- [OU] InRedWorld — objet: portal_BigHouseKitchenToBigHouseFridge
- [OU] DayIndexIsNot(2) — objet: portal_VeerbootToVeerbootHallway
- [OU] TalkedToOrbInOrbRoom — objet: portal_AtticRoomToLongHallway0
- [OU] NotTriggeredSpotlightJumpscare — objet: hallwayFrame_scare0
- [OU] NotTriggeredSpotlightJumpscare — objet: hallwayFrame_scare0
- [OU] NotTriggeredSpotlightJumpscare — objet: hallwayFrame_scare0
- [OU] InRedWorld — objet: bigHouseHallToStairs_closed0
- [OU] NotInRedWorld — objet: portal_BigHouseKitchenToSnowWorld
- [OU] InGoodEnding — objet: portal_StartGardenToToilet0
- [OU] InRedWorld — objet: portal_BigHouseHallToBijkeuken
- [OU] TriggeredSpotlightJumpscare — objet: hallwayFrame_default0
- [OU] TriggeredSpotlightJumpscare — objet: hallwayFrame_default0
- [OU] NotTriggeredSpotlightJumpscare — objet: hallwayFrame_scare0
- [OU] NotTriggeredSpotlightJumpscare — objet: hallwayFrame_scare0
- [OU] NotInRedWorld — objet: portal_BigHouseHallToBigHouseStairs
- [OU] StartGardenUnder80 — objet: hallwayFrame_default0
- [OU] TriggeredSpotlightJumpscare — objet: hallwayFrame_default0
- [OU] NotInRedWorld — objet: portal_BigHouseHallToKelder
- [OU] TriggeredSpotlightJumpscare — objet: hallwayFrame_default0
- [OU] TriggeredSpotlightJumpscare — objet: hallwayFrame_default0
- [OU] NotTriggeredSpotlightJumpscare — objet: hallwayFrame_scare0
- [OU] TriggeredSpotlightJumpscare — objet: hallwayFrame_default0
- [OU] TriggeredSpotlightJumpscare — objet: hallwayFrame_default0
- [OU] NotTriggeredSpotlightJumpscare — objet: hallwayFrame_scare0
- [OU] NotTalkedToOrbInOrbRoom, DisabledStartGardenToOrbRoomPortal — objet: portal_StartGardenToOrbRoom
- [OU] InRedWorld — objet: entranceDoor0_inside
- [OU] TalkedToOrbInOrbRoom — objet: portal_OrbRoomToLongHallway0
- [OU] NotTriggeredSpotlightJumpscare — objet: hallwayFrame_scare0
- [OU] NotTalkedToOrbInOrbRoom — objet: portal_OrbRoomToStartGarden

### Portails sortants
- BigHouseHallway -> BigHouseKitchen
- BigHouseHallway -> BigHouseOffice
- BigHouseHallway -> BigHouseStairs
- BigHouseHallway -> Bijkeuken
- BigHouseHallway -> Kelder
- BigHouseHallway -> StartGarden (extérieur)

## Zone : BigHouseKitchen

Candidats checks : 1 (pickups 1, polaroids 0, fantômes 0, gulden 0)

### Pickups
- Popcorn — `NonEuclidian/BigHouse_Kitchen/Hide_BigHouseKitchen/popcorn0`

### Interactions conditionnées
- MakePopcorn [OU] PutCornInFryingPan, KeyItemNotObtained(Corn), NotPutButterInFryingPan
- FryingPanButterInspect [OU] NotPutButterInFryingPan, KeyItemObtained(Corn)
- PutButterInFryingPan [OU] PutButterInFryingPan, KeyItemNotObtained(Butter)
- FryingPanEmptyInspect [OU] PutButterInFryingPan, KeyItemObtained(Butter)

### Visibilité conditionnée (ContentHiders)
- [OU] NotMadePopcorn, KeyItemObtained(Popcorn) — objet: popcorn0
- [OU] NotPutButterInFryingPan, MadePopcorn — objet: butterPuddle0
- [OU] NotPutCornInFryingPan, MadePopcorn — objet: cornPiecesHolder0

### Portails sortants
- BigHouseKitchen -> BigHouseFridge
- BigHouseKitchen -> BigHouseHallway
- BigHouseKitchen -> SnowWorld

## Zone : BigHouseOffice

Candidats checks : 2 (pickups 2, polaroids 0, fantômes 0, gulden 0)

### Pickups
- OldKey — `Main/Interactions/oldKey0`
- StrangeKey — `Main/Interactions/strangeKey0_old`

### Portes verrouillées/barrées
- Default — locked:true barred:false — `*** PORTALS ***/portal_BigHouseOfficeToVoid0/bigHouseOfficeDoor0/bigHouseOfficeDoor0/door0`

### Interactions conditionnées
- InsertDisc [OU] ComputerNotOn, InsertedDisc, KeyItemNotObtained(Cd) — refs: {"computer":true}
- OwnerTalk [OU] NotInRedWorld — refs: {"owner":true}

### Portails sortants
- BigHouseOffice -> BigHouseHallway
- BigHouseOffice -> GlassHouse
- BigHouseOffice -> Void

## Zone : BigHouseOverloop

Candidats checks : 0 (pickups 0, polaroids 0, fantômes 0, gulden 0)

### Portes verrouillées/barrées
- Default — locked:true barred:false — `NonEuclidian/BigHouse_Overloop/Hide_BigHouseOverloop/bigHouseAtticDoor0/door0`

### Portails sortants
- BigHouseOverloop -> BigHouseAttic
- BigHouseOverloop -> BigHouseStairs
- BigHouseOverloop -> BottleRoom

## Zone : BigHouseStairs

Candidats checks : 0 (pickups 0, polaroids 0, fantômes 0, gulden 0)

### Portails sortants
- BigHouseStairs -> BigHouseHallway
- BigHouseStairs -> BigHouseOverloop

## Zone : Bijkeuken

Candidats checks : 1 (pickups 1, polaroids 0, fantômes 0, gulden 0)

### Pickups
- GardenKey — `Main/Interactions/gardenKey0`

### Portes verrouillées/barrées
- Default — locked:true barred:false — `Main/Areas/StartGardenArea/Container/Hide_Bijkeuken/bijkeuken0/bijkeuken_regularDoor0/door0`

### Portails sortants
- Bijkeuken -> BigHouseHallway

## Zone : BottleRoom

Candidats checks : 1 (pickups 1, polaroids 0, fantômes 0, gulden 0)

### Pickups
- SoulFragment2 — `NonEuclidian/BigHouse_BottleRoom/Hide_BottleRoom/kasten/bigHouse_bottleRoom_kast0 (4)/content/bigGlassBottle0 (5)/soulFragment1`

### Interactions conditionnées
- Item [OU] NotDestroyedSoulBottle — refs: {"itemPickup":"NonEuclidian/BigHouse_BottleRoom/Hide_BottleRoom/kasten/bigHouse_bottleRoom_kast0 (4)/content/bigGlassBottle0 (5)/soulFragment1"}

### Portails sortants
- BottleRoom -> BigHouseOverloop

## Zone : Bunker

Candidats checks : 3 (pickups 1, polaroids 0, fantômes 1, gulden 1)

### Pickups
- OUTIL Trowel — `Main/Interactions/trowel0`

### Gulden posés
- `Main/Gulden/gulden0 (3)`

### Fantômes
- pos(-90.3, 83.63) — `*** GHOSTS ***/ghost0_bunker`

### Interactions conditionnées
- DeadGardenerInspect [OU] KeyItemObtained(SeveredHand), ReturnedSeveredHand
- ReturnSeveredHand [OU] KeyItemNotObtained(SeveredHand), ReturnedSeveredHand
- BunkerDoorInspect [OU] BunkerDoorNotClosed, BunkerDoorDestroyed

### Visibilité conditionnée (ContentHiders)
- [OU] SeveredHandNotReturned — objet: returnedHand0

## Zone : Church (extérieur)

Candidats checks : 7 (pickups 2, polaroids 4, fantômes 0, gulden 1)

### Pickups
- Doorknob — `Main/Interactions/missingDoorknob0_branchHole`
- Doorknob — `Main/Interactions/missingDoorknob0_grass`

### Gulden posés
- `Main/Gulden/MolehillGuldenContainer0/gulden0 (6)`

### Polaroids
- RedDoor — `Main/Polaroids/polaroid_redDoor0`
- Tent — `Main/Polaroids/polaroid_tent0`
- ChurchOutsideDoor — `Main/Polaroids/polaroid_churchOutsideDoor0`
- BoatPaddle — `Main/Polaroids/polaroid_boatPaddle0`

### Portes verrouillées/barrées
- Gate — locked:true barred:false — `Main/Interactions/churchGate0/door0`

### Interactions conditionnées
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- MissingDoorknobDoorRepair [OU] KeyItemNotObtained(Doorknob), RepairedMissingDoorknobDoor
- PutPrettyFlowerInVase [OU] KeyItemNotObtained(PrettyFlower), PutPrettyFlowerInVase
- Door [OU] NotRepairedMissingDoorknobDoor — refs: {"door":{"path":"*** PORTALS ***/portal_ChurchToPillarSpace0/missingDoorknobDoor0/churchToPillarSpaceDoor0","locked":false,"barred":false,"type":"Default"}}
- WormHillInspect [OU] HasWormAndCanBeUsed
- BoatTravelToPark [OU] KeyItemNotObtained(Paddle)
- WellInspect [ET] KeyItemObtained(Coin), NotThrewBlueCoinInWell
- BoatInspect [OU] KeyItemObtained(Paddle)
- Item [OU] KeyItemObtained(Doorknob) — refs: {"itemPickup":"Main/Interactions/missingDoorknob0_branchHole"}
- Item [OU] KeyItemObtained(Doorknob) — refs: {"itemPickup":"Main/Interactions/missingDoorknob0_grass"}
- PrettyVaseInspect [OU] PutPrettyFlowerInVase, KeyItemObtained(PrettyFlower)
- ChurchDoor [OU] ChurchDoorOpen
- MissingDoorknobDoorInspect [OU] KeyItemObtained(Doorknob), RepairedMissingDoorknobDoor
- WormReturn [OU] WormReturned, NotHasWormAndCanBeUsed
- WellThrowInBlueCoin [OU] KeyItemNotObtained(Coin), ThrewBlueCoinInWell
- DoorknobBranchSearch [OU] DoorknobBranchHoleSearched
- Wait1Hour [OU] ObjectActive — refs: {"objectActiveRef":"manBench0"}
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- MolehillInspect [OU] KeyItemObtained(Trowel)
- MolehillInspect [OU] KeyItemObtained(Trowel)
- MolehillInspect [OU] KeyItemObtained(Trowel)
- MolehillInspect [OU] KeyItemObtained(Trowel)
- MolehillInspect [OU] KeyItemObtained(Trowel)
- MolehillInspect [OU] KeyItemObtained(Trowel)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)

### Visibilité conditionnée (ContentHiders)
- [OU] NotInRedWorld — objet: OwnerContainer
- [OU] ComputerNotOn, NotInsertedDisc — objet: portal_GasStationOfficeToVoid0
- [OU] FenceDestroyed — objet: fenceHole0
- [OU] PlayerInBigHouseOffice — objet: ChuchTreesNotVisibleInsideHouse
- [OU] NotKilledMagpieWithShears — objet: magpieDeadWithoutHead0
- [OU] StartHouseNotEntered, KeyItemObtained(TallIdol) — objet: portal_HedgeMazeToStartGarden
- [OU] AtticRoomNotActive — objet: AtticRoom
- [OU] SmallChapelNotShowing — objet: portal_OutsideRedChapelMagicPond
- [OU] DayIndexIsNot(2) — objet: OutsideVillage_OrbTalkedTo_Container
- [OU] PlayerInBigHouseOffice — objet: ChurchDetailsContainer
- [OU] GnomeDestroyed — objet: gardengnome_NotDestroyed
- [OU] DayIndexIsNot(2), RedCarTrunkNotOpened, RedCarLockNotSmashed — objet: gasStationLady0_dead
- [OU] NotInTimeWindow(20h-24h), DayIndexIsNot(1) — objet: crazyWoman0
- [OU] ParkNotUnlockedHooibaalGarden — objet: portal_HooiGardenToPark0
- [OU] NotDestroyedAllJumpscareGnomes, InRedWorld, InGoodEnding — objet: portal_StartGardenToRoundHallway0
- [OU] NotGaveKidCymbals — objet: CymbalsKid_active
- [OU] DayIndexIsNot(1), NotInTimeWindow(17h-21h) — objet: patatkarOpen0
- [OU] ObjectIsActive, NotMaintainedGardenArea — objet: portal_StartGardenToForestPassage
- [OU] ObjectIsActive — objet: itemPickup_magicSword0_overworld
- [OU] GaveKidTriangle — objet: TriangleKid_inactive
- [OU] NotTalkedToOrbInOrbRoom — objet: polaroid_gnomeIdol0
- [OU] KilledMagpieWithShears, KilledMagpieWithHammer, NotUnlockedMagpie, DestroyedMagpie — objet: magpieAlive0
- [OU] NotInTimeWindow(8h-12h), DayIndexIsNot(1), PlayerInAppleSpace, PlayerInGasStationOfficeAndComputerOn — objet: oldLady0_gasStation
- [OU] NotInTimeWindow(8h-9h) — objet: paddle0
- [OU] NotLimitedContent — objet: LimitedContent
- [OU] FenceNotDestroyed — objet: FenceHoleDestroyed
- [OU] RepairedMissingDoorknobDoor — objet: MissingDoorknobDoorUnrepairedContent
- [OU] DayIndexIsNot(2), IsFinalDay — objet: ghost0_redCar0
- [OU] NotTalkedToOrbInOrbRoom — objet: polaroid_crypt0
- [OU] KeyItemObtained(Hammer) — objet: hammer0_car
- [OU] PlayerInBigHouseOffice — objet: OutsideVillageTreesContainer
- [OU] ComputerNotOn, InsertedDisc — objet: portal_BigHouseOfficeToGlasshouse
- [OU] ComputerOn, InsertedDisc — objet: portal_BigHouseOfficeToHall
- [OU] NotInRedWorld — objet: RedWorldObjects
- [OU] NotDestroyedAllJumpscareGnomes — objet: portal_ParkToRoundHallway0
- [OU] PylonNotFlipped — objet: pylon0_gulden_Flipped
- [ET] ComputerOn, InsertedDisc — objet: GasStationInteriorContainer
- [OU] NotMaintainedParkArea — objet: portal_ParkToBakeryPassage
- [OU] DayIndexIs(1), GasStationDoorNotDestroyed — objet: gasStationDoorDestroyed0
- [OU] BraamstruikNotDestroyed — objet: braamstruik_ashpiles
- [OU] ComputerNotOn, NotInsertedDisc — objet: portal_VoidToGasStationOffice0
- [OU] ObjectIsInactive, PlayerIsTooClose — objet: wormActivatorRef0
- [OU] NotGaveKidTrumpet — objet: TrumpetKid_active
- [OU] DayIndexIsNot(1), GasStationDoorDestroyed — objet: gasStationDoorOpen0
- [OU] PylonFlipped — objet: pylon0_gulden_NotFlipped
- [OU] KeyItemObtained(Doorknob), NotSearchedDoorknobBranchHole — objet: missingDoorknobContent
- [OU] PlayerInBigHouseOffice — objet: RoadsContainer
- [OU] KeyItemObtained(OfficeKey) — objet: officeKey0
- [OU] BranchHoleNotSearched — objet: BranchHoleSearched
- [OU] NotUnlockedWorm, KeyItemObtained(Worm) — objet: wormLine
- [OU] NotInTimeWindow(21h-24h), DayIndexIsNot(1) — objet: oldLady0_park
- [OU] NotLitAllHooiGardenCandles — objet: portal_HooiSchuurToBehindHouse0
- [OU] StartHouseNotEntered, KeyItemObtained(TallIdol), InRedWorld, InGoodEnding — objet: portal_StartGardenToHedgeMazeA
- [OU] NotInTimeWindow(17h-22h), DayIndexIsNot(2) — objet: hangJong1_park
- [OU] PlayerInBigHouseOffice — objet: ChurchTowerContainer
- [OU] NotInGoodEnding — objet: BlackFenceBackGoodEnding0
- [OU] AppleNotPlaced — objet: appleObtained0
- [OU] KeyItemObtained(Doorknob), NotSpawnedMissingDoorknob — objet: missingDoorknobContent
- [OU] KeyItemObtained(Hammer) — objet: hammer0_hedgeMaze
- [OU] CreatedShortcut — objet: hedgeStatic_behindHouse0_closed
- [OU] NotGaveKidTriangle — objet: TriangleKid_active
- [OU] KeyItemNotObtained(TallIdol), NotDestroyedTallMan — objet: portal_HedgeMazeEndToStartGarden
- [OU] PlayerInBigHouseOffice — objet: LetterSignsContainer
- [OU] NotKilledMagpieWithHammer — objet: magpieDeadWithHead0
- [OU] ComputerNotOn, NotInsertedDisc — objet: portal_BigHouseOfficeToVoid0
- [OU] PlayerNotInPillarSpace, HumanMovementHidden, DestroyedTallMan, SawAndHidTallManPillarSpace — objet: tallManPillarSpace0
- [OU] ObjectIsActive — objet: MolehillGuldenContainer0
- [OU] InRedWorld — objet: RedBookContainer
- [OU] DayIndexIsNot(2) — objet: tentSceneContainer
- [OU] AtticRoomNotActive — objet: portal_AtticRoomToStartGarden0
- [OU] SmallChapelNotShowing — objet: Hide_smallChapel_show0
- [OU] FenceDestroyed — objet: FenceHoleNotDestroyed
- [OU] ObjectIsActive — objet: patatkarClosed0
- [OU] NotUnlockedMagpie — objet: nestLetter0
- [OU] FenceNotDestroyed — objet: fenceHole1
- [OU] NotActivatedEndDemon — objet: EndDemon
- [OU] NotCreatedShortcut — objet: hedgeStatic_behindHouse0_open
- [OU] BraamstruikDestroyed — objet: BraamstruikContent0
- [OU] NotInTimeWindow(10h-18h), DayIndexIsNot(1) — objet: manBench0
- [OU] RottenPlankNotPulled — objet: RottenPlankPulled
- [OU] NotDestroyedMagpie — objet: magpieDeadByWorm0
- [OU] NotPutPrettyFlowerInVase — objet: PrettyFlower_InVase
- [OU] InGoodEnding — objet: ToiletBuildingOutside
- [OU] PlayerInBigHouseOffice, InRedWorld — objet: bunkerExtraContainer
- [OU] ParkNotUnlockedHooibaalGarden — objet: portal_ParkToHooiGarden0
- [OU] DayIndexIs(1), GasStationDoorDestroyed — objet: gasStationDoorClosed0
- [OU] NotInTimeWindow(14h-20h), DayIndexIsNot(1) — objet: WomanTankingContainer
- [OU] OldCarTrunkNotOpened, KeyItemObtained(Hammer) — objet: hammer0_car
- [OU] DayIndexIsNot(1) — objet: gasStationLady0
- [OU] KeyItemObtained(OfficeKey) — objet: officeKey0_shop
- [OU] NotRepairedMissingDoorknobDoor — objet: MissingDoorknobDoorRepairedContent
- [OU] NotDestroyedAllJumpscareGnomes — objet: portal_GnomeForestToRoundHallway0
- [OU] GnomeNotDestroyed — objet: gardengnome_Destroyed
- [OU] KeyItemObtained(Lighter) — objet: lighter0_park0
- [OU] BoatTravelDestinationIsNotPark, InRedWorld — objet: boatToChurch0
- [OU] NotInTimeWindow(12h-18h), DayIndexIsNot(1) — objet: oldLady0_gordijn
- [OU] ComputerNotOn, InsertedDisc — objet: portal_GasStationOfficeToAppleSpace0
- [OU] NotInGoodEnding — objet: GoodEndingContainer
- [OU] PlayerNotInBunker, HumanMovementHidden, DestroyedTallMan, SawAndHidTallManOutsideBunker — objet: tallManOutsideBunker0
- [OU] InGoodEnding — objet: BlackFenceBackDefault0
- [OU] NotLitAllHooiGardenCandles — objet: portal_HooiSchuurToHooiGarden0
- [OU] NotActivatedRedChapel — objet: fishermanVanished0
- [OU] AtticRoomNotActive — objet: portal_StartGardenToAtticRoom0
- [OU] ObjectIsActive — objet: GuldenPotContainer0
- [OU] InRedWorld — objet: bijkeuken0
- [OU] NotInTimeWindow(17h-22h), DayIndexIsNot(2) — objet: hangJong0_park
- [OU] PlayerInBigHouseOffice — objet: OuterExtraContainer
- [OU] NotInTimeWindow(22h-24h), DayIndexIsNot(2) — objet: oldLady0_grave
- [ET] NotKilledMagpieWithShears, NotKilledMagpieWithHammer — objet: MagpieBloodstains
- [OU] HighBridgeNotDestroyed, BusIsNotGone — objet: polaroid_van0
- [OU] ObjectIsActive — objet: goldfishDead0
- [OU] ObjectIsActive, KeyItemObtained(Lighter) — objet: lighter0_molehill0
- [OU] NotInTimeWindow(8h-10h), DayIndexIsNot(1) — objet: hangJong0_bus
- [OU] SmallChapelShowing — objet: Hide_smallChapel_hide0
- [OU] NotUnlockedMagpie — objet: polaroid_backGardenFence0
- [OU] PlayerInBigHouseOffice — objet: RoadTreesContainer
- [OU] PlayerNotInBijkeuken, HumanMovementHidden, NotTriggeredTallManBijkeuken, DestroyedTallMan, SawAndHidTallManBijkeuken — objet: tallManBijkeuken0
- [OU] NotUnlockedBijkeukenShortcut, IsFinalDay — objet: portal_BijkeukenToBigHouseHall
- [OU] DayIndexIs(2) — objet: OutsideVillage_OrbNotTalkedTo_Container
- [OU] HighBridgeDestroyed — objet: plankOriginal0
- [OU] NotGaveWormToFisherman — objet: wormFisherman0
- [OU] DayIndexIsNot(2), NotInTimeWindow(22h-0h), BraamstruikNotDestroyed — objet: scooterCrash0
- [OU] HighBridgeNotDestroyed — objet: bridgeRepair0
- [OU] ObjectIsActive — objet: WomanNotTankingObjects
- [ET] UnlockedBijkeukenShortcut, IsNotFinalDay — objet: bijkeuken_regularDoor0
- [OU] HangjongerenNotAppeared — objet: pizzaBox0
- [OU] BraamstruikDestroyed, EnteredRoundHallway, InRedWorld, InGoodEnding — objet: BrugBlokkade0
- [OU] NotLitAllHooiGardenCandles — objet: portal_BehindHouseToHooiSchuur0
- [OU] InRedWorld — objet: churchGateBack_blokkadeHek0
- [OU] ObjectIsActive, KeyItemObtained(Lighter), SecondObjectIsNotActive — objet: lighter0_car0
- [OU] ComputerNotOn — objet: portal_AppleSpaceToGasStationOffice0
- [OU] NotTriggeredGnomeJumpscare — objet: GnomeJumpscareContainer
- [OU] RottenPlankPulled — objet: RottenPlankNotPulled
- [OU] DestroyedTallMan — objet: TallMan_boomgaard0
- [OU] PlayerInBigHouseOffice — objet: Trimballs
- [OU] GaveKidTrumpet — objet: TrumpetKid_inactive
- [OU] BoatTravelDestinationIsNotChurch, InRedWorld — objet: boatToPark0
- [OU] NotInTimeWindow(15h-22h), DayIndexIsNot(1) — objet: hangJong0_hanghok
- [OU] PlayerNotInPlayerSchuur, HumanMovementHidden, NotTriggeredTallManPlayerSchuur, DestroyedTallMan, SawAndHidTallManOutsideWindow — objet: tallManOutsideWindow0
- [OU] NotInRedWorld — objet: WormHillContainer
- [OU] ActivatedRedChapel, NotInTimeWindow(4h-16h) — objet: fisherMan0
- [OU] InRedWorld — objet: backGarden
- [OU] NotMaintainedChurchArea — objet: portal_ChurchToHillPassage
- [OU] StartHouseNotEntered, KeyItemNotObtained(TallIdol), InRedWorld, InGoodEnding — objet: portal_StartGardenToHedgeMazeB
- [OU] PlayerInBigHouseOffice — objet: RoadPropsContainer
- [OU] NotWormReturned — objet: demon_wormWriggle0
- [OU] PlayerInBigHouseOffice — objet: HedgesNotVisibleFromInsideHouse
- [OU] NotInGoodEnding — objet: ToiletBuildingOutside_GoodEnding
- [OU] NotCompletedToiletPaperSequence — objet: portal_ToiletToTentInside
- [OU] NotFoundGoodEnding — objet: ParkSwordContainer
- [OU] GaveKidCymbals — objet: CymbalsKid_inactive
- [OU] NotLitAllHooiGardenCandles — objet: portal_HooiGardenToHooiSchuur0
- [OU] NotInTimeWindow(15h-22h), DayIndexIsNot(1) — objet: hangJong1_hanghok

### Portails sortants
- Church (extérieur) -> HillPassage
- Church (extérieur) -> PillarSpace

## Zone : ChurchBigHall

Candidats checks : 1 (pickups 0, polaroids 1, fantômes 0, gulden 0)

### Polaroids
- FlowerDoor — `NonEuclidian/ChurchBigHall/Hide_ChurchBigHall/polaroid_flowerDoor0`

### Interactions conditionnées
- CandleLight [OU] KeyItemNotObtained(Lighter), CandleLit
- AltarInspect [OU] InsertedFlowerGem, KeyItemObtained(FlowerGem)
- CandleStop [OU] CandleNotLit
- CandleInspect [OU] CandleLit, KeyItemObtained(Lighter)
- CandleLight [OU] KeyItemNotObtained(Lighter), CandleLit
- CandleLight [OU] KeyItemNotObtained(Lighter), CandleLit
- CandleInspect [OU] CandleLit, KeyItemObtained(Lighter)
- CandleInspect [OU] CandleLit, KeyItemObtained(Lighter)
- CandleStop [OU] CandleNotLit
- CandleStop [OU] CandleNotLit
- FlowerGemInsert [OU] KeyItemNotObtained(FlowerGem), InsertedFlowerGem
- CandleLight [OU] KeyItemNotObtained(Lighter), CandleLit
- CandleLight [OU] KeyItemNotObtained(Lighter), CandleLit
- CandleLight [OU] KeyItemNotObtained(Lighter), CandleLit
- CandleLight [OU] KeyItemNotObtained(Lighter), CandleLit
- CandleLight [OU] KeyItemNotObtained(Lighter), CandleLit
- CandleLight [OU] KeyItemNotObtained(Lighter), CandleLit
- CandleStop [OU] CandleNotLit
- CandleStop [OU] CandleNotLit
- CandleStop [OU] CandleNotLit
- CandleStop [OU] CandleNotLit
- CandleStop [OU] CandleNotLit
- CandleStop [OU] CandleNotLit
- CandleInspect [OU] CandleLit, KeyItemObtained(Lighter)
- CandleInspect [OU] CandleLit, KeyItemObtained(Lighter)
- CandleInspect [OU] CandleLit, KeyItemObtained(Lighter)
- CandleInspect [OU] CandleLit, KeyItemObtained(Lighter)
- CandleInspect [OU] CandleLit, KeyItemObtained(Lighter)
- CandleInspect [OU] CandleLit, KeyItemObtained(Lighter)

### Visibilité conditionnée (ContentHiders)
- [OU] NotInRedWorld — objet: Church_SkeletonContainer
- [OU] NotInsertedFlowerGem — objet: flowerGem_inserted0
- [OU] NotRevealedGrandmaInChurch — objet: GrandmaChurchContainer

### Portails sortants
- ChurchBigHall -> ChurchHallway
- ChurchBigHall -> CryptStairs

## Zone : ChurchHallway

Candidats checks : 2 (pickups 1, polaroids 1, fantômes 0, gulden 0)

### Pickups
- OldPlank — `Main/Interactions/item_oldPlank0`

### Polaroids
- ChurchKey — `Main/Polaroids/polaroid_churchKey0`

### Portes verrouillées/barrées
- Default — locked:true barred:false — `*** PORTALS ***/portal_ChurchHallwayToChurchBigHall0/churchHallwayDoor0/door0`

### Portails sortants
- ChurchHallway -> ChurchBigHall

## Zone : CornField

Candidats checks : 1 (pickups 1, polaroids 0, fantômes 0, gulden 0)

### Pickups
- Corn — `Main/Areas/Road/Container/Hide_Corn/corn0`

## Zone : CornFieldCenter

Candidats checks : 2 (pickups 1, polaroids 1, fantômes 0, gulden 0)

### Pickups
- ToiletPaper — `Main/Interactions/toiletPaper0`

### Polaroids
- ToiletStall — `Main/Polaroids/polaroid_toiletStall0`

## Zone : Crypt

Candidats checks : 0 (pickups 0, polaroids 0, fantômes 0, gulden 0)

### Interactions conditionnées
- GnomeIdolReturn [OU] ReturnedGnomeIdol, KeyItemNotObtained(GnomeIdol)
- ShortDemonStatueInspect [OU] ObtainedShortIdolButNotUsed
- TallDemonStatueInspect [OU] ObtainedTallIdolButNotUsed
- GnomeStatueInspect [OU] ObtainedGnomeIdolButNotUsed
- SmallManIdolReturn [OU] ReturnedShortIdol, KeyItemNotObtained(ShortIdol)
- InvisibleManIdolReturn [OU] ReturnedShyIdol, KeyItemNotObtained(ShyIdol)
- ShyDemonStatueInspect [OU] ObtainedShyIdolButNotUsed
- TallManIdolReturn [OU] ReturnedTallIdol, KeyItemNotObtained(TallIdol)

### Visibilité conditionnée (ContentHiders)
- [OU] NotReturnedGnomeIdol — objet: gnomeIdol0_returned
- [OU] NotReturnedTallIdol — objet: tallIdol0_returned
- [OU] NotReturnedShyIdol — objet: shyIdol0_returned
- [OU] NotReturnedShortIdol — objet: shortIdol0_returned

### Portails sortants
- Crypt -> CryptStairs

## Zone : CryptStairs

Candidats checks : 0 (pickups 0, polaroids 0, fantômes 0, gulden 0)

### Portails sortants
- CryptStairs -> ChurchBigHall
- CryptStairs -> Crypt

## Zone : Ferry

Candidats checks : 2 (pickups 1, polaroids 0, fantômes 0, gulden 1)

### Pickups
- ShortIdol — `NonEuclidian/Ferry/Hide_Ferry/SmallDemonScene/shortIdol0`

### Gulden posés
- `NonEuclidian/Ferry/Hide_Ferry/gulden_ferry0`

### Interactions conditionnées
- StrangeLookingKidInspect [OU] GaveToyToFerryKid
- FerryKidHappyInspect [OU] NotGaveToyToFerryKid
- GiveToyToFerryKid [OU] GaveToyToFerryKid, KeyItemNotObtained(ToyBoat)
- FerryKidSadInspect [OU] GaveToyToFerryKid, KeyItemObtained(ToyBoat)

### Visibilité conditionnée (ContentHiders)
- [OU] NotGaveToyToFerryKid — objet: happyFace
- [OU] DestroyedSmallDemon — objet: smallDemon0_ferry0
- [OU] NotGaveToyToFerryKid — objet: happyKid_content
- [OU] NotGaveToyToFerryKid — objet: angryFace
- [OU] GaveToyToFerryKid — objet: sadFace
- [OU] GaveToyToFerryKid — objet: normalFace
- [OU] NotDestroyedSmallDemon — objet: shortIdol0

### Portails sortants
- Ferry -> VeerbootHallway

## Zone : Forest

Candidats checks : 0 (pickups 0, polaroids 0, fantômes 0, gulden 0)

### Interactions conditionnées
- InteractionPromptOnly [OU] KeyItemObtained(Sandwich)
- InteractionPromptOnly [OU] PlayerHasBlanketAndNotUsed, PlayerHasSandwichAndNotUsed
- PicnicPlaceSandwich [OU] KeyItemNotObtained(Sandwich), PicnicPlacedSandwich
- GoPicnic [OU] NotPicnicPlacedSandwich
- PicnicPlaceBlanket [OU] KeyItemNotObtained(Blanket), PicnicPlacedBlanket

### Visibilité conditionnée (ContentHiders)
- [OU] NotPicnicPlacedSandwich, NotPicnicPlacedBlanket — objet: Picnic_CompleteContainer
- [OU] PicnicPlacedSandwich, NotPicnicPlacedBlanket — objet: Picnic_BlanketOnlyContainer
- [OU] PicnicPlacedBlanket — objet: Picnic_EmptyContainer

### Portails sortants
- Forest -> ForestPassage

## Zone : ForestPassage

Candidats checks : 0 (pickups 0, polaroids 0, fantômes 0, gulden 0)

### Portails sortants
- ForestPassage -> Forest
- ForestPassage -> StartGarden (extérieur)

## Zone : GasStation

Candidats checks : 3 (pickups 2, polaroids 1, fantômes 0, gulden 0)

### Pickups
- Lighter — BOUTIQUE 5 gulden — `Main/Interactions/LighterGasStationContainer/lighter0_gasStation0`
- OfficeKey — `Main/Interactions/officeKey0`

### Polaroids
- VoidSkeleton — `Main/Polaroids/polaroid_skeletonVoid0`

### Portes verrouillées/barrées
- Default — locked:true barred:false — `Main/Areas/Road/Container/GasStation/GasStationExtraHiderContainer/GasStationDoors/gasStationDoorClosed0/door0`

### Visibilité conditionnée (ContentHiders)
- [OU] DayIndexIsNot(1) — objet: LighterGasStationContainer

## Zone : GasStationOffice

Candidats checks : 1 (pickups 0, polaroids 0, fantômes 0, gulden 1)

### Gulden posés
- `Main/Gulden/gulden0 (4)`

### Portes verrouillées/barrées
- Default — locked:true barred:false — `Main/Areas/Road/Container/GasStation/GasStationExtraHiderContainer/container/Hide_gasStation/smallDoor1 (1)/door0`

### Interactions conditionnées
- Read [OU] LimitedContent
- InsertDisc [OU] ComputerNotOn, InsertedDisc, KeyItemNotObtained(Cd) — refs: {"computer":true}

### Portails sortants
- GasStationOffice -> AppleSpace
- GasStationOffice -> Void

## Zone : GlassHouse

Candidats checks : 1 (pickups 1, polaroids 0, fantômes 0, gulden 0)

### Pickups
- SpecialSeed — `NonEuclidian/GlassHouse/Hide_GlassHouse/GlassHouseExtraHiderContainer/prettySeed0`

### Visibilité conditionnée (ContentHiders)
- [OU] ComputerNotOn, InsertedDisc — objet: GlassHouseExtraHiderContainer

### Portails sortants
- GlassHouse -> BigHouseOffice

## Zone : GnomeForest

Candidats checks : 5 (pickups 3, polaroids 1, fantômes 1, gulden 0)

### Pickups
- KidTrumpet — `NonEuclidian/GnomeForest/Hide_GnomeForest/kidTrumpet0`
- Bone — `NonEuclidian/GnomeForest/Hide_GnomeForest/skeletonBone_gnomeForest0/bone_skeleton_gnomeForest0`
- GnomeIdol — `Main/Interactions/gnomeIdol0`

### Polaroids
- GardenGnomes — `NonEuclidian/GnomeForest/Hide_GnomeForest/Mushrooms/bigMushroom0/bigMushroom0/polaroid_gardenGnomes0`

### Fantômes
- pos(190.65, 1003.9) — `*** GHOSTS ***/ghost0_gnomeForest`

### Interactions conditionnées
- MolehillInspect [OU] KeyItemObtained(Trowel)

### Visibilité conditionnée (ContentHiders)
- [OU] ObjectIsActive — objet: kidTrumpet0
- [OU] KeyItemNotObtained(GnomeIdol) — objet: gardenGnome_giant0

### Portails sortants
- GnomeForest -> RoundHallway

## Zone : HedgeMaze

Candidats checks : 2 (pickups 1, polaroids 1, fantômes 0, gulden 0)

### Pickups
- KidCymbals — `NonEuclidian/HedgeMaze/Hide_HedgeMaze/kidCymbals0`

### Polaroids
- Compass — `NonEuclidian/HedgeMaze/Hide_HedgeMaze/polaroid_compass0`

### Interactions conditionnées
- DeepGapPlacePlank [OU] PlacedPlankDeepGap, DontHaveAnyPlank, AllPlanksUsed
- GapTooDeep [OU] HasAPlankToUse, PlacedPlankDeepGap

### Visibilité conditionnée (ContentHiders)
- [OU] HedgeMazeNotDone — objet: Hedge_magicPath_done
- [OU] NotDeepGapPlacePlank — objet: GapPlankVisualsContainer
- [OU] BadWeather, DeepGapPlacePlank — objet: GapClosedCollidersContainer
- [OU] DestroyedTallMan — objet: HedgeMaze_endRoomOpen0
- [OU] HedgeMazeNotActive — objet: Hedge_magicPath_active
- [OU] HedgeMazeNotInactive — objet: Hedge_magicPath_inactive
- [OU] NotClosedGapWithOldPlank — objet: gapPlank_old0
- [OU] InspectedStrangeSymbol — objet: hedgeMaze_path_room_open
- [OU] NotDestroyedTallMan — objet: tallIdol0_hedgeMaze0
- [OU] NotDestroyedTallMan — objet: HedgeMaze_endRoomClosed0
- [OU] NotBadWeather, NotDeepGapPlacePlank — objet: GapPlankAndRainingCollidersContainer
- [OU] NotBadWeather — objet: GapRainingVisualsContainer
- [OU] BadWeather, NotDeepGapPlacePlank — objet: GapPlankOnlyCollidersContainer
- [OU] NotClosedGapWithPlank — objet: gapPlank_default0
- [OU] NotInspectedStrangeSymbol — objet: hedgeMaze_path_room_closed
- [OU] NotBadWeather, DeepGapPlacePlank — objet: GapRainingOnlyCollidersContainer
- [OU] KeyItemObtained(Compass) — objet: HedgeMaze_NotObtainedCompass
- [OU] KeyItemNotObtained(Compass) — objet: HedgeMaze_ObtainedCompass

### Portails sortants
- HedgeMaze -> StartGarden (extérieur)

## Zone : HedgeMazeInner

Candidats checks : 2 (pickups 2, polaroids 0, fantômes 0, gulden 0)

### Pickups
- TallIdol — `NonEuclidian/HedgeMaze/Hide_HedgeMaze/HedgeMaze_paths/HedgeMaze_ObtainedCompass/HedgeMaze_endRoomAlways0/tallManHedgeMazeChair0/tallIdol0_hedgeMaze0`
- OUTIL Hammer — `Main/Interactions/hammer0_hedgeMaze`

### Interactions conditionnées
- StrangeSymbolInspect [OU] InspectedStrangeSymbol
- HedgeMazeTallManInspect [OU] DestroyedTallDemon

### Portails sortants
- HedgeMazeInner -> StartGarden (extérieur)

## Zone : Hill

Candidats checks : 1 (pickups 1, polaroids 0, fantômes 0, gulden 0)

### Pickups
- Blanket — `NonEuclidian/Hill/Hide_Hill/Hill_content/Waslijn/blanket_object0`

### Portails sortants
- Hill -> HillPassage

## Zone : HillPassage

Candidats checks : 0 (pickups 0, polaroids 0, fantômes 0, gulden 0)

### Portails sortants
- HillPassage -> Church (extérieur)
- HillPassage -> Hill

## Zone : HooibaalGarden

Candidats checks : 1 (pickups 0, polaroids 1, fantômes 0, gulden 0)

### Polaroids
- HooibaalTuin — `NonEuclidian/HooibaalGarden/Hide_HooibaalGarden/polaroid_hooibaalTuin0`

### Interactions conditionnées
- CandleLight [OU] KeyItemNotObtained(Lighter), CandleLit
- CandleLight [OU] KeyItemNotObtained(Lighter), CandleLit
- CandleLight [OU] KeyItemNotObtained(Lighter), CandleLit
- CandleLight [OU] KeyItemNotObtained(Lighter), CandleLit
- CandleLight [OU] KeyItemNotObtained(Lighter), CandleLit
- CandleStop [OU] CandleNotLit
- CandleStop [OU] CandleNotLit
- CandleStop [OU] CandleNotLit
- CandleStop [OU] CandleNotLit
- CandleInspect [OU] CandleLit, KeyItemObtained(Lighter)
- CandleStop [OU] CandleNotLit
- CandleInspect [OU] CandleLit, KeyItemObtained(Lighter)
- CandleInspect [OU] CandleLit, KeyItemObtained(Lighter)
- CandleInspect [OU] CandleLit, KeyItemObtained(Lighter)
- CandleInspect [OU] CandleLit, KeyItemObtained(Lighter)

### Portails sortants
- HooibaalGarden -> HooibaalSchuur
- HooibaalGarden -> Park (extérieur)

## Zone : HooibaalSchuur

Candidats checks : 5 (pickups 4, polaroids 1, fantômes 0, gulden 0)

### Pickups
- OfficeKey — BOUTIQUE 2 gulden (gamin) — `Main/Interactions/officeKey0_shop`
- Compass — BOUTIQUE 4 gulden (gamin) — `Main/Interactions/compass0`
- Cd — BOUTIQUE 5 gulden (gamin) — `Main/Interactions/cd0`
- Medal — BOUTIQUE 10 gulden (gamin) — `Main/Interactions/medal0`

### Polaroids
- GasStationComputer — `NonEuclidian/HooiSchuur/Hide_HooiSchuur/polaroid_gasStationComputer0`

### Portails sortants
- HooibaalSchuur -> BehindHouse
- HooibaalSchuur -> HooibaalGarden

## Zone : Intratuin

Candidats checks : 2 (pickups 1, polaroids 0, fantômes 0, gulden 1)

### Pickups
- FlowerGem — `Main/Interactions/flowerGem0`

### Gulden posés
- `Main/Gulden/gulden_intratuin0`

### Portails sortants
- Intratuin -> StartGarden (extérieur)

## Zone : Kelder

Candidats checks : 1 (pickups 1, polaroids 0, fantômes 0, gulden 0)

### Pickups
- SoulFragment1 — `NonEuclidian/Kelder/Hide_Kelder/soulFragment0`

### Portails sortants
- Kelder -> BigHouseHallway

## Zone : LongHallway

Candidats checks : 0 (pickups 0, polaroids 0, fantômes 0, gulden 0)

### Portes verrouillées/barrées
- Default — locked:true barred:false — `NonEuclidian/LongHallway/doors/door_end/door0`

### Visibilité conditionnée (ContentHiders)
- [OU] NotTriggeredSmallManInLongHallway — objet: smallDemon0_longHallway0

### Portails sortants
- LongHallway -> AtticRoom
- LongHallway -> OrbRoom
- LongHallway -> SmallChapelOutside

## Zone : MagicPond

Candidats checks : 0 (pickups 0, polaroids 0, fantômes 0, gulden 0)

### Interactions conditionnées
- MagicPondInspect [OU] FishInMagicPond, FishRevived, ObtainedDeadGoldFishButNotUsed
- MagicPondRetrieveFish [OU] NotFishInMagicPond, KeyItemObtained(GoldFishAlive)
- MagicPondPlaceFish [OU] FishInMagicPond, KeyItemNotObtained(GoldFishDead), FishRevived, UsedDeadGoldFish

### Visibilité conditionnée (ContentHiders)
- [OU] NotFishInMagicPond, KeyItemObtained(GoldFishAlive) — objet: MagicPond_FishAlive_Content

### Portails sortants
- MagicPond -> SmallChapelOutside

## Zone : OrbRoom

Candidats checks : 0 (pickups 0, polaroids 0, fantômes 0, gulden 0)

### Portails sortants
- OrbRoom -> LongHallway
- OrbRoom -> StartGarden (extérieur)

## Zone : OutsideVillage

Candidats checks : 0 (pickups 0, polaroids 0, fantômes 0, gulden 0)

### Visibilité conditionnée (ContentHiders)
- [OU] PlayerInBigHouseOffice — objet: content

## Zone : Park (extérieur)

Candidats checks : 12 (pickups 6, polaroids 4, fantômes 0, gulden 2)

### Pickups
- Lighter — `Main/Interactions/lighter0_park0`
- PizzaBox — `Main/Interactions/pizzaBox0`
- OUTIL MagicSword — `Main/Areas/Park/Container/Hide_Park/ParkSwordContainer/itemPickup_magicSword0_overworld`
- OUTIL WateringCan — `Main/Interactions/wateringCan0`
- Eggball — BOUTIQUE 5 gulden — `Main/Areas/Park/Container/Hide_Park/patatkar0/content/patatkarOpen0/eggball0`
- GoldFishDead — `Main/Interactions/goldfishDead0`

### Gulden posés
- `Main/Areas/Park/Container/Hide_Park/BranchHoleContainer/BranchHoleSearched/gulden0 (6)`
- `Main/Gulden/gulden0 (5)`

### Polaroids
- LighterMolehill — `Main/Polaroids/polaroid_lighterMolehill0` [zone: proximité]
- OldLadyBackGarden — `Main/Polaroids/polaroid_oldLadyBackGarden0`
- WateringCan — `Main/Polaroids/polaroid_wateringCan0`
- PurifiedStone — `Main/Areas/Park/Container/Hide_Park/polaroid_purifiedStone0`

### Portes verrouillées/barrées
- Default — locked:true barred:false — `Main/Areas/Park/Container/smallHouse0 (1)/smallDoor1/door0`
- Default — locked:true barred:false — `Main/Areas/Park/Container/smallHouse0/smallDoor1/door0`

### Interactions conditionnées
- KidGiveTrumpet [OU] KeyItemNotObtained(KidTrumpet)
- BranchHoleSearch [OU] BranchHoleSearched
- KidMissingTrumpetInspect [OU] KeyItemObtained(KidTrumpet)
- ScooterInspect [OU] ObjectInactive — refs: {"objectActiveRef":"scooter0"}
- KidGiveTriangle [OU] KeyItemNotObtained(KidTriangle)
- BoatTravelToChurch [OU] KeyItemNotObtained(Paddle)
- KidGiveCymbals [OU] KeyItemNotObtained(KidCymbals)
- KidMissingCymbalsInspect [OU] KeyItemObtained(KidCymbals)
- BraamstruikInspect [OU] KeyItemObtained(Lighter), BraamstruikLit, BraamstruikDestroyed
- BoatInspect [OU] KeyItemObtained(Paddle)
- BraamstruikLight [OU] KeyItemNotObtained(Lighter), BraamstruikLit, BraamstruikDestroyed
- KidMissingTriangleInspect [OU] KeyItemObtained(KidTriangle)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- MolehillInspect [OU] KeyItemObtained(Trowel)
- MolehillInspect [OU] KeyItemObtained(Trowel)
- MolehillInspect [OU] KeyItemObtained(Trowel)
- MolehillInspect [OU] KeyItemObtained(Trowel)
- MolehillInspect [OU] KeyItemObtained(Trowel)
- MolehillInspect [OU] KeyItemObtained(Trowel)
- MolehillInspect [OU] KeyItemObtained(Trowel)
- MolehillInspect [OU] KeyItemObtained(Trowel)
- MolehillInspect [OU] KeyItemObtained(Trowel)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)

### Visibilité conditionnée (ContentHiders)
- [OU] NotInTimeWindow(9h-16h), DayIndexIsNot(2) — objet: SchoolBandPerformance
- [OU] KeyItemObtained(Lighter) — objet: polaroid_lighterMolehill0
- [OU] DayIndexIsNot(2) — objet: polaroid_backGardenTrunk0
- [OU] NotEnteredPlayerSchuur — objet: polaroid_magpieNest0

### Portails sortants
- Park (extérieur) -> BakeryPassage
- Park (extérieur) -> HooibaalGarden
- Park (extérieur) -> RoundHallway

## Zone : PillarSpace

Candidats checks : 5 (pickups 2, polaroids 0, fantômes 2, gulden 1)

### Pickups
- OUTIL Trumpet — `Main/Interactions/trumpet0`
- Bone — `NonEuclidian/PillarSpace/Hide_PillarSpace/skeletonBone_pillarSpace0/bone_skeleton_pillarSpace0`

### Gulden posés
- `Main/Gulden/gulden0 (6)`

### Fantômes
- pos(1202.07, 989.87) — `*** GHOSTS ***/ghost0_pillarspace`
- pos(1202.07, 989.87) — `*** GHOSTS ***/ghost0_backup`

### Interactions conditionnées
- SnailInspect [OU] HasMedalAndNotAwardedSnail, SnailStateIsRacing — refs: {"snail":true}
- SnailAward [OU] SnailStateIsRacing, KeyItemNotObtained(Medal) — refs: {"snail":true}
- SnailInspect [OU] SnailStateIsFinished — refs: {"snail":true}
- Door [OU] NotRepairedMissingDoorknobDoor — refs: {"door":{"path":"*** PORTALS ***/portal_PillarSpaceToChurch0/door0/door0","locked":false,"barred":false,"type":"Default"}}
- Door [OU] NotRepairedMissingDoorknobDoor — refs: {"door":{"path":"*** PORTALS ***/portal_PillarSpaceToRoadMemorial0/door0/door0","locked":false,"barred":false,"type":"Default"}}

### Visibilité conditionnée (ContentHiders)
- [OU] NotSnailAwardedMedal — objet: MedalAwarded

### Portails sortants
- PillarSpace -> Church (extérieur)
- PillarSpace -> Road

## Zone : PlayerSchuur

Candidats checks : 2 (pickups 2, polaroids 0, fantômes 0, gulden 0)

### Pickups
- OUTIL Scissor — `Main/Interactions/scissors0`
- ToiletKey — `Main/Areas/StartGardenArea/Container/Hide_PlayerSchuur/toiletKeyContainer/content/toiletKey0`

### Portails sortants
- PlayerSchuur -> AtticRoom

## Zone : Road

Candidats checks : 15 (pickups 5, polaroids 4, fantômes 2, gulden 4)

### Pickups
- BridgeKey — `Main/Interactions/bridgeKey0` [zone: chemin]
- Bone — `Main/Areas/Road/Container/Hide_Road/skeletonBone_bridge0/bone_skeleton_bridge0` [zone: chemin]
- Lighter — `Main/Interactions/lighter0_car0` [zone: proximité]
- Lighter — `Main/Interactions/lighter0_molehill0` [zone: proximité]
- Plank — `Main/Interactions/item_plank0` [zone: manuel]

### Gulden posés
- `Main/Gulden/GuldenPotContainer0/gulden0_pot` [zone: proximité]
- `Main/Gulden/gulden0 (1)` [zone: proximité]
- `Main/Areas/Road/Container/Hide_RoadMemorial/Pylonnen (1)/pylon0_gulden_Flipped/gulden0_pylon` [zone: chemin]
- `Main/BusArriving0/busArrivingContainer/gulden0_arriveBus0` [zone: chemin]

### Polaroids
- GnomeForestDoor — `Main/Areas/Road/Container/Hide_Road/GnomeJumpscareContainer/gardenGnome0 (15)/polaroid_gnomeForestDoor0` [zone: chemin]
- Van — `Main/Interactions/polaroid_van0` [zone: proximité]
- HighBridgeKey — `Main/Polaroids/polaroid_highBridgeKey0` [zone: chemin]
- HangjongPizzaBox — `Main/Polaroids/polaroid_hangjongPizzaBox0` [zone: proximité]

### Fantômes
- pos(-29.14, -91.93) — `*** GHOSTS ***/ghost0_redCar0` [zone: proximité]
- pos(-114.1, -47.36) — `*** GHOSTS ***/ghost0_bridge` [zone: chemin]

### Portes verrouillées/barrées
- Gate — locked:true barred:true — `Main/Interactions/churchGateBarred0/door0`
- Default — locked:true barred:false — `Main/Areas/Road/Container/Hide_Road/highBridgeDoor0/door0`

### Interactions conditionnées
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- Human [OU] HasEggballAndNotTradedEggball — refs: {"human":"Main/Areas/Road/Container/Hide_Road/scruffyMan0/scruffyMan_talkInteraction0"}
- PizzaBoxClear [OU] ClearedPizzaBox, KeyItemNotObtained(PizzaBox)
- CarTrunk [OU] RedCarTrunkOpened, RedCarLocked — refs: {"carTrunk":"RedCar"}
- CarTrunk [OU] TankCarTrunkOpened — refs: {"carTrunk":"TankCar"}
- GasStationLadyDeadInspect [OU] RedCarLocked, RedCarLockNotSmashed
- ScooterInspect [OU] ObjectInactive — refs: {"objectActiveRef":"scooter0 (1)"}
- TradeEggball [OU] KeyItemNotObtained(Eggball), TradedEggball
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- BridgeGap [OU] BridgeRepaired, HighBridgeNotDetroyed, PlayerHasPlankToRepairBridge
- BridgeRepair [OU] BridgeRepaired, HighBridgeNotDetroyed, DontHaveAnyPlank, AllPlanksUsed
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- MolehillInspect [OU] KeyItemObtained(Trowel)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- BusSeat [OU] ObjectActive — refs: {"objectActiveRef":"gulden0_arriveBus0_content"}

### Visibilité conditionnée (ContentHiders)
- [ET] EnteredStartGarden, IsNotVisible, BusIsGone — objet: BusArriving0
- [OU] PlayerInBigHouseOffice, InRedWorld — objet: GasStationExtraHiderContainer
- [OU] NotRepairedBridgeWithPlank — objet: plankRepaired_default0
- [OU] NotTradedEggball — objet: scruffyMan_eggball0
- [OU] RedCarLockNotSmashed — objet: redCarLockSmashed0
- [OU] TradedEggball — objet: scruffyMan_triangleStick0
- [OU] ChurchGateUnbarred — objet: ChurchGateBarredContent
- [OU] IsFinalDay, PlayerInBigHouseOffice — objet: RedCarObjectContainer
- [OU] TankCarTrunkOpened — objet: carNotOpen0
- [OU] TradedEggball — objet: scruffyMan_triangleInstrument0
- [OU] TankCarTrunkNotOpened — objet: carOpen0
- [OU] NotRepairedBridgeWithOldPlank — objet: plankRepaired_oldPlank0
- [OU] RedCarTrunkNotOpened, RedCarLocked — objet: carOpen0
- [OU] NotRepairedMissingDoorknobDoor — objet: portal_RoadMemorialToPillarSpace0
- [OU] RedCarTrunkOpened, RedCarLockSmashed — objet: carNotOpen0
- [OU] ChurchGateBarred — objet: ChurchGateUnbarredContent
- [OU] RedCarLockSmashed, RedCarNotLocked — objet: redCarLockNotSmashed0

### Portails sortants
- Road -> PillarSpace

## Zone : RoundHallway

Candidats checks : 1 (pickups 1, polaroids 0, fantômes 0, gulden 0)

### Pickups
- OUTIL WateringCan — `NonEuclidian/RoundHallway/Hide_RoundHallway/wateringCan0_demo`

### Portes verrouillées/barrées
- Default — locked:true barred:false — `*** PORTALS ***/portal_RoundHallwayToPark0/gnomeDoor0 (1)/door0 (1)`

### Interactions conditionnées
- FishbowlRetrieveDeadFish [OU] FishbowlStateIsNotFishDead — refs: {"fishbowl":true}
- FishbowlInspect [OU] FishbowlStateIsFishDead, PlayerHasFishDeadAndNotUsed, PlayerHasFishAliveAndNotUsed — refs: {"fishbowl":true}
- FishbowlPlaceFishAlive [OU] FishbowlStateIsNotEmpty, KeyItemNotObtained(GoldFishAlive) — refs: {"fishbowl":true}
- FishbowlPlaceFishDead [OU] FishbowlStateIsNotEmpty, KeyItemNotObtained(GoldFishDead), PlayerHasFishAliveAndNotUsed — refs: {"fishbowl":true}

### Visibilité conditionnée (ContentHiders)
- [OU] FishbowlStateIsNotFishAlive — objet: FishAliveContainer
- [OU] FishbowlStateIsNotFishDead — objet: FishDeadContainer

### Portails sortants
- RoundHallway -> GnomeForest
- RoundHallway -> Park (extérieur)
- RoundHallway -> StartGarden (extérieur)

## Zone : RummikubSpace

Candidats checks : 1 (pickups 1, polaroids 0, fantômes 0, gulden 0)

### Pickups
- PurifiedStone — `NonEuclidian/Rummikub/Hide_Rummikub/purifiedStone0`

### Interactions conditionnées
- PurifiedStoneCantTake [OU] LitRummikubHooibaal
- RummikubHooibaalInspect [OU] KeyItemObtained(Lighter), LitRummikubHooibaal
- Item [OU] NotLitRummikubHooibaal — refs: {"itemPickup":"NonEuclidian/Rummikub/Hide_Rummikub/purifiedStone0"}
- RummikubOudjesConcerned [OU] NotLitRummikubHooibaal
- RummikubInspect [OU] LitRummikubHooibaal
- RummikubHooibaalLight [OU] KeyItemNotObtained(Lighter), LitRummikubHooibaal

### Visibilité conditionnée (ContentHiders)
- [OU] LitRummikubHooibaal — objet: Scenario_hooibaal_not_lit
- [OU] NotLitRummikubHooibaal — objet: Scenario_hooibaal_lit

## Zone : SmallChapelOutside

Candidats checks : 0 (pickups 0, polaroids 0, fantômes 0, gulden 0)

### Portails sortants
- SmallChapelOutside -> LongHallway
- SmallChapelOutside -> MagicPond

## Zone : SnowWorld

Candidats checks : 1 (pickups 1, polaroids 0, fantômes 0, gulden 0)

### Pickups
- SoulFragment3 — `NonEuclidian/SnowWorld/Hide_SnowWorld/soulFragment2`

### Portails sortants
- SnowWorld -> BigHouseKitchen

## Zone : StartGarden (extérieur)

Candidats checks : 21 (pickups 9, polaroids 9, fantômes 0, gulden 3)

### Pickups
- Paddle — `Main/Areas/StartGardenArea/Container/Hide_StartGarden_back0/paddle0`
- Worm — répétable — `Characters/Magpie/magpieDeadByWorm0/wormMagpie0`
- Worm — `Main/Interactions/worm0`
- StrangeKey — `Characters/Magpie/magpieDeadByWorm0/strangeKey0_demo`
- OUTIL Hammer — `Main/Interactions/hammer0_car` [zone: manuel]
- ShyIdol — `Main/InvisibleMan/shyIdol0`
- PrettyFlower — `Main/Areas/StartGardenArea/Container/Hide_StartGarden_mid0/PrettyPotContainer/PrettyPot_interactions/prettyFlower_remove0`
- (sans keyItem) — `Main/Interactions/interactionTest0`
- SeveredHand — `Main/Areas/StartGardenArea/Container/Hide_StartGarden_back0/outsideWindowTwigs0/severedHand0`

### Gulden posés
- `Main/Areas/StartGardenArea/Container/Hide_HouseDetails/gardenGnome0/gardengnome_Destroyed/gulden0_gardenGnome`
- `Main/Gulden/gulden0`
- `Main/Gulden/gulden0 (2)`

### Polaroids
- BackGardenCarTrunk — `Main/Polaroids/polaroid_backGardenTrunk0`
- GasStation — `Main/Areas/StartGardenArea/Container/Hide_HouseDetails/gardenGnome0/gardengnome_Destroyed/polaroid_gasStation0`
- BackGardenFence — `Characters/Magpie/polaroid_backGardenFence0`
- GnomeIdol — `Main/Polaroids/polaroid_gnomeIdol0`
- PlayerShed — `Main/Polaroids/polaroid_playerShed0`
- MagpieNest — `Main/Polaroids/polaroid_magpieNest0`
- TallManWindow — `Main/Polaroids/polaroid_tallManWindow0`
- DeadGardener — `Main/Areas/StartGardenArea/Container/Hide_StartGarden_back0/outsideWindowTwigs0/polaroid_deadGardener0`
- Crypt — `Main/Polaroids/polaroid_crypt0`

### Portes verrouillées/barrées
- Gate — locked:true barred:false — `Main/Interactions/gardenGate0/door0`
- Default — locked:true barred:false — `*** PORTALS ***/portal_StartGardenToToilet0/toiletBuilding_door0/door0`
- Default — locked:true barred:false — `Main/Areas/StartGardenArea/Container/Hide_HouseDetails/bigHouseDoors/frontEntrance0/entranceDoor0/bigHouseFrontEntranceDoor0 (1)/bigHouseFrontDoorOther0`
- Default — locked:true barred:false — `Main/Areas/StartGardenArea/Container/Hide_HouseDetails/intratuinDoor0/door0`
- Default — locked:true barred:false — `Main/Areas/StartGardenArea/Container/Hide_HouseDetails/bigHouseDoors/frontEntrance0/entranceDoor0/bigHouseFrontEntranceDoor0/bigHouseFrontDoorMain0`
- Default — locked:true barred:false — `Main/Areas/StartGardenArea/Container/Hide_HouseDetails/smallDoor0/door0`

### Interactions conditionnées
- LeaveThroughFrontGate [OU] NotInGoodEnding, NotTalkedToOwnerSaved
- MagpieNestInspect [OU] StartGardenOver30 — refs: {"objectActiveRef":"Magpie"}
- MagpieFeed [OU] ObjectInactive, KeyItemNotObtained(Worm), MagpieFed, MagpieKilledWithShears, MagpieKilledWithHammer — refs: {"objectActiveRef":"magpieAlive0"}
- LoosePlank [OU] RottenPlankPulled
- GnomeInspect [OU] DestroyedStartGnome
- Item [OU] ObjectInactive, NotPlacedApple — refs: {"itemPickup":"Main/Interactions/worm0","objectActiveRef":"wormLine"}
- SpecialSeedPlant [OU] SpecialSeedPlanted, KeyItemNotObtained(SpecialSeed)
- PlaceApple [OU] KeyItemNotObtained(Apple), PlacedApple
- RestoreSoul [OU] NotHasAllSoulFragments — refs: {"ownerSaved":true}
- Item [OU] NotSpecialSeedPlanted, SpecialFlowerStateIsNotGrowFinished, KeyItemObtained(PrettyFlower) — refs: {"itemPickup":"Main/Areas/StartGardenArea/Container/Hide_StartGarden_mid0/PrettyPotContainer/PrettyPot_interactions/prettyFlower_remove0"}
- EmptyPlateInspect [OU] KeyItemObtained(Apple), PlacedApple
- CarTrunk [OU] OldCarTrunkOpened — refs: {"carTrunk":"OldCar"}
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- OwnerSavedTalk [OU] HasAllSoulFragmentsButNotRestoredSoul — refs: {"ownerSaved":true}
- SpecialPotInspect [OU] HasSpecialSeedButNotPlanted, SpecialFlowerStateIsGrowFinished
- FenceHole [OU] FenceDestroyed, RottenPlankNotPulled
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- MolehillInspect [OU] KeyItemObtained(Trowel)
- MolehillInspect [OU] KeyItemObtained(Trowel)
- MolehillInspect [OU] KeyItemObtained(Trowel)
- MolehillInspect [OU] KeyItemObtained(Trowel)
- MolehillInspect [OU] KeyItemObtained(Trowel)
- MolehillInspect [OU] KeyItemObtained(Trowel)
- MolehillInspect [OU] KeyItemObtained(Trowel)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- Door [OU] IsBigHouseFrontDoorAndInRedWorld — refs: {"door":{"path":"Main/Areas/StartGardenArea/Container/Hide_HouseDetails/bigHouseDoors/frontEntrance0/entranceDoor0/bigHouseFrontEntranceDoor0/bigHouseFrontDoorMain0","locked":true,"barred":false,"type":"Default"}}
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)

### Visibilité conditionnée (ContentHiders)
- [OU] OldCarTrunkNotOpened — objet: carOpenContainer
- [OU] FenceNotDestroyed — objet: FenceHoleDestroyedCollider0
- [OU] NotDestroyedShyDemon, ObtainedShyIdol — objet: shyIdol0
- [OU] NotInRedWorld — objet: tornPages0
- [OU] NotSpecialSeedPlanted, PrettyFlowerStateIsRemoved — objet: PrettyFlower0
- [OU] PlayerNotSawTallManOutsideWindow — objet: outsideWindowTwigs0
- [OU] UnlockedIntratuin — objet: flowerSymbol_off0
- [OU] NotInTitle, NotTitleScreenShowTallMan — objet: TallManTitleScreenContainer0
- [OU] NotSpecialSeedPlanted, PrettyFlowerStateNotIsPlanted — objet: prettySeed_overworld0
- [OU] NotUnlockedMagpie, GotPlankHit — objet: nestLetterParticles0
- [OU] ReadPlayerSchuurBrief — objet: playerSchuurBriefParticles0
- [OU] NotRevealedShyDemon, DestroyedShyDemon — objet: shyDemon0
- [OU] OldCarTrunkOpened — objet: carNotOpenContainer
- [OU] NotUnlockedIntratuin — objet: flowerSymbol_on0
- [OU] FenceDestroyed — objet: FenceHoleNotDestroyedCollider0

### Portails sortants
- StartGarden (extérieur) -> BigHouseHallway
- StartGarden (extérieur) -> ForestPassage
- StartGarden (extérieur) -> HedgeMaze
- StartGarden (extérieur) -> HedgeMazeInner
- StartGarden (extérieur) -> Intratuin
- StartGarden (extérieur) -> OrbRoom
- StartGarden (extérieur) -> RoundHallway
- StartGarden (extérieur) -> Toilet

## Zone : Tent

Candidats checks : 2 (pickups 1, polaroids 1, fantômes 0, gulden 0)

### Pickups
- Coin — `NonEuclidian/TentInside/Hide_Tent/blueCoin0`

### Polaroids
- Well — `NonEuclidian/TentInside/Hide_Tent/polaroid_well0`

### Portails sortants
- Tent -> Toilet

## Zone : Toilet

Candidats checks : 2 (pickups 1, polaroids 1, fantômes 0, gulden 0)

### Pickups
- OUTIL Trowel — `Main/Interactions/trowel0_demo`

### Polaroids
- Gnome — `Main/Polaroids/polaroid_gnome0`

### Portes verrouillées/barrées
- Default — locked:true barred:false — `NonEuclidian/ToiletBuilding/Hide_ToiletBuilding/Doors/toiletStallDoor0 (2)/door0`

### Interactions conditionnées
- GiveToiletPaper [OU] CompletedToiletPaperSequence, GaveToiletPaperToSmallDemon, KeyItemNotObtained(ToiletPaper), SmallManRemovedFromToilet

### Visibilité conditionnée (ContentHiders)
- [OU] NotTriggeredSmallManOnToilet, RemovedSmallManFromToilet, HumanMovementHidden, GaveToiletPaperToSmallDemon, DestroyedSmallDemon — objet: smallDemonToiletPeekContent
- [OU] RemovedSmallManFromToilet, HumanMovementNotHidden, GaveToiletPaperToSmallDemon, DestroyedSmallDemon — objet: smallDemon0_toilet0
- [OU] CompletedToiletPaperSequence — objet: toiletPot0_smallDemon0

### Portails sortants
- Toilet -> StartGarden (extérieur)
- Toilet -> Tent

## Zone : Veerboot

Candidats checks : 0 (pickups 0, polaroids 0, fantômes 0, gulden 0)

### Portails sortants
- Veerboot -> VeerbootHallway

## Zone : VeerbootHallway

Candidats checks : 0 (pickups 0, polaroids 0, fantômes 0, gulden 0)

### Portails sortants
- VeerbootHallway -> Ferry
- VeerbootHallway -> Veerboot

## Zone : VeerbootHuis

Candidats checks : 2 (pickups 1, polaroids 1, fantômes 0, gulden 0)

### Pickups
- ToyBoat — `NonEuclidian/VeerbootHuis/Hide_VeerbootHuis/toyBoat0`

### Polaroids
- Ferry — `NonEuclidian/VeerbootHuis/Hide_VeerbootHuis/polaroid_ferry0`

### Portails sortants
- VeerbootHuis -> WindyPath

## Zone : Void

Candidats checks : 2 (pickups 1, polaroids 0, fantômes 1, gulden 0)

### Pickups
- Bone — `NonEuclidian/Void/Hide_void/VoidExtraHiderContainer/skeletonBone_void0/bone_skeleton_void0`

### Fantômes
- pos(5.13, 994.96) — `*** GHOSTS ***/ghost0_void`

### Visibilité conditionnée (ContentHiders)
- [OU] NotInsertedDisc — objet: VoidExtraHiderContainer

### Portails sortants
- Void -> BigHouseOffice
- Void -> GasStationOffice

## Zone : WindyPath

Candidats checks : 4 (pickups 2, polaroids 1, fantômes 1, gulden 0)

### Pickups
- AbandonedKey — `Main/Interactions/abandonedKey0` [zone: proximité]
- Worm — répétable — `Main/Areas/BehindHouse/Container/Hide_BehindHouse1/fishermanVanished0/wormFisherman0`

### Polaroids
- Bone — `Main/Polaroids/polaroid_bone0`

### Fantômes
- pos(-14.83, 91.15) — `Main/Interactions/scooterCrash0/ghost0_scooterCrash0`

### Interactions conditionnées
- ScooterCrash [OU] ObjectInactive — refs: {"objectActiveRef":"scooterCrash0"}
- Human [ET] FishermanNotGaveWorm, KeyItemObtained(Worm) — refs: {"human":"Main/Areas/BehindHouse/Container/Hide_BehindHouse1/fisherMan0/fishermanInteraction0"}
- FishermanGiveWorm [OU] FishermanGaveWorm, KeyItemNotObtained(Worm)
- DogGiveBone [OU] GaveBoneToDog, KeyItemNotObtained(Bone) — refs: {"dog":true}
- DogPet [OU] NotGaveBoneToDog — refs: {"dog":true}
- Wait1Hour [OU] ObjectActive — refs: {"objectActiveRef":"fisherMan0"}
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)
- FlowerInspect [OU] KeyItemObtained(WateringCan)

### Visibilité conditionnée (ContentHiders)
- [OU] NotGaveBoneToDog — objet: angryDog_bone0

### Portails sortants
- WindyPath -> VeerbootHuis
