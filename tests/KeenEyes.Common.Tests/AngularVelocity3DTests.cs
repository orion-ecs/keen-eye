using System.Numerics;

namespace KeenEyes.Common.Tests;

/// <summary>
/// Tests for the AngularVelocity3D component.
/// </summary>
public class AngularVelocity3DTests
{
    private const float Epsilon = 1e-5f;

    #region Zero Tests

    [Fact]
    public void Zero_Value_IsZeroVector()
    {
        var angularVelocity = AngularVelocity3D.Zero;

        Assert.Equal(Vector3.Zero, angularVelocity.Value);
    }

    [Fact]
    public void Zero_MultipleAccesses_ReturnsSameValue()
    {
        var zero1 = AngularVelocity3D.Zero;
        var zero2 = AngularVelocity3D.Zero;

        Assert.Equal(zero1.Value, zero2.Value);
    }

    #endregion

    #region Constructor Tests - Vector3

    [Fact]
    public void Constructor_WithVector3_SetsValue()
    {
        var vector = new Vector3(1.5f, 2.5f, 3.5f);

        var angularVelocity = new AngularVelocity3D(vector);

        Assert.Equal(vector, angularVelocity.Value);
    }

    [Fact]
    public void Constructor_WithZeroVector_SetsZeroValue()
    {
        var angularVelocity = new AngularVelocity3D(Vector3.Zero);

        Assert.Equal(Vector3.Zero, angularVelocity.Value);
    }

    [Fact]
    public void Constructor_WithNegativeComponents_PreservesSign()
    {
        var vector = new Vector3(-1.5f, -2.5f, -3.5f);

        var angularVelocity = new AngularVelocity3D(vector);

        Assert.Equal(vector, angularVelocity.Value);
    }

    [Fact]
    public void Constructor_WithUnitX_CreatesRotationAroundXAxis()
    {
        var angularVelocity = new AngularVelocity3D(Vector3.UnitX);

        Assert.Equal(1f, angularVelocity.Value.X, Epsilon);
        Assert.Equal(0f, angularVelocity.Value.Y, Epsilon);
        Assert.Equal(0f, angularVelocity.Value.Z, Epsilon);
    }

    [Fact]
    public void Constructor_WithUnitY_CreatesRotationAroundYAxis()
    {
        var angularVelocity = new AngularVelocity3D(Vector3.UnitY);

        Assert.Equal(0f, angularVelocity.Value.X, Epsilon);
        Assert.Equal(1f, angularVelocity.Value.Y, Epsilon);
        Assert.Equal(0f, angularVelocity.Value.Z, Epsilon);
    }

    [Fact]
    public void Constructor_WithUnitZ_CreatesRotationAroundZAxis()
    {
        var angularVelocity = new AngularVelocity3D(Vector3.UnitZ);

        Assert.Equal(0f, angularVelocity.Value.X, Epsilon);
        Assert.Equal(0f, angularVelocity.Value.Y, Epsilon);
        Assert.Equal(1f, angularVelocity.Value.Z, Epsilon);
    }

    #endregion

    #region Constructor Tests - XYZ Components

    [Fact]
    public void Constructor_WithXYZ_SetsCorrectValue()
    {
        var angularVelocity = new AngularVelocity3D(1.5f, 2.5f, 3.5f);

        Assert.Equal(1.5f, angularVelocity.Value.X, Epsilon);
        Assert.Equal(2.5f, angularVelocity.Value.Y, Epsilon);
        Assert.Equal(3.5f, angularVelocity.Value.Z, Epsilon);
    }

    [Fact]
    public void Constructor_WithAllZeros_CreatesZeroVelocity()
    {
        var angularVelocity = new AngularVelocity3D(0f, 0f, 0f);

        Assert.Equal(Vector3.Zero, angularVelocity.Value);
    }

    [Fact]
    public void Constructor_WithNegativeComponents_PreservesSignInXYZ()
    {
        var angularVelocity = new AngularVelocity3D(-1.5f, -2.5f, -3.5f);

        Assert.Equal(-1.5f, angularVelocity.Value.X, Epsilon);
        Assert.Equal(-2.5f, angularVelocity.Value.Y, Epsilon);
        Assert.Equal(-3.5f, angularVelocity.Value.Z, Epsilon);
    }

    [Fact]
    public void Constructor_WithMixedSigns_PreservesAllSigns()
    {
        var angularVelocity = new AngularVelocity3D(1.5f, -2.5f, 3.5f);

        Assert.Equal(1.5f, angularVelocity.Value.X, Epsilon);
        Assert.Equal(-2.5f, angularVelocity.Value.Y, Epsilon);
        Assert.Equal(3.5f, angularVelocity.Value.Z, Epsilon);
    }

    [Fact]
    public void Constructor_XYZAndVector3_AreEquivalent()
    {
        var xyz = new AngularVelocity3D(1.5f, 2.5f, 3.5f);
        var vec = new AngularVelocity3D(new Vector3(1.5f, 2.5f, 3.5f));

        Assert.Equal(xyz.Value, vec.Value);
    }

    #endregion

    #region Value Modification Tests

    [Fact]
    public void Value_CanBeModified()
    {
        var angularVelocity = AngularVelocity3D.Zero;
        var newValue = new Vector3(5f, 10f, 15f);

        angularVelocity.Value = newValue;

        Assert.Equal(newValue, angularVelocity.Value);
    }

    [Fact]
    public void Value_ModificationDoesNotAffectZero()
    {
        var angularVelocity = AngularVelocity3D.Zero;
        angularVelocity.Value = new Vector3(1f, 2f, 3f);

        var newZero = AngularVelocity3D.Zero;

        Assert.Equal(Vector3.Zero, newZero.Value);
    }

    #endregion

    #region Physical Interpretation Tests

    [Fact]
    public void Constructor_SpinningTopExample_CorrectMagnitude()
    {
        // Spinning top at 10 rad/s around Y axis
        var spinSpeed = 10f;
        var angularVelocity = new AngularVelocity3D(0f, spinSpeed, 0f);

        var magnitude = angularVelocity.Value.Length();

        Assert.Equal(spinSpeed, magnitude, Epsilon);
    }

    [Fact]
    public void Constructor_CompoundRotation_CombinesAxes()
    {
        // Rotating 3 rad/s around X and 4 rad/s around Y simultaneously
        var angularVelocity = new AngularVelocity3D(3f, 4f, 0f);

        // Magnitude should be sqrt(3^2 + 4^2) = 5
        var magnitude = angularVelocity.Value.Length();

        Assert.Equal(5f, magnitude, Epsilon);
    }

    [Fact]
    public void Constructor_OppositeDirections_CancelOut()
    {
        // Creating two opposite angular velocities
        var clockwise = new AngularVelocity3D(5f, 0f, 0f);
        var counterClockwise = new AngularVelocity3D(-5f, 0f, 0f);

        var combined = new AngularVelocity3D(
            clockwise.Value + counterClockwise.Value);

        Assert.Equal(Vector3.Zero, combined.Value);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Constructor_WithVeryLargeValues_StoresCorrectly()
    {
        var largeValue = new Vector3(1000000f, 2000000f, 3000000f);

        var angularVelocity = new AngularVelocity3D(largeValue);

        Assert.Equal(largeValue, angularVelocity.Value);
    }

    [Fact]
    public void Constructor_WithVerySmallValues_StoresCorrectly()
    {
        var smallValue = new Vector3(0.000001f, 0.000002f, 0.000003f);

        var angularVelocity = new AngularVelocity3D(smallValue);

        Assert.Equal(smallValue.X, angularVelocity.Value.X, 1e-10f);
        Assert.Equal(smallValue.Y, angularVelocity.Value.Y, 1e-10f);
        Assert.Equal(smallValue.Z, angularVelocity.Value.Z, 1e-10f);
    }

    [Fact]
    public void Constructor_WithNaN_StoresNaN()
    {
        var nanValue = new Vector3(float.NaN, float.NaN, float.NaN);

        var angularVelocity = new AngularVelocity3D(nanValue);

        Assert.True(float.IsNaN(angularVelocity.Value.X));
        Assert.True(float.IsNaN(angularVelocity.Value.Y));
        Assert.True(float.IsNaN(angularVelocity.Value.Z));
    }

    [Fact]
    public void Constructor_WithInfinity_StoresInfinity()
    {
        var infinityValue = new Vector3(float.PositiveInfinity, float.NegativeInfinity, 0f);

        var angularVelocity = new AngularVelocity3D(infinityValue);

        Assert.True(float.IsPositiveInfinity(angularVelocity.Value.X));
        Assert.True(float.IsNegativeInfinity(angularVelocity.Value.Y));
        Assert.Equal(0f, angularVelocity.Value.Z);
    }

    #endregion
}
