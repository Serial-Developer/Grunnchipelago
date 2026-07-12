# Prompt Claude Code — APWorld Grunn (à exécuter EN PREMIER)

Tu vas créer l'APWorld Archipelago du jeu Grunn. Tout le design est décidé et documenté —
ton travail est une implémentation fidèle, pas de la conception. En cas d'ambiguïté ou de
trou dans la spec : POSE LA QUESTION, n'invente rien.

## Lecture obligatoire avant d'écrire du code

1. `C:\Users\jonat\Desktop\Projets\Grunnchipelago\design\apworld_design.md` — la spec (goals, pools, options, persistance, hooks)
2. `C:\Users\jonat\Desktop\Projets\Grunnchipelago\design\regions_v3.md` — graphe de régions, connexions, règles d'accès, équivalences
3. `C:\Users\jonat\Desktop\Projets\Grunnchipelago\design\ids.json` — IDs et noms de TOUS les items/locations (source de vérité, NE PAS renuméroter)
4. `C:\Users\jonat\Desktop\Projets\Grunnchipelago\dump\zone_logic.md` — données exhaustives si besoin de détails

## Environnement

- Demande à Jonath le chemin de son checkout Archipelago local (il en a un pour MHFU :
  `C:\Users\jonat\Desktop\Archipelago\mhfu`) ou clone ArchipelagoMW/Archipelago (main).
- Cible : Archipelago >= 0.6.x, API World standard.
- Crée `worlds/grunn/` dans ce checkout.

## Fichiers à produire

- `__init__.py` — classe `GrunnWorld(World)` : game = "Grunn", création items/régions,
  fill_slot_data (options utiles au mod : goal, coinsanity, persistent_shortcuts, death_link)
- `items.py` — table générée depuis `ids.json` (items + classification
  progression/useful/trap/filler selon apworld_design.md §4)
- `locations.py` — table générée depuis `ids.json` (5 catégories : keyitem/ending/polaroid/ghost/gulden)
- `options.py` — les options du tableau §7 de apworld_design.md, défauts EXACTS :
  goal (good_ending / true_ending [défaut] / all_endings), keep_shears (off),
  exclude_garden_key (ON), polaroid_checks (on), ghost_checks (on), coinsanity (off),
  persistent_shortcuts (off), death_link (off), trap/buff quantités (valeurs
  raisonnables, commentées « à calibrer »)
- `regions.py` — le graphe de regions_v3.md, régions et connexions avec leurs règles
- `rules.py` — helpers d'équivalence + règles d'accès
- `test/` — au moins un test de génération (unittest AP standard)

## Modélisation de la logique (décisions actées, regions_v3.md fait foi)

- Helpers : `can_cut_grass = Shears|MagicSword` ; `can_water = WateringCan|(Coin ET accès Église)` ;
  `has_plank = Plank|OldPlank` ; `can_get_bone = Hammer|MagicSword|Trowel|accès Manoir`
- Jours/heures (DayIndexIs, IsFinalDay, fenêtres horaires) : logiquement LIBRES (dormir suffit)
- Église : DEUX accès — portail jardin (ChurchKey) OU Extérieur (libre) ; Extérieur :
  DEUX accès — pont (has_plank) OU haie de la cour (can_cut_grass)
- MagicSword : source logique = grenier du Manoir (chaîne du disque : gulden -> Cd +
  OfficeKey -> PC bureau station -> Void -> Manoir) ; l'épée du parc (post-bonne-fin)
  est IGNORÉE par la logique
- Économie sans coinsanity : la tonte est un revenu vanilla renouvelable — tout coût en
  gulden est logiquement couvert par can_cut_grass
- Coinsanity ON : coûts = count("Gulden") >= prix (bus 10, Cd 5, Compass 4, OfficeKey 2, Medal 10)
- Complétion de zone (20 % / 100 %) : nécessite l'accès à la zone + can_cut_grass +
  can_water (+ Trowel pour les taupes, à vérifier dans zone_logic si besoin)
- Une location par item logique : les multi-emplacements (Lighter x4, OfficeKey x2) sont
  des routes OR dans la règle de la location correspondante
- exclude_garden_key ON -> GardenKey en placement vanilla local ; keep_shears ON -> idem Shears
- Goals : good_ending = event GoodEnd ; true_ending = GoodEnd + restauration d'âme
  (3 SoulFragments) ; all_endings = les 11 events de fin

## Critères d'acceptation

1. `python -m worlds.grunn` inexistant — le test standard : génération solo réussie avec
   le YAML template, et avec chaque valeur de goal
2. Génération multi (2+ mondes Grunn) sans erreur de fill
3. Aucun ID/nom divergent de ids.json
4. Rien d'inventé : chaque règle de rules.py doit être traçable à une ligne de
   regions_v3.md — mets la référence en commentaire (ex. `# regions_v3: Cour -> Extérieur`)

## Style

- Code et docstrings en anglais (conventions AP), réponses à Jonath en français
- Commits atomiques si dépôt git présent, sinon ne pas initialiser sans demander
