using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using FastReport.Drawing;
using FastReport.Drawing.Text;
using FastReport.Utils;
using Xunit;

namespace FastReport.Tests.OpenSource
{
    public class ReceiptLayoutTests
    {
        [Theory]
        [InlineData("LightGray")]
        [InlineData("WhiteSmoke")]
        [InlineData("Gainsboro")]
        [InlineData("128, 12, 34, 56")]
        [InlineData("Transparent")]
        public void ColorsSurvivePreparedPageSerialization(string value)
        {
            var color = (Color)Converter.FromString(typeof(Color), value);
            Assert.IsType<ColorConverter>(TypeDescriptor.GetConverter(typeof(Color)));
            Assert.Equal(color, Converter.FromString(typeof(Color), Converter.ToString(color)));

            using var report = new Report();
            var page = new ReportPage();
            report.Pages.Add(page);
            page.ReportTitle = new ReportTitleBand { Height = 20, FillColor = color };
            var text = new TextObject
            {
                Parent = page.ReportTitle, Name = "ReceiptRow", Width = 200, Height = 20,
                Text = "ITEM", FillColor = color
            };
            text.Border.Color = color;
            Assert.True(report.Prepare());
            using var stream = new MemoryStream();
            report.SavePrepared(stream);
            stream.Position = 0;
            using var restored = new Report();
            restored.LoadPrepared(stream);
            using var prepared = restored.PreparedPages.GetPage(0);
            var row = prepared.AllObjects.OfType<TextObject>().Single();
            Assert.Equal(color, row.FillColor);
            Assert.Equal(color, row.Border.Color);
            Assert.Equal(color, ((BandBase)row.Parent).FillColor);
        }

        [Theory]
        [InlineData("AAA", 18f, 16f, 1)]
        [InlineData("AAA\r\n", 18f, 16f, 1)]
        [InlineData("AAA\n", 18f, 16f, 1)]
        [InlineData("AAA\r", 18f, 16f, 1)]
        [InlineData("AAA\r\n\r\n", 34f, 32f, 2)]
        [InlineData("AAA\r\nAAA", 34f, 32f, 2)]
        [InlineData("\r\n", 18f, 16f, 1)]
        [InlineData("", 0f, 0f, 0)]
        public void TextHeightMatchesLegacyReceiptLayout(string text, float defaultHeight, float typographicHeight, int expectedLines)
        {
            // Golden heights measured with GDI+ and the same bundled font at 96 DPI.
            using var fonts = LoadTestFont();
            using var font = new Font(fonts.Families[0], 12);
            using var bitmap = new Bitmap(1, 1);
            using var graphics = Graphics.FromImage(bitmap);
            using var format = StringFormat.GenericTypographic;
            Assert.Equal(defaultHeight, graphics.MeasureString(text, font).Height, 3);
            Assert.Equal(typographicHeight, graphics.MeasureString(text, font, new SizeF(200, 1000), format).Height, 3);
            graphics.MeasureString(text, font, new SizeF(200, 1000), format, out int fitted, out int lines);
            Assert.Equal(text.Length, fitted);
            Assert.Equal(expectedLines, lines);
        }

        [Fact]
        public void FontMetricsComeFromTheFontInsteadOfArialConstants()
        {
            using var fonts = LoadTestFont();
            var family = fonts.Families[0];
            Assert.Equal(1000, family.GetEmHeight(FontStyle.Regular));
            Assert.Equal(800, family.GetCellAscent(FontStyle.Regular));
            Assert.Equal(200, family.GetCellDescent(FontStyle.Regular));
            Assert.Equal(1000, family.GetLineSpacing(FontStyle.Regular));
            using var font = new Font(family, 12);
            Assert.Equal(16f, font.GetHeight(96));
            Assert.Equal(24f, font.GetHeight(144));
        }

        [Fact]
        public void TrailingNewlineDoesNotDoubleReceiptRowHeight()
        {
            using var fonts = LoadTestFont();
            using var report = new Report();
            var page = new ReportPage();
            report.Pages.Add(page);
            page.ReportTitle = new ReportTitleBand { Height = 19, CanGrow = true };
            var text = new TextObject
            {
                Parent = page.ReportTitle, Width = 200, Height = 19, CanGrow = true,
                Font = new Font(fonts.Families[0], 12), Text = "AAA\r\n"
            };
            Assert.Equal(19f, text.CalcHeight(), 3);
        }

        [Fact]
        public void TextBaselineUsesTheFontAscent()
        {
            using var fonts = LoadTestFont();
            using var font = new Font(fonts.Families[0], 75); // 100 pixel em at 96 DPI
            using var bitmap = new Bitmap(160, 140);
            using var graphics = Graphics.FromImage(bitmap);
            using var format = StringFormat.GenericTypographic;
            graphics.Clear(Color.White);
            graphics.DrawString("A", font, Brushes.Black, 10, 10, format);
            int lastInkRow = -1;
            for (int y = 0; y < bitmap.Height; y++)
                for (int x = 0; x < bitmap.Width; x++)
                    if (bitmap.GetPixel(x, y).ToArgb() != Color.White.ToArgb()) lastInkRow = y;
            // The test glyph ends at the baseline: top 10 + ascent 80 = 90.
            Assert.Equal(89, lastInkRow);
        }

        [Theory]
        [InlineData("ARIALN.TTF", FontStyle.Regular)]
        [InlineData("ARIALNB.TTF", FontStyle.Bold)]
        [InlineData("ARIALNI.TTF", FontStyle.Italic)]
        [InlineData("ARIALNBI.TTF", FontStyle.Bold | FontStyle.Italic)]
        public void InstalledArialNarrowKeepsItsLegacyFamilyAndCondensedWidth(string filename, FontStyle style)
        {
            // Optional platform integration check: Arial Narrow is not shipped with the tests.
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), filename);
            if (!OperatingSystem.IsWindows() || !File.Exists(path)) return;
            using var fonts = new PrivateFontCollection();
            fonts.AddFontFile(path);
            var narrow = (Font)new FastReport.TypeConverters.FontConverter().ConvertFromInvariantString("Arial Narrow, 9pt, style=" + style);
            using (narrow)
            using (var fromFile = new Font(fonts.Families[0], 9, style))
            using (var bitmap = new Bitmap(1, 1))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                Assert.Equal("Arial Narrow", narrow.Name);
                Assert.Equal(graphics.MeasureString("ITEM DESCRIPTION", fromFile).Width,
                    graphics.MeasureString("ITEM DESCRIPTION", narrow).Width, 2);
                Assert.Equal(fromFile.GetHeight(), narrow.GetHeight(), 3);
            }
        }

        private static PrivateFontCollection LoadTestFont()
        {
            var fonts = new PrivateFontCollection();
            fonts.AddFontFile(Path.Combine(AppContext.BaseDirectory, "Fixtures", "TestSans-Regular.ttf"));
            return fonts;
        }
    }
}
