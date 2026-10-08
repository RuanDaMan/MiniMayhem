using UnityEngine;

namespace MiniMayhem
{
    /// <summary>
    /// One live enemy. Plain data simulated by EnemyManager (no physics bodies); the GameObject is only a visual.
    /// Instances are pooled and reused, so anything holding a reference should also remember <see cref="serial"/>.
    /// </summary>
    public class Enemy
    {
        public const int HitGroups = 32;

        public int index;
        public int serial;
        public bool alive;
        public EnemyDefinition def;
        public bool elite;
        public bool isBoss;

        public Vector2 pos, vel, knock;
        public float hp, maxHp, damage, speed, radius, armor, knockResist;
        public float age, flash, spawnAnim, phase;
        public float slowTimer, slowFactor;
        public float poisonTimer, poisonDps, poisonTick;
        public float attackTimer, stateTimer;
        public int state;
        public Vector2 dashDir;
        public float launchTimer, launchDamage;
        public float teleportCooldown;
        public bool submerged;
        public readonly float[] nextHit = new float[HitGroups];

        // Boss brain
        public int patternIndex;
        public BossPattern currentPattern;
        public float patternTimer, patternTick;
        public int patternStep;
        public int repeat;

        // Visuals
        public GameObject go;
        public Transform tf;
        public SpriteRenderer sr;
        public SpriteRenderer eliteRing;
        public SpriteRenderer barBack, barFill;
        public float baseScale;

        public float HpFraction => maxHp > 0 ? Mathf.Clamp01(hp / maxHp) : 0f;
        public bool Targetable => alive && !submerged && spawnAnim >= 0.15f;

        public bool CanBeHitBy(int group, float time) => group < 0 || nextHit[group] <= time;
        public void MarkHit(int group, float time, float cooldown) { if (group >= 0) nextHit[group] = time + cooldown; }
    }
}
