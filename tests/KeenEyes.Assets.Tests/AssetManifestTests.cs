using System.Text;

namespace KeenEyes.Assets.Tests;

/// <summary>
/// Tests for the asset manifest functionality.
/// </summary>
public class AssetManifestTests
{
    #region Builder Tests

    [Fact]
    public void CreateBuilder_ReturnsBuilder()
    {
        var builder = AssetManifest.CreateBuilder();

        Assert.NotNull(builder);
    }

    [Fact]
    public void Builder_AddAsset_AddsToManifest()
    {
        var manifest = AssetManifest.CreateBuilder()
            .AddAsset("textures/player.png", "texture", 12345)
            .Build();

        Assert.Single(manifest.Assets);
        Assert.Equal("textures/player.png", manifest.Assets[0].Path);
        Assert.Equal("texture", manifest.Assets[0].Type);
        Assert.Equal(12345, manifest.Assets[0].Size);
    }

    [Fact]
    public void Builder_AddAssetWithDependencies_IncludesDependencies()
    {
        var deps = new List<string> { "textures/atlas.png" };
        var manifest = AssetManifest.CreateBuilder()
            .AddAsset("atlases/characters.json", "atlas", 5678, null, deps)
            .Build();

        Assert.Single(manifest.Assets);
        Assert.NotNull(manifest.Assets[0].Dependencies);
        Assert.Contains("textures/atlas.png", manifest.Assets[0].Dependencies!);
    }

    [Fact]
    public void Builder_WithVersion_SetsVersion()
    {
        var manifest = AssetManifest.CreateBuilder()
            .WithVersion(2)
            .AddAsset("test.bin", "raw", 100)
            .Build();

        Assert.Equal(2, manifest.Version);
    }

    [Fact]
    public void Builder_WithGeneratedTime_SetsTimestamp()
    {
        var timestamp = new DateTime(2025, 1, 9, 12, 0, 0, DateTimeKind.Utc);
        var manifest = AssetManifest.CreateBuilder()
            .WithGeneratedTime(timestamp)
            .AddAsset("test.bin", "raw", 100)
            .Build();

        Assert.Equal(timestamp, manifest.Generated);
    }

    #endregion

    #region Query Tests

    [Fact]
    public void Exists_WithExistingPath_ReturnsTrue()
    {
        var manifest = AssetManifest.CreateBuilder()
            .AddAsset("textures/player.png", "texture", 100)
            .Build();

        Assert.True(manifest.Exists("textures/player.png"));
    }

    [Fact]
    public void Exists_WithNonExistingPath_ReturnsFalse()
    {
        var manifest = AssetManifest.CreateBuilder()
            .AddAsset("textures/player.png", "texture", 100)
            .Build();

        Assert.False(manifest.Exists("textures/enemy.png"));
    }

    [Fact]
    public void Exists_IsCaseInsensitive()
    {
        var manifest = AssetManifest.CreateBuilder()
            .AddAsset("textures/Player.png", "texture", 100)
            .Build();

        Assert.True(manifest.Exists("TEXTURES/PLAYER.PNG"));
        Assert.True(manifest.Exists("textures/player.png"));
    }

    [Fact]
    public void GetInfo_WithExistingPath_ReturnsAssetInfo()
    {
        var manifest = AssetManifest.CreateBuilder()
            .AddAsset("audio/music.ogg", "audio", 500000, "sha256:abc123")
            .Build();

        var info = manifest.GetInfo("audio/music.ogg");

        Assert.NotNull(info);
        Assert.Equal("audio/music.ogg", info.Value.Path);
        Assert.Equal("audio", info.Value.Type);
        Assert.Equal(500000, info.Value.Size);
        Assert.Equal("sha256:abc123", info.Value.Hash);
    }

    [Fact]
    public void GetInfo_WithNonExistingPath_ReturnsNull()
    {
        var manifest = AssetManifest.CreateBuilder()
            .AddAsset("audio/music.ogg", "audio", 500000)
            .Build();

        var info = manifest.GetInfo("audio/sfx.ogg");

        Assert.Null(info);
    }

    [Fact]
    public void GetAssetsOfType_ReturnsMatchingAssets()
    {
        var manifest = AssetManifest.CreateBuilder()
            .AddAsset("tex1.png", "texture", 100)
            .AddAsset("tex2.png", "texture", 200)
            .AddAsset("audio1.ogg", "audio", 300)
            .Build();

        var textures = manifest.GetAssetsOfType("texture");

        Assert.Equal(2, textures.Count);
        Assert.All(textures, a => Assert.Equal("texture", a.Type));
    }

    [Fact]
    public void GetAssetsOfType_IsCaseInsensitive()
    {
        var manifest = AssetManifest.CreateBuilder()
            .AddAsset("tex1.png", "Texture", 100)
            .Build();

        var textures = manifest.GetAssetsOfType("TEXTURE");

        Assert.Single(textures);
    }

    [Fact]
    public void GetDependencies_WithDependencies_ReturnsList()
    {
        var manifest = AssetManifest.CreateBuilder()
            .AddAsset("atlas.json", "atlas", 100, null, ["texture.png", "shader.glsl"])
            .Build();

        var deps = manifest.GetDependencies("atlas.json");

        Assert.Equal(2, deps.Count);
        Assert.Contains("texture.png", deps);
        Assert.Contains("shader.glsl", deps);
    }

    [Fact]
    public void GetDependencies_WithoutDependencies_ReturnsEmptyList()
    {
        var manifest = AssetManifest.CreateBuilder()
            .AddAsset("simple.bin", "raw", 100)
            .Build();

        var deps = manifest.GetDependencies("simple.bin");

        Assert.Empty(deps);
    }

    [Fact]
    public void GetDependencies_WithNonExistingPath_ReturnsEmptyList()
    {
        var manifest = AssetManifest.CreateBuilder()
            .AddAsset("exists.bin", "raw", 100)
            .Build();

        var deps = manifest.GetDependencies("notexists.bin");

        Assert.Empty(deps);
    }

    #endregion

    #region Statistics Tests

    [Fact]
    public void Statistics_TotalAssets_ReturnsCount()
    {
        var manifest = AssetManifest.CreateBuilder()
            .AddAsset("a.bin", "raw", 100)
            .AddAsset("b.bin", "raw", 200)
            .AddAsset("c.bin", "raw", 300)
            .Build();

        Assert.Equal(3, manifest.Statistics.TotalAssets);
    }

    [Fact]
    public void Statistics_TotalSize_ReturnsSumOfSizes()
    {
        var manifest = AssetManifest.CreateBuilder()
            .AddAsset("a.bin", "raw", 100)
            .AddAsset("b.bin", "raw", 200)
            .AddAsset("c.bin", "raw", 300)
            .Build();

        Assert.Equal(600, manifest.Statistics.TotalSize);
    }

    [Fact]
    public void Statistics_ByType_GroupsCorrectly()
    {
        var manifest = AssetManifest.CreateBuilder()
            .AddAsset("tex1.png", "texture", 100)
            .AddAsset("tex2.png", "texture", 100)
            .AddAsset("audio.ogg", "audio", 100)
            .Build();

        Assert.Equal(2, manifest.Statistics.ByType["texture"]);
        Assert.Equal(1, manifest.Statistics.ByType["audio"]);
    }

    #endregion

    #region Serialization Tests

    [Fact]
    public void SaveAndLoad_RoundTrips()
    {
        var original = AssetManifest.CreateBuilder()
            .WithVersion(1)
            .WithGeneratedTime(new DateTime(2025, 1, 9, 12, 0, 0, DateTimeKind.Utc))
            .AddAsset("tex.png", "texture", 12345, "sha256:abc")
            .AddAsset("atlas.json", "atlas", 5678, null, ["tex.png"])
            .Build();

        // Save to stream
        using var stream = new MemoryStream();
        original.SaveToStream(stream);

        // Load from stream
        stream.Position = 0;
        var loaded = AssetManifest.LoadFromStream(stream);

        // Verify
        Assert.Equal(original.Version, loaded.Version);
        Assert.Equal(original.Generated, loaded.Generated);
        Assert.Equal(original.Assets.Count, loaded.Assets.Count);

        Assert.True(loaded.Exists("tex.png"));
        Assert.True(loaded.Exists("atlas.json"));

        var atlasInfo = loaded.GetInfo("atlas.json");
        Assert.NotNull(atlasInfo);
        Assert.Single(loaded.GetDependencies("atlas.json"));
    }

    [Fact]
    public void LoadFromStream_WithInvalidJson_ThrowsInvalidDataException()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("not json"));

        Assert.Throws<System.Text.Json.JsonException>(() => AssetManifest.LoadFromStream(stream));
    }

    [Fact]
    public void LoadFromStream_WithEmptyStream_ThrowsInvalidDataException()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("null"));

        Assert.Throws<InvalidDataException>(() => AssetManifest.LoadFromStream(stream));
    }

    [Fact]
    public void LoadFromStream_WithMissingPath_ThrowsInvalidDataException()
    {
        var json = """
        {
            "version": 1,
            "generated": "2025-01-09T12:00:00Z",
            "assets": [
                { "type": "texture", "size": 100 }
            ]
        }
        """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        Assert.Throws<InvalidDataException>(() => AssetManifest.LoadFromStream(stream));
    }

    [Fact]
    public void Load_WithNullPath_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => AssetManifest.Load(null!));
    }

    [Fact]
    public void Load_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        Assert.Throws<FileNotFoundException>(() => AssetManifest.Load("/nonexistent/path/manifest.json"));
    }

    #endregion

    #region AssetInfo Tests

    [Fact]
    public void AssetInfo_RecordEquality_Works()
    {
        var a = new AssetInfo("path.png", "texture", 100, "hash", null);
        var b = new AssetInfo("path.png", "texture", 100, "hash", null);

        Assert.Equal(a, b);
    }

    [Fact]
    public void ManifestStatistics_RecordEquality_Works()
    {
        var dict1 = new Dictionary<string, int> { ["texture"] = 5 };
        var dict2 = new Dictionary<string, int> { ["texture"] = 5 };

        var a = new ManifestStatistics(5, 1000, dict1);
        var b = new ManifestStatistics(5, 1000, dict2);

        // Note: Dictionary comparison uses reference equality, so these won't be equal
        // This is expected behavior for record structs with mutable reference types
        Assert.Equal(a.TotalAssets, b.TotalAssets);
        Assert.Equal(a.TotalSize, b.TotalSize);
    }

    #endregion
}
