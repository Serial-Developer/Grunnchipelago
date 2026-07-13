# Prompt CC — Consolidation post-playtest round 1 (2026-07-13)

Bilan du playtest en cours de Jonath : un bug prioritaire, des corrections de règles,
de l'observabilité, du QoL et deux nouvelles options. Lis aussi
`design/playtest_notes.md` (contexte) avant de commencer. En cas d'ambiguïté : demande.
Ordre de traitement : section A d'abord (bug bloquant), puis B, C, D, E.

## A. BUG PRIORITAIRE — locations tuées par la possession de l'item

Symptôme observé en jeu : Medal et OfficeKey reçus du multiworld -> la marchande ne les
propose plus -> checks boutique inaccessibles. Même mécanique pour TOUT pickup dont
l'item arrive avant son check (sur la seed en cours : FlowerGem et ShyIdol reçus ->
serre des gnomes et idole fantôme vraisemblablement masquées).

Cause racine : l'octroi AP écrit dans keyItemsObtained ; le jeu masque tout
pickup/article dont l'item est possédé (`ItemPickup.CheckIfAlreadyObtainedThisItem`,
filtrage boutique, et l'event `GrabbedItem()` que tu as branché).

FIX (sémantique standard de randomizer) : la visibilité d'un pickup/article dépend de
l'ÉTAT DU CHECK (cache des locations envoyées), JAMAIS de la possession de l'item :
- location non envoyée -> pickup visible et ramassable, même si l'item homonyme est possédé
- location envoyée -> pickup masqué/inerte
À patcher : `CheckIfAlreadyObtainedThisItem`, le filtrage boutique, l'usage de
`GrabbedItem()`. Vérifier que le prefix `ObtainKeyItem` envoie le check même quand
l'item est déjà possédé (le garde vanilla `if ObtainedKeyItem return` ne doit pas
l'empêcher — le prefix s'exécute avant, confirme-le par test).
Critères : sur la seed en cours, les emplacements masqués réapparaissent ; test manuel
« item reçu avant son check -> l'emplacement reste ramassable et envoie le check ».

## B. Corrections de règles et de données (apworld)

1. **Gulden #2** : abri face à la Station essence (banc) — région Extérieur, accès
   LIBRE (source : Jonath in-game).
2. **Gulden #8** : Extérieur + `Hammer` requis (pot à casser).
3. **Polaroid: Demon** : déplace la location en région Hell (octroi introuvable
   statiquement ; règle plus stricte = pas de faux positif). FLAG playtest conservé.
4. **`_demo`** : suffixe de nommage, PAS du contenu démo — preuves : le mécanisme démo
   réel est `hideInDemo` + `SaveManager.demo` (ContentHider.cs:214), les trois pickups
   ont hideInDemo:False / startState:Show / aucun hider, et `strangeKey0_demo` est
   l'enfant de `magpieDeadByWorm0` (la clé de la pie, source canonique). Garde les
   sources OR + commentaire de justification.
5. **Cabane -> Couloir final** : confirmé libre (aucun item, attendre dimanche soir).
6. **Table clé -> porte (dump v0.3)** : le dumper capture désormais
   `Door.unlockItemNeeded`. Jonath va relancer le jeu pour produire le dump frais
   (`grunnchipelago_dump.json` à la racine du jeu, meta.dumper == 0.3.0 — VÉRIFIE la
   version avant usage ; s'il est encore en 0.2.0, demande le relancement).
   Croise alors TOUTES les règles de clés (GardenKey, ChurchKey, ToiletKey, BridgeKey,
   StrangeKey, AtticKey, OldKey, AbandonedKey) contre cette table et corrige les écarts,
   avec référence en commentaire.

## C. Observabilité et logs

1. **Ramassages silencieux** (VerboseLogs) : logue chaque interception avec verdict —
   `Check envoyé : X` / `Silencieux : X (déjà envoyé)` / `Silencieux : gulden vanilla
   (coinsanity off)` / `Silencieux : pas une location (<raison>)`.
2. **Log de session persistant** : BepInEx écrase LogOutput.log à chaque lancement —
   écris un log horodaté propre au mod, en append (`grunnchipelago_session.log` dans le
   dossier du plugin), rotation simple (ex. 2 Mo).
3. **Résumé de resync à la connexion** : « Polaroids restaurés en monde : N » + tout
   autre resync GlobalData.
4. **Audit specialItemTypes** : vérifie qu'aucun octroi ne contourne `ObtainKeyItem`
   (chemin `HandleSpecialItem` éventuel) — rapporte le résultat.

## D. QoL demandé par Jonath (retours in-game)

1. **Message d'item à la fin** : quand une fin envoie son check, aucun message
   n'indique l'item débloqué. Affiche le résultat du check de fin (item reçu en solo /
   « Envoyé : <item> -> <joueur> » en multi). Si les popups sont supprimés pendant les
   cutscenes de fin, mets-les en file et affiche-les à la nouvelle run.
2. **Dialogues de fin skippables** : plusieurs fins lancent un dialogue PNJ pénible à
   répéter. Ajoute une option config mod (ex. `SkipEndingDialogues`, défaut false) ou
   une touche de skip — localise le système de conversation dans le décompilé
   (Owner.cs / classes de conversation) et choisis l'approche la plus sûre.
3. **Popup de ramassage** (rappel du chantier pré-playtest, vérifie qu'il est bien
   livré) : le popup doit montrer le vrai contenu du check, pas l'objet vanilla vu.

## E. Nouvelle option (conditionnée au dump v0.3)

**`lock_player_hut`** (YAML, défaut off, marquée expérimentale) — idée de Jonath :
si la table clé->porte confirme qu'AUCUNE porte n'exige `AbandonedKey` (clé orpheline,
vestige : elle ouvrait vraisemblablement la cabane du joueur à l'origine), alors :
- Option ON : le mod verrouille la porte de la cabane du joueur et lui assigne
  `AbandonedKey` (côté mod : forcer locked + unlockItemNeeded au chargement)
- L'apworld classe alors AbandonedKey en progression et gate la région Cabane joueur
  (et ses dépendances : ToiletKey vanilla spot, chaîne du dimanche soir) par la clé
- Défaut OFF : comportement actuel, AbandonedKey reste un item useful sans usage
Si la table révèle au contraire une porte utilisant AbandonedKey : implémente
uniquement la correction de règle (section B.6) et signale-le, pas d'option.

## F. Design à discuter — NE PAS implémenter, donner ton avis seulement

1. **Consommables** : la logique traite has_plank en booléen mais les planches se
   consomment (pont + gap dans une même run). Analyse le risque réel de seed
   soft-lockée (l'alternative haie/can_cut_grass suffit-elle toujours dans la logique ?)
   et propose : règle consumable-aware, item Plank supplémentaire dans le pool, ou rien.
2. **UX des pickups inertes** (location envoyée, item non possédé -> respawn inerte) :
   options possibles en attendant les modèles custom du backlog.

## G. Rappels
- Relance les 31+ tests + génération all_endings après chaque section.
- Mets à jour design/playtest_notes.md (coche ce qui est traité).
- Commits atomiques par section. AUCUN push/PR distant sans confirmation de Jonath.

## H. UI in-game (ajouts Jonath, 2026-07-13 soir)

1. **Marqueur Archipelago au menu principal** : sous le titre du jeu, afficher un gros
   « ARCHIPELAGO » en ROUGE, avec la MÊME police que le titre (récupère l'asset de
   police du titre — TMP ou UI Text selon ce que le jeu utilise — et clone l'élément).
   Visible uniquement quand le mod est Enabled. Sert d'indicateur immédiat « cette
   partie tourne en mode Archipelago ».
2. **Panneau de stats (menus Tab et Pause)** : dans le coin supérieur DROIT, une liste
   des stats modifiées par les buffs/traps. Base = 100 % ; afficher la valeur courante
   calculée depuis les multiplicateurs réels du mod :
   - Vitesse : 100 % + 15 %/palier de Move Speed Boost (ex. 2 paliers = 130 %) ;
     pendant un Speed Trap : valeur x0,5 (ex. 130 % -> 65 %)
   - Portée sécateur : 100 % + 25 %/palier
   - Cadence de découpe : 100 % + 25 %/palier
   - Taille : 100 % ; pendant un Size Trap : 45 %
   - Contrôles : « Normaux » / « INVERSÉS »
   Pour chaque trap actif, afficher la durée restante en temps de JEU (ex. « 0h34 »).
   Affiché uniquement si connecté. N'affiche que les lignes différentes de la base OU
   toutes les lignes — au choix de Jonath, prévois un booléen de config.
