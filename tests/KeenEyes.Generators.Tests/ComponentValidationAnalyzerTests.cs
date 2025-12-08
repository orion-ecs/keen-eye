using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace KeenEyes.Generators.Tests;

/// <summary>
/// Tests for the ComponentValidationAnalyzer diagnostic analyzer.
/// </summary>
public class ComponentValidationAnalyzerTests
{
    #region KEEN010: Self-Referential Constraint

    [Fact]
    public void SelfReferentialRequires_ReportsError()
    {
        var source = """
            namespace TestApp;

            [KeenEyes.Component]
            [KeenEyes.RequiresComponent(typeof(SelfRefComponent))]
            public partial struct SelfRefComponent { public int X; }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN010");
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("SelfRefComponent", diagnostic.GetMessage());
    }

    [Fact]
    public void SelfReferentialConflicts_ReportsError()
    {
        var source = """
            namespace TestApp;

            [KeenEyes.Component]
            [KeenEyes.ConflictsWith(typeof(SelfRefComponent))]
            public partial struct SelfRefComponent { public int X; }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN010");
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("SelfRefComponent", diagnostic.GetMessage());
    }

    #endregion

    #region KEEN011: Target Not A Struct

    [Fact]
    public void RequiresComponentWithClass_ReportsError()
    {
        var source = """
            namespace TestApp;

            public class NotAStruct { }

            [KeenEyes.Component]
            [KeenEyes.RequiresComponent(typeof(NotAStruct))]
            public partial struct TestComponent { public int X; }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN011");
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("NotAStruct", diagnostic.GetMessage());
        Assert.Contains("class", diagnostic.GetMessage());
    }

    [Fact]
    public void ConflictsWithInterface_ReportsError()
    {
        var source = """
            namespace TestApp;

            public interface INotAStruct { }

            [KeenEyes.Component]
            [KeenEyes.ConflictsWith(typeof(INotAStruct))]
            public partial struct TestComponent { public int X; }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN011");
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("INotAStruct", diagnostic.GetMessage());
        Assert.Contains("interface", diagnostic.GetMessage());
    }

    [Fact]
    public void RequiresComponentWithEnum_ReportsError()
    {
        var source = """
            namespace TestApp;

            public enum NotAStruct { A, B }

            [KeenEyes.Component]
            [KeenEyes.RequiresComponent(typeof(NotAStruct))]
            public partial struct TestComponent { public int X; }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN011");
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("NotAStruct", diagnostic.GetMessage());
        Assert.Contains("enum", diagnostic.GetMessage());
    }

    #endregion

    #region KEEN012: Target Not A Component

    [Fact]
    public void RequiresNonComponent_ReportsWarning()
    {
        var source = """
            namespace TestApp;

            public struct NotAComponent { public int X; }

            [KeenEyes.Component]
            [KeenEyes.RequiresComponent(typeof(NotAComponent))]
            public partial struct TestComponent { public int X; }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN012");
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Contains("NotAComponent", diagnostic.GetMessage());
        Assert.Contains("IComponent", diagnostic.GetMessage());
    }

    [Fact]
    public void RequiresValidComponent_NoWarning()
    {
        var source = """
            namespace TestApp;

            [KeenEyes.Component]
            public partial struct Transform { public float X, Y; }

            [KeenEyes.Component]
            [KeenEyes.RequiresComponent(typeof(Transform))]
            public partial struct Renderable { public string TextureId; }
            """;

        var diagnostics = RunAnalyzer(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == "KEEN012");
    }

    #endregion

    #region KEEN013: Missing Component Attribute

    [Fact]
    public void RequiresWithoutComponentAttribute_ReportsWarning()
    {
        var source = """
            namespace TestApp;

            [KeenEyes.Component]
            public partial struct Transform { public float X, Y; }

            // Missing [Component] attribute
            [KeenEyes.RequiresComponent(typeof(Transform))]
            public partial struct NotMarkedComponent { public int X; }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN013");
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Contains("NotMarkedComponent", diagnostic.GetMessage());
        Assert.Contains("Component", diagnostic.GetMessage());
    }

    [Fact]
    public void ConflictsWithoutComponentAttribute_ReportsWarning()
    {
        var source = """
            namespace TestApp;

            [KeenEyes.Component]
            public partial struct OtherComponent { public float X; }

            // Missing [Component] attribute
            [KeenEyes.ConflictsWith(typeof(OtherComponent))]
            public partial struct NotMarkedComponent { public int X; }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN013");
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public void WithTagComponentAttribute_NoWarning()
    {
        var source = """
            namespace TestApp;

            [KeenEyes.Component]
            public partial struct Player { public int Id; }

            [KeenEyes.TagComponent]
            [KeenEyes.RequiresComponent(typeof(Player))]
            public partial struct Active;
            """;

        var diagnostics = RunAnalyzer(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == "KEEN013");
    }

    #endregion

    #region KEEN014: Mutual Conflict Warning

    [Fact]
    public void OneWayConflict_ReportsInfo()
    {
        var source = """
            namespace TestApp;

            [KeenEyes.Component]
            public partial struct DynamicBody { public float Mass; }

            [KeenEyes.Component]
            [KeenEyes.ConflictsWith(typeof(DynamicBody))]
            public partial struct StaticBody { public bool IsKinematic; }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN014");
        Assert.Equal(DiagnosticSeverity.Info, diagnostic.Severity);
        Assert.Contains("StaticBody", diagnostic.GetMessage());
        Assert.Contains("DynamicBody", diagnostic.GetMessage());
    }

    [Fact]
    public void MutualConflict_NoInfo()
    {
        var source = """
            namespace TestApp;

            [KeenEyes.Component]
            [KeenEyes.ConflictsWith(typeof(StaticBody))]
            public partial struct DynamicBody { public float Mass; }

            [KeenEyes.Component]
            [KeenEyes.ConflictsWith(typeof(DynamicBody))]
            public partial struct StaticBody { public bool IsKinematic; }
            """;

        var diagnostics = RunAnalyzer(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == "KEEN014");
    }

    #endregion

    #region Valid Configurations

    [Fact]
    public void ValidRequiresComponent_NoErrors()
    {
        var source = """
            namespace TestApp;

            [KeenEyes.Component]
            public partial struct Transform { public float X, Y; }

            [KeenEyes.Component]
            [KeenEyes.RequiresComponent(typeof(Transform))]
            public partial struct Renderable { public string TextureId; }
            """;

        var diagnostics = RunAnalyzer(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void ValidConflictsWith_NoErrors()
    {
        var source = """
            namespace TestApp;

            [KeenEyes.Component]
            [KeenEyes.ConflictsWith(typeof(StaticBody))]
            public partial struct DynamicBody { public float Mass; }

            [KeenEyes.Component]
            [KeenEyes.ConflictsWith(typeof(DynamicBody))]
            public partial struct StaticBody { public bool IsKinematic; }
            """;

        var diagnostics = RunAnalyzer(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void MultipleRequiresAndConflicts_NoErrors()
    {
        var source = """
            namespace TestApp;

            [KeenEyes.Component]
            public partial struct Transform { public float X, Y; }

            [KeenEyes.Component]
            [KeenEyes.ConflictsWith(typeof(RigidBody))]
            public partial struct StaticBody { public bool IsKinematic; }

            [KeenEyes.Component]
            [KeenEyes.RequiresComponent(typeof(Transform))]
            [KeenEyes.ConflictsWith(typeof(StaticBody))]
            public partial struct RigidBody { public float Mass; }
            """;

        var diagnostics = RunAnalyzer(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void RequiresComponentWithDelegate_ReportsError()
    {
        var source = """
            namespace TestApp;

            public delegate void MyDelegate();

            [KeenEyes.Component]
            [KeenEyes.RequiresComponent(typeof(MyDelegate))]
            public partial struct TestComponent { public int X; }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN011");
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("MyDelegate", diagnostic.GetMessage());
        Assert.Contains("delegate", diagnostic.GetMessage());
    }

    [Fact]
    public void ConflictsWithDelegate_ReportsError()
    {
        var source = """
            namespace TestApp;

            public delegate int Calculator(int a, int b);

            [KeenEyes.Component]
            [KeenEyes.ConflictsWith(typeof(Calculator))]
            public partial struct TestComponent { public int X; }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN011");
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("Calculator", diagnostic.GetMessage());
        Assert.Contains("delegate", diagnostic.GetMessage());
    }

    #endregion

    private static IReadOnlyList<Diagnostic> RunAnalyzer(string source)
    {
        var attributesAssembly = typeof(ComponentAttribute).Assembly;
        var coreAssembly = typeof(KeenEyes.World).Assembly;

        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            MetadataReference.CreateFromFile(attributesAssembly.Location),
            MetadataReference.CreateFromFile(coreAssembly.Location),
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

        var analyzer = new ComponentValidationAnalyzer();
        var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));

        var diagnostics = compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().Result;

        return diagnostics.ToList();
    }
}
