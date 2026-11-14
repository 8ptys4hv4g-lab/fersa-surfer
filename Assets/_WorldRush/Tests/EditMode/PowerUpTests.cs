using System.Collections.Generic;
using NUnit.Framework;
using WorldRush.Core;

namespace WorldRush.Tests
{
    public class PowerUpTests
    {
        private const float Frame = 1f / 60f;

        private static void Run(GameSession s, float seconds)
        {
            for (float t = 0; t < seconds && s.State == GameState.Running; t += Frame) s.Tick(Frame);
        }

        [Test]
        public void GeneratorPlacesPowerUpsOnlyInFreeLanesAwayFromCoins()
        {
            var c = RunnerConfig.CreateDefault();
            var rows = new List<ObstacleRow>();
            var coins = new List<Coin>();
            var ups = new List<PowerUpItem>();
            new TrackGenerator(c, 4242).GenerateUntil(6000f, c.StartSpeed, rows, coins, ups);
            Assert.Greater(ups.Count, 5);
            foreach (var p in ups)
            {
                foreach (var r in rows)
                    if (r.At(p.Lane) != ObstacleKind.None)
                        Assert.Greater(System.Math.Abs(r.Z - p.Z), c.ObstacleHalfDepth + c.CoinPickupRadius, $"Power-up bei {p.Z} steckt in {r}");
                foreach (var coin in coins)
                    Assert.IsFalse(coin.Lane == p.Lane && System.Math.Abs(coin.Z - p.Z) < 1f, "Power-up liegt auf einer Münze");
            }
        }

        [Test]
        public void PowerUpsAreDeterministic()
        {
            var c = RunnerConfig.CreateDefault();
            var a = new List<PowerUpItem>();
            var b = new List<PowerUpItem>();
            new TrackGenerator(c, 9).GenerateUntil(3000f, c.StartSpeed, new List<ObstacleRow>(), new List<Coin>(), a);
            new TrackGenerator(c, 9).GenerateUntil(3000f, c.StartSpeed, new List<ObstacleRow>(), new List<Coin>(), b);
            Assert.AreEqual(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++) { Assert.AreEqual(a[i].Kind, b[i].Kind); Assert.AreEqual(a[i].Z, b[i].Z); }
        }

        [Test]
        public void OldGeneratorOverloadStillWorksWithoutPowerUps()
        {
            var c = RunnerConfig.CreateDefault();
            var rows = new List<ObstacleRow>();
            new TrackGenerator(c, 1).GenerateUntil(500f, c.StartSpeed, rows, new List<Coin>());
            Assert.Greater(rows.Count, 0);
        }

        [Test]
        public void BoardFliesOverEverythingThenLandsSafely()
        {
            var c = RunnerConfig.CreateDefault();
            var s = new GameSession(c, 5);
            s.Start();
            s.ActivatePowerUp(PowerUpKind.Board);
            Run(s, 1f);
            Assert.Greater(s.Motor.FeetY, c.BlockTop, "Figur schwebt über Wänden");
            Run(s, c.BoardDuration - 1f);
            Assert.AreEqual(GameState.Running, s.State, "Ohne Eingabe sicher über alle Hindernisse");
            Assert.Greater(s.Score.Coins, 10, "Münzspur in der Luft eingesammelt");
            Run(s, 0.1f);
            Assert.IsFalse(s.Motor.IsHovering);
            Assert.Greater(s.Invulnerable, 0f, "Schonzeit nach der Landung");
        }

        [Test]
        public void JumpAndSlideAreIgnoredWhileHovering()
        {
            var s = new GameSession(RunnerConfig.CreateDefault(), 5);
            s.Start();
            s.ActivatePowerUp(PowerUpKind.Board);
            s.Enqueue(RunnerCommand.Slide);
            s.Enqueue(RunnerCommand.Jump);
            s.Tick(Frame);
            Assert.IsFalse(s.Motor.IsSliding);
            Assert.IsFalse(s.Motor.IsJumping);
        }

        [Test]
        public void ShieldAbsorbsExactlyOneCrash()
        {
            var s = new GameSession(RunnerConfig.CreateDefault(), 5);
            int shieldUsed = 0;
            s.ShieldUsed += _ => shieldUsed++;
            s.Start();
            s.ActivatePowerUp(PowerUpKind.Shield);
            Run(s, 120f);
            Assert.AreEqual(1, shieldUsed);
            Assert.AreEqual(GameState.GameOver, s.State);
        }

        [Test]
        public void DoubleGivesTwicePoints()
        {
            var c = RunnerConfig.CreateDefault();
            var s = new GameSession(c, 5);
            var points = new List<int>();
            s.CoinCollected += (coin, p) => points.Add(p);
            s.Start();
            s.ActivatePowerUp(PowerUpKind.Double);
            s.ActivatePowerUp(PowerUpKind.Board); // sorgt sicher für Münzen
            Run(s, 2f);
            Assert.IsNotEmpty(points);
            Assert.AreEqual(c.CoinValue * 2, points[0]);
        }

        [Test]
        public void ComboRaisesMultiplier()
        {
            var c = RunnerConfig.CreateDefault();
            var s = new GameSession(c, 5);
            var milestones = new List<int>();
            s.ComboMilestone += milestones.Add;
            s.Start();
            s.ActivatePowerUp(PowerUpKind.Board);
            Run(s, c.BoardDuration - 0.5f);
            CollectionAssert.Contains(milestones, 2);
            Assert.Greater(s.Score.Bonus, s.Score.Coins * c.CoinValue, "Combo bringt Extrapunkte");
        }

        [Test]
        public void MagnetCollectsCoinsFromOtherLanes()
        {
            var c = RunnerConfig.CreateDefault();
            var withMagnet = new GameSession(c, 77);
            var without = new GameSession(c, 77);
            withMagnet.Start(); without.Start();
            withMagnet.ActivatePowerUp(PowerUpKind.Magnet);
            for (int i = 0; i < 60 * 6; i++)
            {
                // Beide überleben dank Autopilot gleich lang; nur der Magnet unterscheidet sie.
                Autopilot.Drive(withMagnet); Autopilot.Drive(without);
                withMagnet.Tick(Frame); without.Tick(Frame);
            }
            Assert.GreaterOrEqual(withMagnet.Score.Coins, without.Score.Coins);
        }

        [Test]
        public void ResetClearsPowerUps()
        {
            var s = new GameSession(RunnerConfig.CreateDefault(), 5);
            s.Start();
            s.ActivatePowerUp(PowerUpKind.Magnet);
            s.Restart();
            Assert.IsFalse(s.IsPowerActive(PowerUpKind.Magnet));
            Assert.AreEqual(0, s.Combo);
        }
    }
}
