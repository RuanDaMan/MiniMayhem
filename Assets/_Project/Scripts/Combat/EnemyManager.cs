using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiniMayhem
{
    /// <summary>
    /// Pooled enemy simulation: custom movement + separation through a spatial hash, contact damage, status
    /// effects, behaviours (chaser, shooter, charger, ...) and the boss pattern brain.
    /// </summary>
    public class EnemyManager
    {
        readonly RunController run;
        readonly Transform root;
        readonly List<Enemy> pool = new();
        readonly List<Enemy> active = new();
        readonly SpatialHash hash;
        readonly Enemy[] neighbours = new Enemy[24];
        readonly List<Enemy> queryBuf = new();
        int serialCounter;

        public IReadOnlyList<Enemy> Active => active;
        public int Count => active.Count;
        public SpatialHash Hash => hash;

        public event Action<Enemy> Killed;
        public event Action<Enemy> Spawned;

        public EnemyManager(RunController run, Transform parent)
        {
            this.run = run;
            root = new GameObject("Enemies").transform;
            root.SetParent(parent, false);
            hash = new SpatialHash(2f, 2048);
            hash.Clear();
        }

        // ------------------------------------------------------------------ spawning

        Enemy Rent()
        {
            Enemy e = null;
            for (int i = pool.Count - 1; i >= 0; i--)
                if (!pool[i].alive && !pool[i].go.activeSelf) { e = pool[i]; pool.RemoveAt(i); break; }
            if (e == null)
            {
                e = new Enemy();
                e.go = new GameObject("Enemy");
                e.tf = e.go.transform;
                e.tf.SetParent(root, false);
                e.sr = e.go.AddComponent<SpriteRenderer>();
                e.sr.sharedMaterial = Art.SpriteMaterial;
            }
            return e;
        }

        public Enemy Spawn(EnemyDefinition def, Vector2 pos, bool elite = false, float hpMultiplier = 1f)
        {
            if (def == null) return null;
            var e = Rent();
            var cfg = run.Config;
            float minute = run.State.time / 60f;
            float diff = run.Biome != null ? run.Biome.difficulty : 1f;
            float hpScale = (1f + cfg.enemyHpPerMinute * minute) * diff * hpMultiplier;
            float dmgScale = (1f + cfg.enemyDamagePerMinute * minute) * Mathf.Sqrt(diff);
            float spdScale = 1f + cfg.enemySpeedPerMinute * minute;
            if (def.IsBossLike) hpScale = diff * hpMultiplier * (1f + cfg.enemyHpPerMinute * minute * 0.5f);

            e.index = active.Count;
            e.serial = ++serialCounter;
            e.alive = true;
            e.def = def;
            e.elite = elite;
            e.isBoss = def.tier == EnemyTier.Boss;
            e.pos = pos;
            e.vel = Vector2.zero;
            e.knock = Vector2.zero;
            e.maxHp = e.hp = def.maxHp * hpScale * (elite ? cfg.eliteHpMultiplier : 1f);
            e.damage = def.damage * dmgScale * (elite ? 1.4f : 1f);
            e.speed = def.speed * spdScale * (elite ? 1.1f : 1f);
            e.radius = def.radius * (elite ? 1.25f : 1f);
            e.armor = def.armor;
            e.knockResist = elite ? Mathf.Max(def.knockbackResist, 0.5f) : def.knockbackResist;
            e.age = 0; e.flash = 0; e.spawnAnim = 0; e.phase = UnityEngine.Random.value * 10f;
            e.slowTimer = 0; e.slowFactor = 0; e.poisonTimer = 0; e.poisonDps = 0; e.poisonTick = 0;
            e.attackTimer = def.attackInterval * UnityEngine.Random.Range(0.5f, 1f);
            e.stateTimer = 0; e.state = 0; e.launchTimer = 0; e.teleportCooldown = 0;
            e.submerged = def.behaviour == EnemyBehaviour.Ambusher;
            e.patternIndex = 0; e.patternTimer = 2.5f; e.patternStep = 0; e.patternTick = 0;
            Array.Clear(e.nextHit, 0, e.nextHit.Length);

            e.go.SetActive(true);
            e.go.name = def.id;
            e.sr.sprite = Art.Enemy(def);
            e.sr.color = e.submerged ? new Color(1, 1, 1, 0.35f) : Color.white;
            e.sr.sortingOrder = def.tier == EnemyTier.Boss ? 6 : def.tier == EnemyTier.MiniBoss ? 5 : 0;
            e.baseScale = e.radius * 2f * def.visualScale;
            e.tf.localScale = Vector3.zero;
            e.tf.position = new Vector3(pos.x, pos.y, 0);

            if (elite)
            {
                if (e.eliteRing == null)
                {
                    var ring = new GameObject("EliteRing");
                    ring.transform.SetParent(e.tf, false);
                    e.eliteRing = ring.AddComponent<SpriteRenderer>();
                    e.eliteRing.sharedMaterial = Art.SpriteMaterial;
                    e.eliteRing.sprite = Art.Get(ArtId.Glow);
                    e.eliteRing.sortingOrder = -1;
                }
                e.eliteRing.enabled = true;
                e.eliteRing.color = new Color(1f, 0.85f, 0.2f, 0.75f);
                e.eliteRing.transform.localScale = Vector3.one * 1.5f;
            }
            else if (e.eliteRing != null) e.eliteRing.enabled = false;

            if (def.IsBossLike)
            {
                if (e.barBack == null)
                {
                    e.barBack = MakeBar(e.tf, "BarBack", new Color(0.1f, 0.08f, 0.15f, 0.85f), 20);
                    e.barFill = MakeBar(e.tf, "BarFill", new Color(1f, 0.3f, 0.35f), 21);
                }
                e.barBack.enabled = e.barFill.enabled = def.tier == EnemyTier.MiniBoss;
            }
            else if (e.barBack != null) e.barBack.enabled = e.barFill.enabled = false;

            active.Add(e);
            hash.MaxRadius = Mathf.Max(hash.MaxRadius, e.radius);
            Spawned?.Invoke(e);
            return e;
        }

        static SpriteRenderer MakeBar(Transform parent, string name, Color c, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sharedMaterial = Art.SpriteMaterial;
            sr.sprite = Art.Get(ArtId.Pixel);
            sr.color = c;
            sr.sortingOrder = order;
            return sr;
        }

        void Despawn(Enemy e)
        {
            e.alive = false;
            e.go.SetActive(false);
            pool.Add(e);
        }

        public void Clear()
        {
            foreach (var e in active) Despawn(e);
            active.Clear();
        }

        // ------------------------------------------------------------------ simulation

        public void Tick(float dt)
        {
            float time = run.State.time;
            var hero = run.Hero;

            // Compact the active list (killed enemies are removed lazily) and rebuild the hash.
            int w = 0;
            for (int i = 0; i < active.Count; i++)
            {
                var e = active[i];
                if (!e.alive) { Despawn(e); continue; }
                e.index = w;
                active[w++] = e;
            }
            active.RemoveRange(w, active.Count - w);
            hash.Clear();
            foreach (var e in active) hash.Insert(e);

            for (int i = 0; i < active.Count; i++)
            {
                var e = active[i];
                if (!e.alive) continue;
                e.age += dt;
                e.spawnAnim = Mathf.Min(1f, e.spawnAnim + dt * 4f);
                if (e.flash > 0) e.flash -= dt;
                if (e.teleportCooldown > 0) e.teleportCooldown -= dt;

                // Status effects.
                float slowMul = 1f;
                if (e.slowTimer > 0) { e.slowTimer -= dt; slowMul = 1f - e.slowFactor * (e.def.IsBossLike ? 0.4f : 1f); }
                if (e.poisonTimer > 0)
                {
                    e.poisonTimer -= dt;
                    e.poisonTick += dt;
                    if (e.poisonTick >= 0.5f)
                    {
                        e.poisonTick -= 0.5f;
                        Damage(e, e.poisonDps * 0.5f, false, Vector2.zero, 0f, null, quiet: true);
                        if (!e.alive) continue;
                    }
                }

                Vector2 toHero = hero.Position - e.pos;
                float dist = toHero.magnitude;
                Vector2 dir = dist > 1e-4f ? toHero / dist : Vector2.right;

                Vector2 desired = e.def.IsBossLike ? BossBrain(e, dt, dir, dist) : Behave(e, dt, dir, dist);
                if (!e.alive) continue;
                if (e.state == 2 && (e.def.behaviour == EnemyBehaviour.Charger || e.def.IsBossLike) && e.dashDir != Vector2.zero)
                    e.vel = desired; // dashes are instant
                else
                    e.vel = Vector2.Lerp(e.vel, desired * slowMul, 1f - Mathf.Exp(-10f * dt));

                e.pos += (e.vel + e.knock) * dt;
                e.knock *= Mathf.Exp(-9f * dt);

                // Separation (cheap, capped neighbour count).
                int n = hash.Neighbours(e.pos, neighbours);
                Vector2 push = Vector2.zero;
                for (int k = 0; k < n; k++)
                {
                    var o = neighbours[k];
                    if (o == e || !o.alive) continue;
                    Vector2 d = e.pos - o.pos;
                    float rr = e.radius + o.radius;
                    float sq = d.sqrMagnitude;
                    if (sq >= rr * rr || sq < 1e-8f) continue;
                    float m = Mathf.Sqrt(sq);
                    float weight = o.radius / (e.radius + o.radius);
                    push += d / m * (rr - m) * weight;
                }
                e.pos += push * Mathf.Min(1f, 12f * dt) * (e.def.IsBossLike ? 0.15f : 1f);

                run.Map.ResolveCircle(ref e.pos, e.radius);

                // Home-run launched enemies bowl into others.
                if (e.launchTimer > 0)
                {
                    e.launchTimer -= dt;
                    for (int k = 0; k < n; k++)
                    {
                        var o = neighbours[k];
                        if (o == e || !o.alive || o.launchTimer > 0) continue;
                        if ((o.pos - e.pos).sqrMagnitude < (o.radius + e.radius) * (o.radius + e.radius))
                            Damage(o, e.launchDamage, false, (o.pos - e.pos).normalized, 4f, null);
                    }
                }

                // Contact damage.
                float hr = run.Config.heroRadius + e.radius;
                if (!e.submerged && e.spawnAnim >= 1f && toHero.sqrMagnitude < hr * hr)
                    hero.TakeDamage(e.damage, e.def.displayName);

                // Endless map: enemies left far behind wrap back in front of the hero.
                if (!run.Map.Bounded && !e.def.IsBossLike && dist > run.Config.despawnDistance && e.teleportCooldown <= 0)
                {
                    e.pos = run.Director.OffscreenPoint(hero.Position + hero.Velocity * 1.5f);
                    e.teleportCooldown = 3f;
                }

                UpdateVisual(e, dir);
            }
        }

        void UpdateVisual(Enemy e, Vector2 dirToHero)
        {
            e.tf.position = new Vector3(e.pos.x, e.pos.y, 0);
            float wobble = 1f + Mathf.Sin(e.age * 9f + e.phase) * 0.06f;
            float pop = e.spawnAnim < 1f ? Mathf.SmoothStep(0f, 1f, e.spawnAnim) * (1f + Mathf.Sin(e.spawnAnim * Mathf.PI) * 0.25f) : 1f;
            float s = e.baseScale * pop;
            if (e.state == 1 && (e.def.behaviour == EnemyBehaviour.Exploder)) s *= 1f + Mathf.Abs(Mathf.Sin(e.age * 25f)) * 0.2f;
            e.tf.localScale = new Vector3(s * (2f - wobble), s * wobble, 1f);
            bool faceLeft = dirToHero.x < -0.05f;
            e.sr.flipX = faceLeft;
            bool flashing = e.flash > 0 || (e.state == 1 && (e.def.behaviour == EnemyBehaviour.Charger || e.def.behaviour == EnemyBehaviour.Exploder) && Mathf.Repeat(e.age, 0.16f) < 0.08f);
            e.sr.sprite = flashing ? Art.EnemyFlash(e.def) : Art.Enemy(e.def);
            if (e.barFill != null && e.barFill.enabled)
            {
                float width = 1.1f / Mathf.Max(0.01f, s) * 1.2f;
                float y = 0.62f;
                e.barBack.transform.localPosition = new Vector3(0, y, 0);
                e.barBack.transform.localScale = new Vector3(width, 0.1f / s * 1.2f, 1);
                e.barFill.transform.localPosition = new Vector3(-width * (1f - e.HpFraction) * 0.5f, y, 0);
                e.barFill.transform.localScale = new Vector3(width * e.HpFraction, 0.07f / s * 1.2f, 1);
            }
        }

        // ------------------------------------------------------------------ behaviours

        Vector2 Behave(Enemy e, float dt, Vector2 dir, float dist)
        {
            var d = e.def;
            switch (d.behaviour)
            {
                case EnemyBehaviour.Rusher:
                {
                    var side = new Vector2(-dir.y, dir.x) * Mathf.Sin(e.age * 5f + e.phase) * 0.35f;
                    return (dir + side).normalized * e.speed;
                }
                case EnemyBehaviour.Hopper:
                {
                    e.stateTimer += dt;
                    float cycle = Mathf.Repeat(e.stateTimer + e.phase, 0.85f);
                    return cycle < 0.35f ? dir * e.speed * 2.6f : Vector2.zero;
                }
                case EnemyBehaviour.Shooter:
                {
                    Vector2 v;
                    if (dist > d.attackRange * 0.85f) v = dir * e.speed;
                    else if (dist < d.attackRange * 0.5f) v = -dir * e.speed * 0.8f;
                    else v = new Vector2(-dir.y, dir.x) * e.speed * 0.5f * Mathf.Sign(Mathf.Sin(e.phase));
                    e.attackTimer -= dt;
                    if (e.attackTimer <= 0 && dist < d.attackRange * 1.15f)
                    {
                        e.attackTimer = d.attackInterval;
                        FireSpread(e, dir, d.projectileCount, 14f, d.projectileSpeed);
                    }
                    return v;
                }
                case EnemyBehaviour.Charger:
                {
                    if (e.state == 0)
                    {
                        e.attackTimer -= dt;
                        if (e.attackTimer <= 0 && dist < d.attackRange)
                        {
                            e.state = 1; e.stateTimer = 0.7f; e.dashDir = dir;
                            run.Fx.Line(e.pos, e.pos + dir * Mathf.Min(dist + 2f, 9f), new Color(1f, 0.3f, 0.3f, 0.45f), e.radius * 1.4f, 0.7f, false);
                        }
                        return dir * e.speed;
                    }
                    if (e.state == 1)
                    {
                        e.stateTimer -= dt;
                        if (e.stateTimer <= 0) { e.state = 2; e.stateTimer = 0.55f; Sfx.Play(SfxId.Whoosh, 0.4f); }
                        return Vector2.zero;
                    }
                    e.stateTimer -= dt;
                    if (e.stateTimer <= 0) { e.state = 0; e.attackTimer = d.attackInterval; e.dashDir = Vector2.zero; }
                    return e.dashDir * e.speed * 4.5f;
                }
                case EnemyBehaviour.Summoner:
                {
                    Vector2 v = dist > 7f ? dir * e.speed : dist < 5f ? -dir * e.speed : new Vector2(-dir.y, dir.x) * e.speed * 0.4f;
                    e.attackTimer -= dt;
                    if (e.attackTimer <= 0)
                    {
                        e.attackTimer = d.attackInterval;
                        var child = d.child != null ? d.child : run.Biome.normals[0];
                        for (int k = 0; k < Mathf.Max(1, d.childCount); k++)
                        {
                            if (active.Count >= run.Config.enemyCap) break;
                            Spawn(child, e.pos + UnityEngine.Random.insideUnitCircle * 1.2f);
                        }
                        run.Fx.Burst(e.pos, d.secondary, 6, 3f, 0.3f);
                        Sfx.Play(SfxId.Spawn, 0.4f);
                    }
                    return v;
                }
                case EnemyBehaviour.Exploder:
                {
                    if (e.state == 0)
                    {
                        if (dist < 1.6f + e.radius) { e.state = 1; e.stateTimer = 0.65f; }
                        return dir * e.speed;
                    }
                    e.stateTimer -= dt;
                    if (e.stateTimer <= 0)
                    {
                        run.Zones.Blast(e.pos, 2.1f, 0f, e.damage * 1.6f, true, null, 0f, new Color(1f, 0.5f, 0.2f));
                        Kill(e, null, noDrops: false);
                    }
                    return dir * e.speed * 0.2f;
                }
                case EnemyBehaviour.Orbiter:
                {
                    float ring = Mathf.Max(1.4f, 7.5f - e.age * 0.35f);
                    Vector2 tangent = new Vector2(-dir.y, dir.x);
                    float radial = (dist - ring) * 1.2f;
                    return (tangent * 1f + dir * radial).normalized * e.speed;
                }
                case EnemyBehaviour.Ambusher:
                {
                    if (e.submerged)
                    {
                        if (dist < 2.6f)
                        {
                            e.submerged = false;
                            e.sr.color = Color.white;
                            e.spawnAnim = 0.3f;
                            run.Fx.Burst(e.pos, d.primary, 8, 4f, 0.35f);
                            Sfx.Play(SfxId.Pop, 0.5f);
                        }
                        return dir * e.speed * 1.25f;
                    }
                    return dir * e.speed;
                }
                case EnemyBehaviour.Bomber:
                {
                    Vector2 v = dist > 7f ? dir * e.speed : dist < 4.5f ? -dir * e.speed : new Vector2(-dir.y, dir.x) * e.speed * 0.6f;
                    e.attackTimer -= dt;
                    if (e.attackTimer <= 0 && dist < 10f)
                    {
                        e.attackTimer = d.attackInterval;
                        Vector2 at = run.Hero.Position + run.Hero.Velocity * 0.6f;
                        run.Zones.Blast(at, 1.5f, 1.1f, e.damage * 1.3f, true, null, 0f, d.secondary);
                        Sfx.Play(SfxId.EnemyShot, 0.35f);
                    }
                    return v;
                }
                default: // Chaser, Tank, Swarm, Splitter
                    return dir * e.speed;
            }
        }

        void FireSpread(Enemy e, Vector2 dir, int count, float spreadDeg, float speed)
        {
            float start = -(count - 1) * 0.5f * spreadDeg;
            float baseAng = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            for (int k = 0; k < count; k++)
            {
                float a = (baseAng + start + k * spreadDeg) * Mathf.Deg2Rad;
                run.Projectiles.FireHostile(e.pos + dir * e.radius, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * speed, e.damage * 0.8f);
            }
            Sfx.Play(SfxId.EnemyShot, 0.35f);
        }

        // ------------------------------------------------------------------ boss brain

        Vector2 BossBrain(Enemy e, float dt, Vector2 dir, float dist)
        {
            var d = e.def;
            bool enraged = e.HpFraction < 0.5f;
            float gapScale = enraged ? 0.6f : 1f;

            if (e.state == 0)
            {
                e.patternTimer -= dt;
                if (e.patternTimer <= 0 && d.patterns != null && d.patterns.Length > 0)
                {
                    e.currentPattern = d.patterns[e.patternIndex % d.patterns.Length];
                    e.patternIndex++;
                    e.patternStep = 0;
                    e.patternTick = 0;
                    e.state = 1;
                    e.stateTimer = 0f;
                    if (e.currentPattern == BossPattern.Charge)
                    {
                        e.dashDir = dir;
                        e.stateTimer = 0.85f;
                        run.Fx.Line(e.pos, e.pos + dir * 12f, new Color(1f, 0.25f, 0.25f, 0.4f), e.radius * 1.6f, 0.85f, false);
                        Sfx.Play(SfxId.Warning, 0.4f);
                    }
                }
                return dir * e.speed;
            }

            int extra = enraged ? 1 : 0;
            switch (e.currentPattern)
            {
                case BossPattern.RadialBurst:
                    e.patternTick -= dt;
                    if (e.patternTick <= 0)
                    {
                        int n = 12 + extra * 6 + (e.isBoss ? 4 : 0);
                        float off = e.patternStep * 12f;
                        for (int k = 0; k < n; k++)
                        {
                            float a = (off + k * 360f / n) * Mathf.Deg2Rad;
                            run.Projectiles.FireHostile(e.pos, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * d.projectileSpeed, e.damage * 0.7f);
                        }
                        Sfx.Play(SfxId.EnemyShot, 0.5f);
                        e.patternStep++;
                        e.patternTick = 0.45f;
                        if (e.patternStep >= 2 + extra) EndPattern(e, gapScale);
                    }
                    return dir * e.speed * 0.3f;
                case BossPattern.AimedSpread:
                    e.patternTick -= dt;
                    if (e.patternTick <= 0)
                    {
                        FireSpread(e, dir, 5 + extra * 2, 11f, d.projectileSpeed * 1.2f);
                        e.patternStep++;
                        e.patternTick = 0.5f;
                        if (e.patternStep >= 3) EndPattern(e, gapScale);
                    }
                    return dir * e.speed * 0.4f;
                case BossPattern.Charge:
                    if (e.state == 1)
                    {
                        e.stateTimer -= dt;
                        if (e.stateTimer <= 0) { e.state = 2; e.stateTimer = 0.75f; Sfx.Play(SfxId.Whoosh, 0.6f); }
                        return Vector2.zero;
                    }
                    e.stateTimer -= dt;
                    if (e.stateTimer <= 0) { e.dashDir = Vector2.zero; EndPattern(e, gapScale); }
                    return e.dashDir * e.speed * 5f;
                case BossPattern.Summon:
                {
                    var child = d.child != null ? d.child : run.Biome.normals[0];
                    int n = d.childCount + extra * 2;
                    for (int k = 0; k < n; k++)
                    {
                        if (active.Count >= run.Config.enemyCap) break;
                        float a = k * Mathf.PI * 2f / n;
                        Spawn(child, e.pos + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (e.radius + 1f));
                    }
                    run.Fx.Burst(e.pos, d.secondary, 12, 5f, 0.4f);
                    Sfx.Play(SfxId.Spawn, 0.6f);
                    EndPattern(e, gapScale);
                    return Vector2.zero;
                }
                case BossPattern.Slam:
                {
                    int n = 4 + extra * 2;
                    Vector2 hp = run.Hero.Position;
                    run.Zones.Blast(hp + run.Hero.Velocity * 0.5f, 1.8f, 1.1f, e.damage * 1.2f, true, null, 0f, d.secondary);
                    for (int k = 0; k < n; k++)
                    {
                        Vector2 p = hp + UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(2f, 5f);
                        run.Zones.Blast(p, 1.6f, 1.1f + k * 0.12f, e.damage * 1.2f, true, null, 0f, d.secondary);
                    }
                    Sfx.Play(SfxId.Warning, 0.4f);
                    EndPattern(e, gapScale);
                    return Vector2.zero;
                }
                case BossPattern.Spiral:
                    e.patternTimer += dt;
                    e.patternTick -= dt;
                    if (e.patternTick <= 0)
                    {
                        e.patternTick = enraged ? 0.07f : 0.1f;
                        float baseA = e.patternTimer * 200f;
                        for (int arm = 0; arm < 3; arm++)
                        {
                            float a = (baseA + arm * 120f) * Mathf.Deg2Rad;
                            run.Projectiles.FireHostile(e.pos, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * d.projectileSpeed * 0.8f, e.damage * 0.6f);
                        }
                        Sfx.Play(SfxId.EnemyShot, 0.25f, 1.2f, 0.09f);
                    }
                    if (e.patternTimer > 2.6f) { e.patternTimer = 0; EndPattern(e, gapScale); }
                    return Vector2.zero;
                case BossPattern.HazardDrop:
                {
                    var b = run.Biome;
                    for (int k = 0; k < 3 + extra; k++)
                    {
                        Vector2 p = run.Hero.Position + UnityEngine.Random.insideUnitCircle * 4f;
                        run.Zones.HostilePool(p, 1.8f, 7f, e.damage * 0.6f, b.hazardColor.a > 0 ? new Color(b.hazardColor.r, b.hazardColor.g, b.hazardColor.b, 0.7f) : d.secondary, 0.9f);
                    }
                    EndPattern(e, gapScale);
                    return dir * e.speed * 0.5f;
                }
            }
            EndPattern(e, gapScale);
            return dir * e.speed;
        }

        static void EndPattern(Enemy e, float gapScale)
        {
            e.state = 0;
            e.patternTimer = (e.isBoss ? 2.2f : 3f) * gapScale;
        }

        // ------------------------------------------------------------------ damage

        /// <summary>Apply damage. Returns true if this hit killed the enemy.</summary>
        public bool Damage(Enemy e, float amount, bool crit, Vector2 knockDir, float knockback, WeaponInstance source, bool quiet = false)
        {
            if (e == null || !e.alive || e.submerged) return false;
            amount *= 1f - e.armor;
            if (amount <= 0) return false;
            e.hp -= amount;
            e.flash = 0.08f;
            if (knockback > 0 && knockDir != Vector2.zero)
            {
                float k = knockback * (1f - e.knockResist) * (e.def.IsBossLike ? 0.15f : 1f);
                e.knock += knockDir.normalized * k * 2.2f;
            }
            if (source != null) source.damageDealt += amount;
            run.Numbers.Show(e.pos + Vector2.up * e.radius, amount, crit, quiet);
            if (!quiet) Sfx.Play(crit ? SfxId.Crit : SfxId.Hit, crit ? 0.35f : 0.22f, 1f, 0.05f);
            if (e.hp <= 0)
            {
                Kill(e, source);
                return true;
            }
            return false;
        }

        public void ApplySlow(Enemy e, float factor, float duration)
        {
            if (e == null || !e.alive) return;
            e.slowFactor = Mathf.Max(factor, e.slowTimer > 0 ? e.slowFactor : 0f);
            e.slowTimer = Mathf.Max(e.slowTimer, duration);
        }

        public void ApplyPoison(Enemy e, float dps, float duration)
        {
            if (e == null || !e.alive) return;
            e.poisonDps = Mathf.Max(e.poisonDps, dps);
            e.poisonTimer = Mathf.Max(e.poisonTimer, duration);
        }

        public void Launch(Enemy e, Vector2 dir, float force, float damage)
        {
            if (e == null || !e.alive) return;
            e.knock += dir.normalized * force;
            e.launchTimer = 0.6f;
            e.launchDamage = damage;
        }

        public void Kill(Enemy e, WeaponInstance source, bool noDrops = false)
        {
            if (!e.alive) return;
            e.alive = false;
            e.go.SetActive(false);
            var d = e.def;
            run.State.kills++;
            if (source != null) source.kills++;
            run.Fx.Burst(e.pos, d.primary, d.IsBossLike ? 24 : 6, d.IsBossLike ? 7f : 4f, d.IsBossLike ? 0.6f : 0.3f);
            run.Fx.Spawn(ArtId.Puff, e.pos, e.radius * 2.6f, new Color(1, 1, 1, 0.7f), 0.3f, growTo: 1.5f);
            Sfx.Play(d.IsBossLike ? SfxId.Explosion : SfxId.EnemyDie, d.IsBossLike ? 0.8f : 0.25f, UnityEngine.Random.Range(0.9f, 1.2f), 0.03f);

            if (!noDrops)
            {
                int xp = d.xp * (e.elite ? 5 : 1);
                if (d.IsBossLike)
                {
                    for (int k = 0; k < 6; k++) run.Pickups.SpawnGem(e.pos + UnityEngine.Random.insideUnitCircle * 1.5f, Mathf.Max(1, xp / 6));
                    run.Pickups.Spawn(PickupKind.Chest, e.pos, d.tier == EnemyTier.Boss ? 2 : 1);
                    for (int k = 0; k < (d.tier == EnemyTier.Boss ? 12 : 5); k++) run.Pickups.Spawn(PickupKind.Coin, e.pos + UnityEngine.Random.insideUnitCircle * 2f, 1);
                }
                else
                {
                    run.Pickups.SpawnGem(e.pos, xp);
                    float luck = 1f + run.Stats.Luck;
                    if (UnityEngine.Random.value < d.goldChance * luck * (e.elite ? 5f : 1f)) run.Pickups.Spawn(PickupKind.Coin, e.pos + UnityEngine.Random.insideUnitCircle * 0.4f, e.elite ? 5 : 1);
                    if (UnityEngine.Random.value < run.Config.healDropChance * luck) run.Pickups.Spawn(PickupKind.Heal, e.pos, 1);
                    if (UnityEngine.Random.value < run.Config.bombDropChance * luck) run.Pickups.Spawn(PickupKind.Bomb, e.pos, 1);
                    if (e.elite) run.Pickups.Spawn(PickupKind.Chest, e.pos, 1);
                }
            }

            if (d.behaviour == EnemyBehaviour.Splitter && d.child != null)
                for (int k = 0; k < d.childCount; k++)
                    Spawn(d.child, e.pos + UnityEngine.Random.insideUnitCircle * e.radius, e.elite);

            Killed?.Invoke(e);
        }

        // ------------------------------------------------------------------ queries

        public Enemy Nearest(Vector2 p, float maxRange)
        {
            Enemy best = null;
            float bd = maxRange * maxRange;
            // Grow the search radius so we do not scan the whole map for a close target.
            for (float r = 4f; r <= maxRange * 2f; r *= 2f)
            {
                float qr = Mathf.Min(r, maxRange);
                hash.Query(p, qr, queryBuf, false);
                foreach (var e in queryBuf)
                {
                    if (!e.Targetable) continue;
                    float d = (e.pos - p).sqrMagnitude;
                    if (d < bd) { bd = d; best = e; }
                }
                if (best != null || qr >= maxRange) break;
            }
            return best;
        }

        public Enemy NearestExcluding(Vector2 p, float maxRange, List<int> excludeSerials)
        {
            Enemy best = null;
            float bd = maxRange * maxRange;
            hash.Query(p, maxRange, queryBuf, false);
            foreach (var e in queryBuf)
            {
                if (!e.Targetable || excludeSerials.Contains(e.serial)) continue;
                float d = (e.pos - p).sqrMagnitude;
                if (d < bd) { bd = d; best = e; }
            }
            return best;
        }

        public Enemy RandomInRange(Vector2 p, float range)
        {
            hash.Query(p, range, queryBuf, false);
            int n = 0;
            Enemy pick = null;
            foreach (var e in queryBuf)
            {
                if (!e.Targetable) continue;
                n++;
                if (UnityEngine.Random.Range(0, n) == 0) pick = e; // reservoir sampling
            }
            return pick;
        }

        public Enemy Strongest(Vector2 p, float range)
        {
            hash.Query(p, range, queryBuf, false);
            Enemy best = null;
            foreach (var e in queryBuf)
                if (e.Targetable && (best == null || e.hp > best.hp)) best = e;
            return best;
        }

        /// <summary>Enemies overlapping a circle (results are reused: copy if you keep them).</summary>
        public List<Enemy> InRadius(Vector2 p, float r, List<Enemy> results)
        {
            hash.Query(p, r, results, true);
            for (int i = results.Count - 1; i >= 0; i--) if (!results[i].Targetable) results.RemoveAt(i);
            return results;
        }

        /// <summary>Enemies overlapping a capsule from a to b with the given half width.</summary>
        public void AlongSegment(Vector2 a, Vector2 b, float halfWidth, List<Enemy> results)
        {
            results.Clear();
            Vector2 ab = b - a;
            float len = ab.magnitude;
            if (len < 1e-4f) { InRadius(a, halfWidth, results); return; }
            Vector2 dir = ab / len;
            float step = hash.CellSize;
            var tmp = queryBuf;
            for (float t = 0; t <= len + step * 0.5f; t += step)
            {
                Vector2 c = a + dir * Mathf.Min(t, len);
                hash.Query(c, step * 0.75f + halfWidth, tmp, true);
                foreach (var e in tmp)
                {
                    if (!e.Targetable || results.Contains(e)) continue;
                    float proj = Mathf.Clamp(Vector2.Dot(e.pos - a, dir), 0, len);
                    if ((a + dir * proj - e.pos).sqrMagnitude <= (halfWidth + e.radius) * (halfWidth + e.radius)) results.Add(e);
                }
            }
        }

        public Enemy FirstBoss()
        {
            foreach (var e in active) if (e.alive && e.isBoss) return e;
            return null;
        }
    }
}
