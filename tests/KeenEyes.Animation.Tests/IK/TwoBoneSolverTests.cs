using System.Numerics;

using KeenEyes.Animation.IK;
using KeenEyes.Animation.IK.Solvers;
using KeenEyes.Common;

namespace KeenEyes.Animation.Tests.IK;

/// <summary>
/// Tests for the analytical two-bone IK solver.
/// </summary>
public class TwoBoneSolverTests
{
    private const float PositionTolerance = 0.001f;

    private static IKChainDefinition CreateTwoBoneChain(Vector3? poleVector = null)
        => IKChainDefinition.TwoBone("Arm", "Upper", "Lower", "Hand", poleVector ?? Vector3.UnitZ);

    private static IKSolverContext CreateContext(
        World world,
        Entity[] bones,
        Vector3 target,
        IKChainDefinition? chain = null,
        Vector3? polePosition = null,
        Quaternion? targetRotation = null) => new()
        {
            World = world,
            Chain = chain ?? CreateTwoBoneChain(),
            BoneEntities = bones,
            TargetPosition = target,
            PolePosition = polePosition,
            TargetRotation = targetRotation,
        };

    #region Metadata Tests

    [Fact]
    public void Name_ReturnsTwoBone()
    {
        var solver = new TwoBoneSolver();

        solver.Name.ShouldBe("TwoBone");
    }

    [Fact]
    public void SolverType_ReturnsTwoBone()
    {
        var solver = new TwoBoneSolver();

        solver.SolverType.ShouldBe(IKSolverType.TwoBone);
    }

    [Fact]
    public void CanHandle_ThreeBones_ReturnsTrue()
    {
        var solver = new TwoBoneSolver();

        solver.CanHandle(3).ShouldBeTrue();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(8)]
    public void CanHandle_NonThreeBoneCounts_ReturnsFalse(int boneCount)
    {
        var solver = new TwoBoneSolver();

        solver.CanHandle(boneCount).ShouldBeFalse();
    }

    #endregion

    #region Reachable Target Tests

    [Fact]
    public void Solve_ArmReachingForwardTarget_TipReachesTarget()
    {
        using var world = new World();
        var bones = IKSolverTestHelpers.CreateChain(
            world, new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 0, 0));
        var target = new Vector3(1.2f, 0.8f, 0);
        var context = CreateContext(world, bones, target, polePosition: new Vector3(0.5f, 0, 2f));

        var result = new TwoBoneSolver().Solve(in context);

        result.Converged.ShouldBeTrue();
        var tipPos = IKSolverTestHelpers.GetWorldPosition(world, bones[2]);
        Vector3.Distance(tipPos, target).ShouldBeLessThan(PositionTolerance);
    }

    [Fact]
    public void Solve_ArmReachingUpwardTarget_TipReachesTarget()
    {
        using var world = new World();
        var bones = IKSolverTestHelpers.CreateChain(
            world, new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 0, 0));
        var target = new Vector3(0, 1.5f, 0);
        var context = CreateContext(world, bones, target, polePosition: new Vector3(0, 0.75f, 2f));

        var result = new TwoBoneSolver().Solve(in context);

        result.Converged.ShouldBeTrue();
        var tipPos = IKSolverTestHelpers.GetWorldPosition(world, bones[2]);
        Vector3.Distance(tipPos, target).ShouldBeLessThan(PositionTolerance);
    }

    [Fact]
    public void Solve_LegPlantingOnGround_TipReachesTarget()
    {
        using var world = new World();

        // Hip at (0, 2, 0) with knee and foot pointing down; knee pole in front (+Z).
        var bones = IKSolverTestHelpers.CreateChain(
            world, new Vector3(0, 2, 0), new Vector3(0, -1, 0), new Vector3(0, -1, 0));
        var target = new Vector3(0.3f, 0.5f, 0.2f);
        var context = CreateContext(world, bones, target, polePosition: new Vector3(0, 1.5f, 2f));

        var result = new TwoBoneSolver().Solve(in context);

        result.Converged.ShouldBeTrue();
        var footPos = IKSolverTestHelpers.GetWorldPosition(world, bones[2]);
        Vector3.Distance(footPos, target).ShouldBeLessThan(PositionTolerance);

        // The knee should bend forward, toward the pole.
        var kneePos = IKSolverTestHelpers.GetWorldPosition(world, bones[1]);
        kneePos.Z.ShouldBeGreaterThan(0f);
    }

    [Fact]
    public void Solve_ReachableTarget_ReturnsZeroIterationsAndSmallError()
    {
        using var world = new World();
        var bones = IKSolverTestHelpers.CreateChain(
            world, new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 0, 0));
        var chain = CreateTwoBoneChain();
        var context = CreateContext(world, bones, new Vector3(1.2f, 0.8f, 0), chain);

        var result = new TwoBoneSolver().Solve(in context);

        result.Converged.ShouldBeTrue();
        result.Iterations.ShouldBe(0);
        result.FinalError.ShouldBeLessThanOrEqualTo(chain.Tolerance);
    }

    [Fact]
    public void Solve_ReachableTarget_PreservesBoneLengths()
    {
        using var world = new World();
        var bones = IKSolverTestHelpers.CreateChain(
            world, new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 0, 0));
        var lengthsBefore = IKSolverTestHelpers.GetBoneLengths(world, bones);
        var context = CreateContext(world, bones, new Vector3(0.5f, 1.2f, 0.4f));

        new TwoBoneSolver().Solve(in context);

        var lengthsAfter = IKSolverTestHelpers.GetBoneLengths(world, bones);
        for (var i = 0; i < lengthsBefore.Length; i++)
        {
            lengthsAfter[i].ShouldBe(lengthsBefore[i], PositionTolerance);
        }
    }

    #endregion

    #region Unreachable Target Tests

    [Fact]
    public void Solve_TargetBeyondReach_StretchesTowardTarget()
    {
        using var world = new World();
        var bones = IKSolverTestHelpers.CreateChain(
            world, new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 0, 0));
        var context = CreateContext(world, bones, new Vector3(5, 0, 0));

        var result = new TwoBoneSolver().Solve(in context);

        // The chain straightens fully toward the target: tip at max reach along the axis.
        result.Converged.ShouldBeFalse();
        var tipPos = IKSolverTestHelpers.GetWorldPosition(world, bones[2]);
        Vector3.Distance(tipPos, new Vector3(2, 0, 0)).ShouldBeLessThan(PositionTolerance);

        var lengths = IKSolverTestHelpers.GetBoneLengths(world, bones);
        lengths[0].ShouldBe(1f, PositionTolerance);
        lengths[1].ShouldBe(1f, PositionTolerance);
    }

    [Fact]
    public void Solve_TargetTooClose_FoldsLimbNaturally()
    {
        using var world = new World();
        var bones = IKSolverTestHelpers.CreateChain(
            world, new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 0, 0));
        var target = new Vector3(0.2f, 0, 0);
        var context = CreateContext(world, bones, target, polePosition: new Vector3(0.1f, 5f, 0));

        var result = new TwoBoneSolver().Solve(in context);

        // Equal bone lengths make even a near-root target reachable by folding.
        result.Converged.ShouldBeTrue();
        var tipPos = IKSolverTestHelpers.GetWorldPosition(world, bones[2]);
        Vector3.Distance(tipPos, target).ShouldBeLessThan(PositionTolerance);

        // The mid joint folds far off the root-target axis, toward the pole.
        var midPos = IKSolverTestHelpers.GetWorldPosition(world, bones[1]);
        midPos.Y.ShouldBeGreaterThan(0.9f);

        var lengths = IKSolverTestHelpers.GetBoneLengths(world, bones);
        lengths[0].ShouldBe(1f, PositionTolerance);
        lengths[1].ShouldBe(1f, PositionTolerance);
    }

    #endregion

    #region Pole Vector Tests

    [Fact]
    public void Solve_PoleVectorForward_BendsMidTowardPole()
    {
        using var world = new World();
        var bones = IKSolverTestHelpers.CreateChain(
            world, new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 0, 0));
        var context = CreateContext(
            world, bones, new Vector3(1.5f, 0, 0), polePosition: new Vector3(0.75f, 0, 5f));

        new TwoBoneSolver().Solve(in context);

        var midPos = IKSolverTestHelpers.GetWorldPosition(world, bones[1]);
        midPos.Z.ShouldBeGreaterThan(0.5f);
    }

    [Fact]
    public void Solve_PoleVectorBackward_BendsMidAwayFromForward()
    {
        using var world = new World();
        var bones = IKSolverTestHelpers.CreateChain(
            world, new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 0, 0));
        var context = CreateContext(
            world, bones, new Vector3(1.5f, 0, 0), polePosition: new Vector3(0.75f, 0, -5f));

        new TwoBoneSolver().Solve(in context);

        var midPos = IKSolverTestHelpers.GetWorldPosition(world, bones[1]);
        midPos.Z.ShouldBeLessThan(-0.5f);
    }

    [Fact]
    public void Solve_WithoutPolePosition_UsesChainPoleVector()
    {
        using var world = new World();
        var bones = IKSolverTestHelpers.CreateChain(
            world, new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 0, 0));
        var chain = CreateTwoBoneChain(Vector3.UnitY);
        var context = CreateContext(world, bones, new Vector3(1.5f, 0, 0), chain);

        new TwoBoneSolver().Solve(in context);

        var midPos = IKSolverTestHelpers.GetWorldPosition(world, bones[1]);
        midPos.Y.ShouldBeGreaterThan(0.5f);
    }

    #endregion

    #region End Effector Rotation Tests

    [Fact]
    public void Solve_WithTargetRotation_EndEffectorMatchesTargetRotation()
    {
        using var world = new World();
        var bones = IKSolverTestHelpers.CreateChain(
            world, new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 0, 0));
        var targetRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 2f);
        var context = CreateContext(
            world, bones, new Vector3(1.2f, 0.8f, 0), targetRotation: targetRotation);

        var result = new TwoBoneSolver().Solve(in context);

        result.Converged.ShouldBeTrue();
        var (_, tipRotation) = IKSolverTestHelpers.GetWorldTransform(world, bones[2]);
        MathF.Abs(Quaternion.Dot(tipRotation, targetRotation)).ShouldBeGreaterThan(0.9999f);
    }

    [Fact]
    public void Solve_WithoutTargetRotation_LeavesEndEffectorLocalRotationUnchanged()
    {
        using var world = new World();
        var bones = IKSolverTestHelpers.CreateChain(
            world, new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 0, 0));
        var context = CreateContext(world, bones, new Vector3(1.2f, 0.8f, 0));

        new TwoBoneSolver().Solve(in context);

        ref readonly var tipLocal = ref world.Get<Transform3D>(bones[2]);
        MathF.Abs(Quaternion.Dot(tipLocal.Rotation, Quaternion.Identity)).ShouldBeGreaterThan(0.9999f);
    }

    #endregion
}
