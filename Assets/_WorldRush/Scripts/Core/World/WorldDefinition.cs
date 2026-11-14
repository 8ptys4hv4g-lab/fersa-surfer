using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace WorldRush.Core.World
{
    /// <summary>
    /// Eine gespeicherte Spieler-Welt. Gespeichert wird die ZEICHNUNG (Striche + Seed);
    /// der 3D-Weltplan wird beim Laden daraus neu und identisch berechnet.
    /// So geht beim Umwandeln nichts verloren und Dateien bleiben klein.
    /// Enthält bewusst keine persönlichen Daten.
    /// </summary>
    public sealed class WorldDefinition
    {
        public const int CurrentSchemaVersion = 1;
        public const int MaxNameLength = 40;

        public string WorldId = Guid.NewGuid().ToString("N");
        public string Name = "Meine Welt";
        public string ThemeId = "neon-garden-city";
        public int Seed;
        public List<Stroke> Strokes = new List<Stroke>();

        public WorldPlan BuildPlan() => new WorldBuilder(Seed).Build(Strokes);

        public static WorldDefinition FromDocument(DrawingDocument doc, string name, int seed)
        {
            return new WorldDefinition { Name = SanitizeName(name), Seed = seed, Strokes = new List<Stroke>(doc.Strokes) };
        }

        public static string SanitizeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Meine Welt";
            var sb = new StringBuilder();
            foreach (char c in name.Trim())
            {
                if (char.IsControl(c)) continue;
                sb.Append(c);
                if (sb.Length >= MaxNameLength) break;
            }
            return sb.Length == 0 ? "Meine Welt" : sb.ToString();
        }

        public string ToJson()
        {
            var strokes = new List<object>();
            foreach (var s in Strokes)
            {
                var pts = new List<object>(s.Points.Count * 2);
                foreach (var p in s.Points) { pts.Add(Math.Round(p.X, 2)); pts.Add(Math.Round(p.Y, 2)); }
                strokes.Add(new Dictionary<string, object> { { "brush", s.Brush.ToString() }, { "points", pts } });
            }
            return MiniJson.Serialize(new Dictionary<string, object>
            {
                { "schemaVersion", CurrentSchemaVersion },
                { "worldId", WorldId },
                { "name", Name },
                { "themeId", ThemeId },
                { "seed", Seed },
                { "strokes", strokes }
            });
        }

        /// <summary>Liest eine Welt und prüft alles streng. Ungültige Striche werden verworfen, nicht übernommen.</summary>
        public static bool TryFromJson(string json, out WorldDefinition world, out string error)
        {
            world = null;
            error = null;
            if (!MiniJson.TryParse(json, out var root) || !(root is Dictionary<string, object> obj))
            {
                error = "Datei ist kein gültiges JSON.";
                return false;
            }
            if (!(Get(obj, "schemaVersion") is double v) || (int)v < 1 || (int)v > CurrentSchemaVersion)
            {
                error = "Unbekannte Dateiversion.";
                return false;
            }

            var w = new WorldDefinition
            {
                WorldId = IsSafeId(Get(obj, "worldId") as string) ? (string)obj["worldId"] : Guid.NewGuid().ToString("N"),
                Name = SanitizeName(Get(obj, "name") as string),
                ThemeId = IsSafeId(Get(obj, "themeId") as string) ? (string)obj["themeId"] : "neon-garden-city",
                Seed = Get(obj, "seed") is double seed && Math.Abs(seed) < int.MaxValue ? (int)seed : 0
            };

            // Über das DrawingDocument laden: dort gelten dieselben Grenzen wie beim Zeichnen.
            var doc = new DrawingDocument();
            if (Get(obj, "strokes") is List<object> strokes)
            {
                foreach (var item in strokes)
                {
                    if (!(item is Dictionary<string, object> so)) continue;
                    if (!(Get(so, "brush") is string brushName) || !Enum.TryParse(brushName, false, out BrushKind brush) || !Enum.IsDefined(typeof(BrushKind), brush)) continue;
                    if (!(Get(so, "points") is List<object> raw) || raw.Count < 2) continue;
                    var pts = new List<Vec2>(raw.Count / 2);
                    for (int i = 0; i + 1 < raw.Count && pts.Count < DrawingDocument.MaxPointsPerStroke; i += 2)
                        if (raw[i] is double x && raw[i + 1] is double y) pts.Add(new Vec2((float)x, (float)y));
                    doc.AddStroke(brush, pts, 0f);
                }
            }
            w.Strokes = new List<Stroke>(doc.Strokes);
            world = w;
            return true;
        }

        private static object Get(Dictionary<string, object> d, string key) => d.TryGetValue(key, out var v) ? v : null;

        private static bool IsSafeId(string id)
        {
            if (string.IsNullOrEmpty(id) || id.Length > 64) return false;
            foreach (char c in id) if (!(char.IsLetterOrDigit(c) || c == '-' || c == '_')) return false;
            return true;
        }
    }

    /// <summary>Speichert Welten als einzelne Dateien in einem Ordner (atomar, mit Sicherung).</summary>
    public sealed class WorldRepository
    {
        private readonly string _folder;

        public WorldRepository(string folder)
        {
            _folder = folder ?? throw new ArgumentNullException(nameof(folder));
        }

        private string PathFor(string worldId) => Path.Combine(_folder, worldId + ".world.json");

        public void Save(WorldDefinition world)
        {
            Directory.CreateDirectory(_folder);
            AtomicFile.WriteAllText(PathFor(world.WorldId), world.ToJson());
        }

        public bool TryLoad(string worldId, out WorldDefinition world, out string error)
        {
            world = null;
            error = null;
            foreach (var path in new[] { PathFor(worldId), PathFor(worldId) + ".bak" })
            {
                try
                {
                    if (File.Exists(path) && WorldDefinition.TryFromJson(File.ReadAllText(path), out world, out error)) return true;
                }
                catch (IOException e) { error = e.Message; }
                catch (UnauthorizedAccessException e) { error = e.Message; }
            }
            error = error ?? "Welt nicht gefunden.";
            return false;
        }

        public List<string> ListIds()
        {
            var ids = new List<string>();
            if (!Directory.Exists(_folder)) return ids;
            foreach (var f in Directory.GetFiles(_folder, "*.world.json"))
                ids.Add(Path.GetFileName(f).Replace(".world.json", ""));
            ids.Sort(StringComparer.Ordinal);
            return ids;
        }

        public bool Delete(string worldId)
        {
            bool any = false;
            foreach (var path in new[] { PathFor(worldId), PathFor(worldId) + ".bak" })
                if (File.Exists(path)) { File.Delete(path); any = true; }
            return any;
        }
    }
}
