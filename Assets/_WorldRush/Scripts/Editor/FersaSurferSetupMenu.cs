using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using WorldRush.Core;
using WorldRush.Runtime;

namespace WorldRush.EditorTools
{
    /// <summary>
    /// Menü "Fersa Surfer → Demo-Szene erstellen": Unity selbst erzeugt Szene und Materialien
    /// (keine handgeschriebenen .unity/.mat-Dateien), trägt die Szene in die Build-Liste ein
    /// und setzt Produktname und sichere Build-Einstellungen.
    /// </summary>
    public static class FersaSurferSetupMenu
    {
        private const string Root = "Assets/_WorldRush";
        private const string ScenePath = Root + "/Scenes/RunnerDemo.unity";
        private const string MaterialDir = Root + "/Art/Materials";

        [MenuItem("Fersa Surfer/1. Demo-Szene erstellen", priority = 1)]
        public static void CreateDemoScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            Directory.CreateDirectory(MaterialDir);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject("FersaSurfer");
            var root = go.AddComponent<GameRoot>();
            root.groundMaterial = Mat("Ground", new Color(0.22f, 0.22f, 0.32f), 0.4f);
            root.laneLineMaterial = Mat("LaneLine", new Color(0.9f, 0.95f, 1f), 0.2f, new Color(0.4f, 0.6f, 1f));
            root.playerMaterial = Mat("Player", new Color(1f, 0.45f, 0.35f), 0.6f);
            root.hurdleMaterial = Mat("Hurdle", new Color(1f, 0.8f, 0.2f), 0.5f);
            root.overheadMaterial = Mat("Overhead", new Color(0.3f, 0.85f, 1f), 0.5f);
            root.blockMaterial = Mat("Block", new Color(0.95f, 0.3f, 0.55f), 0.5f);
            root.coinMaterial = Mat("Coin", new Color(1f, 0.85f, 0.25f), 0.9f, new Color(0.6f, 0.45f, 0f));
            root.buildingMaterial = Mat("Building", Color.white, 0.3f);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuild(ScenePath);
            ApplyPlayerSettings();
            AssetDatabase.SaveAssets();
            Debug.Log($"[FersaSurfer] Szene erstellt: {ScenePath}. Jetzt oben auf ▶ (Play) drücken.");
        }

        [MenuItem("Fersa Surfer/2. Sichere Build-Einstellungen setzen", priority = 2)]
        public static void ApplyPlayerSettings()
        {
            PlayerSettings.productName = ProductInfo.DisplayName;
            PlayerSettings.bundleVersion = ProductInfo.Version;

            // IL2CPP statt Mono: nativer Code, schwerer zu manipulieren und schneller.
            // Hinweis: benötigt die IL2CPP-Module im Unity Hub.
            foreach (var target in new[] { BuildTargetGroup.Android, BuildTargetGroup.iOS, BuildTargetGroup.Standalone })
            {
                PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(target), ScriptingImplementation.IL2CPP);
                PlayerSettings.SetManagedStrippingLevel(UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(target), ManagedStrippingLevel.Medium);
            }
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.insecureHttpOption = InsecureHttpOption.NotAllowed;
            Debug.Log("[FersaSurfer] Build-Einstellungen gesetzt: IL2CPP, Code-Stripping, nur HTTPS, Android ARM64.");
        }

        [MenuItem("Fersa Surfer/3. 3D-Figuren neu importieren", priority = 3)]
        public static void ReimportCharacters()
        {
            int n = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { Root + "/Resources/Characters" }))
            {
                AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceUpdate);
                n++;
            }
            Debug.Log($"[FersaSurfer] {n} Figur(en) neu importiert (Animationen als Legacy). Jetzt ▶ drücken.");
        }

        [MenuItem("Fersa Surfer/4. Figuren übernehmen (HAON & Co.)", priority = 4)]
        public static void UseCharacters()
        {
            if (EditorApplication.isPlaying) { EditorUtility.DisplayDialog("Fersa Surfer", "Bitte zuerst das Spiel stoppen (■).", "OK"); return; }

            // 1) Ausgewählte Figur/Ordner, sonst 2) automatisch alle Figuren-Prefabs aus Paketen mit "haon"/"character"/"chara" im Pfad
            var candidates = new List<string>();
            string sel = Selection.activeObject != null ? AssetDatabase.GetAssetPath(Selection.activeObject) : null;
            if (!string.IsNullOrEmpty(sel) && !sel.StartsWith(Root))
            {
                if (AssetDatabase.IsValidFolder(sel)) foreach (var g in AssetDatabase.FindAssets("t:Prefab", new[] { sel })) candidates.Add(AssetDatabase.GUIDToAssetPath(g));
                else candidates.Add(sel);
            }
            if (candidates.Count == 0)
                foreach (var g in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(g), lower = path.ToLowerInvariant();
                    if (path.StartsWith(Root)) continue;
                    if (lower.Contains("haon") || lower.Contains("/character") || lower.Contains("chara")) candidates.Add(path);
                }

            const string folder = Root + "/Resources/Characters/Selected";
            if (AssetDatabase.IsValidFolder(folder)) AssetDatabase.DeleteAsset(folder);
            AssetDatabase.CreateFolder(Root + "/Resources/Characters", "Selected");

            int n = 0;
            string first = null;
            foreach (var path in candidates)
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go == null || go.GetComponentInChildren<SkinnedMeshRenderer>(true) == null) continue; // nur echte Figuren
                if (go.GetComponentInChildren<Animator>(true) == null && go.GetComponentInChildren<Animation>(true) == null) continue; // ganze, animierbare Figur
                string lowerName = go.name.ToLowerInvariant();
                if (Has(lowerName, PartWords)) continue; // keine Einzelteile (Haare, Gesicht, Accessoires …)
                if (!AssetDatabase.CopyAsset(path, folder + "/" + System.IO.Path.GetFileName(path))) continue;
                first ??= go.name;
                n++;
                if (n >= 30) break;
            }
            AssetDatabase.Refresh();
            if (n == 0)
            {
                EditorUtility.DisplayDialog("Fersa Surfer", "Keine Figuren gefunden. Bitte zuerst 'HAON SD series Free bundle' über Window → Package Manager → My Assets importieren.", "OK");
                return;
            }
            UnityEngine.PlayerPrefs.SetString("fs-character", first);
            Debug.Log($"[FersaSurfer] {n} Figur(en) übernommen. Im Startmenü mit < > auswählen.");
            EditorUtility.DisplayDialog("Fersa Surfer", $"{n} Figur(en) übernommen.\nIm Startmenü mit < > auswählen, dann ▶.", "OK");
        }

        private static readonly string[] PartWords = { "acc", "hair", "face", "prop", "guide", "preview", "dummy", "weapon", "costume0", "blendshape" };
        private static readonly string[] BuildingWords = { "building", "house", "shop", "store", "tower", "apartment", "office", "bank", "hospital", "school", "cafe", "restaurant", "hotel", "market", "skyscraper", "home", "station", "bakery", "cinema", "mall" };
        private static readonly string[] CarWords = { "car", "taxi", "bus", "truck", "van", "police", "ambulance", "vehicle", "suv", "pickup", "firetruck" };
        private static readonly string[] SkipWords = { "road", "street", "crossing", "sidewalk", "terrain", "ground", "tile", "floor", "plane", "base", "demo", "scene", "camera", "light probe" };

        [MenuItem("Fersa Surfer/5. Ausgewählten Stadt-Ordner verwenden", priority = 5)]
        public static void UseSelectedCityFolder()
        {
            if (EditorApplication.isPlaying) { EditorUtility.DisplayDialog("Fersa Surfer", "Bitte zuerst das Spiel stoppen (■).", "OK"); return; }
            string folder = Selection.activeObject != null ? AssetDatabase.GetAssetPath(Selection.activeObject) : null;
            if (!string.IsNullOrEmpty(folder) && !AssetDatabase.IsValidFolder(folder)) folder = System.IO.Path.GetDirectoryName(folder)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(folder) || !AssetDatabase.IsValidFolder(folder) || folder.StartsWith(Root)) folder = FindCityFolder();
            if (folder == null)
            {
                EditorUtility.DisplayDialog("Fersa Surfer", "Kein Stadt-Paket gefunden. Bitte zuerst 'Cartoon City' über Window → Package Manager → My Assets importieren.", "OK");
                return;
            }
            Debug.Log("[FersaSurfer] Verwende Stadt-Ordner: " + folder);
            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });
            if (guids.Length == 0) guids = AssetDatabase.FindAssets("t:Model", new[] { folder });

            const string env = Root + "/Resources/Environment";
            foreach (var sub in new[] { "Buildings", "Props", "Cars" })
            {
                string dir = env + "/" + sub;
                if (AssetDatabase.IsValidFolder(dir)) AssetDatabase.DeleteAsset(dir); // alte Modelle (z. B. Quaternius) ersetzen
                AssetDatabase.CreateFolder(env, sub);
            }

            int b = 0, c = 0, p = 0, skipped = 0;
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string name = System.IO.Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                if (Has(name, SkipWords)) { skipped++; continue; }
                string sub = Has(name, CarWords) ? "Cars" : Has(name, BuildingWords) ? "Buildings" : "Props";
                if (!AssetDatabase.CopyAsset(path, $"{env}/{sub}/{System.IO.Path.GetFileName(path)}")) continue;
                if (sub == "Cars") c++; else if (sub == "Buildings") b++; else p++;
            }
            AssetDatabase.Refresh();
            Debug.Log($"[FersaSurfer] Stadt übernommen: {b} Gebäude, {c} Autos, {p} Deko ({skipped} Straßen-/Bodenteile übersprungen). ▶ drücken.");
            EditorUtility.DisplayDialog("Fersa Surfer", $"Übernommen: {b} Gebäude, {c} Autos, {p} Deko.\nJetzt ▶ drücken.", "OK");
        }

/// <summary>Sucht den Ordner eines importierten Stadt-Pakets (Name enthält "city"/"town"), bevorzugt dessen Prefabs-Ordner.</summary>
        private static string FindCityFolder()
        {
            string best = null;
            foreach (var guid in AssetDatabase.FindAssets("t:Folder", new[] { "Assets" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.StartsWith(Root)) continue;
                string lower = path.ToLowerInvariant();
                if (!lower.Contains("city") && !lower.Contains("town")) continue;
                if (AssetDatabase.FindAssets("t:Prefab", new[] { path }).Length == 0) continue;
                if (lower.EndsWith("/prefabs")) return path;
                if (best == null || path.Length < best.Length) best = path;
            }
            return best;
        }

                private static bool Has(string name, string[] words)
        {
            foreach (var w in words) if (name.Contains(w)) return true;
            return false;
        }

        private static Material Mat(string name, Color color, float smoothness, Color? emission = null)
        {
            string path = $"{MaterialDir}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            var m = MaterialFactory.Create(color, smoothness, emission);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        private static void AddSceneToBuild(string path)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (scenes.Exists(s => s.path == path)) return;
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
