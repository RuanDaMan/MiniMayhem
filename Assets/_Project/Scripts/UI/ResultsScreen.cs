using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiniMayhem
{
    /// <summary>End of run: outcome, gold breakdown (converted to meta gold), damage per weapon and unlocks.</summary>
    public class ResultsScreen : UiScreen
    {
        TextMeshProUGUI title, reason, summary, gold, weapons, unlocks;
        Button again;

        protected override void BuildContent()
        {
            Ui.Panel(Root, "Dim", new Color(0.05f, 0.03f, 0.1f, 0.85f), false).rectTransform.Stretch();
            title = Ui.OutlinedText(Root, "", 110, UiColors.Gold);
            title.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(1400, 130), new Vector2(0.5f, 1));
            reason = Ui.OutlinedText(Root, "", 36, UiColors.Text);
            reason.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -170), new Vector2(1400, 50), new Vector2(0.5f, 1));

            var left = Ui.Panel(Root, "Summary", UiColors.Panel);
            left.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(-600, -250), new Vector2(560, 600), new Vector2(0.5f, 1));
            summary = Ui.Text(left.transform, "", 28, UiColors.Text, TextAlignmentOptions.TopLeft);
            summary.rectTransform.Stretch(26, 20, 26, 20);
            var mid = Ui.Panel(Root, "Gold", UiColors.Panel);
            mid.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -250), new Vector2(560, 600), new Vector2(0.5f, 1));
            gold = Ui.Text(mid.transform, "", 28, UiColors.Text, TextAlignmentOptions.TopLeft);
            gold.rectTransform.Stretch(26, 20, 26, 20);
            var right = Ui.Panel(Root, "Weapons", UiColors.Panel);
            right.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(600, -250), new Vector2(560, 600), new Vector2(0.5f, 1));
            weapons = Ui.Text(right.transform, "", 26, UiColors.Text, TextAlignmentOptions.TopLeft);
            weapons.rectTransform.Stretch(26, 20, 26, 20);

            unlocks = Ui.OutlinedText(Root, "", 32, UiColors.Good);
            unlocks.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0, 140), new Vector2(1600, 90), new Vector2(0.5f, 0));

            var row = Ui.Node(Root, "Buttons").Place(new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(1400, 90), new Vector2(0.5f, 0));
            Ui.HList(row.gameObject, 30);
            again = Ui.Button(row, "Continue", () => flow.ExitToMenu(FlowState.MatchSelect), new Vector2(380, 84), 34);
            Ui.Button(row, "Skill Tree", () => flow.ExitToMenu(FlowState.SkillTree), new Vector2(380, 84), 34);
            Ui.Button(row, "Title", () => flow.ExitToMenu(FlowState.Title), new Vector2(380, 84), 34);
        }

        public override GameObject DefaultSelection => again.gameObject;

        public override void Refresh()
        {
            var run = flow.Run;
            if (run == null || run.Result == null) return;
            var r = run.Result;
            title.text = r.won ? "VICTORY!" : "DEFEATED";
            title.color = r.won ? UiColors.Gold : UiColors.Bad;
            reason.text = r.reason;
            summary.text =
                $"<b><size=32>Run</size></b>\n{r.biome.displayName} · Match {r.matchIndex + 1}\n{run.Config.Mode(r.mode).displayName} · {MatchSelectScreen.StyleName(r.style)}\n\n" +
                $"Time survived  <b>{Ui.Time(r.time)}</b>\nKills  <b>{r.kills}</b>\nLevel  <b>{r.level}</b>\nBosses defeated  <b>{run.State.bossKills}</b>\n" +
                $"Damage taken  <b>{run.State.damageTaken:0}</b>";
            var sb = new StringBuilder("<b><size=32>Gold</size></b>\n");
            sb.Append($"Gold picked up  {r.fromGold}\nKills bonus  {r.fromKills}\nTime bonus  {r.fromTime}\n");
            if (r.won) sb.Append($"Victory bonus  {r.winBonus}\n");
            sb.Append($"Mode multiplier  x{r.modeMultiplier:0.##}\n");
            if (!r.won) sb.Append($"<color=#FF8080>Kept on death  {r.keepFraction * 100:0}%</color>\n");
            sb.Append($"\n<size=40><color=#FFD54A><b>+{r.metaGold} gold</b></color></size>\nTotal: {Meta.Gold}");
            gold.text = sb.ToString();
            var wb = new StringBuilder("<b><size=32>Damage</size></b>\n");
            foreach (var w in r.weapons)
                wb.Append($"{w.name} {(w.form == WeaponForm.Base ? "Lv " + w.level : "*")}\n  <color=#B8B0D0>{w.damage:0} dmg · {w.kills} kills</color>\n");
            weapons.text = wb.ToString();
            unlocks.text = string.Join("\n", r.unlocks);
        }
    }
}
