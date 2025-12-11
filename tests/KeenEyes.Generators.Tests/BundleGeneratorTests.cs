using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace KeenEyes.Generators.Tests;

public class BundleGeneratorTests
{
    [Fact]
    public void BundleGenerator_WithNoBundle_GeneratesNoOutput()
    {
        var source = """
            namespace TestApp;

            public struct NotABundle
            {
                public int X;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.Empty(diagnostics);
        Assert.Empty(generatedTrees);
    }

    [Fact]
    public void BundleGenerator_WithValidBundle_GeneratesPartialStruct()
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
        Assert.Contains(generatedTrees, t => t.Contains("partial struct TransformBundle"));
    }

    [Fact]
    public void BundleGenerator_ImplementsIBundle()
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
        Assert.Contains(generatedTrees, t => t.Contains("IBundle"));
    }

    [Fact]
    public void BundleGenerator_GeneratesConstructor()
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
            t.Contains("public PhysicsBundle(") &&
            t.Contains("TestApp.Position position") &&
            t.Contains("TestApp.Velocity velocity"));
    }

    [Fact]
    public void BundleGenerator_GeneratesBuilderExtension()
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
        Assert.Contains(generatedTrees, t => t.Contains("WithSimpleBundle"));
    }

    [Fact]
    public void BundleGenerator_OnClass_ProducesDiagnostic()
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
            public partial class NotAStruct
            {
                public Position Position;
            }
            """;

        var (diagnostics, _) = RunGenerator(source);

        Assert.Contains(diagnostics, d =>
            d.Id == "KEEN020" &&
            d.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void BundleGenerator_WithNonComponentField_ProducesDiagnostic()
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

            public struct NotAComponent
            {
                public int Value;
            }

            [Bundle]
            public partial struct InvalidBundle
            {
                public Position Position;
                public NotAComponent Invalid;
            }
            """;

        var (diagnostics, _) = RunGenerator(source);

        Assert.Contains(diagnostics, d =>
            d.Id == "KEEN021" &&
            d.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void BundleGenerator_WithNoFields_ProducesDiagnostic()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Bundle]
            public partial struct EmptyBundle
            {
            }
            """;

        var (diagnostics, _) = RunGenerator(source);

        Assert.Contains(diagnostics, d =>
            d.Id == "KEEN022" &&
            d.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void BundleGenerator_WithCircularReference_ProducesDiagnostic()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Bundle]
            public partial struct RecursiveBundle
            {
                public RecursiveBundle Self;
            }
            """;

        var (diagnostics, _) = RunGenerator(source);

        Assert.Contains(diagnostics, d =>
            d.Id == "KEEN023" &&
            d.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void BundleGenerator_WithMultipleComponents_GeneratesAllParameters()
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

        // Verify constructor has all parameters
        Assert.Contains(generatedTrees, t =>
            t.Contains("TestApp.Position position") &&
            t.Contains("TestApp.Rotation rotation") &&
            t.Contains("TestApp.Scale scale"));

        // Verify builder method exists
        Assert.Contains(generatedTrees, t => t.Contains("WithTransformBundle"));
    }

    [Fact]
    public void BundleGenerator_BuilderExtensionUsesFullyQualifiedName()
    {
        var source = """
            using KeenEyes;

            namespace Game.Bundles;

            [Component]
            public partial struct Position
            {
                public float X;
                public float Y;
            }

            [Bundle]
            public partial struct QualifiedBundle
            {
                public Position Position;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("new Game.Bundles.QualifiedBundle"));
    }

    [Fact]
    public void BundleGenerator_IgnoresStaticFields()
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
            public partial struct BundleWithStatic
            {
                public Position Position;
                public static int Counter;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);

        // Verify constructor only has Position parameter, not Counter
        Assert.Contains(generatedTrees, t =>
            t.Contains("TestApp.Position position") &&
            !t.Contains("int counter"));
    }

    [Fact]
    public void BundleGenerator_WithTagComponent_Works()
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
            public partial struct MixedBundle
            {
                public Position Position;
                public PlayerTag Tag;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t =>
            t.Contains("TestApp.Position position") &&
            t.Contains("TestApp.PlayerTag tag"));
    }

    private static (IReadOnlyList<Diagnostic> Diagnostics, IReadOnlyList<string> GeneratedSources) RunGenerator(string source)
    {
        var attributesAssembly = typeof(ComponentAttribute).Assembly;
        var abstractionsAssembly = typeof(IComponent).Assembly;

        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            MetadataReference.CreateFromFile(attributesAssembly.Location),
            MetadataReference.CreateFromFile(abstractionsAssembly.Location),
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

        var componentGenerator = new ComponentGenerator();
        var bundleGenerator = new BundleGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(componentGenerator, bundleGenerator);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var _);

        var runResult = driver.GetRunResult();
        var generatedSources = runResult.GeneratedTrees
            .Select(t => t.GetText().ToString())
            .ToList();

        // Get all diagnostics from the generator run
        var allDiagnostics = runResult.Diagnostics.ToList();

        return (allDiagnostics, generatedSources);
    }
}
