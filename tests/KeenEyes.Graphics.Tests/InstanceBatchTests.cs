using System.Numerics;

using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Graphics.Tests;

/// <summary>
/// Tests for <see cref="InstanceBatch"/> component.
/// </summary>
public class InstanceBatchTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithBatchIdAndColor_SetsBothValues()
    {
        var colorTint = new Vector4(1, 0, 0, 1);

        var batch = new InstanceBatch(42, colorTint);

        Assert.Equal(42, batch.BatchId);
        Assert.Equal(colorTint, batch.ColorTint);
    }

    [Fact]
    public void Constructor_WithBatchIdOnly_SetsDefaultColorTint()
    {
        var batch = new InstanceBatch(42);

        Assert.Equal(42, batch.BatchId);
        Assert.Equal(Vector4.One, batch.ColorTint);
    }

    [Fact]
    public void Constructor_WithZeroBatchId_IsValid()
    {
        var batch = new InstanceBatch(0);

        Assert.Equal(0, batch.BatchId);
    }

    [Fact]
    public void Constructor_WithNegativeBatchId_IsAllowed()
    {
        var batch = new InstanceBatch(-1);

        Assert.Equal(-1, batch.BatchId);
    }

    #endregion

    #region Factory Method Tests

    [Fact]
    public void WithBatch_SetsBatchIdAndDefaultColor()
    {
        var batch = InstanceBatch.WithBatch(123);

        Assert.Equal(123, batch.BatchId);
        Assert.Equal(Vector4.One, batch.ColorTint);
    }

    [Fact]
    public void WithTint_SetsBatchIdAndColor()
    {
        var color = new Vector4(0.5f, 0.5f, 0.5f, 1f);

        var batch = InstanceBatch.WithTint(456, color);

        Assert.Equal(456, batch.BatchId);
        Assert.Equal(color, batch.ColorTint);
    }

    [Fact]
    public void WithTint_WithTransparentColor_IsAllowed()
    {
        var transparentColor = new Vector4(1, 1, 1, 0.5f);

        var batch = InstanceBatch.WithTint(1, transparentColor);

        Assert.Equal(transparentColor, batch.ColorTint);
    }

    #endregion

    #region Field Modification Tests

    [Fact]
    public void BatchId_CanBeModified()
    {
        var batch = new InstanceBatch(1);
        Assert.Equal(1, batch.BatchId);

        // Modify the field and verify new value
        batch.BatchId = 99;
        Assert.Equal(99, batch.BatchId);
    }

    [Fact]
    public void ColorTint_CanBeModified()
    {
        var batch = new InstanceBatch(1);
        Assert.Equal(Vector4.One, batch.ColorTint);

        // Modify the field and verify new value
        var newColor = new Vector4(0, 1, 0, 1);
        batch.ColorTint = newColor;
        Assert.Equal(newColor, batch.ColorTint);
    }

    #endregion

    #region Common Use Case Tests

    [Fact]
    public void InstanceBatch_RedTint_CreatesExpectedColor()
    {
        var red = new Vector4(1, 0, 0, 1);

        var batch = InstanceBatch.WithTint(1, red);

        Assert.Equal(1f, batch.ColorTint.X); // R
        Assert.Equal(0f, batch.ColorTint.Y); // G
        Assert.Equal(0f, batch.ColorTint.Z); // B
        Assert.Equal(1f, batch.ColorTint.W); // A
    }

    [Fact]
    public void InstanceBatch_DifferentBatchIds_CanBeDistinguished()
    {
        var batch1 = new InstanceBatch(1);
        var batch2 = new InstanceBatch(2);
        var batch3 = new InstanceBatch(1);

        Assert.NotEqual(batch1.BatchId, batch2.BatchId);
        Assert.Equal(batch1.BatchId, batch3.BatchId);
    }

    #endregion
}
