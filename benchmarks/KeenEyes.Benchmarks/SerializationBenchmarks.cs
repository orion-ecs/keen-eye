using System.Text.Json;
using System.Text.Json.Serialization;
using BenchmarkDotNet.Attributes;
using KeenEyes.Serialization;

namespace KeenEyes.Benchmarks;

/// <summary>
/// Benchmarks comparing JSON vs binary serialization performance.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class SerializationBenchmarks
{
    private World world = null!;
    private WorldSnapshot snapshot = null!;
    private BenchmarkComponentSerializer serializer = null!;
    private string jsonData = null!;
    private byte[] binaryData = null!;

    [Params(10, 100, 1000)]
    public int EntityCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        world = new World();
        serializer = new BenchmarkComponentSerializer();

        // Create entities with various component configurations
        for (var i = 0; i < EntityCount; i++)
        {
            var builder = world.Spawn($"Entity_{i}");

            // Every entity has position and velocity
            builder.With(new Position { X = i * 1.5f, Y = i * 2.5f });
            builder.With(new Velocity { X = i * 0.1f, Y = i * 0.2f });

            // Half have health
            if (i % 2 == 0)
            {
                builder.With(new Health { Current = 100 - (i % 50), Max = 100 });
            }

            // Quarter have rotation
            if (i % 4 == 0)
            {
                builder.With(new Rotation { Angle = i * 0.5f });
            }

            // Add some tags
            if (i % 3 == 0)
            {
                builder.WithTag<ActiveTag>();
            }

            builder.Build();
        }

        // Set up some singletons
        world.SetSingleton(new GameTime { DeltaTime = 0.016f, TotalTime = 123.456f });
        world.SetSingleton(new GameConfig { MaxEntities = 10000, Gravity = 9.8f });

        // Pre-generate snapshot and serialized forms for deserialization benchmarks
        snapshot = SnapshotManager.CreateSnapshot(world, serializer);
        jsonData = SnapshotManager.ToJson(snapshot);
        binaryData = SnapshotManager.ToBinary(snapshot, serializer);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        world.Dispose();
    }

    /// <summary>
    /// Measures snapshot creation time.
    /// </summary>
    [Benchmark]
    public WorldSnapshot CreateSnapshot()
    {
        return SnapshotManager.CreateSnapshot(world, serializer);
    }

    /// <summary>
    /// Measures JSON serialization (snapshot to string).
    /// </summary>
    [Benchmark]
    public string ToJson()
    {
        return SnapshotManager.ToJson(snapshot);
    }

    /// <summary>
    /// Measures binary serialization (snapshot to bytes).
    /// </summary>
    [Benchmark]
    public byte[] ToBinary()
    {
        return SnapshotManager.ToBinary(snapshot, serializer);
    }

    /// <summary>
    /// Measures JSON deserialization (string to snapshot).
    /// </summary>
    [Benchmark]
    public WorldSnapshot? FromJson()
    {
        return SnapshotManager.FromJson(jsonData);
    }

    /// <summary>
    /// Measures binary deserialization (bytes to snapshot).
    /// </summary>
    [Benchmark]
    public WorldSnapshot FromBinary()
    {
        return SnapshotManager.FromBinary(binaryData, serializer);
    }

    /// <summary>
    /// Measures full JSON round-trip (create, serialize, deserialize).
    /// </summary>
    [Benchmark]
    public WorldSnapshot? JsonRoundTrip()
    {
        var snap = SnapshotManager.CreateSnapshot(world, serializer);
        var json = SnapshotManager.ToJson(snap);
        return SnapshotManager.FromJson(json);
    }

    /// <summary>
    /// Measures full binary round-trip (create, serialize, deserialize).
    /// </summary>
    [Benchmark]
    public WorldSnapshot BinaryRoundTrip()
    {
        var snap = SnapshotManager.CreateSnapshot(world, serializer);
        var binary = SnapshotManager.ToBinary(snap, serializer);
        return SnapshotManager.FromBinary(binary, serializer);
    }
}

/// <summary>
/// Benchmarks for serialized data sizes.
/// </summary>
[ShortRunJob]
public class SerializationSizeBenchmarks
{
    private World world = null!;
    private WorldSnapshot snapshot = null!;
    private BenchmarkComponentSerializer serializer = null!;

    [Params(10, 100, 1000)]
    public int EntityCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        world = new World();
        serializer = new BenchmarkComponentSerializer();

        for (var i = 0; i < EntityCount; i++)
        {
            world.Spawn($"Entity_{i}")
                .With(new Position { X = i * 1.5f, Y = i * 2.5f })
                .With(new Velocity { X = i * 0.1f, Y = i * 0.2f })
                .With(new Health { Current = 100, Max = 100 })
                .Build();
        }

        snapshot = SnapshotManager.CreateSnapshot(world, serializer);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        world.Dispose();
    }

    /// <summary>
    /// Returns JSON size in bytes (for comparison, not a timing benchmark).
    /// </summary>
    [Benchmark(Baseline = true)]
    public int JsonSize()
    {
        var json = SnapshotManager.ToJson(snapshot);
        return System.Text.Encoding.UTF8.GetByteCount(json);
    }

    /// <summary>
    /// Returns binary size in bytes.
    /// </summary>
    [Benchmark]
    public int BinarySize()
    {
        var binary = SnapshotManager.ToBinary(snapshot, serializer);
        return binary.Length;
    }
}

/// <summary>
/// A simple component serializer for benchmarks supporting common benchmark components.
/// </summary>
internal sealed class BenchmarkComponentSerializer : IComponentSerializer, IBinaryComponentSerializer
{
    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        IncludeFields = true,
        PropertyNameCaseInsensitive = true
    };

    public bool IsSerializable(Type type)
    {
        return type == typeof(Position) || type == typeof(Velocity) ||
               type == typeof(Health) || type == typeof(Rotation) ||
               type == typeof(ActiveTag) || type == typeof(FrozenTag) ||
               type == typeof(GameTime) || type == typeof(GameConfig);
    }

    public bool IsSerializable(string typeName)
    {
        return typeName.Contains(nameof(Position)) || typeName.Contains(nameof(Velocity)) ||
               typeName.Contains(nameof(Health)) || typeName.Contains(nameof(Rotation)) ||
               typeName.Contains(nameof(ActiveTag)) || typeName.Contains(nameof(FrozenTag)) ||
               typeName.Contains(nameof(GameTime)) || typeName.Contains(nameof(GameConfig));
    }

    public object? Deserialize(string typeName, JsonElement json)
    {
        if (typeName.Contains(nameof(Position)))
        {
            return JsonSerializer.Deserialize<Position>(json.GetRawText(), jsonOptions);
        }

        if (typeName.Contains(nameof(Velocity)))
        {
            return JsonSerializer.Deserialize<Velocity>(json.GetRawText(), jsonOptions);
        }

        if (typeName.Contains(nameof(Health)))
        {
            return JsonSerializer.Deserialize<Health>(json.GetRawText(), jsonOptions);
        }

        if (typeName.Contains(nameof(Rotation)))
        {
            return JsonSerializer.Deserialize<Rotation>(json.GetRawText(), jsonOptions);
        }

        if (typeName.Contains(nameof(ActiveTag)))
        {
            return default(ActiveTag);
        }

        if (typeName.Contains(nameof(FrozenTag)))
        {
            return default(FrozenTag);
        }

        if (typeName.Contains(nameof(GameTime)))
        {
            return JsonSerializer.Deserialize<GameTime>(json.GetRawText(), jsonOptions);
        }

        if (typeName.Contains(nameof(GameConfig)))
        {
            return JsonSerializer.Deserialize<GameConfig>(json.GetRawText(), jsonOptions);
        }

        return null;
    }

    public JsonElement? Serialize(Type type, object value)
    {
        string json;
        if (type == typeof(Position))
        {
            json = JsonSerializer.Serialize((Position)value, jsonOptions);
        }
        else if (type == typeof(Velocity))
        {
            json = JsonSerializer.Serialize((Velocity)value, jsonOptions);
        }
        else if (type == typeof(Health))
        {
            json = JsonSerializer.Serialize((Health)value, jsonOptions);
        }
        else if (type == typeof(Rotation))
        {
            json = JsonSerializer.Serialize((Rotation)value, jsonOptions);
        }
        else if (type == typeof(ActiveTag))
        {
            return null;
        }
        else if (type == typeof(FrozenTag))
        {
            return null;
        }
        else if (type == typeof(GameTime))
        {
            json = JsonSerializer.Serialize((GameTime)value, jsonOptions);
        }
        else if (type == typeof(GameConfig))
        {
            json = JsonSerializer.Serialize((GameConfig)value, jsonOptions);
        }
        else
        {
            return null;
        }

        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    public Type? GetType(string typeName)
    {
        if (typeName.Contains(nameof(Position)))
        {
            return typeof(Position);
        }

        if (typeName.Contains(nameof(Velocity)))
        {
            return typeof(Velocity);
        }

        if (typeName.Contains(nameof(Health)))
        {
            return typeof(Health);
        }

        if (typeName.Contains(nameof(Rotation)))
        {
            return typeof(Rotation);
        }

        if (typeName.Contains(nameof(ActiveTag)))
        {
            return typeof(ActiveTag);
        }

        if (typeName.Contains(nameof(FrozenTag)))
        {
            return typeof(FrozenTag);
        }

        if (typeName.Contains(nameof(GameTime)))
        {
            return typeof(GameTime);
        }

        if (typeName.Contains(nameof(GameConfig)))
        {
            return typeof(GameConfig);
        }

        return null;
    }

    public ComponentInfo? RegisterComponent(World world, string typeName, bool isTag)
    {
        if (typeName.Contains(nameof(Position)))
        {
            return world.Components.Register<Position>();
        }

        if (typeName.Contains(nameof(Velocity)))
        {
            return world.Components.Register<Velocity>();
        }

        if (typeName.Contains(nameof(Health)))
        {
            return world.Components.Register<Health>();
        }

        if (typeName.Contains(nameof(Rotation)))
        {
            return world.Components.Register<Rotation>();
        }

        if (typeName.Contains(nameof(ActiveTag)))
        {
            return world.Components.Register<ActiveTag>(isTag: true);
        }

        if (typeName.Contains(nameof(FrozenTag)))
        {
            return world.Components.Register<FrozenTag>(isTag: true);
        }

        return null;
    }

    public bool SetSingleton(World world, string typeName, object value)
    {
        if (typeName.Contains(nameof(GameTime)))
        {
            world.SetSingleton((GameTime)value);
            return true;
        }

        if (typeName.Contains(nameof(GameConfig)))
        {
            world.SetSingleton((GameConfig)value);
            return true;
        }

        return false;
    }

    // IBinaryComponentSerializer implementation
    public bool WriteTo(Type type, object value, BinaryWriter writer)
    {
        if (type == typeof(Position))
        {
            var p = (Position)value;
            writer.Write(p.X);
            writer.Write(p.Y);
            return true;
        }

        if (type == typeof(Velocity))
        {
            var v = (Velocity)value;
            writer.Write(v.X);
            writer.Write(v.Y);
            return true;
        }

        if (type == typeof(Health))
        {
            var h = (Health)value;
            writer.Write(h.Current);
            writer.Write(h.Max);
            return true;
        }

        if (type == typeof(Rotation))
        {
            var r = (Rotation)value;
            writer.Write(r.Angle);
            return true;
        }

        if (type == typeof(GameTime))
        {
            var t = (GameTime)value;
            writer.Write(t.DeltaTime);
            writer.Write(t.TotalTime);
            return true;
        }

        if (type == typeof(GameConfig))
        {
            var c = (GameConfig)value;
            writer.Write(c.MaxEntities);
            writer.Write(c.Gravity);
            return true;
        }

        // Tags have no data
        if (type == typeof(ActiveTag) || type == typeof(FrozenTag))
        {
            return true;
        }

        return false;
    }

    public object? ReadFrom(string typeName, BinaryReader reader)
    {
        if (typeName.Contains(nameof(Position)))
        {
            return new Position { X = reader.ReadSingle(), Y = reader.ReadSingle() };
        }

        if (typeName.Contains(nameof(Velocity)))
        {
            return new Velocity { X = reader.ReadSingle(), Y = reader.ReadSingle() };
        }

        if (typeName.Contains(nameof(Health)))
        {
            return new Health { Current = reader.ReadInt32(), Max = reader.ReadInt32() };
        }

        if (typeName.Contains(nameof(Rotation)))
        {
            return new Rotation { Angle = reader.ReadSingle() };
        }

        if (typeName.Contains(nameof(GameTime)))
        {
            return new GameTime { DeltaTime = reader.ReadSingle(), TotalTime = reader.ReadSingle() };
        }

        if (typeName.Contains(nameof(GameConfig)))
        {
            return new GameConfig { MaxEntities = reader.ReadInt32(), Gravity = reader.ReadSingle() };
        }

        if (typeName.Contains(nameof(ActiveTag)))
        {
            return default(ActiveTag);
        }

        if (typeName.Contains(nameof(FrozenTag)))
        {
            return default(FrozenTag);
        }

        return null;
    }
}
