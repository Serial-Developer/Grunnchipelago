using System;
using UnityEngine;

namespace Grunnchipelago.Client
{
    /// <summary>
    /// SAVE-DERIVED SCENE STATE - the single place that re-applies it after a profile switch
    /// [demande 2026-08-01, "une methode clean qui fixe tout ce qu'il y a a fixer"].
    ///
    /// Grunn loads ONE scene and never reloads it. Several components read the save file
    /// exactly once, at boot, and cache what they read. Swapping SaveManager's path at the
    /// title screen replaces the FILE, so anything already cached keeps describing the
    /// previous multiworld - which is where every leak found so far came from:
    ///
    ///   - the WORLD ITSELF - doors, polaroids, the bus, the per-area counters. Grunn only
    ///     resets it in TriggerNewRun, which pressing "Start" does NOT reach, so a fresh save
    ///     inherited the previous multiworld's world (see ResetWorldForNewSave);
    ///   - the player position (see ReapplyPlayerPosition);
    ///   - the inventory and tools;
    ///   - the gulden / area / instruction-sign strings on screen.
    ///
    /// Everything else that leaked is owned by the mod and is reset by its owner, called
    /// from Plugin's session-reset block: ModelSwap.ResetForNewSession (posed clones and the
    /// harvested library), GiftPickups.ResetForNewSession (the bone / compass / strange-key
    /// clones), ApClient.ResetSessionState (checks, scouts, gift flags), ConsoleUi.Clear.
    ///
    /// RULE FOR THE NEXT LEAK: if a value comes from the save file and is read once at boot,
    /// it belongs HERE. If it is state the mod itself built, it belongs in that component's
    /// own ResetForNewSession. Nothing should be reset in two places.
    /// </summary>
    internal static class WorldState
    {
        /// <summary>Called from SaveProfile.Switch, right after the new save is loaded and
        /// BEFORE the title screen is rebuilt.</summary>
        public static void ReapplyFromSave()
        {
            int applied = 0;
            applied += Try("position du joueur", ReapplyPlayerPosition);
            applied += Try("inventaire et outils", ReapplyTools);
            applied += Try("affichages (gulden, zones, panneau)", ReapplyStrings);
            applied += Try("effets temporaires", ResetTimedEffects);
            // The world itself cannot be reset from here - see WorldResetPending.
            WorldResetPending = true;
            Plugin.Log?.LogInfo($"[Grunnchipelago] Etat de scene reapplique depuis la nouvelle "
                + $"sauvegarde ({applied}/4 volets), remise a neuf du monde armee.");
        }

        /// <summary>THE root cause of nearly every switch symptom [2026-08-01, log].
        ///
        /// Grunn NEVER resets the world when you start playing. UIManager.
        /// SelectOptionThatStartsGame just flips startedRun and enters GameState.Game
        /// (UIManager.cs:2450-2458), and the entry point only calls LoadWorldFromSave
        /// (GameManager.ToGame, GameManager.cs:3539-3543). TriggerNewRun - the one routine
        /// that calls ResetWorld - is reached only by Restart and by "overwrite run".
        ///
        /// Vanilla can afford that: on a fresh save the game has just launched, so the world
        /// IS pristine. Our profile switch breaks the assumption - the scene still carries
        /// the previous multiworld's world state, and nothing puts it back. Everything
        /// reported follows from it, and the log proved it in one line: busActif=False on
        /// entering save 2, with no TriggerNewRun anywhere.
        ///
        ///   doors left open, polaroids taken in save 1 still hidden, the bus gone, the
        ///   player never placed, and the per-area counters (hence "all the hedges trimmed"
        ///   after a single hedge).
        ///
        /// ResetWorld is the game's own routine for exactly this: it re-runs ResetState on
        /// every registered object and resets every ContentHider (GameManager.cs:3918+), each
        /// one re-reading the NEW save through its own CheckForLoadOperation.
        ///
        /// The maxes are zeroed first because ResetState raises them by one per object
        /// (StoreIndex): vanilla gets away with it by running ResetRunProgress immediately
        /// before, on data where they are already zero. We keep the loaded save's progress
        /// and only rebuild the counts.</summary>
        /// <summary>Armed by the profile switch, honoured on the next world load.
        ///
        /// ResetWorld CANNOT run from the title screen: it threw a NullReferenceException
        /// there every time and the whole step was skipped [2026-08-01, log: "Reapplication
        /// 'monde' echouee"]. Half of it had already run when it threw, which is very likely
        /// what left the game frozen on "recommencer" afterwards. The world simply is not
        /// live at the menu.
        ///
        /// So it waits for LoadWorldFromSave - the route the game really takes when entering
        /// a save (GameManager.ToGame) - where every object is loaded and ResetWorld is the
        /// routine's normal companion.</summary>
        public static bool WorldResetPending { get; set; }

        /// <summary>Called from the LoadWorldFromSave postfix, world live.</summary>
        public static void ResetWorldIfPending()
        {
            if (!WorldResetPending) return;
            WorldResetPending = false;
            try
            {
                ResetWorldForNewSave();
                Plugin.Log?.LogInfo("[Grunnchipelago] Monde remis a neuf pour la nouvelle "
                    + "sauvegarde (portes, polaroids, bus, compteurs).");
            }
            catch (Exception e)
            {
                Plugin.Log?.LogWarning("[Grunnchipelago] Remise a neuf du monde echouee : " + e);
            }
        }

        private static void ResetWorldForNewSave()
        {
            var data = SaveManager.progressDataCheck;
            if (data?.areaProgress != null)
                foreach (SaveManager.AreaProgress area in data.areaProgress)
                {
                    if (area == null) continue;
                    area.grassCutMax = 0;
                    area.trimBallMax = 0;
                    area.molehillMax = 0;
                    area.flowerMax = 0;
                    area.troepjeMax = 0;
                }
            GameManager.ResetWorld();
        }

        /// <summary>The player position setter [2026-08-01: "je reprends a la meme position
        /// que dans la save 1"].
        ///
        /// SetPlayerPosAndRot of type Load reads playerPos/playerRot into its OWN transform in
        /// Init, guarded by a `loaded` flag, so it happens once per launch
        /// (SetPlayerPosAndRot.cs:118-124). GameManager.LoadWorldFromSave then teleports the
        /// player onto that transform (GameManager.cs:3885). After a switch it therefore still
        /// held the previous save's spot.
        ///
        /// Worse, HandleLoadSetter copies the LIVE player position back into it every 120
        /// frames and calls SaveManager.SavePlayerPosAndRot - so the stale spot was also being
        /// written INTO the new save file. Re-seating the transform fixes both directions.</summary>
        private static void ReapplyPlayerPosition()
        {
            SetPlayerPosAndRot setter = GameManager.loadPosAndRotSetter;
            var data = SaveManager.progressDataCheck;
            if (setter == null || data == null) return;

            Vector3 position = data.playerPos.UnityVector;
            Vector3 euler = data.playerRot.UnityVector;
            setter.transform.position = position;
            setter.transform.rotation = Quaternion.Euler(euler);
            if (setter.myTransform != null && setter.myTransform != setter.transform)
            {
                setter.myTransform.position = position;
                setter.myTransform.rotation = Quaternion.Euler(euler);
            }
        }

        /// <summary>PlayerManager caches the tool list from the save; the game itself re-reads
        /// it on every new run (TriggerNewRun, GameManager.cs:3772), so calling the same
        /// method is faithful rather than invented.</summary>
        private static void ReapplyTools()
        {
            PlayerManager.ClearTools();
            PlayerManager.LoadToolsFromSave();
        }

        /// <summary>On-screen strings built from the save at boot. Cosmetic, but a menu still
        /// showing the other multiworld's money reads as a bug.</summary>
        private static void ReapplyStrings()
        {
            UIManager.UpdateGuldenString();
            UIManager.BuildInstructionSignString();
            UIManager.instance?.UpdateAreaPercentageString();
        }

        /// <summary>Timed traps are pinned to a day and hour of the PREVIOUS save, so they
        /// would expire at a moment that means nothing in the new one.</summary>
        private static void ResetTimedEffects()
        {
            Effects.ResetForNewSession();
        }

        /// <summary>Each step is independent: one failing must not cost the others. The name
        /// is logged so a failure says which part of the state stayed stale.</summary>
        private static int Try(string what, Action step)
        {
            try
            {
                step();
                return 1;
            }
            catch (Exception e)
            {
                // Full exception, not just Message: two of these came back with an EMPTY
                // message and told us nothing [2026-08-01].
                Plugin.Log?.LogWarning($"[Grunnchipelago] Reapplication '{what}' echouee : " + e);
                return 0;
            }
        }
    }
}
