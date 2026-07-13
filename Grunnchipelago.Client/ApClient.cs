using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Models;
using BepInEx.Logging;

namespace Grunnchipelago.Client
{
    /// <summary>
    /// Owns the Archipelago session for a Grunn slot: connection, slot data, sending
    /// checks (deduplicated), granting received items, run re-injection, and DeathLink.
    /// </summary>
    public class ApClient
    {
        public const string Game = "Grunn";

        private readonly ManualLogSource log;
        private readonly ConcurrentQueue<ItemInfo> pending = new ConcurrentQueue<ItemInfo>();
        private readonly HashSet<EndingType> endingsSeen = new HashSet<EndingType>();

        // Local cache of already-sent location ids. Serves two purposes: dedupes the
        // double hooks on tools (ObtainKeyItem + AddTool both map to the same
        // "Obtain X"), and silences re-pickups across runs/sessions (seeded from the
        // server's AllLocationsChecked at login).
        private readonly HashSet<long> sentLocations = new HashSet<long>();

        private ArchipelagoSession session;
        private DeathLinkService deathLinkService;
        private volatile bool connecting;
        private float reconnectTimer;
        private string slotName = "";

        private int goal = GameIds.GoalTrueEnding;
        private int pendingDeathLinks;

        // Items already processed in past sessions (persisted by Plugin per seed+slot).
        // The server replays EVERY item on each connect; anything at or below this ordinal
        // is "historical": granted silently, never re-firing traps / popups / money.
        private int seenItemCount;
        private int itemOrdinal;
        public Func<string> LoadSeenState;      // returns "<seed>:<count>" or ""
        public Action<string> SaveSeenState;    // persists "<seed>:<count>"

        /// <summary>Verbose dev logging (config). Errors/connection/goal always log.</summary>
        public static bool Verbose = true;

        private void Info(string message)
        {
            if (Verbose) log.LogInfo(message);
        }

        // Guards so that server-side grants run the ORIGINAL game methods instead of
        // being re-interpreted as in-game pickups by our prefixes.
        public static bool GrantGuard { get; private set; }
        public static bool GuldenPickupGuard { get; set; }

        public bool Connected { get; private set; }
        public bool Coinsanity { get; private set; }
        public bool PersistentShortcuts { get; private set; }
        public bool DeathLinkEnabled { get; private set; }
        public bool PolaroidChecks { get; private set; } = true;
        public bool GhostChecks { get; private set; } = true;

        /// <summary>True once the received-items list says we own the AP "Bone" item.
        /// Bone is NEVER injected into the inventory: a world pickup spawns near the
        /// start instead, so the Dog ending stays reachable (design section 10).</summary>
        public bool BoneOwnedFromAp { get; private set; }

        /// <summary>Set after login: the local save must drop uncollected-check polaroids.</summary>
        public bool NeedsPolaroidSync { get; set; }

        /// <summary>Set after login: pickup visibility must be recomputed from check state
        /// (GrabbedItem event, once the world is loaded).</summary>
        public bool NeedsVisibilityRefresh { get; set; }

        // Scouted contents of our own locations (id -> "item (player)" + is-it-ours).
        private readonly Dictionary<long, ScoutedItemInfo> scouted = new Dictionary<long, ScoutedItemInfo>();

        // Popups queued from patch context (vanilla-popup suppression is active there);
        // drained by Plugin.Update outside any pickup call.
        private readonly ConcurrentQueue<string> pendingPopups = new ConcurrentQueue<string>();

        public ApClient(ManualLogSource logger)
        {
            log = logger;
        }

        // ---------- Connection ----------

        public void Connect(string host, int port, string slot, string password)
        {
            if (connecting || Connected) return;
            connecting = true;
            slotName = slot;
            log.LogInfo($"[Grunnchipelago] Connecting to {host}:{port} as '{slot}'...");

            Task.Run(() =>
            {
                try
                {
                    session = ArchipelagoSessionFactory.CreateSession(host, port);
                    session.Items.ItemReceived += OnItemReceived;
                    session.Socket.SocketClosed += OnSocketClosed;

                    LoginResult result = session.TryConnectAndLogin(
                        Game, slot, ItemsHandlingFlags.AllItems,
                        password: string.IsNullOrEmpty(password) ? null : password);

                    if (result is LoginSuccessful success)
                    {
                        ReadSlotData(success.SlotData);
                        SeedSentLocations();
                        SetupDeathLink();
                        LoadSeenItemCount();
                        ScoutOwnLocations();
                        itemOrdinal = 0;   // the replay stream restarts on every connect
                        NeedsPolaroidSync = PolaroidChecks;
                        NeedsVisibilityRefresh = true;
                        Connected = true;
                        log.LogInfo($"[Grunnchipelago] Connected. goal={goal}, coinsanity={Coinsanity}, " +
                                    $"persistent_shortcuts={PersistentShortcuts}, death_link={DeathLinkEnabled}.");
                    }
                    else
                    {
                        var failure = (LoginFailure)result;
                        log.LogError("[Grunnchipelago] Login failed: " + string.Join("; ", failure.Errors));
                    }
                }
                catch (Exception e)
                {
                    log.LogError("[Grunnchipelago] Connection error: " + e.Message);
                }
                finally
                {
                    connecting = false;
                }
            });
        }

        private void ReadSlotData(Dictionary<string, object> data)
        {
            if (data == null) return;
            if (data.TryGetValue(GameIds.SlotGoal, out var g)) goal = Convert.ToInt32(g);
            if (data.TryGetValue(GameIds.SlotCoinsanity, out var c)) Coinsanity = Convert.ToInt64(c) != 0;
            if (data.TryGetValue(GameIds.SlotPersistentShortcuts, out var p)) PersistentShortcuts = Convert.ToInt64(p) != 0;
            if (data.TryGetValue(GameIds.SlotDeathLink, out var d)) DeathLinkEnabled = Convert.ToInt64(d) != 0;
            if (data.TryGetValue(GameIds.SlotPolaroidChecks, out var pol)) PolaroidChecks = Convert.ToInt64(pol) != 0;
            if (data.TryGetValue(GameIds.SlotGhostChecks, out var gh)) GhostChecks = Convert.ToInt64(gh) != 0;
        }

        /// <summary>Scout every location of this slot so pickups can announce their real
        /// content ("Envoye : X -> joueur"). One request, cached for the session.</summary>
        private void ScoutOwnLocations()
        {
            try
            {
                long[] ids = session.Locations.AllLocations.ToArray();
                var task = session.Locations.ScoutLocationsAsync(ids);
                task.ContinueWith(t =>
                {
                    if (t.Status != TaskStatus.RanToCompletion || t.Result == null) return;
                    lock (scouted)
                        foreach (var kv in t.Result)
                            scouted[kv.Key] = kv.Value;
                });
            }
            catch (Exception e)
            {
                log.LogWarning("[Grunnchipelago] Location scout failed: " + e.Message);
            }
        }

        private void SeedSentLocations()
        {
            try
            {
                lock (sentLocations)
                    foreach (long id in session.Locations.AllLocationsChecked)
                        sentLocations.Add(id);
            }
            catch (Exception e)
            {
                log.LogWarning("[Grunnchipelago] Could not seed checked locations: " + e.Message);
            }
        }

        private void SetupDeathLink()
        {
            if (!DeathLinkEnabled) return;
            deathLinkService = session.CreateDeathLinkService();
            deathLinkService.EnableDeathLink();
            deathLinkService.OnDeathLinkReceived += OnDeathLinkReceived;
        }

        private void LoadSeenItemCount()
        {
            seenItemCount = 0;
            try
            {
                string stored = LoadSeenState?.Invoke() ?? "";
                string key = session.RoomState.Seed + ":" + slotName;
                int sep = stored.LastIndexOf(':');
                if (sep > 0 && stored.Substring(0, sep) == key)
                    seenItemCount = int.Parse(stored.Substring(sep + 1));
            }
            catch (Exception) { seenItemCount = 0; }
        }

        private void PersistSeenItemCount()
        {
            try { SaveSeenState?.Invoke(session.RoomState.Seed + ":" + slotName + ":" + seenItemCount); }
            catch (Exception) { }
        }

        private void OnSocketClosed(string reason)
        {
            if (Connected) log.LogWarning("[Grunnchipelago] Disconnected: " + reason);
            Connected = false;
        }

        public void Tick(float deltaTime, string host, int port, string slot, string password)
        {
            if (Connected || connecting) { reconnectTimer = 0f; return; }
            if (string.IsNullOrEmpty(slot)) return;
            reconnectTimer += deltaTime;
            if (reconnectTimer >= 5f)
            {
                reconnectTimer = 0f;
                Connect(host, port, slot, password);
            }
        }

        // ---------- Sending checks (deduplicated) ----------

        /// <summary>Returns true if the check was actually sent (not deduplicated).</summary>
        private bool TrySend(long id, string label)
        {
            if (!Connected || session == null || id <= 0) return false;
            lock (sentLocations)
            {
                if (!sentLocations.Add(id)) return false;   // already sent / already checked
            }
            Info($"[Grunnchipelago] Check: {label}");
            session.Locations.CompleteLocationChecks(id);
            return true;
        }

        private bool SendByName(string location)
        {
            if (!Connected || session == null) return false;
            long id = session.Locations.GetLocationIdFromName(Game, location);
            if (id <= 0)
            {
                log.LogWarning($"[Grunnchipelago] No location id for '{location}'.");
                return false;
            }
            bool sent = TrySend(id, location);
            if (sent) AnnounceScoutedContent(id);
            return sent;
        }

        /// <summary>Popup fix (design section 10): the vanilla pickup popup shows the item
        /// SEEN, not the item the location holds. When a check goes to another player,
        /// announce it; our own items announce themselves on receipt.</summary>
        private void AnnounceScoutedContent(long id)
        {
            ScoutedItemInfo info;
            lock (scouted)
            {
                if (!scouted.TryGetValue(id, out info)) return;
            }
            if (info == null || info.IsReceiverRelatedToActivePlayer) return;
            QueuePopup($"Envoye : {info.ItemName} -> {info.Player?.Name}");
        }

        public void QueuePopup(string text) => pendingPopups.Enqueue(text);

        /// <summary>design section 10, feature #5 - Grunn has a single save file: on a
        /// finished save, GlobalData.polaroidsCollected already holds everything, killing
        /// the 35 polaroid checks. Drop every polaroid whose AP check is NOT sent yet so
        /// the world object reappears (Polaroid.ResetState reads CheckPolaroidCollected).
        ///
        /// GlobalData audit (2026-07-13): polaroids are the ONLY GlobalData-gated checks.
        /// Ghosts (ghostCalmPosition) and gulden (coinGrabPosition) live in per-run
        /// ProgressData, endings are re-triggerable events, key items / tools are per-run.
        /// polaroidsSolved is ProgressData too - left untouched.</summary>
        public void SyncPolaroidsWithServer()
        {
            var collected = SaveManager.globalDataCheck?.polaroidsCollected;
            if (collected == null || session == null) return;

            var removed = new List<PolaroidType>();
            foreach (PolaroidType type in collected.ToArray())
            {
                if (type.ToString().StartsWith("Ending", StringComparison.Ordinal)) continue;
                long id = session.Locations.GetLocationIdFromName(Game, "Polaroid: " + type);
                if (id <= 0) continue;
                bool alreadyChecked;
                lock (sentLocations) alreadyChecked = sentLocations.Contains(id);
                if (!alreadyChecked)
                {
                    collected.Remove(type);
                    removed.Add(type);
                }
            }

            if (removed.Count == 0) return;
            SaveManager.Save(SaveManager.curSlotIndex);
            // Immediate world refresh: Polaroid.ResetState re-reads the collected list.
            if (GameManager.allPolaroids != null)
                foreach (Polaroid polaroid in GameManager.allPolaroids)
                    if (polaroid != null) polaroid.ResetState();
            Info($"[Grunnchipelago] Polaroid sync: {removed.Count} restored to the world " +
                 $"({string.Join(", ", removed.Select(t => t.ToString()).ToArray())}).");
        }

        /// <summary>Drained by Plugin.Update, outside pickup calls (whose vanilla popups
        /// are suppressed). Main thread only.</summary>
        public void FlushPendingPopups()
        {
            if (UIManager.instance == null) return;
            while (pendingPopups.TryDequeue(out string text))
                UIManager.instance.AddPopup(text);
        }

        /// <summary>Key items AND tools both map to "Obtain &lt;KeyItem&gt;".</summary>
        public void SendKeyItemCheck(KeyItem keyItem) => SendByName("Obtain " + keyItem);

        public void SendToolCheck(Item tool)
        {
            if (GameIds.ToolToKeyItem.TryGetValue(tool, out KeyItem keyItem))
                SendKeyItemCheck(keyItem);
        }

        // KeyItem -> "Obtain X" location id, cached: KeyItemCheckSent is polled every
        // frame by the ContentHider visibility patch.
        private readonly Dictionary<KeyItem, long> obtainLocationIds = new Dictionary<KeyItem, long>();

        private long ObtainLocationId(KeyItem keyItem)
        {
            lock (obtainLocationIds)
            {
                if (!obtainLocationIds.TryGetValue(keyItem, out long id))
                {
                    id = session.Locations.GetLocationIdFromName(Game, "Obtain " + keyItem);
                    obtainLocationIds[keyItem] = id;
                }
                return id;
            }
        }

        /// <summary>True if the "Obtain X" check for this key item is still unsent.</summary>
        public bool KeyItemCheckPending(KeyItem keyItem)
        {
            if (!Connected || session == null) return false;
            long id = ObtainLocationId(keyItem);
            if (id <= 0) return false;
            lock (sentLocations) return !sentLocations.Contains(id);
        }

        /// <summary>True if the "Obtain X" check for this key item HAS been sent. Drives
        /// pickup/shop visibility (randomizer semantics: check state, never possession).</summary>
        public bool KeyItemCheckSent(KeyItem keyItem)
        {
            if (!Connected || session == null) return false;
            long id = ObtainLocationId(keyItem);
            if (id <= 0) return false;
            lock (sentLocations) return sentLocations.Contains(id);
        }

        public void SendPolaroidCheck(PolaroidType type)
        {
            if (!PolaroidChecks) return;   // option off -> polaroids stay vanilla
            // Ending polaroids are awarded by the endings, never shuffled.
            if (type.ToString().StartsWith("Ending", StringComparison.Ordinal)) return;
            SendByName("Polaroid: " + type);
        }

        /// <summary>Ghost / gulden indices come from the frozen path tables in GameIds.</summary>
        public void SendGhostCheck(int index)
        {
            if (!GhostChecks) return;      // option off -> ghosts stay vanilla
            TrySend(GameIds.GhostBaseId + index, $"Calm Ghost #{index + 1}");
        }

        public void SendGuldenCheck(int index) => TrySend(GameIds.GuldenBaseId + index, GameIds.GuldenLocationNames[index]);

        // ---------- Endings & goal ----------

        public void OnEndingTriggered(EndingType ending)
        {
            if (!Connected) return;
            if (ending != EndingType.DemoEnding)
            {
                SendByName("Ending: " + ending);
                endingsSeen.Add(ending);
            }
            CheckGoal(ending);
            // Death endings send a DeathLink (decision Jonath: every ending except
            // Bus / Picnic / GoodEnd is a death). Received DeathLinks never trigger an
            // ending (they reset the run), so this cannot loop.
            if (DeathLinkEnabled && deathLinkService != null && GameIds.DeathLinkEndings.Contains(ending))
            {
                Info($"[Grunnchipelago] Death ending ({ending}) - sending DeathLink.");
                try { deathLinkService.SendDeathLink(new DeathLink(slotName, $"{slotName} met the {ending} ending")); }
                catch (Exception e) { log.LogError("[Grunnchipelago] SendDeathLink failed: " + e.Message); }
            }
        }

        private void CheckGoal(EndingType ending)
        {
            bool done =
                (goal == GameIds.GoalGoodEnding && ending == EndingType.GoodEnd) ||
                (goal == GameIds.GoalTrueEnding && ending == EndingType.GoodEnd
                    && SaveManager.progressDataCheck.restoredOwnerSoul) ||
                (goal == GameIds.GoalAllEndings && GameIds.AllEndings.IsSubsetOf(endingsSeen));

            if (done)
            {
                log.LogInfo("[Grunnchipelago] Goal achieved!");
                try { session.SetGoalAchieved(); }
                catch (Exception e) { log.LogError("[Grunnchipelago] SetGoalAchieved failed: " + e.Message); }
            }
        }

        // ---------- DeathLink (STRICT, design decision) ----------
        // Send: handled in OnEndingTriggered (death endings only).
        // Receive: the run is reset (no ending triggered, no check granted) after a
        // cosmetic nightmare jumpscare - orchestrated by Plugin.Update.

        private void OnDeathLinkReceived(DeathLink deathLink)
        {
            Interlocked.Increment(ref pendingDeathLinks);
            Info($"[Grunnchipelago] DeathLink received from '{deathLink.Source}' - the run will reset.");
        }

        /// <summary>Consume every pending received DeathLink at once (several deaths while
        /// away collapse into a single run reset). Returns how many were pending.</summary>
        public int TakeAllPendingDeathLinks()
        {
            return Interlocked.Exchange(ref pendingDeathLinks, 0);
        }

        // ---------- Receiving items ----------

        private void OnItemReceived(ReceivedItemsHelper helper)
        {
            while (helper.PeekItem() != null)
                pending.Enqueue(helper.DequeueItem());
        }

        /// <summary>Grant queued items. Called from the Unity main thread, only while
        /// actually in-game (menu / black screen / transitions keep items queued).
        /// The server replays every item on each connect: items at or below the persisted
        /// seen-count are "historical" (silent grant, no trap / popup / money).</summary>
        public void ApplyPendingItems()
        {
            if (GameManager.instance == null) return;
            bool granted = false;
            while (pending.TryDequeue(out ItemInfo item))
            {
                itemOrdinal++;
                bool historical = itemOrdinal <= seenItemCount;
                GrantItem(item, realtime: true, historical: historical);
                if (!historical) seenItemCount = itemOrdinal;
                granted = true;
            }
            if (granted)
            {
                // Hide the world models of now-owned items: every ItemPickup listens to
                // this vanilla event (GameManager.cs:3317, CheckIfAlreadyObtainedThisItem).
                GameManager.GrabbedItem();
                RecomputeBuffs();
                PersistSeenItemCount();
            }
        }

        private void GrantItem(ItemInfo item, bool realtime, bool historical)
        {
            string name = item.ItemName;
            if (Enum.TryParse(name, out KeyItem keyItem))
            {
                // Bone is special (design section 10): never injected into the inventory.
                // A world pickup spawns near the start instead (BoneGift), so the player
                // only takes it when needed and the Dog ending stays reachable.
                if (keyItem == KeyItem.Bone)
                {
                    BoneOwnedFromAp = true;
                    if (realtime && !historical) QueuePopup("Un os attend pres du bus...");
                    Info("[Grunnchipelago] Bone received - world pickup will spawn near the start.");
                    return;
                }
                try
                {
                    GrantGuard = true;
                    if (GameIds.KeyItemToTool.TryGetValue(keyItem, out Item tool))
                        PlayerManager.instance?.AddTool(tool);   // tool item grants both
                    if (realtime && !historical) GameManager.TriggerItemObtainPopup(keyItem);
                    GameManager.instance.ObtainKeyItem(keyItem, false);
                    Info($"[Grunnchipelago] Granted {keyItem}" + (realtime ? "." : " (reinject)."));
                }
                catch (Exception e) { log.LogError($"[Grunnchipelago] grant {keyItem} failed: {e.Message}"); }
                finally { GrantGuard = false; }
            }
            else if (name == "Gulden")
            {
                // Money only matters under coinsanity. Historical gulden are already in the
                // save; run resets are restored wholesale by ReinjectInventory.
                if (realtime && !historical && Coinsanity)
                {
                    GameManager.AddGulden(1, false);
                    Info("[Grunnchipelago] Granted 1 Gulden.");
                }
            }
            else if (realtime && !historical)
            {
                // Traps fire once, on fresh receipt only (buff counts are recomputed
                // separately from the full list, so nothing to do for buffs here).
                if (name.EndsWith("Trap", StringComparison.Ordinal))
                {
                    Effects.ApplyTrap(name);
                    Info($"[Grunnchipelago] Trap applied: {name}.");
                }
            }
        }

        /// <summary>Buff tiers are stateless: recount them from the authoritative full
        /// received list (idempotent across reconnects and run resets).</summary>
        private void RecomputeBuffs()
        {
            int move = 0, range = 0, rate = 0;
            try
            {
                foreach (ItemInfo item in session.Items.AllItemsReceived)
                {
                    switch (item.ItemName)
                    {
                        case GameIds.BuffMoveSpeed: move++; break;
                        case GameIds.BuffCutterRange: range++; break;
                        case GameIds.BuffCuttingRate: rate++; break;
                        case "Bone": BoneOwnedFromAp = true; break;
                    }
                }
            }
            catch (Exception) { return; }
            if (move != Effects.MoveSpeedBoosts || range != Effects.CutterRangeBoosts || rate != Effects.CuttingRateBoosts)
            {
                Effects.MoveSpeedBoosts = move;
                Effects.CutterRangeBoosts = range;
                Effects.CuttingRateBoosts = rate;
                Info($"[Grunnchipelago] Buffs: speed x{move}, range x{range}, rate x{rate}.");
            }
        }

        /// <summary>Re-grant the whole received inventory after a run reset (design section 5).</summary>
        public void ReinjectInventory()
        {
            if (!Connected || session == null || GameManager.instance == null) return;
            ItemInfo[] all;
            try { all = session.Items.AllItemsReceived.ToArray(); }
            catch (Exception) { return; }

            int gulden = 0;
            foreach (ItemInfo item in all)
            {
                if (item.ItemName == "Gulden") gulden++;
                else if (Enum.TryParse<KeyItem>(item.ItemName, out _))
                    GrantItem(item, realtime: false, historical: true);
                // traps never re-fire on re-injection; buffs are recomputed below
            }

            if (Coinsanity && gulden > 0)
                GameManager.AddGulden(gulden, false);

            // The world was reset BEFORE this re-injection, so the pickups of now-owned
            // items are visible again - hide them (vanilla event, GameManager.cs:3317).
            GameManager.GrabbedItem();
            RecomputeBuffs();

            Info($"[Grunnchipelago] Re-injected inventory ({all.Length} items).");
        }

        public void Disconnect()
        {
            try { session?.Socket?.DisconnectAsync(); }
            catch (Exception) { }
            Connected = false;
        }
    }
}
