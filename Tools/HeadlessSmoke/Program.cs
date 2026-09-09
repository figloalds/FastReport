using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using FastReport;
using FastReport.Drawing;
using FastReport.Export.PdfSimple;
using FastReport.Layout;
using FastReport.Table;

string outputDirectory = args.Length == 0 ? AppContext.BaseDirectory : Path.GetFullPath(args[0]);
Directory.CreateDirectory(outputDirectory);
FontManager.AddFont(Path.Combine(AppContext.BaseDirectory, "Fixtures", "TestSans-Regular.ttf"));
FontManager.AddFont(Path.Combine(AppContext.BaseDirectory, "Fixtures", "TestSans-Bold.ttf"));

foreach (bool async in new[] { false, true })
{
    using var original = new Report();
    original.Load(Path.Combine(AppContext.BaseDirectory, "Smoke.frx"));
    using var bitmap = new Bitmap(8, 8);
    using (var graphics = Graphics.FromImage(bitmap)) graphics.Clear(Color.Orange);
    ((PictureObject)original.FindObject("Picture")).Image = (Image)bitmap.Clone();
    using var frx = new MemoryStream();
    original.Save(frx);
    frx.Position = 0;
    using var report = new Report();
    report.Load(frx);
    report.RegisterData(new[] { new Row("A"), new Row("AA"), new Row("AAA") }, "RowsData");
    var source = report.GetDataSource("RowsData");
    source.Enabled = true;
    ((DataBand)report.FindObject("Rows")).DataSource = source;
    Check(async ? await report.PrepareAsync() : report.Prepare(), "prepare");
    Check(report.PreparedPages.Count == 2, "page break");
    using (var page = report.PreparedPages.GetPage(0))
    {
        var values = page.AllObjects.OfType<TextObject>().Where(t => t.Name == "Value").ToArray();
        Check(values.Select(t => t.Text).SequenceEqual(new[] { "A", "AA", "AAA" }), "bound rows");
        Check(values.All(t => t.Padding == new Padding(3, 2, 3, 2) && Math.Abs(t.Left - 10) < 0.01 && Math.Abs(t.Width - 120) < 0.01), "geometry and script padding");
        Check(page.AllObjects.OfType<TableCell>().Any(t => t.Text == "AA"), "table content");
        Check(page.AllObjects.OfType<FastReport.Barcode.BarcodeObject>().Any(), "barcode");
    }
    string pdf = Export(report, async ? "smoke-async.pdf" : "smoke-sync.pdf");
    Check(Regex.Matches(pdf, @"/Type\s*/Page\b").Count == 2, "PDF pages");
    Check(pdf.Contains("/Subtype /Image"), "embedded image");
    Check(pdf.Contains("+FastReportTestSans-Regular") && pdf.Contains("+FastReportTestSans-Bold"), "controlled embedded fonts");
}

using (var report = new Report())
{
    report.Load(Path.Combine(AppContext.BaseDirectory, "Matrix.frx"));
    var data = new DataSet("NorthWind");
    var table = data.Tables.Add("MatrixDemo");
    table.Columns.Add("Name", typeof(string)); table.Columns.Add("Year", typeof(int));
    table.Columns.Add("Month", typeof(int)); table.Columns.Add("ItemsSold", typeof(int));
    table.Columns.Add("Revenue", typeof(decimal));
    table.Rows.Add("A", 2026, 1, 1, 10m); table.Rows.Add("A", 2026, 2, 1, 20m);
    report.RegisterData(data, "NorthWind");
    Check(report.Prepare(), "matrix prepare");
    Check(report.PreparedPages.Count == 1, "matrix pages");
    using var page = report.PreparedPages.GetPage(0);
    Check(page.AllObjects.OfType<TableCell>().Any(t => t.Text.Contains("30")), "matrix aggregation");
    Export(report, "matrix.pdf");
}

#if WINDOWS
Check(FastReport.Windows.ReportHostConversions.ToNative(new Padding(2)).Left == 2, "native adapter");
Check(typeof(System.Windows.Forms.Form).Assembly.GetName().Name == "System.Windows.Forms", "native Forms");
#else
Check(!AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "FastReport.OpenSource.Windows"), "adapter absent");
#endif
Check(!AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "FastReport.Compat"), "Compat absent");
Console.WriteLine("PASS: FRX round trip, sync/async rows, geometry, table, matrix, barcode, image, fonts and PDF pages.");

string Export(Report report, string filename)
{
    using var exporter = new PDFSimpleExport();
    using var stream = new MemoryStream();
    report.Export(exporter, stream);
    byte[] bytes = stream.ToArray();
    Check(bytes.Length > 1000, "PDF size");
    File.WriteAllBytes(Path.Combine(outputDirectory, filename), bytes);
    return Encoding.Latin1.GetString(bytes);
}
static void Check(bool condition, string contract)
{
    if (!condition) throw new Exception("Failed smoke contract: " + contract);
}
public record Row(string Name);
