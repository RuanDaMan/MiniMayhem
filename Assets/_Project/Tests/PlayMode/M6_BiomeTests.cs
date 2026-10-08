using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MiniMayhem.Tests
{
    /// <summary>M6: six biomes, their rosters and bosses, and the 3-matches unlock flow.</summary>
    [Timeout(600000)]
    public class M6_BiomeTests : MayhemTestBase
    {
        [UnityTest]
        public IEnumerator WinningThreeMatches_UnlocksTheSwamp_InTheMenus()
        {
            yield return Boot();
            for (int m = 0; m < 3; m++)
            {
                flow.StartRun(new RunSetup { biome = db.biomes[0], matchIndex = m, mode = MatchMode.OutlastTime, style = flow.StyleFor(db.biomes[0], m), startingWeapon = Weapon("pea_shooter"), seed = 7 + m });
                yield return null;
                Run.Hero.GodMode = true;
                Run.State.time = 599.5f;
                Simulate(Run, 1f);
                Assert.IsTrue(Run.State.won);
                yield return TestUtil.WaitUntil(() => flow.State == FlowState.Results, 3f);
                if (m == 2)
                {
                    StringAssert.Contains("Swamp", string.Join(" ", Run.Result.unlocks));
                    yield return TestUtil.Capture("M6_unlock_results");
                }
            }
            Assert.IsTrue(meta.IsBiomeUnlocked(1));
            Assert.IsTrue(meta.IsWeaponUnlocked(Weapon("boomerang")));
            flow.ExitToMenu(FlowState.MatchSelect);
            yield return null;
            yield return TestUtil.Capture("M6_match_select_unlocked");
            Assert.IsTrue(flow.MatchSelect.DefaultSelection.name.StartsWith("Match"), "a match is focused");
            Assert.AreEqual("Row_swamp", flow.MatchSelect.DefaultSelection.transform.parent.name, "focus moves to the first unfinished match: the Swamp");
        }

        [UnityTest]
        public IEnumerator EveryBiome_EveryEnemy_FightsWithoutErrors()
        {
            yield return Boot();
            foreach (var b in db.biomes)
            {
                flow.StartRun(new RunSetup { biome = b, matchIndex = 0, mode = MatchMode.OutlastTime, style = b.matchStyles[0], startingWeapon = Weapon("frying_pan"), seed = b.index + 1 });
                yield return null;
                Run.Hero.GodMode = true;
                Run.Director.Paused = true;
                foreach (var w in new[] { "zap_rod", "stinky_socks", "star_wand" }) Run.Inventory.AddWeapon(Weapon(w));
                foreach (var w in Run.Inventory.Weapons) Run.Inventory.LevelWeapon(w, 4);
                var roster = b.normals.Concat(b.miniBosses).Append(b.boss).ToList();
                int i = 0;
                foreach (var e in roster)
                {
                    float a = i++ * Mathf.PI * 2f / roster.Count;
                    Run.Enemies.Spawn(e, Run.Hero.Position + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 6f);
                    Run.Enemies.Spawn(e, Run.Hero.Position + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 9f, elite: true);
                }
                int hostileSeen = 0;
                for (int k = 0; k < 600; k++)
                {
                    Run.Step(1f / 30f);
                    if (Run.Projectiles.Active.Any(p => p.alive && p.kind == ProjectileKind.Hostile)) hostileSeen++;
                    if (k == 240) { yield return null; yield return TestUtil.Capture("M6_biome_" + b.id); }
                }
                Assert.Greater(Run.State.kills, 3, $"{b.displayName}: enemies die");
                Assert.Greater(hostileSeen, 0, $"{b.displayName}: shooters / bosses attack");
                Assert.Greater(Run.State.damageTaken, 0, $"{b.displayName}: enemies hurt the hero (god mode only blocks HP loss)");
            }
        }
    }
}
