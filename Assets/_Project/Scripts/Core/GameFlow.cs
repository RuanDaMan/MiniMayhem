using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MiniMayhem
{
    public enum FlowState { Title, MatchSelect, WeaponPick, Running, LevelUp, Paused, BossIntro, Results, SkillTree, Codex, Settings }

    /// <summary>
    /// The game's state machine: Title -> Match select -> Weapon pick -> Running (Level-up / Pause / Boss intro)
    /// -> Results -> back to menus. Owns the UI screens and the current run.
    /// </summary>
    public class GameFlow : MonoBehaviour
    {
        public GameDatabase Db { get; private set; }
        public MetaService Meta { get; private set; }
        public Controls Controls { get; private set; }
        public CameraRig CameraRig { get; private set; }
        public AudioService Audio { get; private set; }
        public RunController Run { get; private set; }
        public FlowState State { get; private set; } = FlowState.Title;
        public Canvas Canvas { get; private set; }

        public TitleScreen Title { get; private set; }
        public MatchSelectScreen MatchSelect { get; private set; }
        public WeaponPickScreen WeaponPick { get; private set; }
        public HudScreen Hud { get; private set; }
        public LevelUpScreen LevelUp { get; private set; }
        public PauseScreen Pause { get; private set; }
        public ResultsScreen Results { get; private set; }
        public SkillTreeScreen SkillTree { get; private set; }
        public CodexScreen Codex { get; private set; }
        public SettingsScreen Settings { get; private set; }

        readonly List<UiScreen> screens = new();
        readonly Dictionary<(int, int), MatchMode> plannedModes = new();
        FlowState settingsReturn, codexReturn;
        float bossIntroTimer, resultsDelay;
        System.Random modeRng = new();

        public BiomeDefinition SelectedBiome { get; set; }
        public int SelectedMatch { get; set; }

        /// <summary>Tests can skip the title screen.</summary>
        public static FlowState? StartState;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => StartState = null;

        public void Init(GameDatabase db, MetaService meta, Controls controls, CameraRig cam, AudioService audio)
        {
            Db = db; Meta = meta; Controls = controls; CameraRig = cam; Audio = audio;
            GameServices.Register(this);
            BuildCanvas();
            Title = Add(new TitleScreen(), "Title");
            MatchSelect = Add(new MatchSelectScreen(), "MatchSelect");
            WeaponPick = Add(new WeaponPickScreen(), "WeaponPick");
            Hud = Add(new HudScreen(), "Hud");
            LevelUp = Add(new LevelUpScreen(), "LevelUp");
            Pause = Add(new PauseScreen(), "Pause");
            Results = Add(new ResultsScreen(), "Results");
            SkillTree = Add(new SkillTreeScreen(), "SkillTree");
            Codex = Add(new CodexScreen(), "Codex");
            Settings = Add(new SettingsScreen(), "Settings");
            ApplySettings();
            Go(StartState ?? FlowState.Title);
        }

        void OnDestroy() => GameServices.Unregister(this);

        T Add<T>(T screen, string name) where T : UiScreen
        {
            screen.Build(Canvas.transform, this, name);
            screens.Add(screen);
            return screen;
        }

        void BuildCanvas()
        {
            var go = new GameObject("UI", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            Canvas = go.AddComponent<Canvas>();
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Canvas.sortingOrder = 100;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
        }

        public void ApplySettings()
        {
            var s = Meta.Data.settings;
            if (Audio != null) { Audio.Master = s.master; Audio.MusicVolume = s.music; Audio.SfxVolume = s.sfx; }
            if (CameraRig != null) CameraRig.ShakeEnabled = s.screenShake;
            if (Run != null) Run.Numbers.Enabled = s.damageNumbers;
        }

        // ------------------------------------------------------------------ state machine

        public void Go(FlowState next)
        {
            State = next;
            bool inRun = next is FlowState.Running or FlowState.LevelUp or FlowState.Paused or FlowState.BossIntro or FlowState.Results;
            if (Run != null) Run.Paused = next != FlowState.Running;
            foreach (var s in screens)
            {
                bool show = s == Hud ? inRun && Run != null : s == ScreenFor(next) || (s == Results && next == FlowState.Results);
                if (show && !s.Visible) s.Show();
                else if (!show && s.Visible) s.Hide();
                else if (show && s.Visible && s != Hud) s.Show(); // re-entering: refresh + reselect
            }
            if (!inRun && Audio != null && Audio.CurrentMusic != "title") Audio.PlayTitleMusic();
        }

        UiScreen ScreenFor(FlowState s) => s switch
        {
            FlowState.Title => Title,
            FlowState.MatchSelect => MatchSelect,
            FlowState.WeaponPick => WeaponPick,
            FlowState.LevelUp => LevelUp,
            FlowState.Paused => Pause,
            FlowState.Results => Results,
            FlowState.SkillTree => SkillTree,
            FlowState.Codex => Codex,
            FlowState.Settings => Settings,
            _ => null,
        };

        void Update()
        {
            if (Controls == null) return;
            if (Controls.Debug.WasPressedThisFrame())
            {
                Meta.Data.settings.showFps = !Meta.Data.settings.showFps;
            }

            switch (State)
            {
                case FlowState.Running:
                    if (Run == null) { Go(FlowState.Title); break; }
                    if (Run.Ended) break;
                    if (Run.State.pendingLevelUps > 0) { OpenLevelUp(); break; }
                    if (Controls.Pause.WasPressedThisFrame()) { Go(FlowState.Paused); Sfx.Play(SfxId.Click); }
                    break;
                case FlowState.BossIntro:
                    bossIntroTimer -= Time.unscaledDeltaTime;
                    if (bossIntroTimer <= 0) Go(FlowState.Running);
                    break;
                case FlowState.Paused:
                    if (Controls.Pause.WasPressedThisFrame() || Controls.Back.WasPressedThisFrame()) Go(FlowState.Running);
                    break;
                default:
                    var cur = ScreenFor(State);
                    if (cur != null && Controls.Back.WasPressedThisFrame()) cur.OnBack();
                    break;
            }

            if (resultsDelay > 0)
            {
                resultsDelay -= Time.unscaledDeltaTime;
                if (resultsDelay <= 0) Go(FlowState.Results);
            }

            foreach (var s in screens) if (s.Visible) s.Tick();
            var active = ScreenFor(State);
            active?.EnsureSelection();
        }

        // ------------------------------------------------------------------ matches

        public MatchMode PlannedMode(BiomeDefinition b, int match)
        {
            var key = (b.index, match);
            if (!plannedModes.TryGetValue(key, out var m))
            {
                m = (MatchMode)modeRng.Next(3);
                plannedModes[key] = m;
            }
            return m;
        }

        public MapStyle StyleFor(BiomeDefinition b, int match) =>
            b.matchStyles != null && match < b.matchStyles.Length ? b.matchStyles[match] : MapStyle.Endless;

        public void BeginWeaponPick(BiomeDefinition b, int match)
        {
            SelectedBiome = b;
            SelectedMatch = match;
            Go(FlowState.WeaponPick);
        }

        public void StartRun(RunSetup setup)
        {
            DestroyRun();
            Meta.Data.lastWeapon = setup.startingWeapon != null ? setup.startingWeapon.id : null;
            Run = RunController.Create(Db, Meta, Controls, CameraRig, setup);
            Run.RunEnded += OnRunEnded;
            Run.BossSpawned += OnBossSpawned;
            Run.Numbers.Enabled = Meta.Data.settings.damageNumbers;
            Hud.Bind(Run);
            Audio?.PlayBiomeMusic(setup.biome);
            Go(FlowState.Running);
        }

        public void StartSelectedRun(WeaponDefinition weapon)
        {
            var b = SelectedBiome != null ? SelectedBiome : Db.biomes[0];
            StartRun(new RunSetup
            {
                biome = b, matchIndex = SelectedMatch, mode = PlannedMode(b, SelectedMatch), style = StyleFor(b, SelectedMatch),
                startingWeapon = weapon, seed = Random.Range(1, int.MaxValue),
            });
        }

        void OpenLevelUp()
        {
            Run.LevelUp.Roll();
            Sfx.Play(SfxId.LevelUp, 0.7f);
            Go(FlowState.LevelUp);
        }

        /// <summary>Called by the level-up screen after a pick / skip.</summary>
        public void LevelUpDone()
        {
            if (Run != null && Run.State.pendingLevelUps > 0) { OpenLevelUp(); return; }
            Go(FlowState.Running);
        }

        void OnBossSpawned(Enemy boss, bool finale)
        {
            if (State != FlowState.Running) return;
            bossIntroTimer = 1.3f;
            Go(FlowState.BossIntro);
        }

        void OnRunEnded(RunController run)
        {
            var r = run.Result;
            var stats = Meta.Data.stats;
            stats.runs++;
            stats.kills += r.kills;
            stats.bossKills += run.State.bossKills;
            stats.bestSurvived = Mathf.Max(stats.bestSurvived, r.time);
            stats.evolutions += run.Inventory.Evolutions;
            stats.fusions += run.Inventory.Fusions;
            stats.playSeconds += r.time;
            if (r.won)
            {
                stats.wins++;
                r.unlocks.AddRange(Meta.CompleteMatch(r.biome, r.matchIndex));
            }
            Meta.AddGold(r.metaGold);
            Meta.Save();
            // Re-roll the mode for this match so replays feel fresh.
            plannedModes.Remove((r.biome.index, r.matchIndex));
            Run.Paused = true;
            resultsDelay = 1.2f;
        }

        public void AbandonRun()
        {
            if (Run == null || Run.Ended) return;
            Run.EndRun(false, "Gave up");
            resultsDelay = 0.01f;
        }

        public void DestroyRun()
        {
            resultsDelay = 0f;
            if (Run == null) return;
            Run.RunEnded -= OnRunEnded;
            Run.BossSpawned -= OnBossSpawned;
            Hud.Bind(null);
            Destroy(Run.gameObject);
            Run = null;
        }

        public void ExitToMenu(FlowState menu)
        {
            DestroyRun();
            CameraRig?.Snap(Vector2.zero);
            Go(menu);
        }

        public void OpenSettings(FlowState returnTo)
        {
            settingsReturn = returnTo;
            Go(FlowState.Settings);
        }

        public void CloseSettings()
        {
            Meta.Save();
            Go(settingsReturn);
        }

        public void OpenCodex(FlowState returnTo)
        {
            codexReturn = returnTo;
            Go(FlowState.Codex);
        }

        public void CloseCodex() => Go(codexReturn);

        public void Quit()
        {
            Meta.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
