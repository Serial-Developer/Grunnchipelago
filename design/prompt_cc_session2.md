# Prompt CC — Session 2 : UI, modèles, fins & profil de sauvegarde

NOUVELLE SESSION. Contexte à charger d'abord : `README.md`, `design/apworld_design.md`
(spec, sections 8 et 10), `design/playtest_notes.md`, et le code de
`Grunnchipelago.Client/`. Sources décompilées du jeu :
`C:\Users\jonat\Desktop\Archipelago\Jeux\Grunn\analysis\decompiled\`.
Règles permanentes : rien d'inventé (chaque hook cite sa ligne décompilée), Jonath
valide en jeu entre chaque bloc, AUCUN push/PR distant sans confirmation explicite.
Leçons des sessions précédentes à respecter : tout clone Unity naît sous un parent
INACTIF avant nettoyage de ses scripts ; UI sur notre canvas overlay dédié (jamais de
clonage d'éléments du canvas du jeu) ; sémantique randomizer = visibilité pilotée par
l'état des checks.

## 1. UI

1.1 **Titre GRUNNCHIPELAGO** : remplace l'approche actuelle (« ARCHIPELAGO » rouge en
surimpression, qui chevauche le sous-titre « par Tom van den Boogaart ») par la
MODIFICATION du texte du titre lui-même : « GRUNN » -> « GRUNNCHIPELAGO », taille de
police réduite pour tenir dans la largeur d'origine. Uniquement quand le mod est
Enabled ; restauration du titre vanilla sinon. Le sous-titre doit rester lisible.

1.2 **Panneau de stats (menu pause uniquement, déjà retiré de Tab)** : il chevauche
l'ATH jour/heure en haut à droite (cf. capture : « Vitesse : 115 % » par-dessus
« samedi 08:00 »). Repositionne-le pour qu'il ne recouvre RIEN (par ex. sous le bloc
jour/heure, ou en haut à gauche sous « en pause » — propose et Jonath tranche).
Renomme les stats avec précision :
- « Vitesse de déplacement : 115 % »
- « Portée du sécateur : 125 % »
- « Cadence de découpe : 125 % »
- « Taille : 100 % » / « Contrôles : normaux / INVERSÉS »
Durée restante des traps actifs en temps de jeu, comme déjà spécifié.

1.3 **Item affiché sur l'écran de fin** : toujours absent. Sur l'écran du polaroid de
fin (celui de la capture « le chien vous a mordu »), affiche À GAUCHE du polaroid
l'objet débloqué par le check de la fin : nom de l'item + destinataire si multi
(« Envoyé : X -> joueur ») + si possible son visuel (modèle/sprite via la
bibliothèque de modèles, sinon texte seul). Le moment d'affichage est CET écran,
pas la reprise de run.

## 2. Modèles

2.1 **Étendre le swap aux polaroids du monde** : constaté en jeu — un polaroid
ramassé a donné une Plank sans que l'emplacement montre le modèle de la planche.
Le swap ne couvre que les ItemPickups : étends-le aux objets `Polaroid` de la scène
(mêmes règles : contenu scouté -> vrai modèle Grunn si item local, modèle AP par
classification sinon ; parent inactif pendant le clonage).
2.2 **Modèles AP plus gros** : les polaroids teintés par classification sont trop
petits en jeu — augmente l'échelle (propose un facteur, valide avec une capture).
2.3 **Popup pour les items non-Grunn** : ramasser un emplacement contenant un
buff/trap/filler n'affiche pas « Objet obtenu : X ». Ajoute le popup avec le nom réel
de l'item AP reçu (ex. « Objet obtenu : Cutter Range Boost ») — même canal que les
popups existants, et cohérent avec les verdicts du log.

## 3. Fins & architecture de sauvegarde (lire en entier avant de coder)

Constat (capture) : « fins découvertes : 11 sur 11 » sur la save vétéran de Jonath —
le compteur vanilla (GlobalData.endingTypesSeen) n'a aucun rapport avec la
progression de la session AP. Même famille de problème que la sync des polaroids.

3.1 **Solution recommandée : PROFIL DE SAUVEGARDE PAR SEED** (à évaluer d'abord,
implémenter si faisable proprement) : quand le mod est Enabled et connecté, rediriger
le chemin de sauvegarde (`SaveManager.savePath` ou équivalent — localise le point
exact dans le décompilé) vers un fichier dédié `grunn_ap_<seed>_<slot>.json`.
Bénéfices : la save vanilla du joueur est INTOUCHÉE (on supprime au passage la
resync destructive des polaroids qui édite GlobalData) ; compteur de fins,
polaroids, runsCompleted etc. démarrent à zéro naturellement par seed ; reprendre
une seed retrouve son état. Points d'attention : moment du switch (avant tout
Load), création du fichier au premier lancement, comportement si déconnecté en
cours de partie, et message clair au joueur (« Session Archipelago : sauvegarde
dédiée »). Évalue la faisabilité, présente le plan à Jonath AVANT d'implémenter.
FALLBACK si infaisable proprement : ombrage à l'affichage seulement — le compteur
de fins et les écrans de progression lisent l'état AP (checks de fins envoyés)
au lieu de GlobalData quand connecté, sans modifier la save.

3.2 **Liste des fins sur l'écran de fin** : à DROITE du polaroid de fin, liste
verticale numérotée 1-11 : les fins dont le check AP est envoyé s'affichent en
toutes lettres (noms localisés du jeu), les autres en « ??? ». Basée sur l'état AP
de la session (pas GlobalData). Mise en page : notre canvas overlay, police du jeu,
lisible sans gêner le polaroid.

## 4. Validation
- Checklist de test in-game par bloc (1 -> 2 -> 3), Jonath valide entre chaque.
- Tests apworld + build + déploiement après chaque bloc ; commits atomiques.
- Mets à jour design/playtest_notes.md (statuts).
- Reste ouvert d'anciennes sessions, à NE PAS traiter ici sans demande explicite :
  DA finale des modèles AP, calibrage buffs/traps, post Discord.
