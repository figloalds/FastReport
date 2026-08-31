using System;
using FastReport.Drawing;
using FastReport.Drawing.Drawing2D;
using FastReport.Utils;
using SkiaSharp;

namespace FastReport.Export.PdfSimple
{
    /// <summary>
    /// Exports prepared reports through Skia's native PDF document backend.
    /// Text and supported geometry remain vector content; effects that Skia cannot
    /// express in PDF are rasterized by Skia at <see cref="ImageDpi"/>.
    /// </summary>
    public partial class PDFSimpleExport : ExportBase
    {
        private const float PdfPointsPerReportPixel = 72f / 96f;
        private SKDocument document;
        private Graphics pageGraphics;

        /// <summary>
        /// Initializes a new PDF exporter.
        /// </summary>
        public PDFSimpleExport() { }

        /// <inheritdoc/>
        protected override void Start()
        {
            base.Start();
            var metadata = new SKDocumentPdfMetadata(ImageDpi, JpegQuality)
            {
                Title = Title ?? string.Empty,
                Author = Author ?? string.Empty,
                Subject = Subject ?? string.Empty,
                Keywords = Keywords ?? string.Empty,
                Creator = "FastReport OpenSource",
                Producer = "FastReport OpenSource / SkiaSharp",
                PdfA = PdfA
            };
            document = SKDocument.CreatePdf(Stream, metadata)
                ?? throw new InvalidOperationException("Skia could not create the PDF document.");
        }

        /// <inheritdoc/>
        protected override void ExportPageBegin(ReportPage page)
        {
            base.ExportPageBegin(page);

            float pageWidthPoints = ExportUtils.GetPageWidth(page) * Units.Millimeters * PdfPointsPerReportPixel;
            float pageHeightPoints = ExportUtils.GetPageHeight(page) * Units.Millimeters * PdfPointsPerReportPixel;
            SKCanvas canvas = document.BeginPage(pageWidthPoints, pageHeightPoints)
                ?? throw new InvalidOperationException("Skia could not begin a PDF page.");

            pageGraphics = Graphics.FromCanvas(canvas, 96, 96);

            // SKDocument's PDF canvas uses RasterDpi logical units and applies its
            // own 72 / RasterDpi transform when writing the content stream. Report
            // coordinates are 1/96 inch, so this compensating scale produces the
            // required 72/96 point mapping while retaining ImageDpi for fallback
            // rasterization quality.
            float reportPixelsToPdfCanvasUnits = ImageDpi / 96f;
            pageGraphics.ScaleTransform(reportPixelsToPdfCanvasUnits, reportPixelsToPdfCanvasUnits, MatrixOrder.Append);
            pageGraphics.TranslateTransform(page.LeftMargin * Units.Millimeters, page.TopMargin * Units.Millimeters, MatrixOrder.Prepend);

            using (var pageFill = new TextObject
            {
                Fill = page.Fill,
                Left = -page.LeftMargin * Units.Millimeters,
                Top = -page.TopMargin * Units.Millimeters,
                Width = ExportUtils.GetPageWidth(page) * Units.Millimeters,
                Height = ExportUtils.GetPageHeight(page) * Units.Millimeters
            })
            {
                ExportObj(pageFill);
            }

            if (page.Watermark.Enabled && !page.Watermark.ShowImageOnTop)
                AddImageWatermark(page);
            if (page.Watermark.Enabled && !page.Watermark.ShowTextOnTop)
                AddTextWatermark(page);

            if (page.Border.Lines != BorderLines.None)
            {
                using var pageBorder = new TextObject
                {
                    Border = page.Border,
                    Left = 0,
                    Top = 0,
                    Width = (ExportUtils.GetPageWidth(page) - page.LeftMargin - page.RightMargin) * Units.Millimeters,
                    Height = (ExportUtils.GetPageHeight(page) - page.TopMargin - page.BottomMargin) * Units.Millimeters
                };
                ExportObj(pageBorder);
            }
        }

        /// <inheritdoc/>
        protected override void ExportBand(BandBase band)
        {
            base.ExportBand(band);
            ExportObj(band);
            foreach (Base child in band.ForEachAllConvectedObjects(this))
            {
                if (child is not (Table.TableColumn or Table.TableCell or Table.TableRow))
                    ExportObj(child);
            }
        }

        /// <inheritdoc/>
        protected override void ExportPageEnd(ReportPage page)
        {
            if (page.Watermark.Enabled && page.Watermark.ShowImageOnTop)
                AddImageWatermark(page);
            if (page.Watermark.Enabled && page.Watermark.ShowTextOnTop)
                AddTextWatermark(page);

            pageGraphics?.Flush();
            pageGraphics?.Dispose();
            pageGraphics = null;
            document.EndPage();
            base.ExportPageEnd(page);
        }

        /// <inheritdoc/>
        protected override void Finish()
        {
            pageGraphics?.Dispose();
            pageGraphics = null;
            document?.Close();
            document?.Dispose();
            document = null;
            Stream.Flush();
            base.Finish();
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                pageGraphics?.Dispose();
                pageGraphics = null;
                document?.Dispose();
                document = null;
            }
            base.Dispose(disposing);
        }

        /// <inheritdoc/>
        protected override string GetFileFilter() => new MyRes("FileFilters").Get("PdfFile");

        private void ExportObj(Base obj)
        {
            if (pageGraphics != null && obj is ReportComponentBase component && component.Exportable)
                component.Draw(new FRPaintEventArgs(pageGraphics, 1, 1, Report.GraphicCache));
        }

        private RectangleF PageWatermarkBounds(ReportPage page) => new(
            -page.LeftMargin * Units.Millimeters,
            -page.TopMargin * Units.Millimeters,
            ExportUtils.GetPageWidth(page) * Units.Millimeters,
            ExportUtils.GetPageHeight(page) * Units.Millimeters);

        private void AddImageWatermark(ReportPage page)
        {
            if (pageGraphics != null)
                page.Watermark.DrawImage(new FRPaintEventArgs(pageGraphics, 1, 1, Report.GraphicCache), PageWatermarkBounds(page), page.Report, false);
        }

        private void AddTextWatermark(ReportPage page)
        {
            if (pageGraphics != null && !string.IsNullOrEmpty(page.Watermark.Text))
                page.Watermark.DrawText(new FRPaintEventArgs(pageGraphics, 1, 1, Report.GraphicCache), PageWatermarkBounds(page), page.Report, false);
        }
    }
}
