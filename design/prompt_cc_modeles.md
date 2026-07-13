# Prompt CC — Modèles d'objets (features #1 et #2 du backlog)

PRÉREQUIS : à lancer APRÈS `prompt_cc_global_playtest1.md` (en particulier le fix du
bug A — la visibilité des pickups pilotée par l'état des checks — dont ce chantier
dépend directement). En cas d'ambiguïté : demande. Jonath garde l'autorité sur toute
décision visuelle — propose, ne tranche pas seul.

## Architecture commune : scouting

Pour afficher le bon modèle sur un emplacement, il faut connaître son contenu AVANT
ramassage. Utilise le scouting AP (`Session.Locations.ScoutLocationsAsync` sur toutes
les locations du slot, une fois à la connexion, caché par seed+slot). Chaque location
scoutée fournit : item (id/nom), joueur destinataire, et `ItemFlags`
(progression / useful / filler / trap). Hors connexion : visuels vanilla, aucun swap.

## Feature #1 — Modèles concrets (items Grunn locaux)

Objectif : si l'emplacement des cisailles contient le marteau, on voit le marteau.

1. **Bibliothèque de modèles** : au chargement du monde, construis un dictionnaire
   KeyItem/outil -> visuel, en récoltant les enfants « visuals » des ItemPickups
   existants de la scène (chaque item posé fournit son propre modèle). Complète avec
   les prefabs accessibles (KeyItemData/ItemObject) pour les items sans pickup posé —
   liste ce que tu ne parviens PAS à couvrir.
2. **Swap** : pour chaque pickup porteur d'une location dont le contenu scouté est un
   item Grunn du MÊME monde -> remplace le visuel par celui de l'item contenu
   (position/échelle raisonnables ; conserve collider et interaction d'origine).
   Application au chargement du monde et à chaque nouvelle run.
3. **Boutique** : même traitement pour les articles de la marchande et de la station.
4. **Cohérence popup** : avec le modèle juste, le popup de ramassage (D.3 du prompt
   global) doit nommer le même item — vérifie l'alignement.

## Feature #2 — Modèles « Archipelago » (multiworld)

Objectif : les emplacements contenant des items d'AUTRES mondes (ou des items Grunn
non couverts par la bibliothèque) affichent un modèle AP par classification.

1. **Trois modèles distincts** requis : progression / useful / filler. PROPOSE à
   Jonath 2-3 approches concrètes avec captures avant d'implémenter, par exemple :
   (a) un prop existant du jeu recoloré par classification (teinte matériau),
   (b) une construction simple de primitives évoquant le logo Archipelago,
   (c) un polaroid/carton retexturé. Jonath choisit la direction artistique.
2. **Traps déguisés** : quand `ItemFlags` = trap, affiche aléatoirement le modèle
   useful OU progression — aléa DÉTERMINISTE par location (seed+location id), stable
   entre sessions, pour ne pas trahir le trap en relançant.
3. Les items destinés à d'AUTRES joueurs utilisent toujours ces modèles AP (même si
   l'item existe dans Grunn) — c'est la convention des grands randomizers, et ça rend
   le multiworld lisible.

## Annexe — vérification des features #3 et #4 (livrées, non validées en jeu)

1. **#3 Bone près du spawn** : confirme l'état d'implémentation (le Bone reçu ne doit
   JAMAIS être injecté en inventaire ; un pickup monde apparaît près du départ, hors
   du bus ; son ramassage n'envoie aucun check). Décris à Jonath le comportement
   exact attendu et où regarder pour qu'il le valide dans sa run.
2. **#4 Popup de ramassage** : confirme que le popup montre le contenu réel du check
   et non l'objet vanilla vu (chantier pré-playtest c). Si non livré, c'est D.3 du
   prompt global — traite-le là-bas.

## Rappels
- Tests + génération après chaque feature ; commits atomiques.
- Mets à jour design/apworld_design.md section 10 (statuts) et playtest_notes.md.
- AUCUN push/PR distant sans confirmation explicite de Jonath.
