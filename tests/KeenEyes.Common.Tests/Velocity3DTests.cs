using System.Numerics;

namespace KeenEyes.Common.Tests;

/// <summary>
/// Tests for the Velocity3D component.
/// </summary>
public class Velocity3DTests
{
    private const float Epsilon = 1e-5f;

    #region Constructor Tests

    [Fact]
    public void Constructor_WithVector_SetsValue()
    {
        var vector = new Vector3(10, 20, 30);

        var velocity = new Velocity3D(vector);

        Assert.Equal(vector, velocity.Value);
    }

    [Fact]
    public void Constructor_WithComponents_SetsValue()
    {
        var velocity = new Velocity3D(10, 20, 30);

        Assert.Equal(10f, velocity.Value.X);
        Assert.Equal(20f, velocity.Value.Y);
        Assert.Equal(30f, velocity.Value.Z);
    }

    #endregion

    #region Zero Tests

    [Fact]
    public void Zero_HasZeroValue()
    {
        var velocity = Velocity3D.Zero;

        Assert.Equal(Vector3.Zero, velocity.Value);
    }

    [Fact]
    public void Zero_HasZeroMagnitude()
    {
        var velocity = Velocity3D.Zero;

        Assert.Equal(0f, velocity.Magnitude());
    }

    #endregion

    #region Magnitude Tests

    [Fact]
    public void Magnitude_ReturnsCorrectLength()
    {
        // Using 3-4-5 triangle in 3D: sqrt(2^2 + 3^2 + 6^2) = sqrt(4 + 9 + 36) = sqrt(49) = 7
        var velocity = new Velocity3D(2, 3, 6);

        Assert.Equal(7f, velocity.Magnitude(), Epsilon);
    }

    [Fact]
    public void Magnitude_WithNegativeComponents_ReturnsPositive()
    {
        var velocity = new Velocity3D(-2, -3, -6);

        Assert.Equal(7f, velocity.Magnitude(), Epsilon);
    }

    [Fact]
    public void MagnitudeSquared_ReturnsCorrectValue()
    {
        var velocity = new Velocity3D(2, 3, 6);

        Assert.Equal(49f, velocity.MagnitudeSquared(), Epsilon);
    }

    [Fact]
    public void MagnitudeSquared_AvoidsSqrt()
    {
        // This test verifies that MagnitudeSquared and Magnitude are consistent
        var velocity = new Velocity3D(1, 2, 2);

        var magnitude = velocity.Magnitude();
        var magnitudeSquared = velocity.MagnitudeSquared();

        Assert.Equal(magnitude * magnitude, magnitudeSquared, Epsilon);
    }

    [Fact]
    public void MagnitudeSquared_WithZero_ReturnsZero()
    {
        var velocity = Velocity3D.Zero;

        Assert.Equal(0f, velocity.MagnitudeSquared());
    }

    #endregion

    #region Component Value Tests

    [Fact]
    public void Value_CanBeModified()
    {
        var velocity = new Velocity3D(10, 20, 30);

        velocity.Value = new Vector3(40, 50, 60);

        Assert.Equal(40f, velocity.Value.X);
        Assert.Equal(50f, velocity.Value.Y);
        Assert.Equal(60f, velocity.Value.Z);
    }

    [Fact]
    public void Value_AfterModification_UpdatesMagnitude()
    {
        var velocity = new Velocity3D(2, 3, 6);
        Assert.Equal(7f, velocity.Magnitude(), Epsilon);

        velocity.Value = new Vector3(0, 0, 5);

        Assert.Equal(5f, velocity.Magnitude(), Epsilon);
    }

    #endregion
}
