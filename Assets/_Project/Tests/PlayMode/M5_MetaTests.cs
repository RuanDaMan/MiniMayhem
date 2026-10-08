using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MiniMayhem.Tests
{
    /// <summary>M5: skill tree shop, saving between sessions, results gold conversion, tree effects in runs.</summary>
    [Timeout(300000)]
    public class M5_MetaTests : MayhemTestBase
    {
        [UnityTest]
        public IEnumerator SkillTree_BuyWithTheGamepad_ThenRespec()
        {
            yield return Boot();
            meta.AddGold(500);
            flow.Go(FlowState.SkillTree);
            yield return null;
            yield return null;
            Assert.AreEqual(FlowState.SkillTree, flow.State);
            var core = db.Node("core");
            yield return TestUtil.Capture("M5_skill_tree_start");
            yield return Tap(pad.buttonSouth); // focus starts on the root: buy it
            Assert.AreEqual(1, meta.Rank(core), "bought the root with A");
            Assert.AreEqual(500 - core.Cost(0), meta.Gold);
            // Move right into the Might branch and buy Muscles.
            yield return Tap(pad.dpad.right);
            yield return Tap(pad.buttonSouth);
            Assert.AreEqual(1, meta.Rank(db.Node("m_damage")), "d-pad moves to the neighbouring node");
            yield return TestUtil.Capture("M5_skill_tree");
            int spent = 500 - meta.Gold;
            yield return Tap(pad.buttonNorth); // respec
            yield return Tap(pad.buttonSouth); // confirm
            Assert.AreEqual(500, meta.Gold, "full refund");
            Assert.AreEqual(0, meta.Rank(core));
            Assert.Greater(spent, 0);
            yield return Tap(pad.buttonEast);
            Assert.AreEqual(FlowState.MatchSelect, flow.State, "B returns Home");
        }

        [UnityTest]
        public IEnumerator TreeStats_ApplyInTheNextRun()
        {
            yield return Boot();
            meta.AddGold(100000);
            foreach (var id in new[] { "core", "v_hp", "v_hp", "f_gold", "f_reroll", "f_reroll", "v_regen", "v_revive" }) Assert.IsTrue(meta.Buy(db.Node(id)), id);
            yield return StartRun();
            Assert.AreEqual(100 + 10 + 20, Run.Stats.MaxHp, 0.01f, "core + 2 ranks of Tough Cookie");
            Assert.AreEqual(2, Run.LevelUp.Rerolls, "reroll unlocked by the tree");
            Assert.AreEqual(1, Run.Hero.RevivesLeft, "revive unlocked by the tree");
            Run.Hero.TakeDamage(9999, "test");
            Assert.IsFalse(Run.Ended, "revived");
            Assert.AreEqual(Run.Stats.MaxHp * 0.5f, Run.Hero.Hp, 0.01f);
            Assert.AreEqual(0, Run.Hero.RevivesLeft);
        }

        [UnityTest]
        public IEnumerator Results_ConvertGold_AndTheSaveSurvivesARestart()
        {
            yield return Boot();
            yield return StartRun();
            Run.State.gold = 100;
            Run.State.kills = 200;
            Run.State.time = 120f;
            Run.Hero.TakeDamage(99999, "test");
            var r = Run.Result;
            Assert.IsFalse(r.won);
            Assert.AreEqual(0.5f, r.keepFraction, 1e-4, "death keeps 50%");
            float expected = (100 + 200 * Run.Config.goldPerKill + 2 * Run.Config.goldPerMinute) * Run.Config.Mode(MatchMode.OutlastTime).rewardMultiplier * 0.5f;
            Assert.AreEqual(Mathf.RoundToInt(expected), r.metaGold, 1);
            Assert.AreEqual(r.metaGold, meta.Gold, "gold banked");
            yield return TestUtil.WaitUntil(() => flow.State == FlowState.Results, 3f);
            yield return TestUtil.Capture("M5_results_defeat");
            int gold = meta.Gold;

            // "Restart the game": reload the scene keeping the save.
            yield return Boot(FlowState.Title, keepSave: true);
            Assert.AreEqual(gold, meta.Gold, "gold persisted");
            Assert.AreEqual(1, meta.Data.stats.runs);
        }

        [UnityTest]
        public IEnumerator DeathKeep_IsRaisedByTheTree_UpToTheCap()
        {
            yield return Boot();
            meta.AddGold(1000000);
            foreach (var id in new[] { "core", "v_hp", "v_regen", "v_revive" }) meta.Buy(db.Node(id));
            for (int i = 0; i < 5; i++) meta.Buy(db.Node("v_keep"));
            yield return StartRun();
            Assert.AreEqual(0.8f, Run.Stats.DeathKeep, 1e-4, "50% + 5 x 6% capped at 80%");
        }
    }
}
