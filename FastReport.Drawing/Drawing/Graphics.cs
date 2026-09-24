using System;
using System.Collections.Generic;
using System.Linq;
using SkiaSharp;
using SkiaSharp.HarfBuzz;
using FastReport.Drawing.Drawing2D;
using FastReport.Drawing.Imaging;
using FastReport.Drawing.Text;

namespace FastReport.Drawing
{
    public sealed class Region : IDisposable, ICloneable
    {
        internal SKPath Path { get; private set; }
        public Region() { Path = CreateRectanglePath(new SKRect(-1_000_000, -1_000_000, 1_000_000, 1_000_000)); }
        public Region(Rectangle rect) : this((RectangleF)rect) { }
        public Region(RectangleF rect) { Path = CreateRectanglePath(rect.ToSkRect()); }
        public Region(GraphicsPath path) { Path = path == null ? new SKPath() : new SKPath(path.Path); }
        internal Region(SKPath path) { Path = path; }
        public RectangleF GetBounds(Graphics graphics) { var b = Path.Bounds; return new(b.Left, b.Top, b.Width, b.Height); }
        public bool IsEmpty(Graphics graphics) => Path.IsEmpty;
        public void MakeEmpty() { Path.Dispose(); Path = new SKPath(); }
        public void Union(RectangleF rect) { using var p = CreateRectanglePath(rect.ToSkRect()); Combine(p, SKPathOp.Union); }
        public void Intersect(RectangleF rect) { using var p = CreateRectanglePath(rect.ToSkRect()); Combine(p, SKPathOp.Intersect); }
        public void Exclude(RectangleF rect) { using var p = CreateRectanglePath(rect.ToSkRect()); Combine(p, SKPathOp.Difference); }
        private static SKPath CreateRectanglePath(SKRect rectangle)
        {
            using var builder = new SKPathBuilder();
            builder.AddRect(rectangle);
            return builder.Detach();
        }
        private void Combine(SKPath other, SKPathOp operation)
        {
            using var result = new SKPath();
            if (Path.Op(other, operation, result)) { Path.Dispose(); Path = new SKPath(result); }
        }
        internal void Combine(Region other, CombineMode mode)
        {
            if (other == null) return;
            SKPathOp operation = mode switch
            {
                CombineMode.Union => SKPathOp.Union,
                CombineMode.Xor => SKPathOp.Xor,
                CombineMode.Exclude or CombineMode.Complement => SKPathOp.Difference,
                _ => SKPathOp.Intersect
            };
            if (mode == CombineMode.Complement)
            {
                using var result = new SKPath();
                if (other.Path.Op(Path, operation, result)) { Path.Dispose(); Path = new SKPath(result); }
            }
            else
                Combine(other.Path, operation);
        }
        public object Clone() => new Region(new SKPath(Path));
        public void Dispose() => Path?.Dispose();
    }

    /// <summary>
    /// A System.Drawing-free graphics surface implemented by SkiaSharp.
    /// Coordinates use the report engine's traditional 1/96-inch logical unit.
    /// </summary>
    public sealed class Graphics : IDisposable
    {
        private readonly SKCanvas canvas;
        private readonly bool ownsCanvas;
        private readonly Image targetImage;
        private readonly int baseSaveCount;
        private bool disposed;
        private Matrix transform = new();
        // Store the clip in device coordinates so later transforms cannot move it.
        private Region clip;
        public SKCanvas NativeCanvas => canvas;
        public float DpiX { get; }
        public float DpiY { get; }
        public TextRenderingHint TextRenderingHint { get; set; } = TextRenderingHint.AntiAlias;
        public InterpolationMode InterpolationMode { get; set; } = InterpolationMode.Bilinear;
        public SmoothingMode SmoothingMode { get; set; } = SmoothingMode.AntiAlias;
        public CompositingQuality CompositingQuality { get; set; } = CompositingQuality.HighQuality;
        public CompositingMode CompositingMode { get; set; } = CompositingMode.SourceOver;
        public PixelOffsetMode PixelOffsetMode { get; set; } = PixelOffsetMode.Default;
        public GraphicsUnit PageUnit { get; set; } = GraphicsUnit.Display;
        public Matrix Transform { get => (Matrix)transform.Clone(); set { transform = (Matrix)(value?.Clone() ?? new Matrix()); ApplyState(); } }
        public Region Clip
        {
            get
            {
                if (clip == null) return new Region(VisibleClipBounds);
                var result = (Region)clip.Clone();
                using var inverse = new Matrix(canvas.TotalMatrix.ToMatrix3x2());
                inverse.Invert();
                result.Path.Transform(inverse.ToSkMatrix());
                return result;
            }
            set
            {
                clip?.Dispose();
                clip = value == null ? null : (Region)value.Clone();
                clip?.Path.Transform(canvas.TotalMatrix);
                ApplyState();
            }
        }
        public RectangleF VisibleClipBounds { get { var b = canvas.LocalClipBounds; return new(b.Left, b.Top, b.Width, b.Height); } }
        public bool IsClipEmpty => canvas.LocalClipBounds.IsEmpty;

        private Graphics(SKCanvas canvas, float dpiX, float dpiY, bool ownsCanvas, Image targetImage = null)
        {
            this.canvas = canvas ?? throw new ArgumentNullException(nameof(canvas)); this.ownsCanvas = ownsCanvas; this.targetImage = targetImage;
            DpiX = dpiX > 0 ? dpiX : 96; DpiY = dpiY > 0 ? dpiY : 96;
            baseSaveCount = canvas.Save();
        }
        public static Graphics FromImage(Image image)
        {
            if (image?.Bitmap == null) throw new ArgumentNullException(nameof(image));
            return new Graphics(new SKCanvas(image.Bitmap), image.HorizontalResolution, image.VerticalResolution, true, image);
        }
        public static Graphics FromCanvas(SKCanvas canvas, float dpiX = 96, float dpiY = 96) => new(canvas, dpiX, dpiY, false);
        public static Graphics FromHwnd(IntPtr hwnd) => FromImage(new Bitmap(1, 1));
        public static Graphics FromHdc(IntPtr hdc) => FromImage(new Bitmap(1, 1));

        public void Clear(Color color) => canvas.Clear(color.ToSkColor());
        public void Flush() => canvas.Flush();
        public IntPtr GetHdc() => IntPtr.Zero;
        public void ReleaseHdc(IntPtr hdc) { }

        public Drawing2D.GraphicsState Save() => new()
        {
            Transform = (Matrix)transform.Clone(),
            Clip = clip == null ? null : (Region)clip.Clone(),
            TextRenderingHint = TextRenderingHint,
            InterpolationMode = InterpolationMode,
            SmoothingMode = SmoothingMode,
            CompositingQuality = CompositingQuality
        };
        public void Restore(Drawing2D.GraphicsState state)
        {
            if (state == null) return;
            transform = (Matrix)(state.Transform?.Clone() ?? new Matrix());
            clip?.Dispose();
            clip = state.Clip == null ? null : (Region)state.Clip.Clone();
            TextRenderingHint = state.TextRenderingHint;
            InterpolationMode = state.InterpolationMode; SmoothingMode = state.SmoothingMode; CompositingQuality = state.CompositingQuality;
            ApplyState();
        }
        public void ResetTransform() { transform.Reset(); ApplyState(); }
        public void MultiplyTransform(Matrix matrix) => MultiplyTransform(matrix, MatrixOrder.Prepend);
        public void MultiplyTransform(Matrix matrix, MatrixOrder order) { transform.Multiply(matrix, order); ApplyState(); }
        public void RotateTransform(float angle) => RotateTransform(angle, MatrixOrder.Prepend);
        public void RotateTransform(float angle, MatrixOrder order) { transform.Rotate(angle, order); ApplyState(); }
        public void ScaleTransform(float sx, float sy) => ScaleTransform(sx, sy, MatrixOrder.Prepend);
        public void ScaleTransform(float sx, float sy, MatrixOrder order) { transform.Scale(sx, sy, order); ApplyState(); }
        public void TranslateTransform(float dx, float dy) => TranslateTransform(dx, dy, MatrixOrder.Prepend);
        public void TranslateTransform(float dx, float dy, MatrixOrder order) { transform.Translate(dx, dy, order); ApplyState(); }

        private void ApplyState()
        {
            canvas.RestoreToCount(baseSaveCount);
            canvas.Save();
            canvas.ResetMatrix();
            if (clip != null)
                canvas.ClipPath(clip.Path, SKClipOperation.Intersect, true);
            canvas.SetMatrix(transform.ToSkMatrix());
        }

        public void ResetClip()
        {
            clip?.Dispose();
            clip = null;
            ApplyState();
        }
        public void SetClip(Rectangle rect) => SetClip((RectangleF)rect);
        public void SetClip(RectangleF rect) => SetClip(rect, CombineMode.Replace);
        public void SetClip(RectangleF rect, CombineMode mode)
        {
            using var path = new GraphicsPath(); path.AddRectangle(rect); SetClip(path, mode);
        }
        public void SetClip(GraphicsPath path) => SetClip(path, CombineMode.Replace);
        public void SetClip(GraphicsPath path, CombineMode mode)
        {
            if (path == null) return;
            using var incoming = new Region(new SKPath(path.Path));
            incoming.Path.Transform(canvas.TotalMatrix);
            Region nextClip;
            if (mode == CombineMode.Replace || clip == null)
            {
                nextClip = (Region)incoming.Clone();
            }
            else
            {
                nextClip = (Region)clip.Clone();
                nextClip.Combine(incoming, mode);
            }
            clip?.Dispose();
            clip = nextClip;
            ApplyState();
        }
        public bool IsVisible(RectangleF rect) => !RectangleF.Intersect(rect, VisibleClipBounds).IsEmpty;

        private static RectangleF Bounds(PointF[] points)
        {
            if (points == null || points.Length == 0) return RectangleF.Empty;
            float left = points.Min(p => p.X), top = points.Min(p => p.Y), right = points.Max(p => p.X), bottom = points.Max(p => p.Y);
            return RectangleF.FromLTRB(left, top, right, bottom);
        }
        public void DrawLine(Pen pen, float x1, float y1, float x2, float y2) { using var paint = pen.CreatePaint(RectangleF.FromLTRB(Math.Min(x1, x2), Math.Min(y1, y2), Math.Max(x1, x2), Math.Max(y1, y2))); canvas.DrawLine(x1, y1, x2, y2, paint); }
        public void DrawLine(Pen pen, PointF p1, PointF p2) => DrawLine(pen, p1.X, p1.Y, p2.X, p2.Y);
        public void DrawLines(Pen pen, PointF[] points)
        {
            if (points?.Length < 2) return;
            using var builder = new SKPathBuilder();
            builder.MoveTo(points[0].X, points[0].Y);
            for (int i = 1; i < points.Length; i++) builder.LineTo(points[i].X, points[i].Y);
            using SKPath path = builder.Detach();
            using var paint = pen.CreatePaint(Bounds(points));
            canvas.DrawPath(path, paint);
        }
        public void DrawLines(Pen pen, Point[] points) => DrawLines(pen, points?.Select(p => new PointF(p.X, p.Y)).ToArray());
        public void DrawRectangle(Pen pen, float x, float y, float width, float height) { var rect = new RectangleF(x, y, width, height); using var paint = pen.CreatePaint(rect); canvas.DrawRect(rect.ToSkRect(), paint); }
        public void DrawRectangle(Pen pen, Rectangle rect) => DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
        public void DrawRectangle(Pen pen, RectangleF rect) => DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
        public void DrawEllipse(Pen pen, float x, float y, float width, float height) { var rect = new RectangleF(x, y, width, height); using var paint = pen.CreatePaint(rect); canvas.DrawOval(rect.ToSkRect(), paint); }
        public void DrawEllipse(Pen pen, RectangleF rect) => DrawEllipse(pen, rect.X, rect.Y, rect.Width, rect.Height);
        public void DrawArc(Pen pen, float x, float y, float width, float height, float startAngle, float sweepAngle) { var rect = new RectangleF(x, y, width, height); using var paint = pen.CreatePaint(rect); canvas.DrawArc(rect.ToSkRect(), startAngle, sweepAngle, false, paint); }
        public void DrawPie(Pen pen, float x, float y, float width, float height, float startAngle, float sweepAngle) { using var path = new GraphicsPath(); path.AddPie(x, y, width, height, startAngle, sweepAngle); DrawPath(pen, path); }
        public void DrawPolygon(Pen pen, PointF[] points) { using var path = new GraphicsPath(); path.AddPolygon(points); DrawPath(pen, path); }
        public void DrawPolygon(Pen pen, Point[] points) => DrawPolygon(pen, points?.Select(p => new PointF(p.X, p.Y)).ToArray());
        public void DrawCurve(Pen pen, PointF[] points) => DrawLines(pen, points);
        public void DrawCurve(Pen pen, PointF[] points, float tension) => DrawLines(pen, points);
        public void DrawCurve(Pen pen, PointF[] points, int offset, int numberOfSegments, float tension) => DrawLines(pen, points?.Skip(offset).Take(numberOfSegments + 1).ToArray());
        public void DrawPath(Pen pen, GraphicsPath path) { if (path == null) return; using var paint = pen.CreatePaint(path.GetBounds()); canvas.DrawPath(path.Path, paint); }

        public void FillRectangle(Brush brush, float x, float y, float width, float height) => FillRectangle(brush, new RectangleF(x, y, width, height));
        public void FillRectangle(Brush brush, Rectangle rect) => FillRectangle(brush, (RectangleF)rect);
        public void FillRectangle(Brush brush, RectangleF rect) { using var paint = brush.CreatePaint(rect); canvas.DrawRect(rect.ToSkRect(), paint); }
        public void FillEllipse(Brush brush, float x, float y, float width, float height) => FillEllipse(brush, new RectangleF(x, y, width, height));
        public void FillEllipse(Brush brush, RectangleF rect) { using var paint = brush.CreatePaint(rect); canvas.DrawOval(rect.ToSkRect(), paint); }
        public void FillPie(Brush brush, float x, float y, float width, float height, float startAngle, float sweepAngle) { using var path = new GraphicsPath(); path.AddPie(x, y, width, height, startAngle, sweepAngle); FillPath(brush, path); }
        public void FillPolygon(Brush brush, PointF[] points) { using var path = new GraphicsPath(); path.AddPolygon(points); FillPath(brush, path); }
        public void FillPolygon(Brush brush, Point[] points) => FillPolygon(brush, points?.Select(p => new PointF(p.X, p.Y)).ToArray());
        public void FillPath(Brush brush, GraphicsPath path) { if (path == null) return; using var paint = brush.CreatePaint(path.GetBounds()); canvas.DrawPath(path.Path, paint); }
        public void FillRegion(Brush brush, Region region) { if (region == null) return; using var paint = brush.CreatePaint(region.GetBounds(this)); canvas.DrawPath(region.Path, paint); }

        public void DrawImage(Image image, float x, float y) => DrawImage(image, x, y, image.Width, image.Height);
        public void DrawImage(Image image, Point point) => DrawImage(image, point.X, point.Y);
        public void DrawImage(Image image, PointF point) => DrawImage(image, point.X, point.Y);
        public void DrawImage(Image image, Rectangle rect) => DrawImage(image, (RectangleF)rect);
        public void DrawImage(Image image, RectangleF rect) => DrawImage(image, rect.X, rect.Y, rect.Width, rect.Height);
        public void DrawImage(Image image, float x, float y, float width, float height) => DrawImage(image, new RectangleF(x, y, width, height), new RectangleF(0, 0, image.Width, image.Height), GraphicsUnit.Pixel);
        public void DrawImage(Image image, RectangleF destRect, RectangleF srcRect, GraphicsUnit unit) => DrawImageCore(image, destRect, srcRect, null);
        public void DrawImage(Image image, Rectangle destRect, int srcX, int srcY, int srcWidth, int srcHeight, GraphicsUnit unit) => DrawImageCore(image, destRect, new RectangleF(srcX, srcY, srcWidth, srcHeight), null);
        public void DrawImage(Image image, Rectangle destRect, int srcX, int srcY, int srcWidth, int srcHeight, GraphicsUnit unit, ImageAttributes attributes) => DrawImageCore(image, destRect, new RectangleF(srcX, srcY, srcWidth, srcHeight), attributes);
        public void DrawImage(Image image, Rectangle destRect, float srcX, float srcY, float srcWidth, float srcHeight, GraphicsUnit unit, ImageAttributes attributes) => DrawImageCore(image, destRect, new RectangleF(srcX, srcY, srcWidth, srcHeight), attributes);
        private void DrawImageCore(Image image, RectangleF destRect, RectangleF srcRect, ImageAttributes attributes)
        {
            if (image?.Bitmap == null || destRect.Width == 0 || destRect.Height == 0) return;
            using SKImage snapshot = image.Snapshot(); using var paint = new SKPaint { IsAntialias = true };
            var sampling = InterpolationMode is InterpolationMode.NearestNeighbor or InterpolationMode.Low
                ? new SKSamplingOptions(SKFilterMode.Nearest)
                : InterpolationMode is InterpolationMode.HighQualityBicubic or InterpolationMode.Bicubic
                    ? new SKSamplingOptions(SKCubicResampler.Mitchell)
                    : new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear);
            if (attributes?.ColorMatrix != null) paint.ColorFilter = SKColorFilter.CreateColorMatrix(attributes.ColorMatrix.ToSkiaMatrix());
            canvas.DrawImage(snapshot, srcRect.ToSkRect(), destRect.ToSkRect(), sampling, paint);
        }
        public void DrawImageUnscaled(Image image, int x, int y) => DrawImage(image, x, y, image.Width, image.Height);
        public void DrawImageUnscaled(Image image, Rectangle rect) => DrawImage(image, rect.X, rect.Y, image.Width, image.Height);
        public void DrawImage(Image image, PointF[] destPoints)
        {
            if (destPoints?.Length < 3 || image == null) return;
            using var state = new DisposableState(this, Save());
            float m11 = (destPoints[1].X - destPoints[0].X) / image.Width, m12 = (destPoints[1].Y - destPoints[0].Y) / image.Width;
            float m21 = (destPoints[2].X - destPoints[0].X) / image.Height, m22 = (destPoints[2].Y - destPoints[0].Y) / image.Height;
            MultiplyTransform(new Matrix(m11, m12, m21, m22, destPoints[0].X, destPoints[0].Y), MatrixOrder.Prepend); DrawImage(image, 0, 0);
        }

        public SizeF MeasureString(string text, Font font) => MeasureString(text, font, new SizeF(float.MaxValue, float.MaxValue), StringFormat.GenericDefault);
        public SizeF MeasureString(string text, Font font, SizeF layoutArea) => MeasureString(text, font, layoutArea, StringFormat.GenericDefault);
        public SizeF MeasureString(string text, Font font, int width) => MeasureString(text, font, width, StringFormat.GenericDefault);
        public SizeF MeasureString(string text, Font font, int width, StringFormat format) => MeasureString(text, font, new SizeF(width, float.MaxValue), format);
        public SizeF MeasureString(string text, Font font, SizeF layoutArea, StringFormat format)
        {
            LayoutText(text, font, layoutArea, format, out var lines, out _, out _);
            if (lines.Count == 0) return SizeF.Empty;
            using var skFont = font.CreateSkFont(DpiY);
            var metrics = skFont.Metrics;
            bool typographic = ((format?.FormatFlags ?? 0) & StringFormatFlags.FitBlackBox) != 0;
            // GDI's default format includes an overhang allowance. A terminated or
            // multiline paragraph occupies full line spacing, not an extra empty line.
            float height = lines.Count == 1 && !text.EndsWith('\r') && !text.EndsWith('\n')
                ? metrics.Descent - metrics.Ascent : lines.Count * font.GetHeight(DpiY);
            return new SizeF(lines.Max(l => l.Width) + (typographic ? 0 : skFont.Size / 3),
                height + (typographic ? 0 : skFont.Size / 8));
        }
        public void MeasureString(string text, Font font, SizeF layoutArea, StringFormat format, out int charactersFitted, out int linesFilled)
        {
            LayoutText(text, font, layoutArea, format, out var lines, out charactersFitted, out linesFilled);
        }
        public Region[] MeasureCharacterRanges(string text, Font font, RectangleF layoutRect, StringFormat format)
        {
            CharacterRange[] ranges = format?.MeasurableCharacterRanges ?? Array.Empty<CharacterRange>();
            var result = new Region[ranges.Length];
            for (int i = 0; i < ranges.Length; i++)
            {
                int first = Math.Clamp(ranges[i].First, 0, text?.Length ?? 0), length = Math.Clamp(ranges[i].Length, 0, (text?.Length ?? 0) - first);
                float x = layoutRect.Left + MeasureTextWidth((text ?? string.Empty).Substring(0, first), font);
                float width = MeasureTextWidth((text ?? string.Empty).Substring(first, length), font);
                result[i] = new Region(new RectangleF(x, layoutRect.Top, width, font.GetHeight(DpiY)));
            }
            return result;
        }
        public void DrawString(string text, Font font, Brush brush, float x, float y) => DrawString(text, font, brush, x, y, StringFormat.GenericDefault);
        public void DrawString(string text, Font font, Brush brush, PointF point) => DrawString(text, font, brush, point.X, point.Y);
        // A point has no layout boundary. Huge synthetic bounds overflow when transformed
        // by a PDF canvas and can clip away the entire text operation.
        public void DrawString(string text, Font font, Brush brush, float x, float y, StringFormat format) => DrawString(text, font, brush, new RectangleF(x, y, 0, 0), format);
        public void DrawString(string text, Font font, Brush brush, PointF point, StringFormat format) => DrawString(text, font, brush, point.X, point.Y, format);
        public void DrawString(string text, Font font, Brush brush, RectangleF layoutRect) => DrawString(text, font, brush, layoutRect, StringFormat.GenericDefault);
        public void DrawString(string text, Font font, Brush brush, RectangleF layoutRect, StringFormat format)
        {
            if (string.IsNullOrEmpty(text) || font == null || brush == null) return;
            LayoutText(text, font, layoutRect.Size, format, out var lines, out _, out _);
            using var skFont = font.CreateSkFont(DpiY);
            float lineHeight = font.GetHeight(DpiY), totalHeight = lines.Count * lineHeight;
            bool typographic = ((format?.FormatFlags ?? 0) & StringFormatFlags.FitBlackBox) != 0;
            float overhang = typographic ? 0 : skFont.Size / 6;
            float y = format?.LineAlignment switch { StringAlignment.Center => layoutRect.Top + (layoutRect.Height - totalHeight) / 2, StringAlignment.Far => layoutRect.Bottom - totalHeight, _ => layoutRect.Top };
            int saveCount = canvas.Save();
            try
            {
                if (((format?.FormatFlags ?? 0) & StringFormatFlags.NoClip) == 0)
                {
                    // Non-positive layout bounds are unbounded, just as in LayoutText.
                    var bounds = canvas.LocalClipBounds;
                    canvas.ClipRect(new SKRect(
                        layoutRect.Width > 0 ? layoutRect.Left : bounds.Left,
                        layoutRect.Height > 0 ? layoutRect.Top : bounds.Top,
                        layoutRect.Width > 0 ? layoutRect.Right : bounds.Right,
                        layoutRect.Height > 0 ? layoutRect.Bottom : bounds.Bottom));
                }
                foreach (var line in lines)
                {
                    float x = format?.Alignment switch { StringAlignment.Center => layoutRect.Left + (layoutRect.Width - line.Width) / 2, StringAlignment.Far => layoutRect.Right - line.Width - overhang, _ => layoutRect.Left + overhang };
                    DrawShapedLine(line.Text, font, brush, x, y - skFont.Metrics.Ascent); y += lineHeight;
                }
            }
            finally
            {
                canvas.RestoreToCount(saveCount);
            }
        }
        private void DrawShapedLine(string text, Font font, Brush brush, float x, float baseline)
        {
            text ??= string.Empty;
            using SKFont skFont = font.CreateSkFont(DpiY);
            float width;
            using SKPaint paint = brush.CreatePaint(new RectangleF(x, baseline - font.GetHeight(DpiY), MeasureTextWidth(text, font), font.GetHeight(DpiY)));
            try
            {
                using var shaper = new SKShaper(skFont.Typeface);
                var shaped = shaper.Shape(text, skFont);
                width = shaped.Width;
                ushort[] glyphs = shaped.Codepoints.Select(c => (ushort)c).ToArray();
                SKPoint[] points = shaped.Points.Select(p => new SKPoint(p.X + x, p.Y + baseline)).ToArray();
                using var builder = new SKTextBlobBuilder();
                builder.AddPositionedRun(glyphs, skFont, points);
                using SKTextBlob blob = builder.Build();
                canvas.DrawText(blob, 0, 0, paint);
            }
            catch (ArgumentException)
            {
                // Minimal Linux images may not contain a stream-backed system font,
                // which HarfBuzz requires. Skia can still use its default glyph path.
                width = skFont.MeasureText(text, null);
                canvas.DrawText(text, x, baseline, SKTextAlign.Left, skFont, paint);
            }
            if (font.Underline || font.Strikeout)
            {
                using var decorationPaint = brush.CreatePaint(new RectangleF(x, baseline - font.GetHeight(DpiY), width, font.GetHeight(DpiY)));
                decorationPaint.Style = SKPaintStyle.Stroke;
                decorationPaint.StrokeWidth = Math.Max(1, skFont.Size / 16);
                float yy = font.Strikeout ? baseline - lineMetric(font) * .35f : baseline + lineMetric(font) * .08f;
                canvas.DrawLine(x, yy, x + width, yy, decorationPaint);
            }
            static float lineMetric(Font f) => f.GetHeight();
        }
        private float MeasureTextWidth(string text, Font font)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            using SKFont skFont = font.CreateSkFont(DpiY);
            try
            {
                using var shaper = new SKShaper(skFont.Typeface);
                return shaper.Shape(text, skFont).Width;
            }
            catch (ArgumentException)
            {
                return skFont.MeasureText(text, null);
            }
        }
        private void LayoutText(string text, Font font, SizeF layoutArea, StringFormat format, out List<TextLine> lines, out int charsFit, out int linesFit)
        {
            lines = new List<TextLine>(); charsFit = 0; linesFit = 0; text ??= string.Empty; font ??= SystemFonts.DefaultFont;
            if (text.Length == 0) return;
            // GDI+ treats a zero or negative layout bound as unbounded: no wrapping and no
            // line limit. TextObject.CalcSize relies on this when measuring with width 0.
            float maxWidth = layoutArea.Width <= 0 ? 0 : layoutArea.Width; bool noWrap = layoutArea.Width <= 0 || ((format?.FormatFlags ?? 0) & StringFormatFlags.NoWrap) != 0 || float.IsPositiveInfinity(maxWidth) || maxWidth > 1e20f;
            if (!noWrap && ((format?.FormatFlags ?? 0) & StringFormatFlags.FitBlackBox) == 0)
                maxWidth = Math.Max(0, maxWidth - font.SizeInPoints * DpiY / 72f / 3);
            float lineHeight = font.GetHeight(DpiY); int maxLines = layoutArea.Height <= 0 || float.IsPositiveInfinity(layoutArea.Height) || layoutArea.Height > 1e20f ? int.MaxValue : Math.Max(1, (int)Math.Floor(layoutArea.Height / lineHeight + .001f));
            int offset = 0;
            while (offset < text.Length && lines.Count < maxLines)
            {
                // Keep offsets in the original string: TextObject.Break slices it using charsFit.
                int newline = text.AsSpan(offset).IndexOfAny('\r', '\n');
                int end = newline < 0 ? text.Length : offset + newline;
                int newlineLength = end == text.Length ? 0 :
                    text[end] == '\r' && end + 1 < text.Length && text[end + 1] == '\n' ? 2 : 1;
                string paragraph = text.Substring(offset, end - offset);
                int local = 0;
                if (paragraph.Length == 0) { lines.Add(new TextLine(string.Empty, 0)); linesFit++; }
                while (local < paragraph.Length && lines.Count < maxLines)
                {
                    int take = noWrap ? paragraph.Length - local : FitCharacters(paragraph.AsSpan(local), font, maxWidth);
                    if (take <= 0) take = 1;
                    if (!noWrap && local + take < paragraph.Length)
                    {
                        int whitespace = paragraph.LastIndexOfAny(new[] { ' ', '\t', '-' }, local + take - 1, take);
                        if (whitespace >= local) take = whitespace - local + 1;
                    }
                    string line = paragraph.Substring(local, take);
                    if (((format?.FormatFlags ?? 0) & StringFormatFlags.MeasureTrailingSpaces) == 0)
                        line = line.TrimEnd(' ', '\t');
                    float width = MeasureTextWidth(line, font); lines.Add(new TextLine(line, width)); local += take; linesFit++;
                }
                charsFit = offset + local;
                if (local < paragraph.Length) break;
                // Consume the complete line ending even when this is the last fitted line.
                charsFit = end + newlineLength;
                if (newlineLength == 0) break;
                offset = charsFit;
            }
            charsFit = Math.Min(charsFit, text.Length);
        }
        private int FitCharacters(ReadOnlySpan<char> text, Font font, float maxWidth)
        {
            if (maxWidth <= 0) return 0; string value = text.ToString(); if (MeasureTextWidth(value, font) <= maxWidth) return value.Length;
            int low = 0, high = value.Length; while (low < high) { int middle = (low + high + 1) / 2; if (MeasureTextWidth(value[..middle], font) <= maxWidth) low = middle; else high = middle - 1; } return low;
        }
        private readonly record struct TextLine(string Text, float Width);
        private sealed class DisposableState : IDisposable { private Graphics graphics; private readonly Drawing2D.GraphicsState state; public DisposableState(Graphics graphics, Drawing2D.GraphicsState state) { this.graphics = graphics; this.state = state; } public void Dispose() { graphics?.Restore(state); graphics = null; } }
        public void Dispose()
        {
            if (disposed) return;
            canvas.Flush();
            if (ownsCanvas)
                canvas.Dispose();
            else
                canvas.RestoreToCount(baseSaveCount);
            transform.Dispose();
            clip?.Dispose();
            disposed = true;
        }
    }

    internal static class SkiaMatrixExtensions
    {
        internal static System.Numerics.Matrix3x2 ToMatrix3x2(this SKMatrix m) => new(m.ScaleX, m.SkewY, m.SkewX, m.ScaleY, m.TransX, m.TransY);
    }
}
