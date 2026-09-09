using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using SkiaSharp;

namespace FastReport.Drawing
{
    [Flags]
    public enum FontStyle { Regular = 0, Bold = 1, Italic = 2, Underline = 4, Strikeout = 8 }

    public sealed class FontFamily : IDisposable, IEquatable<FontFamily>
    {
        private readonly SKTypeface typeface;
        private readonly IReadOnlyList<SKTypeface> privateTypefaces;
        public string Name { get; }
        internal SKTypeface Typeface => typeface;
        public static FontFamily GenericSansSerif => new("Arial");
        public static FontFamily GenericSerif => new("Times New Roman");
        public static FontFamily GenericMonospace => new("Courier New");
        public static FontFamily[] Families => SKFontManager.Default.FontFamilies.Select(name => new FontFamily(name)).ToArray();

        public FontFamily(string name)
        {
            Name = string.IsNullOrWhiteSpace(name) ? "Arial" : name;
            typeface = ResolveTypeface(Name);
        }

        internal FontFamily(string name, SKTypeface typeface, IReadOnlyList<SKTypeface> privateTypefaces = null)
        {
            Name = string.IsNullOrWhiteSpace(name) ? typeface?.FamilyName ?? "Arial" : name;
            this.typeface = typeface ?? SKTypeface.Default;
            this.privateTypefaces = privateTypefaces;
        }

        internal SKTypeface ResolveStyle(FontStyle style)
        {
            int weight = (style & FontStyle.Bold) != 0 ? 700 : 400;
            var slant = (style & FontStyle.Italic) != 0 ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright;
            if (privateTypefaces == null)
                return SKTypeface.FromFamilyName(Name, new SKFontStyle(weight, (int)SKFontStyleWidth.Normal, slant)) ?? typeface;

            // Search only this collection: system lookup can silently substitute another family.
            return privateTypefaces
                .Where(face => string.Equals(face.FamilyName, Name, StringComparison.OrdinalIgnoreCase))
                .OrderBy(face => (face.FontSlant == slant ? 0 : 1000) + Math.Abs(face.FontWeight - weight))
                .FirstOrDefault() ?? typeface;
        }

        private static SKTypeface ResolveTypeface(string familyName)
        {
            SKTypeface resolved = SKTypeface.FromFamilyName(familyName) ?? SKTypeface.Default;
            if (resolved == null || resolved.GlyphCount == 0)
            {
                resolved?.Dispose();
                throw new InvalidOperationException(
                    "No usable system font was found. Install fontconfig and at least one TrueType or OpenType font family, or register a private font with Config.PrivateFontCollection.");
            }
            return resolved;
        }

        public bool IsStyleAvailable(FontStyle style) => true;
        public int GetEmHeight(FontStyle style) => 2048;
        public int GetCellAscent(FontStyle style) => 1854;
        public int GetCellDescent(FontStyle style) => 434;
        public int GetLineSpacing(FontStyle style) => 2355;
        public bool Equals(FontFamily other) => other != null && string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);
        public override bool Equals(object obj) => Equals(obj as FontFamily);
        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Name);
        public override string ToString() => Name;
        public void Dispose() { /* typefaces are cached and owned by Skia */ }
    }

    public sealed class Font : IDisposable, ICloneable, IEquatable<Font>
    {
        public FontFamily FontFamily { get; }
        public string Name => FontFamily.Name;
        public float Size { get; }
        public float SizeInPoints => Unit switch
        {
            GraphicsUnit.Point => Size,
            GraphicsUnit.Pixel or GraphicsUnit.Display or GraphicsUnit.World => Size * 72f / 96f,
            GraphicsUnit.Inch => Size * 72f,
            GraphicsUnit.Millimeter => Size * 72f / 25.4f,
            GraphicsUnit.Document => Size * 72f / 300f,
            _ => Size
        };
        public FontStyle Style { get; }
        public GraphicsUnit Unit { get; }
        public byte GdiCharSet { get; }
        public bool GdiVerticalFont { get; }
        public bool Bold => (Style & FontStyle.Bold) != 0;
        public bool Italic => (Style & FontStyle.Italic) != 0;
        public bool Underline => (Style & FontStyle.Underline) != 0;
        public bool Strikeout => (Style & FontStyle.Strikeout) != 0;
        public int Height => (int)Math.Ceiling(GetHeight());

        public Font(string familyName, float emSize) : this(familyName, emSize, FontStyle.Regular, GraphicsUnit.Point) { }
        public Font(string familyName, float emSize, FontStyle style) : this(familyName, emSize, style, GraphicsUnit.Point) { }
        public Font(string familyName, float emSize, FontStyle style, GraphicsUnit unit) : this(new FontFamily(familyName), emSize, style, unit, 1, false) { }
        public Font(string familyName, float emSize, FontStyle style, GraphicsUnit unit, byte gdiCharSet) : this(new FontFamily(familyName), emSize, style, unit, gdiCharSet, false) { }
        public Font(string familyName, float emSize, FontStyle style, GraphicsUnit unit, byte gdiCharSet, bool gdiVerticalFont) : this(new FontFamily(familyName), emSize, style, unit, gdiCharSet, gdiVerticalFont) { }
        public Font(FontFamily family, float emSize) : this(family, emSize, FontStyle.Regular, GraphicsUnit.Point) { }
        public Font(FontFamily family, float emSize, FontStyle style) : this(family, emSize, style, GraphicsUnit.Point) { }
        public Font(FontFamily family, float emSize, FontStyle style, GraphicsUnit unit) : this(family, emSize, style, unit, 1, false) { }
        public Font(FontFamily family, float emSize, FontStyle style, GraphicsUnit unit, byte gdiCharSet) : this(family, emSize, style, unit, gdiCharSet, false) { }
        public Font(FontFamily family, float emSize, FontStyle style, GraphicsUnit unit, byte gdiCharSet, bool gdiVerticalFont)
        {
            if (emSize <= 0 || float.IsNaN(emSize) || float.IsInfinity(emSize)) throw new ArgumentException("Font size must be positive.", nameof(emSize));
            FontFamily = family ?? FontFamily.GenericSansSerif;
            Size = emSize;
            Style = style;
            Unit = unit;
            GdiCharSet = gdiCharSet;
            GdiVerticalFont = gdiVerticalFont;
        }
        public Font(Font prototype, FontStyle newStyle) : this(prototype?.FontFamily, prototype?.Size ?? throw new ArgumentNullException(nameof(prototype)), newStyle, prototype.Unit, prototype.GdiCharSet, prototype.GdiVerticalFont) { }

        internal SKFont CreateSkFont(float dpi = 96f)
        {
            var face = FontFamily.ResolveStyle(Style);
            return new SKFont(face, SizeInPoints * dpi / 72f);
        }

        public float GetHeight() => GetHeight(96f);
        public float GetHeight(float dpi) => SizeInPoints * dpi / 72f * 1.2f;
        public float GetHeight(Graphics graphics) => GetHeight(graphics?.DpiY ?? 96f);
        public object Clone() => new Font(FontFamily, Size, Style, Unit, GdiCharSet, GdiVerticalFont);
        public bool Equals(Font other) => other != null && FontFamily.Equals(other.FontFamily) && Size.Equals(other.Size) && Style == other.Style && Unit == other.Unit && GdiCharSet == other.GdiCharSet && GdiVerticalFont == other.GdiVerticalFont;
        public override bool Equals(object obj) => Equals(obj as Font);
        public override int GetHashCode() => HashCode.Combine(FontFamily, Size, Style, Unit, GdiCharSet, GdiVerticalFont);
        public override string ToString() => $"[Font: Name={Name}, Size={Size}, Units={Unit}, GdiCharSet={GdiCharSet}, GdiVerticalFont={GdiVerticalFont}]";
        public void Dispose() { }
    }

    public static class SystemFonts
    {
        private static readonly Font defaultFont = new("Arial", 8.25f);
        public static Font DefaultFont => defaultFont;
    }
}

namespace FastReport.Drawing.Text
{
    public enum TextRenderingHint { SystemDefault, SingleBitPerPixelGridFit, SingleBitPerPixel, AntiAliasGridFit, AntiAlias, ClearTypeGridFit }
    public enum HotkeyPrefix { None, Show, Hide }

    public abstract class FontCollection : IDisposable
    {
        protected readonly List<FastReport.Drawing.FontFamily> families = new();
        public FastReport.Drawing.FontFamily[] Families => families.ToArray();
        public virtual void Dispose() { }
    }

    public sealed class InstalledFontCollection : FontCollection
    {
        public InstalledFontCollection()
        {
            foreach (string name in SKFontManager.Default.FontFamilies)
                families.Add(new FastReport.Drawing.FontFamily(name));
        }
    }

    public sealed class PrivateFontCollection : FontCollection
    {
        private readonly List<SKTypeface> ownedTypefaces = new();

        public void AddFontFile(string filename)
        {
            var face = SKTypeface.FromFile(filename) ?? throw new ArgumentException($"Unable to load font '{filename}'.", nameof(filename));
            ownedTypefaces.Add(face);
            families.Add(new FastReport.Drawing.FontFamily(face.FamilyName, face, ownedTypefaces));
        }

        public void AddMemoryFont(IntPtr memory, int length)
        {
            if (memory == IntPtr.Zero || length <= 0) throw new ArgumentException("Font memory is empty.");
            byte[] bytes = new byte[length];
            Marshal.Copy(memory, bytes, 0, length);
            using var data = SKData.CreateCopy(bytes);
            var face = SKTypeface.FromData(data) ?? throw new ArgumentException("Unable to load the font data.");
            ownedTypefaces.Add(face);
            families.Add(new FastReport.Drawing.FontFamily(face.FamilyName, face, ownedTypefaces));
        }

        public override void Dispose()
        {
            foreach (var face in ownedTypefaces) face.Dispose();
            ownedTypefaces.Clear();
            families.Clear();
        }
    }
}

namespace FastReport.Drawing
{
    public enum StringAlignment { Near, Center, Far }
    public enum StringTrimming { None, Character, Word, EllipsisCharacter, EllipsisWord, EllipsisPath }
    [Flags]
    public enum StringFormatFlags
    {
        DirectionRightToLeft = 0x00000001, DirectionVertical = 0x00000002, FitBlackBox = 0x00000004,
        DisplayFormatControl = 0x00000020, NoFontFallback = 0x00000400, MeasureTrailingSpaces = 0x00000800,
        NoWrap = 0x00001000, LineLimit = 0x00002000, NoClip = 0x00004000
    }

    public readonly struct CharacterRange
    {
        public int First { get; }
        public int Length { get; }
        public CharacterRange(int first, int length) { First = first; Length = length; }
    }

    public sealed class StringFormat : IDisposable, ICloneable
    {
        private float firstTabOffset;
        private float[] tabStops = Array.Empty<float>();
        private CharacterRange[] ranges = Array.Empty<CharacterRange>();
        public StringAlignment Alignment { get; set; }
        public StringAlignment LineAlignment { get; set; }
        public StringFormatFlags FormatFlags { get; set; }
        public StringTrimming Trimming { get; set; }
        public Text.HotkeyPrefix HotkeyPrefix { get; set; }
        public int DigitSubstitutionLanguage { get; private set; }
        public int DigitSubstitutionMethod { get; private set; }
        public static StringFormat GenericDefault => new();
        public static StringFormat GenericTypographic => new(StringFormatFlags.FitBlackBox | StringFormatFlags.MeasureTrailingSpaces);
        public StringFormat() { }
        public StringFormat(StringFormatFlags options) { FormatFlags = options; }
        public StringFormat(StringFormat source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            Alignment = source.Alignment; LineAlignment = source.LineAlignment; FormatFlags = source.FormatFlags;
            Trimming = source.Trimming; HotkeyPrefix = source.HotkeyPrefix; firstTabOffset = source.firstTabOffset;
            tabStops = (float[])source.tabStops.Clone(); ranges = (CharacterRange[])source.ranges.Clone();
        }
        public void SetTabStops(float firstTabOffset, float[] tabStops) { this.firstTabOffset = firstTabOffset; this.tabStops = tabStops?.ToArray() ?? Array.Empty<float>(); }
        public float[] GetTabStops(out float firstTabOffset) { firstTabOffset = this.firstTabOffset; return tabStops.ToArray(); }
        public void SetMeasurableCharacterRanges(CharacterRange[] ranges) => this.ranges = ranges?.ToArray() ?? Array.Empty<CharacterRange>();
        internal CharacterRange[] MeasurableCharacterRanges => ranges;
        public object Clone() => new StringFormat(this);
        public void Dispose() { }
    }
}
