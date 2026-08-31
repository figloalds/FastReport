using FastReport.Drawing;
using Xunit;

namespace FastReport.Tests.OpenSource
{
    public class DrawingTests
    {
        [Fact]
        public void NamedColorsResolveToArgbValues()
        {
            Assert.Equal(unchecked((int)0xFFF0F8FF), Color.FromName("AliceBlue").ToArgb());
            Assert.Equal(unchecked((int)0xFF87CEFA), Color.LightSkyBlue.ToArgb());
            Assert.Equal(Color.Gray.ToArgb(), Color.FromName("grey").ToArgb());
            Assert.True(Color.AliceBlue.IsKnownColor);
            Assert.Equal(KnownColor.AliceBlue, Color.AliceBlue.ToKnownColor());
            Assert.Equal(Color.AliceBlue, FastReport.Utils.ColorHelper.FromString("AliceBlue"));
        }

        [Fact]
        public void ResetClipAndRestoreRebuildCanvasState()
        {
            using var bitmap = new Bitmap(20, 10);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.Clear(Color.White);
            graphics.SetClip(new Rectangle(0, 0, 10, 10));
            var state = graphics.Save();

            graphics.ResetClip();
            graphics.FillRectangle(Brushes.Blue, 0, 0, 20, 10);
            graphics.Restore(state);
            graphics.FillRectangle(Brushes.Red, 0, 0, 20, 10);

            Assert.Equal(Color.Red.ToArgb(), bitmap.GetPixel(5, 5).ToArgb());
            Assert.Equal(Color.Blue.ToArgb(), bitmap.GetPixel(15, 5).ToArgb());
        }

        [Fact]
        public void ZeroOrNegativeLayoutBoundsMeasureAsUnbounded()
        {
            // GDI+ treats a zero/negative layout bound as unbounded (single line, no line
            // limit). TextObject.CalcSize passes width 0 for non-wrapped measurements.
            using var bitmap = new Bitmap(1, 1);
            using var graphics = Graphics.FromImage(bitmap);
            using var font = new Font("Arial", 8f);

            SizeF unconstrained = graphics.MeasureString("Andrew Fuller", font);
            SizeF zeroWidth = graphics.MeasureString("Andrew Fuller", font, new SizeF(0, 100000));
            SizeF zeroBoth = graphics.MeasureString("Andrew Fuller", font, new SizeF(0, 0));
            SizeF negative = graphics.MeasureString("Andrew Fuller", font, new SizeF(-5, -5));

            Assert.True(unconstrained.Width > 40, $"sanity width, got {unconstrained.Width}");
            Assert.Equal(unconstrained.Width, zeroWidth.Width);
            Assert.Equal(unconstrained.Height, zeroWidth.Height);
            Assert.Equal(unconstrained.Width, zeroBoth.Width);
            Assert.Equal(unconstrained.Height, zeroBoth.Height);
            Assert.Equal(unconstrained.Width, negative.Width);

            graphics.MeasureString("Andrew Fuller", font, new SizeF(0, 100000), StringFormat.GenericDefault, out int charsFitted, out int linesFilled);
            Assert.Equal(1, linesFilled);
            Assert.Equal("Andrew Fuller".Length, charsFitted);
        }

        [Fact]
        public void PositiveLayoutWidthStillWraps()
        {
            using var bitmap = new Bitmap(1, 1);
            using var graphics = Graphics.FromImage(bitmap);
            using var font = new Font("Arial", 8f);

            SizeF narrow = graphics.MeasureString("Andrew Fuller", font, new SizeF(30, 100000));
            Assert.True(narrow.Height > unconstrainedSingleLineHeight(font) * 1.5f,
                $"expected wrapping at 30px layout, got height {narrow.Height}");

            static float unconstrainedSingleLineHeight(Font font)
            {
                using var bmp = new Bitmap(1, 1);
                using var g = Graphics.FromImage(bmp);
                return g.MeasureString("Andrew Fuller", font).Height;
            }
        }
    }
}
