using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiniMayhem
{
    /// <summary>
    /// Level-up popup (game paused): pick 1 of N cards. Y = reroll, X = banish the focused card, B = skip,
    /// when those are unlocked in the skill tree.
    /// </summary>
    public class LevelUpScreen : UiScreen
    {
        class CardView { public Button button; public Image bg, icon, tagBg; public TextMeshProUGUI title, tag, body; }

        readonly List<CardView> views = new();
        RectTransform cardRow;
        TextMeshProUGUI header, utilHint;
        Button reroll, banish, skip;
        float inputDelay;

        RunController Run => flow.Run;

        protected override void BuildContent()
        {
            var dim = Ui.Panel(Root, "Dim", new Color(0.05f, 0.03f, 0.1f, 0.72f), false);
            dim.rectTransform.Stretch();
            header = Ui.OutlinedText(Root, "LEVEL UP!", 84, UiColors.Gold);
            header.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -150), new Vector2(1400, 110), new Vector2(0.5f, 1));
            cardRow = Ui.Node(Root, "Cards").Place(new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(1800, 600));
            Ui.HList(cardRow.gameObject, 34);
            for (int i = 0; i < 4; i++) views.Add(MakeCard(i));

            var utils = Ui.Node(Root, "Utilities").Place(new Vector2(0.5f, 0), new Vector2(0, 90), new Vector2(1200, 80), new Vector2(0.5f, 0));
            Ui.HList(utils.gameObject, 30);
            reroll = Ui.Button(utils, "Reroll", DoReroll, new Vector2(320, 74), 28);
            banish = Ui.Button(utils, "Banish", () => DoBanish(FocusedIndex()), new Vector2(320, 74), 28);
            skip = Ui.Button(utils, "Skip", DoSkip, new Vector2(320, 74), 28);
            utilHint = Ui.Text(Root, "", 24, UiColors.TextDim);
            utilHint.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(1400, 40), new Vector2(0.5f, 0));
        }

        CardView MakeCard(int i)
        {
            var v = new CardView();
            int idx = i;
            v.button = Ui.Button(cardRow, "", () => Pick(idx), new Vector2(380, 560), 24, "Card" + i);
            v.bg = v.button.Bg();
            v.tagBg = Ui.Panel(v.button.transform, "TagBg", new Color(0, 0, 0, 0.3f));
            v.tagBg.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -16), new Vector2(330, 44), new Vector2(0.5f, 1));
            v.tag = Ui.Text(v.tagBg.transform, "", 24, UiColors.Text);
            v.tag.fontStyle = FontStyles.Bold;
            v.tag.rectTransform.Stretch();
            v.icon = Ui.Icon(v.button.transform, ArtId.Circle, 170);
            v.icon.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -74), new Vector2(170, 170), new Vector2(0.5f, 1));
            v.title = Ui.OutlinedText(v.button.transform, "", 34, UiColors.Text);
            v.title.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -256), new Vector2(350, 50), new Vector2(0.5f, 1));
            v.body = Ui.Text(v.button.transform, "", 23, UiColors.Text, TextAlignmentOptions.Top);
            v.body.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -318), new Vector2(340, 230), new Vector2(0.5f, 1));
            return v;
        }

        public override GameObject DefaultSelection => views.Count > 0 && views[0].button.gameObject.activeSelf ? views[0].button.gameObject : null;

        public override void Show()
        {
            inputDelay = 0.35f;
            base.Show();
        }

        public override void Refresh()
        {
            if (Run == null) return;
            var cards = Run.LevelUp.Current;
            bool chest = Run.State.pendingLevelUps > 1 || false;
            header.text = chest ? $"LEVEL UP!  <size=50>(+{Run.State.pendingLevelUps - 1} more)</size>" : "LEVEL UP!";
            for (int i = 0; i < views.Count; i++)
            {
                var v = views[i];
                bool on = i < cards.Count;
                v.button.gameObject.SetActive(on);
                if (!on) continue;
                var c = cards[i];
                v.icon.sprite = Art.Get(c.icon);
                v.title.text = c.title;
                v.tag.text = c.tag;
                v.tagBg.color = c.IsSpecial ? new Color(c.color.r, c.color.g, c.color.b, 0.85f) : new Color(0, 0, 0, 0.3f);
                v.tag.color = c.IsSpecial ? UiColors.Ink : (c.kind == CardKind.NewWeapon || c.kind == CardKind.NewItem ? UiColors.Gold : UiColors.Text);
                v.body.text = c.description;
                var cb = v.button.colors;
                var baseCol = Color.Lerp(UiColors.Button, c.color, c.IsSpecial ? 0.45f : 0.18f);
                cb.normalColor = baseCol;
                cb.highlightedColor = baseCol;
                cb.selectedColor = Color.Lerp(baseCol, Color.white, 0.18f);
                v.button.colors = cb;
            }
            var lu = Run.LevelUp;
            reroll.gameObject.SetActive(lu.Rerolls > 0);
            banish.gameObject.SetActive(lu.Banishes > 0);
            skip.gameObject.SetActive(lu.Skips > 0);
            reroll.Label().text = $"Y  Reroll ({lu.Rerolls})";
            banish.Label().text = $"X  Banish ({lu.Banishes})";
            skip.Label().text = $"B  Skip ({lu.Skips})";
            utilHint.text = lu.Rerolls + lu.Banishes + lu.Skips == 0 ? "Unlock Reroll, Skip and Banish in the Skill Tree (Fortune branch)." : "Banish removes the focused card from this run's pool.";
            // Keep focus on a card after the content changes.
            if (Ui.Selected == null || !Ui.Selected.activeInHierarchy) Ui.Select(DefaultSelection);
        }

        int FocusedIndex()
        {
            var sel = Ui.Selected;
            for (int i = 0; i < views.Count; i++) if (views[i].button.gameObject == sel) return i;
            return -1;
        }

        public override void Tick()
        {
            if (Run == null) return;
            if (inputDelay > 0) { inputDelay -= Time.unscaledDeltaTime; return; }
            if (Input.Reroll.WasPressedThisFrame()) DoReroll();
            else if (Input.Banish.WasPressedThisFrame()) DoBanish(FocusedIndex());
            else if (Input.Skip.WasPressedThisFrame()) DoSkip();
            // Gentle bob on the focused card's icon.
            int f = FocusedIndex();
            for (int i = 0; i < views.Count; i++)
                views[i].icon.rectTransform.localRotation = Quaternion.Euler(0, 0, i == f ? Mathf.Sin(Time.unscaledTime * 5f) * 6f : 0f);
        }

        public void Pick(int index)
        {
            if (inputDelay > 0 || Run == null) return;
            var cards = Run.LevelUp.Current;
            if (index < 0 || index >= cards.Count) return;
            var c = cards[index];
            Run.LevelUp.Apply(c);
            Sfx.Play(c.IsSpecial ? SfxId.Purchase : SfxId.Select, 0.6f);
            flow.LevelUpDone();
        }

        void DoReroll()
        {
            if (Run == null || !Run.LevelUp.Reroll()) { Sfx.Play(SfxId.Error, 0.4f); return; }
            Sfx.Play(SfxId.Boing, 0.5f, 1.4f);
            Refresh();
            Ui.Select(DefaultSelection);
        }

        void DoBanish(int idx)
        {
            if (Run == null || idx < 0 || !Run.LevelUp.Banish(idx)) { Sfx.Play(SfxId.Error, 0.4f); return; }
            Sfx.Play(SfxId.Thunk, 0.6f);
            Refresh();
            if (idx < views.Count && views[idx].button.gameObject.activeSelf) Ui.Select(views[idx].button.gameObject);
            else Ui.Select(DefaultSelection);
        }

        void DoSkip()
        {
            if (Run == null || !Run.LevelUp.Skip()) { Sfx.Play(SfxId.Error, 0.4f); return; }
            Sfx.Play(SfxId.Coin, 0.5f);
            flow.LevelUpDone();
        }
    }
}
