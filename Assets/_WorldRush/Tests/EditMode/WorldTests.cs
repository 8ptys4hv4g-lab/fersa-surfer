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
