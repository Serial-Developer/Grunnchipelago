using System;
using System.Collections.Generic;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using UnityEngine;
using Color = UnityEngine.Color;   // Models has its own Color type

namespace Grunnchipelago.Client
{
    /// <summary>
    /// Features #1/#2 (prompt_cc_modeles) - a pickup shows the model of what its check
    /// actually CONTAINS (from the connection scout), not the vanilla item:
    /// - our own Grunn items use the real item model, harvested from the visualsObject
    ///   of the scene's own ItemPickups (feature #1);
    /// - other players' items, and Grunn items with no harvested model, use an
    ///   "Archipelago" model per classification: progression / useful / filler
    ///   (feature #2, PROVISIONAL art direction: tinted polaroid clone - Jonath picks
    ///   the final look);
    /// - trap-flagged checks disguise as useful OR progression, deterministically per
    ///   seed+location so relaunching never betrays them.
    /// Only the visuals change: colliders, interaction and check flow stay vanilla
    /// (the model is parented under visualsObject, whose SetActive keeps driving
    /// visibility - ItemPickup.cs:133).
    /// </summary>
    internal static class ModelSwap
    {
        private enum ApKind { Progression, Useful, Filler }

        private static readonly Color ProgressionTint = new Color(1f, 0.35f, 0.2f);  // AP red-orange
        private static readonly Color UsefulTint = new Color(0.35f, 0.55f, 1f);      // blue
        private static readonly Color FillerTint = new Color(0.75f, 0.75f, 0.75f);   // grey

        private static bool applied;
        private static readonly Dictionary<KeyItem, GameObject> library = new Dictionary<KeyItem, GameObject>();
        private static GameObject apModelSource;   // polaroid visual (provisional)

        /// <summary>One-shot per session, once connected + scout done + world loaded.</summary>
        public static void Tick(ApClient ap)
        {
            if (applied || !ap.Connected || !ap.ScoutReady) return;
            if (GameManager.instance == null || GameManager.allItemPickups == null
                || GameManager.allItemPickups.Count < 50) return;

            BuildLibrary();
            int swapped = 0, apModels = 0, uncovered = 0;
            foreach (ItemPickup pickup in GameManager.allItemPickups)
            {
                int result = Apply(pickup, ap);
                if (result == 1) swapped++;
                else if (result == 2) apModels++;
                else if (result == 3) uncovered++;
            }
            applied = true;
            Plugin.Log?.LogInfo($"[Grunnchipelago] Model swap: {swapped} item models, " +
                                $"{apModels} AP models, {uncovered} left vanilla (no visual).");
        }

        // ---------- library (feature #1.1) ----------

        private static void BuildLibrary()
        {
            library.Clear();
            foreach (ItemPickup pickup in GameManager.allItemPickups)
            {
                if (pickup == null || pickup.isGulden || pickup.visualsObject == null) continue;
                if (pickup.keyItemObtain == null || pickup.keyItemObtain.Count == 0) continue;
                KeyItem key = pickup.keyItemObtain[0];
                if (!library.ContainsKey(key)) library[key] = pickup.visualsObject;
            }
            // Approximations for items with no placed pickup (given by NPC/event):
            // GoldFishAlive looks like the dead one, AtticKey like another generic key.
            // NOT covered (fall back to AP models): KidTriangle.
            if (!library.ContainsKey(KeyItem.GoldFishAlive) && library.ContainsKey(KeyItem.GoldFishDead))
                library[KeyItem.GoldFishAlive] = library[KeyItem.GoldFishDead];
            if (!library.ContainsKey(KeyItem.AtticKey) && library.ContainsKey(KeyItem.OldKey))
                library[KeyItem.AtticKey] = library[KeyItem.OldKey];

            // Provisional AP-model source: a polaroid ("photo from another world").
            if (apModelSource == null && GameManager.allPolaroids != null)
                foreach (Polaroid polaroid in GameManager.allPolaroids)
                    if (polaroid != null) { apModelSource = polaroid.gameObject; break; }
        }

        // ---------- per-pickup swap ----------

        /// <summary>0 = untouched, 1 = item model, 2 = AP model, 3 = no visual to swap.</summary>
        private static int Apply(ItemPickup pickup, ApClient ap)
        {
            if (pickup == null || pickup.isGulden || pickup.isRepeatablePickup) return 0;
            if (pickup.keyItemObtain == null || pickup.keyItemObtain.Count == 0) return 0;
            if (pickup.gameObject.name.StartsWith("grunnchipelago", StringComparison.Ordinal))
                return 0;   // bone gift really contains a bone

            KeyItem vanilla = pickup.keyItemObtain[0];
            long locationId = ap.ObtainLocationIdFor(vanilla);
            if (locationId <= 0 || !ap.TryGetScout(locationId, out ScoutedItemInfo scout) || scout == null)
                return 0;

            // Our own Grunn item -> concrete model (feature #1). Other players' items
            // ALWAYS use AP models even if the item exists in Grunn (feature #2.3).
            if (scout.IsReceiverRelatedToActivePlayer
                && Enum.TryParse(scout.ItemName, out KeyItem contained))
            {
                if (contained == vanilla) return 0;   // vanilla model already truthful
                if (library.TryGetValue(contained, out GameObject model))
                {
                    if (pickup.visualsObject == null) return 3;
                    SwapVisual(pickup, model, null);
                    return 1;
                }
                // Grunn item without a harvested model -> AP model fallback.
            }

            if (pickup.visualsObject == null) return 3;
            ApKind kind = KindFor(scout.Flags, locationId, ap.SeedString);
            if (apModelSource == null) return 3;
            SwapVisual(pickup, apModelSource, TintFor(kind));
            return 2;
        }

        /// <summary>Trap checks disguise as useful or progression - deterministic per
        /// seed+location, stable across sessions (feature #2.2).</summary>
        private static ApKind KindFor(ItemFlags flags, long locationId, string seed)
        {
            if ((flags & ItemFlags.Trap) != 0)
            {
                int hash = (seed + ":" + locationId).GetHashCode();
                return (hash & 1) == 0 ? ApKind.Progression : ApKind.Useful;
            }
            if ((flags & ItemFlags.Advancement) != 0) return ApKind.Progression;
            if ((flags & ItemFlags.NeverExclude) != 0) return ApKind.Useful;
            return ApKind.Filler;
        }

        private static Color TintFor(ApKind kind)
        {
            switch (kind)
            {
                case ApKind.Progression: return ProgressionTint;
                case ApKind.Useful: return UsefulTint;
                default: return FillerTint;
            }
        }

        /// <summary>Hide the original renderers and parent the replacement model under
        /// visualsObject, so the vanilla SetActive flow keeps driving visibility.</summary>
        private static void SwapVisual(ItemPickup pickup, GameObject modelSource, Color? tint)
        {
            foreach (Renderer renderer in pickup.visualsObject.GetComponentsInChildren<Renderer>(true))
                renderer.enabled = false;

            GameObject clone = UnityEngine.Object.Instantiate(modelSource, pickup.visualsObject.transform);
            clone.name = "grunnchipelago_model";
            clone.transform.localPosition = Vector3.zero;
            clone.transform.localRotation = Quaternion.identity;

            // Visual-only clone: strip behaviours and colliders, re-enable renderers.
            foreach (MonoBehaviour behaviour in clone.GetComponentsInChildren<MonoBehaviour>(true))
                UnityEngine.Object.Destroy(behaviour);
            foreach (Collider collider in clone.GetComponentsInChildren<Collider>(true))
                UnityEngine.Object.Destroy(collider);
            foreach (Renderer renderer in clone.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = true;
                if (tint.HasValue) renderer.material.color = tint.Value;
            }
            clone.SetActive(true);
        }
    }
}
