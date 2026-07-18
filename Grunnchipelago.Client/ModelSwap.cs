using System;
using System.Collections.Generic;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using UnityEngine;
using Color = UnityEngine.Color;   // Models has its own Color type

namespace Grunnchipelago.Client
{
    /// <summary>
    /// Features #1/#2 (prompt_cc_modeles) + session 2 (2.1/2.2) - a pickup OR a world
    /// polaroid shows the model of what its check actually CONTAINS (from the
    /// connection scout), not the vanilla item:
    /// - our own Grunn items use the real item model, harvested from the visualsObject
    ///   of the scene's own ItemPickups (feature #1);
    /// - other players' items, and Grunn items with no harvested model, use an
    ///   "Archipelago" model per classification: progression / useful / filler
    ///   (feature #2, PROVISIONAL art direction: tinted polaroid clone scaled up
    ///   ApModelScale - Jonath picks the final look);
    /// - trap-flagged checks disguise as useful OR progression, deterministically per
    ///   seed+location so relaunching never betrays them.
    /// Only the visuals change: colliders, interaction and check flow stay vanilla
    /// (the model is parented under visualsObject, whose SetActive keeps driving
    /// visibility - ItemPickup.cs:133, Polaroid.cs:87).
    /// </summary>
    internal static class ModelSwap
    {
        private enum ApKind { Progression, Useful, Filler }

        // Saturated tints (session 2 iter 2: the old grey filler blended into paths,
        // capture Jonath) - filler is now bright yellow.
        private static readonly Color ProgressionTint = new Color(1f, 0.3f, 0.1f);   // AP red-orange
        private static readonly Color UsefulTint = new Color(0.2f, 0.5f, 1f);        // blue
        private static readonly Color FillerTint = new Color(1f, 0.85f, 0.1f);       // yellow

        /// <summary>Session 2, 2.2 (iter 2) - the tinted-polaroid AP models were still
        /// barely visible at 1.75 (captures Jonath): scale up harder and lift the card
        /// slightly off the ground.</summary>
        private const float ApModelScale = 2.75f;
        private const float ApModelLift = 0.12f;

        private static bool applied;
        private static readonly Dictionary<KeyItem, GameObject> library = new Dictionary<KeyItem, GameObject>();
        private static GameObject apModelSource;   // polaroid visual (provisional)

        /// <summary>One-shot per session, once connected + scout done + world loaded.</summary>
        public static void Tick(ApClient ap)
        {
            if (applied || !ap.Connected || !ap.ScoutReady) return;
            if (GameManager.instance == null || GameManager.allItemPickups == null
                || GameManager.allItemPickups.Count < 50) return;
            // Session 2, 2.1: polaroids are swapped too - wait for their registry
            // (Polaroid.Init populates allPolaroids over the first world frames).
            if (GameManager.allPolaroids == null || GameManager.allPolaroids.Count == 0) return;

            BuildLibrary();
            int swapped = 0, apModels = 0, uncovered = 0;
            foreach (ItemPickup pickup in GameManager.allItemPickups)
            {
                int result = Apply(pickup, ap);
                if (result == 1) swapped++;
                else if (result == 2) apModels++;
                else if (result == 3) uncovered++;
            }
            int polaroidSwapped = 0, polaroidAp = 0;
            foreach (Polaroid polaroid in GameManager.allPolaroids)
            {
                int result = ApplyPolaroid(polaroid, ap);
                if (result == 1) polaroidSwapped++;
                else if (result == 2) polaroidAp++;
            }
            applied = true;
            Plugin.Log?.LogInfo($"[Grunnchipelago] Model swap: {swapped} item models, " +
                                $"{apModels} AP models, {uncovered} left vanilla (no visual); " +
                                $"polaroids: {polaroidSwapped} item models, {polaroidAp} AP models.");
        }

        // ---------- library (feature #1.1) ----------

        private static void BuildLibrary()
        {
            library.Clear();
            foreach (ItemPickup pickup in GameManager.allItemPickups)
            {
                if (pickup == null || pickup.isGulden) continue;
                if (pickup.keyItemObtain == null || pickup.keyItemObtain.Count == 0) continue;
                KeyItem key = pickup.keyItemObtain[0];
                // Some pickups have no designated visualsObject (suspected: flowerGem0,
                // whose location showed the AP card instead of the gem - retour Jonath).
                // MODEL SOURCE fallback only: harvest the whole pickup object (renderers
                // included, scripts stripped at clone time); swap TARGETS still require
                // a real visualsObject.
                GameObject source = pickup.visualsObject != null
                    ? pickup.visualsObject : pickup.gameObject;
                if (!library.ContainsKey(key)) library[key] = source;
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
            // Session 2 retour Jonath: the worm pickups reveal through a WORLD EVENT
            // (apple placed on the plate - worm0's interaction has preventTypes
            // ObjectInactive/NotPlacedApple, dump:5334). The vanilla mesh sits in a
            // child object kept inactive until then, but our clone under visualsObject
            // rendered immediately and betrayed the spot: leave worm pickups vanilla.
            if (vanilla == KeyItem.Worm) return 0;
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
                    SwapVisual(pickup.visualsObject, model, null);
                    return 1;
                }
                // Grunn item without a harvested model -> AP model fallback.
            }

            if (pickup.visualsObject == null) return 3;
            ApKind kind = KindFor(scout.Flags, locationId, ap.SeedString);
            if (apModelSource == null) return 3;
            SwapVisual(pickup.visualsObject, apModelSource, TintFor(kind));
            return 2;
        }

        /// <summary>Session 2, 2.1 - same swap for the scene's Polaroid objects (seen
        /// in-game: a picked polaroid granted a Plank with no visual hint). Rules match
        /// Apply: own scouted Grunn item -> its real model, anything else -> AP model
        /// by classification. Ending polaroids are ending rewards, never locations.
        /// 0 = untouched, 1 = item model, 2 = AP model.</summary>
        private static int ApplyPolaroid(Polaroid polaroid, ApClient ap)
        {
            if (polaroid == null || polaroid.visualsObject == null) return 0;
            string typeName = polaroid.polaroidType.ToString();
            if (typeName.StartsWith("Ending", StringComparison.Ordinal)) return 0;

            long locationId = ap.LocationIdByName("Polaroid: " + typeName);
            if (locationId <= 0 || !ap.TryGetScout(locationId, out ScoutedItemInfo scout) || scout == null)
                return 0;   // polaroid_checks off or absent from the seed -> vanilla

            if (scout.IsReceiverRelatedToActivePlayer
                && Enum.TryParse(scout.ItemName, out KeyItem contained)
                && library.TryGetValue(contained, out GameObject model))
            {
                SwapVisual(polaroid.visualsObject, model, null);
                return 1;
            }

            if (apModelSource == null) return 0;
            ApKind kind = KindFor(scout.Flags, locationId, ap.SeedString);
            SwapVisual(polaroid.visualsObject, apModelSource, TintFor(kind));
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
        /// the given visualsObject, so the vanilla SetActive flow keeps driving
        /// visibility (ItemPickup and Polaroid both work that way). AP models (tinted)
        /// are scaled up by ApModelScale (session 2, 2.2).
        ///
        /// CRITICAL (playtest round 2 freeze): the clone is instantiated under an
        /// INACTIVE holder so Awake NEVER runs. Cloning active then Destroy()ing the
        /// scripts let BaseMonoBehaviour.Awake register the clone in UpdateManager,
        /// whose update loop has no null check (UpdateManager.cs:88) - the destroyed
        /// Polaroid component then threw every frame (2153 NullReferenceException in
        /// Player.log), killing the loop and freezing all inputs. DestroyImmediate on
        /// never-awakened components is safe and leaves no trace.</summary>
        private static void SwapVisual(GameObject visualsObject, GameObject modelSource, Color? tint)
        {
            foreach (Renderer renderer in visualsObject.GetComponentsInChildren<Renderer>(true))
                renderer.enabled = false;

            var holder = new GameObject("grunnchipelago_model");
            holder.SetActive(false);   // BEFORE receiving children: no Awake ever fires
            holder.transform.SetParent(visualsObject.transform, false);
            holder.transform.localPosition = tint.HasValue
                ? new Vector3(0f, ApModelLift, 0f) : Vector3.zero;
            holder.transform.localRotation = Quaternion.identity;
            if (tint.HasValue) holder.transform.localScale = Vector3.one * ApModelScale;

            GameObject clone = UnityEngine.Object.Instantiate(modelSource, holder.transform);
            clone.name = "model";
            clone.transform.localPosition = Vector3.zero;
            clone.transform.localRotation = Quaternion.identity;

            StripNonVisuals(clone);
            foreach (Renderer renderer in clone.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = true;
                if (tint.HasValue)
                {
                    // Session 2 iter 2 (capture de nuit): also push the tint through
                    // the emission channel when the shader has one, so AP models stay
                    // readable in the dark. No-op on shaders without _EmissionColor.
                    Material material = renderer.material;
                    material.color = tint.Value;
                    if (material.HasProperty("_EmissionColor"))
                    {
                        material.EnableKeyword("_EMISSION");
                        material.SetColor("_EmissionColor", tint.Value * 0.5f);
                    }
                }
            }
            clone.SetActive(true);
            holder.SetActive(true);   // only renderers/meshes remain: nothing to awake
        }

        /// <summary>Remove every script and collider from a never-activated clone.
        /// Two passes + try/catch cover [RequireComponent] dependency chains.</summary>
        private static void StripNonVisuals(GameObject root)
        {
            for (int pass = 0; pass < 2; pass++)
                foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (behaviour == null) continue;
                    try { UnityEngine.Object.DestroyImmediate(behaviour); } catch (Exception) { }
                }
            foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            {
                if (collider == null) continue;
                try { UnityEngine.Object.DestroyImmediate(collider); } catch (Exception) { }
            }
        }
    }
}
