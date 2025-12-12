using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace KeenEyes.Generators.Tests;

/// <summary>
/// Tests for bundle query and GetBundle generation features.
/// </summary>
public class BundleQueryGeneratorTests
{
    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void BundleGenerator_GeneratesRefStruct()
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

        // Verify ref struct is generated
        Assert.Contains(generatedTrees, t => t.Contains("public ref struct TransformBundleRef"));

        // Verify ref fields
        Assert.Contains(generatedTrees, t =>
            t.Contains("public ref TestApp.Position Position") &&
            t.Contains("public ref TestApp.Rotation Rotation"));
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void BundleGenerator_RefStruct_HasConstructor()
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

        // Verify constructor with ref parameters
        Assert.Contains(generatedTrees, t =>
            t.Contains("public SimpleBundleRef(ref TestApp.Position position)"));
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void BundleGenerator_GeneratesGetBundleExtension()
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

        // Verify GetBundle extension method
        Assert.Contains(generatedTrees, t =>
            t.Contains("public static TestApp.PhysicsBundleRef GetBundle"));

        // Verify it returns the ref struct
        Assert.Contains(generatedTrees, t =>
            t.Contains("return new TestApp.PhysicsBundleRef("));
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void BundleGenerator_GeneratesQueryMethod()
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

        // Verify Query<T>() method
        Assert.Contains(generatedTrees, t =>
            t.Contains("public QueryBuilder<TestApp.Position, TestApp.Rotation> Query<T>()"));
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void BundleGenerator_GeneratesWithBundleFilter()
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

        // Verify WithTransformBundle<TBundle>() extension exists for different QueryBuilder arities
        Assert.Contains(generatedTrees, t =>
            t.Contains("public static QueryBuilder<T1> WithTransformBundle<TBundle, T1>"));

        Assert.Contains(generatedTrees, t =>
            t.Contains("public static QueryBuilder<T1, T2> WithTransformBundle<TBundle, T1, T2>"));
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void BundleGenerator_GeneratesWithoutBundleFilter()
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

        // Verify WithoutTransformBundle<TBundle>() extension exists
        Assert.Contains(generatedTrees, t =>
            t.Contains("public static QueryBuilder<T1> WithoutTransformBundle<TBundle, T1>"));
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void BundleGenerator_WithBundleFilter_CallsWithForEachComponent()
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

            [Component]
            public partial struct Scale
            {
                public float X;
                public float Y;
            }

            [Bundle]
            public partial struct TransformBundle
            {
                public Position Position;
                public Rotation Rotation;
                public Scale Scale;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);

        // Verify With<TBundle>() calls With<> for each component
        Assert.Contains(generatedTrees, t =>
            t.Contains("builder = builder.With<TestApp.Position>()") &&
            t.Contains("builder = builder.With<TestApp.Rotation>()") &&
            t.Contains("builder = builder.With<TestApp.Scale>()"));
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void BundleGenerator_WithSingleComponentBundle_GeneratesQueryBuilderOfOne()
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
            public partial struct SingleBundle
            {
                public Position Position;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);

        // Verify Query<T>() returns QueryBuilder<Position>
        Assert.Contains(generatedTrees, t =>
            t.Contains("public QueryBuilder<TestApp.Position> Query<T>()"));
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void BundleGenerator_WithThreeComponentBundle_GeneratesQueryBuilderOfThree()
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

            [Component]
            public partial struct Scale
            {
                public float X;
                public float Y;
            }

            [Bundle]
            public partial struct TransformBundle
            {
                public Position Position;
                public Rotation Rotation;
                public Scale Scale;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);

        // Verify Query<T>() returns QueryBuilder<Position, Rotation, Scale>
        Assert.Contains(generatedTrees, t =>
            t.Contains("public QueryBuilder<TestApp.Position, TestApp.Rotation, TestApp.Scale> Query<T>()"));
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void BundleGenerator_RefStruct_HasXmlDocumentation()
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
            t.Contains("/// Ref struct providing zero-copy access"));
    }

    [Trait("Category", "SourceGenerator")]
    [Fact]
    public void BundleGenerator_GetBundleExtension_HasXmlDocumentation()
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

        // Verify XML documentation for GetBundle
        Assert.Contains(generatedTrees, t =>
            t.Contains("/// <summary>") &&
            t.Contains("/// Gets a ref struct with references to all components"));
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
