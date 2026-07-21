using System.Numerics;

using KeenEyes.Animation.Components;
using KeenEyes.Animation.IK;
using KeenEyes.Animation.IK.Solvers;
using KeenEyes.Common;

namespace KeenEyes.Animation.Tests.IK;

/// <summary>
/// Tests for joint angle constraints: the <see cref="IKConstraintSolver"/> clamping math
/// and constraint enforcement inside the TwoBone and FABRIK solvers.
/// </summary>
public class IKConstraintTests
{
    private const float PositionTolerance = 0.001f;
    private const float QuaternionDotTolerance = 0.9999f;
    private const float DegToRad = MathF.PI / 180f;

    private static void AssertRotationsEqual(Quaternion actual, Quaternion expected)
        => MathF.Abs(Quaternion.Dot(actual, expected)).ShouldBeGreaterThan(QuaternionDotTolerance);

    private static IKSolverContext CreateContext(
        World world,
        Entity[] bones,
        Vector3 target,
        IKChainDefinition chain,
        Vector3? polePosition = null) => new()
        {
            World = world,
            Chain = chain,
            BoneEntities = bones,
            TargetPosition = target,
            PolePosition = polePosition,
        };

    private static IKChainDefinition CreateLegChain()
        => IKChainDefinition.TwoBone("Leg", "Hip", "Knee", "Foot", Vector3.UnitZ);

    #region Hinge Clamping Tests

    [Fact]
    public void Apply_NoneConstraint_ReturnsRotationUnchanged()
    {
        var rotation = Quaternion.CreateFromYawPitchRoll(0.5f, 0.3f, -0.2f);

        var result = IKConstraintSolver.Apply(rotation, IKConstraint.Default);

        AssertRotationsEqual(result, rotation);
    }

    [Fact]
    public void Apply_HingeWithinLimits_ReturnsRotationUnchanged()
    {
        var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, 45f * DegToRad);
        var constraint = IKConstraint.Hinge(Vector3.UnitZ, 0f, 160f);

        var result = IKConstraintSolver.Apply(rotation, constraint);

        AssertRotationsEqual(result, rotation);
    }

    [Fact]
    public void Apply_HingeAboveMax_ClampsToMaxAngle()
    {
        var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, 170f * DegToRad);
        var constraint = IKConstraint.Hinge(Vector3.UnitZ, 0f, 160f);

        var result = IKConstraintSolver.Apply(rotation, constraint);

        AssertRotationsEqual(result, Quaternion.CreateFromAxisAngle(Vector3.UnitZ, 160f * DegToRad));
    }

    [Fact]
    public void Apply_HingeBelowMin_ClampsToMinAngle()
    {
        var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, -30f * DegToRad);
        var constraint = IKConstraint.Hinge(Vector3.UnitZ, 0f, 160f);

        var result = IKConstraintSolver.Apply(rotation, constraint);

        AssertRotationsEqual(result, Quaternion.Identity);
    }

    [Fact]
    public void Apply_HingeOffAxisRotation_RemovesSwingComponent()
    {
        // A rotation purely about Y has no twist about the Z hinge axis, so the
        // hinge reduces it to its clamped (zero) hinge angle.
        var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, 40f * DegToRad);
        var constraint = IKConstraint.Hinge(Vector3.UnitZ, -160f, 160f);

        var result = IKConstraintSolver.Apply(rotation, constraint);

        AssertRotationsEqual(result, Quaternion.Identity);
    }

    #endregion

    #region Ball-Socket Clamping Tests

    [Fact]
    public void Apply_BallSocketWithinCone_ReturnsRotationUnchanged()
    {
        // Rotation about Y swings the default X socket axis by 30 degrees.
        var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, 30f * DegToRad);
        var constraint = IKConstraint.BallSocket(45f);

        var result = IKConstraintSolver.Apply(rotation, constraint);

        AssertRotationsEqual(result, rotation);
    }

    [Fact]
    public void Apply_BallSocketBeyondCone_ClampsSwingToConeAngle()
    {
        var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, 90f * DegToRad);
        var constraint = IKConstraint.BallSocket(45f);

        var result = IKConstraintSolver.Apply(rotation, constraint);

        AssertRotationsEqual(result, Quaternion.CreateFromAxisAngle(Vector3.UnitZ, 45f * DegToRad));
    }

    [Fact]
    public void Apply_BallSocketTwistBeyondLimit_ClampsTwist()
    {
        // Rotation about the default X socket axis is pure twist.
        var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, 90f * DegToRad);
        var constraint = IKConstraint.BallSocket(90f, twistLimit: 30f);

        var result = IKConstraintSolver.Apply(rotation, constraint);

        AssertRotationsEqual(result, Quaternion.CreateFromAxisAngle(Vector3.UnitX, 30f * DegToRad));
    }

    [Fact]
    public void Apply_BallSocketSwingAndTwist_ClampsBothIndependently()
    {
        var swing = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, 60f * DegToRad);
        var twist = Quaternion.CreateFromAxisAngle(Vector3.UnitX, 50f * DegToRad);
        var constraint = IKConstraint.BallSocket(45f, twistLimit: 20f);

        var result = IKConstraintSolver.Apply(swing * twist, constraint);

        var expected = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, 45f * DegToRad)
            * Quaternion.CreateFromAxisAngle(Vector3.UnitX, 20f * DegToRad);
        AssertRotationsEqual(result, expected);
    }

    #endregion

    #region Euler Clamping Tests

    [Fact]
    public void Apply_EulerWithinLimits_ReturnsRotationUnchanged()
    {
        var rotation = Quaternion.CreateFromYawPitchRoll(
            20f * DegToRad, 10f * DegToRad, -5f * DegToRad);
        var constraint = IKConstraint.Euler(
            new Vector3(-30f, -45f, -20f), new Vector3(30f, 45f, 20f));

        var result = IKConstraintSolver.Apply(rotation, constraint);

        AssertRotationsEqual(result, rotation);
    }

    [Fact]
    public void Apply_EulerBeyondLimits_ClampsEachAxisIndependently()
    {
        // Yaw 50 / pitch 40 / roll -35 degrees against limits of X (pitch) +/-30,
        // Y (yaw) +/-45, Z (roll) +/-20.
        var rotation = Quaternion.CreateFromYawPitchRoll(
            50f * DegToRad, 40f * DegToRad, -35f * DegToRad);
        var constraint = IKConstraint.Euler(
            new Vector3(-30f, -45f, -20f), new Vector3(30f, 45f, 20f));

        var result = IKConstraintSolver.Apply(rotation, constraint);

        var expected = Quaternion.CreateFromYawPitchRoll(
            45f * DegToRad, 30f * DegToRad, -20f * DegToRad);
        AssertRotationsEqual(result, expected);
    }

    [Fact]
    public void Apply_EulerSingleAxisBeyondLimit_ClampsOnlyThatAxis()
    {
        var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, 60f * DegToRad);
        var constraint = IKConstraint.Euler(
            new Vector3(-45f, -90f, -90f), new Vector3(45f, 90f, 90f));

        var result = IKConstraintSolver.Apply(rotation, constraint);

        AssertRotationsEqual(result, Quaternion.CreateFromAxisAngle(Vector3.UnitX, 45f * DegToRad));
    }

    #endregion

    #region TwoBone Solver Constraint Tests

    [Fact]
    public void Solve_KneeHingeAgainstBendDirection_PreventsHyperextension()
    {
        using var world = new World();
        var bones = IKSolverTestHelpers.CreateChain(
            world, new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 0, 0));

        // Knee may only bend with a positive hinge angle; the pole asks for the
        // opposite (negative) bend, so the knee must clamp at 0 and stay straight.
        world.Add(bones[1], IKConstraint.Hinge(Vector3.UnitZ, 0f, 160f));
        var target = new Vector3(1.2f, 0.8f, 0);
        var context = CreateContext(
            world, bones, target, CreateLegChain(), polePosition: new Vector3(0.6f, 2f, 0));

        var result = new TwoBoneSolver().Solve(in context);

        result.Converged.ShouldBeFalse();

        // A blocked knee keeps the leg straight: root-to-foot distance stays at
        // full reach instead of folding to the 1.44 target distance.
        var rootPos = IKSolverTestHelpers.GetWorldPosition(world, bones[0]);
        var footPos = IKSolverTestHelpers.GetWorldPosition(world, bones[2]);
        Vector3.Distance(rootPos, footPos).ShouldBeGreaterThan(1.9f);
        Vector3.Distance(footPos, target).ShouldBeGreaterThan(PositionTolerance);
    }

    [Fact]
    public void Solve_KneeHingeBendWithinLimits_ReachesTarget()
    {
        using var world = new World();
        var bones = IKSolverTestHelpers.CreateChain(
            world, new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 0, 0));

        // Bending toward the negative-Y pole produces a positive hinge angle,
        // which the limits allow.
        world.Add(bones[1], IKConstraint.Hinge(Vector3.UnitZ, 0f, 160f));
        var target = new Vector3(1.2f, -0.8f, 0);
        var context = CreateContext(
            world, bones, target, CreateLegChain(), polePosition: new Vector3(0.6f, -2f, 0));

        var result = new TwoBoneSolver().Solve(in context);

        result.Converged.ShouldBeTrue();
        var footPos = IKSolverTestHelpers.GetWorldPosition(world, bones[2]);
        Vector3.Distance(footPos, target).ShouldBeLessThan(PositionTolerance);
    }

    [Fact]
    public void Solve_ShoulderBallSocket_RespectsConeReachingExtremeTarget()
    {
        using var world = new World();
        var bones = IKSolverTestHelpers.CreateChain(
            world, new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 0, 0));

        // Shoulder swing limited to 45 degrees around the +X rest direction.
        world.Add(bones[0], IKConstraint.BallSocket(45f));
        var target = new Vector3(0, 1.5f, 0);
        var context = CreateContext(world, bones, target, CreateLegChain());

        var result = new TwoBoneSolver().Solve(in context);

        // The target straight above needs ~90 degrees of shoulder swing; the cone
        // clamps the upper bone to exactly its 45-degree boundary.
        var rootPos = IKSolverTestHelpers.GetWorldPosition(world, bones[0]);
        var midPos = IKSolverTestHelpers.GetWorldPosition(world, bones[1]);
        var upperDirection = Vector3.Normalize(midPos - rootPos);
        Vector3.Dot(upperDirection, Vector3.UnitX).ShouldBe(MathF.Cos(45f * DegToRad), 0.01f);

        result.Converged.ShouldBeFalse();
    }

    [Fact]
    public void Solve_ConstrainedTwoBone_FinalErrorMatchesActualTipDistance()
    {
        using var world = new World();
        var bones = IKSolverTestHelpers.CreateChain(
            world, new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 0, 0));
        world.Add(bones[1], IKConstraint.Hinge(Vector3.UnitZ, 0f, 160f));
        var target = new Vector3(1.2f, 0.8f, 0);
        var context = CreateContext(
            world, bones, target, CreateLegChain(), polePosition: new Vector3(0.6f, 2f, 0));

        var result = new TwoBoneSolver().Solve(in context);

        var footPos = IKSolverTestHelpers.GetWorldPosition(world, bones[2]);
        result.FinalError.ShouldBe(Vector3.Distance(footPos, target), PositionTolerance);
    }

    [Fact]
    public void Solve_TwoBoneWithNoneConstraintComponents_MatchesUnconstrainedSolve()
    {
        using var unconstrained = new World();
        var plainBones = IKSolverTestHelpers.CreateChain(
            unconstrained, new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 0, 0));

        using var constrained = new World();
        var taggedBones = IKSolverTestHelpers.CreateChain(
            constrained, new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 0, 0));
        foreach (var bone in taggedBones)
        {
            constrained.Add(bone, IKConstraint.Default);
        }

        var target = new Vector3(1.2f, 0.8f, 0);
        var pole = new Vector3(0.6f, 0, 2f);
        var plainContext = CreateContext(
            unconstrained, plainBones, target, CreateLegChain(), polePosition: pole);
        var taggedContext = CreateContext(
            constrained, taggedBones, target, CreateLegChain(), polePosition: pole);

        var plainResult = new TwoBoneSolver().Solve(in plainContext);
        var taggedResult = new TwoBoneSolver().Solve(in taggedContext);

        taggedResult.Converged.ShouldBe(plainResult.Converged);
        for (var i = 0; i < plainBones.Length; i++)
        {
            var plainPos = IKSolverTestHelpers.GetWorldPosition(unconstrained, plainBones[i]);
            var taggedPos = IKSolverTestHelpers.GetWorldPosition(constrained, taggedBones[i]);
            Vector3.Distance(plainPos, taggedPos).ShouldBeLessThan(1e-5f);
        }
    }

    #endregion

    #region FABRIK Solver Constraint Tests

    [Fact]
    public void Solve_FABRIKWithTightHinges_RespectsLimitsAndPreservesBoneLengths()
    {
        using var world = new World();
        var bones = IKSolverTestHelpers.CreateChain(
            world,
            new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 0, 0));

        var hinge = IKConstraint.Hinge(Vector3.UnitZ, -15f, 15f);
        world.Add(bones[1], hinge);
        world.Add(bones[2], hinge);

        // Target closer than the chain's minimum constrained reach: the nearly
        // rigid chain cannot curl enough, so the solve stays partial.
        var chain = IKChainDefinition.MultiBone("Tail", ["A", "B", "C", "D"], maxIterations: 20);
        var context = CreateContext(world, bones, new Vector3(0, 2.5f, 0), chain);

        var result = new FABRIKSolver().Solve(in context);

        result.Converged.ShouldBeFalse();

        var lengths = IKSolverTestHelpers.GetBoneLengths(world, bones);
        foreach (var length in lengths)
        {
            length.ShouldBe(1f, PositionTolerance);
        }

        // The written pose must satisfy the hinge limits: clamping again is a no-op.
        for (var i = 1; i <= 2; i++)
        {
            ref readonly var local = ref world.Get<Transform3D>(bones[i]);
            var reclamped = IKConstraintSolver.Apply(local.Rotation, hinge);
            MathF.Abs(Quaternion.Dot(reclamped, local.Rotation))
                .ShouldBeGreaterThan(QuaternionDotTolerance);
        }
    }

    [Fact]
    public void Solve_FABRIKWithLooseHinge_ConvergesToReachableTarget()
    {
        using var world = new World();
        var bones = IKSolverTestHelpers.CreateChain(
            world, new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 0, 0));
        world.Add(bones[1], IKConstraint.Hinge(Vector3.UnitZ, -170f, 170f));

        var chain = IKChainDefinition.MultiBone("Arm", ["A", "B", "C"], maxIterations: 30);
        var target = new Vector3(1f, 1f, 0);
        var context = CreateContext(world, bones, target, chain);

        var result = new FABRIKSolver().Solve(in context);

        result.Converged.ShouldBeTrue();
        result.Iterations.ShouldBeLessThanOrEqualTo(chain.MaxIterations);
        var tipPos = IKSolverTestHelpers.GetWorldPosition(world, bones[2]);
        Vector3.Distance(tipPos, target).ShouldBeLessThanOrEqualTo(chain.Tolerance + PositionTolerance);
    }

    [Fact]
    public void Solve_FABRIKWithNoneConstraintComponents_MatchesUnconstrainedSolve()
    {
        using var unconstrained = new World();
        var plainBones = IKSolverTestHelpers.CreateChain(
            unconstrained, new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 0, 0));

        using var constrained = new World();
        var taggedBones = IKSolverTestHelpers.CreateChain(
            constrained, new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 0, 0));
        foreach (var bone in taggedBones)
        {
            constrained.Add(bone, IKConstraint.Default);
        }

        var chain = IKChainDefinition.MultiBone("Arm", ["A", "B", "C"]);
        var target = new Vector3(1f, 1f, 0);
        var plainContext = CreateContext(unconstrained, plainBones, target, chain);
        var taggedContext = CreateContext(constrained, taggedBones, target, chain);

        var plainResult = new FABRIKSolver().Solve(in plainContext);
        var taggedResult = new FABRIKSolver().Solve(in taggedContext);

        taggedResult.Converged.ShouldBe(plainResult.Converged);
        taggedResult.Iterations.ShouldBe(plainResult.Iterations);
        for (var i = 0; i < plainBones.Length; i++)
        {
            var plainPos = IKSolverTestHelpers.GetWorldPosition(unconstrained, plainBones[i]);
            var taggedPos = IKSolverTestHelpers.GetWorldPosition(constrained, taggedBones[i]);
            Vector3.Distance(plainPos, taggedPos).ShouldBeLessThan(1e-5f);
        }
    }

    #endregion
}
