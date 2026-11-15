using System.Collections.Generic;
using NUnit.Framework;
using WorldRush.Core;
using WorldRush.Core.World;

namespace WorldRush.Tests
{
    public class WorldLevelTests
    {
        private static Stroke Water(int id, params float[] xy)
        {
            var pts = new List<Vec2>();
            for (int i = 0; i + 1 < xy.Length; i += 2) pts.Add(new Vec2(xy[i], xy[i + 1]));
            return new Stroke(id, BrushKind.Water, pts);
        }

        [Test]
        public void No_water_means_no_surfing()
        {
            Assert.IsEmpty(WorldLevel.SurfSections(new List<Stroke>()));
            Assert.IsEmpty(WorldLevel.SurfSections(new List<Stroke> { new Stroke(1, BrushKind.Tree, new List<Vec2> { new Vec2(0, 200) }) }));
        }

        [Test]
        public void Line_across_the_road_becomes_a_surf_section_of_minimum_length()
        {
            var s = WorldLevel.SurfSections(new List<Stroke> { Water(1, -20, 200, 20, 204) });
            Assert.AreEqual(1, s.Count);
            Assert.GreaterOrEqual(s[0].end - s[0].start, WorldLevel.MinSurf - 0.01f);
            Assert.That(s[0].start, Is.LessThan(202f).And.GreaterThan(160f));
        }

        [Test]
        public void A_single_tap_on_the_road_is_enough()
        {
            var s = WorldLevel.SurfSections(new List<Stroke> { Water(1, 1, 150) });
            Assert.AreEqual(1, s.Count);
            Assert.AreEqual(130f, s[0].start, 0.01f);
            Assert.AreEqual(170f, s[0].end, 0.01f);
        }

        [Test]
        public void Start_stays_dry_and_finish_stays_free()
        {
            var s = WorldLevel.SurfSections(new List<Stroke> { Water(1, 0, 5), Water(2, 0, 398) });
            Assert.AreEqual(2, s.Count);
            Assert.GreaterOrEqual(s[0].start, WorldLevel.DryStart);
            Assert.LessOrEqual(s[1].end, WorldLevel.LastWaterEnd);
            Assert.LessOrEqual(s[1].end, WorldLevel.Length - LevelDefinition.FinishClearance);
        }

        [Test]
        public void Close_sections_are_merged()
        {
            var s = WorldLevel.SurfSections(new List<Stroke> { Water(1, 0, 150), Water(2, 0, 200) });
            Assert.AreEqual(1, s.Count);
            Assert.AreEqual(130f, s[0].start, 0.01f);
            Assert.AreEqual(220f, s[0].end, 0.01f);
        }

        [Test]
        public void Water_beside_the_road_becomes_a_lake_not_a_surf_section()
        {
            var lake = Water(1, 30, 100, 45, 100, 45, 130, 30, 130);
            Assert.IsFalse(WorldLevel.TouchesRoad(lake));
            Assert.IsEmpty(WorldLevel.SurfSections(new List<Stroke> { lake }));
            var plan = new WorldBuilder(1).Build(new List<Stroke> { lake });
            Assert.AreEqual(1, plan.WaterAreas.Count);
        }

        [Test]
        public void Road_water_makes_no_lake_and_a_tapped_pond_is_round()
        {
            var plan = new WorldBuilder(1).Build(new List<Stroke> { Water(1, -20, 200, 20, 200), Water(2, 40, 300) });
            Assert.AreEqual(1, plan.WaterAreas.Count, "nur der Teich neben der Straße");
            Assert.AreEqual(12, plan.WaterAreas[0].Count);
        }

        [Test]
        public void Custom_level_is_playable_with_finish_and_water()
        {
            var level = WorldLevel.Create(new List<Stroke> { Water(1, 0, 200) }, "Meine Welt");
            Assert.AreEqual(WorldLevel.CustomIndex, level.Index);
            Assert.AreEqual(WorldLevel.Length, level.Length);
            Assert.IsTrue(level.IsWater(200f));
            Assert.IsFalse(level.IsWater(20f));
        }

        [Test]
        public void Custom_level_runs_to_the_finish_in_a_session()
        {
            var level = WorldLevel.Create(new List<Stroke> { Water(1, 0, 120), Water(2, 0, 260) }, "Test");
            var session = new GameSession(RunnerConfig.CreateDefault(), 7, 0, level);
            session.Start();
            for (int i = 0; i < 60 * 120 && session.State == GameState.Running; i++)
            {
                session.ActivatePowerUp(PowerUpKind.Shield); // Schild: Test prüft nur, dass Wasser + Ziel funktionieren
                session.Tick(1f / 60f);
            }
            Assert.AreEqual(GameState.Won, session.State, "Level mit gemaltem Wasser lässt sich bis ins Ziel spielen");
        }
    }
}
