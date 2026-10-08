using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MiniMayhem
{
    [Serializable]
    public class ItemLevel
    {
        [Tooltip("Modifiers gained when reaching this level (they stack with the earlier levels).")]
        public List<StatMod> mods = new();
    }

    /// <summary>Passive item. Each level adds its modifiers on top of the previous levels.</summary>
    [CreateAssetMenu(menuName = "Mini Mayhem/Item", fileName = "Item")]
    public class ItemDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea(2, 4)] public string description;
        public ArtId icon;
        public Color color = Color.white;
        [Tooltip("General items are always available. Evolution items unlock together with their weapon.")]
        public bool general;
        public ItemLevel[] levels = new ItemLevel[5];

        public int MaxLevel => levels != null ? levels.Length : 0;

        /// <summary>Total modifiers at a level (sum of levels 1..level).</summary>
        public void Accumulate(int level, StatBlock into)
        {
            for (int i = 0; i < Mathf.Min(level, MaxLevel); i++)
                into.Add(levels[i].mods);
        }

        public string LevelText(int level)
        {
            if (level < 1 || level > MaxLevel) return "";
            var sb = new StringBuilder();
            foreach (var m in levels[level - 1].mods)
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(m.ToString());
            }
            return sb.ToString();
        }
    }
}
