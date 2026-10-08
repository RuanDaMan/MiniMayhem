using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiniMayhem
{
    /// <summary>
    /// Everything that persists between runs: meta gold, the skill tree, biome progress, weapon unlocks, codex
    /// discovery and lifetime stats. Pure C# on top of SaveService so it is easy to test.
    /// </summary>
    public class MetaService
    {
        readonly GameDatabase db;
        readonly SaveService save;

        public event Action Changed;

        public MetaService(GameDatabase db, SaveService save)
        {
            this.db = db;
            this.save = save;
            EnsureStarterUnlocks();
        }

        public SaveData Data => save.Data;
        public int Gold => Data.gold;
        public GameDatabase Database => db;

        public void Save() => save.Save();
        public void NotifyChanged() => Changed?.Invoke();

        void EnsureStarterUnlocks()
        {
            foreach (var w in db.BaseWeapons)
                if (w.startsUnlocked) Discover("w:" + w.id);
            foreach (var i in db.items)
                if (i != null && IsItemAvailable(i)) Discover("i:" + i.id);
            if (db.biomes.Count > 0) Discover("b:" + db.biomes[0].id);
        }

        // ------------------------------------------------------------------ gold

        public void AddGold(int amount)
        {
            if (amount <= 0) return;
            Data.gold += amount;
            Data.lifetimeGold += amount;
            Changed?.Invoke();
        }

        // ------------------------------------------------------------------ skill tree

        public int Rank(SkillNodeDefinition n)
        {
            if (n == null) return 0;
            foreach (var e in Data.skills) if (e.id == n.id) return e.rank;
            return 0;
        }

        void SetRank(SkillNodeDefinition n, int rank)
        {
            for (int i = 0; i < Data.skills.Count; i++)
                if (Data.skills[i].id == n.id) { Data.skills[i] = new SaveData.RankEntry { id = n.id, rank = rank }; return; }
            Data.skills.Add(new SaveData.RankEntry { id = n.id, rank = rank });
        }

        public bool IsReachable(SkillNodeDefinition n)
        {
            if (n.prerequisites == null || n.prerequisites.Length == 0) return true;
            foreach (var p in n.prerequisites) if (p != null && Rank(p) > 0) return true;
            return false;
        }

        public bool IsMaxed(SkillNodeDefinition n) => Rank(n) >= n.maxRank;
        public int NextCost(SkillNodeDefinition n) => n.Cost(Rank(n));
        public bool CanBuy(SkillNodeDefinition n) => n != null && !IsMaxed(n) && IsReachable(n) && Gold >= NextCost(n);

        public bool Buy(SkillNodeDefinition n)
        {
            if (!CanBuy(n)) return false;
            Data.gold -= NextCost(n);
            SetRank(n, Rank(n) + 1);
            Changed?.Invoke();
            return true;
        }

        public int SpentOnTree()
        {
            int total = 0;
            foreach (var n in db.skillNodes)
            {
                if (n == null) continue;
                int r = Rank(n);
                for (int k = 0; k < r; k++) total += n.Cost(k);
            }
            return total;
        }

        /// <summary>Full refund of every node (no penalty).</summary>
        public int Respec()
        {
            int refund = SpentOnTree();
            Data.gold += refund;
            Data.skills.Clear();
            Changed?.Invoke();
            return refund;
        }

        /// <summary>Sum of all skill-tree modifiers.</summary>
        public void AccumulateStats(StatBlock into)
        {
            foreach (var n in db.skillNodes)
            {
                if (n == null) continue;
                int r = Rank(n);
                if (r > 0) into.Add(n.perRank, r);
            }
        }

        // ------------------------------------------------------------------ biomes & matches

        SaveData.BiomeEntry Entry(BiomeDefinition b, bool create)
        {
            foreach (var e in Data.biomes) if (e.id == b.id) return e;
            if (!create) return null;
            var n = new SaveData.BiomeEntry { id = b.id };
            Data.biomes.Add(n);
            return n;
        }

        public bool IsMatchCompleted(BiomeDefinition b, int match)
        {
            var e = Entry(b, false);
            return e != null && (e.completedMask & (1 << match)) != 0;
        }

        public int CompletedMatches(BiomeDefinition b)
        {
            var e = Entry(b, false);
            if (e == null) return 0;
            int c = 0;
            for (int i = 0; i < 3; i++) if ((e.completedMask & (1 << i)) != 0) c++;
            return c;
        }

        public bool IsBiomeCleared(BiomeDefinition b) => CompletedMatches(b) >= db.config.matchesToUnlockNextBiome;

        public bool IsBiomeUnlocked(int index)
        {
            if (index <= 0) return true;
            if (index >= db.biomes.Count) return false;
            return IsBiomeCleared(db.biomes[index - 1]);
        }

        public void RecordPlay(BiomeDefinition b) { Entry(b, true).plays++; }

        /// <summary>Marks a match as completed. Returns human readable unlock messages (new biome, new weapons).</summary>
        public List<string> CompleteMatch(BiomeDefinition b, int match)
        {
            var msgs = new List<string>();
            var e = Entry(b, true);
            bool wasCleared = IsBiomeCleared(b);
            e.completedMask |= 1 << match;
            e.wins++;
            if (!wasCleared && IsBiomeCleared(b))
            {
                msgs.Add($"{b.displayName} cleared!");
                int next = b.index + 1;
                if (next < db.biomes.Count)
                {
                    Discover("b:" + db.biomes[next].id);
                    msgs.Add($"New biome unlocked: {db.biomes[next].displayName}");
                }
                foreach (var w in db.BaseWeapons)
                {
                    if (w.unlockBiome != b.index || IsWeaponUnlocked(w)) continue;
                    Data.unlockedWeapons.Add(w.id);
                    Discover("w:" + w.id);
                    var evo = db.EvolutionFor(w);
                    if (evo != null && evo.item != null) Discover("i:" + evo.item.id);
                    msgs.Add($"New weapon unlocked: {w.displayName}");
                }
            }
            Changed?.Invoke();
            return msgs;
        }

        // ------------------------------------------------------------------ weapons & items

        public bool IsWeaponUnlocked(WeaponDefinition w) => w != null && (w.startsUnlocked || Data.unlockedWeapons.Contains(w.id));

        /// <summary>General items are always in the pool; evolution items come with their weapon.</summary>
        public bool IsItemAvailable(ItemDefinition i)
        {
            if (i == null) return false;
            if (i.general) return true;
            var w = db.WeaponForItem(i);
            return w == null || IsWeaponUnlocked(w);
        }

        // ------------------------------------------------------------------ codex

        public bool IsDiscovered(string key) => Data.discovered.Contains(key);

        public bool Discover(string key)
        {
            if (Data.discovered.Contains(key)) return false;
            Data.discovered.Add(key);
            return true;
        }

        public void ResetAll()
        {
            save.Wipe();
            EnsureStarterUnlocks();
            Changed?.Invoke();
        }
    }
}
