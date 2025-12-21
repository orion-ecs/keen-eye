namespace KeenEyes.Assets.Tests;

/// <summary>
/// Test asset that represents a simple string-based resource.
/// </summary>
public sealed class TestAsset(string content) : IDisposable
{
    public string Content { get; } = content;
    public bool IsDisposed { get; private set; }

    public void Dispose()
    {
        IsDisposed = true;
    }
}

/// <summary>
/// Another test asset type for multi-type testing.
/// </summary>
public sealed class SecondTestAsset(int value) : IDisposable
{
    public int Value { get; } = value;
    public bool IsDisposed { get; private set; }

    public void Dispose()
    {
        IsDisposed = true;
    }
}

/// <summary>
/// Test loader that reads text files as TestAsset.
/// </summary>
public sealed class TestAssetLoader : IAssetLoader<TestAsset>
{
    public IReadOnlyList<string> Extensions => [".txt", ".test"];

    public TestAsset Load(Stream stream, AssetLoadContext context)
    {
        using var reader = new StreamReader(stream);
        var content = reader.ReadToEnd();
        return new TestAsset(content);
    }

    public async Task<TestAsset> LoadAsync(Stream stream, AssetLoadContext context, CancellationToken ct = default)
    {
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync(ct);
        return new TestAsset(content);
    }

    public long EstimateSize(TestAsset asset)
    {
        return asset.Content.Length * 2; // Approximate UTF-16 size
    }
}

/// <summary>
/// Test loader for SecondTestAsset.
/// </summary>
public sealed class SecondTestAssetLoader : IAssetLoader<SecondTestAsset>
{
    public IReadOnlyList<string> Extensions => [".num"];

    public SecondTestAsset Load(Stream stream, AssetLoadContext context)
    {
        using var reader = new StreamReader(stream);
        var content = reader.ReadToEnd();
        return new SecondTestAsset(int.Parse(content.Trim()));
    }

    public async Task<SecondTestAsset> LoadAsync(Stream stream, AssetLoadContext context, CancellationToken ct = default)
    {
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync(ct);
        return new SecondTestAsset(int.Parse(content.Trim()));
    }

    public long EstimateSize(SecondTestAsset asset)
    {
        return sizeof(int);
    }
}

/// <summary>
/// Loader that throws on load for error testing.
/// </summary>
public sealed class FailingLoader : IAssetLoader<TestAsset>
{
    public IReadOnlyList<string> Extensions => [".fail"];

    public TestAsset Load(Stream stream, AssetLoadContext context)
    {
        throw new InvalidOperationException("Intentional test failure");
    }

    public Task<TestAsset> LoadAsync(Stream stream, AssetLoadContext context, CancellationToken ct = default)
    {
        throw new InvalidOperationException("Intentional test failure");
    }

    public long EstimateSize(TestAsset asset) => 0;
}

/// <summary>
/// Slow loader for async testing.
/// </summary>
public sealed class SlowLoader(int delayMs = 100) : IAssetLoader<TestAsset>
{
    public IReadOnlyList<string> Extensions => [".slow"];

    public TestAsset Load(Stream stream, AssetLoadContext context)
    {
        Thread.Sleep(delayMs);
        using var reader = new StreamReader(stream);
        return new TestAsset(reader.ReadToEnd());
    }

    public async Task<TestAsset> LoadAsync(Stream stream, AssetLoadContext context, CancellationToken ct = default)
    {
        await Task.Delay(delayMs, ct);
        using var reader = new StreamReader(stream);
        return new TestAsset(await reader.ReadToEndAsync(ct));
    }

    public long EstimateSize(TestAsset asset) => asset.Content.Length * 2;
}

/// <summary>
/// Helper to create temporary test files.
/// </summary>
public sealed class TestAssetDirectory : IDisposable
{
    public string RootPath { get; }

    public TestAssetDirectory()
    {
        RootPath = Path.Combine(Path.GetTempPath(), $"KeenEyes_Assets_Test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(RootPath);
    }

    public string CreateFile(string relativePath, string content)
    {
        var fullPath = Path.Combine(RootPath, relativePath);
        var dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllText(fullPath, content);
        return relativePath;
    }

    public string CreateFile(string relativePath, byte[] content)
    {
        var fullPath = Path.Combine(RootPath, relativePath);
        var dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllBytes(fullPath, content);
        return relativePath;
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(RootPath))
            {
                Directory.Delete(RootPath, recursive: true);
            }
        }
        catch
        {
            // Best effort cleanup
        }
    }
}
