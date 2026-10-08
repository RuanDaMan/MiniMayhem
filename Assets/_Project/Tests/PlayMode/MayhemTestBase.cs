using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MiniMayhem.Tests
{
    /// <summary>Boots the real game scene with a virtual gamepad and a throwaway save file.</summary>
    public abstract class MayhemTestBase : InputTestFixture
    {
        protected Gamepad pad;
        protected GameFlow flow;
        protected GameDatabase db;
        protected MetaService meta;

        public static string TestSavePath => System.IO.Path.Combine(Application.temporaryCachePath, "minimayhem_test_save.json");

        protected IEnumerator Boot(FlowState start = FlowState.Title, bool keepSave = false)
        {
            SaveService.PathOverride = TestSavePath;
            GameFlow.StartState = start;
            if (!keepSave && System.IO.File.Exists(TestSavePath)) System.IO.File.Delete(TestSavePath);
            pad = InputSystem.AddDevice<Gamepad>();
            yield return TestUtil.LoadScene("MiniMayhem");
            flow = Object.FindAnyObjectByType<GameFlow>();
            Assert.IsNotNull(flow, "GameFlow exists");
            db = flow.Db;
            meta = flow.Meta;
            yield return null;
        }

        protected RunController Run => flow.Run;

        protected WeaponDefinition Weapon(string id) => db.Weapon(id);
        protected ItemDefinition Item(string id) => db.Item(id);

        /// <summary>Start a run directly (skips the menus).</summary>
        protected IEnumerator StartRun(string weapon = "pea_shooter", int biome = 0, MatchMode mode = MatchMode.OutlastTime, MapStyle style = MapStyle.Endless, int seed = 12345)
        {
            flow.StartRun(new RunSetup { biome = db.biomes[biome], matchIndex = 0, mode = mode, style = style, startingWeapon = weapon != null ? Weapon(weapon) : null, seed = seed });
            yield return null;
            Assert.AreEqual(FlowState.Running, flow.State);
        }

        /// <summary>Fast-forward the simulation with fixed steps (independent of frame time).</summary>
        protected static void Simulate(RunController run, float seconds, float dt = 1f / 30f)
        {
            for (float t = 0; t < seconds && !run.Ended; t += dt) run.Step(dt);
        }

        protected IEnumerator Tap(UnityEngine.InputSystem.Controls.ButtonControl b)
        {
            Press(b);
            yield return null;
            Release(b);
            yield return null;
            yield return null;
        }

        [TearDown]
        public void ResetTime()
        {
            Time.timeScale = 1f;
            GameFlow.StartState = null;
        }
    }
}
