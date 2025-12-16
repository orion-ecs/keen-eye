using System.Numerics;

using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Graphics.Tests;

/// <summary>
/// Tests for the Light component.
/// </summary>
public class LightTests
{
    private const float Epsilon = 1e-5f;

    #region Directional Light Tests

    [Fact]
    public void Directional_SetsType()
    {
        var light = Light.Directional(Vector3.One, 1f);

        Assert.Equal(LightType.Directional, light.Type);
    }

    [Fact]
    public void Directional_SetsColor()
    {
        var color = new Vector3(1f, 0.9f, 0.8f);

        var light = Light.Directional(color, 1f);

        Assert.Equal(color, light.Color);
    }

    [Fact]
    public void Directional_SetsIntensity()
    {
        var light = Light.Directional(Vector3.One, 2.5f);

        Assert.Equal(2.5f, light.Intensity, Epsilon);
    }

    [Fact]
    public void Directional_CastShadows_IsTrue()
    {
        var light = Light.Directional(Vector3.One, 1f);

        Assert.True(light.CastShadows);
    }

    [Fact]
    public void Directional_ShadowBias_HasDefaultValue()
    {
        var light = Light.Directional(Vector3.One, 1f);

        Assert.Equal(0.005f, light.ShadowBias, Epsilon);
    }

    [Fact]
    public void Directional_Range_IsNotSet()
    {
        var light = Light.Directional(Vector3.One, 1f);

        // Directional lights don't use range
        Assert.Equal(0f, light.Range);
    }

    #endregion

    #region Point Light Tests

    [Fact]
    public void Point_SetsType()
    {
        var light = Light.Point(Vector3.One, 1f, 10f);

        Assert.Equal(LightType.Point, light.Type);
    }

    [Fact]
    public void Point_SetsColor()
    {
        var color = new Vector3(1f, 0.5f, 0f);

        var light = Light.Point(color, 1f, 10f);

        Assert.Equal(color, light.Color);
    }

    [Fact]
    public void Point_SetsIntensity()
    {
        var light = Light.Point(Vector3.One, 3f, 10f);

        Assert.Equal(3f, light.Intensity, Epsilon);
    }

    [Fact]
    public void Point_SetsRange()
    {
        var light = Light.Point(Vector3.One, 1f, 15f);

        Assert.Equal(15f, light.Range, Epsilon);
    }

    [Fact]
    public void Point_CastShadows_IsFalse()
    {
        var light = Light.Point(Vector3.One, 1f, 10f);

        Assert.False(light.CastShadows);
    }

    [Fact]
    public void Point_ShadowBias_HasDefaultValue()
    {
        var light = Light.Point(Vector3.One, 1f, 10f);

        Assert.Equal(0.005f, light.ShadowBias, Epsilon);
    }

    [Fact]
    public void Point_ConeAngles_AreNotSet()
    {
        var light = Light.Point(Vector3.One, 1f, 10f);

        // Point lights don't use cone angles
        Assert.Equal(0f, light.InnerConeAngle);
        Assert.Equal(0f, light.OuterConeAngle);
    }

    #endregion

    #region Spot Light Tests

    [Fact]
    public void Spot_SetsType()
    {
        var light = Light.Spot(Vector3.One, 1f, 10f, 30f, 45f);

        Assert.Equal(LightType.Spot, light.Type);
    }

    [Fact]
    public void Spot_SetsColor()
    {
        var color = new Vector3(0f, 1f, 0f);

        var light = Light.Spot(color, 1f, 10f, 30f, 45f);

        Assert.Equal(color, light.Color);
    }

    [Fact]
    public void Spot_SetsIntensity()
    {
        var light = Light.Spot(Vector3.One, 5f, 10f, 30f, 45f);

        Assert.Equal(5f, light.Intensity, Epsilon);
    }

    [Fact]
    public void Spot_SetsRange()
    {
        var light = Light.Spot(Vector3.One, 1f, 20f, 30f, 45f);

        Assert.Equal(20f, light.Range, Epsilon);
    }

    [Fact]
    public void Spot_SetsInnerConeAngle()
    {
        var light = Light.Spot(Vector3.One, 1f, 10f, 25f, 45f);

        Assert.Equal(25f, light.InnerConeAngle, Epsilon);
    }

    [Fact]
    public void Spot_SetsOuterConeAngle()
    {
        var light = Light.Spot(Vector3.One, 1f, 10f, 30f, 60f);

        Assert.Equal(60f, light.OuterConeAngle, Epsilon);
    }

    [Fact]
    public void Spot_CastShadows_IsTrue()
    {
        var light = Light.Spot(Vector3.One, 1f, 10f, 30f, 45f);

        Assert.True(light.CastShadows);
    }

    [Fact]
    public void Spot_ShadowBias_HasDefaultValue()
    {
        var light = Light.Spot(Vector3.One, 1f, 10f, 30f, 45f);

        Assert.Equal(0.005f, light.ShadowBias, Epsilon);
    }

    #endregion

    #region LightType Enum Tests

    [Fact]
    public void LightType_HasExpectedValues()
    {
        Assert.Equal(0, (int)LightType.Directional);
        Assert.Equal(1, (int)LightType.Point);
        Assert.Equal(2, (int)LightType.Spot);
    }

    #endregion

    #region Struct Behavior Tests

    [Fact]
    public void Light_IsValueType()
    {
        var light1 = Light.Point(Vector3.One, 1f, 10f);
        var light2 = light1;

        light2.Intensity = 5f;

        // Changes to light2 should not affect light1
        Assert.Equal(1f, light1.Intensity);
        Assert.Equal(5f, light2.Intensity);
    }

    [Fact]
    public void Light_DefaultConstructor_InitializesToDefaults()
    {
        var light = new Light();

        Assert.Equal(LightType.Directional, light.Type);
        Assert.Equal(Vector3.Zero, light.Color);
        Assert.Equal(0f, light.Intensity);
        Assert.Equal(0f, light.Range);
        Assert.False(light.CastShadows);
    }

    #endregion
}
