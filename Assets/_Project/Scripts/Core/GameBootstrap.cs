using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace MiniMayhem
{
    /// <summary>
    /// First thing to run in the scene: loads the save, builds the sprite atlas, creates audio, input, camera rig
    /// and the GameFlow (which owns all screens and runs).
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] GameDatabase database;
        [Tooltip("Load/save progress (tests point the save at a temp file).")]
        [SerializeField] bool enableSaving = true;

        public GameDatabase Database { get => database; set => database = value; }
        public SaveService Save { get; private set; }
        public MetaService Meta { get; private set; }
        public Controls Controls { get; private set; }
        public GameFlow Flow { get; private set; }

        void Awake()
        {
            if (database == null) { Debug.LogError("[MiniMayhem] GameBootstrap: missing GameDatabase", this); return; }
            Application.targetFrameRate = 120;
            GameServices.Register(database);
            GameServices.Register(database.config);

            Art.Build(database);

            Save = new SaveService { Enabled = enableSaving };
            if (enableSaving) Save.Load();
            GameServices.Register(Save);
            Meta = new MetaService(database, Save);
            GameServices.Register(Meta);

            Controls = new Controls();
            GameServices.Register(Controls);

            var audioGo = new GameObject("Audio");
            audioGo.transform.SetParent(transform, false);
            var audio = audioGo.AddComponent<AudioService>();

            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }
            var rig = new CameraRig(cam, database.config);
            GameServices.Register(rig);

            EnsureEventSystem();
            ApplyDisplay(Meta.Data.settings);

            var flowGo = new GameObject("Flow");
            flowGo.transform.SetParent(transform, false);
            Flow = flowGo.AddComponent<GameFlow>();
            Flow.Init(database, Meta, Controls, rig, audio);
        }

        void OnDestroy()
        {
            if (database == null) return;
            Controls?.Dispose();
            GameServices.Unregister(database);
            GameServices.Unregister(database.config);
            GameServices.Unregister(Save);
            GameServices.Unregister(Meta);
            GameServices.Unregister(Controls);
        }

        void OnApplicationQuit()
        {
            if (enableSaving) Save?.Save();
        }

        void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var go = new GameObject("EventSystem");
            go.transform.SetParent(transform, false);
            go.AddComponent<EventSystem>();
            var module = go.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }

        public static void ApplyDisplay(SettingsData s)
        {
            if (Application.isEditor) return;
            var res = Screen.resolutions;
            if (s.resolutionIndex >= 0 && s.resolutionIndex < res.Length)
                Screen.SetResolution(res[s.resolutionIndex].width, res[s.resolutionIndex].height, s.fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
            else Screen.fullScreenMode = s.fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        }
    }
}
