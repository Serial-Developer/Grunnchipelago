using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Grunnchipelago.Client
{
    /// <summary>
    /// In-game UI (playtest H + round 2): built on our OWN screen-space overlay canvas
    /// (sortingOrder 5000, above every game canvas) instead of cloning game elements -
    /// round 2 showed clones inherit donor rects/states and never displayed reliably.
    /// The game's own TMP font asset (taken from UIManager.titleText) keeps the look:
    /// - red "ARCHIPELAGO" under the main-menu title (visible whenever the mod runs);
    /// - stats panel, top-right of the PAUSE menu only (visible while connected);
    /// - "ESC : skip" hint during ending NPC dialogues.
    /// </summary>
    internal static class ModUi
    {
        private static Canvas canvas;
        private static TextMeshProUGUI titleMarker;
        private static TextMeshProUGUI statsPanel;
        private static TextMeshProUGUI escHint;

        public static void Tick(ApClient ap, bool statsShowAllLines)
        {
            UIManager ui = UIManager.instance;
            if (ui == null || ui.titleText == null) return;
            if (canvas == null && !CreateUi(ui)) return;

            // Title marker: on the title screen, following the vanilla title's fade.
            bool showTitle = GameManager.CurGameState == GameManager.GameState.Title
                             && ui.titleText.enabled && ui.titleText.gameObject.activeInHierarchy;
            if (titleMarker.enabled != showTitle) titleMarker.enabled = showTitle;

            // Stats panel: PAUSE menu only (Tab removed on request), while connected.
            bool showStats = ap.Connected
                && GameManager.CurGameState == GameManager.GameState.Paused
                && UIManager.curPausedState == PausedState.Default;
            if (statsPanel.enabled != showStats) statsPanel.enabled = showStats;
            if (showStats) statsPanel.SetText(Effects.BuildStatsText(statsShowAllLines));

            // "ESC : skip" during ending NPC dialogues (skip = EscSkipsEndingDialoguePatch).
            bool showHint = Plugin.EndingDialogueActive;
            if (escHint.enabled != showHint) escHint.enabled = showHint;
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

            // Same italic style as the vanilla "<i>GRUNN</i>" title.
            titleMarker = MakeText(root.transform, "titleMarker", font,
                anchor: new Vector2(0.5f, 1f), pivot: new Vector2(0.5f, 1f),
                position: new Vector2(0f, -300f), size: new Vector2(1600f, 160f),
                fontSize: 96f, TextAlignmentOptions.Center, Color.red);
            titleMarker.SetText("<i>ARCHIPELAGO</i>");

            statsPanel = MakeText(root.transform, "statsPanel", font,
                anchor: new Vector2(1f, 1f), pivot: new Vector2(1f, 1f),
                position: new Vector2(-40f, -40f), size: new Vector2(700f, 600f),
                fontSize: 34f, TextAlignmentOptions.TopRight, Color.white);

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
