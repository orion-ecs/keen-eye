using System.Diagnostics;
using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Spatial;

namespace KeenEyes.Sample.CollisionDetection;

/// <summary>
/// Demonstrates broadphase/narrowphase collision detection using spatial partitioning.
/// Compares performance against naive O(n²) approach.
/// </summary>
public static class Program
{
    /// <summary>Number of entities to simulate.</summary>
    public const int EntityCount = 1000;

    /// <summary>Width and depth of the square world in units.</summary>
    public const float WorldSize = 1000f;

    /// <summary>Collision radius for each entity.</summary>
    public const float EntityRadius = 5f;

    /// <summary>Maximum entity speed in units per second.</summary>
    public const float MaxSpeed = 50f;

    /// <summary>Number of simulation frames to run per strategy.</summary>
    public const int FrameCount = 100;

    /// <summary>Application entry point.</summary>
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
        using var world = new World(seed: 42);

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
        for (int i = 0; i < EntityCount; i++)
        {
            var position = new Vector3(
                world.NextFloat() * WorldSize - WorldSize / 2,
                0,
                world.NextFloat() * WorldSize - WorldSize / 2);

            var velocity = new Vector3(
                world.NextFloat() * MaxSpeed - MaxSpeed / 2,
                0,
                world.NextFloat() * MaxSpeed - MaxSpeed / 2);

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
