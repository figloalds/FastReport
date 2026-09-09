using FastReport.Export.PdfSimple;
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using FastReport.Drawing;
using Xunit;

namespace FastReport.Tests.OpenSource.Export.PdfSimple
{
    public class PdfSimpleExportTests
    {
        [Fact]
        public void ExportPdf()
        {
            Report r = new Report();
            r.LoadPrepared("TestReport.fpx");

            PDFSimpleExport export = new PDFSimpleExport();
            byte[] pdfBytes;

            using (MemoryStream ms = new MemoryStream())
            {
                r.Export(export, ms);
                pdfBytes = ms.ToArray();
            }

            string pdf = Encoding.Latin1.GetString(pdfBytes);

            Assert.StartsWith("%PDF-1.", pdf);
            Assert.Equal(4, Regex.Matches(pdf, @"/Type\s*/Page\b").Count);
            Assert.Contains("/Font", pdf);
            Assert.True(pdfBytes.Length > 1_000);
        }

        [Fact]
        public void TestExportPdfInfo()
        {
            Report r = new Report();
            ReportPage page = new ReportPage();
            PageHeaderBand pageHeaderBand = new PageHeaderBand();
            pageHeaderBand.CreateUniqueName();
            pageHeaderBand.Height = 300;
            page.Bands.Add(pageHeaderBand);
            r.Pages.Add(page);
            r.Prepare();

            PDFSimpleExport export = new PDFSimpleExport();
            export.Title = "FastReport OpenSource Test Title dad5dd69-4c07-4789-ab4d-f03d0ba68c9c";
            export.Subject = "FastReport OpenSource Test Subject 7cf3d3d9-716f-4c51-a397-c6389c3100ca";
            export.Keywords = "FastReport OpenSource Test Keywors 2fbbf8b9-2daf-40b5-b216-a4c3130aac56";
            export.Author = "FastReport OpenSource Test Author a1e57c3e-1e0e-4b94-a472-07b5f05fa515";
            string pdf;

            using (MemoryStream ms = new MemoryStream())
            {
                r.Export(export, ms);
                pdf = Encoding.Latin1.GetString(ms.ToArray());
            }

            Assert.Contains(export.Title, pdf);
            Assert.Contains(export.Subject, pdf);
            Assert.Contains(export.Keywords, pdf);
            Assert.Contains(export.Author, pdf);
        }

        [Fact]
        public void TestExportWatermark()
        {
            Report r = new Report();
            r.LoadPrepared("Watermark.fpx");

            using PDFSimpleExport export = new PDFSimpleExport();
            using MemoryStream output = new MemoryStream();
            r.Export(export, output);
            string pdf = Encoding.Latin1.GetString(output.ToArray());
            Assert.StartsWith("%PDF-1.", pdf);
            Assert.Equal(3, Regex.Matches(pdf, @"/Type\s*/Page\b").Count);
        }

        [Theory]
        [InlineData(Language.CSharp, false, "System.Windows.Forms.dll")]
        [InlineData(Language.CSharp, false, "FastReport.Compat.dll")]
        [InlineData(Language.Vb, true, "FastReport.Compat, Version=1.0.0.0")]
        [InlineData(Language.CSharp, true, "System.Windows.Forms")]
        [InlineData(Language.Vb, false, "System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
        [InlineData(Language.Vb, true, "System.Windows.Forms.dll")]
        public async Task LoadFrxRegisterDataPrepareAndExport(Language language, bool prepareAsync, string legacyReference)
        {
            var data = new DataSet("Data");
            DataTable employees = data.Tables.Add("Employees");
            employees.Columns.Add("ID", typeof(int));
            employees.Columns.Add("Name", typeof(string));
            employees.Rows.Add(1, "Alice");
            employees.Rows.Add(2, "Bob");

            var template = XDocument.Load("EndToEnd.frx");
            if (language == Language.Vb)
            {
                template.Root.SetAttributeValue("ScriptLanguage", "Vb");
                template.Root.Element("ScriptText").Value = """
                    Imports System
                    Imports System.Drawing
                    Imports System.Windows.Forms
                    Imports FastReport
                    Namespace FastReport
                        Public Class ReportScript
                            Private Sub Name_BeforePrint(sender As Object, e As EventArgs)
                                ' Legacy desktop names after a VB comment must also migrate.
                                Name.TextColor = Color.DarkGreen
                                Name.Padding = New System.Windows.Forms.Padding(3, 2, 3, 2)
                            End Sub
                        End Class
                    End Namespace
                    """;
            }
            using var report = new Report();
            using var templateStream = new MemoryStream();
            template.Save(templateStream);
            templateStream.Position = 0;
            report.Load(templateStream);
            string originalScript = report.ScriptText;
            report.ReferencedAssemblies = report.ReferencedAssemblies.Concat(new[] { legacyReference }).ToArray();
            report.RegisterData(data);
            report.SetParameterValue("HeadingText", "EMPLOYEES-789");

            Assert.True(prepareAsync ? await report.PrepareAsync() : report.Prepare());
            Assert.Equal(originalScript, report.ScriptText);
            Assert.Equal(1, report.PreparedPages.Count);

            using (var page = report.PreparedPages.GetPage(0))
            {
                var texts = page.AllObjects.OfType<TextObject>().ToArray();
                Assert.Equal("EMPLOYEES-789", texts.Single(t => t.Name == "Heading").Text);
                Assert.Equal(new[] { "Alice", "Bob" }, texts.Where(t => t.Name == "Name").Select(t => t.Text).ToArray());
                Assert.All(texts.Where(t => t.Name == "Name"), text =>
                {
                    Assert.Equal(new FastReport.Layout.Padding(3, 2, 3, 2), text.Padding);
                });
            }

            using var output = new MemoryStream();
            using var export = new PDFSimpleExport();
            report.Export(export, output);
            string pdf = Encoding.Latin1.GetString(output.ToArray());

            Assert.StartsWith("%PDF-1.", pdf);
            Assert.Single(Regex.Matches(pdf, @"/Type\s*/Page\b"));
            Assert.Contains("/Font", pdf);
        }

        [Fact]
        public void RotatedFrxTextIsIncludedInPdf()
        {
            using var report = new Report();
            report.LoadFromString("""
                <?xml version="1.0" encoding="utf-8"?>
                <Report><ReportPage Name="Page1"><ReportTitleBand Name="Title" Height="300">
                <TextObject Name="Rotated" Left="100" Top="50" Width="40" Height="200"
                  Text="ROTATED-TEXT-789" Angle="90" Font="Arial, 12pt" Border.Lines="All"/>
                </ReportTitleBand></ReportPage></Report>
                """);
            Assert.True(report.Prepare());
            using var output = new MemoryStream();
            using var export = new PDFSimpleExport();
            report.Export(export, output);
            // This page has only rotated text: a clipped-away run emits no font resource.
            Assert.Contains("/Font", Encoding.Latin1.GetString(output.ToArray()));
        }

        [Fact]
        public void PrivateFontStylesSurviveFrxPreparationAndPdfEmbedding()
        {
            string fixtures = Path.Combine(Path.GetDirectoryName(typeof(PdfSimpleExportTests).Assembly.Location), "Fixtures");
            FontManager.AddFont(Path.Combine(fixtures, "TestSans-Regular.ttf"));
            FontManager.AddFont(Path.Combine(fixtures, "TestSans-Bold.ttf"));
            using var report = new Report();
            report.LoadFromString("""
                <?xml version="1.0" encoding="utf-8"?>
                <Report><ReportPage Name="Page1"><ReportTitleBand Name="Title" Height="80">
                <TextObject Name="Regular" Width="200" Height="30" Text="AAA" Font="FastReport Test Sans, 12pt"/>
                <TextObject Name="Bold" Top="40" Width="200" Height="30" Text="AAA" Font="FastReport Test Sans, 12pt, style=Bold"/>
                </ReportTitleBand></ReportPage></Report>
                """);
            Assert.True(report.Prepare());
            using var output = new MemoryStream();
            using var export = new PDFSimpleExport();
            report.Export(export, output);
            string pdf = Encoding.Latin1.GetString(output.ToArray());
            Assert.Contains("+FastReportTestSans-Regular", pdf);
            Assert.Contains("+FastReportTestSans-Bold", pdf);
        }

        [Fact]
        public void TestExportPdfImages()
        {
            

            PDFSimpleExport export = new PDFSimpleExport();

            export.ImageDpi = 300;
            export.JpegQuality = 90;
            export.PdfA = true;
            Assert.Equal(300, export.ImageDpi);
            Assert.Equal(90, export.JpegQuality);
            Assert.True(export.PdfA);


            export.ImageDpi = 1200;
            export.JpegQuality = 100;
            Assert.Equal(1200, export.ImageDpi);
            Assert.Equal(100, export.JpegQuality);

            export.ImageDpi = 96;
            export.JpegQuality = 10;
            Assert.Equal(96, export.ImageDpi);
            Assert.Equal(10, export.JpegQuality);

            export.ImageDpi = 300;
            export.JpegQuality = 90;
            Assert.Equal(300, export.ImageDpi);
            Assert.Equal(90, export.JpegQuality);

            export.ImageDpi = 3000;
            export.JpegQuality = 110;
            Assert.Equal(1200, export.ImageDpi);
            Assert.Equal(100, export.JpegQuality);

            export.ImageDpi = 0;
            export.JpegQuality = 0;
            Assert.Equal(96, export.ImageDpi);
            Assert.Equal(10, export.JpegQuality);
        }

        [Fact]
        public void EmbeddedPictureRemainsAnImageObject()
        {
            using var report = new Report();
            var page = new ReportPage();
            var band = new ReportTitleBand { Height = 100 };
            report.Pages.Add(page);
            page.CreateUniqueName();
            page.ReportTitle = band;
            band.CreateUniqueName();
            using var bitmap = new Bitmap(32, 32);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.AliceBlue);
                graphics.FillEllipse(Brushes.Red, 4, 4, 24, 24);
            }
            var picture = new PictureObject
            {
                Bounds = new RectangleF(0, 0, 96, 96),
                Image = (Image)bitmap.Clone()
            };
            picture.Parent = band;
            picture.CreateUniqueName();
            Assert.True(report.Prepare());

            using var output = new MemoryStream();
            using var export = new PDFSimpleExport();
            report.Export(export, output);
            string pdf = Encoding.Latin1.GetString(output.ToArray());

            Assert.Contains("/Subtype /Image", pdf);
        }
    }
}
