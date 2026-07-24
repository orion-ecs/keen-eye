using KeenEyes.Serialization;

namespace KeenEyes.Persistence.Tests;

/// <summary>
/// Tests that <see cref="EncryptedPersistenceApi"/> reports slot sizes that match the
/// actual bytes written to disk (regression guard for #1166).
/// </summary>
public class EncryptedSlotSizeTests : IDisposable
{
    private readonly string testSaveDirectory;
    private readonly TestPersistenceSerializer serializer;

    public EncryptedSlotSizeTests()
    {
        testSaveDirectory = Path.Combine(Path.GetTempPath(), $"keen_eye_slot_size_tests_{Guid.NewGuid():N}");
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

    private World CreateEncryptedWorld()
    {
        var world = new World { SaveDirectory = testSaveDirectory };
        var config = PersistenceConfig.WithEncryption("test-password", testSaveDirectory);
        world.InstallPlugin(new PersistencePlugin(config));
        world.Spawn("Player")
            .With(new TestPosition { X = 10, Y = 20 })
            .With(new TestVelocity { X = 1, Y = 2 })
            .With(new TestHealth { Current = 50, Max = 100 })
            .Build();
        return world;
    }

    [Fact]
    public void SaveToSlot_Encrypted_ReturnedSizesMatchPersistedSlotInfo()
    {
        using var world = CreateEncryptedWorld();
        var api = world.GetExtension<EncryptedPersistenceApi>();

        // Default options use AES encryption + GZip compression, so both the
        // encrypted length and the compressed length differ from the plaintext length.
        var returned = api.SaveToSlot("encrypted_slot", serializer);
        var persisted = api.GetSlotInfo("encrypted_slot");

        persisted.ShouldNotBeNull();
        returned.UncompressedSize.ShouldBe(persisted.UncompressedSize);
        returned.CompressedSize.ShouldBe(persisted.CompressedSize);
    }

    [Fact]
    public void SaveToSlot_Encrypted_UncompressedSizeReflectsEncryptedBytes()
    {
        using var world = CreateEncryptedWorld();
        var api = world.GetExtension<EncryptedPersistenceApi>();

        var returned = api.SaveToSlot("encrypted_slot", serializer);

        // The container's UncompressedSize is the length of the encrypted payload handed
        // to the writer, which (AES adds an IV and block padding) exceeds the plaintext
        // snapshot length. Before the fix the returned value was the plaintext length.
        var plaintextLength = SnapshotManager.ToBinary(
            SnapshotManager.CreateSnapshot(world, serializer),
            serializer).Length;

        returned.UncompressedSize.ShouldBeGreaterThan(plaintextLength);
    }

    [Fact]
    public async Task SaveToSlotAsync_Encrypted_ReturnedSizesMatchPersistedSlotInfo()
    {
        using var world = CreateEncryptedWorld();
        var api = world.GetExtension<EncryptedPersistenceApi>();

        var returned = await api.SaveToSlotAsync(
            "encrypted_async_slot",
            serializer,
            cancellationToken: TestContext.Current.CancellationToken);
        var persisted = api.GetSlotInfo("encrypted_async_slot");

        persisted.ShouldNotBeNull();
        returned.UncompressedSize.ShouldBe(persisted.UncompressedSize);
        returned.CompressedSize.ShouldBe(persisted.CompressedSize);
    }
}
