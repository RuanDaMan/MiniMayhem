using UnityEngine;

namespace MiniMayhem
{
    public enum SkillBranch { Core, Vitality, Might, Swift, Fortune }

    /// <summary>
    /// One node of the meta skill tree. A node can be bought once any prerequisite has at least one rank
    /// (the root has none). cost(rank) = baseCost * costGrowth^rank.
    /// </summary>
    [CreateAssetMenu(menuName = "Mini Mayhem/Skill Node", fileName = "Skill")]
    public class SkillNodeDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea(2, 3)] public string description;
        public SkillBranch branch;
        [Tooltip("Position in the tree view (units of ~1 node).")]
        public Vector2 position;
        public int maxRank = 5;
        public float baseCost = 50f;
        public float costGrowth = 1.6f;
        public SkillNodeDefinition[] prerequisites = new SkillNodeDefinition[0];
        [Tooltip("Modifier gained per rank.")]
        public StatMod perRank;

        public int Cost(int currentRank) => Mathf.RoundToInt(baseCost * Mathf.Pow(costGrowth, currentRank));
    }
}
