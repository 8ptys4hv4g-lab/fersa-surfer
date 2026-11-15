using System;
using System.Collections.Generic;

namespace WorldRush.Core.Branding
{
    /// <summary>
    /// Zeichnet das Fesara-Logo ("Platsch-Welle") per Code: rosa-lila Verlauf, weiße Welle mit Schwung,
    /// türkises Wasser, Tropfen und Sonne. Eigenes Motiv, keine fremden Dateien.
    /// Koordinaten wie im Logo-Entwurf (120 × 120 Einheiten, y nach unten). Ergebnis: RGBA-Bytes, Zeile 0 = oben.
    /// Rein in C#, damit es ohne Unity getestet werden kann.
    /// </summary>
    public static class IconPainter
    {
        private const int Supersample = 3;

        private struct Rgb { public float R, G, B; public Rgb(int hex) { R = ((hex >> 16) & 255) / 255f; G = ((hex >> 8) & 255) / 255f; B = (hex & 255) / 255f; } }

        private sealed class Shape { public List<(float x, float y)> Points = new List<(float, float)>(); public Rgb Color; }

        private static readonly Rgb GradA = new Rgb(0xff7eb6), GradB = new Rgb(0x7b5cff);
        private static readonly Rgb White = new Rgb(0xffffff), Sea = new Rgb(0x2ad1e8), Sun = new Rgb(0xffe066);

        /// <param name="square">true = App-Icon (quadratisch), false = breites Titelbild (Motiv rechts, Wasser über die ganze Breite).</param>
        public static byte[] Paint(int width, int height, bool square)
        {
            if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            float s = (square ? Math.Min(width, height) : height) / 120f;
            float ox = square ? (width - 120f * s) * 0.5f : width - 120f * s - width * 0.06f;
            float oy = square ? (height - 120f * s) * 0.5f : 0f;
            float left = -ox / s - 2f, right = (width - ox) / s + 2f;

            var shapes = new List<Shape>();
            if (!square)
            {
                // Welle und Wasser nach links verlängern, damit das breite Bild nicht abgeschnitten wirkt.
                var band = new Shape { Color = White };
                for (float x = left; x <= -5f; x += 1f) band.Points.Add((x, 78f - 6f * (float)Math.Sin((x + 5f) * 0.1f)));
                for (float x = -5f; x >= left; x -= 1f) band.Points.Add((x, 100f - 4f * (float)Math.Sin((x + 5f) * 0.08f)));
                shapes.Add(band);
            }
            shapes.Add(new PathBuilder(-5, 78).C(15, 66, 30, 88, 52, 76).C(70, 66, 82, 52, 98, 58).C(110, 62, 112, 74, 104, 80)
                .C(98, 84, 92, 78, 96, 72).C(84, 70, 78, 84, 66, 92).C(44, 106, 20, 92, -5, 100).Build(White));
            if (square)
                shapes.Add(new PathBuilder(-5, 104).C(20, 96, 40, 112, 62, 104).C(84, 96, 100, 96, 125, 104).L(125, 125).L(-5, 125).Build(Sea));
            else
            {
                var sea = new Shape { Color = Sea };
                for (float x = left; x <= right; x += 1f) sea.Points.Add((x, 104f + 4f * (float)Math.Sin(x * 0.075f + 1.2f)));
                sea.Points.Add((right, 125f)); sea.Points.Add((left, 125f));
                shapes.Add(sea);
            }
            shapes.Add(Circle(38, 44, 7, White));
            shapes.Add(Circle(56, 32, 5, White));
            shapes.Add(Circle(26, 28, 4, White));
            shapes.Add(Circle(88, 30, 10, Sun));
            if (!square)
            {
                shapes.Add(Circle(-60, 40, 5, White));
                shapes.Add(Circle(-40, 26, 3.5f, White));
                shapes.Add(Circle(-150, 50, 4, White));
            }

            // In Pixel umrechnen
            foreach (var sh in shapes)
                for (int i = 0; i < sh.Points.Count; i++)
                    sh.Points[i] = (ox + sh.Points[i].x * s, oy + sh.Points[i].y * s);

            var result = new byte[width * height * 4];
            int sw = width * Supersample;
            var rowR = new float[sw]; var rowG = new float[sw]; var rowB = new float[sw];
            var accR = new float[width]; var accG = new float[width]; var accB = new float[width];
            var xs = new List<float>();
            float inv = 1f / (Supersample * Supersample);

            for (int py = 0; py < height; py++)
            {
                Array.Clear(accR, 0, width); Array.Clear(accG, 0, width); Array.Clear(accB, 0, width);
                for (int sy = 0; sy < Supersample; sy++)
                {
                    float y = py + (sy + 0.5f) / Supersample;
                    for (int sx = 0; sx < sw; sx++)
                    {
                        float x = (sx + 0.5f) / Supersample;
                        float t = Clamp01((x / width + y / height) * 0.5f);
                        rowR[sx] = GradA.R + (GradB.R - GradA.R) * t;
                        rowG[sx] = GradA.G + (GradB.G - GradA.G) * t;
                        rowB[sx] = GradA.B + (GradB.B - GradA.B) * t;
                    }
                    foreach (var sh in shapes)
                    {
                        xs.Clear();
                        var p = sh.Points;
                        for (int i = 0, j = p.Count - 1; i < p.Count; j = i++)
                        {
                            var a = p[j]; var b = p[i];
                            if ((a.y <= y && b.y > y) || (b.y <= y && a.y > y))
                                xs.Add(a.x + (y - a.y) * (b.x - a.x) / (b.y - a.y));
                        }
                        if (xs.Count < 2) continue;
                        xs.Sort();
                        for (int k = 0; k + 1 < xs.Count; k += 2)
                        {
                            int from = Math.Max(0, (int)Math.Ceiling(xs[k] * Supersample - 0.5f));
                            int to = Math.Min(sw - 1, (int)Math.Floor(xs[k + 1] * Supersample - 0.5f));
                            for (int sx = from; sx <= to; sx++) { rowR[sx] = sh.Color.R; rowG[sx] = sh.Color.G; rowB[sx] = sh.Color.B; }
                        }
                    }
                    for (int sx = 0; sx < sw; sx++)
                    {
                        int px = sx / Supersample;
                        accR[px] += rowR[sx]; accG[px] += rowG[sx]; accB[px] += rowB[sx];
                    }
                }
                for (int px = 0; px < width; px++)
                {
                    int o = (py * width + px) * 4;
                    result[o] = ToByte(accR[px] * inv);
                    result[o + 1] = ToByte(accG[px] * inv);
                    result[o + 2] = ToByte(accB[px] * inv);
                    result[o + 3] = 255;
                }
            }
            return result;
        }

        private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
        private static byte ToByte(float v) => (byte)Math.Round(Clamp01(v) * 255f);

        private static Shape Circle(float cx, float cy, float r, Rgb color)
        {
            var sh = new Shape { Color = color };
            for (int i = 0; i < 64; i++)
            {
                double a = i * Math.PI * 2 / 64;
                sh.Points.Add((cx + r * (float)Math.Cos(a), cy + r * (float)Math.Sin(a)));
            }
            return sh;
        }

        private sealed class PathBuilder
        {
            private readonly List<(float x, float y)> _pts = new List<(float, float)>();
            private float _x, _y;
            public PathBuilder(float x, float y) { _x = x; _y = y; _pts.Add((x, y)); }
            public PathBuilder L(float x, float y) { _x = x; _y = y; _pts.Add((x, y)); return this; }
            public PathBuilder C(float x1, float y1, float x2, float y2, float x, float y)
            {
                for (int i = 1; i <= 24; i++)
                {
                    float t = i / 24f, u = 1f - t;
                    float bx = u * u * u * _x + 3 * u * u * t * x1 + 3 * u * t * t * x2 + t * t * t * x;
                    float by = u * u * u * _y + 3 * u * u * t * y1 + 3 * u * t * t * y2 + t * t * t * y;
                    _pts.Add((bx, by));
                }
                _x = x; _y = y;
                return this;
            }
            public Shape Build(Rgb color) => new Shape { Points = _pts, Color = color };
        }
    }
}
