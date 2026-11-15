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
        public const string PackageId = "com.fesara.game";

        [MenuItem("Fesara/6. Store-Einstellungen + App-Icon (Android)", priority = 20)]
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
            PlayerSettings.companyName = "Fesara";
            PlayerSettings.productName = ProductInfo.DisplayName;
            PlayerSettings.bundleVersion = ProductInfo.Version;
            if (icon != null) PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
            PlayerSettings.SplashScreen.show = false;
            ShaderIncludes.EnsureAll();

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
                         "Nächster Schritt: Menü 'Fesara → 7. Android-AAB bauen'.";
            Debug.Log("[Fesara] " + msg.Replace("\n", " | "));
            EditorUtility.DisplayDialog("Fesara – Store", msg, "OK");
        }

        [MenuItem("Fesara/7. Android-AAB bauen", priority = 21)]
        public static void BuildAab()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                EditorUtility.DisplayDialog("Fesara", "Bitte zuerst auf Android umstellen:\nFile → Build Profiles → Android → 'Switch Platform'.\n\n(Falls Android fehlt: im Unity Hub bei Unity 6.6 das Modul 'Android Build Support' hinzufügen.)", "OK");
                return;
            }
            if (!PlayerSettings.Android.useCustomKeystore || string.IsNullOrEmpty(PlayerSettings.Android.keystoreName) || !File.Exists(PlayerSettings.Android.keystoreName))
            {
                EditorUtility.DisplayDialog("Fesara – Signatur fehlt",
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
            string path = $"Builds/Android/Fesara-{ProductInfo.Version}.aab";
            ShaderIncludes.EnsureAll();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = scenes.ToArray(), locationPathName = path, target = BuildTarget.Android, options = BuildOptions.None });
            bool ok = report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded;
            string text = ok
                ? $"Gebaut: {path}\nGröße: {report.summary.totalSize / (1024 * 1024)} MB\n\nDiese Datei lädst du in der Google Play Console hoch (Test → Geschlossener Test)."
                : $"Build fehlgeschlagen ({report.summary.totalErrors} Fehler). Details in der Console.";
            Debug.Log("[Fesara] " + text.Replace("\n", " | "));
            EditorUtility.DisplayDialog("Fesara – Android", text, "OK");
            if (ok) EditorUtility.RevealInFinder(path);
        }

        [MenuItem("Fesara/8. Web-Version bauen (für iPad/iPhone im Browser)", priority = 22)]
        public static void BuildWeb()
        {
            var scenes = new System.Collections.Generic.List<string>();
            foreach (var sc in EditorBuildSettings.scenes) if (sc.enabled) scenes.Add(sc.path);
            if (scenes.Count == 0) { FersaSurferSetupMenu.CreateDemoScene(); scenes.Add("Assets/_WorldRush/Scenes/RunnerDemo.unity"); }

            PlayerSettings.productName = ProductInfo.DisplayName;
            PlayerSettings.bundleVersion = ProductInfo.Version;
            // Gzip + Fallback: läuft auf itch.io und jedem einfachen Webserver ohne Sonder-Einstellungen
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;

            string dir = "Builds/Web";
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
            Directory.CreateDirectory(dir);
            ShaderIncludes.EnsureAll();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = scenes.ToArray(), locationPathName = dir, target = BuildTarget.WebGL, options = BuildOptions.None });
            bool ok = report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded;
            string zip = $"Builds/Fesara-Web-{ProductInfo.Version}.zip";
            if (ok)
            {
                if (File.Exists(zip)) File.Delete(zip);
                System.IO.Compression.ZipFile.CreateFromDirectory(dir, zip);
            }
            string text = ok
                ? $"Fertig: {zip}\n\nAuf itch.io hochladen:\n1. itch.io → 'Upload new project'\n2. Kind of project: HTML\n3. ZIP hochladen, 'This file will be played in the browser' anhaken\n4. Sichtbarkeit: 'Draft' oder 'Restricted' (mit Passwort)\n5. Link auf iPad/iPhone in Safari öffnen."
                : $"Build fehlgeschlagen ({report.summary.totalErrors} Fehler). Falls 'Web' fehlt: Unity Hub → Installationen → Unity 6.6 → Module hinzufügen → 'Web Build Support'.";
            Debug.Log("[Fesara] " + text.Replace("\n", " | "));
            EditorUtility.DisplayDialog("Fesara – Web", text, "OK");
            if (ok) EditorUtility.RevealInFinder(zip);
        }

        /// <summary>Versionscode aus "Major.Minor.Patch", z. B. 0.3.0 → 300, 1.2.5 → 10205.</summary>
        public static int VersionCode(string version)
        {
            var parts = version.Split('.');
            int Get(int i) => i < parts.Length && int.TryParse(parts[i], out int v) ? v : 0;
            return System.Math.Max(1, Get(0) * 10000 + Get(1) * 100 + Get(2));
        }

        // ---------- Eigenes Motiv: Fesara-Logo "Platsch-Welle" (gezeichnet in Core/Branding/IconPainter) ----------

        private static Texture2D Paint(int w, int h, bool square)
        {
            byte[] rgba = WorldRush.Core.Branding.IconPainter.Paint(w, h, square);
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                int src = (h - 1 - y) * w; // Unity-Texturen beginnen unten, das Bild oben
                for (int x = 0; x < w; x++)
                {
                    int o = (src + x) * 4;
                    px[y * w + x] = new Color32(rgba[o], rgba[o + 1], rgba[o + 2], rgba[o + 3]);
                }
            }
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

    }
}
