using UnityEditor;
using UnityEngine;

namespace MiniMayhem.EditorTools
{
    internal static class BuilderUtil
    {
        public const string ProjectRoot = "Assets/_Project";
        public const string SoRoot = ProjectRoot + "/ScriptableObjects";
        public const string SceneRoot = ProjectRoot + "/Scenes";

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int i = path.LastIndexOf('/');
            string parent = path.Substring(0, i), leaf = path.Substring(i + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        /// <summary>When set, existing data assets are reset to the values in code (Mini Mayhem > Reset Data To Defaults).</summary>
        public static bool OverwriteData;

        /// <summary>
        /// Load an asset or create it. init runs only on creation unless overwrite is set, so hand-tuned values
        /// survive rebuilds.
        /// </summary>

        public static T LoadOrCreate<T>(string path, System.Action<T> init, bool overwrite = false) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a != null)
            {
                if (overwrite || OverwriteData)
                {
                    if (init != null) init(a);
                    else
                    {
                        // Reset to the class defaults.
                        var fresh = ScriptableObject.CreateInstance<T>();
                        string name = a.name;
                        EditorUtility.CopySerialized(fresh, a);
                        a.name = name;
                        Object.DestroyImmediate(fresh);
                    }
                    EditorUtility.SetDirty(a);
                }
                return a;
            }
            EnsureFolder(path.Substring(0, path.LastIndexOf('/')));
            a = ScriptableObject.CreateInstance<T>();
            init?.Invoke(a);
            AssetDatabase.CreateAsset(a, path);
            return a;
        }

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }
    }
}
