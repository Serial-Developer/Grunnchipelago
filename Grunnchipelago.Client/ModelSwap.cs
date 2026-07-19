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

        /// <summary>Retour Jonath iter 5: our own buffs (and disguised traps) use the
        /// soul-fragment model tinted green instead of the AP card.</summary>
        private static readonly Color BuffTint = new Color(0.35f, 1f, 0.4f);
        private const float BuffModelScale = 1.25f;

        private static bool applied;
        private static readonly Dictionary<KeyItem, GameObject> library = new Dictionary<KeyItem, GameObject>();
        private static GameObject apModelSource;   // polaroid visual (provisional)
        private static Transform archiveRoot;      // inactive vault of pristine copies

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
                // included, scripts stripped at archive time); swap TARGETS still
                // require a real visualsObject.
                GameObject source = pickup.visualsObject != null
                    ? pickup.visualsObject : pickup.gameObject;
                // ARCHIVE a pristine copy (retour Jonath iter 6, "objets enchevetres"):
                // referencing the LIVE visualsObject meant that once a pickup got a
                // clone embedded by a swap, every later swap using that pickup's model
                // cloned the embedded model along (sandwich-in-fragment, and
                // retroactively trowel-in-polaroid / boat-idol earlier).
                if (!library.ContainsKey(key)) library[key] = Archive(source);
            }
            // Approximations for items with no placed pickup (given by NPC/event):
            // GoldFishAlive looks like the dead one; KidTriangle borrows another kid
            // instrument (retour Jonath iter 8: no model at all).
            if (!library.ContainsKey(KeyItem.GoldFishAlive) && library.ContainsKey(KeyItem.GoldFishDead))
                library[KeyItem.GoldFishAlive] = library[KeyItem.GoldFishDead];
            if (!library.ContainsKey(KeyItem.KidTriangle))
            {
                // The triangle only exists IN HANDS (retour Jonath iter 9): harvest the
                // scruffy man's held triangle+stick (scruffyMan_triangleStick0, in his
                // right hand until TradedEggball - dump:15694). FindObjectsOfTypeAll
                // reaches inactive bone children; one-shot at library build.
                foreach (Transform t in Resources.FindObjectsOfTypeAll<Transform>())
                    if (t != null && t.name == "scruffyMan_triangleStick0")
                    {
                        library[KeyItem.KidTriangle] = Archive(t.gameObject);
                        break;
                    }
                if (!library.ContainsKey(KeyItem.KidTriangle)
                    && (library.TryGetValue(KeyItem.KidTrumpet, out GameObject instrument)
                        || library.TryGetValue(KeyItem.KidCymbals, out instrument)
                        || library.TryGetValue(KeyItem.Trumpet, out instrument)))
                    library[KeyItem.KidTriangle] = instrument;
            }

            // Generic key fallback (retour Jonath iter 7): AbandonedKey / OldKey /
            // AtticKey are ORPHAN keys - no placed pickup, so they showed AP cards.
            // Grunn keys all look alike: any *Key item without a model borrows the
            // first harvested key model.
            GameObject genericKey = null;
            KeyItem[] keySources =
            {
                KeyItem.GardenKey, KeyItem.BridgeKey, KeyItem.OfficeKey,
                KeyItem.ToiletKey, KeyItem.ChurchKey, KeyItem.StrangeKey,
            };
            foreach (KeyItem k in keySources)
                if (library.TryGetValue(k, out GameObject m)) { genericKey = m; break; }
            if (genericKey != null)
                foreach (KeyItem k in (KeyItem[])Enum.GetValues(typeof(KeyItem)))
                    if (k.ToString().EndsWith("Key", StringComparison.Ordinal)
                        && !library.ContainsKey(k))
                        library[k] = genericKey;

            // Provisional AP-model source: a polaroid ("photo from another world") -
            // archived too, or the polaroid swaps (which now hide the WHOLE polaroid
            // render tree) would blank our own card source.
            if (apModelSource == null && GameManager.allPolaroids != null)
                foreach (Polaroid polaroid in GameManager.allPolaroids)
                    if (polaroid != null) { apModelSource = Archive(polaroid.gameObject); break; }
        }

        /// <summary>Copy a scene visual into an INACTIVE off-world vault (no Awake ever
        /// fires - playtest round 2 rule) with its WORLD scale baked in, scripts and
        /// colliders stripped. Swaps clone from these pristine copies, never from the
        /// live scene objects that later swaps mutate.</summary>
        private static GameObject Archive(GameObject source)
        {
            if (archiveRoot == null)
            {
                var root = new GameObject("grunnchipelago_model_library");
                root.SetActive(false);
                UnityEngine.Object.DontDestroyOnLoad(root);
                archiveRoot = root.transform;
            }
            GameObject copy = UnityEngine.Object.Instantiate(source, archiveRoot);
            copy.name = source.name;
            copy.transform.localPosition = Vector3.zero;
            copy.transform.localRotation = Quaternion.identity;
            // Bake the source's WORLD scale - clamping degenerate components to 1:
            // the pretty flower is harvested at connect time, BEFORE it grows, at
            // scale ~0 (retour Jonath iter 9: bake the FULLY GROWN flower, growth is
            // a scale animation over the same mesh).
            Vector3 s = source.transform.lossyScale;
            copy.transform.localScale = new Vector3(
                Mathf.Abs(s.x) < 0.01f ? 1f : s.x,
                Mathf.Abs(s.y) < 0.01f ? 1f : s.y,
                Mathf.Abs(s.z) < 0.01f ? 1f : s.z);
            StripNonVisuals(copy);
            return copy;
        }

        // ---------- per-pickup swap ----------

        /// <summary>0 = untouched, 1 = item model, 2 = AP model, 3 = no visual to swap.</summary>
        private static int Apply(ItemPickup pickup, ApClient ap)
        {
            if (pickup == null || pickup.isGulden || pickup.isRepeatablePickup) return 0;
            // Pretty flower: swapped again since iter 8 - Jonath wants the content
            // model there, and the growth animation works WITH the normalisation:
            // while the parent scale is ~0 the clone is invisible (SafeRatio caps the
            // ratio), and at full growth (scale 1) it lands at natural world size.
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

            return SwapForScout(pickup.visualsObject, scout, locationId, ap, vanilla);
        }

        /// <summary>Shared swap decision (pickups and world polaroids). Our own Grunn
        /// item -> concrete model (feature #1); our own buffs AND trap-flagged checks ->
        /// green soul-fragment (retour Jonath iter 5; traps MUST share the buff look,
        /// the pool's own non-key items are only buffs and traps so a card would betray
        /// them); anything else -> AP card by classification (feature #2).
        /// 0 = untouched, 1 = item model, 2 = AP/buff model, 3 = no visual.</summary>
        private static int SwapForScout(GameObject visualsObject, ScoutedItemInfo scout,
            long locationId, ApClient ap, KeyItem? vanillaItem)
        {
            if (visualsObject == null) return 3;

            if (scout.IsReceiverRelatedToActivePlayer)
            {
                if (Enum.TryParse(scout.ItemName, out KeyItem contained))
                {
                    if (vanillaItem.HasValue && contained == vanillaItem.Value)
                        return 0;   // vanilla model already truthful
                    if (library.TryGetValue(contained, out GameObject model))
                    {
                        SwapVisual(visualsObject, model, null, 1f, 0f);
                        return 1;
                    }
                    // Grunn item without a harvested model -> AP card fallback.
                }
                else if ((IsBuffName(scout.ItemName) || (scout.Flags & ItemFlags.Trap) != 0)
                         && TryGetBuffModel(out GameObject fragment))
                {
                    SwapVisual(visualsObject, fragment, BuffTint, BuffModelScale, ApModelLift);
                    return 2;
                }
            }

            if (apModelSource == null) return 3;
            ApKind kind = KindFor(scout.Flags, locationId, ap.SeedString);
            SwapVisual(visualsObject, apModelSource, TintFor(kind), ApModelScale, ApModelLift);
            return 2;
        }

        private static bool IsBuffName(string name)
        {
            return name == GameIds.BuffMoveSpeed || name == GameIds.BuffCutterRange
                || name == GameIds.BuffCuttingRate;
        }

        /// <summary>Buff model = a soul fragment (retour Jonath iter 5), harvested like
        /// any library model (soulFragment1 sits in a bottle in the Big House).</summary>
        private static bool TryGetBuffModel(out GameObject model)
        {
            if (library.TryGetValue(KeyItem.SoulFragment1, out model)) return true;
            if (library.TryGetValue(KeyItem.SoulFragment2, out model)) return true;
            if (library.TryGetValue(KeyItem.SoulFragment3, out model)) return true;
            return false;
        }

        /// <summary>Session 2, 2.1 - same swap for the scene's Polaroid objects (seen
        /// in-game: a picked polaroid granted a Plank with no visual hint). Rules match
        /// Apply (SwapForScout). After a successful swap the polaroid's WHOLE render
        /// tree is hidden, not just visualsObject - the big frame meshes live outside
        /// it and framed our clones ("popcorn dans un polaroid geant", retour Jonath
        /// iter 5). Ending polaroids are ending rewards, never locations.
        /// 0 = untouched, 1 = item model, 2 = AP model.</summary>
        private static int ApplyPolaroid(Polaroid polaroid, ApClient ap)
        {
            if (polaroid == null || polaroid.visualsObject == null) return 0;
            string typeName = polaroid.polaroidType.ToString();
            if (typeName.StartsWith("Ending", StringComparison.Ordinal)) return 0;

            long locationId = ap.LocationIdByName("Polaroid: " + typeName);
            if (locationId <= 0 || !ap.TryGetScout(locationId, out ScoutedItemInfo scout) || scout == null)
                return 0;   // polaroid_checks off or absent from the seed -> vanilla

            int result = SwapForScout(polaroid.visualsObject, scout, locationId, ap, null);
            if (result == 1 || result == 2)
                HideRenderersOutsideHolder(polaroid.gameObject, polaroid.visualsObject);
            return result;
        }

        /// <summary>Disable every renderer of the polaroid's whole tree except our
        /// clone's (the frame meshes outside visualsObject stayed visible and framed
        /// the content model). Vanilla hide-on-collect only drives visualsObject, which
        /// our holder lives under - so collection behaviour is unchanged.</summary>
        private static void HideRenderersOutsideHolder(GameObject root, GameObject visualsObject)
        {
            Transform holder = visualsObject.transform.Find("grunnchipelago_model");
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                if (holder == null || !renderer.transform.IsChildOf(holder))
                    renderer.enabled = false;
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
        private static void SwapVisual(GameObject visualsObject, GameObject modelSource,
            Color? tint, float scaleMult, float lift)
        {
            foreach (Renderer renderer in visualsObject.GetComponentsInChildren<Renderer>(true))
                renderer.enabled = false;

            var holder = new GameObject("grunnchipelago_model");
            holder.SetActive(false);   // BEFORE receiving children: no Awake ever fires
            holder.transform.SetParent(visualsObject.transform, false);
            holder.transform.localRotation = Quaternion.identity;

            GameObject clone = UnityEngine.Object.Instantiate(modelSource, holder.transform);
            clone.name = "model";
            clone.transform.localPosition = Vector3.zero;
            clone.transform.localRotation = Quaternion.identity;

            // WORLD-size normalisation (retour Jonath, iter 4: "la taille du modele
            // depend de sa position dans le monde") - the clone inherits the target
            // parent's scale chain (soulFragment in a scaled bottle = giant, trowel
            // under a shrunk parent = tiny). Cancel it out so the clone always renders
            // at its source's natural world size, x ApModelScale for AP cards.
            // cloneWorld = parentLossy * holderLocal * cloneLocal  =>  holderLocal =
            // mult * sourceLossy / (parentLossy * cloneLocal), per component.
            Vector3 parentLossy = visualsObject.transform.lossyScale;
            Vector3 sourceLossy = modelSource.transform.lossyScale;
            Vector3 cloneLocal = clone.transform.localScale;
            holder.transform.localScale = new Vector3(
                scaleMult * SafeRatio(sourceLossy.x, parentLossy.x * cloneLocal.x),
                scaleMult * SafeRatio(sourceLossy.y, parentLossy.y * cloneLocal.y),
                scaleMult * SafeRatio(sourceLossy.z, parentLossy.z * cloneLocal.z));
            // Lift in world units too.
            holder.transform.localPosition = lift > 0f
                ? new Vector3(0f, SafeRatio(lift, parentLossy.y), 0f)
                : Vector3.zero;

            StripNonVisuals(clone);
            foreach (Renderer renderer in clone.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = true;
                if (tint.HasValue)
                {
                    // Session 2 iter 2 (capture de nuit): also push the tint through
                    // the emission channel when the shader has one, so AP models stay
                    // readable in the dark. _BaseColor covers URP-style shaders whose
                    // .color (_Color) is a no-op (iter 6: the buff fragment stayed
                    // white). No-op on shaders without these properties.
                    Material material = renderer.material;
                    material.color = tint.Value;
                    if (material.HasProperty("_BaseColor"))
                        material.SetColor("_BaseColor", tint.Value);
                    if (material.HasProperty("_EmissionColor"))
                    {
                        // 1.5: the bloom "glowing orb" look Jonath liked, now that the
                        // cards render at their normalised (smaller) size.
                        material.EnableKeyword("_EMISSION");
                        material.SetColor("_EmissionColor", tint.Value * 1.5f);
                    }
                }
            }
            // The soul fragment carries a real Light (the white-blue halo, iter 6):
            // Lights are not MonoBehaviours, StripNonVisuals leaves them - tint them
            // with the model so a green buff glows GREEN.
            if (tint.HasValue)
                foreach (Light light in clone.GetComponentsInChildren<Light>(true))
                    light.color = tint.Value;
            clone.SetActive(true);
            holder.SetActive(true);   // only renderers/meshes remain: nothing to awake
        }

        /// <summary>0-safe component ratio (a growing/zero-scaled parent must not
        /// produce NaN/Infinity - it stays invisible anyway while its scale is 0).</summary>
        private static float SafeRatio(float a, float b)
        {
            return Mathf.Abs(b) < 1e-4f ? 1f : a / b;
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
