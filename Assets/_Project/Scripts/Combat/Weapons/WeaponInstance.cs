using System.Collections.Generic;
using UnityEngine;

namespace MiniMayhem
{
    /// <summary>
    /// A weapon owned in a run. One subclass per archetype; stats come from the definition's level table
    /// scaled by the hero's stats. Level-ups make it bigger and shift its colour.
    /// </summary>
    public abstract class WeaponInstance
    {
        public WeaponDefinition def;
        public int level = 1;
        public int hitGroup;
        public float damageDealt;
        public int kills;
        protected RunController run;
        protected float cooldownTimer;
        protected readonly List<Enemy> buf = new();

        public WeaponLevel L => def.Level(level);
        public WeaponBehaviour B => def.behaviour;
        public bool IsMaxLevel => level >= def.MaxLevel;
        public bool CanLevel => def.IsBase && !IsMaxLevel;

        protected PlayerStats S => run.Stats;
        protected Hero H => run.Hero;

        public float Damage => L.damage * S.DamageMult;
        public float Cooldown => Mathf.Max(0.05f, L.cooldown / S.AttackSpeedMult);
        public float Area => L.area * S.AreaMult;
        public int Amount => Mathf.Max(1, L.amount + S.Amount);
        public float Speed => L.speed * S.ProjectileSpeedMult;
        public float Duration => L.duration * S.DurationMult;
        public float Range => B.range * Mathf.Sqrt(S.AreaMult);

        /// <summary>Visual growth per level (and for evolved / fused forms).</summary>
        public float VisualScale => def.form == WeaponForm.Base ? 1f + 0.09f * (level - 1) : 1.45f;

        public Color VisualColor
        {
            get
            {
                if (def.form != WeaponForm.Base && B.colorCycle) return Color.HSVToRGB(Mathf.Repeat(Time.time * 0.6f, 1f), 0.7f, 1f);
                float t = def.MaxLevel > 1 ? (level - 1f) / (def.MaxLevel - 1f) : 1f;
                return Color.Lerp(def.color, def.colorMax, t);
            }
        }

        public static WeaponInstance Create(WeaponDefinition def, RunController run, int hitGroup)
        {
            WeaponInstance w = def.archetype switch
            {
                WeaponArchetype.Projectile => new ProjectileWeapon(),
                WeaponArchetype.Boomerang => new BoomerangWeapon(),
                WeaponArchetype.MeleeArc => new MeleeWeapon(),
                WeaponArchetype.Orbit => new OrbitWeapon(),
                WeaponArchetype.Chain => new ChainWeapon(),
                WeaponArchetype.Lob => new LobWeapon(),
                WeaponArchetype.Aura => new AuraWeapon(),
                WeaponArchetype.Turret => new TurretWeapon(),
                WeaponArchetype.Trap => new TrapWeapon(),
                WeaponArchetype.Beam => new BeamWeapon(),
                _ => new ProjectileWeapon(),
            };
            w.def = def;
            w.run = run;
            w.hitGroup = hitGroup % Enemy.HitGroups;
            w.cooldownTimer = 0.3f;
            w.Init();
            return w;
        }

        protected virtual void Init() { }
        public virtual void OnLevelChanged() { }
        public virtual void Dispose() { }

        public void Tick(float dt)
        {
            cooldownTimer -= dt;
            Update(dt);
            if (cooldownTimer <= 0f)
            {
                if (Fire()) cooldownTimer = Cooldown;
                else cooldownTimer = 0.1f; // nothing to shoot at: check again soon
            }
        }

        /// <summary>Per-frame behaviour (orbiters, auras, beams). Optional.</summary>
        protected virtual void Update(float dt) { }

        /// <summary>Attack once the cooldown is up. Return false if there was no target (retries shortly).</summary>
        protected abstract bool Fire();

        /// <summary>Apply a hit with a crit roll. damage already includes the hero's damage multiplier.</summary>
        public void HitEnemy(Enemy e, float damage, Vector2 dir, float knockback, float slow = 0f, float poison = 0f, bool quiet = false)
        {
            if (e == null || !e.alive) return;
            bool crit = !quiet && Random.value < S.CritChance + (def.form != WeaponForm.Base ? 0.05f : 0f);
            float dmg = crit ? damage * S.CritDamage : damage;
            if (slow > 0) run.Enemies.ApplySlow(e, slow, 1.5f);
            if (poison > 0) run.Enemies.ApplyPoison(e, poison * S.DamageMult, 3f);
            if (B.launchEnemies && !quiet && knockback > 0) run.Enemies.Launch(e, dir, knockback * 3f, dmg * 0.5f);
            run.Enemies.Damage(e, dmg, crit, dir, knockback, this, quiet);
        }

        // ------------------------------------------------------------------ targeting helpers

        protected Enemy PickTarget(float range)
        {
            Vector2 p = H.Position;
            return def.targeting switch
            {
                TargetMode.RandomInRange => run.Enemies.RandomInRange(p, range),
                TargetMode.Strongest => run.Enemies.Strongest(p, range) ?? run.Enemies.Nearest(p, range),
                _ => run.Enemies.Nearest(p, range),
            };
        }

        /// <summary>Direction to attack in: facing for Facing weapons, otherwise towards the chosen target.</summary>
        protected bool AimDirection(float range, out Vector2 dir, out Enemy target)
        {
            target = null;
            if (def.targeting == TargetMode.Facing) { dir = H.Facing; target = run.Enemies.Nearest(H.Position, range); return true; }
            if (def.targeting == TargetMode.AllAround) { dir = Random.insideUnitCircle.normalized; return true; }
            target = PickTarget(range);
            if (target == null) { dir = H.Facing; return false; }
            dir = (target.pos - H.Position).normalized;
            return true;
        }

        protected static Vector2 Rotate(Vector2 v, float deg)
        {
            float r = deg * Mathf.Deg2Rad, s = Mathf.Sin(r), c = Mathf.Cos(r);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }

        protected SpriteRenderer MakeSprite(string name, ArtId art, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(run.WorldRoot, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sharedMaterial = Art.SpriteMaterial;
            sr.sprite = Art.Get(art);
            sr.sortingOrder = order;
            return sr;
        }

        /// <summary>Short card / codex description of the next level-up.</summary>
        public string NextLevelText()
        {
            if (!CanLevel) return "";
            var n = def.Level(level + 1);
            return string.IsNullOrEmpty(n.note) ? "Stronger." : n.note;
        }
    }
}
