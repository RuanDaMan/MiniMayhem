using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MiniMayhem.EditorTools
{
    /// <summary>Editor menu: rebuild generated assets, open the scene, build the Windows player.</summary>
    public static class MiniMayhemMenu
    {
        [MenuItem("Mini Mayhem/Rebuild Project Assets")]
        public static void Rebuild() => ProjectBuilder.BuildAll();

        [MenuItem("Mini Mayhem/Open Game Scene")]
        public static void OpenScene()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ProjectBuilder.ScenePath);
        }

        [MenuItem("Mini Mayhem/Delete Save File")]
        public static void DeleteSave()
        {
            var p = SaveService.SavePath;
            if (System.IO.File.Exists(p)) System.IO.File.Delete(p);
            Debug.Log($"[MiniMayhem] Deleted {p}");
        }

        [MenuItem("Mini Mayhem/Build Windows Player")]
        public static void BuildWindowsPlayer()
        {
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { ProjectBuilder.ScenePath },
                locationPathName = "Builds/Windows/MiniMayhem.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(opts);
            Debug.Log($"[MiniMayhem] Player build: {report.summary.result}, {report.summary.totalSize / (1024 * 1024)} MB");
        }
    }

    /// <summary>Opens the game scene when the project is opened with an empty scene.</summary>
    [InitializeOnLoad]
    static class OpenSceneOnLoad
    {
        static OpenSceneOnLoad()
        {
            EditorApplication.delayCall += () =>
            {
                if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode) return;
                var s = EditorSceneManager.GetActiveScene();
                if (string.IsNullOrEmpty(s.path) && System.IO.File.Exists(ProjectBuilder.ScenePath)) EditorSceneManager.OpenScene(ProjectBuilder.ScenePath);
            };
        }
    }
}
