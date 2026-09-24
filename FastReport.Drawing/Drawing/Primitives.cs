using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using SkiaSharp;

namespace FastReport.Drawing
{
    public struct Point : IEquatable<Point>
    {
        public static readonly Point Empty;
        public int X { get; set; }
        public int Y { get; set; }
        public bool IsEmpty => X == 0 && Y == 0;
        public Point(int x, int y) { X = x; Y = y; }
        public Point(Size size) : this(size.Width, size.Height) { }
        public static Point Add(Point point, Size size) => point + size;
        public static Point Subtract(Point point, Size size) => point - size;
        public static Point operator +(Point point, Size size) => new(point.X + size.Width, point.Y + size.Height);
        public static Point operator -(Point point, Size size) => new(point.X - size.Width, point.Y - size.Height);
        public static explicit operator Size(Point point) => new(point.X, point.Y);
        public static implicit operator PointF(Point point) => new(point.X, point.Y);
        public bool Equals(Point other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is Point other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(X, Y);
        public static bool operator ==(Point left, Point right) => left.Equals(right);
        public static bool operator !=(Point left, Point right) => !left.Equals(right);
        public override string ToString() => $"{{X={X},Y={Y}}}";
    }

    public struct PointF : IEquatable<PointF>
    {
        public static readonly PointF Empty;
        public float X { get; set; }
        public float Y { get; set; }
        public bool IsEmpty => X == 0 && Y == 0;
        public PointF(float x, float y) { X = x; Y = y; }
        public static PointF Add(PointF point, SizeF size) => point + size;
        public static PointF Subtract(PointF point, SizeF size) => point - size;
        public static PointF operator +(PointF point, SizeF size) => new(point.X + size.Width, point.Y + size.Height);
        public static PointF operator -(PointF point, SizeF size) => new(point.X - size.Width, point.Y - size.Height);
        public bool Equals(PointF other) => X.Equals(other.X) && Y.Equals(other.Y);
        public override bool Equals(object obj) => obj is PointF other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(X, Y);
        public static bool operator ==(PointF left, PointF right) => left.Equals(right);
        public static bool operator !=(PointF left, PointF right) => !left.Equals(right);
        public override string ToString() => FormattableString.Invariant($"{{X={X}, Y={Y}}}");
    }

    public struct Size : IEquatable<Size>
    {
        public static readonly Size Empty;
        public int Width { get; set; }
        public int Height { get; set; }
        public bool IsEmpty => Width == 0 && Height == 0;
        public Size(int width, int height) { Width = width; Height = height; }
        public Size(Point point) : this(point.X, point.Y) { }
        public static Size Add(Size left, Size right) => left + right;
        public static Size Subtract(Size left, Size right) => left - right;
        public static Size operator +(Size left, Size right) => new(left.Width + right.Width, left.Height + right.Height);
        public static Size operator -(Size left, Size right) => new(left.Width - right.Width, left.Height - right.Height);
        public static implicit operator SizeF(Size size) => new(size.Width, size.Height);
        public bool Equals(Size other) => Width == other.Width && Height == other.Height;
        public override bool Equals(object obj) => obj is Size other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Width, Height);
        public static bool operator ==(Size left, Size right) => left.Equals(right);
        public static bool operator !=(Size left, Size right) => !left.Equals(right);
        public override string ToString() => $"{{Width={Width}, Height={Height}}}";
    }

    public struct SizeF : IEquatable<SizeF>
    {
        public static readonly SizeF Empty;
        public float Width { get; set; }
        public float Height { get; set; }
        public bool IsEmpty => Width == 0 && Height == 0;
        public SizeF(float width, float height) { Width = width; Height = height; }
        public SizeF(PointF point) : this(point.X, point.Y) { }
        public static SizeF Add(SizeF left, SizeF right) => left + right;
        public static SizeF Subtract(SizeF left, SizeF right) => left - right;
        public static SizeF operator +(SizeF left, SizeF right) => new(left.Width + right.Width, left.Height + right.Height);
        public static SizeF operator -(SizeF left, SizeF right) => new(left.Width - right.Width, left.Height - right.Height);
        public static explicit operator PointF(SizeF size) => new(size.Width, size.Height);
        public bool Equals(SizeF other) => Width.Equals(other.Width) && Height.Equals(other.Height);
        public override bool Equals(object obj) => obj is SizeF other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Width, Height);
        public static bool operator ==(SizeF left, SizeF right) => left.Equals(right);
        public static bool operator !=(SizeF left, SizeF right) => !left.Equals(right);
        public override string ToString() => FormattableString.Invariant($"{{Width={Width}, Height={Height}}}");
    }

    public struct Rectangle : IEquatable<Rectangle>
    {
        public static readonly Rectangle Empty;
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public readonly int Left => X;
        public readonly int Top => Y;
        public readonly int Right => X + Width;
        public readonly int Bottom => Y + Height;
        public Point Location { readonly get => new(X, Y); set { X = value.X; Y = value.Y; } }
        public Size Size { readonly get => new(Width, Height); set { Width = value.Width; Height = value.Height; } }
        public readonly bool IsEmpty => Width == 0 && Height == 0 && X == 0 && Y == 0;
        public Rectangle(int x, int y, int width, int height) { X = x; Y = y; Width = width; Height = height; }
        public Rectangle(Point location, Size size) : this(location.X, location.Y, size.Width, size.Height) { }
        public static Rectangle FromLTRB(int left, int top, int right, int bottom) => new(left, top, right - left, bottom - top);
        public readonly bool Contains(int x, int y) => x >= Left && x < Right && y >= Top && y < Bottom;
        public readonly bool Contains(Point point) => Contains(point.X, point.Y);
        public readonly bool Contains(Rectangle rect) => rect.Left >= Left && rect.Right <= Right && rect.Top >= Top && rect.Bottom <= Bottom;
        public void Inflate(int width, int height) { X -= width; Y -= height; Width += width * 2; Height += height * 2; }
        public void Offset(int x, int y) { X += x; Y += y; }
        public readonly bool IntersectsWith(Rectangle rect) => rect.Left < Right && Left < rect.Right && rect.Top < Bottom && Top < rect.Bottom;
        public static Rectangle Intersect(Rectangle a, Rectangle b)
        {
            int x1 = Math.Max(a.Left, b.Left), y1 = Math.Max(a.Top, b.Top);
            int x2 = Math.Min(a.Right, b.Right), y2 = Math.Min(a.Bottom, b.Bottom);
            return x2 >= x1 && y2 >= y1 ? FromLTRB(x1, y1, x2, y2) : Empty;
        }
        public static Rectangle Union(Rectangle a, Rectangle b) => FromLTRB(Math.Min(a.Left, b.Left), Math.Min(a.Top, b.Top), Math.Max(a.Right, b.Right), Math.Max(a.Bottom, b.Bottom));
        public static implicit operator RectangleF(Rectangle rect) => new(rect.X, rect.Y, rect.Width, rect.Height);
        public readonly bool Equals(Rectangle other) => X == other.X && Y == other.Y && Width == other.Width && Height == other.Height;
        public override readonly bool Equals(object obj) => obj is Rectangle other && Equals(other);
        public override readonly int GetHashCode() => HashCode.Combine(X, Y, Width, Height);
        public static bool operator ==(Rectangle left, Rectangle right) => left.Equals(right);
        public static bool operator !=(Rectangle left, Rectangle right) => !left.Equals(right);
        public override readonly string ToString() => $"{{X={X},Y={Y},Width={Width},Height={Height}}}";
    }

    public struct RectangleF : IEquatable<RectangleF>
    {
        public static readonly RectangleF Empty;
        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public readonly float Left => X;
        public readonly float Top => Y;
        public readonly float Right => X + Width;
        public readonly float Bottom => Y + Height;
        public PointF Location { readonly get => new(X, Y); set { X = value.X; Y = value.Y; } }
        public SizeF Size { readonly get => new(Width, Height); set { Width = value.Width; Height = value.Height; } }
        public readonly bool IsEmpty => Width == 0 && Height == 0 && X == 0 && Y == 0;
        public RectangleF(float x, float y, float width, float height) { X = x; Y = y; Width = width; Height = height; }
        public RectangleF(PointF location, SizeF size) : this(location.X, location.Y, size.Width, size.Height) { }
        public static RectangleF FromLTRB(float left, float top, float right, float bottom) => new(left, top, right - left, bottom - top);
        public readonly bool Contains(float x, float y) => x >= Left && x <= Right && y >= Top && y <= Bottom;
        public readonly bool Contains(PointF point) => Contains(point.X, point.Y);
        public readonly bool Contains(RectangleF rect) => rect.Left >= Left && rect.Right <= Right && rect.Top >= Top && rect.Bottom <= Bottom;
        public void Inflate(float width, float height) { X -= width; Y -= height; Width += width * 2; Height += height * 2; }
        public static RectangleF Inflate(RectangleF rect, float width, float height) { rect.Inflate(width, height); return rect; }
        public void Offset(float x, float y) { X += x; Y += y; }
        public void Offset(PointF point) => Offset(point.X, point.Y);
        public readonly bool IntersectsWith(RectangleF rect) => rect.Left < Right && Left < rect.Right && rect.Top < Bottom && Top < rect.Bottom;
        public static RectangleF Intersect(RectangleF a, RectangleF b)
        {
            float x1 = Math.Max(a.Left, b.Left), y1 = Math.Max(a.Top, b.Top);
            float x2 = Math.Min(a.Right, b.Right), y2 = Math.Min(a.Bottom, b.Bottom);
            return x2 >= x1 && y2 >= y1 ? FromLTRB(x1, y1, x2, y2) : Empty;
        }
        public static RectangleF Union(RectangleF a, RectangleF b) => FromLTRB(Math.Min(a.Left, b.Left), Math.Min(a.Top, b.Top), Math.Max(a.Right, b.Right), Math.Max(a.Bottom, b.Bottom));
        public readonly bool Equals(RectangleF other) => X.Equals(other.X) && Y.Equals(other.Y) && Width.Equals(other.Width) && Height.Equals(other.Height);
        public override readonly bool Equals(object obj) => obj is RectangleF other && Equals(other);
        public override readonly int GetHashCode() => HashCode.Combine(X, Y, Width, Height);
        public static bool operator ==(RectangleF left, RectangleF right) => left.Equals(right);
        public static bool operator !=(RectangleF left, RectangleF right) => !left.Equals(right);
        public override readonly string ToString() => FormattableString.Invariant($"{{X={X},Y={Y},Width={Width},Height={Height}}}");
    }

    public enum KnownColor
    {
        Empty = 0,
        ActiveBorder = 1, ActiveCaption, ActiveCaptionText, AppWorkspace, Control, ControlDark,
        ControlDarkDark, ControlLight, ControlLightLight, ControlText, Desktop, GrayText, Highlight,
        HighlightText, HotTrack, InactiveBorder, InactiveCaption, InactiveCaptionText, Info, InfoText,
        Menu, MenuText, ScrollBar, Window, WindowFrame, WindowText, Transparent,
        AliceBlue, AntiqueWhite, Aqua, Aquamarine, Azure, Beige, Bisque, Black, BlanchedAlmond, Blue,
        BlueViolet, Brown, BurlyWood, CadetBlue, Chartreuse, Chocolate, Coral, CornflowerBlue, Cornsilk,
        Crimson, Cyan, DarkBlue, DarkCyan, DarkGoldenrod, DarkGray, DarkGreen, DarkKhaki, DarkMagenta,
        DarkOliveGreen, DarkOrange, DarkOrchid, DarkRed, DarkSalmon, DarkSeaGreen, DarkSlateBlue,
        DarkSlateGray, DarkTurquoise, DarkViolet, DeepPink, DeepSkyBlue, DimGray, DodgerBlue, Firebrick,
        FloralWhite, ForestGreen, Fuchsia, Gainsboro, GhostWhite, Gold, Goldenrod, Gray, Green,
        GreenYellow, Honeydew, HotPink, IndianRed, Indigo, Ivory, Khaki, Lavender, LavenderBlush,
        LawnGreen, LemonChiffon, LightBlue, LightCoral, LightCyan, LightGoldenrodYellow, LightGray,
        LightGreen, LightPink, LightSalmon, LightSeaGreen, LightSkyBlue, LightSlateGray, LightSteelBlue,
        LightYellow, Lime, LimeGreen, Linen, Magenta, Maroon, MediumAquamarine, MediumBlue, MediumOrchid,
        MediumPurple, MediumSeaGreen, MediumSlateBlue, MediumSpringGreen, MediumTurquoise,
        MediumVioletRed, MidnightBlue, MintCream, MistyRose, Moccasin, NavajoWhite, Navy, OldLace,
        Olive, OliveDrab, Orange, OrangeRed, Orchid, PaleGoldenrod, PaleGreen, PaleTurquoise,
        PaleVioletRed, PapayaWhip, PeachPuff, Peru, Pink, Plum, PowderBlue, Purple, Red, RosyBrown,
        RoyalBlue, SaddleBrown, Salmon, SandyBrown, SeaGreen, SeaShell, Sienna, Silver, SkyBlue,
        SlateBlue, SlateGray, Snow, SpringGreen, SteelBlue, Tan, Teal, Thistle, Tomato, Turquoise,
        Violet, Wheat, White, WhiteSmoke, Yellow, YellowGreen, ButtonFace, ButtonHighlight,
        ButtonShadow, GradientActiveCaption, GradientInactiveCaption, MenuBar, MenuHighlight
    }

    [System.ComponentModel.TypeConverter(typeof(ColorConverter))]
    public readonly partial struct Color : IEquatable<Color>
    {
        private static readonly Lazy<IReadOnlyDictionary<string, uint>> NamedColors = new(CreateNamedColors);
        private readonly uint value;
        private readonly string name;
        private Color(uint value, string name = null) { this.value = value; this.name = name; }
        public byte A => (byte)(value >> 24);
        public byte R => (byte)(value >> 16);
        public byte G => (byte)(value >> 8);
        public byte B => (byte)value;
        public bool IsEmpty => value == 0 && name == null;
        public bool IsNamedColor => name != null;
        public bool IsKnownColor => ToKnownColor() != KnownColor.Empty;
        public bool IsSystemColor
        {
            get
            {
                KnownColor knownColor = ToKnownColor();
                return knownColor != KnownColor.Empty &&
                    (knownColor <= KnownColor.WindowText || knownColor > KnownColor.YellowGreen);
            }
        }
        public string Name => name ?? value.ToString("x8", CultureInfo.InvariantCulture);
        public static Color Empty => default;
        public static Color Transparent => new(0x00FFFFFF, nameof(Transparent));
        public static Color Black => new(0xFF000000, nameof(Black));
        public static Color White => new(0xFFFFFFFF, nameof(White));
        public static Color Red => new(0xFFFF0000, nameof(Red));
        public static Color Green => new(0xFF008000, nameof(Green));
        public static Color Blue => new(0xFF0000FF, nameof(Blue));
        public static Color Gray => new(0xFF808080, nameof(Gray));
        public static Color LightGray => new(0xFFD3D3D3, nameof(LightGray));
        public static Color Silver => new(0xFFC0C0C0, nameof(Silver));
        public static Color Orange => new(0xFFFFA500, nameof(Orange));
        public static Color FromArgb(int argb) => new(unchecked((uint)argb));
        public static Color FromArgb(int alpha, Color baseColor) => FromArgb(alpha, baseColor.R, baseColor.G, baseColor.B);
        public static Color FromArgb(int red, int green, int blue) => FromArgb(255, red, green, blue);
        public static Color FromArgb(int alpha, int red, int green, int blue)
        {
            if ((uint)alpha > 255 || (uint)red > 255 || (uint)green > 255 || (uint)blue > 255)
                throw new ArgumentException("Color channels must be between 0 and 255.");
            return new((uint)(alpha << 24 | red << 16 | green << 8 | blue));
        }
        public static Color FromName(string colorName)
        {
            if (string.IsNullOrWhiteSpace(colorName)) return Empty;
            string trimmedName = colorName.Trim();
            if (NamedColors.Value.TryGetValue(trimmedName, out uint argb))
                return new Color(argb, trimmedName);
            return new Color(0, trimmedName);
        }

        private static IReadOnlyDictionary<string, uint> CreateNamedColors()
        {
            var colors = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
            foreach (PropertyInfo property in typeof(SKColors).GetProperties(BindingFlags.Public | BindingFlags.Static))
            {
                if (property.PropertyType == typeof(SKColor) && property.GetValue(null) is SKColor color)
                    colors[property.Name] = (uint)(color.Alpha << 24 | color.Red << 16 | color.Green << 8 | color.Blue);
            }
            foreach (FieldInfo field in typeof(SKColors).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (field.FieldType == typeof(SKColor) && field.GetValue(null) is SKColor color)
                    colors[field.Name] = (uint)(color.Alpha << 24 | color.Red << 16 | color.Green << 8 | color.Blue);
            }
            colors["Grey"] = colors[nameof(Gray)];
            colors["LightGrey"] = colors[nameof(LightGray)];
            foreach (PropertyInfo property in typeof(SystemColors).GetProperties(BindingFlags.Public | BindingFlags.Static))
            {
                if (property.PropertyType == typeof(Color) && property.GetValue(null) is Color color)
                    colors[property.Name] = unchecked((uint)color.ToArgb());
            }
            return colors;
        }
        public static Color FromKnownColor(KnownColor color) => FromName(color.ToString());
        public KnownColor ToKnownColor() => Enum.TryParse<KnownColor>(name, true, out var known) ? known : KnownColor.Empty;
        public int ToArgb() => unchecked((int)value);
        public float GetBrightness()
        {
            float max = Math.Max(R, Math.Max(G, B)) / 255f, min = Math.Min(R, Math.Min(G, B)) / 255f;
            return (max + min) / 2;
        }
        public float GetSaturation()
        {
            float max = Math.Max(R, Math.Max(G, B)) / 255f, min = Math.Min(R, Math.Min(G, B)) / 255f;
            if (max == min) return 0;
            float lightness = (max + min) / 2;
            return lightness <= .5f ? (max - min) / (max + min) : (max - min) / (2 - max - min);
        }
        public float GetHue()
        {
            if (R == G && G == B) return 0;
            float r = R / 255f, g = G / 255f, b = B / 255f;
            float max = Math.Max(r, Math.Max(g, b)), min = Math.Min(r, Math.Min(g, b)), delta = max - min;
            float hue = max == r ? (g - b) / delta : max == g ? 2 + (b - r) / delta : 4 + (r - g) / delta;
            hue *= 60;
            return hue < 0 ? hue + 360 : hue;
        }
        public bool Equals(Color other) => value == other.value && string.Equals(name, other.name, StringComparison.OrdinalIgnoreCase);
        public override bool Equals(object obj) => obj is Color other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(value, name?.ToUpperInvariant());
        public static bool operator ==(Color left, Color right) => left.Equals(right);
        public static bool operator !=(Color left, Color right) => !left.Equals(right);
        public override string ToString() => IsEmpty ? "Color [Empty]" : IsNamedColor ? $"Color [{Name}]" : $"Color [A={A}, R={R}, G={G}, B={B}]";
    }

    public static class SystemColors
    {
        public static Color ActiveBorder => Color.FromArgb(255, 180, 180, 180);
        public static Color ActiveCaption => Color.FromArgb(255, 153, 180, 209);
        public static Color ActiveCaptionText => Color.Black;
        public static Color AppWorkspace => Color.FromArgb(255, 171, 171, 171);
        public static Color Control => Color.FromArgb(255, 240, 240, 240);
        public static Color ControlDark => Color.FromArgb(255, 160, 160, 160);
        public static Color ControlDarkDark => Color.FromArgb(255, 105, 105, 105);
        public static Color ControlLight => Color.FromArgb(255, 227, 227, 227);
        public static Color ControlLightLight => Color.White;
        public static Color ControlText => Color.Black;
        public static Color Desktop => Color.Black;
        public static Color GrayText => Color.FromArgb(255, 109, 109, 109);
        public static Color Window => Color.White;
        public static Color WindowText => Color.Black;
        public static Color Highlight => Color.FromArgb(255, 0, 120, 215);
        public static Color HighlightText => Color.White;
        public static Color HotTrack => Color.FromArgb(255, 0, 102, 204);
        public static Color InactiveBorder => Color.FromArgb(255, 244, 247, 252);
        public static Color InactiveCaption => Color.FromArgb(255, 191, 205, 219);
        public static Color InactiveCaptionText => Color.Black;
        public static Color Info => Color.FromArgb(255, 255, 255, 225);
        public static Color InfoText => Color.Black;
        public static Color Menu => Control;
        public static Color MenuText => Color.Black;
        public static Color ScrollBar => Color.FromArgb(255, 200, 200, 200);
        public static Color WindowFrame => Color.FromArgb(255, 100, 100, 100);
        public static Color ButtonFace => Control;
        public static Color ButtonHighlight => ControlLightLight;
        public static Color ButtonShadow => ControlDark;
        public static Color GradientActiveCaption => Color.FromArgb(255, 185, 209, 234);
        public static Color GradientInactiveCaption => Color.FromArgb(255, 215, 228, 242);
        public static Color MenuBar => Control;
        public static Color MenuHighlight => Highlight;
    }

    public static class ColorTranslator
    {
        public static Color FromHtml(string htmlColor)
        {
            if (string.IsNullOrWhiteSpace(htmlColor)) return Color.Empty;
            string value = htmlColor.Trim();
            if (value.StartsWith('#'))
            {
                value = value[1..];
                if (value.Length == 3)
                    value = string.Concat(value.Select(c => new string(c, 2)));
                if (uint.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint rgb))
                    return value.Length == 8 ? Color.FromArgb(unchecked((int)rgb)) : Color.FromArgb((int)(rgb >> 16) & 255, (int)(rgb >> 8) & 255, (int)rgb & 255);
            }
            return Color.FromName(value);
        }

        public static string ToHtml(Color color) => color.IsNamedColor ? color.Name : $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    [Flags]
    public enum ContentAlignment
    {
        TopLeft = 0x001, TopCenter = 0x002, TopRight = 0x004,
        MiddleLeft = 0x010, MiddleCenter = 0x020, MiddleRight = 0x040,
        BottomLeft = 0x100, BottomCenter = 0x200, BottomRight = 0x400
    }

    public enum GraphicsUnit { World, Display, Pixel, Point, Inch, Document, Millimeter }
    public enum RotateFlipType
    {
        RotateNoneFlipNone, Rotate90FlipNone, Rotate180FlipNone, Rotate270FlipNone,
        RotateNoneFlipX, Rotate90FlipX, Rotate180FlipX, Rotate270FlipX,
        RotateNoneFlipY = Rotate180FlipX, Rotate90FlipY = Rotate270FlipX,
        Rotate180FlipY = RotateNoneFlipX, Rotate270FlipY = Rotate90FlipX
    }
}
