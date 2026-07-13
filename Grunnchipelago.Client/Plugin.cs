using BepInEx;
using BepInEx.Configuration;
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
            cfgSeenItems = Config.Bind("Progress", "SeenItems", "",
                "Internal: '<seed>:<slot>:<count>' of already-applied items. Do not edit.");

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

        private void Update()
        {
            if (Ap == null) return;
            // Grant received items on the main thread, but ONLY while actually in-game -
            // items received in the menu / during a black screen / while switching state
            // stay queued until we're back (GameManager.cs:848-854 state accessors).
            bool inGame = GameManager.CurGameState == GameManager.GameState.Game
                          && !GameManager.BlackScreen && !GameManager.SwitchingState;
            if (inGame) Ap.ApplyPendingItems();
            // Buff multipliers + timed-trap expiry (restores vanilla when disconnected).
            Effects.Tick(Ap.Connected);
            if (Ap.Connected)
            {
                // Popups queued from patch context (vanilla-popup suppression scope).
                Ap.FlushPendingPopups();
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
            HandleDeathLink(inGame);
            // Simple reconnection loop.
            Ap.Tick(Time.deltaTime, cfgHost.Value, cfgPort.Value, cfgSlot.Value, cfgPassword.Value);
        }

        /// <summary>Received DeathLink sequence (design Jonath 2026-07-13):
        /// 1. the screen cuts to black and the player freezes (movement is blocked while
        ///    BlackScreen is up, PlayerControllerNew.cs:687) for FadeToBlackDuration;
        /// 2. a GUARANTEED random nightmare shot + sound displays over the black
        ///    (the nightmare PostFX composes above the black screen, PostFXStack.cs:476);
        /// 3. the run resets (TriggerNewRun - our patch re-injects the AP inventory).
        /// No ending is triggered, no check granted.</summary>
        private void HandleDeathLink(bool inGame)
        {
            if (!Ap.DeathLinkEnabled) return;

            if (deathLinkTimer < 0f)
            {
                // Several deaths received while away collapse into a single reset.
                if (inGame && Ap.TakeAllPendingDeathLinks() > 0)
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
}
