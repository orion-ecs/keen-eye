using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Physics.Components;
using KeenEyes.Physics.Core;
using KeenEyes.Physics.Events;

namespace KeenEyes.Physics.Tests;

/// <summary>
/// Regression tests for the physics correctness cluster (#1122, #1123, #1124, #1126, #1127,
/// #1128, #1130). Each test fails against the pre-fix behavior and passes once the fix is in.
/// </summary>
public class PhysicsCorrectnessTests : IDisposable
{
    private World? world;

    public void Dispose()
    {
        world?.Dispose();
    }

    #region #1122 NeverSleep

    [Fact]
    public void NeverSleep_DynamicBodyAtRest_StaysAwakeAfterManySteps()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin(new PhysicsConfig { Gravity = Vector3.Zero }));
        var physics = world.GetExtension<PhysicsWorld>();

        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(0.5f))
            .With(new RigidBody(1f, ActivityDescription.NeverSleep))
            .Build();

        for (var i = 0; i < 400; i++)
        {
            physics.Step(1f / 60f);
        }

        // A body configured to never sleep must remain awake even at rest.
        Assert.True(physics.IsAwake(entity));
    }

    #endregion

    #region #1123 Interpolation feedback

    [Fact]
    public void PhysicsSync_ConstantVelocityUnderHighRenderRate_RendersMonotonically()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin(new PhysicsConfig
        {
            FixedTimestep = 1f / 60f,
            Gravity = Vector3.Zero,
            EnableInterpolation = true,
            MaxStepsPerFrame = 8
        }));
        var physics = world.GetExtension<PhysicsWorld>();

        var body = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(new Velocity3D(2f, 0f, 0f))
            .With(PhysicsShape.Sphere(0.5f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        physics.SetVelocity(body, new Vector3(2f, 0f, 0f));

        // Render four times as often as physics steps. Update(float) runs the FixedUpdate
        // step system then the LateUpdate sync system, matching a real game loop.
        var renderDt = (1f / 60f) / 4f;
        var samples = new List<float>();
        for (var frame = 0; frame < 40; frame++)
        {
            world.Update(renderDt);
            samples.Add(world.Get<Transform3D>(body).Position.X);
        }

        // A body moving steadily in +X must never appear to move backward on screen.
        for (var i = 1; i < samples.Count; i++)
        {
            Assert.True(
                samples[i] >= samples[i - 1] - 1e-4f,
                $"Rendered X regressed at frame {i}: {samples[i - 1]} -> {samples[i]}");
        }

        // Sanity: it actually advanced overall (the body is really moving).
        Assert.True(samples[^1] > samples[0]);
    }

    #endregion

    #region #1124 Collision-ended on sleep

    [Fact]
    public void CollisionEnded_BoxRestingOnGround_DoesNotFireWhenBodySleeps()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin(new PhysicsConfig
        {
            Gravity = new Vector3(0f, -9.81f, 0f)
        }));
        var physics = world.GetExtension<PhysicsWorld>();

        var endedEvents = new List<CollisionEndedEvent>();
        using var subscription = world.Subscribe<CollisionEndedEvent>(e => endedEvents.Add(e));

        // Static ground: 1-unit-tall box centered at y = -0.5, so its top surface is at y = 0.
        world.Spawn()
            .With(new Transform3D(new Vector3(0f, -0.5f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Box(20f, 1f, 20f))
            .With(RigidBody.Static())
            .Build();

        // Dynamic 1-unit box resting on the ground (half-extent 0.5, centered at y = 0.5).
        var box = world.Spawn()
            .With(new Transform3D(new Vector3(0f, 0.5f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Box(1f, 1f, 1f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        for (var i = 0; i < 300; i++)
        {
            physics.Step(1f / 60f);
        }

        // Precondition: the box went to sleep while still resting (the scenario is real).
        Assert.False(physics.IsAwake(box));

        // The contact never actually separated, so no CollisionEnded should have fired.
        Assert.Empty(endedEvents);
    }

    #endregion

    #region #1126 SetGravity

    [Fact]
    public void SetGravity_ToZero_StopsBodyFromAccelerating()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin(new PhysicsConfig
        {
            Gravity = new Vector3(0f, -9.81f, 0f)
        }));
        var physics = world.GetExtension<PhysicsWorld>();

        var entity = world.Spawn()
            .With(new Transform3D(new Vector3(0f, 10f, 0f), Quaternion.Identity, Vector3.One))
            .With(PhysicsShape.Sphere(0.5f))
            .With(RigidBody.Dynamic(1f))
            .Build();

        physics.SetGravity(Vector3.Zero);

        for (var i = 0; i < 30; i++)
        {
            physics.Step(1f / 60f);
        }

        // With gravity zeroed on the live simulation, the body must not accelerate downward.
        var velocity = physics.GetVelocity(entity);
        Assert.True(
            velocity.Y.IsApproximatelyZero(),
            $"Expected ~0 vertical velocity after zeroing gravity, got {velocity.Y}");
    }

    #endregion

    #region #1127 Shape slot leak

    [Fact]
    public void RemoveBody_AfterManyCreateDespawnCycles_ReusesShapeSlots()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin());
        var physics = world.GetExtension<PhysicsWorld>();

        var maxShapeIndex = -1;
        for (var i = 0; i < 40; i++)
        {
            var entity = world.Spawn()
                .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
                .With(PhysicsShape.Sphere(0.5f))
                .With(RigidBody.Dynamic(1f))
                .Build();

            Assert.True(physics.BodyLookup.TryGetBody(entity, out var handle));
            var shapeIndex = physics.Simulation.Bodies[handle].Collidable.Shape.Index;
            maxShapeIndex = Math.Max(maxShapeIndex, shapeIndex);

            world.Despawn(entity);
        }

        // If shapes are removed on despawn, freed slots are reused and the shape index never
        // climbs with the cycle count. Leaked shapes make it grow toward 39.
        Assert.True(
            maxShapeIndex < 5,
            $"Shape slots leaked: max shape index was {maxShapeIndex} across 40 create/despawn cycles");
    }

    #endregion

    #region #1128 Interpolation alpha clamp

    [Fact]
    public void Step_PastMaxStepsPerFrameCap_ClampsInterpolationAlpha()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin(new PhysicsConfig
        {
            FixedTimestep = 1f / 60f,
            MaxStepsPerFrame = 3,
            EnableInterpolation = true
        }));
        var physics = world.GetExtension<PhysicsWorld>();

        // One second of delta with a 3-step cap leaves a large leftover accumulator.
        physics.Step(1f);

        Assert.InRange(physics.InterpolationAlpha, 0f, 1f);
    }

    #endregion

    #region #1130 BodyCount includes sleeping bodies

    [Fact]
    public void BodyCount_WithSleepingBodies_IncludesSleepingSet()
    {
        world = new World();
        world.InstallPlugin(new PhysicsPlugin(new PhysicsConfig { Gravity = Vector3.Zero }));
        var physics = world.GetExtension<PhysicsWorld>();

        var entities = new List<Entity>();
        for (var i = 0; i < 3; i++)
        {
            entities.Add(world.Spawn()
                .With(new Transform3D(new Vector3(i * 10f, 0f, 0f), Quaternion.Identity, Vector3.One))
                .With(PhysicsShape.Sphere(0.5f))
                .With(RigidBody.Dynamic(1f))
                .Build());
        }

        // Let the resting bodies fall asleep.
        for (var i = 0; i < 200; i++)
        {
            physics.Step(1f / 60f);
        }

        // Precondition: the bodies really did go to sleep.
        Assert.All(entities, e => Assert.False(physics.IsAwake(e)));

        // BodyCount must include sleeping bodies, not just the active set.
        Assert.Equal(3, physics.BodyCount);
    }

    #endregion
}
