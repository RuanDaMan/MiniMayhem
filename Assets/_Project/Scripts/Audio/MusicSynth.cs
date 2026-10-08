using UnityEngine;

namespace MiniMayhem
{
    /// <summary>
    /// Builds a seamless 8-bar chiptune loop from a key, tempo and seed: bass, arpeggio, a simple
    /// melody and a noise drum kit. Each biome gets its own flavour from its music settings.
    /// </summary>
    public static class MusicSynth
    {
        const int Rate = SfxSynth.Rate;

        static readonly int[] Major = { 0, 2, 4, 5, 7, 9, 11 };
        static readonly int[] Minor = { 0, 2, 3, 5, 7, 8, 10 };
        static readonly int[] ProgMajor = { 0, 4, 5, 3, 0, 4, 3, 4 }; // I V vi IV ...
        static readonly int[] ProgMinor = { 0, 5, 2, 6, 0, 5, 3, 4 }; // i VI III VII ...

        static float Midi(int n) => 440f * Mathf.Pow(2f, (n - 69) / 12f);

        public static AudioClip Build(string name, int root, bool minor, float bpm, int seed, float intensity = 1f)
        {
            var rnd = new System.Random(seed);
            int[] scale = minor ? Minor : Major;
            int[] prog = minor ? ProgMinor : ProgMajor;
            float beat = 60f / bpm;
            int bars = 8;
            int total = Mathf.RoundToInt(bars * 4 * beat * Rate);
            var s = new float[total];

            int Degree(int deg, int octave = 0)
            {
                int o = Mathf.FloorToInt(deg / 7f);
                int d = ((deg % 7) + 7) % 7;
                return root + scale[d] + 12 * (o + octave);
            }

            // Melody: one note per beat-ish, random walk over chord tones.
            int[] melody = new int[bars * 8];
            int cur = 4;
            for (int i = 0; i < melody.Length; i++)
            {
                if (rnd.NextDouble() < 0.25) { melody[i] = -999; continue; }
                cur += rnd.Next(-2, 3);
                cur = Mathf.Clamp(cur, 0, 9);
                melody[i] = cur;
            }

            for (int bar = 0; bar < bars; bar++)
            {
                int chord = prog[bar % prog.Length];
                for (int eighth = 0; eighth < 8; eighth++)
                {
                    int start = Mathf.RoundToInt((bar * 4 + eighth * 0.5f) * beat * Rate);
                    int len = Mathf.RoundToInt(0.5f * beat * Rate);
                    // Bass: root / fifth on eighths.
                    int bassNote = Degree(chord + (eighth % 4 == 2 ? 4 : 0), -2);
                    AddNote(s, start, len, Midi(bassNote), 0.22f * intensity, SfxSynth.Wave.Triangle, 6f);
                    // Arpeggio: chord tones cycling, in sixteenths.
                    for (int h = 0; h < 2; h++)
                    {
                        int step = (eighth * 2 + h) % 3;
                        int an = Degree(chord + step * 2, 1);
                        AddNote(s, start + h * len / 2, len / 2, Midi(an), 0.05f * intensity, SfxSynth.Wave.Square, 18f);
                    }
                    // Melody.
                    int m = melody[bar * 8 + eighth];
                    if (m > -999 && eighth % 2 == 0)
                        AddNote(s, start, len * 2, Midi(Degree(chord + m, 1)), 0.09f, SfxSynth.Wave.Square, 4f, vibrato: true);
                    // Drums.
                    if (eighth % 4 == 0) AddKick(s, start);
                    if (eighth % 4 == 2 && intensity > 0.5f) AddNoise(s, start, 0.12f, 0.16f, 3500, rnd);
                    AddNoise(s, start, 0.03f, 0.04f * intensity, 9000, rnd);
                }
            }
            SfxSynth.Normalize(s, 0.7f);
            // Soften the loop seam.
            int fade = Rate / 200;
            for (int i = 0; i < fade; i++) { float k = i / (float)fade; s[i] *= k; s[total - 1 - i] *= k; }
            var clip = AudioClip.Create(name, total, 1, Rate, false);
            clip.SetData(s, 0);
            return clip;
        }

        static void AddNote(float[] s, int start, int len, float freq, float amp, SfxSynth.Wave w, float decay, bool vibrato = false)
        {
            double ph = 0;
            for (int i = 0; i < len && start + i < s.Length; i++)
            {
                float t = i / (float)Rate;
                float f = vibrato ? freq * (1f + Mathf.Sin(t * 30f) * 0.006f) : freq;
                ph += f / Rate;
                float env = Mathf.Exp(-t * decay) * Mathf.Clamp01(i / 80f) * Mathf.Clamp01((len - i) / 200f);
                s[start + i] += SfxSynth.Osc(w, ph) * amp * env;
            }
        }

        static void AddKick(float[] s, int start)
        {
            double ph = 0;
            int len = Rate / 7;
            for (int i = 0; i < len && start + i < s.Length; i++)
            {
                float t = i / (float)Rate;
                ph += Mathf.Lerp(150f, 45f, t * 7f) / Rate;
                s[start + i] += Mathf.Sin((float)(ph * Mathf.PI * 2)) * 0.35f * Mathf.Exp(-t * 18f);
            }
        }

        static void AddNoise(float[] s, int start, float sec, float amp, float cutoff, System.Random r)
        {
            int len = (int)(sec * Rate);
            float lp = 0, a = 1f - Mathf.Exp(-2f * Mathf.PI * cutoff / Rate);
            for (int i = 0; i < len && start + i < s.Length; i++)
            {
                float n = (float)(r.NextDouble() * 2 - 1);
                lp += (n - lp) * a;
                s[start + i] += (n - lp) * amp * (1f - i / (float)len);
            }
        }
    }
}
