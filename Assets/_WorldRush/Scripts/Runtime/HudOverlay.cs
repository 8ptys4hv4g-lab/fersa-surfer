using UnityEngine;
using WorldRush.Core;

namespace WorldRush.Runtime
{
    /// <summary>
    /// Prototyp-Anzeige (Punkte, Münzen, Pause, Game Over) mit Unity IMGUI.
    /// Bewusst einfach: In M3 wird sie durch das echte Menü (UI Toolkit + eigene Schrift) ersetzt.
    /// </summary>
    public sealed class HudOverlay : MonoBehaviour
    {
        private GameRoot _root;
        private GUIStyle _big, _mid, _small, _button;
        private float _scale = 1f;

        public void Init(GameRoot root) => _root = root;

        private void EnsureStyles()
        {
            _scale = Mathf.Max(1f, Mathf.Min(Screen.width, Screen.height) / 720f);
            if (_big != null) return;
            _big = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            _mid = new GUIStyle(_big);
            _small = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            _button = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold };
            foreach (var s in new[] { _big, _mid, _small, _button }) s.normal.textColor = Color.white;
        }

        private void OnGUI()
        {
            if (_root == null || _root.Session == null) return;
            EnsureStyles();
            _big.fontSize = (int)(54 * _scale);
            _mid.fontSize = (int)(26 * _scale);
            _small.fontSize = (int)(22 * _scale);
            _button.fontSize = (int)(26 * _scale);

            var s = _root.Session;
            Rect safe = Screen.safeArea;
            safe.y = Screen.height - safe.yMax; // IMGUI zählt von oben
            float pad = 16f * _scale;

            GUI.Label(new Rect(safe.x + pad, safe.y + pad, 400 * _scale, 34 * _scale), $"Punkte  {s.Score.Score}", _small);
            GUI.Label(new Rect(safe.x + pad, safe.y + pad + 32 * _scale, 400 * _scale, 34 * _scale), $"Münzen  {s.Score.Coins}", _small);
            GUI.Label(new Rect(safe.x + pad, safe.y + pad + 64 * _scale, 400 * _scale, 34 * _scale), $"Bestwert  {s.Score.HighScore}", _small);

            if (s.State == GameState.Running || s.State == GameState.Paused)
            {
                var pr = new Rect(safe.xMax - pad - 120 * _scale, safe.y + pad, 120 * _scale, 56 * _scale);
                if (GUI.Button(pr, s.State == GameState.Paused ? "Weiter" : "Pause", _button)) _root.TogglePause();
            }

            Rect center = new Rect(0, Screen.height * 0.28f, Screen.width, 70 * _scale);
            Rect sub = new Rect(0, center.yMax + 8 * _scale, Screen.width, 40 * _scale);
            Rect btn = new Rect(Screen.width * 0.5f - 130 * _scale, sub.yMax + 24 * _scale, 260 * _scale, 64 * _scale);

            switch (s.State)
            {
                case GameState.Ready:
                    GUI.Label(center, ProductInfo.DisplayName, _big);
                    GUI.Label(sub, "Wischen oder Pfeiltasten: ← → springen ↑ rutschen ↓", _mid);
                    if (GUI.Button(btn, "Spielen", _button)) _root.StartRun();
                    break;
                case GameState.Paused:
                    GUI.Label(center, "Pause", _big);
                    if (GUI.Button(btn, "Weiter", _button)) _root.TogglePause();
                    break;
                case GameState.GameOver:
                    GUI.Label(center, s.Score.IsNewHighScore ? "Neuer Bestwert!" : "Game Over", _big);
                    GUI.Label(sub, $"{(int)s.Score.Distance} m  ·  {s.Score.Coins} Münzen  ·  {s.Score.Score} Punkte", _mid);
                    if (GUI.Button(btn, "Nochmal", _button)) _root.Restart();
                    break;
            }
        }
    }
}
