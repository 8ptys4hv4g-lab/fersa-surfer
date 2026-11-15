using System;
using System.Collections.Generic;

namespace WorldRush.Core.World
{
    public enum WorldObjectKind { Tree, House, Flower, Hill }

    /// <summary>Ein platziertes 3D-Objekt. Nur Daten: Unity setzt später das passende Prefab ein.</summary>
    public sealed class WorldObject
    {
        public WorldObjectKind Kind;
        public string PrefabId;
        public float X;
        public float Z;
        public float RotationY;
        public float Scale = 1f;
        /// <summary>Für Häuser/Hügel: Grundfläche in Metern.</summary>
        public float Width;
        public float Depth;
        public int SourceStrokeId;
    }

    public sealed class WorldPlan
    {
        public List<Vec2> TrackPath = new List<Vec2>();
        public bool TrackIsFallback;
        public List<WorldObject> Objects = new List<WorldObject>();
        public List<List<Vec2>> WaterAreas = new List<List<Vec2>>();
        public List<string> Warnings = new List<string>();
    }

    /// <summary>
    /// Übersetzt Striche in einen sicheren Weltplan (V1, ohne KI):
    /// - Erster Straßen-Strich wird zur Laufstrecke (geglättet, geprüft; sonst sichere Gerade).
    /// - Baum: Punkt = ein Baum, Linie = Baumreihe.
    /// - Haus: Umriss = Haus in passender Größe.
    /// - Wasser: Fläche = See.  Hügel: Fläche = Hügel.  Garten: Fläche = verteilte Blumen.
    /// Objekte auf der Laufstrecke werden automatisch zur Seite geschoben.
    /// </summary>
    public sealed class WorldBuilder
    {
        public const float TrackHalfWidth = 5f;      // 3 Spuren à 2,5 m + Rand
        public const float ObjectClearance = 2.5f;   // zusätzlicher Abstand zur Strecke
        public const float MinTrackLength = 120f;
        public const float MaxTurnDegreesPer10m = 25f;
        public const float TrackSampleSpacing = 10f;
        public const int MaxObjects = 1500;

        private readonly int _seed;

        public WorldBuilder(int seed) { _seed = seed; }

        public WorldPlan Build(IReadOnlyList<Stroke> strokes)
        {
            var plan = new WorldPlan();
            var random = new DeterministicRandom(_seed);

            Stroke road = null;
            foreach (var s in strokes) if (s.Brush == BrushKind.Road) { road = s; break; }
            BuildTrack(road, plan);

            foreach (var s in strokes)
            {
                switch (s.Brush)
                {
                    case BrushKind.Tree: AddTrees(s, plan, random); break;
                    case BrushKind.House: AddHouse(s, plan, random); break;
                    case BrushKind.Water: AddWater(s, plan); break;
                    case BrushKind.Hill: AddHill(s, plan); break;
                    case BrushKind.Garden: AddGarden(s, plan, random); break;
                }
            }

            KeepTrackClear(plan);
            if (plan.Objects.Count > MaxObjects)
            {
                plan.Objects.RemoveRange(MaxObjects, plan.Objects.Count - MaxObjects);
                plan.Warnings.Add($"Zu viele Objekte – nur die ersten {MaxObjects} werden gebaut.");
            }
            return plan;
        }

        // ---------- Strecke ----------

        private static void BuildTrack(Stroke road, WorldPlan plan)
        {
            if (road == null || road.Points.Count < 2)
            {
                SetFallbackTrack(plan, "Keine Straße gezeichnet – es wird eine gerade Strecke verwendet.");
                return;
            }

            var points = new List<Vec2>(road.Points);
            // Strecke läuft immer nach vorne: falls rückwärts gezeichnet, umdrehen.
            if (points[points.Count - 1].Y < points[0].Y) points.Reverse();

            var sampled = Geometry.Resample(points, TrackSampleSpacing);
            if (Geometry.PolylineLength(sampled) < MinTrackLength)
            {
                SetFallbackTrack(plan, $"Die Straße ist kürzer als {MinTrackLength:0} m – es wird eine gerade Strecke verwendet.");
                return;
            }
            if (!IsTrackSmooth(sampled, out string reason))
            {
                SetFallbackTrack(plan, reason + " Es wird eine sichere Strecke verwendet.");
                return;
            }
            plan.TrackPath = sampled;
        }

        public static bool IsTrackSmooth(IReadOnlyList<Vec2> sampled, out string reason)
        {
            reason = null;
            for (int i = 1; i < sampled.Count - 1; i++)
            {
                var a = sampled[i] - sampled[i - 1];
                var b = sampled[i + 1] - sampled[i];
                if (b.Y <= 0f)
                {
                    reason = "Die Straße läuft rückwärts.";
                    return false;
                }
                float la = a.Length, lb = b.Length;
                if (la < 1e-3f || lb < 1e-3f) continue;
                float cos = Vec2.Dot(a, b) / (la * lb);
                cos = cos > 1f ? 1f : cos < -1f ? -1f : cos;
                float deg = (float)(Math.Acos(cos) * 180.0 / Math.PI);
                if (deg > MaxTurnDegreesPer10m)
                {
                    reason = "Die Straße hat eine zu enge Kurve.";
                    return false;
                }
            }
            return true;
        }

        private static void SetFallbackTrack(WorldPlan plan, string warning)
        {
            plan.TrackIsFallback = true;
            plan.TrackPath = Geometry.Resample(new List<Vec2> { new Vec2(0, 0), new Vec2(0, DrawingDocument.PlanLength) }, TrackSampleSpacing);
            plan.Warnings.Add(warning);
        }

        // ---------- Objekte ----------

        private static void AddTrees(Stroke s, WorldPlan plan, DeterministicRandom r)
        {
            float length = Geometry.PolylineLength(s.Points);
            var spots = length < 3f ? new List<Vec2> { Geometry.Centroid(s.Points) } : Geometry.Resample(s.Points, 4f);
            foreach (var p in spots)
                plan.Objects.Add(new WorldObject
                {
                    Kind = WorldObjectKind.Tree, PrefabId = "tree-a", X = p.X, Z = p.Y,
                    RotationY = r.Range(0f, 360f), Scale = r.Range(0.85f, 1.2f), SourceStrokeId = s.Id
                });
        }

        private static void AddHouse(Stroke s, WorldPlan plan, DeterministicRandom r)
        {
            Geometry.Bounds(s.Points, out var min, out var max);
            float w = Math.Max(4f, Math.Min(30f, max.X - min.X));
            float d = Math.Max(4f, Math.Min(30f, max.Y - min.Y));
            var c = new Vec2((min.X + max.X) * 0.5f, (min.Y + max.Y) * 0.5f);
            plan.Objects.Add(new WorldObject
            {
                Kind = WorldObjectKind.House, PrefabId = "house-a", X = c.X, Z = c.Y,
                Width = w, Depth = d, RotationY = 0f, Scale = 1f, SourceStrokeId = s.Id
            });
        }

        private static void AddWater(Stroke s, WorldPlan plan)
        {
            // Wasser über der Straße wird zur Surf-Strecke (siehe WorldLevel) – kein See.
            if (WorldLevel.TouchesRoad(s)) return;
            if (s.Points.Count < 3)
            {
                // Antippen oder kurzer Strich: kleiner runder Teich
                var c = Geometry.Centroid(s.Points);
                var pond = new List<Vec2>();
                for (int i = 0; i < 12; i++)
                {
                    double a = i * Math.PI * 2 / 12;
                    pond.Add(new Vec2(c.X + 5f * (float)Math.Cos(a), c.Y + 5f * (float)Math.Sin(a)));
                }
                plan.WaterAreas.Add(pond);
                return;
            }
            plan.WaterAreas.Add(new List<Vec2>(s.Points)); // Polygon wird automatisch geschlossen
        }

        private static void AddHill(Stroke s, WorldPlan plan)
        {
            Geometry.Bounds(s.Points, out var min, out var max);
            var c = new Vec2((min.X + max.X) * 0.5f, (min.Y + max.Y) * 0.5f);
            plan.Objects.Add(new WorldObject
            {
                Kind = WorldObjectKind.Hill, PrefabId = "hill-a", X = c.X, Z = c.Y,
                Width = Math.Max(6f, max.X - min.X), Depth = Math.Max(6f, max.Y - min.Y), SourceStrokeId = s.Id
            });
        }

        private static void AddGarden(Stroke s, WorldPlan plan, DeterministicRandom r)
        {
            if (s.Points.Count < 3) return;
            Geometry.Bounds(s.Points, out var min, out var max);
            float area = (max.X - min.X) * (max.Y - min.Y);
            int count = Math.Min(80, Math.Max(3, (int)(area / 6f)));
            for (int i = 0, tries = 0; i < count && tries < count * 10; tries++)
            {
                var p = new Vec2(r.Range(min.X, max.X), r.Range(min.Y, max.Y));
                if (!Geometry.Contains(s.Points, p)) continue;
                plan.Objects.Add(new WorldObject
                {
                    Kind = WorldObjectKind.Flower, PrefabId = "flower-a", X = p.X, Z = p.Y,
                    RotationY = r.Range(0f, 360f), Scale = r.Range(0.7f, 1.2f), SourceStrokeId = s.Id
                });
                i++;
            }
        }

        // ---------- Strecke freihalten ----------

        private static void KeepTrackClear(WorldPlan plan)
        {
            int moved = 0;
            foreach (var o in plan.Objects)
            {
                float radius = o.Kind == WorldObjectKind.House || o.Kind == WorldObjectKind.Hill
                    ? Math.Max(o.Width, o.Depth) * 0.5f
                    : 0.5f;
                float needed = TrackHalfWidth + ObjectClearance + radius;
                var p = new Vec2(o.X, o.Z);
                float dist = Geometry.DistanceToPolyline(p, plan.TrackPath);
                if (dist >= needed) continue;

                // Zur nächstgelegenen Seite der Strecke wegschieben.
                float trackX = TrackXAt(plan.TrackPath, o.Z);
                float side = o.X >= trackX ? 1f : -1f;
                o.X = trackX + side * (needed + 0.1f);
                moved++;
            }
            if (moved > 0) plan.Warnings.Add($"{moved} Objekt(e) standen auf der Laufstrecke und wurden zur Seite verschoben.");
        }

        /// <summary>X-Position der Strecke bei gegebener Tiefe (lineare Interpolation).</summary>
        public static float TrackXAt(IReadOnlyList<Vec2> path, float z)
        {
            if (path.Count == 0) return 0f;
            if (z <= path[0].Y) return path[0].X;
            for (int i = 0; i < path.Count - 1; i++)
            {
                var a = path[i];
                var b = path[i + 1];
                if (z >= a.Y && z <= b.Y)
                {
                    float t = (b.Y - a.Y) < 1e-4f ? 0f : (z - a.Y) / (b.Y - a.Y);
                    return a.X + (b.X - a.X) * t;
                }
            }
            return path[path.Count - 1].X;
        }
    }
}
