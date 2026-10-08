using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiniMayhem
{
    /// <summary>
    /// Every player stat. Values are additive in their own unit:
    /// percentage stats are fractions (0.1 = +10%), flat stats are plain numbers.
    /// </summary>
    public enum StatId
    {
        MaxHp, Regen, Armor, MoveSpeed, Damage, AttackSpeed, Area, ProjectileSpeed, Duration, Amount,
        PickupRadius, XpGain, GoldGain, CritChance, CritDamage, Luck, Revives, Rerolls, Skips, Banishes,
        CardChoices, DeathKeep,
    }

    public enum StatFormat { Flat, Percent, Integer, PerSecond }

    [Serializable]
    public struct StatMod
    {
        public StatId stat;
        public float value;

        public StatMod(StatId stat, float value) { this.stat = stat; this.value = value; }

        public override string ToString() => StatInfo.FormatMod(stat, value);
    }

    /// <summary>A full set of stat values (one float per StatId).</summary>
    public class StatBlock
    {
        public static readonly int Count = Enum.GetValues(typeof(StatId)).Length;
        readonly float[] v = new float[Count];

        public float this[StatId id] { get => v[(int)id]; set => v[(int)id] = value; }

        public void Clear() => Array.Clear(v, 0, v.Length);
        public void Add(StatId id, float value) => v[(int)id] += value;
        public void Add(StatMod m, float times = 1f) => v[(int)m.stat] += m.value * times;

        public void Add(IEnumerable<StatMod> mods, float times = 1f)
        {
            if (mods == null) return;
            foreach (var m in mods) Add(m, times);
        }

        public void Add(StatBlock other)
        {
            for (int i = 0; i < Count; i++) v[i] += other.v[i];
        }

        public void CopyFrom(StatBlock other) => Array.Copy(other.v, v, Count);
    }

    /// <summary>Display names / formatting for stats, shared by the HUD, skill tree, cards and codex.</summary>
    public static class StatInfo
    {
        public static string Name(StatId id) => id switch
        {
            StatId.MaxHp => "Max HP",
            StatId.Regen => "HP Regen",
            StatId.Armor => "Armor",
            StatId.MoveSpeed => "Move Speed",
            StatId.Damage => "Damage",
            StatId.AttackSpeed => "Attack Speed",
            StatId.Area => "Area",
            StatId.ProjectileSpeed => "Projectile Speed",
            StatId.Duration => "Duration",
            StatId.Amount => "Amount",
            StatId.PickupRadius => "Pickup Radius",
            StatId.XpGain => "XP Gain",
            StatId.GoldGain => "Gold Gain",
            StatId.CritChance => "Crit Chance",
            StatId.CritDamage => "Crit Damage",
            StatId.Luck => "Luck",
            StatId.Revives => "Revives",
            StatId.Rerolls => "Rerolls",
            StatId.Skips => "Skips",
            StatId.Banishes => "Banishes",
            StatId.CardChoices => "Level-up Choices",
            StatId.DeathKeep => "Gold kept on death",
            _ => id.ToString(),
        };

        public static StatFormat Format(StatId id) => id switch
        {
            StatId.MaxHp or StatId.Armor or StatId.PickupRadius => StatFormat.Flat,
            StatId.Regen => StatFormat.PerSecond,
            StatId.Amount or StatId.Revives or StatId.Rerolls or StatId.Skips or StatId.Banishes or StatId.CardChoices => StatFormat.Integer,
            _ => StatFormat.Percent,
        };

        public static string Describe(StatId id) => id switch
        {
            StatId.MaxHp => "How much damage you can take.",
            StatId.Regen => "HP restored every second.",
            StatId.Armor => "Flat damage removed from every hit you take (minimum 1 damage).",
            StatId.MoveSpeed => "How fast the hero walks.",
            StatId.Damage => "Multiplies all weapon damage.",
            StatId.AttackSpeed => "Weapons fire more often (shorter cooldowns).",
            StatId.Area => "Bigger swings, auras, splashes and explosions.",
            StatId.ProjectileSpeed => "Faster projectiles, orbits and boomerangs.",
            StatId.Duration => "Longer lasting puddles, turrets, beams and boomerang flights.",
            StatId.Amount => "Extra projectiles, orbiters, turrets and traps.",
            StatId.PickupRadius => "How far away XP gems and coins are pulled in (metres).",
            StatId.XpGain => "More XP from every gem.",
            StatId.GoldGain => "More gold from every coin.",
            StatId.CritChance => "Chance for a hit to critically strike.",
            StatId.CritDamage => "Damage multiplier of critical hits.",
            StatId.Luck => "Better level-up cards (double upgrades) and more drops.",
            StatId.Revives => "Get back up once per revive, at half HP.",
            StatId.Rerolls => "Re-roll the level-up cards.",
            StatId.Skips => "Skip a level-up for a little gold and healing.",
            StatId.Banishes => "Remove a card from the pool for the rest of the run.",
            StatId.CardChoices => "Number of cards offered at each level-up.",
            StatId.DeathKeep => "Share of the run's gold you keep if you die.",
            _ => "",
        };

        public static string FormatValue(StatId id, float value) => Format(id) switch
        {
            StatFormat.Percent => $"{value * 100f:0.#}%",
            StatFormat.Integer => $"{Mathf.RoundToInt(value)}",
            StatFormat.PerSecond => $"{value:0.##}/s",
            _ => $"{value:0.##}",
        };

        public static string FormatMod(StatId id, float value)
        {
            string sign = value >= 0 ? "+" : "-";
            float a = Mathf.Abs(value);
            return Format(id) switch
            {
                StatFormat.Percent => $"{sign}{a * 100f:0.#}% {Name(id)}",
                StatFormat.Integer => $"{sign}{Mathf.RoundToInt(a)} {Name(id)}",
                StatFormat.PerSecond => $"{sign}{a:0.##} {Name(id)}/s",
                _ => $"{sign}{a:0.##} {Name(id)}",
            };
        }
    }
}
