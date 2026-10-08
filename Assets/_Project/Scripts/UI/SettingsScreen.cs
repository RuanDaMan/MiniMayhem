using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MiniMayhem
{
    /// <summary>Settings: volumes, damage numbers, screen shake, colour-blind mode, display, FPS, reset progress.</summary>
    public class SettingsScreen : UiScreen
    {
        class Row { public Button button; public Func<string> label; public Action<int> change; public Action press; }

        readonly List<Row> rows = new();
        readonly NavRepeater nav = new();
        GameObject confirm;
        Button confirmYes;

        SettingsData S => Meta.Data.settings;

        protected override void BuildContent()
        {
            Ui.Panel(Root, "Dim", new Color(0.1f, 0.08f, 0.16f, 0.97f), false).rectTransform.Stretch();
            var title = Ui.OutlinedText(Root, "Settings", 70, UiColors.Gold);
            title.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(800, 90), new Vector2(0.5f, 1));
            var col = Ui.Node(Root, "Rows").Place(new Vector2(0.5f, 1), new Vector2(0, -150), new Vector2(900, 860), new Vector2(0.5f, 1));
            Ui.VList(col.gameObject, 12);

            AddRow(col, () => $"Master volume   < {Pct(S.master)} >", d => { S.master = Step(S.master, d); flow.ApplySettings(); }, null);
            AddRow(col, () => $"Music volume   < {Pct(S.music)} >", d => { S.music = Step(S.music, d); flow.ApplySettings(); }, null);
            AddRow(col, () => $"Sound effects   < {Pct(S.sfx)} >", d => { S.sfx = Step(S.sfx, d); flow.ApplySettings(); Sfx.Play(SfxId.Coin); }, null);
            AddRow(col, () => $"Damage numbers: {OnOff(S.damageNumbers)}", _ => { S.damageNumbers = !S.damageNumbers; flow.ApplySettings(); }, () => { S.damageNumbers = !S.damageNumbers; flow.ApplySettings(); });
            AddRow(col, () => $"Screen shake: {OnOff(S.screenShake)}", _ => { S.screenShake = !S.screenShake; flow.ApplySettings(); }, () => { S.screenShake = !S.screenShake; flow.ApplySettings(); });
            AddRow(col, () => $"Colour-blind enemy shots: {OnOff(S.colorblind)}", _ => S.colorblind = !S.colorblind, () => S.colorblind = !S.colorblind);
            AddRow(col, () => $"Controller rumble: {OnOff(S.rumble)}", _ => S.rumble = !S.rumble, () => { S.rumble = !S.rumble; if (S.rumble) flow.Rumble(0.4f, 0.4f, 0.2f); });
            AddRow(col, () => $"Fullscreen: {OnOff(S.fullscreen)}", _ => { S.fullscreen = !S.fullscreen; GameBootstrap.ApplyDisplay(S); }, () => { S.fullscreen = !S.fullscreen; GameBootstrap.ApplyDisplay(S); });
            AddRow(col, () => $"Resolution   < {ResText()} >", ChangeRes, null);
            AddRow(col, () => $"Show FPS (View / F3): {OnOff(S.showFps)}", _ => S.showFps = !S.showFps, () => S.showFps = !S.showFps);
            AddRow(col, () => "Reset all progress...", null, () => ShowConfirm(true));
            AddRow(col, () => "Back", null, () => flow.CloseSettings());

            var hint = Ui.Text(Root, "Left / Right to change · A to toggle · B to go back", 26, UiColors.TextDim);
            hint.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0, 24), new Vector2(1200, 40), new Vector2(0.5f, 0));

            var c = Ui.Panel(Root, "Confirm", UiColors.PanelLight);
            c.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820, 320));
            var ct = Ui.Text(c.transform, "Erase ALL progress?\nGold, skill tree, unlocks and the codex are reset.", 30, UiColors.Text);
            ct.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(760, 140), new Vector2(0.5f, 1));
            var row = Ui.Node(c.transform, "Row").Place(new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(760, 90), new Vector2(0.5f, 0));
            Ui.HList(row.gameObject, 30);
            confirmYes = Ui.Button(row, "Erase", () => { Meta.ResetAll(); Meta.Save(); ShowConfirm(false); Refresh(); }, new Vector2(300, 80), 30);
            Ui.Button(row, "Cancel", () => ShowConfirm(false), new Vector2(300, 80), 30);
            confirm = c.gameObject;
            confirm.SetActive(false);
        }

        static string Pct(float v) => $"{Mathf.RoundToInt(v * 100)}%";
        static string OnOff(bool b) => b ? "<color=#7CF08A>ON</color>" : "<color=#FF8080>OFF</color>";
        static float Step(float v, int d) => Mathf.Clamp01(Mathf.Round((v + d * 0.1f) * 10f) / 10f);

        string ResText()
        {
            var res = Screen.resolutions;
            if (S.resolutionIndex < 0 || S.resolutionIndex >= res.Length) return "Native";
            return $"{res[S.resolutionIndex].width}x{res[S.resolutionIndex].height}";
        }

        void ChangeRes(int d)
        {
            int n = Screen.resolutions.Length;
            if (n == 0) return;
            S.resolutionIndex = Mathf.Clamp(S.resolutionIndex + d, -1, n - 1);
            GameBootstrap.ApplyDisplay(S);
        }

        void AddRow(Transform parent, Func<string> label, Action<int> change, Action press)
        {
            var r = new Row { label = label, change = change, press = press };
            r.button = Ui.Button(parent, "", () => { r.press?.Invoke(); RefreshLabels(); }, new Vector2(880, 60), 30);
            var t = Ui.Text(r.button.transform, "", 30, UiColors.Text);
            t.rectTransform.Stretch(20, 4, 20, 4);
            rows.Add(r);
        }

        void ShowConfirm(bool on)
        {
            confirm.SetActive(on);
            foreach (var r in rows) r.button.interactable = !on;
            Ui.Select(on ? confirmYes.gameObject : rows[0].button.gameObject);
        }

        public override GameObject DefaultSelection => confirm != null && confirm.activeSelf ? confirmYes.gameObject : rows[0].button.gameObject;

        public override void Refresh()
        {
            confirm.SetActive(false);
            foreach (var r in rows) r.button.interactable = true;
            RefreshLabels();
        }

        void RefreshLabels()
        {
            foreach (var r in rows) r.button.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = r.label();
        }

        public override void Tick()
        {
            if (confirm.activeSelf) return;
            var step = nav.Step(Input.Navigate.ReadValue<Vector2>());
            if (step.x == 0) return;
            var sel = Ui.Selected;
            foreach (var r in rows)
            {
                if (r.button.gameObject != sel || r.change == null) continue;
                r.change(step.x);
                Sfx.Play(SfxId.Click, 0.5f);
                RefreshLabels();
            }
        }

        public override void OnBack()
        {
            if (confirm.activeSelf) { ShowConfirm(false); return; }
            flow.CloseSettings();
        }
    }
}
