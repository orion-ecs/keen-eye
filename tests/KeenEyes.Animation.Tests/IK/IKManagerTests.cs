using System.Numerics;

using KeenEyes.Animation.IK;
using KeenEyes.Animation.IK.Solvers;

namespace KeenEyes.Animation.Tests.IK;

/// <summary>
/// Tests for IKManager registration and retrieval.
/// </summary>
public class IKManagerTests
{
    #region Rig Registration Tests

    [Fact]
    public void RegisterRig_ReturnsUniqueId()
    {
        using var manager = new IKManager();
        var rig1 = new IKRigDefinition { Name = "Rig1" };
        var rig2 = new IKRigDefinition { Name = "Rig2" };

        var id1 = manager.RegisterRig(rig1);
        var id2 = manager.RegisterRig(rig2);

        id1.ShouldNotBe(id2);
        id1.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void GetRig_ReturnsRegisteredRig()
    {
        using var manager = new IKManager();
        var rig = new IKRigDefinition { Name = "TestRig" };

        var id = manager.RegisterRig(rig);
        var retrieved = manager.GetRig(id);

        retrieved.ShouldBe(rig);
        retrieved.Name.ShouldBe("TestRig");
    }

    [Fact]
    public void TryGetRig_ReturnsTrueForRegistered()
    {
        using var manager = new IKManager();
        var rig = new IKRigDefinition { Name = "TestRig" };
        var id = manager.RegisterRig(rig);

        var found = manager.TryGetRig(id, out var retrieved);

        found.ShouldBeTrue();
        retrieved.ShouldBe(rig);
    }

    [Fact]
    public void TryGetRig_ReturnsFalseForUnknownId()
    {
        using var manager = new IKManager();

        var found = manager.TryGetRig(999, out var retrieved);

        found.ShouldBeFalse();
        retrieved.ShouldBeNull();
    }

    [Fact]
    public void UnregisterRig_RemovesRig()
    {
        using var manager = new IKManager();
        var rig = new IKRigDefinition { Name = "TestRig" };
        var id = manager.RegisterRig(rig);

        var removed = manager.UnregisterRig(id);

        removed.ShouldBeTrue();
        manager.TryGetRig(id, out _).ShouldBeFalse();
    }

    [Fact]
    public void RigCount_ReturnsCorrectCount()
    {
        using var manager = new IKManager();

        manager.RigCount.ShouldBe(0);

        manager.RegisterRig(new IKRigDefinition { Name = "Rig1" });
        manager.RegisterRig(new IKRigDefinition { Name = "Rig2" });

        manager.RigCount.ShouldBe(2);
    }

    #endregion

    #region Chain Registration Tests

    [Fact]
    public void RegisterChain_ReturnsUniqueId()
    {
        using var manager = new IKManager();
        var chain1 = IKChainDefinition.TwoBone("Arm", "A", "B", "C", Vector3.UnitZ);
        var chain2 = IKChainDefinition.TwoBone("Leg", "D", "E", "F", Vector3.UnitZ);

        var id1 = manager.RegisterChain(chain1);
        var id2 = manager.RegisterChain(chain2);

        id1.ShouldNotBe(id2);
        id1.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void TryGetChain_ReturnsRegisteredChain()
    {
        using var manager = new IKManager();
        var chain = IKChainDefinition.TwoBone("LeftArm", "Shoulder", "Elbow", "Hand", Vector3.UnitZ);

        var id = manager.RegisterChain(chain);
        var found = manager.TryGetChain(id, out var retrieved);

        found.ShouldBeTrue();
        retrieved.ShouldBe(chain);
        retrieved!.Name.ShouldBe("LeftArm");
    }

    [Fact]
    public void TryGetChain_ReturnsFalseForUnknownId()
    {
        using var manager = new IKManager();

        var found = manager.TryGetChain(999, out var retrieved);

        found.ShouldBeFalse();
        retrieved.ShouldBeNull();
    }

    [Fact]
    public void RegisterRig_AlsoRegistersContainedChains()
    {
        using var manager = new IKManager();
        var rig = new IKRigDefinition { Name = "Humanoid" }
            .AddTwoBoneChain("LeftArm", "A", "B", "C", Vector3.UnitZ)
            .AddTwoBoneChain("RightArm", "D", "E", "F", Vector3.UnitZ);

        manager.RegisterRig(rig);

        // Rig has 2 chains, so ChainCount should be 2
        manager.ChainCount.ShouldBe(2);
    }

    [Fact]
    public void UnregisterChain_RemovesChain()
    {
        using var manager = new IKManager();
        var chain = IKChainDefinition.TwoBone("Arm", "A", "B", "C", Vector3.UnitZ);
        var id = manager.RegisterChain(chain);

        var removed = manager.UnregisterChain(id);

        removed.ShouldBeTrue();
        manager.TryGetChain(id, out _).ShouldBeFalse();
    }

    [Fact]
    public void ChainCount_ReturnsCorrectCount()
    {
        using var manager = new IKManager();

        manager.ChainCount.ShouldBe(0);

        manager.RegisterChain(IKChainDefinition.TwoBone("C1", "A", "B", "C", Vector3.UnitZ));
        manager.RegisterChain(IKChainDefinition.TwoBone("C2", "D", "E", "F", Vector3.UnitZ));

        manager.ChainCount.ShouldBe(2);
    }

    #endregion

    #region Solver Tests

    [Fact]
    public void RegisterSolver_AddsSolver()
    {
        using var manager = new IKManager();
        var solver = new MockSolver(IKSolverType.TwoBone);

        manager.RegisterSolver(solver);

        manager.HasSolver(IKSolverType.TwoBone).ShouldBeTrue();
        manager.SolverCount.ShouldBe(1);
    }

    [Fact]
    public void GetSolverForType_ReturnsRegisteredSolver()
    {
        using var manager = new IKManager();
        var solver = new MockSolver(IKSolverType.FABRIK);
        manager.RegisterSolver(solver);

        var retrieved = manager.GetSolverForType(IKSolverType.FABRIK);

        retrieved.ShouldBe(solver);
    }

    [Fact]
    public void GetSolverForType_ThrowsForUnregisteredType()
    {
        using var manager = new IKManager();

        Should.Throw<InvalidOperationException>(() =>
            manager.GetSolverForType(IKSolverType.TwoBone));
    }

    [Fact]
    public void TryGetSolver_ReturnsFalseForUnregistered()
    {
        using var manager = new IKManager();

        var found = manager.TryGetSolver(IKSolverType.CCD, out var solver);

        found.ShouldBeFalse();
        solver.ShouldBeNull();
    }

    [Fact]
    public void GetSolver_ByChainId_ReturnsSolverForChainType()
    {
        using var manager = new IKManager();
        var chain = IKChainDefinition.TwoBone("Arm", "A", "B", "C", Vector3.UnitZ);
        var solver = new MockSolver(IKSolverType.TwoBone);

        var chainId = manager.RegisterChain(chain);
        manager.RegisterSolver(solver);

        var retrieved = manager.GetSolver(chainId);

        retrieved.ShouldBe(solver);
    }

    [Fact]
    public void GetSolver_ByChainId_ThrowsForUnknownChain()
    {
        using var manager = new IKManager();

        Should.Throw<KeyNotFoundException>(() => manager.GetSolver(999));
    }

    [Fact]
    public void HasSolver_ReturnsFalseForUnregistered()
    {
        using var manager = new IKManager();

        manager.HasSolver(IKSolverType.CCD).ShouldBeFalse();
    }

    #endregion

    #region Cleanup Tests

    [Fact]
    public void Clear_RemovesAllAssets()
    {
        using var manager = new IKManager();
        manager.RegisterRig(new IKRigDefinition { Name = "Rig" });
        manager.RegisterChain(IKChainDefinition.TwoBone("C", "A", "B", "C", Vector3.UnitZ));
        manager.RegisterSolver(new MockSolver(IKSolverType.TwoBone));

        manager.Clear();

        manager.RigCount.ShouldBe(0);
        manager.ChainCount.ShouldBe(0);
        manager.SolverCount.ShouldBe(0);
    }

    [Fact]
    public void Clear_ResetsIdCounters()
    {
        using var manager = new IKManager();
        manager.RegisterRig(new IKRigDefinition { Name = "Rig1" });
        manager.RegisterRig(new IKRigDefinition { Name = "Rig2" });

        manager.Clear();

        var id = manager.RegisterRig(new IKRigDefinition { Name = "NewRig" });
        id.ShouldBe(1); // IDs reset to 1
    }

    [Fact]
    public void Dispose_PreventsRegistration()
    {
        var manager = new IKManager();
        manager.Dispose();

        Should.Throw<ObjectDisposedException>(() =>
            manager.RegisterRig(new IKRigDefinition { Name = "Test" }));
    }

    [Fact]
    public void Dispose_PreventsChainRegistration()
    {
        var manager = new IKManager();
        manager.Dispose();

        Should.Throw<ObjectDisposedException>(() =>
            manager.RegisterChain(IKChainDefinition.TwoBone("C", "A", "B", "C", Vector3.UnitZ)));
    }

    [Fact]
    public void Dispose_PreventsSolverRegistration()
    {
        var manager = new IKManager();
        manager.Dispose();

        Should.Throw<ObjectDisposedException>(() =>
            manager.RegisterSolver(new MockSolver(IKSolverType.TwoBone)));
    }

    #endregion

    #region Mock Solver

    private sealed class MockSolver(IKSolverType solverType) : IIKSolver
    {
        public string Name => $"Mock{solverType}Solver";
        public IKSolverType SolverType => solverType;

        public IKSolverResult Solve(in IKSolverContext context)
            => IKSolverResult.Success();

        public bool CanHandle(int boneCount) => true;
    }

    #endregion
}
