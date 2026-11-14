using System.Collections.Generic;
using UnityEngine;
using WorldRush.Core;
using WorldRush.Core.Outfits;

namespace WorldRush.Runtime
{
    /// <summary>
    /// Spielfigur. Liegt ein echtes 3D-Modell in Resources/Characters, wird dieses verwendet
    /// (skaliert, animiert, nach Outfit-Text eingefärbt). Sonst entsteht eine Platzhalter-Figur aus Grundformen.
    /// </summary>
    public sealed class CharacterView
    {
        public Transform Root { get; }
        public bool UsesModel => _model != null;

        private readonly Transform _body;
        private readonly GameObject _board;
        private readonly Transform _shield;
        private float _phase;

        // Echtes Modell
        private readonly GameObject _model;
        private readonly Animation _animation;
        private readonly Renderer[] _modelRenderers;
        private string _runClip, _idleClip, _jumpClip, _slideClip, _current;

        // Platzhalter
        private Transform _legL, _legR, _armL, _armR, _tail;
        private GameObject _dress, _torso, _pantsL, _pantsR, _cap;
        private Material _skin, _hair, _dressMat, _top, _pants, _shoes, _capMat;

        public static readonly Dictionary<string, Color> Palette = new Dictionary<string, Color>
        {
            { "red", Hex("#e8304a") }, { "blue", Hex("#3a6ee8") }, { "green", Hex("#2fcf7a") }, { "yellow", Hex("#ffd23f") },
            { "black", Hex("#24203a") }, { "white", Hex("#f4f2ff") }, { "brown", Hex("#7a4a2a") }, { "blonde", Hex("#f5d36b") },
            { "pink", Hex("#ff7ac8") }, { "purple", Hex("#8f5bff") }, { "orange", Hex("#ff8a3d") }, { "grey", Hex("#9a98b0") },
            { "silver", Hex("#d6d8e6") }, { "gold", Hex("#e3b23c") }, { "beige", Hex("#e8d3b0") }
        };

        /// <summary>Schlüsselwörter in Material-/Objektnamen, um Outfit-Farben auf echte Modelle zu übertragen.</summary>
        private static readonly Dictionary<OutfitSlot, string[]> SlotKeywords = new Dictionary<OutfitSlot, string[]>
        {
            { OutfitSlot.Hair, new[] { "hair", "haar" } },
            { OutfitSlot.Dress, new[] { "dress", "kleid", "skirt" } },
            { OutfitSlot.Top, new[] { "shirt", "top", "jacket", "hoodie", "torso", "cloth" } },
            { OutfitSlot.Bottom, new[] { "pant", "trouser", "jean", "short" } },
            { OutfitSlot.Shoes, new[] { "shoe", "boot", "sneaker", "feet", "foot" } },
            { OutfitSlot.Accessory, new[] { "hat", "cap", "helmet" } },
        };

        /// <summary>Alle 3D-Figuren aus Resources/Characters (auch Unterordner), "Casual" zuerst.</summary>
        public static List<GameObject> LoadModels()
        {
            var result = new List<GameObject>();
            var names = new HashSet<string>();
            foreach (var path in new[] { "Characters", "Characters/FBX", "Characters/Models" })
                foreach (var go in Resources.LoadAll<GameObject>(path))
                    if (go != null && go.GetComponentInChildren<Renderer>(true) != null && names.Add(go.name)) result.Add(go);
            result.Sort((a, b) =>
            {
                int pa = a.name.ToLowerInvariant().Contains("casual") ? 0 : 1, pb = b.name.ToLowerInvariant().Contains("casual") ? 0 : 1;
                return pa != pb ? pa.CompareTo(pb) : string.CompareOrdinal(a.name, b.name);
            });
            return result;
        }

        public CharacterView(Transform parent, GameObject prefab)
        {
            Root = new GameObject("Character").transform;
            Root.SetParent(parent, false);
            _body = new GameObject("Body").transform;
            _body.SetParent(Root, false);

            if (prefab != null)
            {
                _model = Object.Instantiate(prefab, _body, false);
                _model.name = "Model";
                FitToHeight(_model, 2.0f);
                _modelRenderers = _model.GetComponentsInChildren<Renderer>(true);
                foreach (var r in _modelRenderers)
                {
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                    r.materials = r.materials; // eigene Instanzen, damit Outfit-Farben nur diese Figur betreffen
                }
                _animation = _model.GetComponentInChildren<Animation>();
                PickClips();
                Debug.Log($"[FersaSurfer] 3D-Figur geladen: {prefab.name} · Renderer: {_modelRenderers.Length} · " +
                          $"Animationen: {(_animation != null ? _animation.GetClipCount() : 0)} (Laufen: {_runClip ?? "-"}, Springen: {_jumpClip ?? "-"}, Stehen: {_idleClip ?? "-"})");
            }
            else
            {
                int found = Resources.LoadAll<Object>("Characters").Length;
                Debug.LogWarning($"[FersaSurfer] Keine 3D-Figur in Assets/_WorldRush/Resources/Characters gefunden (Dateien dort: {found}). Platzhalter-Figur wird verwendet.");
                BuildPlaceholder();
            }

            var boardMat = MaterialFactory.Create(Hex("#3de8ff"), 0.9f, Hex("#3de8ff") * 2.2f);
            _board = Part(PrimitiveType.Cube, Root, boardMat, new Vector3(0, -0.05f, 0), new Vector3(0.9f, 0.09f, 2.1f));
            _board.SetActive(false);

            _shield = new GameObject("Shield").transform;
            _shield.SetParent(Root, false);
            _shield.localPosition = new Vector3(0, 1.1f, 0);
            var shieldMat = MaterialFactory.DoubleSided(MaterialFactory.Create(Hex("#9b8cff"), 0.9f, Hex("#9b8cff") * 2.5f));
            for (int i = 0; i < 3; i++)
            {
                var ring = new GameObject("Ring" + i);
                ring.transform.SetParent(_shield, false);
                ring.transform.localRotation = Quaternion.Euler(i * 60f, i * 45f, 0f);
                ring.AddComponent<MeshFilter>().sharedMesh = MeshFactory.Ring(1.2f, 0.035f, 48);
                ring.AddComponent<MeshRenderer>().sharedMaterial = shieldMat;
            }
            _shield.gameObject.SetActive(false);
        }

        /// <summary>
        /// Bringt das Modell auf die gewünschte Höhe und stellt die Füße auf den Boden.
        /// Nutzt die Mesh-Daten statt Renderer.bounds, weil animierte Figuren beim Start oft falsche Bounds melden.
        /// </summary>
        private static void FitToHeight(GameObject go, float height)
        {
            foreach (var smr in go.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                smr.updateWhenOffscreen = true; // nie fälschlich ausblenden

            if (!TryMeshBounds(go, out var b) || b.size.y < 0.0001f) { Debug.LogWarning("[FersaSurfer] Figur hat keine messbare Größe."); return; }
            float factor = height / b.size.y;
            if (float.IsNaN(factor) || float.IsInfinity(factor)) return;
            go.transform.localScale *= factor;
            TryMeshBounds(go, out b);
            var anchor = go.transform.parent != null ? go.transform.parent.position : Vector3.zero;
            go.transform.position += new Vector3(anchor.x - b.center.x, anchor.y - b.min.y, anchor.z - b.center.z);
            Debug.Log($"[FersaSurfer] Figur skaliert mit Faktor {factor:0.###}, Höhe jetzt {b.size.y:0.00} m");
        }

        private static bool TryMeshBounds(GameObject go, out Bounds bounds)
        {
            var points = new List<Vector3>();
            foreach (var smr in go.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (smr.sharedMesh != null) AddCorners(points, smr.transform.localToWorldMatrix, smr.sharedMesh.bounds);
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true))
                if (mf.sharedMesh != null) AddCorners(points, mf.transform.localToWorldMatrix, mf.sharedMesh.bounds);

            bounds = new Bounds(points.Count > 0 ? points[0] : Vector3.zero, Vector3.zero);
            for (int i = 1; i < points.Count; i++) bounds.Encapsulate(points[i]);
            return points.Count > 0;
        }

        private static void AddCorners(List<Vector3> points, Matrix4x4 m, Bounds local)
        {
            var c = local.center;
            var e = local.extents;
            for (int i = 0; i < 8; i++)
                points.Add(m.MultiplyPoint3x4(c + new Vector3((i & 1) == 0 ? -e.x : e.x, (i & 2) == 0 ? -e.y : e.y, (i & 4) == 0 ? -e.z : e.z)));
        }

        private void PickClips()
        {
            if (_animation == null) return;
            foreach (AnimationState state in _animation)
            {
                string n = state.name.ToLowerInvariant();
                if (_runClip == null && (n.Contains("run") || n.Contains("sprint"))) _runClip = state.name;
                else if (_idleClip == null && n.Contains("idle")) _idleClip = state.name;
                else if (_jumpClip == null && n.Contains("jump")) _jumpClip = state.name;
                else if (_slideClip == null && (n.Contains("slide") || n.Contains("roll") || n.Contains("crouch") || n.Contains("duck"))) _slideClip = state.name;
            }
            if (_runClip == null)
                foreach (AnimationState state in _animation)
                    if (state.name.ToLowerInvariant().Contains("walk")) { _runClip = state.name; break; }
        }

        private void Play(string clip, float speed = 1f)
        {
            if (_animation == null || clip == null) return;
            var state = _animation[clip];
            if (state != null) state.speed = speed;
            if (_current == clip) return;
            _animation.CrossFade(clip, 0.12f);
            _current = clip;
        }

        /// <summary>Wendet einen Outfit-Text an. Gibt kurze Rückmeldungen auf Deutsch zurück.</summary>
        public List<string> ApplyOutfit(string text)
        {
            var feedback = new List<string>();
            var parsed = OutfitTextParser.Parse(text);
            var matched = OutfitCatalog.CreateStarter().Match(parsed);

            if (!UsesModel)
            {
                bool hasDress = parsed.Find(OutfitSlot.Dress) != null;
                bool hasSeparates = parsed.Find(OutfitSlot.Top) != null || parsed.Find(OutfitSlot.Bottom) != null;
                bool useDress = hasDress || (!hasSeparates && _dress.activeSelf);
                _dress.SetActive(useDress);
                _torso.SetActive(!useDress);
                _pantsL.SetActive(!useDress);
                _pantsR.SetActive(!useDress);
                _cap.SetActive(false);
            }

            foreach (var choice in matched)
            {
                if (choice.Item == null) { feedback.Add("Nicht im Katalog: " + SlotName(choice.Wish.Slot)); continue; }
                bool colored = choice.Color != null && Palette.TryGetValue(choice.Color, out var color) && Recolor(choice.Wish.Slot, color);
                string note = choice.Quality == MatchQuality.Exact ? "" : " (Ersatz)";
                if (UsesModel && choice.Color != null && !colored) note = " (an diesem Modell nicht färbbar)";
                feedback.Add($"{SlotName(choice.Wish.Slot)}: {ColorName(choice.Color)}{note}");
            }
            foreach (var w in parsed.UnknownWords) if (feedback.Count < 6) feedback.Add("Unbekannt: " + w);
            return feedback;
        }

        private bool Recolor(OutfitSlot slot, Color color)
        {
            if (!UsesModel)
            {
                Material target = slot switch
                {
                    OutfitSlot.Hair => _hair,
                    OutfitSlot.Dress => _dressMat,
                    OutfitSlot.Top => _top,
                    OutfitSlot.Bottom => _pants,
                    OutfitSlot.Shoes => _shoes,
                    _ => _capMat
                };
                if (slot == OutfitSlot.Accessory) _cap.SetActive(true);
                SetColor(target, color);
                return true;
            }

            bool any = false;
            var keys = SlotKeywords[slot];
            foreach (var r in _modelRenderers)
            {
                string objName = r.name.ToLowerInvariant();
                foreach (var m in r.materials)
                {
                    string matName = m.name.ToLowerInvariant();
                    foreach (var key in keys)
                    {
                        if (!matName.Contains(key) && !objName.Contains(key)) continue;
                        SetColor(m, color);
                        any = true;
                        break;
                    }
                }
            }
            return any;
        }

        public void Animate(RunnerMotor motor, bool running, bool hovering, bool shield, float speed, float dt)
        {
            _phase += dt * (running ? speed * 0.9f : 2f);
            bool air = motor.IsJumping || motor.FeetY > 0.05f;

            if (UsesModel)
            {
                if (!running) Play(_idleClip ?? _runClip, 1f);
                else if (motor.IsSliding && _slideClip != null) Play(_slideClip, 1.2f);
                else if (air && !hovering && _jumpClip != null) Play(_jumpClip, 1.1f);
                else Play(hovering ? (_idleClip ?? _runClip) : _runClip, Mathf.Clamp(speed / 10f, 0.9f, 1.8f));
                bool fakeSlide = motor.IsSliding && _slideClip == null;
                Tilt(fakeSlide ? -70f : 0f, fakeSlide ? 0.25f : 0f, dt);
            }
            else
            {
                float swing = running && !air && !motor.IsSliding ? Mathf.Sin(_phase) * 50f : air ? 30f : 0f;
                _legL.localRotation = Quaternion.Euler(swing, 0, 0);
                _legR.localRotation = Quaternion.Euler(-swing, 0, 0);
                _armL.localRotation = Quaternion.Euler(-swing * 0.8f, 0, 0);
                _armR.localRotation = Quaternion.Euler(swing * 0.8f, 0, 0);
                _tail.localRotation = Quaternion.Euler(25f + Mathf.Sin(_phase * 2f) * 15f, 0, 0);
                float bob = running && !air && !motor.IsSliding ? Mathf.Abs(Mathf.Cos(_phase)) * 0.06f : 0f;
                Tilt(motor.IsSliding ? -70f : 0f, motor.IsSliding ? 0.25f : bob, dt);
            }

            _board.SetActive(hovering);
            if (hovering) _board.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 5f) * 5f);
            _shield.gameObject.SetActive(shield);
            if (shield) _shield.Rotate(0f, 120f * dt, 40f * dt, Space.Self);
        }

        private void Tilt(float targetTilt, float y, float dt)
        {
            var e = _body.localEulerAngles;
            float current = e.x > 180f ? e.x - 360f : e.x;
            _body.localRotation = Quaternion.Euler(Mathf.Lerp(current, targetTilt, Mathf.Min(1f, dt * 18f)), 0, 0);
            _body.localPosition = new Vector3(0, y, 0);
        }

        // ---------- Platzhalter-Figur (nur ohne echtes Modell) ----------

        private void BuildPlaceholder()
        {
            _skin = MaterialFactory.Create(Hex("#f2c2a0"), 0.35f);
            _hair = MaterialFactory.Create(Hex("#f5d36b"), 0.55f);
            _dressMat = MaterialFactory.DoubleSided(MaterialFactory.Create(Hex("#e8304a"), 0.45f));
            _top = MaterialFactory.Create(Hex("#f4f2ff"), 0.4f);
            _pants = MaterialFactory.Create(Hex("#3a6ee8"), 0.4f);
            _shoes = MaterialFactory.Create(Hex("#7a4a2a"), 0.5f);
            _capMat = MaterialFactory.Create(Hex("#3de8ff"), 0.6f);
            var eyeMat = MaterialFactory.Create(Hex("#1b1430"), 0.9f);
            var white = MaterialFactory.Create(Color.white, 0.9f, Color.white * 0.6f);
            var blush = MaterialFactory.Create(Hex("#ff9db8"), 0.2f);

            _legL = Pivot("LegL", new Vector3(-0.15f, 0.8f, 0f));
            _legR = Pivot("LegR", new Vector3(0.15f, 0.8f, 0f));
            foreach (var leg in new[] { _legL, _legR })
            {
                Part(PrimitiveType.Capsule, leg, _skin, new Vector3(0, -0.38f, 0), new Vector3(0.15f, 0.36f, 0.15f));
                Part(PrimitiveType.Cube, leg, _shoes, new Vector3(0, -0.76f, 0.05f), new Vector3(0.2f, 0.14f, 0.32f));
            }
            _pantsL = Part(PrimitiveType.Capsule, _legL, _pants, new Vector3(0, -0.18f, 0), new Vector3(0.2f, 0.26f, 0.2f));
            _pantsR = Part(PrimitiveType.Capsule, _legR, _pants, new Vector3(0, -0.18f, 0), new Vector3(0.2f, 0.26f, 0.2f));
            _torso = Part(PrimitiveType.Capsule, _body, _top, new Vector3(0, 1.12f, 0), new Vector3(0.52f, 0.36f, 0.42f));
            _dress = new GameObject("Dress");
            _dress.transform.SetParent(_body, false);
            _dress.transform.localPosition = new Vector3(0, 0.55f, 0);
            _dress.AddComponent<MeshFilter>().sharedMesh = MeshFactory.Cone(0.46f, 0.24f, 0.9f, 24);
            _dress.AddComponent<MeshRenderer>().sharedMaterial = _dressMat;
            _armL = Pivot("ArmL", new Vector3(-0.34f, 1.38f, 0f));
            _armR = Pivot("ArmR", new Vector3(0.34f, 1.38f, 0f));
            foreach (var arm in new[] { _armL, _armR })
                Part(PrimitiveType.Capsule, arm, _skin, new Vector3(0, -0.28f, 0), new Vector3(0.12f, 0.3f, 0.12f));
            Part(PrimitiveType.Sphere, _body, _skin, new Vector3(0, 1.86f, 0), Vector3.one * 0.68f);
            for (int side = -1; side <= 1; side += 2)
            {
                Part(PrimitiveType.Sphere, _body, eyeMat, new Vector3(side * 0.12f, 1.88f, 0.29f), new Vector3(0.11f, 0.15f, 0.06f));
                Part(PrimitiveType.Sphere, _body, white, new Vector3(side * 0.12f + 0.02f, 1.92f, 0.32f), Vector3.one * 0.035f);
                Part(PrimitiveType.Sphere, _body, blush, new Vector3(side * 0.19f, 1.79f, 0.27f), new Vector3(0.09f, 0.05f, 0.03f));
            }
            Part(PrimitiveType.Sphere, _body, _hair, new Vector3(0, 1.95f, -0.04f), new Vector3(0.74f, 0.6f, 0.72f));
            Part(PrimitiveType.Sphere, _body, _hair, new Vector3(0, 2.05f, 0.14f), new Vector3(0.62f, 0.22f, 0.4f));
            _tail = Pivot("Ponytail", new Vector3(0, 1.95f, -0.32f));
            Part(PrimitiveType.Capsule, _tail, _hair, new Vector3(0, -0.2f, -0.05f), new Vector3(0.16f, 0.24f, 0.16f));
            _cap = Part(PrimitiveType.Cylinder, _body, _capMat, new Vector3(0, 2.2f, 0), new Vector3(0.66f, 0.06f, 0.66f));
            _cap.SetActive(false);
        }

        private Transform Pivot(string name, Vector3 pos)
        {
            var t = new GameObject(name).transform;
            t.SetParent(_body, false);
            t.localPosition = pos;
            return t;
        }

        private static GameObject Part(PrimitiveType type, Transform parent, Material mat, Vector3 pos, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(type);
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            return go;
        }

        private static void SetColor(Material m, Color c)
        {
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        }

        private static string SlotName(OutfitSlot slot) => slot switch
        {
            OutfitSlot.Hair => "Haare",
            OutfitSlot.Dress => "Kleid",
            OutfitSlot.Top => "Oberteil",
            OutfitSlot.Bottom => "Hose/Rock",
            OutfitSlot.Shoes => "Schuhe",
            _ => "Mütze"
        };

        private static readonly Dictionary<string, string> GermanColors = new Dictionary<string, string>
        {
            { "red", "rot" }, { "blue", "blau" }, { "green", "grün" }, { "yellow", "gelb" }, { "black", "schwarz" },
            { "white", "weiß" }, { "brown", "braun" }, { "blonde", "blond" }, { "pink", "pink" }, { "purple", "lila" },
            { "orange", "orange" }, { "grey", "grau" }, { "silver", "silber" }, { "gold", "gold" }, { "beige", "beige" }
        };

        private static string ColorName(string id) => id != null && GermanColors.TryGetValue(id, out var de) ? de : "-";

        public static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;
    }

    /// <summary>Einfache prozedurale Meshes (Kegel, Ring), die Unity nicht als Grundform hat.</summary>
    public static class MeshFactory
    {
        public static Mesh Cone(float bottomRadius, float topRadius, float height, int segments)
        {
            var v = new List<Vector3>();
            var t = new List<int>();
            for (int i = 0; i <= segments; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2f;
                var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                v.Add(dir * bottomRadius);
                v.Add(dir * topRadius + Vector3.up * height);
            }
            for (int i = 0; i < segments; i++)
            {
                int b = i * 2;
                t.AddRange(new[] { b, b + 1, b + 2, b + 1, b + 3, b + 2 });
            }
            int center = v.Count;
            v.Add(Vector3.zero);
            for (int i = 0; i < segments; i++) t.AddRange(new[] { center, i * 2 + 2, i * 2 });
            var mesh = new Mesh { name = "Cone" };
            mesh.SetVertices(v);
            mesh.SetTriangles(t, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh Ring(float radius, float thickness, int segments)
        {
            var v = new List<Vector3>();
            var t = new List<int>();
            const int sides = 6;
            for (int i = 0; i <= segments; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2f;
                var center = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * radius;
                var outward = center.normalized;
                for (int j = 0; j <= sides; j++)
                {
                    float b = j / (float)sides * Mathf.PI * 2f;
                    v.Add(center + (outward * Mathf.Cos(b) + Vector3.up * Mathf.Sin(b)) * thickness);
                }
            }
            for (int i = 0; i < segments; i++)
                for (int j = 0; j < sides; j++)
                {
                    int a0 = i * (sides + 1) + j, b0 = (i + 1) * (sides + 1) + j;
                    t.AddRange(new[] { a0, b0, a0 + 1, a0 + 1, b0, b0 + 1 });
                }
            var mesh = new Mesh { name = "Ring" };
            mesh.SetVertices(v);
            mesh.SetTriangles(t, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
