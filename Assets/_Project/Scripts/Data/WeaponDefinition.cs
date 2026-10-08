using System;
using UnityEngine;

namespace MiniMayhem
{
    public enum WeaponArchetype { Projectile, Boomerang, MeleeArc, Orbit, Chain, Lob, Aura, Turret, Trap, Beam }
    public enum TargetMode { Nearest, RandomInRange, Strongest, Facing, AllAround }
    public enum WeaponForm { Base, Evolved, Fused }

    [Flags]
    public enum WeaponTags
    {
        None = 0, Physical = 1, Fire = 2, Water = 4, Electric = 8, Magic = 16, Projectile = 32, Melee = 64,
        Area = 128, Summon = 256, Poison = 512, Beam = 1024, Trap = 2048,
    }

    /// <summary>Stats for one weapon level. Interpretation per archetype is documented on WeaponDefinition.</summary>
    [Serializable]
    public class WeaponLevel
    {
        [Tooltip("Damage per hit (or per tick for auras/beams).")]
        public float damage = 10f;
        [Tooltip("Seconds between attacks (before Attack Speed).")]
        public float cooldown = 1f;
        [Tooltip("Projectiles / orbiters / turrets / traps / beams.")]
        public int amount = 1;
        [Tooltip("Size multiplier (swing radius, splash, aura, beam width...).")]
        public float area = 1f;
        [Tooltip("Projectile speed / orbit speed (rad/s).")]
        public float speed = 10f;
        [Tooltip("Lifetime of puddles, turrets, beams, boomerang flights, traps.")]
        public float duration = 1f;
        [Tooltip("Extra enemies a projectile passes through (99 = unlimited).")]
        public int pierce;
        public float knockback = 1f;
        [Tooltip("Short line shown on the level-up card and in the codex.")]
        public string note;
    }

    /// <summary>Archetype-specific behaviour switches. Unused fields are ignored by archetypes that do not need them.</summary>
    [Serializable]
    public class WeaponBehaviour
    {
        [Tooltip("Max targeting range in metres.")]
        public float range = 9f;
        [Tooltip("Homing turn rate (rad/s), 0 = straight.")]
        public float homing;
        [Tooltip("Explosion radius on impact (0 = none).")]
        public float explodeRadius;
        [Tooltip("Times a projectile redirects to a new enemy after a hit.")]
        public int bounces;
        [Tooltip("Extra enemies hit by chain lightning.")]
        public int chains;
        [Tooltip("Swing arc in degrees (melee).")]
        public float arcDegrees = 150f;
        [Tooltip("Seconds between damage ticks for auras / beams / puddles.")]
        public float tickInterval = 0.5f;
        [Tooltip("Seconds before the same orbiter/boomerang can hit the same enemy again.")]
        public float hitCooldown = 0.5f;
        [Tooltip("Slow applied to enemies (0..1).")]
        public float slow;
        [Tooltip("Poison damage per second applied for 3 s.")]
        public float poisonDps;
        [Tooltip("Orbiters added on top (Bubble Bath).")]
        public int orbiters;
        [Tooltip("Secondary blasts spawned by explosions (Fireworks).")]
        public int subBlasts;
        [Tooltip("Spread angle between multiple projectiles (degrees).")]
        public float spread = 12f;
        public bool followsPlayer;
        public bool launchEnemies;
        public bool alwaysOn;
        public bool shockwave;
        public bool pulses;
        public bool stormStrikes;
        public bool colorCycle;
    }

    /// <summary>
    /// One weapon (base, evolved or fused form). All numbers live here so balance is edited in the Inspector.
    /// Base weapons have 5 levels, evolved / fused forms have a single level and never level up.
    /// </summary>
    [CreateAssetMenu(menuName = "Mini Mayhem/Weapon", fileName = "Weapon")]
    public class WeaponDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea(2, 5)] public string description;
        public WeaponArchetype archetype;
        public TargetMode targeting;
        public WeaponTags tags;
        public WeaponForm form;
        [Tooltip("Available from the very first run.")]
        public bool startsUnlocked;
        [Tooltip("Clearing this biome (index) unlocks the weapon. -1 = not unlocked by a biome.")]
        public int unlockBiome = -1;
        public ArtId icon;
        public ArtId projectileArt;
        [Tooltip("Colour at level 1; shifts towards colorMax as the weapon levels up.")]
        public Color color = Color.white;
        public Color colorMax = Color.white;
        public WeaponLevel[] levels = new WeaponLevel[5];
        public WeaponBehaviour behaviour = new();

        public int MaxLevel => levels != null ? levels.Length : 0;
        public bool IsBase => form == WeaponForm.Base;

        public WeaponLevel Level(int level) => levels[Mathf.Clamp(level, 1, MaxLevel) - 1];

        public string HowToUnlock(GameDatabase db)
        {
            if (form == WeaponForm.Evolved) return "Evolve its base weapon.";
            if (form == WeaponForm.Fused) return "Fuse its two base weapons.";
            if (startsUnlocked) return "Unlocked from the start.";
            if (unlockBiome >= 0 && db != null && unlockBiome < db.biomes.Count)
                return $"Complete all 3 matches in {db.biomes[unlockBiome].displayName}.";
            return "Locked.";
        }
    }
}
