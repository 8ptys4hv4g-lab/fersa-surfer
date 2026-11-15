using System.Collections.Generic;
using NUnit.Framework;
using WorldRush.Core;
using WorldRush.Core.Outfits;

namespace WorldRush.Tests
{
    public class OutfitStudioTests
    {
        [TestCase(OutfitSlot.Dress, "red", "rotes Kleid")]
        [TestCase(OutfitSlot.Bottom, "red", "rote Hose")]
        [TestCase(OutfitSlot.Hair, "blonde", "blonde Haare")]
        [TestCase(OutfitSlot.Dress, "purple", "lila Kleid")]
        [TestCase(OutfitSlot.Top, "orange", "oranges Oberteil")]
        [TestCase(OutfitSlot.Shoes, "gold", "goldene Schuhe")]
        [TestCase(OutfitSlot.Dress, "turquoise", "türkises Kleid")]
        [TestCase(OutfitSlot.Hair, "silver", "silberne Haare")]
        public void German_phrases_use_correct_endings(OutfitSlot slot, string color, string expected)
        {
            Assert.AreEqual(expected, OutfitStudio.Phrase(slot, color, Language.German));
        }

        [Test]
        public void English_phrase()
        {
            Assert.AreEqual("turquoise dress", OutfitStudio.Phrase(OutfitSlot.Dress, "turquoise", Language.English));
        }

        [Test]
        public void Every_tappable_look_is_understood_again_by_the_text_parser()
        {
            var specs = new List<OutfitSpec>();
            foreach (var p in OutfitStudio.Presets) specs.Add(p.Spec);
            for (int i = 0; i < 60; i++) specs.Add(OutfitStudio.Roll(i));
            foreach (var slot in new[] { OutfitSlot.Hair, OutfitSlot.Top, OutfitSlot.Bottom, OutfitSlot.Dress, OutfitSlot.Shoes })
                foreach (var c in OutfitStudio.ColorsFor(slot))
                {
                    var s = new OutfitSpec();
                    s.Set(slot, c);
                    specs.Add(s);
                }

            foreach (var lang in new[] { Language.German, Language.English })
                foreach (var spec in specs)
                {
                    string text = spec.ToText(lang);
                    var back = OutfitSpec.FromText(text, new OutfitSpec { Hair = "black", Top = "black", Bottom = "black", Dress = "black", Shoes = "black", WearsDress = !spec.WearsDress });
                    Assert.IsTrue(back.SameAs(spec), $"{lang}: \"{text}\" wurde nicht richtig verstanden");
                    Assert.IsEmpty(OutfitTextParser.Parse(text).UnknownWords, text);
                }
        }

        [Test]
        public void Dice_is_repeatable_and_varied()
        {
            Assert.IsTrue(OutfitStudio.Roll(5).SameAs(OutfitStudio.Roll(5)));
            var seen = new HashSet<string>();
            for (int i = 0; i < 20; i++) seen.Add(OutfitStudio.Roll(i).ToText(Language.German));
            Assert.GreaterOrEqual(seen.Count, 12);
        }

        [Test]
        public void Choosing_top_or_dress_switches_the_style()
        {
            var s = new OutfitSpec { WearsDress = true };
            s.Set(OutfitSlot.Top, "pink");
            Assert.IsFalse(s.WearsDress);
            s.Set(OutfitSlot.Dress, "blue");
            Assert.IsTrue(s.WearsDress);
            s.Set(OutfitSlot.Hair, "rainbow-unknown");
            Assert.AreEqual("blonde", s.Hair, "unbekannte Farben werden ignoriert");
        }
    }
}
