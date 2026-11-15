using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using WorldRush.Core;

namespace WorldRush.Runtime
{
    /// <summary>
    /// Baut die komplette Runner-Szene im Neon-Garden-City-Stil auf und zeigt den Zustand der
    /// reinen Spiellogik (<see cref="GameSession"/>) an: Stadt, Figur, Hindernisse, Münzen, Power-ups,
    /// Partikel, Leuchten (Bloom), Tag-Nacht-Wechsel, Sound und Musik.
    /// Ein einziges leeres GameObject mit diesem Skript genügt.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameRoot : MonoBehaviour
    {
        [Header("Optional: Materialien (sonst automatisch erzeugt)")]
        public Material groundMaterial;
        public Material laneLineMaterial;
        public Material playerMaterial;
        public Material hurdleMaterial;
        public Material overheadMaterial;
        public Material blockMaterial;
        public Material coinMaterial;
        public Material buildingMaterial;

        [Header("Start")]
        public int seed = 0;
        public bool autoStart = false;
        [TextArea] public string outfit = "Blonde Haare, rotes Kleid, braune Schuhe";

        private GameSession _session;
        private RunnerInput _input;
        private LocalSaveRepository _saveRepo;
        private SaveData _save;
        private Transform _player;
        private CharacterView _character;
        private Camera _camera;
        private Light _sun;
        private ParticleSystem _particles;
        private AudioSource _sfx, _music;
        private AudioClip _coinClip, _jumpClip, _slideClip, _powerClip, _crashClip;
        private Material _skyMaterial;
        private Transform _sunDisk;
        private Material _sunMat;
        private bool _kitBuildings;
        private float _shake;

        private readonly Dictionary<int, GameObject> _rowViews = new Dictionary<int, GameObject>();
        private readonly Dictionary<int, GameObject> _coinViews = new Dictionary<int, GameObject>();
        private readonly Dictionary<int, GameObject> _powerViews = new Dictionary<int, GameObject>();
        private readonly Stack<GameObject> _coinPool = new Stack<GameObject>();
        private readonly List<Transform> _groundTiles = new List<Transform>();
        private readonly List<Transform> _scenery = new List<Transform>();
        /// <summary>Straßen-Objekte (Autos, Deko), die im Wasser-Abschnitt nicht stehen dürfen.</summary>
        private readonly HashSet<Transform> _streetOnly = new HashSet<Transform>();
        private readonly HashSet<int> _seen = new HashSet<int>();
        private readonly List<int> _remove = new List<int>();
        private HudOverlay _hud;

        private const float TileLength = 20f;
        private const int TileCount = 10;
        private const float SceneryLoop = 240f;

        public GameSession Session => _session;
        public CharacterView Character => _character;

        public static readonly Color NeonCyan = new Color(0.24f, 0.91f, 1f);
        public static readonly Color NeonPink = new Color(1f, 0.31f, 0.64f);
        public static readonly Color Sun = new Color(1f, 0.82f, 0.25f);
        public static readonly Color Violet = new Color(0.61f, 0.55f, 1f);

        private void Awake()
        {
            Application.targetFrameRate = 60;
            CreateMaterialsIfMissing();

            _saveRepo = new LocalSaveRepository(Path.Combine(Application.persistentDataPath, ProductInfo.SaveFileName));
            _save = _saveRepo.Load();

            int startSeed = seed != 0 ? seed : System.Environment.TickCount;
            _levelIndex = Mathf.Clamp(PlayerPrefs.GetInt("fs-level", 0), 0, UnlockedLevels);
            _session = new GameSession(RunnerConfig.CreateDefault(), startSeed, _save.HighScore, LevelDefinition.Create(_levelIndex));
            _session.StateChanged += OnStateChanged;
            _session.CoinCollected += OnCoin;
            _session.PowerUpActivated += OnPowerUp;
            _session.ShieldUsed += OnShieldUsed;
            _session.ComboMilestone += m => _hud?.Popup("Combo ×" + m + "!", Sun);
            _session.Crashed += (row, kind) => OnCrash();
            _session.TrickPerformed += p =>
            {
                _character.Trick();
                _sfx.PlayOneShot(_powerClip, 0.7f);
                Burst(_player.position + Vector3.up * 1.4f, NeonCyan, 30);
                _hud?.Popup("Salto! +" + p, NeonCyan);
            };
            _input = new RunnerInput();

            BuildAtmosphere();
            BuildEnvironment();
            if (MaterialFactory.EmptySlotsFilled > 0)
                Debug.Log($"[FersaSurfer] {MaterialFactory.EmptySlotsFilled} leere Material-Plätze mit Cartoon-Farben gefüllt.");
            BuildPlayer();
            BuildCamera();
            BuildParticles();
            BuildAudio();
            BuildLevelView();

            _hud = gameObject.AddComponent<HudOverlay>();
            _hud.Init(this);
            ApplyOutfit(outfit);

            if (autoStart) _session.Start();
        }

        public void StartRun()
        {
            if (_session.State == GameState.Ready) _session.Start();
        }

        public void Restart()
        {
            ClearViews();
            _session.Restart();
        }

        public void TogglePause() => _session.TogglePause();

        public List<string> ApplyOutfit(string text)
        {
            outfit = text ?? "";
            return _character.ApplyOutfit(outfit);
        }

        private void Update()
        {
            var commands = _input.Poll();
            if (_input.PausePressed) _session.TogglePause();

            switch (_session.State)
            {
                case GameState.Ready:
                    // Nicht starten, während jemand ins Outfit-Feld tippt.
                    if (GUIUtility.keyboardControl == 0 && (_input.ConfirmPressed || commands.Count > 0)) _session.Start();
                    break;
                case GameState.GameOver:
                    if (_input.ConfirmPressed) Restart();
                    break;
                case GameState.Won:
                    if (_input.ConfirmPressed) NextLevel();
                    break;
                case GameState.Running:
                    foreach (var c in commands)
                    {
                        if (c == RunnerCommand.Jump && !_session.Motor.IsJumping && _session.Motor.FeetY <= 0f && !_session.Motor.IsHovering) _sfx.PlayOneShot(_jumpClip, 0.7f);
                        if (c == RunnerCommand.Slide && !_session.Motor.IsSliding && !_session.Motor.IsHovering) _sfx.PlayOneShot(_slideClip, 0.6f);
                        _session.Enqueue(c);
                    }
                    _session.Tick(Time.deltaTime);
                    break;
            }

            UpdateMusic();
            SyncViews();
            UpdateLevelView();
            UpdateSky();
        }

        private void LateUpdate()
        {
            if (_camera == null) return;
            var motor = _session.Motor;
            bool menu = _session.State == GameState.Ready;
            Vector3 target = menu
                ? new Vector3(2.6f, 2.3f, _session.PlayerZ + 4.6f)
                : new Vector3(motor.LateralX * 0.6f, 3.6f + motor.FeetY * 0.35f, _session.PlayerZ - 6.6f);
            var cam = _camera.transform;
            cam.position = Vector3.Lerp(cam.position, target, 1f - Mathf.Exp(-(menu ? 3f : 9f) * Time.deltaTime));
            if (_shake > 0f)
            {
                _shake -= Time.deltaTime;
                cam.position += Random.insideUnitSphere * _shake * 0.6f;
            }
            Vector3 look = menu
                ? new Vector3(0f, 1.3f, _session.PlayerZ)
                : new Vector3(motor.LateralX * 0.4f, 1.1f, _session.PlayerZ + 8f);
            cam.rotation = Quaternion.Slerp(cam.rotation, Quaternion.LookRotation(look - cam.position), 1f - Mathf.Exp(-10f * Time.deltaTime));
            float speedT = Mathf.InverseLerp(_session.Config.StartSpeed, _session.Config.MaxSpeed, _session.Speed);
            float fov = menu ? 50f : Mathf.Lerp(62f, 74f, speedT) + (motor.IsHovering ? 4f : 0f);
            _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, fov, Time.deltaTime * 3f);
            if (_sun != null) _sun.transform.position = _player.position;
        }

        // ---------- Ereignisse ----------

        private void OnStateChanged(GameState state)
        {
            if (state == GameState.Running && !_music.isPlaying) _music.Play();
            if (state == GameState.Won)
            {
                int stars = _session.Stars;
                if (stars > LevelStars(_levelIndex)) PlayerPrefs.SetInt("fs-stars-" + _levelIndex, stars);
                PlayerPrefs.Save();
                _music.Stop();
                _sfx.PlayOneShot(_powerClip, 1f);
                Burst(_player.position + Vector3.up * 2f, Sun, 120);
            }
            if (state != GameState.GameOver && state != GameState.Won) return;
            _save.RunsPlayed++;
            _save.TotalCoins += _session.Score.Coins;
            _save.HighScore = Mathf.Max(_save.HighScore, _session.Score.HighScore);
            try { _saveRepo.Save(_save); }
            catch (System.Exception e) { Debug.LogWarning("[FersaSurfer] Speichern fehlgeschlagen: " + e.Message); }
        }

        private void OnCoin(Coin coin, int points)
        {
            _sfx.pitch = 1f + Mathf.Min(_session.Combo, 24) * 0.02f;
            _sfx.PlayOneShot(_coinClip, 0.5f);
            _sfx.pitch = 1f;
            Burst(_player.position + Vector3.up * 1.2f, Sun, 6);
        }

        private void OnPowerUp(PowerUpKind kind)
        {
            _sfx.PlayOneShot(_powerClip, 0.8f);
            Burst(_player.position + Vector3.up * 1.4f, PowerColor(kind), 40);
            _hud?.Popup(PowerName(kind) + "!", PowerColor(kind));
        }

        private void OnShieldUsed(ObstacleRow row)
        {
            _sfx.PlayOneShot(_crashClip, 0.5f);
            _shake = 0.3f;
            Burst(new Vector3(_session.Motor.LateralX, 1.5f, row.Z), Violet, 60);
            if (_rowViews.TryGetValue(row.Id, out var view)) { Destroy(view); _rowViews.Remove(row.Id); }
            _hud?.Popup("Schild!", Violet);
        }

        private void OnCrash()
        {
            _sfx.PlayOneShot(_crashClip, 0.9f);
            _shake = 0.5f;
            Burst(_player.position + Vector3.up * 1.2f, NeonPink, 80);
            _music.Stop();
        }

        public static string PowerName(PowerUpKind k) =>
            k == PowerUpKind.Board ? "Surfboard" : k == PowerUpKind.Magnet ? "Magnet" : k == PowerUpKind.Shield ? "Schild" : "2× Punkte";

        public static Color PowerColor(PowerUpKind k) =>
            k == PowerUpKind.Board ? NeonCyan : k == PowerUpKind.Magnet ? new Color(1f, 0.35f, 0.43f) : k == PowerUpKind.Shield ? Violet : Sun;

        // ---------- Darstellung ----------

        private void SyncViews()
        {
            var cfg = _session.Config;
            var motor = _session.Motor;
            float lift = _session.IsInWater && !motor.IsHovering ? 0.5f + WaveHeight(_session.PlayerZ, motor.LateralX, Time.time) : 0f;
            _player.position = new Vector3(motor.LateralX, motor.FeetY + lift, _session.PlayerZ);
            _player.rotation = Quaternion.Euler(0f, 0f, (Lanes.ToX(motor.TargetLane, cfg.LaneWidth) - motor.LateralX) * -4f);
            bool running = _session.State == GameState.Running;
            _character.Animate(motor, running, motor.IsHovering || _session.IsInWater, _session.IsPowerActive(PowerUpKind.Shield), _session.Speed, Time.deltaTime);
            bool blink = running && _session.Invulnerable > 0f && _session.Invulnerable < 1f && !motor.IsHovering && Mathf.FloorToInt(Time.time * 11f) % 2 == 0;
            _character.Root.gameObject.SetActive(!blink);
            if (running && motor.IsHovering && Random.value < 0.6f) Burst(_player.position - Vector3.forward * 0.9f, NeonCyan, 1);

            // Hindernisreihen
            _seen.Clear();
            foreach (var row in _session.Rows)
            {
                _seen.Add(row.Id);
                if (!_rowViews.ContainsKey(row.Id) && !row.IsEmpty) _rowViews[row.Id] = CreateRowView(row, cfg);
            }
            RemoveUnseen(_rowViews, go => Destroy(go));

            // Münzen (gepoolt), mit Magnet-Sog
            bool magnet = _session.IsPowerActive(PowerUpKind.Magnet);
            _seen.Clear();
            foreach (var coin in _session.Coins)
            {
                _seen.Add(coin.Id);
                if (!_coinViews.TryGetValue(coin.Id, out var go))
                {
                    go = _coinPool.Count > 0 ? _coinPool.Pop() : CreateCoinView();
                    go.SetActive(true);
                    go.transform.position = new Vector3(Lanes.ToX(coin.Lane, cfg.LaneWidth), coin.Y, coin.Z);
                    _coinViews[coin.Id] = go;
                }
                if (magnet && coin.Z - _session.PlayerZ < 16f)
                    go.transform.position = Vector3.Lerp(go.transform.position, _player.position + Vector3.up, Time.deltaTime * 6f);
                go.transform.rotation = Quaternion.Euler(90f, Time.time * 200f + coin.Id * 7f, 0f);
            }
            RemoveUnseen(_coinViews, go => { go.SetActive(false); _coinPool.Push(go); });

            // Power-ups
            _seen.Clear();
            foreach (var p in _session.PowerUps)
            {
                _seen.Add(p.Id);
                if (!_powerViews.TryGetValue(p.Id, out var go)) _powerViews[p.Id] = go = CreatePowerView(p, cfg);
                go.transform.position = new Vector3(Lanes.ToX(p.Lane, cfg.LaneWidth), 1f + Mathf.Sin(Time.time * 3f + p.Id) * 0.15f, p.Z);
                go.transform.rotation = Quaternion.Euler(0f, Time.time * 140f, 0f);
            }
            RemoveUnseen(_powerViews, go => Destroy(go));

            // Boden und Kulisse wandern mit (Endlos-Effekt).
            float baseZ = Mathf.Floor((_session.PlayerZ - TileLength) / TileLength) * TileLength;
            for (int i = 0; i < _groundTiles.Count; i++)
                _groundTiles[i].position = new Vector3(0f, 0f, baseZ + i * TileLength + TileLength * 0.5f);
            foreach (var b in _scenery)
            {
                float z = b.position.z;
                if (z < _session.PlayerZ - 20f) z += SceneryLoop;
                else if (z > _session.PlayerZ + SceneryLoop) z -= SceneryLoop * Mathf.Ceil((z - _session.PlayerZ - 20f) / SceneryLoop);
                b.position = new Vector3(b.position.x, b.position.y, z);
                if (_streetOnly.Contains(b))
                {
                    var lvl = _session.Level;
                    bool inWater = lvl != null && (lvl.IsWater(z) || lvl.IsWater(z - 3f) || lvl.IsWater(z + 3f));
                    if (b.gameObject.activeSelf == inWater) b.gameObject.SetActive(!inWater);
                }
            }
        }

        private void RemoveUnseen(Dictionary<int, GameObject> views, System.Action<GameObject> release)
        {
            _remove.Clear();
            foreach (var kv in views) if (!_seen.Contains(kv.Key)) _remove.Add(kv.Key);
            foreach (var id in _remove) { release(views[id]); views.Remove(id); }
        }

        private void ClearViews()
        {
            foreach (var go in _rowViews.Values) Destroy(go);
            _rowViews.Clear();
            foreach (var go in _powerViews.Values) Destroy(go);
            _powerViews.Clear();
            foreach (var go in _coinViews.Values) { go.SetActive(false); _coinPool.Push(go); }
            _coinViews.Clear();
        }

        private GameObject CreateRowView(ObstacleRow row, RunnerConfig cfg)
        {
            var root = new GameObject("Row " + row.Id);
            root.transform.position = new Vector3(0f, 0f, row.Z);
            bool water = _session.Level != null && _session.Level.IsWater(row.Z);
            for (int lane = Lanes.Left; lane <= Lanes.Right; lane++)
            {
                var kind = row.At(lane);
                if (kind == ObstacleKind.None) continue;
                float x = Lanes.ToX(lane, cfg.LaneWidth);
                if (water)
                {
                    if (kind == ObstacleKind.Block)
                    {
                        var rock = MakePrimitive(PrimitiveType.Sphere, "Rock", root.transform, _rockMat);
                        rock.transform.localPosition = new Vector3(x, 1.3f, 0f);
                        rock.transform.localScale = new Vector3(2.1f, 2.9f, 1.8f);
                        rock.transform.localRotation = Quaternion.Euler(10f * lane, 25f * lane, 8f);
                    }
                    else if (kind == ObstacleKind.Hurdle)
                    {
                        var wave = MakePrimitive(PrimitiveType.Capsule, "Wave", root.transform, _foamMat);
                        wave.transform.localPosition = new Vector3(x, 0.55f, 0f);
                        wave.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                        wave.transform.localScale = new Vector3(0.9f, 1.05f, 0.9f);
                    }
                    else
                    {
                        Box(root.transform, _woodMat, new Vector3(x, cfg.OverheadBottom + 0.2f, 0f), new Vector3(2.3f, 0.4f, 0.7f));
                        for (int s = -1; s <= 1; s += 2) Box(root.transform, _woodMat, new Vector3(x + s * 1.1f, (cfg.OverheadBottom + 0.4f) * 0.5f, 0f), new Vector3(0.25f, cfg.OverheadBottom + 0.4f, 0.25f));
                    }
                    continue;
                }
                CollisionRules.GetVerticalRange(cfg, kind, out float bottom, out float top);
                float w = cfg.ObstacleHalfWidth * 2f, d = cfg.ObstacleHalfDepth * 2f;
                if (kind == ObstacleKind.Hurdle)
                {
                    Box(root.transform, hurdleMaterial, new Vector3(x, top - 0.18f, 0f), new Vector3(w, 0.32f, 0.25f));
                    for (int i = -2; i <= 2; i++) Box(root.transform, laneLineMaterial, new Vector3(x + i * 0.4f, top - 0.18f, 0f), new Vector3(0.17f, 0.33f, 0.26f), _stripe);
                    for (int s = -1; s <= 1; s += 2) Box(root.transform, laneLineMaterial, new Vector3(x + s * 0.9f, (top - 0.3f) * 0.5f, 0f), new Vector3(0.12f, top - 0.3f, 0.12f));
                }
                else if (kind == ObstacleKind.Overhead)
                {
                    Box(root.transform, overheadMaterial, new Vector3(x, bottom + 0.2f, 0f), new Vector3(w + 0.1f, 0.35f, 0.4f));
                    Box(root.transform, overheadMaterial, new Vector3(x, bottom + 0.9f, 0f), new Vector3(1.6f, 0.9f, 0.1f));
                    for (int s = -1; s <= 1; s += 2) Box(root.transform, laneLineMaterial, new Vector3(x + s * 1.05f, (bottom + 1.4f) * 0.5f, 0f), new Vector3(0.14f, bottom + 1.4f, 0.14f));
                }
                else
                {
                    Box(root.transform, blockMaterial, new Vector3(x, (top - 0.3f) * 0.5f, 0f), new Vector3(w, top - 0.3f, d + 1.1f));
                    Box(root.transform, laneLineMaterial, new Vector3(x, top - 0.25f, 0f), new Vector3(w + 0.04f, 0.18f, d + 1.14f));
                }
            }
            return root;
        }

        private GameObject CreateCoinView()
        {
            var coin = MakePrimitive(PrimitiveType.Cylinder, "Coin", transform, coinMaterial);
            coin.transform.localScale = new Vector3(0.62f, 0.05f, 0.62f);
            return coin;
        }

        private GameObject CreatePowerView(PowerUpItem p, RunnerConfig cfg)
        {
            var root = new GameObject("PowerUp " + p.Kind);
            var col = PowerColor(p.Kind);
            var mat = MaterialFactory.Create(col, 0.8f, col * 2.2f);
            switch (p.Kind)
            {
                case PowerUpKind.Board: Box(root.transform, mat, Vector3.zero, new Vector3(0.5f, 0.08f, 1.3f)).transform.localRotation = Quaternion.Euler(30f, 0, 0); break;
                case PowerUpKind.Magnet:
                    Box(root.transform, mat, new Vector3(0, 0.15f, 0), new Vector3(0.7f, 0.18f, 0.2f));
                    for (int s = -1; s <= 1; s += 2) Box(root.transform, mat, new Vector3(s * 0.26f, -0.1f, 0), new Vector3(0.18f, 0.5f, 0.2f));
                    break;
                case PowerUpKind.Shield: MakePrimitive(PrimitiveType.Sphere, "Orb", root.transform, mat).transform.localScale = Vector3.one * 0.7f; break;
                default: Box(root.transform, mat, Vector3.zero, Vector3.one * 0.5f).transform.localRotation = Quaternion.Euler(45f, 0, 45f); break;
            }
            var ring = new GameObject("Halo");
            ring.transform.SetParent(root.transform, false);
            ring.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            ring.AddComponent<MeshFilter>().sharedMesh = MeshFactory.Ring(0.62f, 0.03f, 40);
            ring.AddComponent<MeshRenderer>().sharedMaterial = MaterialFactory.DoubleSided(MaterialFactory.Create(col, 0.9f, col * 3f));
            return root;
        }

        // ---------- Aufbau ----------

        private Material _stripe;

        private void BuildAtmosphere()
        {
            var lightGo = new GameObject("Sun");
            _sun = lightGo.AddComponent<Light>();
            _sun.type = LightType.Directional;
            _sun.intensity = 1.35f;
            _sun.color = new Color(1f, 0.84f, 0.68f);
            _sun.shadows = LightShadows.Soft;
            _sun.shadowStrength = 0.8f;
            lightGo.transform.rotation = Quaternion.Euler(48f, -35f, 0f);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 50f;
            RenderSettings.fogEndDistance = 230f;

            RenderSettings.skybox = null;
            _sunDisk = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
            _sunDisk.name = "SunDisk";
            Destroy(_sunDisk.GetComponent<Collider>());
            _sunDisk.SetParent(transform, false);
            _sunDisk.localScale = Vector3.one * 34f;
            _sunMat = MaterialFactory.Create(new Color(1f, 0.75f, 0.45f), 0f, new Color(1f, 0.6f, 0.35f) * 2.2f);
            _sunDisk.GetComponent<Renderer>().sharedMaterial = _sunMat;
            _sunDisk.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

            // Leuchten (Bloom) und kräftige Farben über ein globales URP-Volume
            if (GraphicsSettings.currentRenderPipeline != null)
            {
                var volumeGo = new GameObject("PostFX");
                volumeGo.transform.SetParent(transform, false);
                var volume = volumeGo.AddComponent<Volume>();
                volume.isGlobal = true;
                volume.priority = 10f;
                var profile = ScriptableObject.CreateInstance<VolumeProfile>();
                var bloom = profile.Add<Bloom>(true);
                bloom.intensity.Override(1.1f);
                bloom.threshold.Override(0.95f);
                bloom.scatter.Override(0.65f);
                var colors = profile.Add<ColorAdjustments>(true);
                colors.saturation.Override(18f);
                colors.contrast.Override(8f);
                var vignette = profile.Add<Vignette>(true);
                vignette.intensity.Override(0.22f);
                var tone = profile.Add<Tonemapping>(true);
                tone.mode.Override(TonemappingMode.Neutral);
                var dof = profile.Add<DepthOfField>(true);
                dof.mode.Override(DepthOfFieldMode.Gaussian);
                dof.gaussianStart.Override(45f);
                dof.gaussianEnd.Override(150f);
                dof.gaussianMaxRadius.Override(1.2f);
                volume.profile = profile;
            }

            // Bildqualität: Kantenglättung, HDR, weitere und schärfere Schatten
            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp)
            {
                urp.msaaSampleCount = 4;
                urp.supportsHDR = true;
                urp.shadowDistance = 70f;
                urp.shadowCascadeCount = 2;
            }
        }

        private static readonly Color[][] Skies =
        {
            new[] { new Color(0.75f, 0.35f, 0.85f), new Color(0.42f, 0.25f, 0.63f), new Color(0.55f, 0.6f, 1f) },  // Sonnenuntergang
            new[] { new Color(0.15f, 0.2f, 0.55f), new Color(0.11f, 0.12f, 0.3f), new Color(0.3f, 0.35f, 0.7f) },   // Nacht
            new[] { new Color(0.15f, 0.7f, 0.6f), new Color(0.11f, 0.26f, 0.32f), new Color(0.35f, 0.75f, 0.8f) },  // Polarlicht
            new[] { new Color(1f, 0.55f, 0.6f), new Color(0.56f, 0.31f, 0.56f), new Color(0.8f, 0.65f, 0.9f) },     // Morgen
        };

        private void UpdateSky()
        {
            float phase = (_session.Score.Distance / 550f) % Skies.Length;
            int i = (int)phase;
            float f = phase - i;
            float t = f < 0.7f ? 0f : Mathf.SmoothStep(0f, 1f, (f - 0.7f) / 0.3f);
            var a = Skies[i];
            var b = Skies[(i + 1) % Skies.Length];
            Color tint = Color.Lerp(a[0], b[0], t), fog = Color.Lerp(a[1], b[1], t), amb = Color.Lerp(a[2], b[2], t);
            if (_skyMaterial != null) _skyMaterial.SetColor("_SkyTint", tint);
            RenderSettings.fogColor = fog;
            RenderSettings.ambientSkyColor = amb;
            RenderSettings.ambientEquatorColor = Color.Lerp(amb, fog, 0.5f);
            RenderSettings.ambientGroundColor = fog * 0.6f;
            if (_camera != null && _skyMaterial == null) _camera.backgroundColor = Color.Lerp(fog, tint, 0.45f);
            if (_sunDisk != null)
            {
                _sunDisk.position = new Vector3(0f, 26f, _session.PlayerZ + 205f);
                _sunMat.SetColor("_EmissionColor", Color.Lerp(new Color(1f, 0.55f, 0.35f), tint, 0.35f) * 2.2f);
            }
        }

        private void BuildEnvironment()
        {
            var cfg = _session.Config;
            float trackWidth = cfg.LaneWidth * 3f + 1f;
            _stripe = MaterialFactory.Create(new Color(0.14f, 0.12f, 0.23f), 0.3f);
            var lawn = MaterialFactory.Create(new Color(0.24f, 0.17f, 0.48f), 0.1f);
            var edge = MaterialFactory.Create(NeonPink, 0.6f, NeonPink * 2.2f);
            var parking = MaterialFactory.Create(new Color(0.16f, 0.14f, 0.27f), 0.4f);
            var sidewalk = MaterialFactory.Create(new Color(0.62f, 0.6f, 0.7f), 0.25f);

            for (int i = 0; i < TileCount; i++)
            {
                var tile = new GameObject("GroundTile " + i).transform;
                tile.SetParent(transform);
                Box(tile, groundMaterial, new Vector3(0f, -0.05f, 0f), new Vector3(trackWidth, 0.1f, TileLength));
                Box(tile, lawn, new Vector3(0f, -0.08f, 0f), new Vector3(120f, 0.1f, TileLength));
                for (int line = 0; line < 2; line++)
                    for (int dash = 0; dash < 4; dash++)
                        Box(tile, laneLineMaterial, new Vector3((line - 0.5f) * cfg.LaneWidth, 0.01f, -TileLength * 0.5f + 2.5f + dash * 5f), new Vector3(0.1f, 0.02f, 2.8f));
                for (int s = -1; s <= 1; s += 2)
                {
                    Box(tile, edge, new Vector3(s * trackWidth * 0.5f, 0.02f, 0f), new Vector3(0.12f, 0.04f, TileLength));
                    Box(tile, parking, new Vector3(s * (trackWidth * 0.5f + 1.3f), -0.03f, 0f), new Vector3(2.6f, 0.1f, TileLength));
                    Box(tile, sidewalk, new Vector3(s * (trackWidth * 0.5f + 4.6f), 0.06f, 0f), new Vector3(4f, 0.24f, TileLength));
                }
                _groundTiles.Add(tile);
            }

            // Neon-Hochhäuser mit leuchtenden Fenstern
            var rnd = new DeterministicRandom(7);
            var palette = new[] { new Color(0.35f, 0.25f, 0.82f), new Color(0.48f, 0.31f, 0.88f), new Color(0.23f, 0.43f, 0.91f), new Color(0.75f, 0.31f, 0.85f), new Color(0.88f, 0.37f, 0.66f) };
            var windows = WindowTexture();
            for (int i = 0; i < 26; i++)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    float height = rnd.Range(8f, 34f), width = rnd.Range(5f, 10f), depth = rnd.Range(6f, 11f);
                    var mat = MaterialFactory.Create(palette[rnd.Range(0, palette.Length)], 0.55f, Color.white * 1.6f);
                    mat.SetTexture("_EmissionMap", windows);
                    mat.mainTextureScale = new Vector2(Mathf.Max(1f, Mathf.Round(width / 4f)), Mathf.Max(1f, Mathf.Round(height / 7f)));
                    var b = MakePrimitive(PrimitiveType.Cube, "Building", transform, mat);
                    b.transform.localScale = new Vector3(width, height, depth);
                    b.transform.position = new Vector3(side * (trackWidth * 0.5f + 7f + width * 0.5f + rnd.Range(0f, 8f)), height * 0.5f, i * (SceneryLoop / 26f));
                    _scenery.Add(b.transform);
                }
            }

            BuildKitScenery(trackWidth, rnd);

            // Bäume mit Pastellkronen und Neon-Laternen am Straßenrand
            var trunk = MaterialFactory.Create(new Color(0.36f, 0.23f, 0.18f), 0.2f);
            var crowns = new[] { new Color(1f, 0.48f, 0.78f), new Color(0.49f, 1f, 0.69f), new Color(1f, 0.82f, 0.48f), new Color(0.61f, 0.55f, 1f) };
            var lampMat = MaterialFactory.Create(NeonCyan, 0.8f, NeonCyan * 3f);
            for (int i = 0; i < 30; i++)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    if (_kitBuildings) continue; // echte Stadt: keine Spielzeug-Kugelbäume
                    var tree = new GameObject("Tree").transform;
                    tree.SetParent(transform);
                    MakePrimitive(PrimitiveType.Cylinder, "Trunk", tree, trunk).transform.localScale = new Vector3(0.35f, 0.8f, 0.35f);
                    var crown = MakePrimitive(PrimitiveType.Sphere, "Crown", tree, MaterialFactory.Create(crowns[rnd.Range(0, crowns.Length)], 0.3f));
                    crown.transform.localPosition = new Vector3(0, 2.2f, 0);
                    crown.transform.localScale = Vector3.one * rnd.Range(1.8f, 2.6f);
                    tree.position = new Vector3(side * (trackWidth * 0.5f + 5.6f + rnd.Range(0f, 0.6f)), 0.98f, i * (SceneryLoop / 30f));
                    _scenery.Add(tree); _streetOnly.Add(tree);
                }
                if (i % 3 == 0)
                {
                    for (int side = -1; side <= 1; side += 2)
                    {
                        var lamp = new GameObject("Lamp").transform;
                        lamp.SetParent(transform);
                        Box(lamp, laneLineMaterial, new Vector3(0, 2f, 0), new Vector3(0.12f, 4f, 0.12f));
                        Box(lamp, lampMat, new Vector3(-side * 0.5f, 4f, 0), new Vector3(1.1f, 0.14f, 0.3f));
                        lamp.position = new Vector3(side * (trackWidth * 0.5f + 2.9f), 0.18f, i * (SceneryLoop / 30f) + 3f);
                        _scenery.Add(lamp); _streetOnly.Add(lamp);
                    }
                }
            }
        }

        private void BuildKitScenery(float trackWidth, DeterministicRandom rnd)
        {
            var kit = new EnvironmentKit();
            Debug.Log($"[FersaSurfer] Version {ProductInfo.Version} · Stadt-Modelle: {kit.Buildings.Count} Gebäude, {kit.Props.Count} Deko, {kit.Cars.Count} Autos");
            if (!kit.HasAny) return;
            float half = trackWidth * 0.5f;
            _kitBuildings = kit.Buildings.Count > 0;

            if (kit.Buildings.Count > 0)
            {
                // Neon-Hochhäuser werden zur Skyline im Hintergrund
                foreach (var t in _scenery)
                    if (t.name == "Building") t.position += new Vector3(Mathf.Sign(t.position.x) * 26f, 0f, 0f);

                for (int side = -1; side <= 1; side += 2)
                {
                    float z = rnd.Range(0f, 4f);
                    while (z < SceneryLoop)
                    {
                        var prefab = kit.Buildings[rnd.Range(0, kit.Buildings.Count)];
                        float width = rnd.Range(10f, 15f);
                        var go = EnvironmentKit.Spawn(prefab, transform, new Vector3(side * (half + 7f + width * 0.5f), 0f, z + width * 0.5f), side < 0 ? -90f : 90f, width, false, 40f);
                        _scenery.Add(go.transform);
                        StyleModel(go);
                        z += width + rnd.Range(0.3f, 1.5f);
                    }
                }
            }

            if (kit.Cars.Count > 0)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    float z = rnd.Range(2f, 10f);
                    while (z < SceneryLoop)
                    {
                        if (rnd.NextFloat() < 0.7f)
                        {
                            var prefab = kit.Cars[rnd.Range(0, kit.Cars.Count)];
                            var go = EnvironmentKit.Spawn(prefab, transform, new Vector3(side * (half + 1.3f), 0f, z), rnd.NextFloat() < 0.5f ? 0f : 180f, 4.3f, false, 2.4f);
                            _scenery.Add(go.transform);
                            _streetOnly.Add(go.transform);
                        StyleModel(go);
                        }
                        z += rnd.Range(6f, 14f);
                    }
                }
            }

            if (kit.Props.Count > 0)
            {
                for (int i = 0; i < 40; i++)
                {
                    int side = i % 2 == 0 ? -1 : 1;
                    var prefab = kit.Props[rnd.Range(0, kit.Props.Count)];
                    bool tall = EnvironmentKit.NameHas(prefab, "light", "lamp", "tree", "traffic", "sign", "pole");
                    var go = EnvironmentKit.Spawn(prefab, transform, new Vector3(side * (half + 3.6f + rnd.Range(0f, 1.6f)), 0.18f, i * (SceneryLoop / 40f) + rnd.Range(0f, 3f)),
                        side < 0 ? 90f : -90f, tall ? 4.5f : 0f, tall, tall ? 6f : 2.2f);
                    _scenery.Add(go.transform);
                    _streetOnly.Add(go.transform);
                        StyleModel(go);
                }
            }
        }

        private readonly Dictionary<Material, Material> _styled = new Dictionary<Material, Material>();

        /// <summary>Moderner Look für importierte Modelle: leuchtende Fenster, glänzender Lack, sanfter Glanz.</summary>
        private void StyleModel(GameObject go)
        {
            MaterialFactory.FixMaterials(go);
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i];
                    if (m == null) continue;
                    if (!_styled.TryGetValue(m, out var styled))
                    {
                        styled = new Material(m);
                        string n = m.name.ToLowerInvariant();
                        if (styled.HasProperty("_Smoothness")) styled.SetFloat("_Smoothness", 0.3f);
                        if (n.Contains("window") || n.Contains("glass"))
                        {
                            styled.EnableKeyword("_EMISSION");
                            var glow = (_styled.Count % 2 == 0) ? new Color(1f, 0.78f, 0.45f) : new Color(0.45f, 0.85f, 1f);
                            styled.SetColor("_EmissionColor", glow * 1.4f);
                            if (styled.HasProperty("_Smoothness")) styled.SetFloat("_Smoothness", 0.92f);
                        }
                        else if (n.Contains("light") || n.Contains("lamp") || n.Contains("neon") || n.Contains("sign"))
                        {
                            styled.EnableKeyword("_EMISSION");
                            var baseColor = styled.HasProperty("_BaseColor") ? styled.GetColor("_BaseColor") : Color.white;
                            styled.SetColor("_EmissionColor", baseColor * 1.8f);
                        }
                        else if (n.Contains("car") || n.Contains("paint") || n.Contains("body") || n.Contains("metal"))
                        {
                            if (styled.HasProperty("_Smoothness")) styled.SetFloat("_Smoothness", 0.78f);
                            if (styled.HasProperty("_Metallic")) styled.SetFloat("_Metallic", 0.35f);
                        }
                        _styled[m] = styled;
                    }
                    mats[i] = styled;
                }
                r.sharedMaterials = mats;
            }
        }

        // ---------- Level: Wasser, Ziel, Auswahl ----------

        private int _levelIndex;
        private GameObject _levelRoot;
        private Material _rockMat, _foamMat, _woodMat, _waterMat;
        private readonly List<(Mesh mesh, Vector3[] baseVertices, Vector3[] buffer, Transform transform)> _waters = new List<(Mesh, Vector3[], Vector3[], Transform)>();

        public int LevelIndex => _levelIndex;
        public string LevelLabel => $"Level {_levelIndex + 1} · {_session.Level?.Name}";
        public static int LevelStars(int index) => PlayerPrefs.GetInt("fs-stars-" + index, 0);
        public static int UnlockedLevels { get { int u = 0; while (LevelStars(u) > 0 && u < 999) u++; return u; } }
        public bool CanGoNextLevel => _levelIndex < UnlockedLevels;

        public static float WaveHeight(float z, float x, float t) => 0.16f * Mathf.Sin(z * 0.32f + t * 2.2f) + 0.07f * Mathf.Sin(x * 0.6f + t * 3.1f);

        public void ChangeLevel(int direction)
        {
            int target = Mathf.Clamp(_levelIndex + direction, 0, UnlockedLevels);
            if (target == _levelIndex) return;
            SetLevel(target);
        }

        /// <summary>Startet das Level direkt am ersten Wasser-Abschnitt (zum Anschauen).</summary>
        public void StartAtWater()
        {
            if (_session.Level == null || _session.Level.Water.Count == 0) { StartRun(); return; }
            if (_session.State != GameState.Ready) Restart(); else _session.Start();
            if (_session.State == GameState.Ready) _session.Start();
            _session.WarpTo(_session.Level.Water[0].start + 3f, 6f);
            ClearViews();
        }

        public void NextLevel()
        {
            SetLevel(Mathf.Min(_levelIndex + 1, UnlockedLevels));
            _session.Start();
        }

        private void SetLevel(int index)
        {
            _levelIndex = index;
            PlayerPrefs.SetInt("fs-level", index);
            ClearViews();
            _session.SetLevel(LevelDefinition.Create(index));
            BuildLevelView();
        }

        private void BuildLevelView()
        {
            if (_levelRoot != null) Destroy(_levelRoot);
            _waters.Clear();
            _levelRoot = new GameObject("Level");
            _levelRoot.transform.SetParent(transform, false);
            _rockMat ??= MaterialFactory.Create(new Color(0.42f, 0.44f, 0.56f), 0.2f);
            _foamMat ??= MaterialFactory.Create(new Color(0.88f, 0.97f, 1f), 0.5f, new Color(0.2f, 0.55f, 0.7f));
            _woodMat ??= MaterialFactory.Create(new Color(0.62f, 0.4f, 0.24f), 0.3f);
            _waterMat ??= MaterialFactory.Create(new Color(0.08f, 0.42f, 0.84f), 0.93f, new Color(0.02f, 0.1f, 0.3f));
            var level = _session.Level;
            if (level == null) return;

            foreach (var (start, end) in level.Water)
            {
                float length = end - start;
                var mesh = MeshFactory.Grid(60f, length, 40, Mathf.CeilToInt(length / 1.5f));
                var go = new GameObject("Water");
                go.transform.SetParent(_levelRoot.transform, false);
                go.transform.position = new Vector3(0f, 0.45f, start);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                go.AddComponent<MeshRenderer>().sharedMaterial = _waterMat;
                var baseVerts = mesh.vertices;
                _waters.Add((mesh, baseVerts, new Vector3[baseVerts.Length], go.transform));
                foreach (var z in new[] { start, end }) Box(_levelRoot.transform, _foamMat, new Vector3(0f, 0.5f, z), new Vector3(60f, 0.3f, 0.8f));
            }

            // Ziel-Tor mit Schachbrett-Banner
            var post = MaterialFactory.Create(Color.white, 0.6f, NeonCyan * 1.5f);
            for (int s = -1; s <= 1; s += 2) Box(_levelRoot.transform, post, new Vector3(s * 5.6f, 3f, level.Length), new Vector3(0.6f, 6f, 0.6f));
            var checker = new Texture2D(16, 2) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
            for (int y = 0; y < 2; y++) for (int x = 0; x < 16; x++) checker.SetPixel(x, y, (x + y) % 2 == 0 ? Color.white : new Color(0.1f, 0.08f, 0.19f));
            checker.Apply();
            var bannerMat = MaterialFactory.Create(Color.white, 0.3f, Color.white * 0.4f);
            bannerMat.SetTexture("_BaseMap", checker);
            bannerMat.SetTexture("_MainTex", checker);
            bannerMat.SetTexture("_EmissionMap", checker);
            Box(_levelRoot.transform, bannerMat, new Vector3(0f, 5.6f, level.Length), new Vector3(11.8f, 1.6f, 0.3f));
        }

        private void UpdateLevelView()
        {
            float t = Time.time;
            foreach (var (mesh, baseVertices, verts, tr) in _waters)
            {
                if (tr.position.z > _session.PlayerZ + 240f || tr.position.z + 400f < _session.PlayerZ) continue;
                float z0 = tr.position.z;
                for (int i = 0; i < verts.Length; i++)
                {
                    var v = baseVertices[i];
                    v.y = WaveHeight(z0 + v.z, v.x, t);
                    verts[i] = v;
                }
                mesh.vertices = verts;
                mesh.RecalculateNormals();
            }
        }

        private static Texture2D WindowTexture()
        {
            var tex = new Texture2D(16, 32, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
            var r = new System.Random(5);
            var px = new Color32[16 * 32];
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 16; x++)
                {
                    bool window = x % 4 >= 1 && x % 4 <= 2 && y % 4 >= 1 && y % 4 <= 2;
                    bool lit = window && ((x / 4) * 7 + (y / 4) * 13 + r.Next(3)) % 5 < 3;
                    px[y * 16 + x] = lit ? ((x / 4 + y / 4) % 3 == 0 ? new Color32(140, 240, 255, 255) : new Color32(255, 214, 130, 255)) : new Color32(0, 0, 0, 255);
                }
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        private List<GameObject> _models = new List<GameObject>();
        private int _modelIndex;

        public string CharacterName => _models.Count > 0 ? _models[_modelIndex].name : "Standard";
        public int CharacterCount => _models.Count;

        private void BuildPlayer()
        {
            _player = new GameObject("Player").transform;
            _player.SetParent(transform);
            _models = CharacterView.LoadModels();
            string saved = PlayerPrefs.GetString("fs-character", "");
            _modelIndex = Mathf.Max(0, _models.FindIndex(m => m.name == saved));
            _character = new CharacterView(_player, _models.Count > 0 ? _models[_modelIndex] : null);
        }

        /// <summary>Nächste/vorherige Figur auswählen (Startmenü).</summary>
        public List<string> NextCharacter(int direction)
        {
            if (_models.Count < 2) return null;
            _modelIndex = (_modelIndex + direction + _models.Count) % _models.Count;
            PlayerPrefs.SetString("fs-character", _models[_modelIndex].name);
            Destroy(_character.Root.gameObject);
            _character = new CharacterView(_player, _models[_modelIndex]);
            return ApplyOutfit(outfit);
        }

        private void BuildCamera()
        {
            _camera = Camera.main;
            if (_camera == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                _camera = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            _camera.clearFlags = _skyMaterial != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
            _camera.farClipPlane = 260f;
            _camera.transform.position = new Vector3(2.6f, 2.3f, 4.6f);
            if (GraphicsSettings.currentRenderPipeline != null)
            {
                var data = _camera.GetUniversalAdditionalCameraData();
                if (data != null) data.renderPostProcessing = true;
            }
        }

        private void BuildParticles()
        {
            var go = new GameObject("Sparkles");
            go.transform.SetParent(transform, false);
            _particles = go.AddComponent<ParticleSystem>();
            _particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = _particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 7f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
            main.gravityModifier = 1.2f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 1500;
            var emission = _particles.emission;
            emission.rateOverTime = 0f;
            var shape = _particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.25f;
            var size = _particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Sprites/Default");
            var r = go.GetComponent<ParticleSystemRenderer>();
            if (shader != null) r.sharedMaterial = new Material(shader);
            _particles.Play();
        }

        private void Burst(Vector3 position, Color color, int count)
        {
            if (_particles == null) return;
            var p = new ParticleSystem.EmitParams { position = position, applyShapeToPosition = true, startColor = color };
            _particles.Emit(p, count);
        }

        private void BuildAudio()
        {
            _sfx = gameObject.AddComponent<AudioSource>();
            _sfx.playOnAwake = false;
            _music = gameObject.AddComponent<AudioSource>();
            _music.playOnAwake = false;
            _music.loop = true;
            _music.volume = 0.45f;
            _music.clip = SynthAudio.MusicLoop();
            _coinClip = SynthAudio.Coin();
            _jumpClip = SynthAudio.Jump();
            _slideClip = SynthAudio.Slide();
            _powerClip = SynthAudio.PowerUp();
            _crashClip = SynthAudio.Crash();
        }

        public bool SoundOn
        {
            get => AudioListener.volume > 0f;
            set => AudioListener.volume = value ? 1f : 0f;
        }

        private void UpdateMusic()
        {
            if (_music == null) return;
            bool running = _session.State == GameState.Running;
            if (running && !_music.isPlaying) _music.Play();
            if (_session.State == GameState.Paused && _music.isPlaying) _music.Pause();
            float target = 1f + (_session.Speed - _session.Config.StartSpeed) / 60f + (_session.Motor.IsHovering ? 0.06f : 0f);
            _music.pitch = Mathf.Lerp(_music.pitch, target, Time.deltaTime * 2f);
        }

        // ---------- Hilfen ----------

        private static GameObject Box(Transform parent, Material mat, Vector3 localPos, Vector3 scale, Material overrideMat = null)
        {
            var go = MakePrimitive(PrimitiveType.Cube, "Box", parent, overrideMat ?? mat);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            return go;
        }

        private static GameObject MakePrimitive(PrimitiveType type, string name, Transform parent, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col); // Kollision macht die Spiellogik, nicht die Physik-Engine.
            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        private void CreateMaterialsIfMissing()
        {
            groundMaterial = groundMaterial ? groundMaterial : MaterialFactory.Create(new Color(0.12f, 0.1f, 0.22f), 0.78f);
            laneLineMaterial = laneLineMaterial ? laneLineMaterial : MaterialFactory.Create(new Color(0.85f, 0.95f, 1f), 0.6f, NeonCyan * 1.8f);
            playerMaterial = playerMaterial ? playerMaterial : MaterialFactory.Create(NeonPink, 0.6f);
            hurdleMaterial = hurdleMaterial ? hurdleMaterial : MaterialFactory.Create(Sun, 0.6f, Sun * 0.9f);
            overheadMaterial = overheadMaterial ? overheadMaterial : MaterialFactory.Create(NeonCyan, 0.7f, NeonCyan * 2f);
            blockMaterial = blockMaterial ? blockMaterial : MaterialFactory.Create(NeonPink, 0.75f, NeonPink * 1.3f);
            coinMaterial = coinMaterial ? coinMaterial : MaterialFactory.Create(Sun, 0.95f, Sun * 1.8f);
            buildingMaterial = buildingMaterial ? buildingMaterial : MaterialFactory.Create(Color.white, 0.3f);
        }
    }

    /// <summary>Erzeugt Materialien passend zur aktiven Render-Pipeline (URP oder Built-in).</summary>
    public static class MaterialFactory
    {
        public static Shader LitShader()
        {
            Shader s = null;
            if (GraphicsSettings.currentRenderPipeline != null) s = Shader.Find("Universal Render Pipeline/Lit");
            if (s == null) s = Shader.Find("Standard");
            return s;
        }

        public static Material Create(Color color, float smoothness, Color? emission = null)
        {
            var m = new Material(LitShader());
            m.SetColor("_BaseColor", color);
            m.SetColor("_Color", color);
            m.SetFloat("_Smoothness", smoothness);
            m.SetFloat("_Glossiness", smoothness);
            if (emission.HasValue)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission.Value);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            return m;
        }

        private static readonly Dictionary<Material, Material> Converted = new Dictionary<Material, Material>();

        /// <summary>
        /// Wandelt Materialien aus fremden Paketen (Built-in "Standard", eigene Toon-Shader …), die in URP pink
        /// erscheinen würden, in URP/Lit um – Textur und Farbe bleiben erhalten.
        /// </summary>
        public static Material MakeCompatible(Material m)
        {
            if (m == null || GraphicsSettings.currentRenderPipeline == null) return m;
            var shader = m.shader;
            string name = shader != null ? shader.name : "";
            // Nur echte URP-Shader behalten. Shader-Graph-Materialien aus Paketen sind oft für die alte Pipeline gebaut (= pink).
            bool ok = shader != null && shader.isSupported && name.StartsWith("Universal Render Pipeline/") && !name.Contains("Error");
            if (ok) return m;
            if (Converted.TryGetValue(m, out var done)) return done;

            var lit = new Material(LitShader()) { name = m.name + " (URP)" };
            Texture tex = null;
            foreach (var prop in new[] { "_BaseMap", "_MainTex", "_BaseColorMap", "_Albedo", "_Diffuse", "_MainTexture" })
                if (m.HasProperty(prop) && m.GetTexture(prop) != null) { tex = m.GetTexture(prop); break; }
            if (tex == null)
                foreach (var prop in m.GetTexturePropertyNames())
                {
                    string lp = prop.ToLowerInvariant();
                    if (lp.Contains("normal") || lp.Contains("bump") || lp.Contains("mask") || lp.Contains("rough") || lp.Contains("metal") || lp.Contains("occlusion") || lp.Contains("emission")) continue;
                    var t = m.GetTexture(prop);
                    if (t != null) { tex = t; break; }
                }
            Color color = Color.white;
            bool foundColor = false;
            foreach (var prop in new[] { "_BaseColor", "_Color", "_MainColor", "_Tint" })
                if (m.HasProperty(prop)) { color = m.GetColor(prop); foundColor = true; break; }
            if (!foundColor)
                foreach (var prop in m.GetPropertyNames(MaterialPropertyType.Vector))
                {
                    string lp = prop.ToLowerInvariant();
                    if (lp.Contains("color") || lp.Contains("tint") || lp.Contains("albedo")) { color = m.GetColor(prop); break; }
                }
            if (color.a < 0.05f) color.a = 1f;
            if (tex != null) { lit.SetTexture("_BaseMap", tex); lit.mainTextureScale = m.mainTextureScale; }
            lit.SetColor("_BaseColor", color);
            lit.SetFloat("_Smoothness", 0.25f);
            if (m.HasProperty("_Cutoff") || m.IsKeywordEnabled("_ALPHATEST_ON"))
            {
                lit.SetFloat("_AlphaClip", 1f);
                lit.EnableKeyword("_ALPHATEST_ON");
                lit.SetFloat("_Cutoff", m.HasProperty("_Cutoff") ? m.GetFloat("_Cutoff") : 0.5f);
            }
            Converted[m] = lit;
            Debug.Log($"[FersaSurfer] Material umgewandelt: '{m.name}' (Shader '{name}') → URP/Lit, Textur: {(tex != null ? tex.name : "-")}");
            return lit;
        }

        private static readonly Dictionary<string, Material> Fallbacks = new Dictionary<string, Material>();
        public static int EmptySlotsFilled;

        private static readonly Color[] Pastels =
        {
            new Color(0.98f, 0.72f, 0.62f), new Color(0.62f, 0.8f, 0.98f), new Color(0.98f, 0.88f, 0.55f),
            new Color(0.72f, 0.92f, 0.7f), new Color(0.86f, 0.72f, 0.98f), new Color(0.98f, 0.66f, 0.8f)
        };
        private static readonly Color[] CarColors =
        {
            new Color(0.95f, 0.25f, 0.3f), new Color(0.2f, 0.55f, 0.95f), new Color(1f, 0.8f, 0.15f),
            new Color(0.25f, 0.8f, 0.45f), new Color(0.95f, 0.5f, 0.15f), new Color(0.95f, 0.95f, 0.98f)
        };

        /// <summary>Material für leere Material-Plätze: Cartoon-Farbe passend zum Objektnamen.</summary>
        public static Material Fallback(string objectName, int slot)
        {
            string n = (objectName ?? "").ToLowerInvariant();
            int h = 0;
            foreach (char ch in n) h = h * 31 + ch;
            h = System.Math.Abs(h + slot * 7);
            Color c;
            string key;
            if (n.Contains("tree") || n.Contains("bush") || n.Contains("plant") || n.Contains("grass") || n.Contains("leaf"))
            { c = slot == 0 ? new Color(0.36f, 0.78f, 0.38f) : new Color(0.52f, 0.34f, 0.22f); key = "green" + slot; }
            else if (n.Contains("car") || n.Contains("taxi") || n.Contains("bus") || n.Contains("truck") || n.Contains("van"))
            { c = slot == 0 ? CarColors[h % CarColors.Length] : new Color(0.15f, 0.15f, 0.2f); key = "car" + (slot == 0 ? h % CarColors.Length : 99); }
            else if (n.Contains("lamp") || n.Contains("light") || n.Contains("pole") || n.Contains("sign") || n.Contains("fence") || n.Contains("bench"))
            { c = new Color(0.42f, 0.45f, 0.55f); key = "metal"; }
            else if (n.Contains("road") || n.Contains("street") || n.Contains("sidewalk"))
            { c = new Color(0.32f, 0.32f, 0.38f); key = "road"; }
            else
            { c = slot == 0 ? Pastels[h % Pastels.Length] : (slot == 1 ? new Color(0.55f, 0.82f, 0.98f) : Pastels[(h + slot) % Pastels.Length]); key = "pastel" + (h + slot) % Pastels.Length + "_" + System.Math.Min(slot, 1); }
            if (!Fallbacks.TryGetValue(key, out var m))
            {
                m = Create(c, 0.35f);
                m.name = "Cartoon_" + key;
                Fallbacks[key] = m;
            }
            return m;
        }

        /// <summary>Alle Materialien eines Objekts URP-tauglich machen; leere Plätze bekommen Cartoon-Farben.</summary>
        public static void FixMaterials(GameObject go)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer) continue;
                int subMeshes = 1;
                if (r is SkinnedMeshRenderer smr && smr.sharedMesh != null) subMeshes = smr.sharedMesh.subMeshCount;
                else
                {
                    var mf = r.GetComponent<MeshFilter>();
                    if (mf != null && mf.sharedMesh != null) subMeshes = mf.sharedMesh.subMeshCount;
                }
                var mats = r.sharedMaterials;
                bool changed = false;
                if (mats.Length < subMeshes) { System.Array.Resize(ref mats, subMeshes); changed = true; }
                for (int i = 0; i < mats.Length; i++)
                {
                    Material fixedMat = mats[i] == null ? Fallback(r.name + " " + go.name, i) : MakeCompatible(mats[i]);
                    if (mats[i] == null) EmptySlotsFilled++;
                    if (fixedMat != mats[i]) { mats[i] = fixedMat; changed = true; }
                }
                if (changed) r.sharedMaterials = mats;
            }
        }

        /// <summary>Beidseitig sichtbar (für prozedurale Ringe/Kegel).</summary>
        public static Material DoubleSided(Material m)
        {
            if (m.HasProperty("_Cull")) m.SetFloat("_Cull", 0f);
            m.doubleSidedGI = true;
            return m;
        }
    }
}
