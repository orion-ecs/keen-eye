using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace KeenEyes.Generators.Tests;

public class SystemGeneratorTests
{
    [Fact]
    public void SystemGenerator_WithNoSystems_GeneratesNoOutput()
    {
        var source = """
            namespace TestApp;

            public class NotASystem
            {
                public void Update() { }
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.Empty(diagnostics);
        Assert.Empty(generatedTrees);
    }

    [Fact]
    public void SystemGenerator_WithSystem_GeneratesPartialClass()
    {
        var source = """
            namespace TestApp;

            [KeenEyes.System]
            public partial class MovementSystem
            {
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("partial class MovementSystem"));
    }

    [Fact]
    public void SystemGenerator_WithDefaultValues_GeneratesUpdatePhase()
    {
        var source = """
            namespace TestApp;

            [KeenEyes.System]
            public partial class DefaultSystem
            {
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t =>
            t.Contains("Phase => global::KeenEyes.SystemPhase.Update"));
        Assert.Contains(generatedTrees, t => t.Contains("Order => 0"));
        Assert.Contains(generatedTrees, t => t.Contains("Group => null"));
    }

    [Theory]
    [InlineData("EarlyUpdate")]
    [InlineData("FixedUpdate")]
    [InlineData("Update")]
    [InlineData("LateUpdate")]
    [InlineData("Render")]
    [InlineData("PostRender")]
    public void SystemGenerator_WithPhase_GeneratesCorrectPhase(string phase)
    {
        var source = $$"""
            namespace TestApp;

            [KeenEyes.System(Phase = KeenEyes.SystemPhase.{{phase}})]
            public partial class PhaseTestSystem
            {
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t =>
            t.Contains($"Phase => global::KeenEyes.SystemPhase.{phase}"));
    }

    [Theory]
    [InlineData(-100)]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(int.MaxValue)]
    public void SystemGenerator_WithOrder_GeneratesCorrectOrder(int order)
    {
        var source = $$"""
            namespace TestApp;

            [KeenEyes.System(Order = {{order}})]
            public partial class OrderTestSystem
            {
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains($"Order => {order}"));
    }

    [Fact]
    public void SystemGenerator_WithGroup_GeneratesCorrectGroup()
    {
        var source = """
            namespace TestApp;

            [KeenEyes.System(Group = "Physics")]
            public partial class GroupTestSystem
            {
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("Group => \"Physics\""));
    }

    [Fact]
    public void SystemGenerator_WithAllProperties_GeneratesAllMetadata()
    {
        var source = """
            namespace TestApp;

            [KeenEyes.System(Phase = KeenEyes.SystemPhase.FixedUpdate, Order = 10, Group = "AI")]
            public partial class FullMetadataSystem
            {
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t =>
            t.Contains("Phase => global::KeenEyes.SystemPhase.FixedUpdate"));
        Assert.Contains(generatedTrees, t => t.Contains("Order => 10"));
        Assert.Contains(generatedTrees, t => t.Contains("Group => \"AI\""));
    }

    [Fact]
    public void SystemGenerator_InGlobalNamespace_GeneratesWithoutNamespace()
    {
        var source = """
            [KeenEyes.System]
            public partial class GlobalSystem
            {
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("partial class GlobalSystem"));
        // Should not have namespace declaration (only auto-generated header)
        Assert.Contains(generatedTrees, t =>
        {
            var lines = t.Split('\n');
            return !lines.Any(l => l.TrimStart().StartsWith("namespace") && !l.Contains("<global namespace>"));
        });
    }

    [Fact]
    public void SystemGenerator_InNestedNamespace_GeneratesCorrectNamespace()
    {
        var source = """
            namespace Game.Systems.Movement;

            [KeenEyes.System]
            public partial class WalkSystem
            {
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("namespace Game.Systems.Movement;"));
    }

    [Fact]
    public void SystemGenerator_GeneratesXmlDocumentation()
    {
        var source = """
            namespace TestApp;

            [KeenEyes.System]
            public partial class DocumentedSystem
            {
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t =>
            t.Contains("/// <summary>The execution phase for this system.</summary>"));
        Assert.Contains(generatedTrees, t =>
            t.Contains("/// <summary>The execution order within the phase.</summary>"));
        Assert.Contains(generatedTrees, t =>
            t.Contains("/// <summary>The system group name, if any.</summary>"));
    }

    [Fact]
    public void SystemGenerator_MultipleSystems_GeneratesSeparateFiles()
    {
        var source = """
            namespace TestApp;

            [KeenEyes.System(Order = 1)]
            public partial class FirstSystem { }

            [KeenEyes.System(Order = 2)]
            public partial class SecondSystem { }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("partial class FirstSystem"));
        Assert.Contains(generatedTrees, t => t.Contains("partial class SecondSystem"));
    }

    [Fact]
    public void SystemGenerator_GeneratesAutoGeneratedHeader()
    {
        var source = """
            namespace TestApp;

            [KeenEyes.System]
            public partial class HeaderSystem
            {
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("// <auto-generated />"));
        Assert.Contains(generatedTrees, t => t.Contains("#nullable enable"));
    }

    [Fact]
    public void SystemGenerator_OnStruct_GeneratesNoOutput()
    {
        var source = """
            namespace TestApp;

            [KeenEyes.System]
            public partial struct NotAClassSystem { }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        // The attribute should not apply to structs, so no output
        Assert.Empty(generatedTrees);
    }

    private static (IReadOnlyList<Diagnostic> Diagnostics, IReadOnlyList<string> GeneratedSources) RunGenerator(string source)
    {
        var attributesAssembly = typeof(SystemAttribute).Assembly;

        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            MetadataReference.CreateFromFile(attributesAssembly.Location),
        };

        // Add runtime assembly references
        var runtimeDir = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
        references.Add(MetadataReference.CreateFromFile(System.IO.Path.Combine(runtimeDir, "System.Runtime.dll")));
        references.Add(MetadataReference.CreateFromFile(System.IO.Path.Combine(runtimeDir, "netstandard.dll")));

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new SystemGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        var runResult = driver.GetRunResult();
        var generatedSources = runResult.GeneratedTrees
            .Select(t => t.GetText().ToString())
            .ToList();

        return (diagnostics, generatedSources);
    }
}
