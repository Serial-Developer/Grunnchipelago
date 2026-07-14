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
        private static TextMeshProUGUI escHint;

        /// <summary>Clone a UI text element WITHOUT ever letting its game scripts run:
        /// the clone is created under an inactive RectTransform holder (no Awake - see
        /// the UpdateManager freeze note in ModelSwap.SwapVisual), game scripts are
        /// DestroyImmediate'd (UI stack kept: TMPro / UnityEngine namespaces), then the
        /// holder is activated. Returns the cloned TMP component, or null.</summary>
        private static TextMeshProUGUI CloneTextElement(TextMeshProUGUI source, string name)
        {
            var holder = new GameObject(name, typeof(RectTransform));
            holder.SetActive(false);   // BEFORE receiving children: no Awake ever fires
            var holderRect = (RectTransform)holder.transform;
            holderRect.SetParent(source.transform.parent, false);
            holderRect.anchorMin = Vector2.zero;
            holderRect.anchorMax = Vector2.one;
            holderRect.offsetMin = Vector2.zero;
            holderRect.offsetMax = Vector2.zero;

            GameObject clone = Object.Instantiate(source.gameObject, holder.transform);
            clone.name = name + "_text";
            foreach (MonoBehaviour behaviour in clone.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour == null) continue;
                string ns = behaviour.GetType().Namespace ?? "";
                if (ns.StartsWith("TMPro") || ns.StartsWith("UnityEngine")) continue;   // keep the UI stack
                try { Object.DestroyImmediate(behaviour); } catch (System.Exception) { }
            }
            TextMeshProUGUI text = clone.GetComponent<TextMeshProUGUI>();
            if (text == null) { Object.Destroy(holder); return null; }
            // "ARCHIPELAGO" is longer than the donor text: without these, TMP wraps or
            // truncates inside the donor-sized rect and nothing shows at all.
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Overflow;
            text.enableAutoSizing = false;
            // Render above siblings (e.g. the pause-menu backdrop on the same canvas).
            holder.transform.SetAsLastSibling();
            clone.SetActive(true);
            holder.SetActive(true);
            Plugin.Log?.LogInfo($"[Grunnchipelago] UI clone '{name}': parent={source.transform.parent?.name}, " +
                                $"anchors={text.rectTransform.anchorMin}-{text.rectTransform.anchorMax}, " +
                                $"pos={text.rectTransform.anchoredPosition}, size={text.rectTransform.sizeDelta}");
            return text;
        }

        public static void Tick(ApClient ap, bool statsShowAllLines)
        {
            UIManager ui = UIManager.instance;
            if (ui == null) return;
            TickTitleMarker(ui);
            TickStatsPanel(ui, ap, statsShowAllLines);
            TickEscHint(ui);
        }

        /// <summary>"ESC : skip" hint while an ending NPC dialogue is running (the actual
        /// skip is the ToGameState patch: Escape ends the dialogue instead of pausing).</summary>
        private static void TickEscHint(UIManager ui)
        {
            if (ui.newDayText == null) return;
            if (escHint == null)
            {
                escHint = CloneTextElement(ui.newDayText, "grunnchipelago_escHint");
                if (escHint == null) return;
                RectTransform rect = escHint.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = new Vector2(0f, 40f);
                rect.sizeDelta = new Vector2(800f, 80f);
                escHint.fontSize = ui.newDayText.fontSize * 0.3f;
                escHint.alignment = TextAlignmentOptions.Bottom;
                escHint.color = new Color(1f, 1f, 1f, 0.85f);
                escHint.SetText("ESC : skip");
            }
            bool show = Plugin.EndingDialogueActive;
            if (escHint.enabled != show) escHint.enabled = show;
        }

        // ---------- H.1 title marker ----------

        private static void TickTitleMarker(UIManager ui)
        {
            if (ui.titleText == null) return;
            if (titleMarker == null)
            {
                titleMarker = CloneTextElement(ui.titleText, "grunnchipelago_titleMarker");
                if (titleMarker == null) return;
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
                statsPanel = CloneTextElement(ui.newDayText, "grunnchipelago_statsPanel");
                if (statsPanel == null) return;
                RectTransform rect = statsPanel.rectTransform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
                rect.anchoredPosition = new Vector2(-30f, -30f);
                rect.sizeDelta = new Vector2(700f, 500f);
                statsPanel.fontSize = ui.newDayText.fontSize * 0.35f;
                statsPanel.alignment = TextAlignmentOptions.TopRight;
                statsPanel.color = Color.white;
                Plugin.Log?.LogInfo("[Grunnchipelago] Stats panel created.");
            }

            // Pause menu only (Tab removed on Jonath's request, round 2), while connected.
            bool show = ap.Connected
                && GameManager.CurGameState == GameManager.GameState.Paused
                && UIManager.curPausedState == PausedState.Default;
            if (statsPanel.enabled != show) statsPanel.enabled = show;
            if (show) statsPanel.SetText(Effects.BuildStatsText(showAllLines));
        }
    }
}
