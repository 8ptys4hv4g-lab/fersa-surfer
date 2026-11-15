using System.Collections.Generic;
using UnityEngine;

namespace WorldRush.Runtime
{
    /// <summary>
    /// Knöpfe und einzeilige Texte, die ihre Schrift verkleinern, statt abgeschnitten zu werden
    /// (wichtig für lange deutsche Wörter, Englisch und kleine Handys).
    /// </summary>
    internal static class UiFit
    {
        private static readonly Dictionary<GUIStyle, GUIStyle> Cache = new Dictionary<GUIStyle, GUIStyle>();
        private static readonly GUIContent Content = new GUIContent();

        private static GUIStyle Fit(GUIStyle baseStyle, string text, float width)
        {
            if (!Cache.TryGetValue(baseStyle, out var fit))
            {
                fit = new GUIStyle(baseStyle);
                Cache[baseStyle] = fit;
            }
            fit.fontSize = baseStyle.fontSize > 0 ? baseStyle.fontSize : 16;
            fit.wordWrap = false;
            Content.text = text;
            float room = width - 6f;
            while (fit.fontSize > 9 && fit.CalcSize(Content).x > room) fit.fontSize--;
            return fit;
        }

        private static readonly Dictionary<(GUIStyle, int, int, bool, int, int), GUIStyle> Styles = new Dictionary<(GUIStyle, int, int, bool, int, int), GUIStyle>();

        /// <summary>
        /// Abgewandelter Stil (Schriftgröße, Ausrichtung …), der nur einmal angelegt und dann wiederverwendet wird –
        /// statt in jedem Bild neue Stile zu erzeugen (spart Speicher-Aufräumarbeit und Ruckler auf Handys).
        /// </summary>
        public static GUIStyle Style(GUIStyle baseStyle, int fontSize = 0, TextAnchor? align = null, bool rich = false, bool? wrap = null, TextClipping? clip = null)
        {
            var key = (baseStyle, fontSize, align.HasValue ? (int)align.Value : -1, rich, wrap.HasValue ? (wrap.Value ? 1 : 0) : -1, clip.HasValue ? (int)clip.Value : -1);
            if (!Styles.TryGetValue(key, out var st))
            {
                st = new GUIStyle(baseStyle) { richText = rich };
                if (fontSize > 0) st.fontSize = fontSize;
                if (align.HasValue) st.alignment = align.Value;
                if (wrap.HasValue) st.wordWrap = wrap.Value;
                if (clip.HasValue) st.clipping = clip.Value;
                if (Styles.Count > 400) Styles.Clear(); // z. B. nach vielen Fenstergrößen-Wechseln
                Styles[key] = st;
            }
            if (fontSize <= 0) st.fontSize = baseStyle.fontSize; // folgt der aktuellen Bildschirmgröße
            return st;
        }

        public static bool Button(Rect r, string text, GUIStyle style) => GUI.Button(r, text, Fit(style, text, r.width));

        public static void Label(Rect r, string text, GUIStyle style) => GUI.Label(r, text, Fit(style, text, r.width));
    }
}
