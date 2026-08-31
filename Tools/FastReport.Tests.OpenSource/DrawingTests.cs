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
    }
}
