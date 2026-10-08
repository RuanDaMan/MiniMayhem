using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MiniMayhem.Tests
{
    /// <summary>M4: match modes (win/lose rules) and map styles (endless, fixed arena, expanding arena) + hazards.</summary>
    [Timeout(300000)]
    public class M4_ModesMapsTests : MayhemTestBase
    {
        [UnityTest]
        public IEnumerator Outlast_WinsAtTenMinutes()
        {
            yield return Boot();
            yield return StartRun(mode: MatchMode.OutlastTime);
            Run.Hero.GodMode = true;
            Run.State.time = 598.5f;
            Simulate(Run, 2f);
            Assert.IsTrue(Run.Ended && Run.State.won);
            yield return TestUtil.WaitUntil(() => flow.State == FlowState.Results, 3f);
            Assert.Greater(Run.Result.metaGold, 0);
            Assert.AreEqual(1f, Run.Result.keepFraction, "keep 100% on a win");
            Assert.IsTrue(meta.IsMatchCompleted(db.biomes[0], 0), "match counts as completed");
            yield return TestUtil.Capture("M4_victory");
        }

        [UnityTest]
        public IEnumerator KillCount_WinsOnTarget_LosesOnTime()
        {
            yield return Boot();
            yield return StartRun(mode: MatchMode.KillCount);
            Run.Hero.GodMode = true;
            Assert.AreEqual(db.biomes[0].killTarget, Run.State.killTarget);
            Run.State.kills = Run.State.killTarget - 1;
            Simulate(Run, 0.5f);
            Assert.IsFalse(Run.Ended);
            Run.Enemies.Kill(Run.Enemies.Spawn(db.biomes[0].normals[0], Run.Hero.Position + Vector2.right * 3), null);
            Simulate(Run, 0.1f);
            Assert.IsTrue(Run.Ended && Run.State.won, "target reached");

            yield return StartRun(mode: MatchMode.KillCount);
            Run.Hero.GodMode = true;
            Run.State.time = 599.5f;
            Simulate(Run, 1f);
            Assert.IsTrue(Run.Ended);
            Assert.IsFalse(Run.State.won, "time's up without the kills");
            Assert.AreEqual(Run.Stats.DeathKeep, Run.Result.keepFraction, 1e-4, "loss keeps only the death share");
        }

        [UnityTest]
        public IEnumerator Survive_WinsByKillingTheFinaleBoss()
        {
            yield return Boot();
            yield return StartRun(mode: MatchMode.Survive);
            Run.Hero.GodMode = true;
            Run.State.time = 179.5f;
            Simulate(Run, 1f);
            var first = Run.Enemies.FirstBoss();
            Run.Enemies.Kill(first, null);
            Simulate(Run, 0.1f);
            Assert.IsFalse(Run.Ended, "the 3:00 boss is not the finale");
            Run.State.time = 539.5f;
            Simulate(Run, 1f);
            Assert.IsTrue(Run.Director.FinaleSpawned);
            var finale = Run.Director.FinaleBoss;
            Assert.IsTrue(finale.alive);
            Simulate(Run, 70f);
            Assert.IsFalse(Run.Ended, "Survive keeps going past 10:00 until the finale boss falls");
            Run.Enemies.Damage(finale, finale.hp + 1, false, Vector2.zero, 0, null);
            Simulate(Run, 0.1f);
            Assert.IsTrue(Run.Ended && Run.State.won);
        }

        [UnityTest]
        public IEnumerator FixedArena_HasWalls_Obstacles_AndTelegraphedSpawns()
        {
            yield return Boot();
            yield return StartRun(style: MapStyle.FixedArena);
            Run.Hero.GodMode = true;
            Assert.IsTrue(Run.Map.Bounded);
            Assert.Greater(Run.Map.Obstacles.Count, 2, "obstacles placed");
            // Walk into the right wall along a row that is clear of obstacles.
            float row = 0;
            for (float y = Run.Map.Bounds.yMin + 1f; y < Run.Map.Bounds.yMax - 1f; y += 0.25f)
            {
                bool clear = true;
                foreach (var o in Run.Map.Obstacles) if (y > o.yMin - 0.8f && y < o.yMax + 0.8f) clear = false;
                if (clear) { row = y; break; }
            }
            Run.Hero.Teleport(new Vector2(0, row));
            Run.Hero.MoveOverride = Vector2.right;
            Simulate(Run, 8f);
            Assert.LessOrEqual(Run.Hero.Position.x, Run.Map.Bounds.xMax, "wall stops the hero");
            Assert.Greater(Run.Hero.Position.x, Run.Map.Bounds.xMax - 2f);
            Run.Hero.MoveOverride = Vector2.zero;
            // Spawns are telegraphed, then appear inside the arena.
            Run.Director.SpawnGroup(db.biomes[0].normals[0], 10, SpawnPattern.Ring, false);
            Assert.Greater(Run.Director.PendingCount, 0, "spawns wait behind a ! marker");
            Simulate(Run, 1f);
            foreach (var e in Run.Enemies.Active)
                if (e.alive) Assert.IsTrue(Run.Map.Bounds.Contains(e.pos), $"enemy inside the arena at {e.pos}");
            foreach (var o in Run.Map.Obstacles)
            {
                var p = o.center;
                Run.Map.ResolveCircle(ref p, 0.4f);
                Assert.IsFalse(o.Contains(p), "obstacles push circles out");
            }
            Run.Hero.Teleport(Vector2.zero);
            Simulate(Run, 3f);
            yield return null;
            yield return TestUtil.Capture("M4_fixed_arena");
        }

        [UnityTest]
        public IEnumerator ExpandingArena_GrowsAtEachBoss()
        {
            yield return Boot();
            yield return StartRun(style: MapStyle.ExpandingArena);
            Run.Hero.GodMode = true;
            float w0 = Run.Map.Bounds.width;
            Run.State.time = 179.5f;
            Simulate(Run, 6f);
            Assert.Greater(Run.Map.Bounds.width, w0 + 4f, "walls moved out at the 3:00 boss");
            yield return null;
            yield return TestUtil.Capture("M4_expanding_arena");
        }

        [UnityTest]
        public IEnumerator Endless_StreamsTheWorld_AndRecyclesFarEnemies()
        {
            yield return Boot();
            yield return StartRun(style: MapStyle.Endless);
            Run.Hero.GodMode = true;
            Assert.IsFalse(Run.Map.Bounded);
            var e = Run.Enemies.Spawn(db.biomes[0].normals[3], Vector2.zero);
            Run.Hero.MoveOverride = Vector2.up;
            Simulate(Run, 10f);
            Assert.Greater(Run.Hero.Position.y, 30f, "free to roam");
            if (e.alive) Assert.Less((e.pos - Run.Hero.Position).magnitude, Run.Config.despawnDistance + 2f, "far enemies are moved back near the hero");
        }

        [UnityTest]
        public IEnumerator Modes_AreRandomPerMatch()
        {
            yield return Boot();
            var seen = new HashSet<MatchMode>();
            for (int b = 0; b < 6; b++) for (int m = 0; m < 3; m++) seen.Add(flow.PlannedMode(db.biomes[b], m));
            Assert.GreaterOrEqual(seen.Count, 2, "modes vary between matches");
            Assert.AreEqual(flow.PlannedMode(db.biomes[2], 1), flow.PlannedMode(db.biomes[2], 1), "stable until played");
        }

        [UnityTest]
        public IEnumerator Hazards_SlowAndHurt()
        {
            yield return Boot();
            yield return StartRun(biome: 1); // Swamp: slow mud
            Simulate(Run, 0.5f);
            Assert.IsTrue(Run.Map.FindHazard(Vector2.zero, 40f, out var mud), "mud patches exist");
            Run.Hero.Teleport(mud);
            Simulate(Run, 0.1f);
            Assert.AreEqual(HazardType.SlowMud, Run.Hero.StandingOn);
            Assert.Less(Run.Hero.Speed, Run.Stats.MoveSpeed, "mud slows");
            yield return TestUtil.Capture("M4_swamp");

            yield return StartRun(biome: 4, style: MapStyle.FixedArena); // Volcano: lava
            Run.Director.Paused = true;
            Assert.IsTrue(Run.Map.FindHazard(Vector2.zero, 40f, out var lava));
            Run.Hero.Teleport(lava);
            float hp = Run.Hero.Hp;
            Simulate(Run, 1.5f);
            Assert.Less(Run.Hero.Hp, hp, "lava burns");

            yield return StartRun(biome: 2); // Desert: sandstorm
            Run.Hero.GodMode = true;
            Run.State.time = 0;
            Simulate(Run, 45f);
            Assert.Greater(Run.Sandstorm, 0.5f, "sandstorm rolls in");
            yield return null;
            yield return TestUtil.Capture("M4_sandstorm");
        }
    }
}
