using KeenEyes.Assets;

namespace KeenEyes.Localization.Tests;

/// <summary>
/// Tests that preloading locale assets does not poison the asset cache so that later
/// typed loads of the same path still resolve (regression guard for #1194).
/// </summary>
public sealed class LocalizationAssetPreloadTests : IDisposable
{
    private readonly string rootPath;
    private readonly World world;
    private readonly AssetManager assetManager;

    public LocalizationAssetPreloadTests()
    {
        rootPath = Path.Combine(Path.GetTempPath(), $"keen_eye_loc_preload_{Guid.NewGuid():N}");
        Directory.CreateDirectory(rootPath);
        File.WriteAllBytes(Path.Combine(rootPath, "greeting.dat"), [1, 2, 3, 4, 5]);

        world = new World();
        assetManager = new AssetManager(new AssetsConfig { RootPath = rootPath });
        assetManager.RegisterLoader(new StubAssetLoader());

        world.SetExtension(assetManager, owned: false);
        world.SetExtension<ILocalizedAssetResolver>(new StubResolver("greeting.dat"), owned: false);
    }

    public void Dispose()
    {
        assetManager.Dispose();
        world.Dispose();
        if (Directory.Exists(rootPath))
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public async Task PreloadLocaleAssetsAsync_ThenTypedLoad_ResolvesTypedAsset()
    {
        using var manager = new LocalizationManager(LocalizationConfig.Default, world);

        await manager.PreloadLocaleAssetsAsync(
            Locale.EnglishUS,
            ["greeting"],
            TestContext.Current.CancellationToken);

        // The subsequent typed load of the same path must produce a real StubAsset.
        // Before the fix, preloading as RawAsset poisoned the path-keyed cache, so this
        // handle reported IsLoaded == true but Asset == null (RawAsset cast to StubAsset).
        var handle = assetManager.Load<StubAsset>("greeting.dat");

        handle.IsLoaded.ShouldBeTrue();
        handle.Asset.ShouldNotBeNull();
        handle.Asset!.Data.Length.ShouldBe(5);
    }

    [Fact]
    public async Task PreloadLocaleAssetsAsync_DoesNotCacheAssetAsWrongType()
    {
        using var manager = new LocalizationManager(LocalizationConfig.Default, world);

        await manager.PreloadLocaleAssetsAsync(
            Locale.EnglishUS,
            ["greeting"],
            TestContext.Current.CancellationToken);

        // Preloading must not register any asset-cache entry (correct- or wrong-typed,
        // loaded or failed) that would shadow the eventual typed load.
        assetManager.GetCacheStats().TotalAssets.ShouldBe(0);
    }

    private sealed class StubAsset(byte[] data) : IDisposable
    {
        public byte[] Data { get; } = data;

        public void Dispose()
        {
        }
    }

    private sealed class StubAssetLoader : IAssetLoader<StubAsset>
    {
        public IReadOnlyList<string> Extensions => [".dat"];

        public StubAsset Load(Stream stream, AssetLoadContext context)
        {
            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            return new StubAsset(memory.ToArray());
        }

        public Task<StubAsset> LoadAsync(Stream stream, AssetLoadContext context, CancellationToken cancellationToken = default)
            => Task.FromResult(Load(stream, context));

        public long EstimateSize(StubAsset asset) => asset.Data.Length;
    }

    private sealed class StubResolver(string resolvedPath) : ILocalizedAssetResolver
    {
        public string? Resolve(string assetKey, Locale locale) => resolvedPath;

        public IEnumerable<string> GetAllPaths(string assetKey) => [resolvedPath];

        public bool HasLocaleVariant(string assetKey, Locale locale) => true;
    }
}
