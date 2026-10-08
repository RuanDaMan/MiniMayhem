using System;
using System.Collections.Generic;

namespace MiniMayhem
{
    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public int gold;
        public int lifetimeGold;
        public List<RankEntry> skills = new();
        public List<BiomeEntry> biomes = new();
        public List<string> unlockedWeapons = new();
        public List<string> discovered = new();
        public RunStats stats = new();
        public SettingsData settings = new();
        public string lastWeapon;

        [Serializable] public struct RankEntry { public string id; public int rank; }

        [Serializable]
        public class BiomeEntry
        {
            public string id;
            /// <summary>Bit i set = match i completed at least once.</summary>
            public int completedMask;
            public int plays;
            public int wins;
        }
    }

    [Serializable]
    public class RunStats
    {
        public int runs;
        public int wins;
        public int kills;
        public int bossKills;
        public float bestSurvived;
        public int evolutions;
        public int fusions;
        public float playSeconds;
    }

    [Serializable]
    public class SettingsData
    {
        public float master = 0.8f;
        public float music = 0.6f;
        public float sfx = 0.8f;
        public bool damageNumbers = true;
        public bool screenShake = true;
        public bool colorblind;
        public bool fullscreen = true;
        public int resolutionIndex = -1;
        public bool showFps;
        public bool rumble = true;
    }
}
