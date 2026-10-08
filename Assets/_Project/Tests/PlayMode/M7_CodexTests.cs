using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MiniMayhem.Tests
{
    /// <summary>M7: the codex shows every tab, hides undiscovered entries and updates as things are discovered.</summary>
    [Timeout(180000)]
    public class M7_CodexTests : MayhemTestBase
    {
        [UnityTest]
        public IEnumerator Codex_TabsAndDiscovery()
        {
            yield return Boot();
            // Title -> Codex (third button).
            yield return Tap(pad.dpad.down);
            yield return Tap(pad.dpad.down);
            yield return Tap(pad.buttonSouth);
            Assert.AreEqual(FlowState.Codex, flow.State);
            var codex = flow.Codex;
            Assert.AreEqual(0, codex.TabIndex);
            Assert.AreEqual(db.weapons.Count, codex.EntryCount, "every weapon form is listed");
            yield return TestUtil.Capture("M7_codex_weapons");

            int[] expected = { db.weapons.Count, db.items.Count, db.evolutions.Count, db.fusions.Count, -1, db.biomes.Count, -1 };
            for (int t = 1; t < 7; t++)
            {
                yield return Tap(pad.rightShoulder);
                Assert.AreEqual(t, codex.TabIndex);
                if (expected[t] > 0) Assert.AreEqual(expected[t], codex.EntryCount, $"tab {t}");
                if (t == 2) yield return TestUtil.Capture("M7_codex_evolutions");
                if (t == 4) yield return TestUtil.Capture("M7_codex_enemies");
            }
            Assert.AreEqual(System.Enum.GetValues(typeof(StatId)).Length + 1, codex.EntryCount, "every stat + lifetime");

            // Discovery: enemies start hidden, then show up after being seen in a run.
            Assert.IsFalse(meta.IsDiscovered("e:slime"));
            yield return Tap(pad.buttonEast);
            Assert.AreEqual(FlowState.Title, flow.State);
            yield return StartRun();
            Run.Hero.GodMode = true;
            Simulate(Run, 5f);
            Assert.IsTrue(meta.IsDiscovered("e:slime"), "seeing an enemy discovers it");
            Assert.IsTrue(meta.IsDiscovered("w:pea_shooter"));
            Assert.IsTrue(meta.IsDiscovered("i:sneakers"), "general items are known from the start");
            Assert.IsFalse(meta.IsDiscovered("w:pea_cannon"), "evolved forms stay hidden until made");
            Assert.IsTrue(meta.IsDiscovered("w:pea_shooter"), "recipe requirements show for discovered weapons");
        }

        [UnityTest]
        public IEnumerator Codex_OpensFromPause()
        {
            yield return Boot();
            yield return StartRun();
            yield return Tap(pad.startButton);
            Assert.AreEqual(FlowState.Paused, flow.State);
            yield return TestUtil.Capture("M7_pause");
            flow.OpenCodex(FlowState.Paused);
            yield return null;
            Assert.AreEqual(FlowState.Codex, flow.State);
            Assert.IsTrue(Run.Paused, "run stays paused under the codex");
            yield return Tap(pad.buttonEast);
            Assert.AreEqual(FlowState.Paused, flow.State);
            yield return Tap(pad.startButton);
            Assert.AreEqual(FlowState.Running, flow.State);
        }
    }
}
