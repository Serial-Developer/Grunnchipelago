# Prompt CC — Suite playtest coinsanity + lock_player_hut

NOUVELLE SESSION. Charge le contexte : `README.md`, `design/apworld_design.md`,
**`design/playtest_notes.md`** (le plus important : il contient tout l'historique des bugs
et des décisions de la session précédente), et `apworld/grunn/` (surtout `rules.py`,
`regions.py`, `locations.py`).

Règles permanentes : **rien d'inventé** (tout traçable au code décompilé, au dump ou à un
retour in-game de Jonath), **Jonath valide en jeu**, **AUCUN push/PR distant sans
confirmation**. Tout est local, rien n'est commité.

---

## 0. Méthode — lis ça avant tout (leçons payées cher)

La session précédente a produit plusieurs erreurs évitables. Applique ces règles :

1. **MESURER, PAS DEVINER.** Trois diagnostics faux d'affilée sur un modèle 3D invisible
   avant d'instrumenter le mod (log de position/bounds) — la mesure a donné la réponse en
   une passe. Dès qu'une hypothèse ne tient pas du premier coup : ajouter un log ciblé et
   relancer, plutôt que d'enchaîner les suppositions.
2. **Un check n'est valide que si son PICKUP existe ET est atteignable en jeu.** Vérifier :
   objet présent dans `GameManager.allPolaroids` / `allItemPickups`, hider de révélation,
   et absence de `RemoveUnusedPolaroid`. 6 checks fantômes ont déjà été retirés (§2).
3. **Retirer un item du pool ≠ retirer sa location.** Se poser les deux questions.
4. **Ne JAMAIS réduire le périmètre demandé.** Si une option semble cassée : le signaler,
   proposer, et laisser Jonath trancher. Ne pas la désactiver d'autorité.
5. **Vérifier ce qu'une porte ENFERME**, pas seulement si sa clé est atteignable
   (le lit est dans le cabanon → `lock_player_hut` bloque le sommeil).
6. **Nom de fichier de seed TOUJOURS unique** (`Grunn1_<qqch>_s<seed>.archipelago`). Le
   serveur nomme son `.apsave` d'après le FICHIER : réutiliser un nom fait rejouer les
   items de la seed précédente (contamination silencieuse, déjà arrivé).
7. Avant de livrer une seed : **vérifier explicitement la sortie du jardin** et que les
   items requis ne sont pas piégés.

---

## 1. État actuel

- **Seed en cours** : `dist/Grunn1_hut_s08873442762073113111.archipelago`
  (seed affichée `66373763042940781548`, **116 locations**), `serve.bat` pointe dessus.
  Options : `true_ending`, coinsanity **on**, keep_shears **on**, **lock_player_hut ON**,
  death_link off. Slot `Grunn1`, port `38281`.
- **Aucun spoiler n'a encore été généré pour cette seed** — c'est la première tâche si
  Jonath la joue (voir §4).
- **40 tests OK** (`python scripts/sync_apworld.py` puis
  `cd Archipelago && .venv313/Scripts/python -m unittest worlds.grunn.test.test_generation`).
- Rien n'est commité (`git status` : ~13 fichiers modifiés + `DemonHunt.cs` non suivi).

## 2. Contenu mort retiré du pool (ne pas le réintroduire)

`locations.UNSOURCED_LOCATIONS` — chacun prouvé, pas supposé :

| Location | Preuve |
|---|---|
| `Calm Ghost #8 (PillarSpace)` | 7 GhostTouch, `ghostCalmMax=7` |
| `Polaroid: Demon` | aucun octroi dans tout l'assembly ; boucle « collect all » dans du code mort |
| `Polaroid: VoidSkeleton` | `RemoveUnusedPolaroid(VoidSkeleton)` appelé à chaque load |
| `Polaroid: GardenGnomes` | jamais instancié dans `allPolaroids` alors que Jonath était à 3 m |
| `Obtain OldKey` | clé non utilisable en jeu (écartée par Jonath) |
| `Obtain AbandonedKey` | aucun check vanilla ; la clé sert uniquement à verrouiller le cabanon |

⚠️ L'**ITEM** `AbandonedKey` reste dans le pool (nécessaire à `lock_player_hut`), seule sa
location est retirée.

## 3. Règles de logique ajoutées/corrigées (session précédente)

- `can_advance_days` (**`rules.py`**) = `not lock_player_hut or has(AbandonedKey)` — le LIT
  est dans le cabanon. Appliqué à : `Ending: Mist` (j3), `Ending: Bus` (`dayIndex<=1`
  refusé), `Calm Ghost #3` (redCar, j2), `Calm Ghost #4` (scooter, j2),
  `Obtain PizzaBox` (j2), `Obtain PrettyFlower` (j3).
- `reach(CABANE_JOUEUR)` sur `Obtain SeveredHand`, `Polaroid: MagpieNest`,
  `Polaroid: TallManWindow` (exigent d'être entré dans la cabane).
- `Obtain PrettyFlower` = `SpecialSeed` + **`WateringCan` nommément** (pas `can_water` :
  la pluie n'est pas répétable jusqu'au j3) + j3. `SpecialSeed` est passé en progression.
- `Obtain Worm` = Jardin + **`Apple`** (assiette ; les 2 autres vers sont circulaires).
- `garden_30` (pie ≥ 30 %) = tondre **ou** arroser — **la truelle seule ne suffit pas**.
- PillarSpace = **`Doorknob`** (pas les 20 % de l'Église).
- Parc : **3 routes** (Briquet / Rame / **Marteau via passage des gnomes**, rendu
  bidirectionnel).
- `GoldFishAlive` = **MagicPond uniquement** (le bocal ne réanime pas).
- **Ferry : AUCUNE contrainte de jour** (erreur corrigée).

## 4. À FAIRE — par ordre de priorité

### 4.1 Spoiler de la seed en cours
Produire `dist/hell_access_spoiler_hut.md` pour
`Grunn1_hut_s08873442762073113111` : chemin vers Hell, les 11 items requis avec leur
location, la chaîne `AbandonedKey`, et ce que le lock gate. Format : voir les spoilers
précédents dans `dist/`.

### 4.2 Nettoyer l'instrumentation temporaire
`Grunnchipelago.Client/DemonHunt.cs` contient encore :
- `[DEMON-HUNT]` (hooks polaroids + dump de scène Hell/Crypt) — **a rempli sa mission**
  (a prouvé Demon mort), et **spamme le log en boucle dans le Crypt** ;
- `[DIAG-POLAROID]` (watchlist Tent / GardenGnomes / VoidSkeleton).

Demander à Jonath s'il veut tout retirer (le fichier entier + l'appel `DemonHunt.Tick()`
dans `Plugin.Update`), ou garder la watchlist le temps de lever le cas `Tent`.

### 4.3 Règle du passage des gnomes — trop stricte (non bloquant)
`regions.py` exige `Hammer` pour `JARDIN <-> PASSAGE_GNOMES <-> PARC`. Or le code
(`TriggerGnomeJumpscare`, ligne ~29071) pose `destroyedAllJumpscareGnomes` dès que :
**nain de jardin cassé (`Hammer|MagicSword|Trowel`) + entrer dans la zone station**.
Il existe aussi `DestroyedAllJumpscareGnomes()` (destruction directe des nains).
⇒ La règle prive de routes légitimes. À corriger après validation de Jonath.

### 4.4 Suspect non levé : `Polaroid: Tent`
0 collecte sur tout l'historique, mais **aucune preuve qu'il soit mort**. Position réelle :
`(20.45, 10.66, 32.8)`, zone **Église**, **à 2,16 m du PUITS** — PAS dans la tente
(les noms de polaroïds décrivent le SUJET de la photo, pas le lieu : `Polaroid: Well` est,
lui, DANS la tente). Aucun hider ne le cible. Le log `[DIAG-POLAROID] Tent ...` le
confirmera vivant dès que Jonath passera le ramasser.

### 4.5 Idées en attente (demandes Jonath, non commencées)
- **Check « poisson vivant dans le bocal »** : Jonath veut peut-être l'ajouter plus tard
  (nouvelle location + règle) — c'est un check SÉPARÉ, ça ne réactive pas la réanimation.
- **Modèles distincts pour les items d'AUTRES joueurs** en multiworld (backlog
  `apworld_design.md` §10 feature #2, DA à trancher par Jonath).

## 5. Correctifs mod déjà livrés (ne pas régresser)

- **Owner/AtticKey** : `Owner.EndConversation` ne donnait la clé que si non possédée ⇒
  postfix qui envoie le check quoi qu'il arrive.
- **Magic Pond** : le check `Obtain GoldFishAlive` part au **DÉPÔT du poisson mort** ;
  hider dé-gaté de la possession ; modèle = contenu du check.
- **Gulden (coinsanity)** : visibilité = **état du check** (ne respawnent plus au reset de
  run), modèle du contenu, masquage additif (`checkSent || GuldenGrabbedThisRun`).
- **Ver/pomme** : clone masqué jusqu'au flag `placedApple` (per-run, se re-masque seul).
- **GoldFishAlive** : modèle récolté sur `FishAliveContainer` (aquarium) **et** sur l'objet
  porteur du mesh (sinon décalage de 11 m).
- **`lock_player_hut`** : `HutLock` verrouille `playerSchuurDoor` + re-verrouille par run.

---

## Cadre
- Ne touche PAS aux features validées (UI, fins, sauvegarde par seed, DeathLink).
- Mets à jour `design/playtest_notes.md` avec les résultats.
- Si un doute logique apparaît : **demander à Jonath**, il connaît le jeu par cœur et ses
  retours in-game ont systématiquement eu raison contre les déductions théoriques.
