using UnityEngine;

namespace MiniMayhem
{
    /// <summary>Two specific max-level, non-evolved weapons -> one fused weapon (frees a slot).</summary>
    [CreateAssetMenu(menuName = "Mini Mayhem/Fusion Recipe", fileName = "Fusion")]
    public class FusionRecipe : ScriptableObject
    {
        public WeaponDefinition a;
        public WeaponDefinition b;
        public WeaponDefinition result;

        public bool Uses(WeaponDefinition w) => w == a || w == b;
        public WeaponDefinition Partner(WeaponDefinition w) => w == a ? b : w == b ? a : null;
    }
}
