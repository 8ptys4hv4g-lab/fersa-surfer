using System;
using System.Collections.Generic;

namespace WorldRush.Core
{
    /// <summary>
    /// Ein Level: feste Länge bis zur Ziellinie, dazu Surf-Abschnitte (Wasser).
    /// Levels werden aus ihrer Nummer erzeugt – jedes Level ist etwas länger und hat mehr Wasser.
    /// </summary>
    public sealed class LevelDefinition
    {
        private static readonly string[] Names = { "Neon-Kanal", "Hafen-Sprint", "Wellen-Allee", "Sternen-Bucht", "Gewitter-Wellen", "Mitternachts-Surf" };

        public LevelDefinition(int index, string name, float length, IReadOnlyList<(float start, float end)> water)
        {
            if (length <= 0f) throw new ArgumentOutOfRangeException(nameof(length));
            Index = index;
            Name = name;
            Length = length;
            Water = water ?? Array.Empty<(float, float)>();
        }

        public int Index { get; }
        public string Name { get; }
        public float Length { get; }
        public IReadOnlyList<(float start, float end)> Water { get; }

        /// <summary>Kein Hindernis in diesem Abstand vor dem Ziel.</summary>
        public const float FinishClearance = 30f;

        public bool IsWater(float z)
        {
            foreach (var (start, end) in Water)
                if (z >= start && z <= end) return true;
            return false;
        }

        public static LevelDefinition Create(int index)
        {
            index = Math.Max(0, index);
            float length = 520f + index * 140f;
            var water = new List<(float, float)>();
            float firstWater = index == 0 ? 70f : 170f; // Level 1: Surfen kommt früh, damit man es sofort erlebt
            for (float a = firstWater; a + 140f < length - 60f; a += 320f)
                water.Add((a, a + 140f + Math.Min(index, 4) * 10f));
            string name = Names[index % Names.Length] + (index >= Names.Length ? " " + (index / Names.Length + 1) : "");
            return new LevelDefinition(index, name, length, water);
        }

        /// <summary>1 Stern fürs Ziel, +1 ab 50 % der Münzen, +1 ab 80 %.</summary>
        public static int StarsFor(int collected, int total)
        {
            if (total <= 0) return 3;
            float ratio = collected / (float)total;
            return 1 + (ratio >= 0.5f ? 1 : 0) + (ratio >= 0.8f ? 1 : 0);
        }
    }
}
