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
        /// <summary>Below this world-space size a posed clone counts as "nothing drawn".
        /// The smallest legitimate model is the gulden coin (~5 cm), so 5 mm only ever
        /// catches empty meshes - never a small but real item.</summary>
        private const float MinRenderedSize = 0.005f;

        /// <summary>Emission multiplier. 1.5 is the bloom "glowing orb" look Jonath liked on
        /// the AP cards; the crowns need far less or their pastel palette burns out.</summary>
        private const float DefaultEmission = 1.5f;
        private const float CrownEmission = 0.35f;

        private const float ApModelScale = 2.75f;
        private const float ApModelLift = 0.12f;

        /// <summary>Retour Jonath iter 5: our own buffs (and disguised traps) use the
        /// soul-fragment model tinted green instead of the AP card.</summary>
        private static readonly Color BuffTint = new Color(0.35f, 1f, 0.4f);
        private const float BuffModelScale = 1.25f;

        /// <summary>Retour Jonath (coinsanity, 2026-07-21): "Gulden" is not a KeyItem and
        /// not a buff, so a check CONTAINING money fell through to the AP card - tinted
        /// RED because coinsanity makes Gulden progression (items.py:95). With 36 Gulden
        /// in the pool that is red cards everywhere, unreadable. Money now shows the real
        /// coin. Slight boost + lift: a coin is small and often replaces a big polaroid
        /// card on a post (tune GuldenModelScale if it reads too small/large).</summary>
        private const string GuldenItemName = "Gulden";
        private const float GuldenModelScale = 1.5f;

        /// <summary>Placed gulden lie flat on the ground: their CONTENT model is lifted
        /// very slightly so it reads without floating (retour Jonath 2026-07-21).</summary>
        private const float GuldenContentLift = 0.1f;

        /// <summary>The chore coin (demande Jonath 2026-07-30): same coin mesh, tinted a
        /// warm GOLD and a touch bigger, so "worth 2 gulden" reads at a glance without a
        /// second model. Deliberately gold rather than yellow - it must not be mistaken for
        /// the yellow AP filler card.</summary>
        /// <summary>+15 % over a plain gulden (was +25 %, too much - retour Jonath 2026-07-31):
        /// enough to read as "worth more" without looking like a different object.</summary>
        private const float GoldenGuldenScaleBoost = 1.15f;

        /// <summary>AP CROWNS (demande Jonath 2026-07-28) - the multiworld models.
        ///
        /// Idea from Jonath: assemble SOUL FRAGMENTS into a ring shaped like the Archipelago
        /// logo (a five-petal flower). The fragment is already harvested in the library (it
        /// is the buff model), so a crown is just N copies laid out on a circle under one
        /// holder, archived like any other source.
        ///
        /// All three crowns carry the SAME SIX petals, laid out like the logo [J 2026-07-30];
        /// only the COLOURING tells them apart:
        ///   - Filler      : dull grey
        ///   - Useful      : green
        ///   - Progression : multicoloured (the logo's own six hues, one per petal)
        /// Every constant below is meant to be tuned from Jonath's in-game captures.</summary>
        private const int CrownPetalCount = 6;

        /// <summary>Progression wears the logo's six hues, one per petal, sampled from the
        /// Archipelago logo Jonath supplied (2026-08-01) and listed CLOCKWISE FROM THE TOP as
        /// they appear on it - rose, green, orchid, tan, blue-violet, yellow - so the ring
        /// reads in the logo's own order rather than an arbitrary one.</summary>
        private static readonly Color[] LogoPetalColors =
        {
            // Back to the logo's own values: in flat mode the petal no longer multiplies our
            // colour by the fragment's gradient, so there is nothing left to compensate for.
            new Color(0.77f, 0.455f, 0.50f),   // rose (top)
            new Color(0.50f, 0.75f, 0.48f),    // green
            new Color(0.77f, 0.61f, 0.82f),    // orchid
            new Color(0.85f, 0.60f, 0.42f),    // tan
            new Color(0.48f, 0.52f, 0.77f),    // blue-violet
            new Color(0.91f, 0.88f, 0.59f),    // yellow
        };

        /// <summary>Useful = green, Filler = dull grey [J 2026-07-30].
        ///
        /// VALIDATED IN GAME, DO NOT TOUCH [J 2026-08-01: "les couronnes filler et useful sont
        /// tres bien, ne les change pas"]. That includes how Paint renders them - any work on
        /// the colours must stay scoped to the Progression petals and to the gold coin, or it
        /// will silently undo these two.
        ///
        /// Progression is not here: it is multicoloured, handled petal by petal.</summary>
        private static readonly Color UsefulCrownColor = new Color(0.30f, 0.85f, 0.35f);
        private static readonly Color FillerCrownColor = new Color(0.55f, 0.55f, 0.55f);

        /// <summary>Ring radius, as a multiple of one petal's width.
        /// 0.62 was the first guess and the petals overlapped into one solid blob (capture
        /// Jonath 2026-07-30); 0.80 spreads them just enough to read as six distinct pieces
        /// - the "slightly exploded" look Jonath asked for - while still holding together as
        /// one flower. Push higher only if they should read as separate objects.</summary>
        private const float CrownRadiusFactor = 0.80f;

        /// <summary>Degrees each petal leans OUTWARD from the ring axis - the logo's petals
        /// fan out rather than standing straight.</summary>
        private const float CrownPetalTilt = 20f;

        /// <summary>true = the ring stands upright and reads face-on (default, like the logo);
        /// false = it lies flat and only reads from above. Flip to compare in game.</summary>
        private const bool CrownUpright = true;

        /// <summary>Crowns are assembled from several meshes, so they need their own scale
        /// (ApModelScale was calibrated on a single flat polaroid card).</summary>
        private const float ApCrownScale = 1.1f;

        private static bool applied;
        private static readonly Dictionary<KeyItem, GameObject> library = new Dictionary<KeyItem, GameObject>();
        private static readonly Dictionary<ApKind, GameObject> apCrowns = new Dictionary<ApKind, GameObject>();
        private static GameObject guldenModel;     // harvested from a placed gulden pickup
        private static GameObject apModelSource;   // polaroid visual (fallback when no crown)
        private static Transform archiveRoot;      // inactive vault of pristine copies

        /// <summary>Every holder we parented into the scene, and every vanilla renderer we
        /// switched off to make room for it. Kept so a session switch can put the scene back
        /// exactly as the game built it - see RevertSwaps.</summary>
        private static readonly List<GameObject> posedHolders = new List<GameObject>();
        private static readonly List<Renderer> hiddenVanillaRenderers = new List<Renderer>();

        /// <summary>Forget everything harvested for the PREVIOUS multiworld, so the next
        /// connection rebuilds from its own scouts. Without this the models, the crowns and
        /// the "already swapped" flag survived a switch between two multiworlds
        /// [J 2026-08-01]. Must run on the main thread: it destroys GameObjects.</summary>
        public static void ResetForNewSession()
        {
            // BEFORE anything else: put the scene back. Grunn does not reload the scene on a
            // profile switch, so the previous multiworld's clones were still parented under
            // the pickups - and BuildLibrary harvests its models FROM those very pickups.
            // Seed 2 therefore archived seed 1's clones: the buff model came out as a coin
            // and the crown petals grew from 0,335 m to 0,535 m by compounding
            // [J 2026-08-01: "les modeles des boost sont melanges a celui des guldens"].
            // Pickups left un-swapped in the new seed also kept run 1's renderers switched
            // off, which is the same report seen from the other side ("les objets caches
            // dans la save 1 le sont toujours dans la save 2").
            RevertSwaps();
            applied = false;
            library.Clear();
            apCrowns.Clear();
            guldenModel = null;
            apModelSource = null;
            wormHolders.Clear();
            if (archiveRoot != null)
            {
                UnityEngine.Object.Destroy(archiveRoot.gameObject);
                archiveRoot = null;
            }
            strippedAudioSources = 0;
        }

        /// <summary>One-shot per session, once connected + scout done + world loaded.
        /// After that pass it keeps running a light watcher that shows/hides the worm
        /// plate's clone with the game's own placedApple flag (see wormHolders).</summary>
        public static void Tick(ApClient ap)
        {
            if (!ap.Connected || !ap.ScoutReady) return;
            if (applied) { TickWormHolders(); return; }
            // ONLY on a world that is actually being played. The scene objects survive on the
            // title screen in whatever state the previous run left them - collected pickups
            // and polaroids deactivated - and the swap used to run right there, on connect.
            // Everything already taken in run 1 was therefore swapped while INACTIVE, which
            // also skips the rendered-size check (it cannot measure a hidden object), and
            // those checks stayed missing for the whole of run 2 [J 2026-08-01: "les items
            // que je ne voyais pas dans la save 1 sont toujours disparus dans la save 2"].
            // Measured: 30 polaroids reported inactive at swap time after a switch, against
            // 7 on a freshly loaded world.
            if (GameManager.CurGameState != GameManager.GameState.Game) return;
            if (GameManager.instance == null || GameManager.allItemPickups == null
                || GameManager.allItemPickups.Count < 50) return;
            // Session 2, 2.1: polaroids are swapped too - wait for their registry
            // (Polaroid.Init populates allPolaroids over the first world frames).
            if (GameManager.allPolaroids == null || GameManager.allPolaroids.Count == 0) return;

            BuildLibrary();
            wormHolders.Clear();
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
            SwapMagicPondFish(ap);
            applied = true;
            Plugin.Log?.LogInfo($"[Grunnchipelago] Model swap: {swapped} item models, " +
                                $"{apModels} AP models, {uncovered} left vanilla (no visual); " +
                                $"polaroids: {polaroidSwapped} item models, {polaroidAp} AP models.");
            LogAudioBudget();
        }

        /// <summary>MEASURE, DON'T GUESS. Right after the swap, NAME every pickup the game
        /// wants on screen but that draws nothing [J 2026-08-01: "toujours le souci d'objet
        /// invisible dans la save 2", cause still unidentified]. The rendered-size check
        /// inside SwapVisual only covers what WE posed; this sweeps the whole world, so an
        /// object blanked by anything else shows up too.
        ///
        /// visualsObject active = the game decided this pickup is visible (ItemPickup.
        /// SetVisuals, the only thing it drives). Nothing rendering under it is therefore a
        /// defect, never a legitimate hidden state.</summary>
        /// <summary>Undo every swap posed in the scene: drop our holders, switch the vanilla
        /// renderers back on. The scene objects themselves are the game's, and they survive a
        /// profile switch untouched - so whatever we did to them, we have to undo ourselves.
        /// </summary>
        private static void RevertSwaps()
        {
            int destroyed = 0, restored = 0;
            foreach (GameObject holder in posedHolders)
                if (holder != null) { UnityEngine.Object.DestroyImmediate(holder); destroyed++; }
            posedHolders.Clear();

            foreach (Renderer renderer in hiddenVanillaRenderers)
                if (renderer != null) { renderer.enabled = true; restored++; }
            hiddenVanillaRenderers.Clear();

            if (destroyed > 0 || restored > 0)
                Plugin.Log?.LogInfo($"[Grunnchipelago] Swaps annules : {destroyed} modeles retires, "
                    + $"{restored} rendus vanilla restaures.");
        }

        /// <summary>Assemble the kid's triangle from the two halves the game keeps in the
        /// scruffy man's hands. The instrument carries the identity, so it stays at the
        /// origin; the beater is laid alongside it, offset by a fraction of the instrument's
        /// own measured width rather than by any fixed distance - their in-game positions are
        /// two arm-lengths apart, which would read as two unrelated props.</summary>
        private static GameObject BuildTriangleModel()
        {
            GameObject instrument = FindRenderableByName("scruffyMan_triangleInstrument0");
            GameObject stick = FindRenderableByName("scruffyMan_triangleStick0");
            if (instrument == null)
            {
                // Better a bare beater than an AP card if the instrument ever goes missing.
                if (stick == null) return null;
                Plugin.Log?.LogWarning("[Grunnchipelago] Modele KidTriangle : instrument "
                    + "introuvable, repli sur le baton seul.");
                return Archive(stick);
            }

            GameObject model = Archive(instrument);
            Vector3 size = WorldSize(model);
            if (stick != null)
            {
                GameObject beater = Archive(stick);
                // true: keep the beater's own world scale across the re-parenting, so it
                // stays the size the game gives it instead of inheriting the instrument's.
                beater.transform.SetParent(model.transform, true);
                beater.transform.position = model.transform.position
                                            + new Vector3(size.x * 0.7f, 0f, 0f);
            }
            Plugin.Log?.LogInfo("[Grunnchipelago] Modele KidTriangle assemble : instrument "
                + $"~{size.x:0.##} x {size.y:0.##} x {size.z:0.##} m"
                + (stick != null ? " + baton." : " (baton introuvable)."));
            return model;
        }

        // ---------- library (feature #1.1) ----------

        private static void BuildLibrary()
        {
            library.Clear();
            guldenModel = null;
            foreach (ItemPickup pickup in GameManager.allItemPickups)
            {
                if (pickup == null) continue;
                if (pickup.isGulden)
                {
                    // Gulden pickups are never swap TARGETS (Apply returns 0 for them),
                    // but one of them is the model SOURCE for money-carrying checks.
                    if (guldenModel == null)
                    {
                        GameObject coin = pickup.visualsObject != null
                            ? pickup.visualsObject : pickup.gameObject;
                        if (coin.GetComponentsInChildren<Renderer>(true).Length > 0)
                            guldenModel = Archive(coin);
                    }
                    continue;
                }
                if (pickup.keyItemObtain == null || pickup.keyItemObtain.Count == 0) continue;
                KeyItem key = pickup.keyItemObtain[0];
                // Some pickups have no designated visualsObject (suspected: flowerGem0,
                // whose location showed the AP card instead of the gem - retour Jonath).
                // MODEL SOURCE fallback only: harvest the whole pickup object (renderers
                // included, scripts stripped at archive time); swap TARGETS still
                // require a real visualsObject.
                GameObject source = pickup.visualsObject != null
                    ? pickup.visualsObject : pickup.gameObject;
                // Never archive a source without a single mesh: some pickups are pure
                // interaction markers whose visual lives elsewhere (prettyFlower_remove0
                // archived 0 renderers - retour Jonath). Handled case by case below.
                if (source.GetComponentsInChildren<Renderer>(true).Length == 0) continue;
                // ARCHIVE a pristine copy (retour Jonath iter 6, "objets enchevetres"):
                // referencing the LIVE visualsObject meant that once a pickup got a
                // clone embedded by a swap, every later swap using that pickup's model
                // cloned the embedded model along (sandwich-in-fragment, and
                // retroactively trowel-in-polaroid / boat-idol earlier).
                if (!library.ContainsKey(key)) library[key] = Archive(source);
            }
            // The pretty flower's mesh does NOT belong to its pickup (that one is a
            // bare interaction marker): it is three MeshRenderers on the Flower itself -
            // prettyFlowerBase / Leaves / Top - which the game merely ENABLES as the
            // plant grows (Flower.UpdatePrettyFlowerVisuals, Flower.cs:165-175). Harvest
            // the flower object and force the three on, which is the fully grown bloom
            // Jonath asked for.
            if (GameManager.prettyFlower != null)
            {
                GameObject bloom = Archive(GameManager.prettyFlower.gameObject);
                int meshes = 0;
                foreach (Renderer renderer in bloom.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.enabled = true;
                    meshes++;
                }
                library[KeyItem.PrettyFlower] = bloom;
                Plugin.Log?.LogInfo($"[Grunnchipelago] Modele PrettyFlower recolte sur la fleur ({meshes} renderers).");
            }

            // GoldFishAlive has no placed pickup; its LIVING visual is a scene object kept
            // hidden until revealed. Source order fixed by Jonath in-game (2026-07-21):
            // take the fish shown IN THE FISHBOWL once placed - dump
            // fishbowl0/FishAlive_ContentHider0 -> objectRef "FishAliveContainer".
            // The MagicPond content was tried first before and archived EMPTY: the probe
            // was grabbable but showed NOTHING. Hence FindRenderableByName - a source
            // without a single Renderer is useless (the general path above already
            // enforces that; this special case used to skip the check).
            if (!library.ContainsKey(KeyItem.GoldFishAlive))
            {
                GameObject alive = FindRenderableByName("FishAliveContainer")
                                   ?? FindRenderableByName("MagicPond_FishAlive_Content");
                if (alive != null)
                {
                    // Harvest the object that actually CARRIES the mesh, not the container.
                    // The fish sits at a big LOCAL OFFSET inside the bowl container, and
                    // Archive only zeroes the ROOT's position: that offset survived and the
                    // clone floated ~11 m above the pickup - grabbable (collider on the
                    // ground) but off-screen. Measured by the probe: model centre y=21.25
                    // for a pickup at y=10.4 [J 2026-07-22].
                    Renderer[] fishMeshes = alive.GetComponentsInChildren<Renderer>(true);
                    if (fishMeshes.Length == 1 && fishMeshes[0] != null
                        && fishMeshes[0].gameObject != alive)
                        alive = fishMeshes[0].gameObject;
                    library[KeyItem.GoldFishAlive] = Archive(alive);
                    Plugin.Log?.LogInfo($"[Grunnchipelago] Modele GoldFishAlive recolte sur "
                        + $"'{alive.name}' ({alive.GetComponentsInChildren<Renderer>(true).Length} renderers).");
                }
                else if (library.ContainsKey(KeyItem.GoldFishDead))
                {
                    library[KeyItem.GoldFishAlive] = library[KeyItem.GoldFishDead];
                    Plugin.Log?.LogWarning("[Grunnchipelago] Modele GoldFishAlive : aucun visuel "
                        + "vivant avec mesh trouve - repli sur le poisson MORT.");
                }
            }

            // KidTriangle borrows another kid instrument (retour Jonath iter 8).
            if (!library.ContainsKey(KeyItem.KidTriangle))
            {
                // The triangle only exists IN HANDS (retour Jonath iter 9), and it is TWO
                // separate objects on two different bones: the instrument in the scruffy
                // man's LEFT hand (scruffyMan_triangleInstrument0) and its beater in his
                // RIGHT (scruffyMan_triangleStick0), each behind its own contentHider.
                // Harvesting only the stick shipped a bare rod [J 2026-08-01: "le modele du
                // triangle ne comporte que le baton, et pas le triangle"]. Assemble both.
                GameObject triangle = BuildTriangleModel();
                if (triangle != null) library[KeyItem.KidTriangle] = triangle;
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

            // Fallback AP-model source: a polaroid ("photo from another world") - archived
            // too, or the polaroid swaps (which now hide the WHOLE polaroid render tree)
            // would blank our own card source. Used only when no crown could be built.
            if (apModelSource == null && GameManager.allPolaroids != null)
                foreach (Polaroid polaroid in GameManager.allPolaroids)
                    if (polaroid != null) { apModelSource = Archive(polaroid.gameObject); break; }

            BuildApCrowns();
        }


        /// <summary>Assemble the three AP crowns out of soul fragments (demande Jonath
        /// 2026-07-28). Silently skipped when no fragment was harvested - SwapForScout then
        /// falls back to the tinted polaroid card, exactly as before.</summary>
        private static void BuildApCrowns()
        {
            if (apCrowns.Count > 0) return;
            if (!TryGetBuffModel(out GameObject petal) || petal == null)
            {
                Plugin.Log?.LogWarning("[Grunnchipelago] Couronnes AP : aucun fragment d'ame "
                    + "recolte - repli sur la carte AP (polaroid).");
                return;
            }

            // Ring radius from the petal's own MEASURED width - never a guessed constant:
            // the fragment's baked scale differs from run to run (it is harvested wherever
            // the scene happens to hold it).
            float petalWidth = WorldSize(petal).x;
            if (petalWidth <= 0.001f) petalWidth = 0.25f;   // degenerate mesh: sane default
            float radius = petalWidth * CrownRadiusFactor;

            foreach (ApKind kind in new[] { ApKind.Progression, ApKind.Useful, ApKind.Filler })
            {
                GameObject crown = BuildCrown(petal, CrownPetalCount, radius, kind);
                if (crown == null) continue;
                apCrowns[kind] = crown;
                // Measured final size: what Jonath needs to calibrate ApCrownScale from a
                // capture rather than by trial and error.
                Vector3 size = WorldSize(crown) * ApCrownScale;
                Plugin.Log?.LogInfo($"[Grunnchipelago] Couronne AP {kind} : {CrownPetalCount} petales, "
                    + $"taille rendue ~{size.x:0.00} x {size.y:0.00} x {size.z:0.00} m.");
            }
            Plugin.Log?.LogInfo($"[Grunnchipelago] Couronnes AP construites ({apCrowns.Count}/3), "
                + $"petale {petalWidth:0.000} m, rayon {radius:0.000} m, "
                + $"orientation {(CrownUpright ? "verticale (lecture de face)" : "a plat")}.");
        }

        /// <summary>One crown: <paramref name="count"/> petals evenly spread on a circle,
        /// each leaning outward by CrownPetalTilt - the Archipelago logo's fanned-out flower.
        ///
        /// The ring stands UPRIGHT (petals in the XY plane, spun around Z): a logo is meant
        /// to be read face-on as the player walks up to the pickup. Laid flat it would only
        /// read from directly above. Flip CrownUpright to compare in game.</summary>
        private static GameObject BuildCrown(GameObject petal, int count, float radius, ApKind kind)
        {
            if (count <= 0 || archiveRoot == null) return null;
            var crown = new GameObject($"grunnchipelago_ap_crown_{kind}");
            crown.transform.SetParent(archiveRoot, false);   // inactive vault: no Awake fires
            crown.transform.localPosition = Vector3.zero;
            crown.transform.localRotation = Quaternion.identity;
            crown.transform.localScale = Vector3.one;

            for (int i = 0; i < count; i++)
            {
                float angle = 360f / count * i;
                Quaternion around = CrownUpright
                    ? Quaternion.Euler(0f, 0f, angle)    // upright ring, read face-on
                    : Quaternion.Euler(0f, angle, 0f);   // flat ring, read from above
                Vector3 offset = CrownUpright
                    ? new Vector3(0f, radius, 0f)
                    : new Vector3(0f, 0f, radius);
                GameObject leaf = UnityEngine.Object.Instantiate(petal, crown.transform);
                leaf.name = "petal" + i;
                leaf.transform.localPosition = around * offset;
                leaf.transform.localRotation = around * Quaternion.Euler(
                    CrownUpright ? 0f : CrownPetalTilt, CrownUpright ? CrownPetalTilt : 0f, 0f);
                // Colour is baked PETAL BY PETAL here, not passed to SwapVisual, because
                // progression needs SIX different hues on one model - a single tint could
                // never express that.
                Paint(leaf, kind == ApKind.Progression
                    ? LogoPetalColors[i % LogoPetalColors.Length]
                    : (kind == ApKind.Useful ? UsefulCrownColor : FillerCrownColor));

                // The soul fragment carries a real Light. Six per crown, on every swapped
                // check, is a lot of real-time lights for nothing: keep the halo on the
                // FIRST petal only. Same reasoning as dropping the AudioSources.
                if (i > 0)
                    foreach (Light light in leaf.GetComponentsInChildren<Light>(true))
                        if (light != null) UnityEngine.Object.DestroyImmediate(light);
            }
            return crown;
        }

        /// <summary>Paint one petal. Same channels as SwapVisual (colour + _BaseColor +
        /// emission + the fragment's own Light), so a crown reads at night like the tinted
        /// models do. Filler stays MATT on purpose: dull grey should not glow.</summary>
        private static void Paint(GameObject model, Color color)
        {
            bool matt = color == FillerCrownColor;   // dull filler: no glow
            // CrownEmission, not the default: emission ADDS light, and at 1.5 it pushed the
            // logo's pastel hues past saturation - orchid read as pure magenta, blue-violet
            // as cyan [J 2026-08-01, "pas exactement comme le logo"]. A gentle glow keeps
            // the hue.
            TintModel(model, color, !matt, emission: CrownEmission);
            foreach (Light light in model.GetComponentsInChildren<Light>(true))
            {
                light.color = color;
                if (matt) light.intensity *= 0.35f;   // dull filler keeps a faint halo only
            }
        }

        /// <summary>Bounding size of a model in WORLD units, read from the shared meshes and
        /// the baked scale. Renderer.bounds is unusable here: the archive is inactive, so it
        /// is never rendered and its world bounds stay stale.</summary>
        private static Vector3 WorldSize(GameObject model)
        {
            Vector3 size = Vector3.zero;
            foreach (MeshFilter filter in model.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter == null || filter.sharedMesh == null) continue;
                Vector3 s = filter.sharedMesh.bounds.size;
                Vector3 lossy = filter.transform.lossyScale;
                size = Vector3.Max(size, new Vector3(
                    Mathf.Abs(s.x * lossy.x), Mathf.Abs(s.y * lossy.y), Mathf.Abs(s.z * lossy.z)));
            }
            foreach (SkinnedMeshRenderer skinned in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (skinned == null || skinned.sharedMesh == null) continue;
                Vector3 s = skinned.sharedMesh.bounds.size;
                Vector3 lossy = skinned.transform.lossyScale;
                size = Vector3.Max(size, new Vector3(
                    Mathf.Abs(s.x * lossy.x), Mathf.Abs(s.y * lossy.y), Mathf.Abs(s.z * lossy.z)));
            }
            return size;
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
            RevealIfEmpty(copy);
            return copy;
        }

        /// <summary>Some models keep their mesh in a child object that the game only
        /// activates on an event - the pretty flower's grown bloom is the one Jonath
        /// asked for (iter 10: it stayed invisible). When a freshly archived copy has
        /// no usable renderer at all, activate its whole tree so the mesh exists.
        /// Scoped to otherwise-EMPTY copies: models that already display something keep
        /// exactly the variant the scene shows (no "destroyed + intact" doubles).</summary>
        private static void RevealIfEmpty(GameObject copy)
        {
            foreach (Renderer renderer in copy.GetComponentsInChildren<Renderer>(false))
                if (renderer.enabled) return;   // already shows something

            foreach (Transform child in copy.GetComponentsInChildren<Transform>(true))
                if (!child.gameObject.activeSelf) child.gameObject.SetActive(true);
            foreach (Renderer renderer in copy.GetComponentsInChildren<Renderer>(true))
                renderer.enabled = true;
        }

        /// <summary>Magic Pond fish revival (retour Jonath 2026-07-27): the "Obtain
        /// GoldFishAlive" check now fires when the DEAD fish is PLACED (MagicPondPlaceFishPatch),
        /// and the revived-fish content must show the CHECK's model, not the vanilla alive
        /// fish. The content (MagicPond_FishAlive_Content) is not an ItemPickup nor a
        /// Polaroid, so it is swapped here explicitly. It starts inactive (hidden until the
        /// dead fish is placed); the clone follows its parent's active state, so swapping it
        /// now is fine.</summary>
        private static void SwapMagicPondFish(ApClient ap)
        {
            GameObject content = FindInactiveByName("MagicPond_FishAlive_Content");
            if (content == null) return;
            long loc = ap.ObtainLocationIdFor(KeyItem.GoldFishAlive);
            if (loc <= 0 || !ap.TryGetScout(loc, out ScoutedItemInfo scout) || scout == null) return;

            // The clone is parented at localPosition ZERO (SwapVisual), so it lands on the
            // object we hand over - and MagicPond_FishAlive_Content is a CONTAINER whose
            // fish mesh sits at a local offset (dump: the hider is at x=6500 while the pond
            // interactions are at x=6503.83). Handing the container over therefore dropped
            // the model several metres off the water [J 2026-07-27, capture]. Same root
            // cause as the GoldFishAlive HARVEST bug of 2026-07-21, mirrored: target the
            // object that actually CARRIES the mesh, so the model replaces the fish exactly
            // where the fish renders.
            GameObject target = FindMeshHolder(content) ?? content;

            // vanillaItem = GoldFishAlive: if the check really holds the alive fish, keep it.
            int r = SwapForScout(target, scout, loc, ap, KeyItem.GoldFishAlive);
            if (r != 1 && r != 2) return;   // untouched (r=0) -> leave the vanilla fish alone

            if (target != content)
            {
                // Hide the REST of the container's vanilla visuals: SwapVisual only disables
                // the renderers under the object it received. Done after the swap so a
                // no-op swap never leaves an invisible fish.
                foreach (Renderer renderer in content.GetComponentsInChildren<Renderer>(true))
                    renderer.enabled = false;
                float gap = Vector3.Distance(content.transform.position, target.transform.position);
                Plugin.Log?.LogInfo(
                    $"[Grunnchipelago] Magic Pond : conteneur @{content.transform.position}, "
                    + $"mesh @{target.transform.position} (ecart {gap:0.00} m) - modele pose sur le mesh.");
            }
            Plugin.Log?.LogInfo($"[Grunnchipelago] Magic Pond : poisson vivant -> modele du check ({scout.ItemName}).");
        }

        /// <summary>First descendant that actually carries a Renderer (the object itself if
        /// it does). Null when the subtree has no mesh at all.</summary>
        private static GameObject FindMeshHolder(GameObject root)
        {
            if (root == null) return null;
            Renderer found = root.GetComponentInChildren<Renderer>(true);
            return found != null ? found.gameObject : null;
        }

        // ---------- event-gated pickup: the worm plate ----------

        /// <summary>Clone holders of the worm pickup, shown ONLY while the apple is on the
        /// plate.
        ///
        /// The worm is swapped like any other pickup, but its clone must not be visible
        /// before the world event. Renderer/activeInHierarchy heuristics DO NOT WORK here
        /// (retour Jonath 2026-07-21, bug reproduit) : the pickup's startState is Show, so
        /// ItemPickup.SetVisuals force-activates visualsObject (ItemPickup.cs SetVisuals),
        /// and area streaming re-activates it too - both read as "revealed" while the plate
        /// is still empty. The vanilla worm is hidden by a SEPARATE object
        /// (dump: ContentHiders/wormHider0 -> objectRef "wormLine"), not by visualsObject.
        ///
        /// So we gate on the GAME'S OWN condition instead: ProgressData.placedApple, set by
        /// GameManager.PlaceApple (GameManager.cs:4690-4695) - the exact flag behind the
        /// interaction's NotPlacedApple preventType (dump: Main/Interactions/worm0).
        /// It lives in PER-RUN ProgressData, so the spot re-hides by itself every run.</summary>
        private static readonly List<GameObject> wormHolders = new List<GameObject>();

        private static void TickWormHolders()
        {
            if (wormHolders.Count == 0) return;
            bool placed;
            try
            {
                placed = SaveManager.progressDataCheck != null
                         && SaveManager.progressDataCheck.placedApple;
            }
            catch (Exception) { return; }

            for (int i = wormHolders.Count - 1; i >= 0; i--)
            {
                GameObject holder = wormHolders[i];
                if (holder == null) { wormHolders.RemoveAt(i); continue; }
                if (holder.activeSelf != placed) holder.SetActive(placed);
            }
        }

        // ---------- per-pickup swap ----------

        /// <summary>0 = untouched, 1 = item model, 2 = AP model, 3 = no visual to swap.</summary>
        private static int Apply(ItemPickup pickup, ApClient ap)
        {
            if (pickup == null || pickup.isRepeatablePickup) return 0;
            if (pickup.gameObject.name.StartsWith("grunnchipelago", StringComparison.Ordinal))
                return 0;   // bone gift really contains a bone

            // Placed gulden (retour Jonath 2026-07-21): under coinsanity they ARE checks
            // and can hold anything (this seed: Gulden #2 = MagicSword, #8 = SoulFragment1,
            // #14 = PurifiedStone), so they now show their real content like any pickup.
            // Their location is not an "Obtain X" - it is resolved by frozen scene path.
            // Coinsanity OFF: a gulden is plain money, not a check -> stays vanilla.
            if (pickup.isGulden)
            {
                if (!ap.Coinsanity) return 0;
                int guldenIndex = ScenePaths.GuldenIndex(pickup);
                if (guldenIndex < 0) return 0;
                long guldenLoc = ap.LocationIdByName(GameIds.GuldenLocationNames[guldenIndex]);
                if (guldenLoc <= 0 || !ap.TryGetScout(guldenLoc, out ScoutedItemInfo guldenScout)
                    || guldenScout == null) return 0;
                // Really contains money: the vanilla coin is already truthful - unless
                // mask_items is on, where a truthful coin would leak that this spot holds
                // filler (or progression under coinsanity) while every other spot is masked.
                if (!ap.MaskItems && guldenScout.ItemName == GuldenItemName
                    && guldenScout.IsReceiverRelatedToActivePlayer) return 0;
                // Coins sit flat on the ground: lift the content model slightly so it reads.
                return SwapForScout(pickup.visualsObject, guldenScout, guldenLoc, ap, null,
                    GuldenContentLift);
            }

            // Pretty flower: swapped again since iter 8 - Jonath wants the content
            // model there, and the growth animation works WITH the normalisation:
            // while the parent scale is ~0 the clone is invisible (SafeRatio caps the
            // ratio), and at full growth (scale 1) it lands at natural world size.
            if (pickup.keyItemObtain == null || pickup.keyItemObtain.Count == 0) return 0;

            KeyItem vanilla = pickup.keyItemObtain[0];
            // The worm plate reveals through a WORLD EVENT (place the apple). Swap it like
            // any other pickup, but start the clone HIDDEN and let TickWormHolders drive it
            // from the game's own placedApple flag - see wormHolders for why renderer-based
            // detection cannot work here.
            if (vanilla == KeyItem.Worm)
            {
                long wormLoc = ap.ObtainLocationIdFor(vanilla);
                if (wormLoc <= 0 || !ap.TryGetScout(wormLoc, out ScoutedItemInfo wormScout)
                    || wormScout == null || pickup.visualsObject == null) return 0;
                int wormResult = SwapForScout(pickup.visualsObject, wormScout, wormLoc, ap, vanilla);
                Transform wormHolder =
                    pickup.visualsObject.transform.Find("grunnchipelago_model");
                if (wormHolder != null)
                {
                    wormHolder.gameObject.SetActive(false);   // rien avant la pomme
                    wormHolders.Add(wormHolder.gameObject);
                    Plugin.Log?.LogInfo($"[Grunnchipelago] Worm -> {wormScout.ItemName} : modele "
                        + "pose, masque jusqu'a la pose de la pomme (placedApple).");
                }
                return wormResult;
            }
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
            long locationId, ApClient ap, KeyItem? vanillaItem, float itemLift = 0f)
        {
            if (visualsObject == null) return 3;

            // mask_items (YAML, 1.1.0): show NOTHING of what a location holds - every spot
            // wears the AP crown of its class, our own Grunn items included. That is the
            // whole point of the mode, so all the "show the real thing" branches below are
            // skipped and we fall through to the crown selection.
            if (!ap.MaskItems && scout.IsReceiverRelatedToActivePlayer)
            {
                if (Enum.TryParse(scout.ItemName, out KeyItem contained))
                {
                    if (vanillaItem.HasValue && contained == vanillaItem.Value)
                        return 0;   // vanilla model already truthful
                    if (library.TryGetValue(contained, out GameObject model)
                        && SwapVisual(visualsObject, model, null, 1f, itemLift))
                        return 1;
                    // Grunn item without a harvested model -> AP card fallback.
                }
                else if (scout.ItemName == GameIds.ItemGoldenGulden
                         && (scout.Flags & ItemFlags.Trap) == 0 && guldenModel != null)
                {
                    // The chore coin (2026-07-30): the real coin, tinted GOLD so it reads as
                    // "worth more" without needing a second model. Slightly larger than a
                    // plain gulden for the same reason.
                    // NOT TINTED. The chore coin is told apart by its SIZE alone.
                    //
                    // Seven attempts failed to make this mesh gold, and each ruled something
                    // out: the property name, every colour property at once, the vertex
                    // colours (there are none), a forced Unlit/Color material, the soul
                    // fragment's material, overwriting the gradient itself, and finally the
                    // lights (there are none either - measured, zero). Whatever paints this
                    // mesh is not reachable from a material, and the honest end state is a
                    // readable coin rather than a red blob [J 2026-08-01, closed].
                    if (SwapVisual(visualsObject, guldenModel, null,
                                   GuldenModelScale * GoldenGuldenScaleBoost, ApModelLift))
                        return 1;
                }
                else if (scout.ItemName == GuldenItemName && (scout.Flags & ItemFlags.Trap) == 0
                         && guldenModel != null)
                {
                    // Money shows the real coin instead of the red progression card
                    // (retour Jonath, coinsanity). Trap-flagged checks are excluded here
                    // so a disguised trap can never leak through the coin look.
                    if (SwapVisual(visualsObject, guldenModel, null, GuldenModelScale, ApModelLift))
                        return 1;
                }
                else if ((IsBuffName(scout.ItemName) || (scout.Flags & ItemFlags.Trap) != 0)
                         && TryGetBuffModel(out GameObject fragment))
                {
                    if (SwapVisual(visualsObject, fragment, BuffTint, BuffModelScale, ApModelLift))
                        return 2;
                }
            }

            // Multiworld item: the AP CROWN of its class (demande Jonath 2026-07-28) -
            // 5/4/3 soul fragments in a ring, tinted. Falls back to the old tinted polaroid
            // card if no fragment could be harvested this session.
            ApKind kind = KindFor(scout.Flags, locationId, ap.SeedString, ap.MaskItems);
            if (apCrowns.TryGetValue(kind, out GameObject crown) && crown != null)
            {
                // tint = null: the crown is ALREADY painted, petal by petal (see Paint).
                // Passing a tint here would flatten progression's six hues into one.
                if (SwapVisual(visualsObject, crown, null, ApCrownScale, ApModelLift))
                    return 2;
            }
            if (apModelSource == null) return 3;
            return SwapVisual(visualsObject, apModelSource, TintFor(kind), ApModelScale, ApModelLift)
                ? 2 : 3;
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
                if ((holder == null || !renderer.transform.IsChildOf(holder)) && renderer.enabled)
                {
                    renderer.enabled = false;
                    hiddenVanillaRenderers.Add(renderer);   // restored on a session switch
                }
        }

        /// <summary>Trap checks disguise as another class - deterministic per seed+location,
        /// so a relaunch never gives them away (feature #2.2).
        ///
        /// Normally a trap borrows progression or useful: filler is excluded because in the
        /// default mode our OWN filler is money, which shows the real coin, so a filler crown
        /// on a Grunn spot would already stand out. Under mask_items every class wears a
        /// crown, so the trap draws from all THREE (spec 1.1.0) - a two-way choice would make
        /// "never filler" a tell.</summary>
        private static ApKind KindFor(ItemFlags flags, long locationId, string seed, bool masked)
        {
            if ((flags & ItemFlags.Trap) != 0)
            {
                // Mono's string.GetHashCode is deterministic (Unity does not enable
                // randomized hashing), so the same seed+location always yields the same
                // disguise on the player's machine.
                int hash = (seed + ":" + locationId).GetHashCode();
                if (!masked) return (hash & 1) == 0 ? ApKind.Progression : ApKind.Useful;
                switch ((hash & 0x7FFFFFFF) % 3)
                {
                    case 0: return ApKind.Progression;
                    case 1: return ApKind.Useful;
                    default: return ApKind.Filler;
                }
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
        private static bool SwapVisual(GameObject visualsObject, GameObject modelSource,
            Color? tint, float scaleMult, float lift, float emission = DefaultEmission)
        {
            // Remember what we switch off: if the clone turns out to render nothing, the
            // vanilla visual must come back rather than leave an empty spot.
            var hiddenVanilla = new List<Renderer>();
            foreach (Renderer renderer in visualsObject.GetComponentsInChildren<Renderer>(true))
                if (renderer.enabled) { renderer.enabled = false; hiddenVanilla.Add(renderer); }

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
            // NO blanket enable here. Grunn keeps VARIANTS of the same prop under one object
            // and only switches on the one it wants - the magic trumpet is the plain trumpet
            // plus its magic version, side by side. Instantiate copies each renderer's
            // enabled flag, so the archive already carries the game's choice; forcing them
            // all on displayed both at once [J 2026-08-01: "le modele de la trompette magique
            // est dedoublee"]. A model that genuinely comes out with everything off gets a
            // second chance below, where it can be MEASURED instead of assumed.
            // Session 2 iter 2 (capture de nuit): the tint also goes through the emission
            // channel so AP models stay readable in the dark - 1.5 is the bloom "glowing
            // orb" look Jonath liked, now that the cards render at their normalised size.
            if (tint.HasValue) TintModel(clone, tint.Value, true, emission);
            // The soul fragment carries a real Light (the white-blue halo, iter 6):
            // Lights are not MonoBehaviours, StripNonVisuals leaves them - tint them
            // with the model so a green buff glows GREEN.
            if (tint.HasValue)
                foreach (Light light in clone.GetComponentsInChildren<Light>(true))
                    light.color = tint.Value;
            clone.SetActive(true);
            holder.SetActive(true);   // only renderers/meshes remain: nothing to awake

            // MEASURE, DON'T GUESS [J 2026-08-01: "des endroits ou on devrait avoir des
            // checks" et il n'y a rien]. A swap that produces no visible geometry is worse
            // than no swap at all: the vanilla renderers are already off, so the location
            // becomes an invisible check. Verify, and roll back if the clone renders
            // nothing. Only measurable while the target is active - a polaroid the game
            // keeps hidden reports zero bounds whatever its model is worth.
            float size = visualsObject.activeInHierarchy ? RenderedSize(clone) : float.MaxValue;
            if (size < MinRenderedSize)
            {
                // Nothing drawn: the archive may have been taken while the game had every
                // variant switched off. Turn them on and measure again - if it renders, we
                // keep it; if not, the rollback below returns the vanilla visual.
                foreach (Renderer renderer in clone.GetComponentsInChildren<Renderer>(true))
                    renderer.enabled = true;
                float retried = RenderedSize(clone);
                if (retried >= MinRenderedSize)
                    Plugin.Log?.LogInfo($"[Grunnchipelago] Modele '{modelSource.name}' : archive "
                        + "entierement masquee, renderers forces (taille rendue "
                        + $"{retried:0.###} m).");
                size = retried;
            }
            if (size >= MinRenderedSize)
            {
                // Kept so a switch to another multiworld can undo exactly this (RevertSwaps).
                posedHolders.Add(holder);
                hiddenVanillaRenderers.AddRange(hiddenVanilla);
                return true;
            }

            Plugin.Log?.LogWarning($"[Grunnchipelago] Modele invisible sur "
                + $"'{visualsObject.transform.parent?.name ?? visualsObject.name}' "
                + $"(source '{modelSource.name}', taille rendue {size:0.###} m) - "
                + "retour au visuel vanilla.");
            UnityEngine.Object.DestroyImmediate(holder);
            foreach (Renderer renderer in hiddenVanilla) renderer.enabled = true;
            return false;
        }

        /// <summary>Largest world-space dimension actually drawn by a clone. Renderers on
        /// inactive objects report stale/zero bounds, so they are skipped rather than
        /// encapsulated - one of them would otherwise drag the box to the origin.</summary>
        private static float RenderedSize(GameObject root)
        {
            bool any = false;
            Bounds bounds = default(Bounds);
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                if (!any) { bounds = renderer.bounds; any = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            if (!any) return 0f;
            Vector3 size = bounds.size;
            return Mathf.Max(size.x, Mathf.Max(size.y, size.z));
        }

        // ---------- teinte (Grunn = URP + ShaderGraph) ----------

        /// <summary>Paint every material of a model.
        ///
        /// renderer.material only ever returns SLOT 0, and the coin carries more than one -
        /// half of it kept its vanilla colour whatever we asked for [J 2026-08-01]. Hence
        /// renderer.materials, all slots.</summary>
        private static void TintModel(GameObject root, Color color, bool emissive,
            float emission = DefaultEmission)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                foreach (Material material in renderer.materials)   // ALL slots, not just [0]
                    TintMaterial(material, color, emissive, emission);
        }

        private static void NeutraliseVertexColors(GameObject root)
        {
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter == null) continue;
                Mesh mesh = filter.mesh;   // play-mode instance: editing it cannot touch the asset
                if (mesh == null) continue;
                Color[] colors = mesh.colors;
                if (colors == null || colors.Length == 0) continue;
                for (int i = 0; i < colors.Length; i++) colors[i] = Color.white;
                mesh.colors = colors;
            }
        }

        private static void TintMaterial(Material material, Color color, bool emissive,
            float emission)
        {
            if (material == null) return;

            // Only the base colour. Writing every colour slot the shader declares multiplies
            // the colour by itself - gold came out red and the crown's petals turned garish
            // [J 2026-08-01, measured on the probes].
            if (material.HasProperty("_Color")) material.color = color;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);

            if (material.HasProperty("_EmissionColor"))
            {
                if (emissive)
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", color * emission);
                }
                else
                {
                    material.DisableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", Color.black);
                }
            }
        }

        /// <summary>Find a scene object by exact name, INCLUDING inactive ones
        /// (Resources.FindObjectsOfTypeAll reaches disabled hierarchy, unlike
        /// GameObject.Find). First match wins.</summary>
        private static GameObject FindInactiveByName(string name)
        {
            foreach (Transform t in Resources.FindObjectsOfTypeAll<Transform>())
                if (t != null && t.name == name && t.gameObject.scene.IsValid())
                    return t.gameObject;
            return null;
        }

        /// <summary>FindInactiveByName, but only accepts an object that actually carries a
        /// mesh, and KEEPS SCANNING when a same-named object is an empty container.
        /// An empty source archives to an invisible model - precisely what made the fish
        /// probe grabbable but invisible (retour Jonath 2026-07-21).</summary>
        private static GameObject FindRenderableByName(string name)
        {
            foreach (Transform t in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (t == null || t.name != name || !t.gameObject.scene.IsValid()) continue;
                if (t.GetComponentsInChildren<Renderer>(true).Length > 0) return t.gameObject;
            }
            return null;
        }

        /// <summary>0-safe component ratio (a growing/zero-scaled parent must not
        /// produce NaN/Infinity - it stays invisible anyway while its scale is 0).</summary>
        private static float SafeRatio(float a, float b)
        {
            return Mathf.Abs(b) < 1e-4f ? 1f : a / b;
        }

        /// <summary>Remove every script and collider from a never-activated clone.
        /// Two passes + try/catch cover [RequireComponent] dependency chains.</summary>
        /// <summary>Total AudioSource dropped from clones this session - diagnostic for the
        /// "many sounds no longer play" report [J 2026-07-30].</summary>
        private static int strippedAudioSources;

        /// <summary>MEASURE, don't guess: after the swap pass, report how many AudioSources
        /// our clones carried and how many are live in the scene, against Unity's real-voice
        /// budget. If the live count sits at (or above) the cap, sounds ARE being dropped by
        /// priority and the cause is confirmed rather than assumed.</summary>
        private static void LogAudioBudget()
        {
            try
            {
                AudioSource[] all = UnityEngine.Object.FindObjectsOfType<AudioSource>();
                int playing = 0;
                foreach (AudioSource source in all)
                    if (source != null && source.isPlaying) playing++;
                int realVoices = AudioSettings.GetConfiguration().numRealVoices;
                Plugin.Log?.LogInfo(
                    $"[Grunnchipelago] Budget audio : {strippedAudioSources} AudioSource retirees "
                    + $"des clones ; scene = {all.Length} sources dont {playing} en lecture ; "
                    + $"budget Unity = {realVoices} voix reelles"
                    + (playing >= realVoices ? " -> SATURE, des sons sont ecartes." : "."));
            }
            catch (Exception e)
            {
                Plugin.Log?.LogWarning("[Grunnchipelago] Budget audio indisponible : " + e.Message);
            }
        }

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
            // AudioSource is NOT a MonoBehaviour, so the loop above never removed it - and
            // SwapVisual then reactivates the clone ("nothing to awake" was wrong: Lights
            // AND AudioSources survive). A playOnAwake/loop source on a harvested model was
            // therefore duplicated onto every swapped check, and a crown multiplies that by
            // its SIX petals. Unity only mixes a limited number of real voices (32 by
            // default): past that, new sounds are dropped by priority - exactly the "many
            // sounds are not played" symptom. A decorative clone must be SILENT.
            foreach (AudioSource source in root.GetComponentsInChildren<AudioSource>(true))
            {
                if (source == null) continue;
                try { UnityEngine.Object.DestroyImmediate(source); strippedAudioSources++; }
                catch (Exception) { }
            }
        }
    }
}
