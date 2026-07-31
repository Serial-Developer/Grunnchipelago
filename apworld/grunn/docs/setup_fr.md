# Guide de configuration Multimonde pour Grunn

## Logiciels nécessaires

- **Grunn** (Sokpop Collective) sur Steam.
- **BepInEx 5** (x64) — <https://github.com/BepInEx/BepInEx/releases>.
  Prenez une version `BepInEx_x64_5.4.x`, et **non** BepInEx 6.
- Le **mod client Grunnchipelago** (`Grunnchipelago.Client.dll` et ses dépendances).
- L'**apworld Grunn** (`grunn.apworld`), utile uniquement à la personne qui génère la seed.

## Installer BepInEx

1. Localisez le dossier du jeu : dans Steam, clic droit sur **Grunn** → *Gérer* →
   *Parcourir les fichiers locaux*. Vous devez arriver sur un dossier contenant `Grunn.exe`.
2. Décompressez l'archive BepInEx **dans ce dossier**, de sorte que `BepInEx/` se retrouve à
   côté de `Grunn.exe`.
3. Lancez le jeu une fois, puis fermez-le. BepInEx crée ses dossiers à ce premier
   démarrage : `BepInEx/plugins/` doit maintenant exister.

Si `BepInEx/plugins/` n'a pas été créé, c'est que BepInEx ne s'est pas chargé : vérifiez que
vous avez bien décompressé dans le dossier contenant `Grunn.exe`, et que vous avez pris la
version **x64**.

## Installer le mod

1. Créez le dossier `BepInEx/plugins/Grunnchipelago/`.
2. Placez-y `Grunnchipelago.Client.dll` ainsi que les DLL fournies avec.
3. Lancez le jeu une fois, puis fermez-le : le mod écrit son fichier de configuration dans
   `BepInEx/config/grunnchipelago.client.cfg`.

## Se connecter à un multimonde

Ouvrez `BepInEx/config/grunnchipelago.client.cfg` et renseignez la section `[Connection]` :

```ini
[Connection]
Enabled = true
Host = archipelago.gg      ## ou localhost pour un serveur local
Port = 38281               ## le port indiqué par la room
Slot = VotreNomDeSlot      ## doit correspondre au "name" de votre YAML
Password =                 ## laissez vide si la room n'en a pas
```

Enregistrez, puis lancez le jeu. Le mod se connecte tout seul ; le titre du menu principal
affiche **GRUNNCHIPELAGO** quand il est actif. Les checks partent au fur et à mesure, et les
objets reçus arrivent dans votre inventaire.

`Enabled = false` désactive tout et vous rend le jeu vanilla — aucun patch n'est appliqué
dans cet état.

## Options utiles

- **QoL** : `SkipEndingDialogues` accélère les dialogues des PNJ de fin.
  `StatsShowAllLines` affiche systématiquement toutes les lignes du panneau de stats
  (Tab/Pause).
- **Journalisation** : `VerboseLogs` enregistre chaque check, octroi et piège. Le mod tient
  aussi un journal horodaté persistant dans
  `BepInEx/plugins/Grunnchipelago/grunnchipelago_session.log` — c'est ce fichier qu'il faut
  joindre en cas de problème.

## Sauvegardes

Le mod utilise un **profil de sauvegarde dédié par seed**, nommé d'après celle-ci. Votre
sauvegarde vanilla n'est jamais touchée, et deux multimondes différents ne partagent jamais
leur progression. Commencer une nouvelle seed repart donc toujours de zéro.

## Générer une seed

Seule la personne qui génère a besoin de l'apworld.

1. Placez `grunn.apworld` dans le dossier `custom_worlds/` de votre installation
   Archipelago.
2. Récupérez le YAML modèle (`Players/Templates/Grunn.yaml`, ou générez les modèles depuis
   le Launcher Archipelago) et modifiez-le selon vos envies.
3. Placez votre YAML dans `Players/` et lancez **Generate**, ou déposez-le sur
   <https://archipelago.gg/uploads>.

## En cas de problème

**Le jeu démarre mais rien ne se connecte.** Vérifiez que `Slot` correspond exactement au
champ `name:` de votre YAML, majuscules comprises. Consultez ensuite le journal de session :
les erreurs de connexion y sont écrites en entier.

**Un objet ramassé ne donne rien.** C'est normal pour un check déjà envoyé : l'objet
réapparaît mais reste inerte. Le journal indique `Silencieux : … (deja envoye)`.

**Un objet n'est jamais apparu dans le monde.** Trois objets ne sont volontairement jamais
injectés dans votre inventaire — l'Os, la Boussole et la Clé étrange. Les posséder tuerait
une fin (respectivement Chien, Labyrinthe et Long Couloir). Ils apparaissent à la place sous
forme de ramassage près du panneau des roses, au point de départ, et vous ne les prenez que
lorsque vous en avez réellement besoin.
