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

        /// <summary>
        /// Load an asset or create it. init runs only on creation unless overwrite is set, so hand-tuned values
        /// survive rebuilds.
        /// </summary>
        public static T LoadOrCreate<T>(string path, System.Action<T> init, bool overwrite = false) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a != null)
            {
                if (overwrite) { init?.Invoke(a); EditorUtility.SetDirty(a); }
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
