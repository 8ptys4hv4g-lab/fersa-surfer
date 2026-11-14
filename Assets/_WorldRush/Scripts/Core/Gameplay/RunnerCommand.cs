namespace WorldRush.Core
{
    /// <summary>Was der Spieler tun möchte – egal ob per Wischen, Tastatur oder Gamepad.</summary>
    public enum RunnerCommand
    {
        None = 0,
        MoveLeft,
        MoveRight,
        Jump,
        Slide
    }

    public static class Lanes
    {
        public const int Left = -1;
        public const int Middle = 0;
        public const int Right = 1;
        public const int Count = 3;

        public static float ToX(int lane, float laneWidth) => lane * laneWidth;
        public static int ToIndex(int lane) => lane + 1;
        public static int FromIndex(int index) => index - 1;
        public static bool IsValid(int lane) => lane >= Left && lane <= Right;
    }
}
