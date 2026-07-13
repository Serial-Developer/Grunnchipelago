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
        private int guldenReceivedTotal;
        private int pendingDeathLinks;

        // Guards so that server-side grants run the ORIGINAL game methods instead of
        // being re-interpreted as in-game pickups by our prefixes.
        public static bool GrantGuard { get; private set; }
        public static bool GuldenPickupGuard { get; set; }

        public bool Connected { get; private set; }
        public bool Coinsanity { get; private set; }
        public bool PersistentShortcuts { get; private set; }
        public bool DeathLinkEnabled { get; private set; }

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

        private void TrySend(long id, string label)
        {
            if (!Connected || session == null || id <= 0) return;
            lock (sentLocations)
            {
                if (!sentLocations.Add(id)) return;   // already sent / already checked
            }
            log.LogInfo($"[Grunnchipelago] Check: {label}");
            session.Locations.CompleteLocationChecks(id);
        }

        private void SendByName(string location)
        {
            if (!Connected || session == null) return;
            long id = session.Locations.GetLocationIdFromName(Game, location);
            if (id <= 0)
            {
                log.LogWarning($"[Grunnchipelago] No location id for '{location}'.");
                return;
            }
            TrySend(id, location);
        }

        /// <summary>Key items AND tools both map to "Obtain &lt;KeyItem&gt;".</summary>
        public void SendKeyItemCheck(KeyItem keyItem) => SendByName("Obtain " + keyItem);

        public void SendToolCheck(Item tool)
        {
            if (GameIds.ToolToKeyItem.TryGetValue(tool, out KeyItem keyItem))
                SendKeyItemCheck(keyItem);
        }

        public void SendPolaroidCheck(PolaroidType type)
        {
            // Ending polaroids are awarded by the endings, never shuffled.
            if (type.ToString().StartsWith("Ending", StringComparison.Ordinal)) return;
            SendByName("Polaroid: " + type);
        }

        /// <summary>Ghost / gulden indices come from the frozen path tables in GameIds.</summary>
        public void SendGhostCheck(int index) => TrySend(GameIds.GhostBaseId + index, $"Calm Ghost #{index + 1}");

        public void SendGuldenCheck(int index) => TrySend(GameIds.GuldenBaseId + index, $"Gulden #{index + 1}");

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
                log.LogInfo($"[Grunnchipelago] Death ending ({ending}) - sending DeathLink.");
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
            log.LogInfo($"[Grunnchipelago] DeathLink received from '{deathLink.Source}' - the run will reset.");
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
            {
                ItemInfo item = helper.DequeueItem();
                if (item.ItemName == "Gulden") Interlocked.Increment(ref guldenReceivedTotal);
                pending.Enqueue(item);
            }
        }

        /// <summary>Grant queued items. Called from the Unity main thread, only while
        /// actually in-game (menu / black screen / transitions keep items queued).</summary>
        public void ApplyPendingItems()
        {
            if (GameManager.instance == null) return;
            while (pending.TryDequeue(out ItemInfo item))
                GrantItem(item, realtime: true);
        }

        private void GrantItem(ItemInfo item, bool realtime)
        {
            string name = item.ItemName;
            if (Enum.TryParse(name, out KeyItem keyItem))
            {
                try
                {
                    GrantGuard = true;
                    if (GameIds.KeyItemToTool.TryGetValue(keyItem, out Item tool))
                        PlayerManager.instance?.AddTool(tool);   // tool item grants both
                    if (realtime) GameManager.TriggerItemObtainPopup(keyItem);   // no popup spam on re-inject
                    GameManager.instance.ObtainKeyItem(keyItem, false);
                    log.LogInfo($"[Grunnchipelago] Granted {keyItem}" + (realtime ? "." : " (reinject)."));
                }
                catch (Exception e) { log.LogError($"[Grunnchipelago] grant {keyItem} failed: {e.Message}"); }
                finally { GrantGuard = false; }
            }
            else if (name == "Gulden")
            {
                // Money only matters under coinsanity. On a fresh run the total is
                // restored by ReinjectInventory; here we only add the live delta.
                if (realtime && Coinsanity)
                {
                    GameManager.AddGulden(1, false);
                    log.LogInfo("[Grunnchipelago] Granted 1 Gulden.");
                }
            }
            // Buffs / traps land in a later milestone.
        }

        /// <summary>Re-grant the whole received inventory after a run reset (design section 5).</summary>
        public void ReinjectInventory()
        {
            if (!Connected || session == null || GameManager.instance == null) return;
            ItemInfo[] all;
            try { all = session.Items.AllItemsReceived.ToArray(); }
            catch (Exception) { return; }

            foreach (ItemInfo item in all)
                if (item.ItemName != "Gulden")
                    GrantItem(item, realtime: false);

            if (Coinsanity && guldenReceivedTotal > 0)
                GameManager.AddGulden(guldenReceivedTotal, false);

            log.LogInfo($"[Grunnchipelago] Re-injected inventory ({all.Length} items).");
        }

        public void Disconnect()
        {
            try { session?.Socket?.DisconnectAsync(); }
            catch (Exception) { }
            Connected = false;
        }
    }
}
