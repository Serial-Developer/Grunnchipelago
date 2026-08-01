using HarmonyLib;

namespace Grunnchipelago.Client
{
    /// <summary>
    /// "Chore" checks (demande Jonath 2026-07-30): the five START-GARDEN maintenance jobs.
    ///
    /// Vanilla pays <c>GameManager.areaCompleteGuldenAdd</c> = 2 gulden the FIRST time each
    /// job is completed, and only in the start garden - each guarded by its own
    /// ProgressData flag:
    ///   grass      GameManager.CutGrass:3131   cutAllGrassInStartGardenArea
    ///   hedges     TrimBall.Trim:149           trimmedAllHedgesInStartGardenArea
    ///   molehills  Molehill.Remove:184         removedAllMolehillsInStartGardenArea
    ///   flowers    Flower.Water:529            wateredAllFlowersInStartGardenArea
    ///   litter     Troepje.Trigger:100         clearedAllTrashInStartGardenArea
    ///
    /// Those payouts become CHECKS. Each patch below reads the flag BEFORE the vanilla call
    /// and sends the check when the call flipped it - so the check fires exactly when the
    /// game decided the job was done, with no duplicated condition of our own.
    ///
    /// The 2 vanilla gulden are NOT suppressed here: the AddGulden they trigger is already
    /// swallowed by AddGuldenPatch when a check is in flight, and the pool hands the money
    /// back as five "Golden Gulden" worth 2 each (items.py).
    /// </summary>
    internal static class Chores
    {
        public static void Send(string location)
        {
            ApClient ap = Plugin.Ap;
            if (ap == null || !ap.Connected) return;
            ap.SendDeedCheck(location);   // same by-name path as the deeds
        }

        /// <summary>Snapshot of the five flags, taken in the prefixes.</summary>
        public static bool Grass => Flag(f => f.cutAllGrassInStartGardenArea);
        public static bool Hedges => Flag(f => f.trimmedAllHedgesInStartGardenArea);
        public static bool Molehills => Flag(f => f.removedAllMolehillsInStartGardenArea);
        public static bool Flowers => Flag(f => f.wateredAllFlowersInStartGardenArea);
        public static bool Litter => Flag(f => f.clearedAllTrashInStartGardenArea);

        private static bool Flag(System.Func<SaveManager.ProgressData, bool> read)
        {
            var data = SaveManager.progressDataCheck;
            return data != null && read(data);
        }
    }

    /// <summary>Grass: GameManager.CutGrass (GameManager.cs:3098).
    /// NOTE: unlike the four others, the vanilla guard here does NOT test
    /// myArea == StartGarden - it only checks the cutAllGrassInStartGardenArea flag, so the
    /// bonus actually lands on whichever zone is finished FIRST (vanilla quirk). Reading the
    /// flag rather than re-deriving the condition keeps us faithful to that behaviour.</summary>
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.CutGrass))]
    public static class ChoreGrassPatch
    {
        private static void Prefix(out bool __state) => __state = Chores.Grass;

        private static void Postfix(bool __state)
        {
            if (!__state && Chores.Grass) Chores.Send(GameIds.ChoreGrass);
        }
    }

    /// <summary>Hedges: TrimBall.Trim (TrimBall.cs:135).</summary>
    [HarmonyPatch(typeof(TrimBall), nameof(TrimBall.Trim))]
    public static class ChoreHedgesPatch
    {
        private static void Prefix(out bool __state) => __state = Chores.Hedges;

        private static void Postfix(bool __state)
        {
            if (!__state && Chores.Hedges) Chores.Send(GameIds.ChoreHedges);
        }
    }

    /// <summary>Molehills: Molehill.Remove (Molehill.cs:166).</summary>
    [HarmonyPatch(typeof(Molehill), nameof(Molehill.Remove))]
    public static class ChoreMolehillsPatch
    {
        private static void Prefix(out bool __state) => __state = Chores.Molehills;

        private static void Postfix(bool __state)
        {
            if (!__state && Chores.Molehills) Chores.Send(GameIds.ChoreMolehills);
        }
    }

    /// <summary>Flowers: Flower.Water (Flower.cs:503).</summary>
    [HarmonyPatch(typeof(Flower), nameof(Flower.Water))]
    public static class ChoreFlowersPatch
    {
        private static void Prefix(out bool __state) => __state = Chores.Flowers;

        private static void Postfix(bool __state)
        {
            if (!__state && Chores.Flowers) Chores.Send(GameIds.ChoreFlowers);
        }
    }

    /// <summary>Litter: Troepje.Trigger (Troepje.cs:81) - "troepje" is a piece of litter.</summary>
    [HarmonyPatch(typeof(Troepje), nameof(Troepje.Trigger))]
    public static class ChoreLitterPatch
    {
        private static void Prefix(out bool __state) => __state = Chores.Litter;

        private static void Postfix(bool __state)
        {
            if (!__state && Chores.Litter) Chores.Send(GameIds.ChoreLitter);
        }
    }

    /// <summary>Potted plants: PottedPlant.Trigger (PottedPlant.cs:73).
    /// The odd one out - vanilla has NO boolean flag here, only a counter compared to
    /// SaveManager.trimmedPottedPlantMax (= 8), which is also what gates its achievement.
    /// So this patch watches the COUNTER crossing the threshold instead of a flag; TrySend
    /// dedupes anyway if a ninth pot ever existed.</summary>
    [HarmonyPatch(typeof(PottedPlant), nameof(PottedPlant.Trigger))]
    public static class ChorePottedPlantsPatch
    {
        private static void Prefix(out int __state)
        {
            __state = SaveManager.progressDataCheck?.pottedPlantTrimmedCur ?? 0;
        }

        private static void Postfix(int __state)
        {
            var data = SaveManager.progressDataCheck;
            if (data == null) return;
            int max = SaveManager.trimmedPottedPlantMax;
            if (__state < max && data.pottedPlantTrimmedCur >= max)
                Chores.Send(GameIds.ChorePottedPlants);
        }
    }

}
