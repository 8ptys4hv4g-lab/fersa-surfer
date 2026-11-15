using System;
using System.Collections.Generic;

namespace WorldRush.Core
{
    public enum GameState
    {
        Ready = 0,
        Running,
        Paused,
        GameOver,
        /// <summary>Ziellinie eines Levels erreicht.</summary>
        Won
    }

    /// <summary>
    /// Das komplette Runner-Spiel als reine C#-Logik: Zustand, Bewegung, Strecke,
    /// Kollisionen, Münzen, Power-ups, Combo, Punkte. Unity zeigt diesen Zustand nur an.
    /// </summary>
    public sealed class GameSession
    {
        private const int SkyCoinIdBase = 1_000_000_000;

        private readonly RunnerConfig _config;
        private readonly List<ObstacleRow> _rows = new List<ObstacleRow>();
        private readonly List<Coin> _coins = new List<Coin>();
        private readonly List<PowerUpItem> _powerUps = new List<PowerUpItem>();
        private readonly Queue<RunnerCommand> _pending = new Queue<RunnerCommand>();
        private readonly float[] _powerTime = new float[4];
        private TrackGenerator _generator;
        private DeterministicRandom _random;
        private int _nextSkyCoinId;

        public GameSession(RunnerConfig config, int seed, int highScore = 0, LevelDefinition level = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            Level = level;
            Motor = new RunnerMotor(config);
            Score = new ScoreService(config, highScore);
            Seed = seed;
            ResetWorld(seed);
        }

        public event Action<GameState> StateChanged;
        /// <summary>Münze eingesammelt (Münze, erhaltene Punkte).</summary>
        public event Action<Coin, int> CoinCollected;
        public event Action<ObstacleRow, ObstacleKind> Crashed;
        public event Action<PowerUpKind> PowerUpActivated;
        /// <summary>Schild hat einen Zusammenstoß verhindert; die Reihe ist zerstört.</summary>
        public event Action<ObstacleRow> ShieldUsed;
        /// <summary>Neuer Combo-Multiplikator erreicht (2, 3, ...).</summary>
        public event Action<int> ComboMilestone;
        /// <summary>Salto beim Surfen (erhaltene Punkte).</summary>
        public event Action<int> TrickPerformed;

        /// <summary>Aktuelles Level oder null für den Endlos-Modus.</summary>
        public LevelDefinition Level { get; private set; }
        public bool IsInWater => Level != null && Level.IsWater(PlayerZ);
        public int Tricks { get; private set; }
        public int LevelCoinsTotal { get; private set; }
        public float LevelProgress => Level == null ? 0f : Math.Min(1f, PlayerZ / Level.Length);
        public int Stars => LevelDefinition.StarsFor(Score.Coins, LevelCoinsTotal);
        private bool _trickUsed;

        /// <summary>
        /// Springt direkt zu einer Stelle der Strecke (z. B. "Surfen ansehen"). Alte Hindernisse werden entfernt,
        /// die Figur ist kurz unverwundbar.
        /// </summary>
        public void WarpTo(float z, float invulnerableSeconds = 4f)
        {
            if (z <= PlayerZ) return;
            float moved = z - PlayerZ;
            PlayerZ = z;
            Score.AddDistance(moved);
            _rows.RemoveAll(r => r.Z < PlayerZ + 8f);
            _coins.RemoveAll(c => c.Z < PlayerZ);
            _powerUps.RemoveAll(p => p.Z < PlayerZ);
            Invulnerable = Math.Max(Invulnerable, invulnerableSeconds);
            Generate();
            _rows.RemoveAll(r => r.Z < PlayerZ + 8f);
        }

        /// <summary>Wechselt das Level (setzt die Runde zurück).</summary>
        public void SetLevel(LevelDefinition level)
        {
            Level = level;
            ResetWorld(Seed);
            SetState(GameState.Ready);
        }

        public RunnerConfig Config => _config;
        public RunnerMotor Motor { get; }
        public ScoreService Score { get; }
        public GameState State { get; private set; } = GameState.Ready;
        public int Seed { get; private set; }
        public float PlayerZ { get; private set; }
        public float Speed { get; private set; }
        public float ElapsedTime { get; private set; }
        public IReadOnlyList<ObstacleRow> Rows => _rows;
        public IReadOnlyList<Coin> Coins => _coins;
        public IReadOnlyList<PowerUpItem> PowerUps => _powerUps;
        public int FallbackRowsUsed => _generator.FallbackRowsUsed;
        public int Combo { get; private set; }
        public float ComboTimer { get; private set; }
        public float Invulnerable { get; private set; }

        public int ComboMultiplier => Math.Min(_config.MaxComboMultiplier, 1 + Combo / Math.Max(1, _config.ComboStep));
        public float PowerTimeLeft(PowerUpKind kind) => _powerTime[(int)kind];
        public bool IsPowerActive(PowerUpKind kind) => _powerTime[(int)kind] > 0f;
        public static float DurationOf(RunnerConfig c, PowerUpKind kind) =>
            kind == PowerUpKind.Board ? c.BoardDuration :
            kind == PowerUpKind.Magnet ? c.MagnetDuration :
            kind == PowerUpKind.Shield ? c.ShieldDuration : c.DoubleDuration;

        public void Start()
        {
            if (State == GameState.Ready) SetState(GameState.Running);
        }

        public void Pause()
        {
            if (State == GameState.Running) SetState(GameState.Paused);
        }

        public void Resume()
        {
            if (State == GameState.Paused) SetState(GameState.Running);
        }

        public void TogglePause()
        {
            if (State == GameState.Running) Pause();
            else if (State == GameState.Paused) Resume();
        }

        /// <summary>Neue Runde. Ohne Seed wird eine neue Strecke erzeugt.</summary>
        public void Restart(int? seed = null)
        {
            ResetWorld(seed ?? unchecked(Seed * 1103515245 + 12345));
            SetState(GameState.Ready);
            Start();
        }

        public void ReturnToReady()
        {
            ResetWorld(Seed);
            SetState(GameState.Ready);
        }

        public void Enqueue(RunnerCommand command)
        {
            if (command == RunnerCommand.None) return;
            if (State != GameState.Running) return;
            _pending.Enqueue(command);
        }

        /// <summary>Aktiviert ein Power-up (beim Einsammeln; auch für Tests/Shop nutzbar).</summary>
        public void ActivatePowerUp(PowerUpKind kind)
        {
            _powerTime[(int)kind] = DurationOf(_config, kind);
            if (kind == PowerUpKind.Board)
            {
                Motor.SetHovering(true);
                SpawnSkyCoins();
            }
            PowerUpActivated?.Invoke(kind);
        }

        /// <summary>Simuliert <paramref name="deltaTime"/> Sekunden in kleinen Teilschritten.</summary>
        public void Tick(float deltaTime)
        {
            if (State != GameState.Running || deltaTime <= 0f) return;

            while (_pending.Count > 0)
            {
                var cmd = _pending.Dequeue();
                if (cmd == RunnerCommand.Jump && IsInWater && Motor.IsJumping && !_trickUsed) { DoTrick(); continue; }
                Motor.Apply(cmd);
            }

            float remaining = Math.Min(deltaTime, _config.MaxFrameDelta);
            while (remaining > 0f && State == GameState.Running)
            {
                float step = Math.Min(remaining, _config.MaxSubstep);
                Step(step);
                remaining -= step;
            }
        }

        private void Step(float dt)
        {
            ElapsedTime += dt;
            TickTimers(dt);
            Speed = Math.Min(_config.MaxSpeed, Speed + _config.Acceleration * dt);
            Motor.Tick(dt);

            float moved = Speed * dt;
            PlayerZ += moved;
            Score.AddDistance(moved);
            if (!Motor.IsJumping) _trickUsed = false;

            if (Level != null && PlayerZ >= Level.Length)
            {
                Score.CommitHighScore();
                SetState(GameState.Won);
                return;
            }

            Generate();

            float x = Motor.LateralX;
            float feet = Motor.FeetY;
            float top = Motor.TopY;

            foreach (var row in _rows)
            {
                if (Math.Abs(row.Z - PlayerZ) > 3f) continue;
                for (int lane = Lanes.Left; lane <= Lanes.Right; lane++)
                {
                    var kind = row.At(lane);
                    if (!CollisionRules.HitsObstacle(_config, x, PlayerZ, feet, top, lane, row.Z, kind)) continue;
                    if (Invulnerable > 0f) continue;
                    if (IsPowerActive(PowerUpKind.Shield))
                    {
                        _powerTime[(int)PowerUpKind.Shield] = 0f;
                        Invulnerable = _config.ShieldGrace;
                        for (int i = 0; i < row.Slots.Length; i++) row.Slots[i] = ObstacleKind.None;
                        ShieldUsed?.Invoke(row);
                        break;
                    }
                    Crash(row, kind);
                    return;
                }
            }

            bool magnet = IsPowerActive(PowerUpKind.Magnet);
            foreach (var coin in _coins)
            {
                if (coin.Collected) continue;
                bool hit = CollisionRules.CollectsCoin(_config, x, PlayerZ, feet, top, coin);
                if (!hit && magnet)
                {
                    float ahead = coin.Z - PlayerZ;
                    hit = ahead >= -_config.CoinPickupRadius && ahead <= _config.MagnetRange;
                }
                if (hit) CollectCoin(coin);
            }

            foreach (var p in _powerUps)
            {
                if (p.Collected) continue;
                if (Math.Abs(p.Z - PlayerZ) > _config.CoinPickupRadius + 0.1f) continue;
                if (Math.Abs(x - Lanes.ToX(p.Lane, _config.LaneWidth)) > _config.CoinPickupRadius + 0.1f) continue;
                if (feet > 1.8f) continue;
                p.Collected = true;
                ActivatePowerUp(p.Kind);
            }

            float cleanupZ = PlayerZ - _config.CleanupBehindDistance;
            _rows.RemoveAll(r => r.Z < cleanupZ);
            _coins.RemoveAll(c => c.Z < cleanupZ || c.Collected);
            _powerUps.RemoveAll(p => p.Z < cleanupZ || p.Collected);
        }

        private void Generate()
        {
            float until = PlayerZ + _config.GenerateAheadDistance;
            if (Level != null) until = Math.Min(until, Level.Length - LevelDefinition.FinishClearance);
            int before = _coins.Count;
            _generator.GenerateUntil(until, Speed, _rows, _coins, _powerUps);
            LevelCoinsTotal += _coins.Count - before;
        }

        private void DoTrick()
        {
            _trickUsed = true;
            Tricks++;
            int points = 100 * ComboMultiplier * (IsPowerActive(PowerUpKind.Double) ? 2 : 1);
            Score.AddBonus(points);
            TrickPerformed?.Invoke(points);
        }

        private void TickTimers(float dt)
        {
            bool wasHovering = IsPowerActive(PowerUpKind.Board);
            for (int i = 0; i < _powerTime.Length; i++) _powerTime[i] = Math.Max(0f, _powerTime[i] - dt);
            if (wasHovering && !IsPowerActive(PowerUpKind.Board))
            {
                Motor.SetHovering(false);
                Invulnerable = Math.Max(Invulnerable, _config.LandingGrace);
            }
            Invulnerable = Math.Max(0f, Invulnerable - dt);
            ComboTimer = Math.Max(0f, ComboTimer - dt);
            if (ComboTimer <= 0f) Combo = 0;
        }

        private void CollectCoin(Coin coin)
        {
            coin.Collected = true;
            int before = ComboMultiplier;
            Combo++;
            ComboTimer = _config.ComboWindow;
            int multiplier = ComboMultiplier * (IsPowerActive(PowerUpKind.Double) ? 2 : 1);
            int points = Score.AddCoin(multiplier);
            CoinCollected?.Invoke(coin, points);
            if (ComboMultiplier > before) ComboMilestone?.Invoke(ComboMultiplier);
        }

        /// <summary>Münzspur in der Luft für das Surfboard, mit gelegentlichem Spurwechsel.</summary>
        private void SpawnSkyCoins()
        {
            int lane = Motor.TargetLane;
            float length = _config.BoardDuration * Math.Max(Speed, _config.StartSpeed);
            for (float d = 10f; d < length; d += 2.6f)
            {
                if (_random.NextFloat() < 0.08f)
                    lane = Math.Max(Lanes.Left, Math.Min(Lanes.Right, lane + (_random.NextFloat() < 0.5f ? -1 : 1)));
                _coins.Add(new Coin(SkyCoinIdBase + _nextSkyCoinId++, lane, PlayerZ + d, _config.SkyCoinHeight));
            }
        }

        private void Crash(ObstacleRow row, ObstacleKind kind)
        {
            Score.CommitHighScore();
            SetState(GameState.GameOver);
            Crashed?.Invoke(row, kind);
        }

        private void ResetWorld(int seed)
        {
            Seed = seed;
            _rows.Clear();
            _coins.Clear();
            _powerUps.Clear();
            _pending.Clear();
            Array.Clear(_powerTime, 0, _powerTime.Length);
            Motor.Reset();
            Score.Reset();
            PlayerZ = 0f;
            ElapsedTime = 0f;
            Combo = 0;
            ComboTimer = 0f;
            Invulnerable = 0f;
            _nextSkyCoinId = 0;
            Tricks = 0;
            LevelCoinsTotal = 0;
            _trickUsed = false;
            Speed = _config.StartSpeed;
            _random = new DeterministicRandom(unchecked(seed * 31 + 7));
            _generator = new TrackGenerator(_config, seed);
            Generate();
        }

        private void SetState(GameState state)
        {
            if (State == state) return;
            State = state;
            StateChanged?.Invoke(state);
        }
    }
}
