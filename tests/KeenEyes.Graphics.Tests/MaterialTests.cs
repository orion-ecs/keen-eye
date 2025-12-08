using System.Numerics;
using KeenEyes.Graphics;

namespace KeenEyes.Graphics.Tests;

/// <summary>
/// Tests for the Material component.
/// </summary>
public class MaterialTests
{
    private const float Epsilon = 1e-5f;

    #region Default Property Tests

    [Fact]
    public void Default_ShaderId_IsZero()
    {
        var material = Material.Default;

        Assert.Equal(0, material.ShaderId);
    }

    [Fact]
    public void Default_TextureId_IsZero()
    {
        var material = Material.Default;

        Assert.Equal(0, material.TextureId);
    }

    [Fact]
    public void Default_NormalMapId_IsZero()
    {
        var material = Material.Default;

        Assert.Equal(0, material.NormalMapId);
    }

    [Fact]
    public void Default_Color_IsWhite()
    {
        var material = Material.Default;

        Assert.Equal(Vector4.One, material.Color);
    }

    [Fact]
    public void Default_EmissiveColor_IsBlack()
    {
        var material = Material.Default;

        Assert.Equal(Vector3.Zero, material.EmissiveColor);
    }

    [Fact]
    public void Default_Metallic_IsZero()
    {
        var material = Material.Default;

        Assert.Equal(0f, material.Metallic, Epsilon);
    }

    [Fact]
    public void Default_Roughness_IsHalf()
    {
        var material = Material.Default;

        Assert.Equal(0.5f, material.Roughness, Epsilon);
    }

    #endregion

    #region Unlit Factory Tests

    [Fact]
    public void Unlit_SetsColor()
    {
        var color = new Vector4(1f, 0f, 0f, 1f);

        var material = Material.Unlit(color);

        Assert.Equal(color, material.Color);
    }

    [Fact]
    public void Unlit_Metallic_IsZero()
    {
        var material = Material.Unlit(Vector4.One);

        Assert.Equal(0f, material.Metallic, Epsilon);
    }

    [Fact]
    public void Unlit_Roughness_IsOne()
    {
        var material = Material.Unlit(Vector4.One);

        Assert.Equal(1f, material.Roughness, Epsilon);
    }

    [Fact]
    public void Unlit_TextureIds_AreZero()
    {
        var material = Material.Unlit(Vector4.One);

        Assert.Equal(0, material.ShaderId);
        Assert.Equal(0, material.TextureId);
        Assert.Equal(0, material.NormalMapId);
    }

    [Fact]
    public void Unlit_WithTransparentColor_PreservesAlpha()
    {
        var color = new Vector4(1f, 1f, 1f, 0.5f);

        var material = Material.Unlit(color);

        Assert.Equal(0.5f, material.Color.W, Epsilon);
    }

    #endregion

    #region Struct Behavior Tests

    [Fact]
    public void Material_IsValueType()
    {
        var material1 = Material.Default;
        var material2 = material1;

        material2.Metallic = 1f;

        // Changes to material2 should not affect material1
        Assert.Equal(0f, material1.Metallic);
        Assert.Equal(1f, material2.Metallic);
    }

    [Fact]
    public void Material_DefaultConstructor_InitializesToZero()
    {
        var material = new Material();

        Assert.Equal(0, material.ShaderId);
        Assert.Equal(0, material.TextureId);
        Assert.Equal(Vector4.Zero, material.Color);
        Assert.Equal(0f, material.Metallic);
        Assert.Equal(0f, material.Roughness);
    }

    #endregion
}
