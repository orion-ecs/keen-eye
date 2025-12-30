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

    #region Initialization With Existing Baseline Tests

    [Fact]
    public void OnInitialize_WithExistingBaseline_LoadsAndSetsBaseline()
    {
        // First create a baseline save
        using var world1 = new World { SaveDirectory = testSaveDirectory };
        world1.Spawn("Entity1").With(new SerializablePosition { X = 10, Y = 20 }).Build();

        var config = new AutoSaveConfig { AutoSaveIntervalSeconds = 1000f };
        var system1 = new AutoSaveSystem<TestComponentSerializer>(serializer, config);
        world1.AddSystem(system1);
        system1.SaveNow();

        // Now create a new world that should load the existing baseline
        using var world2 = new World { SaveDirectory = testSaveDirectory };
        world2.Components.Register<SerializablePosition>();

        var system2 = new AutoSaveSystem<TestComponentSerializer>(serializer, config);
        world2.AddSystem(system2);

        Assert.True(system2.HasBaseline);
    }


    [Fact]
    public void OnInitialize_WithCorruptedBaseline_StartsClean()
    {
        // Create directory with corrupted baseline file
        Directory.CreateDirectory(testSaveDirectory);
        var baselinePath = Path.Combine(testSaveDirectory, "autosave_baseline.kesave");
        File.WriteAllBytes(baselinePath, [0xDE, 0xAD, 0xBE, 0xEF]);

        using var world = new World { SaveDirectory = testSaveDirectory };
        var config = new AutoSaveConfig { AutoSaveIntervalSeconds = 1000f };
        var system = new AutoSaveSystem<TestComponentSerializer>(serializer, config);
        world.AddSystem(system);

        // Should start clean when baseline is corrupted
        Assert.False(system.HasBaseline);
        Assert.Equal(0, system.CurrentDeltaSequence);
    }

    #endregion

    #region TimeSinceLastSave Tests

    [Fact]
    public void TimeSinceLastSave_AccumulatesBetweenSaves()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Spawn().With(new SerializablePosition()).Build();

        var config = new AutoSaveConfig { AutoSaveIntervalSeconds = 1000f };
        var system = new AutoSaveSystem<TestComponentSerializer>(serializer, config);
        world.AddSystem(system);

        world.Update(5f);
        Assert.Equal(5f, system.TimeSinceLastSave, 0.01f);

        world.Update(3f);
        Assert.Equal(8f, system.TimeSinceLastSave, 0.01f);
    }

    #endregion

}
