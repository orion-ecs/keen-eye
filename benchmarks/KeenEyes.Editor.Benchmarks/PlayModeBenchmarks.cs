using BenchmarkDotNet.Attributes;

using KeenEyes;
using KeenEyes.Editor.PlayMode;
using KeenEyes.Editor.Serialization;

namespace KeenEyes.Editor.Benchmarks;

/// <summary>
/// Measures play-mode enter and exit at scale using the exact production wiring from
/// <c>EditorApplication</c>: a real <see cref="PlayModeManager"/> backed by a real
/// <see cref="EditorComponentSerializer"/>.
/// </summary>
/// <remarks>
/// <para>
/// This is the deepest headless-constructible layer. <c>PlayModeManager</c> itself needs
/// only a <c>World</c> and an <c>IComponentSerializer</c> - no window, renderer, or running
/// application - so the benchmark exercises the genuine snapshot/restore path rather than a
/// stand-in. Enter captures a <c>WorldSnapshot</c> of the whole scene; exit clears the world
/// and rebuilds it from that snapshot.
/// </para>
/// <para>
/// These operations mutate world state, so the class uses per-target iteration setup to
/// guarantee the manager is in the correct state before each timed call. Each snapshot
/// operation costs hundreds of milliseconds, so BenchmarkDotNet naturally pilots to a single
/// invocation per iteration (with unroll factor 1), keeping every timed call a real state
/// transition.
/// </para>
/// </remarks>
[MemoryDiagnoser]
public class PlayModeBenchmarks
{
    private World world = null!;
    private PlayModeManager manager = null!;

    [Params(1000, 5000)]
    public int EntityCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        world = new World();
        SceneGenerator.Generate(world, EntityCount);
        manager = new PlayModeManager(world, new EditorComponentSerializer());
    }

    [GlobalCleanup]
    public void Cleanup() => world.Dispose();

    [IterationSetup(Target = nameof(PlayEnter))]
    public void EnsureEditing()
    {
        if (manager.IsInPlayMode)
        {
            manager.Stop();
        }
    }

    [IterationSetup(Target = nameof(PlayExit))]
    public void EnsurePlaying()
    {
        if (manager.IsEditing)
        {
            manager.Play();
        }
    }

    /// <summary>
    /// Enters play mode: captures a full world snapshot.
    /// </summary>
    [Benchmark]
    public bool PlayEnter() => manager.Play();

    /// <summary>
    /// Exits play mode: clears the world and restores it from the snapshot.
    /// </summary>
    [Benchmark]
    public bool PlayExit() => manager.Stop();
}
