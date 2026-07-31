# Notes de playtest — à consolider dans le prompt CC global (en cours, 2026-07-13)

### Features temporalité + refonte des traps (demande Jonath, 2026-07-27) — à valider in-game

**Temporalité** (`Grunnchipelago.Client/TimeFeatures.cs`, nouveau) — vanilla ne sait
avancer l'horloge que d'UNE heure (`GameManager.Wait1Hour` → `TimeController.Skip1Hour`,
15 bancs `InteractionType.Wait1Hour` au dump) :
- **Gouttière du jardin** (`Main/Triggers/Rainpipe`, la seule du dump ; postfix sur
  `Rainpipe.Trigger`) → saut à **00h05**, avec écran noir + son d'attente vanilla.
- **Banc de l'église** `bench0 (3)` (10.59, 10.0, 1.28) → saut à **00h05**. C'est le banc
  Wait1Hour le plus proche de la porte (~34 m de `churchSideDoorKnock0`, type
  `ChurchDoor`) contre ~66 m pour l'autre. ⚠️ C'est aussi le banc du monsieur
  (`manBench0`, hider jour 1 / 10h-18h) : le garde vanilla `ObjectActive` reste actif,
  donc l'attente y est refusée tant qu'il est assis.
- **3 bancs du Parc** (`bench0 (4)/(5)/(6)`) → **3 h** au lieu de 1.
- Tous les autres bancs sont inchangés. Les gardes vanilla (`CanWait1Hour` : nuit,
  dernier jour, espace non-euclidien…) sont respectés.
- **00h05 et pas 00h00, volontairement** : `UpdateTimeOfDay` déclenche la brume Darkness
  dès `currentHour <= 3`, alors que le compteur de jour n'avance QUE sur le tick exact
  `currentHour <= 0 && currentMinute <= 0` — qu'un saut direct à 00h05 franchit sans le
  toucher. La nuit (et la fin Darkness) arrivent donc **sans faire avancer le jour** :
  dormir reste le seul moyen de changer de jour, ce que `can_advance_days` suppose.

**Traps refondus** (mêmes ids 478660301..308, noms changés dans `ids.json`) :
| Ancien nom | Nouveau nom | Effet |
|---|---|---|
| Regrow Hedge Trap | **Garden Reset Trap** | Jardin à 0 % (herbe, taupes, haie, fleurs, déchets) |
| Regrow Molehills Trap | **Church Reset Trap** | Église à 0 % (herbe, taupes, fleurs) |
| Regrow Grass Trap | **Park Reset Trap** | Parc à 0 % (herbe, taupes, fleurs, déchets) |
| Return Trash Trap | **Night Trap** | Heure → 03h00 |
| Rewater Flowers Trap | **Sacred Flower Trap** | Coupe 4 fleurs sacrées (son compris) |

- 🔴 **`Regrow Grass Trap` réhabilité — l'ancien verdict « non implémentable » était FAUX.**
  L'herbe DOTS se reconstruit bien à chaud : `GameManager.ResetWorld` (GameManager.cs:4064)
  fait `GrassManager.ClearEntities()` + `Reset()` + `CornManager.Reset()`, et les coupes du
  save sont rejouées par `performedLoadOperations`/`PerformLoadOperations`
  (GameManager.cs:874) + `GrassSystem` (GrassSystem.cs:483-572) — sans ré-écriture du save
  ni son. `Effects.ResetGrassInArea` emprunte ce chemin exact : purge des positions de la
  zone (les 2 listes `grassCutPosition`/`grassCutRadius` sont **parallèles**), `grassCutCur`
  des **3** zones à 0 (le replay les reconstruit), rebuild, replay. `UNIMPLEMENTED_ITEMS`
  est donc vide et le trap revient dans le pool (8 traps générables au lieu de 7).
- **Sacred Flower Trap** : appelle le vrai `Flower.Cut()` (Flower.cs:580) sur jusqu'à 4
  fleurs de cimetière encore debout ⇒ compteur, son `graveyardFlowerCut` et **seuils
  vanilla** (≥4 warning, ≥5 `ActivateSpookyWorld` → fin SacredFlowers) s'appliquent seuls.
  La spec de Jonath tombe donc pile sur le comportement natif : à ≥1 fleur déjà coupée le
  trap donne la fin tout de suite, à 0 il laisse à 4 et la prochaine coupe la déclenche.
  S'il reste moins de 4 fleurs debout, le compteur est complété (le trap « coûte » toujours 4).
- 🔴 **Le « 0 % » exigeait de lever `maintained<Zone>Area`** : `GetAreaCompletedPercentage`
  (SaveManager.cs:2394-2416) renvoie **100 en dur** dès qu'une zone a été maintenue une
  fois, quels que soient les compteurs — sans ce flag le reset aurait été **cosmétique**.
  Il est donc remis à false. ⚠️ **Conséquence à connaître** : le portail 100 % de la zone
  (Picnic / Plage / Boulangerie, hider `Maintained<Zone>Area`) se **referme** jusqu'au
  re-nettoyage. Rien n'est perdu définitivement (re-nettoyer le rouvre,
  GameManager.cs:6110-6141), mais c'est punitif — dire si tu préfères préserver les
  portails, c'est une ligne à retirer.
- **Les 5 éléments sont remis à zéro dans les 3 zones** (et non la sous-liste par zone) :
  un élément absent d'une zone a déjà un compteur à 0, donc c'est équivalent à ta spec, et
  c'est la seule façon de garantir le 0 % (le % somme TOUS les compteurs, SaveManager.cs:2381).
- **Volontairement NON réinitialisé** : `cutAllGrassInStartGardenArea`, qui garde la prime
  en gulden de fin de tonte (GameManager.cs:3131). La lever paierait la prime à chaque
  re-nettoyage — argent infini, et sous coinsanity le gulden est de la progression.
- Effet de bord assumé : `ClearEntities()` détruit aussi les entités du **maïs**, recréé
  juste après par `CornManager.Reset()` (comme le fait `ResetWorld`) ⇒ le maïs coupé
  repousse, exactement comme après un rechargement de partie.
- **Compatibilité run en cours** : les ids étant inchangés, `GameIds` accepte AUSSI les
  anciens noms (`TrapLegacy*`) — une seed générée avant le renommage reste jouable, avec
  le NOUVEAU comportement du même id.
- Touche de debug F10 (VerboseLogs) : cycle sur les 5 traps monde, nom loggé à chaque appui.
- Build 0 warning, déployé. **Relancer le jeu pour charger la nouvelle DLL.**

### ✅ Option `chore_checks` + calibrage du Golden Gulden (demande Jonath, 2026-07-31)
- **`chore_checks`** (`DefaultOnToggle`, groupe « Goal & Pools ») : active/désactive les
  6 checks de tâches. **Indépendante de coinsanity** — question de Jonath, vérifiée au
  code : `create_all_locations` ajoute les `CHORE_LOCS` et le pool ajoute les Golden Gulden
  hors de tout test coinsanity.
- 🔴 **Piège du double paiement, évité** : avec l'option **OFF**, le client n'intercepte
  rien, donc les tâches paient leurs 2 gulden **comme en vanilla**. Ajouter quand même les
  5 Golden Gulden aurait **doublé** cet argent. Le nombre de coins est donc conditionné à
  l'option (`golden_gulden = PAID_GARDEN_CHORES if chore_checks else 0`), et le retrait des
  10 gulden de la réserve coinsanity suit la même condition.
- Mesure des 4 combinaisons (total d'argent dépensable) :
  | coinsanity | chore_checks | locations | Golden Gulden | Gulden | total |
  |---|---|---|---|---|---|
  | off | on | 114 | 5 | 0 | 10 |
  | off | off | 108 | 0 | 0 | 0 |
  | on | on | 129 | 5 | 26 | **36** |
  | on | off | 123 | 0 | 36 | **36** |
  ⇒ sous coinsanity la bourse vaut **36 dans les deux cas** = la somme exacte des prix.
  Verrouillé par 2 classes de test dédiées.
- **Taille du Golden Gulden ramenée de +25 % à +15 %** (`GoldenGuldenScaleBoost`, retour
  Jonath) : assez pour lire « vaut plus » sans ressembler à un autre objet.
- **57 tests OK** (48 + 9), build 0 warning.
- 📄 **Template YAML officiel régénéré** : `players/Grunn_template.yaml` (via
  `Options.generate_yaml_templates`), 13 options Grunn documentées.

### ✅ Option `exclude_bad_endings` (demande Jonath, 2026-07-30)
- Retire les checks des **8 fins qui TUENT** — exactement le set DeathLink : Mist,
  SacredFlowers, Drown, Darkness, LongHallway, HedgeMaze, WorldEnd, Dog. Ne restent que
  **Bus, Picnic et GoodEnd**. But : ne pas être obligé de mourir — et, sous DeathLink, de
  tuer tout le monde — juste pour un check. Les fins restent atteignables en jeu, elles
  cessent seulement d'être des locations.
- ⚠️ **Ignorée quand `goal = all_endings`** : ce goal exige de voir toutes les fins, retirer
  leurs checks n'aurait aucun sens. Vérifié par un test dédié.
- Liste tracée une seule fois dans `constants.DEATH_ENDINGS`, miroir de
  `GameIds.DeathLinkEndings` côté client (décision Jonath 2026-07-13).
- Côté client, l'option est lue du slot_data uniquement pour la **lisibilité du log** :
  `Silencieux : Ending: X (option exclude_bad_endings)` au lieu de « pas une location ».
  Fonctionnellement, `SendByName` dégradait déjà proprement.
- Mesure : 114 locations → **106** avec l'option (−8 exactement), et 114 si
  `goal = all_endings`. **48 tests OK** (40 + 8 nouveaux, dont 2 classes dédiées à
  cette option).

### ✅ 5 checks « chore » + le Golden Gulden (demande Jonath, 2026-07-30) — à valider in-game
- **Constat de Jonath confirmé au code** : les 5 tâches d'entretien paient **2 gulden**
  (`GameManager.areaCompleteGuldenAdd = 2`) la première fois qu'elles sont finies, et
  **uniquement dans le jardin de départ** — chacune gardée par son propre flag :
  | Tâche | Hook | Flag |
  |---|---|---|
  | Haies | `TrimBall.Trim` (TrimBall.cs:149) | `trimmedAllHedgesInStartGardenArea` |
  | Herbe | `GameManager.CutGrass` (:3131) | `cutAllGrassInStartGardenArea` |
  | Taupinières | `Molehill.Remove` (:184) | `removedAllMolehillsInStartGardenArea` |
  | Fleurs | `Flower.Water` (:529) | `wateredAllFlowersInStartGardenArea` |
  | Déchets | `Troepje.Trigger` (:100) | `clearedAllTrashInStartGardenArea` |
- **Nouvelle catégorie `chore`** (ids **478661601..605**), locations logiques en `Menu`,
  règles dans `rules.CHORE_RULES` — exigences d'outils données par Jonath :
  haies + herbe = `can_cut_grass` (sécateurs OU épée) · taupinières = `Trowel` ·
  fleurs = `can_water` (arrosoir OU pièce bleue) · **déchets = `reach(TOILET)`** (il y a des
  déchets DANS les toilettes ⇒ la clé des toilettes est requise).
- **Nouvel item `Golden Gulden`** (id 478660402, catégorie `filler`) : vaut **2 gulden**,
  affiché avec le **vrai modèle de pièce teinté OR** (`GoldenGuldenTint`, +25 % de taille) —
  volontairement doré et non jaune, pour ne pas être confondu avec la carte AP filler jaune.
- **L'économie coinsanity est inchangée par construction** : les 5 tâches ne versent plus
  leurs 10 gulden d'elles-mêmes, donc `create_all_items` retire exactement
  `5 × 2 = 10` gulden de la réserve calculée et ajoute **5 Golden Gulden** à la place. Ils
  sont mélangés au multiworld comme n'importe quel item. `ReinjectInventory` les recompte
  pour 2 — sans ça un reset de run rétrécirait silencieusement la bourse.
- **Les patches lisent le FLAG avant/après l'appel vanilla** (`ChorePatches.cs`) plutôt que
  de redériver la condition : le check part exactement quand le jeu décide que la tâche est
  finie. ⚠️ Nuance relevée au passage : pour l'HERBE, la garde vanilla ne teste PAS
  `myArea == StartGarden` (contrairement aux 4 autres) — elle ne regarde que le flag, donc
  le bonus tombe sur la **première zone terminée**, quelle qu'elle soit. Bizarrerie vanilla
  conservée telle quelle, justement parce qu'on lit le flag.
- **6ᵉ chore : les plantes en pot** (localisations données par Jonath 2026-07-30, 8 pots
  exactement — cohérent avec `trimmedPottedPlantMax = 8`) : 1 cabane du joueur (**jour 2
  uniquement**) · 1 toilettes · 2 station essence · 2 bureau de la station · 2 bureau du
  manoir. Taille aux sécateurs OU à l'épée. Il faut TOUS les tailler ⇒ la règle est
  l'**intersection** des accès : `can_cut_grass` + `CabaneJoueur` + `can_advance_days`
  (jour 2) + `Toilet` + `GasStation` + `GasOffice` + `Manoir`.
  Hook à part des 5 autres : vanilla n'a **aucun flag booléen** ici, seulement le compteur
  `pottedPlantTrimmedCur` comparé à `trimmedPottedPlantMax` — le patch surveille donc le
  **franchissement du seuil** (`PottedPlant.Trigger`, PottedPlant.cs:73).
- 🔴 **Piège évité** : les plantes en pot ne paient **AUCUN gulden** en vanilla (achievement
  seul). Compter les Golden Gulden sur la catégorie `chore` (6) au lieu des 5 tâches
  payantes aurait **créé 2 gulden ex nihilo** et gonflé l'économie coinsanity. D'où
  `items.PAID_GARDEN_CHORES = 5`, explicitement découplé du nombre de locations.
- 48 tests OK, 10 générations de contrôle (2 échecs = le flake `lock_player_hut`
  préexistant), build 0 warning.

### 🔴 Beaucoup de sons ne sont plus joués (retour Jonath, 2026-07-30) — fix livré, cause à CONFIRMER par le log
- **Cause identifiée au code** : `ModelSwap.StripNonVisuals` ne retirait que les
  `MonoBehaviour` et les `Collider`. **`AudioSource` n'est PAS un MonoBehaviour**, il
  survivait donc à chaque clone — et `SwapVisual` réactive ensuite le clone, avec un
  commentaire qui affirmait « only renderers/meshes remain: nothing to awake », **ce qui
  était faux** (les `Light` y sont manipulées deux lignes plus haut !).
  Un modèle source portant un `AudioSource` (playOnAwake / loop) était donc dupliqué sur
  chaque check swappé, et **une couronne multiplie ça par ses SIX pétales**. Unity ne mixe
  qu'un nombre limité de voix réelles (32 par défaut) : au-delà, les nouveaux sons sont
  **écartés par priorité** = « beaucoup de sons ne sont pas joués ». Le passage aux
  couronnes a très probablement fait franchir le seuil (avant : 1 fragment par check buff).
- **Fix** : `StripNonVisuals` détruit désormais aussi les `AudioSource` — un clone décoratif
  doit être SILENCIEUX. Et dans une couronne, la `Light` du fragment n'est gardée que sur le
  **premier pétale** (6 lumières temps réel par check, pour rien).
- ✅ **RÉSOLU en jeu** [J 2026-07-30 : « je n'ai plus le problème »]. Mesure du log
  (23:27:03, seed multiworld DeathLink) :
  `Budget audio : 4 AudioSource retirees des clones ; scene = 68 sources dont 16 en lecture ;
  budget Unity = 32 voix reelles.` — plus de saturation (16/32).
- ⚠️ **Nuance honnête sur le diagnostic : mon ORDRE DE GRANDEUR était faux.** Ce ne sont pas
  des centaines d'`AudioSource` clonées mais **4 modèles SOURCES** qui en portaient une — le
  nettoyage a lieu dans `Archive`, donc une fois par modèle archivé, pas par clone posé. La
  multiplication réelle se faisait ensuite à l'instanciation : la même session compte
  **38 AP models** et une couronne = 6 pétales, donc si le fragment d'âme fait partie de ces
  4 (très probable, c'est la source des couronnes), cela donnait ~**228 sources sonores
  ajoutées** contre un budget de 32 voix. Le mécanisme est donc bien réel et l'échelle
  cohérente avec le symptôme.
- **Ce qui reste non prouvé** : il n'existe aucune mesure d'AVANT le fix (le diagnostic a été
  ajouté en même temps que la correction), donc la preuve est une **forte corrélation**
  (symptôme disparu exactement au moment où ces sources ont été retirées), pas une mesure
  comparative. Suffisant en pratique ; à ne pas présenter comme une démonstration.

### 🌍 MULTIWORLD #2 : Grunn × Nonopelagram avec DEATHLINK (2026-07-30)
- **Seed livrée** : `dist/Grunn1_mw_nono_dl_s85103302899301734198.archipelago` (+ spoiler),
  `serve.bat` repointé, smoke-test OK. **32 items croisés dans chaque sens.**
- **DeathLink actif des deux côtés**, vérifié dans le spoiler : Grunn `Death Link: Yes`,
  Nonopelagram `Death Link: **On**` (et non `Damage` — sur `on` le joueur perd toutes ses
  vies et sa propre mort renvoie un DeathLink, cf. la docstring de l'option).
- Rappel du comportement côté Grunn (décision Jonath 2026-07-13) : **toutes les fins sauf
  Bus / Picnic / GoodEnd** comptent comme une mort et envoient un DeathLink
  (`GameIds.DeathLinkEndings`) ; un DeathLink REÇU ne déclenche jamais de fin — il reset la
  run après le screamer, donc pas de boucle possible entre les deux jeux.
- ✅ **Aperçu des 3 couronnes RETIRÉ** (validé par Jonath) : la région temporaire
  `SpawnCrownPreview` + son appel sont supprimés de `ModelSwap.cs`, zéro résidu. Les
  couronnes ne s'affichent plus que là où elles ont un sens : sur les vrais checks contenant
  un item d'un autre monde.

### 🌍 PREMIER MULTIWORLD : Grunn × Nonopelagram (2026-07-30)
- **Seed** : `dist/Grunn1_mw_nono_s82909862181621345190.archipelago` (+ son spoiler),
  `serve.bat` repointé, smoke-test serveur OK (les 2 data packages se chargent, 0 exception,
  bind `0.0.0.0:38281`). Seed affichée `9175576942605510480`, **167 locations** au total.
  Nom unique conforme à la règle anti-contamination d'apsave.
- **Slots** : `Grunn1` (Grunn, 123 loc — true_ending + coinsanity + keep_shears +
  lock_player_hut, comme la dernière run solo) et `Nonogram` (Nonopelagram v1.0.2, 44 loc).
  YAML versionnés dans `players/multiworld_test/`.
- **Les échanges d'items sont réels** — mesuré sur le spoiler : **34 items Nonopelagram
  placés dans Grunn** (ce sont eux qui afficheront les couronnes AP) et **34 items Grunn
  placés dans Nonopelagram** (dont les traps renommés : « Sacred Flower Trap », « Inverted
  Controls Trap » — le renommage voyage donc bien dans le datapackage).
- 🔴 **Piège rencontré : deux versions de l'apworld Nonopelagram cohabitent.**
  `Desktop/Archipelago/APWorld/nonopelagram.apworld` (6,8 Ko) est **PÉRIMÉ** — il déclare
  `CoinsPerBundle(Range)` alors que le YAML de Jonath passe `coins_per_bundle: normal`, d'où
  `AttributeError: type object 'CoinsPerBundle' has no attribute '_RANDOM_OPTS'` à la
  génération. La version à jour est celle des sources,
  `Desktop/Archipelago/Jeux/Nonogram/nonogram-archipelago/apworld/nonopelagram.apworld`
  (15,3 Ko, 27/07, `CoinsPerBundle(Choice)` avec `option_normal`). C'est elle qui est
  installée dans `Archipelago/custom_worlds/` de CE checkout.
  ⚠️ **Le dossier `APWorld/` de Jonath n'a PAS été touché** — à lui de décider s'il veut y
  remplacer la vieille version (elle cassera toute génération utilisant ses YAML récents).
- Avertissement bénin au chargement : « Invalid or missing manifest file … stop working with
  Archipelago 0.7.0 » — l'apworld n'embarque pas d'`archipelago.json`. Sans effet en 0.6.7,
  à corriger côté projet Nonogram avant 0.7.0.

### 🎨 Modèles multiworld : les « couronnes AP » (idée Jonath, 2026-07-30) — à calibrer in-game
- **Idée de Jonath** : assembler des **fragments d'âme en couronne**, en forme du logo
  Archipelago (une fleur à 5 pétales). Faisable directement : le fragment est déjà archivé
  dans la librairie (c'est le modèle des buffs), donc une couronne = N copies posées sur un
  cercle sous un même holder, archivée comme n'importe quelle autre source.
- **Recherche d'assets faite avant de coder** : il n'existe **aucun asset 3D Archipelago**
  réutilisable. Le projet AP ne distribue que du 2D (logo, icônes web) — zéro `.obj/.fbx/
  .glb/.blend` dans le repo — et les mods 3D existants (Outer Wilds d'Ixrec, TUNIC,
  ULTRAKILL) n'ont pas de base commune, chacun fabrique la sienne.
- **Spec arrêtée par Jonath (2026-07-30)** : les 3 couronnes ont **le même nombre de pétales,
  6**, disposés comme sur le logo ; seule la **couleur** les distingue.
  | Classe | Couleur |
  |---|---|
  | Filler | **gris terne** (et volontairement MAT : émission coupée, halo du fragment réduit à 35 %) |
  | Useful | **vert** |
  | Progression | **multicolore** — les 6 teintes du logo, une par pétale |
  ⚠️ *Correction de CC* : j'avais compté **5** pétales sur le logo, c'était faux — il y en a
  **6**. Le compte vient maintenant d'une seule constante `CrownPetalCount`.
- La couleur est **cuite pétale par pétale à la construction** (`Paint`), et le swap passe
  donc `tint = null` : une teinte unique passée à `SwapVisual` écraserait les 6 couleurs de
  la progression. `Paint` couvre les mêmes canaux que `SwapVisual` (couleur, `_BaseColor`,
  émission, et la `Light` que porte le fragment) pour rester lisible de nuit.
- Les traps continuent d'être déguisés en l'une des 3 classes, de façon déterministe par
  seed+location (`KindFor`) — inchangé.
- **Rayon MESURÉ, pas deviné** : `WorldSize()` lit les `sharedMesh.bounds` × la `lossyScale`
  bakée (⚠️ `Renderer.bounds` est inutilisable ici — l'archive est inactive, donc jamais
  rendue, et ses bounds monde restent périmées). Le rayon vaut `largeur du pétale ×
  CrownRadiusFactor`, car l'échelle du fragment change d'une session à l'autre selon l'endroit
  où la scène l'expose.
- **Couronne VERTICALE par défaut** (`CrownUpright = true`) : un logo se lit **de face** quand
  on marche vers le pickup ; à plat il ne se lirait que vu du dessus. Basculer la constante
  pour comparer en jeu.
- Constantes à calibrer sur capture : `CrownRadiusFactor` (0.62), `CrownPetalTilt` (20°),
  `ApCrownScale` (1.1), `CrownUpright`, et le compte de pétales par classe (`CrownPetals`).
  Le log donne la **taille rendue mesurée** de chaque couronne pour calibrer sans tâtonner.
- **Repli sûr** : si aucun fragment d'âme n'a pu être récolté dans la session, on retombe sur
  l'ancienne carte AP teintée (le polaroïd). Aucun changement de comportement par ailleurs.
- ⚠️ **TEMPORAIRE À RETIRER — aperçu des 3 couronnes devant le bus** (demande Jonath) :
  `ModelSwap.SpawnCrownPreview` pose les 3 modèles au sol à partir de
  **(-35.2, 10.9, -69.0)**, espacés de (1.3, 0, -0.9), dans le prolongement des cadeaux Os
  (-37.3, -67.5) et Boussole (-38.6, -66.6). **Purement décoratif** : les copies archivées
  sont déjà passées par `StripNonVisuals` (ni script ni collider) et ne portent **aucun
  `ItemPickup`** ⇒ rien à ramasser, aucun check possible. Réveil récursif de l'arbre avant
  `SetActive` — sans ça l'aperçu peut naître invisible (le piège exact de l'ancienne sonde
  poisson). Retrait : `CrownPreviewEnabled = false` ou suppression de la région.
- ✅ **VALIDÉ VISUELLEMENT** [J 2026-07-30, capture] : « les models sont OK ». Les trois se
  lisent bien côte à côte (multicolore / vert / gris pâle). Rien à recalibrer sur le rayon,
  le tilt ni l'échelle.
- ℹ️ **« On ne peut pas interagir avec eux »** — c'est **voulu** pour l'aperçu : ce sont des
  décors sans `ItemPickup` ni collider (`StripNonVisuals`), justement pour qu'aucun check ne
  puisse partir d'un objet de test. Les VRAIS checks, eux, restent interactifs : `SwapVisual`
  ne touche que les *renderers* du pickup, jamais son collider ni son `Interaction`.

### 🔴 Cadeau StrangeKey bien trop gros + prompt décalé (retour Jonath, 2026-07-30) — CORRIGÉ
- **Symptôme** : la clé étrange posée au panneau des roses est énorme (visible sur la capture
  des couronnes), et son option de ramassage apparaît décalée sur le côté.
- **Cause** : `GiftPickups.Spawn` fait `Instantiate(template, position, rotation)` **sans
  parent**. Unity conserve alors la `localScale` du template mais **perd l'échelle de ses
  parents** — un template vivant sous un parent réduit spawne donc GÉANT. C'est le cas des
  pickups de StrangeKey (`strangeKey0_old` au bureau du manoir / `strangeKey0_demo`, enfant
  de `magpieDeadByWorm0`). L'Os et la Boussole n'avaient rien montré parce que leur chaîne
  d'échelle est neutre.
  Le prompt décalé en découle : le collider grossit avec l'objet, donc le point d'ancrage de
  l'interaction se déplace.
- **Fix** : le clone n'ayant pas de parent, sa `localScale` EST sa `lossyScale` ⇒ on lui
  copie la `lossyScale` du template, et le cadeau rend exactement à la taille que l'item a
  réellement en jeu. Log de mesure ajouté (ancienne → nouvelle échelle) ; no-op pour l'Os et
  la Boussole.
- **Leçon (déjà payée une fois)** : c'est exactement le problème résolu dans
  `ModelSwap.SwapVisual` — « la taille du modèle dépend de sa position dans le monde »
  (retour Jonath iter 4) — mais la normalisation n'avait **jamais été appliquée à
  `GiftPickups.Spawn`**. Vérifier ce réflexe partout où on `Instantiate` un objet de scène
  hors de sa hiérarchie.
- Build 0 warning, DLL déployée.

### ✅ 7 nouveaux checks « deed » livrés (demande Jonath, 2026-07-28) — à valider in-game
- Nouvelle catégorie **`deed`** dans `ids.json` (bloc **478661501..507**, `locationCounts.deed = 7`) :
  des checks récompensant une **ACTION** et non un ramassage. Locations **logiques** (région
  `Menu` + règle explicite, comme les `Obtain X` et les fins), règles dans `rules.DEED_RULES`,
  patches client dans le nouveau `Grunnchipelago.Client/DeedPatches.cs`.
- Chaque hook est la **méthode dédiée du jeu**, chacune gardée par son propre flag
  `ProgressData` ⇒ postfix qui ne peut pas tirer deux fois (+ dédup `TrySend`). Même motif que
  `MagicPondPlaceFishPatch`. Détail complet et traçabilité : `design/backlog_checks.md`.
  | Check | Hook | Règle |
  |---|---|---|
  | `Deed: Throw Away PizzaBox` | `GameManager.ClearPizzaBox` (4346) | `Exterieur + PizzaBox` |
  | `Deed: Place PrettyFlower in Vase` | `GameManager.PutPrettyFlowerInVase` (6058) | `Eglise + PrettyFlower` |
  | `Deed: Complete the School Band` | `GameManager.KidReadyTrigger` (6009), gaté sur `schoolbandCompleteIndex >= 3` | `Parc + KidTrumpet + KidCymbals + KidTriangle` |
  | `Deed: Place GoldFishAlive in Fishbowl` | `Fishbowl.PlaceFishAlive` (79) | `PassageGnomes + GoldFishAlive` |
  | `Deed: Return Worm to the Worm Hill` | `GameManager.ReturnWorm` (5887) | `Hell + Worm` |
  | `Deed: Award Medal to the Snail` | `Snail.Award` (177) | `PillarSpace + Medal + can_advance_days` |
  | `Deed: Return SeveredHand` | `GameManager.ReturnSeveredHand` (4242) | `Bunker + SeveredHand` |
- **Trois corrections apportées par Jonath sur mes relevés au dump** :
  1. **Ver** : la butte est géographiquement dans le coin de l'Église (dump :
     `ChurchArea/Hide_ChurchCorner/WormHillContainer`) mais **accessible uniquement depuis
     Hell** ⇒ `reach(HELL)`, pas `reach(EGLISE)`. Le dump seul m'aurait fait écrire la
     mauvaise règle (écart énorme : sphère précoce vs fin de partie).
  2. **Escargot** : le seul prevent du dump est `SnailStateIsRacing` ; Jonath précise que la
     course finit **vers 23h45 le jour 2** ⇒ il faut pouvoir dormir ⇒ **`can_advance_days`**
     ajouté (décisif sous `lock_player_hut`).
  3. **Plantes en pot** : critère connu (`pottedPlantTrimmedCur >= trimmedPottedPlantMax = 8`,
     SaveManager.cs:1350) mais les pots sont éparpillés et absents du dump ⇒ **reporté
     post-launch** (id 478661508 réservé).
- 🔴 **8 items passent en `progression`** : `PizzaBox`, `PrettyFlower`, `KidTrumpet`,
  `KidCymbals`, `KidTriangle`, `GoldFishAlive`, `Medal`, `SeveredHand`. Sans ça `state.has()`
  ne les voit pas et les deeds sont injouables en logique — attrapé du premier coup par
  `test_all_state_can_reach_everything` (54 échecs). Ils pèsent désormais sur les sphères.
- Parité tenue (+7 locations ⇒ +7 fillers via `create_filler`). **Aucune option** ne gouverne
  ces checks (toujours actifs, comme les `Obtain X` et les fins) — dire si une option
  `deed_checks` est souhaitée. **Les seeds antérieures sont périmées** ; le client dégrade
  proprement (`SendByName` logue « pas une location » et n'envoie rien).
- 40 tests OK, build 0 warning, DLL déployée.

### ✅ TRUE END ATTEINTE — seed `hut_s08873442762073113111` bouclée à 100 % (2026-07-28)
- Confirmé **côté serveur** (`dist/Grunn1_hut_s08873442762073113111.apsave`, l'autorité) :
  `client_game_state = {(0,1): 30}` = `ClientStatus.CLIENT_GOAL`, et
  `location_checks = 116` sur 116 ⇒ **toutes** les locations ont été checkées.
- Côté client : `Check envoye : Ending: GoodEnd` puis `Goal achieved!` (00:24:29), sans
  `SetGoalAchieved failed`. Session connectée en `goal=1` (true_ending), donc `CheckGoal`
  exigeait bien `GoodEnd` **ET** `restoredOwnerSoul` (les 3 fragments consommés).
- 🟢 **`Polaroid: Tent` fait partie des 116 checks ⇒ Tent est VIVANT, prouvé.** Le dernier
  suspect de la liste des polaroïds morts est levé : Demon, VoidSkeleton et GardenGnomes
  restent les 3 seuls contenus morts, et il n'y a plus rien à surveiller.

### Nettoyage `[DEMON-HUNT]` — SUPPRIMÉ (feu vert Jonath, 2026-07-28)
- `Grunnchipelago.Client/DemonHunt.cs` **supprimé en entier** + l'appel `DemonHunt.Tick()`
  retiré de `Plugin.Update`. Aucun résidu (`DemonHunt` / `DEMON-HUNT` / `DIAG-POLAROID`
  = 0 occurrence dans le client). Build 0 warning.
- Mesure qui a motivé la suppression : **7 910 lignes sur 8 795 = 90 % du log de session**
  (fichier à 2,2 Mo). Le garde `sceneDumpDone` se ré-arme à chaque `StartScenario`, or le
  jeu rappelle `StartScenario(Hell)` en boucle ⇒ dump des 31 polaroïds **plusieurs fois par
  seconde** dans le Crypt. Mission accomplie sur les 3 fronts (Demon, VoidSkeleton,
  GardenGnomes) + Tent tranché par la run complète.

### 🔴 Passage des gnomes : le constat du prompt (§4.3) était FAUX — l'erreur était ailleurs
- Le prompt annonçait `regions.py` trop STRICT (« le code accepte `Hammer|MagicSword|Trowel` »).
  **Vérification au code : faux.** `Gnome.GetHit` (Gnome.cs:182) ouvre sur
  `if (curEquipmentData.handRightItem != Item.Hammer || curState == Hide) return;`, et
  `GameManager.DestroyGnome()` n'a **que cette méthode comme appelant**. Les deux routes vers
  `destroyedAllJumpscareGnomes` (jumpscare via `AmbienceManager.GetAmbienceArea:585` =
  `destroyedGnome` + entrer dans la zone Station ; ou casser les 5 nains via
  `DestroyedAllJumpscareGnomes`) passent donc **toutes deux par le MARTEAU**.
  ⇒ La règle `Hammer` des arêtes `JARDIN <-> PASSAGE_GNOMES <-> PARC` est **correcte**,
  rien à corriger. [Confirmé par Jonath 2026-07-28 : « seul le marteau permet de détruire
  le gnome ».]
- ⚠️ **En revanche deux règles étaient trop PERMISSIVES** (le sens dangereux) — corrigées :
  | Règle | Avant | Après |
  |---|---|---|
  | `OBTAIN_RULES["GnomeIdol"]` | `has_any(Hammer, MagicSword, Trowel)` | **`has("Hammer")`** |
  | `Polaroid: GasStation` | `has_any(Hammer, MagicSword, Trowel)` | **`has("Hammer")`** |
  Le `GnomeIdol` est une des 4 idoles **requises** pour Hell : une seed pouvait le croire
  atteignable à la seule Truelle et placer de la progression derrière ⇒ **seed bloquée**.
  40 tests OK. ⚠️ Impose une **régénération** pour toute future seed (les anciennes gardent
  leur fill, calculé avec la règle laxiste).
- 📊 Effet mesuré sur le flake `TestLockPlayerHut.test_fill` : **4/20** après durcissement
  contre 2/12 avant — même ordre de grandeur, mais resserrer une idole requise ne peut
  qu'aggraver marginalement un fill déjà tendu. Le flake reste à traiter séparément.
- ✅ **`can_get_bone` : VÉRIFIÉ, aucun changement — ma fausse alerte, tranchée par Jonath.**
  J'avais signalé la règle comme peut-être trop stricte en m'appuyant sur les
  `preventTypes = []` des 5 pickups d'os du dump. **Faux raisonnement.** Retour Jonath
  (2026-07-28) : l'os est libre **uniquement au Manoir** (`hallwayTable0/bone0`) ; les 4
  autres sont des **squelettes à casser** avec Marteau/Épée/Truelle. Le dump ne pouvait pas
  le montrer : le gate n'est pas une condition d'interaction mais la mécanique de **frappe**
  — `SkeletonBone.Smash` n'a qu'un appelant, `HitReceiver` (HitReceiver.cs:110), alimenté
  par la hurtbox de l'arme, donc il faut un outil qu'on balance.
  ⇒ La règle actuelle `has_any(Hammer, MagicSword, Trowel) OR reach(MANOIR)` modélise
  **exactement** ça (Manoir ⇒ libre ; squelette ⇒ outil), elle est juste. Commentaire de
  traçabilité ajouté dans `rules.py` avec un « ne pas simplifier sur la foi du dump seul ».
  **Leçon** : `preventTypes` vide ne veut pas dire « libre » — un objet peut être gaté par
  une mécanique (frappe, hider séparé) que le dump ne capture pas. Même piège que le worm
  (masqué par un objet séparé, pas par `visualsObject`).

### Ajustements après test in-game (Jonath, 2026-07-27) — validés côté build
- **Gouttière le jour de brume** : le 3ᵉ jour (lundi = `progressDataCheck.dayIndex >= 3`,
  = `GameManager.IsFinalDay`, le jour où `EnterSleeping` lance `ScenarioType.MistDay`),
  la gouttière et le banc de l'église visent **11h15** au lieu de 00h05. Choix d'heure tracé
  au code : `HandleMist` ignore tout avant **10h** (GameManager.cs:2380), la brume culmine à
  `mistHour = 12h` et la fin est **forcée à `mistHour + 2` = 14h** (GameManager.cs:2384) —
  11h15 est donc dans la fenêtre active sans sauter la séquence. Jamais de retour en
  arrière : déjà passé 11h15, on avance d'1 h.
  ⚠️ `GameManager.CanWait1Hour` **refuse tout le jour final** (GameManager.cs:6352) : le
  garde de la gouttière reproduit donc les autres tests un par un SAUF celui-là. Le **banc**
  de l'église, lui, passe par `Wait1Hour` vanilla ⇒ il reste inopérant le jour 3. La
  gouttière est l'outil du jour de brume.
- 🔴 **Popup trompeur au Magic Pond** : `RetrieveFishFromMagicPond` (GameManager.cs:6085)
  annonce le poisson **trois** fois. `TriggerItemObtainPopup` était déjà supprimé et
  `ObtainKeyItem` déjà intercepté, mais la 3ᵉ ligne construit la chaîne
  « obtenu poisson rouge en vie » **à la main** et appelle `UIManager.AddPopup`
  (GameManager.cs:6095) — hors de tout garde. D'où le message vanilla alors que Jonath
  recevait bien son buff/trap. Fix : `MagicPondRetrieveFishPatch` pose
  `SuppressVanillaPopup` le temps de l'appel (le check est parti au DÉPÔT, donc le retrait
  doit rester muet). Sons vanilla intacts.
- 🔴 **Modèle décalé au Magic Pond** : `SwapVisual` parente le clone à `localPosition = 0`,
  or `MagicPond_FishAlive_Content` est un **conteneur** dont le mesh du poisson est à un
  décalage local (dump : hider à x=6500, interactions du bassin à x=6503,83 ⇒ ~3,8 m).
  Le modèle atterrissait donc sur la berge. **Même cause racine que le bug de récolte du
  2026-07-21, en miroir** : on cible désormais l'objet **porteur du mesh**
  (`FindMeshHolder`), et on masque le reste du conteneur — mais **seulement après un swap
  réussi**, sinon un swap no-op (le check contient vraiment le poisson) rendait le poisson
  vanilla invisible. Log de mesure ajouté (positions conteneur/mesh + écart) pour vérifier
  en jeu.

### 🔴🔴 BUG TROUVÉ (Jonath, non corrigé — en attente d'arbitrage) : StrangeKey tue `Ending: LongHallway`
- **Symptôme** : clé étrange en poche ⇒ l'assaillant du couloir final n'apparaît plus,
  la fin est impossible.
- **Cause racine, 100 % vanilla** (`Door.cs`) : le petit démon n'est déclenché que par
  `if (locked && _triggeredByPlayerInteraction && triggerSmallManInLongHallway)`
  (Door.cs:770-773). Or, dans le MÊME appel `Trigger`, le bloc `if (locked || flag)`
  (Door.cs:644) appelle `Unlock()` dès que `PlayerHasUnlockItem()` — qui teste la simple
  **POSSESSION** (`ObtainedKeyItem`, Door.cs:910-923). Donc la porte se déverrouille,
  `locked` passe à false, et le test du démon en fin de méthode est faux : **le démon n'est
  jamais armé**. Encore la famille « la possession tue le contenu », cette fois sur une FIN.
- **Portée AP** : `triggeredSmallManInLongHallway` est en ProgressData (par run), mais la
  possession de la clé, elle, est définitive ⇒ le check est **perdu pour toute la partie**
  dès réception de la StrangeKey. Si un item de progression y est placé (ou en goal
  `all_endings`), la seed est **bloquée**. La règle `Ending: LongHallway = reach(CouloirFinal)`
  ne modélise pas ce piège d'ORDRE.
- ✅ **CORRIGÉ — choix Jonath : le pattern « item cadeau » de l'Os et de la Boussole**
  (plutôt que le patch `Door.Trigger` que CC proposait). La `StrangeKey` reçue n'est plus
  jamais injectée en inventaire (`ApClient.GrantItem` sort tôt, donc `ReinjectInventory`
  ne la remet pas non plus à chaque run) : un pickup monde
  `grunnchipelago_strangeKeyGift` apparaît au panneau des roses, à
  **(-36.0, 10.35, -68.4)**, dans la continuité de l'Os (-37.3, -67.5) et de la Boussole
  (-38.6, -66.6), à l'écart du massif RedRoses qui avalait l'os. Le joueur ne prend la clé
  que quand il VEUT ouvrir la porte ⇒ la fin LongHallway reste atteignable, et la clé
  disparaît de l'inventaire à chaque nouvelle run (possession = ProgressData per-run).
  Comme pour les deux autres : aucun check envoyé au ramassage
  (`ItemPickupTriggerPatch.SpecialPickupActive`), et le cadeau est exclu de l'override
  visibilité=état-du-check (`PickupVisibility.AppliesTo`, préfixe `grunnchipelago`).
  Popup à la réception : « Une clé étrange attend près du panneau des roses... ».
  Logique apworld **inchangée** (`has("StrangeKey")` reste vrai à la réception, le cadeau
  est librement atteignable au spawn — même raisonnement que Bone/Compass).

### ✅ Flake de génération : ÉLUCIDÉ et RÉSOLU (2026-07-31) — la cause n'était pas celle annoncée
> **Correction d'une conclusion erronée de CC.** J'avais annoncé « le fill échoue 20–30 % du
> temps avec `lock_player_hut` ON par défaut, inacceptable pour publier » et proposé de
> sacrifier l'intérêt multiworld de l'AbandonedKey pour y remédier. **Faux sur les deux
> points.** Jonath : « tu pars du principe que les gens vont jouer solo. En général on joue à
> plusieurs en Archipelago (c'est le but quand même !) ». Mesure faite avec de VRAIES
> générations (`Generate.py`), et non plus avec des configurations de test solo :
>
> | Configuration | Échecs / 20 |
> |---|---|
> | Solo, défauts | **0** |
> | **Duo Grunn + Nonopelagram, défauts** | **0** |
> | Solo, `exclude_bridge_key: false` | **4** |
> | **Duo Grunn + Nonopelagram, `exclude_bridge_key: false`** | **0** |
> | Solo, `exclude_bridge_key: false` + coinsanity | **0** |
>
> ⇒ **La cause n'est pas `lock_player_hut` mais `exclude_bridge_key: false`** : la BridgeKey
> passe dans le pool, la sphère de départ tombe à UNE location (`Obtain BridgeKey`), et le
> fill solo s'y coince. Exactement ce que la docstring de l'option décrit déjà et
> **déconseille**. Et le multiworld — le cas d'usage normal — l'absorbe totalement : les
> locations des autres joueurs redonnent au fill l'espace qui lui manque.
> ⇒ **Rien à corriger dans les défauts.** Aucune concession de design nécessaire.
>
> Piste `multiworld.early_items` : **testée et ANNULÉE**, elle EMPIRAIT tout (20/20 d'échec).
> Archipelago l'explique : « Ran out of early locations for early items » — cette mécanique
> exige des locations atteignables à état VIDE, or tout Grunn est derrière la BridgeKey,
> elle-même verrouillée sur sa propre location. Note laissée dans `__init__.py`.
>
> **Leçon** : ne pas extrapoler un taux d'échec depuis des configurations de test solo
> volontairement adverses. Mesurer le cas d'usage réel — ici, le multiworld.

### 🔴 VRAIE cause du flake : `Polaroid: HighBridgeKey` mal placé (Jonath, 2026-07-31) — CORRIGÉ
- **Info de Jonath** : la sphère de départ (avant le pont haut) contient **3 checks** —
  `Obtain BridgeKey`, **le polaroïd de la clé du pont** (contre la porte fermée) et le
  gulden du bus (coinsanity). Or `locations.py` classait `Polaroid: HighBridgeKey` en
  **`EXTERIEUR`**, c'est-à-dire de l'autre côté du pont.
- **Confirmé au dump** : `polaroid_highBridgeKey0` en **(-38.12, 10.17, -48.4)** contre
  `highBridgeDoor0` en **(-39.98, 11.76, -46.39)** ⇒ **2,7 m**, côté route/spawn. Aucun
  item requis. ⇒ région **`MENU`**.
- **Effet mesuré** : le cas adverse (solo + `exclude_bridge_key: false`) passe de **4/20 à
  0/20 échecs**. La sphère de départ n'était pas « structurellement étroite » — il y
  manquait simplement une location à cause d'une erreur de classement.
- ⇒ Le contournement `coinsanity` ajouté aux 3 classes de test a été **retiré** : inutile
  une fois la vraie cause corrigée. Suite complète : **0 échec sur 20 exécutions** (57 tests).
- **Info connexe de Jonath, vérifiée au code, aucun changement** : `Ending: Darkness` est
  impossible avant le pont, car le temps ne s'écoule qu'une fois le jardin atteint. Tracé :
  `progressDataCheck.allowTimePass`, posé par `GameManager.AllowTimePass()` depuis
  `SetMadeProgress` (jardinage, GameManager.cs:3041), `EnteredStartHouse` (:4185), le
  **premier outil** obtenu (PlayerManager.cs:236-241) ou `Read.cs:252`. Tous ces
  déclencheurs sont dans le jardin ⇒ la règle actuelle `Ending: Darkness = reach(JARDIN)`
  est déjà correcte.
- **Leçon (bis)** : le flake n'était pas un problème d'algorithme de fill mais un **bug de
  logique**. Chercher la règle fausse avant de contourner le générateur.

### Spoiler de la seed en cours `hut_s08873442762073113111` (2026-07-27, run live)
- `dist/hell_access_spoiler_hut.md` **régénéré** pour la seed réellement servie
  (`serve.bat` ligne 7), 116 locations, seed affichée `66373763042940781548`.
  L'ancien contenu (seed `s76554098605004193370`, 118 loc) était périmé.
- Points saillants de cette seed :
  - **`TallIdol` est sur `Gulden #5 (Road)` = le gulden DANS LE BUS au spawn** (sphère 1).
  - **Chaîne `AbandonedKey` très courte** : `Obtain GardenKey` (buanderie, libre) → portail
    du jardin → Église → `Polaroid: BoatPaddle` → `AbandonedKey` ⇒ cabanon + sommeil.
  - **Sortie du jardin = le pont** via `OldPlank`, placé sur `Ending: Darkness` (rester
    dehors après minuit sans dormir) — les Sécateurs sont enfermés dans le cabanon.
  - **Aucun des 11 items requis n'est dans Hell** ⇒ Hell ouvert = vraie fin immédiate.
    `Obtain AtticKey` non requis (l'`AtticKey` est sur `Obtain Doorknob`).
  - Goulot réel : la **Trompette** (`Ending: Picnic`, exige Jardin 100 % donc la Truelle
    du cabanon) gate les 2 fantômes qui portent `SoulFragment1` et `MagicSword`.
- Aucune régénération, aucun changement de code : livrable purement documentaire.

## Session coinsanity + spoiler Hell (CC, 2026-07-21) — à valider in-game

Session de test ciblée (pas de dev de fond). Livrables produits :

### Livrable 1.1 — Seed coinsanity de test
- Générée : `goal=true_ending`, **coinsanity=on**, exclude_bridge_key=on,
  **death_link=off** (choix Jonath) ; reste aux **défauts réels** de `options.py`
  (keep_shears=off, persistent_shortcuts=off — l'ancien `Grunn1.yaml` les forçait).
- Fichiers : `dist/Grunn1_coinsanity.archipelago` + `dist/Grunn1_coinsanity_spoiler.txt`.
  Seed AP `56298642647241916426`. **Slot `Grunn1`, port `38281`.**
- `serve.bat` repointé sur cette seed. Smoke-test serveur OK (chargement propre,
  bind `0.0.0.0:38281`, 0 exception).
- Config mod : `[Connection] Slot = Grunn1` (Host/Port déjà au défaut).

### Livrable 1.2 — Spoiler chemin Hell → `dist/hell_access_spoiler_coinsanity.md`
- **Seed très « ouverte »** : chemin critique en **4 sphères**, toute la progression
  front-load dans **Jardin / Extérieur / Église / Ferry / Zone vélo**.
- **Court-circuits confirmés** (item requis placé hors de sa chaîne vanilla) :
  `ChurchKey` sur `Obtain Doorknob` (pas la chaîne du disque/Manoir) ; `FlowerGem`
  sur `Obtain PurifiedStone` (pas l'arrosage des 4 zones) ; `MagicSword` sur
  `Gulden #2` (pas le grenier). ⇒ **`AtticKey` NON requis** cette seed (il est sur
  `Calm Ghost #5 (Void)`). Manoir / disque / labyrinthe / fanfare / achats hors chemin.
- **FLAGs à valider** : (a) fragments d'âme ramassés **hors Hell** (SF2/SF3 Jardin,
  SF1 Extérieur+Hammer) — la possession AP doit suffire à `restoredOwnerSoul` ;
  (b) déclenchement réel du Polaroid Demon (instrumentation ci-dessous).

### Livrable 1.3 — Instrumentation Polaroid Demon → `Grunnchipelago.Client/DemonHunt.cs`
- Log seul (aucun effet de jeu), compilé + déployé (0 warning). LogWarning haute
  visibilité, préfixe `[Grunnchipelago]` (capté par le session log).
- Hooks : `AddPolaroidCollected` / `AddPolaroidSolved` / `AddUniquePolaroidSolved`
  → `[DEMON-HUNT] Polaroid <verbe> : <type> @ <zone>` (zone = `curArea` +
  `[scenario:<type>]`). `StartScenario` (Hell/Crypt) → dump de la liste des
  polaroïds de la scène + verdict `Demon present dans la scene : OUI|non`.
- À retirer d'un bloc une fois la question tranchée en jeu.

### Seed coinsanity #2 (test des correctifs) — `dist/Grunn1_coinsanity2.archipelago`
- Régénérée avec **`keep_shears = true`** (demande Jonath : les sécateurs n'étaient pas
  dans la cabane sur la #1). Reste identique : `true_ending`, coinsanity on,
  exclude_bridge_key on, death_link off. Seed `43080710220257549787`.
  `serve.bat` repointé ; nouveau n° de seed ⇒ **profil de sauvegarde neuf**, la #1 intacte.
- Spoiler : `dist/hell_access_spoiler_coinsanity2.md`.
- **Beaucoup plus profonde que la #1 (5 sphères)** et bien meilleure pour le test :
  - `SoulFragment2` sur **`Obtain Cd`** ⇒ **achat obligatoire de 5 gulden** à la boutique
    Hooibaal (`rules.py:126`) — l'économie coinsanity est enfin sur le chemin critique
    (la #1 n'exigeait aucun achat).
  - `SoulFragment1` sur **`Obtain Worm`** ⇒ teste directement le swap différé du ver.
  - **8 des 15 gulden posés contiennent de vrais items** (dont `Hammer` #7 et `OfficeKey`
    #2, tous deux critiques) ⇒ teste modèle + non-respawn.
  - Chaîne du disque (Manoir), passage des gnomes (Hammer), Ferry (ToyBoat) et PillarSpace
    (Église 20 % via arrosage par la pièce bleue) sont tous requis.

### 🔴 Corrections de LOGIQUE apworld (relecture du spoiler par Jonath, 2026-07-21)
Trois erreurs réelles trouvées par Jonath en relisant le spoiler — toutes confirmées au
dump + code décompilé, toutes des règles **trop permissives** (risque de seed bloquée).
40 tests OK + 3 générations de contrôle après correction.

1. **`Obtain Worm` exigeait rien → exige la `Apple`.** L'assiette du jardin est la SEULE
   première source de ver : `Main/Interactions/worm0` a `preventTypes [ObjectInactive,
   NotPlacedApple]` (flag `ProgressData.placedApple`, `GameManager.PlaceApple`
   GameManager.cs:4690). Les deux autres vers sont **circulaires** : `wormFisherman0`
   n'apparaît qu'après avoir DONNÉ un ver au pêcheur (hider `NotGaveWormToFisherman`) et
   `wormMagpie0` est le ver rendu après avoir nourri la pie (+ `isRepeatablePickup`).
   ⇒ branche `CABANE_PECHEUR` supprimée ; **`Apple` passe en `PROGRESSION_ITEMS`**.
2. **Route de la pie : ajout du seuil 30 % du jardin.** `GameManager.cs:3046-3050` :
   `GetAreaCompletedPercentage(StartGarden) >= 30` → `UnlockMagpie()`, et la pie reste
   masquée tant que `NotUnlockedMagpie`. Nouveau helper **`garden_30`** — n'importe quelle
   action de jardinage suffit (tondre `Shears|MagicSword`, taupinières `Trowel`, arroser
   `WateringCan|Coin`), barre bien plus basse que `complete_zone` qui exige tondre ET
   arroser. Appliqué à la branche magpie de `Obtain StrangeKey`.
3. **PillarSpace : ce ne sont PAS les 20 % de l'Église, c'est la poignée.**
   `portal_ChurchToPillarSpace0/churchToPillarSpaceDoor0` a `preventTypes
   [NotRepairedMissingDoorknobDoor]`, et réparer exige de POSSÉDER le `Doorknob`
   (`missingDoorknobDoorRepairInteraction0`). L'arête `EGLISE → PILLAR_SPACE` passe donc
   de `complete_zone(20 %)` à `has(Doorknob)`. En revanche `Obtain Doorknob` reste **libre**
   à l'Église : fouiller le trou de la branche (`doorknobBranchHoleInteraction0`) n'a aucun
   prérequis — le 20 % ne fait que faire apparaître la poignée dans l'herbe, route alternative.

4. **Correction de la correction (Jonath, 2026-07-22)** : dans `garden_30`, la **truelle
   seule ne suffit PAS** — taupinières + déchets restent sous les 30 %. Le helper se
   réduit donc à `can_cut_grass OU can_water` (tondre **ou** arroser). La truelle n'aide
   que par-dessus une route qui suffit déjà.
5. **Poignée sans jardinage confirmée par Jonath** : elle reste récupérable dans l'arbre
   examinable (trou de la branche) si on ne l'a pas fait apparaître en tondant. Cela
   valide `Obtain Doorknob = reach(EGLISE)` **libre** — seule l'arête vers PillarSpace
   exige de POSSÉDER la poignée. Commentaire de traçabilité ajouté dans `rules.py`.

6. **VÉRIFIÉ, aucun changement — gulden du bus au spawn.** Doute soulevé (« #7 est dans
   le bus »), infirmé au dump + log : le gulden du bus est **`#5`**
   (`Main/BusArriving0/busArrivingContainer/gulden0_arriveBus0`, y=13.12 au niveau des
   sièges, 13 m du spawn), déjà mappé sur **`c.MENU`** = sphère 1 avant `BridgeKey`.
   `#7` est un pylône au sol (`Hide_RoadMemorial/.../gulden0_pylon`), 31 m du spawn →
   `EXTERIEUR`, correct. Log de session corroborant : `#5` ramassé en premier au
   démarrage, `#7` bien plus tard après station/banc/bunker. Bonus : **0** occurrence de
   `Unknown gulden path` toutes sessions ⇒ mapping chemin→index fiable sur les 15 pièces.

### Seed coinsanity #3 — `dist/Grunn1_coinsanity3.archipelago` (2026-07-22)
- **Première seed générée avec les règles corrigées.** Mêmes options que la #2
  (`true_ending`, coinsanity on, keep_shears on, death_link off).
  Seed `28320697491472470999`. Spoiler : `dist/hell_access_spoiler_coinsanity3.md`.
  `serve.bat` repointé ; smoke-test serveur OK.
- **8 sphères** (vs 4 et 5) — la plus profonde jusqu'ici :
  `ToiletPaper` sur `Ending: Bus` ⇒ **10 gulden obligatoires** sur le chemin critique ;
  `SoulFragment1` (`Obtain AtticKey`) et `SoulFragment2` (`Polaroid: Demon`) sont **tous
  deux dans Hell** ⇒ `Polaroid: Demon` devient un check REQUIS, ce qui teste directement
  l'instrumentation `[DEMON-HUNT]`.

**Impact sur la seed #2 en cours : aucune casse** — `Apple` est sur
`Polaroid: HangjongPizzaBox` (Extérieur, libre) et `StrangeKey` sur
`Polaroid: HighBridgeKey`, donc tout reste atteignable ; seules les sphères annoncées dans
le spoiler étaient fausses (corrigées dans `hell_access_spoiler_coinsanity2.md`).

### 🔴 Bug Owner/AtticKey + Polaroid Demon = contenu mort (retour Jonath, seed #3)
- **Polaroid Demon** : la chasse `[DEMON-HUNT]` a tranché — **jamais octroyé** par le jeu
  commercialisé. Décompile complète : `PolaroidType.Demon` n'apparaît QUE dans
  `DefinePolaroidString` (nom) ; aucun `AddPolaroidSolved(Demon)` codé en dur ; le seul
  chemin générique (`Polaroid.Trigger()`) est piloté par un objet de scène, or Demon n'en
  a aucun (dump + log Hell `Demon present : non`) ; la boucle « collecte-tout » est dans
  `if (Application.isEditor) { if (false) {...} }` = code mort. ⇒ `Polaroid: Demon`
  **exclu du pool** (`UNSOURCED_LOCATIONS`, `locations.py`) comme `Calm Ghost #8`. Sur la
  seed #3 il portait `SoulFragment2` ⇒ vraie fin était impossible.
- **Bug Owner/AtticKey** : `Owner.EndConversation` (Owner.cs:277) ne donne la clé du
  grenier que `if (!ObtainedKeyItem(AtticKey))`. Si AtticKey a été reçue du multiworld
  avant, le dialogue saute le grant → `ObtainKeyItem` jamais appelé → check `Obtain
  AtticKey` jamais envoyé. Même classe que « possession tue la location », sur le chemin
  du dialogue. Sur la seed #3, `Obtain AtticKey` portait `SoulFragment1` ⇒ 2e blocage de
  la vraie fin. Fix : Postfix `OwnerEndConversationPatch` envoie le check quoi qu'il
  arrive (TrySend déduplique). Audit : `KidTriangle`/`GoldFishAlive` sont gardés par des
  flags de progression (pas la possession) ⇒ non touchés ; seul AtticKey était concerné.

### OldKey retirée du pool (retour Jonath, 2026-07-27)
- Confirmé inutilisée : 0 porte (`unlockItemNeeded`), 0 interaction, aucun `has("OldKey")`
  dans la logique — seule sa propre location `Obtain OldKey` existe. Retirée du pool
  d'items via `POOL_SHELVED_ITEMS` (`items.py`) : l'item n'est plus créé (slot → filler),
  la location reste un check. Réversible (retirer le nom du set). 40 tests OK.

### Épée sacrée — vérifié, déjà correct (retour Jonath, 2026-07-27)
- Save par seed = vanilla-fresh ⇒ une seule épée ; le spot du parc (post-bonne-fin) n'est
  pas débloqué avant la bonne fin. La logique le gère déjà : `Obtain MagicSword =
  reach(HELL) + AtticKey` (grenier de Hell, seule source pré-fin), park **ignoré**
  (design §6 ligne 110). `can_cut_grass = Shears | MagicSword` (OU, Sécateurs = route
  précoce). Aucun changement.

### Seed coinsanity #4 — `dist/Grunn1_coinsanity4.archipelago` (2026-07-27)
- Intègre TOUT ce qui précède + **`lock_player_hut: true`** (cabanon verrouillé par
  l'`AbandonedKey`, demande Jonath). Seed `25204929381721943683`, 120 locations.
  Spoiler : `dist/hell_access_spoiler_coinsanity4.md`. `serve.bat` repointé ; smoke-test OK.
- Lock cabanon validé en génération : `AbandonedKey` en sphère 2 (jardin, libre) avant
  tout ce qui est derrière le cabanon (Sécateurs sphère 4, `Lighter`/couloir du dimanche
  sphère 3) — **pas de verrou circulaire**. Vérif dump : aucun polaroïd n'est dans
  l'intérieur `PlayerSchuur` (ex. `Polaroid: PlayerShed` = `MACRO:StartGarden`), donc le
  lock ne gate bien que Sécateurs / Clé toilettes / couloir final.

### 🔴 Polaroid VoidSkeleton = contenu mort (retour Jonath, seed #4)
- Jonath ne trouvait pas `Polaroid: VoidSkeleton` (station essence) ; sur la seed #4 il
  portait `ShyIdol` ⇒ Hell inatteignable. Diagnostic `[SWAP-DIAG]` (ModelSwap) : sur 34
  polaroïds, **31 présents au connect**, les 3 absents = `Tent` + `GardenGnomes` (légitimes,
  révélés par événement/jour) + **`VoidSkeleton`**. Code : `VoidSkeleton` n'apparaît QUE
  comme nom d'enum + **`SaveManager.RemoveAndAddCertainPolaroids →
  RemoveUnusedPolaroid(PolaroidType.VoidSkeleton)`** (le jeu le strippe comme « unused »
  à chaque load) ; aucun hider de révélation, aucun octroi. ⇒ **exclu du pool**
  (`UNSOURCED_LOCATIONS`) comme Demon. Audit complet : Demon + VoidSkeleton sont les SEULS
  polaroïds morts ; les 32 autres sont collectables. Le check était bien le souci, pas le
  modèle (l'hypothèse « idole sous le sol » a été RÉFUTÉE par la mesure).
- Diagnostic `[SWAP-DIAG]` (mesure écart modèle/polaroïd) : a servi à réfuter l'hypothèse
  « idole sous le sol ». **Retiré le 2026-07-27** avec la sonde poisson (voir ci-dessous).

### Nettoyage — sonde poisson + SWAP-DIAG retirés (Jonath, 2026-07-27)
- La sonde visuelle GoldFishAlive au spawn (`SpawnFishModelProbe` / `fishProbeInstance` /
  `FishProbePosition` / `LogProbeRenderers`, `Patches.cs`) et les helpers
  `ModelSwap.LibraryReady` / `TryApplyLibraryModel` + le diagnostic `LogSwapDisplacement`
  (SWAP-DIAG) sont **supprimés**. Le vrai fix (récolte du mesh porteur pour GoldFishAlive,
  `ModelSwap.BuildLibrary`) reste — le poisson vivant s'affiche correctement en jeu.
  Build 0 warning, aucun résidu.
- Reste en place : instrumentation `[DEMON-HUNT]` (a prouvé Demon + aidé VoidSkeleton) —
  à retirer sur demande (spamme le log dans le Crypt).

### Seed coinsanity #4 régénérée (v finale, 2026-07-27)
- Après exclusion de VoidSkeleton : seed `76103154173425576054`, **119 locations**
  (Demon + VoidSkeleton exclus). Même options (lock hut + keep shears + coinsanity +
  true_ending). Spoiler `hell_access_spoiler_coinsanity4.md` mis à jour. Smoke-test OK.
- Les seeds coinsanity1/2/3 précédentes sont périmées (règles + exclusions plus anciennes).

### 🔴🔴 BUG PROCESS — contamination d'apsave entre seeds (retour Jonath, 2026-07-27)
- **Symptôme** : sur la nouvelle seed, Jonath avait « tous les items de son ancienne run ».
- **Cause** : le serveur AP nomme son `.apsave` d'après le **fichier `.archipelago`**, pas
  d'après la seed. J'ai réutilisé le nom `Grunn1_coinsanity4` pour 3 seeds successives ⇒
  le serveur de la nouvelle seed a **chargé l'apsave de l'ancienne** et rejoué ses items.
  Preuve : sur les 88 items reçus dans l'apsave, **73/88 incohérents** avec le fill de la
  seed courante mais cohérents avec la précédente (ex. `MagicSword ← Polaroid: PlayerShed`,
  `AbandonedKey ← Polaroid: TallManWindow` = seed 25204…). Le profil de sauvegarde par seed
  fonctionnait bien (log « nouveau ») — le leak était **100 % côté serveur/apsave**.
- **Fix** : nom de fichier **unique = n° de seed** (`Grunn1_hut_<archiveSeed>.archipelago`).
  L'archiveSeed nomme AUSSI le profil (`grunn_ap_<archiveSeed>_<slot>`), donc apsave ET
  profil sont garantis neufs par construction. Apsave `coinsanity4` contaminé supprimé.
- **Règle pour la suite** : NE JAMAIS réutiliser un nom de `.archipelago` entre deux seeds ;
  toujours inclure le n° de seed dans le nom. (À terme : script de gen qui le fait seul.)
- Seed propre livrée : `Grunn1_hut_23400565906336956356` (display 7784511242727784160),
  119 loc, mêmes options. Spoiler `hell_access_spoiler_hut.md`. Smoke-test OK.

### Magic Pond — check au dépôt + modèle + fausse route (retour Jonath, 2026-07-27)
- **Contexte** : `Obtain GoldFishAlive` était octroyé au RETRAIT du poisson vivant
  (`RetrieveFishFromMagicPond → ObtainKeyItem`), tué par la possession (le contenu
  `MagicPond_FishAlive_Content` est masqué sur `KeyItemObtained`). Le bocal ne réanime pas
  (code : seul `FishbowlRetrieveDeadFish`, pas de « retrieve alive »). L'item de ce check
  = **Trumpet** (magique, id 478660020 ≠ KidTrumpet 478660044).
- **Fix mod (3 parties, demande Jonath)** :
  1. **Check au DÉPÔT** : `MagicPondPlaceFishPatch` (postfix sur
     `GameManager.PlaceFishInMagicPond`) envoie `Obtain GoldFishAlive` quand on **dépose le
     poisson mort** — critère unique. TrySend déduplique (le retrait vanilla devient no-op).
  2. **Hider** : `MagicPond_FishAlive_Content` (keyItemRef GoldFishAlive) ne se masque plus
     sur la possession (`KeyItemObtained → false` dans `ContentHiderConditionPatch`) —
     piloté par `NotFishInMagicPond` seul, donc le contenu s'affiche après le dépôt.
  3. **Modèle** : `ModelSwap.SwapMagicPondFish` remplace le visuel du poisson vivant par le
     **modèle du contenu du check** (Trumpet), pas le poisson vanilla.
- **Fix apworld (fausse route)** : la règle `GoldFishAlive` autorisait
  `MAGIC_POND OU PASSAGE_GNOMES` (bocal), or le bocal ne réanime pas ⇒ **`PASSAGE_GNOMES`
  retiré**, `GoldFishAlive = reach(MAGIC_POND) + has(GoldFishDead)`. 40 tests OK. **La hut
  seed en cours reste valide** (Magic Pond y est atteignable via le cabanon), pas de régen
  nécessaire ; la correction protège les futures seeds.
- Note Jonath : un check « poisson vivant dans le bocal » pourra être AJOUTÉ plus tard
  (séparé, sur l'ALIVE fish) — ça ne réactivera pas la route de réanimation.

### 🔴 Logique Parc — 3ᵉ route manquante (retour Jonath, 2026-07-27)
- **Bug** : le Parc a 3 accès (Briquet+Extérieur, Rame+Église/barque, **Marteau+passage
  des gnomes**), mais l'apworld ne modélisait que Briquet + Rame. Le passage des gnomes
  (RoundHallway/GnomeForest) n'avait que les arêtes ENTRANTES (`JARDIN→PASSAGE`,
  `PARC→PASSAGE`) — pas les SORTIES → impossible de traverser Jardin→Parc. Portails dump :
  `portal_StartGardenToRoundHallway0 <-> portal_RoundHallwayToStartGarden0`,
  `portal_ParkToRoundHallway0 <-> portal_RoundHallwayToPark0` (portes gnomes = Hammer).
- **Fix** : passage rendu **bidirectionnel** (ajout `PASSAGE_GNOMES→JARDIN` et
  `PASSAGE_GNOMES→PARC`, Hammer). 40 tests OK. Logique plus permissive ⇒ **hut seed en
  cours reste valide, pas de régen**.
- **Corrige une sur-correction de CC** : avec la route Marteau, le fantôme #2 (Briquet)
  n'est PAS vital pour la hut seed (Parc atteignable au Marteau) ⇒ la seed n'a jamais été
  inwinnable. Le fix Magic Pond (Trumpet) reste correct mais n'était pas un anti-softlock.
- **Confusion GnomeIdol (Jonath)** : `Polaroid: GnomeIdol` (jardin de départ) ≠
  `Polaroid: GardenGnomes` (forêt des gnomes). Sur la hut seed, l'item GnomeIdol est sur
  **GardenGnomes** ; Jonath avait collecté `Polaroid: GnomeIdol` (= Bone). Aucun bug.

### 🔴 Polaroid GardenGnomes = 3ᵉ contenu mort (retour Jonath, 2026-07-27)
- **Preuve empirique décisive** (diag `[DIAG-GNOME]`, scan de `allPolaroids` chaque frame) :
  GardenGnomes n'est **JAMAIS instancié**, alors que Jonath était à ~3 m — log de la même
  seconde : `Granted KidTrumpet` (ce pickup est à **2,83 m** du polaroïd) +
  `GnomeForestDoor resolu`. 0 collecte sur **tout l'historique** (toutes seeds).
- Code : `PolaroidType.GardenGnomes` n'apparaît QUE dans `DefinePolaroidString` ; aucun
  hider de révélation, aucun octroi. **Témoin de contrôle** : `AppleAndWorm` (aussi
  non-euclidien, AppleSpace) EST dans `allPolaroids` et se collecte — donc l'absence est
  propre à cet objet, pas aux zones non-euclidiennes.
- ⇒ **Exclu** (`UNSOURCED_LOCATIONS`). Il portait `GnomeIdol` (idole REQUISE) sur la hut
  seed ⇒ Hell inatteignable.
- **Suspect restant : `Tent`** (0 collecte aussi) ⇒ **non exclu**. Diag généralisé en
  **watchlist** `[DIAG-POLAROID]` (Tent / GardenGnomes / VoidSkeleton) : si Tent apparaît
  dans `allPolaroids`, le log le confirmera vivant.
  ⚠️ **Correction d'une erreur de CC (2026-07-27, question de Jonath)** : j'avais annoncé
  que le polaroïd Tent était « dans la tente, révélé au jour 2 » — **FAUX**. Position réelle
  au dump : `(20.45, 10.66, 32.8)`, zone **Église**, à **2,16 m du PUITS**, et **AUCUN hider
  ne le cible** (accès libre, toujours visible). Le hider `TentSceneContentHider0` (jour 2)
  concerne le **campement près du champ de maïs** (`Hide_Corn/tentSceneContainer`), rien à
  voir. **Les noms de polaroïds désignent le SUJET de la photo, pas l'emplacement** — preuve
  croisée : `Polaroid: Well` (puits) est, lui, **DANS la tente**
  (`NonEuclidian/TentInside/Hide_Tent/polaroid_well0`). Le mapping `locations.py` était déjà
  correct (`Tent → EGLISE`, `Well → TENTE`) ; seule l'explication verbale était fausse.
  ⇒ Tent est très probablement **vivant**, simplement jamais ramassé (petit objet au puits).
- ⚠️ Biais méthodo relevé : le diff « polaroïds swappés au connect » sous-estime la
  présence (un swap `result==0/3` n'est pas loggé) ⇒ ne JAMAIS conclure « mort » sur ce
  seul critère ; exiger le scan direct de `allPolaroids` + absence d'octroi dans le code.

### Seed `Grunn1_s29916071001062526936` (2026-07-27) — première seed 100 % saine
- **118 locations** : Demon + VoidSkeleton + GardenGnomes + Calm Ghost #8 tous exclus.
  Options inchangées (true_ending, coinsanity, keep_shears, lock_player_hut, death_link off).
  Seed affichée `53456554656030481917`. Spoiler `hell_access_spoiler_s29916071001062526936.md`.
  Nom de fichier unique (règle anti-contamination d'apsave). Smoke-test OK, 40 tests OK.
- Choix Jonath : régénérer plutôt que débloquer la run précédente au `/send`.

### ✅ RÉSOLU — `lock_player_hut` : le SOMMEIL est désormais modélisé (2026-07-27)
> Le bloc ci-dessous (« cassée par design ») était une **conclusion erronée de CC** :
> j'avais désactivé l'option **de ma propre initiative** alors que Jonath voulait
> précisément la tester — décision qui ne m'appartenait pas. L'option **est viable**
> (et intéressante en multiworld : l'AbandonedKey peut venir d'un autre monde). Le vrai
> correctif n'était pas de la retirer mais de **modéliser le sommeil** :
> - nouveau helper **`rules.can_advance_days`** = `not lock_player_hut or has(AbandonedKey)` ;
> - appliqué aux checks réellement jour-dépendants, tracés au code/dump :
>   `Ending: Mist` (scénario MistDay, **jour 3** [J]), `Ending: Bus`
>   (`BusSeat: if (dayIndex <= 1) return;`), `Calm Ghost #3 (Road)`
>   (= `ghost0_redCar0`, hider `DayIndexIsNot day=2`), et l'arête **Ferry**
>   (`DayIndexIs(2)`).
> - `options.py` : docstring rétablie et enrichie (elle documente maintenant que la clé
>   gate AUSSI le sommeil) ; `display_name` remis à `Lock Player Hut`.
> - 40 tests OK. Seed de test livrée : `Grunn1_hut_s34382771046716962829` (118 loc),
>   départ vérifié : `Ending: Darkness` → `OldPlank` → pont (les Sécateurs étant enfermés),
>   puis `Obtain Lighter` → `AbandonedKey` → cabanon + sommeil.
> - **Leçon de process** : réduire le périmètre demandé (désactiver une option à tester)
>   est une décision de Jonath, pas de CC. Signaler le problème, proposer, laisser trancher.
>
> **Liste complète des conditions fournie par Jonath (2026-07-27), intégrée :**
> | Check | Condition | Statut |
> |---|---|---|
> | `Ending: Mist` | jour 3 | `can_advance_days` |
> | `Ending: Bus` | jour 3 (code : `dayIndex <= 1` refuse) | `can_advance_days` |
> | `Ending: LongHallway` + orbe (`Polaroid: Crypt`/`GnomeIdol`) | jour 2 | déjà gaté via `CABANE_JOUEUR` |
> | `Calm Ghost #3 (Road)` | jour 2 (`ghost0_redCar0`) | `can_advance_days` |
> | `Calm Ghost #4 (WindyPath)` | jour 2 (`ScooterCrashContentHider0`) | `can_advance_days` |
> | `Obtain PizzaBox` | jour 2 (hangjongeren) | `can_advance_days` |
> | `Obtain PrettyFlower` | jour 3, arrosée chaque jour | `can_advance_days` (+ FLAG graine/arrosage) |
> | `Obtain SeveredHand` | screamer TallMan ⇒ entrer dans la cabane | `reach(CABANE_JOUEUR)` |
> | `Polaroid: MagpieNest` | hider `NotEnteredPlayerSchuur` | `reach(CABANE_JOUEUR)` |
> | `Polaroid: TallManWindow` | TallMan visible seulement depuis l'intérieur | `reach(CABANE_JOUEUR)` |
> | `Obtain OfficeKey` | station (jour 1) **OU** marchande (Parc+Briquet+20 %) | déjà 2 routes, inchangé |
> | **Ferry** | **AUCUNE contrainte de jour** | ❌ `can_advance_days` RETIRÉ (erreur CC) |
> 40 tests OK + 3 générations de contrôle. Seed livrée :
> `Grunn1_hut_s37401968028493530688` (118 loc), spoiler `hell_access_spoiler_hut.md`.
>
> **FLAG RÉSOLU — `Obtain PrettyFlower`** [J 2026-07-27] : il faut **SpecialSeed**
> (à planter dans le pot du jardin, accessible dès le départ) **+ `WateringCan`** + **jour 3**.
> Point important : **`can_water` ne convient PAS** — la pluie (pièce bleue + église) ne
> peut pas être répétée chaque jour jusqu'au jour 3, donc la règle exige **l'arrosoir
> nommément**. Règle finale :
> `reach(JARDIN) and has_all(SpecialSeed, WateringCan) and can_advance_days`.
> Conséquence : **`SpecialSeed` passe en `PROGRESSION_ITEMS`** (il gate désormais un check).
> 40 tests OK. Seed livrée : `Grunn1_hut_s76554098605004193370` (118 loc), spoiler
> `hell_access_spoiler_hut.md` ; départ : `Obtain GardenKey` → `Plank` → pont, puis
> `Polaroid: Ferry` → `AbandonedKey`.

### ~~🔴🔴 `lock_player_hut` est CASSÉE par design — désactivée~~ (analyse PÉRIMÉE, voir ci-dessus)
- **Symptôme** : cabanon verrouillé ⇒ Jonath ne peut plus **passer les journées** ; seule
  la fin `Darkness` reste atteignable, et `Ending: Mist` (**jour 3**) devient impossible.
  Sur la seed s2991…, `Ending: Mist` portait la `ToiletKey`, seule sortie du jardin ⇒ **run
  bloquée dès le départ**.
- **Cause racine** : le **LIT est DANS le cabanon**
  (`Hide_PlayerSchuur/interior/bed0`, type=Bed). Le dump ne contient que **2 lits** et le
  second est dans l'**AtticRoom** (contenu d'endgame) ⇒ verrouiller le cabanon = **ne plus
  pouvoir dormir** = les jours n'avancent plus. Tout le contenu daté meurt avec (Mist j3,
  couloir du dimanche, PNJ/boutique par jour...).
- **Non rattrapable en logique** : l'apworld pose « le temps est LIBRE » (design §6) ;
  modéliser l'inverse gaterait des dizaines de checks sans rapport derrière l'AbandonedKey.
- **Décision** : option **désactivée** dans le YAML de playtest + docstring de
  `LockPlayerHut` réécrite en « **BROKEN - DO NOT ENABLE** » avec la preuve
  (`options.py`), display_name → `Lock Player Hut (BROKEN)`. 40 tests OK.
- **Leçon** : j'avais listé le contenu du cabanon (dont `bed0`) lors de l'audit du lock
  sans en tirer la conséquence ; j'ai validé l'option sur la seule question « la clé est-elle
  atteignable ? » sans vérifier ce que la porte **enferme**. Vérifier désormais les
  MÉCANIQUES enfermées, pas juste les items.

### Seed `Grunn1_s82423945450666732196` (2026-07-27) — seed de remplacement
- 118 locations, `lock_player_hut` **off**, reste inchangé. Seed affichée
  `73112102825117634063`. Spoiler `hell_access_spoiler_s82423945450666732196.md`.
- **Sortie du jardin vérifiée avant livraison** (nouvelle exigence) : sphère 2 =
  `Obtain Shears: Shears` ⇒ Sécateurs dans la cabane ouverte → haie → Extérieur, et le
  **lit est de nouveau accessible**. Smoke-test serveur OK.

### 🔴 `Obtain OldKey` et `Obtain AbandonedKey` = checks fantômes, RETIRÉS (Jonath, 2026-07-27)
- **Erreur de CC** : quand Jonath a demandé de retirer l'**OldKey**, j'ai retiré l'**item**
  (`POOL_SHELVED_ITEMS`) mais **gardé la location** `Obtain OldKey`, en annonçant ça comme
  un choix réversible. Faux raisonnement : un objet non récupérable en jeu ne peut pas
  servir d'emplacement. Idem pour `Obtain AbandonedKey` — l'AbandonedKey n'a **aucun check
  vanilla** : on l'a réquisitionnée uniquement pour verrouiller le cabanon.
- **Conséquence réelle** : sur la seed `hut_s7655…`, la **Truelle** était placée sur
  `Obtain OldKey` ⇒ inatteignable ⇒ run bloquée (elle gate Église 100 % → Plage → l'os →
  `AbandonedKey`). Jonath l'a repéré, pas moi.
- **Fix** : les deux locations passent dans `UNSOURCED_LOCATIONS` ; `create_all_locations`
  filtre désormais sur `UNSOURCED_LOCATIONS` en plus de `UNSOURCED_ITEMS` ; leurs entrées
  sont retirées de `OBTAIN_RULES`. ⚠️ L'**ITEM** `AbandonedKey` **reste dans le pool**
  (lock_player_hut en a besoin) — seule sa location disparaît. Test
  `test_sourced_key_item_locations_exist` mis à jour. 40 tests OK.
- **Passage de 118 → 116 locations.** Seed livrée : `Grunn1_hut_s08873442762073113111`.
- **Règle à retenir** : retirer un item du pool ≠ retirer sa location. Toujours se demander
  si le PICKUP existe et est atteignable en jeu avant de garder un check.

### Correctif visuel en cours de session — modèle des Gulden (retour Jonath, capture)
- **Symptôme** : sous coinsanity, plein de checks affichaient la **carte AP rouge**.
- **Cause** : `"Gulden"` n'est ni un `KeyItem` ni un buff → il tombait dans le cas par
  défaut de `SwapForScout` = carte AP, **teintée rouge** car coinsanity classe le Gulden
  en *progression* (`items.py:95`). Avec **36 Gulden** dans le pool ⇒ cartes partout.
  (Les gulden *posés* n'étaient pas en cause : `Apply` les ignore déjà, `ModelSwap.cs`.)
- **Fix** (`ModelSwap.cs`) : récolte du modèle de pièce depuis un gulden posé
  (`BuildLibrary`, les gulden étaient sautés) + branche dédiée dans `SwapForScout` →
  un check contenant de l'argent affiche **la vraie pièce** (`GuldenModelScale = 1.5f`
  + lift, ajustable). Traps explicitement exclus de cette branche (pas de fuite).
- **Gulden posés → vrai contenu** (demandé ensuite) : `Apply` les ignorait totalement.
  Ils sont désormais résolus par **chemin de scène gelé** (`ScenePaths.GuldenIndex` →
  `GameIds.GuldenLocationNames` → `LocationIdByName`, il n'y a pas d'« Obtain X » pour
  eux) et affichent leur contenu, **surélevé de `GuldenContentLift = 0.1f`** (les pièces
  sont à plat au sol). Garde-fous : **uniquement si coinsanity est ON** (sinon un gulden
  est de l'argent, pas un check → vanilla), et si le check contient réellement du
  `Gulden`, on laisse la pièce vanilla (déjà véridique).

### Correctif — modèle du ver (plateau de la pomme), retour Jonath
- **Symptôme** : après avoir posé la pomme, l'objet révélé gardait le **modèle du ver**
  quel que soit son vrai contenu.
- **Cause** : les pickups `Worm` étaient **volontairement laissés vanilla** (session 2) —
  leur mesh est dans un enfant inactif révélé par événement monde, et un clone posé à la
  connexion s'affichait tout de suite et **trahissait l'emplacement**.
- **Tentative 1 (ÉCHEC, 2026-07-21)** : swap différé piloté par les renderers du pickup
  (`activeInHierarchy` + `enabled`). Bug reproduit en jeu (capture Jonath) : le modèle
  apparaissait **avant** la pose de la pomme. Preuve dans le log de session :
  `Model swap (revele) : Worm -> SoulFragment1` à 04:25:48, soit 7 min après la passe
  one-shot — c'est-à-dire au **streaming de la zone**, pas à la pose de la pomme.
  Cause : `worm0.startState = Show`, donc `ItemPickup.SetVisuals` **force**
  `visualsObject.SetActive(true)` ; et le worm vanilla est masqué par un objet
  **séparé** (`ContentHiders/wormHider0` → `objectRef "wormLine"`), pas par
  `visualsObject`. Toute heuristique renderer/activeInHierarchy est donc invalide ici.
- **Fix retenu — condition RÉELLE du jeu** : le swap se fait normalement dans la passe
  one-shot, mais le clone démarre **masqué** et `TickWormHolders` le pilote depuis
  **`SaveManager.progressDataCheck.placedApple`**, posé par `GameManager.PlaceApple()`
  (GameManager.cs:4690-4695) — le flag exact derrière le preventType `NotPlacedApple`
  de l'interaction (dump `Main/Interactions/worm0`). Étant en ProgressData **par run**,
  l'emplacement se re-masque tout seul à chaque nouvelle run.
- Portée : seul `Main/Interactions/worm0` (assiette) est concerné — les deux autres vers
  (`wormMagpie0`, `wormFisherman0`) sont `isRepeatablePickup` et déjà exclus par `Apply`.

### Correctif — gulden coinsanity qui réapparaissent au reset de run (retour Jonath)
- **Symptôme** : sous coinsanity, un gulden dont le check est ENVOYÉ revenait en monde
  au lancement d'une nouvelle run.
- **Cause** : `PickupVisibility.AppliesTo` **excluait explicitement** `isGulden`, donc les
  gulden restaient pilotés par le vanilla — or ils vivent dans `ProgressData.coinGrabPosition`
  (**per-run**), remis à zéro à chaque run. Correct pour de l'argent, faux pour un CHECK.
- **Fix** : les gulden entrent dans la machinerie « visibilité = ÉTAT DU CHECK »
  (`ApClient.GuldenCheckSent(index)` + `PickupVisibility`), **uniquement si coinsanity
  est ON** (sinon un gulden est de l'argent et doit respawner comme en vanilla).
  Index résolu par chemin de scène gelé et **mis en cache par instanceID**
  (`ScenePaths.GuldenIndex` construit le chemin et logue un warning sur miss, alors que
  `ResetState` tourne à chaque reset).
- **Garde-fou (régression évitée)** : `ResetState` appelle `SetState` **puis**
  `CheckForLoadOperation` (ItemPickup.cs:56-72) qui masque une pièce déjà prise dans la
  run via `coinGrabPosition`. Notre postfix passant après, repartir de `startState`
  aurait fait **réapparaître en pleine run** une pièce déjà ramassée → le masquage est
  désormais **additif** : `checkSent || GuldenGrabbedThisRun` (miroir fidèle du test de
  distance 0.25 vanilla).

### ⚠️ TEMPORAIRE À RETIRER — sonde visuelle GoldFishAlive (2026-07-21)
- Sonde temporaire (pickup cadeau au spawn, `-39.9 / 10.35 / -65.7`) pour vérifier de visu
  le modèle du poisson VIVANT, invisible autrement sur la seed #2 (`GoldFishAlive` y est
  sur `Ending: Picnic`, une location logique sans objet en monde).
- `GoldFishAlive` n'ayant **aucun pickup posé**, on clone le template de l'Os, on repointe
  `keyItemObtain` et on repeint via `ModelSwap.TryApplyLibraryModel` (+ `LibraryReady`).
- **Innocuité** : préfixe `grunnchipelago` ⇒ octroi vanilla, **aucun check** ; et
  `GoldFishAlive` n'est pas dans `PROGRESSION_ITEMS` ⇒ logique de seed intouchée.
- Retirée une première fois, puis **REMISE** (Jonath voulait bien revoir le modèle 3D :
  un item simplement *reçu* atterrit en inventaire et n'affiche aucun modèle en monde).
- **Piège rencontré** : avec `keyItemObtain = [GoldFishAlive]`, la sonde **se masquait
  toute seule** dès l'item possédé — `ItemPickup.ResetState` applique la règle vanilla
  « item obtenu → Hide », et les pickups cadeaux sont volontairement **exclus** de
  l'override visibilité=état-du-check (`PickupVisibility.AppliesTo` renvoie `false` sur le
  préfixe `grunnchipelago`). Déclenché en pratique par un `/send GoldFishAlive`.
  ⇒ sonde rendue **purement visuelle** : `keyItemObtain` vide (rien à masquer, rien à
  octroyer) + petit lift `0.15` (l'herbe haute du jardin avalait le modèle).
- **VRAI BUG trouvé grâce à la sonde** : le modèle `GoldFishAlive` était **vide**
  (pickup saisissable, aucun visuel). Cause : le cas spécial `GoldFishAlive` de
  `BuildLibrary` archivait sa source **sans vérifier qu'elle porte un mesh**, alors que
  le chemin général impose `Renderer.Length > 0` — et il essayait
  `MagicPond_FishAlive_Content` **en premier**, qui archive vide.
  Fix : helper `FindRenderableByName` (exige un mesh, continue à scanner si un homonyme
  est un conteneur vide) + ordre corrigé — **`FishAliveContainer` (aquarium) d'abord**,
  source indiquée par Jonath en jeu (dump : `fishbowl0/FishAlive_ContentHider0` →
  objectRef `FishAliveContainer`), MagicPond en repli, poisson mort en dernier recours.
  Log ajouté : source réellement retenue + nombre de renderers.
- **2e cause, trouvée en MESURANT (sonde instrumentée)** : une fois la bonne source
  récoltée, le modèle rendait bien mais **flottait ~11 m au-dessus du pickup** — d'où un
  objet saisissable (collider au sol) et invisible. Mesure décisive :
  `bounds=(4.124,2.727,2.219) centre=(-39.84, 21.25, -65.47)` pour un pickup à `y=10.4`.
  Cause : le poisson est à un **décalage local** dans le bocal, et `Archive` ne remet à
  zéro que la position de la **racine** — le décalage de l'enfant survit puis est
  multiplié par l'échelle. Fix : récolter l'objet **porteur du mesh** (le renderer
  unique) au lieu du conteneur. Taille réelle mesurée ≈ `0.69/0.45/0.37`, comparable à
  l'os ⇒ aucun grossissement nécessaire.
- Leçon : deux diagnostics faux d'affilée (heuristique renderer, puis échelle supposée)
  avant que l'**instrumentation de mesure** ne donne la réponse en une passe.
- **À SUPPRIMER** au final : `SpawnFishModelProbe` / `fishProbeInstance` /
  `FishProbePosition` + appel dans `EnsureSpawned` (`Patches.cs`), et
  `LibraryReady` / `TryApplyLibraryModel` (`ModelSwap.cs`).
- Au passage, `apworld_design.md` §10 corrigé : la mention « approximations
  GoldFishAlive→Dead, AtticKey→OldKey ; non couvert : KidTriangle » était **périmée**
  sur les trois points (poisson vivant récolté en scène, KidTriangle = triangle de
  l'homme hirsute, clés orphelines = fallback générique vers un modèle de clé).

### Livrable 2 — Checklist coinsanity
- Gulden #8 (pot, Extérieur) exige `Hammer` (`rules.py:306`) — contient `SoulFragment1`.
- Gulden #2 (banc abri, Extérieur, libre) — contient `MagicSword`.
- ⚠️ **Correction tracée** : le popup diagnostic gulden a été **retiré en session 2**
  (`Patches.cs:396`). Diagnostic désormais via **`VerboseLogs=true`** →
  `Gulden pickup: Gulden #n (zone)`. Checklist mise à jour en conséquence.
- Réserve d'argent = **36 gulden** placés (= somme des prix). Génération OK en
  `accessibility: full` ⇒ tous les achats restent faisables ; **aucun achat sur le
  chemin critique Hell**.
- Secours test : `/send Grunn1 Gulden` (console serveur) — à retirer du bilan.

## Session 2 (CC, 2026-07-16) — statuts

### Bloc 1 — UI : LIVRÉ, à valider in-game
- [x] 1.1 Titre : `TitleTextPatch` (postfix `UIManager.BuildStaticStrings`,
  UIManager.cs:4103) — le titre devient « GRUNNCHIPELAGO », police réduite au ratio
  des largeurs TMP pour tenir dans la largeur de « GRUNN » ; marqueur rouge
  « ARCHIPELAGO » supprimé ; mod désactivé = titre vanilla (aucun patch).
- [x] 1.2 Panneau de stats : descendu sous le bloc jour/heure (ancre haut-droite,
  y −230 au lieu de −40 — choix « sous jour/heure », alternative haut-gauche possible
  si ça déborde) ; libellés précis avec accents (« Vitesse de déplacement »,
  « Portée du sécateur », « Cadence de découpe », « Taille », « Contrôles :
  normaux / INVERSÉS ») ; durée restante des traps inchangée (temps de jeu).
  ⚠ à vérifier in-game : rendu des accents avec la police TMP du jeu.
- [x] 1.3 Item sur l'écran de fin : panneau à GAUCHE du polaroid pendant
  `EndingState.Start` (visible tant que `polaroidRead` est affiché) — « Objet
  débloqué : X » ou « Envoyé : X -> joueur » depuis le scout. Texte seul pour v1 :
  le jeu n'a AUCUN sprite d'item (KeyItemInfo = textes, SaveManager.cs:1176) ;
  visuel 3D sur canvas overlay = non trivial (render texture), reporté.

### Bloc 1 — validé in-game par Jonath (2026-07-16), + ESC-skip du dialogue de
l'orbe ajouté en cours de bloc (quitData, pas cancelData : ESC = action quit).

### Bloc 2 — Modèles : LIVRÉ, à valider in-game
- [x] 2.1 Swap étendu aux `Polaroid` de la scène (ApplyPolaroid) : mêmes règles
  que les pickups (item Grunn local scouté -> vrai modèle, sinon modèle AP par
  classification, traps déguisés) ; polaroids Ending exclus ; parent inactif.
- [x] 2.2 Modèles AP agrandis : ×1.75 (const ApModelScale, appliquée au holder
  des clones teintés uniquement) — facteur à valider sur capture.
- [x] 2.3 Popup « Objet obtenu : X » pour les buffs/fillers reçus (GrantItem,
  même canal QueuePopup ; traps conservent leur popup « X ! », key items le
  popup vanilla).

### Bloc 3 — Fins
- [x] 3.2 Liste des 11 fins à DROITE du polaroid de fin (x +450, même raison que
  le panneau item à −450 : la carte du polaroid rend au-dessus de notre canvas).
  Basée sur l'ÉTAT AP (`ApClient.EndingCheckSent`), jamais sur
  GlobalData.endingTypesSeen. Numérotation + noms repris du jeu via
  `PolaroidManager.GetPolaroidData(...).myIndex` et `DefinePolaroidString`, donc
  identiques aux polaroids (« 3. bus »). Fins non envoyées = « ??? ».
  Compteur « n / 11 » en pied de liste.
- [x] 3.1 Profil de sauvegarde par seed (`SaveProfile.cs`) : `SaveManager.savePath`
  est un PRÉFIXE (`savePath + slotIndex + ".txt"`, SaveManager.cs:2055/2080/2092)
  et `curSlotIndex` vaut toujours 0 (assigné une seule fois, ligne 1405) — on
  redirige donc le préfixe vers `grunn_ap_<seed>_<slot>`, sans toucher aux slots
  ni ajouter d'UI. Bascule UNIQUEMENT au menu titre (avant tout chargement de
  monde), puis rejeu de la routine de boot (`CheckIfFileExists` →
  `LoadFromFile` / `CreateNewSave` + `UpdateSaveDataCheck`).
  Conséquences : save vanilla intouchée, compteur de fins / polaroids /
  runsCompleted / raccourcis à zéro par seed, resync destructive des polaroids
  et reset des raccourcis désormais court-circuités. Réglages partagés
  (`settingsPath` distinct). Déconnexion en cours de partie = on reste sur le
  profil (pas de bascule à chaud). En cas d'erreur, retour au chemin vanilla.

### Bugs bloquants trouvés au playtest (2026-07-16) — tous corrigés
- Fantômes exigent la Trompette (logique les croyait libres).
- « Calm Ghost #8 » n'existe pas (ghost0_backup, 7 GhostTouch, ghostCalmMax=7).
- Polaroid Crypt/GnomeIdol gatés par l'Orbe (couloir final).
- Polaroid LighterMolehill tué par la possession du Briquet (hider polaroid).
- Poignée de porte tuée par la possession (Interaction.PreventType.KeyItemObtained,
  3e couche de possession, jamais patchée).
- Regrow Grass Trap impossible en cours de run (DOTS) -> retiré du pool ; les 4
  autres traps de repousse restaurent désormais les objets en monde.

## ✅ Statut consolidation (CC, 2026-07-14) — tout traité
- [x] A/PRIORITÉ 1 — visibilité pickups/boutique = état du check (commit 9211112) :
  CheckIfAlreadyObtainedThisItem + ResetState + hiders KeyItem[Not]Obtained ciblant un
  ItemPickup ; recalcul global à la connexion ; bone gift exempté.
- [x] B.1 Gulden #2 (abri station, libre) + B.6 table clé→porte v0.3 croisée : AUCUN
  écart, références en commentaire ; OldKey/AbandonedKey orphelines (fb7e9a4).
- [x] C.1 verdicts de ramassage / C.3 résumé resync (03ff9e1) ; C.2 log de session
  persistant + rotation (dbbdcaa) ; C.4 audit specialItemTypes : sons uniquement,
  aucun octroi hors ObtainKeyItem/AddTool — rien à patcher.
- [x] D.1 récompense des fins annoncée (scout, popup à la reprise) ; D.2 option config
  SkipEndingDialogues (Owner/OwnerSaved, texte instantané + skip immédiat) ; D.3 popup
  ramassage livré au tour précédent (dbbdcaa).
- [x] E lock_player_hut implémentée (AbandonedKey orpheline confirmée) : mod + apworld
  + slot_data + 2 tests (5b09230). 36 tests OK, génération all_endings+hut OK.
- [x] 5/6/7/8 (Polaroid Demon→Hell, _demo, Gulden #8 Hammer, Couloir final libre) :
  livrés au tour précédent (d6c897f).
- F (design, avis rendu, pas d'implémentation) : voir réponse CC — consommables = pas
  de softlock possible (réinjection par run), pickups inertes = résolus de facto par A.

## Résolu pendant le playtest (à intégrer)
1. **Gulden #2** : abri face à la Station essence, posé sur un banc — région Extérieur,
   accès LIBRE. Mettre à jour la règle (source : Jonath in-game).
2. **« Pickups sans check »** : élucidé — re-ramassages de locations déjà envoyées
   (Obtain Bone : Jalon 1 ; Obtain Plank : session Jalon 2). Dédup par design.
   Cause de leur réapparition : l'octroi vanilla étant intercepté, le jeu n'enregistre
   jamais ces objets comme obtenus -> les pickups respawnent, inertes, tant que l'item
   AP correspondant n'est pas possédé.
3. **Polaroids sur save de vétéran** : la resync fonctionne (checks Polaroid: Bone et
   Polaroid: OldLadyBackGarden partis en jeu).

## Demandes pour le prompt CC global

0. **Dumper v0.3** : capturer `Door.unlockItemNeeded` (List<KeyItem>, scène) pour obtenir
   la table complète clé -> porte (vérification des règles de clés + doc). En profiter
   pour capturer aussi tout autre champ de gating manquant repéré depuis (ex.
   ItemPickup.specialItemTypes déjà capturé, vérifier Interaction.keyItemUseRef éventuel).

### PRIORITÉ 1 — BUG : locations tuées par la possession de l'item (découvert in-game)
Symptôme observé : Medal et OfficeKey reçus du multiworld -> la marchande ne les vend
plus -> checks boutique inaccessibles. Cause racine : l'octroi AP écrit dans
keyItemsObtained, et le jeu masque tout pickup/article dont l'item est possédé
(ItemPickup.CheckIfAlreadyObtainedThisItem + filtrage boutique + l'event GrabbedItem()
ajouté par CC). Portée : TOUTE location dont l'item arrive avant son check est morte
(sur la seed en cours : FlowerGem et ShyIdol reçus -> serre des gnomes et idole
fantôme vraisemblablement masquées). Fill-dépendant, silencieux, peut bloquer une seed.
FIX : la visibilité des pickups/articles doit dépendre de l'ÉTAT DU CHECK (cache des
locations envoyées), jamais de la possession — patcher CheckIfAlreadyObtainedThisItem,
le filtrage boutique et l'usage de GrabbedItem() pour consulter le cache quand connecté.
Vérifier aussi que le prefix ObtainKeyItem envoie bien le check même si l'item est déjà
possédé (le garde vanilla `if ObtainedKeyItem return` ne doit pas l'empêcher).
Après fix : les emplacements masqués de la seed en cours doivent réapparaître
(visibilité recalculée). Ajouter un test manuel : recevoir un item AVANT son check,
vérifier que son emplacement reste ramassable et envoie le check.

1. **Observabilité des ramassages silencieux** (VerboseLogs) : logguer chaque
   interception avec verdict — `Check envoyé : X` / `Silencieux : X (déjà envoyé)` /
   `Silencieux : gulden vanilla (coinsanity off)` / `Silencieux : pas une location (raison)`.
2. **Log de session persistant** : BepInEx écrase LogOutput.log à chaque lancement ->
   log horodaté propre au mod, en append (ex. grunnchipelago_session.log), rotation simple.
3. **Résumé de resync à la connexion** : « Polaroids restaurés en monde : N » (+ tout
   autre resync GlobalData), pour visibilité.
4. **Audit specialItemTypes** : vérifier qu'aucun octroi ne contourne ObtainKeyItem
   (chemin HandleSpecialItem éventuel).
5. **Polaroid: Demon** : déplacer la location en région Hell (octroi introuvable
   statiquement ; règle plus stricte = pas de faux positif). FLAG playtest conservé.
6. **`_demo`** : suffixe de nommage, PAS du contenu démo (hideInDemo:False,
   startState:Show, aucun hider ; strangeKey0_demo = la clé de la pie). Garder les
   sources OR + commentaire de justification.
7. **Gulden #8** : Extérieur + `Hammer` (pot à casser) — règle à mettre à jour.
8. **Cabane -> Couloir final** : confirmé libre (aucun item, attendre dimanche soir).

## Considérations de design à discuter
1. **Consommables (Plank/OldPlank, Bone, etc.)** : la logique traite has_plank en
   booléen, mais les planches se CONSOMMENT (pont + gap = 2 usages potentiels dans une
   même run, max 2 items planche dans le pool). Cas limite : seed où l'Extérieur n'est
   atteignable QUE par le pont ET TallIdol requis -> conflit d'usage dans la run.
   À examiner : règle de logique « consumable-aware » ou garantie que la haie
   (can_cut_grass) reste une route alternative dans la logique.
2. **UX des pickups inertes** (location déjà checkée, item non possédé) : respawnent
   et ne donnent rien — les rendre visuellement distincts ou les masquer (lié aux
   features modèles #1/#2 du backlog).

## Observations de session (2026-07-13, en cours)
- Reconnexion : replay 13 items sans popup, buffs recomptés (speed x2, range x1, rate x1)
- death_link=True actif sur la seed de playtest
- Gulden #14 (Ferry) ramassé et loggé (coinsanity off -> vanilla, correct)
- 0 exception sur toutes les sessions observées
