using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using WorldRush.Core;

namespace WorldRush.EditorTools
{
    /// <summary>
    /// Store-Vorbereitung: App-Icon und Store-Grafiken erzeugen (eigenes Motiv, per Code gezeichnet),
    /// Android-Einstellungen für Google Play setzen und ein signiertes .aab bauen.
    /// </summary>
    public static class StoreBuild
    {
        private const string IconPath = "Assets/_WorldRush/Art/AppIcon.png";
        private const string StoreFolder = "StoreAssets";
        public const string PackageId = "com.fersasurfer.game";

        [MenuItem("Fersa Surfer/6. Store-Einstellungen + App-Icon (Android)", priority = 20)]
        public static void PrepareStore()
        {
            // 1) Icon + Store-Grafiken
            Directory.CreateDirectory(Path.GetDirectoryName(IconPath));
            File.WriteAllBytes(IconPath, Paint(1024, 1024, true).EncodeToPNG());
            Directory.CreateDirectory(StoreFolder);
            File.WriteAllBytes(Path.Combine(StoreFolder, "icon-512.png"), Paint(512, 512, true).EncodeToPNG());
            File.WriteAllBytes(Path.Combine(StoreFolder, "feature-graphic-1024x500.png"), Paint(1024, 500, false).EncodeToPNG());
            AssetDatabase.ImportAsset(IconPath, ImportAssetOptions.ForceUpdate);
            if (AssetImporter.GetAtPath(IconPath) is TextureImporter ti)
            {
                ti.textureType = TextureImporterType.Default;
                ti.mipmapEnabled = false;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                ti.SaveAndReimport();
            }
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);

            // 2) Allgemeine Angaben
            PlayerSettings.companyName = "FersaSurfer";
            PlayerSettings.productName = ProductInfo.DisplayName;
            PlayerSettings.bundleVersion = ProductInfo.Version;
            if (icon != null) PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
            PlayerSettings.SplashScreen.show = false;

            // 3) Querformat (Runner)
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

            // 4) Android für Google Play: Paketname, Versionsnummer, IL2CPP, ARM64, App Bundle
            var android = NamedBuildTarget.Android;
            PlayerSettings.SetApplicationIdentifier(android, PackageId);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, PackageId);
            PlayerSettings.Android.bundleVersionCode = VersionCode(ProductInfo.Version);
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetManagedStrippingLevel(android, ManagedStrippingLevel.Medium);
            PlayerSettings.insecureHttpOption = InsecureHttpOption.NotAllowed;
            EditorUserBuildSettings.buildAppBundle = true;
            AssetDatabase.SaveAssets();

            string msg = $"Fertig:\n• App-Icon erstellt ({IconPath})\n• Store-Grafiken im Ordner '{StoreFolder}' (Icon 512, Titelbild 1024×500)\n" +
                         $"• Paketname {PackageId}, Version {ProductInfo.Version} (Code {VersionCode(ProductInfo.Version)})\n• Querformat, IL2CPP, ARM64, App Bundle (.aab)\n\n" +
                         "Nächster Schritt: Menü 'Fersa Surfer → 7. Android-AAB bauen'.";
            Debug.Log("[FersaSurfer] " + msg.Replace("\n", " | "));
            EditorUtility.DisplayDialog("Fersa Surfer – Store", msg, "OK");
        }

        [MenuItem("Fersa Surfer/7. Android-AAB bauen", priority = 21)]
        public static void BuildAab()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                EditorUtility.DisplayDialog("Fersa Surfer", "Bitte zuerst auf Android umstellen:\nFile → Build Profiles → Android → 'Switch Platform'.\n\n(Falls Android fehlt: im Unity Hub bei Unity 6.6 das Modul 'Android Build Support' hinzufügen.)", "OK");
                return;
            }
            if (!PlayerSettings.Android.useCustomKeystore || string.IsNullOrEmpty(PlayerSettings.Android.keystoreName) || !File.Exists(PlayerSettings.Android.keystoreName))
            {
                EditorUtility.DisplayDialog("Fersa Surfer – Signatur fehlt",
                    "Google Play braucht einen eigenen Signatur-Schlüssel (Keystore):\n\n" +
                    "Edit → Project Settings → Player → Android → Publishing Settings → 'Keystore Manager' → neuen Keystore anlegen, Passwort merken, 'Add key'.\n\n" +
                    "WICHTIG: Keystore-Datei + Passwort sicher aufbewahren (z. B. USB-Stick + Passwort-Manager) und NIE auf GitHub hochladen. Ohne ihn kannst du keine Updates mehr veröffentlichen.", "OK");
                return;
            }
            var scenes = new System.Collections.Generic.List<string>();
            foreach (var sc in EditorBuildSettings.scenes) if (sc.enabled) scenes.Add(sc.path);
            if (scenes.Count == 0) { FersaSurferSetupMenu.CreateDemoScene(); scenes.Add("Assets/_WorldRush/Scenes/RunnerDemo.unity"); }

            EditorUserBuildSettings.buildAppBundle = true;
            Directory.CreateDirectory("Builds/Android");
            string path = $"Builds/Android/FersaSurfer-{ProductInfo.Version}.aab";
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = scenes.ToArray(), locationPathName = path, target = BuildTarget.Android, options = BuildOptions.None });
            bool ok = report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded;
            string text = ok
                ? $"Gebaut: {path}\nGröße: {report.summary.totalSize / (1024 * 1024)} MB\n\nDiese Datei lädst du in der Google Play Console hoch (Test → Geschlossener Test)."
                : $"Build fehlgeschlagen ({report.summary.totalErrors} Fehler). Details in der Console.";
            Debug.Log("[FersaSurfer] " + text.Replace("\n", " | "));
            EditorUtility.DisplayDialog("Fersa Surfer – Android", text, "OK");
            if (ok) EditorUtility.RevealInFinder(path);
        }

        /// <summary>Versionscode aus "Major.Minor.Patch", z. B. 0.3.0 → 300, 1.2.5 → 10205.</summary>
        public static int VersionCode(string version)
        {
            var parts = version.Split('.');
            int Get(int i) => i < parts.Length && int.TryParse(parts[i], out int v) ? v : 0;
            return System.Math.Max(1, Get(0) * 10000 + Get(1) * 100 + Get(2));
        }

        // ---------- Eigenes Motiv: Abendhimmel, Retro-Sonne, Welle, Surfbrett ----------

        private static Texture2D Paint(int w, int h, bool square)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color[w * h];
            Color top = Hex("#1d0b4a"), mid = Hex("#7a2fb0"), bottom = Hex("#ff6fa8");
            Color sun = Hex("#ffd23f"), sunLow = Hex("#ff8a3d"), wave = Hex("#2fd7ff"), waveDark = Hex("#1677d6"), foam = Hex("#f2fdff");
            Color board = Hex("#ffe066"), stripe = Hex("#ff4fa3");
            float sunX = square ? 0.5f : 0.68f, sunY = 0.6f, sunR = square ? 0.27f : 0.3f * h / w;

            for (int y = 0; y < h; y++)
            {
                float v = y / (float)(h - 1);
                Color sky = v > 0.5f ? Color.Lerp(mid, top, (v - 0.5f) * 2f) : Color.Lerp(bottom, mid, v * 2f);
                for (int x = 0; x < w; x++)
                {
                    float u = x / (float)(w - 1);
                    Color c = sky;

                    // Retro-Sonne mit Streifen im unteren Teil
                    float du = (u - sunX) * w / h, dv = v - sunY;
                    float d = Mathf.Sqrt(du * du + dv * dv);
                    if (d < sunR)
                    {
                        bool gap = dv < 0f && Mathf.Repeat((sunY - v) * 28f, 1f) < 0.32f + (-dv / sunR) * 0.35f;
                        if (!gap) c = Color.Lerp(sunLow, sun, Mathf.Clamp01((dv / sunR + 1f) * 0.5f));
                    }
                    else if (d < sunR * 1.25f) c = Color.Lerp(c, sun, (1f - (d - sunR) / (sunR * 0.25f)) * 0.25f);

                    // Welle (zwei Ebenen) mit Schaumkante
                    float crest = 0.36f + 0.07f * Mathf.Sin(u * Mathf.PI * 2.2f + 0.6f) + 0.03f * Mathf.Sin(u * Mathf.PI * 7f);
                    if (v < crest)
                    {
                        c = Color.Lerp(waveDark, wave, Mathf.Clamp01(v / crest));
                        if (crest - v < 0.022f) c = foam;
                        float back = 0.2f + 0.05f * Mathf.Sin(u * Mathf.PI * 3.1f + 2f);
                        if (v < back) c = Color.Lerp(waveDark * 0.8f, waveDark, v / back);
                    }

                    // Surfbrett: gekippte Ellipse auf der Welle
                    float bx = square ? 0.5f : 0.42f, by = 0.4f;
                    float rx = (u - bx) * w / h, ry = v - by;
                    float ang = 0.35f, cs = Mathf.Cos(ang), sn = Mathf.Sin(ang);
                    float ex = rx * cs + ry * sn, ey = -rx * sn + ry * cs;
                    float e = (ex * ex) / (0.24f * 0.24f) + (ey * ey) / (0.05f * 0.05f);
                    if (e < 1f)
                    {
                        c = board;
                        if (Mathf.Abs(ey) < 0.009f) c = stripe;
                        if (e > 0.82f) c = Color.Lerp(board, Hex("#c98a00"), 0.6f);
                    }
                    px[y * w + x] = c;
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        private static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;
    }
}
