using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace KeenEyes.Generators.Tests;

/// <summary>
/// Tests for World bundle extension generation (Add/Remove bundle operations).
/// </summary>
public class WorldBundleExtensionsTests
{
    [Fact]
    public void BundleGenerator_GeneratesWorldAddExtension()
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

            [Component]
            public partial struct Rotation
            {
                public float Angle;
            }

            [Bundle]
            public partial struct TransformBundle
            {
                public Position Position;
                public Rotation Rotation;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t =>
            t.Contains("AddTransformBundle") &&
            t.Contains("this global::KeenEyes.World world") &&
            t.Contains("global::KeenEyes.Entity entity"));
    }

    [Fact]
    public void BundleGenerator_GeneratesWorldRemoveExtension()
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

            [Bundle]
            public partial struct SimpleBundle
            {
                public Position Position;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t =>
            t.Contains("RemoveSimpleBundle") &&
            t.Contains("this global::KeenEyes.World world") &&
            t.Contains("global::KeenEyes.Entity entity"));
    }

    [Fact]
    public void BundleGenerator_WorldAddExtension_HasAllParameters()
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

            [Component]
            public partial struct Velocity
            {
                public float X;
                public float Y;
            }

            [Component]
            public partial struct Scale
            {
                public float Value;
            }

            [Bundle]
            public partial struct ComplexBundle
            {
                public Position Position;
                public Velocity Velocity;
                public Scale Scale;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);

        // Verify all parameters are present
        Assert.Contains(generatedTrees, t =>
            t.Contains("AddComplexBundle") &&
            t.Contains("TestApp.Position position") &&
            t.Contains("TestApp.Velocity velocity") &&
            t.Contains("TestApp.Scale scale"));
    }

    [Fact]
    public void BundleGenerator_WorldAddExtension_CreatesBundle()
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

            [Bundle]
            public partial struct SimpleBundle
            {
                public Position Position;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t =>
            t.Contains("var bundle = new TestApp.SimpleBundle(position)"));
    }

    [Fact]
    public void BundleGenerator_WorldAddExtension_CallsWorldAdd()
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

            [Component]
            public partial struct Rotation
            {
                public float Angle;
            }

            [Bundle]
            public partial struct TransformBundle
            {
                public Position Position;
                public Rotation Rotation;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t =>
            t.Contains("world.Add(entity, bundle.Position)") &&
            t.Contains("world.Add(entity, bundle.Rotation)"));
    }

    [Fact]
    public void BundleGenerator_WorldRemoveExtension_CallsWorldRemove()
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

            [Component]
            public partial struct Velocity
            {
                public float X;
                public float Y;
            }

            [Bundle]
            public partial struct PhysicsBundle
            {
                public Position Position;
                public Velocity Velocity;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t =>
            t.Contains("world.Remove<TestApp.Position>(entity)") &&
            t.Contains("world.Remove<TestApp.Velocity>(entity)"));
    }

    [Fact]
    public void BundleGenerator_WorldRemoveExtension_NoParameters()
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

            [Bundle]
            public partial struct SimpleBundle
            {
                public Position Position;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);

        // Verify Remove method signature has only world and entity parameters
        Assert.Contains(generatedTrees, t =>
            t.Contains("RemoveSimpleBundle(this global::KeenEyes.World world, global::KeenEyes.Entity entity)"));
    }

    [Fact]
    public void BundleGenerator_WorldExtensions_HasXmlDocumentation()
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

            [Bundle]
            public partial struct SimpleBundle
            {
                public Position Position;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);

        // Verify XML documentation exists
        Assert.Contains(generatedTrees, t =>
            t.Contains("/// <summary>") &&
            t.Contains("/// <param name=\"world\">") &&
            t.Contains("/// <param name=\"entity\">"));
    }

    [Fact]
    public void BundleGenerator_WorldExtensions_InCorrectNamespace()
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

            [Bundle]
            public partial struct SimpleBundle
            {
                public Position Position;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);

        // Verify extensions are in KeenEyes namespace
        Assert.Contains(generatedTrees, t =>
            t.Contains("namespace KeenEyes;") &&
            t.Contains("public static partial class WorldBundleExtensions"));
    }

    [Fact]
    public void BundleGenerator_WithMultipleBundles_GeneratesAllExtensions()
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

            [Component]
            public partial struct Velocity
            {
                public float X;
                public float Y;
            }

            [Component]
            public partial struct Health
            {
                public int Current;
                public int Max;
            }

            [Bundle]
            public partial struct PhysicsBundle
            {
                public Position Position;
                public Velocity Velocity;
            }

            [Bundle]
            public partial struct HealthBundle
            {
                public Health Health;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);

        // Verify both Add methods exist
        Assert.Contains(generatedTrees, t => t.Contains("AddPhysicsBundle"));
        Assert.Contains(generatedTrees, t => t.Contains("AddHealthBundle"));

        // Verify both Remove methods exist
        Assert.Contains(generatedTrees, t => t.Contains("RemovePhysicsBundle"));
        Assert.Contains(generatedTrees, t => t.Contains("RemoveHealthBundle"));
    }

    [Fact]
    public void BundleGenerator_WorldExtensions_GeneratesForQualifiedNames()
    {
        var source = """
            using KeenEyes;

            namespace Game.Components;

            [Component]
            public partial struct Position
            {
                public float X;
                public float Y;
            }

            namespace Game.Bundles;

            using Game.Components;

            [Bundle]
            public partial struct QualifiedBundle
            {
                public Position Position;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);

        // Verify World extensions are generated (names will be fully qualified)
        Assert.Contains(generatedTrees, t =>
            t.Contains("AddQualifiedBundle"));

        Assert.Contains(generatedTrees, t =>
            t.Contains("RemoveQualifiedBundle"));
    }

    [Fact]
    public void BundleGenerator_WorldExtensions_WithTagComponent()
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

            [TagComponent]
            public partial struct PlayerTag { }

            [Bundle]
            public partial struct PlayerBundle
            {
                public Position Position;
                public PlayerTag Tag;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);

        // Verify tag component is handled
        Assert.Contains(generatedTrees, t =>
            t.Contains("TestApp.PlayerTag tag") &&
            t.Contains("world.Add(entity, bundle.Tag)") &&
            t.Contains("world.Remove<TestApp.PlayerTag>(entity)"));
    }

    private static (IReadOnlyList<Diagnostic> Diagnostics, IReadOnlyList<string> GeneratedSources) RunGenerator(string source)
    {
        var abstractionsAssembly = typeof(IComponent).Assembly;

        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            MetadataReference.CreateFromFile(abstractionsAssembly.Location),
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
        var componentGenerator = new ComponentGenerator();
        var bundleGenerator = new BundleGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(markerGenerator, componentGenerator, bundleGenerator);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var _);

        var runResult = driver.GetRunResult();
        // Filter to only include output from BundleGenerator, not MarkerAttributesGenerator
        var bundleResult = runResult.Results.FirstOrDefault(r => r.Generator.GetType() == typeof(BundleGenerator));
        var generatedSources = bundleResult.GeneratedSources.IsDefault
            ? []
            : bundleResult.GeneratedSources.Select(s => s.SourceText.ToString()).ToList();

        // Get all diagnostics from the generator run
        var allDiagnostics = runResult.Diagnostics.ToList();

        return (allDiagnostics, generatedSources);
    }
}
