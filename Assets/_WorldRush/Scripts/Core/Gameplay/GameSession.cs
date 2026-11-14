using System;
using System.Collections.Generic;

namespace WorldRush.Core
{
    public enum GameState
    {
        Ready = 0,
        Running,
        Paused,
        GameOver
    }

    /// <summary>
    /// Das komplette Runner-Spiel als reine C#-Logik: Zustand, Bewegung, Strecke,
    /// Kollisionen, Münzen, Punkte. Unity zeigt diesen Zustand nur an.
    /// </summary>
    public sealed class GameSession
    {
        private readonly RunnerConfig _config;
        private readonly List<ObstacleRow> _rows = new List<ObstacleRow>();
        private readonly List<Coin> _coins = new List<Coin>();
        private readonly Queue<RunnerCommand> _pending = new Queue<RunnerCommand>();
        private TrackGenerator _generator;

        public GameSession(RunnerConfig config, int seed, int highScore = 0)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            Motor = new RunnerMotor(config);
            Score = new ScoreService(config, highScore);
            Seed = seed;
            ResetWorld(seed);
        }

        public event Action<GameState> StateChanged;
        public event Action<Coin> CoinCollected;
        public event Action<ObstacleRow, ObstacleKind> Crashed;

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
        public int FallbackRowsUsed => _generator.FallbackRowsUsed;

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

        /// <summary>Simuliert <paramref name="deltaTime"/> Sekunden in kleinen Teilschritten.</summary>
        public void Tick(float deltaTime)
        {
            if (State != GameState.Running || deltaTime <= 0f) return;

            while (_pending.Count > 0) Motor.Apply(_pending.Dequeue());

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
            Speed = Math.Min(_config.MaxSpeed, Speed + _config.Acceleration * dt);
            Motor.Tick(dt);

            float moved = Speed * dt;
            PlayerZ += moved;
            Score.AddDistance(moved);

            _generator.GenerateUntil(PlayerZ + _config.GenerateAheadDistance, Speed, _rows, _coins);

            float x = Motor.LateralX;
            float feet = Motor.FeetY;
            float top = Motor.TopY;

            foreach (var row in _rows)
            {
                if (Math.Abs(row.Z - PlayerZ) > 3f) continue;
                for (int lane = Lanes.Left; lane <= Lanes.Right; lane++)
                {
                    var kind = row.At(lane);
                    if (CollisionRules.HitsObstacle(_config, x, PlayerZ, feet, top, lane, row.Z, kind))
                    {
                        Crash(row, kind);
                        return;
                    }
                }
            }

            foreach (var coin in _coins)
            {
                if (CollisionRules.CollectsCoin(_config, x, PlayerZ, feet, top, coin))
                {
                    coin.Collected = true;
                    Score.AddCoin();
                    CoinCollected?.Invoke(coin);
                }
            }

            float cleanupZ = PlayerZ - _config.CleanupBehindDistance;
            _rows.RemoveAll(r => r.Z < cleanupZ);
            _coins.RemoveAll(c => c.Z < cleanupZ || c.Collected);
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
            _pending.Clear();
            Motor.Reset();
            Score.Reset();
            PlayerZ = 0f;
            ElapsedTime = 0f;
            Speed = _config.StartSpeed;
            _generator = new TrackGenerator(_config, seed);
            _generator.GenerateUntil(_config.GenerateAheadDistance, Speed, _rows, _coins);
        }

        private void SetState(GameState state)
        {
            if (State == state) return;
            State = state;
            StateChanged?.Invoke(state);
        }
    }
}
