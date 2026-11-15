using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using WorldRush.Core;

namespace WorldRush.EditorTools
{
    /// <summary>
    /// Automatischer Build für GitHub Actions (GameCI): erzeugt die Demo-Szene, setzt Einstellungen und baut.
    /// Aufruf: -executeMethod WorldRush.EditorTools.CiBuild.Build -buildTarget X -customBuildPath Y
    /// </summary>
    public static class CiBuild
    {
        public static void Build()
        {
            string path = Arg("-customBuildPath") ?? "Build/Fesara";
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            var group = BuildPipeline.GetBuildTargetGroup(target);
            var named = NamedBuildTarget.FromBuildTargetGroup(group);

            FersaSurferSetupMenu.CreateDemoScene();
            PlayerSettings.productName = ProductInfo.DisplayName;
            PlayerSettings.bundleVersion = ProductInfo.Version;
            PlayerSettings.companyName = "Fesara";

            // Linux-Build-Server können Windows nur mit Mono bauen; Android/WebGL nutzen IL2CPP.
            var backend = target == BuildTarget.StandaloneWindows64 || target == BuildTarget.StandaloneLinux64
                ? ScriptingImplementation.Mono2x
                : ScriptingImplementation.IL2CPP;
            if (target != BuildTarget.WebGL) PlayerSettings.SetScriptingBackend(named, backend);

            if (target == BuildTarget.Android)
            {
                PlayerSettings.SetApplicationIdentifier(named, StoreBuild.PackageId);
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
                PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
                EditorUserBuildSettings.buildAppBundle = false; // APK zum direkten Installieren
                if (!path.EndsWith(".apk")) path += ".apk";
            }
            else if (target == BuildTarget.iOS)
            {
                // iPhone/iPad: Unity erzeugt ein Xcode-Projekt; signiert und hochgeladen wird auf dem Mac-Server (siehe .github/workflows/ios.yml).
                PlayerSettings.SetApplicationIdentifier(named, StoreBuild.PackageId);
                PlayerSettings.iOS.targetOSVersionString = "15.0";
                PlayerSettings.iOS.appleEnableAutomaticSigning = true;
                string team = Environment.GetEnvironmentVariable("APPLE_TEAM_ID");
                if (!string.IsNullOrEmpty(team)) PlayerSettings.iOS.appleDeveloperTeamID = team;
                string run = Environment.GetEnvironmentVariable("BUILD_NUMBER") ?? Environment.GetEnvironmentVariable("GITHUB_RUN_NUMBER");
                PlayerSettings.iOS.buildNumber = string.IsNullOrEmpty(run) ? "1" : run; // muss bei jedem Upload steigen
                PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
                PlayerSettings.allowedAutorotateToLandscapeLeft = true;
                PlayerSettings.allowedAutorotateToLandscapeRight = true;
                PlayerSettings.allowedAutorotateToPortrait = false;
                PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            }
            else if (target == BuildTarget.StandaloneWindows64 && !path.EndsWith(".exe"))
            {
                path += "/Fesara.exe";
            }

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/_WorldRush/Scenes/RunnerDemo.unity" },
                locationPathName = path,
                target = target,
                options = BuildOptions.None
            });

            Debug.Log($"[Fesara] Build {target}: {report.summary.result}, {report.summary.totalSize / (1024 * 1024)} MB, {report.summary.totalErrors} Fehler");
            if (Application.isBatchMode) EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }

        private static string Arg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == name) return args[i + 1];
            return null;
        }
    }
}
