using System.Collections.Generic;
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
        private static TextMeshProUGUI titleCredit;
        private static TextMeshProUGUI statsPanel;
        private static TextMeshProUGUI escHint;
        private static TextMeshProUGUI endingItemPanel;
        private static TextMeshProUGUI endingListPanel;

        // Ending whose reward text is currently built (rebuilt when it changes).
        private static EndingType? endingShown;

        /// <summary>Session 2, 3.2 - the 11 goal endings paired with their ending
        /// polaroid, which carries the game's own number and localized name
        /// (PolaroidManager.GetPolaroidData -> myIndex, PolaroidManager.cs:141) so the
        /// list reads exactly like the polaroids do ("+3. bus"). DemoEnding excluded.</summary>
        private static readonly KeyValuePair<EndingType, PolaroidType>[] EndingPolaroids =
        {
            new KeyValuePair<EndingType, PolaroidType>(EndingType.Mist, PolaroidType.EndingMist),
            new KeyValuePair<EndingType, PolaroidType>(EndingType.Bus, PolaroidType.EndingBus),
            new KeyValuePair<EndingType, PolaroidType>(EndingType.SacredFlowers, PolaroidType.EndingSacredFlowers),
            new KeyValuePair<EndingType, PolaroidType>(EndingType.Darkness, PolaroidType.EndingDarkness),
            new KeyValuePair<EndingType, PolaroidType>(EndingType.Drown, PolaroidType.EndingDrowned),
            new KeyValuePair<EndingType, PolaroidType>(EndingType.LongHallway, PolaroidType.EndingLongHallway),
            new KeyValuePair<EndingType, PolaroidType>(EndingType.HedgeMaze, PolaroidType.EndingHedgeMaze),
            new KeyValuePair<EndingType, PolaroidType>(EndingType.WorldEnd, PolaroidType.EndingWorldEnd),
            new KeyValuePair<EndingType, PolaroidType>(EndingType.GoodEnd, PolaroidType.EndingGoodEnd),
            new KeyValuePair<EndingType, PolaroidType>(EndingType.Dog, PolaroidType.EndingDog),
            new KeyValuePair<EndingType, PolaroidType>(EndingType.Picnic, PolaroidType.EndingPicnic),
        };

        /// <summary>How long the title has to hold before the credit is drawn. The real
        /// menu stays up indefinitely, so this is invisible there.</summary>
        private const float CreditSettleSeconds = 0.35f;

        private static float titleUpSince = -1f;

        /// <summary>True once the title has been continuously up for CreditSettleSeconds.</summary>
        private static bool SettledOnTitle(bool titleUp)
        {
            float now = Time.realtimeSinceStartup;
            if (!titleUp)
            {
                titleUpSince = -1f;
                return false;
            }
            if (titleUpSince < 0f) titleUpSince = now;
            return now - titleUpSince >= CreditSettleSeconds;
        }

        public static void Tick(ApClient ap, bool statsShowAllLines)
        {
            UIManager ui = UIManager.instance;
            if (ui == null || ui.titleText == null) return;
            if (canvas == null && !CreateUi(ui)) return;

            // Mod credit: title screen only. Two conditions on top of "the title is up":
            //
            //  - the game's OWN rule for its title UI, showUI = !BlackScreen &&
            //    !SwitchingState (UIManager.cs:1543-1551). We never applied it, and our
            //    canvas sits at sortingOrder 5000 - above the splash and the loading screen -
            //    so a title state the game was busy covering still painted our credit;
            //  - a short settle delay. The flash is over in a few frames [2026-08-01:
            //    "flash tres rapidement entre l'ecran sokpop et le chargement"], so anything
            //    that brief is a transition artefact, not a title screen.
            bool titleUp = GameManager.CurGameState == GameManager.GameState.Title
                           && ui.titleText.enabled && ui.titleText.gameObject.activeInHierarchy
                           && !GameManager.BlackScreen && !GameManager.SwitchingState;
            bool showCredit = SettledOnTitle(titleUp);
            if (titleCredit.enabled != showCredit) titleCredit.enabled = showCredit;

            // Stats panel: PAUSE menu only (Tab removed on request), while connected.
            bool showStats = ap.Connected
                && GameManager.CurGameState == GameManager.GameState.Paused
                && UIManager.curPausedState == PausedState.Default;
            if (statsPanel.enabled != showStats) statsPanel.enabled = showStats;
            if (showStats) statsPanel.SetText(Effects.BuildStatsText(statsShowAllLines));

            // "ESC : skip" during the post-death orb sequence (skip = HandleSkipOrbDialogue).
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
                    endingListPanel.SetText(BuildEndingList(ap));
                }
            }
            else
            {
                endingShown = null;   // re-watching the same ending rebuilds the text
            }

            bool showItem = show && endingItemPanel.text.Length > 0;
            if (endingItemPanel.enabled != showItem) endingItemPanel.enabled = showItem;
            if (endingListPanel.enabled != show) endingListPanel.enabled = show;
        }

        /// <summary>Session 2, 3.2 - the 11 endings, right of the polaroid: those whose
        /// AP check is sent show the game's own number + localized name, the others
        /// "???". Session state only - GlobalData.endingTypesSeen is meaningless here
        /// (a veteran save reads 11/11 on a brand-new seed).</summary>
        private static string BuildEndingList(ApClient ap)
        {
            var lines = new List<KeyValuePair<int, string>>();
            int found = 0;
            for (int i = 0; i < EndingPolaroids.Length; i++)
            {
                KeyValuePair<EndingType, PolaroidType> pair = EndingPolaroids[i];
                PolaroidData data = null;
                try { data = PolaroidManager.GetPolaroidData(pair.Value); }
                catch (System.Exception) { }

                // The game's own numbering is NOT the enum order (constate en jeu: the
                // list came out shuffled) - sort on myIndex so it reads 1..11.
                int number = i + 1;
                if (data != null && !int.TryParse(data.myIndex, out number)) number = i + 1;

                string label = "???";
                if (ap.EndingCheckSent(pair.Key))
                {
                    found++;
                    label = NameOf(pair.Value, data);
                }
                lines.Add(new KeyValuePair<int, string>(number, number + ". " + label));
            }
            lines.Sort((a, b) => a.Key.CompareTo(b.Key));

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<i>Fins</i>");
            foreach (KeyValuePair<int, string> line in lines) sb.AppendLine(line.Value);
            sb.AppendLine($"<i>{found} / {EndingPolaroids.Length}</i>");
            return sb.ToString();
        }

        /// <summary>Localized polaroid name, via the game's own string table.</summary>
        private static string NameOf(PolaroidType type, PolaroidData data)
        {
            try
            {
                // DefinePolaroidString gives "<sprite…>3. bus"; we lay the number out
                // ourselves, so strip everything up to the first ". ".
                string full = PolaroidManager.DefinePolaroidString(data, false, "", "", true);
                int cut = full.IndexOf(". ", System.StringComparison.Ordinal);
                if (cut >= 0 && cut + 2 < full.Length) return full.Substring(cut + 2);
                if (!string.IsNullOrEmpty(full)) return full;
            }
            catch (System.Exception) { }
            return type.ToString().Replace("Ending", "");
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

            // Mod credit (ce que je veux) - bottom-right of the title screen, in red,
            // italic like the vanilla title. Sits ABOVE the "E - sélectionner" prompt
            // box, whose top edge is ~190 px off the bottom (ma capture).
            titleCredit = MakeText(root.transform, "titleCredit", font,
                anchor: new Vector2(1f, 0f), pivot: new Vector2(1f, 0f),
                position: new Vector2(-40f, 205f), size: new Vector2(700f, 100f),
                fontSize: 28f, TextAlignmentOptions.BottomRight, new Color(0.9f, 0.15f, 0.15f));
            titleCredit.SetText($"<i>v{Plugin.ModVersion}\nmodded by Serial</i>");

            // Below the pause menu's day/time block (timePausedText, top-right,
            // UIManager.cs:3510) so it no longer covers "samedi 08:00" (session 2, 1.2).
            statsPanel = MakeText(root.transform, "statsPanel", font,
                anchor: new Vector2(1f, 1f), pivot: new Vector2(1f, 1f),
                position: new Vector2(-40f, -230f), size: new Vector2(700f, 600f),
                fontSize: 34f, TextAlignmentOptions.TopRight, Color.white);

            // Left of the centered ending polaroid (session 2, 1.3, iteration 3):
            // the polaroid CARD renders above our canvas and its left edge sits at
            // ~-420 from center (ma capture) - the column ends at -450 so the
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

            // Session 2, 3.2: the 11 endings, RIGHT of the polaroid. Starts at +450
            // from centre for the same reason the item panel ends at -450 - the
            // polaroid card renders above our canvas (ma capture, 1.3 iter 3).
            endingListPanel = MakeText(root.transform, "endingListPanel", font,
                anchor: new Vector2(0.5f, 0.5f), pivot: new Vector2(0f, 0.5f),
                position: new Vector2(450f, 0f), size: new Vector2(420f, 620f),
                fontSize: 26f, TextAlignmentOptions.Left, Color.white);
            endingListPanel.enableWordWrapping = false;
            endingListPanel.overflowMode = TextOverflowModes.Truncate;
            endingListPanel.enableAutoSizing = true;
            endingListPanel.fontSizeMax = 26f;
            endingListPanel.fontSizeMin = 15f;

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
