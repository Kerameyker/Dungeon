using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hollow
{
    public enum SfxKind { Swing, Hit, Crit, Skill, Hurt, Die, LevelUp, Pickup, Slam, Stairs }

    /// <summary>All sound effects and the ambient loop are synthesized in code at runtime.</summary>
    public static class Sfx
    {
        const int Rate = 44100;
        static readonly Dictionary<SfxKind, AudioClip> Clips = new Dictionary<SfxKind, AudioClip>();
        static readonly System.Random Rng = new System.Random(42);

        static float Noise() { return (float)(Rng.NextDouble() * 2.0 - 1.0); }
        static float Sine(float f, float t) { return Mathf.Sin(2f * Mathf.PI * f * t); }
        static float Exp(float t, float k) { return Mathf.Exp(-t * k); }

        public static AudioClip Get(SfxKind kind)
        {
            AudioClip c;
            if (!Clips.TryGetValue(kind, out c) || c == null)
            {
                c = Build(kind);
                Clips[kind] = c;
            }
            return c;
        }

        /// <summary>Plays a mostly-2D one-shot at a position (spatialBlend 0.3 so it stays audible from the camera distance).</summary>
        public static void Play(SfxKind kind, Vector3 pos, float volume = 1f)
        {
            var clip = Get(kind);
            var go = new GameObject("Sfx_" + kind);
            go.transform.position = pos;
            var src = go.AddComponent<AudioSource>();
            src.clip = clip;
            src.volume = Mathf.Clamp01(volume);
            src.spatialBlend = 0.3f;
            src.minDistance = 10f;
            src.maxDistance = 60f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.Play();
            UnityEngine.Object.Destroy(go, clip.length + 0.1f);
        }

        public static void Play2D(SfxKind kind, float volume = 1f)
        {
            var cam = Camera.main;
            Play(kind, cam != null ? cam.transform.position : Vector3.zero, volume);
        }

        static AudioClip Synth(string name, float duration, Func<float, float> f)
        {
            int n = Mathf.Max(1, (int)(duration * Rate));
            var data = new float[n];
            for (int i = 0; i < n; i++)
                data[i] = Mathf.Clamp(f(i / (float)Rate), -1f, 1f);
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip Build(SfxKind kind)
        {
            switch (kind)
            {
                case SfxKind.Swing:
                    return Synth("swing", 0.22f, t => Noise() * Mathf.Sin(Mathf.PI * t / 0.22f) * 0.32f);
                case SfxKind.Hit:
                    return Synth("hit", 0.18f, t =>
                        Sine(170f - 100f * (t / 0.18f), t) * Exp(t, 22f) * 0.8f + Noise() * Exp(t, 45f) * 0.5f);
                case SfxKind.Crit:
                    return Synth("crit", 0.3f, t =>
                        Sine(150f, t) * Exp(t, 20f) * 0.7f + Noise() * Exp(t, 40f) * 0.4f + Sine(1400f, t) * Exp(t, 14f) * 0.35f);
                case SfxKind.Skill:
                    return Synth("skill", 0.35f, t =>
                    {
                        float p = t / 0.35f;
                        float env = Mathf.Sin(Mathf.PI * p);
                        return Sine(300f + 700f * p * p, t) * env * 0.45f + Noise() * env * 0.12f;
                    });
                case SfxKind.Hurt:
                    return Synth("hurt", 0.3f, t => (((t * 150f) % 1f) * 2f - 1f) * Exp(t, 9f) * 0.55f);
                case SfxKind.Die:
                    return Synth("die", 0.5f, t =>
                    {
                        float p = t / 0.5f;
                        return Sine(400f - 330f * p, t) * (1f - p) * 0.55f + Noise() * (1f - p) * 0.18f;
                    });
                case SfxKind.LevelUp:
                    return Synth("levelup", 0.75f, t =>
                    {
                        float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
                        int idx = Mathf.Min(3, (int)(t / 0.12f));
                        float local = t - idx * 0.12f;
                        return Sine(notes[idx], t) * Exp(local, 5f) * 0.45f;
                    });
                case SfxKind.Pickup:
                    return Synth("pickup", 0.18f, t =>
                        Sine(880f + 440f * (t / 0.18f), t) * Mathf.Sin(Mathf.PI * t / 0.18f) * 0.4f);
                case SfxKind.Slam:
                    return Synth("slam", 0.7f, t =>
                        Sine(60f - 20f * t, t) * Exp(t, 5f) * 0.9f + Noise() * Exp(t, 10f) * 0.5f);
                case SfxKind.Stairs:
                    return Synth("stairs", 1.2f, t =>
                    {
                        float env = Mathf.Sin(Mathf.PI * t / 1.2f);
                        return (Sine(392f, t) + Sine(493.88f, t) + Sine(587.33f, t)) * env * 0.16f;
                    });
                default:
                    return Synth("silence", 0.05f, t => 0f);
            }
        }

        /// <summary>16 second seamless dark ambient loop (A minor drone with slow pulses and bell notes).</summary>
        public static AudioClip AmbientLoop()
        {
            const float D = 16f;
            // Snap every frequency to a whole number of cycles per loop so the loop has no click.
            Func<float, float> snap = f => Mathf.Round(f * D) / D;
            float f1 = snap(55f), f2 = snap(82.41f), f3 = snap(110f), f4 = snap(164.81f);
            float[] bellTimes = { 2f, 6.5f, 11f };
            float[] bellFreqs = { 440f, 329.63f, 392f };

            return Synth("ambient", D, t =>
            {
                float lfo = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 2f * t / D);
                float lfo2 = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 3f * t / D);
                float v = Sine(f1, t) * 0.22f + Sine(f2, t) * 0.14f * lfo + Sine(f3, t) * 0.10f * lfo2 + Sine(f4, t) * 0.06f;
                for (int i = 0; i < bellTimes.Length; i++)
                {
                    float local = t - bellTimes[i];
                    if (local >= 0f && local < 3.5f)
                        v += Sine(bellFreqs[i], t) * Exp(local, 1.6f) * 0.10f;
                }
                return v;
            });
        }
    }
}
