using NUnit.Framework;
using WorldRush.Core;

namespace WorldRush.Tests
{
    public class MissionTests
    {
        [Test]
        public void SameSetSameMissionsAndThreeDistinctKinds()
        {
            var a = MissionBoard.Create(42);
            var b = MissionBoard.Create(42);
            Assert.AreEqual(3, a.Missions.Count);
            for (int i = 0; i < 3; i++) { Assert.AreEqual(a.Missions[i].Kind, b.Missions[i].Kind); Assert.AreEqual(a.Missions[i].Target, b.Missions[i].Target); }
            Assert.AreNotEqual(a.Missions[0].Kind, a.Missions[1].Kind);
            Assert.AreNotEqual(a.Missions[1].Kind, a.Missions[2].Kind);
            Assert.AreNotEqual(a.Missions[0].Kind, a.Missions[2].Kind);
            foreach (var m in a.Missions) Assert.IsNotEmpty(m.Title);
        }

        [Test]
        public void ProgressAccumulatesAndCompletesOnce()
        {
            var board = MissionBoard.Create(1);
            var big = new RunStats { Coins = 9999, Tricks = 999, Slides = 999, Jumps = 999, Distance = 99999, LevelsFinished = 9, PowerUps = 99 };
            var done = board.ApplyRun(big);
            Assert.AreEqual(3, done.Count);
            foreach (var m in board.Missions) { Assert.IsTrue(m.Completed); Assert.AreEqual(m.Target, m.Progress); }
            Assert.IsEmpty(board.ApplyRun(big), "Belohnung nur einmal");
        }

        [Test]
        public void SmallRunsAddUp()
        {
            var board = MissionBoard.Create(5);
            var tiny = new RunStats { Coins = 1, Tricks = 1, Slides = 1, Jumps = 1, Distance = 1, LevelsFinished = 1, PowerUps = 1 };
            int runs = 0;
            while (!board.Missions[0].Completed && runs < 10000) { board.ApplyRun(tiny); runs++; }
            Assert.IsTrue(board.Missions[0].Completed);
            Assert.AreEqual(board.Missions[0].Target, runs);
        }

        [Test]
        public void StoreAndLoadKeepsProgressOnlyForSameDay()
        {
            var save = new SaveData();
            var board = MissionBoard.Create(100);
            board.ApplyRun(new RunStats { Coins = 10, Tricks = 1, Slides = 2, Jumps = 3, Distance = 50, PowerUps = 1 });
            board.Store(save);
            Assert.IsTrue(SaveData.TryParse(save.ToJson(), out var loadedSave));
            var same = MissionBoard.Load(100, loadedSave);
            for (int i = 0; i < 3; i++) Assert.AreEqual(board.Missions[i].Progress, same.Missions[i].Progress);
            var nextDay = MissionBoard.Load(101, loadedSave);
            foreach (var m in nextDay.Missions) Assert.AreEqual(0, m.Progress);
        }

        [Test]
        public void SessionCountsJumpsSlidesAndStats()
        {
            var s = new GameSession(RunnerConfig.CreateDefault(), 1);
            s.Start();
            s.Enqueue(RunnerCommand.Jump);
            s.Tick(1f / 60f);
            s.Enqueue(RunnerCommand.Jump); // in der Luft: zählt nicht
            s.Tick(1f / 60f);
            Assert.AreEqual(1, s.JumpCount);
            for (int i = 0; i < 60; i++) s.Tick(1f / 60f);
            s.Enqueue(RunnerCommand.Slide);
            s.Tick(1f / 60f);
            Assert.AreEqual(1, s.SlideCount);
            var stats = RunStats.From(s);
            Assert.AreEqual(1, stats.Jumps);
            Assert.AreEqual(1, stats.Slides);
        }

        [Test]
        public void DayBasedSetIds()
        {
            Assert.AreEqual(0, MissionBoard.SetIdForDate(new System.DateTime(2026, 1, 1, 15, 0, 0)));
            Assert.AreEqual(318, MissionBoard.SetIdForDate(new System.DateTime(2026, 11, 15)));
        }
    }

    public class BoardShopTests
    {
        [Test]
        public void FirstBoardIsFreeAndOwned()
        {
            var save = new SaveData();
            Assert.IsTrue(BoardShop.Owns(save, 0));
            Assert.IsFalse(BoardShop.Owns(save, 1));
        }

        [Test]
        public void BuyingNeedsEnoughCoinsAndDeducts()
        {
            var save = new SaveData { Wallet = 100 };
            Assert.AreEqual(BoardShop.BuyResult.NotEnoughCoins, BoardShop.TryBuy(save, 1));
            save.Wallet = 200;
            Assert.AreEqual(BoardShop.BuyResult.Bought, BoardShop.TryBuy(save, 1));
            Assert.AreEqual(50, save.Wallet);
            Assert.IsTrue(BoardShop.Owns(save, 1));
            Assert.AreEqual(1, save.SelectedBoard);
            Assert.AreEqual(BoardShop.BuyResult.AlreadyOwned, BoardShop.TryBuy(save, 1));
            Assert.AreEqual(50, save.Wallet, "Kein doppelter Abzug");
        }

        [Test]
        public void CannotSelectUnownedOrUnknown()
        {
            var save = new SaveData();
            Assert.IsFalse(BoardShop.Select(save, 3));
            Assert.IsFalse(BoardShop.Select(save, 99));
            Assert.AreEqual(BoardShop.BuyResult.Unknown, BoardShop.TryBuy(save, -1));
            Assert.IsTrue(BoardShop.Select(save, 0));
        }

        [Test]
        public void ShopStateSurvivesSaveAndRejectsTampering()
        {
            var save = new SaveData { Wallet = 1000 };
            BoardShop.TryBuy(save, 3);
            Assert.IsTrue(SaveData.TryParse(save.ToJson(), out var loaded));
            Assert.IsTrue(BoardShop.Owns(loaded, 3));
            Assert.AreEqual(3, loaded.SelectedBoard);
            Assert.AreEqual(500, loaded.Wallet);

            Assert.IsTrue(SaveData.TryParse("{\"schemaVersion\":1,\"selectedBoard\":5,\"ownedBoards\":0,\"wallet\":-20}", out var tampered));
            Assert.AreEqual(0, tampered.SelectedBoard, "Nicht gekauftes Brett wird nicht ausgewählt");
            Assert.AreEqual(0, tampered.Wallet);
        }
    }
}
