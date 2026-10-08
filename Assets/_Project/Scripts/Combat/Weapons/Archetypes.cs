using System.Collections.Generic;
using UnityEngine;

namespace MiniMayhem
{
    /// <summary>Pea Shooter, Star Wand, Pea Cannon, Comet Wand, Star Blaster: straight or homing shots.</summary>
    public class ProjectileWeapon : WeaponInstance
    {
        protected override bool Fire()
        {
            if (!AimDirection(Range, out var dir, out _)) return false;
            int n = Amount;
            float size = 0.5f * VisualScale * Mathf.Sqrt(Area);
            float life = Range / Mathf.Max(1f, Speed) * 1.35f + 0.25f;
            for (int k = 0; k < n; k++)
            {
                var d = Rotate(dir, (k - (n - 1) * 0.5f) * B.spread);
                var p = run.Projectiles.Fire(this, def.projectileArt, H.Position + d * 0.45f, d, Speed, Damage, size, life, L.pierce);
                if (p == null) break;
                p.homing = B.homing;
                p.explodeRadius = B.explodeRadius * Area;
                p.knockback = L.knockback;
                p.bounces = B.bounces;
                p.spin = def.projectileArt == ArtId.Star ? 540f : 0f;
                p.faceVelocity = def.projectileArt == ArtId.Comet;
                p.slow = B.slow;
            }
            Sfx.Play(SfxId.Shoot, 0.22f, def.form == WeaponForm.Base ? 1.1f : 0.8f);
            return true;
        }
    }

    /// <summary>Boomerang / Buzzsaw: thrown out and back, hits on both legs; the buzzsaw bounces between enemies.</summary>
    public class BoomerangWeapon : WeaponInstance
    {
        protected override bool Fire()
        {
            if (!AimDirection(Range, out var dir, out _)) return false;
            int n = Amount;
            float size = 0.75f * VisualScale * Mathf.Sqrt(Area);
            for (int k = 0; k < n; k++)
            {
                var d = Rotate(dir, (k - (n - 1) * 0.5f) * B.spread);
                var p = run.Projectiles.Fire(this, def.projectileArt, H.Position, d, Speed, Damage, size, Duration, 999, ProjectileKind.Boomerang);
                if (p == null) break;
                p.spin = 900f;
                p.knockback = L.knockback;
                p.bounces = B.bounces;
            }
            Sfx.Play(SfxId.Whoosh, 0.3f, 1.3f);
            return true;
        }
    }

    /// <summary>Frying Pan, Baseball Bat and their upgrades: an arc swing in front (Amount adds swings around).</summary>
    public class MeleeWeapon : WeaponInstance
    {
        float Radius => 1.85f * Area * (def.form == WeaponForm.Base ? 1f + 0.05f * (level - 1) : 1f);

        protected override bool Fire()
        {
            float r = Radius;
            var target = run.Enemies.Nearest(H.Position, r + 1.5f);
            if (target == null) return false;
            Vector2 dir = (target.pos - H.Position).normalized;
            int n = Amount;
            float arc = B.arcDegrees;
            run.Enemies.InRadius(H.Position, r, buf);
            var hits = new List<Enemy>(buf);
            for (int k = 0; k < n; k++)
            {
                Vector2 d = Rotate(dir, k * 360f / n);
                if (arc >= 359f) run.Fx.Spawn(ArtId.Ring, H.Position, r * 2f, VisualColor, 0.25f, growTo: 1.15f);
                else run.Fx.Spawn(ArtId.Swoosh, H.Position + d * 0.1f, r * 2.1f, VisualColor, 0.2f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg, growTo: 1.08f, order: 15);
                foreach (var e in hits)
                {
                    if (!e.alive) continue;
                    Vector2 to = e.pos - H.Position;
                    if (arc < 359f && Vector2.Angle(d, to) > arc * 0.5f + 8f) continue;
                    HitEnemy(e, Damage, to.normalized, L.knockback);
                }
            }
            if (B.shockwave)
            {
                float sr = B.explodeRadius * Area;
                run.Zones.Blast(H.Position + dir * r * 0.5f, sr, 0.12f, Damage * 0.5f, false, this, L.knockback * 0.6f, VisualColor);
                run.Fx.Spawn(ArtId.Ring, H.Position + dir * r * 0.5f, sr * 0.5f, VisualColor, 0.35f, growTo: 2f);
            }
            Sfx.Play(SfxId.Swing, 0.35f, def.archetype == WeaponArchetype.MeleeArc && arc > 300f ? 0.7f : 1f);
            run.Shake(0.04f);
            return true;
        }
    }

    /// <summary>Duck Orbit / Golden Duck Parade: orbiters circling the hero (the parade pulses outward).</summary>
    public class OrbitWeapon : WeaponInstance
    {
        readonly List<SpriteRenderer> orbiters = new();
        float angle, pulse;

        public float OrbitRadius => 1.7f * Area * (1f + Mathf.Sin(Mathf.Clamp01(pulse) * Mathf.PI) * 0.8f);

        protected override void Update(float dt)
        {
            int n = Amount;
            while (orbiters.Count < n) orbiters.Add(MakeSprite("Orbiter", def.projectileArt, 9));
            while (orbiters.Count > n) { Object.Destroy(orbiters[^1].gameObject); orbiters.RemoveAt(orbiters.Count - 1); }
            angle += Speed * dt;
            if (pulse > 0) pulse -= dt / 0.7f;
            float r = OrbitRadius;
            float size = 0.7f * VisualScale * Mathf.Sqrt(Area);
            float t = run.State.time;
            for (int i = 0; i < n; i++)
            {
                float a = angle + i * Mathf.PI * 2f / n;
                Vector2 p = H.Position + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                var sr = orbiters[i];
                sr.transform.position = p;
                sr.transform.localScale = new Vector3(Mathf.Cos(a) > 0 ? -size : size, size, 1);
                sr.color = def.form == WeaponForm.Base ? Color.Lerp(Color.white, VisualColor, 0.3f) : Color.white;
                run.Enemies.InRadius(p, size * 0.45f, buf);
                foreach (var e in buf)
                {
                    if (!e.CanBeHitBy(hitGroup, t)) continue;
                    e.MarkHit(hitGroup, t, B.hitCooldown);
                    HitEnemy(e, Damage, (e.pos - H.Position).normalized, L.knockback);
                    Sfx.Play(SfxId.Quack, 0.18f, 1f, 0.25f);
                }
            }
        }

        protected override bool Fire()
        {
            if (B.pulses) { pulse = 1f; run.Fx.Spawn(ArtId.Ring, H.Position, 3f, VisualColor, 0.5f, growTo: 2.2f); }
            return true;
        }

        public override void Dispose()
        {
            foreach (var o in orbiters) if (o != null) Object.Destroy(o.gameObject);
            orbiters.Clear();
        }
    }

    /// <summary>Zap Rod / Thunder Rod: chain lightning (L.pierce = extra chains). Thunder Rod also calls storm strikes.</summary>
    public class ChainWeapon : WeaponInstance
    {
        readonly List<int> chained = new();
        float stormTimer = 1.5f;

        protected override void Update(float dt)
        {
            if (!B.stormStrikes) return;
            stormTimer -= dt;
            if (stormTimer > 0) return;
            stormTimer = 1.5f / S.AttackSpeedMult;
            for (int k = 0; k < 3; k++)
            {
                var e = run.Enemies.RandomInRange(H.Position, 11f);
                if (e == null) break;
                run.Fx.Line(e.pos + new Vector2(Random.Range(-1f, 1f), 7f), e.pos, new Color(0.75f, 0.85f, 1f), 0.35f, 0.18f, true);
                run.Zones.Blast(e.pos, 1.4f * Area, 0f, Damage * 1.2f, false, this, 1f, new Color(0.6f, 0.75f, 1f));
            }
            Sfx.Play(SfxId.Zap, 0.4f, 0.7f);
        }

        protected override bool Fire()
        {
            bool any = false;
            for (int k = 0; k < Amount; k++)
            {
                var first = PickTarget(Range);
                if (first == null) break;
                any = true;
                chained.Clear();
                Vector2 from = H.Position;
                var cur = first;
                int links = 1 + B.chains + L.pierce;
                for (int c = 0; c < links && cur != null; c++)
                {
                    chained.Add(cur.serial);
                    run.Fx.Line(from, cur.pos, VisualColor, 0.22f * VisualScale, 0.16f, true);
                    Vector2 hitPos = cur.pos;
                    HitEnemy(cur, Damage, (cur.pos - from).normalized, L.knockback);
                    from = hitPos;
                    cur = run.Enemies.NearestExcluding(hitPos, 4.5f * Mathf.Sqrt(Area), chained);
                }
            }
            if (any) Sfx.Play(SfxId.Zap, 0.3f);
            return any;
        }
    }

    /// <summary>Water Balloon / Flood Bomb: lobbed, splashes, leaves a slowing puddle.</summary>
    public class LobWeapon : WeaponInstance
    {
        protected override bool Fire()
        {
            bool any = false;
            for (int k = 0; k < Amount; k++)
            {
                var t = PickTarget(Range);
                if (t == null) break;
                any = true;
                Vector2 to = t.pos + t.vel * 0.4f + Random.insideUnitCircle * 0.6f;
                run.Projectiles.Lob(this, def.projectileArt, H.Position, to, 0.6f, 0.65f * VisualScale, p => Splash(p.pos));
            }
            if (any) Sfx.Play(SfxId.Whoosh, 0.2f, 1.6f);
            return any;
        }

        void Splash(Vector2 pos)
        {
            float r = 1.3f * Area;
            run.Enemies.InRadius(pos, r, buf);
            for (int i = 0; i < buf.Count; i++) HitEnemy(buf[i], Damage, (buf[i].pos - pos).normalized, L.knockback, B.slow);
            run.Zones.Puddle(pos, r * 1.05f, Duration, Damage * 0.35f, B.slow, this, ArtId.Puddle, new Color(VisualColor.r, VisualColor.g, VisualColor.b, 0.6f));
            run.Fx.Burst(pos, new Color(0.6f, 0.85f, 1f), 8, 4f, 0.3f);
            Sfx.Play(SfxId.Splash, 0.3f);
        }
    }

    /// <summary>Stinky Socks / Toxic Laundry Cloud / Bubble Bath: damage aura around the hero (Fire = one tick).</summary>
    public class AuraWeapon : WeaponInstance
    {
        SpriteRenderer cloud;
        readonly List<SpriteRenderer> orbiters = new();
        float pulseTimer = 2f, spin, orbitAngle;

        float Radius => 1.6f * Area * (def.form == WeaponForm.Base ? 1f + 0.04f * (level - 1) : 1f);

        protected override void Init()
        {
            cloud = MakeSprite("Aura", def.projectileArt, -12);
        }

        protected override void Update(float dt)
        {
            float r = Radius;
            spin += dt * 20f;
            float wob = 1f + Mathf.Sin(run.State.time * 3f) * 0.04f;
            cloud.transform.position = H.Position;
            cloud.transform.localScale = Vector3.one * r * 2.3f * wob;
            cloud.transform.rotation = Quaternion.Euler(0, 0, spin);
            var c = VisualColor; c.a = def.projectileArt == ArtId.Bubble ? 0.75f : 0.6f;
            cloud.color = c;

            if (B.pulses)
            {
                pulseTimer -= dt;
                if (pulseTimer <= 0)
                {
                    pulseTimer = 2f / S.AttackSpeedMult;
                    run.Enemies.InRadius(H.Position, r * 1.2f, buf);
                    for (int i = 0; i < buf.Count; i++) HitEnemy(buf[i], Damage * 3f, (buf[i].pos - H.Position).normalized, 3f, B.slow, B.poisonDps);
                    run.Fx.Spawn(ArtId.Ring, H.Position, r * 2f, VisualColor, 0.35f, growTo: 1.3f);
                    Sfx.Play(SfxId.Stink, 0.4f);
                }
            }

            if (B.orbiters > 0)
            {
                int n = B.orbiters + S.Amount;
                while (orbiters.Count < n) orbiters.Add(MakeSprite("BathDuck", ArtId.Duck, 9));
                while (orbiters.Count > n) { Object.Destroy(orbiters[^1].gameObject); orbiters.RemoveAt(orbiters.Count - 1); }
                orbitAngle += Speed * dt;
                float t = run.State.time;
                for (int i = 0; i < n; i++)
                {
                    float a = orbitAngle + i * Mathf.PI * 2f / n;
                    Vector2 p = H.Position + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r * 0.85f;
                    orbiters[i].transform.position = p;
                    orbiters[i].transform.localScale = new Vector3(Mathf.Cos(a) > 0 ? -0.85f : 0.85f, 0.85f, 1);
                    run.Enemies.InRadius(p, 0.4f, buf);
                    foreach (var e in buf)
                    {
                        if (!e.CanBeHitBy(hitGroup, t)) continue;
                        e.MarkHit(hitGroup, t, B.hitCooldown);
                        HitEnemy(e, Damage * 1.5f, (e.pos - H.Position).normalized, 1.5f);
                    }
                }
            }
        }

        protected override bool Fire()
        {
            run.Enemies.InRadius(H.Position, Radius, buf);
            for (int i = 0; i < buf.Count; i++)
                HitEnemy(buf[i], Damage, (buf[i].pos - H.Position).normalized, L.knockback * 0.3f, B.slow, B.poisonDps, quiet: buf.Count > 12);
            return true;
        }

        public override void Dispose()
        {
            if (cloud != null) Object.Destroy(cloud.gameObject);
            foreach (var o in orbiters) if (o != null) Object.Destroy(o.gameObject);
            orbiters.Clear();
        }
    }

    /// <summary>Teddy Turret / Robo Teddy: summons that shoot the nearest enemy (Robo Teddy follows you, permanently).</summary>
    public class TurretWeapon : WeaponInstance
    {
        class Turret { public Vector2 pos; public float life, shoot, age; public SpriteRenderer sr; }
        readonly List<Turret> turrets = new();

        public int TurretCount => turrets.Count;

        protected override bool Fire()
        {
            if (B.followsPlayer)
            {
                while (turrets.Count < Amount) turrets.Add(NewTurret(H.Position, float.MaxValue));
                return true;
            }
            if (turrets.Count >= Amount) Remove(0);
            Vector2 p = H.Position + Random.insideUnitCircle.normalized * Random.Range(0.8f, 1.6f);
            run.Map.ResolveCircle(ref p, 0.4f);
            turrets.Add(NewTurret(p, Duration));
            run.Fx.Spawn(ArtId.Puff, p, 1.2f, Color.white, 0.3f, growTo: 1.4f);
            Sfx.Play(SfxId.Pop, 0.4f);
            return true;
        }

        Turret NewTurret(Vector2 p, float life)
        {
            var t = new Turret { pos = p, life = life, shoot = 0.2f, sr = MakeSprite("Teddy", def.projectileArt, 3) };
            t.sr.transform.position = p;
            return t;
        }

        void Remove(int i)
        {
            if (turrets[i].sr != null) Object.Destroy(turrets[i].sr.gameObject);
            turrets.RemoveAt(i);
        }

        protected override void Update(float dt)
        {
            float interval = B.tickInterval / S.AttackSpeedMult;
            for (int i = turrets.Count - 1; i >= 0; i--)
            {
                var t = turrets[i];
                t.age += dt;
                if (!B.followsPlayer)
                {
                    t.life -= dt;
                    if (t.life <= 0) { run.Fx.Spawn(ArtId.Puff, t.pos, 1f, Color.white, 0.25f); Remove(i); continue; }
                }
                else
                {
                    float a = run.State.time * 1.2f + i * Mathf.PI * 2f / Mathf.Max(1, turrets.Count);
                    Vector2 want = H.Position + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 1.3f;
                    t.pos = Vector2.Lerp(t.pos, want, 1f - Mathf.Exp(-6f * dt));
                }
                t.shoot -= dt;
                if (t.shoot <= 0)
                {
                    var target = run.Enemies.Nearest(t.pos, Range);
                    if (target != null)
                    {
                        t.shoot = interval;
                        Vector2 d = (target.pos - t.pos).normalized;
                        var p = run.Projectiles.Fire(this, ArtId.TeddyShot, t.pos + d * 0.3f, d, 13f * S.ProjectileSpeedMult, Damage, 0.38f * VisualScale, 1.2f, L.pierce);
                        if (p != null) p.knockback = L.knockback;
                        Sfx.Play(SfxId.Shoot, 0.1f, 1.5f);
                    }
                    else t.shoot = 0.15f;
                }
                float bob = Mathf.Abs(Mathf.Sin(t.age * 6f)) * 0.08f;
                float s = 0.85f * VisualScale * (t.age < 0.2f ? t.age / 0.2f : 1f);
                t.sr.transform.position = new Vector3(t.pos.x, t.pos.y + bob, 0);
                t.sr.transform.localScale = Vector3.one * s;
            }
        }

        public override void Dispose()
        {
            for (int i = turrets.Count - 1; i >= 0; i--) Remove(i);
        }
    }

    /// <summary>Firecracker Trap / Fireworks Show: placed traps that blow up when enemies come close.</summary>
    public class TrapWeapon : WeaponInstance
    {
        class Trap { public Vector2 pos; public float life, age; public SpriteRenderer sr; }
        readonly List<Trap> traps = new();

        protected override bool Fire()
        {
            if (traps.Count >= Amount) return true;
            Vector2 p = H.Position + Random.insideUnitCircle.normalized * Random.Range(1.2f, 3.2f);
            run.Map.ResolveCircle(ref p, 0.3f);
            var t = new Trap { pos = p, life = Duration, sr = MakeSprite("Trap", def.projectileArt, -4) };
            t.sr.transform.position = p;
            t.sr.color = VisualColor;
            traps.Add(t);
            return true;
        }

        protected override void Update(float dt)
        {
            for (int i = traps.Count - 1; i >= 0; i--)
            {
                var t = traps[i];
                t.age += dt;
                t.life -= dt;
                float s = 0.55f * VisualScale * Mathf.Min(1f, t.age / 0.15f);
                t.sr.transform.localScale = Vector3.one * s * (1f + Mathf.Sin(t.age * 10f) * 0.05f);
                bool trigger = t.life <= 0;
                if (!trigger && t.age > 0.4f)
                {
                    run.Enemies.InRadius(t.pos, 0.9f, buf);
                    trigger = buf.Count > 0;
                }
                if (!trigger) continue;
                run.Zones.Blast(t.pos, 1.8f * Area, 0f, Damage, false, this, L.knockback, VisualColor, B.subBlasts);
                Object.Destroy(t.sr.gameObject);
                traps.RemoveAt(i);
            }
        }

        public override void Dispose()
        {
            foreach (var t in traps) if (t.sr != null) Object.Destroy(t.sr.gameObject);
            traps.Clear();
        }
    }

    /// <summary>Laser Pointer / Disco Laser / Prism Storm: sweeping beams that tick damage along their length.</summary>
    public class BeamWeapon : WeaponInstance
    {
        readonly List<SpriteRenderer> beams = new();
        readonly List<int> chained = new();
        float active, baseAngle, tick, sweep;

        public bool BeamActive => B.alwaysOn || active > 0;

        protected override bool Fire()
        {
            if (B.alwaysOn) return true;
            var t = run.Enemies.Nearest(H.Position, Range);
            if (t == null) return false;
            Vector2 d = t.pos - H.Position;
            baseAngle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            active = Duration;
            sweep = 0;
            Sfx.Play(SfxId.Laser, 0.35f);
            return true;
        }

        protected override void Update(float dt)
        {
            int n = B.alwaysOn ? Mathf.Max(Amount, 3) : Amount;
            while (beams.Count < n) beams.Add(MakeSprite("Beam", ArtId.Beam, 14));
            while (beams.Count > n) { Object.Destroy(beams[^1].gameObject); beams.RemoveAt(beams.Count - 1); }

            if (!BeamActive)
            {
                foreach (var b in beams) b.enabled = false;
                return;
            }
            if (!B.alwaysOn)
            {
                active -= dt;
                cooldownTimer = Mathf.Max(cooldownTimer, Cooldown); // cooldown starts after the beam ends
                // Track the nearest enemy slowly while sweeping.
                var t = run.Enemies.Nearest(H.Position, Range);
                if (t != null)
                {
                    Vector2 d = t.pos - H.Position;
                    baseAngle = Mathf.MoveTowardsAngle(baseAngle, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg, 70f * dt);
                }
            }
            else baseAngle += 55f * S.ProjectileSpeedMult * dt;
            sweep += dt;

            float len = Range;
            float width = 0.38f * Area * VisualScale;
            tick -= dt;
            bool doTick = tick <= 0;
            if (doTick) tick = B.tickInterval / S.AttackSpeedMult;

            for (int i = 0; i < n; i++)
            {
                float ang = baseAngle + (n > 1 ? i * 360f / n : 0f) + (B.alwaysOn ? 0f : Mathf.Sin(sweep * 4f) * 22f);
                Vector2 dir = new(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad));
                Vector2 a = H.Position + dir * 0.3f, b = a + dir * len;
                var sr = beams[i];
                sr.enabled = true;
                sr.transform.position = (a + b) * 0.5f;
                sr.transform.rotation = Quaternion.Euler(0, 0, ang);
                sr.transform.localScale = new Vector3(len, width * (1f + Mathf.Sin(run.State.time * 30f + i) * 0.12f), 1);
                var col = B.colorCycle ? Color.HSVToRGB(Mathf.Repeat(run.State.time * 0.5f + i / (float)n, 1f), 0.75f, 1f) : VisualColor;
                col.a = 0.85f;
                sr.color = col;
                if (!doTick) continue;
                run.Enemies.AlongSegment(a, b, width * 0.5f, buf);
                Enemy chainFrom = null;
                for (int k = 0; k < buf.Count; k++)
                {
                    HitEnemy(buf[k], Damage, dir, L.knockback, 0f, 0f, quiet: buf.Count > 8);
                    if (chainFrom == null && buf[k].alive) chainFrom = buf[k];
                }
                if (B.chains > 0 && chainFrom != null) Chain(chainFrom);
            }
        }

        void Chain(Enemy from)
        {
            chained.Clear();
            chained.Add(from.serial);
            Vector2 p = from.pos;
            for (int c = 0; c < B.chains; c++)
            {
                var next = run.Enemies.NearestExcluding(p, 4f, chained);
                if (next == null) break;
                chained.Add(next.serial);
                run.Fx.Line(p, next.pos, Color.HSVToRGB(Random.value, 0.6f, 1f), 0.16f, 0.12f, true);
                Vector2 np = next.pos;
                HitEnemy(next, Damage * 0.8f, (np - p).normalized, 0.5f, 0f, 0f, quiet: true);
                p = np;
            }
        }

        public override void Dispose()
        {
            foreach (var b in beams) if (b != null) Object.Destroy(b.gameObject);
            beams.Clear();
        }
    }
}
