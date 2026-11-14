using System;

namespace WorldRush.Core
{
    /// <summary>
    /// Erkennt Wischgesten aus Start- und Endpunkt (Bildschirmpixel, y zeigt nach oben).
    /// Kennt keine Unity-Touch-Klassen und ist daher ohne Gerät testbar.
    /// </summary>
    public sealed class SwipeDetector
    {
        private bool _tracking;
        private float _startX, _startY, _startTime;

        /// <param name="minDistancePixels">Mindestlänge einer Geste.</param>
        /// <param name="maxDuration">Längere Berührungen zählen nicht als Wischen.</param>
        public SwipeDetector(float minDistancePixels = 60f, float maxDuration = 0.6f)
        {
            MinDistance = minDistancePixels;
            MaxDuration = maxDuration;
        }

        public float MinDistance { get; set; }
        public float MaxDuration { get; set; }
        public bool IsTracking => _tracking;

        /// <summary>Mindestlänge passend zur Bildschirmgröße (ca. 6 % der kürzeren Seite).</summary>
        public static float DistanceForScreen(int width, int height) => Math.Max(30f, Math.Min(width, height) * 0.06f);

        public void Begin(float x, float y, float time)
        {
            _tracking = true;
            _startX = x;
            _startY = y;
            _startTime = time;
        }

        /// <summary>
        /// Während der Berührung aufrufen: liefert die Geste sobald sie eindeutig ist
        /// (fühlt sich schneller an als erst beim Loslassen).
        /// </summary>
        public RunnerCommand Update(float x, float y, float time)
        {
            if (!_tracking) return RunnerCommand.None;
            var cmd = Evaluate(x, y, time);
            if (cmd != RunnerCommand.None) _tracking = false;
            return cmd;
        }

        public RunnerCommand End(float x, float y, float time)
        {
            if (!_tracking) return RunnerCommand.None;
            _tracking = false;
            return Evaluate(x, y, time);
        }

        public void Cancel() => _tracking = false;

        private RunnerCommand Evaluate(float x, float y, float time)
        {
            if (time - _startTime > MaxDuration) return RunnerCommand.None;
            float dx = x - _startX;
            float dy = y - _startY;
            float ax = Math.Abs(dx);
            float ay = Math.Abs(dy);
            if (Math.Max(ax, ay) < MinDistance) return RunnerCommand.None;
            if (ax > ay) return dx < 0 ? RunnerCommand.MoveLeft : RunnerCommand.MoveRight;
            return dy > 0 ? RunnerCommand.Jump : RunnerCommand.Slide;
        }
    }
}
