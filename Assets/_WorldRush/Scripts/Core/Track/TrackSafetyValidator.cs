using System;
using System.Collections.Generic;

namespace WorldRush.Core
{
    /// <summary>
    /// Prüft, dass eine Strecke immer schaffbar ist.
    /// Regeln:
    /// 1. Jede Reihe hat mindestens eine Spur ohne Wand.
    /// 2. Zwischen zwei Reihen ist genug Abstand zum Reagieren (abhängig vom Tempo).
    /// 3. Von JEDER begehbaren Spur einer Reihe erreicht man rechtzeitig eine
    ///    begehbare Spur der nächsten Reihe (keine Sackgassen).
    /// </summary>
    public sealed class TrackSafetyValidator
    {
        private readonly RunnerConfig _config;

        public TrackSafetyValidator(RunnerConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public bool IsRowSafe(ObstacleRow row) => row != null && row.HasAnyPassableLane;

        /// <summary>Prüft, ob <paramref name="next"/> sicher auf <paramref name="previous"/> folgen darf.</summary>
        public bool IsTransitionSafe(ObstacleRow previous, ObstacleRow next, float speed, out string reason)
        {
            reason = null;
            if (!IsRowSafe(next))
            {
                reason = "Reihe blockiert alle drei Spuren.";
                return false;
            }
            if (previous == null) return true;

            if (speed <= 0f) speed = _config.StartSpeed;
            float gap = next.Z - previous.Z;
            float minGap = _config.MinRowGapMeters(speed);
            if (gap + 1e-3f < minGap)
            {
                reason = $"Abstand {gap:0.0} m ist kleiner als {minGap:0.0} m.";
                return false;
            }

            float gapTime = gap / speed;
            for (int from = Lanes.Left; from <= Lanes.Right; from++)
            {
                if (!previous.IsPassable(from)) continue;
                bool escape = false;
                for (int to = Lanes.Left; to <= Lanes.Right && !escape; to++)
                {
                    if (!next.IsPassable(to)) continue;
                    float needed = _config.ReactionTime + Math.Abs(to - from) * _config.LaneChangeDuration;
                    if (needed <= gapTime) escape = true;
                }
                if (!escape)
                {
                    reason = $"Sackgasse: von Spur {from} keine freie Spur rechtzeitig erreichbar.";
                    return false;
                }
            }
            return true;
        }

        /// <summary>Prüft eine ganze Strecke (sortiert nach Z).</summary>
        public bool IsTrackSafe(IReadOnlyList<ObstacleRow> rows, float speed, out string reason)
        {
            reason = null;
            ObstacleRow previous = null;
            foreach (var row in rows)
            {
                if (previous == null && row.Z < _config.SafeStartDistance - 1e-3f)
                {
                    reason = "Erstes Hindernis liegt zu nah am Start.";
                    return false;
                }
                if (!IsTransitionSafe(previous, row, speed, out reason)) return false;
                previous = row;
            }
            return true;
        }
    }
}
