using Microsoft.CodeAnalysis.CSharp;

namespace KeenEyes.Generators.Tests;

public class ComponentGeneratorTests
{
    [Fact]
    public void ComponentGenerator_WithNoComponents_GeneratesNoOutput()
    {
        var source = """
            namespace TestApp;

            public struct NotAComponent
            {
                public int X;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.Empty(diagnostics);
        Assert.Empty(generatedTrees);
    }

    [Fact]
    public void ComponentGenerator_WithComponent_GeneratesPartialStruct()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component]
            public partial struct Position
            {
                public float X;
                public float Y;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("partial struct Position"));
    }

    [Fact]
    public void ComponentGenerator_WithTagComponent_GeneratesTagInterface()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [TagComponent]
            public partial struct Frozen { }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("ITagComponent"));
    }

    [Fact]
    public void ComponentGenerator_GeneratesBuilderExtensions()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component]
            public partial struct Velocity
            {
                public float X;
                public float Y;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("WithVelocity"));
    }

    private static (IReadOnlyList<Microsoft.CodeAnalysis.Diagnostic> Diagnostics, IReadOnlyList<string> GeneratedSources) RunGenerator(string source)
    {
        var attributesAssembly = typeof(ComponentAttribute).Assembly;

        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = new List<Microsoft.CodeAnalysis.MetadataReference>
        {
            Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(attributesAssembly.Location),
        };

        // Add runtime assembly references
        var runtimeDir = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
        references.Add(Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(System.IO.Path.Combine(runtimeDir, "System.Runtime.dll")));

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new KeenEyes.Generators.ComponentGenerator();
        Microsoft.CodeAnalysis.GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        var runResult = driver.GetRunResult();
        var generatedSources = runResult.GeneratedTrees
            .Select(t => t.GetText().ToString())
            .ToList();

        return (diagnostics, generatedSources);
    }
}
