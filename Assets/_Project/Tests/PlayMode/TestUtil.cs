using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MiniMayhem.Tests
{
    public static class TestUtil
    {
        public static string ScreenshotDir
        {
            get
            {
                var dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "Screens"));
                Directory.CreateDirectory(dir);
                return dir;
            }
        }

        /// <summary>Render the main camera plus the overlay UI into a PNG under Logs/Screens.</summary>
        public static IEnumerator Capture(string name, int width = 1600, int height = 900)
        {
            yield return null;
            var cam = Camera.main;
            if (cam == null) yield break;
            // Overlay canvases are not rendered by Camera.Render: temporarily switch them to camera space.
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            var modes = new RenderMode[canvases.Length];
            for (int i = 0; i < canvases.Length; i++)
            {
                modes[i] = canvases[i].renderMode;
                if (canvases[i].isRootCanvas && modes[i] == RenderMode.ScreenSpaceOverlay)
                {
                    canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
                    canvases[i].worldCamera = cam;
                    canvases[i].planeDistance = 1f;
                }
            }
            var rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var prevTarget = cam.targetTexture;
            var prevActive = RenderTexture.active;
            cam.targetTexture = rt;
            Canvas.ForceUpdateCanvases();
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            cam.targetTexture = prevTarget;
            RenderTexture.active = prevActive;
            RenderTexture.ReleaseTemporary(rt);
            for (int i = 0; i < canvases.Length; i++) if (canvases[i] != null) canvases[i].renderMode = modes[i];
            File.WriteAllBytes(Path.Combine(ScreenshotDir, name + ".png"), tex.EncodeToPNG());
            Object.Destroy(tex);
        }

        public static IEnumerator LoadScene(string name)
        {
            var op = SceneManager.LoadSceneAsync(name, LoadSceneMode.Single);
            while (!op.isDone) yield return null;
            yield return null;
            yield return null;
        }

        public static IEnumerator WaitSeconds(float s)
        {
            float end = Time.realtimeSinceStartup + s;
            while (Time.realtimeSinceStartup < end) yield return null;
        }

        public static IEnumerator WaitUntil(System.Func<bool> cond, float timeout)
        {
            float end = Time.realtimeSinceStartup + timeout;
            while (!cond() && Time.realtimeSinceStartup < end) yield return null;
        }
    }
}
