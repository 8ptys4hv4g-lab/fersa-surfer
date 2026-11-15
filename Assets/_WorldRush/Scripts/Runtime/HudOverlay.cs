using System.Collections.Generic;
using UnityEngine;
using WorldRush.Core;
using WorldRush.Core.Outfits;

namespace WorldRush.Runtime
{
    /// <summary>
    /// Bildschirm-Anzeige (Unity IMGUI): Punkte, Combo, Power-ups, Fortschritt, Tutorial-Hinweise, Einblendungen
    /// und das Startmenü mit den Tabs Start / Aufgaben / Shop / Optionen. Alle Texte über <see cref="L"/> (DE/EN).
    /// </summary>
    public sealed class HudOverlay : MonoBehaviour
    {
        private GameRoot _root;
        private GUIStyle _big, _mid, _small, _button, _panel, _field, _chip, _banner, _levelStyle;
        private Texture2D _panelTex, _buttonTex, _barTex, _bannerTex, _starTex, _circleTex;
        private float _scale = 1f;
        private string _outfitText;
        private int _tab;
        private const int TabStart = 0, TabLook = 1, TabMissions = 2, TabShop = 3, TabWorld = 4, TabSettings = 5;
        private OutfitSpec _look;
        private int _diceCount;
        private readonly WorldEditorPanel _worldPanel = new WorldEditorPanel();
        private bool _showCredits;
        private bool _settingsDirty;
        private string _confirm = "";
        private string _settingsMessage = "";
        private string _shopMessage = "";
        private List<string> _outfitFeedback = new List<string>();
        private readonly List<(string text, Color color, float time)> _popups = new List<(string, Color, float)>();

        private static readonly PowerUpKind[] Kinds = { PowerUpKind.Board, PowerUpKind.Magnet, PowerUpKind.Shield, PowerUpKind.Double };
        private static readonly string[] TabKeys = { "tab.start", "tab.look", "tab.missions", "tab.shop", "tab.world", "tab.settings" };

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

        /// <summary>Knopf, dessen Schrift kleiner wird, wenn der Text sonst abgeschnitten wäre.</summary>
        private bool Btn(Rect r, string text) => UiFit.Button(r, text, _button);

        /// <summary>Weißer 5-zackiger Stern mit weichen Kanten (wird beim Zeichnen eingefärbt).</summary>
        private static Texture2D StarTexture(int size)
        {
            var pts = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float a = (90f + i * 36f) * Mathf.Deg2Rad;
                float rad = i % 2 == 0 ? 0.48f : 0.2f;
                pts[i] = new Vector2(0.5f + Mathf.Cos(a) * rad, 0.47f + Mathf.Sin(a) * rad);
            }
            var px = new Color32[size * size];
            const int ss = 4;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    int hit = 0;
                    for (int sy = 0; sy < ss; sy++)
                        for (int sx = 0; sx < ss; sx++)
                        {
                            float u = (x + (sx + 0.5f) / ss) / size, v = (y + (sy + 0.5f) / ss) / size;
                            bool inside = false;
                            for (int i = 0, j = pts.Length - 1; i < pts.Length; j = i++)
                                if ((pts[i].y > v) != (pts[j].y > v) && u < (pts[j].x - pts[i].x) * (v - pts[i].y) / (pts[j].y - pts[i].y) + pts[i].x)
                                    inside = !inside;
                            if (inside) hit++;
                        }
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(255 * hit / (ss * ss)));
                }
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            t.SetPixels32(px);
            t.Apply();
            return t;
        }

        private void DrawStars(float centerX, float top, int stars, float k)
        {
            var prev = GUI.color;
            float[] scale = { 0.82f, 1.1f, 0.82f };
            float baseSize = 50f * k, gap = 10f * k;
            float total = baseSize * (scale[0] + scale[1] + scale[2]) + gap * 2f;
            float x = centerX - total * 0.5f;
            for (int i = 0; i < 3; i++)
            {
                float sz = baseSize * scale[i];
                float y = top + (baseSize * 1.1f - sz) * (i == 1 ? 0f : 0.6f);
                GUI.color = i < stars ? GameRoot.Sun : new Color(1f, 1f, 1f, 0.22f);
                GUI.DrawTexture(new Rect(x, y, sz, sz), _starTex);
                x += sz + gap;
            }
            GUI.color = prev;
        }

        private void EnsureStyles()
        {
            _scale = Mathf.Clamp(Mathf.Min(Screen.width / 1280f, Screen.height / 720f), 0.55f, 3f);
            if (_big != null) return;
            _panelTex = Solid(new Color(0.1f, 0.05f, 0.23f, 0.86f));
            _buttonTex = Solid(new Color(1f, 0.82f, 0.25f, 1f));
            _barTex = Solid(Color.white);
            _starTex = StarTexture(128);
            _circleTex = CircleTexture(64);
            _bannerTex = Solid(new Color(0.24f, 0.91f, 1f, 0.92f));
            _big = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            _mid = new GUIStyle(_big) { fontStyle = FontStyle.Normal, wordWrap = true };
            _small = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            _chip = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _button = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold };
            _button.normal.background = _button.hover.background = _button.active.background = _buttonTex;
            _button.normal.textColor = _button.hover.textColor = _button.active.textColor = new Color(0.1f, 0.05f, 0.23f);
            _panel = new GUIStyle(GUI.skin.box);
            _panel.normal.background = _panelTex;
            _field = new GUIStyle(GUI.skin.textField);
            _banner = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, wordWrap = true };
            _banner.normal.background = _bannerTex;
            _banner.normal.textColor = new Color(0.08f, 0.05f, 0.2f);
            foreach (var st in new[] { _big, _mid, _small, _chip }) st.normal.textColor = Color.white;
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
            _button.fontSize = (int)(24 * k);
            _field.fontSize = (int)(20 * k);
            _banner.fontSize = (int)(26 * k);

            var s = _root.Session;
            Rect safe = Screen.safeArea;
            safe.y = Screen.height - safe.yMax;
            float pad = 16f * k;

            if (s.State != GameState.Ready) DrawScore(s, safe, pad, k); // im Menü würde sie die Menü-Karte überdecken
            GUI.Label(new Rect(safe.xMax - pad - 300 * k, Screen.height - 28 * k, 300 * k, 24 * k), "v" + ProductInfo.Version, UiFit.Style(_chip, 0, TextAnchor.MiddleRight));

            if (s.State == GameState.Running || s.State == GameState.Paused)
            {
                var pr = new Rect(safe.xMax - pad - 150 * k, safe.y + pad, 150 * k, 52 * k);
                if (Btn(pr, L.T(s.State == GameState.Paused ? "hud.resume" : "hud.pause"))) _root.TogglePause();
            }

            if (s.State == GameState.Running)
            {
                DrawProgress(s, safe, pad, k);
                DrawPowers(s, safe, pad, k);
                if (!string.IsNullOrEmpty(_root.TutorialHint))
                {
                    float w = Mathf.Min(Screen.width - 2 * pad, 760 * k);
                    GUI.Box(new Rect((Screen.width - w) * 0.5f, Screen.height * 0.62f, w, 64 * k), L.T(_root.TutorialHint), _banner);
                }
            }

            DrawPopups(k);

            float cw = Mathf.Min(Screen.width - 2 * pad, 600 * k);
            var card = new Rect((Screen.width - cw) * 0.5f, 0, cw, 0);
            switch (s.State)
            {
                case GameState.Ready: DrawMenu(card, safe, k); break;
                case GameState.Paused:
                    card.height = 220 * k;
                    Center(ref card, safe, k);
                    GUI.Box(card, GUIContent.none, _panel);
                    GUI.Label(new Rect(card.x, card.y + 20 * k, card.width, 70 * k), L.T("hud.pause"), _big);
                    if (Btn(new Rect(card.x + (card.width - 240 * k) * 0.5f, card.yMax - 90 * k, 240 * k, 64 * k), L.T("hud.resume"))) _root.TogglePause();
                    break;
                case GameState.Won: DrawWon(s, card, safe, k); break;
                case GameState.GameOver:
                    card.height = 280 * k;
                    Center(ref card, safe, k);
                    GUI.Box(card, GUIContent.none, _panel);
                    GUI.Label(new Rect(card.x, card.y + 20 * k, card.width, 70 * k), L.T(s.Score.IsNewHighScore ? "over.best" : "over.title"), _big);
                    GUI.Label(new Rect(card.x, card.y + 100 * k, card.width, 40 * k), L.T("over.stats", (int)s.Score.Distance, s.Score.Coins, s.Score.Score), _mid);
                    if (Btn(new Rect(card.x + (card.width - 240 * k) * 0.5f, card.yMax - 90 * k, 240 * k, 64 * k), L.T("over.retry"))) _root.Restart();
                    break;
            }
        }

        private static void Center(ref Rect card, Rect safe, float k) => card.y = Mathf.Max(safe.y + 80 * k, (Screen.height - card.height) * 0.5f);

        // ---------- Laufende Runde ----------

        private void DrawScore(GameSession s, Rect safe, float pad, float k)
        {
            GUI.Label(new Rect(safe.x + pad, safe.y + pad, 400 * k, 60 * k), s.Score.Score.ToString(), UiFit.Style(_small, (int)(46 * k)));
            GUI.Label(new Rect(safe.x + pad, safe.y + pad + 52 * k, 500 * k, 30 * k), L.T("hud.coinsBest", s.Score.Coins, s.Score.HighScore), _small);
            if (s.ComboMultiplier > 1)
            {
                var prev = GUI.color;
                GUI.color = GameRoot.Sun;
                GUI.Label(new Rect(safe.x + pad, safe.y + pad + 84 * k, 300 * k, 30 * k), L.T("hud.combo", s.ComboMultiplier), _small);
                GUI.color = prev;
            }
        }

        private void DrawProgress(GameSession s, Rect safe, float pad, float k)
        {
            if (s.Level == null) return;
            float bw = Mathf.Min(420 * k, Screen.width * 0.5f), bx = (Screen.width - bw) * 0.5f, by = safe.y + pad + 56 * k;
            var prev = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.2f);
            GUI.DrawTexture(new Rect(bx, by, bw, 8 * k), _barTex);
            GUI.color = GameRoot.NeonCyan;
            GUI.DrawTexture(new Rect(bx, by, bw * s.LevelProgress, 8 * k), _barTex);
            GUI.color = prev;
            GUI.Label(new Rect(bx + bw + 6 * k, by - 10 * k, 80 * k, 28 * k), L.T("hud.goal"), _chip);
            if (s.IsInWater && string.IsNullOrEmpty(_root.TutorialHint))
                GUI.Label(new Rect(0, by + 14 * k, Screen.width, 30 * k), L.T("hud.surfHint"), _chip);
        }

        private void DrawPowers(GameSession s, Rect safe, float pad, float k)
        {
            float w = 130 * k;
            var active = new List<PowerUpKind>();
            foreach (var kind in Kinds) if (s.IsPowerActive(kind)) active.Add(kind);
            float x = Screen.width * 0.5f - active.Count * (w + 8 * k) * 0.5f;
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

        private void DrawPopups(float k)
        {
            float now = Time.unscaledTime;
            _popups.RemoveAll(p => now - p.time > 1.1f);
            var style = UiFit.Style(_big, (int)(36 * k));
            for (int i = 0; i < _popups.Count; i++)
            {
                var p = _popups[i];
                float t = (now - p.time) / 1.1f;
                var prev = GUI.color;
                GUI.color = new Color(p.color.r, p.color.g, p.color.b, 1f - t);
                GUI.Label(new Rect(0, Screen.height * 0.45f - t * 90 * k - i * 42 * k, Screen.width, 60 * k), p.text, style);
                GUI.color = prev;
            }
        }

        private void DrawWon(GameSession s, Rect card, Rect safe, float k)
        {
            card.height = 320 * k;
            Center(ref card, safe, k);
            GUI.Box(card, GUIContent.none, _panel);
            int stars = s.Stars;
            GUI.Label(new Rect(card.x, card.y + 16 * k, card.width, 64 * k), L.T(stars == 3 ? "won.perfect" : "won.done"), _big);
            DrawStars(card.center.x, card.y + 82 * k, stars, k);
            GUI.Label(new Rect(card.x, card.y + 150 * k, card.width, 40 * k), L.T("won.stats", s.Score.Coins, s.LevelCoinsTotal, s.Tricks, s.Score.Score), _mid);
            float bw3 = (card.width - 60 * k) / 3f;
            if (!_root.IsCustomLevel && Btn(new Rect(card.x + 20 * k, card.yMax - 84 * k, bw3, 64 * k), L.T("won.next"))) _root.NextLevel();
            if (Btn(new Rect(card.x + 30 * k + bw3, card.yMax - 84 * k, bw3, 64 * k), L.T("over.retry"))) _root.Restart();
            if (Btn(new Rect(card.x + 40 * k + 2 * bw3, card.yMax - 84 * k, bw3, 64 * k), L.T("won.menu"))) _root.GoToMenu();
        }

        // ---------- Startmenü ----------

        private void DrawMenu(Rect card, Rect safe, float k)
        {
            float panelHeight = _tab switch
            {
                TabLook => 560 * k,
                TabMissions => 400 * k,
                TabShop => (160 + BoardShop.Items.Length * 64) * k,
                TabWorld => Mathf.Min(620 * k, Screen.height - 150 * k),
                TabSettings => (_showCredits ? 460 : 640) * k,
                _ => 470 * k
            };
            card.height = panelHeight;
            card.y = Mathf.Max(safe.y + 70 * k, (Screen.height - panelHeight - 70 * k) * 0.5f);
            if (Screen.width >= card.width * 2.1f) card.x = safe.x + 40 * k; // Figur bleibt rechts sichtbar
            switch (_tab)
            {
                case TabLook: DrawLook(card, k); break;
                case TabMissions: DrawMissions(card, k); break;
                case TabShop: DrawShop(card, k); break;
                case TabWorld: _worldPanel.Draw(_root, card, k, _big, _mid, _small, _chip, _button, _panel, _barTex); break;
                case TabSettings: if (_showCredits) DrawCredits(card, k); else DrawSettings(card, k); break;
                default: DrawStart(card, k); break;
            }
            DrawTabs(card.x, Mathf.Min(Screen.height - 58 * k, card.yMax + 10 * k), card.width, k);
        }

        private void OpenTab(int tab)
        {
            _tab = tab; _shopMessage = ""; _showCredits = false; _confirm = ""; _settingsMessage = "";
            if (tab == TabLook) _look = OutfitSpec.FromText(_root.Outfit);
        }

        private void DrawTabs(float x, float y, float width, float k)
        {
            float w = (width - 10 * k * (TabKeys.Length - 1)) / TabKeys.Length;
            for (int i = 0; i < TabKeys.Length; i++)
            {
                var r = new Rect(x + i * (w + 10 * k), y, w, 48 * k);
                var prev = GUI.color;
                if (i == _tab) GUI.color = GameRoot.NeonCyan;
                var tabStyle = UiFit.Style(_button, (int)Mathf.Min(20 * k, w / 6.2f), null, false, null, TextClipping.Overflow);
                if (GUI.Button(r, L.T(TabKeys[i]), tabStyle)) OpenTab(i);
                GUI.color = prev;
            }
        }

        private void DrawStart(Rect card, float k)
        {
            GUI.Box(card, GUIContent.none, _panel);
            // Logo-Schriftzug wie im App-Icon: "Fe" lila, "sara" pink, darunter "RUN & SURF" in Türkis.
            var titleStyle = UiFit.Style(_big, 0, null, true);
            titleStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(card.x, card.y + 10 * k, card.width, 64 * k), "<color=#c4b3ff>Fe</color><color=#ff86c0>sara</color>", titleStyle);
            if (!_root.CustomWorldActive)
            {
                var tagStyle = UiFit.Style(_chip, (int)(16 * k), TextAnchor.MiddleCenter);
                tagStyle.normal.textColor = new Color(0.34f, 0.89f, 0.96f);
                GUI.Label(new Rect(card.x, card.y + 66 * k, card.width, 24 * k), ProductInfo.Tagline.ToUpperInvariant().Replace(" ", "  "), tagStyle);
            }
            GUI.Label(new Rect(card.x + 20 * k, card.y + 90 * k, card.width - 40 * k, 60 * k),
                string.IsNullOrEmpty(_root.LastRunText) ? L.T("menu.controls") : _root.LastRunText, _mid);
            if (_root.CustomWorldActive)
            {
                var prev = GUI.color;
                GUI.color = GameRoot.Sun;
                GUI.Label(new Rect(card.x, card.y + 66 * k, card.width, 26 * k), L.T("menu.customWorld"), _chip);
                GUI.color = prev;
            }
            float lookW = Mathf.Min(300 * k, card.width - 40 * k);
            if (Btn(new Rect(card.x + (card.width - lookW) * 0.5f, card.y + 160 * k, lookW, 50 * k), L.T("menu.changeLook"))) OpenTab(TabLook);

            if (_root.CharacterCount > 1)
            {
                float ry = card.y + 226 * k;
                if (Btn(new Rect(card.x + 20 * k, ry, 60 * k, 44 * k), "<")) _outfitFeedback = _root.NextCharacter(-1) ?? _outfitFeedback;
                GUI.Label(new Rect(card.x + 90 * k, ry, card.width - 180 * k, 44 * k), L.T("menu.character", _root.CharacterName), UiFit.Style(_mid, (int)(22 * k)));
                if (Btn(new Rect(card.xMax - 80 * k, ry, 60 * k, 44 * k), ">")) _outfitFeedback = _root.NextCharacter(1) ?? _outfitFeedback;
            }

            float ly = card.y + 284 * k;
            if (_root.CanGoPrevLevel && Btn(new Rect(card.x + 20 * k, ly, 60 * k, 44 * k), "<")) _root.ChangeLevel(-1);
            int stars = GameRoot.LevelStars(_root.StarsIndex);
            UiFit.Label(new Rect(card.x + 90 * k, ly, card.width - 180 * k, 44 * k), _root.LevelLabel + "  ·  " + L.T("menu.stars", stars), _levelStyle ??= new GUIStyle(_mid) { wordWrap = false });
            if (_root.CanGoNextLevel && Btn(new Rect(card.xMax - 80 * k, ly, 60 * k, 44 * k), ">")) _root.ChangeLevel(1);

            float bw = (card.width - 60 * k) * 0.5f;
            if (Btn(new Rect(card.x + 20 * k, card.yMax - 90 * k, bw, 66 * k), L.T("menu.play"))) _root.StartRun();
            if (Btn(new Rect(card.x + 40 * k + bw, card.yMax - 90 * k, bw, 66 * k), L.T("menu.surfPreview"))) _root.StartAtWater();
        }

        // ---------- Look (Outfit zum Antippen) ----------

        private void DrawLook(Rect card, float k)
        {
            GUI.Box(card, GUIContent.none, _panel);
            _look ??= OutfitSpec.FromText(_root.Outfit);
            GUI.Label(new Rect(card.x, card.y + 10 * k, card.width, 50 * k), L.T("look.title"), UiFit.Style(_big, (int)(34 * k)));
            float x = card.x + 20 * k, inner = card.width - 40 * k;
            float y = card.y + 64 * k;

            // Kleid oder Oberteil + Hose
            float half = (inner - 10 * k) * 0.5f;
            if (StyleButton(new Rect(x, y, half, 40 * k), L.T("look.styleDress"), _look.WearsDress)) { _look.WearsDress = true; ApplyLook(); }
            if (StyleButton(new Rect(x + half + 10 * k, y, half, 40 * k), L.T("look.styleSeparates"), !_look.WearsDress)) { _look.WearsDress = false; ApplyLook(); }
            y += 52 * k;

            var slots = _look.WearsDress
                ? new[] { OutfitSlot.Hair, OutfitSlot.Dress, OutfitSlot.Shoes }
                : new[] { OutfitSlot.Hair, OutfitSlot.Top, OutfitSlot.Bottom, OutfitSlot.Shoes };
            float labelW = 120 * k;
            foreach (var slot in slots)
            {
                UiFit.Label(new Rect(x, y, labelW - 8 * k, 40 * k), L.T(SlotKey(slot)), _small);
                var colors = OutfitStudio.ColorsFor(slot);
                float avail = inner - labelW;
                float size = Mathf.Min(36 * k, avail / colors.Length - 6 * k);
                for (int i = 0; i < colors.Length; i++)
                {
                    var r = new Rect(x + labelW + i * (size + 6 * k), y + (40 * k - size) * 0.5f, size, size);
                    if (Swatch(r, colors[i], _look.Get(slot) == colors[i], k)) { _look.Set(slot, colors[i]); ApplyLook(); }
                }
                y += 46 * k;
            }
            if (_look.WearsDress) y += 46 * k; // gleiche Höhe in beiden Varianten
            y += 6 * k;

            // Würfeln + fertige Ideen
            float diceW = 150 * k;
            if (Btn(new Rect(x, y, diceW, 88 * k), L.T("look.dice")))
            {
                _diceCount++;
                _look = OutfitStudio.Roll(_diceCount * 7919 + (int)(Time.realtimeSinceStartup * 1000f));
                ApplyLook();
            }
            float px = x + diceW + 10 * k, pw = (inner - diceW - 10 * k - 2 * 8 * k) / 3f;
            for (int i = 0; i < OutfitStudio.Presets.Length; i++)
            {
                var preset = OutfitStudio.Presets[i];
                var r = new Rect(px + (i % 3) * (pw + 8 * k), y + (i / 3) * (44 * k + 4 * k), pw, 42 * k);
                if (Btn(r, L.T(preset.Key))) { _look = preset.Spec.Clone(); ApplyLook(); }
            }
            y += 100 * k;

            // Eigene Worte
            UiFit.Label(new Rect(x, y, inner, 28 * k), L.T("menu.outfit"), _small);
            y += 30 * k;
            _outfitText = GUI.TextField(new Rect(x, y, inner - 150 * k, 44 * k), _outfitText ?? "", 200, _field);
            if (Btn(new Rect(card.xMax - 160 * k, y, 140 * k, 44 * k), L.T("menu.wear")))
            {
                _outfitFeedback = _root.ApplyOutfit(_outfitText);
                _look = OutfitSpec.FromText(_outfitText, _look);
                _root.OutfitSparkle();
            }
            y += 50 * k;
            GUI.Label(new Rect(x, y, inner, card.yMax - y - 6 * k), string.Join("  ·  ", _outfitFeedback), UiFit.Style(_mid, (int)(16 * k), TextAnchor.UpperLeft));
        }

        private void ApplyLook()
        {
            _outfitText = _look.ToText(L.Current);
            _outfitFeedback = _root.ApplyOutfit(_outfitText);
            _root.OutfitSparkle();
        }

        private static string SlotKey(OutfitSlot slot) => slot switch
        {
            OutfitSlot.Hair => "look.hair",
            OutfitSlot.Top => "look.top",
            OutfitSlot.Bottom => "look.bottom",
            OutfitSlot.Dress => "look.dress",
            _ => "look.shoes"
        };

        private bool StyleButton(Rect r, string text, bool active)
        {
            var prev = GUI.color;
            if (active) GUI.color = GameRoot.NeonCyan;
            bool clicked = Btn(r, text);
            GUI.color = prev;
            return clicked && !active;
        }

        /// <summary>Runder Farbknopf; der gewählte hat einen weißen Ring.</summary>
        private bool Swatch(Rect r, string colorId, bool selected, float k)
        {
            var prev = GUI.color;
            // Heller Rand: weiß und dick für die gewählte Farbe, sonst dezent (damit auch Schwarz sichtbar ist)
            float ring = selected ? 4f * k : 1.5f * k;
            GUI.color = selected ? Color.white : new Color(1f, 1f, 1f, 0.35f);
            GUI.DrawTexture(new Rect(r.x - ring, r.y - ring, r.width + 2 * ring, r.height + 2 * ring), _circleTex);
            GUI.color = CharacterView.Palette.TryGetValue(colorId, out var c) ? c : Color.gray;
            GUI.DrawTexture(r, _circleTex);
            GUI.color = prev;
            return GUI.Button(r, GUIContent.none, GUIStyle.none);
        }

        private static Texture2D CircleTexture(int size)
        {
            var px = new Color32[size * size];
            float rad = size * 0.5f - 1f, c = size * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x + 0.5f - c) * (x + 0.5f - c) + (y + 0.5f - c) * (y + 0.5f - c));
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(255 * Mathf.Clamp01(rad - d + 0.5f)));
                }
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            t.SetPixels32(px);
            t.Apply();
            return t;
        }

        private void DrawMissions(Rect card, float k)
        {
            GUI.Box(card, GUIContent.none, _panel);
            GUI.Label(new Rect(card.x, card.y + 14 * k, card.width, 50 * k), L.T("missions.title"), UiFit.Style(_big, (int)(34 * k)));
            GUI.Label(new Rect(card.x, card.y + 64 * k, card.width, 30 * k), L.T("missions.wallet", _root.Wallet), _mid);
            float y = card.y + 110 * k;
            foreach (var m in _root.Missions)
            {
                UiFit.Label(new Rect(card.x + 24 * k, y, card.width - 48 * k, 30 * k), MissionTitle(m), _small);
                float frac = Mathf.Clamp01(m.Progress / (float)m.Target);
                var bar = new Rect(card.x + 24 * k, y + 36 * k, card.width - 230 * k, 12 * k);
                var prev = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, 0.2f);
                GUI.DrawTexture(bar, _barTex);
                GUI.color = m.Completed ? new Color(0.45f, 1f, 0.6f) : GameRoot.Sun;
                GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * frac, bar.height), _barTex);
                GUI.color = prev;
                string state = m.Completed ? L.T("missions.done") : $"{m.Progress}/{m.Target}  ·  +{m.Reward}";
                GUI.Label(new Rect(bar.xMax + 12 * k, y + 26 * k, 190 * k, 30 * k), state, _chip);
                y += 90 * k;
            }
        }

        /// <summary>Aufgabentitel in der aktuellen Sprache (Deutsch kommt aus dem Kern).</summary>
        private static string MissionTitle(Mission m)
        {
            if (L.Current == Language.German) return m.Title;
            return m.Kind switch
            {
                MissionKind.CollectCoins => $"Collect {m.Target} coins",
                MissionKind.DoTricks => $"Do {m.Target} flips while surfing",
                MissionKind.Slides => $"Slide under {m.Target} bars",
                MissionKind.Jumps => $"Jump {m.Target} times",
                MissionKind.Distance => $"Run {m.Target} m in total",
                MissionKind.FinishLevels => $"Finish {m.Target} level(s)",
                _ => $"Collect {m.Target} power-ups"
            };
        }

        private void DrawShop(Rect card, float k)
        {
            GUI.Box(card, GUIContent.none, _panel);
            GUI.Label(new Rect(card.x, card.y + 14 * k, card.width, 50 * k), L.T("shop.title"), UiFit.Style(_big, (int)(34 * k)));
            GUI.Label(new Rect(card.x, card.y + 64 * k, card.width, 30 * k), L.T("missions.wallet", _root.Wallet) + "   " + _shopMessage, _mid);
            float y = card.y + 110 * k;
            foreach (var item in BoardShop.Items)
            {
                var prev = GUI.color;
                GUI.color = CharacterView.Hex(item.ColorHex);
                GUI.DrawTexture(new Rect(card.x + 24 * k, y + 10 * k, 70 * k, 26 * k), _barTex);
                GUI.color = prev;
                UiFit.Label(new Rect(card.x + 110 * k, y + 6 * k, card.width - 340 * k, 34 * k), item.Name, _small);
                var br = new Rect(card.xMax - 216 * k, y, 192 * k, 48 * k);
                bool owned = _root.OwnsBoard(item.Id);
                if (owned && _root.SelectedBoard == item.Id) GUI.Label(br, L.T("shop.selected"), _chip);
                else if (owned) { if (Btn(br, L.T("shop.select"))) _root.SelectBoard(item.Id); }
                else if (Btn(br, L.T("shop.price", item.Price)))
                {
                    var result = _root.BuyBoard(item.Id);
                    _shopMessage = result == BoardShop.BuyResult.Bought ? L.T("shop.bought") : result == BoardShop.BuyResult.NotEnoughCoins ? L.T("shop.tooPoor") : "";
                }
                y += 64 * k;
            }
            GUI.Label(new Rect(card.x, card.yMax - 40 * k, card.width, 30 * k), L.T("shop.note"), _chip);
        }

        private void DrawSettings(Rect card, float k)
        {
            var st = _root.Settings;
            bool changed = false;
            GUI.Box(card, GUIContent.none, _panel);
            GUI.Label(new Rect(card.x, card.y + 14 * k, card.width, 50 * k), L.T("settings.title"), UiFit.Style(_big, (int)(34 * k)));
            float x = card.x + 24 * k, lw = 170 * k, sw = card.width - lw - 48 * k, y = card.y + 90 * k;

            GUI.Label(new Rect(x, y, lw, 34 * k), L.T("settings.music"), _small);
            float music = GUI.HorizontalSlider(new Rect(x + lw, y + 12 * k, sw, 30 * k), st.MusicVolume, 0f, 1f);
            if (!Mathf.Approximately(music, st.MusicVolume)) { st.MusicVolume = music; changed = true; }
            y += 60 * k;

            GUI.Label(new Rect(x, y, lw, 34 * k), L.T("settings.sfx"), _small);
            float sfx = GUI.HorizontalSlider(new Rect(x + lw, y + 12 * k, sw, 30 * k), st.SfxVolume, 0f, 1f);
            if (!Mathf.Approximately(sfx, st.SfxVolume)) { st.SfxVolume = sfx; changed = true; }
            y += 60 * k;

            GUI.Label(new Rect(x, y, lw, 34 * k), L.T("settings.quality"), _small);
            float qw = (sw - 20 * k) / 3f;
            for (int q = 0; q < 3; q++)
            {
                var prev = GUI.color;
                if (st.Quality == q) GUI.color = GameRoot.NeonCyan;
                if (Btn(new Rect(x + lw + q * (qw + 10 * k), y, qw, 44 * k), L.T("quality." + q)) && st.Quality != q) { st.Quality = q; changed = true; }
                GUI.color = prev;
            }
            y += 64 * k;

            GUI.Label(new Rect(x, y, lw, 34 * k), L.T("settings.language"), _small);
            for (int i = 0; i < 2; i++)
            {
                var lang = (Language)i;
                var prev = GUI.color;
                if (st.Language == lang) GUI.color = GameRoot.NeonCyan;
                if (Btn(new Rect(x + lw + i * (sw * 0.5f + 5 * k), y, sw * 0.5f - 5 * k, 44 * k), L.Raw("lang.name", lang)) && st.Language != lang) { st.Language = lang; changed = true; }
                GUI.color = prev;
            }
            y += 74 * k;

            float bw = (card.width - 68 * k) * 0.5f;
            if (Btn(new Rect(x, y, bw, 54 * k), L.T("settings.tutorial"))) _root.ReplayTutorial();
            if (Btn(new Rect(x + bw + 20 * k, y, bw, 54 * k), L.T("settings.credits"))) _showCredits = true;
            y += 70 * k;

            // Zurücksetzen – mit Bestätigung (zweimal tippen)
            if (Btn(new Rect(x, y, bw, 54 * k), _confirm == "world" ? L.T("settings.confirm") : L.T("settings.resetWorld")))
            {
                if (_confirm == "world") { _root.RestoreDefaultCity(); _confirm = ""; _settingsMessage = L.T("settings.resetDone"); }
                else { _confirm = "world"; _settingsMessage = ""; }
            }
            var prevColor = GUI.color;
            if (_confirm == "all") GUI.color = new Color(1f, 0.55f, 0.55f);
            if (Btn(new Rect(x + bw + 20 * k, y, bw, 54 * k), _confirm == "all" ? L.T("settings.confirm") : L.T("settings.resetAll")))
            {
                if (_confirm == "all") { _confirm = ""; _root.ResetEverything(); }
                else { _confirm = "all"; _settingsMessage = ""; }
            }
            GUI.color = prevColor;
            if (_settingsMessage.Length > 0) GUI.Label(new Rect(card.x, y + 60 * k, card.width, 30 * k), _settingsMessage, _chip);

            // Sofort anwenden; speichern erst, wenn kein Regler mehr gezogen wird (nicht bei jedem Pixel).
            if (changed) { _root.ApplySettings(); _settingsDirty = true; }
            if (_settingsDirty && GUIUtility.hotControl == 0) { _root.SaveSettings(); _settingsDirty = false; }
        }

        private void DrawCredits(Rect card, float k)
        {
            GUI.Box(card, GUIContent.none, _panel);
            GUI.Label(new Rect(card.x, card.y + 14 * k, card.width, 50 * k), L.T("settings.credits"), UiFit.Style(_big, (int)(34 * k)));
            GUI.Label(new Rect(card.x + 30 * k, card.y + 80 * k, card.width - 60 * k, card.height - 170 * k), L.T("credits.text"),
                UiFit.Style(_mid, (int)(19 * k), TextAnchor.UpperCenter));
            if (Btn(new Rect(card.x + (card.width - 220 * k) * 0.5f, card.yMax - 76 * k, 220 * k, 56 * k), L.T("settings.back"))) _showCredits = false;
        }

        private void OnDestroy()
        {
            foreach (var t in new[] { _panelTex, _buttonTex, _barTex, _bannerTex })
                if (t != null) Destroy(t);
        }
    }
}
