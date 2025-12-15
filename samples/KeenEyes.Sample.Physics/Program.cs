using System.Numerics;
using KeenEyes;
using KeenEyes.Common;
using KeenEyes.Physics;
using KeenEyes.Physics.Components;
using KeenEyes.Physics.Core;
using KeenEyes.Physics.Events;
using KeenEyes.Sample.Physics;

// =============================================================================
// KEEN EYES ECS - Physics Plugin Sample
// =============================================================================
// This sample demonstrates the KeenEyes.Physics plugin with three interactive
// demonstrations:
//
// Demo 1: Falling Objects - Shows dynamic bodies falling under gravity with
//         various shapes (spheres, boxes) and materials (rubber, metal, wood).
//
// Demo 2: Stacking & Collision - Demonstrates collision events, material
//         properties, and stable box stacking.
//
// Demo 3: Raycasting - Shows how to use raycasts for object detection and
//         querying the physics world.
//
// See README.md for detailed documentation on each pattern.
// =============================================================================

Console.WriteLine("=".PadRight(70, '='));
Console.WriteLine("  KeenEyes Physics Sample - BepuPhysics v2 Integration");
Console.WriteLine("=".PadRight(70, '='));
Console.WriteLine();

// Run all three demos
RunFallingObjectsDemo();
Console.WriteLine();

RunStackingCollisionDemo();
Console.WriteLine();

RunRaycastingDemo();

Console.WriteLine();
Console.WriteLine("All physics demos completed successfully!");

// =============================================================================
// DEMO 1: FALLING OBJECTS
// =============================================================================
// Demonstrates:
// - Creating dynamic rigid bodies with RigidBody.Dynamic()
// - Different collision shapes (Sphere, Box)
// - Physics materials (Rubber, Metal, Wood)
// - Gravity-driven simulation
// =============================================================================

void RunFallingObjectsDemo()
{
    Console.WriteLine("-".PadRight(70, '-'));
    Console.WriteLine("  Demo 1: Falling Objects");
    Console.WriteLine("-".PadRight(70, '-'));

    // Create world and install physics plugin
    using var world = new World();
    world.InstallPlugin(new PhysicsPlugin(new PhysicsConfig
    {
        Gravity = new Vector3(0, -9.81f, 0),
        FixedTimestep = 1f / 60f
    }));

    // Create ground plane (static body - never moves)
    var ground = world.Spawn()
        .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
        .With(RigidBody.Static())
        .With(PhysicsShape.Box(100, 1, 100))  // Large flat box
        .WithGroundTag()
        .Build();

    Console.WriteLine("Created static ground plane at Y=0");

    // Create falling spheres at different heights with different materials
    var rubberBall = world.Spawn()
        .With(new Transform3D(new Vector3(0, 10, 0), Quaternion.Identity, Vector3.One))
        .With(new Velocity3D(0, 0, 0))
        .With(RigidBody.Dynamic(1.0f))
        .With(PhysicsShape.Sphere(0.5f))
        .With(PhysicsMaterial.Rubber)  // High bounce
        .WithFallingObjectTag()
        .WithEntityLabel("Rubber Ball")
        .Build();

    var metalSphere = world.Spawn()
        .With(new Transform3D(new Vector3(3, 15, 0), Quaternion.Identity, Vector3.One))
        .With(new Velocity3D(0, 0, 0))
        .With(RigidBody.Dynamic(5.0f))  // Heavier
        .With(PhysicsShape.Sphere(0.5f))
        .With(PhysicsMaterial.Metal)
        .WithFallingObjectTag()
        .WithEntityLabel("Metal Sphere")
        .Build();

    // Create falling boxes
    var woodBox = world.Spawn()
        .With(new Transform3D(new Vector3(-3, 12, 0), Quaternion.Identity, Vector3.One))
        .With(new Velocity3D(0, 0, 0))
        .With(RigidBody.Dynamic(2.0f))
        .With(PhysicsShape.Box(1, 1, 1))  // 1x1x1 cube
        .With(PhysicsMaterial.Wood)
        .WithFallingObjectTag()
        .WithEntityLabel("Wood Box")
        .Build();

    Console.WriteLine("Spawned 3 falling objects:");
    Console.WriteLine("  - Rubber Ball at Y=10 (high bounce, mass=1)");
    Console.WriteLine("  - Metal Sphere at Y=15 (moderate bounce, mass=5)");
    Console.WriteLine("  - Wood Box at Y=12 (low bounce, mass=2)");
    Console.WriteLine();

    // Simulate for 3 seconds
    Console.WriteLine("Running physics simulation (3 seconds)...");
    float simTime = 0;
    float totalTime = 3.0f;
    float dt = 1f / 60f;

    while (simTime < totalTime)
    {
        world.FixedUpdate(dt);
        simTime += dt;

        // Print positions every 0.5 seconds
        if (Math.Abs(simTime % 0.5f) < dt)
        {
            Console.WriteLine($"  t={simTime:F1}s:");
            PrintEntityPosition(world, rubberBall, "    Rubber Ball");
            PrintEntityPosition(world, metalSphere, "    Metal Sphere");
            PrintEntityPosition(world, woodBox, "    Wood Box");
        }
    }

    Console.WriteLine("Demo 1 completed - Objects fell and bounced based on materials!");
}

// =============================================================================
// DEMO 2: STACKING & COLLISION EVENTS
// =============================================================================
// Demonstrates:
// - Collision event handling (CollisionEvent, CollisionStartedEvent)
// - Building stable stacks of boxes
// - Collision filters for selective collisions
// - Tracking collision counts
// =============================================================================

void RunStackingCollisionDemo()
{
    Console.WriteLine("-".PadRight(70, '-'));
    Console.WriteLine("  Demo 2: Stacking & Collision Events");
    Console.WriteLine("-".PadRight(70, '-'));

    using var world = new World();
    world.InstallPlugin(new PhysicsPlugin(new PhysicsConfig
    {
        Gravity = new Vector3(0, -9.81f, 0),
        VelocityIterations = 10,  // More iterations for stable stacking
        SubstepCount = 2
    }));

    // Track collision events
    int collisionStartCount = 0;
    int totalCollisions = 0;

    // Subscribe to collision events
    var collisionSub = world.Subscribe<CollisionEvent>(collision =>
    {
        totalCollisions++;
    });

    var collisionStartSub = world.Subscribe<CollisionStartedEvent>(collision =>
    {
        collisionStartCount++;
        Console.WriteLine($"  Collision started between entities {collision.EntityA.Id} and {collision.EntityB.Id}");
    });

    Console.WriteLine("Subscribed to collision events");

    // Create ground
    var ground = world.Spawn()
        .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
        .With(RigidBody.Static())
        .With(PhysicsShape.Box(50, 1, 50))
        .WithGroundTag()
        .Build();

    // Create a stack of 5 boxes
    Console.WriteLine("Creating stack of 5 boxes...");
    var stackedBoxes = new List<Entity>();

    for (int i = 0; i < 5; i++)
    {
        float y = 1.5f + i * 2.1f;  // Stack with slight gap

        var box = world.Spawn()
            .With(new Transform3D(new Vector3(0, y, 0), Quaternion.Identity, Vector3.One))
            .With(new Velocity3D(0, 0, 0))
            .With(RigidBody.Dynamic(1.0f))
            .With(PhysicsShape.Box(2, 2, 2))  // 2x2x2 boxes
            .With(PhysicsMaterial.Wood)
            .WithStackedObjectTag()
            .WithCollisionCounter(0)
            .Build();

        stackedBoxes.Add(box);
        Console.WriteLine($"  Box {i + 1} at Y={y:F1}");
    }

    // Create a ball that will knock over the stack
    Console.WriteLine();
    Console.WriteLine("Creating ball to knock over the stack...");

    var knockerBall = world.Spawn()
        .With(new Transform3D(new Vector3(-20, 5, 0), Quaternion.Identity, Vector3.One))
        .With(new Velocity3D(30, 0, 0))  // Moving fast toward stack
        .With(RigidBody.Dynamic(10.0f))  // Heavy ball
        .With(PhysicsShape.Sphere(2.0f))
        .With(PhysicsMaterial.Metal)
        .WithEntityLabel("Knocker Ball")
        .Build();

    // Run simulation
    Console.WriteLine();
    Console.WriteLine("Running collision simulation (2 seconds)...");

    float simTime = 0;
    float totalTime = 2.0f;
    float dt = 1f / 60f;

    while (simTime < totalTime)
    {
        world.FixedUpdate(dt);
        simTime += dt;
    }

    Console.WriteLine();
    Console.WriteLine($"Collision Statistics:");
    Console.WriteLine($"  New collisions (CollisionStartedEvent): {collisionStartCount}");
    Console.WriteLine($"  Total collision contacts: {totalCollisions}");

    // Check final positions of stacked boxes
    Console.WriteLine();
    Console.WriteLine("Final box positions (stack should be knocked over):");
    for (int i = 0; i < stackedBoxes.Count; i++)
    {
        PrintEntityPosition(world, stackedBoxes[i], $"  Box {i + 1}");
    }

    // Cleanup subscriptions
    collisionSub.Dispose();
    collisionStartSub.Dispose();

    Console.WriteLine("Demo 2 completed - Collision events tracked and stack toppled!");
}

// =============================================================================
// DEMO 3: RAYCASTING
// =============================================================================
// Demonstrates:
// - PhysicsWorld.Raycast() for object detection
// - OverlapSphere() for area queries
// - Using raycast results for gameplay logic
// - Hit information (position, normal, distance)
// =============================================================================

void RunRaycastingDemo()
{
    Console.WriteLine("-".PadRight(70, '-'));
    Console.WriteLine("  Demo 3: Raycasting");
    Console.WriteLine("-".PadRight(70, '-'));

    using var world = new World();
    world.InstallPlugin(new PhysicsPlugin());

    // Get the physics world extension for raycasting
    var physics = world.GetExtension<PhysicsWorld>();

    // Create ground
    var ground = world.Spawn()
        .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
        .With(RigidBody.Static())
        .With(PhysicsShape.Box(100, 1, 100))
        .WithGroundTag()
        .Build();

    // Create several target objects at different positions
    Console.WriteLine("Creating raycast targets...");

    var targets = new List<(Entity entity, string name, Vector3 position)>();

    // Target 1: Directly ahead
    var target1 = world.Spawn()
        .With(new Transform3D(new Vector3(10, 2, 0), Quaternion.Identity, Vector3.One))
        .With(RigidBody.Static())
        .With(PhysicsShape.Sphere(1.0f))
        .WithRaycastTargetTag()
        .Build();
    targets.Add((target1, "Target A (ahead)", new Vector3(10, 2, 0)));

    // Target 2: To the right
    var target2 = world.Spawn()
        .With(new Transform3D(new Vector3(5, 2, 5), Quaternion.Identity, Vector3.One))
        .With(RigidBody.Static())
        .With(PhysicsShape.Box(1, 1, 1))
        .WithRaycastTargetTag()
        .Build();
    targets.Add((target2, "Target B (right)", new Vector3(5, 2, 5)));

    // Target 3: Above
    var target3 = world.Spawn()
        .With(new Transform3D(new Vector3(0, 8, 0), Quaternion.Identity, Vector3.One))
        .With(RigidBody.Static())
        .With(PhysicsShape.Sphere(0.5f))
        .WithRaycastTargetTag()
        .Build();
    targets.Add((target3, "Target C (above)", new Vector3(0, 8, 0)));

    foreach (var (_, name, pos) in targets)
    {
        Console.WriteLine($"  {name} at ({pos.X}, {pos.Y}, {pos.Z})");
    }

    Console.WriteLine();

    // Perform raycasts from origin in different directions
    Console.WriteLine("Performing raycasts from origin (0, 2, 0):");
    var origin = new Vector3(0, 2, 0);

    // Raycast forward (+X)
    Console.WriteLine();
    Console.WriteLine("  Ray 1: Forward (+X direction)");
    if (physics.Raycast(origin, Vector3.UnitX, 50f, out var hit1))
    {
        var hitName = GetTargetName(targets, hit1.Entity);
        Console.WriteLine($"    HIT: {hitName}");
        Console.WriteLine($"    Position: ({hit1.Position.X:F2}, {hit1.Position.Y:F2}, {hit1.Position.Z:F2})");
        Console.WriteLine($"    Normal: ({hit1.Normal.X:F2}, {hit1.Normal.Y:F2}, {hit1.Normal.Z:F2})");
        Console.WriteLine($"    Distance: {hit1.Distance:F2}");
    }
    else
    {
        Console.WriteLine("    MISS: No object hit");
    }

    // Raycast up (+Y)
    Console.WriteLine();
    Console.WriteLine("  Ray 2: Upward (+Y direction)");
    if (physics.Raycast(origin, Vector3.UnitY, 50f, out var hit2))
    {
        var hitName = GetTargetName(targets, hit2.Entity);
        Console.WriteLine($"    HIT: {hitName}");
        Console.WriteLine($"    Position: ({hit2.Position.X:F2}, {hit2.Position.Y:F2}, {hit2.Position.Z:F2})");
        Console.WriteLine($"    Distance: {hit2.Distance:F2}");
    }
    else
    {
        Console.WriteLine("    MISS: No object hit");
    }

    // Raycast diagonal (toward Target B)
    Console.WriteLine();
    Console.WriteLine("  Ray 3: Diagonal (toward Target B)");
    var diagonalDir = Vector3.Normalize(new Vector3(5, 0, 5));
    if (physics.Raycast(origin, diagonalDir, 50f, out var hit3))
    {
        var hitName = GetTargetName(targets, hit3.Entity);
        Console.WriteLine($"    HIT: {hitName}");
        Console.WriteLine($"    Distance: {hit3.Distance:F2}");
    }
    else
    {
        Console.WriteLine("    MISS: No object hit");
    }

    // Raycast that misses (backward)
    Console.WriteLine();
    Console.WriteLine("  Ray 4: Backward (-X direction, should miss)");
    if (physics.Raycast(origin, -Vector3.UnitX, 50f, out var hit4))
    {
        Console.WriteLine($"    HIT: Unexpected hit!");
    }
    else
    {
        Console.WriteLine("    MISS: Correct - no targets behind origin");
    }

    // Demonstrate OverlapSphere
    Console.WriteLine();
    Console.WriteLine("Performing overlap sphere query...");
    Console.WriteLine("  Center: (5, 2, 2.5), Radius: 5.0");

    var overlapping = physics.OverlapSphere(new Vector3(5, 2, 2.5f), 5.0f);
    Console.WriteLine($"  Found {overlapping.Count()} entities in sphere:");
    foreach (var entity in overlapping)
    {
        var name = GetTargetName(targets, entity);
        Console.WriteLine($"    - {name}");
    }

    Console.WriteLine();
    Console.WriteLine("Demo 3 completed - Raycasting works!");
}

// =============================================================================
// HELPER METHODS
// =============================================================================

void PrintEntityPosition(World world, Entity entity, string prefix)
{
    if (world.Has<Transform3D>(entity))
    {
        ref readonly var transform = ref world.Get<Transform3D>(entity);
        Console.WriteLine($"{prefix}: Y={transform.Position.Y:F2}");
    }
}

string GetTargetName(List<(Entity entity, string name, Vector3 position)> targets, Entity entity)
{
    foreach (var (e, name, _) in targets)
    {
        if (e == entity)
        {
            return name;
        }
    }

    // Check if it's the ground
    return entity.Id == 1 ? "Ground" : $"Entity {entity.Id}";
}
