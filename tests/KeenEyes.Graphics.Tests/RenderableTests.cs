using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Graphics.Tests;

/// <summary>
/// Tests for the Renderable component.
/// </summary>
public class RenderableTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_SetsMeshId()
    {
        var renderable = new Renderable(42, 10);

        Assert.Equal(42, renderable.MeshId);
    }

    [Fact]
    public void Constructor_SetsMaterialId()
    {
        var renderable = new Renderable(42, 10);

        Assert.Equal(10, renderable.MaterialId);
    }

    [Fact]
    public void Constructor_SetsLayerToZero()
    {
        var renderable = new Renderable(42, 10);

        Assert.Equal(0, renderable.Layer);
    }

    [Fact]
    public void Constructor_SetsCastShadowsToTrue()
    {
        var renderable = new Renderable(42, 10);

        Assert.True(renderable.CastShadows);
    }

    [Fact]
    public void Constructor_SetsReceiveShadowsToTrue()
    {
        var renderable = new Renderable(42, 10);

        Assert.True(renderable.ReceiveShadows);
    }

    #endregion

    #region Default Constructor Tests

    [Fact]
    public void DefaultConstructor_MeshIdIsZero()
    {
        var renderable = new Renderable();

        Assert.Equal(0, renderable.MeshId);
    }

    [Fact]
    public void DefaultConstructor_MaterialIdIsZero()
    {
        var renderable = new Renderable();

        Assert.Equal(0, renderable.MaterialId);
    }

    [Fact]
    public void DefaultConstructor_LayerIsZero()
    {
        var renderable = new Renderable();

        Assert.Equal(0, renderable.Layer);
    }

    [Fact]
    public void DefaultConstructor_CastShadowsIsFalse()
    {
        var renderable = new Renderable();

        Assert.False(renderable.CastShadows);
    }

    [Fact]
    public void DefaultConstructor_ReceiveShadowsIsFalse()
    {
        var renderable = new Renderable();

        Assert.False(renderable.ReceiveShadows);
    }

    #endregion

    #region Struct Behavior Tests

    [Fact]
    public void Renderable_IsValueType()
    {
        var renderable1 = new Renderable(1, 2);
        var renderable2 = renderable1;

        renderable2.MeshId = 99;

        // Changes to renderable2 should not affect renderable1
        Assert.Equal(1, renderable1.MeshId);
        Assert.Equal(99, renderable2.MeshId);
    }

    [Fact]
    public void Renderable_FieldsCanBeModified()
    {
        var renderable = new Renderable(1, 2)
        {
            Layer = 5,
            CastShadows = false,
            ReceiveShadows = false
        };

        Assert.Equal(5, renderable.Layer);
        Assert.False(renderable.CastShadows);
        Assert.False(renderable.ReceiveShadows);
    }

    #endregion
}
