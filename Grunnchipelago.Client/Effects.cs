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
    /// Timed traps (Speed / Size / Inverted Controls) last 2 in-game hours
    /// (TimeController.currentHour) and expire on day change / run reset.
    /// The other five are one-shot: three ZONE RESETS (see ApplyZoneReset), the Night Trap
    /// (TimeFeatures.JumpToTrapNight) and the Sacred Flower Trap (ApplySacredFlowerTrap).
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

            // Each world-altering trap is matched on BOTH its current name and its
            // pre-2026-07-27 name: the ids never changed, so a seed rolled before the
            // rename still sends the old string for the same effect (GameIds).
            switch (name)
            {
                case GameIds.TrapSpeed: speedTrap = timed; break;
                case GameIds.TrapSize: sizeTrap = timed; break;
                case GameIds.TrapInvertedControls: invertedTrap = timed; break;

                case GameIds.TrapGardenReset:
                case GameIds.TrapLegacyRegrowHedge:
                    ApplyZoneReset(Area.StartGarden, "Garden Reset Trap");
                    break;
                case GameIds.TrapChurchReset:
                case GameIds.TrapLegacyRegrowMolehills:
                    ApplyZoneReset(Area.Church, "Church Reset Trap");
                    break;
                case GameIds.TrapParkReset:
                case GameIds.TrapLegacyRegrowGrass:
                    ApplyZoneReset(Area.Park, "Park Reset Trap");
                    break;
                case GameIds.TrapNight:
                case GameIds.TrapLegacyReturnTrash:
                    TimeFeatures.JumpToTrapNight();
                    break;
                case GameIds.TrapSacredFlower:
                case GameIds.TrapLegacyRewaterFlowers:
                    ApplySacredFlowerTrap();
                    break;
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

        // --- Zone reset traps -------------------------------------------------------------
        // REDESIGNED 2026-07-27 (demande Jonath). The four "regrow one element in a random
        // zone" traps became three FULL ZONE RESETS, one per maintainable zone (Garden /
        // Church / Park): the zone drops back to 0 % and every maintenance job has to be
        // redone - grass, molehills, hedge, flowers to water and litter. Grass is now
        // included; see ResetGrassInArea for how it is rebuilt live.
        //
        // NOT reset on purpose: progressDataCheck.cutAllGrassInStartGardenArea, the flag
        // that gates the one-off gulden bonus for finishing the grass (GameManager.cs:3131).
        // Clearing it would pay the bonus again on every re-clean - free money, and under
        // coinsanity gulden is progression currency.

        private enum Element { Grass, Flowers, Hedge, Trash, Molehills }

        /// <summary>All five maintenance elements. Jonath's spec lists them per zone
        /// (garden: the five; church: grass/molehills/flowers; park: grass/molehills/
        /// flowers/trash), but an element a zone does not have has a counter of 0 already,
        /// so resetting all five everywhere is the SAME thing - and it is the only way to
        /// guarantee the "back to 0 %" part, since GetAreaCompletedPercentage sums every
        /// counter (SaveManager.cs:2381).</summary>
        private static readonly Element[] AllElements =
            { Element.Grass, Element.Molehills, Element.Hedge, Element.Flowers, Element.Trash };

        /// <summary>Reset one whole zone: every element back to its initial state, counters
        /// to 0, so the zone reads 0 % and must be cleaned all over again.</summary>
        private static void ApplyZoneReset(Area area, string label)
        {
            var pd = SaveManager.progressDataCheck;
            if (pd?.areaProgress == null || GameManager.instance == null) return;

            int index = (int)area;
            if (index < 0 || index >= pd.areaProgress.Length) return;

            // A zone that has EVER been completed is hard-coded to 100 % by
            // GetAreaCompletedPercentage (SaveManager.cs:2394-2416) whatever the counters
            // say, so the reset has to clear that flag too - otherwise it is purely
            // cosmetic. Consequence to know: the 100 % portal of that zone (Picnic /
            // Plage / Boulangerie, ContentHider condition Maintained<Zone>Area) closes
            // again until the zone is re-maintained. Re-cleaning re-opens it
            // (GameManager.cs:6110-6141), so nothing is permanently lost.
            switch (area)
            {
                case Area.StartGarden: pd.maintainedGardenArea = false; break;
                case Area.Church: pd.maintainedChurchArea = false; break;
                case Area.Park: pd.maintainedParkArea = false; break;
            }

            int restored = 0;
            bool grass = false;
            foreach (Element element in AllElements)
            {
                if (element == Element.Grass)
                {
                    grass = ResetGrassInArea(area);
                    continue;
                }
                SetCounter(pd.areaProgress[index], element, 0);

                // Drop the stored positions that fall inside that zone's macro bounds
                // (PolygonTommie fields on GameManager, as used by the scene dumper).
                List<SerializableVector3> list = ListOf(pd, element);
                PolygonTommie bounds = BoundsOf(index);
                if (list != null && bounds != null)
                    list.RemoveAll(v => bounds.ContainsPoint(v.UnityVector));

                // Restore the objects IN WORLD right away (session 2, retour Jonath: the
                // counter dropped but nothing grew back, so the zone could never reach
                // 100 % again that run). Each ResetState re-reads the (now cleared) save
                // positions via its own CheckForLoadOperation, exactly like ResetWorld
                // does on a run reset - GameManager.cs:3918-4023.
                restored += RestoreObjects(element, area);
            }

            SaveManager.Save(SaveManager.curSlotIndex);
            try { UIManager.instance?.UpdateAreaPercentageString(); } catch (Exception) { }
            Plugin.Log?.LogInfo(
                $"[Grunnchipelago] {label}: zone {area} remise a zero ({restored} objets restaures"
                + (grass ? ", herbe reconstruite" : "") + ").");
        }

        /// <summary>Regrow the grass of ONE area, live.
        ///
        /// The grass is DOTS-rendered, so it cannot be restored object by object like the
        /// rest - but the game rebuilds it wholesale in <c>GameManager.ResetWorld</c>
        /// (GameManager.cs:4064): ClearEntities() destroys every entity, GrassManager.Reset()
        /// recreates the whole grass at its initial grow level, and CornManager.Reset()
        /// puts back the corn that ClearEntities also wiped. Then the CUT state is replayed
        /// from the save: PerformLoadOperations (GameManager.cs:874) reloads
        /// grassCutPosition/Radius and GrassSystem re-cuts those spots (GrassSystem.cs:483-572)
        /// WITHOUT re-saving them or playing any sound.
        ///
        /// So: drop this area's cut positions from the save, zero the grass counter of ALL
        /// three areas (the replay re-increments them from scratch - GrassSystem calls
        /// CutGrass with _throughLoadOperation, GameManager.cs:3098), rebuild, replay.
        /// The other zones come back exactly as they were; this one is uncut again.</summary>
        private static bool ResetGrassInArea(Area area)
        {
            var pd = SaveManager.progressDataCheck;
            if (pd?.grassCutPosition == null || pd.grassCutRadius == null) return false;
            if (GrassManager.instance == null) return false;

            try
            {
                // The two lists are PARALLEL (SaveManager.SaveCutPosition, SaveManager.cs:2129):
                // an entry must be dropped from both at the same index.
                PolygonTommie bounds = BoundsOf((int)area);
                if (bounds != null)
                {
                    for (int i = pd.grassCutPosition.Count - 1; i >= 0; i--)
                    {
                        if (!bounds.ContainsPoint(pd.grassCutPosition[i].UnityVector)) continue;
                        pd.grassCutPosition.RemoveAt(i);
                        if (i < pd.grassCutRadius.Count) pd.grassCutRadius.RemoveAt(i);
                    }
                }

                for (int i = 0; i < 3 && i < pd.areaProgress.Length; i++)
                    pd.areaProgress[i].grassCutCur = 0;
                SaveManager.Save(SaveManager.curSlotIndex);

                GrassManager.instance.ClearEntities();
                GrassManager.instance.Reset();
                CornManager.instance?.Reset();

                // Re-arm the replay. PerformLoadOperations only flips loadOperationCutGrass
                // to true when the list is EMPTY, so drive it explicitly.
                GameManager.performedLoadOperations = false;
                GameManager.PerformLoadOperations();
                GameManager.loadOperationCutGrass = pd.grassCutPosition.Count == 0;
                return true;
            }
            catch (Exception e)
            {
                Plugin.Log?.LogWarning("[Grunnchipelago] Reset de l'herbe echoue : " + e.Message);
                return false;
            }
        }

        /// <summary>Sacred Flower Trap: cut 4 graveyard flowers, sound included.
        ///
        /// Calling the real <c>Flower.Cut()</c> (Flower.cs:580) does everything vanilla does:
        /// graveyardFlowerCutCur++, the graveyardFlowerCut sound, and the thresholds
        /// (>= 4 warning, >= 5 ActivateSpookyWorld -> the SacredFlowers ending). That is
        /// exactly the behaviour Jonath asked for: a player who had already cut one flower
        /// reaches 5 and gets the ending immediately; a player at 0 lands on 4 and triggers
        /// it the moment they cut a single flower themselves.
        /// If fewer than 4 uncut graveyard flowers are left in the world, the counter is
        /// topped up so the trap always "costs" 4.</summary>
        private static void ApplySacredFlowerTrap()
        {
            var pd = SaveManager.progressDataCheck;
            if (pd == null) return;
            const int Flowers = 4;

            int cut = 0;
            if (GameManager.allFlowers != null)
            {
                foreach (Flower flower in GameManager.allFlowers)
                {
                    if (cut >= Flowers) break;
                    if (flower == null || !flower.isGraveyardFlower) continue;
                    if (!flower.canBeCut || flower.contentObject == null
                        || !flower.contentObject.activeInHierarchy) continue;
                    flower.Cut();
                    cut++;
                }
            }

            // Not enough flowers standing: charge the rest to the counter, mirroring what
            // Flower.Cut would have done (counter + thresholds), and play the sound once.
            for (int i = cut; i < Flowers; i++)
            {
                pd.graveyardFlowerCutCur++;
                bool inAlteredWorld = pd.inRedWorld || pd.inGoodEnding || pd.inSpookyWorld;
                if (inAlteredWorld) continue;
                if (pd.graveyardFlowerCutCur >= 4) GameManager.TriggerGraveyardWarning1();
                if (pd.graveyardFlowerCutCur >= 5) GameManager.ActivateSpookyWorld();
            }
            if (cut < Flowers) PlayGraveyardFlowerCutSound();

            SaveManager.Save(SaveManager.curSlotIndex);
            Plugin.Log?.LogInfo(
                $"[Grunnchipelago] Sacred Flower Trap: {cut} fleurs coupees en monde, "
                + $"compteur = {pd.graveyardFlowerCutCur}.");
        }

        private static void PlayGraveyardFlowerCutSound()
        {
            try
            {
                Vector3 at = PlayerControllerNew.Transform != null
                    ? PlayerControllerNew.Transform.position : Vector3.zero;
                AudioManager.instance?.PlaySoundAtPosition(
                    at,
                    BasicFunctions.PickRandomAudioClipFromArray(AudioManager.instance.graveyardFlowerCut),
                    0.9f, 1.1f, 0.375f, 0.4f);
            }
            catch (Exception) { }
        }

        /// <summary>Bring back the world objects of one element in one area. Grass is
        /// absent on purpose: it is DOTS-rendered and goes through ResetGrassInArea.</summary>
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
