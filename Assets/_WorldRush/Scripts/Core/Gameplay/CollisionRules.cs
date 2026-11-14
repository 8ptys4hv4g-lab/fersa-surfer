using System;

namespace WorldRush.Core
{
    /// <summary>
    /// Deterministische Kollisionsregeln mit einfachen Boxen.
    /// Gleiche Eingaben ergeben immer dasselbe Ergebnis – wichtig für Fairness und Tests.
    /// </summary>
    public static class CollisionRules
    {
        public static bool HitsObstacle(RunnerConfig c, float playerX, float playerZ, float feetY, float topY,
            int obstacleLane, float obstacleZ, ObstacleKind kind)
        {
            if (kind == ObstacleKind.None) return false;

            float obstacleX = Lanes.ToX(obstacleLane, c.LaneWidth);
            if (Math.Abs(playerX - obstacleX) >= c.PlayerHalfWidth + c.ObstacleHalfWidth) return false;
            if (Math.Abs(playerZ - obstacleZ) >= c.PlayerHalfDepth + c.ObstacleHalfDepth) return false;

            GetVerticalRange(c, kind, out float bottom, out float top);
            return feetY < top && topY > bottom;
        }

        public static void GetVerticalRange(RunnerConfig c, ObstacleKind kind, out float bottom, out float top)
        {
            switch (kind)
            {
                case ObstacleKind.Hurdle:
                    bottom = 0f; top = c.HurdleTop; break;
                case ObstacleKind.Overhead:
                    bottom = c.OverheadBottom; top = c.OverheadTop; break;
                case ObstacleKind.Block:
                    bottom = 0f; top = c.BlockTop; break;
                default:
                    bottom = 0f; top = 0f; break;
            }
        }

        public static bool CollectsCoin(RunnerConfig c, float playerX, float playerZ, float feetY, float topY, Coin coin)
        {
            if (coin.Collected) return false;
            float coinX = Lanes.ToX(coin.Lane, c.LaneWidth);
            if (Math.Abs(playerX - coinX) > c.CoinPickupRadius) return false;
            if (Math.Abs(playerZ - coin.Z) > c.CoinPickupRadius) return false;
            return coin.Y >= feetY - c.CoinPickupRadius && coin.Y <= topY + c.CoinPickupRadius;
        }
    }
}
