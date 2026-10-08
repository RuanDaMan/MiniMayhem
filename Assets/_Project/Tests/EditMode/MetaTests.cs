using System.IO;
using MiniMayhem.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MiniMayhem.Tests
{
    /// <summary>Meta progression: skill tree costs/purchases/respec, biome unlocks, weapon unlocks, saving.</summary>
    public class MetaTests
    {
        GameDatabase db;
        SaveService save;
        MetaService meta;

        [SetUp]
        public void Setup()
        {
            db = AssetDatabase.LoadAssetAtPath<GameDatabase>(ProjectBuilder.DatabasePath);
            SaveService.PathOverride = Path.Combine(Application.temporaryCachePath, "minimayhem_edit_test.json");
            if (File.Exists(SaveService.PathOverride)) File.Delete(SaveService.PathOverride);
            save = new SaveService();
            save.Load();
            meta = new MetaService(db, save);
        }

        [TearDown]
        public void Teardown() => SaveService.PathOverride = null;

        [Test]
        public void SkillCost_Escalates()
        {
            var n = db.Node("v_hp");
            Assert.AreEqual(Mathf.RoundToInt(n.baseCost), n.Cost(0));
            Assert.Greater(n.Cost(2), n.Cost(1));
            Assert.AreEqual(Mathf.RoundToInt(n.baseCost * Mathf.Pow(n.costGrowth, 3)), n.Cost(3));
        }

        [Test]
        public void Buying_RequiresGold_AndAPrerequisite_ThenRespecRefundsEverything()
        {
            var core = db.Node("core");
            var hp = db.Node("v_hp");
            Assert.IsFalse(meta.CanBuy(core), "no gold yet");
            meta.AddGold(1000);
            Assert.IsFalse(meta.CanBuy(hp), "needs the root first");
            Assert.IsTrue(meta.Buy(core));
            Assert.IsTrue(meta.Buy(hp));
            Assert.IsTrue(meta.Buy(hp));
            Assert.AreEqual(2, meta.Rank(hp));
            int spent = core.Cost(0) + hp.Cost(0) + hp.Cost(1);
            Assert.AreEqual(1000 - spent, meta.Gold);
            var stats = new StatBlock();
            meta.AccumulateStats(stats);
            Assert.AreEqual(core.perRank.value + hp.perRank.value * 2, stats[StatId.MaxHp], 1e-4);
            Assert.AreEqual(spent, meta.Respec());
            Assert.AreEqual(1000, meta.Gold);
            Assert.AreEqual(0, meta.Rank(hp));
        }

        [Test]
        public void ThreeCompletedMatches_UnlockTheNextBiome_AndItsWeapons()
        {
            var meadow = db.biomes[0];
            Assert.IsTrue(meta.IsBiomeUnlocked(0));
            Assert.IsFalse(meta.IsBiomeUnlocked(1));
            var boomerang = db.Weapon("boomerang");
            Assert.IsFalse(meta.IsWeaponUnlocked(boomerang));
            meta.CompleteMatch(meadow, 0);
            meta.CompleteMatch(meadow, 0); // replaying the same match does not count twice
            meta.CompleteMatch(meadow, 1);
            Assert.IsFalse(meta.IsBiomeUnlocked(1), "2 of 3");
            var msgs = meta.CompleteMatch(meadow, 2);
            Assert.IsTrue(meta.IsBiomeUnlocked(1), "3 of 3 unlocks the swamp");
            Assert.IsTrue(meta.IsWeaponUnlocked(boomerang));
            Assert.IsTrue(meta.IsWeaponUnlocked(db.Weapon("zap_rod")));
            Assert.IsTrue(meta.IsItemAvailable(db.Item("sticky_glove")), "evolution item unlocks with its weapon");
            Assert.IsTrue(msgs.Exists(m => m.Contains("Swamp")));
        }

        [Test]
        public void Save_RoundTrips()
        {
            meta.AddGold(321);
            meta.AddGold(1);
            meta.Buy(db.Node("core"));
            meta.CompleteMatch(db.biomes[0], 1);
            meta.Discover("e:slime");
            meta.Data.settings.music = 0.3f;
            meta.Save();
            var save2 = new SaveService();
            save2.Load();
            Assert.IsTrue(save2.LoadedFromDisk);
            var meta2 = new MetaService(db, save2);
            Assert.AreEqual(meta.Gold, meta2.Gold);
            Assert.AreEqual(1, meta2.Rank(db.Node("core")));
            Assert.IsTrue(meta2.IsMatchCompleted(db.biomes[0], 1));
            Assert.IsTrue(meta2.IsDiscovered("e:slime"));
            Assert.AreEqual(0.3f, meta2.Data.settings.music, 1e-4);
        }

        [Test]
        public void ItemStats_Stack_PerLevel()
        {
            var stats = new PlayerStats(db.config);
            var sneakers = db.Item("sneakers");
            float baseSpeed = stats.MoveSpeed;
            sneakers.Accumulate(3, stats.Items);
            stats.Recompute();
            Assert.AreEqual(baseSpeed * 1.24f, stats.MoveSpeed, 1e-3);
            Assert.AreEqual(3, stats.CardChoices);
            Assert.AreEqual(0.5f, stats.DeathKeep, 1e-4);
        }
    }
}
