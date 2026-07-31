using System;
using HarmonyLib;
using UnityEngine;

namespace Grunnchipelago.Client
{
    /// <summary>
    /// Time-of-day conveniences added on top of vanilla (demande Jonath 2026-07-27).
    ///
    /// Vanilla only ever advances the clock by ONE hour, through
    /// <c>GameManager.Wait1Hour</c> (GameManager.cs:6276) -> <c>TimeController.Skip1Hour</c>
    /// (TimeController.cs:425), reachable from the 15 benches carrying an
    /// <c>InteractionType.Wait1Hour</c> interaction. This adds:
    ///   - the GARDEN RAINPIPE (Main/Triggers/Rainpipe, the only one in the dump): examining
    ///     it jumps straight to 00:05, so a stalled sphere-1 run can reach the Darkness
    ///     ending without waiting in real time;
    ///   - the CHURCH BENCH nearest the church door: same jump to 00:05;
    ///   - the three PARK benches: 3 hours instead of 1.
    /// Every other bench keeps its vanilla single hour.
    ///
    /// 00:05 (not 00:00) is deliberate: <c>TimeController.UpdateTimeOfDay</c> starts the
    /// Darkness mist as soon as <c>currentHour &lt;= 3</c>, while the day counter only
    /// increments on the exact <c>currentHour &lt;= 0 &amp;&amp; currentMinute &lt;= 0</c>
    /// tick - which a jump straight to 00:05 skips. So the night (and the Darkness ending)
    /// arrives, but the DAY does not advance: sleeping stays the only way to change day,
    /// which is what lock_player_hut logic assumes.
    /// </summary>
    internal static class TimeFeatures
    {
        /// <summary>Night target for the rainpipe and the church bench (choix Jonath).</summary>
        private const int NightHour = 0;
        private const int NightMinute = 5;

        /// <summary>On the MIST DAY there is no night to jump to - the run ends in the fog
        /// instead - so the rainpipe and the church bench aim at 11:15 to speed that up
        /// [demande Jonath 2026-07-27]. The hour matters: HandleMist ignores everything
        /// before 10:00 (GameManager.cs:2380), the fog peaks at mistHour = 12:00 and the
        /// ending is forced at mistHour + 2 = 14:00 (GameManager.cs:2384). 11:15 sits just
        /// inside the active window without skipping the sequence.</summary>
        private const int MistDayHour = 11;
        private const int MistDayMinute = 15;

        /// <summary>Night Trap target: 03:00, still inside the Darkness window
        /// (currentHour &lt;= 3) with the most margin before 04:00 (choix Jonath).</summary>
        private const int TrapNightHour = 3;
        private const int TrapNightMinute = 0;

        /// <summary>Total hours a park bench skips (vanilla is 1).</summary>
        private const int ParkBenchHours = 3;

        private enum WaitKind { Vanilla, ParkBench, NightBench }

        /// <summary>Which bench the player is currently triggering, captured in the
        /// Interaction.Trigger prefix because GameManager.Wait1Hour is static and does not
        /// know its caller.</summary>
        private static WaitKind pendingWait = WaitKind.Vanilla;

        /// <summary>Classify the interaction about to run (called from the patch below).</summary>
        public static void NoteInteraction(Interaction interaction)
        {
            pendingWait = WaitKind.Vanilla;
            if (interaction == null || interaction.interactionType != InteractionType.Wait1Hour) return;

            string path = ScenePaths.Of(interaction.transform);
            if (GameIds.ParkBenchPaths.Contains(path)) pendingWait = WaitKind.ParkBench;
            else if (path == GameIds.ChurchNightBenchPath) pendingWait = WaitKind.NightBench;
        }

        /// <summary>Runs right after a vanilla Wait1Hour. <paramref name="minutesBefore"/> is
        /// the clock before the call: if it did not move, vanilla refused the wait
        /// (CanWait1Hour: night, final day, non-euclidian space...) and we refuse too.</summary>
        public static void AfterWait(int minutesBefore)
        {
            WaitKind kind = pendingWait;
            pendingWait = WaitKind.Vanilla;
            if (kind == WaitKind.Vanilla) return;
            if (TimeController.instance == null) return;
            if (CurrentMinutes() == minutesBefore) return;   // vanilla refused it

            if (kind == WaitKind.ParkBench)
            {
                // Vanilla already skipped one hour; add the rest.
                for (int i = 1; i < ParkBenchHours; i++) TimeController.instance.Skip1Hour();
                Plugin.Log?.LogInfo($"[Grunnchipelago] Banc du parc : {ParkBenchHours} h passees.");
            }
            else
            {
                // Vanilla already played the black screen + wait sound for this bench.
                JumpTo(NightHour, NightMinute, "Banc de l'eglise");
            }
        }

        /// <summary>Set the clock forward to the next occurrence of hour:minute. Never goes
        /// backwards: from 14:00 to 00:05 means the next calendar day of the underlying
        /// DateTime (the game itself only ever reads .Hour / .Minute out of it - see
        /// TimeController.UpdateCachedValues).</summary>
        public static void JumpTo(int hour, int minute, string reason)
        {
            if (TimeController.instance == null) return;
            try
            {
                DateTime now = TimeController.currentTime;
                DateTime target = now.Date.AddHours(hour).AddMinutes(minute);
                if (target <= now) target = target.AddDays(1.0);
                TimeController.currentTime = target;
                GameManager.ForceUpdateTimeString();
                Plugin.Log?.LogInfo(
                    $"[Grunnchipelago] {reason} : saut de {now:HH:mm} a {target:HH:mm}.");
            }
            catch (Exception e)
            {
                Plugin.Log?.LogWarning("[Grunnchipelago] Saut horaire echoue : " + e.Message);
            }
        }

        /// <summary>Rainpipe / church bench target: 00:05 normally, 11:15 on the mist day.
        /// Never rewinds the clock - past 11:15 on the mist day it advances one hour, which
        /// still pushes towards the 14:00 forced ending.</summary>
        public static void JumpToNight(string reason)
        {
            PlayWaitFeedback();
            if (!GameManager.IsFinalDay)
            {
                JumpTo(NightHour, NightMinute, reason);
                return;
            }
            int now = CurrentMinutes();
            if (now < MistDayHour * 60 + MistDayMinute)
            {
                JumpTo(MistDayHour, MistDayMinute, reason + " (jour de brume)");
            }
            else if (TimeController.instance != null)
            {
                // Already past the target: jumping to 11:15 would mean "tomorrow" and read
                // as the clock going backwards. Push one hour towards the 14:00 ending.
                TimeController.instance.Skip1Hour();
                Plugin.Log?.LogInfo(
                    $"[Grunnchipelago] {reason} (jour de brume) : deja passe 11h15, +1 h.");
            }
        }

        /// <summary>The Night Trap target (03:00), with the vanilla wait presentation.</summary>
        public static void JumpToTrapNight()
        {
            PlayWaitFeedback();
            JumpTo(TrapNightHour, TrapNightMinute, "Night Trap");
        }

        /// <summary>Black screen + wait sound, exactly as GameManager.Wait1Hour does
        /// (GameManager.cs:6285-6287), so a jump never happens abruptly.</summary>
        public static void PlayWaitFeedback()
        {
            try
            {
                GameManager.TriggerBlackScreen(60f);
                AudioManager.instance?.PlaySoundGlobal(
                    BasicFunctions.PickRandomAudioClipFromArray(AudioManager.instance.wait1Hour),
                    1.1f, 1.3f, 0.2f, 0.225f);
            }
            catch (Exception) { }
        }

        /// <summary>Clock as minutes-of-day, used to detect that time actually moved.</summary>
        private static int CurrentMinutes() => TimeController.currentHour * 60 + TimeController.currentMinute;

        /// <summary>Same situations as a vanilla bench wait, MINUS the final-day veto.
        ///
        /// GameManager.CanWait1Hour (GameManager.cs:6322) refuses outright on the final day
        /// - which is exactly the mist day the rainpipe is now meant to speed through, so
        /// that one test is dropped and every other guard is mirrored verbatim.</summary>
        public static bool CanJump()
        {
            var pd = SaveManager.progressDataCheck;
            if (pd == null) return false;
            if (pd.inRedWorld || pd.inSpookyWorld || pd.inGoodEnding) return false;
            if (GameManager.curSpaceMode == GameManager.SpaceMode.NonEuclidian) return false;
            if (GameManager.BlackScreen || GameManager.SwitchingState) return false;
            if (GameManager.CurGameState != GameManager.GameState.Game) return false;
            if (TimeController.currentHour <= 3 || TimeController.currentHour >= 24) return false;
            return true;
        }
    }

    // NOTE on gating: Plugin.Awake only calls Harmony.PatchAll() when the mod is enabled
    // in config, so a disabled mod is 100 % vanilla and these patches do not exist. They
    // deliberately do NOT require an Archipelago connection - they are game features, not
    // randomizer plumbing.

    /// <summary>Captures WHICH bench is being used, just before Wait1Hour runs.</summary>
    [HarmonyPatch(typeof(Interaction), nameof(Interaction.Trigger))]
    public static class InteractionTriggerTimePatch
    {
        private static void Prefix(Interaction __instance) => TimeFeatures.NoteInteraction(__instance);
    }

    /// <summary>Turns the vanilla single hour into 3 h (park benches) or a jump to night
    /// (church bench). Other benches are untouched.</summary>
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.Wait1Hour))]
    public static class Wait1HourPatch
    {
        private static void Prefix(out int __state)
        {
            __state = TimeController.currentHour * 60 + TimeController.currentMinute;
        }

        private static void Postfix(int __state) => TimeFeatures.AfterWait(__state);
    }

    /// <summary>The garden rainpipe (Main/Triggers/Rainpipe, the only one in the dump):
    /// examining it now also jumps to 00:05. Vanilla Rainpipe.Trigger only arms a listening
    /// timer (Rainpipe.cs:74) and has no other caller, so nothing else can fire this.</summary>
    [HarmonyPatch(typeof(Rainpipe), nameof(Rainpipe.Trigger))]
    public static class RainpipeTimePatch
    {
        private static void Postfix()
        {
            if (!TimeFeatures.CanJump()) return;
            TimeFeatures.JumpToNight("Gouttiere");
        }
    }
}
