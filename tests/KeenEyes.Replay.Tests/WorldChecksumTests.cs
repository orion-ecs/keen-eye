using System.Text.Json;
using KeenEyes.Capabilities;
using KeenEyes.Serialization;

namespace KeenEyes.Replay.Tests;

/// <summary>
/// Unit tests for the <see cref="WorldChecksum"/> class.
/// </summary>
public class WorldChecksumTests
{
    #region Calculate(World) Tests

    [Fact]
    public void Calculate_WithEmptyWorld_ReturnsConsistentChecksum()
    {
        // Arrange
        using var world = new World();
        var serializer = new TestComponentSerializer();

        // Act
        var checksum1 = WorldChecksum.Calculate(world, serializer);
        var checksum2 = WorldChecksum.Calculate(world, serializer);

        // Assert
        Assert.Equal(checksum1, checksum2);
    }

    [Fact]
    public void Calculate_WithEntities_ReturnsConsistentChecksum()
    {
        // Arrange
        using var world = new World();
        var serializer = new TestComponentSerializer();

        world.Spawn().Build();
        world.Spawn().Build();
        world.Spawn().Build();

        // Act
        var checksum1 = WorldChecksum.Calculate(world, serializer);
        var checksum2 = WorldChecksum.Calculate(world, serializer);

        // Assert
        Assert.Equal(checksum1, checksum2);
    }

    [Fact]
    public void Calculate_WithDifferentEntityCounts_ReturnsDifferentChecksums()
    {
        // Arrange
        var serializer = new TestComponentSerializer();

        using var world1 = new World();
        world1.Spawn().Build();

        using var world2 = new World();
        world2.Spawn().Build();
        world2.Spawn().Build();

        // Act
        var checksum1 = WorldChecksum.Calculate(world1, serializer);
        var checksum2 = WorldChecksum.Calculate(world2, serializer);

        // Assert
        Assert.NotEqual(checksum1, checksum2);
    }

    [Fact]
    public void Calculate_WithSameEntities_ReturnsSameChecksum()
    {
        // Arrange
        var serializer = new TestComponentSerializer();

        // Two worlds with same entities (by ID and version)
        using var world1 = new World();
        world1.Spawn().Build();
        world1.Spawn().Build();

        using var world2 = new World();
        world2.Spawn().Build();
        world2.Spawn().Build();

        // Act
        var checksum1 = WorldChecksum.Calculate(world1, serializer);
        var checksum2 = WorldChecksum.Calculate(world2, serializer);

        // Assert - Same entity structure = same checksum
        Assert.Equal(checksum1, checksum2);
    }

    [Fact]
    public void Calculate_WithNullWorld_ThrowsArgumentNullException()
    {
        // Arrange
        var serializer = new TestComponentSerializer();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => WorldChecksum.Calculate(null!, serializer));
    }

    [Fact]
    public void Calculate_WithNullSerializer_ThrowsArgumentNullException()
    {
        // Arrange
        using var world = new World();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => WorldChecksum.Calculate(world, null!));
    }

    [Fact]
    public void Calculate_WithNamedEntities_IncludesNamesInChecksum()
    {
        // Arrange
        var serializer = new TestComponentSerializer();

        using var world1 = new World();
        world1.Spawn("Player").Build();

        using var world2 = new World();
        world2.Spawn("Enemy").Build();

        // Act
        var checksum1 = WorldChecksum.Calculate(world1, serializer);
        var checksum2 = WorldChecksum.Calculate(world2, serializer);

        // Assert - Different names = different checksums
        Assert.NotEqual(checksum1, checksum2);
    }

    #endregion

    #region Calculate(WorldSnapshot) Tests

    [Fact]
    public void Calculate_WithEmptySnapshot_ReturnsConsistentChecksum()
    {
        // Arrange
        var snapshot = new WorldSnapshot
        {
            Timestamp = DateTimeOffset.UtcNow,
            Entities = [],
            Singletons = []
        };

        // Act
        var checksum1 = WorldChecksum.Calculate(snapshot);
        var checksum2 = WorldChecksum.Calculate(snapshot);

        // Assert
        Assert.Equal(checksum1, checksum2);
    }

    [Fact]
    public void Calculate_WithSnapshotEntities_ReturnsConsistentChecksum()
    {
        // Arrange
        var snapshot = new WorldSnapshot
        {
            Timestamp = DateTimeOffset.UtcNow,
            Entities =
            [
                new SerializedEntity
                {
                    Id = 1,
                    Components = []
                },
                new SerializedEntity
                {
                    Id = 2,
                    Components = []
                }
            ],
            Singletons = []
        };

        // Act
        var checksum1 = WorldChecksum.Calculate(snapshot);
        var checksum2 = WorldChecksum.Calculate(snapshot);

        // Assert
        Assert.Equal(checksum1, checksum2);
    }

    [Fact]
    public void Calculate_WithDifferentSnapshotEntities_ReturnsDifferentChecksums()
    {
        // Arrange
        var snapshot1 = new WorldSnapshot
        {
            Timestamp = DateTimeOffset.UtcNow,
            Entities =
            [
                new SerializedEntity { Id = 1, Components = [] }
            ],
            Singletons = []
        };

        var snapshot2 = new WorldSnapshot
        {
            Timestamp = DateTimeOffset.UtcNow,
            Entities =
            [
                new SerializedEntity { Id = 1, Components = [] },
                new SerializedEntity { Id = 2, Components = [] }
            ],
            Singletons = []
        };

        // Act
        var checksum1 = WorldChecksum.Calculate(snapshot1);
        var checksum2 = WorldChecksum.Calculate(snapshot2);

        // Assert
        Assert.NotEqual(checksum1, checksum2);
    }

    [Fact]
    public void Calculate_WithNullSnapshot_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => WorldChecksum.Calculate((WorldSnapshot)null!));
    }

    [Fact]
    public void Calculate_WithSnapshotComponents_IncludesComponentDataInChecksum()
    {
        // Arrange
        var data1 = JsonDocument.Parse("{\"x\": 1}").RootElement;
        var data2 = JsonDocument.Parse("{\"x\": 2}").RootElement;

        var snapshot1 = new WorldSnapshot
        {
            Timestamp = DateTimeOffset.UtcNow,
            Entities =
            [
                new SerializedEntity
                {
                    Id = 1,
                    Components =
                    [
                        new SerializedComponent
                        {
                            TypeName = "TestComponent",
                            Data = data1,
                            IsTag = false
                        }
                    ]
                }
            ],
            Singletons = []
        };

        var snapshot2 = new WorldSnapshot
        {
            Timestamp = DateTimeOffset.UtcNow,
            Entities =
            [
                new SerializedEntity
                {
                    Id = 1,
                    Components =
                    [
                        new SerializedComponent
                        {
                            TypeName = "TestComponent",
                            Data = data2,
                            IsTag = false
                        }
                    ]
                }
            ],
            Singletons = []
        };

        // Act
        var checksum1 = WorldChecksum.Calculate(snapshot1);
        var checksum2 = WorldChecksum.Calculate(snapshot2);

        // Assert - Different component data = different checksums
        Assert.NotEqual(checksum1, checksum2);
    }

    [Fact]
    public void Calculate_WithSnapshotSingletons_IncludesSingletonsInChecksum()
    {
        // Arrange
        var singletonData1 = JsonDocument.Parse("{\"value\": 100}").RootElement;
        var singletonData2 = JsonDocument.Parse("{\"value\": 200}").RootElement;

        var snapshot1 = new WorldSnapshot
        {
            Timestamp = DateTimeOffset.UtcNow,
            Entities = [],
            Singletons =
            [
                new SerializedSingleton
                {
                    TypeName = "TestSingleton",
                    Data = singletonData1
                }
            ]
        };

        var snapshot2 = new WorldSnapshot
        {
            Timestamp = DateTimeOffset.UtcNow,
            Entities = [],
            Singletons =
            [
                new SerializedSingleton
                {
                    TypeName = "TestSingleton",
                    Data = singletonData2
                }
            ]
        };

        // Act
        var checksum1 = WorldChecksum.Calculate(snapshot1);
        var checksum2 = WorldChecksum.Calculate(snapshot2);

        // Assert - Different singleton data = different checksums
        Assert.NotEqual(checksum1, checksum2);
    }

    #endregion

    #region Determinism Tests

    [Fact]
    public void Calculate_IsDeterministic_ReturnsConsistentResultsAcrossMultipleCalls()
    {
        // Arrange
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
                            TypeName = "TestComponent",
                            Data = JsonDocument.Parse("{\"x\": 10, \"y\": 20}").RootElement,
                            IsTag = false
                        }
                    ]
                }
            ],
            Singletons =
            [
                new SerializedSingleton
                {
                    TypeName = "GameState",
                    Data = JsonDocument.Parse("{\"score\": 100}").RootElement
                }
            ]
        };

        // Act - Calculate multiple times
        var checksums = Enumerable.Range(0, 100)
            .Select(_ => WorldChecksum.Calculate(snapshot))
            .ToList();

        // Assert - All checksums should be identical
        Assert.True(checksums.All(c => c == checksums[0]));
    }

    [Fact]
    public void Calculate_EntityOrder_DoesNotAffectChecksum()
    {
        // Arrange - Same entities in different order (sorted by ID internally)
        var snapshot1 = new WorldSnapshot
        {
            Timestamp = DateTimeOffset.UtcNow,
            Entities =
            [
                new SerializedEntity { Id = 1, Components = [] },
                new SerializedEntity { Id = 2, Components = [] },
                new SerializedEntity { Id = 3, Components = [] }
            ],
            Singletons = []
        };

        var snapshot2 = new WorldSnapshot
        {
            Timestamp = DateTimeOffset.UtcNow,
            Entities =
            [
                new SerializedEntity { Id = 3, Components = [] },
                new SerializedEntity { Id = 1, Components = [] },
                new SerializedEntity { Id = 2, Components = [] }
            ],
            Singletons = []
        };

        // Act
        var checksum1 = WorldChecksum.Calculate(snapshot1);
        var checksum2 = WorldChecksum.Calculate(snapshot2);

        // Assert - Same entities (sorted by ID) = same checksum
        Assert.Equal(checksum1, checksum2);
    }

    #endregion

    #region Performance Tests

    [Fact]
    public void Calculate_Performance_CompletesQuicklyForTypicalWorldSize()
    {
        // Arrange - Create a moderately sized snapshot
        var entities = new List<SerializedEntity>();
        for (int i = 0; i < 1000; i++)
        {
            entities.Add(new SerializedEntity
            {
                Id = i,
                Name = $"Entity_{i}",
                Components =
                [
                    new SerializedComponent
                    {
                        TypeName = "Position",
                        Data = JsonDocument.Parse($"{{\"x\": {i}, \"y\": {i * 2}}}").RootElement,
                        IsTag = false
                    },
                    new SerializedComponent
                    {
                        TypeName = "Velocity",
                        Data = JsonDocument.Parse($"{{\"vx\": {i % 10}, \"vy\": {i % 5}}}").RootElement,
                        IsTag = false
                    }
                ]
            });
        }

        var snapshot = new WorldSnapshot
        {
            Timestamp = DateTimeOffset.UtcNow,
            Entities = entities,
            Singletons = []
        };

        // Act - Time the calculation
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < 10; i++)
        {
            WorldChecksum.Calculate(snapshot);
        }
        stopwatch.Stop();

        // Assert - Should complete 10 calculations in reasonable time
        // Target: < 1ms per frame, so 10 frames should be < 100ms with margin
        Assert.True(stopwatch.ElapsedMilliseconds < 500,
            $"10 checksum calculations took {stopwatch.ElapsedMilliseconds}ms, expected < 500ms");
    }

    #endregion
}

/// <summary>
/// Simple test component serializer for testing.
/// </summary>
internal sealed class TestComponentSerializer : IComponentSerializer
{
    public bool IsSerializable(Type type) => false;
    public bool IsSerializable(string typeName) => false;
    public object? Deserialize(string typeName, JsonElement json) => null;
    public JsonElement? Serialize(Type type, object value) => null;
    public Type? GetType(string typeName) => null;
    public ComponentInfo? RegisterComponent(ISerializationCapability serialization, string typeName, bool isTag) => null;
    public bool SetSingleton(ISerializationCapability serialization, string typeName, object value) => false;
    public object? CreateDefault(string typeName) => null;
    public int GetVersion(string typeName) => 1;
    public int GetVersion(Type type) => 1;
}
