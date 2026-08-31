using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using SkiaSharp;
using FastReport.Drawing.Drawing2D;

namespace FastReport.Drawing.Imaging
{
    [Flags]
    public enum PixelFormat
    {
        Undefined = 0, DontCare = 0, Indexed = 0x00010000, Gdi = 0x00020000, Alpha = 0x00040000,
        PAlpha = 0x00080000, Extended = 0x00100000, Canonical = 0x00200000,
        Format1bppIndexed = 196865, Format4bppIndexed = 197634, Format8bppIndexed = 198659,
        Format16bppGrayScale = 1052676, Format16bppRgb555 = 135173, Format16bppRgb565 = 135174,
        Format16bppArgb1555 = 397319, Format24bppRgb = 137224, Format32bppRgb = 139273,
        Format32bppArgb = 2498570, Format32bppPArgb = 925707, Format48bppRgb = 1060876,
        Format64bppArgb = 3424269, Format64bppPArgb = 1851406, Max = 15
    }
    public enum ImageLockMode { ReadOnly = 1, WriteOnly = 2, ReadWrite = 3, UserInputBuffer = 4 }
    public enum ColorMatrixFlag { Default, SkipGrays, AltGrays }
    public enum ColorAdjustType { Default, Bitmap, Brush, Pen, Text, Count, Any }
    public enum EncoderValue
    {
        ColorTypeCMYK, ColorTypeYCCK, CompressionLZW, CompressionCCITT3, CompressionCCITT4,
        CompressionRle, CompressionNone, ScanMethodInterlaced, ScanMethodNonInterlaced,
        VersionGif87, VersionGif89, RenderProgressive, RenderNonProgressive,
        TransformRotate90, TransformRotate180, TransformRotate270, TransformFlipHorizontal,
        TransformFlipVertical, MultiFrame, LastFrame, Flush, FrameDimensionTime,
        FrameDimensionResolution, FrameDimensionPage
    }

    public sealed class ImageFormat : IEquatable<ImageFormat>
    {
        public Guid Guid { get; }
        public string Name { get; }
        private ImageFormat(string name, string guid) { Name = name; Guid = new Guid(guid); }
        public static ImageFormat MemoryBmp { get; } = new("MemoryBmp", "b96b3caa-0728-11d3-9d7b-0000f81ef32e");
        public static ImageFormat Bmp { get; } = new("Bmp", "b96b3cab-0728-11d3-9d7b-0000f81ef32e");
        public static ImageFormat Emf { get; } = new("Emf", "b96b3cac-0728-11d3-9d7b-0000f81ef32e");
        public static ImageFormat Wmf { get; } = new("Wmf", "b96b3cad-0728-11d3-9d7b-0000f81ef32e");
        public static ImageFormat Jpeg { get; } = new("Jpeg", "b96b3cae-0728-11d3-9d7b-0000f81ef32e");
        public static ImageFormat Png { get; } = new("Png", "b96b3caf-0728-11d3-9d7b-0000f81ef32e");
        public static ImageFormat Gif { get; } = new("Gif", "b96b3cb0-0728-11d3-9d7b-0000f81ef32e");
        public static ImageFormat Tiff { get; } = new("Tiff", "b96b3cb1-0728-11d3-9d7b-0000f81ef32e");
        public static ImageFormat Exif { get; } = new("Exif", "b96b3cb2-0728-11d3-9d7b-0000f81ef32e");
        public static ImageFormat Icon { get; } = new("Icon", "b96b3cb5-0728-11d3-9d7b-0000f81ef32e");
        public bool Equals(ImageFormat other) => other != null && Guid == other.Guid;
        public override bool Equals(object obj) => Equals(obj as ImageFormat);
        public override int GetHashCode() => Guid.GetHashCode();
        public override string ToString() => Name;
    }

    public sealed class ColorMatrix
    {
        private readonly float[][] values;
        public ColorMatrix() : this(new[] { new[] { 1f, 0, 0, 0, 0 }, new[] { 0f, 1, 0, 0, 0 }, new[] { 0f, 0, 1, 0, 0 }, new[] { 0f, 0, 0, 1, 0 }, new[] { 0f, 0, 0, 0, 1 } }) { }
        public ColorMatrix(float[][] newColorMatrix) { values = newColorMatrix ?? throw new ArgumentNullException(nameof(newColorMatrix)); }
        public float this[int row, int column] { get => values[row][column]; set => values[row][column] = value; }
        public float Matrix00 { get => this[0, 0]; set => this[0, 0] = value; } public float Matrix01 { get => this[0, 1]; set => this[0, 1] = value; }
        public float Matrix02 { get => this[0, 2]; set => this[0, 2] = value; } public float Matrix03 { get => this[0, 3]; set => this[0, 3] = value; }
        public float Matrix04 { get => this[0, 4]; set => this[0, 4] = value; } public float Matrix10 { get => this[1, 0]; set => this[1, 0] = value; }
        public float Matrix11 { get => this[1, 1]; set => this[1, 1] = value; } public float Matrix12 { get => this[1, 2]; set => this[1, 2] = value; }
        public float Matrix13 { get => this[1, 3]; set => this[1, 3] = value; } public float Matrix14 { get => this[1, 4]; set => this[1, 4] = value; }
        public float Matrix20 { get => this[2, 0]; set => this[2, 0] = value; } public float Matrix21 { get => this[2, 1]; set => this[2, 1] = value; }
        public float Matrix22 { get => this[2, 2]; set => this[2, 2] = value; } public float Matrix23 { get => this[2, 3]; set => this[2, 3] = value; }
        public float Matrix24 { get => this[2, 4]; set => this[2, 4] = value; } public float Matrix30 { get => this[3, 0]; set => this[3, 0] = value; }
        public float Matrix31 { get => this[3, 1]; set => this[3, 1] = value; } public float Matrix32 { get => this[3, 2]; set => this[3, 2] = value; }
        public float Matrix33 { get => this[3, 3]; set => this[3, 3] = value; } public float Matrix34 { get => this[3, 4]; set => this[3, 4] = value; }
        public float Matrix40 { get => this[4, 0]; set => this[4, 0] = value; } public float Matrix41 { get => this[4, 1]; set => this[4, 1] = value; }
        public float Matrix42 { get => this[4, 2]; set => this[4, 2] = value; } public float Matrix43 { get => this[4, 3]; set => this[4, 3] = value; }
        public float Matrix44 { get => this[4, 4]; set => this[4, 4] = value; }
        internal float[] ToSkiaMatrix() => new[]
        {
            Matrix00, Matrix01, Matrix02, Matrix03, Matrix04,
            Matrix10, Matrix11, Matrix12, Matrix13, Matrix14,
            Matrix20, Matrix21, Matrix22, Matrix23, Matrix24,
            Matrix30, Matrix31, Matrix32, Matrix33, Matrix34
        };
    }

    public sealed class ImageAttributes : IDisposable, ICloneable
    {
        internal ColorMatrix ColorMatrix { get; private set; }
        internal WrapMode WrapMode { get; private set; }
        public void SetColorMatrix(ColorMatrix newColorMatrix) => ColorMatrix = newColorMatrix;
        public void SetColorMatrix(ColorMatrix newColorMatrix, ColorMatrixFlag flags, ColorAdjustType type) => ColorMatrix = newColorMatrix;
        public void ClearColorMatrix() => ColorMatrix = null;
        public void SetWrapMode(WrapMode mode) => WrapMode = mode;
        public void SetWrapMode(WrapMode mode, FastReport.Drawing.Color color) => WrapMode = mode;
        public void SetWrapMode(WrapMode mode, FastReport.Drawing.Color color, bool clamp) => WrapMode = mode;
        public object Clone() { var clone = new ImageAttributes { ColorMatrix = ColorMatrix, WrapMode = WrapMode }; return clone; }
        public void Dispose() { }
    }

    public sealed class BitmapData
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int Stride { get; set; }
        public PixelFormat PixelFormat { get; set; }
        public IntPtr Scan0 { get; set; }
        internal byte[] TemporaryBuffer { get; set; }
        internal GCHandle Pin { get; set; }
        internal ImageLockMode LockMode { get; set; }
        internal FastReport.Drawing.Rectangle Rectangle { get; set; }
    }

    public sealed class Encoder
    {
        public Guid Guid { get; }
        private Encoder(string value) { Guid = new Guid(value); }
        public static Encoder Quality { get; } = new("1d5be4b5-fa4a-452d-9cdd-5db35105e7eb");
        public static Encoder Compression { get; } = new("e09d739d-ccd4-44ee-8eba-3fbf8be4fc58");
        public static Encoder SaveFlag { get; } = new("292266fc-ac40-47bf-8cfc-a85b89a655de");
    }
    public sealed class EncoderParameter : IDisposable { public Encoder Encoder { get; } public object Value { get; } public EncoderParameter(Encoder encoder, long value) { Encoder = encoder; Value = value; } public void Dispose() { } }
    public sealed class EncoderParameters : IDisposable { public EncoderParameter[] Param { get; } public EncoderParameters() : this(1) { } public EncoderParameters(int count) { Param = new EncoderParameter[count]; } public void Dispose() { foreach (var p in Param) p?.Dispose(); } }
    public sealed class ImageCodecInfo
    {
        public string MimeType { get; init; }
        public string FilenameExtension { get; init; }
        public Guid FormatID { get; init; }
        public static ImageCodecInfo[] GetImageEncoders() => new[]
        {
            new ImageCodecInfo { MimeType = "image/jpeg", FilenameExtension = "*.JPG;*.JPEG", FormatID = ImageFormat.Jpeg.Guid },
            new ImageCodecInfo { MimeType = "image/png", FilenameExtension = "*.PNG", FormatID = ImageFormat.Png.Guid },
            new ImageCodecInfo { MimeType = "image/bmp", FilenameExtension = "*.BMP", FormatID = ImageFormat.Bmp.Guid }
        };
    }
}

namespace FastReport.Drawing
{
    using FastReport.Drawing.Imaging;

    public abstract class Image : IDisposable, ICloneable
    {
        private bool disposed;
        internal SKBitmap Bitmap { get; set; }
        public int Width => Bitmap?.Width ?? 0;
        public int Height => Bitmap?.Height ?? 0;
        public Size Size => new(Width, Height);
        public SizeF PhysicalDimension => new(Width, Height);
        public float HorizontalResolution { get; protected set; } = 96;
        public float VerticalResolution { get; protected set; } = 96;
        public virtual PixelFormat PixelFormat { get; protected set; } = PixelFormat.Format32bppPArgb;
        public virtual ImageFormat RawFormat { get; protected set; } = ImageFormat.MemoryBmp;

        protected Image() { }
        protected Image(SKBitmap bitmap, ImageFormat format = null) { Bitmap = bitmap ?? throw new ArgumentNullException(nameof(bitmap)); RawFormat = format ?? ImageFormat.MemoryBmp; }
        public static Image FromStream(Stream stream) => FromStream(stream, false, false);
        public static Image FromStream(Stream stream, bool useEmbeddedColorManagement) => FromStream(stream, useEmbeddedColorManagement, false);
        public static Image FromStream(Stream stream, bool useEmbeddedColorManagement, bool validateImageData)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            using var data = SKData.Create(stream);
            var codec = SKCodec.Create(data) ?? throw new ArgumentException("The stream does not contain a supported image.", nameof(stream));
            var info = codec.Info.WithColorType(SKColorType.Bgra8888).WithAlphaType(SKAlphaType.Premul);
            var bitmap = new SKBitmap(info);
            if (codec.GetPixels(info, bitmap.GetPixels()) is not (SKCodecResult.Success or SKCodecResult.IncompleteInput))
            { bitmap.Dispose(); throw new ArgumentException("The image could not be decoded.", nameof(stream)); }
            return new Bitmap(bitmap, FormatFromCodec(codec.EncodedFormat));
        }
        public static Image FromFile(string filename) { using var stream = File.OpenRead(filename); return FromStream(stream); }
        private static ImageFormat FormatFromCodec(SKEncodedImageFormat format) => format switch { SKEncodedImageFormat.Jpeg => ImageFormat.Jpeg, SKEncodedImageFormat.Png => ImageFormat.Png, SKEncodedImageFormat.Gif => ImageFormat.Gif, SKEncodedImageFormat.Bmp => ImageFormat.Bmp, SKEncodedImageFormat.Ico => ImageFormat.Icon, SKEncodedImageFormat.Wbmp => ImageFormat.Bmp, _ => ImageFormat.MemoryBmp };
        internal SKImage Snapshot() => SKImage.FromBitmap(Bitmap);
        public virtual void Save(Stream stream, ImageFormat format)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            format ??= RawFormat;
            // Skia has no BMP encoder, so BMP is written by a managed encoder to keep
            // real bytes behind the requested format. GIF/TIFF/WMF/EMF/ICO cannot be
            // encoded at all; fail loudly instead of writing PNG bytes under a false name.
            if (format == ImageFormat.Bmp || format == ImageFormat.MemoryBmp)
            {
                using (SKImage bmpImage = Snapshot())
                    BmpEncoder.Save(bmpImage, stream);
                return;
            }
            if (format != ImageFormat.Jpeg && format != ImageFormat.Png)
                throw new NotSupportedException($"Saving images as '{format}' is not supported by the SkiaSharp rendering pipeline. Use PNG or JPEG.");
            SKEncodedImageFormat encoded = format == ImageFormat.Jpeg ? SKEncodedImageFormat.Jpeg : SKEncodedImageFormat.Png;
            using SKImage image = Snapshot(); using SKData data = EncodeImage(image, encoded, encoded == SKEncodedImageFormat.Jpeg ? 90 : 100);
            data.SaveTo(stream);
        }
        public void Save(string filename) { using var stream = File.Create(filename); Save(stream, FormatFromExtension(Path.GetExtension(filename))); }
        public void Save(string filename, ImageFormat format) { using var stream = File.Create(filename); Save(stream, format); }
        public void Save(Stream stream, ImageCodecInfo encoder, EncoderParameters encoderParams)
        {
            int quality = 90;
            if (encoderParams?.Param?.Length > 0 && encoderParams.Param[0]?.Value is long q && encoderParams.Param[0].Encoder == Imaging.Encoder.Quality) quality = (int)Math.Clamp(q, 0, 100);
            if (encoder?.MimeType == "image/bmp")
            {
                using (SKImage bmpImage = Snapshot())
                    BmpEncoder.Save(bmpImage, stream);
                return;
            }
            if (encoder != null && encoder.MimeType != "image/jpeg" && encoder.MimeType != "image/png")
                throw new NotSupportedException($"Saving images as '{encoder.MimeType}' is not supported by the SkiaSharp rendering pipeline. Use PNG or JPEG.");
            ImageFormat format = encoder?.MimeType == "image/jpeg" ? ImageFormat.Jpeg : ImageFormat.Png;
            using SKImage image = Snapshot();
            using SKData data = EncodeImage(image, format == ImageFormat.Jpeg ? SKEncodedImageFormat.Jpeg : SKEncodedImageFormat.Png, quality);
            data.SaveTo(stream);
        }
        private static SKData EncodeImage(SKImage image, SKEncodedImageFormat format, int quality)
        {
            SKData data = image.Encode(format, quality);
            if (data == null && format != SKEncodedImageFormat.Png)
                data = image.Encode(SKEncodedImageFormat.Png, 100);
            return data ?? throw new InvalidOperationException($"Skia could not encode the image as {format} or PNG.");
        }
        public void SaveAdd(Image image, EncoderParameters encoderParams) => throw new NotSupportedException("Multi-frame image saving is not supported by the SkiaSharp rendering pipeline.");
        public void SaveAdd(EncoderParameters encoderParams) => throw new NotSupportedException("Multi-frame image saving is not supported by the SkiaSharp rendering pipeline.");
        private static ImageFormat FormatFromExtension(string extension) => extension?.ToLowerInvariant() switch { ".jpg" or ".jpeg" => ImageFormat.Jpeg, ".bmp" => ImageFormat.Bmp, ".gif" => ImageFormat.Gif, ".tif" or ".tiff" => ImageFormat.Tiff, ".ico" => ImageFormat.Icon, _ => ImageFormat.Png };
        public virtual object Clone() => new Bitmap(Bitmap.Copy(), RawFormat) { HorizontalResolution = HorizontalResolution, VerticalResolution = VerticalResolution, PixelFormat = PixelFormat };
        public virtual void RotateFlip(RotateFlipType rotateFlipType)
        {
            int rotations = ((int)rotateFlipType) & 3; bool flipX = (((int)rotateFlipType) & 4) != 0;
            int width = rotations % 2 == 0 ? Width : Height, height = rotations % 2 == 0 ? Height : Width;
            var result = new SKBitmap(width, height, Bitmap.ColorType, Bitmap.AlphaType);
            using var canvas = new SKCanvas(result); canvas.Translate(width / 2f, height / 2f); canvas.RotateDegrees(rotations * 90); if (flipX) canvas.Scale(-1, 1); canvas.Translate(-Width / 2f, -Height / 2f); canvas.DrawBitmap(Bitmap, 0, 0, new SKSamplingOptions(SKFilterMode.Linear), null);
            Bitmap.Dispose(); Bitmap = result;
        }
        public virtual void Dispose() { if (!disposed) { Bitmap?.Dispose(); Bitmap = null; disposed = true; } GC.SuppressFinalize(this); }
    }

    public class Bitmap : Image
    {
        public Bitmap(int width, int height) : this(width, height, PixelFormat.Format32bppPArgb) { }
        public Bitmap(int width, int height, PixelFormat format) : base(new SKBitmap(Math.Max(width, 1), Math.Max(height, 1), SKColorType.Bgra8888, SKAlphaType.Premul)) { PixelFormat = format; Bitmap.Erase(SKColors.Transparent); }
        public Bitmap(string filename) : this(FromFile(filename)) { }
        public Bitmap(Stream stream) : this(FromStream(stream)) { }
        public Bitmap(Image original) : this(original, original?.Width ?? 1, original?.Height ?? 1) { }
        public Bitmap(Image original, Size newSize) : this(original, newSize.Width, newSize.Height) { }
        public Bitmap(Image original, int width, int height) : this(width, height)
        {
            if (original == null) throw new ArgumentNullException(nameof(original));
            using var canvas = new SKCanvas(Bitmap); using var image = original.Snapshot();
            canvas.DrawImage(image, new SKRect(0, 0, width, height), new SKSamplingOptions(SKFilterMode.Linear), null); RawFormat = original.RawFormat;
        }
        internal Bitmap(SKBitmap bitmap, ImageFormat format = null) : base(bitmap, format) { }
        public void SetResolution(float xDpi, float yDpi) { if (xDpi <= 0 || yDpi <= 0) throw new ArgumentException("DPI must be positive."); HorizontalResolution = xDpi; VerticalResolution = yDpi; }
        public Color GetPixel(int x, int y) { SKColor c = Bitmap.GetPixel(x, y); return Color.FromArgb(c.Alpha, c.Red, c.Green, c.Blue); }
        public void SetPixel(int x, int y, Color color) => Bitmap.SetPixel(x, y, color.ToSkColor());
        public void MakeTransparent() => MakeTransparent(GetPixel(0, Height - 1));
        public void MakeTransparent(Color transparentColor)
        {
            for (int y = 0; y < Height; y++) for (int x = 0; x < Width; x++) { var c = Bitmap.GetPixel(x, y); if (c.Red == transparentColor.R && c.Green == transparentColor.G && c.Blue == transparentColor.B) Bitmap.SetPixel(x, y, new SKColor(c.Red, c.Green, c.Blue, 0)); }
        }
        public BitmapData LockBits(Rectangle rect, ImageLockMode flags, PixelFormat format)
        {
            int bytesPerPixel = format == PixelFormat.Format1bppIndexed ? 0 : 4;
            int stride = bytesPerPixel == 0 ? ((rect.Width + 31) / 32) * 4 : rect.Width * bytesPerPixel;
            byte[] buffer = new byte[stride * rect.Height];
            if (flags != ImageLockMode.WriteOnly && bytesPerPixel == 4)
            {
                for (int y = 0; y < rect.Height; y++) Marshal.Copy(IntPtr.Add(Bitmap.GetPixels(), (rect.Y + y) * Bitmap.RowBytes + rect.X * 4), buffer, y * stride, stride);
            }
            var pin = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            return new BitmapData { Width = rect.Width, Height = rect.Height, Stride = stride, PixelFormat = format, Scan0 = pin.AddrOfPinnedObject(), TemporaryBuffer = buffer, Pin = pin, LockMode = flags, Rectangle = rect };
        }
        public void UnlockBits(BitmapData data)
        {
            if (data == null) return;
            if (data.LockMode != ImageLockMode.ReadOnly)
            {
                if (data.PixelFormat == PixelFormat.Format1bppIndexed)
                {
                    for (int y = 0; y < data.Height; y++) for (int x = 0; x < data.Width; x++) { bool white = (data.TemporaryBuffer[y * data.Stride + x / 8] & (0x80 >> (x & 7))) != 0; Bitmap.SetPixel(data.Rectangle.X + x, data.Rectangle.Y + y, white ? SKColors.White : SKColors.Black); }
                }
                else for (int y = 0; y < data.Height; y++) Marshal.Copy(data.TemporaryBuffer, y * data.Stride, IntPtr.Add(Bitmap.GetPixels(), (data.Rectangle.Y + y) * Bitmap.RowBytes + data.Rectangle.X * 4), data.Stride);
            }
            if (data.Pin.IsAllocated) data.Pin.Free(); data.Scan0 = IntPtr.Zero;
        }
    }

    public class Metafile : Bitmap
    {
        public Metafile(Stream stream, IntPtr referenceHdc) : base(1, 1) => throw new NotSupportedException("EMF/WMF images are not supported by the SkiaSharp rendering pipeline.");
        public Metafile(string filename, IntPtr referenceHdc) : base(1, 1) => throw new NotSupportedException("EMF/WMF images are not supported by the SkiaSharp rendering pipeline.");
    }

    /// <summary>
    /// Writes 24bpp BI_RGB BMP files (SkiaSharp ships a BMP decoder but no encoder).
    /// Partial transparency is composited over white, matching how reports are printed.
    /// </summary>
    internal static class BmpEncoder
    {
        internal static void Save(SKImage image, Stream stream)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            int width = image.Width, height = image.Height;
            var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Unpremul);
            using var bitmap = new SKBitmap(info);
            if (!image.ReadPixels(info, bitmap.GetPixels(), info.RowBytes))
                throw new InvalidOperationException("Could not read the image pixels for BMP encoding.");

            int rowStride = (width * 3 + 3) & ~3;
            int pixelBytes = rowStride * height;
            long fileSize = 14 + 40 + pixelBytes;
            if (fileSize > int.MaxValue)
                throw new InvalidOperationException("The image is too large for the BMP format.");

            byte[] source = bitmap.Bytes;
            int sourceStride = info.RowBytes;
            byte[] row = new byte[rowStride];

            using var writer = new BinaryWriter(stream, System.Text.Encoding.ASCII, true);
            // BITMAPFILEHEADER
            writer.Write((byte)'B'); writer.Write((byte)'M');
            writer.Write((int)fileSize);
            writer.Write(0);
            writer.Write(54);
            // BITMAPINFOHEADER
            writer.Write(40);
            writer.Write(width);
            writer.Write(height); // positive: rows are stored bottom-up
            writer.Write((short)1);
            writer.Write((short)24);
            writer.Write(0); // BI_RGB
            writer.Write(pixelBytes);
            writer.Write(3780); // ~96 DPI horizontal
            writer.Write(3780); // ~96 DPI vertical
            writer.Write(0);
            writer.Write(0);

            for (int y = height - 1; y >= 0; y--)
            {
                int srcRow = y * sourceStride;
                for (int x = 0; x < width; x++)
                {
                    int src = srcRow + x * 4;
                    byte b = source[src], g = source[src + 1], r = source[src + 2], a = source[src + 3];
                    if (a != 255)
                    {
                        int inv = 255 - a;
                        b = (byte)((b * a + 255 * inv) / 255);
                        g = (byte)((g * a + 255 * inv) / 255);
                        r = (byte)((r * a + 255 * inv) / 255);
                    }
                    row[x * 3] = b; row[x * 3 + 1] = g; row[x * 3 + 2] = r;
                }
                writer.Write(row);
            }
            writer.Flush();
        }
    }
}
