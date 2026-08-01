using System;
using HarmonyLib;
using UnityEngine;

namespace Grunnchipelago.Client
{
    /// <summary>
    /// TITLE-SCREEN CONNECTION PANEL (demande Jonath 2026-07-31).
    ///
    /// Small floating box on the right of the title screen, just above the version credit:
    /// Host / Port / Slot Name / Password + Connect. Before this, joining a multiworld meant
    /// editing BepInEx/config/grunnchipelago.client.cfg by hand and restarting the game.
    ///
    /// MOUSE + KEYBOARD. Grunn hides and locks the cursor everywhere
    /// (GameManager.HideCursor, GameManager.cs:1156-1167), so the mod frees it - on the TITLE
    /// SCREEN ONLY (CursorUnlock). In game the cursor stays locked exactly as vanilla.
    /// Keyboard still works throughout, for whoever prefers it:
    ///   Up/Down   move between fields, Tab does the same
    ///   Tab       next field (Shift+Tab: previous)
    ///   Enter     next field, and Connect from the last one
    ///   Esc       give the keyboard back to the menu
    /// Clicking a field also focuses it. While a field holds the keyboard the game's input
    /// pipeline is muted (InputCapturePatch), so typing a slot name never triggers the title
    /// screen's bindings - "E" being confirm.
    ///
    /// Drawn with IMGUI: text INPUT is the whole point, and GUI.TextField gives working
    /// editing, caret and selection for free, where a TMP_InputField must be assembled by
    /// hand against a canvas built at runtime. Coordinates are authored in a 1920x1080 space
    /// and scaled through GUI.matrix, so the panel stays aligned with ModUi's credit, which
    /// uses the same reference resolution.
    ///
    /// TITLE SCREEN ONLY - and that is what makes it safe for saves: SaveProfile only swaps
    /// the save prefix on the title screen, before any world load, so connecting from here
    /// always lands the player on the right profile BEFORE they load anything.
    /// </summary>
    internal static class ConnectUi
    {
        private const float RefWidth = 1920f;
        private const float RefHeight = 1080f;

        // Panel geometry in reference pixels. ModUi's credit sits 205 px off the bottom and
        // is ~100 px tall, so the panel bottom stays clear of it.
        private const float PanelWidth = 380f;
        private const float PanelHeight = 250f;
        private const float MarginRight = 40f;
        private const float MarginBottom = 330f;

        private static readonly string[] FieldNames = { "apHost", "apPort", "apSlot", "apPass" };
        private static readonly string[] FieldLabels = { "Host", "Port", "Slot Name", "Password" };

        private static readonly string[] values = { "", "", "", "" };
        private static int field;
        private static bool loaded;
        private static bool focusPending;
        private const string ConnectingMessage = "Connexion...";

        /// <summary>How long "Connexion..." may stay up before it is called a failure. The
        /// auto-reconnect loop retries every 5 s, so 10 s covers a full attempt plus one
        /// retry - long enough not to cry wolf on a slow server, short enough that a typo in
        /// the slot name does not leave the panel hanging [J 2026-08-01]. A refused login
        /// usually reports itself well before this, through LastConnectError.</summary>
        private const float ConnectTimeoutSeconds = 10f;

        private static string feedback = "";
        private static float connectStartedAt = -1f;

        /// <summary>True while the panel owns the keyboard. The game's input pipeline is
        /// muted meanwhile (InputCapturePatch).</summary>
        public static bool CapturingKeyboard { get; private set; }

        /// <summary>Set by Plugin: reads/writes the BepInEx config, so what is typed here is
        /// remembered across launches and the auto-reconnect loop uses the new values.</summary>
        public static Func<(string host, int port, string slot, string password)> Load;
        public static Action<string, int, string, string> Save;

        /// <summary>The MAIN MENU proper - not the Sokpop splash or the loading screens that
        /// precede it [J 2026-08-01]. GameState is already Title during those, so the state
        /// alone is not enough: the vanilla title TEXT is what actually appears with the
        /// menu, and ModUi uses the very same test to fade its credit in.</summary>
        private static bool OnTitle
        {
            get
            {
                if (GameManager.CurGameState != GameManager.GameState.Title) return false;
                // The game's own rule for its title UI (showUI, UIManager.cs:1543-1551).
                // Without it a transition still counted as "on the title" and the panel -
                // cursor included - flickered during the splash [J 2026-08-01, reported on
                // the credit line, same condition].
                if (GameManager.BlackScreen || GameManager.SwitchingState) return false;
                UIManager ui = UIManager.instance;
                return ui != null && ui.titleText != null
                       && ui.titleText.enabled && ui.titleText.gameObject.activeInHierarchy;
            }
        }

        /// <summary>Called from Plugin.Update. Frees the cursor while the title screen is
        /// up so the panel can be clicked, and re-locks it on the way out.</summary>
        public static void Tick()
        {
            if (!OnTitle)
            {
                // Leaving the title screen must never strand the game with muted inputs or
                // a stray cursor.
                if (CapturingKeyboard) CapturingKeyboard = false;
                CursorUnlock.Restore();
                return;
            }
            CursorUnlock.Free();
        }

        public static void Draw()
        {
            ApClient ap = Plugin.Ap;
            if (ap == null || !OnTitle) return;

            if (!loaded && Load != null)
            {
                var cfg = Load();
                values[0] = cfg.host ?? "";
                values[1] = cfg.port.ToString();
                values[2] = cfg.slot ?? "";
                values[3] = cfg.password ?? "";
                loaded = true;
            }

            HandleNavigation(ap);

            float scale = Mathf.Min(Screen.width / RefWidth, Screen.height / RefHeight);
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));

            // With a uniform scale the visible area can be wider (or taller) than the
            // reference, so anchor to the REAL edge rather than the reference one.
            float viewWidth = Screen.width / scale;
            float viewHeight = Screen.height / scale;
            var rect = new Rect(viewWidth - PanelWidth - MarginRight,
                                viewHeight - PanelHeight - MarginBottom,
                                PanelWidth, PanelHeight);

            UpdateConnectFeedback(ap);

            GUILayout.BeginArea(rect, GUI.skin.box);
            GUILayout.Label(ap.Connected
                ? "<b>ARCHIPELAGO</b>  <color=#7CFC7C>connecte</color>"
                : "<b>ARCHIPELAGO</b>", Rich());

            for (int i = 0; i < FieldNames.Length; i++) DrawField(i);

            GUILayout.Space(4f);

            // ONE button, always "Connect" [J 2026-08-01]. Nobody wants to disconnect for
            // its own sake: you either connect, or you point the fields at another room and
            // connect again - which drops the previous session on the way.
            if (GUILayout.Button("Connect")) Connect(ap);

            if (!string.IsNullOrEmpty(feedback))
                GUILayout.Label($"<color=#FFD37C>{feedback}</color>", Rich());

            GUILayout.EndArea();
            GUI.matrix = previous;

            // A field holding the caret means the panel owns the keyboard: mute the game.
            string focusedControl = GUI.GetNameOfFocusedControl();
            CapturingKeyboard = false;
            for (int i = 0; i < FieldNames.Length; i++)
            {
                if (focusedControl != FieldNames[i]) continue;
                CapturingKeyboard = true;
                field = i;
                break;
            }
        }

        /// <summary>Resolve a pending "Connexion..." into success, a reported reason, or a
        /// timeout - so the panel never sits on a message that no longer means anything.</summary>
        private static void UpdateConnectFeedback(ApClient ap)
        {
            if (connectStartedAt < 0f) return;

            if (ap.Connected)
            {
                connectStartedAt = -1f;
                if (feedback == ConnectingMessage) feedback = "";
                return;
            }

            // The server said no (bad slot, wrong password, unknown game...): say why.
            string reported = ap.LastConnectError;
            if (!string.IsNullOrEmpty(reported))
            {
                connectStartedAt = -1f;
                feedback = "Echec : " + reported;
                return;
            }

            if (Time.realtimeSinceStartup - connectStartedAt >= ConnectTimeoutSeconds)
            {
                connectStartedAt = -1f;
                feedback = "Echec de la connexion - verifie l'adresse, le port et le slot.";
            }
        }

        private static void DrawField(int index)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(FieldLabels[index], Rich(), GUILayout.Width(100f));
            GUI.SetNextControlName(FieldNames[index]);
            values[index] = index == 3
                ? GUILayout.PasswordField(values[index] ?? "", '*')
                : GUILayout.TextField(values[index] ?? "");
            if (field == index && focusPending)
            {
                GUI.FocusControl(FieldNames[index]);
                focusPending = false;
            }
            GUILayout.EndHorizontal();
        }

        /// <summary>Keyboard navigation, read from the IMGUI event stream so it is consumed
        /// before the TextField sees it.</summary>
        private static void HandleNavigation(ApClient ap)
        {
            Event e = Event.current;
            if (e == null || e.type != EventType.KeyDown) return;

            // IMGUI reports Tab twice: once as a keyCode, once as a bare '	' character
            // event. Swallow the second one or the tab lands IN the field as text.
            if (e.keyCode == KeyCode.None && e.character == '	') { e.Use(); return; }

            switch (e.keyCode)
            {
                case KeyCode.Escape:
                    GUI.FocusControl(null);
                    CapturingKeyboard = false;
                    e.Use();
                    break;
                case KeyCode.Tab:
                    // Tab walks the form, Shift+Tab walks it back [J 2026-08-01].
                    Move(e.shift ? -1 : 1);
                    e.Use();
                    break;
                case KeyCode.DownArrow:
                    Move(1);
                    e.Use();
                    break;
                case KeyCode.UpArrow:
                    Move(-1);
                    e.Use();
                    break;
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    // Enter walks down the form, and connects from the last field.
                    if (field < FieldNames.Length - 1) Move(1);
                    else Connect(ap);
                    e.Use();
                    break;
            }
        }

        private static void Move(int delta)
        {
            field = (field + delta + FieldNames.Length) % FieldNames.Length;
            focusPending = true;
        }

        private static void Connect(ApClient ap)
        {
            string host = (values[0] ?? "").Trim();
            string portText = (values[1] ?? "").Trim();
            string slot = (values[2] ?? "").Trim();
            string password = values[3] ?? "";

            if (string.IsNullOrEmpty(host)) { feedback = "Renseigne une adresse."; field = 0; focusPending = true; return; }
            if (!int.TryParse(portText, out int port) || port <= 0 || port > 65535)
            {
                feedback = "Port invalide.";
                field = 1;
                focusPending = true;
                return;
            }
            if (string.IsNullOrEmpty(slot)) { feedback = "Renseigne un slot."; field = 2; focusPending = true; return; }

            values[0] = host;
            values[1] = port.ToString();
            values[2] = slot;

            // Persist FIRST: Plugin's auto-reconnect loop reads the config, so saving here
            // also makes every later retry use these values.
            Save?.Invoke(host, port, slot, password);
            feedback = ConnectingMessage;
            connectStartedAt = Time.realtimeSinceStartup;
            try
            {
                // Already in a room? Leave it first - Connect() refuses to run while a
                // session is live. Disconnect clears Connected synchronously and detaches
                // the old handlers, so the new attempt starts clean.
                if (ap.Connected) ap.Disconnect();
                // Wipe the previous room's traffic HERE, on the click. The deferred session
                // reset ran a frame or two AFTER login, by which time the server's welcome
                // batch had already arrived - and got wiped with it, leaving an empty console
                // on the second multiworld [J 2026-08-01]. An automatic reconnection does not
                // come through here, so its history is preserved.
                ConsoleUi.Clear();
                ap.Connect(host, port, slot, password);
                GUI.FocusControl(null);      // hand the keyboard back to the title screen
            }
            catch (Exception ex)
            {
                connectStartedAt = -1f;   // a synchronous failure needs no timeout
                feedback = "Echec : " + ex.Message;
            }
        }

        private static GUIStyle rich;

        private static GUIStyle Rich()
        {
            if (rich == null) rich = new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true };
            return rich;
        }
    }

    /// <summary>Mutes the game's input pipeline while the panel owns the keyboard.
    /// InputManager.HandleInput is the single funnel every binding goes through
    /// (InputManager.cs:575), so one prefix covers the lot - without it, typing a slot name
    /// would fire the title screen's own actions, "E" being confirm.</summary>
    [HarmonyPatch(typeof(InputManager), "HandleInput")]
    public static class InputCapturePatch
    {
        private static bool Prefix() => !ConnectUi.CapturingKeyboard && !ConsoleUi.Focused;
    }
}
