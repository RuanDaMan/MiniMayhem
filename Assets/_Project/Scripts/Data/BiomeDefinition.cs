using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiniMayhem
{
    public enum HazardType { None, SlowMud, Sandstorm, Ice, Lava, Sugar }
    public enum MapStyle { Endless, FixedArena, ExpandingArena }
    public enum MatchMode { Survive, OutlastTime, KillCount }
    public enum SpawnPattern { Scatter, Ring, Line, Pincer, Cluster, SwarmBurst }

    /// <summary>A biome: roster, look, hazard, music flavour and the 3 matches that unlock the next biome.</summary>
    [CreateAssetMenu(menuName = "Mini Mayhem/Biome", fileName = "Biome")]
    public class BiomeDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea(2, 4)] public string description;
        public int index;
        [Tooltip("Enemy level multiplier for this biome (biome 6 is the hardest).")]
        public float difficulty = 1f;

        [Header("Roster")]
        public EnemyDefinition[] normals = new EnemyDefinition[6];
        public EnemyDefinition[] miniBosses = new EnemyDefinition[2];
        public EnemyDefinition boss;
        public WaveTimeline waves;

        [Header("Matches")]
        public MapStyle[] matchStyles = { MapStyle.Endless, MapStyle.FixedArena, MapStyle.Endless };
        public int killTarget = 600;

        [Header("Look")]
        public Color groundA = new(0.55f, 0.8f, 0.4f);
        public Color groundB = new(0.5f, 0.74f, 0.36f);
        public Color wallColor = new(0.45f, 0.35f, 0.25f);
        public ArtId[] props = Array.Empty<ArtId>();
        public Color[] propTints = Array.Empty<Color>();
        [Range(0, 6)] public float propDensity = 3f;

        [Header("Hazard")]
        public HazardType hazard;
        public Color hazardColor = new(0.4f, 0.3f, 0.2f, 0.6f);
        [Range(0, 3)] public float hazardDensity = 0.6f;

        [Header("Music")]
        public int musicRoot = 60;
        public bool musicMinor;
        public float musicTempo = 120f;
        public int musicSeed = 1;
    }
}
