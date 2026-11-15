using NUnit.Framework;
using WorldRush.Core.Visuals;

namespace WorldRush.Tests
{
    public class SkyTests
    {
        private const int W = 256, H = 128;
        private static readonly SkyColor SunsetTint = new SkyColor(0.75f, 0.35f, 0.85f), SunsetFog = new SkyColor(0.42f, 0.25f, 0.63f);
        private static readonly SkyColor NightTint = new SkyColor(0.15f, 0.2f, 0.55f), NightFog = new SkyColor(0.11f, 0.12f, 0.3f);

        private static int Row(float elevationDeg) => (int)((90f + elevationDeg) / 180f * H);
        private static (int r, int g, int b) At(byte[] img, float u, int y)
        {
            int x = (int)(u * W);
            int o = (y * W + x) * 4;
            return (img[o], img[o + 1], img[o + 2]);
        }

        [Test]
        public void Sky_is_opaque_and_has_expected_size()
        {
            var img = SkyPainter.Paint(W, H, SunsetTint, SunsetFog);
            Assert.AreEqual(W * H * 4, img.Length);
            for (int i = 3; i < img.Length; i += 4) Assert.AreEqual(255, img[i]);
        }

        [Test]
        public void Horizon_matches_fog_so_far_buildings_blend_in()
        {
            var img = SkyPainter.Paint(W, H, SunsetTint, SunsetFog);
            var c = At(img, 0.1f, H / 2 - 1); // knapp unter dem Horizont, weit weg von der Sonne
            Assert.AreEqual(SunsetFog.R * 255, c.r, 3);
            Assert.AreEqual(SunsetFog.G * 255, c.g, 3);
            Assert.AreEqual(SunsetFog.B * 255, c.b, 3);
        }

        [Test]
        public void Sun_is_visible_in_menu_direction_and_run_direction()
        {
            var img = SkyPainter.Paint(W, H, SunsetTint, SunsetFog);
            int y = Row(SkyPainter.SunElevation);
            foreach (float u in new[] { 0.25f, 0.75f }) // +Z (Laufen) und -Z (Startmenü)
            {
                var c = At(img, u, y);
                Assert.Greater(c.r, 240); Assert.Greater(c.g, 170);
            }
        }

        [Test]
        public void Night_shows_a_pale_moon_instead_of_an_orange_sun()
        {
            var img = SkyPainter.Paint(W, H, NightTint, NightFog);
            var c = At(img, 0.75f, Row(SkyPainter.SunElevation));
            Assert.Greater(c.b, 220);
            Assert.GreaterOrEqual(c.b, c.r - 5);
        }

        [Test]
        public void Painting_in_chunks_gives_the_same_picture()
        {
            var full = SkyPainter.Paint(W, H, SunsetTint, SunsetFog);
            var chunked = new byte[W * H * 4];
            for (int y = 0; y < H; y += 24) SkyPainter.PaintRows(chunked, W, H, y, y + 24, SunsetTint, SunsetFog);
            CollectionAssert.AreEqual(full, chunked);
        }
    }
}
