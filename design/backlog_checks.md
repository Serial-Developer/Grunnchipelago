# Backlog — nouveaux checks (demande Jonath, 2026-07-28)

> ## ✅ 7 des 8 sont LIVRÉS (2026-07-28)
> Après les réponses de Jonath sur les trois points ouverts, les checks 1, 2, 3, 4, 6, 7 et 8
> sont implémentés : nouvelle catégorie **`deed`** (ids `478661501..507`), locations logiques
> en région `Menu`, règles dans `rules.DEED_RULES`, patches client dans `DeedPatches.cs`.
> 40 tests OK, build 0 warning. **Reste le n° 5 (plantes en pot), reporté post-launch.**
>
> Réponses de Jonath qui ont levé les incertitudes :
> - **Ver** : c'est bien dans la zone de l'Église géographiquement, mais **accessible
>   uniquement depuis Hell** ⇒ règle `reach(HELL)`.
> - **Escargot** : la course se termine **vers 23h45 le jour 2**, il faut donc pouvoir dormir
>   ⇒ `can_advance_days` ajouté à la règle (décisif sous `lock_player_hut`).
> - **Plantes en pot** : les pots sont éparpillés, la logique serait trop compliquée à
>   établir ⇒ **post-launch**, non implémenté.

Huit checks demandés par Jonath en fin de playtest. Ce fichier verrouille le travail de
repérage : pour chacun, le hook réel dans le code décompilé, la position au dump, la règle
apworld, et les écarts relevés entre la description initiale et ce que dit le jeu.

> Règles du projet rappelées : rien d'inventé (tout tracé au code décompilé ou au dump),
> **Jonath valide en jeu**, aucun push distant. Il n'y a **aucun remote git** sur ce projet —
> d'où ce fichier plutôt que des issues GitHub (choix Jonath 2026-07-28).

## Travail commun à TOUS ces checks

Chacun demande le même pipeline, quelle que soit la difficulté du hook :

1. un **id** dans `design/ids.json` + `apworld/grunn/data/ids.json` (bloc `locations`) ;
2. la **location** dans `apworld/grunn/locations.py` (+ sa région) ;
3. la **règle** dans `apworld/grunn/rules.py` ;
4. le **patch client** qui envoie le check (postfix Harmony, modèle : `MagicPondPlaceFishPatch`) ;
5. un **test** dans `test_generation.py` + les 40 existants au vert.

⚠️ **Parité items/locations** : ajouter N locations sans ajouter N items casse la parité —
`create_filler` compense automatiquement (voir `items.get_filler_item_name`), mais le compte de
traps/buffs monte d'autant. À surveiller sur 8 checks d'un coup.
⚠️ Toute seed existante devient périmée : ces checks n'existent pas dans son datapackage.

**Bonne nouvelle : 7 des 8 hooks sont triviaux.** Le jeu expose à chaque fois une méthode
statique dédiée avec son propre flag `ProgressData`, exactement le pattern déjà utilisé par
`MagicPondPlaceFishPatch`. Le travail réel est le pipeline ci-dessus, pas la recherche du hook.

---

## 1. Jeter la pizza à la poubelle

| | |
|---|---|
| **Hook** | `GameManager.ClearPizzaBox()` — GameManager.cs:4346, flag `clearedPizzaBox` |
| **Interaction** | `InteractionType.PizzaBoxClear` → `Main/Areas/Road/Container/Hide_Road/pizzaClearInteraction0` |
| **Position** | (-52.7, 11.64, -50.01), hors macro-zone (la Route) ⇒ région **Extérieur** |
| **Prevent** | `ClearedPizzaBox`, `KeyItemNotObtained` (il faut posséder la PizzaBox) |
| **Règle proposée** | `reach(EXTERIEUR) and has("PizzaBox")` |
| **Difficulté** | Triviale — postfix sur `ClearPizzaBox`. |

Note : la méthode fait déjà `AddPolaroidSolved(HangjongPizzaBox)` — le polaroïd est un check
séparé, pas de collision.

## 2. Planter la jolie fleur au cimetière de l'église

| | |
|---|---|
| **Hook** | `GameManager.PutPrettyFlowerInVase()` — GameManager.cs:6058, flag `putPrettyFlowerInVase` |
| **Interaction** | `InteractionType.PutPrettyFlowerInVase` → `.../Hide_ChurchMid/PrettyVaseContainer/PrettyVase_Interactions/PrettyVase_placeFlower0` |
| **Position** | (1.24, 10.13, -10.28), `MACRO:Church` ⇒ région **Église** ✔ conforme |
| **Prevent** | `KeyItemNotObtained`, `PutPrettyFlowerInVase` |
| **Règle proposée** | `reach(EGLISE) and has("PrettyFlower")` |
| **Difficulté** | Triviale — postfix. |

⚠️ Le jeu appelle ça un **vase** (`PrettyVaseContainer`), pas une plantation. Même endroit, même
geste — seul le vocabulaire diffère. À nommer côté location : « Place PrettyFlower (Church) ».

Rappel de chaîne : `Obtain PrettyFlower` exige déjà `SpecialSeed` + `WateringCan` + jour 3
(`can_advance_days`). Ce nouveau check hérite donc de tout ça **implicitement** via `has(PrettyFlower)` —
c'est un des plus profonds de la liste.

## 3. Donner les 3 instruments à la fanfare du parc

| | |
|---|---|
| **Hook** | `GameManager.KidReadyTrigger()` — GameManager.cs:6008, se déclenche quand `schoolbandCompleteIndex >= 3` |
| **Alimenté par** | `KidGiveTrumpet()` (5970), `KidGiveCymbals()` (5983), `KidGiveTriangle()` (5996) — chacun incrémente le compteur |
| **Position** | Parc (les 3 enfants) |
| **Règle proposée** | `reach(PARC) and has_all(("KidTrumpet", "KidCymbals", "KidTriangle"))` |
| **Difficulté** | Triviale — postfix sur `KidReadyTrigger` **en testant `schoolbandCompleteIndex >= 3`** (la méthode est aussi appelée à 1 et 2 instruments). |

Le jeu débloque déjà l'achievement `NEW_ACHIEVEMENT_1_18` à ce moment précis : bon repère.

## 4. Mettre le poisson rouge vivant dans son bocal

| | |
|---|---|
| **Hook** | `Fishbowl.PlaceFishAlive()` (via `InteractionType.FishbowlPlaceFishAlive`, Interaction.cs:370) |
| **Interaction** | `NonEuclidian/RoundHallway/.../fishbowl0/Fishbowl_interactions/fishbowl_place_fish_alive0` |
| **Position** | (402.57, 7.05, 1004.64), `RoundHallway` ⇒ région **PassageGnomes** ✔ conforme |
| **Prevent** | `FishbowlStateIsNotEmpty`, `KeyItemNotObtained` |
| **Règle proposée** | `reach(PASSAGE_GNOMES) and has("GoldFishAlive")` |
| **Difficulté** | Triviale — postfix sur `Fishbowl.PlaceFishAlive`. |

C'est **le check « poisson vivant dans le bocal »** évoqué le 2026-07-27. Il reste bien SÉPARÉ
d'`Obtain GoldFishAlive` (qui part au dépôt du poisson mort au Magic Pond) et ne rouvre PAS la
fausse route de réanimation par le bocal : le bocal ne ressuscite toujours pas
(`FishbowlRetrieveDeadFish` n'a pas d'équivalent « retrieve alive »).

⚠️ Rappel : `has("GoldFishAlive")` suppose le poisson **en inventaire**, or `PASSAGE_GNOMES`
n'exige que le Hammer alors que `GoldFishAlive` exige `reach(MAGIC_POND)` — qui passe par
`CABANE_JOUEUR → COULOIR_FINAL`. Ce check sera donc profond, à vérifier en génération.

## 5. Tailler toutes les plantes en pot

| | |
|---|---|
| **Hook** | `PottedPlant.Trim` — PottedPlant.cs:84-90, incrémente `pottedPlantTrimmedCur` |
| **Seuil** | `pottedPlantTrimmedCur >= SaveManager.trimmedPottedPlantMax`, et **`trimmedPottedPlantMax = 8`** (SaveManager.cs:1350) — achievement `NEW_ACHIEVEMENT_1_24` |
| **Position** | ❓ **INCONNUE** — les plantes en pot **n'apparaissent pas dans le dump v0.3** (0 occurrence) : ce ne sont ni des ItemPickup, ni des Interaction, ni des ContentHider |
| **Règle proposée** | ❓ à déterminer — il faut d'abord savoir OÙ sont les 8 pots |
| **Difficulté** | **La seule vraiment bloquante.** |

Jonath : « trop complexe à décrire ». Le code donne pourtant le critère exact (8 pots taillés) ;
ce qui manque, c'est **leur emplacement**, donc la règle de logique. Deux façons de le lever :
- **étendre le dumper** pour capturer `GameManager.allPottedPlants` (positions + zone) — propre,
  réutilisable, dans l'esprit du dump v0.3 ;
- ou une **sonde temporaire** dans le mod qui logue les 8 positions au chargement.

Tant que ce n'est pas mesuré, toute règle serait devinée. Ne pas l'implémenter au jugé : un
outil de taille manquant (`Shears` ? `MagicSword` ?) rendrait le check faux dans un sens ou
dans l'autre.

## 6. Placer le ver avec les autres vers ⚠️ (« dans Hell » — À CONFIRMER)

| | |
|---|---|
| **Hook** | `GameManager.ReturnWorm()` — GameManager.cs:5887, flag `returnedWorm` |
| **Interaction** | `InteractionType.WormReturn` → `Main/Areas/ChurchArea/Container/Hide_ChurchCorner/WormHillContainer/wormHill0/wormReturn_interaction0` |
| **Position** | **(50.59, 10.26, -9.48)** — coin de la zone **Église**, dans le monde NORMAL |
| **Prevent** | `WormReturned`, `NotHasWormAndCanBeUsed` |
| **Difficulté** | Hook trivial, mais la RÉGION est à confirmer. |

🔴 **Écart avec la description de Jonath.** Il annonce « dans Hell ». Le dump place la butte aux
vers (`wormHill0`) dans **`Main/Areas/ChurchArea`**, coordonnées du monde normal — Hell est en
`NonEuclidian` (x ≈ 2000+). Trois lectures possibles, à trancher **en jeu** :
1. la butte est bien à l'Église et n'apparaît qu'en version Hell de la zone (conteneur
   `Hide_ChurchCorner` révélé par le scénario) ⇒ règle `reach(HELL) and has("Worm")` ;
2. elle est accessible dans le monde normal ⇒ règle `reach(EGLISE) and has("Worm")` ;
3. les deux, selon le moment.

⇒ **Ne pas implémenter avant vérification de Jonath.** L'écart entre les deux règles est énorme
(Église = sphère précoce, Hell = fin de partie).

## 7. Offrir la médaille à l'escargot du PillarSpace

| | |
|---|---|
| **Hook** | `Snail.Award()` — Snail.cs:177, flag `awardedSnail`, consomme `KeyItem.Medal` |
| **Interaction** | `InteractionType.SnailAward` → `NonEuclidian/PillarSpace/Hide_PillarSpace/SnailContainer/Snail_award0` |
| **Position** | (1188.86, 10.28, 990.36), `PillarSpace` ✔ conforme |
| **Prevent** | **`SnailStateIsRacing`**, `KeyItemNotObtained` |
| **Règle proposée** | `reach(PILLAR_SPACE) and has("Medal")` (+ jour 2 ? voir ci-dessous) |
| **Difficulté** | Hook trivial ; c'est la CONDITION temporelle qui est à lever. |

⚠️ **Écart à lever.** Jonath annonce « PENDANT le jour 2 ». Le prevent du dump ne parle pas de
jour mais de l'**état de la course** : l'escargot doit avoir **fini de courir**
(`SnailStateIsRacing` bloque). Si la course est elle-même datée au jour 2, la condition de Jonath
est vraie mais **indirecte** — il faut alors ajouter `can_advance_days` à la règle (important
sous `lock_player_hut`). À confirmer dans `Snail.cs` (machine à états) + en jeu.

Rappel : `Obtain Medal` = boutique Hooibaal à **10 gulden** — sous coinsanity ce check tire
l'économie sur le chemin.

## 8. Rapporter la main coupée au cadavre

| | |
|---|---|
| **Hook** | `GameManager.ReturnSeveredHand()` — GameManager.cs:4242, flag `returnedSeveredHand` |
| **Interaction** | `InteractionType.ReturnSeveredHand` → `Main/Areas/Bunker/Container/bunkerExtraContainer/Hide_Bunker0/returnSeveredHand0` |
| **Position** | (-90.09, 8.91, 80.52), **areas = ['Bunker']** |
| **Prevent** | `KeyItemNotObtained`, `ReturnedSeveredHand` |
| **Règle proposée** | `reach(BUNKER) and has("SeveredHand")` |
| **Difficulté** | Triviale — postfix. |

⚠️ Petite précision sur la description : Jonath dit « accès à l'extérieur », le dump dit
**Bunker**. Dans l'apworld le Bunker est une région à part (`c.BUNKER`), reliée librement depuis
l'Extérieur — donc `reach(BUNKER)` est équivalent en pratique mais plus fidèle.

Rappel de chaîne : `Obtain SeveredHand` exige déjà `reach(CABANE_JOUEUR)` (le screamer du TallMan
ne se déclenche qu'après être entré dans la cabane) ⇒ sous `lock_player_hut`, ce check passe
derrière l'`AbandonedKey`.

---

## Récapitulatif

| # | Check | id | Location | Règle | Statut |
|---|---|---|---|---|---|
| 1 | Pizza à la poubelle | 478661501 | `Deed: Throw Away PizzaBox` | `Exterieur + PizzaBox` | ✅ livré |
| 2 | Jolie fleur au vase | 478661502 | `Deed: Place PrettyFlower in Vase` | `Eglise + PrettyFlower` | ✅ livré |
| 3 | Fanfare complète | 478661503 | `Deed: Complete the School Band` | `Parc + les 3 instruments` | ✅ livré |
| 4 | Poisson vivant au bocal | 478661504 | `Deed: Place GoldFishAlive in Fishbowl` | `PassageGnomes + GoldFishAlive` | ✅ livré |
| 5 | Toutes les plantes en pot | *478661508 réservé* | — | — | ⛔ **post-launch** (position des 8 pots absente du dump) |
| 6 | Ver rendu à la butte | 478661505 | `Deed: Return Worm to the Worm Hill` | `Hell + Worm` | ✅ livré |
| 7 | Médaille à l'escargot | 478661506 | `Deed: Award Medal to the Snail` | `PillarSpace + Medal + can_advance_days` | ✅ livré |
| 8 | Main rendue au cadavre | 478661507 | `Deed: Return SeveredHand` | `Bunker + SeveredHand` | ✅ livré |

### Effets de bord de la livraison (à connaître)

- **8 items passent en `progression`** (`items.PROGRESSION_ITEMS`) : `PizzaBox`,
  `PrettyFlower`, `KidTrumpet`, `KidCymbals`, `KidTriangle`, `GoldFishAlive`, `Medal`,
  `SeveredHand`. Obligatoire : sans ça `state.has()` ne les voit pas et les deeds sont
  injouables en logique (`test_all_state_can_reach_everything` l'a attrapé du premier coup).
  Conséquence : le fill les traite désormais comme de la progression, donc ils pèsent sur les
  sphères.
- **+7 locations ⇒ +7 items filler** (parité maintenue par `create_filler`), soit un peu plus
  de traps/buffs dans le pool.
- **Aucune option** ne gouverne ces checks : contrairement à `polaroid_checks` /
  `ghost_checks` / `coinsanity`, ils sont **toujours actifs**, comme les `Obtain X` et les
  fins. À dire si tu veux une option `deed_checks`.
- **Les seeds existantes sont périmées** : ces locations n'existent pas dans leur datapackage.
  Le client dégrade proprement (`SendByName` logue « pas une location » et n'envoie rien).
