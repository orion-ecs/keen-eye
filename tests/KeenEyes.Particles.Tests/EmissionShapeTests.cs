using System.Numerics;
using KeenEyes.Particles.Data;

namespace KeenEyes.Particles.Tests;

/// <summary>
/// Tests for the EmissionShape struct.
/// </summary>
public class EmissionShapeTests
{
    #region Point Shape Tests

    [Fact]
    public void Point_HasPointType()
    {
        var shape = EmissionShape.Point;

        Assert.Equal(EmissionShapeType.Point, shape.Type);
    }

    [Fact]
    public void Point_HasDefaultValues()
    {
        var shape = EmissionShape.Point;

        Assert.Equal(0f, shape.Radius);
        Assert.Equal(0f, shape.Angle);
        Assert.Equal(Vector2.Zero, shape.Size);
    }

    #endregion

    #region Sphere Shape Tests

    [Fact]
    public void Sphere_HasSphereType()
    {
        var shape = EmissionShape.Sphere(10f);

        Assert.Equal(EmissionShapeType.Sphere, shape.Type);
    }

    [Fact]
    public void Sphere_HasCorrectRadius()
    {
        var shape = EmissionShape.Sphere(25f);

        Assert.Equal(25f, shape.Radius);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(1f)]
    [InlineData(100f)]
    [InlineData(0.5f)]
    public void Sphere_AcceptsVariousRadii(float radius)
    {
        var shape = EmissionShape.Sphere(radius);

        Assert.Equal(radius, shape.Radius);
    }

    #endregion

    #region Cone Shape Tests

    [Fact]
    public void Cone_TwoParams_HasConeType()
    {
        var shape = EmissionShape.Cone(10f, MathF.PI / 4f);

        Assert.Equal(EmissionShapeType.Cone, shape.Type);
    }

    [Fact]
    public void Cone_TwoParams_HasCorrectRadiusAndAngle()
    {
        var shape = EmissionShape.Cone(15f, MathF.PI / 6f);

        Assert.Equal(15f, shape.Radius);
        Assert.Equal(MathF.PI / 6f, shape.Angle);
    }

    [Fact]
    public void Cone_TwoParams_DefaultsToUpwardDirection()
    {
        var shape = EmissionShape.Cone(10f, MathF.PI / 4f);

        Assert.Equal(Vector2.UnitY, shape.Direction);
    }

    [Fact]
    public void Cone_ThreeParams_HasCustomDirection()
    {
        var direction = new Vector2(1f, 0f); // Right
        var shape = EmissionShape.Cone(10f, MathF.PI / 4f, direction);

        Assert.Equal(EmissionShapeType.Cone, shape.Type);
        Assert.Equal(Vector2.UnitX, shape.Direction); // Should be normalized
    }

    [Fact]
    public void Cone_ThreeParams_NormalizesDirection()
    {
        var direction = new Vector2(3f, 4f); // Length = 5
        var shape = EmissionShape.Cone(10f, MathF.PI / 4f, direction);

        var normalized = Vector2.Normalize(direction);
        Assert.Equal(normalized.X, shape.Direction.X, 4);
        Assert.Equal(normalized.Y, shape.Direction.Y, 4);
    }

    [Fact]
    public void Cone_NegativeYDirection_PointsUpward()
    {
        var shape = EmissionShape.Cone(5f, MathF.PI / 3f, new Vector2(0, -1));

        Assert.True(shape.Direction.Y < 0);
    }

    #endregion

    #region Box Shape Tests

    [Fact]
    public void Box_HasBoxType()
    {
        var shape = EmissionShape.Box(100f, 50f);

        Assert.Equal(EmissionShapeType.Box, shape.Type);
    }

    [Fact]
    public void Box_HasCorrectSize()
    {
        var shape = EmissionShape.Box(200f, 100f);

        Assert.Equal(200f, shape.Size.X);
        Assert.Equal(100f, shape.Size.Y);
    }

    [Theory]
    [InlineData(10f, 10f)]
    [InlineData(100f, 50f)]
    [InlineData(1f, 1000f)]
    [InlineData(0f, 0f)]
    public void Box_AcceptsVariousSizes(float width, float height)
    {
        var shape = EmissionShape.Box(width, height);

        Assert.Equal(width, shape.Size.X);
        Assert.Equal(height, shape.Size.Y);
    }

    #endregion

    #region Record Struct Behavior Tests

    [Fact]
    public void EmissionShape_IsValueType()
    {
        var shape = EmissionShape.Point;

        Assert.True(shape.GetType().IsValueType);
    }

    [Fact]
    public void EmissionShape_SupportsEquality()
    {
        var shape1 = EmissionShape.Sphere(10f);
        var shape2 = EmissionShape.Sphere(10f);
        var shape3 = EmissionShape.Sphere(20f);

        Assert.Equal(shape1, shape2);
        Assert.NotEqual(shape1, shape3);
    }

    [Fact]
    public void EmissionShape_SupportsWithExpression()
    {
        var original = EmissionShape.Cone(10f, MathF.PI / 4f);
        var modified = original with { Radius = 20f };

        Assert.Equal(10f, original.Radius);
        Assert.Equal(20f, modified.Radius);
        Assert.Equal(original.Type, modified.Type);
        Assert.Equal(original.Angle, modified.Angle);
    }

    #endregion

    #region Default Shape Tests

    [Fact]
    public void DefaultShape_HasPointType()
    {
        EmissionShape shape = default;

        Assert.Equal(EmissionShapeType.Point, shape.Type);
    }

    #endregion
}
