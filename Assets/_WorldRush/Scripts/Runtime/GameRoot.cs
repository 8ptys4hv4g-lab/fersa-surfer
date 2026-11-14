using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using WorldRush.Core;

namespace WorldRush.Runtime
{
    /// <summary>
    /// M1-Prototyp: baut die komplette Runner-Szene aus einfachen 3D-Formen auf und
    /// zeigt den Zustand der reinen Spiellogik (<see cref="GameSession"/>) an.
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

        private GameSession _session;
        private RunnerInput _input;
        private LocalSaveRepository _saveRepo;
        private SaveData _save;
        private Transform _player;
        private Transform _playerBody;
        private Camera _camera;
        private readonly Dictionary<int, GameObject> _rowViews = new Dictionary<int, GameObject>();
        private readonly Dictionary<int, GameObject> _coinViews = new Dictionary<int, GameObject>();
        private readonly Stack<GameObject> _coinPool = new Stack<GameObject>();
        private readonly List<Transform> _groundTiles = new List<Transform>();
        private readonly List<Transform> _buildings = new List<Transform>();
        private readonly HashSet<int> _seen = new HashSet<int>();
        private readonly List<int> _remove = new List<int>();
        private HudOverlay _hud;

        private const float TileLength = 20f;
        private const int TileCount = 10;

        public GameSession Session => _session;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            CreateMaterialsIfMissing();

            _saveRepo = new LocalSaveRepository(Path.Combine(Application.persistentDataPath, ProductInfo.SaveFileName));
            _save = _saveRepo.Load();

            int startSeed = seed != 0 ? seed : System.Environment.TickCount;
            _session = new GameSession(RunnerConfig.CreateDefault(), startSeed, _save.HighScore);
            _session.StateChanged += OnStateChanged;
            _input = new RunnerInput();

            BuildEnvironment();
            BuildPlayer();
            BuildCamera();

            _hud = gameObject.AddComponent<HudOverlay>();
            _hud.Init(this);

            if (autoStart) _session.Start();
        }

        public void StartRun() => _session.Start();
        public void Restart() { ClearViews(); _session.Restart(); }
        public void TogglePause() => _session.TogglePause();

        private void Update()
        {
            var commands = _input.Poll();
            if (_input.PausePressed) _session.TogglePause();

            switch (_session.State)
            {
                case GameState.Ready:
                    if (_input.ConfirmPressed || commands.Count > 0) _session.Start();
                    break;
                case GameState.GameOver:
                    if (_input.ConfirmPressed) Restart();
                    break;
                case GameState.Running:
                    foreach (var c in commands) _session.Enqueue(c);
                    _session.Tick(Time.deltaTime);
                    break;
            }

            SyncViews();
        }

        private void LateUpdate()
        {
            if (_camera == null) return;
            var target = new Vector3(_session.Motor.LateralX * 0.6f, 4.2f + _session.Motor.FeetY * 0.4f, _session.PlayerZ - 7.5f);
            var cam = _camera.transform;
            cam.position = Vector3.Lerp(cam.position, target, 1f - Mathf.Exp(-10f * Time.deltaTime));
            cam.rotation = Quaternion.Euler(16f, 0f, 0f);
            float speedT = Mathf.InverseLerp(_session.Config.StartSpeed, _session.Config.MaxSpeed, _session.Speed);
            _camera.fieldOfView = Mathf.Lerp(60f, 68f, speedT);
        }

        private void OnStateChanged(GameState state)
        {
            if (state != GameState.GameOver) return;
            _save.RunsPlayed++;
            _save.TotalCoins += _session.Score.Coins;
            _save.HighScore = Mathf.Max(_save.HighScore, _session.Score.HighScore);
            try { _saveRepo.Save(_save); }
            catch (System.Exception e) { Debug.LogWarning("[FersaSurfer] Speichern fehlgeschlagen: " + e.Message); }
        }

        // ---------- Darstellung ----------

        private void SyncViews()
        {
            var cfg = _session.Config;
            var motor = _session.Motor;
            _player.position = new Vector3(motor.LateralX, motor.FeetY, _session.PlayerZ);
            float h = motor.Height;
            _playerBody.localScale = new Vector3(0.8f, h * 0.5f, 0.8f);
            _playerBody.localPosition = new Vector3(0f, h * 0.5f, 0f);

            // Hindernisreihen
            _seen.Clear();
            foreach (var row in _session.Rows)
            {
                _seen.Add(row.Id);
                if (!_rowViews.ContainsKey(row.Id)) _rowViews[row.Id] = CreateRowView(row, cfg);
            }
            _remove.Clear();
            foreach (var kv in _rowViews) if (!_seen.Contains(kv.Key)) _remove.Add(kv.Key);
            foreach (var id in _remove) { Destroy(_rowViews[id]); _rowViews.Remove(id); }

            // Münzen (gepoolt)
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
                go.transform.rotation = Quaternion.Euler(90f, Time.time * 180f, 0f);
            }
            _remove.Clear();
            foreach (var kv in _coinViews) if (!_seen.Contains(kv.Key)) _remove.Add(kv.Key);
            foreach (var id in _remove)
            {
                var go = _coinViews[id];
                go.SetActive(false);
                _coinPool.Push(go);
                _coinViews.Remove(id);
            }

            // Boden und Gebäude wandern mit (Endlos-Effekt).
            float baseZ = Mathf.Floor((_session.PlayerZ - TileLength) / TileLength) * TileLength;
            for (int i = 0; i < _groundTiles.Count; i++)
                _groundTiles[i].position = new Vector3(0f, -0.05f, baseZ + i * TileLength + TileLength * 0.5f);
            for (int i = 0; i < _buildings.Count; i++)
            {
                var b = _buildings[i];
                float span = TileCount * TileLength;
                float z = b.position.z;
                if (z < _session.PlayerZ - 20f) z += span;
                else if (z > _session.PlayerZ + span) z -= span; // nach Neustart
                b.position = new Vector3(b.position.x, b.position.y, z);
            }
        }

        private void ClearViews()
        {
            foreach (var go in _rowViews.Values) Destroy(go);
            _rowViews.Clear();
            foreach (var go in _coinViews.Values) { go.SetActive(false); _coinPool.Push(go); }
            _coinViews.Clear();
        }

        private GameObject CreateRowView(ObstacleRow row, RunnerConfig cfg)
        {
            var root = new GameObject("Row " + row.Id);
            root.transform.position = new Vector3(0f, 0f, row.Z);
            for (int lane = Lanes.Left; lane <= Lanes.Right; lane++)
            {
                var kind = row.At(lane);
                if (kind == ObstacleKind.None) continue;
                CollisionRules.GetVerticalRange(cfg, kind, out float bottom, out float top);
                var mat = kind == ObstacleKind.Hurdle ? hurdleMaterial : kind == ObstacleKind.Overhead ? overheadMaterial : blockMaterial;
                var cube = MakePrimitive(PrimitiveType.Cube, kind.ToString(), root.transform, mat);
                cube.transform.localPosition = new Vector3(Lanes.ToX(lane, cfg.LaneWidth), (bottom + top) * 0.5f, 0f);
                cube.transform.localScale = new Vector3(cfg.ObstacleHalfWidth * 2f, top - bottom, cfg.ObstacleHalfDepth * 2f);

                if (kind == ObstacleKind.Overhead)
                {
                    // Pfosten machen den Balken klar erkennbar (rein optisch, keine Kollision).
                    for (int side = -1; side <= 1; side += 2)
                    {
                        var post = MakePrimitive(PrimitiveType.Cube, "Post", root.transform, overheadMaterial);
                        post.transform.localPosition = new Vector3(Lanes.ToX(lane, cfg.LaneWidth) + side * (cfg.ObstacleHalfWidth + 0.08f), bottom * 0.5f, 0f);
                        post.transform.localScale = new Vector3(0.12f, bottom, 0.12f);
                    }
                }
            }
            return root;
        }

        private GameObject CreateCoinView()
        {
            var coin = MakePrimitive(PrimitiveType.Cylinder, "Coin", transform, coinMaterial);
            coin.transform.localScale = new Vector3(0.6f, 0.06f, 0.6f);
            return coin;
        }

        private void BuildEnvironment()
        {
            var cfg = _session.Config;

            var lightGo = new GameObject("Sun");
            var sun = lightGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.2f;
            sun.color = new Color(1f, 0.95f, 0.88f);
            sun.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.65f, 0.95f);
            RenderSettings.ambientEquatorColor = new Color(0.75f, 0.6f, 0.85f);
            RenderSettings.ambientGroundColor = new Color(0.25f, 0.22f, 0.3f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.72f, 0.66f, 0.92f);
            RenderSettings.fogStartDistance = 60f;
            RenderSettings.fogEndDistance = 170f;

            float trackWidth = cfg.LaneWidth * 3f + 1f;
            for (int i = 0; i < TileCount; i++)
            {
                var tile = new GameObject("GroundTile " + i).transform;
                tile.SetParent(transform);
                var road = MakePrimitive(PrimitiveType.Cube, "Road", tile, groundMaterial);
                road.transform.localScale = new Vector3(trackWidth, 0.1f, TileLength);
                for (int line = 0; line < 2; line++)
                {
                    var stripe = MakePrimitive(PrimitiveType.Cube, "LaneLine", tile, laneLineMaterial);
                    stripe.transform.localPosition = new Vector3((line - 0.5f) * cfg.LaneWidth, 0.06f, 0f);
                    stripe.transform.localScale = new Vector3(0.08f, 0.02f, TileLength * 0.9f);
                }
                _groundTiles.Add(tile);
            }

            // Einfache Stadtkulisse: Blöcke links und rechts (wird in M2 durch Premium-Assets ersetzt).
            var rnd = new DeterministicRandom(7);
            for (int i = 0; i < TileCount * 2; i++)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    var b = MakePrimitive(PrimitiveType.Cube, "Building", transform, buildingMaterial);
                    float height = rnd.Range(6f, 22f);
                    float width = rnd.Range(4f, 8f);
                    b.transform.localScale = new Vector3(width, height, rnd.Range(5f, 9f));
                    b.transform.position = new Vector3(side * (trackWidth * 0.5f + 3f + width * 0.5f + rnd.Range(0f, 4f)), height * 0.5f, i * TileLength * 0.5f);
                    var r = b.GetComponent<Renderer>();
                    var block = new MaterialPropertyBlock();
                    Color tint = Color.HSVToRGB(rnd.Range(0.55f, 0.95f), 0.35f, rnd.Range(0.75f, 1f));
                    block.SetColor("_BaseColor", tint);
                    block.SetColor("_Color", tint);
                    r.SetPropertyBlock(block);
                    _buildings.Add(b.transform);
                }
            }
        }

        private void BuildPlayer()
        {
            _player = new GameObject("Player").transform;
            _player.SetParent(transform);
            var body = MakePrimitive(PrimitiveType.Capsule, "Body", _player, playerMaterial);
            _playerBody = body.transform;
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
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0.72f, 0.66f, 0.92f);
            _camera.farClipPlane = 220f;
            _camera.transform.position = new Vector3(0f, 4.2f, -7.5f);
        }

        // ---------- Hilfen ----------

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
            groundMaterial = groundMaterial ? groundMaterial : MaterialFactory.Create(new Color(0.22f, 0.22f, 0.32f), 0.4f);
            laneLineMaterial = laneLineMaterial ? laneLineMaterial : MaterialFactory.Create(new Color(0.9f, 0.95f, 1f), 0.2f, new Color(0.4f, 0.6f, 1f));
            playerMaterial = playerMaterial ? playerMaterial : MaterialFactory.Create(new Color(1f, 0.45f, 0.35f), 0.6f);
            hurdleMaterial = hurdleMaterial ? hurdleMaterial : MaterialFactory.Create(new Color(1f, 0.8f, 0.2f), 0.5f);
            overheadMaterial = overheadMaterial ? overheadMaterial : MaterialFactory.Create(new Color(0.3f, 0.85f, 1f), 0.5f);
            blockMaterial = blockMaterial ? blockMaterial : MaterialFactory.Create(new Color(0.95f, 0.3f, 0.55f), 0.5f);
            coinMaterial = coinMaterial ? coinMaterial : MaterialFactory.Create(new Color(1f, 0.85f, 0.25f), 0.9f, new Color(0.6f, 0.45f, 0f));
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
            }
            return m;
        }
    }
}
