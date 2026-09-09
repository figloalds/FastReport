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

        [Fact]
        public void FormsImportsAliasesAndExpressionsMigrateWithoutChangingLiteralText()
        {
            const string script = """"
                using System.Windows.Forms;
                using Forms = global::System.Windows.Forms;
                class ReportScript {
                    System.Windows.Forms.Padding padding = new System.Windows.Forms.Padding(2);
                    string value = $"System.Windows.Forms {System.Windows.Forms.DockStyle.Fill}";
                    string raw = """System.Windows.Forms""";
                    // System.Windows.Forms
                    object flag = System /* preserve */ . Windows . Forms . AnchorStyles.Left;
                }
                """";

            string migrated = AssemblyDescriptor.MigrateScriptNamespaces(script);
            Assert.Contains("using FastReport.Layout;", migrated);
            Assert.Contains("using Forms = global::FastReport.Layout;", migrated);
            Assert.Contains("new global::FastReport.Layout.Padding(2)", migrated);
            Assert.Contains("$\"System.Windows.Forms {(global::FastReport.Layout.DockStyle.Fill)}\"", migrated);
            Assert.Contains("\"\"\"System.Windows.Forms\"\"\"", migrated);
            Assert.Contains("// System.Windows.Forms", migrated);
            Assert.Contains("global::FastReport.Layout.AnchorStyles", migrated);
            Assert.Contains("/* preserve */", migrated);
        }

        [Fact]
        public void VisualBasicImportsAndQualifiedNamesMigratePreservingCommentsAndStrings()
        {
            const string script = """"
                Imports system.windows.forms
                Imports Forms = Global.System.Windows.Forms
                Imports System.Drawing
                Public Class ReportScript
                    ' System.Windows.Forms stays in comments; it must not hide the next line.
                    Dim padding As Global.System.Windows.Forms.Padding
                    Dim color As System.Drawing.Color = system.drawing.Color.Red
                    Dim value As String = "System.Windows.Forms ""quoted"""
                    REM System.Windows.Forms
                    Dim anchor = System.Windows.Forms.AnchorStyles.Left
                End Class
                """";

            string migrated = AssemblyDescriptor.MigrateScriptNamespaces(script, Language.Vb);
            Assert.Contains("Imports FastReport.Layout", migrated);
            Assert.Contains("Imports Forms = Global.FastReport.Layout", migrated);
            Assert.Contains("Imports FastReport.Drawing", migrated);
            Assert.Contains("Dim padding As Global.FastReport.Layout.Padding", migrated);
            Assert.Contains("= FastReport.drawing.Color.Red", migrated);
            Assert.Contains("' System.Windows.Forms stays in comments", migrated);
            Assert.Contains("REM System.Windows.Forms", migrated);
            Assert.Contains("\"System.Windows.Forms \"\"quoted\"\"\"", migrated);
            Assert.Contains("Dim anchor = Global.FastReport.Layout.AnchorStyles.Left", migrated);
        }

        [Theory]
        [InlineData("using System.Windows.FormsExtra;")]
        [InlineData("using MySystem.Windows.Forms;")]
        [InlineData("using External::System.Windows.Forms;")]
        public void SimilarNamespacesAreNotMigrated(string script)
        {
            Assert.Equal(script, AssemblyDescriptor.MigrateScriptNamespaces(script));
        }

        [Theory]
        [InlineData("System.Windows.Forms")]
        [InlineData("FastReport.Compatibility.Forms")]
        public void LayoutAliasesMigrateToFinalOwners(string legacyNamespace)
        {
            string script = $$"""
                using {{legacyNamespace}};
                using Forms = {{legacyNamespace}};
                using Insets = {{legacyNamespace}}.Padding;
                class ReportScript {
                    Insets padding = new Insets(1, 2, 3, 4);
                    Forms.PictureBoxSizeMode size = Forms.PictureBoxSizeMode.Zoom;
                }
                """;
            string migrated = AssemblyDescriptor.MigrateScriptNamespaces(script);
            Assert.Contains("using Insets = global::FastReport.Layout.Padding;", migrated);
            Assert.Contains("new global::FastReport.Layout.Padding(1, 2, 3, 4)", migrated);
            Assert.Contains("global::FastReport.Layout.ImageSizeMode.Zoom", migrated);
            Assert.Equal(migrated, AssemblyDescriptor.MigrateScriptNamespaces(migrated));
        }

        [Fact]
        public void UserDefinedTypesAndVariablesAreNotLayoutTypes()
        {
            const string script = """
                using FastReport.Compatibility.Forms;
                class Padding { public static int Empty = 7; }
                class ReportScript {
                    Padding padding = new Padding();
                    int value = Padding.Empty;
                    int Method(int DockStyle) => DockStyle;
                }
                """;
            Assert.Equal(script.Replace("using FastReport.Compatibility.Forms;", "using FastReport.Layout;"), AssemblyDescriptor.MigrateScriptNamespaces(script));
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.Vb)]
        public void LegacyLayoutImportsAndAliasesExecuteAgainstReportOwnedTypes(Language language)
        {
            using var report = new Report { ScriptLanguage = language };
            var page = new ReportPage { Parent = report };
            var band = new ReportTitleBand { Parent = page, Height = 40 };
            var text = new TextObject
            {
                Parent = band, Name = "Text1", Width = 150, Height = 30,
                Text = "Layout", BeforePrintEvent = "SetLayout"
            };
            report.ScriptText = language == Language.CSharp ? """
                using System;
                using System.Windows.Forms;
                using FastReport.Layout;
                using Forms = System.Windows.Forms;
                using Insets = FastReport.Compatibility.Forms.Padding;
                namespace FastReport {
                    public class ReportScript {
                        private void SetLayout(object sender, EventArgs e) {
                            Text1.Padding = new Insets(3);
                            Text1.Anchor = AnchorStyles.Top | Forms.AnchorStyles.Left;
                            Text1.Dock = DockStyle.None;
                            Text1.Text = $"layout {Forms.DockStyle.None}";
                        }
                    }
                }
                """ : """
                Imports System
                Imports System.Windows.Forms
                Imports FastReport.Layout
                Imports Forms = System.Windows.Forms
                Imports Insets = FastReport.Compatibility.Forms.Padding
                Namespace FastReport
                    Public Class ReportScript
                        Private Sub SetLayout(sender As Object, e As EventArgs)
                            Text1.Padding = New Insets(3)
                            Text1.Anchor = AnchorStyles.Top Or Forms.AnchorStyles.Left
                            Text1.Dock = DockStyle.None
                            Text1.Text = $"layout {Forms.DockStyle.None}"
                        End Sub
                    End Class
                End Namespace
                """;
            string original = report.ScriptText;
            Assert.True(report.Prepare());
            Assert.Equal(original, report.ScriptText);
            using var prepared = report.PreparedPages.GetPage(0);
            var preparedText = (TextObject)prepared.FindObject("Text1");
            Assert.Equal(new FastReport.Layout.Padding(3), preparedText.Padding);
            Assert.Equal("layout None", preparedText.Text);
        }

        [Fact]
        public void RuntimeDoesNotDeclareOrReferenceDesktopFrameworkTypes()
        {
            var assemblies = new[] { typeof(Report).Assembly, typeof(FastReport.Drawing.Graphics).Assembly };
            foreach (var assembly in assemblies)
            {
                Assert.DoesNotContain(assembly.GetExportedTypes(), type =>
                    type.Namespace != null && (type.Namespace.StartsWith("System.Windows.Forms") ||
                    type.Namespace.StartsWith("System.Drawing") || type.Namespace.StartsWith("FastReport.Compatibility.Forms")));
                Assert.DoesNotContain(assembly.GetReferencedAssemblies(), reference =>
                    reference.Name == "System.Windows.Forms" || reference.Name == "System.Drawing.Common");
            }
            using var report = new Report();
            Assert.DoesNotContain("System.Windows.Forms.dll", report.ReferencedAssemblies);
        }
    }
}
