using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Grunnchipelago.Client
{
    /// <summary>
    /// In-game UI (playtest H + round 2, session 2): built on our OWN screen-space
    /// overlay canvas (sortingOrder 5000, above every game canvas) instead of cloning
    /// game elements - round 2 showed clones inherit donor rects/states and never
    /// displayed reliably. The game's own TMP font asset (from UIManager.titleText)
    /// keeps the look:
    /// - stats panel, below the day/time block of the PAUSE menu (while connected);
    /// - unlocked-item panel, LEFT of the ending polaroid (session 2, 1.3);
    /// - "ESC : skip" hint during ending NPC dialogues.
    /// The main-menu title itself becomes GRUNNCHIPELAGO via TitleTextPatch (1.1).
    /// </summary>
    internal static class ModUi
    {
        private static Canvas canvas;
        private static TextMeshProUGUI statsPanel;
        private static TextMeshProUGUI escHint;
        private static TextMeshProUGUI endingItemPanel;

        // Ending whose reward text is currently built (rebuilt when it changes).
        private static EndingType? endingShown;

        public static void Tick(ApClient ap, bool statsShowAllLines)
        {
            UIManager ui = UIManager.instance;
            if (ui == null || ui.titleText == null) return;
            if (canvas == null && !CreateUi(ui)) return;

            // Stats panel: PAUSE menu only (Tab removed on request), while connected.
            bool showStats = ap.Connected
                && GameManager.CurGameState == GameManager.GameState.Paused
                && UIManager.curPausedState == PausedState.Default;
            if (statsPanel.enabled != showStats) statsPanel.enabled = showStats;
            if (showStats) statsPanel.SetText(Effects.BuildStatsText(statsShowAllLines));

            // "ESC : skip" during ending NPC dialogues (skip = EscSkipsEndingDialoguePatch).
            bool showHint = Plugin.EndingDialogueActive;
            if (escHint.enabled != showHint) escHint.enabled = showHint;

            TickEndingItem(ap, ui);
        }

        /// <summary>Session 2, 1.3 - on the ending polaroid screen, show LEFT of the
        /// polaroid what the ending's check unlocked (scouted content). Visible exactly
        /// while the vanilla ending polaroid is up: EndingState.Start
        /// (UIManager.EndingScreenLogic) and polaroidRead shown (Read.Trigger, fired by
        /// GameManager.TriggerEndingPolaroid - GameManager.cs:2626).</summary>
        private static void TickEndingItem(ApClient ap, UIManager ui)
        {
            bool show = ap.Connected
                && GameManager.CurGameState == GameManager.GameState.Ending
                && GameManager.curEndingState == GameManager.EndingState.Start
                && GameManager.endingTypeTriggered != EndingType.DemoEnding
                && ui.polaroidRead != null && ui.polaroidRead.curState == ReadState.Show;

            if (show)
            {
                EndingType ending = GameManager.endingTypeTriggered;
                if (endingShown != ending)
                {
                    endingShown = ending;
                    endingItemPanel.SetText(ap.DescribeEndingReward(ending) ?? "");
                }
                show = endingItemPanel.text.Length > 0;
            }
            else
            {
                endingShown = null;   // re-watching the same ending rebuilds the text
            }
            if (endingItemPanel.enabled != show) endingItemPanel.enabled = show;
        }

        private static bool CreateUi(UIManager ui)
        {
            TMP_FontAsset font = ui.titleText.font;
            if (font == null) return false;

            var root = new GameObject("grunnchipelago_canvas");
            Object.DontDestroyOnLoad(root);
            canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;   // above every game canvas (default order 0)
            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            // Below the pause menu's day/time block (timePausedText, top-right,
            // UIManager.cs:3510) so it no longer covers "samedi 08:00" (session 2, 1.2).
            statsPanel = MakeText(root.transform, "statsPanel", font,
                anchor: new Vector2(1f, 1f), pivot: new Vector2(1f, 1f),
                position: new Vector2(-40f, -230f), size: new Vector2(700f, 600f),
                fontSize: 34f, TextAlignmentOptions.TopRight, Color.white);

            // Left of the centered ending polaroid (session 2, 1.3, iteration 3):
            // the polaroid CARD renders above our canvas and its left edge sits at
            // ~-420 from center (capture Jonath) - the column ends at -450 so the
            // text stays fully visible. Word wrap inside the box + auto-size bounded
            // down when a long item name needs it.
            endingItemPanel = MakeText(root.transform, "endingItemPanel", font,
                anchor: new Vector2(0.5f, 0.5f), pivot: new Vector2(1f, 0.5f),
                position: new Vector2(-450f, 0f), size: new Vector2(420f, 400f),
                fontSize: 30f, TextAlignmentOptions.Right, Color.white);
            endingItemPanel.enableWordWrapping = true;
            endingItemPanel.overflowMode = TextOverflowModes.Truncate;
            endingItemPanel.enableAutoSizing = true;
            endingItemPanel.fontSizeMax = 30f;
            endingItemPanel.fontSizeMin = 18f;

            escHint = MakeText(root.transform, "escHint", font,
                anchor: new Vector2(0.5f, 0f), pivot: new Vector2(0.5f, 0f),
                position: new Vector2(0f, 40f), size: new Vector2(800f, 60f),
                fontSize: 30f, TextAlignmentOptions.Bottom, new Color(1f, 1f, 1f, 0.85f));
            escHint.SetText("ESC : skip");

            Plugin.Log?.LogInfo("[Grunnchipelago] Overlay canvas created (own canvas, order 5000).");
            return true;
        }

        private static TextMeshProUGUI MakeText(Transform parent, string name, TMP_FontAsset font,
            Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size,
            float fontSize, TextAlignmentOptions alignment, Color color)
        {
            var go = new GameObject("grunnchipelago_" + name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var text = go.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;   // never intercept clicks
            text.enabled = false;          // Tick decides visibility
            return text;
        }
    }
}
