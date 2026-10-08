using System.Collections.Generic;
using UnityEngine;

namespace MiniMayhem
{
    /// <summary>
    /// Data-driven pacing: continuous trickle (rate curve by minute), scripted timeline entries, swarm bursts,
    /// mini bosses and bosses. At the enemy cap, fodder is replaced by elites instead of adding more bodies.
    /// Arena spawns are telegraphed with a "!" marker first.
    /// </summary>
    public class WaveDirector
    {
        struct Pending { public EnemyDefinition def; public Vector2 pos; public float delay; public bool elite; public float hpMult; }

        readonly RunController run;
        readonly WaveTimeline tl;
        readonly BiomeDefinition biome;
        readonly System.Random rnd;
        readonly List<Pending> pending = new();
        readonly List<Pending> stillPending = new();
        int nextEntry, nextSwarm, nextMini, nextBoss;
        float trickleAcc, breather, overflow;
        readonly List<SpawnEntry> entries;

        public int BossesSpawned => nextBoss;
        public Enemy FinaleBoss { get; private set; }
        public bool FinaleSpawned { get; private set; }
        public float NextBossTime => tl.bossTimes != null && nextBoss < tl.bossTimes.Length ? tl.bossTimes[nextBoss] : -1f;
        public bool Paused { get; set; }

        public WaveDirector(RunController run, BiomeDefinition biome, int seed)
        {
            this.run = run;
            this.biome = biome;
            tl = biome.waves != null ? biome.waves : ScriptableObject.CreateInstance<WaveTimeline>();
            rnd = new System.Random(seed);
            entries = new List<SpawnEntry>(tl.entries);
            entries.Sort((a, b) => a.time.CompareTo(b.time));
            run.Enemies.Killed += OnKilled;
        }

        void OnKilled(Enemy e)
        {
            if (e.isBoss) breather = tl.breatherAfterBoss;
        }

        float ViewRadius
        {
            get
            {
                var cam = run.CameraRig != null ? run.CameraRig.Camera : null;
                float size = cam != null ? cam.orthographicSize : run.Config.cameraSize;
                float aspect = cam != null ? cam.aspect : 16f / 9f;
                return Mathf.Sqrt(size * size * (1 + aspect * aspect)) + 1.5f;
            }
        }

        public Vector2 OffscreenPoint(Vector2 center)
        {
            float a = (float)rnd.NextDouble() * Mathf.PI * 2f;
            return center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (ViewRadius + (float)rnd.NextDouble() * 2f);
        }

        public void Tick(float dt)
        {
            if (Paused) return;
            float t = run.State.time;
            float minute = t / 60f;
            if (breather > 0) breather -= dt;

            // Telegraphed arena spawns.
            if (pending.Count > 0)
            {
                stillPending.Clear();
                foreach (var p in pending)
                {
                    var q = p;
                    q.delay -= dt;
                    if (q.delay <= 0) SpawnNow(q.def, q.pos, q.elite, q.hpMult);
                    else stillPending.Add(q);
                }
                pending.Clear();
                pending.AddRange(stillPending);
            }

            // Bosses (fixed timer).
            if (tl.bossTimes != null && nextBoss < tl.bossTimes.Length && t >= tl.bossTimes[nextBoss])
            {
                bool finale = nextBoss == tl.bossTimes.Length - 1;
                float mult = Mathf.Pow(run.Config.bossRepeatMultiplier, nextBoss);
                var boss = SpawnNow(biome.boss, BossPoint(), false, mult);
                nextBoss++;
                if (finale) { FinaleBoss = boss; FinaleSpawned = true; }
                run.Map.Expand();
                run.OnBossSpawned(boss, finale);
            }

            // Mini bosses.
            if (tl.miniBossTimes != null && nextMini < tl.miniBossTimes.Length && t >= tl.miniBossTimes[nextMini] && biome.miniBosses.Length > 0)
            {
                var def = biome.miniBosses[nextMini % biome.miniBosses.Length];
                float mult = Mathf.Pow(run.Config.bossRepeatMultiplier, nextMini / biome.miniBosses.Length);
                SpawnNow(def, BossPoint(), false, mult);
                nextMini++;
                run.Announce($"{def.displayName} appears!", new Color(1f, 0.7f, 0.3f));
                Sfx.Play(SfxId.Warning, 0.5f);
            }

            // Swarm bursts.
            if (tl.swarmTimes != null && nextSwarm < tl.swarmTimes.Length && t >= tl.swarmTimes[nextSwarm])
            {
                nextSwarm++;
                SpawnGroup(SwarmEnemy(), tl.swarmSize + (int)(minute * 6), SpawnPattern.SwarmBurst, false);
                run.Announce("SWARM INCOMING!", new Color(1f, 0.35f, 0.35f));
                Sfx.Play(SfxId.Warning, 0.7f);
            }

            // Scripted timeline.
            while (nextEntry < entries.Count && t >= entries[nextEntry].time)
            {
                var e = entries[nextEntry++];
                SpawnGroup(e.enemy, e.count, e.pattern, e.elite);
            }

            // Continuous trickle.
            float rate = tl.trickleRate.Evaluate(minute) * (breather > 0 ? 0.3f : 1f);
            trickleAcc += rate * dt;
            while (trickleAcc >= 1f)
            {
                trickleAcc -= 1f;
                var def = RandomNormal(minute);
                bool elite = rnd.NextDouble() < tl.eliteChance.Evaluate(minute);
                if (run.Enemies.Count >= run.Config.enemyCap)
                {
                    // Graceful degradation: stronger enemies replace fodder instead of more bodies.
                    overflow += 1f;
                    if (overflow >= 20f) { overflow = 0; ReplaceFodderWithElite(def); }
                    continue;
                }
                SpawnAt(def, run.Map.Bounded ? run.Map.ArenaPoint(run.Hero.Position, 6f, rnd) : OffscreenPoint(run.Hero.Position + run.Hero.Velocity), elite);
            }
        }

        EnemyDefinition SwarmEnemy()
        {
            foreach (var n in biome.normals) if (n != null && n.behaviour == EnemyBehaviour.Swarm) return n;
            return biome.normals[0];
        }

        EnemyDefinition RandomNormal(float minute)
        {
            int unlocked = 0;
            for (int i = 0; i < biome.normals.Length; i++)
            {
                float m = tl.normalUnlockMinute != null && i < tl.normalUnlockMinute.Length ? tl.normalUnlockMinute[i] : 0f;
                if (minute >= m) unlocked = i + 1;
            }
            unlocked = Mathf.Max(1, unlocked);
            // Favour the newest unlocks a bit so the mix changes over the match.
            int idx = rnd.NextDouble() < 0.35 ? unlocked - 1 : rnd.Next(unlocked);
            return biome.normals[Mathf.Clamp(idx, 0, biome.normals.Length - 1)];
        }

        void ReplaceFodderWithElite(EnemyDefinition def)
        {
            Enemy far = null;
            float best = 0;
            foreach (var e in run.Enemies.Active)
            {
                if (!e.alive || e.elite || e.def.IsBossLike) continue;
                float d = (e.pos - run.Hero.Position).sqrMagnitude;
                if (d > best) { best = d; far = e; }
            }
            if (far == null) return;
            Vector2 p = far.pos;
            run.Enemies.Kill(far, null, noDrops: true);
            run.State.kills--; // not a real kill
            SpawnNow(def, p, true, 1f);
        }

        Vector2 BossPoint() => run.Map.Bounded ? run.Map.ArenaPoint(run.Hero.Position, 8f, rnd) : run.Hero.Position + Random.insideUnitCircle.normalized * (ViewRadius - 2f);

        public void SpawnGroup(EnemyDefinition def, int count, SpawnPattern pattern, bool elite)
        {
            if (def == null) return;
            Vector2 hero = run.Hero.Position;
            float R = ViewRadius + 0.5f;
            float baseA = (float)rnd.NextDouble() * Mathf.PI * 2f;
            Vector2 side = new(Mathf.Cos(baseA), Mathf.Sin(baseA));
            int room = Mathf.Max(0, Mathf.RoundToInt(run.Config.enemyCap * 1.2f) - run.Enemies.Count);
            count = Mathf.Min(count, room);
            for (int i = 0; i < count; i++)
            {
                Vector2 p;
                switch (pattern)
                {
                    case SpawnPattern.Ring:
                    {
                        float a = baseA + i * Mathf.PI * 2f / count;
                        p = hero + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (run.Map.Bounded ? 7f : R);
                        break;
                    }
                    case SpawnPattern.SwarmBurst:
                    {
                        float a = baseA + i * Mathf.PI * 2f / Mathf.Max(1, count / 2) + (float)rnd.NextDouble() * 0.2f;
                        float r = (run.Map.Bounded ? 8f : R) + (i % 2) * 1.2f + (float)rnd.NextDouble();
                        p = hero + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                        break;
                    }
                    case SpawnPattern.Line:
                    {
                        Vector2 perp = new(-side.y, side.x);
                        p = hero + side * R + perp * ((i - count * 0.5f) * 0.9f);
                        break;
                    }
                    case SpawnPattern.Pincer:
                    {
                        Vector2 s = i % 2 == 0 ? side : -side;
                        p = hero + s * R + Random.insideUnitCircle * 2f;
                        break;
                    }
                    case SpawnPattern.Cluster:
                        p = hero + side * (R + 1f) + Random.insideUnitCircle * 2.2f;
                        break;
                    default:
                        p = OffscreenPoint(hero);
                        break;
                }
                if (run.Map.Bounded)
                {
                    run.Map.ResolveCircle(ref p, 0.6f);
                    if ((p - hero).sqrMagnitude < 25f) p = run.Map.ArenaPoint(hero, 6f, rnd);
                }
                SpawnAt(def, p, elite);
            }
        }

        void SpawnAt(EnemyDefinition def, Vector2 p, bool elite, float hpMult = 1f)
        {
            if (run.Map.Bounded)
            {
                pending.Add(new Pending { def = def, pos = p, delay = 0.7f, elite = elite, hpMult = hpMult });
                run.Fx.Spawn(ArtId.Exclaim, p, 0.6f, new Color(1, 1, 1, 0.9f), 0.7f, growTo: 1.2f, order: 35);
                return;
            }
            SpawnNow(def, p, elite, hpMult);
        }

        Enemy SpawnNow(EnemyDefinition def, Vector2 p, bool elite, float hpMult)
        {
            var e = run.Enemies.Spawn(def, p, elite, hpMult);
            if (e != null) run.Meta?.Discover("e:" + def.id);
            return e;
        }

        public int PendingCount => pending.Count;
    }
}
