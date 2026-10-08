using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MiniMayhem.Tests
{
    /// <summary>M8: settings, audio, pause/give up flow, juice toggles.</summary>
    [Timeout(180000)]
    public class M8_PolishTests : MayhemTestBase
    {
        [UnityTest]
        public IEnumerator Settings_ChangeWithTheGamepad_AndApply()
        {
            yield return Boot();
            flow.OpenSettings(FlowState.Title);
            yield return null;
            Assert.AreEqual(FlowState.Settings, flow.State);
            float before = meta.Data.settings.master;
            yield return Tap(pad.dpad.left); // master volume down
            Assert.Less(meta.Data.settings.master, before);
            Assert.AreEqual(meta.Data.settings.master, flow.Audio.Master, 1e-4, "applied to the audio service");
            // Damage numbers row (4th) toggles with A.
            yield return Tap(pad.dpad.down);
            yield return Tap(pad.dpad.down);
            yield return Tap(pad.dpad.down);
            bool dn = meta.Data.settings.damageNumbers;
            yield return Tap(pad.buttonSouth);
            Assert.AreNotEqual(dn, meta.Data.settings.damageNumbers);
            yield return TestUtil.Capture("M8_settings");
            yield return Tap(pad.buttonEast);
            Assert.AreEqual(FlowState.Title, flow.State);

            meta.Data.settings.screenShake = false;
            meta.Data.settings.damageNumbers = false;
            flow.ApplySettings();
            yield return StartRun();
            Assert.IsFalse(Run.Numbers.Enabled, "damage numbers off");
            Assert.IsFalse(flow.CameraRig.ShakeEnabled, "shake off");
        }

        [UnityTest]
        public IEnumerator Audio_AllSoundsAndMusicBuild()
        {
            foreach (SfxId id in System.Enum.GetValues(typeof(SfxId)))
            {
                var clip = SfxSynth.Build(id);
                Assert.Greater(clip.samples, 100, id.ToString());
            }
            yield return Boot();
            Assert.AreEqual("title", flow.Audio.CurrentMusic, "title music plays");
            yield return StartRun(biome: 3);
            Assert.AreEqual("biome_frozen", flow.Audio.CurrentMusic, "biome music plays in runs");
        }

        [UnityTest]
        public IEnumerator Pause_GiveUp_EndsTheRunWithResults()
        {
            yield return Boot();
            yield return StartRun();
            Run.State.gold = 40;
            yield return Tap(pad.startButton);
            Assert.AreEqual(FlowState.Paused, flow.State);
            float t = Run.State.time;
            yield return TestUtil.WaitSeconds(0.3f);
            Assert.AreEqual(t, Run.State.time, "time frozen while paused");
            // Resume, Settings, Codex, Give Up -> 4th button, then confirm.
            yield return Tap(pad.dpad.down);
            yield return Tap(pad.dpad.down);
            yield return Tap(pad.dpad.down);
            yield return Tap(pad.buttonSouth);
            yield return Tap(pad.buttonSouth);
            Assert.IsTrue(Run.Ended);
            Assert.AreEqual("Gave up", Run.State.endReason);
            yield return TestUtil.WaitUntil(() => flow.State == FlowState.Results, 2f);
            Assert.AreEqual(FlowState.Results, flow.State);
            Assert.Greater(meta.Gold, 0, "death share banked");
        }

        [UnityTest]
        public IEnumerator ColourBlind_ChangesEnemyBullets()
        {
            yield return Boot();
            meta.Data.settings.colorblind = true;
            yield return StartRun();
            Run.Projectiles.FireHostile(Vector2.one * 5, Vector2.left, 5);
            var p = Run.Projectiles.Active[Run.Projectiles.Count - 1];
            Assert.AreEqual(Art.Get(ArtId.EnemyBulletAlt), p.sr.sprite);
        }
    }
}
