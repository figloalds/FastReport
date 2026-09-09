using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using FastReport.Code.CodeDom.Compiler;
using CS = Microsoft.CodeAnalysis.CSharp;
using VB = Microsoft.CodeAnalysis.VisualBasic;

namespace FastReport.Code
{
    // This is an FRX import boundary, not an emulation of desktop framework assemblies.
    internal static class LegacyScriptNamespaces
    {
        private static readonly Lazy<MetadataReference[]> MigrationReferences = new Lazy<MetadataReference[]>(() =>
            new[] { typeof(object).Assembly, typeof(Report).Assembly,
                typeof(FastReport.Drawing.Graphics).Assembly,
                System.Reflection.Assembly.Load("System.Runtime") }
                .Distinct().Select(assembly => MetadataReference.CreateFromFile(
                    string.IsNullOrEmpty(assembly.Location)
                        ? CodeDomProvider.TryFixAssemblyReference(assembly) : assembly.Location)).ToArray());

        internal static string Migrate(string script, Language language)
        {
            if (string.IsNullOrEmpty(script))
                return script ?? string.Empty;

            bool visualBasic = language == Language.Vb;
            SyntaxNode root = visualBasic
                ? VB.VisualBasicSyntaxTree.ParseText(script).GetRoot()
                : CS.CSharpSyntaxTree.ParseText(script).GetRoot();
            var comparison = visualBasic ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            var changes = new Dictionary<int, TextChange>();

            foreach (SyntaxNode node in root.DescendantNodes())
            {
                if (!(node is CS.Syntax.QualifiedNameSyntax || node is CS.Syntax.MemberAccessExpressionSyntax ||
                      node is VB.Syntax.QualifiedNameSyntax || node is VB.Syntax.MemberAccessExpressionSyntax))
                    continue;

                // Inspect only namespace-sized prefixes, not every token in longer expressions.
                var tokens = node.DescendantTokens().Take(8).ToArray();
                int offset = 0;
                if (tokens.Length >= 2 &&
                    (visualBasic && tokens[0].ValueText.Equals("Global", comparison) && tokens[1].Text == "." ||
                     !visualBasic && tokens[0].Text == "global" && tokens[1].Text == "::"))
                    offset = 2;

                int length = tokens.Length - offset;
                if ((length != 3 && length != 5) ||
                    !tokens[offset].ValueText.Equals("System", comparison) || tokens[offset + 1].Text != ".")
                    continue;

                bool drawing = length == 3 && tokens[offset + 2].ValueText.Equals("Drawing", comparison);
                bool forms = length == 5 && tokens[offset + 2].ValueText.Equals("Windows", comparison) &&
                    tokens[offset + 3].Text == "." && tokens[offset + 4].ValueText.Equals("Forms", comparison);
                if (!drawing && !forms)
                    continue;

                Replace(tokens[offset], "FastReport");
                if (forms)
                    Replace(tokens[offset + 2], "Compatibility");
            }

            // Replace identifier spans only, preserving all trivia and literal text, including
            // comments between namespace segments and text surrounding interpolation expressions.
            string migrated = changes.Count == 0 ? script : SourceText.From(script).WithChanges(changes.Values).ToString();
            return MigrateLayoutTypes(migrated, visualBasic);

            void Replace(SyntaxToken token, string value)
            {
                changes[token.SpanStart] = new TextChange(token.Span, value);
            }
        }

        // Analysis-only declarations. No replacement desktop types are emitted.
        private static SyntaxTree CreateLegacySymbols(bool visualBasic)
        {
            const string names = "AnchorStyles Appearance Application AutoScaleMode BaseForm BorderStyle Button ButtonBase CharacterCasing CheckBox CheckState CheckedIndexCollection CheckedItemCollection CheckedListBox CloseReason ComboBox ComboBoxStyle Control ControlPaint ControlStyles Cursor Cursors DateRangeEventArgs DateTimePicker DateTimePickerFormat Day DialogResult DockStyle DrawItemEventArgs DrawMode Form FormBorderStyle FormClosedEventArgs FormClosingEventArgs FormStartPosition GroupBox HorizontalAlignment InvalidateEventArgs ItemCheckEventArgs KeyEventArgs KeyPressEventArgs Keys Label LeftRightAlignment ListBox ListControl MeasureItemEventArgs MessageBox MonthCalendar MouseButtons MouseEventArgs Padding PaintEventArgs Panel PictureBox PictureBoxSizeMode RadioButton RightToLeft ScrollBars ScrollableControl SelectionMode SelectionRange SystemInformation TextBox TextImageRelation Timer ToolTip";
            string declarations = visualBasic
                ? "Namespace FastReport.Compatibility.Forms\n" + string.Join("\n", names.Split(' ').Select(name => "Public Class " + name + "\nEnd Class")) + "\nEnd Namespace"
                : "namespace FastReport.Compatibility.Forms {" + string.Join("", names.Split(' ').Select(name => "public class " + name + " {}")) + "}";
            return visualBasic ? VB.VisualBasicSyntaxTree.ParseText(declarations) : CS.CSharpSyntaxTree.ParseText(declarations);
        }

        private static string MigrateLayoutTypes(string script, bool visualBasic)
        {
            if (script.IndexOf("Compatibility", StringComparison.OrdinalIgnoreCase) < 0)
                return script;

            SyntaxTree tree = visualBasic ? VB.VisualBasicSyntaxTree.ParseText(script)
                : CS.CSharpSyntaxTree.ParseText(script);
            Compilation compilation = visualBasic
                ? VB.VisualBasicCompilation.Create("LegacyLayoutMigration", new[] { tree, CreateLegacySymbols(visualBasic) }, MigrationReferences.Value)
                : CS.CSharpCompilation.Create("LegacyLayoutMigration", new[] { tree, CreateLegacySymbols(visualBasic) }, MigrationReferences.Value);
            SemanticModel model = compilation.GetSemanticModel(tree);
            var changes = new List<TextChange>();
            var interpolations = new HashSet<CS.Syntax.InterpolationSyntax>();

            foreach (SyntaxNode node in tree.GetRoot().DescendantNodes())
            {
                // Visit the outermost type reference once, including aliases and static member receivers.
                // Binding symbols prevents a user-defined Padding type or variable from being renamed.
                if (!(node is CS.Syntax.NameSyntax || node is CS.Syntax.MemberAccessExpressionSyntax ||
                    node is VB.Syntax.NameSyntax || node is VB.Syntax.MemberAccessExpressionSyntax) ||
                    changes.Any(change => change.Span.Contains(node.Span)))
                    continue;

                SymbolInfo info = model.GetSymbolInfo(node);
                ISymbol symbol = info.Symbol;
                // Reports may import both the old namespace and the new layout namespace.
                // Resolve only that known transition ambiguity, not unrelated application types.
                if (symbol == null && info.CandidateReason == CandidateReason.Ambiguous &&
                    info.CandidateSymbols.All(candidate => candidate is INamedTypeSymbol candidateType &&
                        (candidateType.ContainingNamespace.ToDisplayString() == "FastReport.Compatibility.Forms" &&
                         candidateType.ContainingAssembly.Name == "LegacyLayoutMigration" ||
                         candidateType.ContainingNamespace.ToDisplayString() == "FastReport.Layout" &&
                         candidateType.ContainingAssembly.Name == typeof(Report).Assembly.GetName().Name)))
                    symbol = info.CandidateSymbols.FirstOrDefault(candidate =>
                        candidate.ContainingNamespace.ToDisplayString() == "FastReport.Compatibility.Forms");
                if (symbol is IAliasSymbol alias)
                    symbol = alias.Target;
                if (!(symbol is INamedTypeSymbol type) ||
                    type.ContainingNamespace.ToDisplayString() != "FastReport.Compatibility.Forms" ||
                    type.ContainingAssembly.Name != "LegacyLayoutMigration")
                    continue;

                string name = type.Name switch
                {
                    "Padding" => "Padding",
                    "AnchorStyles" => "AnchorStyles",
                    "DockStyle" => "DockStyle",
                    "PictureBoxSizeMode" => "ImageSizeMode",
                    _ => null
                };
                if (name == null)
                {
                    var location = tree.GetLineSpan(node.Span).StartLinePosition;
                    throw new NotSupportedException($"Desktop script API '{type.Name}' is unavailable in the headless runtime (line {location.Line + 1}, column {location.Character + 1}).");
                }

                // Keep comments and whitespace inside a qualification, including line continuations.
                string trivia = string.Concat(node.DescendantTrivia().Where(t => node.Span.Contains(t.Span))
                    .Select(t => t.ToFullString()));
                // VB Imports clauses are already rooted; VB rejects Global there.
                string prefix = visualBasic
                    ? (node.Ancestors().Any(parent => parent is VB.Syntax.ImportsStatementSyntax) ? "" : "Global.")
                    : "global::";
                string qualifiedName = prefix + "FastReport.Layout." + name;
                changes.Add(new TextChange(node.Span, qualifiedName + trivia));
                if (!visualBasic)
                {
                    var interpolation = node.Ancestors().OfType<CS.Syntax.InterpolationSyntax>().FirstOrDefault();
                    if (interpolation != null)
                        interpolations.Add(interpolation);
                }
            }

            // At the top level of a C# interpolation, ':' starts the format specifier.
            // Parenthesize the expression so global:: remains an alias qualification.
            foreach (var interpolation in interpolations.OrderByDescending(item => item.SpanStart))
            {
                TextSpan span = interpolation.Expression.Span;
                var contained = changes.Where(change => span.Contains(change.Span)).ToArray();
                string expression = SourceText.From(script.Substring(span.Start, span.Length)).WithChanges(
                    contained.Select(change => new TextChange(
                        new TextSpan(change.Span.Start - span.Start, change.Span.Length), change.NewText))).ToString();
                changes.RemoveAll(change => span.Contains(change.Span));
                changes.Add(new TextChange(span, "(" + expression + ")"));
            }

            string result = changes.Count == 0 ? script : SourceText.From(script).WithChanges(changes).ToString();
            // Remaining legacy namespace references are imports/namespace aliases.
            // Rewrite syntax spans only, preserving literal text and comments.
            SyntaxNode root = (visualBasic ? VB.VisualBasicSyntaxTree.ParseText(result) : CS.CSharpSyntaxTree.ParseText(result)).GetRoot();
            var imports = new List<TextChange>();
            foreach (var node in root.DescendantNodes())
            {
                if (!(node is CS.Syntax.QualifiedNameSyntax || node is VB.Syntax.QualifiedNameSyntax)) continue;
                var tokens = node.DescendantTokens().ToArray();
                int offset = tokens.Length == 7 ? 2 : 0;
                if (tokens.Length - offset == 5 && tokens[offset].ValueText.Equals("FastReport", StringComparison.OrdinalIgnoreCase) &&
                    tokens[offset + 2].ValueText.Equals("Compatibility", StringComparison.OrdinalIgnoreCase) &&
                    tokens[offset + 4].ValueText.Equals("Forms", StringComparison.OrdinalIgnoreCase) &&
                    !imports.Any(change => change.Span.Contains(node.Span)))
                {
                    string trivia = string.Concat(node.DescendantTrivia().Where(t => node.Span.Contains(t.Span)).Select(t => t.ToFullString()));
                    string prefix = offset == 2 ? (visualBasic ? "Global." : "global::") : "";
                    imports.Add(new TextChange(node.Span, prefix + "FastReport.Layout" + trivia));
                }
            }
            result = imports.Count == 0 ? result : SourceText.From(result).WithChanges(imports).ToString();
            if (visualBasic)
            {
                // VB treats duplicate imports as an error. Preserve trivia when removing
                // repeated clauses, including comma-separated imports on the same line.
                var vbRoot = VB.VisualBasicSyntaxTree.ParseText(result).GetRoot();
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var duplicates = new List<SyntaxNode>();
                foreach (var statement in vbRoot.DescendantNodes().OfType<VB.Syntax.ImportsStatementSyntax>())
                {
                    var repeated = statement.ImportsClauses.Where(clause =>
                        !seen.Add(string.Concat(clause.DescendantTokens().Select(token => token.ValueText)))).ToArray();
                    if (repeated.Length == statement.ImportsClauses.Count) duplicates.Add(statement);
                    else duplicates.AddRange(repeated);
                }
                result = vbRoot.RemoveNodes(duplicates, SyntaxRemoveOptions.KeepExteriorTrivia).ToFullString();
            }
            return result;
        }
    }
}
