using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiniMayhem
{
    /// <summary>
    /// Home: plan and pick the next match (biome + one of its 3 matches; shows map style, the random mode and
    /// completion). The middle page of the hub; LB/RB slide to the other pages.
    /// </summary>
    public class MatchSelectScreen : UiScreen
    {
        class Slot { public BiomeDefinition biome; public int match; public Button button; public TextMeshProUGUI label; public Image check; }

        readonly List<Slot> slots = new();
        readonly List<(BiomeDefinition b, TextMeshProUGUI name, TextMeshProUGUI progress, Image lockIcon)> rows = new();
        TextMeshProUGUI detailTitle, detailBody;
        Image detailIcon;
        GameObject lastFocus;

        public static string StyleName(MapStyle s) => s switch
        {
            MapStyle.Endless => "Endless map",
            MapStyle.FixedArena => "Fixed arena",
            _ => "Expanding arena",
        };

        public static string StyleText(MapStyle s) => s switch
        {
            MapStyle.Endless => "An endless field: run anywhere, enemies pour in from off-screen.",
            MapStyle.FixedArena => "A walled arena with obstacles. Enemies spawn inside (watch for the ! markers).",
            _ => "A small arena whose walls move out every time a boss arrives.",
        };

        protected override void BuildContent()
        {
            Ui.Panel(Root, "Bg", new Color(0.16f, 0.13f, 0.24f), false).rectTransform.Stretch();
            var title = Ui.OutlinedText(Root, "Plan your next match", 34, UiColors.Text, TextAlignmentOptions.Left);
            title.rectTransform.Place(new Vector2(0, 1), new Vector2(64, -112), new Vector2(1000, 50), new Vector2(0, 1));

            var grid = Ui.Node(Root, "Grid").Place(new Vector2(0, 1), new Vector2(60, -168), new Vector2(1120, 820), new Vector2(0, 1));
            Ui.VList(grid.gameObject, 14, TextAnchor.UpperLeft);
            foreach (var b in Db.biomes)
            {
                var row = Ui.Panel(grid, "Row_" + b.id, UiColors.Panel);
                row.rectTransform.sizeDelta = new Vector2(1120, 118);
                var name = Ui.Text(row.transform, b.displayName, 34, UiColors.Text, TextAlignmentOptions.Left);
                name.fontStyle = FontStyles.Bold;
                name.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(26, 18), new Vector2(330, 50), new Vector2(0, 0.5f));
                var prog = Ui.Text(row.transform, "", 24, UiColors.TextDim, TextAlignmentOptions.Left);
                prog.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(26, -24), new Vector2(330, 40), new Vector2(0, 0.5f));
                var lockIcon = Ui.Icon(row.transform, ArtId.UiLock, 60);
                lockIcon.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(300, 18), new Vector2(50, 50), new Vector2(0, 0.5f));
                rows.Add((b, name, prog, lockIcon));
                for (int m = 0; m < 3; m++)
                {
                    var slot = new Slot { biome = b, match = m };
                    int mm = m;
                    var bd = b;
                    slot.button = Ui.Button(row.transform, "", () => flow.BeginWeaponPick(bd, mm), new Vector2(236, 100), 22, $"Match{m}");
                    slot.button.GetComponent<RectTransform>().Place(new Vector2(0, 0.5f), new Vector2(370 + m * 248, 0), new Vector2(236, 100), new Vector2(0, 0.5f));
                    slot.label = Ui.Text(slot.button.transform, "", 22, UiColors.Text);
                    slot.label.rectTransform.Stretch(8, 6, 8, 6);
                    slot.check = Ui.Icon(slot.button.transform, ArtId.UiCheck, 40);
                    slot.check.rectTransform.Place(new Vector2(1, 1), new Vector2(10, 10), new Vector2(40, 40), new Vector2(1, 1));
                    slots.Add(slot);
                }
            }

            var detail = Ui.Panel(Root, "Detail", UiColors.Panel);
            detail.rectTransform.Place(new Vector2(1, 1), new Vector2(-60, -130), new Vector2(640, 860), new Vector2(1, 1));
            detailIcon = Ui.Icon(detail.transform, ArtId.Circle, 150);
            detailIcon.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(150, 150), new Vector2(0.5f, 1));
            detailTitle = Ui.OutlinedText(detail.transform, "", 40, UiColors.Gold);
            detailTitle.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -190), new Vector2(600, 60), new Vector2(0.5f, 1));
            detailBody = Ui.Text(detail.transform, "", 26, UiColors.Text, TextAlignmentOptions.TopLeft);
            detailBody.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -260), new Vector2(580, 580), new Vector2(0.5f, 1));

            var hint = Ui.Text(Root, "A: choose   ·   LB / RB: Skill Tree, Codex and more   ·   B: title", 26, UiColors.TextDim);
            hint.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0, 20), new Vector2(1200, 40), new Vector2(0.5f, 0));
        }

        public override GameObject DefaultSelection
        {
            get
            {
                // First not-yet-completed unlocked match, else the first unlocked one.
                foreach (var s in slots) if (s.button.interactable && !Meta.IsMatchCompleted(s.biome, s.match)) return s.button.gameObject;
                foreach (var s in slots) if (s.button.interactable) return s.button.gameObject;
                return null;
            }
        }

        public override void Refresh()
        {
            foreach (var (b, name, progress, lockIcon) in rows)
            {
                bool unlocked = Meta.IsBiomeUnlocked(b.index);
                int done = Meta.CompletedMatches(b);
                name.color = unlocked ? UiColors.Text : UiColors.TextDim;
                progress.text = unlocked ? $"{done}/3 completed" + (done >= 3 ? "  *" : "") : $"Clear {Db.biomes[Mathf.Max(0, b.index - 1)].displayName} first";
                lockIcon.enabled = !unlocked;
            }
            foreach (var s in slots)
            {
                bool unlocked = Meta.IsBiomeUnlocked(s.biome.index);
                s.button.interactable = unlocked;
                var mode = flow.PlannedMode(s.biome, s.match);
                s.label.text = unlocked
                    ? $"<b>Match {s.match + 1}</b>\n<size=19>{StyleName(flow.StyleFor(s.biome, s.match))}\n<color=#FFD54A>{Db.config.Mode(mode).displayName}</color></size>"
                    : $"<b>Match {s.match + 1}</b>\n<size=19>locked</size>";
                s.check.enabled = Meta.IsMatchCompleted(s.biome, s.match);
            }
            lastFocus = null;
        }

        public override void Tick()
        {
            var sel = Ui.Selected;
            if (sel == lastFocus) return;
            lastFocus = sel;
            foreach (var s in slots)
            {
                if (s.button.gameObject != sel) continue;
                var b = s.biome;
                var mode = flow.PlannedMode(b, s.match);
                var ms = Db.config.Mode(mode);
                var style = flow.StyleFor(b, s.match);
                detailIcon.sprite = b.boss != null ? Art.Enemy(b.boss) : Art.Get(ArtId.UiStar);
                detailTitle.text = $"{b.displayName} · Match {s.match + 1}";
                string goal = mode == MatchMode.KillCount ? $"Reach {b.killTarget} kills before 10:00." : ms.description;
                detailBody.text =
                    $"{b.description}\n\n" +
                    $"<color=#FFD54A><b>Mode: {ms.displayName}</b></color>  (rewards x{ms.rewardMultiplier:0.##})\n{goal}\n\n" +
                    $"<color=#8FD8FF><b>{StyleName(style)}</b></color>\n{StyleText(style)}\n\n" +
                    $"<b>Hazard:</b> {HazardText(b.hazard)}\n" +
                    $"<b>Boss:</b> {(b.boss != null ? b.boss.displayName : "?")}  (every 3:00)\n" +
                    (Meta.IsMatchCompleted(b, s.match) ? "\n<color=#7CF08A>Completed - replay to farm gold. The mode re-rolls each time.</color>" : "");
            }
        }

        public static string HazardText(HazardType h) => h switch
        {
            HazardType.SlowMud => "Slow mud patches",
            HazardType.Sandstorm => "Sandstorms that reduce vision",
            HazardType.Ice => "Slippery ice",
            HazardType.Lava => "Lava pools (hurt)",
            HazardType.Sugar => "Sticky sugar patches",
            _ => "None",
        };

        public override void OnBack() => flow.Go(FlowState.Title);
    }
}
