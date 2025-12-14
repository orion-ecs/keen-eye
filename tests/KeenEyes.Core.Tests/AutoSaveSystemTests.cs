using KeenEyes.Serialization;
using KeenEyes.Systems;

namespace KeenEyes.Tests;

/// <summary>
/// Tests for the AutoSaveSystem.
/// </summary>
public class AutoSaveSystemTests : IDisposable
{
    private readonly string testSaveDirectory;
    private readonly TestComponentSerializer serializer;

    public AutoSaveSystemTests()
    {
        testSaveDirectory = Path.Combine(Path.GetTempPath(), $"keen_eye_autosave_tests_{Guid.NewGuid():N}");
        serializer = TestSerializerFactory.CreateForSerializationTests();
    }

    public void Dispose()
    {
        if (Directory.Exists(testSaveDirectory))
        {
            Directory.Delete(testSaveDirectory, recursive: true);
        }
    }

    #region Configuration Tests

    [Fact]
    public void DefaultConfig_HasReasonableDefaults()
    {
        var config = AutoSaveConfig.Default;

        Assert.Equal(300f, config.AutoSaveIntervalSeconds);
        Assert.Equal(1000, config.ChangeThreshold);
        Assert.True(config.UseDeltaSaves);
        Assert.Equal(10, config.MaxDeltasBeforeBaseline);
        Assert.Equal("autosave", config.BaseSlotName);
        Assert.True(config.Enabled);
    }

    [Fact]
    public void FrequentConfig_HasShorterIntervals()
    {
        var config = AutoSaveConfig.Frequent;

        Assert.Equal(60f, config.AutoSaveIntervalSeconds);
        Assert.Equal(500, config.ChangeThreshold);
        Assert.True(config.UseDeltaSaves);
    }

    [Fact]
    public void InfrequentConfig_DisablesDeltaSaves()
    {
        var config = AutoSaveConfig.Infrequent;

        Assert.Equal(600f, config.AutoSaveIntervalSeconds);
        Assert.False(config.UseDeltaSaves);
    }

    [Fact]
    public void Config_BaselineSlotName_ReturnsCorrectFormat()
    {
        var config = new AutoSaveConfig { BaseSlotName = "mysave" };

        Assert.Equal("mysave_baseline", config.BaselineSlotName);
    }

    [Fact]
    public void Config_GetDeltaSlotName_ReturnsCorrectFormat()
    {
        var config = new AutoSaveConfig { BaseSlotName = "mysave" };

        Assert.Equal("mysave_delta_1", config.GetDeltaSlotName(1));
        Assert.Equal("mysave_delta_5", config.GetDeltaSlotName(5));
    }

    #endregion

    #region System Initialization Tests

    [Fact]
    public void Constructor_WithSerializer_CreatesSystem()
    {
        var system = new AutoSaveSystem<TestComponentSerializer>(serializer);

        Assert.NotNull(system);
        Assert.Equal(0, system.TimeSinceLastSave);
        Assert.Equal(0, system.CurrentDeltaSequence);
        Assert.False(system.HasBaseline);
    }

    [Fact]
    public void Constructor_WithConfig_UsesConfig()
    {
        var config = new AutoSaveConfig { AutoSaveIntervalSeconds = 120f };
        var system = new AutoSaveSystem<TestComponentSerializer>(serializer, config);

        Assert.Equal(120f, system.Config.AutoSaveIntervalSeconds);
    }

    [Fact]
    public void Config_CanBeChanged()
    {
        var system = new AutoSaveSystem<TestComponentSerializer>(serializer);
        var newConfig = AutoSaveConfig.Frequent;

        system.Config = newConfig;

        Assert.Equal(60f, system.Config.AutoSaveIntervalSeconds);
    }

    #endregion

    #region Auto-Save Trigger Tests

    [Fact]
    public void Update_BeforeInterval_DoesNotSave()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Spawn().With(new SerializablePosition()).Build();

        var config = new AutoSaveConfig { AutoSaveIntervalSeconds = 10f };
        var system = new AutoSaveSystem<TestComponentSerializer>(serializer, config);
        world.AddSystem(system);

        // Update for less than the interval
        world.Update(5f);

        Assert.False(world.SaveSlotExists(config.BaselineSlotName));
    }

    [Fact]
    public void Update_AfterInterval_TriggersSave()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Spawn().With(new SerializablePosition()).Build();

        var config = new AutoSaveConfig { AutoSaveIntervalSeconds = 10f };
        var system = new AutoSaveSystem<TestComponentSerializer>(serializer, config);
        world.AddSystem(system);

        // Update past the interval
        world.Update(15f);

        Assert.True(world.SaveSlotExists(config.BaselineSlotName));
    }

    [Fact]
    public void Update_WhenDisabled_DoesNotSave()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Spawn().With(new SerializablePosition()).Build();

        var config = new AutoSaveConfig { AutoSaveIntervalSeconds = 10f, Enabled = false };
        var system = new AutoSaveSystem<TestComponentSerializer>(serializer, config);
        world.AddSystem(system);

        // Update past the interval
        world.Update(15f);

        Assert.False(world.SaveSlotExists(config.BaselineSlotName));
    }

    [Fact]
    public void Update_AfterSave_ResetsTimer()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Spawn().With(new SerializablePosition()).Build();

        var config = new AutoSaveConfig { AutoSaveIntervalSeconds = 10f };
        var system = new AutoSaveSystem<TestComponentSerializer>(serializer, config);
        world.AddSystem(system);

        world.Update(15f); // First save
        Assert.True(system.TimeSinceLastSave < 1f); // Timer reset
    }

    #endregion

    #region Manual Save Tests

    [Fact]
    public void SaveNow_CreatesBaseline()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Spawn().With(new SerializablePosition()).Build();

        var config = new AutoSaveConfig { AutoSaveIntervalSeconds = 1000f };
        var system = new AutoSaveSystem<TestComponentSerializer>(serializer, config);
        world.AddSystem(system);

        var info = system.SaveNow();

        Assert.NotNull(info);
        Assert.True(world.SaveSlotExists(config.BaselineSlotName));
        Assert.True(system.HasBaseline);
    }

    [Fact]
    public void CreateNewBaseline_ResetsSequence()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Spawn().With(new SerializablePosition()).Build();

        var config = new AutoSaveConfig { AutoSaveIntervalSeconds = 1f };
        var system = new AutoSaveSystem<TestComponentSerializer>(serializer, config);
        world.AddSystem(system);

        // Create multiple saves
        world.Update(2f); // First baseline
        world.Update(2f); // Delta 1

        // Force new baseline
        system.CreateNewBaseline();

        Assert.Equal(0, system.CurrentDeltaSequence);
    }

    [Fact]
    public void Reset_ClearsState()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Spawn().With(new SerializablePosition()).Build();

        var config = new AutoSaveConfig { AutoSaveIntervalSeconds = 1000f };
        var system = new AutoSaveSystem<TestComponentSerializer>(serializer, config);
        world.AddSystem(system);

        system.SaveNow();
        system.Reset();

        Assert.Equal(0, system.TimeSinceLastSave);
        Assert.Equal(0, system.CurrentDeltaSequence);
        Assert.False(system.HasBaseline);
    }

    #endregion

    #region Delta Save Tests

    [Fact]
    public void DeltaSave_AfterBaseline_IncrementSequence()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Spawn().With(new SerializablePosition()).Build();

        var config = new AutoSaveConfig { AutoSaveIntervalSeconds = 1f, UseDeltaSaves = true };
        var system = new AutoSaveSystem<TestComponentSerializer>(serializer, config);
        world.AddSystem(system);

        world.Update(2f); // Baseline
        Assert.Equal(0, system.CurrentDeltaSequence);

        world.Update(2f); // Delta 1
        Assert.Equal(1, system.CurrentDeltaSequence);

        world.Update(2f); // Delta 2
        Assert.Equal(2, system.CurrentDeltaSequence);
    }

    [Fact]
    public void DeltaSave_AtMaxDeltas_CreatesNewBaseline()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Spawn().With(new SerializablePosition()).Build();

        var config = new AutoSaveConfig
        {
            AutoSaveIntervalSeconds = 1f,
            UseDeltaSaves = true,
            MaxDeltasBeforeBaseline = 3
        };
        var system = new AutoSaveSystem<TestComponentSerializer>(serializer, config);
        world.AddSystem(system);

        // Create baseline + 3 deltas
        for (int i = 0; i < 5; i++)
        {
            world.Update(2f);
        }

        // After max deltas, should have reset to new baseline
        Assert.True(system.CurrentDeltaSequence <= config.MaxDeltasBeforeBaseline);
    }

    [Fact]
    public void FullSaveMode_DoesNotCreateDeltas()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Spawn().With(new SerializablePosition()).Build();

        var config = new AutoSaveConfig
        {
            AutoSaveIntervalSeconds = 1f,
            UseDeltaSaves = false
        };
        var system = new AutoSaveSystem<TestComponentSerializer>(serializer, config);
        world.AddSystem(system);

        world.Update(2f);
        world.Update(2f);
        world.Update(2f);

        // Should always be at sequence 0 (only baselines)
        Assert.Equal(0, system.CurrentDeltaSequence);
    }

    #endregion

    #region Event Tests

    [Fact]
    public void OnAutoSave_IsRaisedOnSave()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Spawn().With(new SerializablePosition()).Build();

        var config = new AutoSaveConfig { AutoSaveIntervalSeconds = 1f };
        var system = new AutoSaveSystem<TestComponentSerializer>(serializer, config);

        SaveSlotInfo? capturedInfo = null;
        system.OnAutoSave += info => capturedInfo = info;

        world.AddSystem(system);
        world.Update(2f);

        Assert.NotNull(capturedInfo);
        Assert.Contains("autosave", capturedInfo!.SlotName);
    }

    #endregion

    #region DeltaSnapshot Tests

    [Fact]
    public void DeltaSnapshot_EmptyDelta_HasNoChanges()
    {
        var delta = new DeltaSnapshot
        {
            BaselineSlotName = "test_baseline"
        };

        Assert.True(delta.IsEmpty);
        Assert.Equal(0, delta.ChangeCount);
    }

    [Fact]
    public void DeltaSnapshot_WithCreatedEntities_IsNotEmpty()
    {
        var delta = new DeltaSnapshot
        {
            BaselineSlotName = "test_baseline",
            CreatedEntities = [new SerializedEntity { Id = 1, Name = "Test", Components = [] }]
        };

        Assert.False(delta.IsEmpty);
        Assert.Equal(1, delta.ChangeCount);
    }

    [Fact]
    public void DeltaSnapshot_WithDestroyedEntities_IsNotEmpty()
    {
        var delta = new DeltaSnapshot
        {
            BaselineSlotName = "test_baseline",
            DestroyedEntityIds = [1, 2, 3]
        };

        Assert.False(delta.IsEmpty);
        Assert.Equal(3, delta.ChangeCount);
    }

    [Fact]
    public void DeltaSnapshot_WithModifiedEntities_IsNotEmpty()
    {
        var delta = new DeltaSnapshot
        {
            BaselineSlotName = "test_baseline",
            ModifiedEntities = [new EntityDelta { EntityId = 1 }]
        };

        Assert.False(delta.IsEmpty);
        Assert.Equal(1, delta.ChangeCount);
    }

    [Fact]
    public void EntityDelta_EmptyDelta_HasNoChanges()
    {
        var delta = new EntityDelta { EntityId = 1 };

        Assert.True(delta.IsEmpty);
    }

    [Fact]
    public void EntityDelta_WithAddedComponents_IsNotEmpty()
    {
        var delta = new EntityDelta
        {
            EntityId = 1,
            AddedComponents = [new SerializedComponent { TypeName = "Position" }]
        };

        Assert.False(delta.IsEmpty);
    }

    #endregion
}
