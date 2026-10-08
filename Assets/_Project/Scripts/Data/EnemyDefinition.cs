using System;
using UnityEngine;

namespace MiniMayhem
{
    public enum EnemyTier { Normal, MiniBoss, Boss }

    public enum EnemyBehaviour
    {
        Chaser, Rusher, Tank, Shooter, Charger, Splitter, Swarm, Summoner, Exploder, Orbiter, Hopper, Ambusher,
        Bomber, Boss,
    }

    public enum BossPattern { RadialBurst, AimedSpread, Charge, Summon, Slam, Spiral, HazardDrop }

    public enum BodyShape { Blob, Round, Bug, Tall, Ghost, Worm, Square, Star, Bird, Flower, Cone, Snowman }

    [Flags]
    public enum BodyFeature
    {
        None = 0, Eyes = 1, BigEye = 2, Wings = 4, Antennae = 8, Spikes = 16, Shell = 32, Petals = 64, Horns = 128,
        Crown = 256, Ears = 512, Stripes = 1024, Teeth = 2048, Hat = 4096, Tail = 8192, Legs = 16384, Shine = 32768,
        Cheeks = 65536, Angry = 131072, Sprinkles = 262144, Bandage = 524288,
    }

    /// <summary>One enemy type. Visuals are generated from body shape + features + colours.</summary>
    [CreateAssetMenu(menuName = "Mini Mayhem/Enemy", fileName = "Enemy")]
    public class EnemyDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea(2, 4)] public string description;
        public EnemyTier tier;
        public EnemyBehaviour behaviour;

        [Header("Stats (minute 0, biome 1)")]
        public float maxHp = 10f;
        public float damage = 5f;
        public float speed = 2f;
        [Tooltip("Collision radius in metres.")]
        public float radius = 0.45f;
        [Tooltip("Damage taken is reduced by this fraction.")]
        [Range(0, 0.9f)] public float armor;
        [Range(0, 1)] public float knockbackResist;
        public int xp = 1;
        [Range(0, 1)] public float goldChance = 0.1f;

        [Header("Attacks")]
        public float attackInterval = 2.5f;
        public float attackRange = 6f;
        public float projectileSpeed = 6f;
        public int projectileCount = 1;
        [Tooltip("Spawned on death (splitter) or periodically (summoner / boss Summon).")]
        public EnemyDefinition child;
        public int childCount = 2;
        public BossPattern[] patterns = Array.Empty<BossPattern>();

        [Header("Look")]
        public BodyShape body;
        public BodyFeature features = BodyFeature.Eyes;
        public Color primary = Color.green;
        public Color secondary = Color.white;
        public Color accent = Color.black;
        [Tooltip("Sprite size relative to the collision diameter.")]
        public float visualScale = 1.35f;

        public bool IsBossLike => tier != EnemyTier.Normal;
    }
}
