using System;
using UnityEngine;

namespace MiniMayhem
{
    /// <summary>The player character: move-only control (weapons fire on their own), health, regen, armor, revives.</summary>
    public class Hero
    {
        readonly RunController run;
        readonly GameObject go;
        readonly Transform tf;
        readonly SpriteRenderer body, shadow, barBack, barFill;
        float animTime, invuln, flash, lavaTick;
        bool faceLeft;

        public Vector2 Position;
        public Vector2 Velocity;
        public Vector2 Facing = Vector2.right;
        public float Hp;
        public int RevivesLeft;
        public bool Dead { get; private set; }
        public float Speed => run.Stats.MoveSpeed * hazardSpeed;
        public bool Invulnerable => invuln > 0;
        public string LastDamageSource { get; private set; }
        public HazardType StandingOn { get; private set; }

        /// <summary>Test / debug hook: when set, used instead of the controls.</summary>
        public Vector2? MoveOverride;
        public bool GodMode;

        float hazardSpeed = 1f;

        public event Action<float> Damaged;
        public event Action Revived;

        public Hero(RunController run, Transform parent)
        {
            this.run = run;
            go = new GameObject("Hero");
            tf = go.transform;
            tf.SetParent(parent, false);

            shadow = Sprite(tf, "Shadow", ArtId.Shadow, -8);
            shadow.transform.localPosition = new Vector3(0, -0.5f, 0);
            shadow.transform.localScale = new Vector3(1f, 0.6f, 1);
            body = Sprite(tf, "Body", ArtId.Hero, 4);
            body.transform.localScale = Vector3.one * 1.45f;
            barBack = Sprite(tf, "HpBack", ArtId.Pixel, 40);
            barBack.color = new Color(0.1f, 0.08f, 0.15f, 0.8f);
            barBack.transform.localPosition = new Vector3(0, -0.78f, 0);
            barBack.transform.localScale = new Vector3(1.0f, 0.12f, 1);
            barFill = Sprite(tf, "HpFill", ArtId.Pixel, 41);
            barFill.color = new Color(0.35f, 0.95f, 0.4f);

            Hp = run.Stats.MaxHp;
            RevivesLeft = run.Stats.Revives;
        }

        static SpriteRenderer Sprite(Transform parent, string name, ArtId art, int order)
        {
            var g = new GameObject(name);
            g.transform.SetParent(parent, false);
            var sr = g.AddComponent<SpriteRenderer>();
            sr.sharedMaterial = Art.SpriteMaterial;
            sr.sprite = Art.Get(art);
            sr.sortingOrder = order;
            return sr;
        }

        public void Teleport(Vector2 p)
        {
            Position = p;
            Velocity = Vector2.zero;
            tf.position = p;
        }

        public void Tick(float dt)
        {
            if (Dead) return;
            var cfg = run.Config;

            // Hazards under the hero.
            var hz = run.Map.HazardAt(Position);
            StandingOn = hz;
            hazardSpeed = hz switch { HazardType.SlowMud => 0.6f, HazardType.Sugar => 0.5f, _ => 1f };
            if (hz == HazardType.Lava)
            {
                lavaTick += dt;
                if (lavaTick >= 0.5f) { lavaTick = 0; TakeDamage(4f + run.State.time / 60f, "lava", ignoreInvuln: true); }
            }

            Vector2 input = MoveOverride ?? run.Controls.MoveValue;
            if (input.sqrMagnitude > 1f) input.Normalize();
            Vector2 want = input * Speed;
            float accel = hz == HazardType.Ice ? 2.2f : 22f;
            Velocity = Vector2.Lerp(Velocity, want, 1f - Mathf.Exp(-accel * dt));
            Position += Velocity * dt;
            run.Map.ResolveCircle(ref Position, cfg.heroRadius);
            if (input.sqrMagnitude > 0.04f) Facing = input.normalized;

            // Regen.
            if (Hp < run.Stats.MaxHp) Hp = Mathf.Min(run.Stats.MaxHp, Hp + run.Stats.Regen * dt);
            if (invuln > 0) invuln -= dt;
            if (flash > 0) flash -= dt;

            // Visual.
            bool moving = Velocity.sqrMagnitude > 0.3f;
            animTime += dt * (moving ? 9f : 3f);
            if (Mathf.Abs(Facing.x) > 0.1f) faceLeft = Facing.x < 0;
            body.flipX = faceLeft;
            body.sprite = Art.Get(moving && Mathf.Repeat(animTime, Mathf.PI * 2f) > Mathf.PI ? ArtId.HeroStep : ArtId.Hero);
            float squash = 1f + Mathf.Sin(animTime * 2f) * (moving ? 0.05f : 0.025f);
            body.transform.localScale = new Vector3(1.45f * (2f - squash), 1.45f * squash, 1f);
            body.transform.localPosition = new Vector3(0, moving ? Mathf.Abs(Mathf.Sin(animTime)) * 0.08f : 0f, 0);
            body.color = flash > 0 ? new Color(1f, 0.45f, 0.45f) : (invuln > 0 && Mathf.Repeat(invuln, 0.16f) < 0.08f ? new Color(1, 1, 1, 0.55f) : Color.white);
            tf.position = new Vector3(Position.x, Position.y, 0);
            float f = Mathf.Clamp01(Hp / run.Stats.MaxHp);
            barFill.transform.localPosition = new Vector3(-(1f - f) * 0.48f, -0.78f, 0);
            barFill.transform.localScale = new Vector3(0.96f * f, 0.07f, 1);
            barFill.color = f > 0.5f ? new Color(0.35f, 0.95f, 0.4f) : f > 0.25f ? new Color(1f, 0.8f, 0.25f) : new Color(1f, 0.3f, 0.3f);
        }

        public void TakeDamage(float amount, string source, bool ignoreInvuln = false)
        {
            if (Dead) return;
            if (invuln > 0 && !ignoreInvuln) return;
            float dmg = Mathf.Max(1f, amount - run.Stats.Armor);
            if (GodMode)
            {
                // Debug / test mode: the hit registers but costs no HP.
                run.State.damageTaken += dmg;
                if (!ignoreInvuln) invuln = run.Config.invulnerableAfterHit;
                return;
            }
            Hp -= dmg;
            LastDamageSource = source;
            if (!ignoreInvuln) invuln = run.Config.invulnerableAfterHit;
            flash = 0.12f;
            run.State.damageTaken += dmg;
            Sfx.Play(SfxId.Hurt, 0.5f, 1f, 0.12f);
            run.Shake(0.12f);
            Damaged?.Invoke(dmg);
            if (Hp <= 0) Die();
        }

        public void Heal(float amount)
        {
            if (Dead) return;
            Hp = Mathf.Min(run.Stats.MaxHp, Hp + amount);
            run.Fx.Spawn(ArtId.Ring, Position, 1.4f, new Color(0.4f, 1f, 0.5f, 0.8f), 0.4f, growTo: 1.8f);
        }

        void Die()
        {
            if (RevivesLeft > 0)
            {
                RevivesLeft--;
                Hp = run.Stats.MaxHp * 0.5f;
                invuln = run.Config.reviveInvulnerable;
                run.Projectiles.ClearHostile();
                run.Zones.ClearHostile();
                foreach (var e in run.Enemies.Active)
                {
                    if (!e.alive) continue;
                    Vector2 d = e.pos - Position;
                    if (d.sqrMagnitude < 36f) e.knock += d.normalized * 14f;
                }
                run.Fx.Spawn(ArtId.Ring, Position, 2f, new Color(1f, 0.95f, 0.5f), 0.6f, growTo: 6f);
                Sfx.Play(SfxId.Revive, 0.8f);
                Revived?.Invoke();
                return;
            }
            Hp = 0;
            Dead = true;
            run.Fx.Burst(Position, new Color(1f, 0.5f, 0.4f), 20, 6f, 0.4f);
            run.EndRun(false, LastDamageSource != null ? $"Defeated by {LastDamageSource}" : "Defeated");
        }

        public void SetVisible(bool v) => go.SetActive(v);
    }
}
