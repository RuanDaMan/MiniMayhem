using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static MiniMayhem.EditorTools.BuilderUtil;

namespace MiniMayhem.EditorTools
{
    /// <summary>
    /// One-click (or batch-mode) generator: materials, the art library, all ScriptableObject data (created only
    /// when missing, so tuning survives) and the game scene (regenerated every time).
    /// Batch: Unity -batchmode -quit -projectPath . -executeMethod MiniMayhem.EditorTools.ProjectBuilder.BuildAll
    /// </summary>
    public static class ProjectBuilder
    {
        public const string ScenePath = SceneRoot + "/MiniMayhem.unity";
        public const string DatabasePath = SoRoot + "/GameDatabase.asset";
        const string ArtLibPath = ProjectRoot + "/Resources/" + ArtLibrary.ResourcePath + ".asset";
        const string MatDir = ProjectRoot + "/Art";

        public static void BuildAll()
        {
            ImportTmpEssentials();
            EnsureArtLibrary();
            var db = BuildData();
            AssetDatabase.SaveAssets();
            BuildScene(db);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[MiniMayhem] Rebuild Project Assets complete.");
        }

        /// <summary>Like BuildAll, but resets every data asset to the values in code (loses Inspector tuning).</summary>
        public static void ResetDataToDefaults()
        {
            OverwriteData = true;
            try { BuildAll(); }
            finally { OverwriteData = false; }
        }

        public static GameDatabase BuildData()
        {
            var cfg = LoadOrCreate<GameConfig>(SoRoot + "/GameConfig.asset", null);
            bool keep = OverwriteData;
            OverwriteData = false;
            var db = LoadOrCreate<GameDatabase>(DatabasePath, null);
            OverwriteData = keep;
            db.config = cfg;
            WeaponData.Build(db);
            WorldData.Build(db);
            SkillData.Build(db);
            EditorUtility.SetDirty(db);
            return db;
        }

        static void ImportTmpEssentials()
        {
            if (AssetDatabase.IsValidFolder("Assets/TextMesh Pro")) return;
            var full = System.IO.Directory.GetFiles(System.IO.Path.GetFullPath("Library/PackageCache"), "TMP Essential Resources.unitypackage", System.IO.SearchOption.AllDirectories);
            if (full.Length == 0) { Debug.LogWarning("[MiniMayhem] TMP Essential Resources package not found."); return; }
            AssetDatabase.ImportPackage(full[0], false);
            AssetDatabase.Refresh();
        }

        static void EnsureArtLibrary()
        {
            var lib = LoadOrCreate<ArtLibrary>(ArtLibPath, null);
            EnsureFolder(MatDir);
            lib.spriteMaterial = Mat("Sprite", new[] { "Universal Render Pipeline/2D/Sprite-Unlit-Default", "Sprites/Default" });
            lib.additiveMaterial = Mat("SpriteAdditive", new[] { "Universal Render Pipeline/Particles/Unlit", "Sprites/Default" });
            lib.lineMaterial = lib.spriteMaterial;
            EditorUtility.SetDirty(lib);
            ArtLibrary.OverrideInstance(lib);
        }

        static Material Mat(string name, string[] shaders)
        {
            string path = $"{MatDir}/{name}.mat";
            Shader sh = null;
            foreach (var s in shaders) { sh = Shader.Find(s); if (sh != null) break; }
            if (sh == null) { Debug.LogError($"[MiniMayhem] Shader not found: {shaders[0]}"); sh = Shader.Find("Sprites/Default"); }
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(sh) { name = name }; AssetDatabase.CreateAsset(m, path); }
            m.shader = sh;
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        static void BuildScene(GameDatabase db)
        {
            EnsureFolder(SceneRoot);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = db.config.cameraSize;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.16f, 0.13f, 0.22f);
            cam.transform.position = new Vector3(0, 0, -10);
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 50f;
            camGo.AddComponent<AudioListener>();
            var urp = camGo.AddComponent<UniversalAdditionalCameraData>();
            urp.renderPostProcessing = false;
            urp.antialiasing = AntialiasingMode.None;

            var game = new GameObject("Game");
            var boot = game.AddComponent<GameBootstrap>();
            db = AssetDatabase.LoadAssetAtPath<GameDatabase>(DatabasePath);
            boot.Database = db;
            EditorUtility.SetDirty(boot);
            EditorSceneManager.MarkSceneDirty(scene);
            if (boot.Database == null) Debug.LogError("[MiniMayhem] Scene build: GameDatabase reference is null");

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }
    }
}
