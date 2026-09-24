using FastReport.Drawing;
using FastReport.Drawing.Imaging;
using FastReport.Drawing.Drawing2D;
using FastReport.Drawing.Text;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;

namespace FastReport.Tests.OpenSource
{
    public class DrawingTests
    {
        [Theory]
        [InlineData(DashStyle.Solid, new float[] { })]
        [InlineData(DashStyle.Dash, new[] { 3f, 1f })]
        [InlineData(DashStyle.Dot, new[] { 1f, 1f })]
        [InlineData(DashStyle.DashDot, new[] { 3f, 1f, 1f, 1f })]
        [InlineData(DashStyle.DashDotDot, new[] { 3f, 1f, 1f, 1f, 1f, 1f })]
        [InlineData(DashStyle.Custom, new[] { 1f })]
        public void PenExposesTheEffectiveDashPattern(DashStyle style, float[] expected)
        {
            using var pen = new Pen(Color.Black) { DashStyle = style };
            Assert.Equal(expected, pen.DashPattern);
            using var clone = (Pen)pen.Clone();
            Assert.Equal(style, clone.DashStyle);
            Assert.Equal(expected, clone.DashPattern);
        }

        [Fact]
        public void AssigningDashPatternSelectsCustomStyleAndCopiesIntervals()
        {
            float[] intervals = { 2, 3, 4 };
            using var pen = new Pen(Color.Black) { DashPattern = intervals };
            intervals[0] = 99;
            float[] returned = pen.DashPattern;
            returned[1] = 99;
            Assert.Equal(DashStyle.Custom, pen.DashStyle);
            Assert.Equal(new[] { 2f, 3f, 4f }, pen.DashPattern);

            using var clone = (Pen)pen.Clone();
            pen.DashStyle = DashStyle.Dot;
            Assert.Equal(new[] { 2f, 3f, 4f }, clone.DashPattern);
            Assert.Equal(DashStyle.Custom, clone.DashStyle);
            pen.DashStyle = DashStyle.Custom;
            Assert.Equal(new[] { 1f, 1f }, pen.DashPattern);
        }

        [Theory]
        [InlineData(new[] { 3f })]
        [InlineData(new[] { 2f, 3f, 4f })]
        public void OddCustomDashPatternsRenderWithGaps(float[] intervals)
        {
            using var bitmap = new Bitmap(100, 20);
            using var graphics = Graphics.FromImage(bitmap);
            using var pen = new Pen(Color.Black) { DashPattern = intervals };
            graphics.Clear(Color.White);
            graphics.DrawLine(pen, 0, 10, 100, 10);
            var colors = Enumerable.Range(5, 90).Select(x => bitmap.GetPixel(x, 10).ToArgb()).ToArray();
            Assert.Contains(Color.White.ToArgb(), colors);
            Assert.Contains(colors, color => color != Color.White.ToArgb());
        }

        [Theory]
        [InlineData(LineStyle.Dash)]
        [InlineData(LineStyle.Dot)]
        [InlineData(LineStyle.DashDot)]
        [InlineData(LineStyle.DashDotDot)]
        public void DashedReportBordersRenderAndAllowFollowingObjects(LineStyle style)
        {
            using var bitmap = new Bitmap(100, 100);
            using var graphics = Graphics.FromImage(bitmap);
            using var report = new Report();
            graphics.Clear(Color.White);
            using var cell = new TextObject { Left = 10, Top = 10, Width = 80, Height = 30 };
            cell.Border.Lines = BorderLines.Left | BorderLines.Right | BorderLines.Bottom;
            cell.Border.BottomLine.Style = style;
            cell.Border.BottomLine.Width = 0.5f;
            cell.Draw(new FastReport.Utils.FRPaintEventArgs(graphics, 1, 1, report.GraphicCache));
            graphics.FillRectangle(Brushes.Blue, 10, 60, 80, 20);

            Assert.Contains(Enumerable.Range(10, 80), x => bitmap.GetPixel(x, 40).ToArgb() != Color.White.ToArgb());
            Assert.Equal(Color.Blue.ToArgb(), bitmap.GetPixel(50, 70).ToArgb());
        }

        [Fact]
        public void ClipStaysInDeviceCoordinatesAcrossRotationAndRestore()
        {
            using var bitmap = new Bitmap(100, 100);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.Clear(Color.White);
            graphics.SetClip(new Rectangle(20, 20, 20, 60));
            var state = graphics.Save();
            graphics.TranslateTransform(30, 50);
            graphics.RotateTransform(90);
            graphics.FillRectangle(Brushes.Red, -30, -10, 60, 20);
            Assert.Equal(Color.Red.ToArgb(), bitmap.GetPixel(30, 50).ToArgb());
            Assert.Equal(Color.White.ToArgb(), bitmap.GetPixel(50, 50).ToArgb());
            graphics.Restore(state);
            graphics.FillRectangle(Brushes.Blue, 0, 0, 100, 100);
            Assert.Equal(Color.Blue.ToArgb(), bitmap.GetPixel(30, 50).ToArgb());
            Assert.Equal(Color.White.ToArgb(), bitmap.GetPixel(50, 50).ToArgb());
        }

        [Fact]
        public void ClipRoundTripsAndIntersectsInCurrentCoordinates()
        {
            using var bitmap = new Bitmap(100, 100);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.TranslateTransform(20, 10);
            graphics.SetClip(new Rectangle(0, 0, 20, 20));
            graphics.ResetTransform();
            using var clip = graphics.Clip;
            Assert.Equal(new RectangleF(20, 10, 20, 20), clip.GetBounds(graphics));
            graphics.Clip = clip;
            graphics.SetClip(new Rectangle(30, 0, 30, 100), CombineMode.Intersect);
            using var intersection = graphics.Clip;
            Assert.Equal(new RectangleF(30, 10, 10, 20), intersection.GetBounds(graphics));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void DrawStringHonorsNoClipAndRestoresThePreviousClip(bool noClip)
        {
            using var bitmap = new Bitmap(200, 80);
            using var graphics = Graphics.FromImage(bitmap);
            using var font = new Font("Arial", 12);
            using var format = new StringFormat(StringFormatFlags.NoWrap |
                (noClip ? StringFormatFlags.NoClip : 0));
            graphics.Clear(Color.White);
            graphics.SetClip(new Rectangle(0, 0, 150, 70));
            graphics.DrawString("WWWWWWWW", font, Brushes.Black, new RectangleF(0, 0, 30, 25), format);
            int outside = 0;
            for (int y = 0; y < 25; y++)
                for (int x = 31; x < 150; x++)
                    if (bitmap.GetPixel(x, y).ToArgb() != Color.White.ToArgb()) outside++;
            Assert.Equal(noClip, outside > 0);
            graphics.FillRectangle(Brushes.Blue, 0, 40, 200, 40);
            Assert.Equal(Color.Blue.ToArgb(), bitmap.GetPixel(100, 50).ToArgb());
            Assert.Equal(Color.White.ToArgb(), bitmap.GetPixel(175, 50).ToArgb());
        }

        [Theory]
        [InlineData("A\r\nB\r\nC", 6)]
        [InlineData("A\nB\nC", 4)]
        [InlineData("A\rB\rC", 4)]
        [InlineData("\r\n\r\nC", 4)]
        [InlineData("A\r\nB\nC", 5)]
        public void FittedCharactersIncludeOriginalLineEndings(string text, int expected)
        {
            using var bitmap = new Bitmap(1, 1);
            using var graphics = Graphics.FromImage(bitmap);
            using var font = new Font("Arial", 12);
            graphics.MeasureString(text, font, new SizeF(100, font.GetHeight() * 2),
                StringFormat.GenericDefault, out int fitted, out int lines);
            Assert.Equal(2, lines);
            Assert.Equal(expected, fitted);
            Assert.Equal("C", text.Substring(fitted));
            graphics.MeasureString(text, font, new SizeF(100, 1000),
                StringFormat.GenericDefault, out fitted, out lines);
            Assert.Equal(text.Length, fitted);
        }

        [Fact]
        public void TextObjectBreakKeepsTwoCompleteCrLfLines()
        {
            using var report = new Report();
            var page = new ReportPage();
            report.Pages.Add(page);
            var band = new ReportTitleBand();
            page.ReportTitle = band;
            using var source = new TextObject { Parent = band, Width = 200, Height = 39,
                Font = new Font("Arial", 12), Text = "A\r\nB\r\nC" };
            using var destination = new TextObject();
            Assert.True(source.Break(destination));
            Assert.Equal("A\r\nB\r\n", source.Text);
            Assert.Equal("C", destination.Text);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void PrivateFontsUseRegisteredRegularAndBoldFaces(bool fromMemory)
        {
            using var fonts = new PrivateFontCollection();
            foreach (string style in new[] { "Regular", "Bold" })
            {
                string path = Path.Combine(Path.GetDirectoryName(typeof(DrawingTests).Assembly.Location),
                    "Fixtures", "TestSans-" + style + ".ttf");
                if (!fromMemory) fonts.AddFontFile(path);
                else
                {
                    byte[] data = File.ReadAllBytes(path);
                    var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
                    try { fonts.AddMemoryFont(handle.AddrOfPinnedObject(), data.Length); }
                    finally { handle.Free(); }
                }
            }
            using var bitmap = new Bitmap(1, 1);
            using var graphics = Graphics.FromImage(bitmap);
            using var regular = new Font(fonts.Families[0], 12);
            using var bold = new Font(fonts.Families[0], 12, FontStyle.Bold);
            // Compare glyph advances without the default format's overhang allowance.
            using var format = StringFormat.GenericTypographic;
            Assert.InRange(graphics.MeasureString("AAA", regular, new SizeF(0, 0), format).Width, 28.7f, 28.9f);
            Assert.InRange(graphics.MeasureString("AAA", bold, new SizeF(0, 0), format).Width, 38.3f, 38.5f);
        }

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
