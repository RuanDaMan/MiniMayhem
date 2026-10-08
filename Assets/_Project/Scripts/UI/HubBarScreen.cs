using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiniMayhem
{
    /// <summary>
    /// The top bar shown over every hub page: page tabs (LB/RB or click to switch), the player's gold and a
    /// slide-in animation for the page that just opened.
    /// </summary>
    public class HubBarScreen : UiScreen
    {
        readonly List<(FlowState page, Button button, TextMeshProUGUI label)> tabs = new();
        readonly List<Image> dots = new();
        TextMeshProUGUI gold;
        RectTransform sliding;
        float slideX;

        protected override void BuildContent()
        {
            // Only the bar itself blocks clicks; the rest of the screen belongs to the page underneath.
            var bar = Ui.Panel(Root, "Bar", new Color(0.08f, 0.06f, 0.13f, 0.97f), false);
            bar.rectTransform.anchorMin = new Vector2(0, 1);
            bar.rectTransform.anchorMax = new Vector2(1, 1);
            bar.rectTransform.pivot = new Vector2(0.5f, 1);
            bar.rectTransform.sizeDelta = new Vector2(0, 100);
            bar.rectTransform.anchoredPosition = Vector2.zero;
            var line = Ui.Panel(bar.transform, "Line", new Color(1f, 0.83f, 0.25f, 0.6f), false);
            line.rectTransform.anchorMin = Vector2.zero;
            line.rectTransform.anchorMax = new Vector2(1, 0);
            line.rectTransform.sizeDelta = new Vector2(0, 4);

            var row = Ui.Node(bar.transform, "Tabs").Place(new Vector2(0.5f, 0.5f), new Vector2(0, 6), new Vector2(1300, 80));
            Ui.HList(row.gameObject, 12);
            var lb = Ui.OutlinedText(row, "LB", 28, UiColors.TextDim);
            lb.rectTransform.sizeDelta = new Vector2(60, 60);
            foreach (var page in GameFlow.HubPages)
            {
                var p = page;
                var b = Ui.Button(row, GameFlow.PageName(page), () => flow.GoHub(p), new Vector2(page == FlowState.MatchSelect ? 230 : 200, 66), 28, "Tab_" + page);
                var nav = b.navigation; nav.mode = Navigation.Mode.None; b.navigation = nav;
                tabs.Add((page, b, b.Label()));
            }
            var rb = Ui.OutlinedText(row, "RB", 28, UiColors.TextDim);
            rb.rectTransform.sizeDelta = new Vector2(60, 60);

            var dotRow = Ui.Node(bar.transform, "Dots").Place(new Vector2(0.5f, 0), new Vector2(0, 8), new Vector2(200, 12), new Vector2(0.5f, 0));
            Ui.HList(dotRow.gameObject, 10);
            foreach (var _ in GameFlow.HubPages)
            {
                var d = Ui.Panel(dotRow, "Dot", UiColors.TextDim);
                d.rectTransform.sizeDelta = new Vector2(10, 10);
                dots.Add(d);
            }

            gold = Ui.OutlinedText(bar.transform, "", 36, UiColors.Gold, TextAlignmentOptions.Right);
            gold.rectTransform.Place(new Vector2(1, 0.5f), new Vector2(-30, 4), new Vector2(260, 60), new Vector2(1, 0.5f));
        }

        public override void Refresh()
        {
            gold.text = $"{Meta.Gold} gold";
            for (int i = 0; i < tabs.Count; i++)
            {
                bool on = tabs[i].page == flow.State;
                var cb = tabs[i].button.colors;
                cb.normalColor = cb.highlightedColor = cb.selectedColor = on ? UiColors.ButtonFocus : new Color(0.2f, 0.17f, 0.3f);
                tabs[i].button.colors = cb;
                tabs[i].label.color = on ? UiColors.Ink : UiColors.TextDim;
                dots[i].color = on ? UiColors.ButtonFocus : new Color(1, 1, 1, 0.25f);
            }
        }

        public void SlideIn(UiScreen page, int dir)
        {
            if (page == null) return;
            if (sliding != null) sliding.anchoredPosition = Vector2.zero;
            sliding = page.Root;
            slideX = dir * 320f;
            sliding.anchoredPosition = new Vector2(slideX, 0);
        }

        public override void Tick()
        {
            gold.text = $"{Meta.Gold} gold";
            if (sliding == null) return;
            slideX = Mathf.Lerp(slideX, 0f, 1f - Mathf.Exp(-16f * Time.unscaledDeltaTime));
            if (Mathf.Abs(slideX) < 0.5f) slideX = 0;
            sliding.anchoredPosition = new Vector2(slideX, 0);
            if (slideX == 0) sliding = null;
        }
    }

    /// <summary>A hub page for features planned for later phases (characters, cosmetics).</summary>
    public class PlaceholderScreen : UiScreen
    {
        readonly string title, body, badge;
        readonly ArtId art;
        Image icon;
        Button back;

        public PlaceholderScreen(string title, ArtId art, string body, string badge)
        {
            this.title = title; this.art = art; this.body = body; this.badge = badge;
        }

        protected override void BuildContent()
        {
            Ui.Panel(Root, "Bg", new Color(0.15f, 0.12f, 0.23f), false).rectTransform.Stretch();
            var card = Ui.Panel(Root, "Card", UiColors.Panel);
            card.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0, -50), new Vector2(1100, 720));
            icon = Ui.Icon(card.transform, art, 260);
            icon.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(260, 260), new Vector2(0.5f, 1));
            var t = Ui.OutlinedText(card.transform, title, 64, UiColors.Gold);
            t.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -310), new Vector2(1000, 80), new Vector2(0.5f, 1));
            var b = Ui.Text(card.transform, body, 30, UiColors.Text);
            b.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -400), new Vector2(900, 180), new Vector2(0.5f, 1));
            var tag = Ui.Panel(card.transform, "Badge", new Color(1f, 0.83f, 0.25f, 0.9f));
            tag.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(520, 60), new Vector2(0.5f, 0));
            var bt = Ui.Text(tag.transform, badge, 28, UiColors.Ink);
            bt.fontStyle = FontStyles.Bold;
            bt.rectTransform.Stretch();
            back = Ui.Button(Root, "Back to Home", () => flow.GoHub(FlowState.MatchSelect), new Vector2(420, 76), 30);
            back.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(420, 76), new Vector2(0.5f, 0));
        }

        public override GameObject DefaultSelection => back.gameObject;

        public override void Tick()
        {
            icon.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.unscaledTime * 2f) * 4f);
        }

        public override void OnBack() => flow.GoHub(FlowState.MatchSelect);
    }
}
