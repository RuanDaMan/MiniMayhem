using System.Collections.Generic;
using UnityEngine;

namespace MiniMayhem
{
    public enum CardKind { NewWeapon, WeaponUpgrade, NewItem, ItemUpgrade, Evolve, Fuse, Heal, Gold, StatBoost }

    public class Card
    {
        public CardKind kind;
        public WeaponDefinition weapon;
        public ItemDefinition item;
        public EvolutionRecipe evolution;
        public FusionRecipe fusion;
        public int levels = 1;
        public string title, tag, description;
        public ArtId icon;
        public Color color = Color.white;

        public string SubjectId => kind switch
        {
            CardKind.NewWeapon or CardKind.WeaponUpgrade => "w:" + weapon.id,
            CardKind.NewItem or CardKind.ItemUpgrade => "i:" + item.id,
            _ => null,
        };

        public bool CanBanish => SubjectId != null;
        public bool IsSpecial => kind == CardKind.Evolve || kind == CardKind.Fuse;
    }

    /// <summary>
    /// Builds the level-up cards: guaranteed Evolve / Fuse cards when their requirements are met, otherwise a
    /// weighted draw of new weapons, new items and upgrades (gently biased towards the missing half of a fusion
    /// and towards evolution items for owned weapons). Handles reroll, skip and banish.
    /// </summary>
    public class LevelUpService
    {
        readonly RunController run;
        readonly GameDatabase db;
        readonly System.Random rnd;

        public List<Card> Current { get; } = new();
        public int Rerolls, Skips, Banishes;

        public LevelUpService(RunController run, GameDatabase db, int seed)
        {
            this.run = run;
            this.db = db;
            rnd = new System.Random(seed);
            Rerolls = run.Stats.Rerolls;
            Skips = run.Stats.Skips;
            Banishes = run.Stats.Banishes;
        }

        Inventory Inv => run.Inventory;

        public List<Card> Roll()
        {
            Current.Clear();
            Current.AddRange(Build(run.Stats.CardChoices, null));
            return Current;
        }

        List<Card> Build(int n, List<Card> exclude)
        {
            var result = new List<Card>();
            bool Excluded(Card c)
            {
                if (exclude == null) return false;
                foreach (var e in exclude)
                    if (e.kind == c.kind && e.weapon == c.weapon && e.item == c.item && e.evolution == c.evolution && e.fusion == c.fusion) return true;
                return false;
            }

            // Guaranteed special cards.
            foreach (var r in Inv.AvailableEvolutions())
            {
                var c = EvolveCard(r);
                if (result.Count < n && !Excluded(c)) result.Add(c);
            }
            foreach (var r in Inv.AvailableFusions())
            {
                var c = FuseCard(r);
                if (result.Count < n && !Excluded(c)) result.Add(c);
            }

            // Weighted pool.
            var pool = new List<(Card card, float w)>();
            var meta = run.Meta;
            if (Inv.WeaponSlotFree)
                foreach (var w in db.BaseWeapons)
                {
                    if (Inv.Owns(w) || Inv.Banished.Contains("w:" + w.id) || Inv.Consumed.Contains(w.id)) continue;
                    if (meta != null && !meta.IsWeaponUnlocked(w)) continue;
                    float weight = 1f;
                    if (IsMissingFusionHalf(w)) weight = 3f;
                    pool.Add((NewWeaponCard(w), weight));
                }
            foreach (var w in Inv.Weapons)
            {
                if (!w.CanLevel || Inv.Banished.Contains("w:" + w.def.id)) continue;
                float weight = 1.2f;
                if (HasMaxedFusionPartner(w.def)) weight = 2.2f;
                pool.Add((UpgradeCard(w), weight));
            }
            if (Inv.ItemSlotFree)
                foreach (var it in db.items)
                {
                    if (it == null || Inv.Owns(it) || Inv.Banished.Contains("i:" + it.id)) continue;
                    if (meta != null && !meta.IsItemAvailable(it)) continue;
                    float weight = 0.9f;
                    var forWeapon = db.WeaponForItem(it);
                    if (forWeapon != null && Inv.Owns(forWeapon)) weight = 1.8f;
                    pool.Add((NewItemCard(it), weight));
                }
            foreach (var it in Inv.Items)
            {
                if (it.IsMax || Inv.Banished.Contains("i:" + it.def.id)) continue;
                pool.Add((ItemUpgradeCard(it), 1f));
            }

            pool.RemoveAll(p => Excluded(p.card));
            while (result.Count < n && pool.Count > 0)
            {
                float total = 0;
                foreach (var p in pool) total += p.w;
                float pick = (float)rnd.NextDouble() * total;
                int idx = 0;
                for (; idx < pool.Count - 1; idx++) { pick -= pool[idx].w; if (pick <= 0) break; }
                var card = pool[idx].card;
                pool.RemoveAt(idx);
                ApplyLuck(card);
                result.Add(card);
            }

            if (result.Count == 0)
            {
                foreach (var c in Fallbacks())
                    if (result.Count < n && !Excluded(c)) result.Add(c);
                if (result.Count == 0) result.AddRange(Fallbacks());
            }
            return result;
        }

        bool IsMissingFusionHalf(WeaponDefinition w)
        {
            foreach (var r in db.FusionsUsing(w))
            {
                var partner = Inv.Find(r.Partner(w));
                if (partner != null && partner.IsMaxLevel && partner.def.IsBase) return true;
            }
            return false;
        }

        bool HasMaxedFusionPartner(WeaponDefinition w)
        {
            foreach (var r in db.FusionsUsing(w))
            {
                var partner = Inv.Find(r.Partner(w));
                if (partner != null && partner.IsMaxLevel) return true;
            }
            return false;
        }

        void ApplyLuck(Card c)
        {
            float chance = Mathf.Min(0.4f, run.Stats.Luck * 0.5f);
            if (chance <= 0 || rnd.NextDouble() >= chance) return;
            if (c.kind == CardKind.WeaponUpgrade)
            {
                var w = Inv.Find(c.weapon);
                if (w != null && w.level + 2 <= w.def.MaxLevel) { c.levels = 2; c.tag = $"LUCKY! Lv {w.level} -> {w.level + 2}"; }
            }
            else if (c.kind == CardKind.ItemUpgrade)
            {
                var it = Inv.Find(c.item);
                if (it != null && it.level + 2 <= it.def.MaxLevel) { c.levels = 2; c.tag = $"LUCKY! Lv {it.level} -> {it.level + 2}"; }
            }
        }

        // ------------------------------------------------------------------ card factories

        static Card NewWeaponCard(WeaponDefinition w) => new()
        {
            kind = CardKind.NewWeapon, weapon = w, title = w.displayName, tag = "NEW WEAPON", icon = w.icon, color = w.color,
            description = w.description,
        };

        static Card UpgradeCard(WeaponInstance w) => new()
        {
            kind = CardKind.WeaponUpgrade, weapon = w.def, title = w.def.displayName, tag = $"Lv {w.level} -> {w.level + 1}", icon = w.def.icon,
            color = w.def.colorMax, description = w.NextLevelText(),
        };

        static Card NewItemCard(ItemDefinition it) => new()
        {
            kind = CardKind.NewItem, item = it, title = it.displayName, tag = "NEW ITEM", icon = it.icon, color = it.color,
            description = $"{it.description}\n<color=#9BE37A>{it.LevelText(1)}</color>",
        };

        static Card ItemUpgradeCard(OwnedItem it) => new()
        {
            kind = CardKind.ItemUpgrade, item = it.def, title = it.def.displayName, tag = $"Lv {it.level} -> {it.level + 1}", icon = it.def.icon,
            color = it.def.color, description = it.def.LevelText(it.level + 1),
        };

        static Card EvolveCard(EvolutionRecipe r) => new()
        {
            kind = CardKind.Evolve, evolution = r, weapon = r.result, title = r.result.displayName, tag = "EVOLUTION!", icon = r.result.icon,
            color = new Color(1f, 0.8f, 0.2f),
            description = $"{r.weapon.displayName} + {r.item.displayName}\n{r.result.description}",
        };

        static Card FuseCard(FusionRecipe r) => new()
        {
            kind = CardKind.Fuse, fusion = r, weapon = r.result, title = r.result.displayName, tag = "FUSION!", icon = r.result.icon,
            color = new Color(0.8f, 0.5f, 1f),
            description = $"{r.a.displayName} + {r.b.displayName} (frees a slot)\n{r.result.description}",
        };

        List<Card> Fallbacks()
        {
            var cfg = run.Config;
            return new List<Card>
            {
                new() { kind = CardKind.Heal, title = "Snack Break", tag = "HEAL", icon = ArtId.HealthPickup, color = new Color(1f, 0.45f, 0.45f), description = $"Restore {cfg.fallbackHeal:0} HP." },
                new() { kind = CardKind.Gold, title = "Gold Bag", tag = "GOLD", icon = ArtId.Coin, color = new Color(1f, 0.85f, 0.3f), description = $"+{cfg.fallbackGold} gold." },
                new() { kind = CardKind.StatBoost, title = "Power Snack", tag = "BOOST", icon = ArtId.UiStar, color = new Color(0.5f, 0.9f, 1f), description = $"+{cfg.fallbackStatBoost * 100:0}% Damage for this run." },
            };
        }

        // ------------------------------------------------------------------ actions

        public void Apply(Card c)
        {
            switch (c.kind)
            {
                case CardKind.NewWeapon: Inv.AddWeapon(c.weapon); break;
                case CardKind.WeaponUpgrade: Inv.LevelWeapon(Inv.Find(c.weapon), c.levels); break;
                case CardKind.NewItem: Inv.AddItem(c.item); break;
                case CardKind.ItemUpgrade: Inv.LevelItem(Inv.Find(c.item), c.levels); break;
                case CardKind.Evolve:
                    if (Inv.Evolve(c.evolution) != null) { run.Announce($"{c.evolution.result.displayName}!", new Color(1f, 0.85f, 0.3f)); Sfx.Play(SfxId.Evolve, 0.8f); run.Fx.Spawn(ArtId.Ring, run.Hero.Position, 2f, new Color(1f, 0.85f, 0.3f), 0.7f, growTo: 5f); }
                    break;
                case CardKind.Fuse:
                    if (Inv.Fuse(c.fusion) != null) { run.Announce($"{c.fusion.result.displayName}!", new Color(0.85f, 0.55f, 1f)); Sfx.Play(SfxId.Fuse, 0.8f); run.Fx.Spawn(ArtId.Ring, run.Hero.Position, 2f, new Color(0.85f, 0.55f, 1f), 0.7f, growTo: 5f); }
                    break;
                case CardKind.Heal: run.Hero.Heal(run.Config.fallbackHeal); break;
                case CardKind.Gold: run.State.gold += run.Config.fallbackGold; break;
                case CardKind.StatBoost: run.Stats.RunBoosts.Add(StatId.Damage, run.Config.fallbackStatBoost); run.Stats.Recompute(); break;
            }
            Consume();
        }

        public bool Reroll()
        {
            if (Rerolls <= 0) return false;
            Rerolls--;
            var old = new List<Card>(Current);
            Current.Clear();
            var fresh = Build(run.Stats.CardChoices, old);
            // If nothing new is possible, allow repeats rather than an empty hand.
            Current.AddRange(fresh.Count > 0 ? fresh : Build(run.Stats.CardChoices, null));
            return true;
        }

        public bool Skip()
        {
            if (Skips <= 0) return false;
            Skips--;
            run.State.gold += run.Config.skipGold;
            run.Hero.Heal(run.Config.skipHeal);
            Consume();
            return true;
        }

        /// <summary>Removes the card's weapon/item from the pool for this run and replaces the card.</summary>
        public bool Banish(int index)
        {
            if (Banishes <= 0 || index < 0 || index >= Current.Count || !Current[index].CanBanish) return false;
            Banishes--;
            Inv.Banished.Add(Current[index].SubjectId);
            var replacement = Build(1, Current);
            if (replacement.Count > 0 && replacement[0].SubjectId != Current[index].SubjectId) Current[index] = replacement[0];
            else Current.RemoveAt(index);
            if (Current.Count == 0) Current.AddRange(Fallbacks());
            return true;
        }

        void Consume()
        {
            run.State.pendingLevelUps = Mathf.Max(0, run.State.pendingLevelUps - 1);
            Current.Clear();
        }
    }
}
