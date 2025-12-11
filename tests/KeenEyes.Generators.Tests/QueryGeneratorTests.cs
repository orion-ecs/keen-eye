using KeenEyes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace KeenEyes.Generators.Tests;

public class QueryGeneratorTests
{
    [Fact]
    public void QueryGenerator_WithNoQueries_GeneratesNoOutput()
    {
        var source = """
            namespace TestApp;

            public struct NotAQuery
            {
                public int X;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.Empty(diagnostics);
        Assert.Empty(generatedTrees);
    }

    [Fact]
    public void QueryGenerator_WithQuery_GeneratesPartialStruct()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            public struct Position { public float X; public float Y; }

            [Query]
            public partial struct MovingEntities
            {
                public Position Position;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("partial struct MovingEntities"));
    }

    [Fact]
    public void QueryGenerator_GeneratesCreateDescriptionMethod()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            public struct Position { public float X; public float Y; }

            [Query]
            public partial struct TestQuery
            {
                public Position Position;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("CreateDescription()"));
        Assert.Contains(generatedTrees, t => t.Contains("QueryDescription"));
    }

    [Fact]
    public void QueryGenerator_GeneratesWorldExtensionMethod()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            public struct Position { public float X; }

            [Query]
            public partial struct ExtensionQuery
            {
                public Position Position;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("public static partial class QueryExtensions"));
        Assert.Contains(generatedTrees, t => t.Contains("this global::KeenEyes.World world"));
        Assert.Contains(generatedTrees, t => t.Contains("IEnumerable<global::KeenEyes.Entity>"));
    }

    [Fact]
    public void QueryGenerator_WithWithAttribute_TracksFieldAsWithAccess()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            public struct Position { public float X; }
            public struct Velocity { public float X; }

            [Query]
            public partial struct WithQuery
            {
                public Position Position;
                [With] public Velocity Velocity;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("// Velocity: With"));
    }

    [Fact]
    public void QueryGenerator_WithWithoutAttribute_TracksFieldAsWithoutAccess()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            public struct Position { public float X; }
            public struct Frozen { }

            [Query]
            public partial struct WithoutQuery
            {
                public Position Position;
                [Without] public Frozen Frozen;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("// Frozen: Without"));
    }

    [Fact]
    public void QueryGenerator_WithOptionalAttribute_TracksFieldAsOptional()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            public struct Position { public float X; }
            public struct Sprite { public int Id; }

            [Query]
            public partial struct OptionalQuery
            {
                public Position Position;
                [Optional] public Sprite Sprite;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        // Optional fields don't add comments to description (returns early)
        Assert.Contains(generatedTrees, t => t.Contains("partial struct OptionalQuery"));
    }

    [Fact]
    public void QueryGenerator_WithMultipleFields_TracksAllFields()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            public struct Position { public float X; public float Y; }
            public struct Velocity { public float X; public float Y; }
            public struct Health { public int Current; }

            [Query]
            public partial struct MultiFieldQuery
            {
                public Position Position;
                public Velocity Velocity;
                public Health Health;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("// Position: Write"));
        Assert.Contains(generatedTrees, t => t.Contains("// Velocity: Write"));
        Assert.Contains(generatedTrees, t => t.Contains("// Health: Write"));
    }

    [Fact]
    public void QueryGenerator_WithNoFields_GeneratesEmptyDescription()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Query]
            public partial struct EmptyQuery { }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("partial struct EmptyQuery"));
        Assert.Contains(generatedTrees, t => t.Contains("CreateDescription()"));
    }

    [Fact]
    public void QueryGenerator_IgnoresStaticFields()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            public struct Position { public float X; }

            [Query]
            public partial struct StaticFieldQuery
            {
                public static int Counter;
                public Position Position;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("// Position: Write"));
        Assert.DoesNotContain(generatedTrees, t => t.Contains("// Counter"));
    }

    [Fact]
    public void QueryGenerator_IgnoresConstFields()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            public struct Position { public float X; }

            [Query]
            public partial struct ConstFieldQuery
            {
                public const int Version = 1;
                public Position Position;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("// Position: Write"));
        Assert.DoesNotContain(generatedTrees, t => t.Contains("// Version"));
    }

    [Fact]
    public void QueryGenerator_InGlobalNamespace_GeneratesWithoutNamespace()
    {
        var source = """
            using KeenEyes;

            public struct Position { public float X; }

            [Query]
            public partial struct GlobalQuery
            {
                public Position Position;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("partial struct GlobalQuery"));
        // Should not have namespace declaration before the struct
        Assert.Contains(generatedTrees, t =>
        {
            var lines = t.Split('\n');
            var structLine = Array.FindIndex(lines, l => l.Contains("partial struct GlobalQuery"));
            // Check that no namespace line appears before struct
            for (int i = 0; i < structLine; i++)
            {
                if (lines[i].TrimStart().StartsWith("namespace") && !lines[i].Contains("KeenEyes"))
                {
                    return false;
                }
            }
            return true;
        });
    }

    [Fact]
    public void QueryGenerator_InNestedNamespace_GeneratesCorrectNamespace()
    {
        var source = """
            using KeenEyes;

            namespace Game.Queries.Movement;

            public struct Position { public float X; }

            [Query]
            public partial struct NestedQuery
            {
                public Position Position;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("namespace Game.Queries.Movement;"));
    }

    [Fact]
    public void QueryGenerator_GeneratesAutoGeneratedHeader()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            public struct Position { public float X; }

            [Query]
            public partial struct HeaderQuery
            {
                public Position Position;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("// <auto-generated />"));
        Assert.Contains(generatedTrees, t => t.Contains("#nullable enable"));
    }

    [Fact]
    public void QueryGenerator_GeneratesXmlDocumentation()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            public struct Position { public float X; }

            [Query]
            public partial struct DocQuery
            {
                public Position Position;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t =>
            t.Contains("/// <summary>Gets the query description for matching entities.</summary>"));
        Assert.Contains(generatedTrees, t =>
            t.Contains("/// <summary>Query extensions for DocQuery.</summary>"));
    }

    [Fact]
    public void QueryGenerator_OnClass_GeneratesNoOutput()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            [Query]
            public partial class NotAStructQuery { }
            """;

        var (_, generatedTrees) = RunGenerator(source);

        // The attribute should not apply to classes, so no output
        Assert.Empty(generatedTrees);
    }

    [Fact]
    public void QueryGenerator_MultipleQueries_GeneratesSeparateFiles()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            public struct Position { public float X; }
            public struct Velocity { public float X; }

            [Query]
            public partial struct FirstQuery
            {
                public Position Position;
            }

            [Query]
            public partial struct SecondQuery
            {
                public Velocity Velocity;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("partial struct FirstQuery"));
        Assert.Contains(generatedTrees, t => t.Contains("partial struct SecondQuery"));
    }

    [Fact]
    public void QueryGenerator_ExtensionMethodUsesFullyQualifiedName()
    {
        var source = """
            using KeenEyes;

            namespace Game.Queries;

            public struct Position { public float X; }

            [Query]
            public partial struct QualifiedQuery
            {
                public Position Position;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("Game.Queries.QualifiedQuery _)"));
        Assert.Contains(generatedTrees, t => t.Contains("Game.Queries.QualifiedQuery.CreateDescription()"));
    }

    [Fact]
    public void QueryGenerator_MixedAccessTypes_TracksEachCorrectly()
    {
        var source = """
            using KeenEyes;

            namespace TestApp;

            public struct Position { public float X; }
            public struct Velocity { public float X; }
            public struct Player { }
            public struct Enemy { }
            public struct Sprite { public int Id; }

            [Query]
            public partial struct MixedQuery
            {
                public Position Position;
                [With] public Player Player;
                [Without] public Enemy Enemy;
                [Optional] public Sprite Sprite;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("// Position: Write"));
        Assert.Contains(generatedTrees, t => t.Contains("// Player: With"));
        Assert.Contains(generatedTrees, t => t.Contains("// Enemy: Without"));
        // Optional doesn't add comment
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
        var generator = new QueryGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(markerGenerator, generator);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        var runResult = driver.GetRunResult();
        // Only get the trees generated by QueryGenerator (skip MarkerAttributesGenerator output)
        var generatorResult = runResult.Results.FirstOrDefault(r => r.Generator.GetType() == typeof(QueryGenerator));
        var generatedSources = generatorResult.GeneratedSources.IsDefault
            ? []
            : generatorResult.GeneratedSources.Select(s => s.SourceText.ToString()).ToList();

        return (diagnostics, generatedSources);
    }
}
