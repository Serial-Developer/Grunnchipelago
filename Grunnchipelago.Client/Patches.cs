using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Grunnchipelago.Client
{
    // Each patch cites the decompiled method it depends on. When not connected, every
    // prefix returns true (100 % vanilla) and every postfix is a no-op.

    /// <summary>GameManager.ObtainKeyItem(KeyItem, bool) - GameManager.cs:3322 (key items).</summary>
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.ObtainKeyItem))]
    public static class ObtainKeyItemPatch
    {
        private static bool Prefix(KeyItem _keyItem)
        {
            ApClient ap = Plugin.Ap;
            if (ap == null || !ap.Connected) return true;
            if (ApClient.GrantGuard) return true;
            ap.SendKeyItemCheck(_keyItem);
            return false;
        }
    }

    /// <summary>PlayerManager.AddTool(Item) - PlayerManager.cs:213. Tools have both an
    /// Item and a KeyItem; both hooks send the same "Obtain X" check, deduplicated by
    /// ApClient's sent-locations cache.</summary>
    [HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.AddTool))]
    public static class AddToolPatch
    {
        private static bool Prefix(Item _item)
        {
            ApClient ap = Plugin.Ap;
            if (ap == null || !ap.Connected) return true;
            if (ApClient.GrantGuard) return true;
            ap.SendToolCheck(_item);
            return false;
        }
    }

    /// <summary>GameManager.TriggerEnding(EndingType) - GameManager.cs:2472. Ending checks
    /// + goal detection (postfix so the ending is already registered).</summary>
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.TriggerEnding))]
    public static class TriggerEndingPatch
    {
        private static void Postfix(EndingType _endingType)
        {
            Plugin.Ap?.OnEndingTriggered(_endingType);
        }
    }

    /// <summary>Ghost.Touch() - Ghost.cs:222. Ghost checks matched by frozen scene path
    /// (GameIds.GhostIndexByPath) - immune to ghosts drifting from their spawn point.</summary>
    [HarmonyPatch(typeof(Ghost), nameof(Ghost.Touch))]
    public static class GhostTouchPatch
    {
        private static void Postfix(Ghost __instance)
        {
            ApClient ap = Plugin.Ap;
            if (ap == null || !ap.Connected) return;
            int index = ScenePaths.GhostIndex(__instance);
            if (index >= 0) ap.SendGhostCheck(index);
        }
    }

    /// <summary>SaveManager.AddPolaroidCollected(PolaroidType, bool) - SaveManager.cs:2650.
    /// Polaroid checks; ending polaroids are filtered out by the client.</summary>
    [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.AddPolaroidCollected))]
    public static class AddPolaroidCollectedPatch
    {
        private static void Postfix(PolaroidType _type)
        {
            Plugin.Ap?.SendPolaroidCheck(_type);
        }
    }

    /// <summary>GameManager.TriggerNewRun() - GameManager.cs:3758. Re-inject the received
    /// inventory after the run reset; optionally persist comfort shortcuts (design section 5).</summary>
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.TriggerNewRun))]
    public static class TriggerNewRunPatch
    {
        private static void Prefix()
        {
            ApClient ap = Plugin.Ap;
            if (ap == null || !ap.Connected) return;
            if (ap.PersistentShortcuts) ShortcutCache.Capture();   // before ResetRunProgress
        }

        private static void Postfix()
        {
            ApClient ap = Plugin.Ap;
            if (ap == null || !ap.Connected) return;
            if (ap.PersistentShortcuts) ShortcutCache.Restore();   // after the reset
            ap.ReinjectInventory();
        }
    }

    // ---------- Coinsanity ----------

    /// <summary>ItemPickup.Trigger(bool) - ItemPickup.cs:143. Under coinsanity, a placed
    /// gulden becomes a check (frozen path table); the money add is suppressed via
    /// AddGuldenPatch.</summary>
    [HarmonyPatch(typeof(ItemPickup), nameof(ItemPickup.Trigger))]
    public static class ItemPickupGuldenPatch
    {
        private static bool Prefix(ItemPickup __instance, bool _throughLoadOperation)
        {
            ApClient ap = Plugin.Ap;
            if (ap == null || !ap.Connected || !ap.Coinsanity) return true;
            if (_throughLoadOperation || !__instance.isGulden) return true;
            int index = ScenePaths.GuldenIndex(__instance);
            if (index >= 0) ap.SendGuldenCheck(index);
            ApClient.GuldenPickupGuard = true;   // suppress the money AddGulden is about to do
            return true;                          // let the pickup consume itself as usual
        }
    }

    /// <summary>GameManager.AddGulden(int, bool) - GameManager.cs:4108. Suppress the money
    /// gain for a coinsanity coin that was just turned into a check.</summary>
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.AddGulden))]
    public static class AddGuldenPatch
    {
        private static bool Prefix()
        {
            if (ApClient.GuldenPickupGuard)
            {
                ApClient.GuldenPickupGuard = false;
                return false;
            }
            return true;
        }
    }

    // ---------- Buffs & traps ----------

    /// <summary>ItemObject.CreateGrassCutter(float _scaleFactor, int _grassCutterIndex) -
    /// ItemObject.cs:135 (private). The FLOAT is the cutter radius (prefab spawn scale);
    /// the INT is a prefab variant index, not a rate. Cutter Range Boost scales the radius.</summary>
    [HarmonyPatch(typeof(ItemObject), "CreateGrassCutter")]
    public static class CreateGrassCutterPatch
    {
        private static void Prefix(ref float _scaleFactor)
        {
            ApClient ap = Plugin.Ap;
            if (ap == null || !ap.Connected) return;
            _scaleFactor *= Effects.CutterScaleMultiplier;
        }
    }

    /// <summary>PlayerArm.PerformAnimation (private) - PlayerArm.cs:533 advances
    /// animationCounter by cachedDeltaTime. Cutting Rate Boost accelerates the swing
    /// animation by adding extra time per frame (faster swings = faster cutting).</summary>
    [HarmonyPatch(typeof(PlayerArm), "PerformAnimation")]
    public static class PerformAnimationPatch
    {
        private static readonly AccessTools.FieldRef<PlayerArm, float> CounterRef =
            AccessTools.FieldRefAccess<PlayerArm, float>("animationCounter");

        private static void Postfix(PlayerArm __instance)
        {
            ApClient ap = Plugin.Ap;
            if (ap == null || !ap.Connected) return;
            float extra = Effects.SwingSpeedMultiplier - 1f;
            if (extra > 0f && GameManager.instance != null)
                CounterRef(__instance) += GameManager.instance.cachedDeltaTime * extra;
        }
    }

    /// <summary>MouseLookNew.UpdateNormal - MouseLookNew.cs:236 reads InputManager.lookDirection
    /// into the public hInputR/vInputR. Inverted Controls Trap negates them (one-frame lag,
    /// imperceptible, applies to both player and camera looks).</summary>
    [HarmonyPatch(typeof(MouseLookNew), nameof(MouseLookNew.UpdateNormal))]
    public static class MouseLookInvertPatch
    {
        private static void Postfix(MouseLookNew __instance)
        {
            if (!Effects.InvertedControlsActive) return;
            __instance.hInputR = -__instance.hInputR;
            __instance.vInputR = -__instance.vInputR;
        }
    }

    /// <summary>InputManager.OnMove(CallbackContext) - InputManager.cs:370 writes the static
    /// moveDirection. Inverted Controls Trap flips movement too.</summary>
    [HarmonyPatch(typeof(InputManager), nameof(InputManager.OnMove))]
    public static class MoveInvertPatch
    {
        private static void Postfix()
        {
            if (!Effects.InvertedControlsActive) return;
            InputManager.moveDirection = -InputManager.moveDirection;
        }
    }

    // ---------- Helpers ----------

    /// <summary>GameManager.HandleNightmare (private, GameManager.cs:1454) zeroes the
    /// nightmare blend factor whenever CanShowNightmare() is false (enteredStartHouse
    /// resets each run), which hid our DeathLink jumpscare image. While the jumpscare is
    /// active we force the factor to the vanilla Show value (0.2, GameManager.cs:1469)
    /// right after the vanilla write: the NightmareCamera renders the shot into
    /// _NightmareMap and the PostFX stack blends it (NightmareCamera.cs:56,
    /// PostFXStack.cs:476). We deliberately do NOT touch enteredStartHouse - it gates
    /// world content (hedge-maze portals).</summary>
    [HarmonyPatch(typeof(GameManager), "HandleNightmare")]
    public static class HandleNightmarePatch
    {
        private static void Postfix()
        {
            if (Plugin.JumpscareActive)
                GameManager.nightmareFactorCur = 0.2f;
        }
    }

    /// <summary>Cosmetic nightmare jumpscare used when a DeathLink is received (the actual
    /// "death" is the run reset done by Plugin.Update after this displays). Shows one of
    /// the vanilla nightmare shots (GameManager.cs:933 display path) in front of the
    /// NightmareCamera; the blend factor is forced by HandleNightmarePatch while active.</summary>
    internal static class NightmareJumpscare
    {
        public static bool Show()
        {
            NightmareShots shots = GameManager.nightmareShots;
            if (shots == null || shots.shotObjects == null || shots.shotObjects.Length == 0)
                return false;   // world not ready - skip the cosmetic part

            shots.HideAll();
            int pick = Random.Range(0, shots.shotObjects.Length);
            for (int i = 0; i < shots.shotObjects.Length; i++)
                shots.shotObjects[i].SetActive(i == pick);

            AudioManager.instance.PlaySoundGlobal(
                BasicFunctions.PickRandomAudioClipFromArray(AudioManager.instance.showNightmareShot),
                1.6f, 2.1f, 0.5f, 0.525f);
            return true;
        }

        public static void Hide()
        {
            GameManager.nightmareShots?.HideAll();
        }
    }

    /// <summary>Scene-path helpers matching the Dumper's GetPath, used to identify ghost
    /// and gulden objects against the frozen tables in GameIds.</summary>
    internal static class ScenePaths
    {
        public static string Of(Transform t)
        {
            var parts = new List<string>();
            while (t != null)
            {
                parts.Insert(0, t.name);
                t = t.parent;
            }
            return string.Join("/", parts);
        }

        public static int GhostIndex(Ghost ghost)
        {
            string path = Of(ghost.transform);
            if (GameIds.GhostIndexByPath.TryGetValue(path, out int index)) return index;
            Plugin.Log?.LogWarning($"[Grunnchipelago] Unknown ghost path '{path}' - check not sent.");
            return -1;
        }

        public static int GuldenIndex(ItemPickup pickup)
        {
            string path = Of(pickup.transform);
            if (GameIds.GuldenIndexByPath.TryGetValue(path, out int index)) return index;
            Plugin.Log?.LogWarning($"[Grunnchipelago] Unknown gulden path '{path}' - check not sent.");
            return -1;
        }
    }

    /// <summary>Monotonic cache of the comfort-shortcut flags, so persistent_shortcuts can
    /// restore them after each ResetRunProgress. Fields: SaveManager.cs progressData.</summary>
    internal static class ShortcutCache
    {
        private static bool bijkeuken, intratuin, created, hooiGarden, maze;
        private static readonly List<Lock> locks = new List<Lock>();

        public static void Capture()
        {
            var pd = SaveManager.progressDataCheck;
            if (pd == null) return;
            bijkeuken |= pd.unlockedBijkeukenShortcut;
            intratuin |= pd.unlockedIntratuin;
            created |= pd.createdShortcut;
            hooiGarden |= pd.parkUnlockedHooibaalGarden;
            maze |= pd.parkUnlockedMaze;
            if (pd.locksUnlocked != null)
                foreach (Lock l in pd.locksUnlocked)
                    if (!locks.Contains(l)) locks.Add(l);
        }

        public static void Restore()
        {
            var pd = SaveManager.progressDataCheck;
            if (pd == null) return;
            pd.unlockedBijkeukenShortcut = bijkeuken;
            pd.unlockedIntratuin = intratuin;
            pd.createdShortcut = created;
            pd.parkUnlockedHooibaalGarden = hooiGarden;
            pd.parkUnlockedMaze = maze;
            if (locks.Count > 0) pd.locksUnlocked = new List<Lock>(locks);
        }
    }
}
