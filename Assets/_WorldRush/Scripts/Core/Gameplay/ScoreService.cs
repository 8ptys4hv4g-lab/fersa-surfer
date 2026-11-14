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

        public int Bonus { get; private set; }

        public int Score => (int)Distance + Bonus;

        public void Reset()
        {
            Distance = 0f;
            Coins = 0;
            Bonus = 0;
            IsNewHighScore = false;
        }

        public void AddDistance(float meters)
        {
            if (meters > 0f) Distance += meters;
        }

        /// <summary>Eine Münze einsammeln. Gibt die erhaltenen Punkte zurück.</summary>
        public int AddCoin(int multiplier = 1)
        {
            Coins++;
            int points = _config.CoinValue * Math.Max(1, multiplier);
            Bonus += points;
            return points;
        }

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
