using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using BepInEx.Logging;

namespace Grunnchipelago.Client
{
    /// <summary>
    /// Owns the Archipelago session for a Grunn slot: connection, sending "Obtain X"
    /// checks, and queueing received key items to be granted on the Unity main thread.
    /// Milestone 1 scope: key items only.
    /// </summary>
    public class ApClient
    {
        public const string Game = "Grunn";

        private readonly ManualLogSource log;
        private readonly ConcurrentQueue<KeyItem> pendingKeyItems = new ConcurrentQueue<KeyItem>();

        private ArchipelagoSession session;
        private volatile bool connecting;
        private float reconnectTimer;

        // Guard so that server-granted items run the ORIGINAL ObtainKeyItem instead of
        // being re-interpreted as an in-game pickup by our prefix.
        public static bool GrantGuard { get; private set; }

        public bool Connected { get; private set; }

        public ApClient(ManualLogSource logger)
        {
            log = logger;
        }

        // ---------- Connection ----------

        public void Connect(string host, int port, string slot, string password)
        {
            if (connecting || Connected) return;
            connecting = true;
            log.LogInfo($"[Grunnchipelago] Connecting to {host}:{port} as '{slot}'...");

            // TryConnectAndLogin is blocking - do it off the Unity thread.
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

                    if (result is LoginSuccessful)
                    {
                        Connected = true;
                        log.LogInfo("[Grunnchipelago] Connected to the multiworld.");
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

        private void OnSocketClosed(string reason)
        {
            if (Connected) log.LogWarning("[Grunnchipelago] Disconnected: " + reason);
            Connected = false;
        }

        /// <summary>Simple reconnection: retry on a timer while enabled but disconnected.</summary>
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

        // ---------- Sending checks ----------

        public void SendKeyItemCheck(KeyItem keyItem)
        {
            if (!Connected || session == null) return;
            string location = "Obtain " + keyItem;
            long id = session.Locations.GetLocationIdFromName(Game, location);
            if (id <= 0)
            {
                // Unsourced (e.g. Cymbals) or excluded: nothing to send.
                log.LogWarning($"[Grunnchipelago] No location id for '{location}' - not sent.");
                return;
            }
            log.LogInfo($"[Grunnchipelago] Check: {location}");
            session.Locations.CompleteLocationChecks(id);
        }

        // ---------- Receiving items ----------

        private void OnItemReceived(ReceivedItemsHelper helper)
        {
            while (helper.PeekItem() != null)
            {
                ItemInfo item = helper.DequeueItem();
                if (Enum.TryParse(item.ItemName, out KeyItem keyItem))
                {
                    pendingKeyItems.Enqueue(keyItem);
                }
                else
                {
                    // Buffs / traps / Gulden are handled in later milestones.
                    log.LogInfo($"[Grunnchipelago] Received non-keyitem '{item.ItemName}' (deferred).");
                }
            }
        }

        /// <summary>Grant queued key items. MUST be called from the Unity main thread.</summary>
        public void ApplyPendingItems()
        {
            if (GameManager.instance == null) return;
            while (pendingKeyItems.TryDequeue(out KeyItem keyItem))
            {
                try
                {
                    GrantGuard = true;
                    // Same pattern the game uses in GameManager.TradeEggball (line ~6035):
                    // popup, then ObtainKeyItem (which no-ops if already obtained).
                    GameManager.TriggerItemObtainPopup(keyItem);
                    GameManager.instance.ObtainKeyItem(keyItem, false);
                    log.LogInfo($"[Grunnchipelago] Granted {keyItem}.");
                }
                catch (Exception e)
                {
                    log.LogError($"[Grunnchipelago] Failed to grant {keyItem}: {e.Message}");
                }
                finally
                {
                    GrantGuard = false;
                }
            }
        }

        public void Disconnect()
        {
            try { session?.Socket?.DisconnectAsync(); }
            catch (Exception) { }
            Connected = false;
        }
    }
}
