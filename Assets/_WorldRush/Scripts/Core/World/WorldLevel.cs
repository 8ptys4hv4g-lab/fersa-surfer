using System;
using System.Collections.Generic;

namespace WorldRush.Core.World
{
    /// <summary>
    /// Macht aus einer Zeichnung ein spielbares Level: Wasser, das über die Straße gemalt wird, wird zur Surf-Strecke.
    /// Regeln (damit es immer fair und spielbar bleibt):
    /// - Die ersten <see cref="DryStart"/> m sind trocken (Anlauf), kurz vor dem Ziel kein Wasser.
    /// - Jede Surf-Strecke ist mindestens <see cref="MinSurf"/> m lang (ein Klecks reicht schon).
    /// - Abschnitte mit weniger als <see cref="MergeGap"/> m Abstand werden zusammengelegt.
    /// </summary>
    public static class WorldLevel
    {
        /// <summary>Kennung des eigenen Levels (für Sterne usw.), getrennt von den normalen Levels 0, 1, 2 …</summary>
        public const int CustomIndex = 999;
        public const float Length = DrawingDocument.PlanLength;
        public const float DryStart = 40f;
        public const float MinSurf = 40f;
        public const float MergeGap = 25f;
        /// <summary>So nah an der Straßenmitte muss Wasser gemalt sein, damit es zur Surf-Strecke wird.</summary>
        public const float RoadReach = WorldBuilder.TrackHalfWidth + 4f;
        public static float LastWaterEnd => Length - LevelDefinition.FinishClearance - 10f;

        public static LevelDefinition Create(IReadOnlyList<Stroke> strokes, string name)
            => new LevelDefinition(CustomIndex, string.IsNullOrWhiteSpace(name) ? "Meine Welt" : name, Length, SurfSections(strokes));

        /// <summary>Berührt dieser Wasser-Strich die Straße?</summary>
        public static bool TouchesRoad(Stroke s) => s != null && s.Brush == BrushKind.Water && RoadSpan(s.Points, out _, out _);

        /// <summary>Surf-Abschnitte (Start, Ende) in Metern, sortiert und ohne Überschneidung.</summary>
        public static List<(float start, float end)> SurfSections(IReadOnlyList<Stroke> strokes)
        {
            var raw = new List<(float start, float end)>();
            if (strokes != null)
                foreach (var s in strokes)
                {
                    if (s == null || s.Brush != BrushKind.Water) continue;
                    if (!RoadSpan(s.Points, out float zMin, out float zMax)) continue;
                    raw.Add(Fit(zMin, zMax));
                }
            raw.Sort((a, b) => a.start.CompareTo(b.start));

            var merged = new List<(float start, float end)>();
            foreach (var r in raw)
            {
                if (merged.Count > 0 && r.start - merged[merged.Count - 1].end < MergeGap)
                {
                    var last = merged[merged.Count - 1];
                    merged[merged.Count - 1] = (last.start, Math.Min(LastWaterEnd, Math.Max(last.end, r.end)));
                }
                else merged.Add(r);
            }
            return merged;
        }

        /// <summary>Mindestlänge herstellen und in den erlaubten Bereich schieben.</summary>
        private static (float start, float end) Fit(float zMin, float zMax)
        {
            float start = zMin, end = zMax;
            if (end - start < MinSurf)
            {
                float mid = (start + end) * 0.5f;
                start = mid - MinSurf * 0.5f;
                end = mid + MinSurf * 0.5f;
            }
            if (start < DryStart) { end += DryStart - start; start = DryStart; }
            if (end > LastWaterEnd) { start -= end - LastWaterEnd; end = LastWaterEnd; }
            if (start < DryStart) start = DryStart; // sehr lange Abschnitte: einfach kürzen
            return (start, end);
        }

        /// <summary>Z-Bereich, in dem der Strich das Straßenband |x| ≤ RoadReach berührt (auch Linien, die quer drüber gehen).</summary>
        private static bool RoadSpan(IReadOnlyList<Vec2> pts, out float zMin, out float zMax)
        {
            float lo = float.MaxValue, hi = float.MinValue;
            bool hit = false;
            if (pts != null)
                for (int i = 0; i < pts.Count; i++)
                {
                    var p = pts[i];
                    if (Math.Abs(p.X) <= RoadReach) { hit = true; lo = Math.Min(lo, p.Y); hi = Math.Max(hi, p.Y); }
                    if (i == 0) continue;
                    var a = pts[i - 1];
                    // Strecke a→p auf das Band |x| ≤ RoadReach zuschneiden (Linien quer über die Straße)
                    float dx = p.X - a.X;
                    if (Math.Abs(dx) < 1e-5f) continue;
                    for (int b = -1; b <= 1; b += 2)
                    {
                        float t = (b * RoadReach - a.X) / dx;
                        if (t <= 0f || t >= 1f) continue;
                        float z = a.Y + (p.Y - a.Y) * t;
                        hit = true; lo = Math.Min(lo, z); hi = Math.Max(hi, z);
                    }
                }
            zMin = lo; zMax = hi;
            return hit;
        }
    }
}
