using System.Diagnostics;
using System.Numerics;
using KeenEyes;
using KeenEyes.Common;
using KeenEyes.Spatial;

/// <summary>
/// Demonstrates broadphase/narrowphase collision detection using spatial partitioning.
/// Compares performance against naive O(n²) approach.
/// </summary>
public class Program
{
    public const int EntityCount = 1000;
    public const float WorldSize = 1000f;
    public const float EntityRadius = 5f;
    public const float MaxSpeed = 50f;
    public const int FrameCount = 100;

    public static void Main()
    {
        Console.WriteLine("=== Collision Detection Sample ===\n");
        Console.WriteLine($"Simulating {EntityCount} entities in {WorldSize}x{WorldSize} world");
        Console.WriteLine($"Entity radius: {EntityRadius} units");
        Console.WriteLine($"Running {FrameCount} frames...\n");

        // Run with spatial partitioning (Grid)
        Console.WriteLine("--- Grid Strategy ---");
        RunSimulation(SpatialStrategy.Grid, useSpatial: true);

        // Run with spatial partitioning (Quadtree)
        Console.WriteLine("\n--- Quadtree Strategy ---");
        RunSimulation(SpatialStrategy.Quadtree, useSpatial: true);

        // Run naive O(n²) approach
        Console.WriteLine("\n--- Naive O(n²) Approach ---");
        RunSimulation(SpatialStrategy.Grid, useSpatial: false);

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }

    private static void RunSimulation(SpatialStrategy strategy, bool useSpatial)
    {
        using var world = new World();

        // Install spatial plugin with appropriate strategy
        var config = strategy switch
        {
            SpatialStrategy.Grid => new SpatialConfig
            {
                Strategy = SpatialStrategy.Grid,
                Grid = new GridConfig
                {
                    CellSize = EntityRadius * 4f,  // Cell size = 4x entity radius
                    WorldMin = new Vector3(-WorldSize / 2, -10, -WorldSize / 2),
                    WorldMax = new Vector3(WorldSize / 2, 10, WorldSize / 2)
                }
            },
            SpatialStrategy.Quadtree => new SpatialConfig
            {
                Strategy = SpatialStrategy.Quadtree,
                Quadtree = new QuadtreeConfig
                {
                    MaxDepth = 8,
                    MaxEntitiesPerNode = 8,
                    WorldMin = new Vector3(-WorldSize / 2, -10, -WorldSize / 2),
                    WorldMax = new Vector3(WorldSize / 2, 10, WorldSize / 2)
                }
            },
            _ => throw new ArgumentException($"Unsupported strategy: {strategy}")
        };

        world.InstallPlugin(new SpatialPlugin(config));

        // Spawn entities with random positions and velocities
        var random = new Random(42);  // Fixed seed for consistent results
        for (int i = 0; i < EntityCount; i++)
        {
            var position = new Vector3(
                random.NextSingle() * WorldSize - WorldSize / 2,
                0,
                random.NextSingle() * WorldSize - WorldSize / 2);

            var velocity = new Vector3(
                random.NextSingle() * MaxSpeed - MaxSpeed / 2,
                0,
                random.NextSingle() * MaxSpeed - MaxSpeed / 2);

            world.Spawn()
                .With(new Transform3D(position, Quaternion.Identity, Vector3.One))
                .With(new Velocity { Value = velocity })
                .With(new CollisionRadius { Value = EntityRadius })
                .WithTag<SpatialIndexed>()
                .Build();
        }

        // Create stats tracker
        var stats = new CollisionStats();

        // Add systems
        world.AddSystem(new MovementSystem(), SystemPhase.Update, order: 0);

        if (useSpatial)
        {
            world.AddSystem(new SpatialCollisionSystem(stats), SystemPhase.Update, order: 10);
        }
        else
        {
            world.AddSystem(new NaiveCollisionSystem(stats), SystemPhase.Update, order: 10);
        }

        // Run simulation
        var stopwatch = Stopwatch.StartNew();
        for (int frame = 0; frame < FrameCount; frame++)
        {
            world.Update(deltaTime: 0.016f);  // 60 FPS (16ms per frame)

            if (frame % 50 == 0)
            {
                Console.Write(".");  // Progress indicator
            }
        }
        stopwatch.Stop();

        // Print results
        Console.WriteLine();
        Console.WriteLine($"Total time: {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"Average frame time: {stopwatch.ElapsedMilliseconds / (float)FrameCount:F2}ms");
        Console.WriteLine($"Total collisions detected: {stats.TotalCollisions}");
        Console.WriteLine($"Average collisions/frame: {stats.TotalCollisions / (float)FrameCount:F1}");

        if (useSpatial)
        {
            Console.WriteLine($"Broadphase candidates: {stats.BroadphaseChecks}");
            Console.WriteLine($"Narrowphase checks: {stats.NarrowphaseChecks}");
            Console.WriteLine($"False positive rate: {(stats.BroadphaseChecks > 0 ? (stats.BroadphaseChecks - stats.NarrowphaseChecks) / (float)stats.BroadphaseChecks * 100f : 0):F1}%");
        }
        else
        {
            Console.WriteLine($"Total entity pair checks: {stats.BroadphaseChecks}");
        }
    }
}

/// <summary>
/// Class for tracking collision statistics.
/// </summary>
public class CollisionStats
{
    public int TotalCollisions;
    public int BroadphaseChecks;
    public int NarrowphaseChecks;
}

/// <summary>
/// Component representing entity velocity.
/// </summary>
[Component]
public partial struct Velocity
{
    public Vector3 Value;
}

/// <summary>
/// Component representing collision radius.
/// </summary>
[Component]
public partial struct CollisionRadius
{
    public float Value;
}

/// <summary>
/// System that moves entities based on their velocity.
/// </summary>
public class MovementSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<Transform3D, Velocity>())
        {
            ref var transform = ref World.Get<Transform3D>(entity);
            ref readonly var velocity = ref World.Get<Velocity>(entity);

            // Move entity
            transform.Position += velocity.Value * deltaTime;

            // Wrap around world bounds (simple toroidal world)
            const float halfSize = Program.WorldSize / 2;
            if (transform.Position.X < -halfSize)
            {
                transform.Position.X += Program.WorldSize;
            }

            if (transform.Position.X > halfSize)
            {
                transform.Position.X -= Program.WorldSize;
            }

            if (transform.Position.Z < -halfSize)
            {
                transform.Position.Z += Program.WorldSize;
            }

            if (transform.Position.Z > halfSize)
            {
                transform.Position.Z -= Program.WorldSize;
            }
        }
    }
}

/// <summary>
/// System that detects collisions using spatial partitioning (broadphase + narrowphase).
/// </summary>
public class SpatialCollisionSystem(CollisionStats stats) : SystemBase
{
    private SpatialQueryApi? spatial;

    protected override void OnInitialize()
    {
        spatial = World.GetExtension<SpatialQueryApi>();
    }

    public override void Update(float deltaTime)
    {
        // Check collisions for all entities
        foreach (var entity in World.Query<Transform3D, CollisionRadius>())
        {
            ref readonly var transform = ref World.Get<Transform3D>(entity);
            ref readonly var radius = ref World.Get<CollisionRadius>(entity);

            // Broadphase: query nearby entities using spatial index
            foreach (var other in spatial!.QueryRadius(transform.Position, radius.Value * 2))
            {
                if (other.Id <= entity.Id)
                {
                    continue;  // Skip self and avoid duplicate pairs
                }

                stats.BroadphaseChecks++;

                // Narrowphase: exact sphere-sphere collision test
                ref readonly var otherTransform = ref World.Get<Transform3D>(other);
                ref readonly var otherRadius = ref World.Get<CollisionRadius>(other);

                float combinedRadius = radius.Value + otherRadius.Value;
                float distSq = Vector3.DistanceSquared(transform.Position, otherTransform.Position);

                stats.NarrowphaseChecks++;

                if (distSq <= combinedRadius * combinedRadius)
                {
                    // Collision detected!
                    stats.TotalCollisions++;
                    HandleCollision(entity, other);
                }
            }
        }
    }

    private void HandleCollision(Entity a, Entity b)
    {
        // In a real game, you might:
        // - Trigger events
        // - Apply physics forces
        // - Deal damage
        // - Play sound effects
        // For this sample, we just count collisions
    }
}

/// <summary>
/// System that detects collisions using naive O(n²) approach (no spatial partitioning).
/// </summary>
public class NaiveCollisionSystem(CollisionStats stats) : SystemBase
{

    public override void Update(float deltaTime)
    {
        // Get all entities (inefficient!)
        var entities = World.Query<Transform3D, CollisionRadius>().ToList();

        // Check every pair (O(n²))
        for (int i = 0; i < entities.Count; i++)
        {
            var entity = entities[i];
            ref readonly var transform = ref World.Get<Transform3D>(entity);
            ref readonly var radius = ref World.Get<CollisionRadius>(entity);

            for (int j = i + 1; j < entities.Count; j++)
            {
                var other = entities[j];
                stats.BroadphaseChecks++;  // Count all pair checks

                ref readonly var otherTransform = ref World.Get<Transform3D>(other);
                ref readonly var otherRadius = ref World.Get<CollisionRadius>(other);

                float combinedRadius = radius.Value + otherRadius.Value;
                float distSq = Vector3.DistanceSquared(transform.Position, otherTransform.Position);

                if (distSq <= combinedRadius * combinedRadius)
                {
                    // Collision detected!
                    stats.TotalCollisions++;
                    HandleCollision(entity, other);
                }
            }
        }
    }

    private void HandleCollision(Entity a, Entity b)
    {
        // For this sample, we just count collisions
    }
}
