using TMPro;
using UnityEngine;

namespace Grunnchipelago.Client
{
    /// <summary>
    /// playtest H - in-game UI additions, built by cloning existing TextMeshProUGUI
    /// elements so the game's own font/material are inherited:
    /// - H.1: a red "ARCHIPELAGO" marker under the main-menu title (clone of
    ///   UIManager.titleText, UIManager.cs:309), shown whenever the mod is enabled;
    /// - H.2: a stats panel in the top-right corner of the Tab/Pause menus (clone of
    ///   UIManager.newDayText, UIManager.cs:559), shown while connected.
    /// </summary>
    internal static class ModUi
    {
        private static TextMeshProUGUI titleMarker;
        private static TextMeshProUGUI statsPanel;

        public static void Tick(ApClient ap, bool statsShowAllLines)
        {
            UIManager ui = UIManager.instance;
            if (ui == null) return;
            TickTitleMarker(ui);
            TickStatsPanel(ui, ap, statsShowAllLines);
        }

        // ---------- H.1 title marker ----------

        private static void TickTitleMarker(UIManager ui)
        {
            if (ui.titleText == null) return;
            if (titleMarker == null)
            {
                GameObject clone = Object.Instantiate(ui.titleText.gameObject, ui.titleText.transform.parent);
                clone.name = "grunnchipelago_titleMarker";
                titleMarker = clone.GetComponent<TextMeshProUGUI>();
                if (titleMarker == null) { Object.Destroy(clone); return; }
                titleMarker.SetText("<i>ARCHIPELAGO</i>");   // same italic style as "<i>GRUNN</i>"
                titleMarker.color = Color.red;
                titleMarker.fontSize = ui.titleText.fontSize * 0.4f;
                RectTransform rect = titleMarker.rectTransform;
                RectTransform titleRect = ui.titleText.rectTransform;
                rect.anchoredPosition = titleRect.anchoredPosition
                    + new Vector2(0f, -Mathf.Max(60f, titleRect.sizeDelta.y * 0.55f));
                Plugin.Log?.LogInfo("[Grunnchipelago] Title marker created.");
            }
            // Mirror the vanilla title visibility (UIManager toggles the component).
            bool show = ui.titleText.enabled && ui.titleText.gameObject.activeInHierarchy;
            if (titleMarker.enabled != show) titleMarker.enabled = show;
        }

        // ---------- H.2 stats panel ----------

        private static void TickStatsPanel(UIManager ui, ApClient ap, bool showAllLines)
        {
            if (ui.newDayText == null) return;
            if (statsPanel == null)
            {
                GameObject clone = Object.Instantiate(ui.newDayText.gameObject, ui.newDayText.transform.parent);
                clone.name = "grunnchipelago_statsPanel";
                statsPanel = clone.GetComponent<TextMeshProUGUI>();
                if (statsPanel == null) { Object.Destroy(clone); return; }
                RectTransform rect = statsPanel.rectTransform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
                rect.anchoredPosition = new Vector2(-30f, -30f);
                rect.sizeDelta = new Vector2(700f, 500f);
                statsPanel.fontSize = ui.newDayText.fontSize * 0.35f;
                statsPanel.alignment = TextAlignmentOptions.TopRight;
                statsPanel.color = Color.white;
                Plugin.Log?.LogInfo("[Grunnchipelago] Stats panel created.");
            }

            // Tab (Inventory/Polaroids) and Pause (Default) menus only, while connected.
            bool show = ap.Connected
                && GameManager.CurGameState == GameManager.GameState.Paused
                && (UIManager.curPausedState == PausedState.Default
                    || UIManager.curPausedState == PausedState.Inventory
                    || UIManager.curPausedState == PausedState.Polaroids);
            if (statsPanel.enabled != show) statsPanel.enabled = show;
            if (show) statsPanel.SetText(Effects.BuildStatsText(showAllLines));
        }
    }
}
