using System.Collections.Generic;
using UnityEngine;
using WorldRush.Core;

namespace WorldRush.Runtime
{
    /// <summary>
    /// Prototyp-Anzeige mit Unity IMGUI: Punkte, Münzen, Combo, Power-up-Balken, Einblendungen,
    /// Startmenü mit Outfit-Eingabe, Pause, Game Over. In M3 folgt das finale Menü (UI Toolkit + eigene Schrift).
    /// </summary>
    public sealed class HudOverlay : MonoBehaviour
    {
        private GameRoot _root;
        private GUIStyle _big, _mid, _small, _button, _panel, _field, _chip;
        private Texture2D _panelTex, _buttonTex, _barTex;
        private float _scale = 1f;
        private string _outfitText;
        private int _tab; // 0 = Start, 1 = Aufgaben, 2 = Shop
        private string _shopMessage = "";
        private List<string> _outfitFeedback = new List<string>();
        private readonly List<(string text, Color color, float time)> _popups = new List<(string, Color, float)>();

        private static readonly PowerUpKind[] Kinds = { PowerUpKind.Board, PowerUpKind.Magnet, PowerUpKind.Shield, PowerUpKind.Double };

        public void Init(GameRoot root)
        {
            _root = root;
            _outfitText = root.outfit;
        }

        public void Popup(string text, Color color) => _popups.Add((text, color, Time.unscaledTime));

        private static Texture2D Solid(Color c)
        {
            var t = new Texture2D(1, 1);
            t.SetPixel(0, 0, c);
            t.Apply();
            return t;
        }

        private void EnsureStyles()
        {
            _scale = Mathf.Clamp(Mathf.Min(Screen.width / 1280f, Screen.height / 720f), 0.55f, 3f);
            if (_big != null) return;
            _panelTex = Solid(new Color(0.1f, 0.05f, 0.23f, 0.82f));
            _buttonTex = Solid(new Color(1f, 0.82f, 0.25f, 1f));
            _barTex = Solid(Color.white);
            _big = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            _mid = new GUIStyle(_big) { fontStyle = FontStyle.Normal, wordWrap = true };
            _small = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            _chip = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _button = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold };
            _button.normal.background = _buttonTex;
            _button.hover.background = _buttonTex;
            _button.active.background = _buttonTex;
            _button.normal.textColor = _button.hover.textColor = _button.active.textColor = new Color(0.1f, 0.05f, 0.23f);
            _panel = new GUIStyle(GUI.skin.box);
            _panel.normal.background = _panelTex;
            _field = new GUIStyle(GUI.skin.textField);
            foreach (var s in new[] { _big, _mid, _small, _chip }) s.normal.textColor = Color.white;
        }

        private void OnGUI()
        {
            if (_root == null || _root.Session == null) return;
            EnsureStyles();
            float k = _scale;
            _big.fontSize = (int)(56 * k);
            _mid.fontSize = (int)(22 * k);
            _small.fontSize = (int)(22 * k);
            _chip.fontSize = (int)(16 * k);
            _button.fontSize = (int)(26 * k);
            _field.fontSize = (int)(20 * k);

            var s = _root.Session;
            Rect safe = Screen.safeArea;
            safe.y = Screen.height - safe.yMax;
            float pad = 16f * k;

            // Punkte oben links
            GUI.Label(new Rect(safe.x + pad, safe.y + pad, 400 * k, 60 * k), s.Score.Score.ToString(), new GUIStyle(_small) { fontSize = (int)(46 * k) });
            GUI.Label(new Rect(safe.x + pad, safe.y + pad + 52 * k, 500 * k, 30 * k), $"Münzen {s.Score.Coins}    Best {s.Score.HighScore}", _small);
            if (s.ComboMultiplier > 1)
            {
                var prev = GUI.color;
                GUI.color = GameRoot.Sun;
                GUI.Label(new Rect(safe.x + pad, safe.y + pad + 84 * k, 300 * k, 30 * k), $"Combo ×{s.ComboMultiplier}", _small);
                GUI.color = prev;
            }

            GUI.Label(new Rect(safe.x + pad, Screen.height - 28 * k, 300 * k, 24 * k), "v" + ProductInfo.Version, new GUIStyle(_chip) { alignment = TextAnchor.MiddleLeft });

            // Oben rechts: Ton + Pause
            var soundRect = new Rect(safe.xMax - pad - 260 * k, safe.y + pad, 120 * k, 52 * k);
            if (GUI.Button(soundRect, _root.SoundOn ? "Ton an" : "Ton aus", _button)) _root.SoundOn = !_root.SoundOn;
            if (s.State == GameState.Running || s.State == GameState.Paused)
            {
                var pr = new Rect(safe.xMax - pad - 130 * k, safe.y + pad, 130 * k, 52 * k);
                if (GUI.Button(pr, s.State == GameState.Paused ? "Weiter" : "Pause", _button)) _root.TogglePause();
            }

            // Fortschritt bis zum Ziel
            if (s.State == GameState.Running && s.Level != null)
            {
                float bw = Mathf.Min(420 * k, Screen.width * 0.5f), bx = (Screen.width - bw) * 0.5f, by = safe.y + pad + 56 * k;
                var prevColor = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, 0.2f);
                GUI.DrawTexture(new Rect(bx, by, bw, 8 * k), _barTex);
                GUI.color = GameRoot.NeonCyan;
                GUI.DrawTexture(new Rect(bx, by, bw * s.LevelProgress, 8 * k), _barTex);
                GUI.color = prevColor;
                GUI.Label(new Rect(bx + bw + 6 * k, by - 10 * k, 60 * k, 28 * k), "Ziel", _chip);
                if (s.IsInWater) GUI.Label(new Rect(0, by + 14 * k, Screen.width, 30 * k), "Surfen! In der Luft nochmal springen = Salto", _chip);
            }

            // Power-up-Balken oben in der Mitte
            if (s.State == GameState.Running)
            {
                float w = 120 * k, x0 = Screen.width * 0.5f;
                var active = new List<PowerUpKind>();
                foreach (var kind in Kinds) if (s.IsPowerActive(kind)) active.Add(kind);
                float x = x0 - active.Count * (w + 8 * k) * 0.5f;
                foreach (var kind in active)
                {
                    var r = new Rect(x, safe.y + pad, w, 48 * k);
                    GUI.Box(r, GUIContent.none, _panel);
                    var prev = GUI.color;
                    GUI.color = GameRoot.PowerColor(kind);
                    GUI.Label(new Rect(r.x, r.y + 2 * k, r.width, 26 * k), GameRoot.PowerName(kind), _chip);
                    float frac = s.PowerTimeLeft(kind) / GameSession.DurationOf(s.Config, kind);
                    GUI.DrawTexture(new Rect(r.x + 10 * k, r.yMax - 12 * k, (r.width - 20 * k) * frac, 5 * k), _barTex);
                    GUI.color = prev;
                    x += w + 8 * k;
                }
            }

            // Einblendungen ("Surfboard!", "Combo ×2!")
            float now = Time.unscaledTime;
            _popups.RemoveAll(p => now - p.time > 1f);
            for (int i = 0; i < _popups.Count; i++)
            {
                var p = _popups[i];
                float t = (now - p.time) / 1f;
                var prev = GUI.color;
                GUI.color = new Color(p.color.r, p.color.g, p.color.b, 1f - t);
                GUI.Label(new Rect(0, Screen.height * 0.5f - t * 90 * k - i * 40 * k, Screen.width, 60 * k), p.text, new GUIStyle(_big) { fontSize = (int)(36 * k) });
                GUI.color = prev;
            }

            // Menüs
            float cw = Mathf.Min(Screen.width - 2 * pad, 560 * k);
            var card = new Rect((Screen.width - cw) * 0.5f, 0, cw, 0);
            if (s.State == GameState.Ready && _tab != 0)
            {
                card.y = Mathf.Max(80 * k, safe.y + 70 * k);
                if (_tab == 1) DrawMissions(card, k); else DrawShop(card, k);
                float panelHeight = (_tab == 1 ? 400 : 140 + BoardShop.Items.Length * 64) * k;
                DrawTabs(card.x, Mathf.Min(Screen.height - 60 * k, card.y + panelHeight + 12 * k), card.width, k);
                return;
            }

            switch (s.State)
            {
                case GameState.Ready:
                    card.height = 540 * k;
                    card.y = Mathf.Max(safe.y + 80 * k, (Screen.height - card.height) * 0.5f);
                    GUI.Box(card, GUIContent.none, _panel);
                    var titleStyle = new GUIStyle(_big);
                    titleStyle.normal.textColor = GameRoot.NeonCyan;
                    GUI.Label(new Rect(card.x, card.y + 16 * k, card.width, 70 * k), ProductInfo.DisplayName, titleStyle);
                    GUI.Label(new Rect(card.x + 20 * k, card.y + 90 * k, card.width - 40 * k, 60 * k),
                        string.IsNullOrEmpty(_root.LastRunText) ? "Pfeiltasten oder Wischen: ← → Spur · ↑ Springen · ↓ Rutschen" : _root.LastRunText, _mid);
                    GUI.Label(new Rect(card.x + 20 * k, card.y + 160 * k, card.width - 40 * k, 30 * k), "Dein Outfit, in eigenen Worten:", _small);
                    _outfitText = GUI.TextField(new Rect(card.x + 20 * k, card.y + 194 * k, card.width - 190 * k, 44 * k), _outfitText ?? "", 200, _field);
                    if (GUI.Button(new Rect(card.xMax - 160 * k, card.y + 194 * k, 140 * k, 44 * k), "Anziehen", _button))
                        _outfitFeedback = _root.ApplyOutfit(_outfitText);
                    GUI.Label(new Rect(card.x + 20 * k, card.y + 246 * k, card.width - 40 * k, 60 * k), string.Join("  ·  ", _outfitFeedback), new GUIStyle(_mid) { fontSize = (int)(16 * k), alignment = TextAnchor.UpperLeft });
                    {
                        float ly = card.y + 356 * k;
                        if (GUI.Button(new Rect(card.x + 20 * k, ly, 60 * k, 44 * k), "<", _button)) _root.ChangeLevel(-1);
                        int st = GameRoot.LevelStars(_root.LevelIndex);
                        GUI.Label(new Rect(card.x + 90 * k, ly, card.width - 180 * k, 44 * k), _root.LevelLabel + "   " + new string('*', st) + new string('-', 3 - st), new GUIStyle(_mid) { fontSize = (int)(20 * k) });
                        if (_root.CanGoNextLevel && GUI.Button(new Rect(card.xMax - 80 * k, ly, 60 * k, 44 * k), ">", _button)) _root.ChangeLevel(1);
                    }
                    if (_root.CharacterCount > 1)
                    {
                        float ry = card.y + 300 * k;
                        if (GUI.Button(new Rect(card.x + 20 * k, ry, 60 * k, 44 * k), "<", _button)) _outfitFeedback = _root.NextCharacter(-1) ?? _outfitFeedback;
                        GUI.Label(new Rect(card.x + 90 * k, ry, card.width - 180 * k, 44 * k), "Figur: " + _root.CharacterName, new GUIStyle(_mid) { fontSize = (int)(22 * k) });
                        if (GUI.Button(new Rect(card.xMax - 80 * k, ry, 60 * k, 44 * k), ">", _button)) _outfitFeedback = _root.NextCharacter(1) ?? _outfitFeedback;
                    }
                    DrawTabs(card.x, card.yMax + 12 * k, card.width, k);
                    if (GUI.Button(new Rect(card.x + card.width * 0.5f - 270 * k, card.yMax - 90 * k, 250 * k, 66 * k), "Spielen", _button)) _root.StartRun();
                    if (GUI.Button(new Rect(card.x + card.width * 0.5f + 20 * k, card.yMax - 90 * k, 250 * k, 66 * k), "Surfen ansehen", _button)) _root.StartAtWater();
                    break;
                case GameState.Paused:
                    card.height = 220 * k;
                    card.y = Mathf.Max(safe.y + 80 * k, (Screen.height - card.height) * 0.5f);
                    GUI.Box(card, GUIContent.none, _panel);
                    GUI.Label(new Rect(card.x, card.y + 20 * k, card.width, 70 * k), "Pause", _big);
                    if (GUI.Button(new Rect(card.x + (card.width - 240 * k) * 0.5f, card.yMax - 90 * k, 240 * k, 64 * k), "Weiter", _button)) _root.TogglePause();
                    break;
                case GameState.Won:
                    card.height = 320 * k;
                    card.y = Mathf.Max(safe.y + 80 * k, (Screen.height - card.height) * 0.5f);
                    GUI.Box(card, GUIContent.none, _panel);
                    int stars = s.Stars;
                    GUI.Label(new Rect(card.x, card.y + 16 * k, card.width, 64 * k), stars == 3 ? "Perfekt!" : "Geschafft!", _big);
                    var starStyle = new GUIStyle(_big) { fontSize = (int)(48 * k) };
                    starStyle.normal.textColor = GameRoot.Sun;
                    GUI.Label(new Rect(card.x, card.y + 82 * k, card.width, 60 * k), new string('*', stars) + new string('-', 3 - stars), starStyle);
                    GUI.Label(new Rect(card.x, card.y + 150 * k, card.width, 40 * k), $"{s.Score.Coins}/{s.LevelCoinsTotal} Münzen  ·  {s.Tricks} Saltos  ·  {s.Score.Score} Punkte", _mid);
                    float bw3 = (card.width - 60 * k) / 3f;
                    if (GUI.Button(new Rect(card.x + 20 * k, card.yMax - 84 * k, bw3, 64 * k), "Nächstes Level", _button)) _root.NextLevel();
                    if (GUI.Button(new Rect(card.x + 30 * k + bw3, card.yMax - 84 * k, bw3, 64 * k), "Nochmal", _button)) _root.Restart();
                    if (GUI.Button(new Rect(card.x + 40 * k + 2 * bw3, card.yMax - 84 * k, bw3, 64 * k), "Menü", _button)) _root.GoToMenu();
                    break;
                case GameState.GameOver:
                    card.height = 280 * k;
                    card.y = Mathf.Max(safe.y + 80 * k, (Screen.height - card.height) * 0.5f);
                    GUI.Box(card, GUIContent.none, _panel);
                    GUI.Label(new Rect(card.x, card.y + 20 * k, card.width, 70 * k), s.Score.IsNewHighScore ? "Neuer Bestwert!" : "Autsch!", _big);
                    GUI.Label(new Rect(card.x, card.y + 100 * k, card.width, 40 * k), $"{(int)s.Score.Distance} m  ·  {s.Score.Coins} Münzen  ·  {s.Score.Score} Punkte", _mid);
                    if (GUI.Button(new Rect(card.x + (card.width - 240 * k) * 0.5f, card.yMax - 90 * k, 240 * k, 64 * k), "Nochmal", _button)) _root.Restart();
                    break;
            }
        }

        private void DrawTabs(float x, float y, float width, float k)
        {
            string[] names = { "Start", "Aufgaben", "Shop" };
            float w = (width - 20 * k) / 3f;
            for (int i = 0; i < 3; i++)
            {
                var r = new Rect(x + i * (w + 10 * k), y, w, 48 * k);
                var prev = GUI.color;
                if (i == _tab) GUI.color = GameRoot.NeonCyan;
                if (GUI.Button(r, names[i], _button)) { _tab = i; _shopMessage = ""; }
                GUI.color = prev;
            }
        }

        private void DrawMissions(Rect card, float k)
        {
            card.height = 400 * k;
            GUI.Box(card, GUIContent.none, _panel);
            GUI.Label(new Rect(card.x, card.y + 14 * k, card.width, 50 * k), "Aufgaben für heute", new GUIStyle(_big) { fontSize = (int)(34 * k) });
            GUI.Label(new Rect(card.x, card.y + 64 * k, card.width, 30 * k), $"Dein Konto: {_root.Wallet} Münzen", _mid);
            float y = card.y + 110 * k;
            foreach (var m in _root.Missions)
            {
                GUI.Label(new Rect(card.x + 24 * k, y, card.width - 48 * k, 30 * k), m.Title, _small);
                float frac = Mathf.Clamp01(m.Progress / (float)m.Target);
                var bar = new Rect(card.x + 24 * k, y + 36 * k, card.width - 220 * k, 12 * k);
                var prev = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, 0.2f);
                GUI.DrawTexture(bar, _barTex);
                GUI.color = m.Completed ? new Color(0.45f, 1f, 0.6f) : GameRoot.Sun;
                GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * frac, bar.height), _barTex);
                GUI.color = prev;
                string state = m.Completed ? "Geschafft!" : $"{m.Progress}/{m.Target}  ·  +{m.Reward}";
                GUI.Label(new Rect(bar.xMax + 12 * k, y + 26 * k, 180 * k, 30 * k), state, _chip);
                y += 90 * k;
            }
        }

        private void DrawShop(Rect card, float k)
        {
            card.height = (140 + BoardShop.Items.Length * 64) * k;
            GUI.Box(card, GUIContent.none, _panel);
            GUI.Label(new Rect(card.x, card.y + 14 * k, card.width, 50 * k), "Surfbrett-Shop", new GUIStyle(_big) { fontSize = (int)(34 * k) });
            GUI.Label(new Rect(card.x, card.y + 64 * k, card.width, 30 * k), $"Dein Konto: {_root.Wallet} Münzen   {_shopMessage}", _mid);
            float y = card.y + 110 * k;
            foreach (var item in BoardShop.Items)
            {
                var prev = GUI.color;
                GUI.color = CharacterView.Hex(item.ColorHex);
                GUI.DrawTexture(new Rect(card.x + 24 * k, y + 8 * k, 70 * k, 26 * k), _barTex);
                GUI.color = prev;
                GUI.Label(new Rect(card.x + 110 * k, y + 4 * k, card.width - 330 * k, 34 * k), item.Name, _small);
                var br = new Rect(card.xMax - 210 * k, y, 186 * k, 48 * k);
                bool owned = _root.OwnsBoard(item.Id);
                if (owned && _root.SelectedBoard == item.Id) GUI.Label(br, "Ausgewählt", _chip);
                else if (owned) { if (GUI.Button(br, "Wählen", _button)) _root.SelectBoard(item.Id); }
                else if (GUI.Button(br, item.Price + " Münzen", _button))
                {
                    var result = _root.BuyBoard(item.Id);
                    _shopMessage = result == BoardShop.BuyResult.Bought ? "Gekauft!" : result == BoardShop.BuyResult.NotEnoughCoins ? "Zu wenig Münzen" : "";
                }
                y += 64 * k;
            }
        }

        private void OnDestroy()
        {
            if (_panelTex != null) Destroy(_panelTex);
            if (_buttonTex != null) Destroy(_buttonTex);
            if (_barTex != null) Destroy(_barTex);
        }
    }
}
