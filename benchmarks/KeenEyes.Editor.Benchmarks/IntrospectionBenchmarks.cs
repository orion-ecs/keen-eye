using BenchmarkDotNet.Attributes;

using KeenEyes;
using KeenEyes.Editor.Common.Inspector;

namespace KeenEyes.Editor.Benchmarks;

/// <summary>
/// Measures <see cref="ComponentIntrospector"/> doing a full inspector pass: enumerating every
/// component on an entity and, for each, resolving editable fields/properties, their metadata,
/// and their current values. This is the inspector-responsiveness proxy - the work done when an
/// entity is selected (one entity) versus a bulk/multi-select refresh (100 entities).
/// </summary>
/// <remarks>
/// The introspector caches reflection metadata per type after first use, matching how the editor
/// reuses it across frames; the global setup warms that cache so the benchmark reflects steady
/// state rather than cold reflection.
/// </remarks>
[MemoryDiagnoser]
public class IntrospectionBenchmarks
{
    private World world = null!;
    private Entity singleEntity;
    private Entity[] hundredEntities = null!;

    [Params(1000, 5000)]
    public int EntityCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        world = new World();
        var entities = SceneGenerator.Generate(world, EntityCount);

        // Pick a deep actor (rich archetype: transform, velocity, health, tags, render meta).
        singleEntity = entities[^1];

        // A representative bulk-select slice from the deeper (actor) portion of the scene.
        hundredEntities = entities.Skip(entities.Count - 100).Take(100).ToArray();

        // Warm the introspector's per-type reflection caches.
        _ = Inspect(singleEntity);
        foreach (var entity in hundredEntities)
        {
            _ = Inspect(entity);
        }
    }

    [GlobalCleanup]
    public void Cleanup() => world.Dispose();

    /// <summary>
    /// Full inspection of a single selected entity.
    /// </summary>
    [Benchmark]
    public int InspectSingleEntity() => Inspect(singleEntity);

    /// <summary>
    /// Full inspection of 100 entities (bulk / multi-select refresh).
    /// </summary>
    [Benchmark]
    public int InspectHundredEntities()
    {
        var acc = 0;
        foreach (var entity in hundredEntities)
        {
            acc += Inspect(entity);
        }

        return acc;
    }

    private int Inspect(Entity entity)
    {
        var acc = 0;

        foreach (var (type, value) in world.GetComponents(entity))
        {
            foreach (var field in ComponentIntrospector.GetEditableFields(type))
            {
                var metadata = ComponentIntrospector.GetFieldMetadata(field);
                acc += metadata.DisplayName.Length;

                var fieldValue = ComponentIntrospector.GetFieldValue(value, field);
                if (fieldValue is not null)
                {
                    acc += fieldValue.GetHashCode();
                }
            }

            foreach (var property in ComponentIntrospector.GetEditableProperties(type))
            {
                var metadata = ComponentIntrospector.GetPropertyMetadata(property);
                acc += metadata.DisplayName.Length;

                var propertyValue = ComponentIntrospector.GetPropertyValue(value, property);
                if (propertyValue is not null)
                {
                    acc += propertyValue.GetHashCode();
                }
            }
        }

        return acc;
    }
}
