using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using WorldRush.Core;
using WorldRush.Runtime;

namespace WorldRush.Tests
{
    /// <summary>Startet das echte Spielobjekt in Unity und prüft den Ablauf Start → Laufen → Pause → Game Over → Neustart.</summary>
    public class RunnerSmokeTests
    {
        private GameObject _go;

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.Destroy(_go);
        }

        [UnityTest]
        public IEnumerator RunnerStartsMovesPausesAndRestarts()
        {
            _go = new GameObject("TestRoot");
            var root = _go.AddComponent<GameRoot>();
            yield return null;

            Assert.AreEqual(GameState.Ready, root.Session.State);
            root.StartRun();
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(GameState.Running, root.Session.State);
            Assert.Greater(root.Session.PlayerZ, 1f);

            root.TogglePause();
            float z = root.Session.PlayerZ;
            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(z, root.Session.PlayerZ);
            root.TogglePause();

            // Ohne Eingabe läuft die Figur irgendwann gegen ein Hindernis.
            float timeout = Time.time + 60f;
            while (root.Session.State == GameState.Running && Time.time < timeout) yield return null;
            Assert.AreEqual(GameState.GameOver, root.Session.State);

            root.Restart();
            yield return null;
            Assert.AreEqual(GameState.Running, root.Session.State);
            Assert.Less(root.Session.PlayerZ, 2f);
        }
    }
}
