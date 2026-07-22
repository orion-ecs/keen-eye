using BenchmarkDotNet.Attributes;

using KeenEyes;

namespace KeenEyes.Editor.Benchmarks;

/// <summary>
/// Measures a single scene-world <see cref="World.Update"/> tick with representative
/// editor-side systems registered (no rendering). This is the "editor FPS" proxy: the
/// per-frame CPU cost the editor pays to simulate an open scene.
/// </summary>
[MemoryDiagnoser]
public class UpdateTickBenchmarks
{
    private World world = null!;

    [Params(1000, 5000)]
    public int EntityCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        world = new World();
        world.AddSystem(new MovementSystem());
        world.AddSystem(new HealthRegenSystem());

        SceneGenerator.Generate(world, EntityCount);

        // Prime archetype/query caches and the change tracker with one warm tick.
        world.Update(0.016f);
    }

    [GlobalCleanup]
    public void Cleanup() => world.Dispose();

    /// <summary>
    /// One 60fps frame tick over the whole scene.
    /// </summary>
    [Benchmark]
    public void UpdateTick() => world.Update(0.016f);
}

/// <summary>
/// Representative motion system: integrates transforms of dynamic actors.
/// </summary>
internal sealed class MovementSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<EditorTransform, Velocity>())
        {
            ref var transform = ref World.Get<EditorTransform>(entity);
            ref readonly var velocity = ref World.Get<Velocity>(entity);
            transform.Position += velocity.Linear * deltaTime;
        }
    }
}

/// <summary>
/// Representative gameplay system: regenerates health toward its maximum.
/// </summary>
internal sealed class HealthRegenSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<Health>())
        {
            ref var health = ref World.Get<Health>(entity);
            if (health.Current < health.Max)
            {
                health.Current++;
            }
        }
    }
}
