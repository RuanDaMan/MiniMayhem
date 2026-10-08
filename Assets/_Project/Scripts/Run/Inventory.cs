using System.Collections.Generic;
using UnityEngine;

namespace MiniMayhem
{
    public class OwnedItem
    {
        public ItemDefinition def;
        public int level = 1;
        public bool IsMax => level >= def.MaxLevel;
    }

    /// <summary>
    /// The run's weapons (max 6) and items (max 6), plus evolution / fusion rules:
    /// evolve = base weapon at max level + its item owned (any level); fuse = two specific max-level base weapons
    /// (frees a slot). Evolved / fused weapons never level, evolve or fuse again.
    /// </summary>
    public class Inventory
    {
        readonly RunController run;
        readonly GameDatabase db;
        int nextHitGroup;

        public readonly List<WeaponInstance> Weapons = new();
        public readonly List<OwnedItem> Items = new();
        /// <summary>Ids removed from the card pool (banished, or consumed by a fusion).</summary>
        public readonly HashSet<string> Banished = new();
        public readonly HashSet<string> Consumed = new();
        public int Evolutions, Fusions;

        public Inventory(RunController run, GameDatabase db)
        {
            this.run = run;
            this.db = db;
        }

        public int MaxWeapons => run.Config.maxWeapons;
        public int MaxItems => run.Config.maxItems;
        public bool WeaponSlotFree => Weapons.Count < MaxWeapons;
        public bool ItemSlotFree => Items.Count < MaxItems;

        public WeaponInstance Find(WeaponDefinition def)
        {
            foreach (var w in Weapons) if (w.def == def) return w;
            return null;
        }

        public OwnedItem Find(ItemDefinition def)
        {
            foreach (var i in Items) if (i.def == def) return i;
            return null;
        }

        public bool Owns(WeaponDefinition def) => Find(def) != null;
        public bool Owns(ItemDefinition def) => Find(def) != null;

        public WeaponInstance AddWeapon(WeaponDefinition def)
        {
            if (def == null || !WeaponSlotFree || Owns(def)) return null;
            var w = WeaponInstance.Create(def, run, nextHitGroup++);
            Weapons.Add(w);
            run.Meta?.Discover("w:" + def.id);
            return w;
        }

        public bool LevelWeapon(WeaponInstance w, int levels = 1)
        {
            if (w == null || !w.CanLevel) return false;
            w.level = Mathf.Min(w.def.MaxLevel, w.level + levels);
            w.OnLevelChanged();
            return true;
        }

        public OwnedItem AddItem(ItemDefinition def)
        {
            if (def == null || !ItemSlotFree || Owns(def)) return null;
            var it = new OwnedItem { def = def };
            Items.Add(it);
            run.Meta?.Discover("i:" + def.id);
            RecomputeItemStats();
            return it;
        }

        public bool LevelItem(OwnedItem it, int levels = 1)
        {
            if (it == null || it.IsMax) return false;
            it.level = Mathf.Min(it.def.MaxLevel, it.level + levels);
            RecomputeItemStats();
            return true;
        }

        public void RecomputeItemStats()
        {
            float oldMax = run.Stats.MaxHp;
            run.Stats.Items.Clear();
            foreach (var it in Items) it.def.Accumulate(it.level, run.Stats.Items);
            run.Stats.Recompute();
            float gained = run.Stats.MaxHp - oldMax;
            if (gained > 0 && run.Hero != null) run.Hero.Hp += gained;
        }

        // ------------------------------------------------------------------ evolution & fusion

        public List<EvolutionRecipe> AvailableEvolutions()
        {
            var list = new List<EvolutionRecipe>();
            foreach (var r in db.evolutions)
            {
                if (r == null || r.result == null) continue;
                var w = Find(r.weapon);
                if (w == null || !w.def.IsBase || !w.IsMaxLevel) continue;
                if (!Owns(r.item)) continue;
                list.Add(r);
            }
            return list;
        }

        public List<FusionRecipe> AvailableFusions()
        {
            var list = new List<FusionRecipe>();
            foreach (var r in db.fusions)
            {
                if (r == null || r.result == null) continue;
                var a = Find(r.a);
                var b = Find(r.b);
                if (a == null || b == null) continue;
                if (!a.IsMaxLevel || !b.IsMaxLevel || !a.def.IsBase || !b.def.IsBase) continue;
                list.Add(r);
            }
            return list;
        }

        WeaponInstance Replace(WeaponInstance old, WeaponDefinition with)
        {
            int idx = Weapons.IndexOf(old);
            old.Dispose();
            var w = WeaponInstance.Create(with, run, old.hitGroup);
            w.damageDealt = old.damageDealt;
            w.kills = old.kills;
            Weapons[idx] = w;
            return w;
        }

        public WeaponInstance Evolve(EvolutionRecipe r)
        {
            if (!AvailableEvolutions().Contains(r)) return null;
            var w = Replace(Find(r.weapon), r.result);
            Evolutions++;
            run.Meta?.Discover("w:" + r.result.id);
            run.Meta?.Discover("evo:" + r.result.id);
            return w;
        }

        public WeaponInstance Fuse(FusionRecipe r)
        {
            if (!AvailableFusions().Contains(r)) return null;
            var a = Find(r.a);
            var b = Find(r.b);
            var w = Replace(a, r.result);
            w.damageDealt += b.damageDealt;
            w.kills += b.kills;
            b.Dispose();
            Weapons.Remove(b);
            Consumed.Add(r.a.id);
            Consumed.Add(r.b.id);
            Fusions++;
            run.Meta?.Discover("w:" + r.result.id);
            run.Meta?.Discover("fus:" + r.result.id);
            return w;
        }

        public void Tick(float dt)
        {
            for (int i = 0; i < Weapons.Count; i++) Weapons[i].Tick(dt);
        }

        public void DisposeAll()
        {
            foreach (var w in Weapons) w.Dispose();
        }
    }
}
