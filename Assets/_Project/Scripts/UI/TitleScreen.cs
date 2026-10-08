using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiniMayhem
{
    /// <summary>Main menu: Play, Skill Tree, Codex, Settings, Quit, with some bouncing critters for charm.</summary>
    public class TitleScreen : UiScreen
    {
        Button play;
        TextMeshProUGUI gold, title, hint;
        readonly List<(RectTransform rt, Vector2 vel, float spin)> critters = new();

        protected override void BuildContent()
        {
            var bg = Ui.Panel(Root, "Bg", new Color(0.18f, 0.14f, 0.27f), false);
            bg.rectTransform.Stretch();

            // Bouncing enemies in the background.
            var rnd = new System.Random(5);
            int n = 0;
            foreach (var e in Db.enemies)
            {
                if (e == null || e.tier != EnemyTier.Normal || n++ % 3 != 0) continue;
                var img = Ui.Icon(Root, ArtId.Circle, 110, "Critter");
                img.sprite = Art.Enemy(e);
                img.color = new Color(1, 1, 1, 0.5f);
                img.rectTransform.anchorMin = img.rectTransform.anchorMax = Vector2.zero;
                img.rectTransform.anchoredPosition = new Vector2(rnd.Next(100, 1820), rnd.Next(100, 980));
                critters.Add((img.rectTransform, new Vector2(rnd.Next(-90, 90), rnd.Next(-90, 90)), rnd.Next(-30, 30)));
            }

            var hero = Ui.Icon(Root, ArtId.Hero, 260);
            hero.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(-470, 60), new Vector2(260, 260), new Vector2(0.5f, 0.5f));

            title = Ui.OutlinedText(Root, "MINI MAYHEM", 150, UiColors.Gold);
            title.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0, -90), new Vector2(1600, 190), new Vector2(0.5f, 1f));
            title.outlineWidth = 0.3f;
            var sub = Ui.OutlinedText(Root, "a cute & chunky bullet heaven", 40, UiColors.Text);
            sub.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0, -280), new Vector2(1200, 60), new Vector2(0.5f, 1f));

            var col = Ui.Node(Root, "Buttons").Place(new Vector2(0.5f, 0.5f), new Vector2(60, -110), new Vector2(520, 560));
            Ui.VList(col.gameObject, 18);
            var size = new Vector2(480, 86);
            play = Ui.Button(col, "Play", () => flow.Go(FlowState.MatchSelect), size, 40);
            Ui.Button(col, "Skill Tree", () => flow.Go(FlowState.SkillTree), size, 36);
            Ui.Button(col, "Codex", () => flow.OpenCodex(FlowState.Title), size, 36);
            Ui.Button(col, "Settings", () => flow.OpenSettings(FlowState.Title), size, 36);
            Ui.Button(col, "Quit", () => flow.Quit(), size, 36);

            gold = Ui.OutlinedText(Root, "", 40, UiColors.Gold, TextAlignmentOptions.Right);
            gold.rectTransform.Place(new Vector2(1, 1), new Vector2(-40, -30), new Vector2(600, 60), new Vector2(1, 1));
            hint = Ui.Text(Root, "Move: Left stick / WASD   ·   Select: A / Enter   ·   Back: B / Esc", 26, UiColors.TextDim);
            hint.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(1600, 40), new Vector2(0.5f, 0));
        }

        public override GameObject DefaultSelection => play != null ? play.gameObject : null;

        public override void Refresh()
        {
            gold.text = $"{Meta.Gold} gold";
        }

        public override void Tick()
        {
            float dt = Time.unscaledDeltaTime;
            title.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.unscaledTime * 1.6f) * 2f);
            title.rectTransform.localScale = Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * 3.1f) * 0.02f);
            for (int i = 0; i < critters.Count; i++)
            {
                var (rt, vel, spin) = critters[i];
                var p = rt.anchoredPosition + vel * dt;
                if (p.x < 0 || p.x > 1920) vel.x = -vel.x;
                if (p.y < 0 || p.y > 1080) vel.y = -vel.y;
                rt.anchoredPosition = p;
                rt.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.unscaledTime * 2f + i) * 10f);
                critters[i] = (rt, vel, spin);
            }
        }

        public override void OnBack() { }
    }
}
