using System;

namespace WorldRush.Core
{
    /// <summary>
    /// Bewegung der Spielfigur quer zur Laufrichtung und in der Höhe:
    /// Spurwechsel, Springen, Rutschen. Die Vorwärtsbewegung macht <see cref="GameSession"/>.
    /// </summary>
    public sealed class RunnerMotor
    {
        private readonly RunnerConfig _config;
        private float _jumpTime;
        private float _slideTime;

        public RunnerMotor(RunnerConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            Reset();
        }

        public int TargetLane { get; private set; }
        public float LateralX { get; private set; }
        public float FeetY { get; private set; }
        public bool IsJumping { get; private set; }
        public bool IsSliding { get; private set; }
        /// <summary>Auf dem Surfboard: schwebt über allen Hindernissen.</summary>
        public bool IsHovering { get; private set; }

        public float Height => IsSliding ? _config.SlidingHeight : _config.StandingHeight;
        public float TopY => FeetY + Height;

        public void Reset()
        {
            TargetLane = Lanes.Middle;
            LateralX = 0f;
            FeetY = 0f;
            IsJumping = false;
            IsSliding = false;
            IsHovering = false;
            _jumpTime = 0f;
            _slideTime = 0f;
        }

        public void SetHovering(bool hovering)
        {
            IsHovering = hovering;
            if (hovering)
            {
                IsJumping = false;
                IsSliding = false;
            }
        }

        public void Apply(RunnerCommand command)
        {
            if (IsHovering && (command == RunnerCommand.Jump || command == RunnerCommand.Slide)) return;
            switch (command)
            {
                case RunnerCommand.MoveLeft:
                    if (TargetLane > Lanes.Left) TargetLane--;
                    break;
                case RunnerCommand.MoveRight:
                    if (TargetLane < Lanes.Right) TargetLane++;
                    break;
                case RunnerCommand.Jump:
                    if (!IsJumping && FeetY <= 0f)
                    {
                        IsSliding = false;
                        _slideTime = 0f;
                        IsJumping = true;
                        _jumpTime = 0f;
                    }
                    break;
                case RunnerCommand.Slide:
                    // In der Luft: sofort landen und rutschen (schnelles Abtauchen).
                    IsJumping = false;
                    _jumpTime = 0f;
                    FeetY = 0f;
                    IsSliding = true;
                    _slideTime = 0f;
                    break;
            }
        }

        public void Tick(float dt)
        {
            if (dt <= 0f) return;

            // Spurwechsel mit konstanter Quergeschwindigkeit.
            float targetX = Lanes.ToX(TargetLane, _config.LaneWidth);
            float lateralSpeed = _config.LaneWidth / _config.LaneChangeDuration;
            float maxStep = lateralSpeed * dt;
            float diff = targetX - LateralX;
            LateralX = Math.Abs(diff) <= maxStep ? targetX : LateralX + Math.Sign(diff) * maxStep;

            if (IsHovering)
            {
                FeetY += (_config.BoardHeight - FeetY) * Math.Min(1f, dt * 6f);
            }
            // Sprung als Parabel: y = 4h * t * (1 - t), t = 0..1
            else if (IsJumping)
            {
                _jumpTime += dt;
                float t = _jumpTime / _config.JumpDuration;
                if (t >= 1f)
                {
                    IsJumping = false;
                    FeetY = 0f;
                }
                else
                {
                    FeetY = 4f * _config.JumpHeight * t * (1f - t);
                }
            }
            else if (FeetY > 0f)
            {
                FeetY = Math.Max(0f, FeetY - _config.FallSpeed * dt);
            }

            if (IsSliding)
            {
                _slideTime += dt;
                if (_slideTime >= _config.SlideDuration) IsSliding = false;
            }
        }
    }
}
