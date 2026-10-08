using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiniMayhem
{
    /// <summary>Free pick of the starting weapon (only unlocked ones).</summary>
    public class WeaponPickScreen : UiScreen
    {
        readonly List<(WeaponDefinition w, Button b, Image icon, Image lockIcon)> cells = new();
        TextMeshProUGUI header, detailTitle, detailBody;
        Image detailIcon;
        GameObject lastFocus;

        protected override void BuildContent()
        {
            Ui.Panel(Root, "Bg", new Color(0.16f, 0.13f, 0.24f), false).rectTransform.Stretch();
            header = Ui.OutlinedText(Root, "Pick your starting weapon", 60, UiColors.Gold);
            header.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(1600, 90), new Vector2(0.5f, 1));

            var grid = Ui.Node(Root, "Grid").Place(new Vector2(0, 1), new Vector2(80, -160), new Vector2(1080, 700), new Vector2(0, 1));
            var g = grid.gameObject.AddComponent<GridLayoutGroup>();
            g.cellSize = new Vector2(240, 230);
            g.spacing = new Vector2(24, 24);
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            g.constraintCount = 4;
            foreach (var w in Db.BaseWeapons)
            {
                var wd = w;
                var b = Ui.Button(grid, "", () => Pick(wd), new Vector2(240, 230), 24, "W_" + w.id);
                var icon = Ui.Icon(b.transform, w.icon, 130);
                icon.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -16), new Vector2(130, 130), new Vector2(0.5f, 1));
                var name = Ui.Text(b.transform, w.displayName, 26, UiColors.Text);
                name.fontStyle = FontStyles.Bold;
                name.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0, 16), new Vector2(230, 64), new Vector2(0.5f, 0));
                var lockIcon = Ui.Icon(b.transform, ArtId.UiLock, 80);
                lockIcon.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(80, 80), new Vector2(0.5f, 0.5f));
                cells.Add((w, b, icon, lockIcon));
            }

            var detail = Ui.Panel(Root, "Detail", UiColors.Panel);
            detail.rectTransform.Place(new Vector2(1, 1), new Vector2(-70, -160), new Vector2(640, 820), new Vector2(1, 1));
            detailIcon = Ui.Icon(detail.transform, ArtId.Circle, 150);
            detailIcon.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(150, 150), new Vector2(0.5f, 1));
            detailTitle = Ui.OutlinedText(detail.transform, "", 42, UiColors.Gold);
            detailTitle.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -190), new Vector2(600, 60), new Vector2(0.5f, 1));
            detailBody = Ui.Text(detail.transform, "", 26, UiColors.Text, TextAlignmentOptions.TopLeft);
            detailBody.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -260), new Vector2(580, 540), new Vector2(0.5f, 1));

            var hint = Ui.Text(Root, "A: start the run   ·   B: back", 26, UiColors.TextDim);
            hint.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0, 20), new Vector2(1200, 40), new Vector2(0.5f, 0));
        }

        void Pick(WeaponDefinition w)
        {
            if (!Meta.IsWeaponUnlocked(w)) { Sfx.Play(SfxId.Error); return; }
            flow.StartSelectedRun(w);
        }

        public override GameObject DefaultSelection
        {
            get
            {
                foreach (var c in cells) if (c.w.id == Meta.Data.lastWeapon && Meta.IsWeaponUnlocked(c.w)) return c.b.gameObject;
                foreach (var c in cells) if (Meta.IsWeaponUnlocked(c.w)) return c.b.gameObject;
                return null;
            }
        }

        public override void Refresh()
        {
            var b = flow.SelectedBiome;
            if (b != null)
                header.text = $"{b.displayName} · Match {flow.SelectedMatch + 1} · {Db.config.Mode(flow.PlannedMode(b, flow.SelectedMatch)).displayName}\n<size=40>Pick your starting weapon</size>";
            foreach (var (w, btn, icon, lockIcon) in cells)
            {
                bool u = Meta.IsWeaponUnlocked(w);
                icon.color = u ? Color.white : new Color(0, 0, 0, 0.6f);
                lockIcon.enabled = !u;
                var cb = btn.colors;
                cb.normalColor = u ? UiColors.Button : UiColors.Locked;
                btn.colors = cb;
            }
            lastFocus = null;
        }

        public override void Tick()
        {
            var sel = Ui.Selected;
            if (sel == lastFocus) return;
            lastFocus = sel;
            foreach (var (w, btn, _, _) in cells)
            {
                if (btn.gameObject != sel) continue;
                bool u = Meta.IsWeaponUnlocked(w);
                detailIcon.sprite = Art.Get(w.icon);
                detailIcon.color = u ? Color.white : new Color(0, 0, 0, 0.6f);
                detailTitle.text = u ? w.displayName : $"{w.displayName} (locked)";
                detailBody.text = CodexText.WeaponSummary(Db, w) + (u ? "" : $"\n\n<color=#FF8080><b>How to unlock:</b> {w.HowToUnlock(Db)}</color>");
            }
        }

        public override void OnBack() => flow.Go(FlowState.MatchSelect);
    }
}
