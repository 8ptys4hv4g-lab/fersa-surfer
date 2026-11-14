using System.Collections.Generic;
using NUnit.Framework;
using WorldRush.Core.World;

namespace WorldRush.Tests
{
    public class DrawingDocumentTests
    {
        private static List<Vec2> Line(params float[] xy)
        {
            var l = new List<Vec2>();
            for (int i = 0; i < xy.Length; i += 2) l.Add(new Vec2(xy[i], xy[i + 1]));
            return l;
        }

        [Test]
        public void UndoRedoAddAndRemove()
        {
            var doc = new DrawingDocument();
            var s = doc.AddStroke(BrushKind.Tree, Line(10, 10));
            Assert.AreEqual(1, doc.Strokes.Count);
            Assert.IsTrue(doc.Undo());
            Assert.AreEqual(0, doc.Strokes.Count);
            Assert.IsTrue(doc.Redo());
            Assert.AreEqual(1, doc.Strokes.Count);

            Assert.IsTrue(doc.RemoveStroke(s.Id));
            Assert.AreEqual(0, doc.Strokes.Count);
            doc.Undo();
            Assert.AreEqual(s.Id, doc.Strokes[0].Id);
        }

        [Test]
        public void NewActionClearsRedo()
        {
            var doc = new DrawingDocument();
            doc.AddStroke(BrushKind.Tree, Line(1, 1));
            doc.Undo();
            doc.AddStroke(BrushKind.House, Line(5, 5, 10, 10));
            Assert.IsFalse(doc.CanRedo);
        }

        [Test]
        public void ClearCanBeUndone()
        {
            var doc = new DrawingDocument();
            doc.AddStroke(BrushKind.Tree, Line(1, 1));
            doc.AddStroke(BrushKind.Tree, Line(2, 2));
            doc.Clear();
            Assert.AreEqual(0, doc.Strokes.Count);
            doc.Undo();
            Assert.AreEqual(2, doc.Strokes.Count);
        }

        [Test]
        public void InvalidPointsAreRemovedAndClamped()
        {
            var doc = new DrawingDocument();
            var s = doc.AddStroke(BrushKind.Tree, new List<Vec2> { new Vec2(float.NaN, 1), new Vec2(9999, -50) }, 0f);
            Assert.AreEqual(1, s.Points.Count);
            Assert.AreEqual(DrawingDocument.PlanHalfWidth, s.Points[0].X);
            Assert.AreEqual(0f, s.Points[0].Y);
            Assert.IsNull(doc.AddStroke(BrushKind.Tree, new List<Vec2>()));
        }

        [Test]
        public void StrokeLimitIsEnforced()
        {
            var doc = new DrawingDocument();
            for (int i = 0; i < DrawingDocument.MaxStrokes; i++) Assert.IsNotNull(doc.AddStroke(BrushKind.Tree, Line(1, 1)));
            Assert.IsNull(doc.AddStroke(BrushKind.Tree, Line(1, 1)));
        }

        [Test]
        public void SimplifierRemovesWobble()
        {
            var wobbly = new List<Vec2>();
            for (int i = 0; i <= 100; i++) wobbly.Add(new Vec2((i % 2) * 0.1f, i));
            var simple = StrokeSimplifier.Simplify(wobbly, 0.5f);
            Assert.AreEqual(2, simple.Count);
        }

        [Test]
        public void SimplifierKeepsCorners()
        {
            var simple = StrokeSimplifier.Simplify(Line(0, 0, 5, 0, 10, 0, 10, 5, 10, 10), 0.5f);
            Assert.AreEqual(3, simple.Count);
        }
    }

    public class WorldBuilderTests
    {
        private static List<Vec2> Line(params float[] xy)
        {
            var l = new List<Vec2>();
            for (int i = 0; i < xy.Length; i += 2) l.Add(new Vec2(xy[i], xy[i + 1]));
            return l;
        }

        /// <summary>Abnahmetest aus dem Masterplan: geschwungene Straße, 3 Häuser, 5 Bäume, ein See.</summary>
        [Test]
        public void AcceptanceScenarioBuildsSafeWorld()
        {
            var doc = new DrawingDocument();
            var road = new List<Vec2>();
            for (int z = 0; z <= 300; z += 5) road.Add(new Vec2(12f * (float)System.Math.Sin(z / 60.0), z));
            doc.AddStroke(BrushKind.Road, road);
            doc.AddStroke(BrushKind.House, Line(20, 40, 30, 40, 30, 52, 20, 52));
            doc.AddStroke(BrushKind.House, Line(-30, 90, -18, 90, -18, 100, -30, 100));
            doc.AddStroke(BrushKind.House, Line(-4, 150, 4, 150, 4, 158, -4, 158)); // liegt auf der Straße!
            foreach (var x in new[] { -25f, -20f, 25f, 30f, 35f }) doc.AddStroke(BrushKind.Tree, Line(x, 200));
            doc.AddStroke(BrushKind.Water, Line(40, 220, 60, 220, 65, 250, 45, 260));

            var plan = new WorldBuilder(42).Build(doc.Strokes);

            Assert.IsFalse(plan.TrackIsFallback, string.Join(" | ", plan.Warnings));
            Assert.AreEqual(3, plan.Objects.FindAll(o => o.Kind == WorldObjectKind.House).Count);
            Assert.AreEqual(5, plan.Objects.FindAll(o => o.Kind == WorldObjectKind.Tree).Count);
            Assert.AreEqual(1, plan.WaterAreas.Count);
            AssertTrackClear(plan);
            Assert.IsTrue(plan.Warnings.Exists(w => w.Contains("verschoben")));
        }

        [Test]
        public void NoRoadGivesStraightFallback()
        {
            var plan = new WorldBuilder(1).Build(new List<Stroke>());
            Assert.IsTrue(plan.TrackIsFallback);
            Assert.Greater(plan.TrackPath.Count, 10);
            Assert.IsNotEmpty(plan.Warnings);
        }

        [Test]
        public void ZigZagRoadIsReplacedBySafeTrack()
        {
            var doc = new DrawingDocument();
            doc.AddStroke(BrushKind.Road, Line(0, 0, 40, 20, -40, 40, 40, 60, -40, 80, 40, 100, -40, 120, 40, 140, 0, 200), 0f);
            var plan = new WorldBuilder(1).Build(doc.Strokes);
            Assert.IsTrue(plan.TrackIsFallback);
            StringAssert.Contains("Kurve", string.Join(" ", plan.Warnings));
        }

        [Test]
        public void ShortRoadIsReplacedBySafeTrack()
        {
            var doc = new DrawingDocument();
            doc.AddStroke(BrushKind.Road, Line(0, 0, 0, 50));
            Assert.IsTrue(new WorldBuilder(1).Build(doc.Strokes).TrackIsFallback);
        }

        [Test]
        public void RoadDrawnBackwardsIsReversed()
        {
            var doc = new DrawingDocument();
            doc.AddStroke(BrushKind.Road, Line(0, 300, 0, 0));
            var plan = new WorldBuilder(1).Build(doc.Strokes);
            Assert.IsFalse(plan.TrackIsFallback);
            Assert.Less(plan.TrackPath[0].Y, plan.TrackPath[plan.TrackPath.Count - 1].Y);
        }

        [Test]
        public void TreeLineBecomesSeveralTrees()
        {
            var doc = new DrawingDocument();
            doc.AddStroke(BrushKind.Tree, Line(20, 0, 20, 40));
            var plan = new WorldBuilder(1).Build(doc.Strokes);
            Assert.GreaterOrEqual(plan.Objects.FindAll(o => o.Kind == WorldObjectKind.Tree).Count, 10);
        }

        [Test]
        public void GardenFlowersStayInsideOutline()
        {
            var doc = new DrawingDocument();
            var outline = Line(20, 20, 40, 20, 40, 40, 20, 40);
            doc.AddStroke(BrushKind.Garden, outline);
            var plan = new WorldBuilder(3).Build(doc.Strokes);
            var flowers = plan.Objects.FindAll(o => o.Kind == WorldObjectKind.Flower);
            Assert.Greater(flowers.Count, 3);
            foreach (var f in flowers) Assert.IsTrue(Geometry.Contains(outline, new Vec2(f.X, f.Z)));
        }

        [Test]
        public void SameSeedSameWorld()
        {
            var doc = new DrawingDocument();
            doc.AddStroke(BrushKind.Garden, Line(20, 20, 40, 20, 40, 40, 20, 40));
            var a = new WorldBuilder(9).Build(doc.Strokes);
            var b = new WorldBuilder(9).Build(doc.Strokes);
            Assert.AreEqual(a.Objects.Count, b.Objects.Count);
            for (int i = 0; i < a.Objects.Count; i++) Assert.AreEqual(a.Objects[i].X, b.Objects[i].X);
        }

        private static void AssertTrackClear(WorldPlan plan)
        {
            foreach (var o in plan.Objects)
            {
                float d = Geometry.DistanceToPolyline(new Vec2(o.X, o.Z), plan.TrackPath);
                Assert.GreaterOrEqual(d, WorldBuilder.TrackHalfWidth, $"{o.Kind} bei ({o.X:0.0},{o.Z:0.0}) steht auf der Strecke");
            }
        }
    }
}

namespace WorldRush.Tests
{
    using System.Collections.Generic;
    using System.IO;
    using NUnit.Framework;
    using WorldRush.Core;
    using WorldRush.Core.World;

    public class WorldPersistenceTests
    {
        private string _dir;

        [SetUp]
        public void SetUp() => _dir = Path.Combine(Path.GetTempPath(), "fs-world-" + System.Guid.NewGuid().ToString("N"));

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        private static WorldDefinition Sample()
        {
            var doc = new DrawingDocument();
            doc.AddStroke(BrushKind.Road, new List<Vec2> { new Vec2(0, 0), new Vec2(5, 150), new Vec2(0, 300) });
            doc.AddStroke(BrushKind.House, new List<Vec2> { new Vec2(20, 40), new Vec2(30, 52) });
            doc.AddStroke(BrushKind.Garden, new List<Vec2> { new Vec2(-40, 20), new Vec2(-20, 20), new Vec2(-20, 40), new Vec2(-40, 40) });
            return WorldDefinition.FromDocument(doc, "Neon \"Stadt\"\n", 77);
        }

        [Test]
        public void RoundTripRebuildsIdenticalWorld()
        {
            var original = Sample();
            Assert.IsTrue(WorldDefinition.TryFromJson(original.ToJson(), out var loaded, out var err), err);
            Assert.AreEqual(original.Name, loaded.Name);
            Assert.AreEqual(original.Seed, loaded.Seed);
            Assert.AreEqual(original.Strokes.Count, loaded.Strokes.Count);

            var a = original.BuildPlan();
            var b = loaded.BuildPlan();
            Assert.AreEqual(a.Objects.Count, b.Objects.Count);
            for (int i = 0; i < a.Objects.Count; i++)
            {
                Assert.AreEqual(a.Objects[i].X, b.Objects[i].X, 0.05f);
                Assert.AreEqual(a.Objects[i].Z, b.Objects[i].Z, 0.05f);
            }
        }

        [Test]
        public void NameIsSanitized()
        {
            Assert.AreEqual("Neon \"Stadt\"", Sample().Name);
            Assert.AreEqual(WorldDefinition.MaxNameLength, WorldDefinition.SanitizeName(new string('a', 500)).Length);
            Assert.AreEqual("Meine Welt", WorldDefinition.SanitizeName("  "));
        }

        [TestCase("")]
        [TestCase("{")]
        [TestCase("[1,2]")]
        [TestCase("{\"schemaVersion\":99}")]
        [TestCase("{\"schemaVersion\":1}garbage")]
        public void InvalidFilesAreRejected(string json)
        {
            Assert.IsFalse(WorldDefinition.TryFromJson(json, out _, out var err));
            Assert.IsNotEmpty(err);
        }

        [Test]
        public void ManipulatedContentIsCleaned()
        {
            string json = "{\"schemaVersion\":1,\"worldId\":\"../../etc/passwd\",\"name\":\"x\",\"seed\":1e30," +
                          "\"strokes\":[{\"brush\":\"Rocket\",\"points\":[0,0,1,1]},{\"brush\":\"Tree\",\"points\":[1e9,5,\"x\",3]},{\"brush\":\"Tree\",\"points\":[1,2,3,4]}]}";
            Assert.IsTrue(WorldDefinition.TryFromJson(json, out var w, out _));
            Assert.IsFalse(w.WorldId.Contains("/"));
            Assert.AreEqual(0, w.Seed);
            Assert.AreEqual(2, w.Strokes.Count); // "Rocket" verworfen
            foreach (var s in w.Strokes)
                foreach (var p in s.Points) Assert.LessOrEqual(System.Math.Abs(p.X), DrawingDocument.PlanHalfWidth);
        }

        [Test]
        public void DeepNestingIsRejected()
        {
            string deep = new string('[', 1000) + new string(']', 1000);
            Assert.IsFalse(MiniJson.TryParse(deep, out _));
        }

        [Test]
        public void RepositorySaveListLoadDelete()
        {
            var repo = new WorldRepository(_dir);
            var w = Sample();
            repo.Save(w);
            repo.Save(w); // zweites Speichern erzeugt .bak
            CollectionAssert.AreEqual(new[] { w.WorldId }, repo.ListIds());

            File.WriteAllText(Path.Combine(_dir, w.WorldId + ".world.json"), "kaputt");
            Assert.IsTrue(repo.TryLoad(w.WorldId, out var loaded, out _)); // aus Backup
            Assert.AreEqual(w.Strokes.Count, loaded.Strokes.Count);

            Assert.IsTrue(repo.Delete(w.WorldId));
            Assert.IsEmpty(repo.ListIds());
            Assert.IsFalse(repo.TryLoad(w.WorldId, out _, out _));
        }

        [Test]
        public void MiniJsonRoundTripsSpecialCharacters()
        {
            var obj = new Dictionary<string, object> { { "s", "ä\"\\\n\t\u0001" }, { "n", 1.5 }, { "b", true }, { "z", null } };
            Assert.IsTrue(MiniJson.TryParse(MiniJson.Serialize(obj), out var back));
            var d = (Dictionary<string, object>)back;
            Assert.AreEqual("ä\"\\\n\t\u0001", d["s"]);
            Assert.AreEqual(1.5, d["n"]);
            Assert.AreEqual(true, d["b"]);
            Assert.IsNull(d["z"]);
        }
    }
}
