using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using KeenEyes.Capabilities;
using KeenEyes.Persistence;
using KeenEyes.Persistence.Encryption;
using KeenEyes.Serialization;
using KeenEyes.Testing.Encryption;

namespace KeenEyes.Persistence.Tests;

/// <summary>
/// Test components for persistence tests.
/// </summary>
public struct TestPosition : IComponent
{
    public float X;
    public float Y;
}

public struct TestVelocity : IComponent
{
    public float X;
    public float Y;
}

public struct TestHealth : IComponent
{
    public int Current;
    public int Max;
}

/// <summary>
/// Tests for the PersistencePlugin.
/// </summary>
public class PersistencePluginTests : IDisposable
{
    private readonly string testSaveDirectory;
    private readonly TestPersistenceSerializer serializer;

    public PersistencePluginTests()
    {
        testSaveDirectory = Path.Combine(Path.GetTempPath(), $"keen_eye_persistence_tests_{Guid.NewGuid():N}");
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

    #region Plugin Installation Tests

    [Fact]
    public void Install_RegistersEncryptedPersistenceApi()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };

        world.InstallPlugin(new PersistencePlugin());

        var api = world.GetExtension<EncryptedPersistenceApi>();
        Assert.NotNull(api);
    }

    [Fact]
    public void Install_WithConfig_UsesConfiguredDirectory()
    {
        using var world = new World();
        var customDir = Path.Combine(testSaveDirectory, "custom");
        var config = new PersistenceConfig { SaveDirectory = customDir };

        world.InstallPlugin(new PersistencePlugin(config));

        var api = world.GetExtension<EncryptedPersistenceApi>();
        Assert.Equal(customDir, api.SaveDirectory);
    }

    [Fact]
    public void Install_WithEncryption_EnablesEncryption()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        var config = PersistenceConfig.WithEncryption("testPassword");

        world.InstallPlugin(new PersistencePlugin(config));

        var api = world.GetExtension<EncryptedPersistenceApi>();
        Assert.True(api.IsEncryptionEnabled);
        Assert.Equal("AES-256", api.EncryptionProviderName);
    }

    [Fact]
    public void Install_WithoutEncryption_DisablesEncryption()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };

        world.InstallPlugin(new PersistencePlugin());

        var api = world.GetExtension<EncryptedPersistenceApi>();
        Assert.False(api.IsEncryptionEnabled);
        Assert.Equal("None", api.EncryptionProviderName);
    }

    [Fact]
    public void Uninstall_RemovesApi()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.InstallPlugin(new PersistencePlugin());

        world.UninstallPlugin<PersistencePlugin>();

        Assert.False(world.TryGetExtension<EncryptedPersistenceApi>(out _));
    }

    #endregion

    #region Unencrypted Save/Load Tests

    [Fact]
    public void SaveToSlot_WithoutEncryption_CreatesFile()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.InstallPlugin(new PersistencePlugin());
        world.Spawn("TestEntity")
            .With(new TestPosition { X = 10, Y = 20 })
            .Build();

        var api = world.GetExtension<EncryptedPersistenceApi>();
        var info = api.SaveToSlot("slot1", serializer);

        Assert.True(File.Exists(api.GetSlotFilePath("slot1")));
        Assert.Equal("slot1", info.SlotName);
    }

    [Fact]
    public void LoadFromSlot_WithoutEncryption_RestoresEntities()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.InstallPlugin(new PersistencePlugin());
        world.Spawn("Player")
            .With(new TestPosition { X = 100, Y = 200 })
            .Build();

        var api = world.GetExtension<EncryptedPersistenceApi>();
        api.SaveToSlot("slot1", serializer);
        world.Clear();

        var (_, entityMap) = api.LoadFromSlot("slot1", serializer);

        Assert.Single(world.GetAllEntities());
        var player = world.GetEntityByName("Player");
        Assert.True(player.IsValid);
    }

    #endregion

    #region Encrypted Save/Load Tests

    [Fact]
    public void SaveToSlot_WithEncryption_CreatesEncryptedFile()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        var config = PersistenceConfig.WithEncryption("secret123");
        world.InstallPlugin(new PersistencePlugin(config));
        world.Spawn("TestEntity")
            .With(new TestPosition { X = 10, Y = 20 })
            .Build();

        var api = world.GetExtension<EncryptedPersistenceApi>();
        api.SaveToSlot("encrypted_slot", serializer);

        Assert.True(File.Exists(api.GetSlotFilePath("encrypted_slot")));
    }

    [Fact]
    public void LoadFromSlot_WithCorrectPassword_RestoresEntities()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        var config = PersistenceConfig.WithEncryption("mySecret");
        world.InstallPlugin(new PersistencePlugin(config));
        world.Spawn("Player")
            .With(new TestPosition { X = 50, Y = 100 })
            .With(new TestHealth { Current = 80, Max = 100 })
            .Build();

        var api = world.GetExtension<EncryptedPersistenceApi>();
        api.SaveToSlot("slot1", serializer);
        world.Clear();

        var (_, entityMap) = api.LoadFromSlot("slot1", serializer);

        var player = world.GetEntityByName("Player");
        Assert.True(player.IsValid);
        Assert.Equal(50, world.Get<TestPosition>(player).X);
    }

    [Fact]
    public void LoadFromSlot_WithWrongPassword_ThrowsCryptographicException()
    {
        // Save with one password
        using var world1 = new World { SaveDirectory = testSaveDirectory };
        var config1 = PersistenceConfig.WithEncryption("correctPassword");
        world1.InstallPlugin(new PersistencePlugin(config1));
        world1.Spawn("Player").With(new TestPosition { X = 10, Y = 20 }).Build();
        world1.GetExtension<EncryptedPersistenceApi>().SaveToSlot("slot1", serializer);

        // Try to load with different password
        using var world2 = new World { SaveDirectory = testSaveDirectory };
        var config2 = PersistenceConfig.WithEncryption("wrongPassword");
        world2.InstallPlugin(new PersistencePlugin(config2));

        var api = world2.GetExtension<EncryptedPersistenceApi>();
        Assert.Throws<CryptographicException>(() => api.LoadFromSlot("slot1", serializer));
    }

    [Fact]
    public void EncryptedSave_DifferentFromUnencrypted()
    {
        // Save without encryption
        using var world1 = new World { SaveDirectory = testSaveDirectory };
        world1.InstallPlugin(new PersistencePlugin());
        world1.Spawn("Player").With(new TestPosition { X = 10, Y = 20 }).Build();
        world1.GetExtension<EncryptedPersistenceApi>().SaveToSlot("unencrypted", serializer);

        // Save with encryption
        using var world2 = new World { SaveDirectory = testSaveDirectory };
        var encryptedConfig = PersistenceConfig.WithEncryption("password");
        world2.InstallPlugin(new PersistencePlugin(encryptedConfig));
        world2.Spawn("Player").With(new TestPosition { X = 10, Y = 20 }).Build();
        world2.GetExtension<EncryptedPersistenceApi>().SaveToSlot("encrypted", serializer);

        var unencryptedFile = File.ReadAllBytes(Path.Combine(testSaveDirectory, "unencrypted.ksave"));
        var encryptedFile = File.ReadAllBytes(Path.Combine(testSaveDirectory, "encrypted.ksave"));

        // Files should have different content (encryption applied)
        Assert.NotEqual(unencryptedFile, encryptedFile);
    }

    #endregion

    #region Slot Management Tests

    [Fact]
    public void SlotExists_ExistingSlot_ReturnsTrue()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.InstallPlugin(new PersistencePlugin());
        world.Spawn().With(new TestPosition()).Build();

        var api = world.GetExtension<EncryptedPersistenceApi>();
        api.SaveToSlot("slot1", serializer);

        Assert.True(api.SlotExists("slot1"));
    }

    [Fact]
    public void SlotExists_NonExistingSlot_ReturnsFalse()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.InstallPlugin(new PersistencePlugin());

        var api = world.GetExtension<EncryptedPersistenceApi>();
        Assert.False(api.SlotExists("nonexistent"));
    }

    [Fact]
    public void GetSlotInfo_ExistingSlot_ReturnsInfo()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.InstallPlugin(new PersistencePlugin());
        world.Spawn().With(new TestPosition()).Build();

        var api = world.GetExtension<EncryptedPersistenceApi>();
        api.SaveToSlot("slot1", serializer);

        var info = api.GetSlotInfo("slot1");
        Assert.NotNull(info);
        Assert.Equal("slot1", info!.SlotName);
    }

    [Fact]
    public void DeleteSlot_ExistingSlot_RemovesFile()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.InstallPlugin(new PersistencePlugin());
        world.Spawn().With(new TestPosition()).Build();

        var api = world.GetExtension<EncryptedPersistenceApi>();
        api.SaveToSlot("slot1", serializer);

        var deleted = api.DeleteSlot("slot1");

        Assert.True(deleted);
        Assert.False(api.SlotExists("slot1"));
    }

    [Fact]
    public void ListSlots_ReturnsAllSlots()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        world.InstallPlugin(new PersistencePlugin());
        world.Spawn().With(new TestPosition()).Build();

        var api = world.GetExtension<EncryptedPersistenceApi>();
        api.SaveToSlot("slot1", serializer);
        api.SaveToSlot("slot2", serializer);
        api.SaveToSlot("slot3", serializer);

        var slots = api.ListSlots().ToList();
        Assert.Equal(3, slots.Count);
    }

    #endregion

    #region Async Tests

    [Fact]
    public async Task SaveToSlotAsync_WithEncryption_CreatesFile()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        var config = PersistenceConfig.WithEncryption("asyncTest");
        world.InstallPlugin(new PersistencePlugin(config));
        world.Spawn("TestEntity")
            .With(new TestPosition { X = 10, Y = 20 })
            .Build();

        var api = world.GetExtension<EncryptedPersistenceApi>();
        await api.SaveToSlotAsync("async_slot", serializer, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(File.Exists(api.GetSlotFilePath("async_slot")));
    }

    [Fact]
    public async Task LoadFromSlotAsync_WithEncryption_RestoresEntities()
    {
        using var world = new World { SaveDirectory = testSaveDirectory };
        var config = PersistenceConfig.WithEncryption("asyncTest");
        world.InstallPlugin(new PersistencePlugin(config));
        world.Spawn("Player")
            .With(new TestPosition { X = 100, Y = 200 })
            .Build();

        var api = world.GetExtension<EncryptedPersistenceApi>();
        await api.SaveToSlotAsync("slot1", serializer, cancellationToken: TestContext.Current.CancellationToken);
        world.Clear();

        var (info, entityMap) = await api.LoadFromSlotAsync("slot1", serializer, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Single(world.GetAllEntities());
    }

    #endregion

    #region MockEncryptionProvider Tests

    [Fact]
    public void SaveToSlot_WithMockEncryption_TracksEncryptionCall()
    {
        var mockEncryption = new MockEncryptionProvider { Mode = MockEncryptionMode.Reversible };
        var config = new PersistenceConfig
        {
            EncryptionProvider = mockEncryption,
            SaveDirectory = testSaveDirectory
        };

        using var world = new World();
        world.InstallPlugin(new PersistencePlugin(config));
        world.Spawn("TestEntity")
            .With(new TestPosition { X = 10, Y = 20 })
            .Build();

        var api = world.GetExtension<EncryptedPersistenceApi>();
        api.SaveToSlot("mock_slot", serializer);

        // Verify encryption was called using fluent assertions
        mockEncryption
            .ShouldHaveEncrypted()
            .ShouldHaveEncryptedTimes(1)
            .ShouldBeEncrypting();
    }

    [Fact]
    public void LoadFromSlot_WithMockEncryption_TracksDecryptionCall()
    {
        var mockEncryption = new MockEncryptionProvider { Mode = MockEncryptionMode.Reversible };
        var config = new PersistenceConfig
        {
            EncryptionProvider = mockEncryption,
            SaveDirectory = testSaveDirectory
        };

        using var world = new World();
        world.InstallPlugin(new PersistencePlugin(config));
        world.Spawn("Player")
            .With(new TestPosition { X = 100, Y = 200 })
            .Build();

        var api = world.GetExtension<EncryptedPersistenceApi>();
        api.SaveToSlot("mock_slot", serializer);
        mockEncryption.ClearOperations(); // Reset counts

        world.Clear();
        api.LoadFromSlot("mock_slot", serializer);

        // Verify decryption was called
        mockEncryption
            .ShouldHaveDecrypted()
            .ShouldHaveDecryptedTimes(1)
            .ShouldNotHaveEncrypted(); // No encryption after clear
    }

    [Fact]
    public void SaveToSlot_WithMockEncryption_EncryptsData()
    {
        var mockEncryption = new MockEncryptionProvider { Mode = MockEncryptionMode.Reversible };
        var config = new PersistenceConfig
        {
            EncryptionProvider = mockEncryption,
            SaveDirectory = testSaveDirectory
        };

        using var world = new World();
        world.InstallPlugin(new PersistencePlugin(config));
        world.Spawn("TestEntity")
            .With(new TestPosition { X = 10, Y = 20 })
            .Build();

        var api = world.GetExtension<EncryptedPersistenceApi>();
        api.SaveToSlot("data_slot", serializer);

        // Verify data was encrypted (should have some size)
        mockEncryption.ShouldHaveEncryptedDataOfSize(10);
    }

    [Fact]
    public void SaveAndLoad_WithMockEncryption_RoundTripsData()
    {
        var mockEncryption = new MockEncryptionProvider { Mode = MockEncryptionMode.Reversible };
        var config = new PersistenceConfig
        {
            EncryptionProvider = mockEncryption,
            SaveDirectory = testSaveDirectory
        };

        using var world = new World();
        world.InstallPlugin(new PersistencePlugin(config));
        world.Spawn("Player")
            .With(new TestPosition { X = 42, Y = 84 })
            .With(new TestHealth { Current = 75, Max = 100 })
            .Build();

        var api = world.GetExtension<EncryptedPersistenceApi>();
        api.SaveToSlot("roundtrip_slot", serializer);
        world.Clear();

        api.LoadFromSlot("roundtrip_slot", serializer);

        // Verify data was restored correctly
        var player = world.GetEntityByName("Player");
        Assert.True(player.IsValid);
        Assert.Equal(42, world.Get<TestPosition>(player).X);
        Assert.Equal(84, world.Get<TestPosition>(player).Y);
        Assert.Equal(75, world.Get<TestHealth>(player).Current);

        // Verify both operations occurred
        mockEncryption
            .ShouldHaveEncryptedTimes(1)
            .ShouldHaveDecryptedTimes(1)
            .ShouldHaveTotalOperations(2);
    }

    [Fact]
    public void SaveToSlot_WithEncryptionFailure_ThrowsException()
    {
        var mockEncryption = new MockEncryptionProvider
        {
            Mode = MockEncryptionMode.Reversible,
            ShouldFailEncrypt = true
        };
        var config = new PersistenceConfig
        {
            EncryptionProvider = mockEncryption,
            SaveDirectory = testSaveDirectory
        };

        using var world = new World();
        world.InstallPlugin(new PersistencePlugin(config));
        world.Spawn("TestEntity")
            .With(new TestPosition { X = 10, Y = 20 })
            .Build();

        var api = world.GetExtension<EncryptedPersistenceApi>();

        Assert.Throws<InvalidOperationException>(() => api.SaveToSlot("fail_slot", serializer));
    }

    [Fact]
    public void LoadFromSlot_WithDecryptionFailure_ThrowsCryptographicException()
    {
        // First save with working encryption
        var mockEncryption = new MockEncryptionProvider { Mode = MockEncryptionMode.Reversible };
        var config = new PersistenceConfig
        {
            EncryptionProvider = mockEncryption,
            SaveDirectory = testSaveDirectory
        };

        using var world = new World();
        world.InstallPlugin(new PersistencePlugin(config));
        world.Spawn("TestEntity")
            .With(new TestPosition { X = 10, Y = 20 })
            .Build();

        var api = world.GetExtension<EncryptedPersistenceApi>();
        api.SaveToSlot("decrypt_fail_slot", serializer);
        world.Clear();

        // Now make decryption fail
        mockEncryption.ShouldFailDecrypt = true;

        Assert.Throws<CryptographicException>(() => api.LoadFromSlot("decrypt_fail_slot", serializer));
    }

    [Fact]
    public void SaveToSlot_WithCustomEncryption_UsesCustomFunction()
    {
        var encryptedValues = new List<byte[]>();
        var mockEncryption = new MockEncryptionProvider
        {
            Mode = MockEncryptionMode.Tracking,
            CustomEncrypt = data =>
            {
                // Custom encryption: reverse bytes
                var reversed = data.Reverse().ToArray();
                encryptedValues.Add(reversed);
                return reversed;
            },
            CustomDecrypt = data =>
            {
                // Custom decryption: reverse bytes back
                return data.Reverse().ToArray();
            }
        };
        var config = new PersistenceConfig
        {
            EncryptionProvider = mockEncryption,
            SaveDirectory = testSaveDirectory
        };

        using var world = new World();
        world.InstallPlugin(new PersistencePlugin(config));
        world.Spawn("TestEntity")
            .With(new TestPosition { X = 10, Y = 20 })
            .Build();

        var api = world.GetExtension<EncryptedPersistenceApi>();
        api.SaveToSlot("custom_slot", serializer);

        Assert.Single(encryptedValues);
        Assert.True(encryptedValues[0].Length > 0);
    }

    [Fact]
    public void SaveToSlot_WithPassThroughMode_DataNotEncrypted()
    {
        var mockEncryption = new MockEncryptionProvider { Mode = MockEncryptionMode.PassThrough };
        var config = new PersistenceConfig
        {
            EncryptionProvider = mockEncryption,
            SaveDirectory = testSaveDirectory
        };

        using var world = new World();
        world.InstallPlugin(new PersistencePlugin(config));
        world.Spawn("TestEntity")
            .With(new TestPosition { X = 10, Y = 20 })
            .Build();

        var api = world.GetExtension<EncryptedPersistenceApi>();

        // PassThrough mode reports IsEncrypted = false
        Assert.False(api.IsEncryptionEnabled);
        Assert.Equal("MockEncryptionProvider", api.EncryptionProviderName);
    }

    [Fact]
    public void MultipleSaves_WithMockEncryption_TracksAllOperations()
    {
        var mockEncryption = new MockEncryptionProvider { Mode = MockEncryptionMode.Reversible };
        var config = new PersistenceConfig
        {
            EncryptionProvider = mockEncryption,
            SaveDirectory = testSaveDirectory
        };

        using var world = new World();
        world.InstallPlugin(new PersistencePlugin(config));
        world.Spawn("TestEntity")
            .With(new TestPosition { X = 10, Y = 20 })
            .Build();

        var api = world.GetExtension<EncryptedPersistenceApi>();

        // Save multiple times
        api.SaveToSlot("slot1", serializer);
        api.SaveToSlot("slot2", serializer);
        api.SaveToSlot("slot3", serializer);

        mockEncryption
            .ShouldHaveEncryptedTimes(3)
            .ShouldHaveTotalOperations(3);
    }

    [Fact]
    public async Task SaveToSlotAsync_WithMockEncryption_TracksAsyncEncryption()
    {
        var mockEncryption = new MockEncryptionProvider { Mode = MockEncryptionMode.Reversible };
        var config = new PersistenceConfig
        {
            EncryptionProvider = mockEncryption,
            SaveDirectory = testSaveDirectory
        };

        using var world = new World();
        world.InstallPlugin(new PersistencePlugin(config));
        world.Spawn("TestEntity")
            .With(new TestPosition { X = 10, Y = 20 })
            .Build();

        var api = world.GetExtension<EncryptedPersistenceApi>();
        await api.SaveToSlotAsync("async_mock_slot", serializer, cancellationToken: TestContext.Current.CancellationToken);

        mockEncryption
            .ShouldHaveEncrypted()
            .ShouldHaveEncryptedTimes(1);
    }

    [Fact]
    public async Task LoadFromSlotAsync_WithMockEncryption_TracksAsyncDecryption()
    {
        var mockEncryption = new MockEncryptionProvider { Mode = MockEncryptionMode.Reversible };
        var config = new PersistenceConfig
        {
            EncryptionProvider = mockEncryption,
            SaveDirectory = testSaveDirectory
        };

        using var world = new World();
        world.InstallPlugin(new PersistencePlugin(config));
        world.Spawn("Player")
            .With(new TestPosition { X = 100, Y = 200 })
            .Build();

        var api = world.GetExtension<EncryptedPersistenceApi>();
        await api.SaveToSlotAsync("async_load_slot", serializer, cancellationToken: TestContext.Current.CancellationToken);
        mockEncryption.ClearOperations();

        world.Clear();
        await api.LoadFromSlotAsync("async_load_slot", serializer, cancellationToken: TestContext.Current.CancellationToken);

        mockEncryption
            .ShouldHaveDecrypted()
            .ShouldHaveDecryptedTimes(1);
    }

    [Fact]
    public void MockEncryption_Reset_ClearsAllState()
    {
        var mockEncryption = new MockEncryptionProvider { Mode = MockEncryptionMode.Reversible };
        var config = new PersistenceConfig
        {
            EncryptionProvider = mockEncryption,
            SaveDirectory = testSaveDirectory
        };

        using var world = new World();
        world.InstallPlugin(new PersistencePlugin(config));
        world.Spawn("TestEntity")
            .With(new TestPosition { X = 10, Y = 20 })
            .Build();

        var api = world.GetExtension<EncryptedPersistenceApi>();
        api.SaveToSlot("reset_slot", serializer);

        Assert.Equal(1, mockEncryption.EncryptCount);

        mockEncryption.Reset();

        Assert.Equal(0, mockEncryption.EncryptCount);
        Assert.Equal(0, mockEncryption.DecryptCount);
        Assert.Empty(mockEncryption.Operations);
        Assert.Equal(MockEncryptionMode.PassThrough, mockEncryption.Mode);
    }

    #endregion
}

#region Test Serializer

/// <summary>
/// A mock IComponentSerializer for persistence testing.
/// </summary>
internal sealed class TestPersistenceSerializer : IComponentSerializer, IBinaryComponentSerializer
{
    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        IncludeFields = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly Dictionary<string, Type> typeMap = [];
    private readonly Dictionary<Type, Func<JsonElement, object>> deserializers = [];
    private readonly Dictionary<Type, Func<object, JsonElement>> serializers = [];
    private readonly Dictionary<Type, Func<ISerializationCapability, bool, ComponentInfo>> registrars = [];
    private readonly Dictionary<Type, Action<ISerializationCapability, object>> singletonSetters = [];
    private readonly Dictionary<string, Func<BinaryReader, object>> binaryDeserializers = [];
    private readonly Dictionary<Type, Action<object, BinaryWriter>> binarySerializers = [];

    public TestPersistenceSerializer WithComponent<T>(string? typeName = null) where T : struct, IComponent
    {
        var type = typeof(T);
        var name = typeName ?? type.AssemblyQualifiedName ?? type.FullName ?? type.Name;
        typeMap[name] = type;

        if (type.FullName is not null && type.FullName != name)
        {
            typeMap[type.FullName] = type;
        }
        if (type.Name != name)
        {
            typeMap[type.Name] = type;
        }

        deserializers[type] = json => JsonSerializer.Deserialize<T>(json.GetRawText(), jsonOptions)!;
        serializers[type] = obj =>
        {
            var jsonStr = JsonSerializer.Serialize((T)obj, jsonOptions);
            using var doc = JsonDocument.Parse(jsonStr);
            return doc.RootElement.Clone();
        };
        registrars[type] = (serialization, isTag) => serialization.Components.Register<T>(isTag);
        singletonSetters[type] = (serialization, value) => serialization.SetSingleton((T)value);

        binaryDeserializers[name] = reader =>
        {
            var json = reader.ReadString();
            return JsonSerializer.Deserialize<T>(json, jsonOptions)!;
        };
        if (type.FullName is not null)
        {
            binaryDeserializers[type.FullName] = binaryDeserializers[name];
        }
        binarySerializers[type] = (obj, writer) =>
        {
            var json = JsonSerializer.Serialize((T)obj, jsonOptions);
            writer.Write(json);
        };

        return this;
    }

    public bool IsSerializable(Type type) => deserializers.ContainsKey(type);
    public bool IsSerializable(string typeName) => typeMap.ContainsKey(typeName);

    public object? Deserialize(string typeName, JsonElement json)
    {
        if (typeMap.TryGetValue(typeName, out var type) &&
            deserializers.TryGetValue(type, out var deserializer))
        {
            return deserializer(json);
        }
        return null;
    }

    public JsonElement? Serialize(Type type, object value)
    {
        if (serializers.TryGetValue(type, out var serializer))
        {
            return serializer(value);
        }
        return null;
    }

    public Type? GetType(string typeName)
    {
        return typeMap.TryGetValue(typeName, out var type) ? type : null;
    }

    public ComponentInfo? RegisterComponent(ISerializationCapability serialization, string typeName, bool isTag)
    {
        if (typeMap.TryGetValue(typeName, out var type) &&
            registrars.TryGetValue(type, out var registrar))
        {
            return registrar(serialization, isTag);
        }
        return null;
    }

    public bool SetSingleton(ISerializationCapability serialization, string typeName, object value)
    {
        if (typeMap.TryGetValue(typeName, out var type) &&
            singletonSetters.TryGetValue(type, out var setter))
        {
            setter(serialization, value);
            return true;
        }
        return false;
    }

    public bool WriteTo(Type type, object value, BinaryWriter writer)
    {
        if (binarySerializers.TryGetValue(type, out var serializer))
        {
            serializer(value, writer);
            return true;
        }
        return false;
    }

    public object? ReadFrom(string typeName, BinaryReader reader)
    {
        if (binaryDeserializers.TryGetValue(typeName, out var deserializer))
        {
            return deserializer(reader);
        }
        return null;
    }

    public object? CreateDefault(string typeName)
    {
        if (typeMap.TryGetValue(typeName, out var type))
        {
            return Activator.CreateInstance(type);
        }
        return null;
    }
}

#endregion
