using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace KeenEyes.Generators.Tests;

/// <summary>
/// Tests for the AssetConstantsGenerator.
/// </summary>
public class AssetConstantsGeneratorTests
{
    [Fact]
    public void Generator_WithGenerateDisabled_GeneratesNoOutput()
    {
        var (_, generatedTrees) = RunGenerator(
            generateAssetConstants: false,
            assetFiles: [("Assets/player.png", "fake content")]);

        Assert.Empty(generatedTrees);
    }

    [Fact]
    public void Generator_WithNoAssetFiles_GeneratesNoOutput()
    {
        var (_, generatedTrees) = RunGenerator(
            generateAssetConstants: true,
            assetFiles: []);

        Assert.Empty(generatedTrees);
    }

    [Fact]
    public void Generator_WithSingleAsset_GeneratesConstant()
    {
        var (diagnostics, generatedTrees) = RunGenerator(
            generateAssetConstants: true,
            assetFiles: [("C:/Project/Assets/player.png", "")],
            rootPath: "C:/Project/Assets");

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Single(generatedTrees);
        Assert.Contains(generatedTrees, t => t.Contains("public const string Player"));
    }

    [Fact]
    public void Generator_WithNestedDirectories_GeneratesNestedClasses()
    {
        var (diagnostics, generatedTrees) = RunGenerator(
            generateAssetConstants: true,
            assetFiles:
            [
                ("C:/Project/Assets/textures/player.png", ""),
                ("C:/Project/Assets/textures/enemy.png", ""),
                ("C:/Project/Assets/audio/music.ogg", "")
            ],
            rootPath: "C:/Project/Assets");

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Single(generatedTrees);

        var source = generatedTrees[0];
        Assert.Contains("public static partial class Textures", source);
        Assert.Contains("public static partial class Audio", source);
        Assert.Contains("public const string Player", source);
        Assert.Contains("public const string Enemy", source);
        Assert.Contains("public const string Music", source);
    }

    [Fact]
    public void Generator_WithCustomNamespace_UsesNamespace()
    {
        var (diagnostics, generatedTrees) = RunGenerator(
            generateAssetConstants: true,
            assetFiles: [("C:/Project/Assets/player.png", "")],
            ns: "MyGame.Generated",
            rootPath: "C:/Project/Assets");

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("namespace MyGame.Generated;"));
    }

    [Fact]
    public void Generator_WithCustomClassName_UsesClassName()
    {
        var (diagnostics, generatedTrees) = RunGenerator(
            generateAssetConstants: true,
            assetFiles: [("C:/Project/Assets/player.png", "")],
            className: "GameAssets",
            rootPath: "C:/Project/Assets");

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(generatedTrees, t => t.Contains("public static partial class GameAssets"));
    }

    [Fact]
    public void Generator_SanitizesIdentifiers()
    {
        var (diagnostics, generatedTrees) = RunGenerator(
            generateAssetConstants: true,
            assetFiles:
            [
                ("C:/Project/Assets/player-sprite.png", ""),
                ("C:/Project/Assets/jump sound.wav", ""),
                ("C:/Project/Assets/01_intro.ogg", "")
            ],
            rootPath: "C:/Project/Assets");

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        var source = generatedTrees[0];

        // Dashes should become PascalCase
        Assert.Contains("PlayerSprite", source);
        // Spaces should become PascalCase
        Assert.Contains("JumpSound", source);
        // Leading digits should be prefixed with underscore
        Assert.Contains("_01Intro", source);
    }

    [Fact]
    public void Generator_HandlesMultipleFormats()
    {
        var (diagnostics, generatedTrees) = RunGenerator(
            generateAssetConstants: true,
            assetFiles:
            [
                ("C:/Project/Assets/texture.png", ""),
                ("C:/Project/Assets/texture.jpg", ""),
                ("C:/Project/Assets/sound.wav", ""),
                ("C:/Project/Assets/sound.ogg", ""),
                ("C:/Project/Assets/font.ttf", ""),
                ("C:/Project/Assets/model.glb", "")
            ],
            rootPath: "C:/Project/Assets");

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Single(generatedTrees);
    }

    [Fact]
    public void Generator_IgnoresUnknownExtensions()
    {
        var (_, generatedTrees) = RunGenerator(
            generateAssetConstants: true,
            assetFiles:
            [
                ("C:/Project/Assets/readme.md", ""),
                ("C:/Project/Assets/script.cs", ""),
                ("C:/Project/Assets/player.png", "")
            ],
            rootPath: "C:/Project/Assets");

        var source = generatedTrees.FirstOrDefault() ?? "";
        Assert.DoesNotContain("Readme", source);
        Assert.DoesNotContain("Script", source);
        Assert.Contains("Player", source);
    }

    [Fact]
    public void Generator_GeneratesAutoGeneratedHeader()
    {
        var (_, generatedTrees) = RunGenerator(
            generateAssetConstants: true,
            assetFiles: [("C:/Project/Assets/player.png", "")],
            rootPath: "C:/Project/Assets");

        var source = generatedTrees[0];
        Assert.Contains("// <auto-generated />", source);
        Assert.Contains("#nullable enable", source);
    }

    [Fact]
    public void Generator_GeneratesXmlDocumentation()
    {
        var (_, generatedTrees) = RunGenerator(
            generateAssetConstants: true,
            assetFiles: [("C:/Project/Assets/textures/player.png", "")],
            rootPath: "C:/Project/Assets");

        var source = generatedTrees[0];
        Assert.Contains("/// <summary>", source);
        Assert.Contains("Asset paths for the textures directory", source);
        Assert.Contains("Path to player.png", source);
    }

    [Fact]
    public void Generator_HandlesPathsWithForwardSlashes()
    {
        var (diagnostics, generatedTrees) = RunGenerator(
            generateAssetConstants: true,
            assetFiles: [("C:/Project/Assets/textures/player.png", "")],
            rootPath: "C:/Project/Assets");

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        var source = generatedTrees[0];
        // Paths should use forward slashes
        Assert.Contains("textures/player.png", source);
    }

    [Fact]
    public void Generator_HandlesDuplicateNames()
    {
        var (diagnostics, generatedTrees) = RunGenerator(
            generateAssetConstants: true,
            assetFiles:
            [
                ("C:/Project/Assets/player.png", ""),
                ("C:/Project/Assets/player.jpg", "")
            ],
            rootPath: "C:/Project/Assets");

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        var source = generatedTrees[0];
        // Both should be present with unique names
        Assert.Contains("Player", source);
        Assert.Contains("Player_1", source);
    }

    [Fact]
    public void Generator_EscapesStringContent()
    {
        var (diagnostics, generatedTrees) = RunGenerator(
            generateAssetConstants: true,
            assetFiles: [("C:/Project/Assets/folder with spaces/file.png", "")],
            rootPath: "C:/Project/Assets");

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        var source = generatedTrees[0];
        // Path should be properly escaped in string literal
        Assert.Contains("\"", source);
    }

    private static (IReadOnlyList<Diagnostic> Diagnostics, IReadOnlyList<string> GeneratedSources) RunGenerator(
        bool generateAssetConstants,
        (string Path, string Content)[] assetFiles,
        string? ns = null,
        string? className = null,
        string? rootPath = null)
    {
        var source = """
            namespace TestApp;

            public class Placeholder { }
            """;

        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
        };

        var runtimeDir = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
        references.Add(MetadataReference.CreateFromFile(System.IO.Path.Combine(runtimeDir, "System.Runtime.dll")));

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // Create analyzer config options
        var globalOptions = new Dictionary<string, string>
        {
            ["build_property.GenerateAssetConstants"] = generateAssetConstants ? "true" : "false"
        };

        if (!string.IsNullOrEmpty(ns))
        {
            globalOptions["build_property.AssetConstantsNamespace"] = ns;
        }

        if (!string.IsNullOrEmpty(className))
        {
            globalOptions["build_property.AssetConstantsClassName"] = className;
        }

        if (!string.IsNullOrEmpty(rootPath))
        {
            globalOptions["build_property.AssetConstantsRootPath"] = rootPath;
        }

        var optionsProvider = new TestAnalyzerConfigOptionsProvider(globalOptions);

        // Create additional texts for asset files
        var additionalTexts = assetFiles
            .Select(f => new TestAdditionalText(f.Path, f.Content))
            .ToImmutableArray<AdditionalText>();

        var generator = new AssetConstantsGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [generator.AsSourceGenerator()],
            additionalTexts: additionalTexts,
            optionsProvider: optionsProvider);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        var runResult = driver.GetRunResult();
        var generatedSources = runResult.GeneratedTrees
            .Select(t => t.GetText().ToString())
            .ToList();

        return (diagnostics, generatedSources);
    }

    private sealed class TestAdditionalText(string path, string content) : AdditionalText
    {
        public override string Path { get; } = path;

        public override SourceText? GetText(CancellationToken cancellationToken = default)
            => SourceText.From(content);
    }

    private sealed class TestAnalyzerConfigOptionsProvider(Dictionary<string, string> options) : AnalyzerConfigOptionsProvider
    {
        private readonly TestGlobalOptions globalOptions = new(options);

        public override AnalyzerConfigOptions GlobalOptions => globalOptions;

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
            => TestAnalyzerConfigOptions.Empty;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
            => TestAnalyzerConfigOptions.Empty;

        private sealed class TestGlobalOptions(Dictionary<string, string> options) : AnalyzerConfigOptions
        {
            public override bool TryGetValue(string key, out string value)
                => options.TryGetValue(key, out value!);
        }

        private sealed class TestAnalyzerConfigOptions : AnalyzerConfigOptions
        {
            public static readonly TestAnalyzerConfigOptions Empty = new();

            public override bool TryGetValue(string key, out string value)
            {
                value = null!;
                return false;
            }
        }
    }
}
