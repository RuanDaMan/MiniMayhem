using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiniMayhem
{
    public enum ProjectileKind { Straight, Boomerang, Lob, Hostile }

    public class Projectile
    {
        public bool alive;
        public ProjectileKind kind;
        public Vector2 pos, vel, origin, target;
        public float speed, damage, radius, life, maxLife, homing, explodeRadius, knockback, spin, rot, size, arcHeight;
        public float slow, poison;
        public int pierce, bounces;
        public bool returning;
        public int hitGroup = -1;
        public float hitCooldown = 0.5f;
        public WeaponInstance source;
        public readonly List<int> hitSerials = new(8);
        public Action<Projectile> onLand;
        public Color color = Color.white;
        public GameObject go;
        public Transform tf;
        public SpriteRenderer sr;
        public bool faceVelocity;
    }

    /// <summary>Pooled projectiles for both sides: straight/homing shots, boomerangs, lobbed bombs and enemy bullets.</summary>
    public class ProjectileManager
    {
        const int Cap = 1600;
        readonly RunController run;
        readonly Transform root;
        readonly List<Projectile> active = new();
        readonly Stack<Projectile> pool = new();
        readonly List<Enemy> buf = new();

        public int Count => active.Count;
        public IReadOnlyList<Projectile> Active => active;

        public ProjectileManager(RunController run, Transform parent)
        {
            this.run = run;
            root = new GameObject("Projectiles").transform;
            root.SetParent(parent, false);
        }

        Projectile Rent()
        {
            var p = pool.Count > 0 ? pool.Pop() : null;
            if (p == null)
            {
                p = new Projectile();
                p.go = new GameObject("Projectile");
                p.tf = p.go.transform;
                p.tf.SetParent(root, false);
                p.sr = p.go.AddComponent<SpriteRenderer>();
                p.sr.sharedMaterial = Art.SpriteMaterial;
                p.sr.sortingOrder = 10;
            }
            p.alive = true;
            p.hitSerials.Clear();
            p.returning = false;
            p.onLand = null;
            p.source = null;
            p.hitGroup = -1;
            p.homing = p.explodeRadius = p.knockback = p.spin = p.rot = p.slow = p.poison = p.arcHeight = 0;
            p.pierce = p.bounces = 0;
            p.faceVelocity = false;
            p.go.SetActive(true);
            active.Add(p);
            return p;
        }

        void Release(Projectile p)
        {
            p.alive = false;
            p.go.SetActive(false);
            pool.Push(p);
        }

        public Projectile Fire(WeaponInstance source, ArtId art, Vector2 pos, Vector2 dir, float speed, float damage, float size, float life,
            int pierce = 0, ProjectileKind kind = ProjectileKind.Straight)
        {
            if (active.Count >= Cap) return null;
            var p = Rent();
            p.kind = kind;
            p.source = source;
            p.pos = p.origin = pos;
            p.vel = dir.normalized * speed;
            p.speed = speed;
            p.damage = damage;
            p.size = size;
            p.radius = size * 0.45f;
            p.life = p.maxLife = life;
            p.pierce = pierce;
            p.sr.sprite = Art.Get(art);
            p.color = source != null ? source.VisualColor : Color.white;
            p.sr.color = p.color;
            p.sr.sortingOrder = 10;
            p.tf.position = pos;
            p.tf.localScale = Vector3.one * size;
            p.tf.rotation = Quaternion.identity;
            return p;
        }

        public Projectile Lob(WeaponInstance source, ArtId art, Vector2 from, Vector2 to, float flightTime, float size, Action<Projectile> onLand)
        {
            var p = Fire(source, art, from, to - from, 0f, 0f, size, flightTime, 0, ProjectileKind.Lob);
            if (p == null) return null;
            p.target = to;
            p.arcHeight = Mathf.Clamp((to - from).magnitude * 0.35f, 1f, 3f);
            p.onLand = onLand;
            p.spin = 360f;
            return p;
        }

        public void FireHostile(Vector2 pos, Vector2 vel, float damage)
        {
            if (active.Count >= Cap) return;
            var p = Rent();
            p.kind = ProjectileKind.Hostile;
            p.pos = pos;
            p.vel = vel;
            p.speed = vel.magnitude;
            p.damage = damage;
            p.size = 0.55f;
            p.radius = 0.2f;
            p.life = p.maxLife = 7f;
            bool cb = GameServices.Get<MetaService>()?.Data.settings.colorblind ?? false;
            p.sr.sprite = Art.Get(cb ? ArtId.EnemyBulletAlt : ArtId.EnemyBullet);
            p.color = Color.white;
            p.sr.color = Color.white;
            p.sr.sortingOrder = 12;
            p.tf.position = pos;
            p.tf.localScale = Vector3.one * p.size;
            p.spin = cb ? 360f : 0f;
        }

        public void ClearHostile()
        {
            foreach (var p in active)
                if (p.alive && p.kind == ProjectileKind.Hostile) { run.Fx.Spawn(ArtId.Puff, p.pos, 0.5f, new Color(1, 0.7f, 0.8f, 0.6f), 0.2f); p.alive = false; }
        }

        public void Clear()
        {
            foreach (var p in active) Release(p);
            active.Clear();
        }

        public void Tick(float dt)
        {
            var hero = run.Hero;
            for (int i = 0; i < active.Count; i++)
            {
                var p = active[i];
                if (!p.alive) continue;
                p.life -= dt;
                switch (p.kind)
                {
                    case ProjectileKind.Hostile:
                    {
                        p.pos += p.vel * dt;
                        float r = p.radius + run.Config.heroRadius * 0.8f;
                        if ((p.pos - hero.Position).sqrMagnitude < r * r)
                        {
                            hero.TakeDamage(p.damage, "a projectile");
                            p.alive = false;
                        }
                        if (run.Map.Bounded && run.Map.Blocks(p.pos)) p.alive = false;
                        break;
                    }
                    case ProjectileKind.Lob:
                    {
                        float t = 1f - Mathf.Clamp01(p.life / p.maxLife);
                        p.pos = Vector2.Lerp(p.origin, p.target, t);
                        if (p.life <= 0) { p.onLand?.Invoke(p); p.alive = false; }
                        break;
                    }
                    case ProjectileKind.Boomerang:
                        TickBoomerang(p, dt);
                        break;
                    default:
                        TickStraight(p, dt);
                        break;
                }
                if (p.life <= 0 && p.kind != ProjectileKind.Lob) p.alive = false;
                if (!p.alive) continue;

                // Visual
                float h = 0f;
                if (p.kind == ProjectileKind.Lob)
                {
                    float t = 1f - Mathf.Clamp01(p.life / p.maxLife);
                    h = Mathf.Sin(t * Mathf.PI) * p.arcHeight;
                }
                p.tf.position = new Vector3(p.pos.x, p.pos.y + h, 0);
                if (p.spin != 0) p.rot += p.spin * dt;
                else if (p.faceVelocity && p.vel.sqrMagnitude > 1e-4f) p.rot = Mathf.Atan2(p.vel.y, p.vel.x) * Mathf.Rad2Deg;
                p.tf.rotation = Quaternion.Euler(0, 0, p.rot);
                float fade = Mathf.Clamp01(p.life / 0.15f);
                if (p.kind != ProjectileKind.Lob) p.tf.localScale = Vector3.one * p.size * Mathf.Lerp(0.6f, 1f, fade);
            }

            // Compact
            int w = 0;
            for (int i = 0; i < active.Count; i++)
            {
                var p = active[i];
                if (!p.alive) { Release(p); continue; }
                active[w++] = p;
            }
            active.RemoveRange(w, active.Count - w);
        }

        void TickStraight(Projectile p, float dt)
        {
            if (p.homing > 0)
            {
                var target = run.Enemies.Nearest(p.pos, 8f);
                if (target != null)
                {
                    Vector2 want = (target.pos - p.pos).normalized * p.speed;
                    p.vel = Vector3.RotateTowards(p.vel, want, p.homing * dt, 0f);
                    p.vel = p.vel.normalized * p.speed;
                }
            }
            p.pos += p.vel * dt;
            if (run.Map.Bounded && run.Map.Blocks(p.pos)) { Impact(p); p.alive = false; return; }
            HitEnemies(p);
        }

        void HitEnemies(Projectile p)
        {
            run.Enemies.InRadius(p.pos, p.radius, buf);
            foreach (var e in buf)
            {
                if (!p.alive) return;
                if (p.hitSerials.Contains(e.serial)) continue;
                if (p.hitGroup >= 0 && !e.CanBeHitBy(p.hitGroup, run.State.time)) continue;
                p.hitSerials.Add(e.serial);
                if (p.hitGroup >= 0) e.MarkHit(p.hitGroup, run.State.time, p.hitCooldown);
                Vector2 dir = p.vel.sqrMagnitude > 0 ? p.vel.normalized : (e.pos - p.pos).normalized;
                p.source?.HitEnemy(e, p.damage, dir, p.knockback, p.slow, p.poison);
                if (p.source == null) run.Enemies.Damage(e, p.damage, false, dir, p.knockback, null);
                if (p.explodeRadius > 0) Impact(p);

                if (p.kind == ProjectileKind.Boomerang)
                {
                    if (p.bounces > 0 && !p.returning)
                    {
                        p.bounces--;
                        var next = run.Enemies.NearestExcluding(p.pos, 7f, p.hitSerials);
                        if (next != null) { p.vel = (next.pos - p.pos).normalized * p.speed; p.life = Mathf.Max(p.life, p.maxLife * 0.5f); }
                    }
                    continue;
                }

                if (p.pierce > 0) { p.pierce--; continue; }
                if (p.bounces > 0)
                {
                    p.bounces--;
                    var next = run.Enemies.NearestExcluding(p.pos, 7f, p.hitSerials);
                    if (next != null) { p.vel = (next.pos - p.pos).normalized * p.speed; continue; }
                }
                p.alive = false;
                return;
            }
        }

        void Impact(Projectile p)
        {
            if (p.explodeRadius <= 0) return;
            run.Zones.Blast(p.pos, p.explodeRadius, 0f, p.damage * 0.7f, false, p.source, p.knockback, p.color);
        }

        void TickBoomerang(Projectile p, float dt)
        {
            var hero = run.Hero;
            float outTime = p.maxLife * 0.5f;
            if (!p.returning && p.life <= p.maxLife - outTime)
            {
                p.returning = true;
                p.hitSerials.Clear();
            }
            if (p.returning)
            {
                Vector2 to = hero.Position - p.pos;
                float d = to.magnitude;
                p.vel = Vector2.Lerp(p.vel, to.normalized * p.speed * 1.3f, 1f - Mathf.Exp(-6f * dt));
                if (d < 0.6f) { p.alive = false; return; }
                p.life = Mathf.Max(p.life, 0.1f); // keep flying until it is back
            }
            else
            {
                // Slow down towards the apex.
                float t = 1f - (p.life - (p.maxLife - outTime)) / outTime;
                p.vel = p.vel.normalized * p.speed * Mathf.Lerp(1.2f, 0.3f, t);
            }
            p.pos += p.vel * dt;
            HitEnemies(p);
        }
    }
}
