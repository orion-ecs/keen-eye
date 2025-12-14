using KeenEyes.Serialization;

namespace KeenEyes.Tests;

/// <summary>
/// Tests for the SaveManager and World save/load APIs.
/// </summary>
public class SaveManagerTests : IDisposable
{
    private readonly string testSaveDirectory;
    private readonly TestComponentSerializer serializer;

    public SaveManagerTests()
    {
        testSaveDirectory = Path.Combine(Path.GetTempPath(), $"keen_eye_tests_{Guid.NewGuid():N}");
        serializer = TestSerializerFactory.CreateForSerializationTests();
    }

    public void Dispose()
    {
        if (Directory.Exists(testSaveDirectory))
        {
            Directory.Delete(testSaveDirectory, recursive: true);
        }
    }

    #region Save Tests

    [Fact]
    public void SaveToSlot_CreatesFile()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Spawn("TestEntity")
            .With(new SerializablePosition { X = 10, Y = 20 })
            .Build();

        var info = world.SaveToSlot("slot1", serializer);

        Assert.True(File.Exists(world.GetSaveSlotPath("slot1")));
        Assert.Equal("slot1", info.SlotName);
    }

    [Fact]
    public void SaveToSlot_CapturesEntityCount()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        for (int i = 0; i < 5; i++)
        {
            world.Spawn($"Entity{i}")
                .With(new SerializablePosition { X = i, Y = i * 2 })
                .Build();
        }

        var info = world.SaveToSlot("slot1", serializer);

        Assert.Equal(5, info.EntityCount);
    }

    [Fact]
    public void SaveToSlot_WithOptions_UsesDisplayName()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Spawn().With(new SerializablePosition()).Build();

        var options = new SaveSlotOptions { DisplayName = "Chapter 3 - The Forest" };
        var info = world.SaveToSlot("slot1", serializer, options);

        Assert.Equal("Chapter 3 - The Forest", info.DisplayName);
    }

    [Fact]
    public void SaveToSlot_WithOptions_UsesPlayTime()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Spawn().With(new SerializablePosition()).Build();

        var playTime = TimeSpan.FromHours(2.5);
        var options = new SaveSlotOptions { PlayTime = playTime };
        var info = world.SaveToSlot("slot1", serializer, options);

        Assert.Equal(playTime, info.PlayTime);
    }

    [Fact]
    public void SaveToSlot_IncrementsSaveCount()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Spawn().With(new SerializablePosition()).Build();

        var info1 = world.SaveToSlot("slot1", serializer);
        Assert.Equal(1, info1.SaveCount);

        var info2 = world.SaveToSlot("slot1", serializer);
        Assert.Equal(2, info2.SaveCount);

        var info3 = world.SaveToSlot("slot1", serializer);
        Assert.Equal(3, info3.SaveCount);
    }

    [Fact]
    public void SaveToSlot_PreservesCreatedAt()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Spawn().With(new SerializablePosition()).Build();

        var info1 = world.SaveToSlot("slot1", serializer);
        var createdAt = info1.CreatedAt;

        // Wait a bit and save again
        Thread.Sleep(50);
        var info2 = world.SaveToSlot("slot1", serializer);

        Assert.Equal(createdAt.ToUnixTimeMilliseconds(), info2.CreatedAt.ToUnixTimeMilliseconds());
        Assert.True(info2.ModifiedAt > info1.ModifiedAt);
    }

    #endregion

    #region Load Tests

    [Fact]
    public void LoadFromSlot_RestoresEntities()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Spawn("Player")
            .With(new SerializablePosition { X = 100, Y = 200 })
            .With(new SerializableHealth { Current = 75, Max = 100 })
            .Build();
        world.Spawn("Enemy")
            .With(new SerializablePosition { X = 50, Y = 60 })
            .Build();

        world.SaveToSlot("slot1", serializer);

        // Clear and restore
        world.Clear();
        Assert.Empty(world.GetAllEntities());

        var (info, entityMap) = world.LoadFromSlot("slot1", serializer);

        Assert.Equal(2, world.GetAllEntities().Count());
        Assert.Equal(2, entityMap.Count);

        var player = world.GetEntityByName("Player");
        Assert.True(player.IsValid);
        ref var pos = ref world.Get<SerializablePosition>(player);
        Assert.Equal(100f, pos.X);
        Assert.Equal(200f, pos.Y);
    }

    [Fact]
    public void LoadFromSlot_RestoresHierarchy()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        var parent = world.Spawn("Parent")
            .With(new SerializablePosition { X = 0, Y = 0 })
            .Build();
        var child = world.Spawn("Child")
            .With(new SerializablePosition { X = 10, Y = 10 })
            .Build();
        world.SetParent(child, parent);

        world.SaveToSlot("slot1", serializer);
        world.Clear();

        var (_, _) = world.LoadFromSlot("slot1", serializer);

        var restoredParent = world.GetEntityByName("Parent");
        var restoredChild = world.GetEntityByName("Child");
        Assert.Equal(restoredParent, world.GetParent(restoredChild));
    }

    [Fact]
    public void LoadFromSlot_RestoresSingletons()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.SetSingleton(new SerializableGameTime { TotalTime = 123.456f, DeltaTime = 0.016f });
        world.Spawn().With(new SerializablePosition()).Build();

        world.SaveToSlot("slot1", serializer);
        world.Clear();

        world.LoadFromSlot("slot1", serializer);

        Assert.True(world.HasSingleton<SerializableGameTime>());
        ref var time = ref world.GetSingleton<SerializableGameTime>();
        Assert.Equal(123.456f, time.TotalTime);
    }

    [Fact]
    public void LoadFromSlot_WithNonExistentSlot_ThrowsFileNotFoundException()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };

        Assert.Throws<FileNotFoundException>(() =>
            world.LoadFromSlot("nonexistent", serializer));
    }

    #endregion

    #region Slot Management Tests

    [Fact]
    public void GetSaveSlotInfo_ReturnsInfo()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Spawn().With(new SerializablePosition()).Build();

        var options = new SaveSlotOptions { DisplayName = "Test Save" };
        world.SaveToSlot("slot1", serializer, options);

        var info = world.GetSaveSlotInfo("slot1");

        Assert.NotNull(info);
        Assert.Equal("slot1", info!.SlotName);
        Assert.Equal("Test Save", info.DisplayName);
    }

    [Fact]
    public void GetSaveSlotInfo_WithNonExistentSlot_ReturnsNull()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };

        var info = world.GetSaveSlotInfo("nonexistent");

        Assert.Null(info);
    }

    [Fact]
    public void ListSaveSlots_ReturnsAllSlots()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Spawn().With(new SerializablePosition()).Build();

        world.SaveToSlot("slot1", serializer);
        world.SaveToSlot("slot2", serializer);
        world.SaveToSlot("slot3", serializer);

        var slots = world.ListSaveSlots().ToList();

        Assert.Equal(3, slots.Count);
        Assert.Contains(slots, s => s.SlotName == "slot1");
        Assert.Contains(slots, s => s.SlotName == "slot2");
        Assert.Contains(slots, s => s.SlotName == "slot3");
    }

    [Fact]
    public void ListSaveSlots_WithNoSlots_ReturnsEmpty()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };

        var slots = world.ListSaveSlots().ToList();

        Assert.Empty(slots);
    }

    [Fact]
    public void SaveSlotExists_WithExistingSlot_ReturnsTrue()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Spawn().With(new SerializablePosition()).Build();
        world.SaveToSlot("slot1", serializer);

        Assert.True(world.SaveSlotExists("slot1"));
    }

    [Fact]
    public void SaveSlotExists_WithNonExistentSlot_ReturnsFalse()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };

        Assert.False(world.SaveSlotExists("nonexistent"));
    }

    [Fact]
    public void DeleteSaveSlot_RemovesFile()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Spawn().With(new SerializablePosition()).Build();
        world.SaveToSlot("slot1", serializer);

        Assert.True(world.SaveSlotExists("slot1"));

        var deleted = world.DeleteSaveSlot("slot1");

        Assert.True(deleted);
        Assert.False(world.SaveSlotExists("slot1"));
    }

    [Fact]
    public void DeleteSaveSlot_WithNonExistentSlot_ReturnsFalse()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };

        var deleted = world.DeleteSaveSlot("nonexistent");

        Assert.False(deleted);
    }

    [Fact]
    public void CopySaveSlot_CreatesCopy()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Spawn("Original").With(new SerializablePosition { X = 10, Y = 20 }).Build();
        world.SaveToSlot("source", serializer);

        var copiedInfo = world.CopySaveSlot("source", "destination");

        Assert.True(world.SaveSlotExists("source"));
        Assert.True(world.SaveSlotExists("destination"));
        Assert.Equal("destination", copiedInfo.SlotName);
    }

    [Fact]
    public void CopySaveSlot_WithNonExistentSource_ThrowsFileNotFoundException()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };

        Assert.Throws<FileNotFoundException>(() =>
            world.CopySaveSlot("nonexistent", "destination"));
    }

    [Fact]
    public void CopySaveSlot_WithExistingDestination_ThrowsIOException()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Spawn().With(new SerializablePosition()).Build();
        world.SaveToSlot("source", serializer);
        world.SaveToSlot("destination", serializer);

        Assert.Throws<IOException>(() =>
            world.CopySaveSlot("source", "destination"));
    }

    [Fact]
    public void CopySaveSlot_WithOverwrite_Overwrites()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Spawn("Source").With(new SerializablePosition { X = 100, Y = 200 }).Build();
        world.SaveToSlot("source", serializer);
        world.Spawn("Destination").With(new SerializablePosition { X = 1, Y = 2 }).Build();
        world.SaveToSlot("destination", serializer);

        var copiedInfo = world.CopySaveSlot("source", "destination", overwrite: true);

        Assert.Equal("destination", copiedInfo.SlotName);
    }

    [Fact]
    public void RenameSaveSlot_MovesSlot()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Spawn().With(new SerializablePosition()).Build();
        world.SaveToSlot("old_name", serializer);

        var renamedInfo = world.RenameSaveSlot("old_name", "new_name");

        Assert.False(world.SaveSlotExists("old_name"));
        Assert.True(world.SaveSlotExists("new_name"));
        Assert.Equal("new_name", renamedInfo.SlotName);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void ValidateSaveSlot_WithValidSlot_ReturnsValidInfo()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Spawn().With(new SerializablePosition()).Build();
        world.SaveToSlot("slot1", serializer);

        var validatedInfo = world.ValidateSaveSlot("slot1");

        Assert.NotNull(validatedInfo);
        Assert.True(validatedInfo!.IsValid);
        Assert.Null(validatedInfo.ValidationError);
    }

    [Fact]
    public void ValidateSaveSlot_WithNonExistentSlot_ReturnsNull()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };

        var validatedInfo = world.ValidateSaveSlot("nonexistent");

        Assert.Null(validatedInfo);
    }

    #endregion

    #region SaveDirectory Tests

    [Fact]
    public void SaveDirectory_CanBeSet()
    {
        using var world = new World();
        var customDir = Path.Combine(testSaveDirectory, "custom");

        world.SaveDirectory = customDir;

        Assert.Equal(customDir, world.SaveDirectory);
    }

    [Fact]
    public void GetSaveSlotPath_ReturnsCorrectPath()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };

        var path = world.GetSaveSlotPath("slot1");

        Assert.Equal(Path.Combine(testSaveDirectory, "slot1.ksave"), path);
    }

    #endregion

    #region Compression Tests

    [Fact]
    public void SaveToSlot_WithBrotliCompression_Works()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        for (int i = 0; i < 100; i++)
        {
            world.Spawn($"Entity{i}")
                .With(new SerializablePosition { X = i, Y = i * 2 })
                .Build();
        }

        var options = SaveSlotOptions.Compact; // Uses Brotli
        var info = world.SaveToSlot("slot1", serializer, options);

        Assert.Equal(KeenEyes.Serialization.CompressionMode.Brotli, info.Compression);
        Assert.True(info.CompressedSize < info.UncompressedSize);

        // Verify we can load it back
        world.Clear();
        var (loadedInfo, _) = world.LoadFromSlot("slot1", serializer);
        Assert.Equal(100, world.GetAllEntities().Count());
    }

    [Fact]
    public void SaveToSlot_WithNoCompression_Works()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Spawn().With(new SerializablePosition()).Build();

        var options = SaveSlotOptions.Debug; // Uses no compression
        var info = world.SaveToSlot("slot1", serializer, options);

        Assert.Equal(KeenEyes.Serialization.CompressionMode.None, info.Compression);

        // Verify we can load it back
        world.Clear();
        world.LoadFromSlot("slot1", serializer);
        Assert.Single(world.GetAllEntities());
    }

    #endregion

    #region Async Tests

    [Fact]
    public async Task SaveToSlotAsync_CreatesFile()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Spawn("TestEntity")
            .With(new SerializablePosition { X = 10, Y = 20 })
            .Build();

        var info = await world.SaveToSlotAsync("slot1", serializer, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(File.Exists(world.GetSaveSlotPath("slot1")));
        Assert.Equal("slot1", info.SlotName);
    }

    [Fact]
    public async Task LoadFromSlotAsync_RestoresEntities()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Spawn("Player")
            .With(new SerializablePosition { X = 100, Y = 200 })
            .Build();

        await world.SaveToSlotAsync("slot1", serializer, cancellationToken: TestContext.Current.CancellationToken);
        world.Clear();

        var (info, entityMap) = await world.LoadFromSlotAsync("slot1", serializer, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Single(world.GetAllEntities());
        Assert.Single(entityMap);

        var player = world.GetEntityByName("Player");
        Assert.True(player.IsValid);
    }

    [Fact]
    public async Task GetSaveSlotInfoAsync_ReturnsInfo()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Spawn().With(new SerializablePosition()).Build();
        await world.SaveToSlotAsync("slot1", serializer, cancellationToken: TestContext.Current.CancellationToken);

        var info = await world.GetSaveSlotInfoAsync("slot1", TestContext.Current.CancellationToken);

        Assert.NotNull(info);
        Assert.Equal("slot1", info!.SlotName);
    }

    [Fact]
    public async Task ListSaveSlotsAsync_ReturnsAllSlots()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Spawn().With(new SerializablePosition()).Build();

        await world.SaveToSlotAsync("slot1", serializer, cancellationToken: TestContext.Current.CancellationToken);
        await world.SaveToSlotAsync("slot2", serializer, cancellationToken: TestContext.Current.CancellationToken);

        var slots = await world.ListSaveSlotsAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, slots.Count);
    }

    [Fact]
    public async Task DeleteSaveSlotAsync_RemovesFile()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Spawn().With(new SerializablePosition()).Build();
        await world.SaveToSlotAsync("slot1", serializer, cancellationToken: TestContext.Current.CancellationToken);

        var deleted = await world.DeleteSaveSlotAsync("slot1", TestContext.Current.CancellationToken);

        Assert.True(deleted);
        Assert.False(world.SaveSlotExists("slot1"));
    }

    [Fact]
    public async Task CopySaveSlotAsync_CreatesCopy()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Spawn().With(new SerializablePosition()).Build();
        await world.SaveToSlotAsync("source", serializer, cancellationToken: TestContext.Current.CancellationToken);

        var copiedInfo = await world.CopySaveSlotAsync("source", "destination", cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(world.SaveSlotExists("source"));
        Assert.True(world.SaveSlotExists("destination"));
        Assert.Equal("destination", copiedInfo.SlotName);
    }

    [Fact]
    public async Task ValidateSaveSlotAsync_ReturnsValidInfo()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Spawn().With(new SerializablePosition()).Build();
        await world.SaveToSlotAsync("slot1", serializer, cancellationToken: TestContext.Current.CancellationToken);

        var validatedInfo = await world.ValidateSaveSlotAsync("slot1", TestContext.Current.CancellationToken);

        Assert.NotNull(validatedInfo);
        Assert.True(validatedInfo!.IsValid);
    }

    [Fact]
    public async Task SaveToSlotAsync_SupportsCancellation()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.Spawn().With(new SerializablePosition()).Build();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await world.SaveToSlotAsync("slot1", serializer, cancellationToken: cts.Token));
    }

    #endregion
}
