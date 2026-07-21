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

    /// <summary>UIManager.BuildStaticStrings (private, UIManager.cs:4101-4107) sets
    /// titleText to "&lt;i&gt;GRUNN&lt;/i&gt;" at load and on every language change.
    /// Session 2, 1.1 (iteration 2): while the mod is Enabled the title itself becomes
    /// GRUNNCHIPELAGO, font size scaled down (ratio of TMP preferred widths) so it
    /// fits the original title's width. The text BOTTOM stays exactly where the
    /// vanilla "GRUNN" bottom renders: both bottoms are MEASURED via textBounds on
    /// the original RectTransform (no invented offset) and the rect is shifted by
    /// the difference. Mod disabled = no patch = vanilla title.</summary>
    [HarmonyPatch(typeof(UIManager), "BuildStaticStrings")]
    public static class TitleTextPatch
    {
        private static float origFontSize = -1f;
        private static Vector2 origAnchoredPos;

        private static void Postfix(UIManager __instance)
        {
            var title = __instance.titleText;
            if (title == null) return;
            if (origFontSize < 0f)
            {
                origFontSize = title.fontSize;
                origAnchoredPos = title.rectTransform.anchoredPosition;
            }

            // Reference pass: vanilla text at the vanilla size on the vanilla rect
            // (BuildStaticStrings just set "<i>GRUNN</i>"). textBounds is in the
            // text object's local space - comparable between both passes.
            title.enableAutoSizing = false;
            title.fontSize = origFontSize;
            title.rectTransform.anchoredPosition = origAnchoredPos;
            title.ForceMeshUpdate(true, true);
            float vanillaBottom = title.textBounds.min.y;

            // Width ratio is font-size invariant; 0.98 margin against rounding.
            float baseWidth = title.GetPreferredValues("GRUNN").x;
            float newWidth = title.GetPreferredValues("GRUNNCHIPELAGO").x;
            if (baseWidth > 0f && newWidth > 0f)
                title.fontSize = origFontSize * (baseWidth / newWidth) * 0.98f;
            title.SetText("<i>GRUNNCHIPELAGO</i>");

            // Align our bottom onto the measured vanilla bottom.
            title.ForceMeshUpdate(true, true);
            float newBottom = title.textBounds.min.y;
            title.rectTransform.anchoredPosition =
                origAnchoredPos + new Vector2(0f, vanillaBottom - newBottom);
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
    /// target object holds an ItemPickup OF THE CONDITION'S OWN key item (shop articles
    /// like officeKey0_shop, world spawns like hammer0_car) hide it on KeyItemObtained:
    /// switch those to check-state. World-mechanic hiders keep vanilla possession
    /// semantics - session 2 iter 8: "contains ANY pickup" was too loose, the
    /// HedgeMaze_NotObtainedCompass container (Compass condition) nests unrelated
    /// pickups, and flipping it to check-state removed the no-compass maze - and its
    /// strange symbol - once "Obtain Compass" was sent, killing the HedgeMaze ending.</summary>
    [HarmonyPatch(typeof(ContentHider), "CheckCondition")]
    public static class ContentHiderConditionPatch
    {
        // instanceID -> does objectRef wrap a pickup of the hider's own key item
        private static readonly Dictionary<int, bool> hidesPickup = new Dictionary<int, bool>();
        // instanceID -> the polaroid this hider drives (session 2: same randomizer
        // semantics for polaroids, whose check would otherwise die on possession)
        private static readonly Dictionary<int, Polaroid> hidesPolaroid = new Dictionary<int, Polaroid>();
        // Hiders we have already announced (VerboseLogs): every flip is a deviation
        // from vanilla, so the list must stay short and auditable.
        private static readonly HashSet<int> logged = new HashSet<int>();

        private static void LogFlip(ContentHider hider, string kind)
        {
            if (!ApClient.Verbose || !logged.Add(hider.GetInstanceID())) return;
            string target = hider.objectRef != null ? hider.objectRef.name : "(null)";
            Plugin.Log?.LogInfo($"[Grunnchipelago] Hider bascule en etat-check ({kind}) : "
                                + $"cible '{target}', item {hider.keyItemRef}.");
        }

        private static void Postfix(ContentHider __instance, HideCondition _c, ref bool __result)
        {
            if (_c != HideCondition.KeyItemObtained && _c != HideCondition.KeyItemNotObtained) return;
            ApClient ap = Plugin.Ap;
            if (ap == null || !ap.Connected) return;

            int key = __instance.GetInstanceID();

            // ALL FOUR hedge-maze portals (2 entrances + 2 exits) route on POSSESSING
            // TallIdol. Vanilla: you get the idol by beating the tall man, so within a
            // run "have idol" == "cleared the maze", and it RESETS next run (key items
            // are per-run) - so next run you redo the puzzle (retour Jonath: "jusqu'a
            // la prochaine run"). We intercept the idol grant, so possession never
            // happens -> the exit never opened AND re-entry never routed to the solved
            // maze. Drive all four by destroyedTallMan (per-run, set when you beat the
            // tall man) instead of possession: same per-run semantics as vanilla, and
            // it resets each run. Persistent check-state would WRONGLY skip the puzzle
            // forever after the first solve. Matches on Contains("HedgeMaze") to catch
            // both the exits (portal_HedgeMaze*) and the entrances
            // (portal_StartGardenToHedgeMazeA/B).
            if (__instance.keyItemRef == KeyItem.TallIdol && __instance.objectRef != null
                && __instance.objectRef.name.IndexOf("HedgeMaze", System.StringComparison.Ordinal) >= 0)
            {
                bool cleared = SaveManager.progressDataCheck != null
                               && SaveManager.progressDataCheck.destroyedTallMan;
                __result = _c == HideCondition.KeyItemObtained ? cleared : !cleared;
                return;
            }

            // Polaroid hiders keyed on possession (polaroid_lighterMolehill0 hides once
            // the Lighter is owned): drive them from the POLAROID's own check instead,
            // or receiving that key item from the multiworld strands the polaroid check.
            //
            // CRITICAL (retour Jonath: empty out-of-bounds hedge maze): the polaroid
            // must be the hider's OWN target object, never a descendant.
            // GetComponentInChildren matched any polaroid buried in a big container -
            // HedgeMaze_NotObtainedCompass / _ObtainedCompass (Compass hiders wrapping
            // the whole maze, dump:19332/19799) each nest one, so collecting a maze
            // polaroid hid BOTH maze variants at once and dropped the player into the
            // raw non-euclidian space, next to other areas' portals.
            if (!hidesPolaroid.TryGetValue(key, out Polaroid polaroid))
            {
                polaroid = __instance.objectRef != null
                    ? __instance.objectRef.GetComponent<Polaroid>() : null;
                hidesPolaroid[key] = polaroid;
            }
            if (polaroid != null)
            {
                LogFlip(__instance, "polaroid " + polaroid.polaroidType);
                bool polaroidSent = ap.PolaroidCheckSent(polaroid.polaroidType);
                __result = _c == HideCondition.KeyItemObtained ? polaroidSent : !polaroidSent;
                return;
            }

            // The hider must TARGET that pickup, not merely contain it somewhere deep:
            // objectRef is the pickup's own object or its direct parent. Anything wider
            // is a zone container (the hedge-maze variants nest dozens of objects) and
            // must keep vanilla semantics - flipping one blanked the whole maze.
            if (!hidesPickup.TryGetValue(key, out bool isPickupHider))
            {
                isPickupHider = false;
                GameObject target = __instance.objectRef;
                if (target != null)
                    foreach (ItemPickup pickup in target.GetComponentsInChildren<ItemPickup>(true))
                    {
                        if (pickup == null || pickup.keyItemObtain == null
                            || pickup.keyItemObtain.Count == 0
                            || pickup.keyItemObtain[0] != __instance.keyItemRef) continue;
                        if (pickup.gameObject == target
                            || pickup.transform.parent == target.transform)
                        {
                            isPickupHider = true;
                            break;
                        }
                    }
                hidesPickup[key] = isPickupHider;
            }
            if (!isPickupHider) return;

            LogFlip(__instance, "pickup");
            bool sent = ap.KeyItemCheckSent(__instance.keyItemRef);
            __result = _c == HideCondition.KeyItemObtained ? sent : !sent;
        }
    }

    /// <summary>Interaction.CheckPreventCondition(PreventType) - Interaction.cs:936.
    /// THIRD possession layer (session 2, retour Jonath: the church Doorknob never
    /// appeared once the AP Doorknob was received, and never came back). Beyond
    /// pickup visibility and ContentHiders, an Interaction can be blocked outright by
    /// PreventType.KeyItemObtained, which reads SaveManager.ObtainedKeyItem
    /// (Interaction.cs:960) - the two doorknob pickups
    /// (missingDoorknob0_grass / _branchHole) use exactly that. Receiving the item
    /// from the multiworld therefore killed their check for good.
    ///
    /// Scoped tightly: only flipped when the interaction HANDS OUT a pickup of that
    /// very key item. Interactions that REQUIRE an item to act (unlocking a door with
    /// a key: KeyItemNotObtained with no matching pickup) keep vanilla semantics, so
    /// no check-state shortcut can open something the player cannot legitimately
    /// open.</summary>
    [HarmonyPatch(typeof(Interaction), nameof(Interaction.CheckPreventCondition))]
    public static class InteractionPreventPatch
    {
        private static void Postfix(Interaction __instance, PreventType _type, ref bool __result)
        {
            if (_type != PreventType.KeyItemObtained && _type != PreventType.KeyItemNotObtained) return;
            ApClient ap = Plugin.Ap;
            if (ap == null || !ap.Connected) return;

            ItemPickup pickup = __instance.itemReference;
            if (pickup == null || pickup.keyItemObtain == null || pickup.keyItemObtain.Count == 0) return;
            if (pickup.keyItemObtain[0] != __instance.keyItemObtainedRef) return;

            bool sent = ap.KeyItemCheckSent(__instance.keyItemObtainedRef);
            __result = _type == PreventType.KeyItemObtained ? sent : !sent;
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
            // Re-injection is DEFERRED to the next safe state (player up, controllable):
            // granting ~40 items - tools especially - during the scripted bus intro froze
            // every input (playtest round 2). ApClient.TickGrants applies it later.
            ap.NeedsReinject = true;
            HutLock.OnNewRun();   // lock_player_hut: re-arm like vanilla locks (field writes only)
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
                    // Diagnostic popup removed (session 2, retour Jonath): it had
                    // identified the unknown gulden spots (#2, #8) - job done. Log only.
                    if (ApClient.Verbose)
                        Plugin.Log?.LogInfo($"[Grunnchipelago] Gulden pickup: {GameIds.GuldenLocationNames[index]}");
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
                // (Bunker flood note: vanilla raises the water while the player HOLDS
                // the Trowel in the bunker, GameManager.cs:2231. We intercept that
                // grant, so BunkerFlood.Tick drives it from the Trowel CHECK every run -
                // firing it only here, on pickup, broke re-runs once the check was sent
                // and the pickup vanished, retour Jonath.)
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

    /// <summary>GameManager.ToGameState (GameManager.cs:3422). Round 2 request: during an
    /// ending NPC dialogue (Owner / OwnerSaved talking), Escape SKIPS the dialogue instead
    /// of opening the pause menu - we fast-forward the prompt chain (the NPC's own
    /// HandleTalking then runs EndConversation, preserving its side effects such as the
    /// AtticKey grant) and swallow the pause. Outside dialogues, Escape pauses as usual.</summary>
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.ToGameState))]
    public static class EscSkipsEndingDialoguePatch
    {
        private static bool Prefix(GameManager.GameState _to)
        {
            if (_to != GameManager.GameState.Paused) return true;
            ApClient ap = Plugin.Ap;
            if (ap == null || !ap.Connected) return true;

            bool skipped = false;
            Owner owner = GameManager.owner;
            if (owner != null && owner.curState == Owner.State.Talk)
            {
                owner.curPromptIndex = owner.curPromptIndexMax;
                owner.waitingForInput = false;
                skipped = true;
            }
            OwnerSaved saved = GameManager.ownerSaved;
            if (saved != null && saved.curState == OwnerSaved.State.Talk)
            {
                saved.curPromptIndex = saved.curPromptIndexMax;
                saved.waitingForInput = false;
                skipped = true;
            }
            if (skipped)
            {
                Plugin.Log?.LogInfo("[Grunnchipelago] Ending dialogue skipped (ESC).");
                return false;   // Escape consumed: no pause menu
            }
            return true;
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

    /// <summary>design section 10, feature #3 + session 2 iter 8 - the AP "Bone" AND
    /// "Compass" items are never injected into the inventory: each would kill a
    /// loupable ending (Dog / HedgeMaze). Instead a world pickup clone spawns next to
    /// the roses sign at the start; taking it grants the vanilla item WITHOUT sending
    /// a check (ItemPickupTriggerPatch.SpecialPickupActive), so the player only picks
    /// it up when needed. Across runs the clones follow vanilla rules: ResetWorld ->
    /// ResetState shows them again whenever the item is not currently held.</summary>
    internal static class GiftPickups
    {
        private static GameObject boneInstance;
        private static GameObject compassInstance;

        // Next to the roses sign / pupitre (plantSign0, dump: -36.5, 10.0, -66.2), on
        // the side AWAY from the RedRoses bed (x -32..-37, z -61..-66) that kept
        // swallowing the bone (retours Jonath iters 3, 6 - "super bien positionné").
        private static readonly Vector3 BonePosition = new Vector3(-37.3f, 10.35f, -67.5f);
        private static readonly Vector3 CompassPosition = new Vector3(-38.6f, 10.35f, -66.6f);

        public static void EnsureSpawned(ApClient ap)
        {
            if (GameManager.instance == null || GameManager.allItemPickups == null
                || GameManager.allItemPickups.Count < 50) return;   // world not loaded yet
            if (boneInstance == null && ap.BoneOwnedFromAp)
                boneInstance = Spawn(KeyItem.Bone, "grunnchipelago_boneGift", BonePosition);
            if (compassInstance == null && ap.CompassOwnedFromAp)
                compassInstance = Spawn(KeyItem.Compass, "grunnchipelago_compassGift", CompassPosition);
        }

        private static GameObject Spawn(KeyItem item, string name, Vector3 position)
        {
            ItemPickup template = null;
            foreach (ItemPickup pickup in GameManager.allItemPickups)
            {
                if (pickup == null || pickup.isGulden || pickup.isTool) continue;
                if (pickup.keyItemObtain != null && pickup.keyItemObtain.Count > 0
                    && pickup.keyItemObtain[0] == item)
                {
                    template = pickup;
                    break;
                }
            }
            if (template == null)
            {
                Plugin.Log?.LogWarning($"[Grunnchipelago] GiftPickups: no {item} template found.");
                return null;
            }

            GameObject instance = Object.Instantiate(template.gameObject, position, Quaternion.identity);
            instance.name = name;
            ItemPickup clone = instance.GetComponent<ItemPickup>();
            clone.startState = ItemPickupState.Show;
            // The compass template is a SHOP article (Hooibaal): the gift is free.
            clone.inShop = false;
            clone.soldByKid = false;
            clone.cost = 0;
            // The template may already be MODEL-SWAPPED (its location's scouted content
            // replaced the visuals - retour Jonath: the gift no longer looked like a
            // bone). Undo the swap on the clone: drop our holder, re-enable renderers.
            if (clone.visualsObject != null)
            {
                Transform swapped = clone.visualsObject.transform.Find("grunnchipelago_model");
                if (swapped != null) Object.DestroyImmediate(swapped.gameObject);
                foreach (Renderer renderer in clone.visualsObject.GetComponentsInChildren<Renderer>(true))
                    renderer.enabled = true;
            }
            instance.SetActive(true);
            // Awake already ran Init->ResetState DURING Instantiate, with the
            // TEMPLATE's startState - re-run it now that startState is Show, or the
            // gift only appears at the next world reset (retour Jonath iter 8: the
            // bone showed up one run late).
            clone.ResetState();
            Plugin.Log?.LogInfo($"[Grunnchipelago] Gift {item} spawned at {position}.");
            return instance;
        }
    }

    /// <summary>playtest E - lock_player_hut (experimental option): the door of the
    /// player hut is locked and requires AbandonedKey (orphan key per the v0.3 door
    /// table: it unlocks nothing in vanilla). Applied on world load and re-applied after
    /// each run reset; a legitimate in-run unlock (key used) is NOT re-locked because we
    /// only re-arm when the door lost our key requirement (world reset rebuilds doors).</summary>
    internal static class HutLock
    {
        /// <summary>Called every frame while connected; cheap (field checks only). Arms the
        /// lock once per world (detected by the missing key requirement); a legitimate
        /// in-run unlock (key used, locked=false but requirement still ours) is left open.</summary>
        public static void Tick(ApClient ap)
        {
            if (!ap.LockPlayerHut) return;
            Door door = GameManager.playerSchuurDoor;   // GameManager.cs:390
            if (door == null) return;

            bool hasOurKey = door.unlockItemNeeded != null
                && door.unlockItemNeeded.Count > 0
                && door.unlockItemNeeded[0] == KeyItem.AbandonedKey;
            if (hasOurKey) return;

            door.locked = true;
            door.unlockItemNeeded = new List<KeyItem> { KeyItem.AbandonedKey };
            Plugin.Log?.LogInfo("[Grunnchipelago] Player hut locked (AbandonedKey required).");
        }

        /// <summary>Re-arm after each run reset (vanilla locks re-lock every run).</summary>
        public static void OnNewRun()
        {
            ApClient ap = Plugin.Ap;
            if (ap == null || !ap.LockPlayerHut) return;
            Door door = GameManager.playerSchuurDoor;
            if (door == null) return;
            door.locked = true;
            if (door.unlockItemNeeded == null || door.unlockItemNeeded.Count == 0
                || door.unlockItemNeeded[0] != KeyItem.AbandonedKey)
                door.unlockItemNeeded = new List<KeyItem> { KeyItem.AbandonedKey };
        }
    }

    /// <summary>Bunker flood (retour Jonath): vanilla raises the water while the player
    /// HOLDS the Trowel in the bunker (a short timer then TriggerEarthQuake,
    /// GameManager.cs:2221-2242). Our interception means the "Obtain Trowel" pickup
    /// never grants the vanilla Trowel, and once its CHECK is sent the pickup is hidden
    /// - so firing the quake on pickup only worked the first run. Drive it from the
    /// check state instead: every run, in the bunker, if the Trowel check is sent (or
    /// the item is genuinely owned), trigger the quake once. triggeredEarthquake is
    /// per-run (reset by ResetRunProgress), so the water rises again each run.</summary>
    internal static class BunkerFlood
    {
        public static void Tick(ApClient ap)
        {
            var pd = SaveManager.progressDataCheck;
            if (pd == null || pd.triggeredEarthquake) return;
            if (AmbienceManager.instance == null
                || AmbienceManager.instance.curAmbienceArea != AmbienceArea.Bunker) return;
            if (GameManager.PlayerReading || GameManager.BlackScreen
                || GameManager.SwitchingState || GameManager.TriggeredNewDay) return;
            if (!ap.KeyItemCheckSent(KeyItem.Trowel)
                && !SaveManager.ObtainedKeyItem(KeyItem.Trowel)) return;

            GameManager.TriggerEarthQuake();
            Plugin.Log?.LogInfo("[Grunnchipelago] Bunker : seisme declenche (etat du check truelle).");
        }
    }

    /// <summary>Monotonic cache of the comfort-shortcut flags, so persistent_shortcuts can
    /// restore them after each ResetRunProgress. Fields: SaveManager.cs progressData.</summary>
    internal static class ShortcutCache
    {
        private static bool bijkeuken, created, hooiGarden, maze;
        private static readonly List<Lock> locks = new List<Lock>();

        /// <summary>Session 2 - the cache is monotonic and STATIC: without this reset a
        /// new seed inherited the previous multiworld's shortcuts (Jonath: the
        /// garden-exterior shears shortcut survived a reseed + fresh run).</summary>
        public static void Clear()
        {
            bijkeuken = created = hooiGarden = maze = false;
            locks.Clear();
        }

        public static void Capture()
        {
            var pd = SaveManager.progressDataCheck;
            if (pd == null) return;
            bijkeuken |= pd.unlockedBijkeukenShortcut;
            created |= pd.createdShortcut;
            hooiGarden |= pd.parkUnlockedHooibaalGarden;
            maze |= pd.parkUnlockedMaze;
            if (pd.locksUnlocked != null)
                foreach (Lock l in pd.locksUnlocked)
                    if (!locks.Contains(l)) locks.Add(l);
        }

        // unlockedIntratuin is deliberately NOT restored (session 2 iter 8): restoring
        // the flag colours the door's flower emblem but the door itself only opens
        // through the watering event - the flag being "already true" then prevents
        // re-watering from opening it. Vanilla per-run behaviour is strictly better.
        public static void Restore()
        {
            var pd = SaveManager.progressDataCheck;
            if (pd == null) return;
            pd.unlockedBijkeukenShortcut = bijkeuken;
            pd.createdShortcut = created;
            pd.parkUnlockedHooibaalGarden = hooiGarden;
            pd.parkUnlockedMaze = maze;
            if (locks.Count > 0) pd.locksUnlocked = new List<Lock>(locks);
        }
    }
}
