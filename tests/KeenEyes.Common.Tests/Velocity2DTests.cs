using System.Numerics;

namespace KeenEyes.Common.Tests;

/// <summary>
/// Tests for the Velocity2D component.
/// </summary>
public class Velocity2DTests
{
    private const float Epsilon = 1e-5f;

    #region Constructor Tests

    [Fact]
    public void Constructor_WithVector_SetsValue()
    {
        var vector = new Vector2(10, 20);

        var velocity = new Velocity2D(vector);

        Assert.Equal(vector, velocity.Value);
    }

    [Fact]
    public void Constructor_WithComponents_SetsValue()
    {
        var velocity = new Velocity2D(10, 20);

        Assert.Equal(10f, velocity.Value.X);
        Assert.Equal(20f, velocity.Value.Y);
    }

    #endregion

    #region Zero Tests

    [Fact]
    public void Zero_HasZeroValue()
    {
        var velocity = Velocity2D.Zero;

        Assert.Equal(Vector2.Zero, velocity.Value);
    }

    [Fact]
    public void Zero_HasZeroMagnitude()
    {
        var velocity = Velocity2D.Zero;

        Assert.Equal(0f, velocity.Magnitude());
    }

    #endregion

    #region Magnitude Tests

    [Fact]
    public void Magnitude_ReturnsCorrectLength()
    {
        var velocity = new Velocity2D(3, 4);

        Assert.Equal(5f, velocity.Magnitude(), Epsilon);
    }

    [Fact]
    public void Magnitude_WithNegativeComponents_ReturnsPositive()
    {
        var velocity = new Velocity2D(-3, -4);

        Assert.Equal(5f, velocity.Magnitude(), Epsilon);
    }

    [Fact]
    public void MagnitudeSquared_ReturnsCorrectValue()
    {
        var velocity = new Velocity2D(3, 4);

        Assert.Equal(25f, velocity.MagnitudeSquared(), Epsilon);
    }

    [Fact]
    public void MagnitudeSquared_AvoidsSqrt()
    {
        // This test verifies that MagnitudeSquared and Magnitude are consistent
        var velocity = new Velocity2D(5, 12);

        var magnitude = velocity.Magnitude();
        var magnitudeSquared = velocity.MagnitudeSquared();

        Assert.Equal(magnitude * magnitude, magnitudeSquared, Epsilon);
    }

    [Fact]
    public void MagnitudeSquared_WithZero_ReturnsZero()
    {
        var velocity = Velocity2D.Zero;

        Assert.Equal(0f, velocity.MagnitudeSquared());
    }

    #endregion

    #region Component Value Tests

    [Fact]
    public void Value_CanBeModified()
    {
        var velocity = new Velocity2D(10, 20);

        velocity.Value = new Vector2(30, 40);

        Assert.Equal(30f, velocity.Value.X);
        Assert.Equal(40f, velocity.Value.Y);
    }

    [Fact]
    public void Value_AfterModification_UpdatesMagnitude()
    {
        var velocity = new Velocity2D(3, 4);
        Assert.Equal(5f, velocity.Magnitude(), Epsilon);

        velocity.Value = new Vector2(5, 12);

        Assert.Equal(13f, velocity.Magnitude(), Epsilon);
    }

    #endregion
}
