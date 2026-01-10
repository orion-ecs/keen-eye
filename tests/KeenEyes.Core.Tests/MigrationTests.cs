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

    [Fact]
    public void Migrate_SingleStep_TransformsData()
    {
        // Migration that adds a Z coordinate
        var migrator = new TestMigrator(new Dictionary<Type, Dictionary<int, Func<JsonElement, object>>>
        {
            [typeof(SerializablePosition)] = new()
            {
                [1] = json =>
                {
                    var x = json.GetProperty("x").GetSingle();
                    var y = json.GetProperty("y").GetSingle();
                    return new { x, y, z = 0.0f }; // Add z = 0
                }
            }
        });

        var data = JsonSerializer.SerializeToElement(new { x = 5.0f, y = 10.0f });

        var result = migrator.Migrate(typeof(SerializablePosition), data, 1, 2);

        Assert.NotNull(result);
        Assert.Equal(5.0f, result.Value.GetProperty("x").GetSingle());
        Assert.Equal(10.0f, result.Value.GetProperty("y").GetSingle());
        Assert.Equal(0.0f, result.Value.GetProperty("z").GetSingle());
    }

    [Fact]
    public void Migrate_MultipleSteps_ChainsTransformations()
    {
        // Multi-step migration: v1 (x,y) -> v2 (x,y,z) -> v3 (x,y,z,w)
        var migrator = new TestMigrator(new Dictionary<Type, Dictionary<int, Func<JsonElement, object>>>
        {
            [typeof(SerializablePosition)] = new()
            {
                [1] = json =>
                {
                    var x = json.GetProperty("x").GetSingle();
                    var y = json.GetProperty("y").GetSingle();
                    return new { x, y, z = 0.0f };
                },
                [2] = json =>
                {
                    var x = json.GetProperty("x").GetSingle();
                    var y = json.GetProperty("y").GetSingle();
                    var z = json.GetProperty("z").GetSingle();
                    return new { x, y, z, w = 1.0f };
                }
            }
        });

        var data = JsonSerializer.SerializeToElement(new { x = 5.0f, y = 10.0f });

        var result = migrator.Migrate(typeof(SerializablePosition), data, 1, 3);

        Assert.NotNull(result);
        Assert.Equal(5.0f, result.Value.GetProperty("x").GetSingle());
        Assert.Equal(10.0f, result.Value.GetProperty("y").GetSingle());
        Assert.Equal(0.0f, result.Value.GetProperty("z").GetSingle());
        Assert.Equal(1.0f, result.Value.GetProperty("w").GetSingle());
    }

    [Fact]
    public void Migrate_PartialChain_MigratesFromMiddle()
    {
        // Start migration from v2 instead of v1
        var migrator = new TestMigrator(new Dictionary<Type, Dictionary<int, Func<JsonElement, object>>>
        {
            [typeof(SerializablePosition)] = new()
            {
                [1] = json => new { x = 0, y = 0, z = 0 },
                [2] = json =>
                {
                    var x = json.GetProperty("x").GetSingle();
                    var y = json.GetProperty("y").GetSingle();
                    var z = json.GetProperty("z").GetSingle();
                    return new { x, y, z, w = 1.0f };
                }
            }
        });

        var data = JsonSerializer.SerializeToElement(new { x = 5.0f, y = 10.0f, z = 15.0f });

        var result = migrator.Migrate(typeof(SerializablePosition), data, 2, 3);

        Assert.NotNull(result);
        Assert.Equal(5.0f, result.Value.GetProperty("x").GetSingle());
        Assert.Equal(10.0f, result.Value.GetProperty("y").GetSingle());
        Assert.Equal(15.0f, result.Value.GetProperty("z").GetSingle());
        Assert.Equal(1.0f, result.Value.GetProperty("w").GetSingle());
    }

    [Fact]
    public void Migrate_WithGap_ThrowsComponentVersionException()
    {
        var migrator = new TestMigrator(new Dictionary<Type, Dictionary<int, Func<JsonElement, object>>>
        {
            [typeof(SerializablePosition)] = new()
            {
                [1] = json => new { x = 0, y = 0, z = 0 },
                // Missing v2 migration
                [3] = json => new { x = 0, y = 0, z = 0, w = 0 }
            }
        });

        var data = JsonSerializer.SerializeToElement(new { x = 5.0f, y = 10.0f });

        Assert.Throws<ComponentVersionException>(() =>
            migrator.Migrate(typeof(SerializablePosition), data, 1, 4));
    }

    [Fact]
    public void Migrate_LongChain_ChainsAllSteps()
    {
        // 5-step migration chain: v1 -> v2 -> v3 -> v4 -> v5 -> v6
        var migrationCount = 0;
        var migrator = new TestMigrator(new Dictionary<Type, Dictionary<int, Func<JsonElement, object>>>
        {
            [typeof(SerializablePosition)] = new()
            {
                [1] = json => { migrationCount++; return new { step = 2 }; },
                [2] = json => { migrationCount++; return new { step = 3 }; },
                [3] = json => { migrationCount++; return new { step = 4 }; },
                [4] = json => { migrationCount++; return new { step = 5 }; },
                [5] = json => { migrationCount++; return new { step = 6 }; }
            }
        });

        var data = JsonSerializer.SerializeToElement(new { step = 1 });

        var result = migrator.Migrate(typeof(SerializablePosition), data, 1, 6);

        Assert.NotNull(result);
        Assert.Equal(5, migrationCount);
        Assert.Equal(6, result.Value.GetProperty("step").GetInt32());
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
            return (KeenEyes.ComponentInfo)serialization.Components.Register<SerializablePosition>(isTag);
        }
        return null;
    }

    public bool SetSingleton(KeenEyes.Capabilities.ISerializationCapability serialization, string typeName, object value) => false;
    public object? CreateDefault(string typeName) => typeName.Contains("SerializablePosition") ? default(SerializablePosition) : null;
    public int GetVersion(string typeName) => typeName.Contains("SerializablePosition") ? currentVersion : 1;
    public int GetVersion(Type type) => type == typeof(SerializablePosition) ? currentVersion : 1;
}

#endregion
