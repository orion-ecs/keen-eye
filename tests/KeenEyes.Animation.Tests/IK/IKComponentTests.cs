using System.Numerics;

using KeenEyes.Animation.Components;
using KeenEyes.Animation.IK;

namespace KeenEyes.Animation.Tests.IK;

/// <summary>
/// Tests for IK component factory methods and defaults.
/// </summary>
public class IKComponentTests
{
    #region IKRig Tests

    [Fact]
    public void IKRig_Default_ReturnsValidDefaults()
    {
        var rig = IKRig.Default;

        rig.RigId.ShouldBe(-1);
        rig.Enabled.ShouldBeTrue();
        rig.GlobalWeight.ShouldBe(1f);
    }

    [Fact]
    public void IKRig_ForRig_SetsRigId()
    {
        var rig = IKRig.ForRig(42);

        rig.RigId.ShouldBe(42);
        rig.Enabled.ShouldBeTrue();
        rig.GlobalWeight.ShouldBe(1f);
    }

    #endregion

    #region IKTarget Tests

    [Fact]
    public void IKTarget_Default_ReturnsValidDefaults()
    {
        var target = IKTarget.Default;

        target.Position.ShouldBe(Vector3.Zero);
        target.Rotation.ShouldBe(Quaternion.Identity);
        target.UseRotation.ShouldBeFalse();
        target.Weight.ShouldBe(1f);
        target.PoleTargetEntityId.ShouldBe(-1);
    }

    [Fact]
    public void IKTarget_AtPosition_SetsPosition()
    {
        var position = new Vector3(1f, 2f, 3f);
        var target = IKTarget.AtPosition(position);

        target.Position.ShouldBe(position);
        target.UseRotation.ShouldBeFalse();
        target.Weight.ShouldBe(1f);
    }

    [Fact]
    public void IKTarget_WithRotation_EnablesUseRotation()
    {
        var position = new Vector3(1f, 2f, 3f);
        var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 4);
        var target = IKTarget.WithRotation(position, rotation);

        target.Position.ShouldBe(position);
        target.Rotation.ShouldBe(rotation);
        target.UseRotation.ShouldBeTrue();
    }

    [Fact]
    public void IKTarget_WithPole_SetsPoleEntityId()
    {
        var position = new Vector3(1f, 2f, 3f);
        var target = IKTarget.WithPole(position, 123);

        target.Position.ShouldBe(position);
        target.PoleTargetEntityId.ShouldBe(123);
    }

    #endregion

    #region IKChainReference Tests

    [Fact]
    public void IKChainReference_Default_ReturnsValidDefaults()
    {
        var reference = IKChainReference.Default;

        reference.ChainId.ShouldBe(-1);
        reference.TargetEntityId.ShouldBe(-1);
        reference.Weight.ShouldBe(1f);
        reference.Enabled.ShouldBeTrue();
    }

    [Fact]
    public void IKChainReference_ForChain_SetsChainAndTarget()
    {
        var reference = IKChainReference.ForChain(5, 10);

        reference.ChainId.ShouldBe(5);
        reference.TargetEntityId.ShouldBe(10);
        reference.Weight.ShouldBe(1f);
        reference.Enabled.ShouldBeTrue();
    }

    #endregion

    #region IKConstraint Tests

    [Fact]
    public void IKConstraint_Default_ReturnsUnconstrained()
    {
        var constraint = IKConstraint.Default;

        constraint.ConstraintType.ShouldBe(IKConstraintType.None);
        constraint.Axis.ShouldBe(Vector3.UnitX);
        constraint.ConeAngle.ShouldBe(180f);
        constraint.TwistLimit.ShouldBe(180f);
    }

    [Fact]
    public void IKConstraint_Hinge_SetsTypeAndAxis()
    {
        var axis = Vector3.UnitZ;
        var constraint = IKConstraint.Hinge(axis, -90f, 0f);

        constraint.ConstraintType.ShouldBe(IKConstraintType.Hinge);

        // Axis should be normalized
        var normalizedAxis = Vector3.Normalize(axis);
        constraint.Axis.X.ShouldBe(normalizedAxis.X, 0.0001f);
        constraint.Axis.Y.ShouldBe(normalizedAxis.Y, 0.0001f);
        constraint.Axis.Z.ShouldBe(normalizedAxis.Z, 0.0001f);

        constraint.MinAngles.X.ShouldBe(-90f);
        constraint.MaxAngles.X.ShouldBe(0f);
    }

    [Fact]
    public void IKConstraint_BallSocket_SetsConeAndTwist()
    {
        var constraint = IKConstraint.BallSocket(45f, 30f);

        constraint.ConstraintType.ShouldBe(IKConstraintType.BallSocket);
        constraint.ConeAngle.ShouldBe(45f);
        constraint.TwistLimit.ShouldBe(30f);
    }

    [Fact]
    public void IKConstraint_BallSocket_DefaultTwist_Is180()
    {
        var constraint = IKConstraint.BallSocket(60f);

        constraint.TwistLimit.ShouldBe(180f);
    }

    [Fact]
    public void IKConstraint_Euler_SetsMinMax()
    {
        var min = new Vector3(-45f, -30f, -15f);
        var max = new Vector3(45f, 30f, 15f);
        var constraint = IKConstraint.Euler(min, max);

        constraint.ConstraintType.ShouldBe(IKConstraintType.Euler);
        constraint.MinAngles.ShouldBe(min);
        constraint.MaxAngles.ShouldBe(max);
    }

    #endregion

    #region LookAtTarget Tests

    [Fact]
    public void LookAtTarget_Default_ReturnsValidDefaults()
    {
        var target = LookAtTarget.Default;

        target.TargetEntityId.ShouldBe(-1);
        target.WorldTarget.ShouldBe(Vector3.Zero);
        target.ForwardAxis.ShouldBe(Vector3.UnitZ);
        target.UpAxis.ShouldBe(Vector3.UnitY);
        target.MaxAngle.ShouldBe(90f);
        target.Weight.ShouldBe(1f);
        target.Smoothing.ShouldBe(5f);
        target.CurrentRotation.ShouldBe(Quaternion.Identity);
    }

    [Fact]
    public void LookAtTarget_AtEntity_SetsEntityId()
    {
        var target = LookAtTarget.AtEntity(42);

        target.TargetEntityId.ShouldBe(42);
        target.WorldTarget.ShouldBe(Vector3.Zero);
    }

    [Fact]
    public void LookAtTarget_AtPosition_SetsWorldTarget()
    {
        var position = new Vector3(10f, 20f, 30f);
        var target = LookAtTarget.AtPosition(position);

        target.TargetEntityId.ShouldBe(-1);
        target.WorldTarget.ShouldBe(position);
    }

    #endregion
}
