using UnityEngine;

namespace MiniMayhem
{
    /// <summary>Weapon (max level) + item (any level) -> evolved weapon.</summary>
    [CreateAssetMenu(menuName = "Mini Mayhem/Evolution Recipe", fileName = "Evolution")]
    public class EvolutionRecipe : ScriptableObject
    {
        public WeaponDefinition weapon;
        public ItemDefinition item;
        public WeaponDefinition result;
    }
}
