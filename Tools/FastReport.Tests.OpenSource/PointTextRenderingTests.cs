using System.IO;
using System.Text;
using FastReport.Drawing;
using SkiaSharp;
using Xunit;

namespace FastReport.Tests.OpenSource
{
    public class PointTextRenderingTests
    {
        [Theory]
        [InlineData(1f, false)]
        [InlineData(3.125f, false)]
        [InlineData(3.125f, true)]
        public void PointTextIsVisibleWithScaledGraphics(float scale, bool typographic)
        {
            using var bitmap = new Bitmap(400, 200);
            using var graphics = Graphics.FromImage(bitmap);
            using var font = new Font("Times New Roman", 6);
            using var format = typographic ? StringFormat.GenericTypographic : StringFormat.GenericDefault;
            graphics.Clear(Color.White);
            graphics.ScaleTransform(scale, scale);
            graphics.DrawString("PRODUCT DESCRIPTION", font, Brushes.Black, 10, 10, format);

            int ink = 0;
            for (int y = 0; y < bitmap.Height; y++)
                for (int x = 0; x < bitmap.Width; x++)
                    if (bitmap.GetPixel(x, y).ToArgb() != Color.White.ToArgb()) ink++;
            Assert.True(ink > 0, "Point text was culled from the scaled drawing surface.");
        }

        [Theory]
        [InlineData(1f, false)]
        [InlineData(3.125f, false)]
        [InlineData(1f, true)]
        [InlineData(3.125f, true)]
        public void PointTextIsIncludedInScaledPdf(float scale, bool typographic)
        {
            using var output = new MemoryStream();
            using (var document = SKDocument.CreatePdf(output))
            {
                var canvas = document.BeginPage(600, 800);
                using (var graphics = Graphics.FromCanvas(canvas))
                using (var font = new Font("Times New Roman", 6))
                using (var format = typographic ? StringFormat.GenericTypographic : StringFormat.GenericDefault)
                {
                    graphics.ScaleTransform(scale, scale);
                    graphics.DrawString("PRODUCT DESCRIPTION", font, Brushes.Black, 10, 10, format);
                }
                document.EndPage();
                document.Close();
            }

            // Skia only emits a font resource when a text operation survives clipping.
            Assert.Contains("/Type /Font", Encoding.ASCII.GetString(output.ToArray()));
        }
    }
}
