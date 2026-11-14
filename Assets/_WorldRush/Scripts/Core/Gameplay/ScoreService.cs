using System;

namespace WorldRush.Core
{
    /// <summary>Punkte = gelaufene Meter + Münzen × Münzwert.</summary>
    public sealed class ScoreService
    {
        private readonly RunnerConfig _config;

        public ScoreService(RunnerConfig config, int highScore = 0)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            HighScore = Math.Max(0, highScore);
        }

        public float Distance { get; private set; }
        public int Coins { get; private set; }
        public int HighScore { get; private set; }
        public bool IsNewHighScore { get; private set; }

        public int Score => (int)Distance + Coins * _config.CoinValue;

        public void Reset()
        {
            Distance = 0f;
            Coins = 0;
            IsNewHighScore = false;
        }

        public void AddDistance(float meters)
        {
            if (meters > 0f) Distance += meters;
        }

        public void AddCoin() => Coins++;

        /// <summary>Am Rundenende aufrufen. Gibt true zurück, wenn ein neuer Bestwert erreicht wurde.</summary>
        public bool CommitHighScore()
        {
            if (Score > HighScore)
            {
                HighScore = Score;
                IsNewHighScore = true;
            }
            return IsNewHighScore;
        }
    }
}
