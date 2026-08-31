using FastReport.Drawing;
using FastReport.Drawing.Imaging;
using System;
using System.IO;
using System.Linq;
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
        public void SaveAsBmpWritesRealBmpBytes()
        {
            using var bitmap = new Bitmap(4, 3);
            using (var graphics = Graphics.FromImage(bitmap))
                graphics.Clear(Color.Red);

            using var stream = new MemoryStream();
            bitmap.Save(stream, ImageFormat.Bmp);
            byte[] bytes = stream.ToArray();

            // 54-byte header + 3 rows of stride (4*3+3)&~3 = 12 → 90 bytes
            Assert.Equal(90, bytes.Length);
            Assert.Equal((byte)'B', bytes[0]);
            Assert.Equal((byte)'M', bytes[1]);
            Assert.Equal(54, BitConverter.ToInt32(bytes, 10)); // pixel data offset
            Assert.Equal(24, BitConverter.ToInt16(bytes, 28)); // bpp
            // rows are bottom-up; every pixel is opaque red (B=0, G=0, R=255)
            Assert.Equal(0, bytes[54]);
            Assert.Equal(0, bytes[55]);
            Assert.Equal(255, bytes[56]);

            // the produced BMP must be decodable again
            using var decoded = (Bitmap)Image.FromStream(new MemoryStream(bytes));
            Assert.Equal(4, decoded.Width);
            Assert.Equal(3, decoded.Height);
            Assert.Equal(Color.Red.ToArgb(), decoded.GetPixel(1, 1).ToArgb());
        }

        [Fact]
        public void BmpEncoderCompositesTransparencyOverWhite()
        {
            using var bitmap = new Bitmap(2, 2);
            bitmap.SetPixel(0, 0, Color.FromArgb(128, 255, 0, 0));

            using var stream = new MemoryStream();
            bitmap.Save(stream, ImageFormat.Bmp);
            byte[] bytes = stream.ToArray();

            // bottom-up: (0,0) is in the LAST row written → last 6 bytes (stride is 8)
            int offset = 54 + 8 + 0;
            byte b = bytes[offset], g = bytes[offset + 1], r = bytes[offset + 2];
            Assert.InRange(r, 250, 255);
            Assert.InRange((int)g, 120, 135); // 127 ± premultiply quantization
            Assert.InRange((int)b, 120, 135);
        }

        [Fact]
        public void UnsupportedImageFormatsThrowInsteadOfWritingWrongBytes()
        {
            using var bitmap = new Bitmap(2, 2);
            using var stream = new MemoryStream();

            Assert.Throws<NotSupportedException>(() => bitmap.Save(stream, ImageFormat.Gif));
            Assert.Throws<NotSupportedException>(() => bitmap.Save(stream, ImageFormat.Tiff));
            Assert.Throws<NotSupportedException>(() => bitmap.SaveAdd((EncoderParameters)null));
            Assert.Throws<NotSupportedException>(() => new Metafile(new MemoryStream(), IntPtr.Zero));
            Assert.Equal(new[] { "image/jpeg", "image/png", "image/bmp" },
                ImageCodecInfo.GetImageEncoders().Select(c => c.MimeType).ToArray());

            // PNG and JPEG remain valid
            bitmap.Save(stream, ImageFormat.Png);
            Assert.True(stream.Length > 0);
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
