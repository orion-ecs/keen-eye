using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace KeenEyes.Generators.Tests;

/// <summary>
/// Tests for the AssetPathAnalyzer diagnostic analyzer.
/// </summary>
public class AssetPathAnalyzerTests
{
    #region KEEN121: Extension Mismatch

    [Fact]
    public void LoadTexture_WithWavExtension_ReportsExtensionMismatch()
    {
        var source = """
            namespace KeenEyes.Assets;

            public class TextureAsset { }

            public class AssetManager
            {
                public T Load<T>(string path) => default!;
            }

            namespace TestApp;

            using KeenEyes.Assets;

            public class TestClass
            {
                private readonly AssetManager assetManager = new();

                public void LoadAssets()
                {
                    var texture = assetManager.Load<TextureAsset>("sounds/music.wav");
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN121");
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Contains(".wav", diagnostic.GetMessage());
        Assert.Contains("TextureAsset", diagnostic.GetMessage());
    }

    [Fact]
    public void LoadAudioClip_WithPngExtension_ReportsExtensionMismatch()
    {
        var source = """
            namespace KeenEyes.Assets;

            public class AudioClipAsset { }

            public class AssetManager
            {
                public T Load<T>(string path) => default!;
            }

            namespace TestApp;

            using KeenEyes.Assets;

            public class TestClass
            {
                private readonly AssetManager assetManager = new();

                public void LoadAssets()
                {
                    var audio = assetManager.Load<AudioClipAsset>("textures/player.png");
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN121");
        Assert.Contains(".png", diagnostic.GetMessage());
        Assert.Contains("AudioClipAsset", diagnostic.GetMessage());
    }

    [Fact]
    public void LoadFont_WithGltfExtension_ReportsExtensionMismatch()
    {
        var source = """
            namespace KeenEyes.Assets;

            public class FontAsset { }

            public class AssetManager
            {
                public T Load<T>(string path) => default!;
            }

            namespace TestApp;

            using KeenEyes.Assets;

            public class TestClass
            {
                private readonly AssetManager assetManager = new();

                public void LoadAssets()
                {
                    var font = assetManager.Load<FontAsset>("models/character.gltf");
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN121");
        Assert.Contains(".gltf", diagnostic.GetMessage());
        Assert.Contains("FontAsset", diagnostic.GetMessage());
        Assert.Contains(".ttf", diagnostic.GetMessage());
        Assert.Contains(".otf", diagnostic.GetMessage());
    }

    #endregion

    #region No False Positives - Extension Matches

    [Fact]
    public void LoadTexture_WithPngExtension_NoMismatchDiagnostic()
    {
        var source = """
            namespace KeenEyes.Assets;

            public class TextureAsset { }

            public class AssetManager
            {
                public T Load<T>(string path) => default!;
            }

            namespace TestApp;

            using KeenEyes.Assets;

            public class TestClass
            {
                private readonly AssetManager assetManager = new();

                public void LoadAssets()
                {
                    var texture = assetManager.Load<TextureAsset>("textures/player.png");
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == "KEEN121");
    }

    [Fact]
    public void LoadAudioClip_WithMp3Extension_NoMismatchDiagnostic()
    {
        var source = """
            namespace KeenEyes.Assets;

            public class AudioClipAsset { }

            public class AssetManager
            {
                public T Load<T>(string path) => default!;
            }

            namespace TestApp;

            using KeenEyes.Assets;

            public class TestClass
            {
                private readonly AssetManager assetManager = new();

                public void LoadAssets()
                {
                    var audio = assetManager.Load<AudioClipAsset>("audio/music.mp3");
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == "KEEN121");
    }

    [Fact]
    public void LoadModel_WithGlbExtension_NoMismatchDiagnostic()
    {
        var source = """
            namespace KeenEyes.Assets;

            public class ModelAsset { }

            public class AssetManager
            {
                public T Load<T>(string path) => default!;
            }

            namespace TestApp;

            using KeenEyes.Assets;

            public class TestClass
            {
                private readonly AssetManager assetManager = new();

                public void LoadAssets()
                {
                    var model = assetManager.Load<ModelAsset>("models/character.glb");
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == "KEEN121");
    }

    [Fact]
    public void LoadTexture_WithDdsExtension_NoMismatchDiagnostic()
    {
        var source = """
            namespace KeenEyes.Assets;

            public class TextureAsset { }

            public class AssetManager
            {
                public T Load<T>(string path) => default!;
            }

            namespace TestApp;

            using KeenEyes.Assets;

            public class TestClass
            {
                private readonly AssetManager assetManager = new();

                public void LoadAssets()
                {
                    var texture = assetManager.Load<TextureAsset>("textures/compressed.dds");
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == "KEEN121");
    }

    #endregion

    #region Non-AssetManager Methods - No False Positives

    [Fact]
    public void NonAssetManagerLoad_NoDiagnostic()
    {
        var source = """
            namespace TestApp;

            public class SomeLoader
            {
                public T Load<T>(string path) => default!;
            }

            public class TestClass
            {
                private readonly SomeLoader loader = new();

                public void LoadAssets()
                {
                    var data = loader.Load<string>("data.txt");
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == "KEEN120" || d.Id == "KEEN121");
    }

    [Fact]
    public void NonGenericLoad_NoDiagnostic()
    {
        var source = """
            namespace KeenEyes.Assets;

            public class AssetManager
            {
                public void Load(string path) { }
            }

            namespace TestApp;

            using KeenEyes.Assets;

            public class TestClass
            {
                private readonly AssetManager assetManager = new();

                public void LoadAssets()
                {
                    assetManager.Load("textures/player.wav");
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == "KEEN121");
    }

    #endregion

    #region LoadAsync Support

    [Fact]
    public void LoadAsync_WithWrongExtension_ReportsExtensionMismatch()
    {
        var source = """
            namespace KeenEyes.Assets;

            public class TextureAsset { }

            public class AssetManager
            {
                public System.Threading.Tasks.Task<T> LoadAsync<T>(string path) => default!;
            }

            namespace TestApp;

            using KeenEyes.Assets;

            public class TestClass
            {
                private readonly AssetManager assetManager = new();

                public async void LoadAssets()
                {
                    var texture = await assetManager.LoadAsync<TextureAsset>("audio/music.ogg");
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN121");
        Assert.Contains(".ogg", diagnostic.GetMessage());
        Assert.Contains("TextureAsset", diagnostic.GetMessage());
    }

    #endregion

    #region All Supported Types

    [Theory]
    [InlineData("TextureAsset", ".png")]
    [InlineData("TextureAsset", ".jpg")]
    [InlineData("TextureAsset", ".jpeg")]
    [InlineData("TextureAsset", ".bmp")]
    [InlineData("TextureAsset", ".dds")]
    [InlineData("AudioClipAsset", ".wav")]
    [InlineData("AudioClipAsset", ".ogg")]
    [InlineData("AudioClipAsset", ".mp3")]
    [InlineData("AudioClipAsset", ".flac")]
    [InlineData("FontAsset", ".ttf")]
    [InlineData("FontAsset", ".otf")]
    [InlineData("MeshAsset", ".gltf")]
    [InlineData("MeshAsset", ".glb")]
    [InlineData("ModelAsset", ".gltf")]
    [InlineData("ModelAsset", ".glb")]
    [InlineData("AnimationAsset", ".keanim")]
    [InlineData("SpriteAtlasAsset", ".atlas")]
    [InlineData("SpriteAtlasAsset", ".json")]
    [InlineData("RawAsset", ".bin")]
    [InlineData("RawAsset", ".dat")]
    public void ValidExtensionForType_NoMismatchDiagnostic(string assetType, string extension)
    {
        var source = $$"""
            namespace KeenEyes.Assets;

            public class {{assetType}} { }

            public class AssetManager
            {
                public T Load<T>(string path) => default!;
            }

            namespace TestApp;

            using KeenEyes.Assets;

            public class TestClass
            {
                private readonly AssetManager assetManager = new();

                public void LoadAssets()
                {
                    var asset = assetManager.Load<{{assetType}}>("test/file{{extension}}");
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == "KEEN121");
    }

    #endregion

    #region IAssetManager Interface Support

    [Fact]
    public void IAssetManagerLoad_WithWrongExtension_ReportsExtensionMismatch()
    {
        var source = """
            namespace KeenEyes.Assets;

            public class TextureAsset { }

            public interface IAssetManager
            {
                T Load<T>(string path);
            }

            namespace TestApp;

            using KeenEyes.Assets;

            public class TestClass
            {
                private readonly IAssetManager assetManager = null!;

                public void LoadAssets()
                {
                    var texture = assetManager.Load<TextureAsset>("sounds/music.wav");
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN121");
        Assert.Contains(".wav", diagnostic.GetMessage());
    }

    #endregion

    #region Multiple Arguments

    [Fact]
    public void LoadWithMultipleArgs_ValidatesFirstPathArg()
    {
        var source = """
            namespace KeenEyes.Assets;

            public class TextureAsset { }

            public class AssetManager
            {
                public T Load<T>(string path, bool cache = true) => default!;
            }

            namespace TestApp;

            using KeenEyes.Assets;

            public class TestClass
            {
                private readonly AssetManager assetManager = new();

                public void LoadAssets()
                {
                    var texture = assetManager.Load<TextureAsset>("audio/music.wav", false);
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "KEEN121");
        Assert.Contains(".wav", diagnostic.GetMessage());
    }

    #endregion

    #region Non-String-Literal Paths

    [Fact]
    public void LoadWithVariablePath_NoDiagnostic()
    {
        var source = """
            namespace KeenEyes.Assets;

            public class TextureAsset { }

            public class AssetManager
            {
                public T Load<T>(string path) => default!;
            }

            namespace TestApp;

            using KeenEyes.Assets;

            public class TestClass
            {
                private readonly AssetManager assetManager = new();

                public void LoadAssets(string texturePath)
                {
                    var texture = assetManager.Load<TextureAsset>(texturePath);
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        // No diagnostic since we can't analyze variable paths at compile time
        Assert.DoesNotContain(diagnostics, d => d.Id == "KEEN120" || d.Id == "KEEN121");
    }

    [Fact]
    public void LoadWithConcatenatedPath_NoDiagnostic()
    {
        var source = """
            namespace KeenEyes.Assets;

            public class TextureAsset { }

            public class AssetManager
            {
                public T Load<T>(string path) => default!;
            }

            namespace TestApp;

            using KeenEyes.Assets;

            public class TestClass
            {
                private readonly AssetManager assetManager = new();

                public void LoadAssets()
                {
                    var texture = assetManager.Load<TextureAsset>("textures/" + "player.png");
                }
            }
            """;

        var diagnostics = RunAnalyzer(source);

        // No diagnostic since concatenated strings aren't literal operations
        Assert.DoesNotContain(diagnostics, d => d.Id == "KEEN121");
    }

    #endregion

    #region Helper Methods

    private static IReadOnlyList<Diagnostic> RunAnalyzer(string source, params string[] additionalFilePaths)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
        };

        // Add runtime assembly references
        var runtimeDir = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
        references.Add(MetadataReference.CreateFromFile(System.IO.Path.Combine(runtimeDir, "System.Runtime.dll")));
        references.Add(MetadataReference.CreateFromFile(System.IO.Path.Combine(runtimeDir, "netstandard.dll")));
        references.Add(MetadataReference.CreateFromFile(System.IO.Path.Combine(runtimeDir, "System.Threading.Tasks.dll")));

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var additionalTexts = additionalFilePaths
            .Select(path => new InMemoryAdditionalText(path, ""))
            .Cast<AdditionalText>()
            .ToImmutableArray();

        var options = new AnalyzerOptions(additionalTexts);

        var analyzer = new AssetPathAnalyzer();
        var compilationWithAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(analyzer),
            options);

        var diagnostics = compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().Result;

        return diagnostics.ToList();
    }

    /// <summary>
    /// Simple in-memory AdditionalText implementation for testing.
    /// </summary>
    private sealed class InMemoryAdditionalText(string path, string content) : AdditionalText
    {
        public override string Path { get; } = path;

        public override SourceText GetText(CancellationToken cancellationToken = default)
        {
            return SourceText.From(content);
        }
    }

    #endregion
}
