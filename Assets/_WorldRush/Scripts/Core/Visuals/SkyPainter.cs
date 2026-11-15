using System;

namespace WorldRush.Core.Visuals
{
    /// <summary>Einfache Farbe (0..1, sRGB) ohne UnityEngine.</summary>
    public readonly struct SkyColor
    {
        public readonly float R, G, B;
        public SkyColor(float r, float g, float b) { R = r; G = g; B = b; }
        public static SkyColor Lerp(SkyColor a, SkyColor b, float t)
        {
            t = t < 0f ? 0f : t > 1f ? 1f : t;
            return new SkyColor(a.R + (b.R - a.R) * t, a.G + (b.G - a.G) * t, a.B + (b.B - a.B) * t);
        }
        public SkyColor Scale(float f) => new SkyColor(R * f, G * f, B * f);
        public float Luminance => 0.2126f * R + 0.7152f * G + 0.0722f * B;
    }

    /// <summary>
    /// Malt den Himmel als Rundum-Bild (Breite = 360°, Höhe = 180°, Zeile 0 = unten), passend für Unitys
    /// Skybox "Panoramic" (Latitude-Longitude). Verlauf vom Horizont (= Nebelfarbe, damit ferne Häuser nahtlos
    /// übergehen) über Abendrot zum dunkleren Zenit, weiche Wolkenstreifen, Sterne bei dunklem Himmel und
    /// zwei Sonnen (nachts Monde): eine vor der Laufrichtung (+Z) und eine dahinter (-Z, sichtbar im Startmenü).
    /// </summary>
    public static class SkyPainter
    {
        public const float SunElevation = 9f, SunRadius = 4.2f, SunHalo = 16f;

        /// <summary>Malt das ganze Bild auf einmal.</summary>
        public static byte[] Paint(int width, int height, SkyColor tint, SkyColor fog)
        {
            var result = new byte[width * height * 4];
            PaintRows(result, width, height, 0, height, tint, fog);
            return result;
        }

        /// <summary>
        /// Malt nur die Zeilen [yFrom, yTo). So kann das Spiel den Himmel über mehrere Bilder verteilt neu malen,
        /// ohne zu ruckeln (funktioniert auch im Browser, wo es keine Hintergrund-Threads gibt).
        /// </summary>
        public static void PaintRows(byte[] target, int width, int height, int yFrom, int yTo, SkyColor tint, SkyColor fog)
        {
            if (width < 8 || height < 4) throw new ArgumentOutOfRangeException(nameof(width));
            if (target == null || target.Length < width * height * 4) throw new ArgumentException("Zielpuffer zu klein", nameof(target));
            yFrom = Math.Max(0, yFrom); yTo = Math.Min(height, yTo);

            var horizon = fog;
            var zenith = new SkyColor(tint.R * 0.38f + 0.03f, tint.G * 0.38f + 0.03f, tint.B * 0.38f + 0.12f);
            float night = Clamp01((0.22f - zenith.Luminance) / 0.12f);   // 0 = Tag/Abend, 1 = Nacht
            var glow = SkyColor.Lerp(tint, new SkyColor(1f, 0.74f, 0.56f), 0.55f * (1f - night));
            var mid = tint;
            var cloud = SkyColor.Lerp(glow, new SkyColor(1f, 0.93f, 0.96f), 0.45f * (1f - night * 0.6f));
            var core = night > 0.5f ? new SkyColor(0.96f, 0.97f, 1f) : new SkyColor(1f, 0.95f, 0.78f);
            var edge = night > 0.5f ? new SkyColor(0.78f, 0.84f, 0.95f) : new SkyColor(1f, 0.66f, 0.45f);
            float radius = night > 0.5f ? SunRadius * 0.7f : SunRadius;
            float haloSize = night > 0.5f ? SunHalo * 0.6f : SunHalo;
            const float soft = 1.0f;

            double se = SunElevation * Math.PI / 180.0;
            float sy = (float)Math.Sin(se), sz = (float)Math.Cos(se);

            for (int y = yFrom; y < yTo; y++)
            {
                float v = (y + 0.5f) / height;
                double lat = (1.0 - v) * Math.PI;                  // 0 = Zenit, π = Nadir
                float elev = 90f - (float)(lat * 180.0 / Math.PI); // Grad über dem Horizont
                float sinLat = (float)Math.Sin(lat), cosLat = (float)Math.Cos(lat);

                SkyColor row;
                if (elev <= 0f) row = SkyColor.Lerp(horizon, horizon.Scale(0.8f), -elev / 90f);
                else if (elev < 8f) row = SkyColor.Lerp(horizon, glow, Smooth(elev / 8f));
                else if (elev < 30f) row = SkyColor.Lerp(glow, mid, Smooth((elev - 8f) / 22f));
                else row = SkyColor.Lerp(mid, zenith, Smooth((elev - 30f) / 60f));

                bool nearSun = Math.Abs(elev - SunElevation) < haloSize + 1f;
                float cloudBand = elev > 5f && elev < 32f ? Bump(elev, 15f, 10f) : 0f;

                for (int x = 0; x < width; x++)
                {
                    float u = (x + 0.5f) / width;
                    var c = row;

                    if (cloudBand > 0f)
                    {
                        double a = u * Math.PI * 2.0;
                        float streak = (float)(0.5 * Math.Sin(3 * a + 1.3) + 0.35 * Math.Sin(7 * a + 0.4) + 0.25 * Math.Sin(13 * a + 2.1)) - 0.2f;
                        float layer = 0.55f + 0.45f * (float)Math.Sin(elev * 0.9 + 5 * a);
                        float alpha = Clamp01(streak) * cloudBand * layer * 0.5f;
                        if (alpha > 0f) c = SkyColor.Lerp(c, cloud, alpha);
                    }

                    if (night > 0f && elev > 14f)
                    {
                        uint h = Hash((uint)x, (uint)y);
                        if ((h & 1023u) < 4u)
                        {
                            float twinkle = 0.55f + 0.45f * ((h >> 10) & 255u) / 255f;
                            c = SkyColor.Lerp(c, new SkyColor(1f, 1f, 1f), night * twinkle * Clamp01((elev - 14f) / 15f));
                        }
                    }

                    if (nearSun)
                    {
                        double lon = (0.5 - u) * Math.PI * 2.0;
                        float dz = sinLat * (float)Math.Sin(lon);
                        float best = 999f;
                        for (int s = -1; s <= 1; s += 2)
                        {
                            float dot = cosLat * sy + dz * sz * s;
                            float ang = (float)(Math.Acos(Math.Max(-1f, Math.Min(1f, dot))) * 180.0 / Math.PI);
                            if (ang < best) best = ang;
                        }
                        if (best < haloSize)
                        {
                            float halo = 1f - best / haloSize;
                            c = SkyColor.Lerp(c, night > 0.5f ? edge : glow, halo * halo * (night > 0.5f ? 0.35f : 0.6f));
                        }
                        if (best < radius + soft)
                        {
                            var disc = SkyColor.Lerp(core, edge, best / radius);
                            c = SkyColor.Lerp(c, disc, Smooth((radius + soft - best) / (2f * soft)));
                        }
                    }

                    int o = (y * width + x) * 4;
                    target[o] = ToByte(c.R);
                    target[o + 1] = ToByte(c.G);
                    target[o + 2] = ToByte(c.B);
                    target[o + 3] = 255;
                }
            }
        }

        private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
        private static float Smooth(float t) { t = Clamp01(t); return t * t * (3f - 2f * t); }
        private static float Bump(float x, float center, float halfWidth) { float d = (x - center) / halfWidth; return d * d >= 1f ? 0f : (1f - d * d) * (1f - d * d); }
        private static byte ToByte(float v) => (byte)Math.Round(Clamp01(v) * 255f);

        private static uint Hash(uint x, uint y)
        {
            uint h = x * 374761393u + y * 668265263u;
            h = (h ^ (h >> 13)) * 1274126177u;
            return h ^ (h >> 16);
        }
    }
}
