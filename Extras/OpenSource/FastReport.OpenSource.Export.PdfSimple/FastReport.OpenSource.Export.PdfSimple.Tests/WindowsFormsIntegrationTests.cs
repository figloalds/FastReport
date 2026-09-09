#if WINDOWS
using System.Windows.Forms;
using Xunit;

namespace FastReport.Tests.OpenSource.Export.PdfSimple
{
    public class WindowsFormsIntegrationTests
    {
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
