using System.Numerics;

using KeenEyes.Animation.IK;
using KeenEyes.Animation.IK.Solvers;

namespace KeenEyes.Animation.Tests.IK;

/// <summary>
/// Tests for the iterative FABRIK IK solver.
/// </summary>
public class FABRIKSolverTests
{
    private const float PositionTolerance = 0.001f;

    private static IKSolverContext CreateContext(
        World world,
        Entity[] bones,
        Vector3 target,
        IKChainDefinition? chain = null) => new()
        {
            World = world,
            Chain = chain ?? IKChainDefinition.MultiBone("Chain", MakeBoneNames(bones.Length)),
            BoneEntities = bones,
            TargetPosition = target,
        };

    private static string[] MakeBoneNames(int count)
    {
        var names = new string[count];
        for (var i = 0; i < count; i++)
        {
            names[i] = $"Bone{i}";
        }

        return names;
    }

    private static Entity[] CreateSpineChain(World world)
    {
        // Five bones stacked along +Y, each segment 1 unit long (total reach 4).
        return IKSolverTestHelpers.CreateChain(
            world,
            new Vector3(0, 0, 0),
            new Vector3(0, 1, 0),
            new Vector3(0, 1, 0),
            new Vector3(0, 1, 0),
            new Vector3(0, 1, 0));
    }

    private static Entity[] CreateTailChain(World world)
    {
        // Eight bones along +X, each segment 0.5 units long (total reach 3.5).
        return IKSolverTestHelpers.CreateChain(
            world,
            new Vector3(0, 0, 0),
            new Vector3(0.5f, 0, 0),
            new Vector3(0.5f, 0, 0),
            new Vector3(0.5f, 0, 0),
            new Vector3(0.5f, 0, 0),
            new Vector3(0.5f, 0, 0),
            new Vector3(0.5f, 0, 0),
            new Vector3(0.5f, 0, 0));
    }

    #region Metadata Tests

    [Fact]
    public void Name_ReturnsFABRIK()
    {
        var solver = new FABRIKSolver();

        solver.Name.ShouldBe("FABRIK");
    }

    [Fact]
    public void SolverType_ReturnsFABRIK()
    {
        var solver = new FABRIKSolver();

        solver.SolverType.ShouldBe(IKSolverType.FABRIK);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(8)]
    public void CanHandle_MultiBoneCounts_ReturnsTrue(int boneCount)
    {
        var solver = new FABRIKSolver();

        solver.CanHandle(boneCount).ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void CanHandle_FewerThanTwoBones_ReturnsFalse(int boneCount)
    {
        var solver = new FABRIKSolver();

        solver.CanHandle(boneCount).ShouldBeFalse();
    }

    #endregion

    #region Reaching Tests

    [Fact]
    public void Solve_FiveBoneSpineReachingTarget_TipReachesTarget()
    {
        using var world = new World();
        var bones = CreateSpineChain(world);
        var target = new Vector3(2, 2, 0);
        var context = CreateContext(world, bones, target);

        var result = new FABRIKSolver().Solve(in context);

        result.Converged.ShouldBeTrue();
        var tipPos = IKSolverTestHelpers.GetWorldPosition(world, bones[^1]);
        Vector3.Distance(tipPos, target).ShouldBeLessThan(PositionTolerance * 2f);
    }

    [Fact]
    public void Solve_EightBoneTailFollowingTarget_TipReachesTarget()
    {
        using var world = new World();
        var bones = CreateTailChain(world);
        var target = new Vector3(2f, 1.5f, 0.5f);
        var context = CreateContext(world, bones, target);

        var result = new FABRIKSolver().Solve(in context);

        result.Converged.ShouldBeTrue();
        var tipPos = IKSolverTestHelpers.GetWorldPosition(world, bones[^1]);
        Vector3.Distance(tipPos, target).ShouldBeLessThan(PositionTolerance * 2f);
    }

    [Fact]
    public void Solve_ReachableTarget_ConvergesWithinTenIterations()
    {
        using var world = new World();
        var bones = CreateSpineChain(world);
        var chain = IKChainDefinition.MultiBone("Spine", MakeBoneNames(bones.Length), maxIterations: 10);
        var context = CreateContext(world, bones, new Vector3(2, 2, 0), chain);

        var result = new FABRIKSolver().Solve(in context);

        result.Converged.ShouldBeTrue();
        result.Iterations.ShouldBeLessThanOrEqualTo(10);
        result.FinalError.ShouldBeLessThanOrEqualTo(chain.Tolerance);
    }

    [Fact]
    public void Solve_ComplexPoseBehindChain_ConvergesWithMultipleIterations()
    {
        using var world = new World();
        var bones = CreateSpineChain(world);

        // Target below and behind the chain root forces a large bend.
        var target = new Vector3(0.5f, -1.5f, 1f);
        var context = CreateContext(world, bones, target);

        var result = new FABRIKSolver().Solve(in context);

        result.Converged.ShouldBeTrue();
        result.Iterations.ShouldBeGreaterThanOrEqualTo(1);
        var tipPos = IKSolverTestHelpers.GetWorldPosition(world, bones[^1]);
        Vector3.Distance(tipPos, target).ShouldBeLessThan(PositionTolerance * 2f);
    }

    [Fact]
    public void Solve_VeryCloseTarget_ReachesExactly()
    {
        using var world = new World();
        var bones = CreateSpineChain(world);

        // Target a small nudge away from the current tip at (0, 4, 0). Near full
        // extension FABRIK converges slowly, so allow extra iterations.
        var target = new Vector3(0.1f, 3.9f, 0);
        var chain = IKChainDefinition.MultiBone("Spine", MakeBoneNames(bones.Length), maxIterations: 30);
        var context = CreateContext(world, bones, target, chain);

        var result = new FABRIKSolver().Solve(in context);

        result.Converged.ShouldBeTrue();
        result.FinalError.ShouldBeLessThanOrEqualTo(chain.Tolerance);
        var tipPos = IKSolverTestHelpers.GetWorldPosition(world, bones[^1]);
        Vector3.Distance(tipPos, target).ShouldBeLessThan(PositionTolerance * 2f);
    }

    [Fact]
    public void Solve_TargetAtCurrentTip_ConvergesWithoutIterating()
    {
        using var world = new World();
        var bones = CreateSpineChain(world);

        // The chain already ends at (0, 4, 0).
        var context = CreateContext(world, bones, new Vector3(0, 4, 0));

        var result = new FABRIKSolver().Solve(in context);

        result.Converged.ShouldBeTrue();
        result.Iterations.ShouldBe(0);
    }

    #endregion

    #region Unreachable Target Tests

    [Fact]
    public void Solve_UnreachableTarget_StretchesTowardTarget()
    {
        using var world = new World();
        var bones = CreateSpineChain(world);

        // Total reach is 4; the target is 10 units away along +X.
        var target = new Vector3(10, 0, 0);
        var context = CreateContext(world, bones, target);

        var result = new FABRIKSolver().Solve(in context);

        result.Converged.ShouldBeFalse();
        var tipPos = IKSolverTestHelpers.GetWorldPosition(world, bones[^1]);
        Vector3.Distance(tipPos, new Vector3(4, 0, 0)).ShouldBeLessThan(PositionTolerance);

        var lengths = IKSolverTestHelpers.GetBoneLengths(world, bones);
        foreach (var length in lengths)
        {
            length.ShouldBe(1f, PositionTolerance);
        }
    }

    #endregion

    #region Bone Length Preservation Tests

    [Fact]
    public void Solve_ReachableBendTarget_PreservesBoneLengths()
    {
        using var world = new World();
        var bones = CreateTailChain(world);
        var lengthsBefore = IKSolverTestHelpers.GetBoneLengths(world, bones);
        var context = CreateContext(world, bones, new Vector3(1.5f, 1.5f, 1f));

        new FABRIKSolver().Solve(in context);

        var lengthsAfter = IKSolverTestHelpers.GetBoneLengths(world, bones);
        for (var i = 0; i < lengthsBefore.Length; i++)
        {
            lengthsAfter[i].ShouldBe(lengthsBefore[i], PositionTolerance);
        }
    }

    #endregion
}
