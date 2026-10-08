using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiniMayhem
{
    /// <summary>Pause menu with the hero's current stats and build.</summary>
    public class PauseScreen : UiScreen
    {
        Button resume;
        TextMeshProUGUI stats, build;
        GameObject confirm;
        Button confirmYes;

        protected override void BuildContent()
        {
            Ui.Panel(Root, "Dim", new Color(0.05f, 0.03f, 0.1f, 0.75f), false).rectTransform.Stretch();
            var title = Ui.OutlinedText(Root, "PAUSED", 90, UiColors.Gold);
            title.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -60), new Vector2(800, 110), new Vector2(0.5f, 1));

            var col = Ui.Node(Root, "Buttons").Place(new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(460, 520));
            Ui.VList(col.gameObject, 16);
            var size = new Vector2(440, 80);
            resume = Ui.Button(col, "Resume", () => flow.Go(FlowState.Running), size, 34);
            Ui.Button(col, "Settings", () => flow.OpenSettings(FlowState.Paused), size, 32);
            Ui.Button(col, "Codex", () => flow.OpenCodex(FlowState.Paused), size, 32);
            Ui.Button(col, "Give Up", () => ShowConfirm(true), size, 32);

            var statsPanel = Ui.Panel(Root, "Stats", UiColors.Panel);
            statsPanel.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(60, -20), new Vector2(520, 820), new Vector2(0, 0.5f));
            stats = Ui.Text(statsPanel.transform, "", 24, UiColors.Text, TextAlignmentOptions.TopLeft);
            stats.rectTransform.Stretch(24, 20, 24, 20);
            var buildPanel = Ui.Panel(Root, "Build", UiColors.Panel);
            buildPanel.rectTransform.Place(new Vector2(1, 0.5f), new Vector2(-60, -20), new Vector2(520, 820), new Vector2(1, 0.5f));
            build = Ui.Text(buildPanel.transform, "", 24, UiColors.Text, TextAlignmentOptions.TopLeft);
            build.rectTransform.Stretch(24, 20, 24, 20);

            var c = Ui.Panel(Root, "Confirm", UiColors.PanelLight);
            c.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700, 300));
            var ct = Ui.Text(c.transform, "Give up this run?\nYou keep your death share of the gold.", 32, UiColors.Text);
            ct.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(640, 120), new Vector2(0.5f, 1));
            var row = Ui.Node(c.transform, "Row").Place(new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(640, 90), new Vector2(0.5f, 0));
            Ui.HList(row.gameObject, 30);
            confirmYes = Ui.Button(row, "Give up", () => { ShowConfirm(false); flow.AbandonRun(); }, new Vector2(280, 80), 30);
            Ui.Button(row, "Keep going", () => ShowConfirm(false), new Vector2(280, 80), 30);
            confirm = c.gameObject;
            confirm.SetActive(false);
        }

        void ShowConfirm(bool on)
        {
            confirm.SetActive(on);
            Ui.Select(on ? confirmYes.gameObject : resume.gameObject);
        }

        public override GameObject DefaultSelection => confirm != null && confirm.activeSelf ? confirmYes.gameObject : resume.gameObject;

        public override void Refresh()
        {
            confirm.SetActive(false);
            var run = flow.Run;
            if (run == null) return;
            var sb = new StringBuilder("<b><size=30>Stats</size></b>\n");
            var st = run.Stats;
            sb.Append($"Max HP {st.MaxHp:0} · Regen {st.Regen:0.##}/s · Armor {st.Armor:0}\n");
            sb.Append($"Move Speed {st.MoveSpeed:0.0} m/s\n");
            foreach (StatId id in new[] { StatId.Damage, StatId.AttackSpeed, StatId.Area, StatId.ProjectileSpeed, StatId.Duration, StatId.XpGain, StatId.GoldGain, StatId.Luck })
                sb.Append($"{StatInfo.Name(id)} {StatInfo.FormatMod(id, st[id]).Split(' ')[0]}\n");
            sb.Append($"Amount +{st.Amount}\nPickup radius {st.PickupRadius:0.0} m\nCrit {st.CritChance * 100:0}% x{st.CritDamage:0.##}\n");
            sb.Append($"Revives left {run.Hero.RevivesLeft}\nGold kept on death {st.DeathKeep * 100:0}%\n");
            stats.text = sb.ToString();

            var bb = new StringBuilder("<b><size=30>Build</size></b>\n");
            foreach (var w in run.Inventory.Weapons)
                bb.Append($"{w.def.displayName} {(w.def.IsBase ? "Lv " + w.level : "*")}  <color=#B8B0D0>{w.damageDealt:0} dmg</color>\n");
            bb.Append('\n');
            foreach (var it in run.Inventory.Items) bb.Append($"{it.def.displayName} Lv {it.level}\n");
            bb.Append($"\n<b>Biome:</b> {run.Biome.displayName} · Match {run.Setup.matchIndex + 1}\n<b>Mode:</b> {run.Config.Mode(run.Mode).displayName} · {MatchSelectScreen.StyleName(run.Setup.style)}\n");
            build.text = bb.ToString();
        }
    }
}
