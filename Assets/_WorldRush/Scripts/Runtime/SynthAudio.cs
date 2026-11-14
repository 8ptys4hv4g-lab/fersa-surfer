using System;
using UnityEngine;

namespace WorldRush.Runtime
{
    /// <summary>
    /// Erzeugt alle Sounds und die Musik zur Laufzeit per Synthese – keine fremden Audiodateien, keine Lizenzfragen.
    /// </summary>
    public static class SynthAudio
    {
        private const int Rate = 44100;

        private static AudioClip Make(string name, float seconds, Func<float, float> sample)
        {
            int n = Mathf.Max(1, (int)(seconds * Rate));
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(sample(i / (float)Rate), -1f, 1f);
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static float Square(float f, float t) => Mathf.Sin(2f * Mathf.PI * f * t) >= 0f ? 1f : -1f;
        private static float Tri(float f, float t) => 2f * Mathf.Abs(2f * (t * f - Mathf.Floor(t * f + 0.5f))) - 1f;
        private static float Env(float t, float decay) => Mathf.Exp(-t * decay);

        public static AudioClip Coin() => Make("coin", 0.22f, t =>
            (t < 0.06f ? Square(988f, t) * Env(t, 30f) : Square(1480f, t) * Env(t - 0.06f, 18f)) * 0.25f);

        public static AudioClip Jump() => Make("jump", 0.25f, t =>
            Mathf.Sin(2f * Mathf.PI * (300f * t + 900f * t * t)) * Env(t, 9f) * 0.5f);

        public static AudioClip Slide()
        {
            var r = new System.Random(3);
            return Make("slide", 0.28f, t => ((float)r.NextDouble() * 2f - 1f) * Env(t, 12f) * 0.25f);
        }

        public static AudioClip PowerUp() => Make("power", 0.45f, t =>
        {
            float[] notes = { 523f, 659f, 784f, 1046f };
            int i = Mathf.Min(3, (int)(t / 0.08f));
            return Tri(notes[i], t) * Env(t - i * 0.08f, 10f) * 0.4f;
        });

        public static AudioClip Crash()
        {
            var r = new System.Random(7);
            return Make("crash", 0.7f, t =>
                (((float)r.NextDouble() * 2f - 1f) * 0.6f + Mathf.Sin(2f * Mathf.PI * (180f * t - 100f * t * t))) * Env(t, 5f) * 0.45f);
        }

        /// <summary>Zwei Takte Beat (Kick, Hi-Hat, Bass) als Schleife. Tempo steuert später die Tonhöhe.</summary>
        public static AudioClip MusicLoop()
        {
            const float bpm = 120f;
            float step = 60f / bpm / 4f;
            float[] bass = { 110f, 0, 0, 130.8f, 0, 0, 146.8f, 0, 110f, 0, 0, 164.8f, 0, 0, 196f, 0 };
            var r = new System.Random(11);
            return Make("music", step * 32f, t =>
            {
                int s = (int)(t / step);
                float local = t - s * step;
                int k = s % 16;
                float v = 0f;
                if (k % 4 == 0) v += Mathf.Sin(2f * Mathf.PI * (140f * local - 160f * local * local)) * Env(local, 14f) * 0.8f;
                if (k % 4 == 2) v += ((float)r.NextDouble() * 2f - 1f) * Env(local, 60f) * 0.18f;
                if (k == 4 || k == 12) v += ((float)r.NextDouble() * 2f - 1f) * Env(local, 25f) * 0.25f;
                int bs = k;
                while (bs > 0 && bass[bs] == 0f) bs--;
                float bt = t - ((s - (k - bs)) * step);
                if (bass[bs] > 0f) v += Tri(bass[bs], bt) * Env(bt, 3f) * 0.35f;
                return v * 0.55f;
            });
        }
    }
}
