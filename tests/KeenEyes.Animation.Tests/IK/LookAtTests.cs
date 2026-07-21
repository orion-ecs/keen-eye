using System.Numerics;

using KeenEyes.Animation.Components;
using KeenEyes.Animation.IK.Solvers;
using KeenEyes.Animation.Systems;
using KeenEyes.Common;

namespace KeenEyes.Animation.Tests.IK;

/// <summary>
/// Tests for <see cref="LookAtSolver"/> clamp/smoothing math and for
/// <see cref="LookAtSystem"/> running inside a world with <see cref="AnimationPlugin"/>
/// installed with IK enabled.
/// </summary>
public class LookAtTests
{
    private const float DeltaTime = 1f / 60f;
    private const float AngleTolerance = 0.01f;

    private static AnimationConfig IKEnabledConfig => new() { EnableIK = true };

    private static float AngleBetween(Vector3 a, Vector3 b)
        => MathF.Acos(Math.Clamp(
            Vector3.Dot(Vector3.Normalize(a), Vector3.Normalize(b)), -1f, 1f));

    private static float DegreesToRadians(float degrees) => degrees * MathF.PI / 180f;

    /// <summary>
    /// Computes the world-space forward direction (+Z rotated by the world rotation)
    /// of an entity.
    /// </summary>
    private static Vector3 WorldForward(World world, Entity entity)
        => Vector3.Transform(
            Vector3.UnitZ,
            IKSolverTestHelpers.GetWorldTransform(world, entity).Rotation);

    /// <summary>
    /// Creates a head bone parented under a root entity, both with identity local
    /// transforms, so the head's FK forward direction is +Z.
    /// </summary>
    private static (Entity Root, Entity Head) CreateHead(World world)
    {
        var root = world.Spawn()
            .With(Transform3D.Identity)
            .Build();

        var head = world.Spawn()
            .With(Transform3D.Identity)
            .Build();
        world.SetParent(head, root);

        return (root, head);
    }

    #region Plugin Wiring Tests

    [Fact]
    public void Install_WithDefaultConfig_DoesNotRegisterLookAtSystem()
    {
        using var world = new World();
        world.InstallPlugin(new AnimationPlugin());

        world.GetSystem<LookAtSystem>().ShouldBeNull();
    }

    [Fact]
    public void Install_WithEnableIK_RegistersLookAtSystem()
    {
        using var world = new World();
        world.InstallPlugin(new AnimationPlugin(IKEnabledConfig));

        world.GetSystem<LookAtSystem>().ShouldNotBeNull();
    }

    #endregion

    #region Target Resolution Tests

    [Fact]
    public void Update_WithEntityTarget_RotatesForwardTowardTarget()
    {
        using var world = new World();
        world.InstallPlugin(new AnimationPlugin(IKEnabledConfig));

        var (_, head) = CreateHead(world);
        var target = world.Spawn()
            .With(new Transform3D(new Vector3(5f, 0f, 5f), Quaternion.Identity, Vector3.One))
            .Build();
        world.Add(head, LookAtTarget.AtEntity(target.Id) with { Smoothing = 0f });

        world.Update(DeltaTime);

        var forward = WorldForward(world, head);
        AngleBetween(forward, new Vector3(1f, 0f, 1f)).ShouldBeLessThan(AngleTolerance);
    }

    [Fact]
    public void Update_WithWorldPositionTarget_RotatesForwardTowardTarget()
    {
        using var world = new World();
        world.InstallPlugin(new AnimationPlugin(IKEnabledConfig));

        var (_, head) = CreateHead(world);
        world.Add(head, LookAtTarget.AtPosition(new Vector3(0f, 3f, 3f)) with { Smoothing = 0f });

        world.Update(DeltaTime);

        var forward = WorldForward(world, head);
        AngleBetween(forward, new Vector3(0f, 1f, 1f)).ShouldBeLessThan(AngleTolerance);
    }

    [Fact]
    public void Update_WithMovingEntityTarget_ForwardFollowsTargetEachFrame()
    {
        using var world = new World();
        world.InstallPlugin(new AnimationPlugin(IKEnabledConfig));

        var (_, head) = CreateHead(world);
        var target = world.Spawn()
            .With(new Transform3D(new Vector3(5f, 0f, 5f), Quaternion.Identity, Vector3.One))
            .Build();
        world.Add(head, LookAtTarget.AtEntity(target.Id) with { Smoothing = 0f });

        world.Update(DeltaTime);

        // Move the target; the constraint must re-aim on the next frame.
        world.Get<Transform3D>(target).Position = new Vector3(-5f, 0f, 5f);
        world.Update(DeltaTime);

        var forward = WorldForward(world, head);
        AngleBetween(forward, new Vector3(-1f, 0f, 1f)).ShouldBeLessThan(AngleTolerance);
    }

    [Fact]
    public void Update_WithRotatedParent_WritesLocalRotationRelativeToParent()
    {
        using var world = new World();
        world.InstallPlugin(new AnimationPlugin(IKEnabledConfig));

        // Parent rotated 90 degrees about Y: the head's FK world forward becomes +X.
        var root = world.Spawn()
            .With(new Transform3D(
                Vector3.Zero,
                Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2f),
                Vector3.One))
            .Build();

        var head = world.Spawn()
            .With(Transform3D.Identity)
            .Build();
        world.SetParent(head, root);

        world.Add(head, LookAtTarget.AtPosition(new Vector3(5f, 5f, 0f)) with { Smoothing = 0f });

        world.Update(DeltaTime);

        var forward = WorldForward(world, head);
        AngleBetween(forward, new Vector3(1f, 1f, 0f)).ShouldBeLessThan(AngleTolerance);
    }

    #endregion

    #region Max Angle Clamp Tests

    [Fact]
    public void Update_WithTargetBehindBone_ClampsRotationToMaxAngle()
    {
        using var world = new World();
        world.InstallPlugin(new AnimationPlugin(IKEnabledConfig));

        var (_, head) = CreateHead(world);

        // Target directly behind (180 degrees from FK forward), limit 60 degrees.
        world.Add(head, LookAtTarget.AtPosition(new Vector3(0f, 0f, -5f)) with
        {
            Smoothing = 0f,
            MaxAngle = 60f,
        });

        world.Update(DeltaTime);

        var deviation = AngleBetween(WorldForward(world, head), Vector3.UnitZ);
        deviation.ShouldBe(DegreesToRadians(60f), AngleTolerance);
    }

    [Fact]
    public void Update_WithTargetBehindBoneOverManyFrames_DeviationStaysAtMaxAngle()
    {
        using var world = new World();
        world.InstallPlugin(new AnimationPlugin(IKEnabledConfig));

        var (_, head) = CreateHead(world);
        world.Add(head, LookAtTarget.AtPosition(new Vector3(0f, 0f, -5f)) with
        {
            Smoothing = 0f,
            MaxAngle = 60f,
        });

        // The clamp is relative to the FK pose: repeated updates must not compound
        // the constraint's own output and creep past the limit.
        for (var frame = 0; frame < 10; frame++)
        {
            world.Update(DeltaTime);
        }

        var deviation = AngleBetween(WorldForward(world, head), Vector3.UnitZ);
        deviation.ShouldBe(DegreesToRadians(60f), AngleTolerance);
    }

    [Fact]
    public void Update_WithTargetWithinMaxAngle_ReachesTargetExactly()
    {
        using var world = new World();
        world.InstallPlugin(new AnimationPlugin(IKEnabledConfig));

        var (_, head) = CreateHead(world);

        // 45 degrees away with a 90 degree limit: no clamping.
        world.Add(head, LookAtTarget.AtPosition(new Vector3(0f, 5f, 5f)) with { Smoothing = 0f });

        world.Update(DeltaTime);

        var forward = WorldForward(world, head);
        AngleBetween(forward, new Vector3(0f, 1f, 1f)).ShouldBeLessThan(AngleTolerance);
    }

    #endregion

    #region Smoothing Tests

    [Fact]
    public void Update_WithSmoothing_ApproachesTargetMonotonicallyWithoutSnapping()
    {
        using var world = new World();
        world.InstallPlugin(new AnimationPlugin(IKEnabledConfig));

        var (_, head) = CreateHead(world);
        var desiredDirection = new Vector3(0f, 1f, 1f);
        world.Add(head, LookAtTarget.AtPosition(new Vector3(0f, 5f, 5f)) with { Smoothing = 0.2f });

        var initialError = AngleBetween(Vector3.UnitZ, desiredDirection);

        // The first frame must move toward the target but not snap onto it.
        world.Update(DeltaTime);
        var previousError = AngleBetween(WorldForward(world, head), desiredDirection);
        previousError.ShouldBeLessThan(initialError);
        previousError.ShouldBeGreaterThan(AngleTolerance);

        // Each subsequent frame keeps shrinking the error.
        for (var frame = 0; frame < 8; frame++)
        {
            world.Update(DeltaTime);
            var error = AngleBetween(WorldForward(world, head), desiredDirection);
            error.ShouldBeLessThan(previousError);
            previousError = error;
        }

        // After enough simulated time the constraint converges onto the target.
        for (var frame = 0; frame < 300; frame++)
        {
            world.Update(DeltaTime);
        }

        AngleBetween(WorldForward(world, head), desiredDirection).ShouldBeLessThan(AngleTolerance);
    }

    [Fact]
    public void Update_WithSameElapsedTimeAtDifferentFrameRates_ConvergesToSameRotation()
    {
        // Simulate 0.5 seconds at 30 FPS and at 60 FPS; the exponential smoothing
        // formula alpha = 1 - exp(-dt / smoothing) must make the residual error depend
        // only on total elapsed time, not on the step count.
        var errorAt30 = SimulateSmoothing(steps: 15, deltaTime: 1f / 30f);
        var errorAt60 = SimulateSmoothing(steps: 30, deltaTime: 1f / 60f);

        errorAt30.ShouldBe(errorAt60, 0.001f);
    }

    private static float SimulateSmoothing(int steps, float deltaTime)
    {
        using var world = new World();
        world.InstallPlugin(new AnimationPlugin(IKEnabledConfig));

        var (_, head) = CreateHead(world);
        var desiredDirection = new Vector3(0f, 5f, 5f);
        world.Add(head, LookAtTarget.AtPosition(desiredDirection) with { Smoothing = 0.3f });

        for (var frame = 0; frame < steps; frame++)
        {
            world.Update(deltaTime);
        }

        return AngleBetween(WorldForward(world, head), desiredDirection);
    }

    #endregion

    #region Weight Blending Tests

    [Fact]
    public void Update_WithZeroWeight_LeavesFkRotationUntouched()
    {
        using var world = new World();
        world.InstallPlugin(new AnimationPlugin(IKEnabledConfig));

        var (_, head) = CreateHead(world);
        world.Add(head, LookAtTarget.AtPosition(new Vector3(0f, 5f, 5f)) with
        {
            Smoothing = 0f,
            Weight = 0f,
        });

        world.Update(DeltaTime);

        var rotation = world.Get<Transform3D>(head).Rotation;
        MathF.Abs(Quaternion.Dot(rotation, Quaternion.Identity)).ShouldBeGreaterThan(1f - 1e-6f);
    }

    [Fact]
    public void Update_WithHalfWeight_RotatesHalfwayTowardTarget()
    {
        using var world = new World();
        world.InstallPlugin(new AnimationPlugin(IKEnabledConfig));

        var (_, head) = CreateHead(world);

        // Target 45 degrees from FK forward; half weight must rotate 22.5 degrees.
        world.Add(head, LookAtTarget.AtPosition(new Vector3(0f, 5f, 5f)) with
        {
            Smoothing = 0f,
            Weight = 0.5f,
        });

        world.Update(DeltaTime);

        var deviation = AngleBetween(WorldForward(world, head), Vector3.UnitZ);
        deviation.ShouldBe(DegreesToRadians(22.5f), AngleTolerance);
    }

    #endregion

    #region Graceful Degradation Tests

    [Fact]
    public void Update_WithDeadTargetEntity_PreservesFkPoseWithoutThrowing()
    {
        using var world = new World();
        world.InstallPlugin(new AnimationPlugin(IKEnabledConfig));

        var (_, head) = CreateHead(world);
        var target = world.Spawn()
            .With(new Transform3D(new Vector3(5f, 0f, 5f), Quaternion.Identity, Vector3.One))
            .Build();
        world.Add(head, LookAtTarget.AtEntity(target.Id) with { Smoothing = 0f });
        world.Despawn(target);

        world.Update(DeltaTime);

        var rotation = world.Get<Transform3D>(head).Rotation;
        MathF.Abs(Quaternion.Dot(rotation, Quaternion.Identity)).ShouldBeGreaterThan(1f - 1e-6f);
    }

    [Fact]
    public void Update_WithMissingTargetEntityId_PreservesFkPose()
    {
        using var world = new World();
        world.InstallPlugin(new AnimationPlugin(IKEnabledConfig));

        var (_, head) = CreateHead(world);
        world.Add(head, LookAtTarget.AtEntity(99999) with { Smoothing = 0f });

        world.Update(DeltaTime);

        var rotation = world.Get<Transform3D>(head).Rotation;
        MathF.Abs(Quaternion.Dot(rotation, Quaternion.Identity)).ShouldBeGreaterThan(1f - 1e-6f);
    }

    #endregion

    #region LookAtSolver Unit Tests

    [Fact]
    public void Solve_WithTargetWithinLimit_RotatesForwardOntoTargetDirection()
    {
        var offset = Quaternion.Identity;

        var rotation = LookAtSolver.Solve(
            Vector3.Zero, Quaternion.Identity, new Vector3(0f, 5f, 5f),
            Vector3.UnitZ, maxAngleDegrees: 90f, smoothing: 0f,
            deltaTime: DeltaTime, ref offset);

        var forward = Vector3.Transform(Vector3.UnitZ, rotation);
        AngleBetween(forward, new Vector3(0f, 1f, 1f)).ShouldBeLessThan(AngleTolerance);
    }

    [Fact]
    public void Solve_WithTargetBeyondLimit_ClampsRotationToMaxAngle()
    {
        var offset = Quaternion.Identity;

        // Target is 180 degrees behind the forward direction; the clamp must stop
        // the rotation at exactly the 45 degree limit.
        var rotation = LookAtSolver.Solve(
            Vector3.Zero, Quaternion.Identity, new Vector3(0f, 0f, -5f),
            Vector3.UnitZ, maxAngleDegrees: 45f, smoothing: 0f,
            deltaTime: DeltaTime, ref offset);

        var forward = Vector3.Transform(Vector3.UnitZ, rotation);
        AngleBetween(forward, Vector3.UnitZ).ShouldBe(DegreesToRadians(45f), AngleTolerance);
    }

    [Fact]
    public void Solve_WithZeroMaxAngle_ReturnsFkRotation()
    {
        var offset = Quaternion.Identity;
        var fkRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, 0.3f);

        var rotation = LookAtSolver.Solve(
            Vector3.Zero, fkRotation, new Vector3(5f, 0f, 0f),
            Vector3.UnitZ, maxAngleDegrees: 0f, smoothing: 0f,
            deltaTime: DeltaTime, ref offset);

        MathF.Abs(Quaternion.Dot(rotation, fkRotation)).ShouldBeGreaterThan(1f - 1e-6f);
    }

    [Fact]
    public void Solve_WithTargetAtBonePosition_ReturnsFkRotationUnchanged()
    {
        var offset = Quaternion.Identity;
        var fkRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, 0.5f);
        var bonePosition = new Vector3(1f, 2f, 3f);

        var rotation = LookAtSolver.Solve(
            bonePosition, fkRotation, bonePosition,
            Vector3.UnitZ, maxAngleDegrees: 90f, smoothing: 0f,
            deltaTime: DeltaTime, ref offset);

        MathF.Abs(Quaternion.Dot(rotation, fkRotation)).ShouldBeGreaterThan(1f - 1e-6f);
    }

    [Fact]
    public void Solve_WithZeroSmoothedOffsetState_TreatsStateAsIdentity()
    {
        // A default-initialized component field is a zero quaternion; the solver
        // must seed it as identity instead of producing NaNs.
        var offset = default(Quaternion);

        var rotation = LookAtSolver.Solve(
            Vector3.Zero, Quaternion.Identity, new Vector3(0f, 5f, 5f),
            Vector3.UnitZ, maxAngleDegrees: 90f, smoothing: 0f,
            deltaTime: DeltaTime, ref offset);

        var forward = Vector3.Transform(Vector3.UnitZ, rotation);
        AngleBetween(forward, new Vector3(0f, 1f, 1f)).ShouldBeLessThan(AngleTolerance);
    }

    [Fact]
    public void Solve_WithSameElapsedTimeAtDifferentSteps_ProducesSameResidualError()
    {
        var desiredDirection = new Vector3(5f, 0f, 0f);

        var errorCoarse = SolveRepeatedly(desiredDirection, steps: 10, deltaTime: 1f / 20f);
        var errorFine = SolveRepeatedly(desiredDirection, steps: 30, deltaTime: 1f / 60f);

        errorCoarse.ShouldBe(errorFine, 0.001f);
    }

    private static float SolveRepeatedly(Vector3 targetPosition, int steps, float deltaTime)
    {
        var offset = Quaternion.Identity;
        var rotation = Quaternion.Identity;

        for (var i = 0; i < steps; i++)
        {
            rotation = LookAtSolver.Solve(
                Vector3.Zero, Quaternion.Identity, targetPosition,
                Vector3.UnitZ, maxAngleDegrees: 180f, smoothing: 0.25f,
                deltaTime: deltaTime, ref offset);
        }

        return AngleBetween(Vector3.Transform(Vector3.UnitZ, rotation), targetPosition);
    }

    #endregion
}
