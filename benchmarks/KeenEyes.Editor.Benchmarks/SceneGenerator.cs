using System.Numerics;

using KeenEyes;

namespace KeenEyes.Editor.Benchmarks;

/// <summary>
/// Deterministic generator for editor benchmark scenes.
/// </summary>
/// <remarks>
/// <para>
/// Produces a reproducible forest of entities with a fixed branching factor so that
/// every entity count maps to a stable 4-level hierarchy (depths 0-3). Component
/// assignment is driven purely by entity index; all numeric field values come from a
/// seeded <see cref="Random"/>, so a given (count, seed) pair always yields byte-for-byte
/// identical scenes across runs.
/// </para>
/// <para>
/// The mix intentionally spans several archetypes: transforms on every entity, tags,
/// gameplay data, and custom string/enum-bearing components. This mirrors a realistic
/// authored scene rather than a single hot archetype.
/// </para>
/// </remarks>
public static class SceneGenerator
{
    /// <summary>
    /// Branching factor. With 17 children per node, counts up to 5,220 stay within four
    /// levels (1 + 17 + 289 + 4,913), covering both the 1,000 and 5,000 entity scales.
    /// </summary>
    private const int Branching = 17;

    private static readonly string[] materials =
    [
        "Standard", "Metal", "Glass", "Foliage", "Terrain", "Skin", "Emissive", "Water"
    ];

    /// <summary>
    /// Generates <paramref name="count"/> entities into <paramref name="world"/>.
    /// </summary>
    /// <param name="world">The world to populate.</param>
    /// <param name="count">The number of entities to create.</param>
    /// <param name="seed">Seed for deterministic field values.</param>
    /// <param name="sceneRoot">
    /// Optional scene root. When valid, every generated entity is registered with the
    /// scene via <see cref="SceneManager.AddToScene"/> so hierarchy-panel style traversal
    /// sees them as scene members.
    /// </param>
    /// <returns>The generated entities, in creation order.</returns>
    public static IReadOnlyList<Entity> Generate(
        World world,
        int count,
        int seed = 1337,
        Entity sceneRoot = default)
    {
        var random = new Random(seed);
        var entities = new Entity[count];
        var depths = new int[count];

        for (int i = 0; i < count; i++)
        {
            var depth = i == 0 ? 0 : depths[(i - 1) / Branching] + 1;
            depths[i] = depth;

            var builder = world.Spawn($"Entity_{i}")
                .With(new EditorTransform
                {
                    Position = new Vector3(
                        (float)(random.NextDouble() * 200.0 - 100.0),
                        (float)(random.NextDouble() * 200.0 - 100.0),
                        (float)(random.NextDouble() * 200.0 - 100.0)),
                    Rotation = Quaternion.CreateFromYawPitchRoll(
                        (float)(random.NextDouble() * Math.PI * 2.0),
                        (float)(random.NextDouble() * Math.PI * 2.0),
                        (float)(random.NextDouble() * Math.PI * 2.0)),
                    Scale = Vector3.One
                });

            // Grouping entities (upper levels) are static containers; deeper entities are
            // dynamic actors. This produces a realistic spread of distinct archetypes.
            if (depth <= 1)
            {
                builder.With(new StaticTag());
                builder.With(new RenderMeta
                {
                    MaterialName = materials[i % materials.Length],
                    LayerId = depth,
                    Visible = true,
                    Queue = RenderQueue.Geometry
                });
            }
            else
            {
                builder.With(new Velocity
                {
                    Linear = new Vector3(
                        (float)(random.NextDouble() * 2.0 - 1.0),
                        (float)(random.NextDouble() * 2.0 - 1.0),
                        (float)(random.NextDouble() * 2.0 - 1.0))
                });
                builder.With(new SelectableTag());
                builder.With(new Health
                {
                    Current = 50 + random.Next(0, 50),
                    Max = 100
                });
            }

            // Every third entity gets an extra custom component, widening archetype variety.
            if (i % 3 == 0)
            {
                builder.With(new SpawnInfo
                {
                    Seed = random.Next(),
                    Weight = (float)random.NextDouble()
                });
            }

            // Roughly half the actors carry render metadata as well.
            if (depth >= 2 && i % 2 == 0)
            {
                builder.With(new RenderMeta
                {
                    MaterialName = materials[i % materials.Length],
                    LayerId = depth,
                    Visible = i % 5 != 0,
                    Queue = (RenderQueue)(i % 4)
                });
            }

            var entity = builder.Build();
            entities[i] = entity;

            if (i > 0)
            {
                world.SetParent(entity, entities[(i - 1) / Branching]);
            }

            if (sceneRoot.IsValid && world.IsAlive(sceneRoot))
            {
                world.Scenes.AddToScene(entity, sceneRoot);
            }
        }

        return entities;
    }
}
