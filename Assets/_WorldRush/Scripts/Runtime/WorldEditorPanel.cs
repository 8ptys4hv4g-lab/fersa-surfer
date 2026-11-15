using System.Collections.Generic;
using UnityEngine;
using WorldRush.Core;
using WorldRush.Core.World;

namespace WorldRush.Runtime
{
    /// <summary>
    /// Zeichenfläche des Welt-Editors (Draufsicht). Pinsel: Baum, Haus, Garten, Wasser, Hügel.
    /// Finger/Maus zeichnen; "In 3D bauen" macht daraus echte 3D-Objekte neben der Laufstrecke.
    /// Wasser über der Straße wird zur Surf-Strecke im eigenen Level – das sieht man sofort auf der Zeichenfläche.
    /// </summary>
    public sealed class WorldEditorPanel
    {
        private const float ViewHalfWidth = 60f;
        private BrushKind _brush = BrushKind.Tree;
        private List<Vec2> _current;
        private string _message = "";
        private int _surfVersion = -1;
        private List<(float start, float end)> _surf = new List<(float start, float end)>();

        public void Draw(GameRoot root, Rect card, float k, GUIStyle big, GUIStyle mid, GUIStyle small, GUIStyle chip, GUIStyle button, GUIStyle panel, Texture2D white)
        {
            GUI.Box(card, GUIContent.none, panel);
            GUI.Label(new Rect(card.x, card.y + 10 * k, card.width, 46 * k), L.T("world.title"), UiFit.Style(big, (int)(32 * k)));

            // Pinsel
            var brushes = new[] { (BrushKind.Water, "world.water"), (BrushKind.Tree, "world.tree"), (BrushKind.House, "world.house"), (BrushKind.Garden, "world.garden"), (BrushKind.Hill, "world.hill") };
            float bw = (card.width - 40 * k - 10 * k * (brushes.Length - 1)) / brushes.Length;
            for (int i = 0; i < brushes.Length; i++)
            {
                var prev = GUI.color;
                if (_brush == brushes[i].Item1) GUI.color = GameRoot.NeonCyan;
                if (UiFit.Button(new Rect(card.x + 20 * k + i * (bw + 10 * k), card.y + 60 * k, bw, 42 * k), L.T(brushes[i].Item2), button)) _brush = brushes[i].Item1;
                GUI.color = prev;
            }

            // Zeichenfläche
            var canvas = new Rect(card.x + 20 * k, card.y + 112 * k, card.width - 40 * k, card.height - 250 * k);
            DrawCanvas(root, canvas, white, k, chip);
            HandleInput(root, canvas);
            GUI.Label(new Rect(card.x + 10 * k, canvas.yMax + 4 * k, card.width - 20 * k, 40 * k), _message.Length > 0 ? _message : L.T("world.hint"),
                UiFit.Style(chip, (int)(14 * k), null, false, true));

            // Aktionen
            float y = card.yMax - 84 * k, w4 = (card.width - 70 * k) / 4f;
            if (UiFit.Button(new Rect(card.x + 20 * k, y, w4, 50 * k), L.T("world.undo"), button)) { root.WorldDoc.Undo(); _message = ""; }
            if (UiFit.Button(new Rect(card.x + 30 * k + w4, y, w4, 50 * k), L.T("world.clear"), button)) { root.WorldDoc.Clear(); _message = ""; }
            if (UiFit.Button(new Rect(card.x + 40 * k + 2 * w4, y, w4, 50 * k), L.T("world.sample"), button)) { root.LoadSampleWorld(); _message = ""; }
            if (UiFit.Button(new Rect(card.x + 50 * k + 3 * w4, y, w4, 50 * k), root.CustomWorldActive ? L.T("world.standard") : L.T("world.build"), button))
            {
                if (root.CustomWorldActive) { root.UseStandardWorld(); _message = L.T("world.standardOn"); }
                else if (root.WorldDoc.Strokes.Count == 0) _message = L.T("world.empty");
                else
                {
                    var notes = root.BuildCustomWorld();
                    _message = L.T("world.built") + (notes.Count > 0 ? "  " + notes[0] : "");
                }
            }
            if (root.CustomWorldActive)
                GUI.Label(new Rect(card.x, card.y + 14 * k, card.width - 20 * k, 30 * k), L.T("world.active"), UiFit.Style(chip, 0, TextAnchor.MiddleRight));
        }

        // Plan (Meter) ↔ Bildschirm
        private static Vector2 ToScreen(Rect c, Vec2 p) =>
            new Vector2(c.x + (p.X + ViewHalfWidth) / (2f * ViewHalfWidth) * c.width, c.yMax - p.Y / DrawingDocument.PlanLength * c.height);

        private static Vec2 ToPlan(Rect c, Vector2 s) =>
            new Vec2((s.x - c.x) / c.width * 2f * ViewHalfWidth - ViewHalfWidth, (c.yMax - s.y) / c.height * DrawingDocument.PlanLength);

        private void DrawCanvas(GameRoot root, Rect c, Texture2D white, float k, GUIStyle chip)
        {
            var prev = GUI.color;
            GUI.color = new Color(0.2f, 0.16f, 0.36f, 1f);
            GUI.DrawTexture(c, white);
            // Straße (fest) + Gehwege
            var a = ToScreen(c, new Vec2(-WorldBuilder.TrackHalfWidth, 0f));
            var b = ToScreen(c, new Vec2(WorldBuilder.TrackHalfWidth, 0f));
            GUI.color = new Color(0.62f, 0.6f, 0.72f, 1f);
            GUI.DrawTexture(new Rect(a.x - 6 * k, c.y, b.x - a.x + 12 * k, c.height), white);
            GUI.color = new Color(0.12f, 0.1f, 0.22f, 1f);
            GUI.DrawTexture(new Rect(a.x, c.y, b.x - a.x, c.height), white);

            // Surf-Strecken (aus dem gemalten Wasser) gleich auf der Straße zeigen
            if (_surfVersion != root.WorldDoc.Version || _surfVersion < 0)
            {
                _surf = WorldLevel.SurfSections(root.WorldDoc.Strokes);
                _surfVersion = root.WorldDoc.Version;
            }
            foreach (var (start, end) in _surf)
            {
                var top = ToScreen(c, new Vec2(-WorldLevel.RoadReach, end));
                var bottom = ToScreen(c, new Vec2(WorldLevel.RoadReach, start));
                var zone = Rect.MinMaxRect(top.x, top.y, bottom.x, bottom.y);
                GUI.color = new Color(0.25f, 0.8f, 1f, 0.55f);
                GUI.DrawTexture(zone, white);
                GUI.color = Color.white;
                var label = UiFit.Style(chip, (int)(12 * k), TextAnchor.MiddleCenter);
                GUI.Label(new Rect(zone.center.x - 50 * k, zone.center.y - 10 * k, 100 * k, 20 * k), L.T("world.surf"), label);
            }
            // Ziel-Linie
            var f0 = ToScreen(c, new Vec2(-WorldBuilder.TrackHalfWidth - 2f, WorldLevel.Length - 2f));
            GUI.color = new Color(1f, 1f, 1f, 0.8f);
            GUI.DrawTexture(new Rect(f0.x, c.y, (WorldBuilder.TrackHalfWidth + 2f) * 2f / (2f * ViewHalfWidth) * c.width, 3f * k), white);

            foreach (var st in root.WorldDoc.Strokes) DrawStroke(c, st.Brush, st.Points, white, k, 1f);
            if (_current != null) DrawStroke(c, _brush, _current, white, k, 0.7f);
            GUI.color = prev;
        }

        private static void DrawStroke(Rect c, BrushKind brush, IReadOnlyList<Vec2> pts, Texture2D white, float k, float alpha)
        {
            if (pts.Count == 0) return;
            if (brush == BrushKind.House)
            {
                Geometry.Bounds(pts, out var min, out var max);
                var p0 = ToScreen(c, new Vec2(min.X, max.Y));
                var p1 = ToScreen(c, new Vec2(max.X, min.Y));
                GUI.color = new Color(0.62f, 0.5f, 1f, alpha);
                GUI.DrawTexture(Rect.MinMaxRect(p0.x, p0.y, Mathf.Max(p1.x, p0.x + 4f), Mathf.Max(p1.y, p0.y + 4f)), white);
                return;
            }
            GUI.color = brush switch
            {
                BrushKind.Tree => new Color(0.45f, 0.95f, 0.55f, alpha),
                BrushKind.Water => new Color(0.3f, 0.72f, 1f, alpha),
                BrushKind.Hill => new Color(0.62f, 0.8f, 0.4f, alpha),
                _ => new Color(1f, 0.5f, 0.78f, alpha)
            };
            float dot = (brush == BrushKind.Water ? 12f : brush == BrushKind.Tree ? 9f : 6f) * k;
            for (int i = 0; i < pts.Count; i++)
            {
                var s = ToScreen(c, pts[i]);
                GUI.DrawTexture(new Rect(s.x - dot * 0.5f, s.y - dot * 0.5f, dot, dot), white);
                if (i == 0) continue;
                // Zwischenpunkte, damit Linien sichtbar durchgehen
                var s0 = ToScreen(c, pts[i - 1]);
                int steps = Mathf.Min(40, Mathf.CeilToInt(Vector2.Distance(s0, s) / (dot * 0.6f)));
                for (int j = 1; j < steps; j++)
                {
                    var m = Vector2.Lerp(s0, s, j / (float)steps);
                    GUI.DrawTexture(new Rect(m.x - dot * 0.3f, m.y - dot * 0.3f, dot * 0.6f, dot * 0.6f), white);
                }
            }
        }

        private void HandleInput(GameRoot root, Rect canvas)
        {
            var e = Event.current;
            switch (e.type)
            {
                case EventType.MouseDown when canvas.Contains(e.mousePosition):
                    _current = new List<Vec2> { ToPlan(canvas, e.mousePosition) };
                    _message = "";
                    e.Use();
                    break;
                case EventType.MouseDrag when _current != null:
                    var p = ToPlan(canvas, ClampTo(canvas, e.mousePosition));
                    var last = _current[_current.Count - 1];
                    if (Vec2.Distance(p, last) > 1.2f && _current.Count < 1500) _current.Add(p);
                    e.Use();
                    break;
                case EventType.MouseUp when _current != null:
                    Finish(root, ToPlan(canvas, ClampTo(canvas, e.mousePosition)));
                    e.Use();
                    break;
            }
        }

        private void Finish(GameRoot root, Vec2 end)
        {
            var pts = _current;
            _current = null;
            if (_brush == BrushKind.House)
            {
                root.WorldDoc.AddStroke(BrushKind.House, new List<Vec2> { pts[0], end }, 0f);
                return;
            }
            if (_brush == BrushKind.Garden && pts.Count < 3) { _message = L.T("world.hint"); return; }
            if (pts.Count == 1 && Vec2.Distance(pts[0], end) > 1.2f) pts.Add(end);
            root.WorldDoc.AddStroke(_brush, pts, 0.8f);
        }

        private static Vector2 ClampTo(Rect r, Vector2 p) => new Vector2(Mathf.Clamp(p.x, r.xMin, r.xMax), Mathf.Clamp(p.y, r.yMin, r.yMax));
    }
}
