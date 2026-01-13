using System.Numerics;

using KeenEyes.Animation.Rendering;

namespace KeenEyes.Animation.Tests;

/// <summary>
/// Tests for BoneMatrixBuffer dirty tracking and matrix management.
/// </summary>
public class BoneMatrixBufferTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithDefaultCapacity_Creates128BoneBuffer()
    {
        using var buffer = new BoneMatrixBuffer();

        Assert.Equal(128, buffer.MaxBones);
    }

    [Fact]
    public void Constructor_WithCustomCapacity_CreatesCorrectSize()
    {
        using var buffer = new BoneMatrixBuffer(64);

        Assert.Equal(64, buffer.MaxBones);
    }

    [Fact]
    public void Constructor_WithZeroCapacity_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BoneMatrixBuffer(0));
    }

    [Fact]
    public void Constructor_WithNegativeCapacity_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BoneMatrixBuffer(-1));
    }

    [Fact]
    public void Constructor_InitializesAllMatricesToIdentity()
    {
        using var buffer = new BoneMatrixBuffer(4);

        for (var i = 0; i < 4; i++)
        {
            Assert.Equal(Matrix4x4.Identity, buffer.GetBoneMatrix(i));
        }
    }

    #endregion

    #region SetBoneMatrix / GetBoneMatrix Tests

    [Fact]
    public void SetBoneMatrix_StoresMatrix()
    {
        using var buffer = new BoneMatrixBuffer(4);
        var matrix = Matrix4x4.CreateTranslation(1, 2, 3);

        buffer.SetBoneMatrix(0, matrix, 1);

        Assert.Equal(matrix, buffer.GetBoneMatrix(0));
    }

    [Fact]
    public void SetBoneMatrix_WithNegativeIndex_ThrowsArgumentOutOfRangeException()
    {
        using var buffer = new BoneMatrixBuffer(4);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            buffer.SetBoneMatrix(-1, Matrix4x4.Identity, 1));
    }

    [Fact]
    public void SetBoneMatrix_WithIndexOutOfRange_ThrowsArgumentOutOfRangeException()
    {
        using var buffer = new BoneMatrixBuffer(4);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            buffer.SetBoneMatrix(4, Matrix4x4.Identity, 1));
    }

    [Fact]
    public void GetBoneMatrix_WithNegativeIndex_ThrowsArgumentOutOfRangeException()
    {
        using var buffer = new BoneMatrixBuffer(4);

        Assert.Throws<ArgumentOutOfRangeException>(() => buffer.GetBoneMatrix(-1));
    }

    [Fact]
    public void GetBoneMatrix_WithIndexOutOfRange_ThrowsArgumentOutOfRangeException()
    {
        using var buffer = new BoneMatrixBuffer(4);

        Assert.Throws<ArgumentOutOfRangeException>(() => buffer.GetBoneMatrix(4));
    }

    #endregion

    #region Generation / Dirty Tracking Tests

    [Fact]
    public void SetBoneMatrix_UpdatesGeneration()
    {
        using var buffer = new BoneMatrixBuffer(4);

        buffer.SetBoneMatrix(0, Matrix4x4.Identity, 5);

        Assert.Equal(5UL, buffer.GetBoneGeneration(0));
    }

    [Fact]
    public void IsDirty_InitiallyFalse()
    {
        using var buffer = new BoneMatrixBuffer(4);

        Assert.False(buffer.IsDirty);
    }

    [Fact]
    public void IsDirty_TrueAfterSetBoneMatrix()
    {
        using var buffer = new BoneMatrixBuffer(4);

        buffer.SetBoneMatrix(0, Matrix4x4.Identity, 1);

        Assert.True(buffer.IsDirty);
    }

    [Fact]
    public void IsDirty_FalseAfterMarkAsUploaded()
    {
        using var buffer = new BoneMatrixBuffer(4);
        buffer.SetBoneMatrix(0, Matrix4x4.Identity, 1);

        buffer.MarkAsUploaded();

        Assert.False(buffer.IsDirty);
    }

    [Fact]
    public void GetDirtyBoneIndices_ReturnsModifiedBones()
    {
        using var buffer = new BoneMatrixBuffer(4);
        buffer.SetBoneMatrix(0, Matrix4x4.CreateTranslation(1, 0, 0), 1);
        buffer.SetBoneMatrix(2, Matrix4x4.CreateTranslation(0, 1, 0), 1);

        var dirtyIndices = buffer.GetDirtyBoneIndices().ToArray();

        Assert.Equal(2, dirtyIndices.Length);
        Assert.Contains(0, dirtyIndices);
        Assert.Contains(2, dirtyIndices);
    }

    [Fact]
    public void GetDirtyBoneIndices_AfterMarkAsUploaded_ReturnsEmpty()
    {
        using var buffer = new BoneMatrixBuffer(4);
        buffer.SetBoneMatrix(0, Matrix4x4.Identity, 1);
        buffer.MarkAsUploaded();

        var dirtyIndices = buffer.GetDirtyBoneIndices().ToArray();

        Assert.Empty(dirtyIndices);
    }

    [Fact]
    public void GetDirtyBoneCount_ReturnsCorrectCount()
    {
        using var buffer = new BoneMatrixBuffer(4);
        buffer.SetBoneMatrix(0, Matrix4x4.Identity, 1);
        buffer.SetBoneMatrix(1, Matrix4x4.Identity, 1);
        buffer.SetBoneMatrix(3, Matrix4x4.Identity, 1);

        Assert.Equal(3, buffer.GetDirtyBoneCount());
    }

    [Fact]
    public void GetDirtyBoneCount_AfterMarkAsUploaded_ReturnsZero()
    {
        using var buffer = new BoneMatrixBuffer(4);
        buffer.SetBoneMatrix(0, Matrix4x4.Identity, 1);
        buffer.MarkAsUploaded();

        Assert.Equal(0, buffer.GetDirtyBoneCount());
    }

    [Fact]
    public void SetBoneMatrix_WithOlderGeneration_DoesNotMarkDirty()
    {
        using var buffer = new BoneMatrixBuffer(4);
        buffer.SetBoneMatrix(0, Matrix4x4.Identity, 10);
        buffer.MarkAsUploaded();

        // Set with older generation
        buffer.SetBoneMatrix(1, Matrix4x4.Identity, 5);

        Assert.False(buffer.IsDirty);
    }

    [Fact]
    public void SetBoneMatrix_WithNewerGeneration_MarksDirty()
    {
        using var buffer = new BoneMatrixBuffer(4);
        buffer.SetBoneMatrix(0, Matrix4x4.Identity, 10);
        buffer.MarkAsUploaded();

        // Set with newer generation
        buffer.SetBoneMatrix(1, Matrix4x4.Identity, 15);

        Assert.True(buffer.IsDirty);
    }

    #endregion

    #region Invalidate / Reset Tests

    [Fact]
    public void Invalidate_MarksDirty()
    {
        using var buffer = new BoneMatrixBuffer(4);

        buffer.Invalidate();

        Assert.True(buffer.IsDirty);
    }

    [Fact]
    public void Invalidate_ResetsLastUploadGeneration()
    {
        using var buffer = new BoneMatrixBuffer(4);
        buffer.SetBoneMatrix(0, Matrix4x4.Identity, 100);
        buffer.MarkAsUploaded();
        buffer.Invalidate();

        // Now even generation 1 should be considered dirty
        buffer.SetBoneMatrix(1, Matrix4x4.Identity, 1);

        Assert.True(buffer.IsDirty);
        Assert.Contains(1, buffer.GetDirtyBoneIndices());
    }

    [Fact]
    public void Reset_ClearsAllMatricesToIdentity()
    {
        using var buffer = new BoneMatrixBuffer(4);
        buffer.SetBoneMatrix(0, Matrix4x4.CreateTranslation(1, 2, 3), 1);
        buffer.SetBoneMatrix(1, Matrix4x4.CreateScale(2), 1);

        buffer.Reset();

        Assert.Equal(Matrix4x4.Identity, buffer.GetBoneMatrix(0));
        Assert.Equal(Matrix4x4.Identity, buffer.GetBoneMatrix(1));
    }

    [Fact]
    public void Reset_ClearsDirtyState()
    {
        using var buffer = new BoneMatrixBuffer(4);
        buffer.SetBoneMatrix(0, Matrix4x4.Identity, 1);

        buffer.Reset();

        Assert.False(buffer.IsDirty);
    }

    [Fact]
    public void Reset_ClearsGenerations()
    {
        using var buffer = new BoneMatrixBuffer(4);
        buffer.SetBoneMatrix(0, Matrix4x4.Identity, 100);

        buffer.Reset();

        Assert.Equal(0UL, buffer.GetBoneGeneration(0));
    }

    #endregion

    #region CopyTo / GetMatrices Tests

    [Fact]
    public void CopyTo_CopiesMatricesToArray()
    {
        using var buffer = new BoneMatrixBuffer(4);
        var matrix0 = Matrix4x4.CreateTranslation(1, 0, 0);
        var matrix1 = Matrix4x4.CreateTranslation(0, 1, 0);
        buffer.SetBoneMatrix(0, matrix0, 1);
        buffer.SetBoneMatrix(1, matrix1, 1);

        var destination = new Matrix4x4[4];
        buffer.CopyTo(destination, 2);

        Assert.Equal(matrix0, destination[0]);
        Assert.Equal(matrix1, destination[1]);
    }

    [Fact]
    public void CopyTo_WithTooSmallDestination_ThrowsArgumentException()
    {
        using var buffer = new BoneMatrixBuffer(4);
        var destination = new Matrix4x4[2];

        Assert.Throws<ArgumentException>(() => buffer.CopyTo(destination, 4));
    }

    [Fact]
    public void CopyTo_WithNullDestination_ThrowsArgumentNullException()
    {
        using var buffer = new BoneMatrixBuffer(4);

        Assert.Throws<ArgumentNullException>(() => buffer.CopyTo(null!, 2));
    }

    [Fact]
    public void GetMatrices_ReturnsSpanOfRequestedSize()
    {
        using var buffer = new BoneMatrixBuffer(8);

        var span = buffer.GetMatrices(4);

        Assert.Equal(4, span.Length);
    }

    [Fact]
    public void GetMatrices_ClampsToMaxBones()
    {
        using var buffer = new BoneMatrixBuffer(4);

        var span = buffer.GetMatrices(100);

        Assert.Equal(4, span.Length);
    }

    [Fact]
    public void Matrices_Property_ReturnsAllMatrices()
    {
        using var buffer = new BoneMatrixBuffer(4);

        var span = buffer.Matrices;

        Assert.Equal(4, span.Length);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_IsIdempotent()
    {
        var buffer = new BoneMatrixBuffer(4);
        buffer.Dispose();
        buffer.Dispose(); // Should not throw
    }

    #endregion
}
