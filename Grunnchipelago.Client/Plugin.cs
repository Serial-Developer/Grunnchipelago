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
    [BepInPlugin("grunnchipelago.client", "Grunnchipelago Client", "0.1.0")]
    public class Plugin : BaseUnityPlugin
    {
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
            cfgVerboseLogs = Config.Bind("Logging", "VerboseLogs", true,
                "Log every check/grant/trap (dev). When false, only connection, errors and goal.");
            cfgSkipEndingDialogues = Config.Bind("QoL", "SkipEndingDialogues", false,
                "Ending NPC dialogues (Owner / saved Owner) display instantly and advance " +
                "without the anti-skip delay - hammer Interact to blow through them.");
            cfgStatsShowAllLines = Config.Bind("QoL", "StatsShowAllLines", false,
                "Stats panel (Tab/Pause): show every stat line. When false, only the " +
                "stats that differ from their 100 % base are listed.");
            cfgSeenItems = Config.Bind("Progress", "SeenItems", "",
                "Internal: '<seed>:<slot>:<count>' of already-applied items. Do not edit.");

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

            if (!string.IsNullOrEmpty(cfgSlot.Value))
                Ap.Connect(cfgHost.Value, cfgPort.Value, cfgSlot.Value, cfgPassword.Value);
        }

        // DeathLink sequence: <0 = idle, otherwise elapsed seconds.
        private float deathLinkTimer = -1f;
        private bool screamerShown;
        private const float FadeToBlackDuration = 1.0f;   // black + frozen player (a ajuster)
        private const float ScreamerDuration = 2.0f;      // nightmare shot on black (a ajuster)

        /// <summary>True while the DeathLink screamer displays; read by HandleNightmarePatch
        /// to force the nightmare blend factor.</summary>
        public static bool JumpscareActive { get; private set; }

        /// <summary>True while an ending NPC dialogue runs (drives the "ESC : skip" hint;
        /// the skip itself is EscSkipsEndingDialoguePatch).</summary>
        public static bool EndingDialogueActive { get; private set; }

        private void Update()
        {
            if (Ap == null) return;
            // Grant pump: replay items AND the post-run re-injection are deferred until
            // the player is in a SAFE state (up, controllable, no cutscene/intro/prompt) -
            // granting during the scripted bus intro froze all inputs (playtest round 2).
            bool safe = ApClient.PlayerInSafeState();
            Ap.TickGrants();
            // Buff multipliers + timed-trap expiry (restores vanilla when disconnected).
            Effects.Tick(Ap.Connected);
            // "ESC : skip" hint state (skip logic: EscSkipsEndingDialoguePatch for the
            // ending NPCs, HandleSkipOrbDialogue for the post-death orb sequence).
            EndingDialogueActive =
                (GameManager.owner != null && GameManager.owner.curState == Owner.State.Talk)
                || (GameManager.ownerSaved != null && GameManager.ownerSaved.curState == OwnerSaved.State.Talk)
                || (Ap.Connected && GameManager.CurGameState == GameManager.GameState.Ending
                    && GameManager.curEndingState == GameManager.EndingState.Orb);
            // Title marker + stats panel (playtest H).
            ModUi.Tick(Ap, cfgStatsShowAllLines.Value);
            // Dev helper (VerboseLogs): trigger traps by key for testing.
            if (ApClient.Verbose && Ap.Connected && safe) HandleDebugTrapKeys();
            // Pickup model swap from the scout (features #1/#2, one-shot per session).
            ModelSwap.Tick(Ap);
            if (Ap.Connected)
            {
                // Popups queued from patch context; drained only in a safe state so
                // ending-check rewards land at the new run, after cutscene AND bus intro.
                if (safe) Ap.FlushPendingPopups();
                HandleSkipEndingDialogues();
                HandleSkipOrbDialogue();
                HutLock.Tick(Ap);
                // Bone gift pickup near the start (design section 10, feature #3).
                BoneGift.EnsureSpawned(Ap);
                // One-shot after login: restore uncollected-check polaroids to the world.
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
        /// F6 = Speed, F7 = Size, F9 = Inverted Controls, F10 = random Regrow trap.</summary>
        private void HandleDebugTrapKeys()
        {
            if (Input.GetKeyDown(KeyCode.F6)) DebugTrap(GameIds.TrapSpeed);
            if (Input.GetKeyDown(KeyCode.F7)) DebugTrap(GameIds.TrapSize);
            if (Input.GetKeyDown(KeyCode.F9)) DebugTrap(GameIds.TrapInvertedControls);
            if (Input.GetKeyDown(KeyCode.F10))
            {
                string[] regrows =
                {
                    GameIds.TrapRegrowGrass, GameIds.TrapRewaterFlowers, GameIds.TrapRegrowHedge,
                    GameIds.TrapReturnTrash, GameIds.TrapRegrowMolehills,
                };
                DebugTrap(regrows[UnityEngine.Random.Range(0, regrows.Length)]);
            }
        }

        private void DebugTrap(string name)
        {
            Logger.LogInfo($"[Grunnchipelago] DEBUG trap key: {name}");
            Effects.ApplyTrap(name);
        }

        /// <summary>playtest D.2 - ending NPC dialogues (Owner / OwnerSaved prompt chains,
        /// Owner.cs HandleTalking) gate each line behind full text scroll + an anti-skip
        /// timer. While one of them talks, force the text complete and the skip timer
        /// finished so Interact advances instantly. Scoped to the ending NPCs only.</summary>
        private void HandleSkipEndingDialogues()
        {
            if (!cfgSkipEndingDialogues.Value || UIManager.instance == null) return;
            bool endingNpcTalking =
                (GameManager.owner != null && GameManager.owner.curState != Owner.State.Off)
                || (GameManager.ownerSaved != null && GameManager.ownerSaved.curState != OwnerSaved.State.Off);
            if (!endingNpcTalking) return;

            UIManager ui = UIManager.instance;
            if (ui.promptCharIndex < ui.promptCharMax) ui.promptCharIndex = ui.promptCharMax;
            ui.skipPromptTimer.counter = ui.skipPromptTimer.duration;
            ui.skipPromptTimer.finished = true;
        }

        /// <summary>Session 2 (retour Jonath) - the post-death-ending ORB dialogue
        /// (EndingState.Orb) ignores every key but Interact in vanilla, Escape
        /// included. While it runs, Escape ends the whole sequence: jump to
        /// EndingState.End - the orb fades (HandleOrb only shows it during Orb,
        /// GameManager.cs:1478) and once orbFactorCur reaches ~0 the vanilla
        /// EndingScreenLogic (UIManager.cs:1817-1822) restarts the run exactly like
        /// the dialogue's natural end. Escape is read from InputManager.cancelData
        /// (input-system callback, InputManager.cs:387 - it keeps updating even when
        /// the game ignores it).</summary>
        private void HandleSkipOrbDialogue()
        {
            if (GameManager.CurGameState != GameManager.GameState.Ending
                || GameManager.curEndingState != GameManager.EndingState.Orb) return;
            if (InputManager.cancelData == null || !InputManager.cancelData.pressed) return;

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
