using NUnit.Framework;
using WorldRush.Core;

namespace WorldRush.Tests
{
    public class LocalizationTests
    {
        [TearDown] public void Reset() => L.Current = Language.German;

        [Test]
        public void EveryKeyHasBothLanguagesAndSamePlaceholders()
        {
            foreach (var key in L.Keys)
            {
                string de = L.Raw(key, Language.German), en = L.Raw(key, Language.English);
                Assert.IsFalse(string.IsNullOrWhiteSpace(de), key);
                Assert.IsFalse(string.IsNullOrWhiteSpace(en), key);
                for (int i = 0; i < 5; i++)
                    Assert.AreEqual(de.Contains("{" + i + "}"), en.Contains("{" + i + "}"), $"Platzhalter {{{i}}} in {key}");
            }
        }

        [Test]
        public void SwitchesLanguageAndFormats()
        {
            L.Current = Language.English;
            Assert.AreEqual("Coins 3    Best 10", L.T("hud.coinsBest", 3, 10));
            L.Current = Language.German;
            Assert.AreEqual("Münzen 3    Best 10", L.T("hud.coinsBest", 3, 10));
            Assert.AreEqual("unknown.key", L.T("unknown.key"));
        }
    }

    public class SettingsTests
    {
        [Test]
        public void RoundTripAndClamp()
        {
            var s = new GameSettings { MusicVolume = 0.3f, SfxVolume = 0.6f, Quality = 1, Language = Language.English, TutorialDone = true };
            var back = GameSettings.FromJson(s.ToJson());
            Assert.AreEqual(0.3f, back.MusicVolume, 0.01f);
            Assert.AreEqual(1, back.Quality);
            Assert.AreEqual(Language.English, back.Language);
            Assert.IsTrue(back.TutorialDone);

            var bad = GameSettings.FromJson("{\"music\":7,\"sfx\":-3,\"quality\":99,\"language\":5}");
            Assert.AreEqual(1f, bad.MusicVolume);
            Assert.AreEqual(0f, bad.SfxVolume);
            Assert.AreEqual(2, bad.Quality);
            Assert.AreEqual(Language.German, bad.Language);
            Assert.AreEqual(2, GameSettings.FromJson("kaputt").Quality);
        }

        [Test]
        public void LanguageFromSystem()
        {
            Assert.AreEqual(Language.German, GameSettings.LanguageFor("de"));
            Assert.AreEqual(Language.English, GameSettings.LanguageFor("fr"));
        }
    }

    public class TutorialTests
    {
        private const float Frame = 1f / 60f;

        [Test]
        public void ShowsHintForObstacleAheadInOwnLane()
        {
            var s = new GameSession(RunnerConfig.CreateDefault(), 7);
            var coach = new TutorialCoach();
            coach.Begin();
            s.Start();
            string seen = null;
            for (int i = 0; i < 60 * 10 && s.State == GameState.Running && seen == null; i++)
            {
                s.Tick(Frame);
                seen = coach.Update(s);
            }
            Assert.IsNotNull(seen);
            StringAssert.StartsWith("tut.", seen);
        }

        [Test]
        public void FinishesAfterAllKindsLearnedIncludingTrick()
        {
            var level = new LevelDefinition(0, "T", 5000f, new[] { (0f, 4000f) });
            var s = new GameSession(RunnerConfig.CreateDefault(), 1, 0, level);
            var coach = new TutorialCoach();
            coach.Begin();
            coach.MarkLandLearned();
            s.Start();
            Assert.AreEqual("tut.trick", coach.Update(s));
            s.Enqueue(RunnerCommand.Jump); s.Tick(Frame);
            s.Enqueue(RunnerCommand.Jump); s.Tick(Frame);
            Assert.AreEqual(1, s.Tricks);
            Assert.AreEqual("tut.done", coach.Update(s));
            for (int i = 0; i < 60 * 3; i++) { s.Tick(Frame); coach.Update(s); }
            Assert.IsFalse(coach.Active);
        }

        [Test]
        public void InactiveCoachSaysNothing()
        {
            var s = new GameSession(RunnerConfig.CreateDefault(), 7);
            s.Start();
            Assert.IsNull(new TutorialCoach().Update(s));
        }
    }
}
