using KeenEyes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace KeenEyes.Generators.Tests;

public class ComponentValidationGeneratorTests
{
    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void ValidationGenerator_WithNoValidationAttributes_GeneratesNoOutput()
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

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        // Should not generate validation metadata since no validation attributes
        Assert.DoesNotContain(generatedTrees, t => t.Contains("ComponentValidationMetadata"));
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void ValidationGenerator_WithRequiresComponent_GeneratesMetadata()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component]
            public partial struct Transform
            {
                public float X, Y;
            }

            [Component]
            [RequiresComponent(typeof(Transform))]
            public partial struct Renderable
            {
                public string TextureId;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("ComponentValidationMetadata"));
        Assert.Contains(generatedTrees, t => t.Contains("typeof(TestApp.Transform)"));
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void ValidationGenerator_WithConflictsWith_GeneratesMetadata()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component]
            [ConflictsWith(typeof(DynamicBody))]
            public partial struct StaticBody
            {
                public bool IsKinematic;
            }

            [Component]
            public partial struct DynamicBody
            {
                public float Mass;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("ComponentValidationMetadata"));
        Assert.Contains(generatedTrees, t => t.Contains("typeof(TestApp.DynamicBody)"));
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void ValidationGenerator_WithMultipleRequires_GeneratesArrayWithAll()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component]
            public partial struct Transform { public float X, Y; }

            [Component]
            public partial struct Renderable { public string TextureId; }

            [Component]
            [RequiresComponent(typeof(Transform))]
            [RequiresComponent(typeof(Renderable))]
            public partial struct Sprite
            {
                public int Layer;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        var metadataFile = generatedTrees.FirstOrDefault(t => t.Contains("ComponentValidationMetadata"));
        Assert.NotNull(metadataFile);
        Assert.Contains("typeof(TestApp.Transform)", metadataFile);
        Assert.Contains("typeof(TestApp.Renderable)", metadataFile);
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void ValidationGenerator_GeneratesTryGetConstraintsMethod()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component]
            public partial struct Transform { public float X, Y; }

            [Component]
            [RequiresComponent(typeof(Transform))]
            public partial struct Renderable
            {
                public string TextureId;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("TryGetConstraints"));
        Assert.Contains(generatedTrees, t => t.Contains("out Type[] required"));
        Assert.Contains(generatedTrees, t => t.Contains("out Type[] conflicts"));
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void ValidationGenerator_GeneratesHasConstraintsMethod()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component]
            public partial struct Transform { public float X, Y; }

            [Component]
            [RequiresComponent(typeof(Transform))]
            public partial struct Renderable
            {
                public string TextureId;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("HasConstraints(Type componentType)"));
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void ValidationGenerator_GeneratesTypedAccessorMethods()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component]
            public partial struct Transform { public float X, Y; }

            [Component]
            [RequiresComponent(typeof(Transform))]
            public partial struct Renderable
            {
                public string TextureId;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("GetRenderableConstraints()"));
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void ValidationGenerator_WithTagComponent_GeneratesMetadata()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component]
            public partial struct Player { }

            [TagComponent]
            [RequiresComponent(typeof(Player))]
            public partial struct Active;
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("ComponentValidationMetadata"));
        Assert.Contains(generatedTrees, t => t.Contains("typeof(TestApp.Player)"));
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void ValidationGenerator_WithBothRequiresAndConflicts_GeneratesBoth()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component]
            public partial struct Transform { public float X, Y; }

            [Component]
            public partial struct StaticBody { }

            [Component]
            [RequiresComponent(typeof(Transform))]
            [ConflictsWith(typeof(StaticBody))]
            public partial struct RigidBody
            {
                public float Mass;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        var metadataFile = generatedTrees.FirstOrDefault(t => t.Contains("ComponentValidationMetadata"));
        Assert.NotNull(metadataFile);
        // Should have Transform in Required array and StaticBody in Conflicts array
        Assert.Contains("typeof(TestApp.Transform)", metadataFile);
        Assert.Contains("typeof(TestApp.StaticBody)", metadataFile);
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void ValidationGenerator_MultipleComponents_GeneratesConstraintsForAll()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component]
            public partial struct Transform { public float X, Y; }

            [Component]
            [RequiresComponent(typeof(Transform))]
            public partial struct Renderable { public string TextureId; }

            [Component]
            [ConflictsWith(typeof(DynamicBody))]
            public partial struct StaticBody { }

            [Component]
            public partial struct DynamicBody { }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        var metadataFile = generatedTrees.FirstOrDefault(t => t.Contains("ComponentValidationMetadata"));
        Assert.NotNull(metadataFile);
        // Should have entries for both Renderable and StaticBody
        Assert.Contains("typeof(TestApp.Renderable)", metadataFile);
        Assert.Contains("typeof(TestApp.StaticBody)", metadataFile);
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
        references.Add(MetadataReference.CreateFromFile(System.IO.Path.Join(runtimeDir, "System.Runtime.dll")));
        references.Add(MetadataReference.CreateFromFile(System.IO.Path.Join(runtimeDir, "System.Collections.dll")));
        references.Add(MetadataReference.CreateFromFile(System.IO.Path.Join(runtimeDir, "netstandard.dll")));

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // Run MarkerAttributesGenerator first to generate the attributes
        var markerGenerator = new MarkerAttributesGenerator();
        var validationGenerator = new ComponentValidationGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(markerGenerator, validationGenerator);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        var runResult = driver.GetRunResult();
        // Filter to only include output from ComponentValidationGenerator, not MarkerAttributesGenerator
        var validationResult = runResult.Results.FirstOrDefault(r => r.Generator.GetType() == typeof(ComponentValidationGenerator));
        var generatedSources = validationResult.GeneratedSources.IsDefault
            ? []
            : validationResult.GeneratedSources.Select(s => s.SourceText.ToString()).ToList();

        return (diagnostics.ToList(), generatedSources);
    }
}
