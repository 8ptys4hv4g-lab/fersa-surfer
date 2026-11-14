using NUnit.Framework;
using WorldRush.Core;

namespace WorldRush.Tests
{
    public class CollisionRulesTests
    {
        private RunnerConfig _c;

        [SetUp]
        public void SetUp() => _c = RunnerConfig.CreateDefault();

        [Test]
        public void StandingPlayerHitsHurdle()
        {
            Assert.IsTrue(CollisionRules.HitsObstacle(_c, 0f, 50f, 0f, _c.StandingHeight, Lanes.Middle, 50f, ObstacleKind.Hurdle));
        }

        [Test]
        public void JumpingPlayerClearsHurdle()
        {
            float feet = _c.HurdleTop + 0.1f;
            Assert.IsFalse(CollisionRules.HitsObstacle(_c, 0f, 50f, feet, feet + _c.StandingHeight, Lanes.Middle, 50f, ObstacleKind.Hurdle));
        }

        [Test]
        public void SlidingPlayerPassesUnderOverhead()
        {
            Assert.IsFalse(CollisionRules.HitsObstacle(_c, 0f, 50f, 0f, _c.SlidingHeight, Lanes.Middle, 50f, ObstacleKind.Overhead));
            Assert.IsTrue(CollisionRules.HitsObstacle(_c, 0f, 50f, 0f, _c.StandingHeight, Lanes.Middle, 50f, ObstacleKind.Overhead));
        }

        [Test]
        public void JumpingDoesNotClearOverhead()
        {
            float feet = _c.JumpHeight;
            Assert.IsTrue(CollisionRules.HitsObstacle(_c, 0f, 50f, feet, feet + _c.StandingHeight, Lanes.Middle, 50f, ObstacleKind.Overhead));
        }

        [Test]
        public void BlockCannotBeJumpedOrSlid()
        {
            float jumpFeet = _c.JumpHeight;
            Assert.IsTrue(CollisionRules.HitsObstacle(_c, 0f, 50f, jumpFeet, jumpFeet + _c.StandingHeight, Lanes.Middle, 50f, ObstacleKind.Block));
            Assert.IsTrue(CollisionRules.HitsObstacle(_c, 0f, 50f, 0f, _c.SlidingHeight, Lanes.Middle, 50f, ObstacleKind.Block));
        }

        [Test]
        public void ObstacleInOtherLaneIsIgnored()
        {
            float leftX = Lanes.ToX(Lanes.Left, _c.LaneWidth);
            Assert.IsFalse(CollisionRules.HitsObstacle(_c, leftX, 50f, 0f, _c.StandingHeight, Lanes.Middle, 50f, ObstacleKind.Block));
        }

        [Test]
        public void CoinIsCollectedOnlyInSameLane()
        {
            var coin = new Coin(1, Lanes.Right, 20f, _c.CoinHeight);
            Assert.IsFalse(CollisionRules.CollectsCoin(_c, 0f, 20f, 0f, _c.StandingHeight, coin));
            Assert.IsTrue(CollisionRules.CollectsCoin(_c, _c.LaneWidth, 20f, 0f, _c.StandingHeight, coin));
        }
    }

    public class ScoreServiceTests
    {
        [Test]
        public void ScoreCombinesDistanceAndCoins()
        {
            var c = RunnerConfig.CreateDefault();
            var score = new ScoreService(c);
            score.AddDistance(123.7f);
            score.AddCoin();
            score.AddCoin();
            Assert.AreEqual(123 + 2 * c.CoinValue, score.Score);
        }

        [Test]
        public void HighScoreOnlyIncreases()
        {
            var score = new ScoreService(RunnerConfig.CreateDefault(), highScore: 500);
            score.AddDistance(100);
            Assert.IsFalse(score.CommitHighScore());
            Assert.AreEqual(500, score.HighScore);

            score.AddDistance(1000);
            Assert.IsTrue(score.CommitHighScore());
            Assert.AreEqual(1100, score.HighScore);
        }

        [Test]
        public void NegativeDistanceIsIgnored()
        {
            var score = new ScoreService(RunnerConfig.CreateDefault());
            score.AddDistance(-50);
            Assert.AreEqual(0, score.Score);
        }
    }
}
