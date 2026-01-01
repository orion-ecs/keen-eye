using KeenEyes.Editor.Assets;

namespace KeenEyes.Editor.Tests.Assets;

public class AssetDatabaseTests : IDisposable
{
    private readonly string tempDir;
    private AssetDatabase? database;

    public AssetDatabaseTests()
    {
        // Create a temporary directory for testing
        tempDir = Path.Combine(Path.GetTempPath(), $"KeenEyes.Editor.Tests.{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
    }

    public void Dispose()
    {
        database?.Dispose();

        // Clean up temp directory
        if (Directory.Exists(tempDir))
        {
            try
            {
                Directory.Delete(tempDir, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidPath_CreatesDatabase()
    {
        database = new AssetDatabase(tempDir);

        Assert.Equal(tempDir, database.ProjectRoot);
    }

    [Fact]
    public void Constructor_WithInvalidPath_ThrowsDirectoryNotFoundException()
    {
        var invalidPath = Path.Combine(tempDir, "nonexistent");

        Assert.Throws<DirectoryNotFoundException>(() => new AssetDatabase(invalidPath));
    }

    #endregion

    #region Scan Tests

    [Fact]
    public void Scan_WithNoExtensionFilter_FindsAllFiles()
    {
        // Create test files
        File.WriteAllText(Path.Combine(tempDir, "scene.kescene"), "{}");
        File.WriteAllText(Path.Combine(tempDir, "prefab.keprefab"), "{}");
        File.WriteAllText(Path.Combine(tempDir, "data.json"), "{}");

        database = new AssetDatabase(tempDir);
        database.Scan();

        Assert.Equal(3, database.AllAssets.Count());
    }

    [Fact]
    public void Scan_WithExtensionFilter_FindsOnlyMatchingFiles()
    {
        // Create test files
        File.WriteAllText(Path.Combine(tempDir, "scene.kescene"), "{}");
        File.WriteAllText(Path.Combine(tempDir, "prefab.keprefab"), "{}");
        File.WriteAllText(Path.Combine(tempDir, "data.json"), "{}");

        database = new AssetDatabase(tempDir);
        database.Scan(".kescene");

        var assets = database.AllAssets.ToList();
        Assert.Single(assets);
        Assert.Equal("scene", assets[0].Name);
    }

    [Fact]
    public void Scan_SetsCorrectAssetType()
    {
        File.WriteAllText(Path.Combine(tempDir, "test.kescene"), "{}");

        database = new AssetDatabase(tempDir);
        database.Scan();

        var asset = database.AllAssets.First();
        Assert.Equal(AssetType.Scene, asset.Type);
    }

    [Fact]
    public void Scan_FindsFilesInSubdirectories()
    {
        var subDir = Path.Combine(tempDir, "Scenes");
        Directory.CreateDirectory(subDir);
        File.WriteAllText(Path.Combine(subDir, "level.kescene"), "{}");

        database = new AssetDatabase(tempDir);
        database.Scan();

        var asset = database.AllAssets.First();
        Assert.Contains("Scenes", asset.RelativePath);
    }

    #endregion

    #region GetAsset Tests

    [Fact]
    public void GetAsset_WithValidPath_ReturnsAsset()
    {
        File.WriteAllText(Path.Combine(tempDir, "test.kescene"), "{}");

        database = new AssetDatabase(tempDir);
        database.Scan();

        var asset = database.GetAsset("test.kescene");

        Assert.NotNull(asset);
        Assert.Equal("test", asset.Name);
    }

    [Fact]
    public void GetAsset_WithInvalidPath_ReturnsNull()
    {
        database = new AssetDatabase(tempDir);
        database.Scan();

        var asset = database.GetAsset("nonexistent.kescene");

        Assert.Null(asset);
    }

    [Fact]
    public void GetAsset_IsCaseInsensitive()
    {
        File.WriteAllText(Path.Combine(tempDir, "Test.kescene"), "{}");

        database = new AssetDatabase(tempDir);
        database.Scan();

        var asset = database.GetAsset("test.KESCENE");

        Assert.NotNull(asset);
    }

    #endregion

    #region GetAssetsByType Tests

    [Fact]
    public void GetAssetsByType_ReturnsMatchingAssets()
    {
        File.WriteAllText(Path.Combine(tempDir, "scene1.kescene"), "{}");
        File.WriteAllText(Path.Combine(tempDir, "scene2.kescene"), "{}");
        File.WriteAllText(Path.Combine(tempDir, "prefab.keprefab"), "{}");

        database = new AssetDatabase(tempDir);
        database.Scan();

        var scenes = database.GetAssetsByType(AssetType.Scene).ToList();

        Assert.Equal(2, scenes.Count);
        Assert.All(scenes, s => Assert.Equal(AssetType.Scene, s.Type));
    }

    [Fact]
    public void GetAssetsByType_ReturnsEmptyForNoMatches()
    {
        File.WriteAllText(Path.Combine(tempDir, "prefab.keprefab"), "{}");

        database = new AssetDatabase(tempDir);
        database.Scan();

        var scenes = database.GetAssetsByType(AssetType.Scene).ToList();

        Assert.Empty(scenes);
    }

    #endregion

    #region AssetType Classification Tests

    [Theory]
    [InlineData("test.kescene", AssetType.Scene)]
    [InlineData("test.keprefab", AssetType.Prefab)]
    [InlineData("test.keworld", AssetType.WorldConfig)]
    [InlineData("test.png", AssetType.Texture)]
    [InlineData("test.jpg", AssetType.Texture)]
    [InlineData("test.kesl", AssetType.Shader)]
    [InlineData("test.wav", AssetType.Audio)]
    [InlineData("test.mp3", AssetType.Audio)]
    [InlineData("test.cs", AssetType.Script)]
    [InlineData("test.json", AssetType.Data)]
    [InlineData("test.xyz", AssetType.Unknown)]
    public void Scan_ClassifiesAssetTypesCorrectly(string filename, AssetType expectedType)
    {
        File.WriteAllText(Path.Combine(tempDir, filename), "");

        database = new AssetDatabase(tempDir);
        database.Scan();

        var asset = database.AllAssets.First();
        Assert.Equal(expectedType, asset.Type);
    }

    #endregion

    #region File Watching Tests

    [Fact]
    public async Task StartWatching_RaisesAssetAddedEvent_WhenFileCreated()
    {
        database = new AssetDatabase(tempDir);
        database.Scan();
        database.StartWatching();

        AssetEntry? addedAsset = null;
        database.AssetAdded += (_, args) => addedAsset = args.Asset;

        // Create a file
        File.WriteAllText(Path.Combine(tempDir, "new.kescene"), "{}");

        // Wait for file system event
        await Task.Delay(500, TestContext.Current.CancellationToken);

        Assert.NotNull(addedAsset);
        Assert.Equal("new", addedAsset.Name);
    }

    [Fact]
    public async Task StartWatching_RaisesAssetRemovedEvent_WhenFileDeleted()
    {
        var testFile = Path.Combine(tempDir, "toDelete.kescene");
        File.WriteAllText(testFile, "{}");

        database = new AssetDatabase(tempDir);
        database.Scan();
        database.StartWatching();

        AssetEntry? removedAsset = null;
        database.AssetRemoved += (_, args) => removedAsset = args.Asset;

        // Delete the file
        File.Delete(testFile);

        // Wait for file system event
        await Task.Delay(500, TestContext.Current.CancellationToken);

        Assert.NotNull(removedAsset);
        Assert.Equal("toDelete", removedAsset.Name);
    }

    [Fact]
    public void StopWatching_StopsRaisingEvents()
    {
        database = new AssetDatabase(tempDir);
        database.Scan();
        database.StartWatching();
        database.StopWatching();

        var eventRaised = false;
        database.AssetAdded += (_, _) => eventRaised = true;

        // Create a file
        File.WriteAllText(Path.Combine(tempDir, "ignored.kescene"), "{}");

        // Wait briefly
        Thread.Sleep(100);

        Assert.False(eventRaised);
    }

    #endregion

    #region AssetEntry Tests

    [Fact]
    public void AssetEntry_ContainsCorrectMetadata()
    {
        var testFile = Path.Combine(tempDir, "test.kescene");
        var content = "{ \"test\": true }";
        File.WriteAllText(testFile, content);

        database = new AssetDatabase(tempDir);
        database.Scan();

        var asset = database.AllAssets.First();

        Assert.Equal("test", asset.Name);
        Assert.Equal("test.kescene", asset.RelativePath);
        Assert.Equal(testFile, asset.FullPath);
        Assert.Equal(".kescene", asset.Extension);
        Assert.Equal(AssetType.Scene, asset.Type);
        Assert.Equal(content.Length, asset.Size);
        Assert.True(asset.LastModified <= DateTime.UtcNow);
    }

    #endregion
}
