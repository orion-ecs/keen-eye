using System.Numerics;

using KeenEyes.Graphics.Abstractions;

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
    public void Default_BaseColorTextureId_IsZero()
    {
        var material = Material.Default;

        Assert.Equal(0, material.BaseColorTextureId);
    }

    [Fact]
    public void Default_NormalMapId_IsZero()
    {
        var material = Material.Default;

        Assert.Equal(0, material.NormalMapId);
    }

    [Fact]
    public void Default_BaseColorFactor_IsWhite()
    {
        var material = Material.Default;

        Assert.Equal(Vector4.One, material.BaseColorFactor);
    }

    [Fact]
    public void Default_EmissiveFactor_IsBlack()
    {
        var material = Material.Default;

        Assert.Equal(Vector3.Zero, material.EmissiveFactor);
    }

    [Fact]
    public void Default_MetallicFactor_IsZero()
    {
        var material = Material.Default;

        Assert.Equal(0f, material.MetallicFactor, Epsilon);
    }

    [Fact]
    public void Default_RoughnessFactor_IsHalf()
    {
        var material = Material.Default;

        Assert.Equal(0.5f, material.RoughnessFactor, Epsilon);
    }

    [Fact]
    public void Default_MetallicRoughnessTextureId_IsZero()
    {
        var material = Material.Default;

        Assert.Equal(0, material.MetallicRoughnessTextureId);
    }

    [Fact]
    public void Default_OcclusionTextureId_IsZero()
    {
        var material = Material.Default;

        Assert.Equal(0, material.OcclusionTextureId);
    }

    [Fact]
    public void Default_EmissiveTextureId_IsZero()
    {
        var material = Material.Default;

        Assert.Equal(0, material.EmissiveTextureId);
    }

    [Fact]
    public void Default_NormalScale_IsOne()
    {
        var material = Material.Default;

        Assert.Equal(1f, material.NormalScale, Epsilon);
    }

    [Fact]
    public void Default_OcclusionStrength_IsOne()
    {
        var material = Material.Default;

        Assert.Equal(1f, material.OcclusionStrength, Epsilon);
    }

    [Fact]
    public void Default_AlphaCutoff_IsHalf()
    {
        var material = Material.Default;

        Assert.Equal(0.5f, material.AlphaCutoff, Epsilon);
    }

    [Fact]
    public void Default_AlphaMode_IsOpaque()
    {
        var material = Material.Default;

        Assert.Equal(AlphaMode.Opaque, material.AlphaMode);
    }

    [Fact]
    public void Default_DoubleSided_IsFalse()
    {
        var material = Material.Default;

        Assert.False(material.DoubleSided);
    }

    #endregion

    #region Unlit Factory Tests

    [Fact]
    public void Unlit_SetsBaseColorFactor()
    {
        var color = new Vector4(1f, 0f, 0f, 1f);

        var material = Material.Unlit(color);

        Assert.Equal(color, material.BaseColorFactor);
    }

    [Fact]
    public void Unlit_MetallicFactor_IsZero()
    {
        var material = Material.Unlit(Vector4.One);

        Assert.Equal(0f, material.MetallicFactor, Epsilon);
    }

    [Fact]
    public void Unlit_RoughnessFactor_IsOne()
    {
        var material = Material.Unlit(Vector4.One);

        Assert.Equal(1f, material.RoughnessFactor, Epsilon);
    }

    [Fact]
    public void Unlit_TextureIds_AreZero()
    {
        var material = Material.Unlit(Vector4.One);

        Assert.Equal(0, material.ShaderId);
        Assert.Equal(0, material.BaseColorTextureId);
        Assert.Equal(0, material.NormalMapId);
    }

    [Fact]
    public void Unlit_WithTransparentColor_PreservesAlpha()
    {
        var color = new Vector4(1f, 1f, 1f, 0.5f);

        var material = Material.Unlit(color);

        Assert.Equal(0.5f, material.BaseColorFactor.W, Epsilon);
    }

    #endregion

    #region Struct Behavior Tests

    [Fact]
    public void Material_IsValueType()
    {
        var material1 = Material.Default;
        var material2 = material1;

        material2.MetallicFactor = 1f;

        // Changes to material2 should not affect material1
        Assert.Equal(0f, material1.MetallicFactor);
        Assert.Equal(1f, material2.MetallicFactor);
    }

    [Fact]
    public void Material_DefaultConstructor_InitializesToZero()
    {
        var material = new Material();

        Assert.Equal(0, material.ShaderId);
        Assert.Equal(0, material.BaseColorTextureId);
        Assert.Equal(Vector4.Zero, material.BaseColorFactor);
        Assert.Equal(0f, material.MetallicFactor);
        Assert.Equal(0f, material.RoughnessFactor);
    }

    #endregion
}
