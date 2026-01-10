using System.Numerics;
using System.Runtime.InteropServices;

using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Graphics.Tests;

/// <summary>
/// Tests for <see cref="InstanceData"/> struct.
/// </summary>
public class InstanceDataTests
{
    #region SizeInBytes Tests

    [Fact]
    public void SizeInBytes_Returns80()
    {
        Assert.Equal(80, InstanceData.SizeInBytes);
    }

    [Fact]
    public void SizeInBytes_MatchesActualStructSize()
    {
        var actualSize = Marshal.SizeOf<InstanceData>();
        Assert.Equal(InstanceData.SizeInBytes, actualSize);
    }

    #endregion

    #region FromTransform Tests

    [Fact]
    public void FromTransform_WithMatrix_SetsModelMatrix()
    {
        var matrix = Matrix4x4.CreateTranslation(1, 2, 3);

        var data = InstanceData.FromTransform(matrix);

        Assert.Equal(matrix, data.ModelMatrix);
    }

    [Fact]
    public void FromTransform_WithMatrix_SetsDefaultColorTint()
    {
        var matrix = Matrix4x4.Identity;

        var data = InstanceData.FromTransform(matrix);

        Assert.Equal(Vector4.One, data.ColorTint);
    }

    [Fact]
    public void FromTransform_WithMatrixAndColor_SetsModelMatrix()
    {
        var matrix = Matrix4x4.CreateScale(2);
        var color = new Vector4(1, 0, 0, 1);

        var data = InstanceData.FromTransform(matrix, color);

        Assert.Equal(matrix, data.ModelMatrix);
    }

    [Fact]
    public void FromTransform_WithMatrixAndColor_SetsColorTint()
    {
        var matrix = Matrix4x4.Identity;
        var color = new Vector4(0.5f, 0.5f, 0.5f, 0.5f);

        var data = InstanceData.FromTransform(matrix, color);

        Assert.Equal(color, data.ColorTint);
    }

    #endregion

    #region FromTRS Tests

    [Fact]
    public void FromTRS_WithTranslation_CreatesCorrectMatrix()
    {
        var position = new Vector3(10, 20, 30);
        var rotation = Quaternion.Identity;
        var scale = Vector3.One;

        var data = InstanceData.FromTRS(position, rotation, scale);

        // Extract translation from the matrix
        Assert.Equal(position.X, data.ModelMatrix.M41);
        Assert.Equal(position.Y, data.ModelMatrix.M42);
        Assert.Equal(position.Z, data.ModelMatrix.M43);
    }

    [Fact]
    public void FromTRS_WithScale_CreatesCorrectMatrix()
    {
        var position = Vector3.Zero;
        var rotation = Quaternion.Identity;
        var scale = new Vector3(2, 3, 4);

        var data = InstanceData.FromTRS(position, rotation, scale);

        // For identity rotation, scale should be on diagonal
        Assert.Equal(scale.X, data.ModelMatrix.M11, 5);
        Assert.Equal(scale.Y, data.ModelMatrix.M22, 5);
        Assert.Equal(scale.Z, data.ModelMatrix.M33, 5);
    }

    [Fact]
    public void FromTRS_WithoutColor_SetsDefaultColorTint()
    {
        var data = InstanceData.FromTRS(Vector3.Zero, Quaternion.Identity, Vector3.One);

        Assert.Equal(Vector4.One, data.ColorTint);
    }

    [Fact]
    public void FromTRS_WithColor_SetsColorTint()
    {
        var color = new Vector4(1, 0, 0, 1);

        var data = InstanceData.FromTRS(Vector3.Zero, Quaternion.Identity, Vector3.One, color);

        Assert.Equal(color, data.ColorTint);
    }

    [Fact]
    public void FromTRS_WithAllComponents_CreatesValidMatrix()
    {
        var position = new Vector3(5, 10, 15);
        var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 4);
        var scale = new Vector3(2, 2, 2);
        var color = new Vector4(0, 1, 0, 1);

        var data = InstanceData.FromTRS(position, rotation, scale, color);

        // The matrix should be valid (M44 should be 1 for a proper transformation matrix)
        Assert.Equal(1f, data.ModelMatrix.M44);
        Assert.Equal(color, data.ColorTint);
    }

    #endregion

    #region Struct Layout Tests

    [Fact]
    public void InstanceData_ModelMatrixOffset_IsZero()
    {
        // ModelMatrix should be at offset 0
        var offset = Marshal.OffsetOf<InstanceData>(nameof(InstanceData.ModelMatrix));
        Assert.Equal(0, offset.ToInt32());
    }

    [Fact]
    public void InstanceData_ColorTintOffset_Is64()
    {
        // ColorTint should be at offset 64 (after 4x4 matrix of floats)
        var offset = Marshal.OffsetOf<InstanceData>(nameof(InstanceData.ColorTint));
        Assert.Equal(64, offset.ToInt32());
    }

    [Fact]
    public void InstanceData_FieldsAreContiguous()
    {
        // Verify the struct is laid out contiguously (Matrix4x4 followed by Vector4)
        var matrixOffset = Marshal.OffsetOf<InstanceData>(nameof(InstanceData.ModelMatrix)).ToInt32();
        var colorOffset = Marshal.OffsetOf<InstanceData>(nameof(InstanceData.ColorTint)).ToInt32();

        // Matrix4x4 is 64 bytes, ColorTint should start immediately after
        Assert.Equal(64, colorOffset - matrixOffset);
    }

    #endregion
}
