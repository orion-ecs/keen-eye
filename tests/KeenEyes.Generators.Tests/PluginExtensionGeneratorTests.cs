using KeenEyes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace KeenEyes.Generators.Tests;

public class PluginExtensionGeneratorTests
{
    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void PluginExtensionGenerator_WithNoExtensions_GeneratesNoOutput()
    {
        var source = """
            namespace TestApp;

            public class NotAnExtension
            {
                public int Value { get; set; }
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.Empty(diagnostics);
        Assert.Empty(generatedTrees);
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void PluginExtensionGenerator_WithPluginExtension_GeneratesExtensionProperty()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [PluginExtension("Physics")]
            public class PhysicsWorld
            {
                public int Gravity { get; set; }
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Single(generatedTrees);
        Assert.Contains(generatedTrees, t => t.Contains("extension(global::KeenEyes.World world)"));
        Assert.Contains(generatedTrees, t => t.Contains("PhysicsWorld Physics =>"));
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void PluginExtensionGenerator_GeneratesStaticClass()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [PluginExtension("Stats")]
            public class DebugStats
            {
                public int Count { get; set; }
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("public static class WorldPluginExtensions"));
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void PluginExtensionGenerator_NonNullable_CallsGetExtension()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [PluginExtension("Physics")]
            public class PhysicsWorld { }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("world.GetExtension<"));
        Assert.DoesNotContain(generatedTrees, t => t.Contains("TryGetExtension"));
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void PluginExtensionGenerator_Nullable_CallsTryGetExtension()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [PluginExtension("Physics", Nullable = true)]
            public class PhysicsWorld { }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("TryGetExtension<"));
        Assert.Contains(generatedTrees, t => t.Contains("PhysicsWorld?"));
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void PluginExtensionGenerator_MultipleExtensions_GeneratesAllProperties()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [PluginExtension("Physics")]
            public class PhysicsWorld { }

            [PluginExtension("Audio")]
            public class AudioManager { }

            [PluginExtension("Debug")]
            public class DebugStats { }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Single(generatedTrees); // All in one file
        Assert.Contains(generatedTrees, t => t.Contains("Physics"));
        Assert.Contains(generatedTrees, t => t.Contains("Audio"));
        Assert.Contains(generatedTrees, t => t.Contains("Debug"));
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void PluginExtensionGenerator_UsesFullyQualifiedTypeName()
    {
        var source = """
            using KeenEyes;

            namespace Game.Plugins.Physics;

            [PluginExtension("Physics")]
            public class PhysicsWorld { }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("global::Game.Plugins.Physics.PhysicsWorld"));
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void PluginExtensionGenerator_GeneratesAutoGeneratedHeader()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [PluginExtension("Test")]
            public class TestExtension { }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.All(generatedTrees, t => Assert.Contains("// <auto-generated />", t));
        Assert.All(generatedTrees, t => Assert.Contains("#nullable enable", t));
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void PluginExtensionGenerator_GeneratesXmlDocumentation()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [PluginExtension("Physics")]
            public class PhysicsWorld { }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("/// <summary>"));
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void PluginExtensionGenerator_NonNullable_DocumentsException()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [PluginExtension("Physics")]
            public class PhysicsWorld { }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("InvalidOperationException"));
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void PluginExtensionGenerator_OnStruct_GeneratesNoOutput()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [PluginExtension("Test")]
            public struct NotAClass { }
            """;

        var (_, generatedTrees) = RunGenerator(source);

        // Should not generate for structs
        Assert.Empty(generatedTrees);
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void PluginExtensionGenerator_InGlobalNamespace_GeneratesWithoutGlobalPrefix()
    {
        var source = """
            using KeenEyes;

            [PluginExtension("Global")]
            public class GlobalExtension { }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("GlobalExtension Global =>"));
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void PluginExtensionGenerator_PropertyNameMatchesAttributeValue()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [PluginExtension("CustomName")]
            public class DifferentClassName { }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("CustomName =>"));
        Assert.DoesNotContain(generatedTrees, t => t.Contains("DifferentClassName =>"));
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void PluginExtensionGenerator_Nullable_DocumentsNullReturn()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [PluginExtension("Physics", Nullable = true)]
            public class PhysicsWorld { }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("otherwise, null"));
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void PluginExtensionGenerator_GeneratesNamespaceCorrectly()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [PluginExtension("Test")]
            public class TestExtension { }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("namespace KeenEyes;"));
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void PluginExtensionGenerator_WithNullPropertyName_ReportsError()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            #pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type
            [PluginExtension(null)]
            #pragma warning restore CS8625
            public class TestExtension { }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        // Should not generate when property name is null
        Assert.Empty(generatedTrees);

        // Should report KEEN005 error
        Assert.Contains(diagnostics, d => d.Id == "KEEN005" && d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(diagnostics, d => d.GetMessage().Contains("TestExtension"));
    }

    private static (IReadOnlyList<Diagnostic> Diagnostics, IReadOnlyList<string> GeneratedSources) RunGenerator(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(SystemPhase).Assembly.Location), // KeenEyes.Abstractions
        };

        // Add runtime assembly references
        var runtimeDir = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
        references.Add(MetadataReference.CreateFromFile(System.IO.Path.Combine(runtimeDir, "System.Runtime.dll")));
        references.Add(MetadataReference.CreateFromFile(System.IO.Path.Combine(runtimeDir, "netstandard.dll")));
        references.Add(MetadataReference.CreateFromFile(System.IO.Path.Join(runtimeDir, "System.Collections.dll")));

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // Run MarkerAttributesGenerator first to generate the attributes
        var markerGenerator = new MarkerAttributesGenerator();
        var generator = new PluginExtensionGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(markerGenerator, generator);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        var runResult = driver.GetRunResult();
        // Only get the trees generated by PluginExtensionGenerator (skip MarkerAttributesGenerator output)
        var generatorResult = runResult.Results.FirstOrDefault(r => r.Generator.GetType() == typeof(PluginExtensionGenerator));
        var generatedSources = generatorResult.GeneratedSources.IsDefault
            ? []
            : generatorResult.GeneratedSources.Select(s => s.SourceText.ToString()).ToList();

        return (diagnostics, generatedSources);
    }
}
