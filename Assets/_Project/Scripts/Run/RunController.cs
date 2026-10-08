using System;
using UnityEngine;

namespace MiniMayhem
{
    /// <summary>
    /// Owns one match: builds the hero, map, enemy/projectile/zone/pickup systems, weapons and the wave director,
    /// ticks them in a fixed order, and decides win / loss for the match mode. Destroyed when the run ends.
    /// </summary>
    public class RunController : MonoBehaviour
    {
        public GameDatabase Db { get; private set; }
        public GameConfig Config { get; private set; }
        public MetaService Meta { get; private set; }
        public Controls Controls { get; private set; }
        public CameraRig CameraRig { get; private set; }
        public RunSetup Setup { get; private set; }
        public BiomeDefinition Biome => Setup.biome;
        public MatchMode Mode => Setup.mode;

        public RunState State { get; } = new();
        public PlayerStats Stats { get; private set; }
        public Hero Hero { get; private set; }
        public EnemyManager Enemies { get; private set; }
        public ProjectileManager Projectiles { get; private set; }
        public ZoneManager Zones { get; private set; }
        public PickupManager Pickups { get; private set; }
        public FxService Fx { get; private set; }
        public DamageNumbers Numbers { get; private set; }
        public GameMap Map { get; private set; }
        public WaveDirector Director { get; private set; }
        public Inventory Inventory { get; private set; }
        public LevelUpService LevelUp { get; private set; }
        public Transform WorldRoot => transform;

        /// <summary>Set by the game flow while menus (pause, level-up) are open.</summary>
        public bool Paused { get; set; }
        public bool Ended => State.finished;
        public RunResult Result { get; private set; }

        /// <summary>0..1 sandstorm strength (Desert hazard): the HUD darkens the edges of the screen.</summary>
        public float Sandstorm { get; private set; }
        float sandTimer = 40f;

        public event Action<RunController> RunEnded;
        public event Action<Enemy, bool> BossSpawned;
        public event Action<string, Color> Announced;
        public event Action LeveledUp;

        public static RunController Create(GameDatabase db, MetaService meta, Controls controls, CameraRig cam, RunSetup setup)
        {
            var go = new GameObject("Run");
            var run = go.AddComponent<RunController>();
            run.Init(db, meta, controls, cam, setup);
            return run;
        }

        void Init(GameDatabase db, MetaService meta, Controls controls, CameraRig cam, RunSetup setup)
        {
            Db = db;
            Config = db.config;
            Meta = meta;
            Controls = controls;
            CameraRig = cam;
            Setup = setup;
            if (setup.seed == 0) setup.seed = UnityEngine.Random.Range(1, int.MaxValue);
            Art.Build(db);

            Stats = new PlayerStats(Config);
            meta?.AccumulateStats(Stats.Meta);
            Stats.Recompute();

            Fx = new FxService(this, transform);
            Numbers = new DamageNumbers(transform);
            Numbers.Enabled = meta?.Data.settings.damageNumbers ?? true;
            Map = new GameMap(this, setup.biome, setup.style, transform, setup.seed);
            Hero = new Hero(this, transform);
            Hero.Teleport(Vector2.zero);
            Enemies = new EnemyManager(this, transform);
            Projectiles = new ProjectileManager(this, transform);
            Zones = new ZoneManager(this, transform);
            Pickups = new PickupManager(this, transform);
            Inventory = new Inventory(this, db);
            LevelUp = new LevelUpService(this, db, setup.seed);
            Director = new WaveDirector(this, setup.biome, setup.seed);
            Enemies.Killed += OnEnemyKilled;

            State.xpToNext = Config.XpToNext(1);
            State.killTarget = setup.biome.killTarget;
            if (setup.startingWeapon != null) Inventory.AddWeapon(setup.startingWeapon);
            cam?.Snap(Vector2.zero);
            meta?.RecordPlay(setup.biome);
        }

        void OnDestroy()
        {
            Inventory?.DisposeAll();
        }

        void Update()
        {
            if (Paused || Ended) return;
            Step(Mathf.Min(Time.deltaTime, 1f / 20f));
        }

        /// <summary>Advance the simulation (also used by tests to fast-forward).</summary>
        public void Step(float dt)
        {
            if (Ended) return;
            State.time += dt;
            Hero.Tick(dt);
            if (Ended) return;
            Director.Tick(dt);
            Enemies.Tick(dt);
            if (Ended) return;
            Inventory.Tick(dt);
            Projectiles.Tick(dt);
            Zones.Tick(dt);
            Pickups.Tick(dt);
            Fx.Tick(dt);
            Numbers.Tick(dt);
            Map.Tick(CameraRig != null ? CameraRig.Position : Hero.Position, dt);
            CameraRig?.Tick(Hero.Position, Enemies.Count, Config, dt);
            TickSandstorm(dt);
            CheckWin();
        }

        void TickSandstorm(float dt)
        {
            if (Biome.hazard != HazardType.Sandstorm) { Sandstorm = 0; return; }
            sandTimer -= dt;
            // 40 s calm, then a 14 s storm.
            float t = -sandTimer;
            float target = sandTimer < 0 && t < 14f ? 1f : 0f;
            if (sandTimer < -14f) sandTimer = 40f;
            if (sandTimer < 0 && t < 0.05f) Announce("Sandstorm!", new Color(1f, 0.85f, 0.5f));
            Sandstorm = Mathf.MoveTowards(Sandstorm, target, dt / 2f);
        }

        void CheckWin()
        {
            switch (Mode)
            {
                case MatchMode.OutlastTime:
                    if (State.time >= Config.matchLength) EndRun(true, "You outlasted the mayhem!");
                    break;
                case MatchMode.KillCount:
                    if (State.kills >= State.killTarget) EndRun(true, $"{State.killTarget} kills reached!");
                    else if (State.time >= Config.matchLength) EndRun(false, "Time's up!");
                    break;
                case MatchMode.Survive:
                    // Won when the finale boss is defeated (see OnEnemyKilled).
                    break;
            }
        }

        void OnEnemyKilled(Enemy e)
        {
            if (!e.isBoss) return;
            State.bossKills++;
            Announce($"{e.def.displayName} defeated!", new Color(1f, 0.9f, 0.4f));
            Projectiles.ClearHostile();
            if (Mode == MatchMode.Survive && Director.FinaleSpawned && Director.FinaleBoss == e && Director.FinaleBoss.serial == e.serial)
                EndRun(true, "Finale boss defeated!");
        }

        // ------------------------------------------------------------------ economy

        public void AddXp(float amount)
        {
            State.xp += amount * Stats.XpMult;
            while (State.xp >= State.xpToNext)
            {
                State.xp -= State.xpToNext;
                State.level++;
                State.xpToNext = Config.XpToNext(State.level);
                State.pendingLevelUps++;
                LeveledUp?.Invoke();
            }
        }

        public void AddGold(int amount) => State.gold += amount * Config.coinValue * Stats.GoldMult;

        public void OpenChest(int levelUps)
        {
            State.pendingLevelUps += Mathf.Max(1, levelUps);
            Announce("Treasure!", new Color(1f, 0.85f, 0.3f));
        }

        /// <summary>Screen-clearing bomb: wipes normal enemies on screen, hurts bosses, clears enemy bullets.</summary>
        public void Bomb()
        {
            var cam = CameraRig?.Camera;
            float half = cam != null ? cam.orthographicSize * cam.aspect + 1f : 16f;
            float halfY = cam != null ? cam.orthographicSize + 1f : 10f;
            var list = new System.Collections.Generic.List<Enemy>(Enemies.Active);
            foreach (var e in list)
            {
                if (!e.alive) continue;
                if (Mathf.Abs(e.pos.x - Hero.Position.x) > half || Mathf.Abs(e.pos.y - Hero.Position.y) > halfY) continue;
                if (e.def.IsBossLike) Enemies.Damage(e, e.maxHp * 0.12f, false, Vector2.zero, 0, null);
                else Enemies.Kill(e, null);
            }
            Projectiles.ClearHostile();
            Fx.Spawn(ArtId.Circle, Hero.Position, 3f, new Color(1f, 0.95f, 0.8f, 0.8f), 0.45f, growTo: 12f);
            Sfx.Play(SfxId.Bomb, 0.9f);
            Shake(0.5f);
        }

        public void Announce(string text, Color color) => Announced?.Invoke(text, color);
        public void Shake(float amount) => CameraRig?.AddShake(amount);

        public void OnBossSpawned(Enemy boss, bool finale)
        {
            if (boss == null) return;
            Sfx.Play(SfxId.BossRoar, 0.9f);
            Announce(finale && Mode == MatchMode.Survive ? $"FINALE: {boss.def.displayName}!" : $"BOSS: {boss.def.displayName}!", new Color(1f, 0.35f, 0.35f));
            Shake(0.4f);
            BossSpawned?.Invoke(boss, finale);
        }

        // ------------------------------------------------------------------ end

        public void EndRun(bool won, string reason)
        {
            if (State.finished) return;
            State.finished = true;
            State.won = won;
            State.endReason = reason;
            Result = BuildResult();
            Sfx.Play(won ? SfxId.Win : SfxId.Lose, 0.8f);
            RunEnded?.Invoke(this);
        }

        RunResult BuildResult()
        {
            var r = new RunResult
            {
                won = State.won, reason = State.endReason, time = State.time, kills = State.kills, level = State.level,
                runGold = Mathf.FloorToInt(State.gold), biome = Biome, matchIndex = Setup.matchIndex, mode = Mode, style = Setup.style,
            };
            r.fromGold = r.runGold;
            r.fromKills = Mathf.RoundToInt(State.kills * Config.goldPerKill);
            r.fromTime = Mathf.RoundToInt(State.time / 60f * Config.goldPerMinute);
            r.winBonus = State.won ? Mathf.RoundToInt(Config.winBonus * Biome.difficulty) : 0;
            r.modeMultiplier = Config.Mode(Mode).rewardMultiplier;
            r.keepFraction = State.won ? 1f : Stats.DeathKeep;
            float total = (r.fromGold + r.fromKills + r.fromTime + r.winBonus) * r.modeMultiplier * r.keepFraction;
            r.metaGold = Mathf.Max(0, Mathf.RoundToInt(total));
            foreach (var w in Inventory.Weapons)
                r.weapons.Add(new WeaponSummary { name = w.def.displayName, icon = w.def.icon, level = w.level, form = w.def.form, damage = w.damageDealt, kills = w.kills });
            r.weapons.Sort((a, b) => b.damage.CompareTo(a.damage));
            return r;
        }
    }
}
