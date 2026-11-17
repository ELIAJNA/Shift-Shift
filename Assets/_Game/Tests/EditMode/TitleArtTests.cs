using NUnit.Framework;
using ShiftShift.Core;

namespace ShiftShift.Tests
{
    public class TitleArtTests
    {
        [TestCase(24)]
        [TestCase(78)]
        public void Render_SizeMatchesAndHasLettering(int cap)
        {
            byte[] px = TitleArt.Render(cap, out int w, out int h);
            Assert.AreEqual(w * h * 4, px.Length);
            Assert.Greater(w, h * 5, "a wide title");
            int opaque = 0;
            for (int i = 3; i < px.Length; i += 4) if (px[i] == 255) opaque++;
            Assert.Greater(opaque, w * h / 5, "letters cover a good part of the image");
            Assert.AreEqual(0, px[3], "top-left corner is transparent");
        }

        [Test]
        public void Render_OuterLettersOrangeAndPlusRed()
        {
            byte[] px = TitleArt.Render(96, out int w, out int h);
            // Middle of the first S's top bar (grid x 5, y 10.5) and of the + (its centre).
            int Sample(double fx, double fy) { int x = (int)(fx * w), y = (int)(fy * h); return (y * w + x) * 4; }
            int s = FindColoured(px, w, h, 0.03, 0.12);
            Assert.Greater(px[s], 200, "S is orange: strong red");
            Assert.Less(px[s + 2], 120, "S is orange: little blue");
            int plus = Sample(0.5, 0.45);
            Assert.Greater(px[plus], 150, "+ is red");
            Assert.Less(px[plus + 1], 90);
        }

        // First fully opaque, bright pixel scanning right from fx along row fy.
        private static int FindColoured(byte[] px, int w, int h, double fx, double fy)
        {
            int y = (int)(fy * h);
            for (int x = (int)(fx * w); x < w; x++)
            {
                int o = (y * w + x) * 4;
                if (px[o + 3] == 255 && px[o] > 150) return o;
            }
            Assert.Fail("no coloured pixel found");
            return 0;
        }
    }
}
