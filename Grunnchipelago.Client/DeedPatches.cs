using HarmonyLib;

namespace Grunnchipelago.Client
{
    /// <summary>
    /// "Deed" checks (demande 2026-07-28): checks rewarded for ACTIONS rather than
    /// pickups - using an item on the right spot, completing the school band...
    ///
    /// Every hook below is the game's OWN dedicated method, each guarded by its own
    /// ProgressData flag, so the postfix fires exactly once per run and TrySend dedupes
    /// across runs. Same pattern as MagicPondPlaceFishPatch. Traces (decompiled sources and
    /// dump positions) are recorded in design/backlog_checks.md and in rules.DEED_RULES.
    ///
    /// NOT covered on purpose: "trim every potted plant". The criterion is known
    /// (pottedPlantTrimmedCur >= trimmedPottedPlantMax = 8) but the POSITION of the 8 pots is
    /// absent from dump v0.3, so no logic rule can be written yet - postponed post-launch
    /// [2026-07-28].
    ///
    /// A seed rolled before these locations existed simply has no such location: SendByName
    /// logs "pas une location" and sends nothing. No exception, no side effect.
    /// </summary>
    internal static class Deeds
    {
        /// <summary>Send a deed check if connected. The vanilla flag has already been set by
        /// the method we postfix, so this cannot fire twice in a run.</summary>
        public static void Send(string location)
        {
            ApClient ap = Plugin.Ap;
            if (ap == null || !ap.Connected) return;
            ap.SendDeedCheck(location);
        }
    }

    /// <summary>GameManager.ClearPizzaBox (GameManager.cs:4346), flag clearedPizzaBox -
    /// interaction PizzaBoxClear on the Road.</summary>
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.ClearPizzaBox))]
    public static class DeedPizzaBoxPatch
    {
        private static void Postfix() => Deeds.Send(GameIds.DeedPizzaBox);
    }

    /// <summary>GameManager.PutPrettyFlowerInVase (GameManager.cs:6058), flag
    /// putPrettyFlowerInVase - the vase sits at the church (dump: PrettyVaseContainer).</summary>
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.PutPrettyFlowerInVase))]
    public static class DeedPrettyFlowerPatch
    {
        private static void Postfix() => Deeds.Send(GameIds.DeedPrettyFlower);
    }

    /// <summary>GameManager.KidReadyTrigger (GameManager.cs:6009) runs after EVERY instrument
    /// given, so the check is gated on the band actually being complete - the same
    /// schoolbandCompleteIndex >= 3 the game uses before its own "band completed" prompt.</summary>
    [HarmonyPatch(typeof(GameManager), "KidReadyTrigger")]
    public static class DeedSchoolBandPatch
    {
        private static void Postfix()
        {
            if (SaveManager.progressDataCheck == null) return;
            if (SaveManager.progressDataCheck.schoolbandCompleteIndex < 3) return;
            Deeds.Send(GameIds.DeedSchoolBand);
        }
    }

    /// <summary>Fishbowl.PlaceFishAlive (Fishbowl.cs:79), reached through
    /// InteractionType.FishbowlPlaceFishAlive - the bowl is in the RoundHallway.
    /// Distinct from "Obtain GoldFishAlive", which fires when the DEAD fish is dropped in
    /// the Magic Pond: the bowl still revives nothing.</summary>
    [HarmonyPatch(typeof(Fishbowl), nameof(Fishbowl.PlaceFishAlive))]
    public static class DeedFishbowlPatch
    {
        private static void Postfix() => Deeds.Send(GameIds.DeedFishbowl);
    }

    /// <summary>GameManager.ReturnWorm (GameManager.cs:5887), flag returnedWorm - the worm
    /// hill is church-side geometrically but only REACHABLE FROM HELL [2026-07-28].</summary>
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.ReturnWorm))]
    public static class DeedWormHillPatch
    {
        private static void Postfix() => Deeds.Send(GameIds.DeedWormHill);
    }

    /// <summary>Snail.Award (Snail.cs:177), flag awardedSnail - PillarSpace. The race ends
    /// around 23:45 on day 2, so the logic rule also needs can_advance_days.</summary>
    [HarmonyPatch(typeof(Snail), nameof(Snail.Award))]
    public static class DeedSnailMedalPatch
    {
        private static void Postfix() => Deeds.Send(GameIds.DeedSnailMedal);
    }

    /// <summary>GameManager.ReturnSeveredHand (GameManager.cs:4242), flag
    /// returnedSeveredHand - the corpse is in the Bunker.</summary>
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.ReturnSeveredHand))]
    public static class DeedSeveredHandPatch
    {
        private static void Postfix() => Deeds.Send(GameIds.DeedSeveredHand);
    }
}
