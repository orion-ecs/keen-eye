using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace KeenEyes.Generators.Tests;

public class PrefabGeneratorTests
{
    [Fact]
    public void PrefabGenerator_WithNoPrefabFiles_GeneratesNoOutput()
    {
        var source = """
            namespace TestApp;

            public struct Position
            {
                public float X;
                public float Y;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source, []);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(generatedTrees, t => t.Contains("class Prefabs"));
    }

    [Fact]
    public void PrefabGenerator_WithValidPrefab_GeneratesSpawnMethod()
    {
        var source = """
            namespace TestApp;

            public struct Position
            {
                public float X;
                public float Y;
            }
            """;

        var prefabJson = """
            {
                "name": "Player",
                "root": {
                    "id": "root",
                    "name": "Player",
                    "components": {
                        "Position": { "X": 0, "Y": 0 }
                    }
                }
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source, [("prefabs/Player.keprefab", prefabJson)]);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("class Prefabs"));
        Assert.Contains(generatedTrees, t => t.Contains("SpawnPlayer"));
    }

    [Fact]
    public void PrefabGenerator_WithOverridableFields_GeneratesOptionalParameters()
    {
        var source = """
            namespace TestApp;

            public struct Position
            {
                public float X;
                public float Y;
            }

            public struct Health
            {
                public int Current;
                public int Max;
            }
            """;

        var prefabJson = """
            {
                "name": "Enemy",
                "root": {
                    "id": "root",
                    "name": "Enemy",
                    "components": {
                        "Position": { "X": 0, "Y": 0 },
                        "Health": { "Current": 100, "Max": 100 }
                    }
                },
                "overridableFields": ["Position.X", "Health.Current"]
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source, [("Enemy.keprefab", prefabJson)]);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("SpawnEnemy"));
        // Should have optional parameters
        Assert.Contains(generatedTrees, t => t.Contains("= null"));
    }

    [Fact]
    public void PrefabGenerator_WithNestedChildren_GeneratesHierarchy()
    {
        var source = """
            namespace TestApp;

            public struct Position { public float X, Y; }
            """;

        var prefabJson = """
            {
                "name": "Tank",
                "root": {
                    "id": "hull",
                    "name": "Hull",
                    "components": { "Position": { "X": 0, "Y": 0 } }
                },
                "children": [
                    {
                        "id": "turret",
                        "name": "Turret",
                        "components": { "Position": { "X": 0, "Y": 1 } }
                    }
                ]
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source, [("Tank.keprefab", prefabJson)]);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("Hull"));
        Assert.Contains(generatedTrees, t => t.Contains("Turret"));
        Assert.Contains(generatedTrees, t => t.Contains("SetParent"));
    }

    [Fact]
    public void PrefabGenerator_WithInvalidJson_ReportsDiagnostic()
    {
        var source = """
            namespace TestApp;

            public struct Position { public float X, Y; }
            """;

        var invalidJson = "{ not valid json }";

        var (diagnostics, _) = RunGenerator(source, [("Invalid.keprefab", invalidJson)]);

        Assert.Contains(diagnostics, d => d.Id == "KEEN071");
    }

    [Fact]
    public void PrefabGenerator_WithInvalidOverrideFieldPath_ReportsDiagnostic()
    {
        var source = """
            namespace TestApp;

            public struct Position { public float X, Y; }
            """;

        var prefabJson = """
            {
                "name": "Test",
                "root": {
                    "id": "root",
                    "name": "Root",
                    "components": { "Position": { "X": 0, "Y": 0 } }
                },
                "overridableFields": ["InvalidPathWithoutDot"]
            }
            """;

        var (diagnostics, _) = RunGenerator(source, [("Test.keprefab", prefabJson)]);

        Assert.Contains(diagnostics, d => d.Id == "KEEN072");
    }

    [Fact]
    public void PrefabGenerator_WithOverrideFieldForMissingComponent_ReportsWarning()
    {
        var source = """
            namespace TestApp;

            public struct Position { public float X, Y; }
            """;

        var prefabJson = """
            {
                "name": "Test",
                "root": {
                    "id": "root",
                    "name": "Root",
                    "components": { "Position": { "X": 0, "Y": 0 } }
                },
                "overridableFields": ["NonExistent.Field"]
            }
            """;

        var (diagnostics, _) = RunGenerator(source, [("Test.keprefab", prefabJson)]);

        Assert.Contains(diagnostics, d => d.Id == "KEEN073");
    }

    [Fact]
    public void PrefabGenerator_GeneratesAllProperty()
    {
        var source = """
            namespace TestApp;

            public struct Position { public float X, Y; }
            """;

        var prefabJson = """
            {
                "name": "TestPrefab",
                "root": {
                    "id": "root",
                    "name": "Root",
                    "components": {}
                }
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source, [("TestPrefab.keprefab", prefabJson)]);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("public static IReadOnlyList<string> All"));
        Assert.Contains(generatedTrees, t => t.Contains("\"TestPrefab\""));
    }

    [Fact]
    public void PrefabGenerator_ReturnsRootEntity()
    {
        var source = """
            namespace TestApp;

            public struct Position { public float X, Y; }
            """;

        var prefabJson = """
            {
                "name": "Simple",
                "root": {
                    "id": "root",
                    "name": "Root",
                    "components": { "Position": { "X": 0, "Y": 0 } }
                }
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source, [("Simple.keprefab", prefabJson)]);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("return "));
        Assert.Contains(generatedTrees, t => t.Contains("global::KeenEyes.Entity"));
    }

    private static (IReadOnlyList<Diagnostic> Diagnostics, IReadOnlyList<string> GeneratedSources) RunGenerator(
        string source,
        (string Path, string Content)[] additionalFiles)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Numerics.Vector3).Assembly.Location),
        };

        var runtimeDir = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
        references.Add(MetadataReference.CreateFromFile(Path.Join(runtimeDir, "System.Runtime.dll")));
        references.Add(MetadataReference.CreateFromFile(Path.Join(runtimeDir, "netstandard.dll")));

        var compilation = CSharpCompilation.Create(
            "TestApp",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var additionalTexts = additionalFiles
            .Select(f => new InMemoryAdditionalText(f.Path, f.Content))
            .Cast<AdditionalText>()
            .ToList();

        var generator = new PrefabGenerator().AsSourceGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [generator],
            additionalTexts: additionalTexts);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var _);

        var runResult = driver.GetRunResult();
        var generatedSources = runResult.GeneratedTrees
            .Select(t => t.GetText().ToString())
            .ToList();

        var allDiagnostics = runResult.Diagnostics.ToList();

        return (allDiagnostics, generatedSources);
    }

    private sealed class InMemoryAdditionalText(string path, string content) : AdditionalText
    {
        private readonly SourceText sourceText = SourceText.From(content);

        public override string Path { get; } = path;

        public override SourceText? GetText(CancellationToken cancellationToken = default) => sourceText;
    }
}
