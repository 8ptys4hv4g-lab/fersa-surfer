using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace WorldRush.EditorTools
{
    /// <summary>
    /// Holt neue Versionen automatisch von GitHub, damit niemand mehr PowerShell-Befehle eingeben muss.
    /// Alle 2 Minuten (nicht während ▶ läuft): git fetch → wenn es etwas Neues gibt: git reset --hard origin/main → Unity lädt neu.
    /// Nur Dateien, die im Repository liegen, werden ersetzt. Eigene Modelle (Figur, Stadt) und Unity-Einstellungen bleiben unberührt.
    /// Abschaltbar über Menü „Fesara → Automatisch aktualisieren“.
    /// </summary>
    [InitializeOnLoad]
    public static class AutoUpdate
    {
        private const string PrefKey = "Fesara.AutoUpdate";
        private const string ToggleMenu = "Fesara/Automatisch aktualisieren";
        private const double IntervalSeconds = 120;

        private enum Outcome { None, Updated, Failed, Silent, AlreadyCurrent }

        private static double _next;
        private static volatile bool _busy;
        private static volatile Outcome _outcome = Outcome.None;
        private static string _message = "";
        private static bool _warnedOnce;

        static AutoUpdate()
        {
            _next = EditorApplication.timeSinceStartup + 8;
            EditorApplication.update += Tick;
        }

        private static bool Enabled => EditorPrefs.GetBool(PrefKey, true);

        [MenuItem("Fesara/0. Neueste Version jetzt holen", priority = 0)]
        private static void UpdateNow()
        {
            if (_busy) return;
            if (EditorApplication.isPlaying) { EditorUtility.DisplayDialog("Fesara", "Bitte zuerst ▶ beenden.", "OK"); return; }
            Begin(true);
        }

        [MenuItem(ToggleMenu, priority = 100)]
        private static void Toggle()
        {
            EditorPrefs.SetBool(PrefKey, !Enabled);
            Debug.Log("[Fesara] Automatisch aktualisieren: " + (Enabled ? "an" : "aus"));
        }

        [MenuItem(ToggleMenu, true)]
        private static bool ToggleValidate()
        {
            Menu.SetChecked(ToggleMenu, Enabled);
            return true;
        }

        private static void Tick()
        {
            var outcome = _outcome;
            if (outcome != Outcome.None)
            {
                _outcome = Outcome.None;
                Report(outcome);
            }
            if (_busy || !Enabled) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + IntervalSeconds;
            Begin(false);
        }

        private static void Report(Outcome outcome)
        {
            switch (outcome)
            {
                case Outcome.Updated:
                    AssetDatabase.Refresh();
                    Debug.Log("[Fesara] Neue Version geladen: " + _message + " – einfach ▶ drücken.");
                    Notify("Fesara: neue Version geladen");
                    break;
                case Outcome.AlreadyCurrent:
                    EditorUtility.DisplayDialog("Fesara", "Du hast schon die neueste Version.", "OK");
                    break;
                case Outcome.Failed:
                    if (!_warnedOnce) Debug.LogWarning("[Fesara] Automatisches Aktualisieren ging nicht: " + _message);
                    _warnedOnce = true;
                    break;
            }
        }

        private static void Notify(string text)
        {
            foreach (var w in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                string n = w.GetType().Name;
                if (n == "GameView" || n == "SceneView") w.ShowNotification(new GUIContent(text));
            }
        }

        private static void Begin(bool manual)
        {
            _busy = true;
            string root = Directory.GetCurrentDirectory();
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    if (!Directory.Exists(Path.Combine(root, ".git"))) { Finish(Outcome.Failed, "Projektordner ist kein Git-Klon."); return; }
                    if (Git(root, "fetch --quiet origin main", out string err) != 0) { Finish(manual ? Outcome.Failed : Outcome.Silent, "Keine Verbindung zu GitHub (" + err.Trim() + ")"); return; }
                    Git(root, "rev-parse HEAD", out string head);
                    Git(root, "rev-parse origin/main", out string remote);
                    head = head.Trim(); remote = remote.Trim();
                    if (head.Length == 0 || remote.Length == 0) { Finish(Outcome.Failed, "Git-Stand nicht lesbar."); return; }
                    if (head == remote) { Finish(manual ? Outcome.AlreadyCurrent : Outcome.Silent, ""); return; }
                    // Nur vorwärts gehen – niemals neuere eigene Stände überschreiben.
                    if (Git(root, $"merge-base --is-ancestor {head} {remote}", out string _) != 0) { Finish(Outcome.Failed, "Lokaler Stand ist nicht älter als GitHub – nichts überschrieben."); return; }
                    if (Git(root, "reset --hard --quiet origin/main", out string resetOut) != 0) { Finish(Outcome.Failed, resetOut.Trim()); return; }
                    Git(root, "log -1 --format=%s", out string subject);
                    Finish(Outcome.Updated, subject.Trim());
                }
                catch (Exception e)
                {
                    Finish(Outcome.Failed, e.Message);
                }
            });
        }

        private static void Finish(Outcome outcome, string message)
        {
            _message = message;
            _outcome = outcome;
            _busy = false;
        }

        private static int Git(string root, string args, out string output)
        {
            var psi = new ProcessStartInfo("git", args)
            {
                WorkingDirectory = root,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            psi.EnvironmentVariables["GIT_TERMINAL_PROMPT"] = "0";
            using (var p = Process.Start(psi))
            {
                if (p == null) { output = "git nicht gefunden"; return -1; }
                string stdout = p.StandardOutput.ReadToEnd();
                string stderr = p.StandardError.ReadToEnd();
                if (!p.WaitForExit(90000)) { try { p.Kill(); } catch { } output = "Zeitüberschreitung"; return -1; }
                output = stdout.Length > 0 ? stdout : stderr;
                return p.ExitCode;
            }
        }
    }
}
