using System;
using System.Collections.Generic;

namespace WorldRush.Core
{
    /// <summary>Spieler-Einstellungen. Werte werden beim Laden auf gültige Bereiche begrenzt.</summary>
    public sealed class GameSettings
    {
        public float MusicVolume = 0.8f;
        public float SfxVolume = 1f;
        /// <summary>0 = sparsam (schwache Handys), 1 = mittel, 2 = hoch.</summary>
        public int Quality = 2;
        public Language Language = Language.German;
        public bool TutorialDone;

        public string ToJson() => MiniJson.Serialize(new Dictionary<string, object>
        {
            { "music", Math.Round(MusicVolume, 2) },
            { "sfx", Math.Round(SfxVolume, 2) },
            { "quality", Quality },
            { "language", (int)Language },
            { "tutorialDone", TutorialDone }
        });

        public static GameSettings FromJson(string json)
        {
            var s = new GameSettings();
            if (!MiniJson.TryParse(json, out var root) || !(root is Dictionary<string, object> d)) return s;
            if (d.TryGetValue("music", out var m) && m is double md) s.MusicVolume = Clamp01((float)md);
            if (d.TryGetValue("sfx", out var f) && f is double fd) s.SfxVolume = Clamp01((float)fd);
            if (d.TryGetValue("quality", out var q) && q is double qd) s.Quality = Math.Max(0, Math.Min(2, (int)qd));
            if (d.TryGetValue("language", out var l) && l is double ld && (int)ld >= 0 && (int)ld <= 1) s.Language = (Language)(int)ld;
            if (d.TryGetValue("tutorialDone", out var t) && t is bool tb) s.TutorialDone = tb;
            return s;
        }

        /// <summary>Sprache aus der Systemsprache ableiten (Deutsch, sonst Englisch).</summary>
        public static Language LanguageFor(string isoTwoLetter) =>
            string.Equals(isoTwoLetter, "de", StringComparison.OrdinalIgnoreCase) ? Language.German : Language.English;

        private static float Clamp01(float v) => float.IsNaN(v) ? 0f : Math.Max(0f, Math.Min(1f, v));
    }
}
