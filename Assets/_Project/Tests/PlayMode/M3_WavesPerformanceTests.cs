using System.Collections;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;

namespace MiniMayhem.Tests
{
    /// <summary>M3: wave director (trickle, timeline, swarms, mini bosses, bosses), cap behaviour, 500+ enemy performance.</summary>
    [Timeout(600000)]
    public class M3_WavesPerformanceTests : MayhemTestBase
    {
        [UnityTest]
        public IEnumerator Director_FollowsThePacingTimeline()
        {
            yield return Boot();
            yield return StartRun();
            Run.Hero.GodMode = true;
            Simulate(Run, 20f);
            Assert.Greater(Run.Enemies.Count, 10, "trickle spawns from the start");
            Assert.IsNull(Run.Enemies.FirstBoss());

            Run.State.time = 89f;
            Simulate(Run, 2f);
            Assert.IsTrue(Run.Enemies.Active.Any(e => e.alive && e.def.tier == EnemyTier.MiniBoss), "mini boss at 1:30");

            int before = Run.Enemies.Count;
            Run.State.time = 149f;
            Simulate(Run, 1.5f);
            Assert.Greater(Run.Enemies.Count, before + 40, "swarm burst at 2:30");

            Run.State.time = 179f;
            Simulate(Run, 1.5f);
            Assert.IsNotNull(Run.Enemies.FirstBoss(), "boss at 3:00");
            Assert.AreEqual(1, Run.Director.BossesSpawned);
        }

        [UnityTest]
        public IEnumerator EnemyCap_IsRespected_AndElitesReplaceFodder()
        {
            yield return Boot();
            yield return StartRun(null);
            Run.Hero.GodMode = true;
            Run.State.time = 400f; // high trickle rate
            int cap = Run.Config.enemyCap;
            for (int i = 0; i < cap; i++) Run.Enemies.Spawn(db.biomes[0].normals[0], Run.Hero.Position + Random.insideUnitCircle.normalized * Random.Range(12f, 20f));
            Simulate(Run, 20f);
            Assert.LessOrEqual(Run.Enemies.Count, Mathf.RoundToInt(cap * 1.25f), "stays near the cap");
            Assert.IsTrue(Run.Enemies.Active.Any(e => e.alive && e.elite), "fodder was replaced by elites");
        }

        [UnityTest]
        public IEnumerator SixHundredEnemies_StepFast()
        {
            yield return Boot();
            yield return StartRun(null);
            Run.Hero.GodMode = true;
            Run.Director.Paused = true;
            foreach (var id in new[] { "pea_cannon", "golden_duck_parade", "thunder_rod", "toxic_laundry_cloud", "disco_laser", "fireworks_show" })
                Run.Inventory.AddWeapon(Weapon(id));
            var roster = db.biomes[0].normals;
            for (int i = 0; i < 600; i++)
                Run.Enemies.Spawn(roster[i % roster.Length], Run.Hero.Position + Random.insideUnitCircle.normalized * Random.Range(3f, 16f), false, 400f);
            // Warm up, then measure.
            Simulate(Run, 1f);
            var sw = new Stopwatch();
            int frames = 0;
            float worst = 0;
            for (int f = 0; f < 240; f++)
            {
                if (Run.Enemies.Count < 500)
                    for (int k = 0; k < 20; k++) Run.Enemies.Spawn(roster[k % roster.Length], Run.Hero.Position + Random.insideUnitCircle.normalized * Random.Range(10f, 16f), false, 400f);
                long t0 = sw.ElapsedTicks;
                sw.Start();
                Run.Step(1f / 60f);
                sw.Stop();
                worst = Mathf.Max(worst, (sw.ElapsedTicks - t0) * 1000f / Stopwatch.Frequency);
                frames++;
            }
            float avg = (float)sw.Elapsed.TotalMilliseconds / frames;
            Debug.Log($"[MiniMayhem] Perf: {Run.Enemies.Count} enemies, {Run.Projectiles.Count} projectiles, {Run.Pickups.GemCount} gems, {Run.Fx.Count} fx -> avg {avg:0.00} ms, worst {worst:0.00} ms per simulation step");
            Run.State.pendingLevelUps = 0;
            flow.Go(FlowState.Running);
            Run.Paused = true;
            yield return null;
            yield return TestUtil.Capture("M3_swarm");
            Assert.GreaterOrEqual(Run.Enemies.Count, 450);
            Assert.Less(avg, 8f, "simulation stays well inside a 60 fps frame");
        }

        /// <summary>A sensible player: evolve/fuse first, then weapons, then items.</summary>
        public static Card AutoPick(System.Collections.Generic.List<Card> cards)
        {
            int Score(Card c) => c.kind switch
            {
                CardKind.Evolve or CardKind.Fuse => 100,
                CardKind.WeaponUpgrade => 50,
                CardKind.NewWeapon => 40,
                CardKind.ItemUpgrade => 30,
                CardKind.NewItem => 20,
                _ => 0,
            };
            return cards.OrderByDescending(Score).First();
        }

        /// <summary>The finale biome with a half-bought skill tree: the auto-pilot should still get a decent build going.</summary>
        [UnityTest]
        public IEnumerator FinaleBiome_Pacing_Log()
        {
            yield return Boot();
            meta.AddGold(1000000);
            foreach (var n in db.skillNodes) for (int r = 0; r < (n.maxRank + 1) / 2; r++) meta.Buy(n);
            yield return StartRun("star_wand", 5, MatchMode.OutlastTime, MapStyle.Endless);
            var run = Run;
            run.Hero.GodMode = true;
            float dt = 1f / 30f;
            for (float t = 0; t < 601f && !run.Ended; t += dt)
            {
                float a = t * 0.35f;
                run.Hero.MoveOverride = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                run.Step(dt);
                while (run.State.pendingLevelUps > 0) run.LevelUp.Apply(AutoPick(run.LevelUp.Roll()));
            }
            Debug.Log($"[MiniMayhem] Pacing (Candy, half tree): level={run.State.level} kills={run.State.kills} dmgTaken={run.State.damageTaken:0} bosses={run.State.bossKills} " +
                      $"weapons={string.Join(",", run.Inventory.Weapons.Select(w => w.def.id + ":" + w.level))}");
            Assert.Greater(run.State.kills, 600);
            Assert.That(run.State.level, Is.InRange(15, 60), "first-pass balance: logged for tuning");
            yield return null;
        }

        /// <summary>Plays a whole 10 minute Outlast match with an auto-pilot (kites in circles, takes the first card).</summary>
        [UnityTest]
        public IEnumerator FullMatch_Pacing_Sanity()
        {
            yield return Boot();
            yield return StartRun("pea_shooter", 0, MatchMode.OutlastTime);
            var run = Run;
            run.Hero.GodMode = true;
            float dt = 1f / 30f;
            int maxEnemies = 0;
            for (float t = 0; t < 600f + 1f && !run.Ended; t += dt)
            {
                float a = t * 0.35f;
                run.Hero.MoveOverride = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                run.Step(dt);
                maxEnemies = Mathf.Max(maxEnemies, run.Enemies.Count);
                while (run.State.pendingLevelUps > 0) run.LevelUp.Apply(AutoPick(run.LevelUp.Roll()));
            }
            Debug.Log($"[MiniMayhem] Pacing: won={run.State.won} level={run.State.level} kills={run.State.kills} gold={run.State.gold:0} " +
                      $"weapons={string.Join(",", run.Inventory.Weapons.Select(w => w.def.id + ":" + w.level))} items={run.Inventory.Items.Count} maxEnemies={maxEnemies} bosses={run.State.bossKills}");
            Assert.IsTrue(run.Ended && run.State.won, "outlast completes at 10:00");
            Assert.That(run.State.level, Is.InRange(15, 45), "a full run of level-ups (the circling auto-pilot misses many gems)");
            Assert.Greater(run.State.kills, 800);
            Assert.GreaterOrEqual(maxEnemies, 150, "it gets busy");
            yield return null;
        }
    }
}
