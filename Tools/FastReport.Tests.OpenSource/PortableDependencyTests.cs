using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace FastReport.Tests.OpenSource;

public class PortableDependencyTests
{
    [Fact]
    public void PortableAssemblyGraphHasNoDesktopOrCompatDependency()
    {
        var pending = new Queue<Assembly>();
        var visited = new HashSet<string>();
        pending.Enqueue(typeof(Report).Assembly);
        while (pending.Count > 0)
        {
            var assembly = pending.Dequeue();
            if (!visited.Add(assembly.FullName)) continue;
            string name = assembly.GetName().Name;
            Assert.DoesNotContain("Compat", name);
            Assert.DoesNotContain(name, new[] { "System.Windows.Forms", "System.Windows.Forms.Primitives",
                "FastReport.OpenSource.Windows", "System.Drawing.Common", "PresentationCore", "PresentationFramework", "WindowsBase" });
            foreach (var reference in assembly.GetReferencedAssemblies())
                pending.Enqueue(Assembly.Load(reference));
            if (name.StartsWith("FastReport"))
                Assert.DoesNotContain(assembly.GetExportedTypes(), type =>
                    type.Namespace?.StartsWith("FastReport.Compatibility.Forms") == true);
        }
        Assert.Contains("FastReport.Drawing", typeof(Drawing.Graphics).Assembly.FullName);
        Assert.Equal(typeof(Report).Assembly, typeof(Code.CodeDom.Compiler.CodeDomProvider).Assembly);
    }
}
