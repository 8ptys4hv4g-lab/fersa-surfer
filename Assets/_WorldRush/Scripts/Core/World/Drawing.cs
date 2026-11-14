using System;
using System.Collections.Generic;

namespace WorldRush.Core.World
{
    /// <summary>2D-Punkt auf dem Weltplan in Metern (x = seitlich, y = nach vorne/Z in 3D).</summary>
    public readonly struct Vec2
    {
        public Vec2(float x, float y) { X = x; Y = y; }
        public float X { get; }
        public float Y { get; }

        public static Vec2 operator +(Vec2 a, Vec2 b) => new Vec2(a.X + b.X, a.Y + b.Y);
        public static Vec2 operator -(Vec2 a, Vec2 b) => new Vec2(a.X - b.X, a.Y - b.Y);
        public static Vec2 operator *(Vec2 a, float s) => new Vec2(a.X * s, a.Y * s);
        public float Length => (float)Math.Sqrt(X * X + Y * Y);
        public static float Distance(Vec2 a, Vec2 b) => (a - b).Length;
        public static float Dot(Vec2 a, Vec2 b) => a.X * b.X + a.Y * b.Y;
        public bool IsFinite => !float.IsNaN(X) && !float.IsNaN(Y) && !float.IsInfinity(X) && !float.IsInfinity(Y);
        public override string ToString() => $"({X:0.0},{Y:0.0})";
    }

    /// <summary>Semantische Pinsel: Der Pinsel bestimmt, was aus dem Strich wird.</summary>
    public enum BrushKind { Road, Tree, House, Water, Hill, Garden }

    public sealed class Stroke
    {
        public Stroke(int id, BrushKind brush, IReadOnlyList<Vec2> points)
        {
            Id = id;
            Brush = brush;
            Points = points;
        }

        public int Id { get; }
        public BrushKind Brush { get; }
        public IReadOnlyList<Vec2> Points { get; }
    }

    /// <summary>
    /// Die Zeichnung des Spielers: Striche + Rückgängig/Wiederholen.
    /// Grenzen schützen vor zu großen oder manipulierten Dateien.
    /// </summary>
    public sealed class DrawingDocument
    {
        public const int MaxStrokes = 400;
        public const int MaxPointsPerStroke = 2000;
        public const float PlanHalfWidth = 100f;
        public const float PlanLength = 400f;

        private readonly List<Stroke> _strokes = new List<Stroke>();
        private readonly Stack<Action> _undo = new Stack<Action>();
        private readonly Stack<Action> _redo = new Stack<Action>();
        private readonly Stack<Action> _undoInverse = new Stack<Action>();
        private readonly Stack<Action> _redoInverse = new Stack<Action>();
        private int _nextId = 1;

        public IReadOnlyList<Stroke> Strokes => _strokes;
        public bool CanUndo => _undo.Count > 0;
        public bool CanRedo => _redo.Count > 0;
        public int Version { get; private set; }

        /// <summary>Fügt einen Strich hinzu (vereinfacht und auf den Plan begrenzt). Gibt null zurück, wenn ungültig.</summary>
        public Stroke AddStroke(BrushKind brush, IReadOnlyList<Vec2> rawPoints, float simplifyTolerance = 0.5f)
        {
            if (rawPoints == null || rawPoints.Count == 0) return null;
            if (_strokes.Count >= MaxStrokes) return null;

            var clean = new List<Vec2>(Math.Min(rawPoints.Count, MaxPointsPerStroke));
            for (int i = 0; i < rawPoints.Count && clean.Count < MaxPointsPerStroke; i++)
            {
                var p = rawPoints[i];
                if (!p.IsFinite) continue;
                clean.Add(new Vec2(Clamp(p.X, -PlanHalfWidth, PlanHalfWidth), Clamp(p.Y, 0f, PlanLength)));
            }
            if (clean.Count == 0) return null;

            var stroke = new Stroke(_nextId++, brush, StrokeSimplifier.Simplify(clean, simplifyTolerance));
            Execute(() => _strokes.Add(stroke), () => _strokes.Remove(stroke));
            return stroke;
        }

        public bool RemoveStroke(int id)
        {
            int index = _strokes.FindIndex(s => s.Id == id);
            if (index < 0) return false;
            var stroke = _strokes[index];
            Execute(() => _strokes.Remove(stroke), () => _strokes.Insert(Math.Min(index, _strokes.Count), stroke));
            return true;
        }

        public void Clear()
        {
            var backup = new List<Stroke>(_strokes);
            Execute(() => _strokes.Clear(), () => { _strokes.Clear(); _strokes.AddRange(backup); });
        }

        public bool Undo()
        {
            if (!CanUndo) return false;
            var doIt = _undo.Pop();
            var inverse = _undoInverse.Pop();
            inverse();
            _redo.Push(doIt);
            _redoInverse.Push(inverse);
            Version++;
            return true;
        }

        public bool Redo()
        {
            if (!CanRedo) return false;
            var doIt = _redo.Pop();
            var inverse = _redoInverse.Pop();
            doIt();
            _undo.Push(doIt);
            _undoInverse.Push(inverse);
            Version++;
            return true;
        }

        private void Execute(Action doIt, Action inverse)
        {
            doIt();
            _undo.Push(doIt);
            _undoInverse.Push(inverse);
            _redo.Clear();
            _redoInverse.Clear();
            Version++;
        }

        private static float Clamp(float v, float min, float max) => v < min ? min : v > max ? max : v;
    }

    /// <summary>Linienvereinfachung (Douglas-Peucker): wackelige Fingerlinien → wenige saubere Punkte.</summary>
    public static class StrokeSimplifier
    {
        public static List<Vec2> Simplify(IReadOnlyList<Vec2> points, float tolerance)
        {
            var result = new List<Vec2>();
            if (points == null || points.Count == 0) return result;
            if (points.Count < 3 || tolerance <= 0f)
            {
                result.AddRange(points);
                return result;
            }

            var keep = new bool[points.Count];
            keep[0] = keep[points.Count - 1] = true;
            var stack = new Stack<(int, int)>();
            stack.Push((0, points.Count - 1));
            while (stack.Count > 0)
            {
                var (first, last) = stack.Pop();
                float maxDist = 0f;
                int index = -1;
                for (int i = first + 1; i < last; i++)
                {
                    float d = Geometry.DistanceToSegment(points[i], points[first], points[last]);
                    if (d > maxDist) { maxDist = d; index = i; }
                }
                if (index >= 0 && maxDist > tolerance)
                {
                    keep[index] = true;
                    stack.Push((first, index));
                    stack.Push((index, last));
                }
            }
            for (int i = 0; i < points.Count; i++) if (keep[i]) result.Add(points[i]);
            return result;
        }
    }

    public static class Geometry
    {
        public static float DistanceToSegment(Vec2 p, Vec2 a, Vec2 b)
        {
            var ab = b - a;
            float lenSq = Vec2.Dot(ab, ab);
            if (lenSq < 1e-8f) return Vec2.Distance(p, a);
            float t = Vec2.Dot(p - a, ab) / lenSq;
            t = t < 0f ? 0f : t > 1f ? 1f : t;
            return Vec2.Distance(p, a + ab * t);
        }

        public static float DistanceToPolyline(Vec2 p, IReadOnlyList<Vec2> line)
        {
            if (line.Count == 1) return Vec2.Distance(p, line[0]);
            float best = float.MaxValue;
            for (int i = 0; i < line.Count - 1; i++) best = Math.Min(best, DistanceToSegment(p, line[i], line[i + 1]));
            return best;
        }

        public static float PolylineLength(IReadOnlyList<Vec2> line)
        {
            float sum = 0f;
            for (int i = 0; i < line.Count - 1; i++) sum += Vec2.Distance(line[i], line[i + 1]);
            return sum;
        }

        public static Vec2 Centroid(IReadOnlyList<Vec2> pts)
        {
            float x = 0, y = 0;
            foreach (var p in pts) { x += p.X; y += p.Y; }
            return new Vec2(x / pts.Count, y / pts.Count);
        }

        public static void Bounds(IReadOnlyList<Vec2> pts, out Vec2 min, out Vec2 max)
        {
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            foreach (var p in pts)
            {
                minX = Math.Min(minX, p.X); minY = Math.Min(minY, p.Y);
                maxX = Math.Max(maxX, p.X); maxY = Math.Max(maxY, p.Y);
            }
            min = new Vec2(minX, minY);
            max = new Vec2(maxX, maxY);
        }

        /// <summary>Punkte gleichmäßig entlang einer Linie (für Straßen-Spline und Baumreihen).</summary>
        public static List<Vec2> Resample(IReadOnlyList<Vec2> line, float spacing)
        {
            var result = new List<Vec2>();
            if (line.Count == 0) return result;
            result.Add(line[0]);
            float carry = 0f;
            for (int i = 0; i < line.Count - 1; i++)
            {
                var a = line[i];
                var b = line[i + 1];
                float segLen = Vec2.Distance(a, b);
                float pos = spacing - carry;
                while (pos <= segLen)
                {
                    result.Add(a + (b - a) * (pos / segLen));
                    pos += spacing;
                }
                carry = segLen - (pos - spacing);
            }
            return result;
        }

        /// <summary>Punkt-in-Polygon (Even-Odd-Regel).</summary>
        public static bool Contains(IReadOnlyList<Vec2> polygon, Vec2 p)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                var a = polygon[i];
                var b = polygon[j];
                if ((a.Y > p.Y) != (b.Y > p.Y) && p.X < (b.X - a.X) * (p.Y - a.Y) / (b.Y - a.Y) + a.X) inside = !inside;
            }
            return inside;
        }
    }
}
