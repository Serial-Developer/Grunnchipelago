# Notes de playtest — à consolider dans le prompt CC global (en cours, 2026-07-13)

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

### Bloc 3 — Fins & profil de sauvegarde : plan 3.1 rédigé ; décision Jonath :
traité EN DERNIER (3.2 liste des fins d'abord possible, sur l'état AP).

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

## Considérations de design à discuter (Jonath + Claude)
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
