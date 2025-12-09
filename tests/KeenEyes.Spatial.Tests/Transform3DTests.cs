using System.Numerics;
using KeenEyes.Common;

namespace KeenEyes.Spatial.Tests;

/// <summary>
/// Tests for the Transform3D component.
/// </summary>
public class Transform3DTests
{
    private const float Epsilon = 1e-5f;

    #region Identity Tests

    [Fact]
    public void Identity_Position_IsZero()
    {
        var transform = Transform3D.Identity;

        Assert.Equal(Vector3.Zero, transform.Position);
    }

    [Fact]
    public void Identity_Rotation_IsIdentityQuaternion()
    {
        var transform = Transform3D.Identity;

        Assert.Equal(Quaternion.Identity, transform.Rotation);
    }

    [Fact]
    public void Identity_Scale_IsOne()
    {
        var transform = Transform3D.Identity;

        Assert.Equal(Vector3.One, transform.Scale);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_SetsPosition()
    {
        var position = new Vector3(1, 2, 3);
        var transform = new Transform3D(position, Quaternion.Identity, Vector3.One);

        Assert.Equal(position, transform.Position);
    }

    [Fact]
    public void Constructor_SetsRotation()
    {
        var rotation = Quaternion.CreateFromYawPitchRoll(1f, 0.5f, 0.25f);
        var transform = new Transform3D(Vector3.Zero, rotation, Vector3.One);

        Assert.Equal(rotation, transform.Rotation);
    }

    [Fact]
    public void Constructor_SetsScale()
    {
        var scale = new Vector3(2, 3, 4);
        var transform = new Transform3D(Vector3.Zero, Quaternion.Identity, scale);

        Assert.Equal(scale, transform.Scale);
    }

    #endregion

    #region ToMatrix Tests

    [Fact]
    public void ToMatrix_Identity_ReturnsIdentityMatrix()
    {
        var transform = Transform3D.Identity;

        var matrix = transform.Matrix();

        Assert.True(AreMatricesEqual(Matrix4x4.Identity, matrix));
    }

    [Fact]
    public void ToMatrix_WithPosition_HasCorrectTranslation()
    {
        var position = new Vector3(10, 20, 30);
        var transform = new Transform3D(position, Quaternion.Identity, Vector3.One);

        var matrix = transform.Matrix();

        Assert.Equal(position.X, matrix.M41, Epsilon);
        Assert.Equal(position.Y, matrix.M42, Epsilon);
        Assert.Equal(position.Z, matrix.M43, Epsilon);
    }

    [Fact]
    public void ToMatrix_WithScale_HasCorrectScaling()
    {
        var scale = new Vector3(2, 3, 4);
        var transform = new Transform3D(Vector3.Zero, Quaternion.Identity, scale);

        var matrix = transform.Matrix();

        // For a scale-only matrix, the diagonal elements reflect the scale
        Assert.Equal(scale.X, matrix.M11, Epsilon);
        Assert.Equal(scale.Y, matrix.M22, Epsilon);
        Assert.Equal(scale.Z, matrix.M33, Epsilon);
    }

    [Fact]
    public void ToMatrix_TransformsPointCorrectly()
    {
        var transform = new Transform3D(
            new Vector3(5, 0, 0),
            Quaternion.Identity,
            new Vector3(2, 2, 2));

        var matrix = transform.Matrix();
        var point = new Vector3(1, 0, 0);
        var transformed = Vector3.Transform(point, matrix);

        // Point at (1,0,0) scaled by 2 then translated by (5,0,0) = (7,0,0)
        Assert.Equal(7f, transformed.X, Epsilon);
        Assert.Equal(0f, transformed.Y, Epsilon);
        Assert.Equal(0f, transformed.Z, Epsilon);
    }

    #endregion

    #region Direction Vector Tests

    [Fact]
    public void Forward_Identity_IsNegativeZ()
    {
        var transform = Transform3D.Identity;

        var forward = transform.Forward();

        Assert.Equal(0f, forward.X, Epsilon);
        Assert.Equal(0f, forward.Y, Epsilon);
        Assert.Equal(-1f, forward.Z, Epsilon);
    }

    [Fact]
    public void Right_Identity_IsPositiveX()
    {
        var transform = Transform3D.Identity;

        var right = transform.Right();

        Assert.Equal(1f, right.X, Epsilon);
        Assert.Equal(0f, right.Y, Epsilon);
        Assert.Equal(0f, right.Z, Epsilon);
    }

    [Fact]
    public void Up_Identity_IsPositiveY()
    {
        var transform = Transform3D.Identity;

        var up = transform.Up();

        Assert.Equal(0f, up.X, Epsilon);
        Assert.Equal(1f, up.Y, Epsilon);
        Assert.Equal(0f, up.Z, Epsilon);
    }

    [Fact]
    public void Forward_Rotated90DegreesAroundY_IsNegativeX()
    {
        // Rotating 90 degrees around Y (right-hand rule): -Z rotates to -X
        var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2f);
        var transform = new Transform3D(Vector3.Zero, rotation, Vector3.One);

        var forward = transform.Forward();

        Assert.Equal(-1f, forward.X, Epsilon);
        Assert.Equal(0f, forward.Y, Epsilon);
        Assert.True(MathF.Abs(forward.Z) < Epsilon);
    }

    [Fact]
    public void DirectionVectors_AreOrthogonal()
    {
        var rotation = Quaternion.CreateFromYawPitchRoll(1f, 0.5f, 0.25f);
        var transform = new Transform3D(Vector3.Zero, rotation, Vector3.One);

        var forward = transform.Forward();
        var right = transform.Right();
        var up = transform.Up();

        // Dot products should be zero for orthogonal vectors
        Assert.Equal(0f, Vector3.Dot(forward, right), Epsilon);
        Assert.Equal(0f, Vector3.Dot(forward, up), Epsilon);
        Assert.Equal(0f, Vector3.Dot(right, up), Epsilon);
    }

    [Fact]
    public void DirectionVectors_AreUnitLength()
    {
        var rotation = Quaternion.CreateFromYawPitchRoll(1f, 0.5f, 0.25f);
        var transform = new Transform3D(Vector3.Zero, rotation, Vector3.One);

        Assert.Equal(1f, transform.Forward().Length(), Epsilon);
        Assert.Equal(1f, transform.Right().Length(), Epsilon);
        Assert.Equal(1f, transform.Up().Length(), Epsilon);
    }

    #endregion

    private static bool AreMatricesEqual(Matrix4x4 a, Matrix4x4 b)
    {
        return MathF.Abs(a.M11 - b.M11) < Epsilon &&
               MathF.Abs(a.M12 - b.M12) < Epsilon &&
               MathF.Abs(a.M13 - b.M13) < Epsilon &&
               MathF.Abs(a.M14 - b.M14) < Epsilon &&
               MathF.Abs(a.M21 - b.M21) < Epsilon &&
               MathF.Abs(a.M22 - b.M22) < Epsilon &&
               MathF.Abs(a.M23 - b.M23) < Epsilon &&
               MathF.Abs(a.M24 - b.M24) < Epsilon &&
               MathF.Abs(a.M31 - b.M31) < Epsilon &&
               MathF.Abs(a.M32 - b.M32) < Epsilon &&
               MathF.Abs(a.M33 - b.M33) < Epsilon &&
               MathF.Abs(a.M34 - b.M34) < Epsilon &&
               MathF.Abs(a.M41 - b.M41) < Epsilon &&
               MathF.Abs(a.M42 - b.M42) < Epsilon &&
               MathF.Abs(a.M43 - b.M43) < Epsilon &&
               MathF.Abs(a.M44 - b.M44) < Epsilon;
    }
}
