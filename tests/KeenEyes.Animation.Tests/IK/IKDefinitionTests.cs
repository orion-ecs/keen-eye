using System.Numerics;

using KeenEyes.Animation.IK;
using KeenEyes.Animation.IK.Solvers;

namespace KeenEyes.Animation.Tests.IK;

/// <summary>
/// Tests for IK definition classes and solver result types.
/// </summary>
public class IKDefinitionTests
{
    #region IKChainDefinition Tests

    [Fact]
    public void IKChainDefinition_TwoBone_CreatesTwoBoneChain()
    {
        var chain = IKChainDefinition.TwoBone(
            "LeftArm",
            "UpperArm",
            "LowerArm",
            "Hand",
            Vector3.UnitZ);

        chain.Name.ShouldBe("LeftArm");
        chain.BoneNames.ShouldBe(["UpperArm", "LowerArm", "Hand"]);
        chain.SolverType.ShouldBe(IKSolverType.TwoBone);
        chain.PoleVector.ShouldBe(Vector3.UnitZ);
    }

    [Fact]
    public void IKChainDefinition_MultiBone_CreatesFABRIKChain()
    {
        var bones = new[] { "Spine1", "Spine2", "Spine3", "Spine4", "Chest" };
        var chain = IKChainDefinition.MultiBone("Spine", bones, 15);

        chain.Name.ShouldBe("Spine");
        chain.BoneNames.ShouldBe(bones);
        chain.SolverType.ShouldBe(IKSolverType.FABRIK);
        chain.MaxIterations.ShouldBe(15);
    }

    [Fact]
    public void IKChainDefinition_MultiBone_DefaultIterations_Is10()
    {
        var bones = new[] { "Bone1", "Bone2", "Bone3" };
        var chain = IKChainDefinition.MultiBone("TestChain", bones);

        chain.MaxIterations.ShouldBe(10);
    }

    [Fact]
    public void IKChainDefinition_BoneCount_ReturnsCorrectCount()
    {
        var chain = IKChainDefinition.TwoBone("Arm", "A", "B", "C", Vector3.UnitZ);

        chain.BoneCount.ShouldBe(3);
    }

    [Fact]
    public void IKChainDefinition_DefaultTolerance_IsSmall()
    {
        var chain = IKChainDefinition.TwoBone("Arm", "A", "B", "C", Vector3.UnitZ);

        chain.Tolerance.ShouldBe(0.001f);
    }

    [Fact]
    public void IKChainDefinition_DefaultPoleVector_IsUnitZ()
    {
        var chain = new IKChainDefinition
        {
            Name = "Test",
            BoneNames = ["A", "B"]
        };

        chain.PoleVector.ShouldBe(Vector3.UnitZ);
    }

    #endregion

    #region IKRigDefinition Tests

    [Fact]
    public void IKRigDefinition_AddChain_AddsToList()
    {
        var rig = new IKRigDefinition { Name = "Humanoid" };
        var chain = IKChainDefinition.TwoBone("LeftArm", "A", "B", "C", Vector3.UnitZ);

        rig.AddChain(chain);

        rig.Chains.Count.ShouldBe(1);
        rig.Chains[0].ShouldBe(chain);
    }

    [Fact]
    public void IKRigDefinition_AddChain_ReturnsRigForFluent()
    {
        var rig = new IKRigDefinition { Name = "Humanoid" };
        var chain = IKChainDefinition.TwoBone("LeftArm", "A", "B", "C", Vector3.UnitZ);

        var result = rig.AddChain(chain);

        result.ShouldBeSameAs(rig);
    }

    [Fact]
    public void IKRigDefinition_AddTwoBoneChain_CreatesAndAdds()
    {
        var rig = new IKRigDefinition { Name = "Humanoid" }
            .AddTwoBoneChain("LeftArm", "UpperArm", "LowerArm", "Hand", Vector3.UnitZ)
            .AddTwoBoneChain("RightArm", "UpperArm.R", "LowerArm.R", "Hand.R", Vector3.UnitZ);

        rig.ChainCount.ShouldBe(2);
        rig.Chains[0].Name.ShouldBe("LeftArm");
        rig.Chains[1].Name.ShouldBe("RightArm");
    }

    [Fact]
    public void IKRigDefinition_AddFABRIKChain_CreatesAndAdds()
    {
        var rig = new IKRigDefinition { Name = "Snake" }
            .AddFABRIKChain("Body", ["Segment1", "Segment2", "Segment3", "Segment4"], 20);

        rig.ChainCount.ShouldBe(1);
        rig.Chains[0].Name.ShouldBe("Body");
        rig.Chains[0].SolverType.ShouldBe(IKSolverType.FABRIK);
        rig.Chains[0].MaxIterations.ShouldBe(20);
    }

    [Fact]
    public void IKRigDefinition_FindChain_ReturnsChainByName()
    {
        var rig = new IKRigDefinition { Name = "Humanoid" }
            .AddTwoBoneChain("LeftArm", "A", "B", "C", Vector3.UnitZ)
            .AddTwoBoneChain("RightArm", "D", "E", "F", Vector3.UnitZ);

        var found = rig.FindChain("RightArm");

        found.ShouldNotBeNull();
        found.Name.ShouldBe("RightArm");
    }

    [Fact]
    public void IKRigDefinition_FindChain_ReturnsNullForUnknown()
    {
        var rig = new IKRigDefinition { Name = "Humanoid" }
            .AddTwoBoneChain("LeftArm", "A", "B", "C", Vector3.UnitZ);

        var found = rig.FindChain("NonExistent");

        found.ShouldBeNull();
    }

    [Fact]
    public void IKRigDefinition_ChainCount_ReturnsCorrectCount()
    {
        var rig = new IKRigDefinition { Name = "Humanoid" }
            .AddTwoBoneChain("LeftArm", "A", "B", "C", Vector3.UnitZ)
            .AddTwoBoneChain("RightArm", "D", "E", "F", Vector3.UnitZ)
            .AddFABRIKChain("Spine", ["S1", "S2", "S3"]);

        rig.ChainCount.ShouldBe(3);
    }

    #endregion

    #region IKSolverResult Tests

    [Fact]
    public void IKSolverResult_Success_SetsConvergedTrue()
    {
        var result = IKSolverResult.Success(iterations: 5, error: 0.0001f);

        result.Converged.ShouldBeTrue();
        result.Iterations.ShouldBe(5);
        result.FinalError.ShouldBe(0.0001f);
    }

    [Fact]
    public void IKSolverResult_Success_DefaultValues()
    {
        var result = IKSolverResult.Success();

        result.Converged.ShouldBeTrue();
        result.Iterations.ShouldBe(0);
        result.FinalError.ShouldBe(0f);
    }

    [Fact]
    public void IKSolverResult_Partial_SetsConvergedFalse()
    {
        var result = IKSolverResult.Partial(iterations: 10, error: 0.5f);

        result.Converged.ShouldBeFalse();
        result.Iterations.ShouldBe(10);
        result.FinalError.ShouldBe(0.5f);
    }

    #endregion
}
