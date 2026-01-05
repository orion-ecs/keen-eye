using System.Text.Json;
using KeenEyes.Serialization;

namespace KeenEyes.Tests;

/// <summary>
/// Tests for the component migration pipeline.
/// </summary>
public class MigrationTests
{
    #region IComponentMigrator Interface Tests

    [Fact]
    public void CanMigrate_WithNoMigrations_ReturnsFalse()
    {
        var migrator = new TestMigrator([]);

        var result = migrator.CanMigrate(typeof(SerializablePosition), 1, 2);

        Assert.False(result);
    }

    [Fact]
    public void CanMigrate_WithEqualVersions_ReturnsFalse()
    {
        var migrator = new TestMigrator(new Dictionary<Type, Dictionary<int, Func<JsonElement, object>>>
        {
            [typeof(SerializablePosition)] = new()
            {
                [1] = json => new SerializablePosition()
            }
        });

        var result = migrator.CanMigrate(typeof(SerializablePosition), 2, 2);

        Assert.False(result);
    }

    [Fact]
    public void CanMigrate_WithHigherFromVersion_ReturnsFalse()
    {
        var migrator = new TestMigrator(new Dictionary<Type, Dictionary<int, Func<JsonElement, object>>>
        {
            [typeof(SerializablePosition)] = new()
            {
                [1] = json => new SerializablePosition()
            }
        });

        var result = migrator.CanMigrate(typeof(SerializablePosition), 3, 2);

        Assert.False(result);
    }

    [Fact]
    public void CanMigrate_WithValidSingleStepMigration_ReturnsTrue()
    {
        var migrator = new TestMigrator(new Dictionary<Type, Dictionary<int, Func<JsonElement, object>>>
        {
            [typeof(SerializablePosition)] = new()
            {
                [1] = json => new SerializablePosition()
            }
        });

        var result = migrator.CanMigrate(typeof(SerializablePosition), 1, 2);

        Assert.True(result);
    }

    [Fact]
    public void CanMigrate_WithValidMultiStepMigration_ReturnsTrue()
    {
        var migrator = new TestMigrator(new Dictionary<Type, Dictionary<int, Func<JsonElement, object>>>
        {
            [typeof(SerializablePosition)] = new()
            {
                [1] = json => new SerializablePosition(),
                [2] = json => new SerializablePosition(),
                [3] = json => new SerializablePosition()
            }
        });

        var result = migrator.CanMigrate(typeof(SerializablePosition), 1, 4);

        Assert.True(result);
    }

    [Fact]
    public void CanMigrate_WithGapInMigrationChain_ReturnsFalse()
    {
        var migrator = new TestMigrator(new Dictionary<Type, Dictionary<int, Func<JsonElement, object>>>
        {
            [typeof(SerializablePosition)] = new()
            {
                [1] = json => new SerializablePosition(),
                // Missing [2]
                [3] = json => new SerializablePosition()
            }
        });

        var result = migrator.CanMigrate(typeof(SerializablePosition), 1, 4);

        Assert.False(result);
    }

    [Fact]
    public void GetMigrationVersions_WithNoMigrations_ReturnsEmpty()
    {
        var migrator = new TestMigrator([]);

        var versions = migrator.GetMigrationVersions(typeof(SerializablePosition));

        Assert.Empty(versions);
    }

    [Fact]
    public void GetMigrationVersions_WithMigrations_ReturnsVersions()
    {
        var migrator = new TestMigrator(new Dictionary<Type, Dictionary<int, Func<JsonElement, object>>>
        {
            [typeof(SerializablePosition)] = new()
            {
                [1] = json => new SerializablePosition(),
                [3] = json => new SerializablePosition(),
                [2] = json => new SerializablePosition()
            }
        });

        var versions = migrator.GetMigrationVersions(typeof(SerializablePosition)).ToList();

        Assert.Equal(3, versions.Count);
        Assert.Equal([1, 2, 3], versions); // Should be sorted
    }

    #endregion

    #region Migration Execution Tests

    [Fact]
    public void Migrate_WithSameVersion_ReturnsOriginalData()
    {
        var migrator = new TestMigrator([]);

        var data = JsonSerializer.SerializeToElement(new { x = 5.0f, y = 10.0f });

        var result = migrator.Migrate(typeof(SerializablePosition), data, 2, 2);

        Assert.NotNull(result);
        // Should return the original data unchanged
        Assert.Equal(5.0f, result.Value.GetProperty("x").GetSingle());
        Assert.Equal(10.0f, result.Value.GetProperty("y").GetSingle());
    }

    [Fact]
    public void Migrate_WithUnknownType_ReturnsNull()
    {
        var migrator = new TestMigrator([]);

        var data = JsonSerializer.SerializeToElement(new { x = 5.0f, y = 10.0f });

        var result = migrator.Migrate(typeof(SerializablePosition), data, 1, 2);

        Assert.Null(result);
    }

    #endregion

    #region MigrateFromAttribute Tests

    [Fact]
    public void MigrateFromAttribute_Constructor_SetsFromVersion()
    {
        var attr = new MigrateFromAttribute(2);

        Assert.Equal(2, attr.FromVersion);
    }

    [Fact]
    public void MigrateFromAttribute_WithInvalidVersion_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MigrateFromAttribute(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new MigrateFromAttribute(-1));
    }

    [Fact]
    public void MigrateFromAttribute_WithValidVersion_DoesNotThrow()
    {
        var attr = new MigrateFromAttribute(1);
        Assert.Equal(1, attr.FromVersion);

        attr = new MigrateFromAttribute(100);
        Assert.Equal(100, attr.FromVersion);
    }

    #endregion

    #region SnapshotManager Migration Integration Tests

    [Fact]
    public void RestoreSnapshot_WithOlderVersion_AndNoMigrator_UsesBestEffortDeserialization()
    {
        // When the serializer doesn't implement IComponentMigrator,
        // the system uses best-effort deserialization (no exception thrown)
        var snapshot = new WorldSnapshot
        {
            Timestamp = DateTimeOffset.UtcNow,
            Entities =
            [
                new SerializedEntity
                {
                    Id = 1,
                    Name = "TestEntity",
                    Components =
                    [
                        new SerializedComponent
                        {
                            TypeName = typeof(SerializablePosition).AssemblyQualifiedName!,
                            Data = JsonDocument.Parse("{\"x\": 1.0, \"y\": 2.0}").RootElement,
                            IsTag = false,
                            Version = 1  // Old version
                        }
                    ]
                }
            ],
            Singletons = []
        };

        using var world = new World();

        // Use a serializer that reports current version as 3 but has no IComponentMigrator
        // This should NOT throw - it proceeds with best-effort deserialization
        var serializer = new VersionedSerializer(currentVersion: 3);

        var entityMap = SnapshotManager.RestoreSnapshot(world, snapshot, serializer);

        // Entity should be created with best-effort deserialized data
        Assert.Single(entityMap);
        var entity = world.GetEntityByName("TestEntity");
        Assert.True(entity.IsValid);
    }

    // Note: Full migration integration test with SnapshotManager requires a properly
    // generated serializer. The individual components (IComponentMigrator, MigrateFromAttribute,
    // SnapshotManager version handling) are tested separately above.

    #endregion
}

#region Test Helpers

/// <summary>
/// Test migrator implementation for unit testing.
/// </summary>
internal sealed class TestMigrator(Dictionary<Type, Dictionary<int, Func<JsonElement, object>>> migrations) : IComponentMigrator
{
    private readonly Dictionary<Type, Dictionary<int, Func<JsonElement, object>>> migrations = migrations;
    private readonly Dictionary<string, Dictionary<int, Func<JsonElement, object>>> migrationsByName = migrations.ToDictionary(
            kvp => kvp.Key.AssemblyQualifiedName ?? kvp.Key.FullName ?? kvp.Key.Name,
            kvp => kvp.Value);

    public bool CanMigrate(string typeName, int fromVersion, int toVersion)
    {
        if (fromVersion >= toVersion)
        {
            return false;
        }

        if (!migrationsByName.TryGetValue(typeName, out var typeMigrations))
        {
            return false;
        }

        for (var v = fromVersion; v < toVersion; v++)
        {
            if (!typeMigrations.ContainsKey(v))
            {
                return false;
            }
        }

        return true;
    }

    public bool CanMigrate(Type type, int fromVersion, int toVersion)
    {
        if (fromVersion >= toVersion)
        {
            return false;
        }

        if (!migrations.TryGetValue(type, out var typeMigrations))
        {
            return false;
        }

        for (var v = fromVersion; v < toVersion; v++)
        {
            if (!typeMigrations.ContainsKey(v))
            {
                return false;
            }
        }

        return true;
    }

    public JsonElement? Migrate(string typeName, JsonElement data, int fromVersion, int toVersion)
    {
        var type = migrationsByName.Keys
            .Select(k => Type.GetType(k))
            .FirstOrDefault(t => t?.AssemblyQualifiedName == typeName || t?.FullName == typeName);

        if (type is null)
        {
            return null;
        }

        return Migrate(type, data, fromVersion, toVersion);
    }

    public JsonElement? Migrate(Type type, JsonElement data, int fromVersion, int toVersion)
    {
        if (fromVersion >= toVersion)
        {
            return data;
        }

        if (!migrations.TryGetValue(type, out var typeMigrations))
        {
            return null;
        }

        var currentData = data;

        for (var v = fromVersion; v < toVersion; v++)
        {
            if (!typeMigrations.TryGetValue(v, out var migrate))
            {
                throw new ComponentVersionException(
                    type.Name,
                    v,
                    toVersion,
                    $"No migration defined from version {v} to {v + 1}");
            }

            var migratedValue = migrate(currentData);
            currentData = JsonSerializer.SerializeToElement(migratedValue);
        }

        return currentData;
    }

    public IEnumerable<int> GetMigrationVersions(string typeName)
    {
        return migrationsByName.TryGetValue(typeName, out var typeMigrations)
            ? typeMigrations.Keys.OrderBy(v => v)
            : Enumerable.Empty<int>();
    }

    public IEnumerable<int> GetMigrationVersions(Type type)
    {
        return migrations.TryGetValue(type, out var typeMigrations)
            ? typeMigrations.Keys.OrderBy(v => v)
            : Enumerable.Empty<int>();
    }
}

/// <summary>
/// Serializer that reports a specific version but has no migration support.
/// </summary>
internal sealed class VersionedSerializer(int currentVersion) : IComponentSerializer
{
    private readonly int currentVersion = currentVersion;

    public bool IsSerializable(Type type) => type == typeof(SerializablePosition);
    public bool IsSerializable(string typeName) => typeName.Contains("SerializablePosition");

    public object? Deserialize(string typeName, JsonElement json)
    {
        if (typeName.Contains("SerializablePosition"))
        {
            var x = json.TryGetProperty("x", out var xProp) ? xProp.GetSingle() : 0f;
            var y = json.TryGetProperty("y", out var yProp) ? yProp.GetSingle() : 0f;
            return new SerializablePosition { X = x, Y = y };
        }
        return null;
    }

    public JsonElement? Serialize(Type type, object value)
    {
        if (type == typeof(SerializablePosition))
        {
            var pos = (SerializablePosition)value;
            return JsonSerializer.SerializeToElement(new { x = pos.X, y = pos.Y });
        }
        return null;
    }

    public Type? GetType(string typeName)
    {
        if (typeName.Contains("SerializablePosition"))
        {
            return typeof(SerializablePosition);
        }
        return null;
    }

    public KeenEyes.ComponentInfo? RegisterComponent(KeenEyes.Capabilities.ISerializationCapability serialization, string typeName, bool isTag)
    {
        if (typeName.Contains("SerializablePosition"))
        {
            return serialization.Components.Register<SerializablePosition>(isTag);
        }
        return null;
    }

    public bool SetSingleton(KeenEyes.Capabilities.ISerializationCapability serialization, string typeName, object value) => false;
    public object? CreateDefault(string typeName) => typeName.Contains("SerializablePosition") ? default(SerializablePosition) : null;
    public int GetVersion(string typeName) => typeName.Contains("SerializablePosition") ? currentVersion : 1;
    public int GetVersion(Type type) => type == typeof(SerializablePosition) ? currentVersion : 1;
}

#endregion
