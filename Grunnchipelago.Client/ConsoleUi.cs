using System;
using System.Collections.Generic;
using UnityEngine;

namespace Grunnchipelago.Client
{
    /// <summary>
    /// IN-GAME ARCHIPELAGO CONSOLE (demande Jonath 2026-08-01).
    ///
    /// Bottom-right overlay, IN GAME ONLY, showing what the server says: item sends, hints,
    /// chat, command results - everything MessageLog reports (ApClient.OnServerMessage).
    /// Very low opacity so the game stays readable behind it, and it sits ABOVE the game's
    /// own prompt box rather than over it.
    ///
    ///   F1   focus / unfocus - the ONLY toggle. Focused, it takes the keyboard and an
    ///        input line appears: type a server command (!hint, !missing, ...) or plain
    ///        chat, Enter sends. Unfocused it is passive - the game plays normally.
    ///        Escape is left alone on purpose: the game opens its pause menu with it.
    ///
    /// While focused the game's input pipeline is muted (InputCapturePatch, shared with the
    /// title-screen panel), so typing never drives the player character.
    /// </summary>
    internal static class ConsoleUi
    {
        private const float RefWidth = 1920f;
        private const float RefHeight = 1080f;

        // Bottom-right, flush with the bottom edge on a hair of margin [J 2026-08-01].
        private const float PanelWidth = 620f;
        private const float PanelHeight = 260f;
        private const float MarginRight = 20f;
        private const float MarginBottom = 12f;

        private const int MaxLines = 200;
        private const int VisibleLines = 8;

        private const KeyCode ToggleKey = KeyCode.F1;

        private static readonly List<string> lines = new List<string>();
        private static readonly object gate = new object();

        private static string input = "";
        private static bool focusPending;

        /// <summary>Frame on which Tick already acted on the toggle key. Update runs BEFORE
        /// OnGUI, so without this the IMGUI fallback below would close the console on the
        /// very press that just opened it.</summary>
        private static int toggledOnFrame = -1;
        private static Vector2 scroll;

        /// <summary>Last position the ScrollView actually accepted. Push parks scroll.y at
        /// float.MaxValue to mean "bottom", which is not a number we can scroll away FROM -
        /// this is the real one to work off.</summary>
        private static float clampedScrollY;

        /// <summary>True while the view follows the newest line. Cleared as soon as the
        /// player scrolls up: an arriving message must not yank the history out from under
        /// them mid-read [J 2026-08-01].</summary>
        private static bool pinnedToBottom = true;

        private const float LineHeight = 22f;

        /// <summary>True while the console owns the keyboard.</summary>
        public static bool Focused { get; private set; }

        /// <summary>Called from the socket thread by ApClient - hence the lock.</summary>
        public static void Push(string line)
        {
            if (string.IsNullOrEmpty(line)) return;
            lock (gate)
            {
                lines.Add(line);
                if (lines.Count > MaxLines) lines.RemoveRange(0, lines.Count - MaxLines);
            }
            if (pinnedToBottom) scroll.y = float.MaxValue;   // stick to the newest line
        }

        /// <summary>Drop the previous multiworld's traffic.</summary>
        public static void Clear()
        {
            lock (gate) lines.Clear();
            input = "";
            Focused = false;
            scroll = Vector2.zero;
            pinnedToBottom = true;
        }

        private static bool InGame =>
            GameManager.CurGameState == GameManager.GameState.Game;

        /// <summary>Read from Plugin.Update: the toggle must work while unfocused, i.e.
        /// outside the IMGUI event stream.</summary>
        public static void Tick()
        {
            if (!InGame)
            {
                Focused = false;   // never leave the game with muted inputs
                return;
            }
            ApClient ap = Plugin.Ap;
            if (ap == null || !ap.Connected) { Focused = false; return; }

            if (Input.GetKeyDown(ToggleKey))
            {
                Focused = !Focused;
                if (Focused) focusPending = true;
                toggledOnFrame = Time.frameCount;
            }
        }

        public static void Draw()
        {
            ApClient ap = Plugin.Ap;
            if (ap == null || !ap.Connected || !InGame) return;

            float scale = Mathf.Min(Screen.width / RefWidth, Screen.height / RefHeight);
            Matrix4x4 previous = GUI.matrix;
            Color previousColor = GUI.color;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));

            float viewWidth = Screen.width / scale;
            float viewHeight = Screen.height / scale;
            float height = PanelHeight + (Focused ? 34f : 0f);
            var rect = new Rect(viewWidth - PanelWidth - MarginRight,
                                viewHeight - height - MarginBottom,
                                PanelWidth, height);

            // Faint while passive, a bit more present once it has the keyboard.
            GUI.color = new Color(1f, 1f, 1f, Focused ? 0.92f : 0.42f);
            GUILayout.BeginArea(rect, GUI.skin.box);

            GUILayout.Label(Focused
                ? $"<b>ARCHIPELAGO</b>  <color=#9FD8FF>Entree : envoyer  ·  PgPrec/PgSuiv : defiler"
                  + $"  ·  [{ToggleKey}] : fermer</color>"
                : $"<b>ARCHIPELAGO</b>  <color=#9FD8FF>[{ToggleKey}]</color>", Rich());

            if (Focused) HandleScrollInput();
            float requested = scroll.y;
            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(VisibleLines * LineHeight));
            clampedScrollY = scroll.y;
            // Asked to go further than the content allows: we are against the bottom edge.
            if (requested > scroll.y + 0.5f) pinnedToBottom = true;
            lock (gate)
            {
                foreach (string line in lines) GUILayout.Label(line, Rich());
            }
            GUILayout.EndScrollView();

            if (Focused)
            {
                // Enter sends. Escape is deliberately NOT handled: the game opens its pause
                // menu on that key through a path of its own, so closing the console with it
                // also paused the game [J 2026-08-01]. F1 is the single toggle.
                if (Event.current != null && Event.current.type == EventType.KeyDown)
                {
                    if (Event.current.keyCode == KeyCode.Return
                        || Event.current.keyCode == KeyCode.KeypadEnter)
                    {
                        Send(ap);
                        Event.current.Use();
                    }
                    // F1 closes from INSIDE the input line too. The focused TextField owns
                    // the keyboard control, so Tick's poll fired while the field kept the
                    // focus and the console reopened on the same press - it took unfocusing
                    // first [J 2026-08-01]. Handled here, the key never reaches the field.
                    else if (Event.current.keyCode == ToggleKey && Time.frameCount != toggledOnFrame)
                    {
                        Focused = false;
                        GUI.FocusControl(null);
                        Event.current.Use();
                    }
                }

                GUI.SetNextControlName("apConsoleInput");
                input = GUILayout.TextField(input ?? "");
                if (focusPending)
                {
                    GUI.FocusControl("apConsoleInput");
                    focusPending = false;
                }
            }

            GUILayout.EndArea();
            GUI.color = previousColor;
            GUI.matrix = previous;
        }

        /// <summary>Scroll the history. The wheel is handled by hand rather than left to the
        /// ScrollView: in game the cursor is LOCKED at the screen centre (CursorUnlock only
        /// frees it on the title screen), so IMGUI never considers the pointer to be over the
        /// view and the built-in wheel handling never fires. Page keys cover the same ground
        /// without a mouse at all - Grunn is a keyboard game [J 2026-08-01].</summary>
        private static void HandleScrollInput()
        {
            Event e = Event.current;
            if (e == null) return;

            if (e.type == EventType.ScrollWheel)
            {
                ScrollBy(e.delta.y * LineHeight);
                e.Use();
                return;
            }
            if (e.type != EventType.KeyDown) return;

            switch (e.keyCode)
            {
                case KeyCode.PageUp:
                    ScrollBy(-VisibleLines * LineHeight);
                    e.Use();
                    break;
                case KeyCode.PageDown:
                    ScrollBy(VisibleLines * LineHeight);
                    e.Use();
                    break;
                case KeyCode.Home:
                    scroll.y = 0f;
                    pinnedToBottom = false;
                    e.Use();
                    break;
                case KeyCode.End:
                    scroll.y = float.MaxValue;
                    pinnedToBottom = true;
                    e.Use();
                    break;
            }
        }

        private static void ScrollBy(float amount)
        {
            float from = scroll.y > 1e6f ? clampedScrollY : scroll.y;
            scroll.y = Mathf.Max(0f, from + amount);
            if (amount < 0f) pinnedToBottom = false;
        }

        private static void Send(ApClient ap)
        {
            string text = (input ?? "").Trim();
            if (text.Length == 0) return;
            input = "";
            focusPending = true;
            // Echo locally: the server does not reply to every command, and without this a
            // typo looks like the console swallowed the line.
            Push("> " + text);
            if (!ap.Say(text)) Push("<color=#FF8080>envoi impossible (deconnecte ?)</color>");
        }

        private static GUIStyle rich;

        private static GUIStyle Rich()
        {
            if (rich == null)
                rich = new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true, fontSize = 15 };
            return rich;
        }
    }
}
