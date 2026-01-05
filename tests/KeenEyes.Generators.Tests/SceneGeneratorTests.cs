using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace KeenEyes.Generators.Tests;

public class SceneGeneratorTests
{
    #region Basic Scene Generation

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
    public void SceneGenerator_WithValidScene_GeneratesSpawnMethod()
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
        Assert.Contains(generatedTrees, t => t.Contains("SpawnMainMenu"));
    }

    [Fact]
    public void SceneGenerator_SpawnMethodReturnsEntity()
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
                        "id": "root",
                        "name": "Root",
                        "components": { "Position": { "X": 0, "Y": 0 } }
                    }
                ]
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source, [("TestScene.kescene", sceneJson)]);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("global::KeenEyes.Entity SpawnTestScene"));
        Assert.Contains(generatedTrees, t => t.Contains("return root;"));
    }

    [Fact]
    public void SceneGenerator_WithMultipleScenes_GeneratesAllSpawnMethods()
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
        Assert.Contains(generatedTrees, t => t.Contains("SpawnLevel1"));
        Assert.Contains(generatedTrees, t => t.Contains("SpawnLevel2"));
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
        Assert.Contains(generatedTrees, t => t.Contains("SpawnMyLevel"));
    }

    #endregion

    #region Prefab Support

    [Fact]
    public void SceneGenerator_WithPrefabFile_GeneratesSpawnMethod()
    {
        var source = """
            namespace TestApp;

            public struct Position { public float X, Y; }
            public struct Health { public int Current, Max; }
            """;

        var prefabJson = """
            {
                "name": "Player",
                "root": {
                    "id": "player",
                    "name": "Player",
                    "components": {
                        "Position": { "X": 0, "Y": 0 },
                        "Health": { "Current": 100, "Max": 100 }
                    }
                }
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source, [("Player.keprefab", prefabJson)]);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("SpawnPlayer"));
        Assert.Contains(generatedTrees, t => t.Contains("global::KeenEyes.Entity SpawnPlayer"));
    }

    [Fact]
    public void SceneGenerator_WithMixedAssets_GeneratesAllSpawnMethods()
    {
        var source = """
            namespace TestApp;

            public struct Position { public float X, Y; }
            """;

        var sceneJson = """
            {
                "name": "MainLevel",
                "entities": [
                    {
                        "id": "root",
                        "name": "Root",
                        "components": { "Position": { "X": 0, "Y": 0 } }
                    }
                ]
            }
            """;

        var prefabJson = """
            {
                "name": "Enemy",
                "root": {
                    "id": "enemy",
                    "name": "Enemy",
                    "components": { "Position": { "X": 0, "Y": 0 } }
                }
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source,
            [("MainLevel.kescene", sceneJson), ("Enemy.keprefab", prefabJson)]);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("SpawnMainLevel"));
        Assert.Contains(generatedTrees, t => t.Contains("SpawnEnemy"));
        // All property should include both
        Assert.Contains(generatedTrees, t => t.Contains("\"MainLevel\"") && t.Contains("\"Enemy\""));
    }

    [Fact]
    public void SceneGenerator_WithPrefabHierarchy_FlattensProperly()
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
        Assert.Contains(generatedTrees, t => t.Contains("SpawnTank"));
        // Check that both entities are spawned
        Assert.Contains(generatedTrees, t => t.Contains("Spawn(\"Hull\")"));
        Assert.Contains(generatedTrees, t => t.Contains("Spawn(\"Turret\")"));
    }

    #endregion

    #region Override Parameters

    [Fact]
    public void SceneGenerator_WithOverridableFields_GeneratesOptionalParameters()
    {
        var source = """
            namespace TestApp;

            public struct Transform
            {
                public System.Numerics.Vector3 Position;
            }
            """;

        var prefabJson = """
            {
                "name": "Spawner",
                "root": {
                    "id": "spawner",
                    "name": "Spawner",
                    "components": {
                        "Transform": { "Position": { "X": 0, "Y": 0, "Z": 0 } }
                    }
                },
                "overridableFields": ["Transform.Position"]
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source, [("Spawner.keprefab", prefabJson)]);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        // Check for optional parameter
        Assert.Contains(generatedTrees, t => t.Contains("Vector3? transformPosition = null"));
        // Check for override application
        Assert.Contains(generatedTrees, t => t.Contains("transformPosition ??"));
    }

    [Fact]
    public void SceneGenerator_WithIntOverridableField_InfersCorrectType()
    {
        var source = """
            namespace TestApp;

            public struct Health
            {
                public int Current;
                public int Max;
            }
            """;

        var prefabJson = """
            {
                "name": "Entity",
                "root": {
                    "id": "entity",
                    "name": "Entity",
                    "components": {
                        "Health": { "Current": 100, "Max": 100 }
                    }
                },
                "overridableFields": ["Health.Current"]
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source, [("Entity.keprefab", prefabJson)]);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        // Check for int? parameter
        Assert.Contains(generatedTrees, t => t.Contains("int? healthCurrent = null"));
    }

    #endregion

    #region Diagnostics

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
    public void SceneGenerator_WithBaseField_ReportsInfoDiagnostic()
    {
        var source = """
            namespace TestApp;

            public struct Position { public float X, Y; }
            """;

        var prefabJson = """
            {
                "name": "ExtendedEnemy",
                "base": "BaseEnemy",
                "root": {
                    "id": "enemy",
                    "name": "ExtendedEnemy",
                    "components": { "Position": { "X": 0, "Y": 0 } }
                }
            }
            """;

        var (diagnostics, _) = RunGenerator(source, [("ExtendedEnemy.keprefab", prefabJson)]);

        // KEEN064 is an Info diagnostic, not error
        Assert.Contains(diagnostics, d => d.Id == "KEEN064" && d.Severity == DiagnosticSeverity.Info);
    }

    [Fact]
    public void SceneGenerator_WithInvalidOverrideFieldPath_ReportsDiagnostic()
    {
        var source = """
            namespace TestApp;

            public struct Position { public float X, Y; }
            """;

        var prefabJson = """
            {
                "name": "Entity",
                "root": {
                    "id": "entity",
                    "name": "Entity",
                    "components": { "Position": { "X": 0, "Y": 0 } }
                },
                "overridableFields": ["InvalidPath"]
            }
            """;

        var (diagnostics, _) = RunGenerator(source, [("Entity.keprefab", prefabJson)]);

        Assert.Contains(diagnostics, d => d.Id == "KEEN065");
    }

    [Fact]
    public void SceneGenerator_WithOverrideFieldMissingComponent_ReportsWarning()
    {
        var source = """
            namespace TestApp;

            public struct Position { public float X, Y; }
            """;

        var prefabJson = """
            {
                "name": "Entity",
                "root": {
                    "id": "entity",
                    "name": "Entity",
                    "components": { "Position": { "X": 0, "Y": 0 } }
                },
                "overridableFields": ["Health.Current"]
            }
            """;

        var (diagnostics, _) = RunGenerator(source, [("Entity.keprefab", prefabJson)]);

        Assert.Contains(diagnostics, d => d.Id == "KEEN066" && d.Severity == DiagnosticSeverity.Warning);
    }

    #endregion

    #region Helper Methods

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

        // Create AdditionalText instances for each asset file
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

    #endregion
}
