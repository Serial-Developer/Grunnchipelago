# Prompt CC — Vérif coinsanity + spoiler chemin Hell (playtest ciblé)

NOUVELLE SESSION. Charge le contexte : `README.md`, `design/apworld_design.md`,
`design/playtest_notes.md`, et `apworld/grunn/` (surtout `rules.py`).
Règles permanentes : rien d'inventé (traçable au code/données), Jonath valide en jeu,
AUCUN push/PR distant sans confirmation. Objectif de cette session : DEUX livrables
ciblés, pas de refonte.

## Livrable 1 — Seed coinsanity de test + spoiler du chemin vers Hell

1. **Génère une seed fraîche** avec `coinsanity: true` (garde les autres options par
   défaut : goal true_ending, exclude_bridge_key on, death_link selon préférence de
   Jonath — demande s'il la veut on/off). Lance le serveur via `serve.bat`, donne à
   Jonath : slot, port, et la ligne exacte à mettre dans la config du mod.

2. **Extrais depuis `rules.py` (pas de mémoire) et produis un fichier spoiler**
   `dist/hell_access_spoiler_<seed>.md` listant, pour CETTE seed, TOUT ce qui est
   nécessaire pour atteindre la région Hell et compléter true_ending. Structure
   demandée, chaque ligne tracée à sa règle :
   - Les 4 idoles + la chaîne complète de chacune (items requis, régions, PNJ)
   - FlowerGem (arrosage / accès aux 4 macro-zones)
   - ChurchKey (chaîne du disque : Cd, OfficeKey, PC, Manoir)
   - Porte de l'église + accès Église
   - Puis dans Hell : AtticKey, MagicSword, PurifiedStone, les 3 SoulFragments
   - Pour chaque ITEM de progression requis : **où la génération l'a placé**
     (location + monde/joueur si multi) en lisant le fichier spoiler AP de la seed
     (`.archipelago` / spoiler log de Generate.py) — pour que Jonath sache quoi
     chercher et dans quel ordre.

3. **Polaroid: Demon — instrumentation dédiée** : Jonath ne l'a jamais vu se déclencher
   (hypothèse : dans Hell). Ajoute un log ciblé haute visibilité qui se déclenche à
   TOUTE collecte/solve de polaroid en signalant le PolaroidType exact
   (`[DEMON-HUNT] Polaroid collecté : <type> @ <zone joueur>`), + au chargement de
   Hell, logue la liste des Polaroid présents dans la scène de cette région. But :
   capturer où/quand Demon apparaît, ou prouver qu'il ne se déclenche jamais en jeu.

## Livrable 2 — Checklist coinsanity pour Jonath

Fournis une checklist de test in-game précise :
- Gulden #8 (pot, Extérieur) exige bien le Hammer ; popup diagnostic `Gulden #n (zone)`
  à chaque pièce (confirme le #2 = abri banc, Extérieur libre).
- Chaque gulden posé = 1 check ; item « Gulden » reçu du multiworld = +1 gulden réel.
- Logique monétaire : les achats (bus 10, Cd 5, Compass 4, OfficeKey 2, Medal 10)
  restent faisables ; la génération n'a pas exigé plus de gulden qu'il n'en existe.
- Rappelle à Jonath la commande `/send Grunn1 Gulden` (console serveur) s'il veut
  débloquer manuellement en cas de blocage de test (à noter pour retrait du bilan).

## Cadre
- Ne touche PAS aux features validées (UI, modèles, fins, sauvegarde). Session de test,
  pas de dev de fond.
- Prochaine session déjà planifiée (NE PAS commencer ici) : modèles distincts pour les
  items provenant d'autres jeux en multiworld (aujourd'hui tous rendus en fragment vert).
- Mets à jour `design/playtest_notes.md` avec les résultats.
