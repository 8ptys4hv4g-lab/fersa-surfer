using NUnit.Framework;
using WorldRush.Core;

namespace WorldRush.Tests
{
    public class LevelTests
    {
        private const float Frame = 1f / 60f;

        [Test]
        public void LevelsGetLongerAndHaveWater()
        {
            var l0 = LevelDefinition.Create(0);
            var l3 = LevelDefinition.Create(3);
            Assert.Greater(l3.Length, l0.Length);
            Assert.IsNotEmpty(l0.Water);
            foreach (var (a, b) in l3.Water) { Assert.Less(a, b); Assert.Less(b, l3.Length); }
            Assert.IsTrue(l0.IsWater(l0.Water[0].start + 1f));
            Assert.IsFalse(l0.IsWater(10f));
        }

        [TestCase(0, 0, 3)]
        [TestCase(0, 10, 1)]
        [TestCase(5, 10, 2)]
        [TestCase(8, 10, 3)]
        public void StarsDependOnCoins(int collected, int total, int stars)
        {
            Assert.AreEqual(stars, LevelDefinition.StarsFor(collected, total));
        }

        [Test]
        public void ReachingFinishWinsAndNoObstaclesNearFinish()
        {
            var c = RunnerConfig.CreateDefault();
            var level = LevelDefinition.Create(0);
            var s = new GameSession(c, 271828, 0, level);
            bool won = false;
            s.StateChanged += st => { if (st == GameState.Won) won = true; };
            s.Start();
            for (int i = 0; i < 60 * 120 && s.State == GameState.Running; i++)
            {
                foreach (var row in s.Rows) Assert.Less(row.Z, level.Length - LevelDefinition.FinishClearance + 0.01f);
                Autopilot.Drive(s);
                s.Tick(Frame);
            }
            Assert.IsTrue(won, $"Ziel nicht erreicht, Zustand {s.State} bei {s.PlayerZ:0} m");
            Assert.GreaterOrEqual(s.PlayerZ, level.Length);
            Assert.Greater(s.LevelCoinsTotal, 0);
            Assert.That(s.Stars, Is.InRange(1, 3));
        }

        [Test]
        public void TrickOnlyInWaterOncePerJump()
        {
            var c = RunnerConfig.CreateDefault();
            var level = new LevelDefinition(0, "Test", 2000f, new[] { (0f, 1000f) });
            var s = new GameSession(c, 1, 0, level);
            int tricks = 0;
            s.TrickPerformed += p => tricks++;
            s.Start();
            s.Enqueue(RunnerCommand.Jump);
            s.Tick(Frame);
            Assert.IsTrue(s.Motor.IsJumping);
            s.Enqueue(RunnerCommand.Jump);
            s.Tick(Frame);
            s.Enqueue(RunnerCommand.Jump);
            s.Tick(Frame);
            Assert.AreEqual(1, tricks, "Nur ein Salto pro Sprung");
            Assert.GreaterOrEqual(s.Score.Bonus, 100);
        }

        [Test]
        public void NoTrickOnLand()
        {
            var s = new GameSession(RunnerConfig.CreateDefault(), 1, 0, new LevelDefinition(0, "Land", 2000f, null));
            int tricks = 0;
            s.TrickPerformed += p => tricks++;
            s.Start();
            s.Enqueue(RunnerCommand.Jump);
            s.Tick(Frame);
            s.Enqueue(RunnerCommand.Jump);
            s.Tick(Frame);
            Assert.AreEqual(0, tricks);
        }

        [Test]
        public void EndlessModeStillWorksWithoutLevel()
        {
            var s = new GameSession(RunnerConfig.CreateDefault(), 5);
            s.Start();
            for (int i = 0; i < 600 && s.State == GameState.Running; i++) s.Tick(Frame);
            Assert.AreNotEqual(GameState.Won, s.State);
        }
    }
}
