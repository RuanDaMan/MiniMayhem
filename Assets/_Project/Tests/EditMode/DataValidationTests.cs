using System.Collections.Generic;
using System.Linq;
using MiniMayhem.EditorTools;
using NUnit.Framework;
using UnityEditor;

namespace MiniMayhem.Tests
{
    /// <summary>Validates the generated content against the design rules (nothing references missing data).</summary>
    public class DataValidationTests
    {
        GameDatabase db;

        [OneTimeSetUp]
        public void Load()
        {
            db = AssetDatabase.LoadAssetAtPath<GameDatabase>(ProjectBuilder.DatabasePath);
            Assert.IsNotNull(db, "GameDatabase exists (run Mini Mayhem > Rebuild Project Assets)");
        }

        [Test]
        public void TwelveBaseWeapons_WithFiveLevels_AndUniqueIds()
        {
            var bases = db.BaseWeapons.ToList();
            Assert.AreEqual(12, bases.Count);
            foreach (var w in bases) Assert.AreEqual(5, w.MaxLevel, w.id);
            foreach (var w in db.weapons.Where(w => !w.IsBase)) Assert.AreEqual(1, w.MaxLevel, w.id);
            Assert.AreEqual(db.weapons.Count, db.weapons.Select(w => w.id).Distinct().Count(), "unique weapon ids");
            Assert.AreEqual(4, bases.Count(w => w.startsUnlocked), "4 starter weapons");
            foreach (var w in bases.Where(w => !w.startsUnlocked))
                Assert.That(w.unlockBiome >= 0 && w.unlockBiome < db.biomes.Count, $"{w.id} unlocks from a real biome");
        }

        [Test]
        public void EveryBaseWeapon_HasOneEvolution_WithItsOwnItem()
        {
            Assert.AreEqual(12, db.evolutions.Count);
            foreach (var w in db.BaseWeapons)
            {
                var r = db.EvolutionFor(w);
                Assert.IsNotNull(r, $"{w.id} evolves");
                Assert.IsNotNull(r.item, $"{w.id} evolution item");
                Assert.IsNotNull(r.result, $"{w.id} evolved form");
                Assert.AreEqual(WeaponForm.Evolved, r.result.form);
                Assert.IsTrue(db.items.Contains(r.item), "item is in the database");
                Assert.IsTrue(db.weapons.Contains(r.result), "evolved weapon is in the database");
                Assert.IsFalse(r.item.general, "evolution items are not general items");
            }
            Assert.AreEqual(12, db.evolutions.Select(e => e.item).Distinct().Count(), "one distinct item per weapon");
        }

        [Test]
        public void FourFusions_UseTwoDifferentBaseWeapons()
        {
            Assert.AreEqual(4, db.fusions.Count);
            foreach (var f in db.fusions)
            {
                Assert.IsTrue(f.a.IsBase && f.b.IsBase, f.result.id);
                Assert.AreNotEqual(f.a, f.b);
                Assert.AreEqual(WeaponForm.Fused, f.result.form);
                Assert.IsTrue(db.weapons.Contains(f.result));
            }
            var used = db.fusions.SelectMany(f => new[] { f.a, f.b }).ToList();
            Assert.AreEqual(used.Count, used.Distinct().Count(), "each weapon is in at most one fusion");
        }

        [Test]
        public void Items_HaveFiveLevels_AndModifiers()
        {
            Assert.AreEqual(19, db.items.Count);
            Assert.AreEqual(7, db.items.Count(i => i.general));
            foreach (var i in db.items)
            {
                Assert.AreEqual(5, i.MaxLevel, i.id);
                foreach (var l in i.levels) Assert.IsNotEmpty(l.mods, i.id);
            }
        }

        [Test]
        public void SixBiomes_WithFullRosters_AndAlternatingMaps()
        {
            Assert.AreEqual(6, db.biomes.Count);
            for (int i = 0; i < db.biomes.Count; i++)
            {
                var b = db.biomes[i];
                Assert.AreEqual(i, b.index, b.id);
                Assert.AreEqual(6, b.normals.Length, b.id);
                Assert.IsTrue(b.normals.All(n => n != null && n.tier == EnemyTier.Normal), b.id);
                Assert.AreEqual(2, b.miniBosses.Length, b.id);
                Assert.IsTrue(b.miniBosses.All(n => n != null && n.tier == EnemyTier.MiniBoss), b.id);
                Assert.IsNotNull(b.boss, b.id);
                Assert.AreEqual(EnemyTier.Boss, b.boss.tier);
                Assert.IsNotNull(b.waves, b.id);
                Assert.AreEqual(3, b.matchStyles.Length);
                Assert.AreEqual(b.matchStyles[0], b.matchStyles[2], "styles alternate");
                Assert.AreNotEqual(b.matchStyles[0], b.matchStyles[1], "styles alternate");
                if (i > 0) Assert.AreNotEqual(db.biomes[i - 1].matchStyles[0] == MapStyle.Endless, b.matchStyles[0] == MapStyle.Endless, "pattern flips each biome");
                Assert.Greater(b.difficulty, i > 0 ? db.biomes[i - 1].difficulty : 0f, "difficulty rises");
                Assert.AreEqual(3, b.waves.bossTimes.Length);
                CollectionAssert.AreEqual(new[] { 180f, 360f, 540f }, b.waves.bossTimes, "boss every 3:00");
                foreach (var e in b.waves.entries) Assert.IsNotNull(e.enemy, $"{b.id} wave entry at {e.time}");
            }
            foreach (var e in db.enemies)
            {
                if (e.behaviour == EnemyBehaviour.Splitter) Assert.IsNotNull(e.child, e.id);
                if (e.IsBossLike) Assert.IsNotEmpty(e.patterns, e.id);
            }
            Assert.AreEqual(db.enemies.Count, db.enemies.Select(e => e.id).Distinct().Count(), "unique enemy ids");
            Assert.GreaterOrEqual(db.enemies.Count, 54);
        }

        [Test]
        public void SkillTree_IsConnectedFromTheRoot()
        {
            var roots = db.skillNodes.Where(n => n.prerequisites == null || n.prerequisites.Length == 0).ToList();
            Assert.AreEqual(1, roots.Count, "single root");
            Assert.AreEqual("core", roots[0].id);
            var reached = new HashSet<SkillNodeDefinition> { roots[0] };
            bool grew = true;
            while (grew)
            {
                grew = false;
                foreach (var n in db.skillNodes)
                    if (!reached.Contains(n) && n.prerequisites.Any(reached.Contains)) { reached.Add(n); grew = true; }
            }
            Assert.AreEqual(db.skillNodes.Count, reached.Count, "every node reachable");
            foreach (var s in new[] { StatId.Rerolls, StatId.Skips, StatId.Banishes, StatId.Revives, StatId.DeathKeep, StatId.CardChoices })
                Assert.IsTrue(db.skillNodes.Any(n => n.perRank.stat == s), $"tree has {s}");
            Assert.AreEqual(db.skillNodes.Count, db.skillNodes.Select(n => n.id).Distinct().Count());
        }

        [Test]
        public void XpCurve_GivesRoughlyThirtyLevelsForATypicalRun()
        {
            var cfg = db.config;
            int total = 0;
            for (int l = 1; l <= 28; l++) total += cfg.XpToNext(l);
            Assert.That(total, Is.InRange(1000, 5000), "~25-30 level ups from a 10 minute run's XP");
            Assert.Greater(cfg.XpToNext(2), cfg.XpToNext(1));
        }
    }
}
