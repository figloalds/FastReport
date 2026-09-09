#if WINDOWS
using System.Windows.Forms;
using Xunit;

namespace FastReport.Tests.OpenSource.Export.PdfSimple
{
    public class WindowsFormsIntegrationTests
    {
        public class TestValue { }
        public class TestEditor : System.Drawing.Design.UITypeEditor { }

        [Fact]
        public void NativeConversionsAndScopedEditorRegistrationWorkOnStaThread()
        {
            System.Exception error = null;
            var thread = new System.Threading.Thread(() =>
            {
                try
                {
                    var insets = new FastReport.Layout.Padding(1, 2, 3, 4);
                    Assert.Equal(insets, FastReport.Windows.ReportHostConversions.ToReport(FastReport.Windows.ReportHostConversions.ToNative(insets)));
                    var anchors = FastReport.Layout.AnchorStyles.Left | FastReport.Layout.AnchorStyles.Bottom;
                    Assert.Equal(anchors, FastReport.Windows.ReportHostConversions.ToReport(FastReport.Windows.ReportHostConversions.ToNative(anchors)));
                    Assert.Equal(PictureBoxSizeMode.Zoom, FastReport.Windows.ReportHostConversions.ToNative(FastReport.Layout.ImageSizeMode.Zoom));
                    Assert.Equal(DockStyle.Fill, FastReport.Windows.ReportHostConversions.ToNative(FastReport.Layout.DockStyle.Fill));
                    Assert.Same(Cursors.Hand, FastReport.Windows.ReportHostConversions.GetCursor("Hand"));
                    using (FastReport.Windows.ReportHostConversions.RegisterEditor(typeof(TestValue), typeof(TestEditor)))
                        Assert.IsType<TestEditor>(System.ComponentModel.TypeDescriptor.GetEditor(typeof(TestValue), typeof(System.Drawing.Design.UITypeEditor)));
                    Assert.Null(System.ComponentModel.TypeDescriptor.GetEditor(typeof(TestValue), typeof(System.Drawing.Design.UITypeEditor)));
                }
                catch (System.Exception exception) { error = exception; }
            });
            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
            Assert.True(thread.Join(System.TimeSpan.FromSeconds(10)));
            if (error != null) throw error;
        }

        [Fact]
        public void NativeFormsTypesAreUnambiguousAlongsideFastReport()
        {
            // These references reproduce CS0433 with the old Compat assembly.
            Assert.Equal("System.Windows.Forms", typeof(Form).Assembly.GetName().Name);
            Assert.Equal("System.Windows.Forms", typeof(BindingSource).Assembly.GetName().Name);
            Assert.StartsWith("System.Windows.Forms", typeof(Padding).Assembly.GetName().Name);
            Assert.StartsWith("System.Windows.Forms", typeof(AnchorStyles).Assembly.GetName().Name);
            Assert.NotEqual(typeof(Padding), typeof(FastReport.Layout.Padding));
        }
    }
}
#endif
