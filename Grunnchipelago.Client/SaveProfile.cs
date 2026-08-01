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

        /// <summary>Set after a failed swap: the feature is disabled for the session
        /// instead of retrying every frame (the first version spammed hundreds of
        /// identical errors per second - retour Jonath).</summary>
        private static bool disabled;

        /// <summary>True once this session plays on a dedicated profile.</summary>
        public static bool Active => activePath != null;

        /// <summary>Called every frame while connected. Switches as soon as the game
        /// sits on the title screen with a pending profile request.</summary>
        public static void Tick(ApClient ap)
        {
            if (disabled || !ap.Connected) return;
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

                // BEFORE Reload: it creates the file when it is missing, so asking afterwards
                // always answered "already there" and the label lied about which case we
                // were in - a detail, but one we read while diagnosing.
                bool existed = File.Exists(path + SaveManager.curSlotIndex + ".txt");

                Reload();
                // Everything the SCENE cached from the old save file, re-read from the new
                // one - see WorldState for the full list and the rule on where a new leak
                // belongs [J 2026-08-01].
                WorldState.ReapplyFromSave();
                RefreshTitleScreen();

                Plugin.Log?.LogInfo($"[Grunnchipelago] Profil de sauvegarde : {file}"
                                    + (existed ? " (repris)." : " (nouveau)."));
                Plugin.Ap?.QueuePopup(existed
                    ? "Session Archipelago : sauvegarde dédiée reprise"
                    : "Session Archipelago : sauvegarde dédiée");
            }
            catch (Exception e)
            {
                // Never strand the player on a half-swapped state: go back to vanilla,
                // and give up for the session rather than retrying every frame.
                Plugin.Log?.LogError("[Grunnchipelago] Bascule de profil impossible, "
                                     + "sauvegarde vanilla conservee : " + e);
                if (vanillaPath != null) SavePathRef() = vanillaPath;
                activePath = null;
                ActiveKey = null;
                disabled = true;
            }
        }

        /// <summary>Make the title screen agree with the save we just switched to.
        ///
        /// Loading the file is not enough: the menu is BUILT from that data when the game
        /// enters the title screen, which happened before our swap. UIManager.UpdateMenuOptions
        /// picks "Continue" or "Start" from progressDataCheck.startedRun (UIManager.cs:1172),
        /// and TimeController.SetCurrentTimeToTitleHour sets the scene's clock. Leaving both
        /// stale let the player CONTINUE a run belonging to the other save - dropped in the
        /// park with no items - and eventually broke the menu outright
        /// [J 2026-08-01: "l'UI du menu principal a disparu", the hut lit as in save A].
        ///
        /// So the game's own title-entry work is replayed: reset the derived values, rebuild
        /// the menu, re-read the title clock.</summary>
        private static void RefreshTitleScreen()
        {
            try
            {
                GameManager.ResetValuesForTitleScreen();
                UIManager.UpdateMenuOptions();
                if (TimeController.instance != null)
                    TimeController.instance.SetCurrentTimeToTitleHour();
            }
            catch (Exception e)
            {
                Plugin.Log?.LogWarning(
                    "[Grunnchipelago] Rafraichissement du menu apres bascule echoue : " + e.Message);
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

            if (exists)
            {
                // LoadFromFile is an INSTANCE method (SaveManager.cs:2050) - calling it
                // with a null target threw "Non-static method requires a target" every
                // frame (retour Jonath). SaveManager is a MonoBehaviour with no static
                // accessor, so fetch the live component.
                var manager = UnityEngine.Object.FindObjectOfType<SaveManager>();
                if (manager == null) throw new Exception("SaveManager introuvable en scene.");
                AccessTools.Method(typeof(SaveManager), "LoadFromFile")
                    .Invoke(manager, new object[] { slot });
            }
            else
            {
                // CreateNewSave is static (SaveManager.cs:2001).
                AccessTools.Method(typeof(SaveManager), "CreateNewSave")
                    .Invoke(null, new object[] { slot });
            }

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
