using System;

namespace WorldRush.Core
{
    /// <summary>Hindernistypen. Jeder Typ verlangt genau eine Reaktion.</summary>
    public enum ObstacleKind
    {
        None = 0,
        /// <summary>Niedrige Hürde – drüberspringen.</summary>
        Hurdle,
        /// <summary>Hoher Balken – drunter durchrutschen.</summary>
        Overhead,
        /// <summary>Wand – Spur wechseln.</summary>
        Block
    }

    /// <summary>Eine Querreihe auf der Strecke: pro Spur ein Hindernis (oder keins).</summary>
    public sealed class ObstacleRow
    {
        public ObstacleRow(int id, float z, ObstacleKind left, ObstacleKind middle, ObstacleKind right)
        {
            Id = id;
            Z = z;
            Slots = new[] { left, middle, right };
        }

        public int Id { get; }
        public float Z { get; }
        public ObstacleKind[] Slots { get; }

        public ObstacleKind At(int lane) => Slots[Lanes.ToIndex(lane)];

        public bool IsPassable(int lane) => At(lane) != ObstacleKind.Block;

        public bool HasAnyPassableLane
        {
            get
            {
                for (int lane = Lanes.Left; lane <= Lanes.Right; lane++)
                    if (IsPassable(lane)) return true;
                return false;
            }
        }

        public bool IsEmpty
        {
            get
            {
                foreach (var s in Slots) if (s != ObstacleKind.None) return false;
                return true;
            }
        }

        public override string ToString() => $"Row#{Id} z={Z:0.0} [{Slots[0]},{Slots[1]},{Slots[2]}]";
    }

    public sealed class Coin
    {
        public Coin(int id, int lane, float z, float y)
        {
            Id = id;
            Lane = lane;
            Z = z;
            Y = y;
        }

        public int Id { get; }
        public int Lane { get; }
        public float Z { get; }
        public float Y { get; }
        public bool Collected { get; internal set; }
    }

    public enum PowerUpKind
    {
        /// <summary>Surfboard: schweben über allen Hindernissen, Münzspur in der Luft.</summary>
        Board = 0,
        /// <summary>Magnet: Münzen aus allen Spuren werden eingesammelt.</summary>
        Magnet = 1,
        /// <summary>Schild: ein Zusammenstoß wird verziehen.</summary>
        Shield = 2,
        /// <summary>Doppelte Münzpunkte.</summary>
        Double = 3
    }

    public sealed class PowerUpItem
    {
        public PowerUpItem(int id, PowerUpKind kind, int lane, float z)
        {
            Id = id;
            Kind = kind;
            Lane = lane;
            Z = z;
        }

        public int Id { get; }
        public PowerUpKind Kind { get; }
        public int Lane { get; }
        public float Z { get; }
        public bool Collected { get; internal set; }
    }

    /// <summary>
    /// Kleiner, plattformunabhängiger Zufallsgenerator (xorshift32).
    /// Gleicher Seed = gleiche Strecke auf jedem Gerät.
    /// </summary>
    public sealed class DeterministicRandom
    {
        private uint _state;

        public DeterministicRandom(int seed)
        {
            _state = (uint)seed ^ 0x9E3779B9u;
            if (_state == 0) _state = 0x6D2B79F5u;
            for (int i = 0; i < 4; i++) NextUInt();
        }

        public uint NextUInt()
        {
            uint x = _state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            _state = x;
            return x;
        }

        /// <summary>0 (inklusive) bis 1 (exklusive).</summary>
        public float NextFloat() => (NextUInt() >> 8) * (1f / 16777216f);

        /// <summary>min (inklusive) bis max (exklusive).</summary>
        public int Range(int min, int max)
        {
            if (max <= min) throw new ArgumentException("max must be greater than min");
            return min + (int)(NextUInt() % (uint)(max - min));
        }

        public float Range(float min, float max) => min + (max - min) * NextFloat();
    }
}
