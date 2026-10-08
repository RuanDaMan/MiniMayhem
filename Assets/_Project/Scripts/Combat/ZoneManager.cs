using System.Collections.Generic;
using UnityEngine;

namespace MiniMayhem
{
    /// <summary>
    /// Ground effects: friendly puddles/clouds that tick damage and slow enemies, hostile pools that hurt the hero,
    /// and (optionally delayed, telegraphed) explosions for both sides.
    /// </summary>
    public class ZoneManager
    {
        class Zone
        {
            public bool alive, hostile;
            public Vector2 pos;
            public float radius, life, maxLife, dps, slow, tick, poison;
            public WeaponInstance source;
            public SpriteRenderer sr;
        }

        class PendingBlast
        {
            public bool alive, hostile;
            public Vector2 pos;
            public float radius, delay, damage, knockback;
            public int sub;
            public WeaponInstance source;
            public Color color;
            public SpriteRenderer telegraph;
        }

        readonly RunController run;
        readonly Transform root;
        readonly List<Zone> zones = new();
        readonly List<PendingBlast> blasts = new();
        readonly Stack<SpriteRenderer> srPool = new();
        readonly List<Enemy> buf = new();

        public int ZoneCount => zones.Count;

        public ZoneManager(RunController run, Transform parent)
        {
            this.run = run;
            root = new GameObject("Zones").transform;
            root.SetParent(parent, false);
        }

        SpriteRenderer RentSr(ArtId art, Color c, int order)
        {
            SpriteRenderer sr = srPool.Count > 0 ? srPool.Pop() : null;
            if (sr == null)
            {
                var go = new GameObject("Zone");
                go.transform.SetParent(root, false);
                sr = go.AddComponent<SpriteRenderer>();
                sr.sharedMaterial = Art.SpriteMaterial;
            }
            sr.gameObject.SetActive(true);
            sr.sprite = Art.Get(art);
            sr.color = c;
            sr.sortingOrder = order;
            return sr;
        }

        void ReturnSr(SpriteRenderer sr)
        {
            if (sr == null) return;
            sr.gameObject.SetActive(false);
            srPool.Push(sr);
        }

        /// <summary>Friendly ground zone (puddle, stink cloud) that damages + slows enemies inside.</summary>
        public void Puddle(Vector2 pos, float radius, float duration, float dps, float slow, WeaponInstance source, ArtId art, Color color, float poison = 0f)
        {
            if (zones.Count > 120) return;
            var z = new Zone { alive = true, pos = pos, radius = radius, life = duration, maxLife = duration, dps = dps, slow = slow, source = source, poison = poison };
            z.sr = RentSr(art, color, -20);
            z.sr.transform.position = pos;
            z.sr.transform.localScale = Vector3.one * radius * 2.1f;
            zones.Add(z);
        }

        /// <summary>Hostile pool (boss hazard drops) that hurts the hero while standing in it.</summary>
        public void HostilePool(Vector2 pos, float radius, float duration, float dps, Color color, float warmup = 0.8f)
        {
            var z = new Zone { alive = true, hostile = true, pos = pos, radius = radius, life = duration + warmup, maxLife = duration + warmup, dps = dps, tick = -warmup };
            z.sr = RentSr(ArtId.HazardPatch, color, -19);
            z.sr.transform.position = pos;
            z.sr.transform.localScale = Vector3.one * radius * 2.2f;
            zones.Add(z);
        }

        /// <summary>Explosion after a delay (with a red telegraph for hostile blasts). sub = follow-up blasts (fireworks).</summary>
        public void Blast(Vector2 pos, float radius, float delay, float damage, bool hostile, WeaponInstance source, float knockback, Color color, int sub = 0)
        {
            var b = new PendingBlast { alive = true, pos = pos, radius = radius, delay = delay, damage = damage, hostile = hostile, source = source, knockback = knockback, color = color, sub = sub };
            if (delay > 0.05f && hostile)
            {
                b.telegraph = RentSr(ArtId.Telegraph, Color.white, -15);
                b.telegraph.transform.position = pos;
                b.telegraph.transform.localScale = Vector3.one * radius * 2f;
            }
            if (delay <= 0f) Detonate(b);
            else blasts.Add(b);
        }

        void Detonate(PendingBlast b)
        {
            b.alive = false;
            ReturnSr(b.telegraph);
            b.telegraph = null;
            run.Fx.Spawn(ArtId.Circle, b.pos, b.radius * 2f, new Color(b.color.r, b.color.g, b.color.b, 0.55f), 0.22f, growTo: 1.2f);
            run.Fx.Spawn(ArtId.Ring, b.pos, b.radius * 2f, Color.white, 0.25f, growTo: 1.35f);
            run.Fx.Burst(b.pos, b.color, 6, 5f, 0.3f);
            Sfx.Play(SfxId.Explosion, b.hostile ? 0.35f : 0.3f, Random.Range(0.9f, 1.25f), 0.06f);
            if (b.hostile)
            {
                float r = b.radius + run.Config.heroRadius;
                if ((run.Hero.Position - b.pos).sqrMagnitude < r * r) run.Hero.TakeDamage(b.damage, "an explosion");
            }
            else
            {
                run.Enemies.InRadius(b.pos, b.radius, buf);
                for (int i = 0; i < buf.Count; i++)
                {
                    var e = buf[i];
                    Vector2 dir = e.pos - b.pos;
                    if (b.source != null) b.source.HitEnemy(e, b.damage, dir, b.knockback);
                    else run.Enemies.Damage(e, b.damage, false, dir, b.knockback, null);
                }
                run.Shake(0.08f);
            }
            for (int k = 0; k < b.sub; k++)
            {
                float a = (k / (float)b.sub + Random.value * 0.2f) * Mathf.PI * 2f;
                Vector2 p = b.pos + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * b.radius * 1.3f;
                blasts.Add(new PendingBlast { alive = true, pos = p, radius = b.radius * 0.7f, delay = 0.18f + k * 0.07f, damage = b.damage * 0.6f, source = b.source, knockback = b.knockback * 0.5f, color = k % 2 == 0 ? new Color(1f, 0.4f, 0.6f) : new Color(0.4f, 0.8f, 1f) });
            }
        }

        public void Tick(float dt)
        {
            var hero = run.Hero;
            for (int i = 0; i < zones.Count; i++)
            {
                var z = zones[i];
                z.life -= dt;
                float alpha = Mathf.Clamp01(z.life / 0.4f);
                if (z.hostile)
                {
                    z.tick += dt;
                    if (z.tick < 0)
                    {
                        // warm-up: blink so it reads as a warning
                        var c = z.sr.color; c.a = 0.25f + Mathf.PingPong(Time.time * 4f, 0.35f); z.sr.color = c;
                    }
                    else
                    {
                        var c = z.sr.color; c.a = 0.7f * alpha; z.sr.color = c;
                        float r = z.radius + run.Config.heroRadius * 0.5f;
                        if ((hero.Position - z.pos).sqrMagnitude < r * r && z.tick >= 0.5f)
                        {
                            z.tick = 0;
                            hero.TakeDamage(z.dps * 0.5f, "a hazard");
                        }
                    }
                }
                else
                {
                    var c = z.sr.color; c.a = Mathf.Min(c.a, alpha * 0.9f + 0.05f); z.sr.color = c;
                    z.tick -= dt;
                    if (z.tick <= 0)
                    {
                        z.tick = 0.5f;
                        run.Enemies.InRadius(z.pos, z.radius, buf);
                        foreach (var e in buf)
                        {
                            if (z.slow > 0) run.Enemies.ApplySlow(e, z.slow, 0.6f);
                            if (z.dps > 0)
                            {
                                if (z.source != null) z.source.HitEnemy(e, z.dps * 0.5f, Vector2.zero, 0f, 0f, z.poison, quiet: true);
                                else run.Enemies.Damage(e, z.dps * 0.5f, false, Vector2.zero, 0f, null, quiet: true);
                            }
                        }
                    }
                }
                if (z.life <= 0) { z.alive = false; ReturnSr(z.sr); }
            }
            zones.RemoveAll(z => !z.alive);

            for (int i = 0; i < blasts.Count; i++)
            {
                var b = blasts[i];
                if (!b.alive) continue;
                b.delay -= dt;
                if (b.telegraph != null)
                {
                    float pulse = 1f + Mathf.Sin(Time.time * 18f) * 0.04f;
                    b.telegraph.transform.localScale = Vector3.one * b.radius * 2f * pulse;
                }
                if (b.delay <= 0) Detonate(b);
            }
            blasts.RemoveAll(b => !b.alive);
        }

        public void Clear()
        {
            foreach (var z in zones) ReturnSr(z.sr);
            foreach (var b in blasts) ReturnSr(b.telegraph);
            zones.Clear();
            blasts.Clear();
        }

        public void ClearHostile()
        {
            foreach (var z in zones) if (z.hostile) { z.alive = false; ReturnSr(z.sr); }
            zones.RemoveAll(z => !z.alive);
            foreach (var b in blasts) if (b.hostile) { b.alive = false; ReturnSr(b.telegraph); }
            blasts.RemoveAll(b => !b.alive);
        }
    }
}
