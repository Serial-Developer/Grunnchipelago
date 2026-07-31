# Grunn Multiworld Setup Guide

## Required Software

- **Grunn** (Sokpop Collective) on Steam.
- **BepInEx 5** (x64) — <https://github.com/BepInEx/BepInEx/releases>.
  Take a `BepInEx_x64_5.4.x` build, **not** BepInEx 6.
- The **Grunnchipelago client mod** (`Grunnchipelago.Client.dll` and its dependencies).
- The **Grunn apworld** (`grunn.apworld`), only needed by whoever generates the seed.

## Installing BepInEx

1. Find your game folder: in Steam, right-click **Grunn** → *Manage* → *Browse local files*.
   You should land on a folder containing `Grunn.exe`.
2. Unzip the BepInEx archive **into that folder**, so that `BepInEx/` sits next to
   `Grunn.exe`.
3. Launch the game once, then close it. BepInEx generates its folders on that first run —
   `BepInEx/plugins/` must now exist.

If `BepInEx/plugins/` was not created, BepInEx did not load: check that you unzipped into
the folder holding `Grunn.exe` and that you took the **x64** build.

## Installing the mod

1. Create the folder `BepInEx/plugins/Grunnchipelago/`.
2. Drop `Grunnchipelago.Client.dll` and the DLLs shipped alongside it into that folder.
3. Launch the game once, then close it. The mod writes its configuration file to
   `BepInEx/config/grunnchipelago.client.cfg`.

## Connecting to a multiworld

Open `BepInEx/config/grunnchipelago.client.cfg` and fill in the `[Connection]` section:

```ini
[Connection]
Enabled = true
Host = archipelago.gg      ## or localhost for a local server
Port = 38281               ## the port given by the room
Slot = YourSlotName        ## must match the "name" in your YAML
Password =                 ## leave empty if the room has none
```

Save, then launch the game. The mod connects on its own; the main menu title reads
**GRUNNCHIPELAGO** when it is active. Checks are sent as you play, and received items arrive
in your inventory.

`Enabled = false` turns everything off and gives you the vanilla game back — no patch is
applied at all in that state.

## Options worth knowing

- **QoL**: `SkipEndingDialogues` speeds through the ending NPC dialogues.
  `StatsShowAllLines` always displays every stat line in the Tab/Pause panel.
- **Logging**: `VerboseLogs` logs every check, grant and trap. The mod also keeps a
  persistent, timestamped log at
  `BepInEx/plugins/Grunnchipelago/grunnchipelago_session.log` — that is the file to attach
  when reporting a problem.

## Saves

The mod uses a **dedicated save profile per seed**, named after the seed. Your vanilla save
is never touched, and two different multiworlds never share progress. Starting a new seed
therefore always starts from a clean slate.

## Generating a seed

Only the person generating needs the apworld.

1. Drop `grunn.apworld` into the `custom_worlds/` folder of your Archipelago installation.
2. Grab the template YAML (`Players/Templates/Grunn.yaml`, or generate the templates from
   the Archipelago Launcher) and edit it to taste.
3. Put your YAML in `Players/` and run **Generate**, or upload it to
   <https://archipelago.gg/uploads>.

## Troubleshooting

**The game starts but nothing connects.** Check `Slot` against the `name:` field of your
YAML — they must match exactly, capitals included. Then read the session log: connection
errors are written there in full.

**A pickup gives nothing.** That is normal for a check you already sent: the object
respawns but stays inert. The log says `Silencieux : … (deja envoye)`.

**An item never showed up in the world.** Three items are deliberately never injected into
your inventory — the Bone, the Compass and the Strange Key. Owning them would kill an
ending (Dog, Hedge Maze and Long Hallway respectively). Instead they appear as a pickup
next to the rose sign at the start, and you take them only when you actually want them.
