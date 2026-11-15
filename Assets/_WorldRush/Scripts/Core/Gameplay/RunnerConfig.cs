namespace WorldRush.Core
{
    /// <summary>
    /// Alle Spielregeln an einem Ort. Reine Daten, keine Unity-Abhängigkeit.
    /// Einheiten: Meter, Sekunden, Meter pro Sekunde.
    /// </summary>
    public sealed class RunnerConfig
    {
        // Spuren
        public float LaneWidth = 2.5f;
        public float LaneChangeDuration = 0.18f;

        // Geschwindigkeit
        public float StartSpeed = 13f;
        public float MaxSpeed = 27f;
        public float Acceleration = 0.22f;

        // Springen / Rutschen
        public float JumpHeight = 1.6f;
        public float JumpDuration = 0.75f;
        public float SlideDuration = 0.7f;

        // Spielfigur (Hitbox)
        public float StandingHeight = 1.8f;
        public float SlidingHeight = 0.8f;
        public float PlayerHalfWidth = 0.4f;
        public float PlayerHalfDepth = 0.4f;

        // Hindernisse
        public float ObstacleHalfWidth = 1.0f;
        public float ObstacleHalfDepth = 0.5f;
        public float HurdleTop = 1.0f;
        public float OverheadBottom = 1.1f;
        public float OverheadTop = 3.0f;
        public float BlockTop = 3.5f;

        // Strecke
        public float SafeStartDistance = 35f;
        public float MinRowGapSeconds = 0.95f;
        public float MaxExtraGapSeconds = 0.9f;
        public float ReactionTime = 0.25f;
        public float GenerateAheadDistance = 140f;
        public float CleanupBehindDistance = 15f;

        // Münzen und Punkte
        public int CoinsPerLine = 5;
        public float CoinSpacing = 2.2f;
        public float CoinHeight = 0.8f;
        public float CoinPickupRadius = 0.8f;
        public int CoinValue = 10;

        // Power-ups und Combo
        public float PowerUpChance = 0.13f;
        public float BoardDuration = 7f;
        public float MagnetDuration = 9f;
        public float ShieldDuration = 20f;
        public float DoubleDuration = 10f;
        public float BoardHeight = 4.6f;
        public float FallSpeed = 9f;
        public float MagnetRange = 3f;
        public float LandingGrace = 1.4f;
        public float ShieldGrace = 1.0f;
        public float SkyCoinHeight = 5.3f;
        public float ComboWindow = 1.4f;
        public int ComboStep = 10;
        public int MaxComboMultiplier = 5;

        // Simulation
        public float MaxSubstep = 0.02f;
        public float MaxFrameDelta = 0.1f;

        public static RunnerConfig CreateDefault() => new RunnerConfig();

        public float MinRowGapMeters(float speed) => speed * MinRowGapSeconds;
    }
}
