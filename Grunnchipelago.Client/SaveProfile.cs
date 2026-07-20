using System;
using System.IO;
using HarmonyLib;
using UnityEngine;

namespace Grunnchipelago.Client
{
    /// <summary>
    /// Session 2, 3.1 - PER-SEED SAVE PROFILE.
    ///
    /// Grunn has a single, always-slot-0 save: curSlotIndex is assigned exactly once
    /// in the whole game (SaveManager.cs:1405) and the file name is built as
    /// <c>savePath + slotIndex + ".txt"</c> (SaveManager.cs:2055/2080/2092), i.e.
    /// savePath is a PREFIX, not a file. Swapping that prefix therefore redirects the
    /// whole save system without touching slots or adding any UI:
    ///
    ///   grunn_v1_save0.txt            the player's vanilla save - NEVER touched
    ///   grunn_ap_&lt;seed&gt;_&lt;slot&gt;0.txt   this multiworld's dedicated profile
    ///
    /// Benefits (design section 3.1): the vanilla save is untouched, and the ending
    /// counter, polaroids, runsCompleted and shortcuts all start at zero PER SEED -
    /// so the "fins découvertes : 11 sur 11" of a veteran save disappears, and the
    /// destructive GlobalData polaroid resync becomes unnecessary.
    ///
    /// Settings stay on the vanilla path (settingsPath is a separate field): audio,
    /// graphics and controls are shared, nothing to reconfigure.
    ///
    /// SAFETY: the swap only ever happens at the TITLE SCREEN, before any world load.
    /// Connecting mid-game keeps the current save until the player returns to the
    /// menu. Once switched, we stay on the AP profile even if the socket drops - a
    /// hot switch back would mix two world states.
    /// </summary>
    internal static class SaveProfile
    {
        private static readonly AccessTools.FieldRef<string> SavePathRef =
            AccessTools.StaticFieldRefAccess<string>(
                AccessTools.Field(typeof(SaveManager), "savePath"));

        /// <summary>Vanilla prefix, captured before the first swap.</summary>
        private static string vanillaPath;

        /// <summary>Prefix currently in force (null while vanilla).</summary>
        private static string activePath;

        /// <summary>Seed:slot the active profile belongs to.</summary>
        public static string ActiveKey { get; private set; }

        /// <summary>True once this session plays on a dedicated profile.</summary>
        public static bool Active => activePath != null;

        /// <summary>Called every frame while connected. Switches as soon as the game
        /// sits on the title screen with a pending profile request.</summary>
        public static void Tick(ApClient ap)
        {
            if (!ap.Connected) return;
            string key = ap.ProfileKey;
            if (string.IsNullOrEmpty(key) || key == ActiveKey) return;
            if (GameManager.CurGameState != GameManager.GameState.Title) return;
            if (GameManager.BlackScreen || GameManager.SwitchingState) return;
            Switch(key);
        }

        private static void Switch(string key)
        {
            try
            {
                if (vanillaPath == null) vanillaPath = SavePathRef();

                string file = "grunn_ap_" + Sanitize(key);
                string path = Path.Combine(Application.persistentDataPath, file);
                SavePathRef() = path;
                activePath = path;
                ActiveKey = key;

                Reload();

                bool existed = File.Exists(path + SaveManager.curSlotIndex + ".txt");
                Plugin.Log?.LogInfo($"[Grunnchipelago] Profil de sauvegarde : {file}"
                                    + (existed ? " (repris)." : " (nouveau)."));
                Plugin.Ap?.QueuePopup(existed
                    ? "Session Archipelago : sauvegarde dédiée reprise"
                    : "Session Archipelago : sauvegarde dédiée");
            }
            catch (Exception e)
            {
                // Never strand the player on a half-swapped state: go back to vanilla.
                Plugin.Log?.LogError("[Grunnchipelago] Bascule de profil impossible : " + e.Message);
                if (vanillaPath != null) SavePathRef() = vanillaPath;
                activePath = null;
                ActiveKey = null;
            }
        }

        /// <summary>Re-run the boot routine for the (single) slot the game uses, so the
        /// in-memory save matches the new file: load it when it exists, otherwise
        /// create a fresh one - exactly what SaveManager.Awake does
        /// (SaveManager.cs:1406-1418). UpdateSaveDataCheck then re-points
        /// progressDataCheck / globalDataCheck at the new data.</summary>
        private static void Reload()
        {
            int slot = SaveManager.curSlotIndex;
            bool exists = (bool)AccessTools.Method(typeof(SaveManager), "CheckIfFileExists")
                .Invoke(null, new object[] { slot });
            string method = exists ? "LoadFromFile" : "CreateNewSave";
            AccessTools.Method(typeof(SaveManager), method).Invoke(null, new object[] { slot });
            SaveManager.UpdateSaveDataCheck();
            if (!exists) SaveManager.SaveReal(slot);   // materialise the file at once
        }

        /// <summary>Seeds and slot names are user data: keep the file name safe.</summary>
        private static string Sanitize(string key)
        {
            var sb = new System.Text.StringBuilder(key.Length);
            foreach (char c in key)
                sb.Append(char.IsLetterOrDigit(c) || c == '_' || c == '-' ? c : '_');
            return sb.ToString();
        }
    }
}
