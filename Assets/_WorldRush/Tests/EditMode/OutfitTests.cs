using NUnit.Framework;
using WorldRush.Core.Outfits;

namespace WorldRush.Tests
{
    public class OutfitTextParserTests
    {
        [Test]
        public void AcceptanceSentenceGerman()
        {
            var r = OutfitTextParser.Parse("Blonde Haare, rotes Kleid, braune Schuhe.");
            Assert.AreEqual(3, r.Wishes.Count);
            Assert.AreEqual("blonde", r.Find(OutfitSlot.Hair).Color);
            Assert.AreEqual("red", r.Find(OutfitSlot.Dress).Color);
            Assert.AreEqual("brown", r.Find(OutfitSlot.Shoes).Color);
            Assert.IsEmpty(r.UnknownWords);
        }

        [Test]
        public void AcceptanceSentenceEnglish()
        {
            var r = OutfitTextParser.Parse("blonde hair, red dress and brown shoes");
            Assert.AreEqual("blonde", r.Find(OutfitSlot.Hair).Color);
            Assert.AreEqual("red", r.Find(OutfitSlot.Dress).Color);
            Assert.AreEqual("brown", r.Find(OutfitSlot.Shoes).Color);
        }

        [Test]
        public void WorksWithoutCommasAndWithFillerWords()
        {
            var r = OutfitTextParser.Parse("Ich möchte bitte schwarze Haare und eine blaue Jeans mit weißen Sneakern");
            Assert.AreEqual("black", r.Find(OutfitSlot.Hair).Color);
            Assert.AreEqual("pants", r.Find(OutfitSlot.Bottom).Type);
            Assert.AreEqual("blue", r.Find(OutfitSlot.Bottom).Color);
            Assert.AreEqual("sneakers", r.Find(OutfitSlot.Shoes).Type);
            Assert.AreEqual("white", r.Find(OutfitSlot.Shoes).Color);
        }

        [Test]
        public void HyphenatedTShirtIsRecognised()
        {
            var r = OutfitTextParser.Parse("grünes T-Shirt");
            Assert.AreEqual("shirt", r.Find(OutfitSlot.Top).Type);
            Assert.AreEqual("green", r.Find(OutfitSlot.Top).Color);
        }

        [Test]
        public void DressReplacesTopAndBottom()
        {
            var r = OutfitTextParser.Parse("weißes Shirt, blaue Hose, rotes Kleid");
            Assert.IsNull(r.Find(OutfitSlot.Top));
            Assert.IsNull(r.Find(OutfitSlot.Bottom));
            Assert.IsNotNull(r.Find(OutfitSlot.Dress));
        }

        [Test]
        public void UnknownWordsAreReported()
        {
            var r = OutfitTextParser.Parse("rotes Kleid, Drachenflügel");
            Assert.AreEqual(1, r.Wishes.Count);
            CollectionAssert.Contains(r.UnknownWords, "drachenflügel");
        }

        [Test]
        public void EmptyOrHugeInputIsSafe()
        {
            Assert.IsEmpty(OutfitTextParser.Parse(null).Wishes);
            Assert.IsEmpty(OutfitTextParser.Parse("   ").Wishes);
            var huge = OutfitTextParser.Parse(new string('x', 100000) + " rotes Kleid");
            Assert.IsEmpty(huge.Wishes); // abgeschnitten nach MaxInputLength
        }

        [Test]
        public void ItemWithoutColorHasNullColor()
        {
            var r = OutfitTextParser.Parse("Kleid");
            Assert.IsNull(r.Find(OutfitSlot.Dress).Color);
        }
    }

    public class OutfitCatalogTests
    {
        private readonly OutfitCatalog _catalog = OutfitCatalog.CreateStarter();

        [Test]
        public void AcceptanceSentenceMatchesExactly()
        {
            var choices = _catalog.Match(OutfitTextParser.Parse("Blonde Haare, rotes Kleid, braune Schuhe."));
            Assert.AreEqual(3, choices.Count);
            foreach (var c in choices) Assert.AreEqual(MatchQuality.Exact, c.Quality, c.Wish.ToString());
            Assert.AreEqual("dress-classic", choices.Find(c => c.Wish.Slot == OutfitSlot.Dress).Item.Id);
        }

        [Test]
        public void MissingColorIsSubstitutedAndMarked()
        {
            var c = _catalog.Match(new OutfitWish(OutfitSlot.Bottom, "pants", "pink"));
            // Jeans gibt es nicht in Pink, aber den Rock → anderes Teil in Wunschfarbe
            Assert.AreEqual(MatchQuality.ItemSubstituted, c.Quality);
            Assert.AreEqual("skirt-short", c.Item.Id);
            Assert.AreEqual("pink", c.Color);
        }

        [Test]
        public void ColorNotAvailableAnywhereFallsBackToSameType()
        {
            var c = _catalog.Match(new OutfitWish(OutfitSlot.Shoes, "shoes", "gold"));
            Assert.AreEqual(MatchQuality.ColorSubstituted, c.Quality);
            Assert.AreEqual("shoes-classic", c.Item.Id);
        }

        [Test]
        public void BootsFallBackToOtherShoes()
        {
            var c = _catalog.Match(new OutfitWish(OutfitSlot.Shoes, "boots", "black"));
            Assert.AreEqual(MatchQuality.ItemSubstituted, c.Quality);
            Assert.IsNotNull(c.Item);
        }

        [Test]
        public void EmptyCatalogReportsNotAvailable()
        {
            var empty = new OutfitCatalog(new OutfitItem[0]);
            Assert.AreEqual(MatchQuality.NotAvailable, empty.Match(new OutfitWish(OutfitSlot.Dress, "dress", "red")).Quality);
        }

        [Test]
        public void DuplicateIdsAreRejected()
        {
            Assert.Throws<System.ArgumentException>(() => new OutfitCatalog(new[]
            {
                new OutfitItem("a", OutfitSlot.Hair, "hair", "red"),
                new OutfitItem("a", OutfitSlot.Dress, "dress", "red")
            }));
        }
    }
}
