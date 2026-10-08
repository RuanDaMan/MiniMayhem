using System.Collections.Generic;
using UnityEngine;

namespace MiniMayhem
{
    /// <summary>Plays synthesised sound effects (pooled voices, per-sound rate limiting) and the music loop.</summary>
    public class AudioService : MonoBehaviour
    {
        [SerializeField] int voices = 24;

        static readonly Dictionary<SfxId, AudioClip> clips = new();
        static readonly Dictionary<string, AudioClip> music = new();
        readonly Dictionary<SfxId, float> lastPlayed = new();
        readonly List<AudioSource> pool = new();
        AudioSource musicSource;
        int next;

        public float Master { get; set; } = 0.8f;
        public float MusicVolume { get; set; } = 0.6f;
        public float SfxVolume { get; set; } = 0.8f;
        public string CurrentMusic { get; private set; }

        void Awake()
        {
            GameServices.Register(this);
            foreach (SfxId id in System.Enum.GetValues(typeof(SfxId)))
                if (!clips.ContainsKey(id)) clips[id] = SfxSynth.Build(id);
            for (int i = 0; i < voices; i++)
            {
                var a = gameObject.AddComponent<AudioSource>();
                a.playOnAwake = false;
                a.spatialBlend = 0f;
                pool.Add(a);
            }
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.spatialBlend = 0f;
        }

        void OnDestroy() => GameServices.Unregister(this);

        void Update()
        {
            if (musicSource != null) musicSource.volume = Master * MusicVolume * 0.55f;
        }

        public void Play(SfxId id, float volume = 1f, float pitch = 1f, float pitchJitter = 0.07f, float minInterval = 0.035f)
        {
            if (lastPlayed.TryGetValue(id, out float t) && Time.unscaledTime - t < minInterval) return;
            lastPlayed[id] = Time.unscaledTime;
            var src = NextVoice();
            src.clip = clips[id];
            src.volume = volume * Master * SfxVolume;
            src.pitch = pitch * (1f + Random.Range(-pitchJitter, pitchJitter));
            src.Play();
        }

        AudioSource NextVoice()
        {
            for (int i = 0; i < pool.Count; i++)
            {
                var s = pool[(next + i) % pool.Count];
                if (!s.isPlaying) { next = (next + i + 1) % pool.Count; return s; }
            }
            var steal = pool[next];
            next = (next + 1) % pool.Count;
            return steal;
        }

        public void PlayMusic(string key, int root, bool minor, float bpm, int seed, float intensity = 1f)
        {
            if (CurrentMusic == key && musicSource.isPlaying) return;
            if (!music.TryGetValue(key, out var clip))
            {
                clip = MusicSynth.Build(key, root, minor, bpm, seed, intensity);
                music[key] = clip;
            }
            CurrentMusic = key;
            musicSource.clip = clip;
            musicSource.Play();
        }

        public void PlayBiomeMusic(BiomeDefinition b) =>
            PlayMusic("biome_" + b.id, b.musicRoot, b.musicMinor, b.musicTempo, b.musicSeed);

        public void PlayTitleMusic() => PlayMusic("title", 60, false, 104f, 3, 0.55f);

        public void StopMusic()
        {
            CurrentMusic = null;
            if (musicSource != null) musicSource.Stop();
        }
    }

    /// <summary>Static helpers so gameplay code can fire sounds without caring whether the service exists.</summary>
    public static class Sfx
    {
        public static void Play(SfxId id, float volume = 1f, float pitch = 1f, float minInterval = 0.035f) =>
            GameServices.Get<AudioService>()?.Play(id, volume, pitch, 0.07f, minInterval);
    }
}
