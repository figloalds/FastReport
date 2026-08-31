using FastReport.Code;
using Xunit;

namespace FastReport.Tests.OpenSource
{
    public class ScriptMigrationTests
    {
        [Fact]
        public void UsingDirectivesAndQualifiedReferencesMigrate()
        {
            string script =
                "using System.Drawing;\n" +
                "using System.Drawing.Drawing2D;\n" +
                "using System.Drawing.Imaging;\n" +
                "using System.Drawing.Text;\n" +
                "public class ReportScript {\n" +
                "  System.Drawing.Color c = System.Drawing.Color.Red;\n" +
                "  global::System.Drawing.Size s;\n" +
                "}\n";

            string migrated = AssemblyDescriptor.MigrateScriptNamespaces(script);

            Assert.Contains("using FastReport.Drawing;", migrated);
            Assert.Contains("using FastReport.Drawing.Drawing2D;", migrated);
            Assert.Contains("using FastReport.Drawing.Imaging;", migrated);
            Assert.Contains("using FastReport.Drawing.Text;", migrated);
            Assert.Contains("FastReport.Drawing.Color c = FastReport.Drawing.Color.Red;", migrated);
            Assert.Contains("global::FastReport.Drawing.Size s;", migrated);
            Assert.DoesNotContain("System.Drawing", migrated);
        }

        [Fact]
        public void StringLiteralsVerbatimStringsAndCommentsArePreserved()
        {
            string script =
                "public class ReportScript {\n" +
                "  string a = \"System.Drawing\";\n" +
                "  string b = @\"System.Drawing.Drawing2D\";\n" +
                "  string c = \"prefix System.Drawing suffix\";\n" +
                "  string d = \"escaped \\\" System.Drawing\";\n" +
                "  // System.Drawing belongs to the old world\n" +
                "  /* System.Drawing.Imaging */\n" +
                "  char quote = '\\'';\n" +
                "}\n";

            string migrated = AssemblyDescriptor.MigrateScriptNamespaces(script);

            Assert.Contains("\"System.Drawing\"", migrated);
            Assert.Contains("@\"System.Drawing.Drawing2D\"", migrated);
            Assert.Contains("\"prefix System.Drawing suffix\"", migrated);
            Assert.Contains("// System.Drawing belongs to the old world", migrated);
            Assert.Contains("/* System.Drawing.Imaging */", migrated);
        }

        [Fact]
        public void InterpolatedStringLiteralTextIsPreserved()
        {
            // Interpolation holes are intentionally not migrated (opaque-literal policy):
            // a missed migration fails loudly at compile time instead of corrupting data.
            string script = "string s = $\"uses System.Drawing here\";\n";
            Assert.Contains("$\"uses System.Drawing here\"", AssemblyDescriptor.MigrateScriptNamespaces(script));
        }

        [Fact]
        public void NonDrawingIdentifiersAreNotMangled()
        {
            string script =
                "string MySystemDrawing = \"x\";\n" +
                "int SystemDrawing2D = 1;\n" +
                "var t = typeof(System.Drawing2D);\n";

            string migrated = AssemblyDescriptor.MigrateScriptNamespaces(script);

            Assert.Contains("MySystemDrawing", migrated);
            Assert.Contains("SystemDrawing2D", migrated);
            Assert.Contains("System.Drawing2D", migrated);
        }

        [Fact]
        public void NullAndEmptyPassThrough()
        {
            Assert.Equal(string.Empty, AssemblyDescriptor.MigrateScriptNamespaces(null));
            Assert.Equal(string.Empty, AssemblyDescriptor.MigrateScriptNamespaces(string.Empty));
            Assert.Equal("no drawing here", AssemblyDescriptor.MigrateScriptNamespaces("no drawing here"));
        }
    }
}
