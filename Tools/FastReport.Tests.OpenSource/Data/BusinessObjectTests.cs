using System.Linq;
using Xunit;

namespace FastReport.Tests.OpenSource.Data
{
    public class BusinessObjectTests
    {
        public class Employee
        {
            public string Name { get; set; }
        }

        [Fact]
        public void RegisterBusinessObjectsAndPrepareBoundFrxRows()
        {
            using var report = new Report();
            report.LoadFromString("""
                <?xml version="1.0" encoding="utf-8"?>
                <Report><ReportPage Name="Page1"><DataBand Name="Rows" Height="24">
                <TextObject Name="EmployeeName" Width="200" Height="24" Text="[Employees.Name]"/>
                </DataBand></ReportPage></Report>
                """);
            report.RegisterData(new[] { new Employee { Name = "Alice" }, new Employee { Name = "Bob" } }, "Employees");
            var source = report.GetDataSource("Employees");
            source.Enabled = true;
            ((DataBand)report.FindObject("Rows")).DataSource = source;

            Assert.True(report.Prepare());
            using var page = report.PreparedPages.GetPage(0);
            Assert.Equal(new[] { "Alice", "Bob" },
                page.AllObjects.OfType<TextObject>().Select(text => text.Text).ToArray());
        }
    }
}
