using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using FastReport.Barcode;
using FastReport.Layout;
using FastReport.Table;
using Xunit;

namespace FastReport.Tests.OpenSource.Layout
{
    public class LayoutTests
    {
        [Fact]
        public void LayoutTypesBelongToTheReportingEngine()
        {
            Assert.Equal(typeof(Report).Assembly, typeof(Padding).Assembly);
            Assert.Equal(typeof(Report).Assembly, typeof(AnchorStyles).Assembly);
            Assert.Equal(typeof(Report).Assembly, typeof(DockStyle).Assembly);
            Assert.Equal(typeof(Report).Assembly, typeof(ImageSizeMode).Assembly);
            Assert.Equal(typeof(Padding), typeof(TextObject).GetProperty("Padding").PropertyType);
            Assert.Equal(typeof(Padding), typeof(PictureObject).GetProperty("Padding").PropertyType);
            Assert.Equal(typeof(Padding), typeof(BarcodeObject).GetProperty("Padding").PropertyType);
            Assert.Equal(typeof(Padding), typeof(TableCell).GetProperty("Padding").PropertyType);
            Assert.Equal(typeof(AnchorStyles), typeof(ComponentBase).GetProperty("Anchor").PropertyType);
            Assert.Equal(typeof(DockStyle), typeof(ComponentBase).GetProperty("Dock").PropertyType);
            Assert.Equal(typeof(ImageSizeMode), typeof(PictureObject).GetProperty("SizeMode").PropertyType);
        }

        [Theory]
        [InlineData("en-US")]
        [InlineData("pt-BR")]
        [InlineData("de-DE")]
        public void PaddingKeepsTheInvariantFrxFormat(string cultureName)
        {
            TypeConverter converter = TypeDescriptor.GetConverter(typeof(Padding));
            var culture = CultureInfo.GetCultureInfo(cultureName);
            var padding = (Padding)converter.ConvertFrom(null, culture, "2, 1, -2, 4");
            Assert.Equal(new Padding(2, 1, -2, 4), padding);
            Assert.Equal("2,1,-2,4", converter.ConvertTo(null, culture, padding, typeof(string)));
            Assert.Equal(new Padding(3, 3, 3, 3), new Padding(3));
        }

        [Theory]
        [InlineData(AnchorStyles.None, 0)]
        [InlineData(AnchorStyles.Top, 1)]
        [InlineData(AnchorStyles.Bottom, 2)]
        [InlineData(AnchorStyles.Left, 4)]
        [InlineData(AnchorStyles.Right, 8)]
        public void AnchorValuesPreserveTheFrxContract(AnchorStyles value, int serializedValue)
        {
            Assert.Equal(serializedValue, (int)value);
        }

        [Fact]
        public void FrxRoundTripPreservesLayoutAndPreparation()
        {
            using var report = new Report();
            report.LoadFromString("""
                <?xml version="1.0" encoding="utf-8"?>
                <Report><ReportPage Name="Page1"><ReportTitleBand Name="Title" Height="100">
                <TextObject Name="Text1" Left="10" Top="5" Width="120" Height="30"
                  Padding="2, 1, 2, 1" Anchor="Top, Left, Right" Text="Portable layout"/>
                <PictureObject Name="Picture1" Top="40" Width="80" Height="40"
                  Padding="1, 2, 3, 4" SizeMode="StretchImage"/>
                <BarcodeObject Name="Barcode1" Left="180" Width="100" Height="40"
                  Padding="3, 2, 3, 2" Text="12345678"/>
                </ReportTitleBand></ReportPage></Report>
                """);

            using var saved = new MemoryStream();
            report.Save(saved);
            saved.Position = 0;
            using var reloaded = new Report();
            reloaded.Load(saved);
            var text = (TextObject)reloaded.FindObject("Text1");
            Assert.Equal(new Padding(2, 1, 2, 1), text.Padding);
            Assert.Equal(AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, text.Anchor);
            Assert.Equal(120, text.Width);
            Assert.Equal(new Padding(1, 2, 3, 4), ((PictureObject)reloaded.FindObject("Picture1")).Padding);
            Assert.Equal(ImageSizeMode.StretchImage, ((PictureObject)reloaded.FindObject("Picture1")).SizeMode);
            Assert.Equal(new Padding(3, 2, 3, 2), ((BarcodeObject)reloaded.FindObject("Barcode1")).Padding);
            Assert.True(reloaded.Prepare());
            Assert.Equal(1, reloaded.PreparedPages.Count);
        }

        [Fact]
        public void AnchoringAndDockingUseReportGeometry()
        {
            using var band = new ReportTitleBand { Width = 200, Height = 60 };
            var anchored = new TextObject
            {
                Parent = band, Left = 10, Top = 5, Width = 100, Height = 20,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            band.Width = 250;
            Assert.Equal(10, anchored.Left);
            Assert.Equal(150, anchored.Width);

            using var dockBand = new ReportTitleBand { Width = 200, Height = 60 };
            var top = new TextObject { Parent = dockBand, Height = 15, Dock = DockStyle.Top };
            var fill = new TextObject { Parent = dockBand, Dock = DockStyle.Fill };
            Assert.Equal(200, top.Width);
            Assert.Equal(15, fill.Top);
            Assert.Equal(45, fill.Height);
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.Vb)]
        public void NewScriptsImportTheReportingLayoutApi(Language language)
        {
            using var report = new Report { ScriptLanguage = language };
            Assert.Contains("FastReport.Layout", report.ScriptText);
            Assert.DoesNotContain("FastReport.Compatibility.Forms", report.ScriptText);
        }
    }
}
