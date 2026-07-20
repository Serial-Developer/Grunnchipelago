using System;
using System.Collections.Generic;
using UnityEngine;

namespace Grunnchipelago.Client
{
    /// <summary>
    /// Buff and trap effects.
    ///
    /// Buffs are cumulative per tier and stateless: the counts are recomputed from the
    /// full received-items list (so re-injection after a run and reconnects are always
    /// consistent), and the resulting multipliers are (re)applied every frame by Tick().
    ///
    /// Timed traps (Speed / Size / Inverted Controls) last 1 in-game hour
    /// (TimeController.currentHour) and expire on day change / run reset.
    /// Regrow traps are one-shot save-data edits (see ApplyRegrowTrap).
    /// </summary>
    public static class Effects
    {
        // --- Buff tiers (a calibrer) -------------------------------------------------
        private const float MoveSpeedPerTier = 0.15f;    // +15 % move speed per boost
        private const float CutterRangePerTier = 0.25f;  // +25 % cutter scale per boost
        private const float CuttingRatePerTier = 0.25f;  // +25 % swing speed per boost
        private const float TrapSpeedMultiplier = 0.5f;  // Speed Trap: half speed
        private const float TrapSizeMultiplier = 0.45f;  // Size Trap: shrunk player

        /// <summary>Timed traps last this many IN-GAME hours (2 h, demande Jonath -
        /// 1 h passait trop vite pour se faire sentir).</summary>
        private const int TrapDurationHours = 2;

        public static int MoveSpeedBoosts;
        public static int CutterRangeBoosts;
        public static int CuttingRateBoosts;

        public static float CutterScaleMultiplier => 1f + CutterRangeBoosts * CutterRangePerTier;
        public static float SwingSpeedMultiplier => 1f + CuttingRateBoosts * CuttingRatePerTier;
        private static float MoveSpeedMultiplier =>
            (1f + MoveSpeedBoosts * MoveSpeedPerTier) * (SpeedTrapActive ? TrapSpeedMultiplier : 1f);

        // --- Timed traps ---------------------------------------------------------------
        private struct TimedTrap
        {
            public bool active;
            public int startDay;
            public int startHour;
        }

        private static TimedTrap speedTrap;
        private static TimedTrap sizeTrap;
        private static TimedTrap invertedTrap;

        public static bool SpeedTrapActive => speedTrap.active;
        public static bool SizeTrapActive => sizeTrap.active;
        public static bool InvertedControlsActive => invertedTrap.active;

        // --- Player state capture (originals restored on disconnect) --------------------
        private static PlayerControllerNew player;
        private static float origWalk, origRun, origCrouch;
        private static Vector3 origScale;
        private static bool captured;

        /// <summary>Apply a received trap by item name. Runs on the main thread, in-game
        /// (the pending-items queue guarantees that).</summary>
        public static void ApplyTrap(string name)
        {
            int day = SaveManager.progressDataCheck?.dayIndex ?? 0;
            int hour = TimeController.currentHour;
            var timed = new TimedTrap { active = true, startDay = day, startHour = hour };

            switch (name)
            {
                case GameIds.TrapSpeed: speedTrap = timed; break;
                case GameIds.TrapSize: sizeTrap = timed; break;
                case GameIds.TrapInvertedControls: invertedTrap = timed; break;
                // Regrow Grass Trap is NO LONGER generated (apworld: the grass is
                // DOTS-rendered and only rebuilt when the world loads, so it could not
                // grow back mid-run - the counter dropped with no way to recover, which
                // could block zone-completion checks). Ignored if an old seed sends it.
                case GameIds.TrapRegrowGrass:
                    Plugin.Log?.LogInfo("[Grunnchipelago] Regrow Grass Trap ignore (non implementable en cours de run).");
                    return;
                case GameIds.TrapRewaterFlowers: ApplyRegrowTrap(Element.Flowers); break;
                case GameIds.TrapRegrowHedge: ApplyRegrowTrap(Element.Hedge); break;
                case GameIds.TrapReturnTrash: ApplyRegrowTrap(Element.Trash); break;
                case GameIds.TrapRegrowMolehills: ApplyRegrowTrap(Element.Molehills); break;
                default: return;
            }
            try { UIManager.instance?.AddPopup(name + "!"); } catch (Exception) { }
        }

        /// <summary>Per-frame maintenance: trap expiry + (re)application of speed/scale.
        /// When the mod is inactive, restores vanilla values once.</summary>
        public static void Tick(bool modActive)
        {
            if (!modActive)
            {
                RestoreVanilla();
                return;
            }

            ExpireTimedTraps();

            // The controller can be recreated across scene reloads - re-capture lazily.
            if (player == null)
            {
                player = PlayerControllerNew.characterController != null
                    ? PlayerControllerNew.characterController.GetComponent<PlayerControllerNew>()
                    : null;
                captured = false;
                if (player == null) return;
            }
            if (!captured)
            {
                origWalk = player.walkSpeed;
                origRun = player.runSpeed;
                origCrouch = player.crouchSpeed;
                origScale = player.transform.localScale;
                captured = true;
            }

            // Move Speed buff + Speed Trap (PlayerControllerNew.cs:63-67 public fields;
            // targetSpeed derives from these each frame, PlayerControllerNew.cs:445-450).
            float m = MoveSpeedMultiplier;
            player.walkSpeed = origWalk * m;
            player.runSpeed = origRun * m;
            player.crouchSpeed = origCrouch * m;

            // Size Trap: shrink the player root (CharacterController capsule scales too).
            player.transform.localScale = origScale * (SizeTrapActive ? TrapSizeMultiplier : 1f);
        }

        private static void ExpireTimedTraps()
        {
            int day = SaveManager.progressDataCheck?.dayIndex ?? 0;
            int hour = TimeController.currentHour;
            Expire(ref speedTrap, day, hour);
            Expire(ref sizeTrap, day, hour);
            Expire(ref invertedTrap, day, hour);
        }

        private static void Expire(ref TimedTrap trap, int day, int hour)
        {
            // TrapDurationHours in-game hours (TimeController.cs:151); a day change or
            // run reset also ends it.
            if (trap.active && (day != trap.startDay || hour >= trap.startHour + TrapDurationHours))
                trap.active = false;
        }

        private static void RestoreVanilla()
        {
            if (!captured || player == null) return;
            player.walkSpeed = origWalk;
            player.runSpeed = origRun;
            player.crouchSpeed = origCrouch;
            player.transform.localScale = origScale;
            captured = false;
            speedTrap.active = sizeTrap.active = invertedTrap.active = false;
        }

        // --- Stats report (playtest H.2) --------------------------------------------------

        /// <summary>Multiline report of the modified stats, base = 100 %. Active traps show
        /// their remaining IN-GAME time ("0h34"). With showAllLines false, only the lines
        /// that differ from base are listed.</summary>
        public static string BuildStatsText(bool showAllLines)
        {
            var sb = new System.Text.StringBuilder();

            float speed = (1f + MoveSpeedBoosts * MoveSpeedPerTier)
                          * (SpeedTrapActive ? TrapSpeedMultiplier : 1f) * 100f;
            if (showAllLines || !Mathf.Approximately(speed, 100f))
                sb.AppendLine($"Vitesse de déplacement : {speed:0} %{TrapSuffix(speedTrap)}");

            float range = CutterScaleMultiplier * 100f;
            if (showAllLines || !Mathf.Approximately(range, 100f))
                sb.AppendLine($"Portée du sécateur : {range:0} %");

            float rate = SwingSpeedMultiplier * 100f;
            if (showAllLines || !Mathf.Approximately(rate, 100f))
                sb.AppendLine($"Cadence de découpe : {rate:0} %");

            float size = SizeTrapActive ? TrapSizeMultiplier * 100f : 100f;
            if (showAllLines || SizeTrapActive)
                sb.AppendLine($"Taille : {size:0} %{TrapSuffix(sizeTrap)}");

            if (showAllLines || InvertedControlsActive)
                sb.AppendLine(InvertedControlsActive
                    ? $"Contrôles : INVERSÉS{TrapSuffix(invertedTrap)}"
                    : "Contrôles : normaux");

            return sb.ToString();
        }

        private static string TrapSuffix(TimedTrap trap)
        {
            if (!trap.active) return "";
            int now = TimeController.currentHour * 60 + TimeController.currentMinute;
            int end = (trap.startHour + TrapDurationHours) * 60;
            int left = Mathf.Max(0, end - now);
            return $" ({left / 60}h{left % 60:00})";
        }

        // --- Regrow traps ----------------------------------------------------------------
        // design/apworld_design.md section 4: reset ONE element of ONE zone - clear the
        // matching entries of the global position list + zero the per-area counter
        // (SaveManager.AreaProgress, SaveManager.cs:30). The zone is picked randomly among
        // the ones where the element has progression. World visuals refresh on reload -
        // that reload behaviour is the priority playtest item.

        private enum Element { Grass, Flowers, Hedge, Trash, Molehills }

        private static void ApplyRegrowTrap(Element element)
        {
            var pd = SaveManager.progressDataCheck;
            if (pd?.areaProgress == null || GameManager.instance == null) return;

            // Eligible zones: Area enum (Area.cs) StartGarden=0, Church=1, Park=2.
            var eligible = new List<int>();
            for (int i = 0; i < 3 && i < pd.areaProgress.Length; i++)
                if (CounterOf(pd.areaProgress[i], element) > 0)
                    eligible.Add(i);
            if (eligible.Count == 0)
            {
                Plugin.Log?.LogInfo("[Grunnchipelago] Regrow trap fizzled (no progression anywhere).");
                return;
            }

            int area = eligible[UnityEngine.Random.Range(0, eligible.Count)];
            SetCounter(pd.areaProgress[area], element, 0);

            // Drop the stored positions that fall inside that zone's macro bounds
            // (PolygonTommie fields on GameManager, as used by the scene dumper).
            List<SerializableVector3> list = ListOf(pd, element);
            PolygonTommie bounds = BoundsOf(area);
            if (list != null && bounds != null)
                list.RemoveAll(v => bounds.ContainsPoint(v.UnityVector));

            SaveManager.Save(SaveManager.curSlotIndex);
            // Restore the objects IN WORLD right away (session 2, retour Jonath: the
            // counter dropped but nothing grew back, so the zone could never reach
            // 100 % again that run). Each ResetState re-reads the (now cleared) save
            // positions via its own CheckForLoadOperation, exactly like ResetWorld
            // does on a run reset - GameManager.cs:3918-4023.
            int restored = RestoreObjects(element, (Area)area);
            Plugin.Log?.LogInfo(
                $"[Grunnchipelago] Regrow trap: {element} reset in area {(Area)area} ({restored} objets restaures).");
        }

        /// <summary>Bring back the world objects of one element in one area. Grass is
        /// absent on purpose: it is rendered by the DOTS GrassSystem and only rebuilt
        /// through the world-load replay, never live.</summary>
        private static int RestoreObjects(Element element, Area area)
        {
            int count = 0;
            try
            {
                switch (element)
                {
                    case Element.Flowers:
                        if (GameManager.allFlowers != null)
                            foreach (Flower flower in GameManager.allFlowers)
                                if (flower != null && flower.myArea == area) { flower.ResetState(); count++; }
                        break;
                    case Element.Molehills:
                        if (GameManager.allMolehills != null)
                            foreach (Molehill molehill in GameManager.allMolehills)
                                if (molehill != null && molehill.myArea == area) { molehill.ResetState(); count++; }
                        break;
                    case Element.Trash:
                        if (GameManager.allTroepjes != null)
                            foreach (Troepje troepje in GameManager.allTroepjes)
                                if (troepje != null && troepje.myArea == area) { troepje.ResetState(); count++; }
                        break;
                    case Element.Hedge:
                        if (GameManager.allTrimballs != null)
                            foreach (TrimBall trimBall in GameManager.allTrimballs)
                                if (trimBall != null && trimBall.myArea == area) { trimBall.ResetState(); count++; }
                        break;
                }
            }
            catch (Exception e)
            {
                Plugin.Log?.LogWarning("[Grunnchipelago] Regrow trap restore failed: " + e.Message);
            }
            return count;
        }

        private static PolygonTommie BoundsOf(int area)
        {
            var gm = GameManager.instance;
            switch (area)
            {
                case 0: return gm.startGardenAreaBounds;
                case 1: return gm.churchAreaBounds;
                case 2: return gm.parkAreaBounds;
                default: return null;
            }
        }

        private static int CounterOf(SaveManager.AreaProgress ap, Element e)
        {
            switch (e)
            {
                case Element.Grass: return ap.grassCutCur;
                case Element.Flowers: return ap.flowerCur;
                case Element.Hedge: return ap.trimBallCur;
                case Element.Trash: return ap.troepjeCur;
                case Element.Molehills: return ap.molehillCur;
                default: return 0;
            }
        }

        private static void SetCounter(SaveManager.AreaProgress ap, Element e, int value)
        {
            switch (e)
            {
                case Element.Grass: ap.grassCutCur = value; break;
                case Element.Flowers: ap.flowerCur = value; break;
                case Element.Hedge: ap.trimBallCur = value; break;
                case Element.Trash: ap.troepjeCur = value; break;
                case Element.Molehills: ap.molehillCur = value; break;
            }
        }

        private static List<SerializableVector3> ListOf(SaveManager.ProgressData pd, Element e)
        {
            switch (e)
            {
                case Element.Grass: return pd.grassCutPosition;        // SaveManager.cs:292
                case Element.Flowers: return pd.flowerWaterPosition;   // SaveManager.cs:300
                case Element.Hedge: return pd.hedgeTrimPosition;       // SaveManager.cs:296
                case Element.Trash: return pd.troepjeClearPosition;    // SaveManager.cs:310
                case Element.Molehills: return pd.moleHillRemovePosition;  // SaveManager.cs:304
                default: return null;
            }
        }
    }
}
