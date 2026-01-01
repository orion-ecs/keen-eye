using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace KeenEyes.Generators.Tests;

public class WorldConfigGeneratorTests
{
    [Fact]
    public void WorldConfigGenerator_WithNoWorldFiles_GeneratesNoOutput()
    {
        var source = """
            namespace TestApp;

            public struct Gravity
            {
                public float Value;
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source, []);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(generatedTrees, t => t.Contains("class WorldConfigs"));
    }

    [Fact]
    public void WorldConfigGenerator_WithValidConfig_GeneratesConfigureMethod()
    {
        var source = """
            namespace TestApp;

            public struct Gravity
            {
                public float Value;
            }
            """;

        var worldJson = """
            {
                "name": "Default",
                "settings": {
                    "fixedTimeStep": 0.02,
                    "maxDeltaTime": 0.1
                }
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source, [("worlds/Default.keworld", worldJson)]);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("class WorldConfigs"));
        Assert.Contains(generatedTrees, t => t.Contains("ConfigureDefault"));
    }

    [Fact]
    public void WorldConfigGenerator_WithSingletons_GeneratesWithSingleton()
    {
        var source = """
            namespace TestApp;

            public struct Gravity
            {
                public System.Numerics.Vector3 Value;
            }
            """;

        var worldJson = """
            {
                "name": "Physics",
                "singletons": {
                    "Gravity": {
                        "Value": { "X": 0, "Y": -9.81, "Z": 0 }
                    }
                }
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source, [("Physics.keworld", worldJson)]);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("WithSingleton"));
        Assert.Contains(generatedTrees, t => t.Contains("Gravity"));
    }

    [Fact]
    public void WorldConfigGenerator_WithPlugins_GeneratesWithPlugin()
    {
        var source = """
            namespace TestApp;

            public class PhysicsPlugin { }
            public class AudioPlugin { }
            """;

        var worldJson = """
            {
                "name": "FullGame",
                "plugins": ["PhysicsPlugin", "AudioPlugin"]
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source, [("FullGame.keworld", worldJson)]);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("WithPlugin<global::PhysicsPlugin>"));
        Assert.Contains(generatedTrees, t => t.Contains("WithPlugin<global::AudioPlugin>"));
    }

    [Fact]
    public void WorldConfigGenerator_WithSystems_GeneratesWithSystem()
    {
        var source = """
            namespace TestApp;

            public class MovementSystem { }
            public class RenderSystem { }
            """;

        var worldJson = """
            {
                "name": "GameWorld",
                "systems": [
                    { "type": "MovementSystem", "phase": "Update", "order": 0 },
                    { "type": "RenderSystem", "phase": "Render", "order": 10 }
                ]
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source, [("GameWorld.keworld", worldJson)]);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("WithSystem<global::MovementSystem>"));
        Assert.Contains(generatedTrees, t => t.Contains("WithSystem<global::RenderSystem>"));
        Assert.Contains(generatedTrees, t => t.Contains("SystemPhase.Update"));
        Assert.Contains(generatedTrees, t => t.Contains("SystemPhase.Render"));
    }

    [Fact]
    public void WorldConfigGenerator_WithInvalidJson_ReportsDiagnostic()
    {
        var source = """
            namespace TestApp;

            public struct Gravity { public float Value; }
            """;

        var invalidJson = "{ not valid json }";

        var (diagnostics, _) = RunGenerator(source, [("Invalid.keworld", invalidJson)]);

        Assert.Contains(diagnostics, d => d.Id == "KEEN081");
    }

    [Fact]
    public void WorldConfigGenerator_WithMissingPluginType_ReportsDiagnostic()
    {
        var source = """
            namespace TestApp;

            public class ExistingPlugin { }
            """;

        var worldJson = """
            {
                "name": "Test",
                "plugins": ["NonExistentPlugin"]
            }
            """;

        var (diagnostics, _) = RunGenerator(source, [("Test.keworld", worldJson)]);

        Assert.Contains(diagnostics, d => d.Id == "KEEN082");
    }

    [Fact]
    public void WorldConfigGenerator_WithMissingSystemType_ReportsDiagnostic()
    {
        var source = """
            namespace TestApp;

            public class ExistingSystem { }
            """;

        var worldJson = """
            {
                "name": "Test",
                "systems": [
                    { "type": "NonExistentSystem", "phase": "Update" }
                ]
            }
            """;

        var (diagnostics, _) = RunGenerator(source, [("Test.keworld", worldJson)]);

        Assert.Contains(diagnostics, d => d.Id == "KEEN083");
    }

    [Fact]
    public void WorldConfigGenerator_GeneratesApplyMethod()
    {
        var source = """
            namespace TestApp;

            public struct GameTime { public float Delta; }
            """;

        var worldJson = """
            {
                "name": "Runtime",
                "singletons": {
                    "GameTime": { "Delta": 0 }
                }
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source, [("Runtime.keworld", worldJson)]);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("ApplyRuntime"));
        Assert.Contains(generatedTrees, t => t.Contains("SetSingleton"));
    }

    [Fact]
    public void WorldConfigGenerator_GeneratesAllProperty()
    {
        var source = """
            namespace TestApp;

            public struct Setting { public int Value; }
            """;

        var worldJson = """
            {
                "name": "TestConfig"
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source, [("TestConfig.keworld", worldJson)]);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("public static IReadOnlyList<string> All"));
        Assert.Contains(generatedTrees, t => t.Contains("\"TestConfig\""));
    }

    [Fact]
    public void WorldConfigGenerator_GeneratesBuilderExtensionMethod()
    {
        var source = """
            namespace TestApp;

            public struct Gravity { public float Value; }
            """;

        var worldJson = """
            {
                "name": "Physics"
            }
            """;

        var (diagnostics, generatedTrees) = RunGenerator(source, [("Physics.keworld", worldJson)]);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("this global::KeenEyes.WorldBuilder builder"));
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

        var generator = new WorldConfigGenerator().AsSourceGenerator();
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
