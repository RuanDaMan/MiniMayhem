using System.Collections.Generic;
using UnityEngine;
using static MiniMayhem.EditorTools.BuilderUtil;

namespace MiniMayhem.EditorTools
{
    /// <summary>The meta skill tree (GAME_PLAN 15 / 22.7): Mayhem Core in the middle, four branches around it.</summary>
    internal static class SkillData
    {
        const string Dir = SoRoot + "/Skills";

        static SkillNodeDefinition N(string id, string name, string desc, SkillBranch branch, float x, float y, StatId stat, float perRank, int maxRank,
            float baseCost, float growth, params SkillNodeDefinition[] prereq) =>
            LoadOrCreate<SkillNodeDefinition>($"{Dir}/{id}.asset", n =>
            {
                n.id = id; n.displayName = name; n.description = desc; n.branch = branch; n.position = new Vector2(x, y);
                n.perRank = new StatMod(stat, perRank); n.maxRank = maxRank; n.baseCost = baseCost; n.costGrowth = growth; n.prerequisites = prereq;
            });

        public static void Build(GameDatabase db)
        {
            const SkillBranch V = SkillBranch.Vitality, M = SkillBranch.Might, S = SkillBranch.Swift, F = SkillBranch.Fortune;
            var core = N("core", "Mayhem Core", "Where it all starts. Unlocks the four branches.", SkillBranch.Core, 0, 0, StatId.MaxHp, 10, 1, 15, 1f);

            // Vitality (up)
            var hp = N("v_hp", "Tough Cookie", "More max HP.", V, 0, 1.1f, StatId.MaxHp, 10, 5, 40, 1.45f, core);
            var regen = N("v_regen", "Band-Aids", "Regenerate HP over time.", V, -0.9f, 2.1f, StatId.Regen, 0.15f, 5, 60, 1.5f, hp);
            var armor = N("v_armor", "Thick Hoodie", "Shrug off a little of every hit.", V, 0.9f, 2.1f, StatId.Armor, 1, 5, 70, 1.55f, hp);
            var revive = N("v_revive", "Second Wind", "Get back up once per run (per rank) at half HP.", V, 0, 3.1f, StatId.Revives, 1, 2, 500, 2.5f, regen, armor);
            var keep = N("v_keep", "Deep Pockets", "Keep more of your gold when you die (50% up to 80%).", V, 0, 4.1f, StatId.DeathKeep, 0.06f, 5, 300, 1.5f, revive);

            // Might (right)
            var dmg = N("m_damage", "Muscles", "More damage for every weapon.", M, 1.1f, 0, StatId.Damage, 0.05f, 5, 50, 1.5f, core);
            var aspd = N("m_speed", "Sugar Rush", "Weapons attack more often.", M, 2.1f, 0.9f, StatId.AttackSpeed, 0.04f, 5, 70, 1.5f, dmg);
            var area = N("m_area", "Big Swings", "Bigger areas for everything.", M, 2.1f, -0.9f, StatId.Area, 0.05f, 5, 70, 1.5f, dmg);
            var crit = N("m_crit", "Sharp Eye", "More critical hits.", M, 3.1f, 0.9f, StatId.CritChance, 0.02f, 5, 90, 1.55f, aspd);
            var critDmg = N("m_critdmg", "Bonk!", "Critical hits hit harder.", M, 3.1f, -0.9f, StatId.CritDamage, 0.1f, 5, 90, 1.55f, area);
            N("m_amount", "Double Trouble", "+1 projectile / orbiter / turret / trap for every weapon.", M, 4.1f, 0, StatId.Amount, 1, 1, 1500, 1f, crit, critDmg);

            // Swift (down)
            var move = N("s_move", "Zoomies", "Walk faster.", S, 0, -1.1f, StatId.MoveSpeed, 0.04f, 5, 45, 1.5f, core);
            var magnet = N("s_magnet", "Sticky Fingers", "Bigger pickup radius.", S, -0.9f, -2.1f, StatId.PickupRadius, 0.3f, 5, 50, 1.5f, move);
            var xp = N("s_xp", "Bookworm", "More XP from every gem.", S, 0.9f, -2.1f, StatId.XpGain, 0.05f, 5, 70, 1.55f, move);
            var proj = N("s_proj", "Strong Arm", "Faster projectiles and orbits.", S, 0, -3.1f, StatId.ProjectileSpeed, 0.06f, 5, 80, 1.5f, magnet, xp);
            N("s_duration", "Long Lasting", "Puddles, turrets, beams and traps last longer.", S, 0, -4.1f, StatId.Duration, 0.05f, 5, 100, 1.55f, proj);

            // Fortune (left)
            var gold = N("f_gold", "Pocket Money", "More gold from every coin.", F, -1.1f, 0, StatId.GoldGain, 0.06f, 5, 50, 1.5f, core);
            var luck = N("f_luck", "Four Leaves", "Better level-up cards and more drops.", F, -2.1f, 0.9f, StatId.Luck, 0.04f, 5, 80, 1.55f, gold);
            var reroll = N("f_reroll", "Reroll", "Unlocks rerolling level-up cards (+1 use per rank).", F, -2.1f, -0.9f, StatId.Rerolls, 1, 3, 150, 2f, gold);
            var skip = N("f_skip", "Skip", "Unlocks skipping a level-up for gold and a snack (+1 use per rank).", F, -3.1f, -0.9f, StatId.Skips, 1, 3, 150, 2f, reroll);
            var banish = N("f_banish", "Banish", "Unlocks banishing a card from the run's pool (+1 use per rank).", F, -3.1f, 0.9f, StatId.Banishes, 1, 3, 200, 2f, luck);
            N("f_card", "Fourth Card", "Level-ups offer 4 cards instead of 3.", F, -4.1f, 0, StatId.CardChoices, 1, 1, 2500, 1f, skip, banish);

            var all = new List<SkillNodeDefinition>();
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:SkillNodeDefinition", new[] { Dir }))
                all.Add(UnityEditor.AssetDatabase.LoadAssetAtPath<SkillNodeDefinition>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid)));
            all.Sort((a, b) => a.position.sqrMagnitude.CompareTo(b.position.sqrMagnitude));
            db.skillNodes = all;
        }
    }
}
