using System.Diagnostics;
using System.Numerics;
using KeenEyes;
using KeenEyes.Common;
using KeenEyes.Spatial;

/// <summary>
/// Demonstrates frustum culling for 3D rendering optimization.
/// Compares rendering with/without spatial frustum queries.
/// </summary>
public class Program
{
    public const int EntityCount = 5000;
    public const float WorldSize = 2000f;
    public const int FrameCount = 100;

    // Camera settings
    public const float CameraFov = 60f;
    public const float CameraAspect = 16f / 9f;
    public const float CameraNear = 1f;
    public const float CameraFar = 1000f;

    public static void Main()
    {
        Console.WriteLine("=== Rendering Culling Sample ===\n");
        Console.WriteLine($"Simulating {EntityCount} entities in {WorldSize}x{WorldSize}x{WorldSize} world");
        Console.WriteLine($"Camera FOV: {CameraFov}°, Aspect: {CameraAspect:F2}, Far: {CameraFar}");
        Console.WriteLine($"Running {FrameCount} frames...\n");

        // Run with frustum culling (spatial partitioning)
        Console.WriteLine("--- With Frustum Culling (Octree) ---");
        RunSimulation(useFrustumCulling: true);

        // Run without frustum culling (render all entities)
        Console.WriteLine("\n--- Without Frustum Culling (Naive) ---");
        RunSimulation(useFrustumCulling: false);

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }

    private static void RunSimulation(bool useFrustumCulling)
    {
        using var world = new World(seed: 42);

        // Install spatial plugin with Octree (best for 3D)
        var config = new SpatialConfig
        {
            Strategy = SpatialStrategy.Octree,
            Octree = new OctreeConfig
            {
                MaxDepth = 6,
                MaxEntitiesPerNode = 8,
                WorldMin = new Vector3(-WorldSize / 2, -WorldSize / 2, -WorldSize / 2),
                WorldMax = new Vector3(WorldSize / 2, WorldSize / 2, WorldSize / 2)
            }
        };

        world.InstallPlugin(new SpatialPlugin(config));

        // Spawn entities scattered throughout 3D space
        for (int i = 0; i < EntityCount; i++)
        {
            var position = new Vector3(
                world.NextFloat() * WorldSize - WorldSize / 2,
                world.NextFloat() * WorldSize - WorldSize / 2,
                world.NextFloat() * WorldSize - WorldSize / 2);

            world.Spawn()
                .With(new Transform3D(position, Quaternion.Identity, Vector3.One))
                .With(new Renderable { MeshId = i % 10 })  // 10 different mesh types
                .WithTag<SpatialIndexed>()
                .Build();
        }

        // Create camera that orbits around origin
        var camera = world.Spawn()
            .With(new Transform3D(
                new Vector3(0, 200, 500),
                Quaternion.Identity,
                Vector3.One))
            .With(new Camera())
            .Build();

        // Create stats tracker
        var stats = new RenderingStats();

        // Add systems
        world.AddSystem(new CameraOrbitSystem(camera), SystemPhase.Update, order: 0);

        if (useFrustumCulling)
        {
            world.AddSystem(new FrustumCullingRenderSystem(camera, stats), SystemPhase.Render, order: 0);
        }
        else
        {
            world.AddSystem(new NaiveRenderSystem(stats), SystemPhase.Render, order: 0);
        }

        // Run simulation
        var stopwatch = Stopwatch.StartNew();
        for (int frame = 0; frame < FrameCount; frame++)
        {
            world.Update(deltaTime: 0.016f);  // 60 FPS

            if (frame % 50 == 0)
            {
                Console.Write(".");
            }
        }
        stopwatch.Stop();

        // Print results
        Console.WriteLine();
        Console.WriteLine($"Total time: {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"Average frame time: {stopwatch.ElapsedMilliseconds / (float)FrameCount:F2}ms");
        Console.WriteLine($"Total entities rendered: {stats.TotalRendered}");
        Console.WriteLine($"Average entities/frame: {stats.TotalRendered / (float)FrameCount:F1}");

        if (useFrustumCulling)
        {
            Console.WriteLine($"Total culled: {stats.TotalCulled}");
            Console.WriteLine($"Average culled/frame: {stats.TotalCulled / (float)FrameCount:F1}");
            Console.WriteLine($"Culling efficiency: {(stats.TotalCulled / (float)(stats.TotalRendered + stats.TotalCulled)) * 100f:F1}%");
            Console.WriteLine($"Broadphase candidates: {stats.BroadphaseCandidates}");
            Console.WriteLine($"False positive rate: {(stats.BroadphaseCandidates > 0 ? (stats.BroadphaseCandidates - stats.TotalRendered) / (float)stats.BroadphaseCandidates * 100f : 0):F1}%");
        }
    }
}

/// <summary>
/// Stats for tracking rendering performance.
/// </summary>
public class RenderingStats
{
    public int TotalRendered;
    public int TotalCulled;
    public int BroadphaseCandidates;
}

/// <summary>
/// Component marking an entity as renderable.
/// </summary>
[Component]
public partial struct Renderable
{
    public int MeshId;
}

/// <summary>
/// Component representing a camera.
/// </summary>
[Component]
public partial struct Camera
{
    public bool IsActive;
}

/// <summary>
/// System that orbits the camera around the origin.
/// </summary>
public class CameraOrbitSystem(Entity cameraEntity) : SystemBase
{
    private float angle = 0f;
    private const float OrbitRadius = 500f;
    private const float OrbitSpeed = 0.5f;  // Radians per second
    private const float OrbitHeight = 200f;

    public override void Update(float deltaTime)
    {
        if (!World.IsAlive(cameraEntity))
        {
            return;
        }

        // Orbit camera around origin
        angle += OrbitSpeed * deltaTime;

        ref var transform = ref World.Get<Transform3D>(cameraEntity);
        transform.Position = new Vector3(
            MathF.Cos(angle) * OrbitRadius,
            OrbitHeight,
            MathF.Sin(angle) * OrbitRadius);

        // Look at origin
        var lookDir = Vector3.Normalize(-transform.Position);
        transform.Rotation = QuaternionFromLookDirection(lookDir, Vector3.UnitY);
    }

    private static Quaternion QuaternionFromLookDirection(Vector3 forward, Vector3 up)
    {
        var right = Vector3.Normalize(Vector3.Cross(up, forward));
        var actualUp = Vector3.Cross(forward, right);

        var m = new Matrix4x4(
            right.X, right.Y, right.Z, 0,
            actualUp.X, actualUp.Y, actualUp.Z, 0,
            -forward.X, -forward.Y, -forward.Z, 0,
            0, 0, 0, 1);

        return Quaternion.CreateFromRotationMatrix(m);
    }
}

/// <summary>
/// System that renders entities visible to camera using frustum culling.
/// </summary>
public class FrustumCullingRenderSystem(Entity cameraEntity, RenderingStats stats) : SystemBase
{
    private SpatialQueryApi? spatial;
    private readonly List<Entity> visibleEntities = [];  // Reused to avoid per-frame allocation

    protected override void OnInitialize()
    {
        spatial = World.GetExtension<SpatialQueryApi>();
    }

    public override void Update(float deltaTime)
    {
        if (!World.IsAlive(cameraEntity) || !World.Has<Camera>(cameraEntity))
        {
            return;
        }

        ref readonly var cameraTransform = ref World.Get<Transform3D>(cameraEntity);

        // Build view-projection matrix
        var viewMatrix = CreateViewMatrix(cameraTransform);
        var projMatrix = CreateProjectionMatrix(
            Program.CameraFov * MathF.PI / 180f,
            Program.CameraAspect,
            Program.CameraNear,
            Program.CameraFar);
        var viewProj = viewMatrix * projMatrix;

        // Extract frustum from view-projection matrix
        var frustum = Frustum.FromMatrix(viewProj);

        // Broadphase: query entities in frustum
        visibleEntities.Clear();  // Clear and reuse to avoid allocation
        foreach (var entity in spatial!.QueryFrustum<Renderable>(frustum))
        {
            stats.BroadphaseCandidates++;

            // Narrowphase: exact frustum test (in real engine, use bounding volumes)
            ref readonly var transform = ref World.Get<Transform3D>(entity);

            // Simple point-in-frustum test (real engine would use AABB or sphere)
            if (frustum.Contains(transform.Position))
            {
                visibleEntities.Add(entity);
            }
        }

        stats.TotalRendered += visibleEntities.Count;
        stats.TotalCulled += Program.EntityCount - visibleEntities.Count;

        // Render visible entities (simulated)
        foreach (var entity in visibleEntities)
        {
            RenderEntity(entity);
        }
    }

    private void RenderEntity(Entity entity)
    {
        // In a real engine, this would:
        // - Submit draw call to graphics API
        // - Update shader uniforms
        // - Bind textures and materials
        // For this sample, we just count renders
    }

    private static Matrix4x4 CreateViewMatrix(Transform3D camera)
    {
        return Matrix4x4.CreateLookAt(
            camera.Position,
            camera.Position + camera.Forward(),
            Vector3.UnitY);
    }

    private static Matrix4x4 CreateProjectionMatrix(float fov, float aspect, float near, float far)
    {
        return Matrix4x4.CreatePerspectiveFieldOfView(fov, aspect, near, far);
    }
}

/// <summary>
/// System that renders all entities without culling (naive approach).
/// </summary>
public class NaiveRenderSystem(RenderingStats stats) : SystemBase
{
    public override void Update(float deltaTime)
    {
        // Render ALL entities (no culling)
        var entityCount = 0;
        foreach (var entity in World.Query<Transform3D, Renderable>())
        {
            entityCount++;
            RenderEntity(entity);
        }

        stats.TotalRendered += entityCount;
        // No entities culled in naive approach
    }

    private void RenderEntity(Entity entity)
    {
        // Simulated rendering
    }
}
