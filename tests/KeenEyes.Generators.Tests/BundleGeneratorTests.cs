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

    [Fact]
    public void BundleGenerator_WithMultipleNamespaces_UsesFullyQualifiedNames()
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

            [Component]
            public partial struct Velocity
            {
                public float X;
                public float Y;
            }

            namespace Game.Bundles;

            using Game.Components;

            [Bundle]
            public partial struct PhysicsBundle
            {
                public Position Position;
                public Velocity Velocity;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);

        // Verify constructor uses fully qualified names for component types from different namespace
        Assert.Contains(generatedTrees, t =>
            t.Contains("Game.Components.Position position") &&
            t.Contains("Game.Components.Velocity velocity"));

        // Verify builder method exists with correct name
        Assert.Contains(generatedTrees, t => t.Contains("WithPhysicsBundle"));

        // Verify the constructor body assigns the fields correctly
        Assert.Contains(generatedTrees, t =>
            t.Contains("Position = position") &&
            t.Contains("Velocity = velocity"));
    }

    [Fact]
    public void BundleGenerator_GeneratesWithBundleMethod()
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

        // Verify With(TBundle bundle) method exists for generic builder
        Assert.Contains(generatedTrees, t =>
            t.Contains("public static TSelf With<TSelf>(this TSelf builder, TestApp.TransformBundle bundle)") &&
            t.Contains("where TSelf : global::KeenEyes.IEntityBuilder<TSelf>"));

        // Verify With(TBundle bundle) method exists for non-generic interface
        Assert.Contains(generatedTrees, t =>
            t.Contains("public static global::KeenEyes.IEntityBuilder With(this global::KeenEyes.IEntityBuilder builder, TestApp.TransformBundle bundle)"));
    }

    [Fact]
    public void BundleGenerator_WithBundleMethod_AddsAllComponents()
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

        // Verify the With method adds all components from the bundle
        Assert.Contains(generatedTrees, t =>
            t.Contains("builder = builder.With(bundle.Position)") &&
            t.Contains("builder = builder.With(bundle.Rotation)") &&
            t.Contains("builder = builder.With(bundle.Scale)"));
    }

    [Fact]
    public void BundleGenerator_WithBundleMethod_HasProperXmlDocs()
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

        // Verify XML documentation is present
        Assert.Contains(generatedTrees, t =>
            t.Contains("/// <summary>") &&
            t.Contains("/// Adds all components from a <see cref=\"TestApp.SimpleBundle\"/> bundle to the entity.") &&
            t.Contains("/// </summary>") &&
            t.Contains("/// <param name=\"builder\">The entity builder.</param>") &&
            t.Contains("/// <param name=\"bundle\">The bundle containing components to add.</param>") &&
            t.Contains("/// <returns>The builder for method chaining.</returns>"));
    }

    [Fact]
    public void BundleGenerator_GeneratesBothWithMethodAndWithNameMethod()
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
            public partial struct LocationBundle
            {
                public Position Position;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);

        // Verify both methods exist
        Assert.Contains(generatedTrees, t => t.Contains("With<TSelf>(this TSelf builder, TestApp.LocationBundle bundle)"));
        Assert.Contains(generatedTrees, t => t.Contains("WithLocationBundle<TSelf>(this TSelf builder, TestApp.Position position)"));
    }

    [Fact(Skip = "Nested bundles implementation in progress")]
    public void BundleGenerator_WithNestedBundle_GeneratesCode()
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
            public partial struct Health
            {
                public int Current;
                public int Max;
            }

            [Bundle]
            public partial struct TransformBundle
            {
                public Position Position;
                public Rotation Rotation;
            }

            [Bundle]
            public partial struct CharacterBundle
            {
                public TransformBundle Transform;
                public Health Health;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("partial struct CharacterBundle"));
        Assert.Contains(generatedTrees, t => t.Contains("partial struct TransformBundle"));
    }

    [Fact(Skip = "Nested bundles implementation in progress")]
    public void BundleGenerator_WithNestedBundle_GeneratesRecursiveWith()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component]
            public partial struct Position { public float X, Y; }

            [Component]
            public partial struct Health { public int Current, Max; }

            [Bundle]
            public partial struct TransformBundle
            {
                public Position Position;
            }

            [Bundle]
            public partial struct CharacterBundle
            {
                public TransformBundle Transform;
                public Health Health;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);

        // Verify that the CharacterBundle's With method calls With on the nested TransformBundle
        var builderExtensions = generatedTrees.FirstOrDefault(t => t.Contains("EntityBuilderExtensions"));
        Assert.NotNull(builderExtensions);
        Assert.Contains("builder.With(bundle.Transform)", builderExtensions);
        Assert.Contains("builder.With(bundle.Health)", builderExtensions);
    }

    [Fact(Skip = "Optional fields implementation in progress")]
    public void BundleGenerator_WithOptionalField_GeneratesNullableType()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component]
            public partial struct Position { public float X, Y; }

            [Component]
            public partial struct Health { public int Current, Max; }

            [Bundle]
            public partial struct EnemyBundle
            {
                public Position Position;
                public Health Health;

                [Optional]
                public Health? Shield;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);

        // Verify that optional field handling is generated
        var builderExtensions = generatedTrees.FirstOrDefault(t => t.Contains("EntityBuilderExtensions"));
        Assert.NotNull(builderExtensions);
        Assert.Contains("if (bundle.Shield.HasValue)", builderExtensions);
        Assert.Contains("builder.With(bundle.Shield.Value)", builderExtensions);
    }

    [Fact]
    public void BundleGenerator_WithOptionalNonNullableField_ProducesDiagnostic()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component]
            public partial struct Position { public float X, Y; }

            [Bundle]
            public partial struct InvalidBundle
            {
                [Optional]
                public Position Position; // Not nullable!
            }
            """;

        var (diagnostics, _) = RunGenerator(source);

        Assert.Contains(diagnostics, d =>
            d.Id == "KEEN025" &&
            d.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void BundleGenerator_WithCircularNestedBundle_ProducesDiagnostic()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component]
            public partial struct Position { public float X, Y; }

            [Bundle]
            public partial struct CircularBundle
            {
                public Position Position;
                public CircularBundle? Nested;
            }
            """;

        var (diagnostics, _) = RunGenerator(source);

        Assert.Contains(diagnostics, d =>
            d.Id == "KEEN023" &&
            d.Severity == DiagnosticSeverity.Error);
    }

    [Fact(Skip = "Depth limit validation in progress")]
    public void BundleGenerator_WithDeepNesting_ProducesDiagnosticAtLimit()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component]
            public partial struct Position { public float X, Y; }

            [Bundle]
            public partial struct Level1
            {
                public Position Position;
            }

            [Bundle]
            public partial struct Level2
            {
                public Level1 L1;
            }

            [Bundle]
            public partial struct Level3
            {
                public Level2 L2;
            }

            [Bundle]
            public partial struct Level4
            {
                public Level3 L3;
            }

            [Bundle]
            public partial struct Level5
            {
                public Level4 L4;
            }

            [Bundle]
            public partial struct Level6
            {
                public Level5 L5; // This should exceed the depth limit
            }
            """;

        var (diagnostics, _) = RunGenerator(source);

        // Should get KEEN024 for exceeding nesting depth
        Assert.Contains(diagnostics, d =>
            d.Id == "KEEN024" &&
            d.Severity == DiagnosticSeverity.Error);
    }

    [Fact(Skip = "Optional nested bundles in progress")]
    public void BundleGenerator_WithOptionalNestedBundle_GeneratesNullCheck()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component]
            public partial struct Position { public float X, Y; }

            [Component]
            public partial struct Health { public int Current, Max; }

            [Bundle]
            public partial struct TransformBundle
            {
                public Position Position;
            }

            [Bundle]
            public partial struct CharacterBundle
            {
                public Health Health;

                [Optional]
                public TransformBundle? Transform;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);

        // Verify that optional nested bundle handling is generated
        var builderExtensions = generatedTrees.FirstOrDefault(t => t.Contains("EntityBuilderExtensions"));
        Assert.NotNull(builderExtensions);
        Assert.Contains("if (bundle.Transform.HasValue)", builderExtensions);
        Assert.Contains("builder.With(bundle.Transform.Value)", builderExtensions);
    }

    [Fact]
    public void BundleGenerator_WithMultiLevelNesting_GeneratesCorrectly()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Component]
            public partial struct Position { public float X, Y; }

            [Component]
            public partial struct Rotation { public float Angle; }

            [Component]
            public partial struct Scale { public float X, Y; }

            [Component]
            public partial struct Health { public int Current, Max; }

            [Bundle]
            public partial struct TransformBundle
            {
                public Position Position;
                public Rotation Rotation;
                public Scale Scale;
            }

            [Bundle]
            public partial struct ActorBundle
            {
                public Health Health;
            }

            [Bundle]
            public partial struct CharacterBundle
            {
                public TransformBundle Transform;
                public ActorBundle Actor;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);

        // Verify all bundles are generated
        Assert.Contains(generatedTrees, t => t.Contains("partial struct TransformBundle"));
        Assert.Contains(generatedTrees, t => t.Contains("partial struct ActorBundle"));
        Assert.Contains(generatedTrees, t => t.Contains("partial struct CharacterBundle"));
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
        references.Add(MetadataReference.CreateFromFile(System.IO.Path.Join(runtimeDir, "System.Runtime.dll")));
        references.Add(MetadataReference.CreateFromFile(System.IO.Path.Join(runtimeDir, "netstandard.dll")));

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
