using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiniMayhem
{
    [Serializable]
    public class SpawnEntry
    {
        [Tooltip("Seconds into the match.")]
        public float time;
        public EnemyDefinition enemy;
        public int count = 10;
        public SpawnPattern pattern = SpawnPattern.Ring;
        public bool elite;
    }

    /// <summary>
    /// Data-driven pacing: scripted spawn entries plus a continuous trickle whose rate grows with time.
    /// Bosses, mini bosses and swarm bursts are added from the biome roster at the configured times.
    /// </summary>
    [CreateAssetMenu(menuName = "Mini Mayhem/Wave Timeline", fileName = "Waves")]
    public class WaveTimeline : ScriptableObject
    {
        [Tooltip("Trickle spawns per second, by match minute (x = minute 0..10).")]
        public AnimationCurve trickleRate = AnimationCurve.Linear(0, 0.8f, 10, 6f);
        [Tooltip("Minute each normal enemy (by roster index) joins the trickle.")]
        public float[] normalUnlockMinute = { 0f, 0.5f, 1.5f, 2.5f, 4f, 5.5f };
        public float[] swarmTimes = { 150f, 300f, 450f, 570f };
        public int swarmSize = 70;
        public float[] miniBossTimes = { 90f, 270f, 450f, 510f };
        public float[] bossTimes = { 180f, 360f, 540f };
        [Tooltip("Chance (by minute) that a trickle spawn is an elite.")]
        public AnimationCurve eliteChance = AnimationCurve.Linear(0, 0f, 10, 0.06f);
        [Tooltip("Seconds of reduced spawning after a boss dies.")]
        public float breatherAfterBoss = 18f;
        public List<SpawnEntry> entries = new();
    }
}
