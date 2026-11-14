using System;
using System.Collections.Generic;

namespace WorldRush.Core
{
    /// <summary>
    /// Erzeugt die endlose Strecke Reihe für Reihe – reproduzierbar per Seed.
    /// Jede neue Reihe wird vom <see cref="TrackSafetyValidator"/> geprüft;
    /// scheitert ein Zufallsversuch mehrfach, wird eine garantiert sichere Reihe eingesetzt.
    /// </summary>
    public sealed class TrackGenerator
    {
        public const int MaxAttemptsPerRow = 12;

        private readonly RunnerConfig _config;
        private readonly TrackSafetyValidator _validator;
        private readonly DeterministicRandom _random;
        private ObstacleRow _lastRow;
        private float _nextZ;
        private int _nextRowId;
        private int _nextCoinId;
        private int _nextPowerUpId;
        private int _rowsCreated;

        public TrackGenerator(RunnerConfig config, int seed)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _validator = new TrackSafetyValidator(config);
            _random = new DeterministicRandom(seed);
            Seed = seed;
            _nextZ = config.SafeStartDistance;
        }

        public int Seed { get; }
        public int FallbackRowsUsed { get; private set; }
        public float GeneratedUntilZ => _lastRow?.Z ?? 0f;

        /// <summary>
        /// Erzeugt Reihen und Münzen, bis <paramref name="untilZ"/> erreicht ist.
        /// </summary>
        public void GenerateUntil(float untilZ, float speed, List<ObstacleRow> rowsOut, List<Coin> coinsOut)
            => GenerateUntil(untilZ, speed, rowsOut, coinsOut, null);

        public void GenerateUntil(float untilZ, float speed, List<ObstacleRow> rowsOut, List<Coin> coinsOut, List<PowerUpItem> powerUpsOut)
        {
            float planningSpeed = Math.Min(_config.MaxSpeed, Math.Max(speed, _config.StartSpeed) * 1.1f);
            while (_nextZ <= untilZ)
            {
                ObstacleRow row = CreateRow(_nextZ, planningSpeed);
                int coinLane = AddCoins(_lastRow, row, coinsOut);
                if (powerUpsOut != null) AddPowerUp(row, coinLane, powerUpsOut);
                rowsOut.Add(row);
                _rowsCreated++;
                _lastRow = row;

                float gapSeconds = _config.MinRowGapSeconds + _random.Range(0f, _config.MaxExtraGapSeconds);
                _nextZ = row.Z + gapSeconds * planningSpeed;
            }
        }

        private ObstacleRow CreateRow(float z, float speed)
        {
            float difficulty = Math.Min(1f, z / 2000f); // 0..1 auf den ersten 2 km
            for (int attempt = 0; attempt < MaxAttemptsPerRow; attempt++)
            {
                var row = RandomRow(z, difficulty);
                if (_validator.IsTransitionSafe(_lastRow, row, speed, out _))
                {
                    _nextRowId++;
                    return row;
                }
            }

            FallbackRowsUsed++;
            var safe = new ObstacleRow(_nextRowId++, z, ObstacleKind.None, ObstacleKind.Hurdle, ObstacleKind.None);
            return safe;
        }

        private ObstacleRow RandomRow(float z, float difficulty)
        {
            var slots = new ObstacleKind[Lanes.Count];
            int filled = _random.NextFloat() < 0.35f + 0.45f * difficulty ? 2 : 1;
            for (int i = 0; i < filled; i++)
            {
                int index = _random.Range(0, Lanes.Count);
                slots[index] = RandomKind(difficulty);
            }
            return new ObstacleRow(_nextRowId, z, slots[0], slots[1], slots[2]);
        }

        private ObstacleKind RandomKind(float difficulty)
        {
            float r = _random.NextFloat();
            float blockChance = 0.30f + 0.20f * difficulty;
            if (r < blockChance) return ObstacleKind.Block;
            if (r < blockChance + (1f - blockChance) * 0.5f) return ObstacleKind.Hurdle;
            return ObstacleKind.Overhead;
        }

        private void AddPowerUp(ObstacleRow row, int coinLane, List<PowerUpItem> powerUpsOut)
        {
            if (_rowsCreated < 2 || _random.NextFloat() >= _config.PowerUpChance) return;
            var lanes = new List<int>(Lanes.Count);
            for (int lane = Lanes.Left; lane <= Lanes.Right; lane++)
                if (row.At(lane) == ObstacleKind.None && lane != coinLane) lanes.Add(lane);
            if (lanes.Count == 0) return;
            var kind = (PowerUpKind)_random.Range(0, 4);
            float z = row.Z - (_config.ObstacleHalfDepth + 2.5f);
            powerUpsOut.Add(new PowerUpItem(_nextPowerUpId++, kind, lanes[_random.Range(0, lanes.Count)], z));
        }

        /// <summary>Gibt die Spur der Münzreihe zurück (oder int.MinValue, wenn keine).</summary>
        private int AddCoins(ObstacleRow previous, ObstacleRow row, List<Coin> coinsOut)
        {
            if (_random.NextFloat() > 0.55f) return int.MinValue;

            // Münzen liegen in einer Spur, die bei der kommenden Reihe begehbar ist –
            // sie zeigen damit einen sicheren Weg.
            var candidates = new List<int>(Lanes.Count);
            for (int lane = Lanes.Left; lane <= Lanes.Right; lane++)
                if (row.At(lane) == ObstacleKind.None) candidates.Add(lane);
            if (candidates.Count == 0) return int.MinValue;
            int coinLane = candidates[_random.Range(0, candidates.Count)];

            float start = previous?.Z ?? 0f;
            float clearance = _config.ObstacleHalfDepth + _config.CoinPickupRadius + 1f;
            float from = start + clearance;
            float to = row.Z - clearance;
            float length = (_config.CoinsPerLine - 1) * _config.CoinSpacing;
            if (to - from < length) return int.MinValue;
            float first = from + (to - from - length) * 0.5f;
            for (int i = 0; i < _config.CoinsPerLine; i++)
                coinsOut.Add(new Coin(_nextCoinId++, coinLane, first + i * _config.CoinSpacing, _config.CoinHeight));
            return coinLane;
        }
    }
}
