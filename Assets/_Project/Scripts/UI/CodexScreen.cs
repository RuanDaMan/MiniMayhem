using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiniMayhem
{
    /// <summary>
    /// Info pages generated from the game data: Weapons, Items, Evolutions, Fusions, Enemies, Biomes, Stats.
    /// Entries stay "???" until discovered; recipe requirements show once the base weapon is known.
    /// LB/RB switch tabs.
    /// </summary>
    public class CodexScreen : UiScreen
    {
        static readonly string[] Tabs = { "Weapons", "Items", "Evolutions", "Fusions", "Enemies", "Biomes", "Stats" };

        class Entry { public string name; public ArtId icon; public Sprite sprite; public bool known; public Func<string> body; public bool check; }

        int tab;
        readonly List<TextMeshProUGUI> tabLabels = new();
        readonly List<Image> tabBgs = new();
        RectTransform listContent;
        ScrollRect scroll;
        readonly List<(Button b, Entry e)> buttons = new();
        TextMeshProUGUI title, body, progress;
        Image bigIcon;
        GameObject lastFocus;

        public int TabIndex => tab;
        public int EntryCount => buttons.Count;

        protected override void BuildContent()
        {
            Ui.Panel(Root, "Bg", new Color(0.14f, 0.11f, 0.21f), false).rectTransform.Stretch();
            var header = Ui.OutlinedText(Root, "Codex", 60, UiColors.Gold, TextAlignmentOptions.Left);
            header.rectTransform.Place(new Vector2(0, 1), new Vector2(50, -20), new Vector2(400, 80), new Vector2(0, 1));
            progress = Ui.Text(Root, "", 26, UiColors.TextDim, TextAlignmentOptions.Right);
            progress.rectTransform.Place(new Vector2(1, 1), new Vector2(-50, -40), new Vector2(600, 40), new Vector2(1, 1));

            var tabsRow = Ui.Node(Root, "Tabs").Place(new Vector2(0.5f, 1), new Vector2(0, -110), new Vector2(1800, 70), new Vector2(0.5f, 1));
            Ui.HList(tabsRow.gameObject, 10);
            var lb = Ui.Text(tabsRow, "LB", 26, UiColors.TextDim);
            lb.rectTransform.sizeDelta = new Vector2(60, 60);
            for (int i = 0; i < Tabs.Length; i++)
            {
                int idx = i;
                var b = Ui.Button(tabsRow, Tabs[i], () => SetTab(idx), new Vector2(220, 62), 26, "Tab" + i);
                var nav = b.navigation; nav.mode = Navigation.Mode.None; b.navigation = nav;
                tabLabels.Add(b.Label());
                tabBgs.Add(b.Bg());
            }
            var rb = Ui.Text(tabsRow, "RB", 26, UiColors.TextDim);
            rb.rectTransform.sizeDelta = new Vector2(60, 60);

            // Scrollable list.
            var listPanel = Ui.Panel(Root, "List", UiColors.Panel);
            listPanel.rectTransform.Place(new Vector2(0, 0), new Vector2(50, 50), new Vector2(560, 850), Vector2.zero);
            var vp = Ui.Node(listPanel.transform, "Viewport").Stretch(10, 10, 10, 10);
            vp.gameObject.AddComponent<RectMask2D>();
            listContent = Ui.Node(vp, "Content");
            listContent.anchorMin = new Vector2(0, 1); listContent.anchorMax = new Vector2(1, 1); listContent.pivot = new Vector2(0.5f, 1);
            listContent.sizeDelta = Vector2.zero;
            var v = Ui.VList(listContent.gameObject, 8, TextAnchor.UpperCenter, 6);
            listContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll = listPanel.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = vp;
            scroll.content = listContent;
            scroll.horizontal = false;
            scroll.scrollSensitivity = 40;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var detail = Ui.Panel(Root, "Detail", UiColors.Panel);
            detail.rectTransform.Place(new Vector2(1, 0), new Vector2(-50, 50), new Vector2(1220, 850), new Vector2(1, 0));
            bigIcon = Ui.Icon(detail.transform, ArtId.Circle, 200);
            bigIcon.rectTransform.Place(new Vector2(0, 1), new Vector2(30, -30), new Vector2(200, 200), new Vector2(0, 1));
            title = Ui.OutlinedText(detail.transform, "", 48, UiColors.Gold, TextAlignmentOptions.Left);
            title.rectTransform.Place(new Vector2(0, 1), new Vector2(260, -40), new Vector2(920, 70), new Vector2(0, 1));
            body = Ui.Text(detail.transform, "", 25, UiColors.Text, TextAlignmentOptions.TopLeft);
            body.rectTransform.Place(new Vector2(0, 1), new Vector2(260, -120), new Vector2(930, 710), new Vector2(0, 1));
            body.overflowMode = TextOverflowModes.Truncate;
        }

        public override GameObject DefaultSelection => buttons.Count > 0 ? buttons[0].b.gameObject : null;

        public override void Refresh() => SetTab(tab);

        public void SetTab(int t)
        {
            tab = (t + Tabs.Length) % Tabs.Length;
            for (int i = 0; i < tabBgs.Count; i++)
            {
                tabBgs[i].color = i == tab ? UiColors.ButtonFocus : UiColors.Button;
                tabLabels[i].color = i == tab ? UiColors.Ink : UiColors.Text;
            }
            foreach (var (b, _) in buttons) UnityEngine.Object.Destroy(b.gameObject);
            buttons.Clear();
            var entries = BuildEntries(tab);
            int known = 0;
            foreach (var e in entries)
            {
                if (e.known) known++;
                var b = Ui.Button(listContent, "", null, new Vector2(520, 76), 24, "Entry");
                var entry = e;
                var icon = Ui.Icon(b.transform, e.known ? e.icon : ArtId.UiQuestion, 56);
                if (e.known && e.sprite != null) icon.sprite = e.sprite;
                icon.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(12, 0), new Vector2(56, 56), new Vector2(0, 0.5f));
                var label = Ui.Text(b.transform, e.known ? e.name : "???", 26, e.known ? UiColors.Text : UiColors.TextDim, TextAlignmentOptions.Left);
                label.rectTransform.Stretch(84, 4, 50, 4);
                if (e.check)
                {
                    var c = Ui.Icon(b.transform, ArtId.UiCheck, 36);
                    c.rectTransform.Place(new Vector2(1, 0.5f), new Vector2(-10, 0), new Vector2(36, 36), new Vector2(1, 0.5f));
                }
                buttons.Add((b, entry));
            }
            progress.text = $"{Tabs[tab]}: {known}/{entries.Count} discovered";
            lastFocus = null;
            if (buttons.Count > 0) Ui.Select(buttons[0].b.gameObject);
            scroll.verticalNormalizedPosition = 1f;
            ShowEntry(buttons.Count > 0 ? buttons[0].e : null);
        }

        void ShowEntry(Entry e)
        {
            if (e == null) { title.text = ""; body.text = ""; bigIcon.enabled = false; return; }
            bigIcon.enabled = true;
            bigIcon.sprite = e.known ? (e.sprite != null ? e.sprite : Art.Get(e.icon)) : Art.Get(ArtId.UiQuestion);
            title.text = e.known ? e.name : "???";
            body.text = e.body();
        }

        public override void Tick()
        {
            if (Input.PrevTab.WasPressedThisFrame()) { SetTab(tab - 1); Sfx.Play(SfxId.Click); }
            else if (Input.NextTab.WasPressedThisFrame()) { SetTab(tab + 1); Sfx.Play(SfxId.Click); }
            var sel = Ui.Selected;
            if (sel == lastFocus) return;
            lastFocus = sel;
            for (int i = 0; i < buttons.Count; i++)
            {
                if (buttons[i].b.gameObject != sel) continue;
                ShowEntry(buttons[i].e);
                // Keep the focused entry visible.
                float h = listContent.rect.height, vh = ((RectTransform)scroll.viewport).rect.height;
                if (h > vh)
                {
                    float y = 6 + i * 84f;
                    float top = (1f - scroll.verticalNormalizedPosition) * (h - vh);
                    if (y < top) top = y;
                    else if (y + 84 > top + vh) top = y + 84 - vh;
                    scroll.verticalNormalizedPosition = 1f - Mathf.Clamp01(top / (h - vh));
                }
            }
        }

        public override void OnBack() => flow.CloseCodex();

        // ------------------------------------------------------------------ entries

        List<Entry> BuildEntries(int t)
        {
            var list = new List<Entry>();
            var meta = Meta;
            switch (t)
            {
                case 0:
                    foreach (var w in Db.weapons)
                    {
                        if (w == null) continue;
                        var wd = w;
                        bool known = meta.IsDiscovered("w:" + w.id);
                        string suffix = w.form == WeaponForm.Evolved ? " *" : w.form == WeaponForm.Fused ? " +" : "";
                        list.Add(new Entry
                        {
                            name = w.displayName + suffix, icon = w.icon, known = known,
                            body = () => known
                                ? CodexText.WeaponSummary(Db, wd) + CodexText.LevelTable(wd) + $"\n<b>How to unlock:</b> {wd.HowToUnlock(Db)}"
                                : $"Not discovered yet.\n\n<b>How to unlock:</b> {wd.HowToUnlock(Db)}" + RecipeHint(wd),
                        });
                    }
                    break;
                case 1:
                    foreach (var it in Db.items)
                    {
                        if (it == null) continue;
                        var itd = it;
                        bool known = meta.IsDiscovered("i:" + it.id);
                        var forW = Db.WeaponForItem(it);
                        list.Add(new Entry
                        {
                            name = it.displayName, icon = it.icon, known = known,
                            body = () => known ? CodexText.ItemText(Db, itd) : $"Not discovered yet.\n\nUnlocks together with {(forW != null ? forW.displayName : "?")}.",
                        });
                    }
                    break;
                case 2:
                    foreach (var r in Db.evolutions)
                    {
                        if (r == null) continue;
                        var rr = r;
                        bool baseKnown = meta.IsDiscovered("w:" + r.weapon.id);
                        bool done = meta.IsDiscovered("evo:" + r.result.id);
                        list.Add(new Entry
                        {
                            name = $"{r.weapon.displayName} -> {r.result.displayName}", icon = r.result.icon, known = baseKnown, check = done,
                            body = () => baseKnown
                                ? $"<b>{rr.weapon.displayName}</b> (level {rr.weapon.MaxLevel}) + <b>{rr.item.displayName}</b> (any level)\n-> <color=#FFD54A><b>{rr.result.displayName}</b></color>\n\n{rr.result.description}\n\n" +
                                  "A guaranteed EVOLUTION card appears in your next level-up once you meet the requirements. The weapon keeps its slot.\n\n" +
                                  (done ? "<color=#7CF08A>You have performed this evolution.</color>" : "<color=#B8B0D0>Not performed yet.</color>")
                                : $"Discover {rr.weapon.displayName} first. {rr.weapon.HowToUnlock(Db)}",
                        });
                    }
                    break;
                case 3:
                    foreach (var r in Db.fusions)
                    {
                        if (r == null) continue;
                        var rr = r;
                        bool known = meta.IsDiscovered("w:" + r.a.id) || meta.IsDiscovered("w:" + r.b.id);
                        bool done = meta.IsDiscovered("fus:" + r.result.id);
                        list.Add(new Entry
                        {
                            name = $"{r.a.displayName} + {r.b.displayName}", icon = r.result.icon, known = known, check = done,
                            body = () => known
                                ? $"<b>{rr.a.displayName}</b> (level {rr.a.MaxLevel}) + <b>{rr.b.displayName}</b> (level {rr.b.MaxLevel}), both un-evolved\n-> <color=#C99BFF><b>{rr.result.displayName}</b></color>\n\n{rr.result.description}\n\n" +
                                  "A guaranteed FUSION card appears once both are maxed. Fusing frees one weapon slot. Fused weapons cannot evolve.\n\n" +
                                  $"{rr.a.displayName}: {rr.a.HowToUnlock(Db)}\n{rr.b.displayName}: {rr.b.HowToUnlock(Db)}\n\n" +
                                  (done ? "<color=#7CF08A>You have performed this fusion.</color>" : "<color=#B8B0D0>Not performed yet.</color>")
                                : "Discover one of the two weapons first.",
                        });
                    }
                    break;
                case 4:
                    foreach (var b in Db.biomes)
                    {
                        foreach (var e in Roster(b))
                        {
                            var ed = e;
                            bool known = meta.IsDiscovered("e:" + e.id);
                            string tierTag = e.tier == EnemyTier.Boss ? " (Boss)" : e.tier == EnemyTier.MiniBoss ? " (Mini)" : "";
                            list.Add(new Entry
                            {
                                name = e.displayName + tierTag, sprite = Art.Enemy(e), icon = ArtId.Circle, known = known,
                                body = () => known ? CodexText.EnemyText(Db, ed) : $"Not encountered yet.\n\nFound in {b.displayName}.",
                            });
                        }
                    }
                    break;
                case 5:
                    foreach (var b in Db.biomes)
                    {
                        var bd = b;
                        bool known = meta.IsDiscovered("b:" + b.id) || meta.IsBiomeUnlocked(b.index);
                        list.Add(new Entry
                        {
                            name = b.displayName, sprite = b.boss != null ? Art.Enemy(b.boss) : null, icon = ArtId.UiStar, known = known, check = meta.IsBiomeCleared(b),
                            body = () => known ? BiomeText(bd) : $"Locked. Complete 3 matches in {Db.biomes[Mathf.Max(0, bd.index - 1)].displayName}.",
                        });
                    }
                    break;
                default:
                    foreach (StatId s in Enum.GetValues(typeof(StatId)))
                    {
                        var sd = s;
                        list.Add(new Entry { name = StatInfo.Name(s), icon = ArtId.UiStar, known = true, body = () => StatText(sd) });
                    }
                    list.Add(new Entry { name = "Lifetime", icon = ArtId.UiGoldIcon, known = true, body = LifetimeText });
                    break;
            }
            return list;
        }

        string RecipeHint(WeaponDefinition w)
        {
            if (w.form == WeaponForm.Evolved)
            {
                var r = Db.EvolutionProducing(w);
                if (r != null && Meta.IsDiscovered("w:" + r.weapon.id)) return $"\n\n<color=#FFD54A>Recipe: {r.weapon.displayName} (max level) + {r.item.displayName}</color>";
            }
            if (w.form == WeaponForm.Fused)
            {
                var r = Db.FusionProducing(w);
                if (r != null && (Meta.IsDiscovered("w:" + r.a.id) || Meta.IsDiscovered("w:" + r.b.id))) return $"\n\n<color=#C99BFF>Recipe: {r.a.displayName} + {r.b.displayName} (both max level)</color>";
            }
            return "";
        }

        static IEnumerable<EnemyDefinition> Roster(BiomeDefinition b)
        {
            foreach (var n in b.normals) if (n != null) yield return n;
            foreach (var n in b.miniBosses) if (n != null) yield return n;
            if (b.boss != null) yield return b.boss;
        }

        string BiomeText(BiomeDefinition b)
        {
            var sb = new StringBuilder();
            sb.Append(b.description).Append("\n\n");
            sb.Append($"<b>Hazard:</b> {MatchSelectScreen.HazardText(b.hazard)}\n");
            sb.Append("<b>Matches:</b> ");
            for (int i = 0; i < b.matchStyles.Length; i++) sb.Append(i > 0 ? ", " : "").Append(MatchSelectScreen.StyleName(b.matchStyles[i]));
            sb.Append($"\n<b>Progress:</b> {Meta.CompletedMatches(b)}/3 matches completed\n<b>Kill Count target:</b> {b.killTarget}\n<b>Difficulty:</b> x{b.difficulty:0.##}\n\n<b>Roster</b>\n");
            foreach (var e in Roster(b)) sb.Append(Meta.IsDiscovered("e:" + e.id) ? e.displayName : "???").Append(e.tier != EnemyTier.Normal ? (e.tier == EnemyTier.Boss ? " (Boss)" : " (Mini boss)") : "").Append('\n');
            var unlocks = new List<string>();
            foreach (var w in Db.BaseWeapons) if (w.unlockBiome == b.index) unlocks.Add(w.displayName);
            if (unlocks.Count > 0) sb.Append($"\n<b>Clearing it unlocks:</b> {string.Join(", ", unlocks)}");
            return sb.ToString();
        }

        string StatText(StatId s)
        {
            var block = new StatBlock();
            block.Add(Db.config.baseStats);
            float baseVal = block[s];
            var meta = new StatBlock();
            Meta.AccumulateStats(meta);
            var sb = new StringBuilder();
            sb.Append(StatInfo.Describe(s)).Append("\n\n");
            sb.Append($"<b>Base:</b> {StatInfo.FormatValue(s, baseVal)}\n<b>From the skill tree:</b> {StatInfo.FormatMod(s, meta[s])}\n\n<b>Raised by</b>\n");
            foreach (var n in Db.skillNodes) if (n.perRank.stat == s) sb.Append($"Skill: {n.displayName} ({StatInfo.FormatMod(s, n.perRank.value)} per rank)\n");
            foreach (var it in Db.items)
            {
                if (it == null) continue;
                bool has = false;
                foreach (var l in it.levels) foreach (var m in l.mods) if (m.stat == s) has = true;
                if (has) sb.Append($"Item: {it.displayName}\n");
            }
            return sb.ToString();
        }

        string LifetimeText()
        {
            var st = Meta.Data.stats;
            return $"<b>Runs:</b> {st.runs}\n<b>Wins:</b> {st.wins}\n<b>Kills:</b> {st.kills}\n<b>Bosses defeated:</b> {st.bossKills}\n" +
                   $"<b>Longest run:</b> {Ui.Time(st.bestSurvived)}\n<b>Evolutions:</b> {st.evolutions}\n<b>Fusions:</b> {st.fusions}\n" +
                   $"<b>Time played:</b> {st.playSeconds / 60f:0} min\n<b>Gold earned (lifetime):</b> {Meta.Data.lifetimeGold}";
        }
    }
}
