using UnityEngine;

namespace MiniMayhem
{
    /// <summary>
    /// Final hero stats for a run: config base + skill tree + items + small in-run boosts (fallback cards).
    /// Exposes ready-to-use multipliers for weapons.
    /// </summary>
    public class PlayerStats
    {
        readonly GameConfig cfg;
        public readonly StatBlock Base = new();
        public readonly StatBlock Meta = new();
        public readonly StatBlock Items = new();
        public readonly StatBlock RunBoosts = new();
        public readonly StatBlock Total = new();

        public PlayerStats(GameConfig cfg)
        {
            this.cfg = cfg;
            Base.Add(cfg.baseStats);
            Recompute();
        }

        public void Recompute()
        {
            Total.CopyFrom(Base);
            Total.Add(Meta);
            Total.Add(Items);
            Total.Add(RunBoosts);
        }

        public float this[StatId id] => Total[id];

        public float MaxHp => Mathf.Max(1f, Total[StatId.MaxHp]);
        public float Regen => Mathf.Max(0f, Total[StatId.Regen]);
        public float Armor => Mathf.Max(0f, Total[StatId.Armor]);
        public float MoveSpeed => cfg.baseMoveSpeed * Mathf.Max(0.3f, 1f + Total[StatId.MoveSpeed]);
        public float DamageMult => Mathf.Max(0.1f, 1f + Total[StatId.Damage]);
        public float AttackSpeedMult => Mathf.Max(0.2f, 1f + Total[StatId.AttackSpeed]);
        public float AreaMult => Mathf.Max(0.2f, 1f + Total[StatId.Area]);
        public float ProjectileSpeedMult => Mathf.Max(0.2f, 1f + Total[StatId.ProjectileSpeed]);
        public float DurationMult => Mathf.Max(0.2f, 1f + Total[StatId.Duration]);
        public int Amount => Mathf.Max(0, Mathf.FloorToInt(Total[StatId.Amount] + 0.001f));
        public float PickupRadius => Mathf.Max(0.5f, Total[StatId.PickupRadius]);
        public float XpMult => Mathf.Max(0f, 1f + Total[StatId.XpGain]);
        public float GoldMult => Mathf.Max(0f, 1f + Total[StatId.GoldGain]);
        public float CritChance => Mathf.Clamp01(Total[StatId.CritChance]);
        public float CritDamage => Mathf.Max(1f, Total[StatId.CritDamage]);
        public float Luck => Mathf.Max(0f, Total[StatId.Luck]);
        public int Revives => Mathf.Max(0, Mathf.RoundToInt(Total[StatId.Revives]));
        public int Rerolls => Mathf.Max(0, Mathf.RoundToInt(Total[StatId.Rerolls]));
        public int Skips => Mathf.Max(0, Mathf.RoundToInt(Total[StatId.Skips]));
        public int Banishes => Mathf.Max(0, Mathf.RoundToInt(Total[StatId.Banishes]));
        public int CardChoices => Mathf.Clamp(Mathf.RoundToInt(Total[StatId.CardChoices]), 1, cfg.maxCardChoices);
        public float DeathKeep => Mathf.Clamp(Total[StatId.DeathKeep], 0f, cfg.deathKeepCap);
    }
}
