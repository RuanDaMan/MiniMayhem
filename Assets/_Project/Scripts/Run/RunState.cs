using System.Collections.Generic;

namespace MiniMayhem
{
    /// <summary>Choices made before a run starts.</summary>
    public class RunSetup
    {
        public BiomeDefinition biome;
        public int matchIndex;
        public MatchMode mode;
        public MapStyle style;
        public WeaponDefinition startingWeapon;
        public int seed;
    }

    /// <summary>Mutable numbers for the current run.</summary>
    public class RunState
    {
        public float time;
        public int kills;
        public float gold;
        public int level = 1;
        public float xp;
        public int xpToNext;
        public int pendingLevelUps;
        public float damageTaken;
        public int bossKills;
        public int killTarget;
        public bool finished;
        public bool won;
        public string endReason;
    }

    public class WeaponSummary
    {
        public string name;
        public ArtId icon;
        public int level;
        public WeaponForm form;
        public float damage;
        public int kills;
    }

    /// <summary>Everything the results screen needs.</summary>
    public class RunResult
    {
        public bool won;
        public string reason;
        public float time;
        public int kills;
        public int level;
        public int runGold;
        public int metaGold;
        public int fromGold, fromKills, fromTime, winBonus;
        public float modeMultiplier, keepFraction;
        public BiomeDefinition biome;
        public int matchIndex;
        public MatchMode mode;
        public MapStyle style;
        public List<WeaponSummary> weapons = new();
        public List<string> unlocks = new();
    }
}
