using System;
using System.IO;
using System.Threading.Tasks;
using FastReport.Code;
using Xunit;

namespace FastReport.Tests.OpenSource;

public class HeadlessCapabilityTests
{
    [Fact]
    public void DialogIsRejectedBeforeUnknownControlsCanBeDiscarded()
    {
        using var report = new Report();
        var error = Assert.Throws<NotSupportedException>(() => report.LoadFromString(
            "<?xml version=\"1.0\" encoding=\"utf-8\"?><Report><DialogPage Name=\"Input\"><UnknownControl Name=\"Required\"/></DialogPage></Report>"));
        Assert.Contains("DialogPage 'Input'", error.Message);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProgrammaticDialogIsRejectedBeforePreparation(bool async)
    {
        using var report = new Report();
        var type = typeof(Report).Assembly.GetType("FastReport.Dialog.DialogPage");
        report.Pages.Add((PageBase)Activator.CreateInstance(type, true));
        if (async) await Assert.ThrowsAsync<NotSupportedException>(() => report.PrepareAsync());
        else Assert.Throws<NotSupportedException>(() => report.Prepare());
    }

    [Theory]
    [InlineData(Language.CSharp, "using System.Windows.Forms; class Script { void Run() { MessageBox.Show(\"hi\"); } }")]
    [InlineData(Language.CSharp, "using F = FastReport.Compatibility.Forms; class Script { F.Form form; }")]
    [InlineData(Language.Vb, "Imports System.Windows.Forms\nPublic Class Script\nSub Run()\nMessageBox.Show(\"hi\")\nEnd Sub\nEnd Class")]
    [InlineData(Language.Vb, "Imports F = System.Windows.Forms\nPublic Class Script\nDim form As F.Form\nEnd Class")]
    public void DesktopScriptDiagnosticsIdentifyApiAndLocation(Language language, string script)
    {
        var error = Assert.Throws<NotSupportedException>(() => AssemblyDescriptor.MigrateScriptNamespaces(script, language));
        Assert.Contains("Desktop script API", error.Message);
        Assert.Contains("line", error.Message);
        Assert.Contains("column", error.Message);
    }

    [Fact]
    public void PassiveMetadataRoundTripsAndPreparesWithoutDesktop()
    {
        using var report = new Report();
        report.LoadFromString("<?xml version=\"1.0\" encoding=\"utf-8\"?><Report><ReportPage Name=\"Page\" Duplex=\"Vertical\"><ReportTitleBand Height=\"30\"><TextObject Name=\"Text\" Cursor=\"Hand\" MouseUpEvent=\"OnMouseUp\" Text=\"Hello\" Width=\"100\" Height=\"20\"/></ReportTitleBand></ReportPage></Report>");
        using var stream = new MemoryStream();
        report.Save(stream);
        stream.Position = 0;
        using var copy = new Report();
        copy.Load(stream);
        Assert.Equal("Hand", ((TextObject)copy.FindObject("Text")).Cursor);
        Assert.Equal("OnMouseUp", ((TextObject)copy.FindObject("Text")).MouseUpEvent);
        Assert.Equal(Drawing.Printing.Duplex.Vertical, ((ReportPage)copy.Pages[0]).Duplex);
        Assert.True(copy.Prepare());
    }
}
