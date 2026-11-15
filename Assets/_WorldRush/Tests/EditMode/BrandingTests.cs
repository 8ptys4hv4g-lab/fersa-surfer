using NUnit.Framework;
using WorldRush.Core;
using WorldRush.Core.Branding;

namespace WorldRush.Tests
{
    public class BrandingTests
    {
        private static (int r, int g, int b) At(byte[] img, int w, int x, int y)
        {
            int o = (y * w + x) * 4;
            return (img[o], img[o + 1], img[o + 2]);
        }

        [Test]
        public void Name_and_tagline_are_english_store_title()
        {
            Assert.AreEqual("Fesara", ProductInfo.DisplayName);
            Assert.AreEqual("Fesara – Run & Surf", ProductInfo.StoreTitle);
            Assert.LessOrEqual(ProductInfo.StoreTitle.Length, 30, "Google Play erlaubt höchstens 30 Zeichen");
        }

        [Test]
        public void Icon_is_opaque_and_shows_logo_parts()
        {
            const int s = 256;
            var img = IconPainter.Paint(s, s, true);
            Assert.AreEqual(s * s * 4, img.Length);
            for (int i = 3; i < img.Length; i += 4) Assert.AreEqual(255, img[i], "Store-Icons dürfen nicht durchsichtig sein");

            var sun = At(img, s, (int)(88 / 120f * s), (int)(30 / 120f * s));
            Assert.Greater(sun.r, 240); Assert.Greater(sun.g, 200); Assert.Less(sun.b, 140);   // gelbe Sonne

            var sea = At(img, s, s / 2, s - 4);
            Assert.Less(sea.r, 80); Assert.Greater(sea.g, 180); Assert.Greater(sea.b, 200);    // türkises Wasser

            var topLeft = At(img, s, 1, 1);
            Assert.Greater(topLeft.r, 230); Assert.Greater(topLeft.b, 160);                       // rosa Ecke
        }

        [Test]
        public void Feature_graphic_has_store_size()
        {
            var img = IconPainter.Paint(1024, 500, false);
            Assert.AreEqual(1024 * 500 * 4, img.Length);
            var sea = At(img, 1024, 20, 495);
            Assert.Greater(sea.b, 200);                                                           // Wasser reicht bis zum linken Rand
        }
    }
}
