using System.Numerics;

using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Graphics.Tests;

/// <summary>
/// Tests for graphics component types (Light, Material, Renderable).
/// </summary>
public class ComponentTests
{
    private const float Epsilon = 1e-5f;

    #region Light Tests

    [Fact]
    public void Light_Directional_SetsTypeCorrectly()
    {
        var light = Light.Directional(Vector3.One, 1f);

        Assert.Equal(LightType.Directional, light.Type);
    }

    [Fact]
    public void Light_Directional_SetsColor()
    {
        var color = new Vector3(1f, 0.5f, 0.25f);
        var light = Light.Directional(color, 1f);

        Assert.Equal(color, light.Color);
    }

    [Fact]
    public void Light_Directional_SetsIntensity()
    {
        var light = Light.Directional(Vector3.One, 2.5f);

        Assert.Equal(2.5f, light.Intensity);
    }

    [Fact]
    public void Light_Directional_CastsShadowsByDefault()
    {
        var light = Light.Directional(Vector3.One, 1f);

        Assert.True(light.CastShadows);
    }

    [Fact]
    public void Light_Directional_HasDefaultShadowBias()
    {
        var light = Light.Directional(Vector3.One, 1f);

        Assert.Equal(0.005f, light.ShadowBias, Epsilon);
    }

    [Fact]
    public void Light_Point_SetsTypeCorrectly()
    {
        var light = Light.Point(Vector3.One, 1f, 10f);

        Assert.Equal(LightType.Point, light.Type);
    }

    [Fact]
    public void Light_Point_SetsColor()
    {
        var color = new Vector3(0.8f, 0.6f, 0.4f);
        var light = Light.Point(color, 1f, 10f);

        Assert.Equal(color, light.Color);
    }

    [Fact]
    public void Light_Point_SetsIntensity()
    {
        var light = Light.Point(Vector3.One, 3.0f, 10f);

        Assert.Equal(3.0f, light.Intensity);
    }

    [Fact]
    public void Light_Point_SetsRange()
    {
        var light = Light.Point(Vector3.One, 1f, 15.5f);

        Assert.Equal(15.5f, light.Range);
    }

    [Fact]
    public void Light_Point_DoesNotCastShadowsByDefault()
    {
        var light = Light.Point(Vector3.One, 1f, 10f);

        Assert.False(light.CastShadows);
    }

    [Fact]
    public void Light_Point_HasDefaultShadowBias()
    {
        var light = Light.Point(Vector3.One, 1f, 10f);

        Assert.Equal(0.005f, light.ShadowBias, Epsilon);
    }

    [Fact]
    public void Light_Spot_SetsTypeCorrectly()
    {
        var light = Light.Spot(Vector3.One, 1f, 10f, 30f, 45f);

        Assert.Equal(LightType.Spot, light.Type);
    }

    [Fact]
    public void Light_Spot_SetsColor()
    {
        var color = new Vector3(1f, 0.9f, 0.7f);
        var light = Light.Spot(color, 1f, 10f, 30f, 45f);

        Assert.Equal(color, light.Color);
    }

    [Fact]
    public void Light_Spot_SetsIntensity()
    {
        var light = Light.Spot(Vector3.One, 4.0f, 10f, 30f, 45f);

        Assert.Equal(4.0f, light.Intensity);
    }

    [Fact]
    public void Light_Spot_SetsRange()
    {
        var light = Light.Spot(Vector3.One, 1f, 20.0f, 30f, 45f);

        Assert.Equal(20.0f, light.Range);
    }

    [Fact]
    public void Light_Spot_SetsInnerConeAngle()
    {
        var light = Light.Spot(Vector3.One, 1f, 10f, 25f, 45f);

        Assert.Equal(25f, light.InnerConeAngle);
    }

    [Fact]
    public void Light_Spot_SetsOuterConeAngle()
    {
        var light = Light.Spot(Vector3.One, 1f, 10f, 30f, 50f);

        Assert.Equal(50f, light.OuterConeAngle);
    }

    [Fact]
    public void Light_Spot_CastsShadowsByDefault()
    {
        var light = Light.Spot(Vector3.One, 1f, 10f, 30f, 45f);

        Assert.True(light.CastShadows);
    }

    [Fact]
    public void Light_Spot_HasDefaultShadowBias()
    {
        var light = Light.Spot(Vector3.One, 1f, 10f, 30f, 45f);

        Assert.Equal(0.005f, light.ShadowBias, Epsilon);
    }

    [Fact]
    public void Light_CanModifyCastShadows()
    {
        var light = Light.Directional(Vector3.One, 1f);
        light.CastShadows = false;

        Assert.False(light.CastShadows);
    }

    [Fact]
    public void Light_CanModifyShadowBias()
    {
        var light = Light.Point(Vector3.One, 1f, 10f);
        light.ShadowBias = 0.01f;

        Assert.Equal(0.01f, light.ShadowBias);
    }

    [Fact]
    public void LightType_HasCorrectValues()
    {
        Assert.Equal(0, (int)LightType.Directional);
        Assert.Equal(1, (int)LightType.Point);
        Assert.Equal(2, (int)LightType.Spot);
    }

    #endregion

    #region Material Tests

    [Fact]
    public void Material_Default_HasWhiteColor()
    {
        var material = Material.Default;

        Assert.Equal(Vector4.One, material.Color);
    }

    [Fact]
    public void Material_Default_HasZeroShaderAndTextureIds()
    {
        var material = Material.Default;

        Assert.Equal(0, material.ShaderId);
        Assert.Equal(0, material.TextureId);
        Assert.Equal(0, material.NormalMapId);
    }

    [Fact]
    public void Material_Default_HasZeroEmissiveColor()
    {
        var material = Material.Default;

        Assert.Equal(Vector3.Zero, material.EmissiveColor);
    }

    [Fact]
    public void Material_Default_HasZeroMetallic()
    {
        var material = Material.Default;

        Assert.Equal(0f, material.Metallic);
    }

    [Fact]
    public void Material_Default_HasMidRoughness()
    {
        var material = Material.Default;

        Assert.Equal(0.5f, material.Roughness);
    }

    [Fact]
    public void Material_Unlit_SetsColor()
    {
        var color = new Vector4(0.5f, 0.25f, 0.75f, 1f);
        var material = Material.Unlit(color);

        Assert.Equal(color, material.Color);
    }

    [Fact]
    public void Material_Unlit_HasZeroMetallic()
    {
        var material = Material.Unlit(Vector4.One);

        Assert.Equal(0f, material.Metallic);
    }

    [Fact]
    public void Material_Unlit_HasFullRoughness()
    {
        var material = Material.Unlit(Vector4.One);

        Assert.Equal(1f, material.Roughness);
    }

    [Fact]
    public void Material_CanSetShaderId()
    {
        var material = Material.Default;
        material.ShaderId = 42;

        Assert.Equal(42, material.ShaderId);
    }

    [Fact]
    public void Material_CanSetTextureId()
    {
        var material = Material.Default;
        material.TextureId = 100;

        Assert.Equal(100, material.TextureId);
    }

    [Fact]
    public void Material_CanSetNormalMapId()
    {
        var material = Material.Default;
        material.NormalMapId = 200;

        Assert.Equal(200, material.NormalMapId);
    }

    [Fact]
    public void Material_CanSetEmissiveColor()
    {
        var material = Material.Default;
        var emissive = new Vector3(1f, 0.5f, 0f);
        material.EmissiveColor = emissive;

        Assert.Equal(emissive, material.EmissiveColor);
    }

    [Fact]
    public void Material_CanSetMetallic()
    {
        var material = Material.Default;
        material.Metallic = 1f;

        Assert.Equal(1f, material.Metallic);
    }

    [Fact]
    public void Material_CanSetRoughness()
    {
        var material = Material.Default;
        material.Roughness = 0.8f;

        Assert.Equal(0.8f, material.Roughness);
    }

    [Fact]
    public void Material_Unlit_HasDefaultTextureIds()
    {
        var material = Material.Unlit(Vector4.One);

        Assert.Equal(0, material.ShaderId);
        Assert.Equal(0, material.TextureId);
        Assert.Equal(0, material.NormalMapId);
    }

    [Fact]
    public void Material_Unlit_HasZeroEmissive()
    {
        var material = Material.Unlit(Vector4.One);

        Assert.Equal(Vector3.Zero, material.EmissiveColor);
    }

    #endregion

    #region Renderable Tests

    [Fact]
    public void Renderable_Constructor_SetsMeshId()
    {
        var renderable = new Renderable(42, 100);

        Assert.Equal(42, renderable.MeshId);
    }

    [Fact]
    public void Renderable_Constructor_SetsMaterialId()
    {
        var renderable = new Renderable(42, 100);

        Assert.Equal(100, renderable.MaterialId);
    }

    [Fact]
    public void Renderable_DefaultLayer_IsZero()
    {
        var renderable = new Renderable(1, 2);

        Assert.Equal(0, renderable.Layer);
    }

    [Fact]
    public void Renderable_DefaultCastShadows_IsTrue()
    {
        var renderable = new Renderable(1, 2);

        Assert.True(renderable.CastShadows);
    }

    [Fact]
    public void Renderable_DefaultReceiveShadows_IsTrue()
    {
        var renderable = new Renderable(1, 2);

        Assert.True(renderable.ReceiveShadows);
    }

    [Fact]
    public void Renderable_CanSetLayer()
    {
        var renderable = new Renderable(1, 2)
        {
            Layer = 5
        };

        Assert.Equal(5, renderable.Layer);
    }

    [Fact]
    public void Renderable_CanSetCastShadows()
    {
        var renderable = new Renderable(1, 2)
        {
            CastShadows = false
        };

        Assert.False(renderable.CastShadows);
    }

    [Fact]
    public void Renderable_CanSetReceiveShadows()
    {
        var renderable = new Renderable(1, 2)
        {
            ReceiveShadows = false
        };

        Assert.False(renderable.ReceiveShadows);
    }

    [Fact]
    public void Renderable_CanModifyMeshId()
    {
        var renderable = new Renderable(1, 2)
        {
            MeshId = 99
        };

        Assert.Equal(99, renderable.MeshId);
    }

    [Fact]
    public void Renderable_CanModifyMaterialId()
    {
        var renderable = new Renderable(1, 2)
        {
            MaterialId = 88
        };

        Assert.Equal(88, renderable.MaterialId);
    }

    [Fact]
    public void Renderable_AllPropertiesCanBeSetTogether()
    {
        var renderable = new Renderable(10, 20)
        {
            Layer = 3,
            CastShadows = false,
            ReceiveShadows = false
        };

        Assert.Equal(10, renderable.MeshId);
        Assert.Equal(20, renderable.MaterialId);
        Assert.Equal(3, renderable.Layer);
        Assert.False(renderable.CastShadows);
        Assert.False(renderable.ReceiveShadows);
    }

    [Fact]
    public void Renderable_NegativeLayer_IsAllowed()
    {
        var renderable = new Renderable(1, 2)
        {
            Layer = -5
        };

        Assert.Equal(-5, renderable.Layer);
    }

    [Fact]
    public void Renderable_ZeroHandles_AreAllowed()
    {
        var renderable = new Renderable(0, 0);

        Assert.Equal(0, renderable.MeshId);
        Assert.Equal(0, renderable.MaterialId);
    }

    [Fact]
    public void Renderable_NegativeHandles_AreAllowed()
    {
        var renderable = new Renderable(-1, -1);

        Assert.Equal(-1, renderable.MeshId);
        Assert.Equal(-1, renderable.MaterialId);
    }

    #endregion
}
