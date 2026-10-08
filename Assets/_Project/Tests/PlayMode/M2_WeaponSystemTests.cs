using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MiniMayhem.Tests
{
    /// <summary>M2: 6+6 slots, level curves, every weapon form, evolutions, fusions, reroll/skip/banish.</summary>
    [Timeout(240000)]
    public class M2_WeaponSystemTests : MayhemTestBase
    {
        void ClearWeapons()
        {
            foreach (var w in Run.Inventory.Weapons) w.Dispose();
            Run.Inventory.Weapons.Clear();
        }

        void SpawnRing(int n, float radius)
        {
            var def = db.biomes[0].normals[3]; // snail: tanky, slow, so it survives long enough to be hit by everything
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI * 2f / n;
                Run.Enemies.Spawn(def, Run.Hero.Position + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius, false, 20f);
            }
        }

        [UnityTest]
        public IEnumerator EveryWeaponForm_DealsDamage()
        {
            yield return Boot();
            yield return StartRun(null);
            Run.Hero.GodMode = true;
            Run.Director.Paused = true;
            foreach (var def in db.weapons)
            {
                ClearWeapons();
                Run.Enemies.Clear();
                var w = Run.Inventory.AddWeapon(def);
                if (def.IsBase) { w.level = def.MaxLevel; w.OnLevelChanged(); }
                SpawnRing(12, 2.2f);
                SpawnRing(12, 5f);
                Simulate(Run, 6f);
                Assert.Greater(w.damageDealt, 0f, $"{def.displayName} hits enemies");
                if (def.id == "disco_laser" || def.id == "bubble_bath" || def.id == "golden_duck_parade" || def.id == "mega_mallet") yield return TestUtil.Capture("M2_" + def.id);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Evolution_CardIsGuaranteed_AndKeepsTheSlot()
        {
            yield return Boot();
            yield return StartRun("pea_shooter");
            var inv = Run.Inventory;
            inv.AddWeapon(Weapon("frying_pan"));
            var pea = inv.Find(Weapon("pea_shooter"));
            inv.LevelWeapon(pea, 4);
            Assert.IsTrue(pea.IsMaxLevel);
            Assert.IsEmpty(inv.AvailableEvolutions(), "needs its item");
            inv.AddItem(Item("spinach_can"));
            Assert.AreEqual(1, inv.AvailableEvolutions().Count);
            Run.AddXp(Run.State.xpToNext);
            yield return null;
            Assert.AreEqual(FlowState.LevelUp, flow.State);
            var cards = Run.LevelUp.Current;
            Assert.AreEqual(CardKind.Evolve, cards[0].kind, "evolution card is guaranteed and first");
            yield return TestUtil.Capture("M2_evolve_card");
            yield return TestUtil.WaitSeconds(0.45f);
            yield return Tap(pad.buttonSouth);
            Assert.AreEqual("pea_cannon", inv.Weapons[0].def.id, "evolved in the same slot");
            Assert.AreEqual(2, inv.Weapons.Count);
            Assert.IsFalse(inv.Weapons[0].CanLevel, "evolved weapons do not level");
            Assert.IsTrue(meta.IsDiscovered("evo:pea_cannon"));
            for (int i = 0; i < 30; i++)
            {
                Run.LevelUp.Roll();
                Assert.IsFalse(Run.LevelUp.Current.Any(c => c.weapon != null && c.weapon.id == "pea_shooter"), "evolved base weapon is not offered again");
            }
        }

        [UnityTest]
        public IEnumerator Fusion_MergesTwoMaxedWeapons_AndFreesASlot()
        {
            yield return Boot();
            yield return StartRun("pea_shooter");
            var inv = Run.Inventory;
            inv.AddWeapon(Weapon("star_wand"));
            inv.AddWeapon(Weapon("duck_orbit"));
            inv.LevelWeapon(inv.Find(Weapon("pea_shooter")), 4);
            Assert.IsEmpty(inv.AvailableFusions(), "both must be maxed");
            inv.LevelWeapon(inv.Find(Weapon("star_wand")), 4);
            var fusions = inv.AvailableFusions();
            Assert.AreEqual(1, fusions.Count);
            Run.LevelUp.Roll();
            var card = Run.LevelUp.Current.First(c => c.kind == CardKind.Fuse);
            Run.LevelUp.Apply(card);
            Assert.AreEqual(2, inv.Weapons.Count, "one slot freed");
            Assert.AreEqual("star_blaster", inv.Weapons[0].def.id);
            Assert.IsTrue(inv.Consumed.Contains("pea_shooter") && inv.Consumed.Contains("star_wand"));
            // Consumed weapons never come back as new-weapon cards.
            for (int i = 0; i < 30; i++)
            {
                Run.LevelUp.Roll();
                Assert.IsFalse(Run.LevelUp.Current.Any(c => c.kind == CardKind.NewWeapon && (c.weapon.id == "pea_shooter" || c.weapon.id == "star_wand")));
            }
            Assert.IsEmpty(inv.AvailableEvolutions(), "fused weapons cannot evolve");
        }

        [UnityTest]
        public IEnumerator FullSlots_OnlyOfferUpgrades_ThenFallbacks()
        {
            yield return Boot();
            yield return StartRun(null);
            var inv = Run.Inventory;
            foreach (var id in new[] { "pea_shooter", "star_wand", "frying_pan", "baseball_bat", "duck_orbit", "boomerang" }) inv.AddWeapon(Weapon(id));
            foreach (var i in db.items.Where(i => i.general).Take(6)) inv.AddItem(i);
            Assert.IsNull(inv.AddWeapon(Weapon("laser_pointer")), "7th weapon rejected");
            Assert.AreEqual(6, inv.Weapons.Count);
            Assert.AreEqual(6, inv.Items.Count);
            for (int k = 0; k < 20; k++)
            {
                Run.LevelUp.Roll();
                Assert.IsTrue(Run.LevelUp.Current.All(c => c.kind == CardKind.WeaponUpgrade || c.kind == CardKind.ItemUpgrade || c.kind == CardKind.Evolve || c.kind == CardKind.Fuse), "only upgrades when slots are full");
            }
            // Max everything and evolve nothing possible (no evolution items owned): fallbacks.
            foreach (var w in inv.Weapons) inv.LevelWeapon(w, 4);
            foreach (var i in inv.Items) inv.LevelItem(i, 4);
            Run.LevelUp.Roll();
            var kinds = Run.LevelUp.Current.Select(c => c.kind).ToList();
            Assert.IsTrue(kinds.All(k => k == CardKind.Fuse || k == CardKind.Heal || k == CardKind.Gold || k == CardKind.StatBoost), string.Join(",", kinds));
            Assert.IsTrue(kinds.Contains(CardKind.Fuse), "pea + star wand / pan + bat are both maxed: fusion offered");
        }

        [UnityTest]
        public IEnumerator Reroll_Banish_Skip_UseTheirCharges()
        {
            yield return Boot();
            yield return StartRun("pea_shooter");
            var lu = Run.LevelUp;
            lu.Rerolls = 1; lu.Banishes = 1; lu.Skips = 1;
            Run.AddXp(Run.State.xpToNext * 3f);
            yield return null;
            Assert.AreEqual(FlowState.LevelUp, flow.State);
            int pending = Run.State.pendingLevelUps;
            Assert.GreaterOrEqual(pending, 2);
            yield return TestUtil.WaitSeconds(0.45f);
            yield return Tap(pad.buttonNorth); // reroll
            Assert.AreEqual(0, lu.Rerolls);
            var target = lu.Current[0];
            string banished = target.SubjectId;
            yield return Tap(pad.buttonWest); // banish focused (first) card
            Assert.AreEqual(0, lu.Banishes);
            Assert.IsTrue(Run.Inventory.Banished.Contains(banished));
            Assert.IsFalse(lu.Current.Any(c => c.SubjectId == banished));
            float gold = Run.State.gold;
            yield return Tap(pad.buttonEast); // skip
            Assert.AreEqual(0, lu.Skips);
            Assert.Greater(Run.State.gold, gold);
            Assert.AreEqual(pending - 1, Run.State.pendingLevelUps);
            Assert.AreEqual(FlowState.LevelUp, flow.State, "next pending level-up opens");
            for (int i = 0; i < 20; i++)
            {
                lu.Roll();
                Assert.IsFalse(lu.Current.Any(c => c.SubjectId == banished), "banished stays gone");
            }
        }

        [UnityTest]
        public IEnumerator ItemsChangeStats_AndWeaponsGrowWithLevel()
        {
            yield return Boot();
            yield return StartRun("pea_shooter");
            float speed = Run.Stats.MoveSpeed;
            var it = Run.Inventory.AddItem(Item("sneakers"));
            Assert.Greater(Run.Stats.MoveSpeed, speed);
            Run.Inventory.LevelItem(it, 4);
            Assert.AreEqual(speed * 1.4f, Run.Stats.MoveSpeed, 0.01f);
            var w = Run.Inventory.Weapons[0];
            float s1 = w.VisualScale;
            var c1 = w.VisualColor;
            Run.Inventory.LevelWeapon(w, 4);
            Assert.Greater(w.VisualScale, s1, "bigger each level");
            Assert.AreNotEqual(c1, w.VisualColor, "colour shifts with level");
            Assert.AreEqual(3, Run.Inventory.Weapons[0].Amount, "level 5 pea shooter fires 3 peas");
        }
    }
}
