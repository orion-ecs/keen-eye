using System.Numerics;

namespace KeenEyes.Common.Tests;

/// <summary>
/// Tests for the Transform2D component.
/// </summary>
public class Transform2DTests
{
    private const float Epsilon = 1e-5f;

    #region Identity Tests

    [Fact]
    public void Identity_Position_IsZero()
    {
        var transform = Transform2D.Identity;

        Assert.Equal(Vector2.Zero, transform.Position);
    }

    [Fact]
    public void Identity_Rotation_IsZero()
    {
        var transform = Transform2D.Identity;

        Assert.Equal(0f, transform.Rotation);
    }

    [Fact]
    public void Identity_Scale_IsOne()
    {
        var transform = Transform2D.Identity;

        Assert.Equal(Vector2.One, transform.Scale);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_SetsPosition()
    {
        var position = new Vector2(10, 20);
        var transform = new Transform2D(position, 0f, Vector2.One);

        Assert.Equal(position, transform.Position);
    }

    [Fact]
    public void Constructor_SetsRotation()
    {
        var rotation = MathF.PI / 4f;
        var transform = new Transform2D(Vector2.Zero, rotation, Vector2.One);

        Assert.Equal(rotation, transform.Rotation);
    }

    [Fact]
    public void Constructor_SetsScale()
    {
        var scale = new Vector2(2, 3);
        var transform = new Transform2D(Vector2.Zero, 0f, scale);

        Assert.Equal(scale, transform.Scale);
    }

    #endregion

    #region ToMatrix Tests

    [Fact]
    public void ToMatrix_Identity_ReturnsIdentityMatrix()
    {
        var transform = Transform2D.Identity;

        var matrix = transform.ToMatrix();

        Assert.True(AreMatricesEqual(Matrix3x2.Identity, matrix));
    }

    [Fact]
    public void ToMatrix_WithPosition_HasCorrectTranslation()
    {
        var position = new Vector2(10, 20);
        var transform = new Transform2D(position, 0f, Vector2.One);

        var matrix = transform.ToMatrix();

        Assert.Equal(position.X, matrix.M31, Epsilon);
        Assert.Equal(position.Y, matrix.M32, Epsilon);
    }

    [Fact]
    public void ToMatrix_WithScale_HasCorrectScaling()
    {
        var scale = new Vector2(2, 3);
        var transform = new Transform2D(Vector2.Zero, 0f, scale);

        var matrix = transform.ToMatrix();

        // For a scale-only matrix, the diagonal elements reflect the scale
        Assert.Equal(scale.X, matrix.M11, Epsilon);
        Assert.Equal(scale.Y, matrix.M22, Epsilon);
    }

    [Fact]
    public void ToMatrix_TransformsPointCorrectly()
    {
        var transform = new Transform2D(
            new Vector2(5, 0),
            0f,
            new Vector2(2, 2));

        var matrix = transform.ToMatrix();
        var point = new Vector2(1, 0);
        var transformed = Vector2.Transform(point, matrix);

        // Point at (1,0) scaled by 2 then translated by (5,0) = (7,0)
        Assert.Equal(7f, transformed.X, Epsilon);
        Assert.Equal(0f, transformed.Y, Epsilon);
    }

    [Fact]
    public void ToMatrix_WithRotation_TransformsPointCorrectly()
    {
        // 90 degree counter-clockwise rotation
        var transform = new Transform2D(
            Vector2.Zero,
            MathF.PI / 2f,
            Vector2.One);

        var matrix = transform.ToMatrix();
        var point = new Vector2(1, 0);
        var transformed = Vector2.Transform(point, matrix);

        // Point at (1,0) rotated 90 degrees CCW = (0,1)
        Assert.True(MathF.Abs(transformed.X) < Epsilon);
        Assert.Equal(1f, transformed.Y, Epsilon);
    }

    #endregion

    #region Direction Vector Tests

    [Fact]
    public void Forward_Identity_IsRight()
    {
        var transform = Transform2D.Identity;

        var forward = transform.Forward;

        Assert.Equal(1f, forward.X, Epsilon);
        Assert.Equal(0f, forward.Y, Epsilon);
    }

    [Fact]
    public void Right_Identity_IsDown()
    {
        var transform = Transform2D.Identity;

        var right = transform.Right;

        Assert.Equal(0f, right.X, Epsilon);
        Assert.Equal(-1f, right.Y, Epsilon);
    }

    [Fact]
    public void Forward_Rotated90Degrees_IsUp()
    {
        // Rotating 90 degrees counter-clockwise: right rotates to up
        var rotation = MathF.PI / 2f;
        var transform = new Transform2D(Vector2.Zero, rotation, Vector2.One);

        var forward = transform.Forward;

        Assert.True(MathF.Abs(forward.X) < Epsilon);
        Assert.Equal(1f, forward.Y, Epsilon);
    }

    [Fact]
    public void Right_Rotated90Degrees_IsRight()
    {
        // Rotating 90 degrees counter-clockwise
        var rotation = MathF.PI / 2f;
        var transform = new Transform2D(Vector2.Zero, rotation, Vector2.One);

        var right = transform.Right;

        Assert.Equal(1f, right.X, Epsilon);
        Assert.True(MathF.Abs(right.Y) < Epsilon);
    }

    [Fact]
    public void DirectionVectors_AreOrthogonal()
    {
        var rotation = 1.2345f; // Arbitrary rotation
        var transform = new Transform2D(Vector2.Zero, rotation, Vector2.One);

        var forward = transform.Forward;
        var right = transform.Right;

        // Dot product should be zero for orthogonal vectors
        Assert.Equal(0f, Vector2.Dot(forward, right), Epsilon);
    }

    [Fact]
    public void DirectionVectors_AreUnitLength()
    {
        var rotation = 1.2345f; // Arbitrary rotation
        var transform = new Transform2D(Vector2.Zero, rotation, Vector2.One);

        Assert.Equal(1f, transform.Forward.Length(), Epsilon);
        Assert.Equal(1f, transform.Right.Length(), Epsilon);
    }

    #endregion

    private static bool AreMatricesEqual(Matrix3x2 a, Matrix3x2 b)
    {
        return MathF.Abs(a.M11 - b.M11) < Epsilon &&
               MathF.Abs(a.M12 - b.M12) < Epsilon &&
               MathF.Abs(a.M21 - b.M21) < Epsilon &&
               MathF.Abs(a.M22 - b.M22) < Epsilon &&
               MathF.Abs(a.M31 - b.M31) < Epsilon &&
               MathF.Abs(a.M32 - b.M32) < Epsilon;
    }
}
