using KeenEyes.Serialization;
using KeenEyes.Systems;

namespace KeenEyes.Tests;

/// <summary>
/// Additional tests for AutoSaveSystem to increase code coverage.
/// </summary>
public class AutoSaveSystemAdditionalTests : IDisposable
{
    private readonly string testSaveDirectory;
    private readonly TestComponentSerializer serializer;

    public AutoSaveSystemAdditionalTests()
    {
        testSaveDirectory = Path.Combine(Path.GetTempPath(), $"keen_eye_autosave_additional_{Guid.NewGuid():N}");
        serializer = TestSerializerFactory.CreateForSerializationTests();
    }

    public void Dispose()
    {
        if (Directory.Exists(testSaveDirectory))
        {
            Directory.Delete(testSaveDirectory, recursive: true);
        }
    }

    #region Constructor Validation Tests

    [Fact]
    public void Constructor_WithNullSerializer_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new AutoSaveSystem<TestComponentSerializer>(null!));
    }

    [Fact]
    public void Constructor_WithNullConfig_UsesDefaultConfig()
    {
        var system = new AutoSaveSystem<TestComponentSerializer>(serializer, null);

        Assert.NotNull(system.Config);
        Assert.Equal(AutoSaveConfig.Default.AutoSaveIntervalSeconds, system.Config.AutoSaveIntervalSeconds);
    }

    #endregion

    #region Config Property Tests

    [Fact]
    public void Config_SetToNull_ThrowsArgumentNullException()
    {
        var system = new AutoSaveSystem<TestComponentSerializer>(serializer);

        Assert.Throws<ArgumentNullException>(() =>
            system.Config = null!);
    }

    #endregion

    #region Pre-Initialization Tests

    [Fact]
    public void SaveNow_BeforeInitialization_ReturnsNull()
    {
        var system = new AutoSaveSystem<TestComponentSerializer>(serializer);

        var result = system.SaveNow();

        Assert.Null(result);
    }

    [Fact]
    public void CreateNewBaseline_BeforeInitialization_ReturnsNull()
    {
        var system = new AutoSaveSystem<TestComponentSerializer>(serializer);

        var result = system.CreateNewBaseline();

        Assert.Null(result);
    }

    #endregion
}
