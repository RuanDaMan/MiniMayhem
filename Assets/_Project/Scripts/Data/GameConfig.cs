using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiniMayhem
{
    [Serializable]
    public class ModeSettings
    {
        public MatchMode mode;
        public string displayName;
        [TextArea] public string description;
        public float rewardMultiplier = 1f;
    }

    /// <summary>Global tuning: hero base stats, XP curve, pacing, drops, economy, camera.</summary>
    [CreateAssetMenu(menuName = "Mini Mayhem/Game Config", fileName = "GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("Hero base stats")]
        public List<StatMod> baseStats = new()
        {
            new(StatId.MaxHp, 100), new(StatId.Regen, 0.2f), new(StatId.MoveSpeed, 0), new(StatId.PickupRadius, 1.8f),
            new(StatId.CritChance, 0.05f), new(StatId.CritDamage, 1.5f), new(StatId.CardChoices, 3), new(StatId.DeathKeep, 0.5f),
        };
        [Tooltip("Metres per second at +0% move speed.")]
        public float baseMoveSpeed = 4.6f;
        public float heroRadius = 0.42f;
        public float invulnerableAfterHit = 0.45f;
        public float reviveInvulnerable = 2.5f;
        [Range(0, 1)] public float deathKeepCap = 0.8f;
        public int maxWeapons = 6;
        public int maxItems = 6;
        public int maxCardChoices = 4;

        [Header("XP curve: xp(level) = base + linear*level + quad*level^2")]
        public float xpBase = 5f;
        public float xpLinear = 2f;
        public float xpQuad = 0.22f;

        [Header("Match")]
        public float matchLength = 600f;
        public List<ModeSettings> modes = new()
        {
            new() { mode = MatchMode.Survive, displayName = "Survive", description = "Defeat the finale boss at 9:00.", rewardMultiplier = 1.25f },
            new() { mode = MatchMode.OutlastTime, displayName = "Outlast", description = "Stay alive until 10:00.", rewardMultiplier = 1f },
            new() { mode = MatchMode.KillCount, displayName = "Kill Count", description = "Reach the kill target before 10:00.", rewardMultiplier = 1.1f },
        };

        [Header("Enemy scaling")]
        public float enemyHpPerMinute = 0.18f;
        public float enemyDamagePerMinute = 0.08f;
        public float enemySpeedPerMinute = 0.015f;
        public int enemyCap = 450;
        public float eliteHpMultiplier = 4f;
        [Tooltip("Boss/mini boss HP multiplier for each repeat in the same match (3:00, 6:00, 9:00).")]
        public float bossRepeatMultiplier = 1.8f;
        public float despawnDistance = 26f;

        [Header("Drops")]
        public int maxGems = 320;
        public float healPickupAmount = 30f;
        public Vector2 magnetInterval = new(45f, 90f);
        public Vector2 healthInterval = new(45f, 90f);
        [Range(0, 1)] public float bombDropChance = 0.0015f;
        [Range(0, 1)] public float healDropChance = 0.002f;
        public int coinValue = 1;

        [Header("Level-up utilities")]
        public int skipGold = 10;
        public float skipHeal = 15f;
        public int fallbackGold = 25;
        public float fallbackHeal = 40f;
        public float fallbackStatBoost = 0.05f;

        [Header("Meta economy")]
        public float goldPerKill = 0.06f;
        public float goldPerMinute = 6f;
        public float winBonus = 60f;
        public int matchesToUnlockNextBiome = 3;

        [Header("Camera")]
        public float cameraSize = 8.5f;
        public float cameraSizeSwarm = 10.5f;
        [Tooltip("Enemy count at which the camera is fully zoomed out.")]
        public int cameraSwarmCount = 350;
        public float cameraFollow = 10f;

        public ModeSettings Mode(MatchMode m)
        {
            foreach (var s in modes) if (s.mode == m) return s;
            return new ModeSettings { mode = m, displayName = m.ToString() };
        }

        public int XpToNext(int level) => Mathf.RoundToInt(xpBase + xpLinear * level + xpQuad * level * level);
    }
}
