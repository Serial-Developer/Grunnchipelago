using System;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace Grunnchipelago.Client
{
    /// <summary>
    /// Archipelago client plugin for Grunn (BepInEx 5). Milestone 1: connect + first check.
    /// When disabled or not connected, all patches are no-ops (100 % vanilla behaviour).
    /// </summary>
    [BepInPlugin("grunnchipelago.client", "Grunnchipelago Client", ModVersion)]
    public class Plugin : BaseUnityPlugin
    {
        /// <summary>Mod version - shown under the main-menu title and used as the BepInEx
        /// plugin version. NOT declared here: it is generated at build time from
        /// <c>apworld/grunn/archipelago.json</c> ("world_version"), the single place a
        /// release is bumped. See the GenerateModVersion target in the .csproj.</summary>
        public const string ModVersion = BuildInfo.ModVersion;

        /// <summary>Set of the running plugin, used by the Harmony patches.</summary>
        public static ApClient Ap { get; private set; }

        /// <summary>Plugin logger, for static contexts (patches).</summary>
        public static BepInEx.Logging.ManualLogSource Log { get; private set; }

        private ConfigEntry<bool> cfgEnabled;
        private ConfigEntry<string> cfgHost;
        private ConfigEntry<int> cfgPort;
        private ConfigEntry<string> cfgSlot;
        private ConfigEntry<string> cfgPassword;
        private ConfigEntry<bool> cfgVerboseLogs;
        private ConfigEntry<string> cfgModelProbe;
        private ConfigEntry<bool> cfgSkipEndingDialogues;
        private ConfigEntry<bool> cfgStatsShowAllLines;
        private ConfigEntry<string> cfgSeenItems;

        private void Awake()
        {
            Log = Logger;
            cfgEnabled = Config.Bind("Connection", "Enabled", true,
                "Master switch. When off, the game is 100 % vanilla.");
            cfgHost = Config.Bind("Connection", "Host", "localhost", "Archipelago server host.");
            cfgPort = Config.Bind("Connection", "Port", 38281, "Archipelago server port.");
            cfgSlot = Config.Bind("Connection", "Slot", "", "Your slot (player) name.");
            cfgPassword = Config.Bind("Connection", "Password", "", "Server password (optional).");
            cfgVerboseLogs = Config.Bind("Logging", "VerboseLogs", false,
                "Log every check/grant/trap (dev). When false, only connection, errors and goal.");
            cfgModelProbe = Config.Bind("Debug", "ShowModelProbe", "",
                "Comma-separated sample models to line up next to the starting bus for "
                + "inspection, posed exactly as a real check would be. Accepts GoldenGulden, "
                + "Gulden, Buff, Progression, Useful, Filler, or any KeyItem name "
                + "(e.g. \"GoldenGulden,Buff,Progression\"). Purely visual, never a check. "
                + "Empty to disable.");
            cfgSkipEndingDialogues = Config.Bind("QoL", "SkipEndingDialogues", true,
                "Escape ends the post-death ORB dialogue at once, instead of being ignored " +
                "as it is in vanilla. Nothing else is ever skipped: every other NPC keeps " +
                "its normal pace, and Escape keeps opening the pause menu.");
            cfgStatsShowAllLines = Config.Bind("QoL", "StatsShowAllLines", false,
                "Stats panel (Tab/Pause): show every stat line. When false, only the " +
                "stats that differ from their 100 % base are listed.");
            cfgSeenItems = Config.Bind("Progress", "SeenItems", "",
                "Internal: 'seed:slot=count|seed:slot=count|...' of already-applied items, "
                + "one entry per multiworld. Do not edit.");

            // Persistent, timestamped mod log (BepInEx overwrites LogOutput.log each boot).
            BepInEx.Logging.Logger.Listeners.Add(new SessionFileLog(
                Path.Combine(Paths.PluginPath, "Grunnchipelago", "grunnchipelago_session.log")));

            if (!cfgEnabled.Value)
            {
                Logger.LogInfo("[Grunnchipelago] Disabled in config - vanilla mode.");
                return;
            }

            ApClient.Verbose = cfgVerboseLogs.Value;
            Ap = new ApClient(Logger)
            {
                LoadSeenState = () => cfgSeenItems.Value,
                SaveSeenState = value => cfgSeenItems.Value = value,
            };
            new Harmony("grunnchipelago.client").PatchAll();
            Logger.LogInfo("[Grunnchipelago] Client loaded. Connecting when a slot is set.");

            // Title-screen connection panel: it reads and writes the SAME config entries as
            // the file, so what the player types is remembered and the auto-reconnect loop
            // (Update, below) picks the new values up on its own.
            ConnectUi.Load = () => (cfgHost.Value, cfgPort.Value, cfgSlot.Value, cfgPassword.Value);
            ConnectUi.Save = (host, port, slot, password) =>
            {
                cfgHost.Value = host;
                cfgPort.Value = port;
                cfgSlot.Value = slot;
                cfgPassword.Value = password;
                Config.Save();
            };

            if (!string.IsNullOrEmpty(cfgSlot.Value))
                Ap.Connect(cfgHost.Value, cfgPort.Value, cfgSlot.Value, cfgPassword.Value);
        }

        private void OnGUI()
        {
            if (Ap == null) return;
            ConnectUi.Draw();    // title screen: connection panel
            ConsoleUi.Draw();    // in game: Archipelago console
        }

        /// <summary>Cursor state and the console's focus key both have to be handled outside
        /// the IMGUI event stream: one must apply every frame, the other must work while the
        /// console is unfocused.</summary>
        private void TickOverlays()
        {
            if (Ap == null) return;
            ConnectUi.Tick();
            ConsoleUi.Tick();
        }

        // DeathLink sequence: <0 = idle, otherwise elapsed seconds.
        private float deathLinkTimer = -1f;
        private bool screamerShown;
        private const float FadeToBlackDuration = 1.0f;   // black + frozen player (a ajuster)
        private const float ScreamerDuration = 2.0f;      // nightmare shot on black (a ajuster)

        /// <summary>True while the DeathLink screamer displays; read by HandleNightmarePatch
        /// to force the nightmare blend factor.</summary>
        public static bool JumpscareActive { get; private set; }

        /// <summary>True during the post-death orb sequence (drives the "ESC : skip" hint;
        /// the skip itself is HandleSkipOrbDialogue).</summary>
        public static bool EndingDialogueActive { get; private set; }

        private void Update()
        {
            if (Ap == null) return;
            // Overlays: title-screen connection panel (cursor) and in-game console (F1).
            TickOverlays();
            // Switched to a DIFFERENT multiworld: wipe what belonged to the previous one.
            // Done here because it destroys GameObjects - the login runs off-thread.
            if (Ap.NeedsSessionReset)
            {
                Ap.NeedsSessionReset = false;
                ModelSwap.ResetForNewSession();
                GiftPickups.ResetForNewSession();
                Ap.NeedsVisibilityRefresh = true;   // recompute pickups from the NEW check state
                Logger.LogInfo("[Grunnchipelago] Nouveau multiworld : etat de session reinitialise.");
            }
            // Grant pump: replay items AND the post-run re-injection are deferred until
            // the player is in a SAFE state (up, controllable, no cutscene/intro/prompt) -
            // granting during the scripted bus intro froze all inputs (playtest round 2).
            bool safe = ApClient.PlayerInSafeState();
            Ap.TickGrants();
            // Buff multipliers + timed-trap expiry (restores vanilla when disconnected).
            Effects.Tick(Ap.Connected);
            // "ESC : skip" hint state - the ORB SEQUENCE ONLY (skip logic:
            // HandleSkipOrbDialogue). Neither owner belongs here [J 2026-08-01]: both are
            // ordinary in-game NPCs whose UpdateNormal only runs in GameState.Game
            // (Owner.cs:160, OwnerSaved.cs:144), so offering to skip them turned a plain
            // conversation into a cutscene - and made Escape stop opening the pause menu.
            EndingDialogueActive = Ap.Connected
                && cfgSkipEndingDialogues.Value     // no hint for something we will not do
                && GameManager.CurGameState == GameManager.GameState.Ending
                && GameManager.curEndingState == GameManager.EndingState.Orb;
            // Title marker + stats panel (playtest H).
            ModUi.Tick(Ap, cfgStatsShowAllLines.Value);
            // Dev helper (VerboseLogs): trigger traps by key for testing.
            // Not while the console has the keyboard: F-keys would fire traps mid-typing.
            if (ApClient.Verbose && Ap.Connected && safe && !ConsoleUi.Focused) HandleDebugTrapKeys();
            // Pickup model swap from the scout (features #1/#2, one-shot per session).
            ModelSwap.Tick(Ap, cfgModelProbe.Value);
            if (Ap.Connected)
            {
                // Per-seed save profile (3.1): swaps SaveManager's save-path prefix at
                // the title screen, before any world load. Must run outside the "safe
                // state" gate - the title screen is never a safe state.
                SaveProfile.Tick(Ap);
                // Popups queued from patch context; drained only in a safe state so
                // ending-check rewards land at the new run, after cutscene AND bus intro.
                if (safe) Ap.FlushPendingPopups();
                HandleSkipOrbDialogue();
                BunkerFlood.Tick(Ap);
                HutLock.Tick(Ap);
                // Bone + compass gift pickups near the start (design section 10 #3,
                // session 2 iter 8 - both items would kill a loupable ending).
                GiftPickups.EnsureSpawned(Ap);
                // One-shot on the FIRST connect to a new seed:slot - the vanilla save
                // (and the static ShortcutCache) carry the previous multiworld's
                // shortcuts; a fresh seed must start with vanilla-fresh shortcuts.
                // Pointless (and undesirable) on a dedicated profile: that save IS
                // fresh, and re-clearing would wipe shortcuts legitimately earned on a
                // resumed seed.
                if (SaveProfile.Active) Ap.NeedsShortcutReset = false;
                if (Ap.NeedsShortcutReset && SaveManager.progressDataCheck != null)
                {
                    Ap.NeedsShortcutReset = false;
                    ShortcutCache.Clear();
                    var pd = SaveManager.progressDataCheck;
                    pd.unlockedBijkeukenShortcut = false;
                    pd.unlockedIntratuin = false;
                    pd.createdShortcut = false;
                    pd.parkUnlockedHooibaalGarden = false;
                    pd.parkUnlockedMaze = false;
                    pd.locksUnlocked = new System.Collections.Generic.List<Lock>();
                    SaveManager.Save(SaveManager.curSlotIndex);
                    Log.LogInfo("[Grunnchipelago] Nouvelle seed : raccourcis remis a zero.");
                }
                // One-shot after login: restore uncollected-check polaroids to the world.
                // On a dedicated profile the save is this seed's own, so its polaroid
                // list is already correct - the destructive GlobalData edit (design
                // section 3.1) is skipped entirely.
                if (Ap.NeedsPolaroidSync && SaveProfile.Active)
                {
                    Ap.NeedsPolaroidSync = false;
                    Log.LogInfo("[Grunnchipelago] Resync polaroids inutile (sauvegarde dediee).");
                }
                if (Ap.NeedsPolaroidSync && GameManager.allPolaroids != null
                    && GameManager.allPolaroids.Count > 0)
                {
                    Ap.NeedsPolaroidSync = false;
                    Ap.SyncPolaroidsWithServer();
                }
                // One-shot after login: recompute pickup visibility from CHECK state
                // (the GrabbedItem event runs the patched per-pickup recomputation).
                if (Ap.NeedsVisibilityRefresh && GameManager.allItemPickups != null
                    && GameManager.allItemPickups.Count > 50)
                {
                    Ap.NeedsVisibilityRefresh = false;
                    GameManager.GrabbedItem();
                    Log.LogInfo("[Grunnchipelago] Pickup visibility recomputed from check state.");
                }
            }
            HandleDeathLink(safe);
            // Simple reconnection loop.
            Ap.Tick(Time.deltaTime, cfgHost.Value, cfgPort.Value, cfgSlot.Value, cfgPassword.Value);
        }

        /// <summary>Dev helper (active under VerboseLogs, in a safe state): trigger traps
        /// by key to test them without a seed containing them. F8 is taken by the dumper.
        /// F6 = Speed, F7 = Size, F9 = Inverted Controls, F10 = next world trap (cycles
        /// through the five one-shot traps in order, so each can be tested deliberately -
        /// the name is logged on every press).</summary>
        private static readonly string[] WorldTraps =
        {
            GameIds.TrapGardenReset, GameIds.TrapChurchReset, GameIds.TrapParkReset,
            GameIds.TrapNight, GameIds.TrapSacredFlower,
        };

        private int worldTrapCursor;

        private void HandleDebugTrapKeys()
        {
            if (Input.GetKeyDown(KeyCode.F6)) DebugTrap(GameIds.TrapSpeed);
            if (Input.GetKeyDown(KeyCode.F7)) DebugTrap(GameIds.TrapSize);
            if (Input.GetKeyDown(KeyCode.F9)) DebugTrap(GameIds.TrapInvertedControls);
            if (Input.GetKeyDown(KeyCode.F10))
            {
                DebugTrap(WorldTraps[worldTrapCursor]);
                worldTrapCursor = (worldTrapCursor + 1) % WorldTraps.Length;
            }
        }

        private void DebugTrap(string name)
        {
            Logger.LogInfo($"[Grunnchipelago] DEBUG trap key: {name}");
            Effects.ApplyTrap(name);
        }

        /// <summary>Session 2 (retour Jonath) - the post-death-ending ORB dialogue
        /// (EndingState.Orb) ignores every key but Interact in vanilla, Escape
        /// included. While it runs, Escape ends the whole sequence: jump to
        /// EndingState.End - the orb fades (HandleOrb only shows it during Orb,
        /// GameManager.cs:1478) and once orbFactorCur reaches ~0 the vanilla
        /// EndingScreenLogic (UIManager.cs:1817-1822) restarts the run exactly like
        /// the dialogue's natural end. Escape is the QUIT action, not cancel
        /// (quitString="input_esc" vs cancelString="Q" - InputManager.cs:244-246);
        /// quitData keeps updating via the input-system callback even when the game
        /// ignores it.</summary>
        private void HandleSkipOrbDialogue()
        {
            if (!cfgSkipEndingDialogues.Value) return;
            if (GameManager.CurGameState != GameManager.GameState.Ending
                || GameManager.curEndingState != GameManager.EndingState.Orb) return;
            if (InputManager.quitData == null || !InputManager.quitData.pressed) return;

            GameManager.curEndingOrbPromptIndex = GameManager.curEndingOrbPromptIndexMax;
            GameManager.orbWaitingForInput = false;
            GameManager.FinishPromptTimer();   // drop the line being displayed
            GameManager.SetEndingState(GameManager.EndingState.End);
            Logger.LogInfo("[Grunnchipelago] Orb dialogue skipped (ESC).");
        }

        /// <summary>Received DeathLink sequence (design Jonath 2026-07-13):
        /// 1. the screen cuts to black and the player freezes (movement is blocked while
        ///    BlackScreen is up, PlayerControllerNew.cs:687) for FadeToBlackDuration;
        /// 2. a GUARANTEED random nightmare shot + sound displays over the black
        ///    (the nightmare PostFX composes above the black screen, PostFXStack.cs:476);
        /// 3. the run resets (TriggerNewRun - our patch re-injects the AP inventory).
        /// No ending is triggered, no check granted.</summary>
        private void HandleDeathLink(bool safe)
        {
            if (!Ap.DeathLinkEnabled) return;

            if (deathLinkTimer < 0f)
            {
                // Several deaths received while away collapse into a single reset; the
                // sequence only starts in a safe state (never during the bus intro).
                if (safe && Ap.TakeAllPendingDeathLinks() > 0)
                {
                    // Covers the whole sequence (durations are in 60ths of a second).
                    GameManager.TriggerBlackScreen((FadeToBlackDuration + ScreamerDuration + 1f) * 60f);
                    deathLinkTimer = 0f;
                    screamerShown = false;
                    Logger.LogInfo("[Grunnchipelago] DeathLink: fade to black...");
                }
                return;
            }

            deathLinkTimer += Time.deltaTime;

            if (!screamerShown && deathLinkTimer >= FadeToBlackDuration)
            {
                screamerShown = true;
                NightmareJumpscare.Show();   // guaranteed shot + sound
                JumpscareActive = true;      // forces the nightmare blend factor
                Logger.LogInfo("[Grunnchipelago] DeathLink: nightmare screamer.");
            }

            if (deathLinkTimer >= FadeToBlackDuration + ScreamerDuration)
            {
                deathLinkTimer = -1f;
                JumpscareActive = false;
                NightmareJumpscare.Hide();
                GameManager.TriggerBlackScreen(120f);   // reset transition (GameManager.cs:3415)
                GameManager.TriggerNewRun();            // GameManager.cs:3758
                Logger.LogInfo("[Grunnchipelago] DeathLink applied: run reset.");
            }
        }

        private void OnDestroy()
        {
            Ap?.Disconnect();
        }
    }

    /// <summary>playtest C.2 - persistent mod log: BepInEx rewrites LogOutput.log on every
    /// boot, so [Grunnchipelago] lines are also appended, timestamped, to
    /// plugins/Grunnchipelago/grunnchipelago_session.log (simple 2 MB rotation to .old).</summary>
    internal sealed class SessionFileLog : ILogListener
    {
        private const long MaxBytes = 2_000_000;
        private readonly StreamWriter writer;

        public SessionFileLog(string path)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                if (File.Exists(path) && new FileInfo(path).Length > MaxBytes)
                {
                    string old = path + ".old";
                    if (File.Exists(old)) File.Delete(old);
                    File.Move(path, old);
                }
                writer = new StreamWriter(path, append: true) { AutoFlush = true };
                writer.WriteLine($"===== session {DateTime.Now:yyyy-MM-dd HH:mm:ss} =====");
            }
            catch (Exception)
            {
                writer = null;   // logging must never break the game
            }
        }

        public void LogEvent(object sender, LogEventArgs eventArgs)
        {
            if (writer == null) return;
            string text = eventArgs.Data?.ToString();
            if (text == null || !text.Contains("[Grunnchipelago]")) return;
            try { writer.WriteLine($"[{DateTime.Now:HH:mm:ss}] [{eventArgs.Level}] {text}"); }
            catch (Exception) { }
        }

        public void Dispose() => writer?.Dispose();
    }
}
