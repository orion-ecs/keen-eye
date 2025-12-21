using System.Text.Json;
using KeenEyes.Persistence.Encryption;
using KeenEyes.Serialization;

namespace KeenEyes.Persistence.Tests;

/// <summary>
/// Additional tests for EncryptedPersistenceApi to improve coverage.
/// </summary>
public class EncryptedPersistenceApiAdditionalTests : IDisposable
{
    private readonly string testSaveDirectory;
    private readonly TestPersistenceSerializer serializer;

    public EncryptedPersistenceApiAdditionalTests()
    {
        testSaveDirectory = Path.Combine(Path.GetTempPath(), $"keen_eye_persistence_additional_tests_{Guid.NewGuid():N}");
        serializer = new TestPersistenceSerializer()
            .WithComponent<TestPosition>()
            .WithComponent<TestVelocity>()
            .WithComponent<TestHealth>();
    }

    public void Dispose()
    {
        if (Directory.Exists(testSaveDirectory))
        {
            Directory.Delete(testSaveDirectory, recursive: true);
        }
    }

    #region JSON Format Tests

    [Fact]
    public void SaveToSlot_JsonFormat_CreatesValidFile()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.InstallPlugin(new PersistencePlugin());
        world.Spawn("Player")
            .With(new TestPosition { X = 10, Y = 20 })
            .With(new TestVelocity { X = 1, Y = 2 })
            .Build();

        var api = world.GetExtension<EncryptedPersistenceApi>();
        var options = new SaveSlotOptions { Format = SaveFormat.Json };
        var info = api.SaveToSlot("json_slot", serializer, options);

        Assert.Equal(SaveFormat.Json, info.Format);
        Assert.True(File.Exists(api.GetSlotFilePath("json_slot")));
    }

    [Fact]
    public void LoadFromSlot_JsonFormat_RestoresEntities()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.InstallPlugin(new PersistencePlugin());
        world.Spawn("Player")
            .With(new TestPosition { X = 100, Y = 200 })
            .With(new TestHealth { Current = 50, Max = 100 })
            .Build();

        var api = world.GetExtension<EncryptedPersistenceApi>();
        var options = new SaveSlotOptions { Format = SaveFormat.Json };
        api.SaveToSlot("json_load_slot", serializer, options);
        world.Clear();

        var (slotInfo, entityMap) = api.LoadFromSlot("json_load_slot", serializer);

        Assert.Equal(SaveFormat.Json, slotInfo.Format);
        var player = world.GetEntityByName("Player");
        Assert.True(player.IsValid);
        Assert.Equal(100, world.Get<TestPosition>(player).X);
        Assert.Equal(50, world.Get<TestHealth>(player).Current);
    }

    [Fact]
    public async Task SaveToSlotAsync_JsonFormat_CreatesValidFile()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.InstallPlugin(new PersistencePlugin());
        world.Spawn("AsyncEntity")
            .With(new TestPosition { X = 15, Y = 25 })
            .Build();

        var api = world.GetExtension<EncryptedPersistenceApi>();
        var options = new SaveSlotOptions { Format = SaveFormat.Json };
        var info = await api.SaveToSlotAsync("async_json_slot", serializer, options, TestContext.Current.CancellationToken);

        Assert.Equal(SaveFormat.Json, info.Format);
        Assert.True(File.Exists(api.GetSlotFilePath("async_json_slot")));
    }

    [Fact]
    public async Task LoadFromSlotAsync_JsonFormat_RestoresEntities()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.InstallPlugin(new PersistencePlugin());
        world.Spawn("AsyncPlayer")
            .With(new TestPosition { X = 99, Y = 88 })
            .With(new TestHealth { Current = 75, Max = 100 })
            .Build();

        var api = world.GetExtension<EncryptedPersistenceApi>();
        var options = new SaveSlotOptions { Format = SaveFormat.Json };
        await api.SaveToSlotAsync("async_json_load", serializer, options, TestContext.Current.CancellationToken);
        world.Clear();

        var (info, entityMap) = await api.LoadFromSlotAsync("async_json_load", serializer, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(SaveFormat.Json, info.Format);
        var player = world.GetEntityByName("AsyncPlayer");
        Assert.True(player.IsValid);
        Assert.Equal(99, world.Get<TestPosition>(player).X);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void LoadFromSlot_NonExistentFile_ThrowsFileNotFoundException()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.InstallPlugin(new PersistencePlugin());

        var api = world.GetExtension<EncryptedPersistenceApi>();

        var exception = Assert.Throws<FileNotFoundException>(() =>
            api.LoadFromSlot("nonexistent_slot", serializer));

        Assert.Contains("nonexistent_slot", exception.Message);
    }

    [Fact]
    public async Task LoadFromSlotAsync_NonExistentFile_ThrowsFileNotFoundException()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.InstallPlugin(new PersistencePlugin());

        var api = world.GetExtension<EncryptedPersistenceApi>();

        var exception = await Assert.ThrowsAsync<FileNotFoundException>(async () =>
            await api.LoadFromSlotAsync("nonexistent_async_slot", serializer, cancellationToken: TestContext.Current.CancellationToken));

        Assert.Contains("nonexistent_async_slot", exception.Message);
    }

    [Fact]
    public void GetSlotInfo_CorruptedFile_ReturnsNull()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.InstallPlugin(new PersistencePlugin());

        var api = world.GetExtension<EncryptedPersistenceApi>();
        var filePath = api.GetSlotFilePath("corrupted_slot");

        // Create directory if needed
        Directory.CreateDirectory(testSaveDirectory);

        // Write corrupted data
        File.WriteAllBytes(filePath, new byte[] { 0x00, 0x01, 0x02, 0x03 });

        var info = api.GetSlotInfo("corrupted_slot");

        Assert.Null(info);
    }

    [Fact]
    public void ListSlots_WithCorruptedFiles_SkipsInvalidFiles()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.InstallPlugin(new PersistencePlugin());
        world.Spawn("ValidEntity").With(new TestPosition()).Build();

        var api = world.GetExtension<EncryptedPersistenceApi>();

        // Save one valid file
        api.SaveToSlot("valid_slot", serializer);

        // Create a corrupted file
        var corruptedPath = Path.Combine(testSaveDirectory, "corrupted.ksave");
        File.WriteAllBytes(corruptedPath, new byte[] { 0xFF, 0xFE });

        var slots = api.ListSlots().ToList();

        // Should only return the valid slot
        Assert.Single(slots);
        Assert.Equal("valid_slot", slots[0].SlotName);
    }

    #endregion
}
