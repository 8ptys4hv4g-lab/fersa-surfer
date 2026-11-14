using NUnit.Framework;
using WorldRush.Core;

namespace WorldRush.Tests
{
    public class RunnerMotorTests
    {
        private RunnerConfig _config;
        private RunnerMotor _motor;

        [SetUp]
        public void SetUp()
        {
            _config = RunnerConfig.CreateDefault();
            _motor = new RunnerMotor(_config);
        }

        private void Simulate(float seconds)
        {
            for (float t = 0; t < seconds; t += 0.01f) _motor.Tick(0.01f);
        }

        [Test]
        public void StartsInMiddleLaneOnGround()
        {
            Assert.AreEqual(Lanes.Middle, _motor.TargetLane);
            Assert.AreEqual(0f, _motor.LateralX);
            Assert.AreEqual(0f, _motor.FeetY);
        }

        [Test]
        public void LaneChangeIsClampedToThreeLanes()
        {
            _motor.Apply(RunnerCommand.MoveLeft);
            _motor.Apply(RunnerCommand.MoveLeft);
            _motor.Apply(RunnerCommand.MoveLeft);
            Assert.AreEqual(Lanes.Left, _motor.TargetLane);

            _motor.Apply(RunnerCommand.MoveRight);
            _motor.Apply(RunnerCommand.MoveRight);
            _motor.Apply(RunnerCommand.MoveRight);
            Assert.AreEqual(Lanes.Right, _motor.TargetLane);
        }

        [Test]
        public void LaneChangeReachesTargetWithinConfiguredDuration()
        {
            _motor.Apply(RunnerCommand.MoveRight);
            Simulate(_config.LaneChangeDuration + 0.02f);
            Assert.AreEqual(_config.LaneWidth, _motor.LateralX, 1e-4f);
        }

        [Test]
        public void JumpRisesToConfiguredHeightAndLands()
        {
            _motor.Apply(RunnerCommand.Jump);
            Simulate(_config.JumpDuration / 2f);
            Assert.That(_motor.FeetY, Is.EqualTo(_config.JumpHeight).Within(0.05f));
            Assert.IsTrue(_motor.IsJumping);

            Simulate(_config.JumpDuration / 2f + 0.05f);
            Assert.IsFalse(_motor.IsJumping);
            Assert.AreEqual(0f, _motor.FeetY);
        }

        [Test]
        public void SlideReducesHeightTemporarily()
        {
            _motor.Apply(RunnerCommand.Slide);
            Assert.IsTrue(_motor.IsSliding);
            Assert.AreEqual(_config.SlidingHeight, _motor.Height);

            Simulate(_config.SlideDuration + 0.05f);
            Assert.IsFalse(_motor.IsSliding);
            Assert.AreEqual(_config.StandingHeight, _motor.Height);
        }

        [Test]
        public void SlideInAirDropsToGroundImmediately()
        {
            _motor.Apply(RunnerCommand.Jump);
            Simulate(0.2f);
            Assert.Greater(_motor.FeetY, 0f);

            _motor.Apply(RunnerCommand.Slide);
            Assert.IsFalse(_motor.IsJumping);
            Assert.IsTrue(_motor.IsSliding);
            Assert.AreEqual(0f, _motor.FeetY);
        }

        [Test]
        public void JumpWhileJumpingIsIgnored()
        {
            _motor.Apply(RunnerCommand.Jump);
            Simulate(0.3f);
            float y = _motor.FeetY;
            _motor.Apply(RunnerCommand.Jump);
            _motor.Tick(0.001f);
            Assert.That(_motor.FeetY, Is.EqualTo(y).Within(0.05f));
        }
    }
}
