using BenchmarkDotNet.Attributes;
using KeenEyes.Replay;
using KeenEyes.Serialization;

using ReplayCompressionMode = KeenEyes.Replay.CompressionMode;

namespace KeenEyes.Benchmarks;

/// <summary>
/// Benchmarks for replay recording overhead during gameplay.
/// Measures the cost of recording frames and events.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class ReplayRecordingBenchmarks
{
    private World world = null!;
    private World worldWithReplay = null!;
    private BenchmarkComponentSerializer serializer = null!;
    private ReplayRecorder recorder = null!;

    [Params(100, 1000)]
    public int EntityCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        // World without replay for baseline
        world = new World();
        PopulateWorld(world, EntityCount);
        world.AddSystem(new MovementSystem(), SystemPhase.Update);

        // World with replay
        worldWithReplay = new World();
        serializer = new BenchmarkComponentSerializer();

        var replayOptions = new ReplayOptions
        {
            SnapshotInterval = TimeSpan.FromSeconds(1),
            RecordSystemEvents = true,
            RecordEntityEvents = true,
            RecordComponentEvents = false // Minimize overhead
        };

        worldWithReplay.InstallPlugin(new ReplayPlugin(serializer, replayOptions));
        PopulateWorld(worldWithReplay, EntityCount);
        worldWithReplay.AddSystem(new MovementSystem(), SystemPhase.Update);

        recorder = worldWithReplay.GetExtension<ReplayRecorder>();
        recorder.StartRecording("Benchmark Session");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        recorder.CancelRecording();
        world.Dispose();
        worldWithReplay.Dispose();
    }

    /// <summary>
    /// Baseline: Update world without any replay recording.
    /// </summary>
    [Benchmark(Baseline = true)]
    public void UpdateWithoutReplay()
    {
        world.Update(0.016f);
    }

    /// <summary>
    /// Update world with replay recording enabled.
    /// </summary>
    [Benchmark]
    public void UpdateWithReplay()
    {
        worldWithReplay.Update(0.016f);
    }

    private static void PopulateWorld(World w, int count)
    {
        for (var i = 0; i < count; i++)
        {
            w.Spawn()
                .With(new Position { X = i * 1.5f, Y = i * 2.5f })
                .With(new Velocity { X = i * 0.1f, Y = i * 0.2f })
                .Build();
        }
    }
}

/// <summary>
/// Benchmarks for replay snapshot capture performance.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class ReplaySnapshotBenchmarks
{
    private World world = null!;
    private BenchmarkComponentSerializer serializer = null!;

    [Params(100, 1000, 10000)]
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
                .With(new Health { Current = 100 - (i % 50), Max = 100 })
                .Build();
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        world.Dispose();
    }

    /// <summary>
    /// Measures time to create a world snapshot for replay.
    /// </summary>
    [Benchmark]
    public WorldSnapshot CreateSnapshot()
    {
        return SnapshotManager.CreateSnapshot(world, serializer);
    }
}

/// <summary>
/// Benchmarks for replay file serialization and compression.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class ReplaySerializationBenchmarks
{
    private ReplayData replayData = null!;
    private byte[] serializedData = null!;

    [Params(60, 600, 3600)]
    public int FrameCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        // Create synthetic replay data
        var frames = new List<ReplayFrame>();
        var snapshots = new List<SnapshotMarker>();

        for (var i = 0; i < FrameCount; i++)
        {
            var events = new List<ReplayEvent>
            {
                new ReplayEvent { Type = ReplayEventType.FrameStart, Timestamp = TimeSpan.Zero },
                new ReplayEvent { Type = ReplayEventType.SystemStart, SystemTypeName = "MovementSystem", Timestamp = TimeSpan.Zero },
                new ReplayEvent { Type = ReplayEventType.SystemEnd, SystemTypeName = "MovementSystem", Timestamp = TimeSpan.FromMilliseconds(1) },
                new ReplayEvent { Type = ReplayEventType.FrameEnd, Timestamp = TimeSpan.FromMilliseconds(16) }
            };

            frames.Add(new ReplayFrame
            {
                FrameNumber = i,
                DeltaTime = TimeSpan.FromMilliseconds(16.67),
                ElapsedTime = TimeSpan.FromMilliseconds(i * 16.67),
                Events = events
            });
        }

        replayData = new ReplayData
        {
            Name = "Benchmark Replay",
            RecordingStarted = DateTimeOffset.UtcNow,
            RecordingEnded = DateTimeOffset.UtcNow.AddSeconds(FrameCount / 60.0),
            Duration = TimeSpan.FromSeconds(FrameCount / 60.0),
            FrameCount = FrameCount,
            Frames = frames,
            Snapshots = snapshots
        };

        // Pre-serialize for read benchmarks
        serializedData = ReplayFileFormat.Write(replayData, new ReplayFileOptions
        {
            Compression = ReplayCompressionMode.GZip
        });
    }

    /// <summary>
    /// Measures serialization without compression.
    /// </summary>
    [Benchmark]
    public byte[] SerializeNoCompression()
    {
        return ReplayFileFormat.Write(replayData, new ReplayFileOptions
        {
            Compression = ReplayCompressionMode.None,
            IncludeChecksum = false
        });
    }

    /// <summary>
    /// Measures serialization with GZip compression.
    /// </summary>
    [Benchmark(Baseline = true)]
    public byte[] SerializeGZip()
    {
        return ReplayFileFormat.Write(replayData, new ReplayFileOptions
        {
            Compression = ReplayCompressionMode.GZip,
            IncludeChecksum = true
        });
    }

    /// <summary>
    /// Measures serialization with Brotli compression.
    /// </summary>
    [Benchmark]
    public byte[] SerializeBrotli()
    {
        return ReplayFileFormat.Write(replayData, new ReplayFileOptions
        {
            Compression = ReplayCompressionMode.Brotli,
            IncludeChecksum = true
        });
    }

    /// <summary>
    /// Measures deserialization with checksum validation.
    /// </summary>
    [Benchmark]
    public ReplayData Deserialize()
    {
        var (_, data) = ReplayFileFormat.Read(serializedData, validateChecksum: true);
        return data;
    }

    /// <summary>
    /// Measures reading metadata only (no full data load).
    /// </summary>
    [Benchmark]
    public ReplayFileInfo ReadMetadataOnly()
    {
        return ReplayFileFormat.ReadMetadata(serializedData);
    }
}

/// <summary>
/// Benchmarks for ring buffer mode performance.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class ReplayRingBufferBenchmarks
{
    private World world = null!;
    private BenchmarkComponentSerializer serializer = null!;
    private ReplayRecorder recorder = null!;

    [Params(30, 100, 300)]
    public int RingBufferSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        world = new World();
        serializer = new BenchmarkComponentSerializer();

        var options = new ReplayOptions
        {
            SnapshotInterval = TimeSpan.FromSeconds(1),
            MaxFrames = RingBufferSize,
            UseRingBuffer = true,
            RecordSystemEvents = false,
            RecordEntityEvents = false
        };

        world.InstallPlugin(new ReplayPlugin(serializer, options));

        // Add some entities
        for (var i = 0; i < 100; i++)
        {
            world.Spawn()
                .With(new Position { X = i * 1.5f, Y = i * 2.5f })
                .With(new Velocity { X = i * 0.1f, Y = i * 0.2f })
                .Build();
        }

        world.AddSystem(new MovementSystem(), SystemPhase.Update);
        recorder = world.GetExtension<ReplayRecorder>();
        recorder.StartRecording("Ring Buffer Benchmark");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        recorder.CancelRecording();
        world.Dispose();
    }

    /// <summary>
    /// Measures update performance with ring buffer at capacity.
    /// </summary>
    [Benchmark]
    public void UpdateAtCapacity()
    {
        // Fill the buffer first
        for (var i = 0; i < RingBufferSize + 10; i++)
        {
            world.Update(0.016f);
        }
    }

    /// <summary>
    /// Measures time to extract ring buffer contents.
    /// </summary>
    [Benchmark]
    public ReplayData ExtractBuffer()
    {
        // Fill buffer
        for (var i = 0; i < RingBufferSize; i++)
        {
            world.Update(0.016f);
        }

        var data = recorder.StopRecording();

        // Restart for next iteration
        recorder.StartRecording("Ring Buffer Benchmark");

        return data!;
    }
}

/// <summary>
/// Benchmarks comparing replay file sizes with different configurations.
/// </summary>
[ShortRunJob]
public class ReplayFileSizeBenchmarks
{
    private ReplayData smallReplay = null!;
    private ReplayData mediumReplay = null!;
    private ReplayData largeReplay = null!;

    [GlobalSetup]
    public void Setup()
    {
        smallReplay = CreateReplayData(60, 10);    // 1 second @ 60fps, 10 events/frame
        mediumReplay = CreateReplayData(3600, 10); // 1 minute @ 60fps
        largeReplay = CreateReplayData(3600, 50);  // 1 minute @ 60fps, 50 events/frame
    }

    private static ReplayData CreateReplayData(int frameCount, int eventsPerFrame)
    {
        var frames = new List<ReplayFrame>();

        for (var i = 0; i < frameCount; i++)
        {
            var events = new List<ReplayEvent>();
            for (var j = 0; j < eventsPerFrame; j++)
            {
                events.Add(new ReplayEvent
                {
                    Type = j % 4 == 0 ? ReplayEventType.SystemStart : ReplayEventType.Custom,
                    CustomType = j % 4 != 0 ? $"Event_{j}" : null,
                    Timestamp = TimeSpan.FromMilliseconds(j)
                });
            }

            frames.Add(new ReplayFrame
            {
                FrameNumber = i,
                DeltaTime = TimeSpan.FromMilliseconds(16.67),
                ElapsedTime = TimeSpan.FromMilliseconds(i * 16.67),
                Events = events
            });
        }

        return new ReplayData
        {
            Name = $"Size Benchmark ({frameCount} frames)",
            RecordingStarted = DateTimeOffset.UtcNow,
            Duration = TimeSpan.FromSeconds(frameCount / 60.0),
            FrameCount = frameCount,
            Frames = frames,
            Snapshots = []
        };
    }

    /// <summary>
    /// Returns uncompressed size of small replay (1 second).
    /// </summary>
    [Benchmark]
    public int SmallReplayUncompressed()
    {
        return ReplayFileFormat.Write(smallReplay, new ReplayFileOptions
        {
            Compression = ReplayCompressionMode.None
        }).Length;
    }

    /// <summary>
    /// Returns GZip compressed size of small replay.
    /// </summary>
    [Benchmark]
    public int SmallReplayGZip()
    {
        return ReplayFileFormat.Write(smallReplay, new ReplayFileOptions
        {
            Compression = ReplayCompressionMode.GZip
        }).Length;
    }

    /// <summary>
    /// Returns Brotli compressed size of small replay.
    /// </summary>
    [Benchmark]
    public int SmallReplayBrotli()
    {
        return ReplayFileFormat.Write(smallReplay, new ReplayFileOptions
        {
            Compression = ReplayCompressionMode.Brotli
        }).Length;
    }

    /// <summary>
    /// Returns GZip compressed size of medium replay (1 minute).
    /// </summary>
    [Benchmark(Baseline = true)]
    public int MediumReplayGZip()
    {
        return ReplayFileFormat.Write(mediumReplay, new ReplayFileOptions
        {
            Compression = ReplayCompressionMode.GZip
        }).Length;
    }

    /// <summary>
    /// Returns Brotli compressed size of large replay (1 minute, many events).
    /// </summary>
    [Benchmark]
    public int LargeReplayBrotli()
    {
        return ReplayFileFormat.Write(largeReplay, new ReplayFileOptions
        {
            Compression = ReplayCompressionMode.Brotli
        }).Length;
    }
}
