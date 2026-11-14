using System.Collections.Generic;
using NUnit.Framework;
using WorldRush.Core;

namespace WorldRush.Tests
{
    public class TrackSafetyValidatorTests
    {
        private RunnerConfig _config;
        private TrackSafetyValidator _validator;

        [SetUp]
        public void SetUp()
        {
            _config = RunnerConfig.CreateDefault();
            _validator = new TrackSafetyValidator(_config);
        }

        private static ObstacleRow Row(float z, ObstacleKind l, ObstacleKind m, ObstacleKind r) => new ObstacleRow(0, z, l, m, r);

        [Test]
        public void RowWithThreeWallsIsRejected()
        {
            var row = Row(50, ObstacleKind.Block, ObstacleKind.Block, ObstacleKind.Block);
            Assert.IsFalse(_validator.IsRowSafe(row));
            Assert.IsFalse(_validator.IsTransitionSafe(null, row, 10f, out var reason));
            Assert.IsNotEmpty(reason);
        }

        [Test]
        public void RowWithJumpableObstacleInEveryLaneIsAllowed()
        {
            var row = Row(50, ObstacleKind.Hurdle, ObstacleKind.Overhead, ObstacleKind.Hurdle);
            Assert.IsTrue(_validator.IsRowSafe(row));
        }

        [Test]
        public void RowsTooCloseTogetherAreRejected()
        {
            var a = Row(50, ObstacleKind.Hurdle, ObstacleKind.None, ObstacleKind.None);
            var b = Row(52, ObstacleKind.None, ObstacleKind.Overhead, ObstacleKind.None);
            Assert.IsFalse(_validator.IsTransitionSafe(a, b, 20f, out _));
        }

        [Test]
        public void DeadEndIsRejectedWhenLaneChangeTooSlow()
        {
            var slow = RunnerConfig.CreateDefault();
            slow.LaneChangeDuration = 0.6f;
            var validator = new TrackSafetyValidator(slow);
            // Spur links frei, danach ist nur rechts frei: 2 Spurwechsel nötig.
            var a = Row(50, ObstacleKind.None, ObstacleKind.Block, ObstacleKind.Block);
            var b = Row(50 + slow.MinRowGapMeters(10f), ObstacleKind.Block, ObstacleKind.Block, ObstacleKind.None);
            Assert.IsFalse(validator.IsTransitionSafe(a, b, 10f, out var reason));
            StringAssert.Contains("Sackgasse", reason);
        }

        [Test]
        public void FirstObstacleTooCloseToStartIsRejected()
        {
            var rows = new List<ObstacleRow> { Row(5, ObstacleKind.Hurdle, ObstacleKind.None, ObstacleKind.None) };
            Assert.IsFalse(_validator.IsTrackSafe(rows, 10f, out _));
        }
    }

    public class TrackGeneratorTests
    {
        [Test]
        public void SameSeedProducesSameTrack()
        {
            var config = RunnerConfig.CreateDefault();
            var a = Generate(config, 1234, 2000f);
            var b = Generate(config, 1234, 2000f);
            Assert.AreEqual(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++)
            {
                Assert.AreEqual(a[i].Z, b[i].Z);
                CollectionAssert.AreEqual(a[i].Slots, b[i].Slots);
            }
        }

        [Test]
        public void DifferentSeedsProduceDifferentTracks()
        {
            var config = RunnerConfig.CreateDefault();
            var a = Generate(config, 1, 1000f);
            var b = Generate(config, 2, 1000f);
            bool different = a.Count != b.Count;
            for (int i = 0; !different && i < a.Count; i++)
                different = a[i].Z != b[i].Z || !Equal(a[i].Slots, b[i].Slots);
            Assert.IsTrue(different);
        }

        [TestCase(1)]
        [TestCase(42)]
        [TestCase(271828)]
        [TestCase(-99)]
        [TestCase(int.MaxValue)]
        public void GeneratedTrackIsAlwaysSafe(int seed)
        {
            var config = RunnerConfig.CreateDefault();
            var rows = Generate(config, seed, 5000f);
            var validator = new TrackSafetyValidator(config);
            Assert.Greater(rows.Count, 50);
            Assert.IsTrue(validator.IsTrackSafe(rows, config.StartSpeed, out var reason), reason);
        }

        [Test]
        public void ObstaclesStartAfterSafeZone()
        {
            var config = RunnerConfig.CreateDefault();
            var rows = Generate(config, 7, 500f);
            Assert.GreaterOrEqual(rows[0].Z, config.SafeStartDistance);
        }

        [Test]
        public void CoinsNeverOverlapObstacles()
        {
            var config = RunnerConfig.CreateDefault();
            var rows = new List<ObstacleRow>();
            var coins = new List<Coin>();
            new TrackGenerator(config, 99).GenerateUntil(3000f, config.StartSpeed, rows, coins);
            Assert.Greater(coins.Count, 0);
            foreach (var coin in coins)
                foreach (var row in rows)
                    if (row.At(coin.Lane) != ObstacleKind.None)
                        Assert.Greater(System.Math.Abs(coin.Z - row.Z), config.ObstacleHalfDepth + config.CoinPickupRadius,
                            $"Münze bei z={coin.Z} liegt im Hindernis {row}");
        }

        private static List<ObstacleRow> Generate(RunnerConfig config, int seed, float until)
        {
            var rows = new List<ObstacleRow>();
            new TrackGenerator(config, seed).GenerateUntil(until, config.StartSpeed, rows, new List<Coin>());
            return rows;
        }

        private static bool Equal(ObstacleKind[] a, ObstacleKind[] b)
        {
            for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
            return true;
        }
    }
}
