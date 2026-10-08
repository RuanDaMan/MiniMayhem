using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MiniMayhem.Tests
{
    /// <summary>M1: hero, endless map, weapons, enemies, XP, level-up popup, boss.</summary>
    [Timeout(120000)]
    public class M1_VerticalSliceTests : MayhemTestBase
    {
        [UnityTest]
        public IEnumerator Menus_AreDrivableWithTheGamepad()
        {
            yield return Boot();
            Assert.AreEqual(FlowState.Title, flow.State);
            yield return TestUtil.Capture("M1_title");
            yield return Tap(pad.buttonSouth); // Play
            Assert.AreEqual(FlowState.MatchSelect, flow.State);
            yield return TestUtil.Capture("M1_match_select");
            yield return Tap(pad.buttonSouth); // first match
            Assert.AreEqual(FlowState.WeaponPick, flow.State);
            yield return TestUtil.Capture("M1_weapon_pick");
            yield return Tap(pad.buttonEast); // back
            Assert.AreEqual(FlowState.MatchSelect, flow.State);
            yield return Tap(pad.buttonSouth);
            yield return Tap(pad.buttonSouth); // start with the default weapon
            Assert.AreEqual(FlowState.Running, flow.State);
            Assert.IsNotNull(Run);
            Assert.AreEqual(1, Run.Inventory.Weapons.Count, "starts with the picked weapon");
        }

        [UnityTest]
        public IEnumerator Hub_PagesSlideLeftAndRight_FromHome()
        {
            yield return Boot();
            Assert.AreEqual(3, flow.Title.Root.GetComponentsInChildren<UnityEngine.UI.Button>().Length, "splash: Play, Settings, Quit");
            yield return Tap(pad.buttonSouth);
            Assert.AreEqual(FlowState.MatchSelect, flow.State, "Play opens Home");
            Assert.IsTrue(flow.HubBar.Visible);
            yield return TestUtil.Capture("M1_home");
            yield return Tap(pad.leftShoulder);
            Assert.AreEqual(FlowState.SkillTree, flow.State);
            yield return Tap(pad.leftShoulder);
            Assert.AreEqual(FlowState.Characters, flow.State);
            yield return TestUtil.Capture("M1_characters");
            yield return Tap(pad.leftShoulder);
            Assert.AreEqual(FlowState.Characters, flow.State, "stops at the left edge");
            yield return Tap(pad.rightShoulder);
            yield return Tap(pad.rightShoulder);
            yield return Tap(pad.rightShoulder);
            Assert.AreEqual(FlowState.Codex, flow.State);
            yield return Tap(pad.rightShoulder);
            Assert.AreEqual(FlowState.Cosmetics, flow.State);
            yield return Tap(pad.buttonEast);
            Assert.AreEqual(FlowState.MatchSelect, flow.State, "B goes back Home");
            yield return Tap(pad.buttonEast);
            Assert.AreEqual(FlowState.Title, flow.State, "B from Home goes to the splash");
            Assert.IsFalse(flow.HubBar.Visible);
        }

        [UnityTest]
        public IEnumerator Hero_MovesWithTheLeftStick()
        {
            yield return Boot();
            yield return StartRun();
            var start = Run.Hero.Position;
            Set(pad.leftStick, new Vector2(1, 0));
            yield return TestUtil.WaitSeconds(0.6f);
            Set(pad.leftStick, Vector2.zero);
            Assert.Greater(Run.Hero.Position.x - start.x, 1.2f, "hero walked right");
            Assert.AreEqual(Vector2.right, Run.Hero.Facing);
        }

        [UnityTest]
        public IEnumerator Weapons_KillEnemies_DropGems_GiveXp()
        {
            yield return Boot();
            yield return StartRun("pea_shooter");
            Run.Hero.GodMode = true;
            for (int i = 0; i < 8; i++) Run.Enemies.Spawn(db.biomes[0].normals[0], new Vector2(4f + i * 0.4f, 0));
            Simulate(Run, 6f);
            Assert.Greater(Run.State.kills, 0, "pea shooter killed slimes");
            Simulate(Run, 4f);
            Assert.That(Run.State.xp > 0 || Run.State.level > 1, "gems were collected");
            yield return null;
            yield return TestUtil.Capture("M1_gameplay");
        }

        [UnityTest]
        public IEnumerator LevelUp_OpensPopup_AndAPickApplies()
        {
            yield return Boot();
            yield return StartRun("pea_shooter");
            Run.AddXp(Run.State.xpToNext);
            yield return null;
            Assert.AreEqual(FlowState.LevelUp, flow.State, "level up opens the popup");
            Assert.IsTrue(Run.Paused, "game paused");
            Assert.AreEqual(3, Run.LevelUp.Current.Count, "3 cards offered");
            yield return TestUtil.Capture("M1_levelup");
            yield return TestUtil.WaitSeconds(0.45f);
            var picked = Run.LevelUp.Current[0];
            int before = Run.Inventory.Weapons.Count + Run.Inventory.Items.Count;
            int lvlBefore = Run.Inventory.Weapons[0].level;
            yield return Tap(pad.buttonSouth);
            Assert.AreEqual(FlowState.Running, flow.State);
            bool changed = Run.Inventory.Weapons.Count + Run.Inventory.Items.Count > before || Run.Inventory.Weapons[0].level > lvlBefore;
            Assert.IsTrue(changed, $"picking '{picked.title}' changed the build");
        }

        [UnityTest]
        public IEnumerator Boss_SpawnsAtThreeMinutes_AndDropsAChest()
        {
            yield return Boot();
            yield return StartRun();
            Run.Hero.GodMode = true;
            Run.State.time = 179.5f;
            Simulate(Run, 1f);
            var boss = Run.Enemies.FirstBoss();
            Assert.IsNotNull(boss, "boss spawned at 3:00");
            Assert.AreEqual(db.biomes[0].boss, boss.def);
            yield return null;
            Assert.AreEqual(FlowState.BossIntro, flow.State);
            yield return TestUtil.Capture("M1_boss");
            yield return TestUtil.WaitSeconds(1.5f);
            Assert.AreEqual(FlowState.Running, flow.State);
            Run.Enemies.Damage(boss, boss.hp + 1, false, Vector2.zero, 0, null);
            bool chest = false;
            foreach (var p in Run.Pickups.Active) if (p.kind == PickupKind.Chest) chest = true;
            Assert.IsTrue(chest, "boss dropped a chest");
            Assert.AreEqual(1, Run.State.bossKills);
        }

        [UnityTest]
        public IEnumerator Hero_Dies_AndResultsShow()
        {
            yield return Boot();
            yield return StartRun();
            Run.Hero.TakeDamage(9999, "a test");
            Assert.IsTrue(Run.Ended);
            Assert.IsFalse(Run.State.won);
            yield return TestUtil.WaitUntil(() => flow.State == FlowState.Results, 3f);
            Assert.AreEqual(FlowState.Results, flow.State);
            yield return TestUtil.Capture("M1_results");
            yield return Tap(pad.buttonSouth); // Continue
            Assert.AreEqual(FlowState.MatchSelect, flow.State);
            Assert.IsNull(Run, "run destroyed");
        }
    }
}
