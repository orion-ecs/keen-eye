using BenchmarkDotNet.Attributes;

using KeenEyes;
using KeenEyes.Editor.Assets;

namespace KeenEyes.Editor.Benchmarks;

/// <summary>
/// Measures <see cref="SceneSerializer"/> save and load of a generated scene: the cost of
/// persisting an authored scene to a <c>.kescene</c> file and reopening it. Save writes the
/// captured world to disk; Load parses the file and reconstructs every entity, component,
/// and parent/child relationship into a fresh world.
/// </summary>
[MemoryDiagnoser]
public class SceneSerializationBenchmarks
{
    private World sourceWorld = null!;
    private SceneSerializer serializer = null!;
    private string scenePath = null!;

    [Params(1000, 5000)]
    public int EntityCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        serializer = new SceneSerializer();

        sourceWorld = new World();
        SceneGenerator.Generate(sourceWorld, EntityCount);

        scenePath = Path.Combine(
            Path.GetTempPath(),
            $"keeneyes-bench-scene-{EntityCount}-{Guid.NewGuid():N}.kescene");

        // Materialize the file so Load has real content to read from the first iteration.
        serializer.Save(sourceWorld, "Benchmark", scenePath);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        sourceWorld.Dispose();
        if (File.Exists(scenePath))
        {
            File.Delete(scenePath);
        }
    }

    /// <summary>
    /// Captures the world and writes the scene file to disk.
    /// </summary>
    [Benchmark]
    public void Save() => serializer.Save(sourceWorld, "Benchmark", scenePath);

    /// <summary>
    /// Parses the scene file and rebuilds the full entity graph into a fresh world.
    /// </summary>
    [Benchmark]
    public void Load()
    {
        using var world = new World();
        _ = SceneSerializer.Load(world, scenePath);
    }
}
