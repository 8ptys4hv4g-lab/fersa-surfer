using System.IO;
using NUnit.Framework;
using WorldRush.Core;

namespace WorldRush.Tests
{
    public class SwipeDetectorTests
    {
        private SwipeDetector _swipe;

        [SetUp]
        public void SetUp() => _swipe = new SwipeDetector(minDistancePixels: 50f, maxDuration: 0.5f);

        [TestCase(-100f, 0f, RunnerCommand.MoveLeft)]
        [TestCase(100f, 10f, RunnerCommand.MoveRight)]
        [TestCase(5f, 120f, RunnerCommand.Jump)]
        [TestCase(-5f, -120f, RunnerCommand.Slide)]
        public void RecognisesFourDirections(float dx, float dy, RunnerCommand expected)
        {
            _swipe.Begin(500f, 500f, 0f);
            Assert.AreEqual(expected, _swipe.End(500f + dx, 500f + dy, 0.2f));
        }

        [Test]
        public void ShortMovementIsNotASwipe()
        {
            _swipe.Begin(0f, 0f, 0f);
            Assert.AreEqual(RunnerCommand.None, _swipe.End(20f, 10f, 0.1f));
        }

        [Test]
        public void SlowDragIsNotASwipe()
        {
            _swipe.Begin(0f, 0f, 0f);
            Assert.AreEqual(RunnerCommand.None, _swipe.End(200f, 0f, 2f));
        }

        [Test]
        public void SwipeFiresOnlyOncePerTouch()
        {
            _swipe.Begin(0f, 0f, 0f);
            Assert.AreEqual(RunnerCommand.MoveRight, _swipe.Update(80f, 0f, 0.1f));
            Assert.AreEqual(RunnerCommand.None, _swipe.Update(200f, 0f, 0.15f));
            Assert.AreEqual(RunnerCommand.None, _swipe.End(250f, 0f, 0.2f));
        }
    }

    public class SaveRepositoryTests
    {
        private string _dir;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "worldrush-tests-" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        [Test]
        public void SaveAndLoadRoundTrip()
        {
            var repo = new LocalSaveRepository(Path.Combine(_dir, "save.json"));
            repo.Save(new SaveData { HighScore = 4321, TotalCoins = 77, RunsPlayed = 3 });
            var loaded = repo.Load();
            Assert.AreEqual(4321, loaded.HighScore);
            Assert.AreEqual(77, loaded.TotalCoins);
            Assert.AreEqual(3, loaded.RunsPlayed);
        }

        [Test]
        public void MissingFileGivesDefaults()
        {
            var loaded = new LocalSaveRepository(Path.Combine(_dir, "none.json")).Load();
            Assert.AreEqual(0, loaded.HighScore);
            Assert.AreEqual(SaveData.CurrentSchemaVersion, loaded.SchemaVersion);
        }

        [Test]
        public void CorruptFileFallsBackToBackup()
        {
            string path = Path.Combine(_dir, "save.json");
            var repo = new LocalSaveRepository(path);
            repo.Save(new SaveData { HighScore = 100 });
            repo.Save(new SaveData { HighScore = 200 }); // erzeugt .bak mit 100
            File.WriteAllText(path, "{kaputt");

            Assert.AreEqual(100, repo.Load().HighScore);
        }

        [Test]
        public void CorruptFileWithoutBackupGivesDefaults()
        {
            string path = Path.Combine(_dir, "save.json");
            File.WriteAllText(path, "nicht json");
            Assert.AreEqual(0, new LocalSaveRepository(path).Load().HighScore);
        }

        [TestCase("{\"schemaVersion\":99,\"highScore\":5}")]
        [TestCase("{\"highScore\":5}")]
        [TestCase("")]
        public void UnknownOrInvalidSchemaIsRejected(string json)
        {
            Assert.IsFalse(SaveData.TryParse(json, out _));
        }

        [Test]
        public void NegativeValuesAreClampedToZero()
        {
            Assert.IsTrue(SaveData.TryParse("{\"schemaVersion\":1,\"highScore\":-50,\"totalCoins\":3}", out var data));
            Assert.AreEqual(0, data.HighScore);
            Assert.AreEqual(3, data.TotalCoins);
        }

        [Test]
        public void NoTempFileIsLeftBehind()
        {
            string path = Path.Combine(_dir, "save.json");
            var repo = new LocalSaveRepository(path);
            repo.Save(new SaveData { HighScore = 1 });
            repo.Save(new SaveData { HighScore = 2 });
            Assert.IsFalse(File.Exists(path + ".tmp"));
        }
    }
}
