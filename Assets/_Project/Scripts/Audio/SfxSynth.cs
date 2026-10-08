using System;
using UnityEngine;

namespace MiniMayhem
{
    public enum SfxId
    {
        Shoot, Hit, Crit, EnemyDie, Gem, Coin, LevelUp, Evolve, Fuse, Hurt, BossRoar, Explosion, Zap, Swing, Quack,
        Splash, Laser, Chest, Heal, Magnet, Click, Select, Error, Purchase, Win, Lose, Warning, Pop, Boing, Whoosh,
        Thunk, EnemyShot, Bomb, Revive, Stink, Spawn,
    }

    /// <summary>
    /// Tiny offline synthesiser: builds every goofy sound effect from oscillators, noise and envelopes.
    /// No audio files needed. All generation is deterministic (seeded) so sounds are stable between runs.
    /// </summary>
    public static class SfxSynth
    {
        public const int Rate = 44100;

        public static AudioClip Build(SfxId id)
        {
            var rnd = new System.Random((int)id * 7919 + 13);
            float[] s = id switch
            {
                SfxId.Shoot => Mix(Sweep(0.09f, 900, 420, Wave.Square, 0.25f, 20f), Noise(rnd, 0.04f, 0.2f, 0.001f, 5000)),
                SfxId.Hit => Mix(Sweep(0.07f, 520, 180, Wave.Sine, 0.8f, 30f), Noise(rnd, 0.03f, 0.35f, 0.0005f, 4000)),
                SfxId.Crit => Mix(Sweep(0.1f, 1200, 300, Wave.Square, 0.35f, 20f), Noise(rnd, 0.05f, 0.5f, 0.0005f, 7000)),
                SfxId.EnemyDie => Mix(Sweep(0.16f, 600, 120, Wave.Square, 0.3f, 12f), Noise(rnd, 0.12f, 0.4f, 0.001f, 2200)),
                SfxId.Gem => Concat(Tone(0.04f, 1568, Wave.Sine, 0.3f, 40f), Tone(0.08f, 2093, Wave.Sine, 0.3f, 25f)),
                SfxId.Coin => Concat(Tone(0.055f, 1320, Wave.Square, 0.22f, 40f), Tone(0.22f, 1760, Wave.Square, 0.22f, 14f)),
                SfxId.LevelUp => Concat(Tone(0.08f, 523, Wave.Square, 0.25f, 12f), Tone(0.08f, 659, Wave.Square, 0.25f, 12f),
                    Tone(0.08f, 784, Wave.Square, 0.25f, 12f), Tone(0.35f, 1046, Wave.Square, 0.25f, 6f)),
                SfxId.Evolve => Mix(Concat(Tone(0.1f, 392, Wave.Square, 0.2f, 8f), Tone(0.1f, 523, Wave.Square, 0.2f, 8f), Tone(0.1f, 659, Wave.Square, 0.2f, 8f),
                    Tone(0.1f, 784, Wave.Square, 0.2f, 8f), Tone(0.5f, 1046, Wave.Square, 0.25f, 4f)), NoiseSweep(rnd, 0.9f, 0.25f, 400, 6000)),
                SfxId.Fuse => Mix(Sweep(0.6f, 200, 1400, Wave.Saw, 0.3f, 2f), Concat(Tone(0.45f, 1, Wave.Sine, 0f, 1f), Tone(0.4f, 1318, Wave.Square, 0.25f, 5f))),
                SfxId.Hurt => Vibrato(0.25f, 330, 150, 18f, 25f, Wave.Square, 0.35f),
                SfxId.BossRoar => Mix(Vibrato(1.1f, 110, 70, 9f, 12f, Wave.Saw, 0.5f), Noise(rnd, 1.0f, 0.4f, 0.05f, 900)),
                SfxId.Explosion => Explosion(rnd, 0.9f),
                SfxId.Zap => Mix(Noise(rnd, 0.14f, 0.6f, 0.0005f, 9000), Sweep(0.14f, 1800, 600, Wave.Square, 0.2f, 10f)),
                SfxId.Swing => NoiseSweep(rnd, 0.16f, 0.6f, 900, 3500),
                SfxId.Quack => Vibrato(0.16f, 520, 380, 35f, 40f, Wave.Saw, 0.3f),
                SfxId.Splash => Mix(Noise(rnd, 0.3f, 0.6f, 0.002f, 2500), Sweep(0.12f, 300, 900, Wave.Sine, 0.3f)),
                SfxId.Laser => Mix(Sweep(0.18f, 1500, 900, Wave.Saw, 0.2f, 6f), Tone(0.18f, 2200, Wave.Sine, 0.1f, 6f)),
                SfxId.Chest => Concat(Tone(0.07f, 659, Wave.Square, 0.25f, 18f), Tone(0.07f, 831, Wave.Square, 0.25f, 18f),
                    Tone(0.07f, 988, Wave.Square, 0.25f, 18f), Tone(0.4f, 1318, Wave.Square, 0.25f, 6f)),
                SfxId.Heal => Concat(Tone(0.08f, 880, Wave.Sine, 0.35f, 10f), Tone(0.16f, 1174, Wave.Sine, 0.35f, 8f)),
                SfxId.Magnet => Sweep(0.5f, 300, 1500, Wave.Sine, 0.35f, 2f),
                SfxId.Click => Tone(0.03f, 1500, Wave.Square, 0.18f, 60f),
                SfxId.Select => Concat(Tone(0.05f, 988, Wave.Square, 0.2f, 30f), Tone(0.1f, 1318, Wave.Square, 0.2f, 20f)),
                SfxId.Error => Concat(Tone(0.12f, 220, Wave.Square, 0.25f, 6f), Tone(0.2f, 165, Wave.Square, 0.25f, 6f)),
                SfxId.Purchase => Concat(Tone(0.07f, 523, Wave.Square, 0.25f, 18f), Tone(0.07f, 659, Wave.Square, 0.25f, 18f),
                    Tone(0.07f, 784, Wave.Square, 0.25f, 18f), Tone(0.25f, 1046, Wave.Square, 0.25f, 9f)),
                SfxId.Win => Concat(Tone(0.12f, 523, Wave.Square, 0.25f, 6f), Tone(0.12f, 659, Wave.Square, 0.25f, 6f), Tone(0.12f, 784, Wave.Square, 0.25f, 6f),
                    Tone(0.12f, 1046, Wave.Square, 0.25f, 6f), Tone(0.12f, 784, Wave.Square, 0.25f, 6f), Tone(0.6f, 1046, Wave.Square, 0.25f, 3f)),
                SfxId.Lose => Concat(Tone(0.2f, 392, Wave.Square, 0.25f, 4f), Tone(0.2f, 330, Wave.Square, 0.25f, 4f), Tone(0.2f, 262, Wave.Square, 0.25f, 4f), Sweep(0.7f, 220, 110, Wave.Square, 0.25f, 2f)),
                SfxId.Warning => Concat(Tone(0.15f, 880, Wave.Square, 0.25f, 3f), Tone(0.1f, 1, Wave.Sine, 0f, 1f), Tone(0.15f, 880, Wave.Square, 0.25f, 3f)),
                SfxId.Pop => Sweep(0.07f, 380, 1100, Wave.Sine, 0.8f, 20f),
                SfxId.Boing => Boing(0.45f, 190, 85, 11f),
                SfxId.Whoosh => NoiseSweep(rnd, 0.3f, 0.5f, 300, 2500),
                SfxId.Thunk => Mix(Sweep(0.12f, 180, 70, Wave.Sine, 1f), Noise(rnd, 0.05f, 0.4f, 0.001f, 1200)),
                SfxId.EnemyShot => Sweep(0.1f, 700, 300, Wave.Triangle, 0.4f, 15f),
                SfxId.Bomb => Mix(Explosion(rnd, 1.4f), Sweep(0.5f, 120, 40, Wave.Sine, 0.8f, 3f)),
                SfxId.Revive => Concat(Sweep(0.3f, 300, 900, Wave.Sine, 0.4f, 3f), Tone(0.4f, 1318, Wave.Sine, 0.35f, 4f)),
                SfxId.Stink => Mix(Noise(rnd, 0.3f, 0.3f, 0.05f, 600), Vibrato(0.3f, 90, 70, 12f, 8f, Wave.Saw, 0.2f)),
                SfxId.Spawn => Sweep(0.12f, 200, 500, Wave.Triangle, 0.3f, 8f),
                _ => Tone(0.1f, 440, Wave.Sine, 0.5f, 10f),
            };
            Normalize(s, 0.85f);
            var clip = AudioClip.Create(id.ToString(), s.Length, 1, Rate, false);
            clip.SetData(s, 0);
            return clip;
        }

        // ------------------------------------------------------------------ primitives

        internal enum Wave { Sine, Square, Saw, Triangle }

        internal static float Osc(Wave w, double phase)
        {
            double p = phase - Math.Floor(phase);
            return w switch
            {
                Wave.Sine => (float)Math.Sin(p * Math.PI * 2),
                Wave.Square => p < 0.5 ? 0.7f : -0.7f,
                Wave.Saw => (float)(p * 2 - 1) * 0.8f,
                _ => (float)(1 - 4 * Math.Abs(p - 0.5)),
            };
        }

        internal static int Len(float sec) => Mathf.Max(1, (int)(sec * Rate));

        /// <summary>Tone with exponential decay (decay = 1/sec).</summary>
        internal static float[] Tone(float sec, float freq, Wave w, float amp, float decay)
        {
            var s = new float[Len(sec)];
            double ph = 0;
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)Rate;
                ph += freq / Rate;
                float env = Mathf.Exp(-t * decay) * Mathf.Clamp01(t * 400f);
                s[i] = Osc(w, ph) * amp * env;
            }
            return s;
        }

        internal static float[] Sweep(float sec, float f0, float f1, Wave w, float amp, float decay = 6f)
        {
            var s = new float[Len(sec)];
            double ph = 0;
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)s.Length;
                float f = Mathf.Lerp(f0, f1, 1f - (1f - t) * (1f - t));
                ph += f / Rate;
                float env = Mathf.Exp(-t * sec * decay) * Mathf.Clamp01(i / (Rate * 0.003f)) * (1f - Mathf.Pow(t, 6f));
                s[i] = Osc(w, ph) * amp * env;
            }
            return s;
        }

        internal static float[] Vibrato(float sec, float f0, float f1, float vibHz, float vibDepth, Wave w, float amp)
        {
            var s = new float[Len(sec)];
            double ph = 0;
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)s.Length;
                float f = Mathf.Lerp(f0, f1, t) + Mathf.Sin(t * sec * vibHz * Mathf.PI * 2f) * vibDepth;
                ph += f / Rate;
                float env = Mathf.Clamp01(t * 30f) * (1f - t);
                s[i] = Osc(w, ph) * amp * env;
            }
            return s;
        }

        internal static float[] Noise(System.Random r, float sec, float amp, float attack, float cutoff)
        {
            var s = new float[Len(sec)];
            float lp = 0f;
            float a = 1f - Mathf.Exp(-2f * Mathf.PI * cutoff / Rate);
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)s.Length;
                float n = (float)(r.NextDouble() * 2 - 1);
                lp += (n - lp) * a;
                float env = Mathf.Clamp01(i / (Rate * Mathf.Max(attack, 0.0005f))) * (1f - t) * (1f - t);
                s[i] = lp * amp * env;
            }
            return s;
        }

        internal static float[] NoiseSweep(System.Random r, float sec, float amp, float c0, float c1)
        {
            var s = new float[Len(sec)];
            float lp = 0f, lp2 = 0f;
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)s.Length;
                float cutoff = Mathf.Lerp(c0, c1, Mathf.Sin(t * Mathf.PI));
                float a = 1f - Mathf.Exp(-2f * Mathf.PI * cutoff / Rate);
                float n = (float)(r.NextDouble() * 2 - 1);
                lp += (n - lp) * a;
                lp2 += (lp - lp2) * a;
                s[i] = (lp - lp2 * 0.5f) * amp * Mathf.Sin(t * Mathf.PI);
            }
            return s;
        }

        internal static float[] Boing(float sec, float f0, float f1, float wobbleHz, float amp = 0.8f)
        {
            var s = new float[Len(sec)];
            double ph = 0;
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)s.Length;
                float wob = Mathf.Sin(t * sec * wobbleHz * Mathf.PI * 2f) * (1f - t);
                float f = Mathf.Lerp(f0, f1, t) * (1f + wob * 0.35f);
                ph += f / Rate;
                s[i] = Osc(Wave.Sine, ph) * amp * (1f - t) * Mathf.Clamp01(t * 200f);
            }
            return s;
        }

        internal static float[] Explosion(System.Random r, float sec)
        {
            var s = new float[Len(sec)];
            float lp = 0f;
            double ph = 0;
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)Rate;
                float cutoff = Mathf.Lerp(3500f, 250f, Mathf.Clamp01(t / sec * 1.5f));
                float a = 1f - Mathf.Exp(-2f * Mathf.PI * cutoff / Rate);
                float n = (float)(r.NextDouble() * 2 - 1);
                lp += (n - lp) * a;
                float env = Mathf.Exp(-t * 3.2f) * Mathf.Clamp01(t * 300f);
                ph += Mathf.Lerp(70f, 30f, t / sec) / Rate;
                s[i] = lp * env * 1.2f + Osc(Wave.Sine, ph) * env * 0.9f;
            }
            return s;
        }

        // ------------------------------------------------------------------ combinators

        internal static float[] Mix(params float[][] parts)
        {
            int len = 0;
            foreach (var p in parts) len = Mathf.Max(len, p.Length);
            var s = new float[len];
            foreach (var p in parts) for (int i = 0; i < p.Length; i++) s[i] += p[i];
            return s;
        }

        internal static float[] Concat(params float[][] parts)
        {
            int len = 0;
            foreach (var p in parts) len += p.Length;
            var s = new float[len];
            int o = 0;
            foreach (var p in parts) { Array.Copy(p, 0, s, o, p.Length); o += p.Length; }
            return s;
        }

        internal static float[] LowPass(float[] s, float cutoff)
        {
            float a = 1f - Mathf.Exp(-2f * Mathf.PI * cutoff / Rate);
            float lp = 0;
            for (int i = 0; i < s.Length; i++) { lp += (s[i] - lp) * a; s[i] = lp; }
            return s;
        }

        internal static void Normalize(float[] s, float peak)
        {
            float m = 1e-6f;
            foreach (var v in s) m = Mathf.Max(m, Mathf.Abs(v));
            float k = peak / m;
            for (int i = 0; i < s.Length; i++) s[i] *= k;
        }
    }
}
