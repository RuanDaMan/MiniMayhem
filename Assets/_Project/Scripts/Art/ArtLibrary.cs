using UnityEngine;

namespace MiniMayhem
{
    /// <summary>
    /// Base materials (so the needed shaders are always included in builds). Lives in a Resources folder so
    /// procedural visual code can reach it from anywhere.
    /// </summary>
    [CreateAssetMenu(menuName = "Mini Mayhem/Art Library", fileName = "MiniMayhemArt")]
    public class ArtLibrary : ScriptableObject
    {
        public const string ResourcePath = "MiniMayhemArt";

        public Material spriteMaterial;
        public Material additiveMaterial;
        public Material lineMaterial;

        static ArtLibrary instance;

        public static void OverrideInstance(ArtLibrary lib) => instance = lib;

        public static ArtLibrary Instance
        {
            get
            {
                if (instance == null) instance = Resources.Load<ArtLibrary>(ResourcePath);
                return instance;
            }
        }
    }
}
