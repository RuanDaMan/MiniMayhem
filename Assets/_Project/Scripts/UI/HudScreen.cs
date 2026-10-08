using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiniMayhem
{
    /// <summary>In-run HUD: XP bar, level, timer, objective, kills, gold, HP, weapon/item slots, boss bar, banners.</summary>
    public class HudScreen : UiScreen
    {
        RunController run;
        Image xpFill, hpFill, bossFill, damageFlash, sandOverlay, sandTint;
        GameObject bossBar;
        TextMeshProUGUI level, timer, objective, kills, gold, hpText, bossName, banner, fps, revives;
        readonly List<(Image bg, Image icon, TextMeshProUGUI lvl)> weaponSlots = new(), itemSlots = new();
        float bannerTime, flashTime, fpsAcc;
        int fpsFrames;

        protected override void BuildContent()
        {
            sandTint = Ui.Panel(Root, "SandTint", new Color(0.85f, 0.7f, 0.45f, 0f), false);
            sandTint.rectTransform.Stretch();
            sandTint.raycastTarget = false;
            sandOverlay = Ui.Icon(Root, ArtId.UiVignette, 100, "Sandstorm");
            sandOverlay.preserveAspect = false;
            sandOverlay.rectTransform.Stretch(-300, -300, -300, -300);
            sandOverlay.color = new Color(0.8f, 0.62f, 0.35f, 0f);
            damageFlash = Ui.Icon(Root, ArtId.UiVignette, 100, "DamageFlash");
            damageFlash.preserveAspect = false;
            damageFlash.rectTransform.Stretch(-100, -100, -100, -100);
            damageFlash.color = new Color(1f, 0.1f, 0.15f, 0f);

            // XP bar across the top.
            xpFill = Ui.Bar(Root, "XpBar", new Color(0.1f, 0.08f, 0.16f, 0.85f), new Color(0.35f, 0.75f, 1f), out var xpBack);
            xpBack.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -8), new Vector2(1880, 30), new Vector2(0.5f, 1));
            level = Ui.OutlinedText(xpBack.transform, "Lv 1", 24, UiColors.Text);
            level.rectTransform.Stretch();

            timer = Ui.OutlinedText(Root, "0:00", 60, UiColors.Text);
            timer.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -42), new Vector2(400, 70), new Vector2(0.5f, 1));
            objective = Ui.OutlinedText(Root, "", 28, UiColors.Gold);
            objective.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -110), new Vector2(900, 40), new Vector2(0.5f, 1));

            kills = Ui.OutlinedText(Root, "", 34, UiColors.Text, TextAlignmentOptions.Right);
            kills.rectTransform.Place(new Vector2(1, 1), new Vector2(-30, -46), new Vector2(400, 44), new Vector2(1, 1));
            gold = Ui.OutlinedText(Root, "", 34, UiColors.Gold, TextAlignmentOptions.Right);
            gold.rectTransform.Place(new Vector2(1, 1), new Vector2(-30, -90), new Vector2(400, 44), new Vector2(1, 1));
            fps = Ui.OutlinedText(Root, "", 22, UiColors.TextDim, TextAlignmentOptions.Right);
            fps.rectTransform.Place(new Vector2(1, 1), new Vector2(-30, -136), new Vector2(500, 34), new Vector2(1, 1));

            hpFill = Ui.Bar(Root, "HpBar", new Color(0.1f, 0.08f, 0.16f, 0.85f), new Color(0.4f, 0.95f, 0.45f), out var hpBack);
            hpBack.rectTransform.Place(new Vector2(0, 1), new Vector2(20, -48), new Vector2(380, 34), new Vector2(0, 1));
            hpText = Ui.OutlinedText(hpBack.transform, "", 22, UiColors.Text);
            hpText.rectTransform.Stretch();
            revives = Ui.OutlinedText(Root, "", 22, UiColors.Gold, TextAlignmentOptions.Left);
            revives.rectTransform.Place(new Vector2(0, 1), new Vector2(410, -50), new Vector2(240, 34), new Vector2(0, 1));

            BuildSlots(weaponSlots, -92, 6, "W");
            BuildSlots(itemSlots, -162, 6, "I");

            bossBar = Ui.Node(Root, "BossBar").Place(new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(1000, 70), new Vector2(0.5f, 0)).gameObject;
            bossName = Ui.OutlinedText(bossBar.transform, "", 30, new Color(1f, 0.6f, 0.6f));
            bossName.rectTransform.Place(new Vector2(0.5f, 1), Vector2.zero, new Vector2(1000, 36), new Vector2(0.5f, 1));
            bossFill = Ui.Bar(bossBar.transform, "Bar", new Color(0.1f, 0.08f, 0.16f, 0.9f), new Color(1f, 0.3f, 0.35f), out var bossBack);
            bossBack.rectTransform.Place(new Vector2(0.5f, 0), Vector2.zero, new Vector2(1000, 28), new Vector2(0.5f, 0));

            banner = Ui.OutlinedText(Root, "", 72, UiColors.Gold);
            banner.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0, 220), new Vector2(1600, 110), new Vector2(0.5f, 0.5f));
            banner.outlineWidth = 0.3f;
        }

        void BuildSlots(List<(Image, Image, TextMeshProUGUI)> list, float y, int n, string prefix)
        {
            for (int i = 0; i < n; i++)
            {
                var bg = Ui.Panel(Root, prefix + i, new Color(0.1f, 0.08f, 0.16f, 0.7f));
                bg.rectTransform.Place(new Vector2(0, 1), new Vector2(20 + i * 66, y), new Vector2(60, 60), new Vector2(0, 1));
                var icon = Ui.Icon(bg.transform, ArtId.Circle, 48);
                icon.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(48, 48), new Vector2(0.5f, 0.5f));
                var lvl = Ui.OutlinedText(bg.transform, "", 20, UiColors.Text, TextAlignmentOptions.BottomRight);
                lvl.rectTransform.Stretch(2, 0, 4, 0);
                list.Add((bg, icon, lvl));
            }
        }

        public void Bind(RunController r)
        {
            if (run != null) { run.Announced -= OnAnnounce; if (run.Hero != null) run.Hero.Damaged -= OnDamaged; }
            run = r;
            if (run != null) { run.Announced += OnAnnounce; run.Hero.Damaged += OnDamaged; }
            banner.text = "";
            bannerTime = 0;
        }

        void OnAnnounce(string text, Color c)
        {
            banner.text = text;
            banner.color = c;
            bannerTime = 2.2f;
        }

        void OnDamaged(float amount) => flashTime = 0.35f;

        public string BannerText => bannerTime > 0 ? banner.text : "";

        public override void Tick()
        {
            if (run == null) return;
            var s = run.State;
            float dt = Time.unscaledDeltaTime;
            xpFill.fillAmount = s.xpToNext > 0 ? s.xp / s.xpToNext : 0;
            level.text = $"Lv {s.level}";
            timer.text = Ui.Time(s.time);
            objective.text = run.Mode switch
            {
                MatchMode.Survive => run.Director.FinaleSpawned ? "Defeat the finale boss!" : $"Survive · finale boss at {Ui.Time(run.Db.config.matchLength - 60f)}",
                MatchMode.OutlastTime => $"Outlast · {Ui.Time(Mathf.Max(0, run.Config.matchLength - s.time))} left",
                _ => $"Kills {s.kills}/{s.killTarget}",
            };
            kills.text = $"{s.kills} kills";
            gold.text = $"{Mathf.FloorToInt(s.gold)} gold";
            float maxHp = run.Stats.MaxHp;
            hpFill.fillAmount = Mathf.Clamp01(run.Hero.Hp / maxHp);
            hpFill.color = hpFill.fillAmount > 0.5f ? new Color(0.4f, 0.95f, 0.45f) : hpFill.fillAmount > 0.25f ? new Color(1f, 0.8f, 0.25f) : new Color(1f, 0.3f, 0.3f);
            hpText.text = $"{Mathf.CeilToInt(run.Hero.Hp)}/{Mathf.CeilToInt(maxHp)}";
            revives.text = run.Hero.RevivesLeft > 0 ? $"Revives x{run.Hero.RevivesLeft}" : "";

            var inv = run.Inventory;
            for (int i = 0; i < weaponSlots.Count; i++)
            {
                var (bg, icon, lvl) = weaponSlots[i];
                bool has = i < inv.Weapons.Count;
                icon.enabled = has;
                if (has)
                {
                    var w = inv.Weapons[i];
                    icon.sprite = Art.Get(w.def.icon);
                    lvl.text = w.def.IsBase ? (w.IsMaxLevel ? "<color=#FFD54A>MAX</color>" : w.level.ToString()) : "<color=#FFD54A>*</color>";
                }
                else lvl.text = "";
            }
            for (int i = 0; i < itemSlots.Count; i++)
            {
                var (bg, icon, lvl) = itemSlots[i];
                bool has = i < inv.Items.Count;
                icon.enabled = has;
                if (has)
                {
                    icon.sprite = Art.Get(inv.Items[i].def.icon);
                    lvl.text = inv.Items[i].IsMax ? "<color=#FFD54A>MAX</color>" : inv.Items[i].level.ToString();
                }
                else lvl.text = "";
            }

            var boss = run.Enemies.FirstBoss();
            bossBar.SetActive(boss != null);
            if (boss != null)
            {
                bossName.text = boss.def.displayName;
                bossFill.fillAmount = boss.HpFraction;
            }

            if (bannerTime > 0)
            {
                bannerTime -= dt;
                float a = Mathf.Clamp01(bannerTime / 0.4f);
                var c = banner.color; c.a = a; banner.color = c;
                banner.rectTransform.localScale = Vector3.one * (1f + Mathf.Clamp01((bannerTime - 1.9f) / 0.3f) * 0.3f);
            }
            else banner.text = "";

            flashTime = Mathf.Max(0, flashTime - dt);
            damageFlash.color = new Color(1f, 0.1f, 0.15f, flashTime * 1.6f + (hpFill.fillAmount < 0.25f ? 0.15f + Mathf.Sin(Time.unscaledTime * 6f) * 0.08f : 0f));
            sandOverlay.color = new Color(0.8f, 0.62f, 0.35f, run.Sandstorm);
            sandTint.color = new Color(0.85f, 0.7f, 0.45f, run.Sandstorm * 0.3f);
            sandOverlay.rectTransform.Stretch(-300 + run.Sandstorm * 320, -300 + run.Sandstorm * 180, -300 + run.Sandstorm * 320, -300 + run.Sandstorm * 180);

            fpsAcc += dt; fpsFrames++;
            if (fpsAcc >= 0.5f)
            {
                fps.text = Meta.Data.settings.showFps
                    ? $"{fpsFrames / fpsAcc:0} fps · {run.Enemies.Count} enemies · {run.Projectiles.Count} shots · {run.Pickups.GemCount} gems"
                    : "";
                fpsAcc = 0; fpsFrames = 0;
            }
        }
    }
}
