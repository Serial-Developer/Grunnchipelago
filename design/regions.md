# Grunn - Graphe de régions v3 (carte Jonath × données de scène)

Consolidation : carte manuelle + 28 points de Jonath (2026-07-12), croisés avec
zone_logic.json / dump. Chaque connexion porte sa source :
- [D] = dérivé des données de scène (fiable)
- [J#] = point n° # de Jonath (connaissance du jeu)
- [D+J#] = croisé, confirmé des deux côtés
- [?] = à clarifier (voir Questions ouvertes)

## Régions (nom carte -> zones techniques)

| Région | Zones techniques | Notes |
|---|---|---|
| Jardin | StartGarden (extérieur), Intratuin | Intratuin librement liée au jardin (portails libres) |
| Cour derrière maison | BehindHouse | marteau (voiture), ver du pêcheur |
| Buanderie | Bijkeuken | contient ChurchKey [J10] |
| Manoir | BigHouseHallway, BigHouseKitchen, BigHouseStairs, BigHouseOverloop, BigHouseAttic, BigHouseFridge, BigHouseOffice | |
| Cave (monde rouge) | Kelder | accès InRedWorld [D] |
| Cabane joueur | PlayerSchuur | |
| Couloir final | LongHallway | dimanche soir [J13] |
| Salle de l'orbe | OrbRoom | |
| Toilettes | Toilet | |
| Tente | Tent | |
| Serre du disque | GlassHouse | SpecialSeed (jolie graine) [J15] |
| Parc | Park (extérieur) | |
| Jardin botte de foin | HooibaalGarden, HooibaalSchuur | marchande dans la Schuur [J19] |
| Église | Church (extérieur), ChurchBigHall, ChurchHallway | vestibule = contrainte horaire [J22] |
| Crypte / Autel des Idoles | Crypt, CryptStairs | idoles Tall/Short/Shy [D+J-Q5] |
| Porte cassée église | PillarSpace (probable) [J-Q5] | débloquée à 20% de complétion Église [J-Q2] ; contient un vélo (carte) |
| Extérieur / Route | Road, OutsideVillage | |
| Station essence | GasStation | se ferme, Hammer pour rentrer [J5] |
| Bureau station | GasStationOffice | |
| Champ de maïs | CornField, CornFieldCenter | maïs ramassable direct [J21] |
| Labyrinthe (entrée) | HedgeMaze | |
| Cœur du labyrinthe | HedgeMazeInner | |
| Passage des Gnomes | GnomeForest, RoundHallway | hub DestroyedAllJumpscareGnomes [D] |
| Temple (poisson) | MagicPond | FishInMagicPond / FishRevived [D] - intuition [J] confirmée |
| Cabane pêcheur | VeerbootHuis, WindyPath | pêcheur/ver/chien/os [D+J24] |
| Ferry | Ferry, Veerboot, VeerbootHallway | DayIndexIs(2) + ToyBoat [D] |
| Bunker | Bunker | SeveredHand [D] |
| Void | Void | traversée -> Bureau du manoir [J14] |
| Zone de la pomme | AppleSpace | |
| Zone après le vélo | RummikubSpace | PurifiedStone via Lighter [J28+J-Q3] |
| Picnic | Forest, ForestPassage | confirmé [J-Q5] ; Blanket + Sandwich [D] |
| Plage | Hill, HillPassage | confirmé [J-Q5] |
| Boulangerie | Bakery, BakeryPassage | |
| Zones scénario | SnowWorld, BottleRoom, AtticRoom, SmallChapelOutside | entrées par événement/état de monde |

## Connexions et règles d'accès

### Démarrage et axe principal
- Spawn (Route, bus) -> **Jardin** : **`BridgeKey`** - LA première clé du jeu (pickup
  `bridgeKey0` sur la Route, près du spawn). Sphère 1 sans la clé = 2 checks :
  la location `Obtain BridgeKey` elle-même + le gulden du bus (ce dernier seulement si
  coinsanity) -> option YAML `exclude_bridge_key` défaut ON [J 2026-07-12].
  Le bus bloque toute autre sortie [J23]
- **Jardin** -> **Extérieur** (pont) : cassé au premier passage ; retour = `Plank OU OldPlank` (RepairedBridgeWithPlank / RepairedBridgeWithOldPlank) [D+J23]
- **Jardin** -> **Cour derrière maison** : libre (planches à retirer, aucun item) [J8]
- **Cour** -> **Extérieur** (haie) : `Shears OU MagicSword` [J9+J25]
- **Cour** -> **Buanderie** : libre [J10]
- **Buanderie** -> **Manoir** : fermée ; s'ouvre définitivement en arrivant dans le manoir via le bureau (`UnlockedBijkeukenShortcut`) [D+J10/15]
- **Jardin** -> **Église** (portail) : **`GardenKey`** (pickup dans la buanderie - la « clé de l'église » au sens de J10 est techniquement GardenKey ; corrigé 2026-07-12)
- **Extérieur** -> **Église** : libre - le portail barré ne s'ouvre QUE depuis le côté Extérieur (ChurchGateUnbarred), impossible depuis le côté Église ; une fois ouvert, passage bidirectionnel [D+J-Q1]. L'Église a donc deux accès : portail jardin (GardenKey) ou par l'Extérieur.
- **Extérieur** -> **Champ de maïs** : libre [J 2026-07-12]
- **Extérieur** -> **Bunker** : libre [J 2026-07-12]
- **Cabane pêcheur (intérieur)** : `Bone` à donner au chien - sans os, le chien tue et déclenche la fin Dog [J 2026-07-12] ; l'accès à l'abord de la cabane est libre
- **Embarcadère Ferry** : libre à pied [J 2026-07-12] ; traversée = DayIndexIs(2) + ToyBoat [D]
- **Église** -> **Porte cassée** (PillarSpace probable) : 20% de complétion de l'Église [J-Q2]
- **Parc** <-> **Extérieur** : `Lighter` (ronce brûlée = BraamstruikDestroyed, permanent dans la run, praticable des deux côtés) [D+J26]
- **Église** <-> **Parc** (barque) : `Paddle` (BoatTravelToPark/Church) [D+J6]

### Chaîne fanfare (instruments du gamin, Parc)
- **KidCymbals** : pickup dans le HedgeMaze [D]
- **KidTriangle** : échange derrière la Station essence - le PNJ veut un `Eggball`
  (œuf pané) ; Eggball = 5 gulden au food truck du Parc, ouvert le samedi en journée
  (jour/horaire = libres logiquement) [J 2026-07-12 + D : TradedEggball]
- Remise des instruments : gamin de la fanfare au Parc (SchoolBandPerformance) [D]

### Maison et disque
- **Extérieur** -> **Station essence** : libre au début ; après fermeture `Hammer` (GasStationDoorDestroyed) [D+J5]
- **Station** -> **Bureau station** : `OfficeKey` - 2 sources : achat marchande (2 gulden) OU gratuite sur le comptoir si entrée au marteau [J5+carte]
- **Bureau station** (+ `Cd` + PC : ComputerOn/InsertedDisc) -> **Zone de la pomme** OU **Void** (2 faces du disque) [D+J14]
- **Void** -> **Bureau du manoir** (traversée) [J14]
- **Bureau du manoir** (+ `Cd`) -> **Void** OU **Serre du disque** (SpecialSeed) ; sans disque -> **Manoir** [J15]
- Arrivée dans le **Manoir** via bureau => ouvre la buanderie définitivement [D+J15]
- **Manoir** -> **Cave** : `InRedWorld` (scénario) [D]

### Cabane, toilettes, fin de semaine
- **Toilettes** : `ToiletKey` (dans la cabane joueur) [J11]
- **Toilettes** -> **Tente** : donner `ToiletPaper` au PNJ jour 1 avant 12h (CompletedToiletPaperSequence) [D+J12]
- **Cabane joueur** -> **Couloir final** : `IsFinalDay` (dimanche) + heure du soir (~18h, à préciser) [D+J13]
- **Couloir final** -> **Salle de l'orbe** : `StrangeKey` - dans la pie : donner `Worm` -> la pie disparaît et lâche la clé [J13+J-Q4 ; données : UnlockedMagpie/DestroyedMagpie/WormReturned]

### Complétion des zones (portails 100%)
- **Jardin 100%** -> **Picnic** [J1 ; données : StartGardenAreaNotCompleted / MaintainedGardenArea]
- **Église 100%** -> **Plage** [J2 ; données : MaintainedChurchArea]
- **Parc 100%** -> **Boulangerie** [J3 ; données : MaintainedParkArea]

### Zones spéciales
- **Jardin** -> **Labyrinthe (entrée)** : précipice = `(Plank OU OldPlank)` OU `(Coin + accès Église)` - la pluie place une planche seule (DeepGapPlacePlank / ClosedGapWith*) [D+J17/18]
- **Labyrinthe** -> **Cœur** : `Compass` obligatoire (sinon bad ending) [J20]
- **Parc** -> **Jardin botte de foin / Marchande** : 20% de complétion du Parc (ParkUnlockedHooibaalGarden) + `Lighter` (bougies, LitAllHooiGardenCandles) [D+J19+J-Q2]
- **Passage des Gnomes** <-> Parc/Jardin : `DestroyedAllJumpscareGnomes` - casser les gnomes = **`Hammer` uniquement** (pas d'équivalence épée/truelle ici) [D+J 2026-07-12] (hub RoundHallway)
- **Extérieur (OutsideVillage, près du bateau)** <-> **Zone après le vélo** : vélo aller-retour, aucun item, fonctionne comme un portail - BikeTravelToRummikub (aller) / BikeTravelToPath (retour vers le vélo de l'extérieur, PAS vers WindyPath) [D+J 2026-07-12]
- **Ferry** : `DayIndexIs(2)` + `ToyBoat` au gamin (GaveToyToFerryKid) [D]
- **Bus** : `BoughtBusTicket` (10 gulden) -> fin Bus [D]

### Fins - conditions précisées (J 2026-07-12)
- **Darkness** : rester dehors (n'importe où) après minuit - logiquement LIBRE dès le départ
- **WorldEnd** : atteindre la zone Hell et mourir (timer OU combat du démon sans PurifiedStone) - règle = accès Hell, aucun item supplémentaire

## Règles d'équivalence d'items (pour la logique AP)

- **Couper l'herbe** : `Shears OU MagicSword` [J25]
- **Arroser** : `WateringCan OU (Coin + accès Église)` - la pièce bleue = KeyItem.Coin, pluie = BadWeather [D+J18]
- **Planche** : `Plank OU OldPlank` - deux KeyItems distincts dans les données [D]
- **Os (Bone)** : casser un squelette (`Hammer OU MagicSword OU Trowel`) OU via le Manoir par le bureau [J24]
- **Serre des gnomes (jardin)** : arroser toutes les fleurs étranges = accès Jardin+Parc+Église+Extérieur + capacité d'arrosage -> pièce + FlowerGem [J16]
- **Économie** : tonte = revenu renouvelable (sécateurs suffisent logiquement pour 10 gulden du bus, disque 5, boussole 4, clé 2, médaille 10) [D+carte]

## Découvertes des données (statut après réponses de Jonath)

1. **MagicSword - deux sources, hiérarchie clarifiée [J-Q6]** : l'épée du grenier du Manoir
   (`attic_cardboardBox0_magicSword`) est la source PRINCIPALE (première obtention, via la
   chaîne du disque). Celle du parc est un bonus post-bonne-fin (`NotFoundGoodEnding` sur
   ParkSwordContainer). Logique : MagicSword atteignable via accès Manoir.
2. **4 pickups Lighter** (pas 2) : station (5 gulden), parc (gratuit), voiture route,
   taupinière route - les deux derniers sont peut-être des variantes d'état du même
   briquet (ContentHiders à examiner).
3. Chaîne popcorn : KeyItems Corn/Butter/Popcorn + conditions MadePopcorn/PutCornInFryingPan :
   logique de cuisine au Manoir non couverte par la carte.
4. Le « Temple ? » de la carte = MagicPond (FishInMagicPond/FishRevived + GoldFishAlive/Dead) :
   l'intuition « résurrection poisson » est confirmée.

## Questions résolues (réponses Jonath, 2026-07-12)

- **Q1** : le portail Église<->Extérieur ne s'ouvre QUE depuis le côté Extérieur, sans clé ;
  impossible depuis le côté Église. Intégré aux connexions.
- **Q2** : « Unlock à 20% » = botte de foin à 20% de complétion du PARC ; porte cassée à
  20% de complétion de l'ÉGLISE. Intégré.
- **Q3** : la « rune » = `PurifiedStone`. Intégré.
- **Q4** : la clé de la pie = `StrangeKey`. Intégré.
- **Q5** : Plage = Hill ; Picnic = Forest ; Autel des Idoles = la Crypte ; PillarSpace =
  probablement la zone derrière la porte cassée. Intégré.
- **Q6** : voir découverte n°1.

## Reste à faire (mineur)

- Confirmer la condition Extérieur -> Champ de maïs (libre ?)
- Vérifier si les 2 briquets « route » (voiture/taupinière) sont des variantes d'état
- Préciser l'heure exacte du passage Cabane -> Couloir final (dimanche ~18h)
- Trancher l'emplacement exact des vélos (porte cassée vs extérieur) et leurs destinations
  (BikeTravelToRummikub / BikeTravelToPath)

## Séquence finale - zone « Hell » (Jonath, 2026-07-12 ; croisé ScenarioType.Hell)

Prérequis d'accès à la crypte (tous nécessaires) :

**I.1 - GnomeIdol** : casser le gnome du jardin (`Hammer|MagicSword|Trowel`) + entrer
dans la Station essence + retour Jardin -> portail -> idole.
**I.2 - ShyIdol (idole fantôme, par élimination d'enum)** : accès Église + porte rouge
cassée réparée (= 20 % complétion Église) + Trumpet à l'intérieur (PillarSpace) +
jouer près des herbes qui bougent (Jardin, accès départ).
**I.3 - ShortIdol (petite)** : accès Extérieur + `Bone` au chien (Cabane pêcheur) ->
`ToyBoat` dans la cabane -> le donner à l'enfant du restaurant du Ferry.
**I.4 - TallIdol (grande)** : franchir le gap labyrinthe (`Plank|OldPlank` OU
`Coin` dans le puits [puits à l'Église ; pickup Coin : Toilettes en vanilla]) +
`Compass` (marchande 4 gulden : Parc + Lighter + 20 % Parc) sinon fin labyrinthe +
frapper le TallMan (`MagicSword|Hammer`).
**II - FlowerGem** : arroser toutes les fleurs étranges = accès Jardin+Parc+Église+
Extérieur + (`WateringCan` OU `Coin` dans le puits [pluie]).
**III - Porte de l'église** : accès Église + après minuit (fenêtre horaire = libre
logiquement) ; reste ouverte pour la run.
**IV - ChurchKey** (porte intérieure) : chaîne du disque - `Cd` (marchande 5 gulden) +
`OfficeKey` (marchande 2 gulden OU comptoir station via `Hammer`) + PC bureau station
-> Manoir -> ChurchKey dans le hall (données : BigHouse_Hallway).
**V - Crypte** : entrer (III) -> ouvrir avec ChurchKey (IV) -> poser FlowerGem sur le
pupitre (InsertedFlowerGem) -> crypte apparaît -> déposer les 4 idoles (ReturnedXIdol).

### Zone Hell (après dépôt des idoles)
- Église en flammes + squelettes ; déplacement restreint : navette Église <-> Manoir
  (entrée principale débloquée), incendie ailleurs
- **Owner** accessible directement -> donne `AtticKey` (Owner.cs/EndConversation,
  ActivateEndDemon)
- **MagicSword** : carton du grenier (nécessite AtticKey pour la porte du grenier)
- **SoulFragments x3** : éparpillés dans le Manoir version Hell ; celui des étagères
  à bocaux nécessite `Hammer`
- **GoodEnd** : `MagicSword` + `PurifiedStone` (= la « pierre de protection » = la
  rune de RummikubSpace [vélo + Lighter]) + confrontation
- **TrueEnding** : GoodEnd + les 3 SoulFragments (RestoreOwnerSoul)

### Correction de clés (données pickups, 2026-07-12)
- Portail Jardin -> Église : **GardenKey** (pickup dans la Bijkeuken) - et non ChurchKey
- Porte intérieure de l'église : **ChurchKey** (pickup dans le hall du Manoir)
- Question ouverte : identité de « la première clé du jeu » visée par l'option
  exclude_garden_key (le pickup GardenKey n'est pas en début de partie)

## Compléments (réponses Jonath, 2026-07-12, session 2)

- **Clés - mapping définitif** : `GardenKey` (pickup Bijkeuken) = portail Jardin->Église
  (Jonath la surnommait « clé de l'église », confusion levée). `ChurchKey` (pickup hall
  du Manoir) = porte intérieure de l'église (chapelle). Conséquence design : GardenKey
  ne verrouille PAS tout le jeu (l'Église reste accessible par l'Extérieur) - le sort de
  l'option exclude_garden_key est en cours de décision.
- **KidTriangle** : s'échange auprès de la personne DERRIÈRE la Station essence contre
  un `Eggball` (œuf pané), acheté 5 gulden au food truck du Parc (ouvert le samedi en
  journée, ferme en début de soirée -> contrainte temporelle = libre logiquement).
  Chaîne : accès Parc + 5 gulden -> Eggball -> arrière de la station -> KidTriangle.
  (Croisement données : condition TradedEggball, camionnette du Parc sur la carte.)
- **Cabane pêcheur - nuance d'entrée** : l'approche est libre, mais ENTRER dans la
  cabane exige de donner `Bone` au chien ; sinon le chien tue le joueur et déclenche la
  fin Dog. Conséquences : région « Cabane pêcheur (intérieur) » [ToyBoat] = `Bone` ;
  location « Ending: Dog » = simple accès à la cabane (mourir est gratuit).
- **Connexions libres confirmées** : Champ de maïs, Bunker, embarcadère Ferry, approche
  de la Cabane pêcheur - sans condition depuis leurs zones adjacentes.
- **Marchande** : 20 % du PARC confirmé (le « jardin » du point IV était un lapsus).
