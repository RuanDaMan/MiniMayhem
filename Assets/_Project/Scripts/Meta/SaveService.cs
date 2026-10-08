using System;
using System.IO;
using UnityEngine;

namespace MiniMayhem
{
    /// <summary>
    /// JSON save in Application.persistentDataPath (LocalLow on Windows). Written atomically (temp file + move).
    /// </summary>
    public class SaveService
    {
        public const string FileName = "minimayhem_save.json";

        /// <summary>Tests point this at a temp file so they never touch a real save.</summary>
        public static string PathOverride;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => PathOverride = null;

        public static string SavePath => PathOverride ?? Path.Combine(Application.persistentDataPath, FileName);

        public SaveData Data { get; private set; } = new();
        /// <summary>When false, Save() does nothing (throwaway sessions).</summary>
        public bool Enabled { get; set; } = true;
        public bool LoadedFromDisk { get; private set; }

        public event Action<SaveData> Saving;

        public SaveData Load()
        {
            LoadedFromDisk = false;
            var data = new SaveData();
            try
            {
                if (File.Exists(SavePath))
                {
                    data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath)) ?? new SaveData();
                    LoadedFromDisk = true;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MiniMayhem] Could not read save ({e.Message}); starting fresh.");
                data = new SaveData();
            }
            data.stats ??= new RunStats();
            data.settings ??= new SettingsData();
            Data = data;
            return data;
        }

        public void Save()
        {
            Saving?.Invoke(Data);
            if (!Enabled) return;
            try
            {
                string tmp = SavePath + ".tmp";
                File.WriteAllText(tmp, JsonUtility.ToJson(Data, true));
                if (File.Exists(SavePath)) File.Delete(SavePath);
                File.Move(tmp, SavePath);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MiniMayhem] Save failed: {e.Message}");
            }
        }

        public void Wipe()
        {
            try { if (File.Exists(SavePath)) File.Delete(SavePath); } catch { /* ignored */ }
            var settings = Data.settings;
            Data = new SaveData { settings = settings ?? new SettingsData() };
        }
    }
}
