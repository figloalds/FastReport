using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SkiaSharp;

namespace FastReport.Drawing.Drawing2D
{
    public sealed class GraphicsState
    {
        internal Matrix Transform { get; init; }
        internal FastReport.Drawing.Region Clip { get; init; }
        internal FastReport.Drawing.Text.TextRenderingHint TextRenderingHint { get; init; }
        internal InterpolationMode InterpolationMode { get; init; }
        internal SmoothingMode SmoothingMode { get; init; }
        internal CompositingQuality CompositingQuality { get; init; }
    }

    public enum DashStyle { Solid, Dash, Dot, DashDot, DashDotDot, Custom }
    public enum DashCap { Flat, Round, Triangle }
    public enum LineCap { Flat, Square, Round, Triangle, NoAnchor = 0x10, SquareAnchor, RoundAnchor, DiamondAnchor, ArrowAnchor, AnchorMask = 0xf0, Custom = 0xff }
    public enum LineJoin { Miter, Bevel, Round, MiterClipped }
    public enum WrapMode { Tile, TileFlipX, TileFlipY, TileFlipXY, Clamp }
    public enum CombineMode { Replace, Intersect, Union, Xor, Exclude, Complement }
    public enum MatrixOrder { Prepend, Append }
    public enum InterpolationMode { Invalid = -1, Default, Low, High, Bilinear, Bicubic, NearestNeighbor, HighQualityBilinear, HighQualityBicubic }
    public enum SmoothingMode { Invalid = -1, Default, HighSpeed, HighQuality, None, AntiAlias }
    public enum CompositingQuality { Invalid = -1, Default, HighSpeed, HighQuality, GammaCorrected, AssumeLinear }
    public enum CompositingMode { SourceOver, SourceCopy }
    public enum PixelOffsetMode { Invalid = -1, Default, HighSpeed, HighQuality, None, Half }
    public enum FillMode { Alternate, Winding }
    public enum HatchStyle
    {
        Horizontal, Vertical, ForwardDiagonal, BackwardDiagonal, Cross, DiagonalCross,
        Percent05, Percent10, Percent20, Percent25, Percent30, Percent40, Percent50,
        Percent60, Percent70, Percent75, Percent80, Percent90, LightDownwardDiagonal,
        LightUpwardDiagonal, DarkDownwardDiagonal, DarkUpwardDiagonal, WideDownwardDiagonal,
        WideUpwardDiagonal, LightVertical, LightHorizontal, NarrowVertical, NarrowHorizontal,
        DarkVertical, DarkHorizontal, DashedDownwardDiagonal, DashedUpwardDiagonal, DashedHorizontal,
        DashedVertical, SmallConfetti, LargeConfetti, ZigZag, Wave, DiagonalBrick, HorizontalBrick,
        Weave, Plaid, Divot, DottedGrid, DottedDiamond, Shingle, Trellis, Sphere, SmallGrid,
        SmallCheckerBoard, LargeCheckerBoard, OutlinedDiamond, SolidDiamond
    }
    public enum LinearGradientMode { Horizontal, Vertical, ForwardDiagonal, BackwardDiagonal }

    public sealed class Matrix : IDisposable, ICloneable
    {
        internal Matrix3x2 Value;
        public bool IsIdentity => Value.IsIdentity;
        public float OffsetX => Value.M31;
        public float OffsetY => Value.M32;
        public float[] Elements => new[] { Value.M11, Value.M12, Value.M21, Value.M22, Value.M31, Value.M32 };
        public Matrix() { Value = Matrix3x2.Identity; }
        public Matrix(float m11, float m12, float m21, float m22, float dx, float dy) { Value = new Matrix3x2(m11, m12, m21, m22, dx, dy); }
        internal Matrix(Matrix3x2 value) { Value = value; }
        public void Reset() => Value = Matrix3x2.Identity;
        public void Invert() { if (!Matrix3x2.Invert(Value, out var inverse)) throw new InvalidOperationException("Matrix is not invertible."); Value = inverse; }
        public void Multiply(Matrix matrix, MatrixOrder order = MatrixOrder.Prepend) => Value = order == MatrixOrder.Prepend ? matrix.Value * Value : Value * matrix.Value;
        public void Rotate(float angle, MatrixOrder order = MatrixOrder.Prepend) => Multiply(new Matrix(Matrix3x2.CreateRotation(angle * MathF.PI / 180f)), order);
        public void RotateAt(float angle, FastReport.Drawing.PointF point, MatrixOrder order = MatrixOrder.Prepend)
        {
            var rotation = Matrix3x2.CreateRotation(angle * MathF.PI / 180f, new Vector2(point.X, point.Y));
            Multiply(new Matrix(rotation), order);
        }
        public void Scale(float scaleX, float scaleY, MatrixOrder order = MatrixOrder.Prepend) => Multiply(new Matrix(Matrix3x2.CreateScale(scaleX, scaleY)), order);
        public void Translate(float offsetX, float offsetY, MatrixOrder order = MatrixOrder.Prepend) => Multiply(new Matrix(Matrix3x2.CreateTranslation(offsetX, offsetY)), order);
        public void TransformPoints(FastReport.Drawing.PointF[] points)
        {
            if (points == null) throw new ArgumentNullException(nameof(points));
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 p = Vector2.Transform(new Vector2(points[i].X, points[i].Y), Value);
                points[i] = new FastReport.Drawing.PointF(p.X, p.Y);
            }
        }
        internal SKMatrix ToSkMatrix() => new()
        {
            ScaleX = Value.M11, SkewX = Value.M21, TransX = Value.M31,
            SkewY = Value.M12, ScaleY = Value.M22, TransY = Value.M32,
            Persp0 = 0, Persp1 = 0, Persp2 = 1
        };
        public object Clone() => new Matrix(Value);
        public void Dispose() { }
    }

    public sealed class GraphicsPath : IDisposable, ICloneable
    {
        internal SKPath Path { get; private set; }
        public FillMode FillMode { get => Path.FillType == SKPathFillType.EvenOdd ? FillMode.Alternate : FillMode.Winding; set => Path.FillType = value == FillMode.Alternate ? SKPathFillType.EvenOdd : SKPathFillType.Winding; }
        public FastReport.Drawing.PointF[] PathPoints => Path.Points.Select(p => new FastReport.Drawing.PointF(p.X, p.Y)).ToArray();
        public byte[] PathTypes => Enumerable.Repeat((byte)0, PointCount).ToArray();
        public int PointCount => Path.PointCount;
        public GraphicsPath() { Path = new SKPath(); }
        public GraphicsPath(FillMode fillMode) : this() { FillMode = fillMode; }
        public GraphicsPath(FastReport.Drawing.PointF[] points, byte[] types) : this() { if (points?.Length > 0) AddLines(points); }
        internal GraphicsPath(SKPath path) { Path = path; }
        public void StartFigure() { }
        private void Mutate(Action<SKPathBuilder> action)
        {
            using var builder = new SKPathBuilder(Path);
            action(builder);
            SKPath next = builder.Detach();
            Path.Dispose();
            Path = next;
        }
        public void CloseFigure() => Mutate(builder => builder.Close());
        public void CloseAllFigures() => Mutate(builder => builder.Close());
        public void Reset() { Path.Dispose(); Path = new SKPath(); }
        public void AddLine(float x1, float y1, float x2, float y2) => Mutate(builder => { builder.MoveTo(x1, y1); builder.LineTo(x2, y2); });
        public void AddLine(FastReport.Drawing.PointF p1, FastReport.Drawing.PointF p2) => AddLine(p1.X, p1.Y, p2.X, p2.Y);
        public void AddLines(FastReport.Drawing.PointF[] points)
        {
            if (points == null || points.Length == 0) return;
            Mutate(builder =>
            {
                builder.MoveTo(points[0].X, points[0].Y);
                for (int i = 1; i < points.Length; i++) builder.LineTo(points[i].X, points[i].Y);
            });
        }
        public void AddLines(FastReport.Drawing.Point[] points) => AddLines(points?.Select(p => new FastReport.Drawing.PointF(p.X, p.Y)).ToArray());
        public void AddRectangle(FastReport.Drawing.RectangleF rect) => Mutate(builder => builder.AddRect(rect.ToSkRect()));
        public void AddRectangle(FastReport.Drawing.Rectangle rect) => AddRectangle((FastReport.Drawing.RectangleF)rect);
        public void AddRectangles(FastReport.Drawing.RectangleF[] rects) { if (rects != null) foreach (var rect in rects) AddRectangle(rect); }
        public void AddEllipse(float x, float y, float width, float height) => Mutate(builder => builder.AddOval(new SKRect(x, y, x + width, y + height)));
        public void AddEllipse(FastReport.Drawing.RectangleF rect) => AddEllipse(rect.X, rect.Y, rect.Width, rect.Height);
        public void AddArc(float x, float y, float width, float height, float startAngle, float sweepAngle) => Mutate(builder => builder.AddArc(new SKRect(x, y, x + width, y + height), startAngle, sweepAngle));
        public void AddArc(FastReport.Drawing.RectangleF rect, float startAngle, float sweepAngle) => AddArc(rect.X, rect.Y, rect.Width, rect.Height, startAngle, sweepAngle);
        public void AddPie(float x, float y, float width, float height, float startAngle, float sweepAngle)
        {
            var oval = new SKRect(x, y, x + width, y + height);
            float cx = x + width / 2, cy = y + height / 2;
            Mutate(builder => { builder.MoveTo(cx, cy); builder.ArcTo(oval, startAngle, sweepAngle, false); builder.Close(); });
        }
        public void AddPolygon(FastReport.Drawing.PointF[] points) { AddLines(points); CloseFigure(); }
        public void AddPolygon(FastReport.Drawing.Point[] points) { AddLines(points); CloseFigure(); }
        public void AddBezier(float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4) => Mutate(builder => { builder.MoveTo(x1, y1); builder.CubicTo(x2, y2, x3, y3, x4, y4); });
        public void AddBezier(FastReport.Drawing.PointF p1, FastReport.Drawing.PointF p2, FastReport.Drawing.PointF p3, FastReport.Drawing.PointF p4) => AddBezier(p1.X, p1.Y, p2.X, p2.Y, p3.X, p3.Y, p4.X, p4.Y);
        public void AddBeziers(FastReport.Drawing.PointF[] points)
        {
            if (points == null || points.Length < 4) return;
            Mutate(builder =>
            {
                builder.MoveTo(points[0].X, points[0].Y);
                for (int i = 1; i + 2 < points.Length; i += 3) builder.CubicTo(points[i].X, points[i].Y, points[i + 1].X, points[i + 1].Y, points[i + 2].X, points[i + 2].Y);
            });
        }
        public void AddCurve(FastReport.Drawing.PointF[] points) => AddLines(points);
        public void AddCurve(FastReport.Drawing.PointF[] points, float tension) => AddLines(points);
        public void AddCurve(FastReport.Drawing.PointF[] points, int offset, int numberOfSegments, float tension) => AddLines(points?.Skip(offset).Take(numberOfSegments + 1).ToArray());
        public void AddPath(GraphicsPath addingPath, bool connect) { if (addingPath != null) Mutate(builder => builder.AddPath(addingPath.Path, connect ? SKPathAddMode.Extend : SKPathAddMode.Append)); }
        public void AddString(string text, FastReport.Drawing.FontFamily family, int style, float emSize, FastReport.Drawing.PointF origin, FastReport.Drawing.StringFormat format)
        {
            using var font = new SKFont(family?.Typeface ?? SKTypeface.Default, emSize);
            using SKPath textPath = font.GetTextPath(text ?? string.Empty, new SKPoint(origin.X, origin.Y + emSize));
            Mutate(builder => builder.AddPath(textPath));
        }
        public void AddString(string text, FastReport.Drawing.FontFamily family, int style, float emSize, FastReport.Drawing.RectangleF layoutRect, FastReport.Drawing.StringFormat format) => AddString(text, family, style, emSize, layoutRect.Location, format);
        public FastReport.Drawing.RectangleF GetBounds()
        {
            var bounds = Path.Bounds;
            return new(bounds.Left, bounds.Top, bounds.Width, bounds.Height);
        }
        public bool IsVisible(FastReport.Drawing.PointF point) => Path.Contains(point.X, point.Y);
        public void Transform(Matrix matrix) { if (matrix != null) Path.Transform(matrix.ToSkMatrix()); }
        public void Flatten() { }
        public void Flatten(Matrix matrix) { Transform(matrix); }
        public object Clone() => new GraphicsPath(new SKPath(Path));
        public void Dispose() => Path.Dispose();
    }

    internal static class DrawingConversions
    {
        internal static SKRect ToSkRect(this FastReport.Drawing.RectangleF rect) => new(rect.Left, rect.Top, rect.Right, rect.Bottom);
        internal static SKPoint[] ToSkPoints(this IEnumerable<FastReport.Drawing.PointF> points) => points.Select(p => new SKPoint(p.X, p.Y)).ToArray();
        internal static SKShaderTileMode ToTileMode(this WrapMode mode) => mode == WrapMode.Clamp ? SKShaderTileMode.Clamp : mode == WrapMode.Tile ? SKShaderTileMode.Repeat : SKShaderTileMode.Mirror;
    }
}

namespace FastReport.Drawing
{
    using FastReport.Drawing.Drawing2D;

    public abstract class Brush : IDisposable, ICloneable
    {
        internal abstract SKPaint CreatePaint(RectangleF bounds);
        public abstract object Clone();
        public virtual void Dispose() { }
    }

    public sealed class SolidBrush : Brush
    {
        public Color Color { get; set; }
        public SolidBrush(Color color) { Color = color; }
        internal override SKPaint CreatePaint(RectangleF bounds) => new() { Color = Color.ToSkColor(), Style = SKPaintStyle.Fill, IsAntialias = true };
        public override object Clone() => new SolidBrush(Color);
    }

    public sealed class LinearGradientBrush : Brush
    {
        private readonly PointF start;
        private readonly PointF end;
        private readonly RectangleF rectangle;
        private readonly LinearGradientMode mode;
        private readonly float angleDegrees;
        private readonly bool usesAngle;
        public Color[] LinearColors { get; set; }
        public WrapMode WrapMode { get; set; } = WrapMode.Tile;
        public Matrix Transform { get; set; } = new();
        private float sigmaFocus = .5f;
        private float sigmaScale = 1f;
        public LinearGradientBrush(PointF point1, PointF point2, Color color1, Color color2) { start = point1; end = point2; LinearColors = new[] { color1, color2 }; }
        public LinearGradientBrush(RectangleF rect, Color color1, Color color2, LinearGradientMode linearGradientMode) { rectangle = rect; mode = linearGradientMode; LinearColors = new[] { color1, color2 }; }
        public LinearGradientBrush(Rectangle rect, Color color1, Color color2, LinearGradientMode linearGradientMode) : this((RectangleF)rect, color1, color2, linearGradientMode) { }
        public LinearGradientBrush(RectangleF rect, Color color1, Color color2, float angle)
        {
            rectangle = rect;
            angleDegrees = angle;
            usesAngle = true;
            LinearColors = new[] { color1, color2 };
        }
        public void ResetTransform() => Transform.Reset();
        public void RotateTransform(float angle) => Transform.Rotate(angle, MatrixOrder.Append);
        public void ScaleTransform(float sx, float sy) => Transform.Scale(sx, sy, MatrixOrder.Append);
        public void TranslateTransform(float dx, float dy) => Transform.Translate(dx, dy, MatrixOrder.Append);
        public void SetSigmaBellShape(float focus) => SetSigmaBellShape(focus, 1f);
        public void SetSigmaBellShape(float focus, float scale) { sigmaFocus = Math.Clamp(focus, 0, 1); sigmaScale = Math.Clamp(scale, 0, 1); }
        internal override SKPaint CreatePaint(RectangleF bounds)
        {
            RectangleF r = rectangle.IsEmpty ? bounds : rectangle;
            PointF p1 = start, p2 = end;
            if (p1 == PointF.Empty && p2 == PointF.Empty)
            {
                if (usesAngle)
                {
                    float radians = angleDegrees * MathF.PI / 180f;
                    float dx = MathF.Cos(radians), dy = MathF.Sin(radians);
                    float extent = (MathF.Abs(dx) * r.Width + MathF.Abs(dy) * r.Height) / 2f;
                    float centerX = r.Left + r.Width / 2f, centerY = r.Top + r.Height / 2f;
                    p1 = new PointF(centerX - dx * extent, centerY - dy * extent);
                    p2 = new PointF(centerX + dx * extent, centerY + dy * extent);
                }
                else
                {
                    p1 = mode switch { LinearGradientMode.Vertical => new(r.Left, r.Top), LinearGradientMode.ForwardDiagonal => new(r.Left, r.Top), LinearGradientMode.BackwardDiagonal => new(r.Right, r.Top), _ => new(r.Left, r.Top) };
                    p2 = mode switch { LinearGradientMode.Vertical => new(r.Left, r.Bottom), LinearGradientMode.ForwardDiagonal => new(r.Right, r.Bottom), LinearGradientMode.BackwardDiagonal => new(r.Left, r.Bottom), _ => new(r.Right, r.Top) };
                }
            }
            var sourceColors = LinearColors?.Length >= 2 ? LinearColors : new[] { Color.Black, Color.White };
            SKColor[] colors; float[] positions;
            if (sigmaScale < .999f)
            {
                Color mid = Color.FromArgb(
                    (int)(sourceColors[0].A + (sourceColors[^1].A - sourceColors[0].A) * sigmaScale),
                    (int)(sourceColors[0].R + (sourceColors[^1].R - sourceColors[0].R) * sigmaScale),
                    (int)(sourceColors[0].G + (sourceColors[^1].G - sourceColors[0].G) * sigmaScale),
                    (int)(sourceColors[0].B + (sourceColors[^1].B - sourceColors[0].B) * sigmaScale));
                colors = new[] { sourceColors[0].ToSkColor(), mid.ToSkColor(), sourceColors[^1].ToSkColor() }; positions = new[] { 0f, sigmaFocus, 1f };
            }
            else { colors = sourceColors.Select(c => c.ToSkColor()).ToArray(); positions = null; }
            return new SKPaint { Style = SKPaintStyle.Fill, IsAntialias = true, Shader = SKShader.CreateLinearGradient(new SKPoint(p1.X, p1.Y), new SKPoint(p2.X, p2.Y), colors, positions, WrapMode.ToTileMode(), Transform.ToSkMatrix()) };
        }
        public override object Clone()
        {
            LinearGradientBrush clone = usesAngle
                ? new LinearGradientBrush(rectangle, LinearColors[0], LinearColors[^1], angleDegrees)
                : new LinearGradientBrush(rectangle, LinearColors[0], LinearColors[^1], mode);
            clone.WrapMode = WrapMode;
            clone.LinearColors = LinearColors.ToArray();
            clone.Transform = (Matrix)Transform.Clone();
            clone.sigmaFocus = sigmaFocus;
            clone.sigmaScale = sigmaScale;
            return clone;
        }
        public override void Dispose() => Transform.Dispose();
    }

    public sealed class HatchBrush : Brush
    {
        public HatchStyle HatchStyle { get; }
        public Color ForegroundColor { get; }
        public Color BackgroundColor { get; }
        public HatchBrush(HatchStyle hatchStyle, Color foreColor) : this(hatchStyle, foreColor, Color.Transparent) { }
        public HatchBrush(HatchStyle hatchStyle, Color foreColor, Color backColor) { HatchStyle = hatchStyle; ForegroundColor = foreColor; BackgroundColor = backColor; }
        internal override SKPaint CreatePaint(RectangleF bounds)
        {
            const int tileSize = 8;
            using var bitmap = new SKBitmap(tileSize, tileSize, SKColorType.Bgra8888, SKAlphaType.Premul);
            using (var canvas = new SKCanvas(bitmap))
            using (var foreground = new SKPaint { Color = ForegroundColor.ToSkColor(), Style = SKPaintStyle.Stroke, StrokeWidth = 1, IsAntialias = false })
            {
                canvas.Clear(BackgroundColor.ToSkColor());
                DrawPattern(canvas, foreground, tileSize);
            }
            using SKImage image = SKImage.FromBitmap(bitmap);
            return new SKPaint
            {
                Style = SKPaintStyle.Fill,
                IsAntialias = true,
                Shader = image.ToShader(SKShaderTileMode.Repeat, SKShaderTileMode.Repeat)
            };
        }

        private void DrawPattern(SKCanvas canvas, SKPaint paint, int size)
        {
            bool Is(params HatchStyle[] styles) => styles.Contains(HatchStyle);
            void Horizontal() => canvas.DrawLine(0, size / 2f, size, size / 2f, paint);
            void Vertical() => canvas.DrawLine(size / 2f, 0, size / 2f, size, paint);
            void ForwardDiagonal()
            {
                canvas.DrawLine(-1, size - 1, size - 1, -1, paint);
                canvas.DrawLine(3, size + 3, size + 3, 3, paint);
            }
            void BackwardDiagonal()
            {
                canvas.DrawLine(-1, -1, size - 1, size - 1, paint);
                canvas.DrawLine(3, -5, size + 3, size + 3, paint);
            }

            if (Is(HatchStyle.Horizontal, HatchStyle.LightHorizontal, HatchStyle.DarkHorizontal, HatchStyle.DashedHorizontal, HatchStyle.NarrowHorizontal)) Horizontal();
            else if (Is(HatchStyle.Vertical, HatchStyle.LightVertical, HatchStyle.DarkVertical, HatchStyle.DashedVertical, HatchStyle.NarrowVertical)) Vertical();
            else if (Is(HatchStyle.Cross, HatchStyle.SmallGrid, HatchStyle.DottedGrid, HatchStyle.HorizontalBrick, HatchStyle.Weave, HatchStyle.Plaid)) { Horizontal(); Vertical(); }
            else if (Is(HatchStyle.ForwardDiagonal, HatchStyle.LightUpwardDiagonal, HatchStyle.DarkUpwardDiagonal, HatchStyle.WideUpwardDiagonal, HatchStyle.DashedUpwardDiagonal)) ForwardDiagonal();
            else if (Is(HatchStyle.BackwardDiagonal, HatchStyle.LightDownwardDiagonal, HatchStyle.DarkDownwardDiagonal, HatchStyle.WideDownwardDiagonal, HatchStyle.DashedDownwardDiagonal)) BackwardDiagonal();
            else if (Is(HatchStyle.DiagonalCross, HatchStyle.DottedDiamond, HatchStyle.OutlinedDiamond, HatchStyle.Trellis)) { ForwardDiagonal(); BackwardDiagonal(); }
            else if (Is(HatchStyle.SmallCheckerBoard, HatchStyle.LargeCheckerBoard, HatchStyle.SolidDiamond))
            {
                using var fill = new SKPaint { Color = ForegroundColor.ToSkColor(), Style = SKPaintStyle.Fill };
                canvas.DrawRect(new SKRect(0, 0, size / 2f, size / 2f), fill);
                canvas.DrawRect(new SKRect(size / 2f, size / 2f, size, size), fill);
            }
            else
            {
                int density = HatchStyle switch
                {
                    HatchStyle.Percent05 => 1, HatchStyle.Percent10 => 2, HatchStyle.Percent20 => 3,
                    HatchStyle.Percent25 => 4, HatchStyle.Percent30 => 5, HatchStyle.Percent40 => 6,
                    HatchStyle.Percent50 => 8, HatchStyle.Percent60 => 10, HatchStyle.Percent70 => 11,
                    HatchStyle.Percent75 => 12, HatchStyle.Percent80 => 13, HatchStyle.Percent90 => 15,
                    _ => 4
                };
                using var dots = new SKPaint { Color = ForegroundColor.ToSkColor(), Style = SKPaintStyle.Fill };
                for (int index = 0; index < density; index++)
                {
                    int x = (index * 5 + index / 3) % size;
                    int y = (index * 3 + index / 2) % size;
                    canvas.DrawPoint(x, y, dots);
                }
            }
        }
        public override object Clone() => new HatchBrush(HatchStyle, ForegroundColor, BackgroundColor);
    }

    public sealed class TextureBrush : Brush
    {
        public Image Image { get; }
        public WrapMode WrapMode { get; set; }
        public Matrix Transform { get; set; } = new();
        public TextureBrush(Image image) : this(image, WrapMode.Tile) { }
        public TextureBrush(Image image, WrapMode wrapMode) { Image = image ?? throw new ArgumentNullException(nameof(image)); WrapMode = wrapMode; }
        public void ResetTransform() => Transform.Reset();
        public void ScaleTransform(float sx, float sy) => Transform.Scale(sx, sy, MatrixOrder.Append);
        public void TranslateTransform(float dx, float dy) => Transform.Translate(dx, dy, MatrixOrder.Append);
        internal override SKPaint CreatePaint(RectangleF bounds)
        {
            using SKImage snapshot = Image.Snapshot();
            return new SKPaint { Style = SKPaintStyle.Fill, IsAntialias = true, Shader = snapshot.ToShader(WrapMode.ToTileMode(), WrapMode.ToTileMode(), Transform.ToSkMatrix()) };
        }
        public override object Clone() => new TextureBrush(Image, WrapMode) { Transform = (Matrix)Transform.Clone() };
        public override void Dispose() => Transform.Dispose();
    }

    public sealed class PathGradientBrush : Brush
    {
        public Color CenterColor { get; set; }
        public Color[] SurroundColors { get; set; } = Array.Empty<Color>();
        public PointF CenterPoint { get; set; }
        public WrapMode WrapMode { get; set; }
        public PathGradientBrush(PointF[] points) { if (points?.Length > 0) CenterPoint = new(points.Average(p => p.X), points.Average(p => p.Y)); }
        public PathGradientBrush(Drawing2D.GraphicsPath path) { var b = path?.GetBounds() ?? RectangleF.Empty; CenterPoint = new(b.Left + b.Width / 2, b.Top + b.Height / 2); }
        internal override SKPaint CreatePaint(RectangleF bounds)
        {
            var edge = SurroundColors?.FirstOrDefault() ?? Color.Transparent;
            float radius = Math.Max(bounds.Width, bounds.Height) / 2;
            return new SKPaint { Style = SKPaintStyle.Fill, IsAntialias = true, Shader = SKShader.CreateRadialGradient(new SKPoint(CenterPoint.X, CenterPoint.Y), Math.Max(radius, 1), new[] { CenterColor.ToSkColor(), edge.ToSkColor() }, null, SKShaderTileMode.Clamp) };
        }
        public override object Clone() => new PathGradientBrush(Array.Empty<PointF>()) { CenterColor = CenterColor, SurroundColors = SurroundColors?.ToArray(), CenterPoint = CenterPoint, WrapMode = WrapMode };
    }

    public sealed class Pen : IDisposable, ICloneable
    {
        private Brush brush;
        public Color Color { get => brush is SolidBrush solid ? solid.Color : Color.Black; set { brush?.Dispose(); brush = new SolidBrush(value); } }
        public Brush Brush { get => brush; set { brush = value ?? throw new ArgumentNullException(nameof(value)); } }
        public float Width { get; set; }
        public DashStyle DashStyle { get; set; }
        public float[] DashPattern { get; set; }
        public float DashOffset { get; set; }
        public DashCap DashCap { get; set; }
        public LineCap StartCap { get; set; }
        public LineCap EndCap { get; set; }
        public LineJoin LineJoin { get; set; }
        public float MiterLimit { get; set; } = 10;
        public Matrix Transform { get; set; } = new();
        public Pen(Color color) : this(color, 1) { }
        public Pen(Color color, float width) { brush = new SolidBrush(color); Width = width; }
        public Pen(Brush brush) : this(brush, 1) { }
        public Pen(Brush brush, float width) { this.brush = brush ?? throw new ArgumentNullException(nameof(brush)); Width = width; }
        internal SKPaint CreatePaint(RectangleF bounds)
        {
            SKPaint paint = brush.CreatePaint(bounds);
            paint.Style = SKPaintStyle.Stroke; paint.StrokeWidth = Width; paint.StrokeMiter = MiterLimit;
            paint.StrokeJoin = LineJoin switch { LineJoin.Round => SKStrokeJoin.Round, LineJoin.Bevel => SKStrokeJoin.Bevel, _ => SKStrokeJoin.Miter };
            paint.StrokeCap = StartCap == LineCap.Round || EndCap == LineCap.Round ? SKStrokeCap.Round : StartCap == LineCap.Square || EndCap == LineCap.Square ? SKStrokeCap.Square : SKStrokeCap.Butt;
            float[] intervals = DashStyle == DashStyle.Custom ? DashPattern : DashStyle switch { DashStyle.Dash => new[] { 3f, 1f }, DashStyle.Dot => new[] { 1f, 1f }, DashStyle.DashDot => new[] { 3f, 1f, 1f, 1f }, DashStyle.DashDotDot => new[] { 3f, 1f, 1f, 1f, 1f, 1f }, _ => null };
            if (intervals?.Length >= 2) paint.PathEffect = SKPathEffect.CreateDash(intervals.Select(x => Math.Max(.1f, x * Math.Max(Width, 1))).ToArray(), DashOffset);
            return paint;
        }
        public object Clone() => new Pen((Brush)brush.Clone(), Width) { DashStyle = DashStyle, DashPattern = DashPattern?.ToArray(), DashOffset = DashOffset, DashCap = DashCap, StartCap = StartCap, EndCap = EndCap, LineJoin = LineJoin, MiterLimit = MiterLimit, Transform = (Matrix)Transform.Clone() };
        public void Dispose() { brush?.Dispose(); Transform?.Dispose(); }
    }

    public static class Brushes
    {
        public static Brush Black => new SolidBrush(Color.Black);
        public static Brush Blue => new SolidBrush(Color.Blue);
        public static Brush Green => new SolidBrush(Color.Green);
        public static Brush Gray => new SolidBrush(Color.Gray);
        public static Brush White => new SolidBrush(Color.White);
        public static Brush Red => new SolidBrush(Color.Red);
        public static Brush Transparent => new SolidBrush(Color.Transparent);
    }

    public static class Pens
    {
        public static Pen Black => new(Color.Black);
        public static Pen Blue => new(Color.Blue);
        public static Pen Red => new(Color.Red);
        public static Pen Silver => new(Color.Silver);
    }

    internal static class ColorExtensions
    {
        internal static SKColor ToSkColor(this Color color) => new(color.R, color.G, color.B, color.A);
    }
}
