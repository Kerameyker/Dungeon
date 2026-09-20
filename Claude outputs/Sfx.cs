using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hollow
{
    public enum SfxKind
    {
        Swing, Hit, Crit, Skill, Hurt, Die, LevelUp, Pickup, Slam, Stairs,
        // Appended for the big update. Append only, never reorder: the first ten keep their original values.
        UiClick, UiOpen, UiClose, Coin, Switch, Door, Potion, Upgrade, UpgradeFail,
        Blip, Crystal, BossRoar, PhaseBreak, Chime, Sheath
    }

    /// <summary>All sound effects and the music loops are synthesized in code at runtime.</summary>
    public static class Sfx
    {
        const int Rate = 44100;
        /// <summary>Music is rendered at half rate: loops are long, and nothing in them lives above ~5 kHz.</summary>
        const int MusicRate = 22050;

        static readonly Dictionary<SfxKind, AudioClip> Clips = new Dictionary<SfxKind, AudioClip>();
        static readonly System.Random Rng = new System.Random(42);

        static float Noise() { return (float)(Rng.NextDouble() * 2.0 - 1.0); }
        static float Sine(float f, float t) { return Mathf.Sin(2f * Mathf.PI * f * t); }
        static float Exp(float t, float k) { return Mathf.Exp(-t * k); }

        // ---------------------------------------------------------------- cheap oscillator table
        // The music loops touch millions of samples, so they use a table lookup instead of Mathf.Sin.
        const int TabSize = 8192;
        static readonly float[] SinTab = BuildSinTab();

        static float[] BuildSinTab()
        {
            var t = new float[TabSize];
            for (int i = 0; i < TabSize; i++) t[i] = Mathf.Sin(2f * Mathf.PI * i / TabSize);
            return t;
        }

        /// <summary>Table sine. Phase is in turns (1 = one full cycle) and wraps by itself.</summary>
        static float Osc(float phase)
        {
            return SinTab[(int)(phase * TabSize) & (TabSize - 1)];
        }

        /// <summary>Sine plus a cheap 3rd harmonic (Chebyshev: sin(3x) = 3s - 4s^3). Warm, organ-like.</summary>
        static float Warm(float s, float third)
        {
            return s + (3f * s - 4f * s * s * s) * third;
        }

        /// <summary>Vocal-ish waveshape (1st + 3rd + 5th harmonic, magnitude stays &lt;= 1).</summary>
        static float ChoirShape(float s)
        {
            float s2 = s * s, s3 = s2 * s, s5 = s3 * s2;
            return s * 0.62f + (3f * s - 4f * s3) * 0.26f + (5f * s - 20f * s3 + 16f * s5) * 0.12f;
        }

        /// <summary>Snaps a frequency to a whole number of cycles per loop, so the loop ends where it started.</summary>
        static float Snap(float f, float loopSeconds)
        {
            float cycles = Mathf.Round(f * loopSeconds);
            if (cycles < 1f) cycles = 1f;
            return cycles / loopSeconds;
        }

        /// <summary>
        /// Gain of one chord slot, wrapped over the loop. Neighbouring slots cross-fade with smoothstep,
        /// which sums to exactly 1, so the last slot fades into the first one across the loop seam.
        /// </summary>
        static float SlotGate(float t, float loopSeconds, float start, float len, float fade)
        {
            float x = t - start;
            if (x < 0f) x += loopSeconds;
            if (x >= loopSeconds) x -= loopSeconds;
            if (x >= len + fade) return 0f;
            if (x < fade) { float s = x / fade; return s * s * (3f - 2f * s); }
            if (x > len) { float s = 1f - (x - len) / fade; return s * s * (3f - 2f * s); }
            return 1f;
        }

        /// <summary>Scales the buffer down if it exceeds the peak (never boosts, so the mix keeps its balance).</summary>
        static void Normalize(float[] data, float peak)
        {
            float max = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                float a = data[i] < 0f ? -data[i] : data[i];
                if (a > max) max = a;
            }
            if (max > peak && max > 1e-6f)
            {
                float g = peak / max;
                for (int i = 0; i < data.Length; i++) data[i] *= g;
            }
        }

        // ---------------------------------------------------------------- public API
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

        /// <summary>
        /// Like <see cref="Synth"/> but limits the result to <paramref name="peak"/> instead of hard clipping.
        /// The generator is called once per sample in order, so it may keep filter state in captured locals.
        /// </summary>
        static AudioClip SynthPeak(string name, float duration, float peak, Func<float, float> f)
        {
            int n = Mathf.Max(1, (int)(duration * Rate));
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = f(i / (float)Rate);
            Normalize(data, peak);
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

                case SfxKind.UiClick: return BuildUiClick();
                case SfxKind.UiOpen: return BuildUiOpen();
                case SfxKind.UiClose: return BuildUiClose();
                case SfxKind.Coin: return BuildCoin();
                case SfxKind.Switch: return BuildSwitch();
                case SfxKind.Door: return BuildDoor();
                case SfxKind.Potion: return BuildPotion();
                case SfxKind.Upgrade: return BuildUpgrade();
                case SfxKind.UpgradeFail: return BuildUpgradeFail();
                case SfxKind.Blip: return BuildBlip();
                case SfxKind.Crystal: return BuildCrystal();
                case SfxKind.BossRoar: return BuildBossRoar();
                case SfxKind.PhaseBreak: return BuildPhaseBreak();
                case SfxKind.Chime: return BuildChime();
                case SfxKind.Sheath: return BuildSheath();

                default:
                    return Synth("silence", 0.05f, t => 0f);
            }
        }

        // ---------------------------------------------------------------- new one-shots

        /// <summary>Short soft tick for menu cursor movement.</summary>
        static AudioClip BuildUiClick()
        {
            return SynthPeak("ui_click", 0.05f, 0.42f, t =>
                Sine(1750f, t) * Exp(t, 130f) * 0.38f
                + Sine(2640f, t) * Exp(t, 260f) * 0.14f
                + Noise() * Exp(t, 500f) * 0.10f);
        }

        /// <summary>Rising sweep: a panel opens.</summary>
        static AudioClip BuildUiOpen()
        {
            const float D = 0.2f;
            float air = 0f;
            return SynthPeak("ui_open", D, 0.6f, t =>
            {
                float p = t / D;
                float env = Mathf.Sin(Mathf.PI * p);
                air += 0.35f * (Noise() - air);
                return Sine(330f + 760f * p * p, t) * env * 0.42f
                     + Sine(660f + 1520f * p * p, t) * env * 0.12f
                     + air * env * 0.10f;
            });
        }

        /// <summary>Falling sweep: a panel closes.</summary>
        static AudioClip BuildUiClose()
        {
            const float D = 0.2f;
            float air = 0f;
            return SynthPeak("ui_close", D, 0.6f, t =>
            {
                float p = t / D;
                float env = Mathf.Sin(Mathf.PI * p);
                float q = 1f - p;
                air += 0.30f * (Noise() - air);
                return Sine(300f + 720f * q * q, t) * env * 0.42f
                     + Sine(150f + 360f * q * q, t) * env * 0.14f
                     + air * env * 0.08f;
            });
        }

        /// <summary>Bright two-tone ping for gold.</summary>
        static AudioClip BuildCoin()
        {
            const float D = 0.5f;
            return SynthPeak("coin", D, 0.8f, t =>
            {
                float v = Sine(1318.51f, t) * Exp(t, 9f) * 0.45f + Sine(2637f, t) * Exp(t, 18f) * 0.12f;
                float l = t - 0.065f;
                if (l >= 0f) v += Sine(1975.53f, t) * Exp(l, 7f) * 0.40f + Sine(3951f, t) * Exp(l, 16f) * 0.10f;
                float tail = 1f - Mathf.Clamp01((t - (D - 0.06f)) / 0.06f);
                return v * tail;
            });
        }

        /// <summary>Swift whoosh into a metallic clang: a lever or switch is thrown.</summary>
        static AudioClip BuildSwitch()
        {
            const float D = 0.55f;
            float[] partials = { 1046f, 1478f, 2217f, 3136f };
            float band = 0f;
            return SynthPeak("switch", D, 0.85f, t =>
            {
                // sweeping one-pole on noise = whoosh (state survives between samples)
                float p = Mathf.Clamp01(t / 0.2f);
                band += (0.06f + 0.50f * p) * (Noise() - band);
                float v = band * 2.2f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 0.23f)) * 0.55f;

                float l = t - 0.19f;
                if (l >= 0f)
                {
                    float m = 0f;
                    for (int i = 0; i < partials.Length; i++) m += Sine(partials[i], t) * (1f / (1f + i));
                    v += m * Exp(l, 24f) * 0.26f + Noise() * Exp(l, 150f) * 0.22f;
                }
                float tail = 1f - Mathf.Clamp01((t - (D - 0.06f)) / 0.06f);
                return v * tail;
            });
        }

        /// <summary>Heavy stone door: deep grinding rumble that ends in a thud.</summary>
        static AudioClip BuildDoor()
        {
            const float D = 1.1f;
            float deep = 0f, grit = 0f;
            return SynthPeak("door", D, 0.88f, t =>
            {
                float p = t / D;
                float env = p < 0.14f ? p / 0.14f : 1f - (p - 0.14f) / 0.86f;
                float grind = 0.45f + 0.55f * Sine(11f, t) * Sine(3.5f, t);
                deep += 0.035f * (Noise() - deep);
                grit += 0.28f * (Noise() - grit);
                float v = deep * 7.5f * env * 0.50f
                        + grit * 2.6f * env * grind * 0.10f
                        + Sine(47f, t) * env * 0.30f
                        + Sine(31f, t) * env * 0.26f;
                float l = t - 0.78f;
                if (l >= 0f) v += Sine(68f - 26f * l, t) * Exp(l, 12f) * 0.60f + Noise() * Exp(l, 42f) * 0.20f;
                return v;
            });
        }

        /// <summary>Three gulps and a bubbly tail.</summary>
        static AudioClip BuildPotion()
        {
            const float D = 0.66f;
            float[] gulp = { 0.02f, 0.17f, 0.33f };
            return SynthPeak("potion", D, 0.78f, t =>
            {
                float v = 0f;
                for (int i = 0; i < gulp.Length; i++)
                {
                    float l = t - gulp[i];
                    if (l < 0f || l > 0.16f) continue;
                    float q = l / 0.16f;
                    float f = 310f - 165f * q - i * 22f;
                    float e = Mathf.Sin(Mathf.PI * q);
                    v += Sine(f, t) * e * 0.44f + Sine(f * 2f, t) * e * e * 0.11f;
                }
                float l2 = t - 0.40f;
                if (l2 >= 0f)
                {
                    float w = 680f + 460f * Sine(16f, t);
                    v += Sine(w, t) * Exp(l2, 9f) * 0.17f;
                }
                float tail = 1f - Mathf.Clamp01((t - (D - 0.06f)) / 0.06f);
                return v * tail;
            });
        }

        /// <summary>Rising sparkle arpeggio: an upgrade succeeded.</summary>
        static AudioClip BuildUpgrade()
        {
            const float D = 0.95f;
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f, 1318.51f };
            float[] at = { 0f, 0.075f, 0.15f, 0.225f, 0.31f };
            float sparkle = 0f;
            return SynthPeak("upgrade", D, 0.85f, t =>
            {
                float v = 0f;
                for (int i = 0; i < notes.Length; i++)
                {
                    float l = t - at[i];
                    if (l < 0f) continue;
                    float f = notes[i];
                    float e = Exp(l, 4.5f);                       // e*e == Exp(l, 9), one call instead of two
                    v += (Sine(f, t) * e + Sine(f * 2f, t) * e * e * 0.30f) * 0.20f;
                }
                sparkle += 0.55f * (Noise() - sparkle);
                float l2 = t - 0.28f;
                if (l2 >= 0f) v += sparkle * 1.3f * Exp(l2, 4f) * 0.12f;
                float tail = 1f - Mathf.Clamp01((t - (D - 0.08f)) / 0.08f);
                return v * tail;
            });
        }

        /// <summary>Dull crack and a sagging tone: the upgrade failed.</summary>
        static AudioClip BuildUpgradeFail()
        {
            const float D = 0.7f;
            float dull = 0f;
            return SynthPeak("upgrade_fail", D, 0.8f, t =>
            {
                dull += 0.12f * (Noise() - dull);
                float v = dull * 4.0f * Exp(t, 26f) * 0.55f;
                v += Sine(190f, t) * Exp(t, 30f) * 0.35f;
                float l = t - 0.07f;
                if (l >= 0f)
                {
                    float q = Mathf.Clamp01(l / 0.55f);
                    v += Sine(420f - 300f * q, t) * (1f - q) * 0.42f;
                    v += Sine(210f - 150f * q, t) * (1f - q) * 0.22f;
                }
                return v;
            });
        }

        /// <summary>Tiny blip for dialogue text.</summary>
        static AudioClip BuildBlip()
        {
            const float D = 0.055f;
            return SynthPeak("blip", D, 0.32f, t =>
            {
                float e = Exp(t, 70f) * (1f - Mathf.Clamp01((t - (D - 0.012f)) / 0.012f));
                // Warm() squares the tone off a little but still starts at zero, so there is no opening step.
                return Warm(Sine(900f, t), 0.30f) * e * 0.34f;
            });
        }

        /// <summary>Glassy shimmer for a return crystal.</summary>
        static AudioClip BuildCrystal()
        {
            const float D = 1.1f;
            float[] mul = { 1f, 2.41f, 3.72f, 5.13f, 6.84f };
            float[] dec = { 2.2f, 3.0f, 4.0f, 5.5f, 7.0f };
            float[] amp = { 0.24f, 0.16f, 0.11f, 0.08f, 0.05f };
            const float baseF = 1174.66f;
            return SynthPeak("crystal", D, 0.82f, t =>
            {
                float trem = 0.78f + 0.22f * Sine(7.5f, t);
                float v = 0f;
                for (int i = 0; i < mul.Length; i++)
                    v += Sine(baseF * mul[i], t) * Exp(t, dec[i]) * amp[i];
                float l = t - 0.015f;
                if (l >= 0f) v += Sine(2349f, t) * Exp(l, 40f) * 0.10f;
                float tail = 1f - Mathf.Clamp01((t - (D - 0.1f)) / 0.1f);
                return v * trem * tail;
            });
        }

        /// <summary>Low growl with a noise burst: a boss notices you.</summary>
        static AudioClip BuildBossRoar()
        {
            const float D = 1.45f;
            float air = 0f;
            return SynthPeak("boss_roar", D, 0.9f, t =>
            {
                float p = t / D;
                float env = p < 0.12f ? p / 0.12f : 1f - (p - 0.12f) / 0.88f;
                float growl = 0.55f + 0.45f * Sine(23f + 7f * p, t);
                float f = 80f - 28f * p;
                float v = (Sine(f, t) * 0.60f + Sine(f * 1.5f, t) * 0.20f + Sine(f * 0.5f, t) * 0.30f) * env * growl;
                air += 0.10f * (Noise() - air);
                v += air * 4.5f * env * (0.35f + 0.65f * growl) * 0.30f;
                float l = t - 0.04f;
                if (l >= 0f) v += Noise() * Exp(l, 7f) * 0.14f;
                return v;
            });
        }

        /// <summary>Impact plus a shockwave rumble: a boss bar breaks.</summary>
        static AudioClip BuildPhaseBreak()
        {
            const float D = 1.35f;
            float rumble = 0f, crackle = 0f;
            return SynthPeak("phase_break", D, 0.9f, t =>
            {
                crackle += 0.55f * (Noise() - crackle);
                float v = crackle * 1.3f * Exp(t, 34f) * 0.60f;
                v += Sine(220f, t) * Exp(t, 26f) * 0.30f;

                float q = Mathf.Clamp01(t / 0.9f);
                v += Sine(130f - 95f * q, t) * (1f - q) * (1f - q) * 0.70f;   // sub sweep

                rumble += 0.05f * (Noise() - rumble);
                float swell = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / D));
                v += rumble * 6.0f * swell * 0.35f;
                return v;
            });
        }

        /// <summary>Pleasant bell for a finished quest.</summary>
        static AudioClip BuildChime()
        {
            const float D = 1.9f;
            float[] notes = { 659.25f, 830.61f, 987.77f, 1318.51f };
            float[] at = { 0f, 0.13f, 0.26f, 0.44f };
            return SynthPeak("chime", D, 0.85f, t =>
            {
                float v = 0f;
                for (int i = 0; i < notes.Length; i++)
                {
                    float l = t - at[i];
                    if (l < 0f) continue;
                    float f = notes[i];
                    float e = Exp(l, 1.7f);                       // e^2 and e^3 give the 3.4 / 5.1 decays for free
                    float e2 = e * e;
                    v += (Sine(f, t) * e
                        + Sine(f * 2.01f, t) * e2 * 0.32f
                        + Sine(f * 2.76f, t) * e2 * e * 0.16f) * 0.26f;
                }
                float tail = 1f - Mathf.Clamp01((t - (D - 0.25f)) / 0.25f);
                return v * tail;
            });
        }

        /// <summary>Metallic slide of a blade going back into its scabbard.</summary>
        static AudioClip BuildSheath()
        {
            const float D = 0.42f;
            float lo = 0f, hi = 0f;
            return SynthPeak("sheath", D, 0.7f, t =>
            {
                float p = Mathf.Clamp01(t / 0.30f);
                float k = 0.10f + 0.55f * p * p;
                lo += k * (Noise() - lo);
                hi += 0.06f * (lo - hi);
                float band = (lo - hi) * 2.4f;
                float env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 0.34f));
                float v = band * env * 0.55f;

                float l = t - 0.27f;
                if (l >= 0f)
                    v += (Sine(2093f, t) * 0.6f + Sine(3136f, t) * 0.3f + Sine(4186f, t) * 0.15f) * Exp(l, 26f) * 0.22f;

                float tail = 1f - Mathf.Clamp01((t - (D - 0.05f)) / 0.05f);
                return v * tail;
            });
        }

        // ---------------------------------------------------------------- loop building blocks

        static AudioClip MusicClip(string name, float[] data, int rate)
        {
            var clip = AudioClip.Create(name, data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// Mixes a decaying tone (plus 2nd/3rd partials) into a loop buffer. The tail wraps around to the
        /// start of the buffer, so a note near the end of the loop keeps ringing over the seam.
        /// </summary>
        static void AddTone(float[] buf, int rate, float startSec, float freq, float durSec, float amp, float decay, float h2, float h3)
        {
            int n = buf.Length;
            int len = (int)(durSec * rate);
            if (len < 8) len = 8;
            if (len > n) len = n;
            int w = (int)(startSec * rate) % n;
            if (w < 0) w += n;

            float i1 = freq / rate, i2 = i1 * 2f, i3 = i1 * 3f;
            if (i2 >= 1f) { i2 = 0f; h2 = 0f; }
            if (i3 >= 1f) { i3 = 0f; h3 = 0f; }
            float p1 = 0f, p2 = 0f, p3 = 0f;

            int atk = rate / 400; if (atk < 2) atk = 2;
            float atkStep = 1f / atk;
            float env = 0f, dec = Mathf.Exp(-decay / rate);

            for (int i = 0; i < len; i++)
            {
                if (i < atk) env += atkStep; else env *= dec;
                float e = env;
                int left = len - i;
                if (left < 192) e *= left * (1f / 192f);
                float e2 = e * e;
                buf[w] += (Osc(p1) * e + Osc(p2) * e2 * h2 + Osc(p3) * e2 * h3) * amp;
                w++; if (w >= n) w = 0;
                p1 += i1; if (p1 >= 1f) p1 -= 1f;
                p2 += i2; if (p2 >= 1f) p2 -= 1f;
                p3 += i3; if (p3 >= 1f) p3 -= 1f;
            }
        }

        /// <summary>Drum-style hit: a sine gliding from f0 down to f1. Also wraps around the loop seam.</summary>
        static void AddKick(float[] buf, int rate, float startSec, float f0, float f1, float durSec, float amp, float decay)
        {
            int n = buf.Length;
            int len = (int)(durSec * rate);
            if (len < 8) len = 8;
            if (len > n) len = n;
            int w = (int)(startSec * rate) % n;
            if (w < 0) w += n;

            float inc = Mathf.Max(1f, f0) / rate;
            float glide = Mathf.Pow(Mathf.Clamp(f1 / Mathf.Max(1f, f0), 0.01f, 100f), 1f / len);
            float ph = 0f, env = 1f, dec = Mathf.Exp(-decay / rate);
            const int Atk = 24;

            for (int i = 0; i < len; i++)
            {
                float e = env;
                if (i < Atk) e *= i * (1f / Atk);
                int left = len - i;
                if (left < 128) e *= left * (1f / 128f);
                buf[w] += Osc(ph) * e * amp;
                w++; if (w >= n) w = 0;
                ph += inc; if (ph >= 1f) ph -= 1f;
                inc *= glide;
                env *= dec;
            }
        }

        /// <summary>Filtered noise hit (percussion, swells). Wraps around the loop seam like the tone helpers.</summary>
        static void AddNoise(float[] buf, int rate, float startSec, float durSec, float amp, float decay, float cutoff)
        {
            int n = buf.Length;
            int len = (int)(durSec * rate);
            if (len < 8) len = 8;
            if (len > n) len = n;
            int w = (int)(startSec * rate) % n;
            if (w < 0) w += n;

            float k = Mathf.Clamp(cutoff, 0.01f, 1f);
            float g = amp * Mathf.Sqrt((2f - k) / k);   // keep the level steady whatever the cutoff is
            float lp = 0f, env = 1f, dec = Mathf.Exp(-decay / rate);

            for (int i = 0; i < len; i++)
            {
                lp += k * (Noise() - lp);
                float e = env;
                if (i < 16) e *= i * (1f / 16f);
                int left = len - i;
                if (left < 128) e *= left * (1f / 128f);
                buf[w] += lp * e * g;
                w++; if (w >= n) w = 0;
                env *= dec;
            }
        }

        // ---------------------------------------------------------------- music loops
        static AudioClip _townLoop, _towerLoop, _bossLoop, _finalBossLoop;

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

        /// <summary>Town: 16 s, warm and calm, plucked melody over a soft major-ish pad. Built once, then cached.</summary>
        public static AudioClip TownLoop()
        {
            if (_townLoop == null) _townLoop = BuildTownLoop();
            return _townLoop;
        }

        /// <summary>Tower: the dark ambient drone, built once and cached.</summary>
        public static AudioClip TowerLoop()
        {
            if (_towerLoop == null) _towerLoop = AmbientLoop();
            return _towerLoop;
        }

        /// <summary>Boss: 16 s, low pulse, rising minor arpeggio and soft percussion. Built once, then cached.</summary>
        public static AudioClip BossLoop()
        {
            if (_bossLoop == null) _bossLoop = BuildBossLoop();
            return _bossLoop;
        }

        /// <summary>Final boss: 24 s, choir-like pad, heavy pulse and a rising line. Built once, then cached.</summary>
        public static AudioClip FinalBossLoop()
        {
            if (_finalBossLoop == null) _finalBossLoop = BuildFinalBossLoop();
            return _finalBossLoop;
        }

        static AudioClip BuildTownLoop()
        {
            const float D = 16f;
            const int Slots = 4;
            const float Fade = 1.1f;
            int rate = MusicRate;
            int n = (int)(D * rate);
            var buf = new float[n];

            // C(add9) - Am - F - G, one chord per 4 seconds.
            float[] padF =
            {
                130.81f, 196.00f, 329.63f,   // C3 G3 E4
                110.00f, 164.81f, 261.63f,   // A2 E3 C4
                 87.31f, 130.81f, 220.00f,   // F2 C3 A3
                 98.00f, 146.83f, 246.94f    // G2 D3 B3
            };
            float[] padAmp = { 0.115f, 0.085f, 0.062f };

            var inc = new float[padF.Length];
            var ph = new float[padF.Length];
            for (int v = 0; v < padF.Length; v++) inc[v] = Snap(padF[v], D) / rate;

            float bi1 = Snap(65.41f, D) / rate, bi2 = Snap(65.66f, D) / rate;   // detuned sub, slow beating
            float bp1 = 0f, bp2 = 0f;
            float shInc = Snap(1046.50f, D) / rate, shPh = 0f;
            float lfo1Inc = (1f / D) / rate, lfo1 = 0f;
            float lfo2Inc = (3f / D) / rate, lfo2 = 0f;

            float slotLen = D / Slots;
            float invRate = 1f / rate;
            var gate = new float[Slots];

            for (int i = 0; i < n; i++)
            {
                float t = i * invRate;
                for (int s = 0; s < Slots; s++) gate[s] = SlotGate(t, D, s * slotLen, slotLen, Fade);

                float v = 0f;
                for (int s = 0; s < Slots; s++)
                {
                    float g = gate[s];
                    for (int k = 0; k < 3; k++)
                    {
                        int vi = s * 3 + k;
                        float o = Osc(ph[vi]);
                        ph[vi] += inc[vi]; if (ph[vi] >= 1f) ph[vi] -= 1f;
                        if (g > 0f) v += Warm(o, 0.16f) * g * padAmp[k];
                    }
                }

                float b = Osc(bp1) + Osc(bp2);
                bp1 += bi1; if (bp1 >= 1f) bp1 -= 1f;
                bp2 += bi2; if (bp2 >= 1f) bp2 -= 1f;
                v += b * 0.080f * (0.72f + 0.28f * Osc(lfo1));

                float sh = Osc(shPh);
                shPh += shInc; if (shPh >= 1f) shPh -= 1f;
                v += sh * 0.022f * (0.5f + 0.5f * Osc(lfo2));

                lfo1 += lfo1Inc; if (lfo1 >= 1f) lfo1 -= 1f;
                lfo2 += lfo2Inc; if (lfo2 >= 1f) lfo2 -= 1f;

                buf[i] = v;
            }

            // Plucked melody; the last note rings over the seam into the start of the loop.
            float[] mt =
            {
                 0.15f,  0.70f,  1.30f,  2.05f,  2.60f,  3.30f,
                 4.15f,  4.70f,  5.30f,  6.05f,  6.60f,  7.30f,
                 8.15f,  8.70f,  9.30f, 10.05f, 10.60f, 11.30f,
                12.15f, 12.70f, 13.30f, 14.05f, 14.70f, 15.40f
            };
            float[] mf =
            {
                659.25f, 783.99f, 523.25f, 587.33f, 659.25f, 392.00f,
                440.00f, 523.25f, 659.25f, 587.33f, 523.25f, 440.00f,
                349.23f, 440.00f, 523.25f, 587.33f, 523.25f, 440.00f,
                392.00f, 493.88f, 587.33f, 783.99f, 587.33f, 493.88f
            };
            for (int i = 0; i < mt.Length; i++)
                AddTone(buf, rate, mt[i], mf[i], 1.35f, 0.150f, 4.2f, 0.26f, 0.10f);

            Normalize(buf, 0.85f);
            return MusicClip("town_loop", buf, rate);
        }

        static AudioClip BuildBossLoop()
        {
            const float D = 16f;
            int rate = MusicRate;
            int n = (int)(D * rate);
            var buf = new float[n];

            float d1 = Snap(55.00f, D) / rate;    // A1
            float d2 = Snap(55.40f, D) / rate;    // detuned twin -> slow tense beating
            float d3 = Snap(82.41f, D) / rate;    // E2
            float d4 = Snap(220.00f, D) / rate;   // A3, thin and far away
            float p1 = 0f, p2 = 0f, p3 = 0f, p4 = 0f;
            float lfoInc = (2f / D) / rate, lfo = 0f;
            float lfo2Inc = (5f / D) / rate, lfo2 = 0f;

            for (int i = 0; i < n; i++)
            {
                float swell = 0.62f + 0.38f * Osc(lfo);
                float o1 = Osc(p1), o2 = Osc(p2), o3 = Osc(p3), o4 = Osc(p4);
                buf[i] = (Warm(o1, 0.22f) + Warm(o2, 0.22f)) * 0.085f * swell
                       + o3 * 0.055f * (0.60f + 0.40f * Osc(lfo2))
                       + o4 * 0.026f;
                p1 += d1; if (p1 >= 1f) p1 -= 1f;
                p2 += d2; if (p2 >= 1f) p2 -= 1f;
                p3 += d3; if (p3 >= 1f) p3 -= 1f;
                p4 += d4; if (p4 >= 1f) p4 -= 1f;
                lfo += lfoInc; if (lfo >= 1f) lfo -= 1f;
                lfo2 += lfo2Inc; if (lfo2 >= 1f) lfo2 -= 1f;
            }

            // low pulse on every eighth (120 BPM), accented on the beat
            for (int i = 0; i < 64; i++)
                AddKick(buf, rate, i * 0.25f, 64f, 46f, 0.23f, (i % 4 == 0) ? 0.34f : 0.19f, 12f);

            // minor arpeggio in sixteenths, coming in after the first bar and growing
            float[] arp = { 220.00f, 261.63f, 329.63f, 440.00f, 329.63f, 261.63f, 220.00f, 329.63f };
            for (int i = 0; i < 96; i++)
                AddTone(buf, rate, 4f + i * 0.125f, arp[i % arp.Length], 0.30f,
                        0.055f + 0.055f * (i / 95f), 15f, 0.30f, 0.12f);

            // soft percussion: backbeat and hats
            for (int i = 0; i < 16; i++) AddNoise(buf, rate, 0.5f + i, 0.20f, 0.16f, 24f, 0.50f);
            for (int i = 0; i < 64; i++) AddNoise(buf, rate, 0.125f + i * 0.25f, 0.05f, 0.055f, 90f, 0.95f);

            Normalize(buf, 0.88f);
            return MusicClip("boss_loop", buf, rate);
        }

        static AudioClip BuildFinalBossLoop()
        {
            const float D = 24f;
            const int Slots = 4;
            const float Fade = 1.6f;
            int rate = MusicRate;
            int n = (int)(D * rate);
            var buf = new float[n];

            // Dm - Bb - Gm - A over a D pedal, one chord per 6 seconds.
            float[] chF =
            {
                146.83f, 174.61f, 220.00f,   // D3 F3 A3
                116.54f, 146.83f, 174.61f,   // Bb2 D3 F3
                 98.00f, 116.54f, 146.83f,   // G2 Bb2 D3
                110.00f, 138.59f, 164.81f    // A2 C#3 E3
            };
            float[] chAmp = { 0.100f, 0.082f, 0.068f };

            var inc = new float[chF.Length];
            var ph = new float[chF.Length];
            for (int v = 0; v < chF.Length; v++) inc[v] = Snap(chF[v], D) / rate;

            float s1 = Snap(36.71f, D) / rate, s2 = Snap(73.42f, D) / rate;   // D1 / D2 pedal
            float sp1 = 0f, sp2 = 0f;
            float vibInc = (120f / D) / rate, vib = 0f;   // 5 Hz, whole cycles per loop
            float swInc = (3f / D) / rate, sw = 0f;

            float slotLen = D / Slots;
            float invRate = 1f / rate;
            var gate = new float[Slots];

            for (int i = 0; i < n; i++)
            {
                float t = i * invRate;
                for (int s = 0; s < Slots; s++) gate[s] = SlotGate(t, D, s * slotLen, slotLen, Fade);

                float trem = 0.88f + 0.12f * Osc(vib);
                float v = 0f;
                for (int s = 0; s < Slots; s++)
                {
                    float g = gate[s];
                    for (int k = 0; k < 3; k++)
                    {
                        int vi = s * 3 + k;
                        float o = Osc(ph[vi]);
                        ph[vi] += inc[vi]; if (ph[vi] >= 1f) ph[vi] -= 1f;
                        if (g > 0f) v += ChoirShape(o) * g * chAmp[k] * trem;
                    }
                }

                v += (Osc(sp1) * 0.115f + Osc(sp2) * 0.070f) * (0.70f + 0.30f * Osc(sw));
                sp1 += s1; if (sp1 >= 1f) sp1 -= 1f;
                sp2 += s2; if (sp2 >= 1f) sp2 -= 1f;
                vib += vibInc; if (vib >= 1f) vib -= 1f;
                sw += swInc; if (sw >= 1f) sw -= 1f;

                buf[i] = v;
            }

            // heavy pulse, every half second, accented every fourth
            for (int i = 0; i < 48; i++)
                AddKick(buf, rate, i * 0.5f, 88f, 38f, 0.46f, (i % 4 == 0) ? 0.40f : 0.22f, 9f);

            // slow crashes marking each chord change
            for (int i = 0; i < 4; i++) AddNoise(buf, rate, i * 6f, 1.6f, 0.13f, 3.2f, 0.30f);

            // rising line: a slow run, then a faster and louder one, then a held high note over the seam
            float[] rise =
            {
                293.66f, 329.63f, 349.23f, 392.00f, 440.00f, 466.16f,
                523.25f, 587.33f, 659.25f, 698.46f, 783.99f, 880.00f
            };
            for (int i = 0; i < rise.Length; i++)
                AddTone(buf, rate, 2f + i * 0.75f, rise[i], 0.90f, 0.075f + 0.045f * (i / 11f), 5.0f, 0.45f, 0.22f);
            for (int i = 0; i < rise.Length; i++)
                AddTone(buf, rate, 12f + i * 0.50f, rise[i], 0.80f, 0.085f + 0.055f * (i / 11f), 6.0f, 0.50f, 0.28f);
            AddTone(buf, rate, 18.4f, 1174.66f, 5.8f, 0.095f, 1.1f, 0.35f, 0.15f);

            Normalize(buf, 0.88f);
            return MusicClip("final_boss_loop", buf, rate);
        }
    }
}
