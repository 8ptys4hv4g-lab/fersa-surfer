using System;

namespace WorldRush.Core
{
    /// <summary>
    /// Zeigt beim ersten Spielen kurze Hinweise zum nächsten Hindernis-Typ (Wand, Hürde, Balken, Wasser).
    /// Ein Typ gilt als gelernt, sobald die Figur ein solches Hindernis hinter sich gelassen hat.
    /// </summary>
    public sealed class TutorialCoach
    {
        public const float LookAhead = 28f;
        private bool _lane, _jump, _slide, _trick;
        private float _doneShownUntil = -1f;

        public bool Active { get; private set; }
        public bool Finished => _lane && _jump && _slide && _trick;

        public void Begin() { Active = true; _lane = _jump = _slide = _trick = false; _doneShownUntil = -1f; }
        public void Stop() => Active = false;

        /// <summary>Gibt den Text-Schlüssel des aktuellen Hinweises zurück (oder null).</summary>
        public string Update(GameSession s)
        {
            if (!Active || s == null || s.State != GameState.Running) return null;

            if (!_trick && s.IsInWater) { if (s.Tricks > 0) _trick = true; else return "tut.trick"; }

            string hint = null;
            foreach (var row in s.Rows)
            {
                float ahead = row.Z - s.PlayerZ;
                for (int lane = Lanes.Left; lane <= Lanes.Right; lane++)
                {
                    var kind = row.At(lane);
                    if (kind == ObstacleKind.None) continue;
                    if (ahead < -1f) { Learn(kind); continue; } // überstanden
                    if (hint == null && ahead <= LookAhead && lane == s.Motor.TargetLane && !Learned(kind)) hint = Key(kind);
                }
            }

            if (Finished)
            {
                if (_doneShownUntil < 0f) _doneShownUntil = s.ElapsedTime + 2.5f;
                if (s.ElapsedTime < _doneShownUntil) return "tut.done";
                Active = false;
                return null;
            }
            return hint;
        }

        private void Learn(ObstacleKind k)
        {
            if (k == ObstacleKind.Block) _lane = true;
            else if (k == ObstacleKind.Hurdle) _jump = true;
            else if (k == ObstacleKind.Overhead) _slide = true;
        }

        private bool Learned(ObstacleKind k) => k == ObstacleKind.Block ? _lane : k == ObstacleKind.Hurdle ? _jump : _slide;
        private static string Key(ObstacleKind k) => k == ObstacleKind.Block ? "tut.lane" : k == ObstacleKind.Hurdle ? "tut.jump" : "tut.slide";

        /// <summary>Nur für Tests: alle Typen als gelernt markieren außer Salto.</summary>
        internal void MarkLandLearned() { _lane = _jump = _slide = true; }
    }
}
