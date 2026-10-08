using System.Collections.Generic;
using UnityEngine;

namespace MiniMayhem
{
    /// <summary>The root of all game data. Everything (runs, codex, skill tree) reads from here.</summary>
    [CreateAssetMenu(menuName = "Mini Mayhem/Game Database", fileName = "GameDatabase")]
    public class GameDatabase : ScriptableObject
    {
        public GameConfig config;
        public List<WeaponDefinition> weapons = new();
        public List<ItemDefinition> items = new();
        public List<EvolutionRecipe> evolutions = new();
        public List<FusionRecipe> fusions = new();
        public List<EnemyDefinition> enemies = new();
        public List<BiomeDefinition> biomes = new();
        public List<SkillNodeDefinition> skillNodes = new();

        Dictionary<string, WeaponDefinition> weaponById;
        Dictionary<string, ItemDefinition> itemById;
        Dictionary<string, SkillNodeDefinition> nodeById;
        Dictionary<string, EnemyDefinition> enemyById;

        void OnEnable() { weaponById = null; itemById = null; nodeById = null; enemyById = null; }

        public IEnumerable<WeaponDefinition> BaseWeapons
        {
            get { foreach (var w in weapons) if (w != null && w.IsBase) yield return w; }
        }

        public WeaponDefinition Weapon(string id)
        {
            if (weaponById == null) { weaponById = new(); foreach (var w in weapons) if (w != null) weaponById[w.id] = w; }
            return id != null && weaponById.TryGetValue(id, out var r) ? r : null;
        }

        public ItemDefinition Item(string id)
        {
            if (itemById == null) { itemById = new(); foreach (var i in items) if (i != null) itemById[i.id] = i; }
            return id != null && itemById.TryGetValue(id, out var r) ? r : null;
        }

        public SkillNodeDefinition Node(string id)
        {
            if (nodeById == null) { nodeById = new(); foreach (var n in skillNodes) if (n != null) nodeById[n.id] = n; }
            return id != null && nodeById.TryGetValue(id, out var r) ? r : null;
        }

        public EnemyDefinition Enemy(string id)
        {
            if (enemyById == null) { enemyById = new(); foreach (var e in enemies) if (e != null) enemyById[e.id] = e; }
            return id != null && enemyById.TryGetValue(id, out var r) ? r : null;
        }

        public EvolutionRecipe EvolutionFor(WeaponDefinition w)
        {
            foreach (var r in evolutions) if (r != null && r.weapon == w) return r;
            return null;
        }

        /// <summary>The recipe that produces this evolved weapon (for the codex).</summary>
        public EvolutionRecipe EvolutionProducing(WeaponDefinition result)
        {
            foreach (var r in evolutions) if (r != null && r.result == result) return r;
            return null;
        }

        public FusionRecipe FusionProducing(WeaponDefinition result)
        {
            foreach (var r in fusions) if (r != null && r.result == result) return r;
            return null;
        }

        public IEnumerable<FusionRecipe> FusionsUsing(WeaponDefinition w)
        {
            foreach (var r in fusions) if (r != null && r.Uses(w)) yield return r;
        }

        public IEnumerable<EvolutionRecipe> EvolutionsUsing(ItemDefinition item)
        {
            foreach (var r in evolutions) if (r != null && r.item == item) yield return r;
        }

        /// <summary>The weapon that unlocks this evolution item (null for general items).</summary>
        public WeaponDefinition WeaponForItem(ItemDefinition item)
        {
            foreach (var r in evolutions) if (r != null && r.item == item) return r.weapon;
            return null;
        }

        public BiomeDefinition BiomeOf(EnemyDefinition e)
        {
            foreach (var b in biomes)
            {
                if (b == null) continue;
                if (b.boss == e) return b;
                foreach (var n in b.normals) if (n == e) return b;
                foreach (var n in b.miniBosses) if (n == e) return b;
            }
            return null;
        }
    }
}
