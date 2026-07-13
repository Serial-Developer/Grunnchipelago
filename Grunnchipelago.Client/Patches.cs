using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Grunnchipelago.Client
{
    // Each patch cites the decompiled method it depends on. When not connected, every
    // prefix returns true (100 % vanilla) and every postfix is a no-op.

    /// <summary>GameManager.ObtainKeyItem(KeyItem, bool) - GameManager.cs:3322 (key items).
    /// NOTE: this prefix runs BEFORE the vanilla early-out (`if ObtainedKeyItem return`,
    /// GameManager.cs:3324), so the check is sent even when the item is already owned.</summary>
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.ObtainKeyItem))]
    public static class ObtainKeyItemPatch
    {
        private static bool Prefix(KeyItem _keyItem)
        {
            ApClient ap = Plugin.Ap;
            if (ap == null || !ap.Connected) return true;
            if (ApClient.GrantGuard) return true;
            if (ItemPickupTriggerPatch.SpecialPickupActive) return true;   // bone gift: vanilla grant, no check
            ap.SendKeyItemCheck(_keyItem);
            return false;
        }
    }

    // ---------- Pickup/shop visibility = CHECK STATE, never possession ----------
    // Playtest round 1 bug: an item received from the multiworld marked its key item as
    // owned, and the game hides every pickup/shop article whose item is owned
    // (ItemPickup.ResetState / CheckIfAlreadyObtainedThisItem / KeyItemObtained hiders)
    // -> the location's check became unreachable (Medal/OfficeKey at the merchant).
    // Randomizer semantics: a location stays visible until ITS CHECK is sent.

    /// <summary>Shared visibility recomputation for key-item pickups.</summary>
    internal static class PickupVisibility
    {
        private static readonly AccessTools.FieldRef<ItemPickup, ItemPickupState> StartStateRef =
            AccessTools.FieldRefAccess<ItemPickup, ItemPickupState>("startState");

        private static System.Reflection.MethodInfo setState;

        public static bool AppliesTo(ItemPickup pickup)
        {
            if (pickup == null || pickup.isGulden || pickup.isRepeatablePickup) return false;
            if (pickup.gameObject.name.StartsWith("grunnchipelago", System.StringComparison.Ordinal))
                return false;   // the bone gift follows vanilla possession rules
            return pickup.keyItemObtain != null && pickup.keyItemObtain.Count > 0;
        }

        public static void Recompute(ItemPickup pickup, ApClient ap)
        {
            ItemPickupState state = StartStateRef(pickup);
            if (pickup.hideInDemo && SaveManager.demo) state = ItemPickupState.Hide;
            if (ap.KeyItemCheckSent(pickup.keyItemObtain[0])) state = ItemPickupState.Hide;
            if (setState == null) setState = AccessTools.Method(typeof(ItemPickup), "SetState");
            setState.Invoke(pickup, new object[] { state });
        }
    }

    /// <summary>ItemPickup.CheckIfAlreadyObtainedThisItem (private, ItemPickup.cs:120) -
    /// runs on every GrabbedItem event and hides owned-item pickups. Replaced by the
    /// check-state recomputation while connected.</summary>
    [HarmonyPatch(typeof(ItemPickup), "CheckIfAlreadyObtainedThisItem")]
    public static class CheckIfAlreadyObtainedPatch
    {
        private static bool Prefix(ItemPickup __instance)
        {
            ApClient ap = Plugin.Ap;
            if (ap == null || !ap.Connected) return true;
            if (!PickupVisibility.AppliesTo(__instance)) return true;
            PickupVisibility.Recompute(__instance, ap);
            return false;
        }
    }

    /// <summary>ItemPickup.ResetState (ItemPickup.cs:74) - the world-reset visibility
    /// decision ("owned -> Hide"). Overridden after the vanilla run while connected.</summary>
    [HarmonyPatch(typeof(ItemPickup), nameof(ItemPickup.ResetState))]
    public static class ItemPickupResetStatePatch
    {
        private static void Postfix(ItemPickup __instance)
        {
            ApClient ap = Plugin.Ap;
            if (ap == null || !ap.Connected) return;
            if (!PickupVisibility.AppliesTo(__instance)) return;
            PickupVisibility.Recompute(__instance, ap);
        }
    }

    /// <summary>ContentHider.CheckCondition (private, ContentHider.cs:416). Hiders whose
    /// target object holds an ItemPickup (shop articles like officeKey0_shop, world
    /// spawns like hammer0_car) hide it on KeyItemObtained: switch those to check-state.
    /// World-mechanic hiders (maze paths on Compass/TallIdol, portals...) target no
    /// pickup and keep vanilla possession semantics.</summary>
    [HarmonyPatch(typeof(ContentHider), "CheckCondition")]
    public static class ContentHiderConditionPatch
    {
        // instanceID -> does objectRef contain an ItemPickup (cached: polled every frame)
        private static readonly Dictionary<int, bool> hidesPickup = new Dictionary<int, bool>();

        private static void Postfix(ContentHider __instance, HideCondition _c, ref bool __result)
        {
            if (_c != HideCondition.KeyItemObtained && _c != HideCondition.KeyItemNotObtained) return;
            ApClient ap = Plugin.Ap;
            if (ap == null || !ap.Connected) return;

            int key = __instance.GetInstanceID();
            if (!hidesPickup.TryGetValue(key, out bool isPickupHider))
            {
                isPickupHider = __instance.objectRef != null
                    && __instance.objectRef.GetComponentInChildren<ItemPickup>(true) != null;
                hidesPickup[key] = isPickupHider;
            }
            if (!isPickupHider) return;

            bool sent = ap.KeyItemCheckSent(__instance.keyItemRef);
            __result = _c == HideCondition.KeyItemObtained ? sent : !sent;
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

    // ---------- Pickups (gulden, bone gift, owned-item bypass, popup fix) ----------

    /// <summary>ItemPickup.Trigger(bool) - ItemPickup.cs:143. Handles, in order:
    /// - the special "bone gift" pickup (spawned by BoneGift): full vanilla grant, no check;
    /// - placed gulden: verbose diagnostic popup, and under coinsanity a check with the
    ///   money add suppressed (AddGuldenPatch);
    /// - key items ALREADY owned via AP: vanilla refuses them ("ItemDontNeed",
    ///   ItemPickup.cs:149-153) which would strand the location's check forever - we send
    ///   the check and hide the pickup instead;
    /// - normal intercepted pickups: suppress the misleading vanilla "obtained X" popup
    ///   (the location's real content is announced by the scout, design section 10).</summary>
    [HarmonyPatch(typeof(ItemPickup), nameof(ItemPickup.Trigger))]
    public static class ItemPickupTriggerPatch
    {
        /// <summary>True while the bone-gift pickup runs its vanilla Trigger.</summary>
        public static bool SpecialPickupActive;

        /// <summary>True while an intercepted pickup runs (read by AddPopupPatch).</summary>
        public static bool SuppressVanillaPopup;

        private static bool Prefix(ItemPickup __instance, bool _throughLoadOperation)
        {
            ApClient ap = Plugin.Ap;
            if (ap == null || !ap.Connected || _throughLoadOperation) return true;

            if (__instance.gameObject.name.StartsWith("grunnchipelago", System.StringComparison.Ordinal))
            {
                SpecialPickupActive = true;   // reset by Postfix
                return true;
            }

            if (__instance.isGulden)
            {
                int index = ScenePaths.GuldenIndex(__instance);
                if (index >= 0)
                {
                    if (ApClient.Verbose)
                    {
                        // FLAG diagnostic [J 2026-07-13]: identify unknown gulden spots in-game.
                        Plugin.Log?.LogInfo($"[Grunnchipelago] Gulden pickup: {GameIds.GuldenLocationNames[index]}");
                        ap.QueuePopup(GameIds.GuldenLocationNames[index]);
                    }
                    if (ap.Coinsanity)
                    {
                        ap.SendGuldenCheck(index);
                        ApClient.GuldenPickupGuard = true;   // suppress the money gain
                    }
                }
                return true;
            }

            var items = __instance.keyItemObtain;
            if (items != null && items.Count > 0 && !__instance.isRepeatablePickup)
            {
                KeyItem first = items[0];
                if (SaveManager.ObtainedKeyItem(first))
                {
                    if (ap.KeyItemCheckPending(first))
                    {
                        ap.SendKeyItemCheck(first);
                        GameManager.GrabbedItem();   // hides this owned-item pickup
                        return false;                 // skip the misleading vanilla refusal
                    }
                    return true;   // check already sent: the vanilla refusal is accurate
                }
                SuppressVanillaPopup = true;   // reset by Postfix
            }
            return true;
        }

        private static void Postfix()
        {
            SpecialPickupActive = false;
            SuppressVanillaPopup = false;
        }
    }

    /// <summary>UIManager.AddPopup(string) - UIManager.cs:4832. Drops the vanilla
    /// "obtained &lt;seen item&gt;" popup while an intercepted pickup runs; the real
    /// content is announced via the scout queue instead.</summary>
    [HarmonyPatch(typeof(UIManager), nameof(UIManager.AddPopup))]
    public static class AddPopupPatch
    {
        private static bool Prefix() => !ItemPickupTriggerPatch.SuppressVanillaPopup;
    }

    /// <summary>GameManager.TriggerItemObtainPopup(KeyItem) - GameManager.cs:6023. NPC
    /// gives (TradeEggball, magpie...) announce the SEEN item too; suppress when the grant
    /// is intercepted. Our own grants run under GrantGuard and keep their popup.</summary>
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.TriggerItemObtainPopup))]
    public static class TriggerItemObtainPopupPatch
    {
        private static bool Prefix()
        {
            ApClient ap = Plugin.Ap;
            if (ap == null || !ap.Connected) return true;
            return ApClient.GrantGuard || ItemPickupTriggerPatch.SpecialPickupActive;
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

    /// <summary>design section 10, feature #3 - the AP "Bone" item is never injected into
    /// the inventory. Instead a world pickup (clone of a skeleton-bone ItemPickup) spawns
    /// near the start (next to the Bridge Key spot, outside the bus). Taking it grants the
    /// vanilla Bone WITHOUT sending a check (ItemPickupTriggerPatch.SpecialPickupActive),
    /// so the player only picks it up when needed and the Dog ending stays reachable.
    /// Across runs the clone follows vanilla rules: ResetWorld -> ResetState shows it
    /// again whenever Bone is not currently held.</summary>
    internal static class BoneGift
    {
        private static GameObject instance;

        public static void EnsureSpawned(ApClient ap)
        {
            if (instance != null || !ap.BoneOwnedFromAp) return;
            if (GameManager.instance == null || GameManager.allItemPickups == null
                || GameManager.allItemPickups.Count < 50) return;   // world not loaded yet

            ItemPickup template = null;
            Vector3 anchor = new Vector3(-33.4f, 10.15f, -64f);   // bridgeKey0 pos (dump)
            foreach (ItemPickup pickup in GameManager.allItemPickups)
            {
                if (pickup == null) continue;
                if (template == null && !pickup.isGulden && !pickup.isTool
                    && pickup.keyItemObtain != null && pickup.keyItemObtain.Count > 0
                    && pickup.keyItemObtain[0] == KeyItem.Bone)
                    template = pickup;
                if (pickup.gameObject.name == "bridgeKey0")
                    anchor = pickup.transform.position;
            }
            if (template == null)
            {
                Plugin.Log?.LogWarning("[Grunnchipelago] BoneGift: no Bone pickup template found.");
                return;
            }

            Vector3 position = anchor + new Vector3(1.5f, 0f, 1.5f);
            instance = Object.Instantiate(template.gameObject, position, Quaternion.identity);
            instance.name = "grunnchipelago_boneGift";
            ItemPickup clone = instance.GetComponent<ItemPickup>();
            clone.startState = ItemPickupState.Show;   // skeleton bones start hidden
            instance.SetActive(true);
            // The clone initialises itself (UpdateNormal -> Init): registers in
            // allItemPickups, subscribes to GrabbedItemAction, then ResetState -> Show
            // (Bone not currently held) or Hide (held this run).
            Plugin.Log?.LogInfo($"[Grunnchipelago] BoneGift spawned at {position}.");
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
