using System.Text;
using UnityEngine;

namespace MiniMayhem
{
    /// <summary>Text generated straight from the ScriptableObjects, so the codex can never go out of date.</summary>
    public static class CodexText
    {
        public static string ArchetypeText(WeaponArchetype a) => a switch
        {
            WeaponArchetype.Projectile => "Projectile",
            WeaponArchetype.Boomerang => "Boomerang (hits going out and coming back)",
            WeaponArchetype.MeleeArc => "Melee swing",
            WeaponArchetype.Orbit => "Orbits the hero",
            WeaponArchetype.Chain => "Chain lightning",
            WeaponArchetype.Lob => "Lobbed splash + puddle",
            WeaponArchetype.Aura => "Aura around the hero",
            WeaponArchetype.Turret => "Summon / turret",
            WeaponArchetype.Trap => "Placed trap",
            WeaponArchetype.Beam => "Laser beam",
            _ => a.ToString(),
        };

        public static string TargetText(TargetMode t) => t switch
        {
            TargetMode.Nearest => "nearest enemy",
            TargetMode.RandomInRange => "random enemy in range",
            TargetMode.Strongest => "strongest enemy",
            TargetMode.Facing => "the way you face",
            _ => "all around",
        };

        public static string Tags(WeaponTags t)
        {
            var sb = new StringBuilder();
            foreach (WeaponTags v in System.Enum.GetValues(typeof(WeaponTags)))
                if (v != WeaponTags.None && (t & v) != 0) { if (sb.Length > 0) sb.Append(", "); sb.Append(v); }
            return sb.ToString();
        }

        public static string WeaponSummary(GameDatabase db, WeaponDefinition w)
        {
            var sb = new StringBuilder();
            sb.Append(w.description).Append("\n\n");
            sb.Append($"<b>Type:</b> {ArchetypeText(w.archetype)}\n<b>Targets:</b> {TargetText(w.targeting)}\n<b>Tags:</b> {Tags(w.tags)}\n");
            if (w.IsBase)
            {
                var evo = db.EvolutionFor(w);
                if (evo != null) sb.Append($"\n<color=#FFD54A><b>Evolves:</b> max level + {evo.item.displayName} -> {evo.result.displayName}</color>");
                foreach (var f in db.FusionsUsing(w))
                    sb.Append($"\n<color=#C99BFF><b>Fuses:</b> max level + {f.Partner(w).displayName} (max) -> {f.result.displayName}</color>");
            }
            else if (w.form == WeaponForm.Evolved)
            {
                var r = db.EvolutionProducing(w);
                if (r != null) sb.Append($"\n<color=#FFD54A><b>Recipe:</b> {r.weapon.displayName} (Lv {r.weapon.MaxLevel}) + {r.item.displayName}</color>");
            }
            else
            {
                var r = db.FusionProducing(w);
                if (r != null) sb.Append($"\n<color=#C99BFF><b>Recipe:</b> {r.a.displayName} (Lv {r.a.MaxLevel}) + {r.b.displayName} (Lv {r.b.MaxLevel}), frees a slot</color>");
            }
            return sb.ToString();
        }

        public static string LevelTable(WeaponDefinition w)
        {
            var sb = new StringBuilder("\n\n<b>Levels</b>\n");
            bool chain = w.archetype == WeaponArchetype.Chain;
            for (int i = 0; i < w.MaxLevel; i++)
            {
                var l = w.levels[i];
                sb.Append(w.MaxLevel > 1 ? $"<b>Lv {i + 1}</b>  " : "");
                sb.Append($"Dmg {l.damage:0.#} · CD {l.cooldown:0.##}s · Amt {l.amount} · Area {l.area:0.##}");
                if (l.pierce > 0) sb.Append(chain ? $" · Chains +{l.pierce}" : $" · Pierce {(l.pierce >= 99 ? "inf" : l.pierce.ToString())}");
                if (!string.IsNullOrEmpty(l.note) && w.MaxLevel > 1) sb.Append($"\n   <color=#B8B0D0>{l.note}</color>");
                sb.Append('\n');
            }
            return sb.ToString();
        }

        public static string ItemText(GameDatabase db, ItemDefinition it)
        {
            var sb = new StringBuilder();
            sb.Append(it.description).Append("\n\n<b>Per level</b>\n");
            for (int i = 1; i <= it.MaxLevel; i++) sb.Append($"Lv {i}: {it.LevelText(i)}\n");
            foreach (var r in db.EvolutionsUsing(it))
                sb.Append($"\n<color=#FFD54A><b>Evolves</b> {r.weapon.displayName} (max level) -> {r.result.displayName}</color>");
            if (it.general) sb.Append("\n<color=#B8B0D0>Always in the level-up pool.</color>");
            else
            {
                var w = db.WeaponForItem(it);
                if (w != null) sb.Append($"\n<color=#B8B0D0>Unlocks together with {w.displayName}.</color>");
            }
            return sb.ToString();
        }

        public static string BehaviourText(EnemyBehaviour b) => b switch
        {
            EnemyBehaviour.Chaser => "Walks straight at you.",
            EnemyBehaviour.Rusher => "Fast, wiggly rusher.",
            EnemyBehaviour.Tank => "Slow and very tough.",
            EnemyBehaviour.Shooter => "Keeps its distance and shoots.",
            EnemyBehaviour.Charger => "Flashes, then charges in a straight line.",
            EnemyBehaviour.Splitter => "Splits into smaller enemies when defeated.",
            EnemyBehaviour.Swarm => "Comes in huge packs.",
            EnemyBehaviour.Summoner => "Hangs back and summons friends.",
            EnemyBehaviour.Exploder => "Runs up, fizzes and explodes.",
            EnemyBehaviour.Orbiter => "Circles around you, closing in.",
            EnemyBehaviour.Hopper => "Moves in big hops.",
            EnemyBehaviour.Ambusher => "Lurks unseen, then pops up when close.",
            EnemyBehaviour.Bomber => "Drops telegraphed blasts where you are heading.",
            _ => "Boss patterns.",
        };

        public static string PatternText(BossPattern p) => p switch
        {
            BossPattern.RadialBurst => "rings of bullets",
            BossPattern.AimedSpread => "aimed bullet fans",
            BossPattern.Charge => "telegraphed charge",
            BossPattern.Summon => "summons minions",
            BossPattern.Slam => "ground slams (red circles)",
            BossPattern.Spiral => "bullet spiral",
            _ => "hazard pools",
        };

        public static string EnemyText(GameDatabase db, EnemyDefinition e)
        {
            var sb = new StringBuilder();
            sb.Append(e.description).Append("\n\n");
            sb.Append($"<b>Tier:</b> {(e.tier == EnemyTier.Normal ? "Normal" : e.tier == EnemyTier.MiniBoss ? "Mini boss" : "Boss")}\n");
            sb.Append($"<b>HP</b> {e.maxHp:0} · <b>Damage</b> {e.damage:0} · <b>Speed</b> {e.speed:0.#}\n");
            if (e.armor > 0) sb.Append($"<b>Armor:</b> takes {(1 - e.armor) * 100:0}% damage\n");
            if (e.tier == EnemyTier.Normal) sb.Append($"<b>Behaviour:</b> {BehaviourText(e.behaviour)}\n");
            if (e.patterns != null && e.patterns.Length > 0)
            {
                sb.Append("<b>Attacks:</b> ");
                for (int i = 0; i < e.patterns.Length; i++) sb.Append(i > 0 ? ", " : "").Append(PatternText(e.patterns[i]));
                sb.Append('\n');
            }
            var b = db.BiomeOf(e);
            if (b != null) sb.Append($"<b>Found in:</b> {b.displayName}\n");
            sb.Append("\nEnemies grow stronger every minute and in later biomes. Elites glow gold and drop a chest.");
            return sb.ToString();
        }
    }
}
