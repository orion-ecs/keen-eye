using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace KeenEyes.Generators.Tests;

public class SceneGeneratorTests
{
    [Fact]
    public void SceneGenerator_WithNoSceneFiles_GeneratesNoOutput()
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
        Assert.DoesNotContain(generatedTrees, t => t.Contains("class Scenes"));
    }

    [Fact]
    public void SceneGenerator_WithValidScene_GeneratesLoadMethod()
    {
        var source = """
            namespace TestApp;

            public struct Position
            {
                public float X;
                public float Y;
            }
            """;

        var sceneJson = """
            {
                "name": "MainMenu",
                "entities": [
                    {
                        "id": "player",
                        "name": "Player",
                        "components": {
                            "Position": { "X": 0, "Y": 0 }
                        }
                    }
                ]
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source, [("scenes/MainMenu.kescene", sceneJson)]);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("class Scenes"));
        Assert.Contains(generatedTrees, t => t.Contains("LoadMainMenu"));
    }

    [Fact]
    public void SceneGenerator_WithMultipleScenes_GeneratesAllLoadMethods()
    {
        var source = """
            namespace TestApp;

            public struct Position
            {
                public float X;
                public float Y;
            }
            """;

        var scene1 = """
            {
                "name": "Level1",
                "entities": []
            }
            """;

        var scene2 = """
            {
                "name": "Level2",
                "entities": []
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source,
            [("scenes/Level1.kescene", scene1), ("scenes/Level2.kescene", scene2)]);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("LoadLevel1"));
        Assert.Contains(generatedTrees, t => t.Contains("LoadLevel2"));
    }

    [Fact]
    public void SceneGenerator_WithEntityHierarchy_GeneratesSetParent()
    {
        var source = """
            namespace TestApp;

            public struct Position { public float X, Y; }
            """;

        var sceneJson = """
            {
                "name": "TestScene",
                "entities": [
                    {
                        "id": "parent",
                        "name": "Parent",
                        "components": { "Position": { "X": 0, "Y": 0 } }
                    },
                    {
                        "id": "child",
                        "name": "Child",
                        "parent": "parent",
                        "components": { "Position": { "X": 1, "Y": 1 } }
                    }
                ]
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source, [("TestScene.kescene", sceneJson)]);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("SetParent"));
    }

    [Fact]
    public void SceneGenerator_WithInvalidJson_ReportsDiagnostic()
    {
        var source = """
            namespace TestApp;

            public struct Position { public float X, Y; }
            """;

        var invalidJson = "{ not valid json }";

        var (diagnostics, _) = RunGenerator(source, [("Invalid.kescene", invalidJson)]);

        Assert.Contains(diagnostics, d => d.Id == "KEEN062");
    }

    [Fact]
    public void SceneGenerator_WithMissingParent_ReportsDiagnostic()
    {
        var source = """
            namespace TestApp;

            public struct Position { public float X, Y; }
            """;

        var sceneJson = """
            {
                "name": "TestScene",
                "entities": [
                    {
                        "id": "child",
                        "name": "Child",
                        "parent": "nonexistent",
                        "components": { "Position": { "X": 0, "Y": 0 } }
                    }
                ]
            }
            """;

        var (diagnostics, _) = RunGenerator(source, [("TestScene.kescene", sceneJson)]);

        Assert.Contains(diagnostics, d => d.Id == "KEEN061");
    }

    [Fact]
    public void SceneGenerator_WithDuplicateEntityId_ReportsDiagnostic()
    {
        var source = """
            namespace TestApp;

            public struct Position { public float X, Y; }
            """;

        var sceneJson = """
            {
                "name": "TestScene",
                "entities": [
                    {
                        "id": "duplicate",
                        "name": "First",
                        "components": { "Position": { "X": 0, "Y": 0 } }
                    },
                    {
                        "id": "duplicate",
                        "name": "Second",
                        "components": { "Position": { "X": 1, "Y": 1 } }
                    }
                ]
            }
            """;

        var (diagnostics, _) = RunGenerator(source, [("TestScene.kescene", sceneJson)]);

        Assert.Contains(diagnostics, d => d.Id == "KEEN063");
    }

    [Fact]
    public void SceneGenerator_WithVector3Component_GeneratesCorrectInitializer()
    {
        var source = """
            namespace TestApp;

            public struct Transform
            {
                public System.Numerics.Vector3 Position;
            }
            """;

        var sceneJson = """
            {
                "name": "Vector3Scene",
                "entities": [
                    {
                        "id": "entity1",
                        "name": "Entity1",
                        "components": {
                            "Transform": {
                                "Position": { "X": 1.5, "Y": 2.5, "Z": 3.5 }
                            }
                        }
                    }
                ]
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source, [("Vector3Scene.kescene", sceneJson)]);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("Vector3(1.5f, 2.5f, 3.5f)"));
    }

    [Fact]
    public void SceneGenerator_GeneratesAllProperty()
    {
        var source = """
            namespace TestApp;

            public struct Position { public float X, Y; }
            """;

        var sceneJson = """
            {
                "name": "TestScene",
                "entities": []
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source, [("TestScene.kescene", sceneJson)]);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("public static IReadOnlyList<string> All"));
        Assert.Contains(generatedTrees, t => t.Contains("\"TestScene\""));
    }

    [Fact]
    public void SceneGenerator_UsesFileNameWhenNoNameProperty()
    {
        var source = """
            namespace TestApp;

            public struct Position { public float X, Y; }
            """;

        var sceneJson = """
            {
                "entities": []
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source, [("MyLevel.kescene", sceneJson)]);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("LoadMyLevel"));
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

        // Add runtime assembly references
        var runtimeDir = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
        references.Add(MetadataReference.CreateFromFile(Path.Join(runtimeDir, "System.Runtime.dll")));
        references.Add(MetadataReference.CreateFromFile(Path.Join(runtimeDir, "netstandard.dll")));

        var compilation = CSharpCompilation.Create(
            "TestApp",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // Create AdditionalText instances for each scene file
        var additionalTexts = additionalFiles
            .Select(f => new InMemoryAdditionalText(f.Path, f.Content))
            .Cast<AdditionalText>()
            .ToList();

        var generator = new SceneGenerator().AsSourceGenerator();
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

    /// <summary>
    /// Helper class to provide in-memory additional files for source generator testing.
    /// </summary>
    private sealed class InMemoryAdditionalText(string path, string content) : AdditionalText
    {
        private readonly SourceText sourceText = SourceText.From(content);

        public override string Path { get; } = path;

        public override SourceText? GetText(CancellationToken cancellationToken = default) => sourceText;
    }
}
