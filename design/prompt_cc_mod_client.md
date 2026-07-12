# Prompt Claude Code — Mod client Grunnchipelago (BepInEx) (à exécuter EN SECOND)

Tu vas créer le mod client Archipelago pour Grunn (Unity Mono x64, BepInEx 5 déjà
installé dans le jeu). Le design est intégralement décidé — implémentation fidèle, et
POSE LA QUESTION en cas d'ambiguïté, n'invente rien.

## Lecture obligatoire avant d'écrire du code

1. `C:\Users\jonat\Desktop\Projets\Grunnchipelago\design\apworld_design.md` — spec, en
   particulier §5 (persistance) et §8 (tableau des hooks : LA référence)
2. `C:\Users\jonat\Desktop\Projets\Grunnchipelago\design\ids.json` — IDs/noms items et locations
3. `C:\Users\jonat\Desktop\Projets\Grunnchipelago\Grunnchipelago.Dumper\` — projet modèle
   qui COMPILE et FONCTIONNE : reprendre son .csproj (références BepInEx/Unity/jeu,
   netstandard2.0, chemins)
4. Sources décompilées du jeu pour les signatures exactes :
   `C:\Users\jonat\Desktop\Archipelago\Jeux\Grunn\analysis\decompiled\`
   (GameManager.cs, PlayerManager.cs, SaveManager.cs, Ghost.cs, ItemPickup.cs)

## Environnement

- Jeu : `C:\Program Files (x86)\Steam\steamapps\common\Grunn\Grunn` (BepInEx 5.4.23.5 x64 installé)
- SDK .NET 8 : `C:\Program Files\dotnet\dotnet.exe` (pas forcément dans le PATH)
- Nouveau projet : `C:\Users\jonat\Desktop\Projets\Grunnchipelago\Grunnchipelago.Client\`
- Dépendance : `Archipelago.MultiClient.Net` (NuGet, dernière stable) — cette DLL et ses
  dépendances doivent être COPIÉES dans BepInEx/plugins (contrairement aux refs jeu en
  Private=false). Newtonsoft.Json est déjà fourni par le jeu : ne pas le dupliquer.
- Harmony : utiliser HarmonyX fourni par BepInEx (0Harmony.dll dans BepInEx/core)

## Périmètre — 3 jalons, dans l'ordre, validation de Jonath entre chaque

### Jalon 1 : connexion + premier check
- Config BepInEx (fichier cfg) : host, port, slot, password + toggle `Enabled`
- Connexion AP au chargement (Archipelago.MultiClient.Net), reconnexion simple
- Patch Prefix `GameManager.ObtainKeyItem(KeyItem, bool)` : si connecté, envoyer la
  location `Obtain <KeyItem>` (ids.json) et ANNULER l'octroi vanilla (return false)
- Réception d'items : KeyItem reçu -> octroi via la mécanique vanilla (écrire dans la
  sauvegarde comme ObtainKeyItem le fait, SANS re-déclencher le patch — flag de garde)
  + affichage via `TriggerItemObtainPopup(KeyItem)`
- Critère : sur un serveur AP local avec le monde Grunn généré (apworld du prompt 1),
  ramasser un objet envoie un check ; un item envoyé depuis le serveur apparaît en jeu

### Jalon 2 : persistance + pools complets
- Postfix `TriggerNewRun()` : réinjection de TOUT l'inventaire AP reçu après
  ResetRunProgress ; si option `persistent_shortcuts` (slot_data) : restaurer la liste
  blanche (§5 du design)
- Prefix `PlayerManager.AddTool(Item)` : checks outils + interception (même schéma que
  Jalon 1 ; attention : les outils existent en double Item/KeyItem, l'item AP accorde les deux)
- Postfix `TriggerEnding(EndingType)` : checks de fins + détection du goal (slot_data)
  -> StatusUpdate goal complete
- Postfix `Ghost.Touch()` : checks fantômes — IDs par tri de position (x puis z),
  MÊME ordre que ids.json (positions incluses dedans pour vérification)
- Postfix `SaveManager.AddPolaroidCollected` : checks polaroids, filtrer les types Ending*
- Coinsanity (si slot_data l'active) : pickups isGulden -> checks Gulden #n (tri de
  position identique à ids.json) ; item "Gulden" reçu -> AddGulden(1)

### Jalon 3 : DeathLink + finitions
- DeathLink STRICT (décision Jonath) : Postfix `SetNightmareState` — tout déclenchement
  de cauchemar envoie un DeathLink ; tout DeathLink reçu déclenche le cauchemar
  (identifier l'état exact de déclenchement dans GameManager.cs ; garde anti-boucle)
- File d'attente des items reçus hors-jeu (menu/écran noir) à appliquer au retour en jeu
- Logs BepInEx clairs, préfixés [Grunnchipelago]

## Contraintes

- Jeu NON connecté ou option Enabled=false -> comportement 100 % vanilla (aucun patch actif ou patchs no-op)
- Ne jamais committer les DLL du jeu ni celles de BepInEx
- Chaque hook doit citer en commentaire la ligne du fichier décompilé dont il dépend
- Build : `dotnet build -c Release` + copie auto vers BepInEx/plugins (target MSBuild)
- Jonath teste en jeu à chaque jalon — demander explicitement ses retours
