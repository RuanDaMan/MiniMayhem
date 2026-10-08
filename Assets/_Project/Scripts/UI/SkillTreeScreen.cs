using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiniMayhem
{
    /// <summary>
    /// The meta shop as a skill tree. Stick / D-pad moves between nodes (UI navigation), A buys a rank,
    /// LT/RT or the mouse wheel zoom, the right stick pans, Y respecs (full refund), B goes back.
    /// </summary>
    public class SkillTreeScreen : UiScreen
    {
        class NodeView { public SkillNodeDefinition def; public Button button; public Image bg; public TextMeshProUGUI label, rank; }

        const float Spacing = 165f;
        RectTransform viewport, content;
        readonly List<NodeView> nodes = new();
        readonly List<(Image line, SkillNodeDefinition a, SkillNodeDefinition b)> lines = new();
        TextMeshProUGUI infoTitle, infoBody;
        GameObject confirm;
        Button confirmYes, respecButton;
        float zoom = 1f;
        Vector2 pan, panTarget;
        NodeView lastFocus;

        public static Color BranchColor(SkillBranch b) => b switch
        {
            SkillBranch.Vitality => new Color(1f, 0.45f, 0.5f),
            SkillBranch.Might => new Color(1f, 0.6f, 0.25f),
            SkillBranch.Swift => new Color(0.3f, 0.85f, 0.9f),
            SkillBranch.Fortune => new Color(0.75f, 0.9f, 0.3f),
            _ => new Color(1f, 0.83f, 0.25f),
        };

        protected override void BuildContent()
        {
            Ui.Panel(Root, "Bg", new Color(0.12f, 0.1f, 0.19f), false).rectTransform.Stretch();
            viewport = Ui.Node(Root, "Viewport").Stretch(0, 0, 600, 110);
            viewport.gameObject.AddComponent<RectMask2D>();
            content = Ui.Node(viewport, "Content").Place(new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            foreach (var n in Db.skillNodes)
                foreach (var p in n.prerequisites)
                {
                    if (p == null) continue;
                    var line = Ui.Panel(content, "Line", new Color(1, 1, 1, 0.2f), false);
                    line.raycastTarget = false;
                    line.rectTransform.pivot = new Vector2(0, 0.5f);
                    lines.Add((line, p, n));
                }
            foreach (var n in Db.skillNodes)
            {
                var v = new NodeView { def = n };
                var def = n;
                v.button = Ui.Button(content, "", () => Buy(def), new Vector2(124, 124), 20, "Node_" + n.id);
                v.bg = v.button.Bg();
                v.bg.sprite = Art.Get(ArtId.UiNode);
                v.bg.type = Image.Type.Simple;
                var frame = v.button.GetComponent<Image>();
                frame.sprite = Art.Get(ArtId.UiNode);
                frame.type = Image.Type.Simple;
                v.label = Ui.Text(v.button.transform, n.displayName, 19, UiColors.Ink);
                v.label.fontStyle = FontStyles.Bold;
                v.label.rectTransform.Stretch(10, 30, 10, 18);
                v.rank = Ui.Text(v.button.transform, "", 20, UiColors.Ink);
                v.rank.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0, 8), new Vector2(110, 26), new Vector2(0.5f, 0));
                nodes.Add(v);
            }

            var info = Ui.Panel(Root, "Info", UiColors.Panel);
            info.rectTransform.Place(new Vector2(1, 0), new Vector2(-30, 30), new Vector2(560, 920), new Vector2(1, 0));
            infoTitle = Ui.OutlinedText(info.transform, "", 40, UiColors.Gold);
            infoTitle.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -24), new Vector2(520, 60), new Vector2(0.5f, 1));
            infoBody = Ui.Text(info.transform, "", 27, UiColors.Text, TextAlignmentOptions.TopLeft);
            infoBody.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -100), new Vector2(510, 640), new Vector2(0.5f, 1));
            respecButton = Ui.Button(info.transform, "Y  Respec (full refund)", () => ShowConfirm(true), new Vector2(480, 70), 26);
            respecButton.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0), new Vector2(0, 110), new Vector2(480, 70), new Vector2(0.5f, 0));
            var hint = Ui.Text(info.transform, "A buy · LT/RT zoom · R-stick pan · B home", 22, UiColors.TextDim);
            hint.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(520, 40), new Vector2(0.5f, 0));

            var c = Ui.Panel(Root, "Confirm", UiColors.PanelLight);
            c.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760, 300));
            var ct = Ui.Text(c.transform, "Refund every node?\nYou get all the gold back.", 32, UiColors.Text);
            ct.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(700, 120), new Vector2(0.5f, 1));
            var row = Ui.Node(c.transform, "Row").Place(new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(700, 90), new Vector2(0.5f, 0));
            Ui.HList(row.gameObject, 30);
            confirmYes = Ui.Button(row, "Refund", () => { Meta.Respec(); Meta.Save(); Sfx.Play(SfxId.Coin); ShowConfirm(false); Refresh(); }, new Vector2(300, 80), 30);
            Ui.Button(row, "Cancel", () => ShowConfirm(false), new Vector2(300, 80), 30);
            confirm = c.gameObject;
            confirm.SetActive(false);
        }

        void ShowConfirm(bool on)
        {
            confirm.SetActive(on);
            foreach (var n in nodes) n.button.interactable = !on;
            respecButton.interactable = !on;
            Ui.Select(on ? confirmYes.gameObject : DefaultSelection);
        }

        public override GameObject DefaultSelection
        {
            get
            {
                if (confirm != null && confirm.activeSelf) return confirmYes.gameObject;
                if (lastFocus != null) return lastFocus.button.gameObject;
                return nodes.Count > 0 ? nodes[0].button.gameObject : null;
            }
        }

        void Buy(SkillNodeDefinition n)
        {
            if (Meta.Buy(n))
            {
                Meta.Save();
                Sfx.Play(SfxId.Purchase, 0.7f);
                Refresh();
                foreach (var v in nodes) if (v.def == n) v.button.transform.localScale = Vector3.one * 1.3f;
            }
            else Sfx.Play(SfxId.Error, 0.5f);
        }

        public override void Refresh()
        {
            foreach (var v in nodes)
            {
                int r = Meta.Rank(v.def);
                bool reach = Meta.IsReachable(v.def);
                var col = BranchColor(v.def.branch);
                if (!reach) col = Color.Lerp(col, UiColors.Locked, 0.75f);
                else if (r == 0) col = Color.Lerp(col, UiColors.Locked, 0.35f);
                var cb = v.button.colors;
                cb.normalColor = col;
                cb.highlightedColor = col;
                cb.selectedColor = Color.Lerp(col, Color.white, 0.25f);
                v.button.colors = cb;
                v.rank.text = Meta.IsMaxed(v.def) ? "MAX" : $"{r}/{v.def.maxRank}";
                v.label.color = reach ? UiColors.Ink : new Color(0.1f, 0.08f, 0.15f, 0.6f);
            }
            foreach (var (line, a, b) in lines)
                line.color = Meta.Rank(a) > 0 ? new Color(1f, 0.9f, 0.6f, 0.75f) : new Color(1, 1, 1, 0.15f);
            Layout();
            UpdateInfo(true);
        }

        void Layout()
        {
            foreach (var v in nodes)
                v.button.GetComponent<RectTransform>().anchoredPosition = v.def.position * Spacing * zoom;
            foreach (var v in nodes) v.button.GetComponent<RectTransform>().sizeDelta = Vector2.one * 124f * Mathf.Clamp(zoom, 0.6f, 1.3f);
            foreach (var (line, a, b) in lines)
            {
                Vector2 pa = a.position * Spacing * zoom, pb = b.position * Spacing * zoom;
                var rt = line.rectTransform;
                rt.anchoredPosition = pa;
                rt.sizeDelta = new Vector2((pb - pa).magnitude, 8f);
                rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(pb.y - pa.y, pb.x - pa.x) * Mathf.Rad2Deg);
            }
        }

        NodeView Focused()
        {
            var sel = Ui.Selected;
            foreach (var v in nodes) if (v.button.gameObject == sel) return v;
            return null;
        }

        void UpdateInfo(bool force)
        {
            var f = Focused();
            if (f == null || (!force && f == lastFocus)) return;
            lastFocus = f;
            var n = f.def;
            int r = Meta.Rank(n);
            infoTitle.text = n.displayName;
            infoTitle.color = BranchColor(n.branch);
            var sb = new StringBuilder();
            sb.Append($"<color=#B8B0D0>{n.branch} branch</color>\n\n{n.description}\n\n");
            sb.Append($"<b>Per rank:</b> {StatInfo.FormatMod(n.perRank.stat, n.perRank.value)}\n");
            sb.Append($"<b>Rank:</b> {r}/{n.maxRank}\n");
            if (r > 0) sb.Append($"<b>Current:</b> {StatInfo.FormatMod(n.perRank.stat, n.perRank.value * r)}\n");
            if (!Meta.IsMaxed(n))
            {
                int cost = Meta.NextCost(n);
                sb.Append($"\n<b>Next rank:</b> <color={(Meta.Gold >= cost ? "#FFD54A" : "#FF8080")}>{cost} gold</color>\n");
            }
            else sb.Append("\n<color=#7CF08A>Maxed!</color>\n");
            if (!Meta.IsReachable(n))
            {
                sb.Append("\n<color=#FF8080><b>Locked.</b> Needs a rank in: ");
                for (int i = 0; i < n.prerequisites.Length; i++) sb.Append(i > 0 ? " or " : "").Append(n.prerequisites[i].displayName);
                sb.Append("</color>");
            }
            sb.Append($"\n\n<size=22><color=#B8B0D0>{StatInfo.Describe(n.perRank.stat)}</color></size>");
            infoBody.text = sb.ToString();
            panTarget = -n.position * Spacing * zoom;
        }

        public override void Tick()
        {
            float dt = Time.unscaledDeltaTime;
            float z = Input.Zoom.ReadValue<float>();
            if (Mathf.Abs(z) > 0.01f)
            {
                zoom = Mathf.Clamp(zoom * (1f + z * dt * 1.5f * (Mathf.Abs(z) > 0.5f ? 1f : 30f)), 0.5f, 1.6f);
                Layout();
                if (lastFocus != null) panTarget = -lastFocus.def.position * Spacing * zoom;
            }
            Vector2 p = Input.Pan.ReadValue<Vector2>();
            if (p.sqrMagnitude > 0.04f) panTarget -= p * 900f * dt;
            if (!confirm.activeSelf && Input.Respec.WasPressedThisFrame()) ShowConfirm(true);
            UpdateInfo(false);
            pan = Vector2.Lerp(pan, panTarget, 1f - Mathf.Exp(-8f * dt));
            content.anchoredPosition = pan;
            foreach (var v in nodes)
            {
                var tf = v.button.transform;
                if (tf.localScale.x > 1.1f) tf.localScale = Vector3.Lerp(tf.localScale, Vector3.one, 1f - Mathf.Exp(-10f * dt));
            }
        }

        public override void OnBack()
        {
            if (confirm.activeSelf) { ShowConfirm(false); return; }
            flow.GoHub(FlowState.MatchSelect);
        }
    }
}
