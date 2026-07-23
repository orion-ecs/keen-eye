using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace KeenEyes.Generators.Tests;

/// <summary>
/// Tests for the AttributeUsageAnalyzer diagnostic analyzer.
/// </summary>
public class AttributeUsageAnalyzerTests
{
    #region KEEN017: Component and TagComponent Conflict

    [Fact]
    public void ComponentAndTagComponent_OnSameStruct_ReportsKeen017()
    {
        var source = """
            namespace TestApp;

            [KeenEyes.Component(Version = 3)]
            [KeenEyes.TagComponent]
            public partial struct DualAttributedComponent { public int X; }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN017");
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("DualAttributedComponent", diagnostic.GetMessage());
    }

    [Fact]
    public void ComponentOnly_DoesNotReportKeen017()
    {
        var source = """
            namespace TestApp;

            [KeenEyes.Component(Version = 2)]
            public partial struct RegularComponent { public int X; }
            """;

        var diagnostics = RunAnalyzer(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == "KEEN017");
    }

    [Fact]
    public void TagComponentOnly_DoesNotReportKeen017()
    {
        var source = """
            namespace TestApp;

            [KeenEyes.TagComponent]
            public partial struct MarkerTag;
            """;

        var diagnostics = RunAnalyzer(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == "KEEN017");
    }

    [Fact]
    public void ComponentAndTagComponent_OnSeparateStructs_DoesNotReportKeen017()
    {
        var source = """
            namespace TestApp;

            [KeenEyes.Component]
            public partial struct RegularComponent { public int X; }

            [KeenEyes.TagComponent]
            public partial struct MarkerTag;
            """;

        var diagnostics = RunAnalyzer(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == "KEEN017");
    }

    #endregion

    private static IReadOnlyList<Diagnostic> RunAnalyzer(string source)
    {
        var attributesAssembly = typeof(ComponentAttribute).Assembly;

        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            MetadataReference.CreateFromFile(attributesAssembly.Location),
        };

        // Add runtime assembly references
        var runtimeDir = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
        references.Add(MetadataReference.CreateFromFile(System.IO.Path.Join(runtimeDir, "System.Runtime.dll")));
        references.Add(MetadataReference.CreateFromFile(System.IO.Path.Join(runtimeDir, "netstandard.dll")));

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var analyzer = new AttributeUsageAnalyzer();
        var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));

        var diagnostics = compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().Result;

        return diagnostics.ToList();
    }
}
