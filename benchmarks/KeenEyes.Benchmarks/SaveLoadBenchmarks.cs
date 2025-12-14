using BenchmarkDotNet.Attributes;
using KeenEyes.Serialization;

namespace KeenEyes.Benchmarks;

/// <summary>
/// Benchmarks for save/load operations including file I/O.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class SaveLoadBenchmarks
{
    private World world = null!;
    private BenchmarkComponentSerializer serializer = null!;
    private string saveDirectory = null!;
    private int saveCounter;

    [Params(10, 100, 1000)]
    public int EntityCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        world = new World();
        serializer = new BenchmarkComponentSerializer();
        saveDirectory = Path.Combine(Path.GetTempPath(), "keeneyes_benchmarks", Guid.NewGuid().ToString());
        Directory.CreateDirectory(saveDirectory);

        world.SaveDirectory = saveDirectory;

        // Create entities with various component configurations
        PopulateWorld(world, EntityCount);

        // Set up some singletons
        world.SetSingleton(new GameTime { DeltaTime = 0.016f, TotalTime = 123.456f });
        world.SetSingleton(new GameConfig { MaxEntities = 10000, Gravity = 9.8f });

        // Pre-save for load benchmark
        world.SaveToSlot("load_slot", serializer);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        world.Dispose();

        if (Directory.Exists(saveDirectory))
        {
            Directory.Delete(saveDirectory, recursive: true);
        }
    }

    /// <summary>
    /// Measures full save to slot (snapshot creation + file write).
    /// </summary>
    [Benchmark(Baseline = true)]
    public SaveSlotInfo SaveToSlot()
    {
        saveCounter++;
        return world.SaveToSlot($"benchmark_slot_{saveCounter}", serializer);
    }

    /// <summary>
    /// Measures full load from slot (file read + world restore).
    /// </summary>
    [Benchmark]
    public int LoadFromSlot()
    {
        var (_, entityMap) = world.LoadFromSlot("load_slot", serializer);

        // Repopulate for next iteration (load clears world)
        if (world.EntityCount < EntityCount)
        {
            PopulateWorld(world, EntityCount - world.EntityCount);
        }

        return entityMap.Count;
    }

    /// <summary>
    /// Measures save with compression.
    /// </summary>
    [Benchmark]
    public SaveSlotInfo SaveWithCompression()
    {
        saveCounter++;
        var options = new SaveSlotOptions
        {
            Compression = CompressionMode.Brotli
        };
        return world.SaveToSlot($"compressed_slot_{saveCounter}", serializer, options);
    }

    /// <summary>
    /// Measures full round-trip (save + clear + load).
    /// </summary>
    [Benchmark]
    public int SaveLoadRoundTrip()
    {
        saveCounter++;
        world.SaveToSlot($"roundtrip_slot_{saveCounter}", serializer);

        // Clear and reload
        var entities = world.Query<Position>().ToArray();
        foreach (var entity in entities)
        {
            world.Despawn(entity);
        }

        var (_, entityMap) = world.LoadFromSlot($"roundtrip_slot_{saveCounter}", serializer);

        // Repopulate for next iteration
        if (world.EntityCount < EntityCount)
        {
            PopulateWorld(world, EntityCount - world.EntityCount);
        }

        return entityMap.Count;
    }

    private static void PopulateWorld(World world, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var builder = world.Spawn($"Entity_{i}");

            builder.With(new Position { X = i * 1.5f, Y = i * 2.5f });
            builder.With(new Velocity { X = i * 0.1f, Y = i * 0.2f });

            if (i % 2 == 0)
            {
                builder.With(new Health { Current = 100 - (i % 50), Max = 100 });
            }

            if (i % 4 == 0)
            {
                builder.With(new Rotation { Angle = i * 0.5f });
            }

            if (i % 3 == 0)
            {
                builder.WithTag<ActiveTag>();
            }

            builder.Build();
        }
    }
}

/// <summary>
/// Benchmarks comparing full saves vs delta saves.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class DeltaSaveBenchmarks
{
    private World world = null!;
    private BenchmarkComponentSerializer serializer = null!;
    private string saveDirectory = null!;
    private WorldSnapshot baseline = null!;
    private int saveCounter;

    [Params(100, 1000)]
    public int EntityCount { get; set; }

    [Params(1, 10, 50)]
    public int ChangePercentage { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        world = new World();
        serializer = new BenchmarkComponentSerializer();
        saveDirectory = Path.Combine(Path.GetTempPath(), "keeneyes_delta_benchmarks", Guid.NewGuid().ToString());
        Directory.CreateDirectory(saveDirectory);

        world.SaveDirectory = saveDirectory;

        // Create initial entities
        for (var i = 0; i < EntityCount; i++)
        {
            world.Spawn($"Entity_{i}")
                .With(new Position { X = i * 1.5f, Y = i * 2.5f })
                .With(new Velocity { X = i * 0.1f, Y = i * 0.2f })
                .With(new Health { Current = 100, Max = 100 })
                .Build();
        }

        // Create baseline
        baseline = SnapshotManager.CreateSnapshot(world, serializer);
        world.SaveToSlot("baseline", serializer);

        // Make changes based on ChangePercentage
        ApplyChanges();
    }

    private void ApplyChanges()
    {
        var entitiesToChange = EntityCount * ChangePercentage / 100;
        var entities = world.Query<Position>().ToArray();

        for (var i = 0; i < entitiesToChange && i < entities.Length; i++)
        {
            ref var pos = ref world.Get<Position>(entities[i]);
            pos.X += 100;
            pos.Y += 100;
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        world.Dispose();

        if (Directory.Exists(saveDirectory))
        {
            Directory.Delete(saveDirectory, recursive: true);
        }
    }

    /// <summary>
    /// Measures creating a delta snapshot (compute differences).
    /// </summary>
    [Benchmark]
    public DeltaSnapshot CreateDelta()
    {
        return DeltaDiffer.CreateDelta(world, baseline, serializer, "baseline", 1);
    }

    /// <summary>
    /// Measures full snapshot creation (for comparison).
    /// </summary>
    [Benchmark(Baseline = true)]
    public WorldSnapshot CreateFullSnapshot()
    {
        return SnapshotManager.CreateSnapshot(world, serializer);
    }

    /// <summary>
    /// Measures full save to slot (for comparison).
    /// </summary>
    [Benchmark]
    public SaveSlotInfo SaveFullToSlot()
    {
        saveCounter++;
        return world.SaveToSlot($"full_slot_{saveCounter}", serializer);
    }
}

/// <summary>
/// Benchmarks for delta restoration performance.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class DeltaRestoreBenchmarks
{
    private World world = null!;
    private World restoreWorld = null!;
    private BenchmarkComponentSerializer serializer = null!;
    private string saveDirectory = null!;
    private WorldSnapshot baseline = null!;
    private DeltaSnapshot delta = null!;
    private Dictionary<int, Entity> baselineEntityMap = null!;

    [Params(100, 1000)]
    public int EntityCount { get; set; }

    [Params(10, 50)]
    public int ChangePercentage { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        world = new World();
        serializer = new BenchmarkComponentSerializer();
        saveDirectory = Path.Combine(Path.GetTempPath(), "keeneyes_restore_benchmarks", Guid.NewGuid().ToString());
        Directory.CreateDirectory(saveDirectory);

        world.SaveDirectory = saveDirectory;

        // Create initial entities
        for (var i = 0; i < EntityCount; i++)
        {
            world.Spawn($"Entity_{i}")
                .With(new Position { X = i * 1.5f, Y = i * 2.5f })
                .With(new Velocity { X = i * 0.1f, Y = i * 0.2f })
                .With(new Health { Current = 100, Max = 100 })
                .Build();
        }

        // Create baseline
        baseline = SnapshotManager.CreateSnapshot(world, serializer);

        // Make changes
        var entitiesToChange = EntityCount * ChangePercentage / 100;
        var entities = world.Query<Position>().ToArray();

        for (var i = 0; i < entitiesToChange && i < entities.Length; i++)
        {
            ref var pos = ref world.Get<Position>(entities[i]);
            pos.X += 100;
            pos.Y += 100;
        }

        // Create delta
        delta = DeltaDiffer.CreateDelta(world, baseline, serializer, "baseline", 1);

        // Setup restore world with baseline
        restoreWorld = new World
        {
            SaveDirectory = saveDirectory
        };
        baselineEntityMap = SnapshotManager.RestoreSnapshot(restoreWorld, baseline, serializer);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        world.Dispose();
        restoreWorld.Dispose();

        if (Directory.Exists(saveDirectory))
        {
            Directory.Delete(saveDirectory, recursive: true);
        }
    }

    [IterationSetup]
    public void IterationSetup()
    {
        // Reset restore world to baseline state before each iteration
        var entities = restoreWorld.Query<Position>().ToArray();
        foreach (var entity in entities)
        {
            restoreWorld.Despawn(entity);
        }
        baselineEntityMap = SnapshotManager.RestoreSnapshot(restoreWorld, baseline, serializer);
    }

    /// <summary>
    /// Measures applying a delta snapshot to a world.
    /// </summary>
    [Benchmark]
    public Dictionary<int, Entity> ApplyDelta()
    {
        return DeltaRestorer.ApplyDelta(restoreWorld, delta, serializer, baselineEntityMap);
    }

    /// <summary>
    /// Measures full snapshot restoration (for comparison).
    /// </summary>
    [Benchmark(Baseline = true)]
    public Dictionary<int, Entity> RestoreFullSnapshot()
    {
        // Clear and restore
        var entities = restoreWorld.Query<Position>().ToArray();
        foreach (var entity in entities)
        {
            restoreWorld.Despawn(entity);
        }

        return SnapshotManager.RestoreSnapshot(restoreWorld, baseline, serializer);
    }
}

/// <summary>
/// Benchmarks comparing save file sizes.
/// </summary>
[ShortRunJob]
public class SaveFileSizeBenchmarks
{
    private World world = null!;
    private BenchmarkComponentSerializer serializer = null!;
    private WorldSnapshot baseline = null!;

    [Params(100, 1000, 10000)]
    public int EntityCount { get; set; }

    [Params(1, 10, 50)]
    public int ChangePercentage { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        world = new World();
        serializer = new BenchmarkComponentSerializer();

        // Create initial entities
        for (var i = 0; i < EntityCount; i++)
        {
            world.Spawn($"Entity_{i}")
                .With(new Position { X = i * 1.5f, Y = i * 2.5f })
                .With(new Velocity { X = i * 0.1f, Y = i * 0.2f })
                .With(new Health { Current = 100, Max = 100 })
                .Build();
        }

        // Create baseline
        baseline = SnapshotManager.CreateSnapshot(world, serializer);

        // Make changes
        var entitiesToChange = EntityCount * ChangePercentage / 100;
        var entities = world.Query<Position>().ToArray();

        for (var i = 0; i < entitiesToChange && i < entities.Length; i++)
        {
            ref var pos = ref world.Get<Position>(entities[i]);
            pos.X += 100;
            pos.Y += 100;
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        world.Dispose();
    }

    /// <summary>
    /// Returns full snapshot JSON size in bytes.
    /// </summary>
    [Benchmark(Baseline = true)]
    public int FullSnapshotJsonSize()
    {
        var snapshot = SnapshotManager.CreateSnapshot(world, serializer);
        var json = SnapshotManager.ToJson(snapshot);
        return System.Text.Encoding.UTF8.GetByteCount(json);
    }

    /// <summary>
    /// Returns delta snapshot JSON size in bytes.
    /// </summary>
    [Benchmark]
    public int DeltaSnapshotJsonSize()
    {
        var delta = DeltaDiffer.CreateDelta(world, baseline, serializer, "baseline", 1);
        var json = System.Text.Json.JsonSerializer.Serialize(delta);
        return System.Text.Encoding.UTF8.GetByteCount(json);
    }

    /// <summary>
    /// Returns full snapshot binary size in bytes.
    /// </summary>
    [Benchmark]
    public int FullSnapshotBinarySize()
    {
        var snapshot = SnapshotManager.CreateSnapshot(world, serializer);
        var binary = SnapshotManager.ToBinary(snapshot, serializer);
        return binary.Length;
    }
}
