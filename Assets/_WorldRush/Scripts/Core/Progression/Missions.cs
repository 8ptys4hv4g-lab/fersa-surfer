using System;
using System.Collections.Generic;

namespace WorldRush.Core
{
    public enum MissionKind { CollectCoins, DoTricks, Slides, Jumps, Distance, FinishLevels, PowerUps }

    /// <summary>Ergebnis einer Runde – daraus wird der Aufgaben-Fortschritt berechnet.</summary>
    public struct RunStats
    {
        public int Coins, Tricks, Slides, Jumps, Distance, LevelsFinished, PowerUps;

        public static RunStats From(GameSession s) => new RunStats
        {
            Coins = s.Score.Coins,
            Tricks = s.Tricks,
            Slides = s.SlideCount,
            Jumps = s.JumpCount,
            Distance = (int)s.Score.Distance,
            LevelsFinished = s.State == GameState.Won ? 1 : 0,
            PowerUps = s.PowerUpsCollected
        };
    }

    public sealed class Mission
    {
        public Mission(MissionKind kind, int target, int reward)
        {
            Kind = kind;
            Target = Math.Max(1, target);
            Reward = Math.Max(0, reward);
        }

        public MissionKind Kind { get; }
        public int Target { get; }
        public int Reward { get; }
        public int Progress { get; internal set; }
        public bool Completed => Progress >= Target;

        public string Title => Kind switch
        {
            MissionKind.CollectCoins => $"Sammle {Target} Münzen",
            MissionKind.DoTricks => $"Mach {Target} Saltos beim Surfen",
            MissionKind.Slides => $"Rutsche {Target}× unter Balken durch",
            MissionKind.Jumps => $"Spring {Target}×",
            MissionKind.Distance => $"Lauf insgesamt {Target} m",
            MissionKind.FinishLevels => Target == 1 ? "Schaffe 1 Level bis ins Ziel" : $"Schaffe {Target} Level bis ins Ziel",
            _ => $"Sammle {Target} Power-ups"
        };

        public int ValueOf(RunStats s) => Kind switch
        {
            MissionKind.CollectCoins => s.Coins,
            MissionKind.DoTricks => s.Tricks,
            MissionKind.Slides => s.Slides,
            MissionKind.Jumps => s.Jumps,
            MissionKind.Distance => s.Distance,
            MissionKind.FinishLevels => s.LevelsFinished,
            _ => s.PowerUps
        };
    }

    /// <summary>
    /// Drei Aufgaben pro "Satz" (z. B. pro Tag). Fortschritt sammelt sich über mehrere Runden.
    /// Gleiche Satz-Nummer = gleiche Aufgaben auf jedem Gerät.
    /// </summary>
    public sealed class MissionBoard
    {
        private static readonly (MissionKind kind, int[] targets)[] Table =
        {
            (MissionKind.CollectCoins, new[] { 40, 80, 150 }),
            (MissionKind.DoTricks, new[] { 3, 5, 10 }),
            (MissionKind.Slides, new[] { 10, 20, 35 }),
            (MissionKind.Jumps, new[] { 15, 30, 50 }),
            (MissionKind.Distance, new[] { 1000, 2500, 5000 }),
            (MissionKind.FinishLevels, new[] { 1, 2, 3 }),
            (MissionKind.PowerUps, new[] { 2, 4, 7 }),
        };

        private MissionBoard(int setId, List<Mission> missions)
        {
            SetId = setId;
            Missions = missions;
        }

        public int SetId { get; }
        public IReadOnlyList<Mission> Missions { get; }

        public static MissionBoard Create(int setId)
        {
            var rnd = new DeterministicRandom(setId * 7919 + 13);
            var kinds = new List<int>();
            while (kinds.Count < 3)
            {
                int k = rnd.Range(0, Table.Length);
                if (!kinds.Contains(k)) kinds.Add(k);
            }
            var missions = new List<Mission>(3);
            foreach (int k in kinds)
            {
                int tier = rnd.Range(0, 3);
                missions.Add(new Mission(Table[k].kind, Table[k].targets[tier], 50 + tier * 50));
            }
            return new MissionBoard(setId, missions);
        }

        /// <summary>Lädt den Satz und seinen gespeicherten Fortschritt. Anderer Satz (neuer Tag) = Neustart.</summary>
        public static MissionBoard Load(int currentSetId, SaveData save)
        {
            var board = Create(currentSetId);
            if (save != null && save.MissionSet == currentSetId)
            {
                board.Missions[0].Progress = Math.Max(0, save.Mission0);
                board.Missions[1].Progress = Math.Max(0, save.Mission1);
                board.Missions[2].Progress = Math.Max(0, save.Mission2);
            }
            return board;
        }

        public void Store(SaveData save)
        {
            save.MissionSet = SetId;
            save.Mission0 = Missions[0].Progress;
            save.Mission1 = Missions[1].Progress;
            save.Mission2 = Missions[2].Progress;
        }

        /// <summary>Rechnet eine Runde an. Gibt die dadurch neu erfüllten Aufgaben zurück.</summary>
        public List<Mission> ApplyRun(RunStats stats)
        {
            var done = new List<Mission>();
            foreach (var m in Missions)
            {
                if (m.Completed) continue;
                int add = Math.Max(0, m.ValueOf(stats));
                m.Progress = (int)Math.Min(m.Target, (long)m.Progress + add);
                if (m.Completed) done.Add(m);
            }
            return done;
        }

        /// <summary>Satz-Nummer für ein Datum (ein neuer Satz pro Tag).</summary>
        public static int SetIdForDate(DateTime utcDate) => (int)(utcDate.Date - new DateTime(2026, 1, 1)).TotalDays;
    }
}
