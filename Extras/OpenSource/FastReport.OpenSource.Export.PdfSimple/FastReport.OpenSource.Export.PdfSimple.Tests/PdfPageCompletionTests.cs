using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using FastReport.Drawing;
using FastReport.Export.PdfSimple;
using Xunit;

namespace FastReport.Tests.OpenSource.Export.PdfSimple
{
    public class PdfPageCompletionTests
    {
        [Theory]
        [InlineData(96)]
        [InlineData(300)]
        [InlineData(1200)]
        public void DottedProductRowsAndJustifiedDescriptionsExportThroughTheSummary(int imageDpi)
        {
            string fixtures = Path.Combine(AppContext.BaseDirectory, "Fixtures");
            FontManager.AddFont(Path.Combine(fixtures, "TestSans-Regular.ttf"));
            FontManager.AddFont(Path.Combine(fixtures, "TestSans-Bold.ttf"));
            using var report = new Report();
            // Reproduce the DANFE table: an auto-sized row, justified description,
            // and a dotted bottom border on the first cell, below the page midpoint.
            report.LoadFromString("""
                <?xml version="1.0" encoding="utf-8"?>
                <Report><ReportPage Name="Page1" LeftMargin="6" TopMargin="7" RightMargin="7" BottomMargin="7">
                  <ReportTitleBand Name="Title" Height="600">
                    <TextObject Name="Heading" Width="500" Height="30" Text="PRODUCTS"/>
                  </ReportTitleBand>
                  <DataBand Name="Products" Height="11.34" CanGrow="true" RowCount="5">
                    <TableObject Name="ProductTable" Width="272.16" Height="11.34">
                      <TableColumn Name="CodeColumn" Width="60.48"/>
                      <TableColumn Name="DescriptionColumn" Width="211.68"/>
                      <TableRow Name="ProductRow" Height="11.34" AutoSize="true">
                        <TableCell Name="Code" Text="123" Font="Times New Roman, 6pt"
                          Border.Lines="Left, Right, Bottom" Border.BottomLine.Style="Dot" Border.BottomLine.Width="0.5"/>
                        <TableCell Name="Description" Text="AAA AAA AAA" Font="FastReport Test Sans, 6pt"
                          HorzAlign="Justify" Border.Lines="Right, Bottom" Border.BottomLine.Style="Dot"/>
                      </TableRow>
                    </TableObject>
                  </DataBand>
                  <ReportSummaryBand Name="Summary" Height="40">
                    <TextObject Name="SummaryText" Width="300" Height="30" Text="AAA"
                      Font="FastReport Test Sans, 12pt, style=Bold"/>
                  </ReportSummaryBand>
                </ReportPage></Report>
                """);
            Assert.True(report.Prepare());
            Assert.Equal(1, report.PreparedPages.Count);
            using (var page = report.PreparedPages.GetPage(0))
            {
                Assert.Equal(5, page.Bands.Cast<BandBase>().Count(b => b.Name == "Products"));
                Assert.InRange(page.Bands.Cast<BandBase>().Single(b => b.Name == "Summary").Top, 600, 1000);
            }

            using var output = new MemoryStream();
            using var export = new PDFSimpleExport { ImageDpi = imageDpi };
            report.Export(export, output);
            string pdf = Encoding.Latin1.GetString(output.ToArray());
            Assert.Single(Regex.Matches(pdf, @"/Type\s*/Page\b"));
            // These fonts occur only in descriptions and after the table respectively.
            // A partial export or an invisible description cannot satisfy these checks.
            Assert.Contains("+FastReportTestSans-Regular", pdf);
            Assert.Contains("+FastReportTestSans-Bold", pdf);
        }

        [Fact]
        public void RenderingFailureIsReportedToTheCaller()
        {
            using var report = new Report();
            report.LoadFromString("""
                <?xml version="1.0" encoding="utf-8"?>
                <Report><ReportPage Name="Page1"><ReportTitleBand Name="Title" Height="30">
                  <TextObject Name="Text" Width="100" Height="30" Text="Test"/>
                </ReportTitleBand></ReportPage></Report>
                """);
            Assert.True(report.Prepare());
            using var output = new MemoryStream();
            using var export = new FailingPdfExport();
            Assert.Same(export.Failure, Assert.Throws<InvalidOperationException>(() => report.Export(export, output)));
        }

        private sealed class FailingPdfExport : PDFSimpleExport
        {
            public InvalidOperationException Failure { get; } = new InvalidOperationException("Rendering failed.");
            protected override void ExportBand(BandBase band) => throw Failure;
        }
    }
}
