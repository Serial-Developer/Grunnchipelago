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

        private void Awake()
        {
            Log = Logger;
            cfgEnabled = Config.Bind("Connection", "Enabled", true,
                "Master switch. When off, the game is 100 % vanilla.");
            cfgHost = Config.Bind("Connection", "Host", "localhost", "Archipelago server host.");
            cfgPort = Config.Bind("Connection", "Port", 38281, "Archipelago server port.");
            cfgSlot = Config.Bind("Connection", "Slot", "", "Your slot (player) name.");
            cfgPassword = Config.Bind("Connection", "Password", "", "Server password (optional).");

            if (!cfgEnabled.Value)
            {
                Logger.LogInfo("[Grunnchipelago] Disabled in config - vanilla mode.");
                return;
            }

            Ap = new ApClient(Logger);
            new Harmony("grunnchipelago.client").PatchAll();
            Logger.LogInfo("[Grunnchipelago] Client loaded. Connecting when a slot is set.");

            if (!string.IsNullOrEmpty(cfgSlot.Value))
                Ap.Connect(cfgHost.Value, cfgPort.Value, cfgSlot.Value, cfgPassword.Value);
        }

        private void Update()
        {
            if (Ap == null) return;
            // Grant received items on the main thread, but ONLY while actually in-game -
            // items received in the menu / during a black screen / while switching state
            // stay queued until we're back (GameManager.cs:848-854 state accessors).
            bool inGame = GameManager.CurGameState == GameManager.GameState.Game
                          && !GameManager.BlackScreen && !GameManager.SwitchingState;
            if (inGame) Ap.ApplyPendingItems();
            // Simple reconnection loop.
            Ap.Tick(Time.deltaTime, cfgHost.Value, cfgPort.Value, cfgSlot.Value, cfgPassword.Value);
        }

        private void OnDestroy()
        {
            Ap?.Disconnect();
        }
    }
}
