using System.Collections.Generic;
using NUnit.Framework;
using WorldRush.Core;

namespace WorldRush.Tests
{
    public class GameSessionTests
    {
        private const float Frame = 1f / 60f;

        [Test]
        public void StartsReadyAndDoesNotMoveUntilStarted()
        {
            var s = new GameSession(RunnerConfig.CreateDefault(), 1);
            Assert.AreEqual(GameState.Ready, s.State);
            s.Tick(1f);
            Assert.AreEqual(0f, s.PlayerZ);
        }

        [Test]
        public void RunningMovesForwardAndAccelerates()
        {
            var c = RunnerConfig.CreateDefault();
            var s = new GameSession(c, 1);
            s.Start();
            for (int i = 0; i < 60; i++) s.Tick(Frame);
            Assert.That(s.PlayerZ, Is.EqualTo(c.StartSpeed).Within(0.5f));
            Assert.Greater(s.Speed, c.StartSpeed);
            Assert.AreEqual(GameState.Running, s.State);
        }

        [Test]
        public void PauseFreezesAndResumeContinues()
        {
            var s = new GameSession(RunnerConfig.CreateDefault(), 1);
            s.Start();
            s.Tick(0.5f);
            s.Pause();
            float z = s.PlayerZ;
            s.Tick(1f);
            Assert.AreEqual(z, s.PlayerZ);
            Assert.AreEqual(GameState.Paused, s.State);

            s.Resume();
            s.Tick(Frame);
            Assert.Greater(s.PlayerZ, z);
        }

        [Test]
        public void InputIsIgnoredWhenNotRunning()
        {
            var s = new GameSession(RunnerConfig.CreateDefault(), 1);
            s.Enqueue(RunnerCommand.MoveLeft);
            s.Start();
            s.Tick(Frame);
            Assert.AreEqual(Lanes.Middle, s.Motor.TargetLane);
        }

        [Test]
        public void RunningIntoObstacleWithoutInputEndsInGameOver()
        {
            var s = new GameSession(RunnerConfig.CreateDefault(), 5);
            var states = new List<GameState>();
            bool crashed = false;
            s.StateChanged += states.Add;
            s.Crashed += (row, kind) => crashed = true;
            s.Start();
            for (int i = 0; i < 60 * 120 && s.State == GameState.Running; i++) s.Tick(Frame);

            Assert.AreEqual(GameState.GameOver, s.State);
            Assert.IsTrue(crashed);
            CollectionAssert.Contains(states, GameState.GameOver);
        }

        [Test]
        public void HighScoreIsKeptAcrossRestart()
        {
            var s = new GameSession(RunnerConfig.CreateDefault(), 5);
            s.Start();
            for (int i = 0; i < 60 * 120 && s.State == GameState.Running; i++) s.Tick(Frame);
            int best = s.Score.HighScore;
            Assert.Greater(best, 0);

            s.Restart();
            Assert.AreEqual(GameState.Running, s.State);
            Assert.AreEqual(0, s.Score.Score);
            Assert.AreEqual(best, s.Score.HighScore);
            Assert.AreEqual(0f, s.PlayerZ);
        }

        [Test]
        public void LargeFrameDoesNotTunnelThroughObstacle()
        {
            // Ein riesiger Frame (z. B. Ruckler) darf nicht durch eine Wand "teleportieren".
            var c = RunnerConfig.CreateDefault();
            c.MaxFrameDelta = 10f;
            var s = new GameSession(c, 5);
            s.Start();
            s.Tick(10f);
            Assert.AreEqual(GameState.GameOver, s.State);
            Assert.Less(s.PlayerZ, 10f * c.MaxSpeed);
        }

        [Test]
        public void AutopilotSurvivesLongRunWithBoundedMemory()
        {
            // Ein einfacher "Bot" weicht nach Regeln aus. Er beweist: die Strecke ist schaffbar,
            // und alte Hindernisse werden aufgeräumt (keine Speicher-Lecks).
            var c = RunnerConfig.CreateDefault();
            var s = new GameSession(c, 271828);
            s.Start();
            int maxRows = 0, maxCoins = 0;
            for (int i = 0; i < 60 * 300 && s.State == GameState.Running; i++)
            {
                Autopilot.Drive(s);
                s.Tick(Frame);
                maxRows = System.Math.Max(maxRows, s.Rows.Count);
                maxCoins = System.Math.Max(maxCoins, s.Coins.Count);
            }

            Assert.AreEqual(GameState.Running, s.State, $"Bot ist nach {s.PlayerZ:0} m verunglückt.");
            Assert.Greater(s.PlayerZ, 4000f);
            Assert.Greater(s.Score.Coins, 0);
            Assert.Less(maxRows, 40);
            Assert.Less(maxCoins, 200);
            // Ersatz-Reihen sind erlaubt (und selbst sicher), sollen aber selten sein.
            Assert.Less(s.FallbackRowsUsed, 5);
        }
    }

    /// <summary>Test-Bot: entscheidet anhand der nächsten Hindernisreihe.</summary>
    internal static class Autopilot
    {
        public static void Drive(GameSession s)
        {
            var c = s.Config;
            ObstacleRow next = null;
            foreach (var row in s.Rows)
                if (row.Z + c.ObstacleHalfDepth + c.PlayerHalfDepth > s.PlayerZ) { next = row; break; }
            if (next == null) return;

            int lane = s.Motor.TargetLane;
            if (!next.IsPassable(lane))
            {
                int best = lane;
                int bestCost = int.MaxValue;
                for (int l = Lanes.Left; l <= Lanes.Right; l++)
                {
                    if (!next.IsPassable(l)) continue;
                    int cost = System.Math.Abs(l - lane) * 10 + (next.At(l) == ObstacleKind.None ? 0 : 1);
                    if (cost < bestCost) { bestCost = cost; best = l; }
                }
                if (best < lane) s.Enqueue(RunnerCommand.MoveLeft);
                else if (best > lane) s.Enqueue(RunnerCommand.MoveRight);
                return;
            }

            float distance = next.Z - s.PlayerZ;
            float time = distance / s.Speed;
            var kind = next.At(lane);
            if (kind == ObstacleKind.Hurdle && !s.Motor.IsJumping && time < c.JumpDuration * 0.5f)
                s.Enqueue(RunnerCommand.Jump);
            else if (kind == ObstacleKind.Overhead && !s.Motor.IsSliding && time < c.SlideDuration * 0.5f)
                s.Enqueue(RunnerCommand.Slide);
        }
    }
}
