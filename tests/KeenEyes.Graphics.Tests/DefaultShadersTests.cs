using KeenEyes.Graphics.Silk.Shaders;

namespace KeenEyes.Graphics.Tests;

/// <summary>
/// Tests for <see cref="DefaultShaders"/> string constants.
/// </summary>
public sealed class DefaultShadersTests
{
    #region Unlit Shaders

    [Fact]
    public void UnlitVertexShader_IsNotNullOrEmpty()
    {
        Assert.False(string.IsNullOrEmpty(DefaultShaders.UnlitVertexShader));
    }

    [Fact]
    public void UnlitVertexShader_ContainsVersion330()
    {
        Assert.Contains("#version 330", DefaultShaders.UnlitVertexShader);
    }

    [Fact]
    public void UnlitVertexShader_ContainsPositionAttribute()
    {
        Assert.Contains("aPosition", DefaultShaders.UnlitVertexShader);
    }

    [Fact]
    public void UnlitVertexShader_ContainsNormalAttribute()
    {
        Assert.Contains("aNormal", DefaultShaders.UnlitVertexShader);
    }

    [Fact]
    public void UnlitVertexShader_ContainsTexCoordAttribute()
    {
        Assert.Contains("aTexCoord", DefaultShaders.UnlitVertexShader);
    }

    [Fact]
    public void UnlitVertexShader_ContainsColorAttribute()
    {
        Assert.Contains("aColor", DefaultShaders.UnlitVertexShader);
    }

    [Fact]
    public void UnlitVertexShader_ContainsMVPUniforms()
    {
        Assert.Contains("uModel", DefaultShaders.UnlitVertexShader);
        Assert.Contains("uView", DefaultShaders.UnlitVertexShader);
        Assert.Contains("uProjection", DefaultShaders.UnlitVertexShader);
    }

    [Fact]
    public void UnlitFragmentShader_IsNotNullOrEmpty()
    {
        Assert.False(string.IsNullOrEmpty(DefaultShaders.UnlitFragmentShader));
    }

    [Fact]
    public void UnlitFragmentShader_ContainsVersion330()
    {
        Assert.Contains("#version 330", DefaultShaders.UnlitFragmentShader);
    }

    [Fact]
    public void UnlitFragmentShader_ContainsTextureUniform()
    {
        Assert.Contains("uTexture", DefaultShaders.UnlitFragmentShader);
    }

    [Fact]
    public void UnlitFragmentShader_ContainsColorUniform()
    {
        Assert.Contains("uColor", DefaultShaders.UnlitFragmentShader);
    }

    [Fact]
    public void UnlitFragmentShader_ContainsFragColorOutput()
    {
        Assert.Contains("FragColor", DefaultShaders.UnlitFragmentShader);
    }

    #endregion

    #region Lit Shaders

    [Fact]
    public void LitVertexShader_IsNotNullOrEmpty()
    {
        Assert.False(string.IsNullOrEmpty(DefaultShaders.LitVertexShader));
    }

    [Fact]
    public void LitVertexShader_ContainsVersion330()
    {
        Assert.Contains("#version 330", DefaultShaders.LitVertexShader);
    }

    [Fact]
    public void LitVertexShader_ContainsAllAttributes()
    {
        Assert.Contains("aPosition", DefaultShaders.LitVertexShader);
        Assert.Contains("aNormal", DefaultShaders.LitVertexShader);
        Assert.Contains("aTexCoord", DefaultShaders.LitVertexShader);
        Assert.Contains("aColor", DefaultShaders.LitVertexShader);
    }

    [Fact]
    public void LitVertexShader_OutputsWorldPosition()
    {
        Assert.Contains("vWorldPos", DefaultShaders.LitVertexShader);
    }

    [Fact]
    public void LitVertexShader_OutputsTransformedNormal()
    {
        Assert.Contains("vNormal", DefaultShaders.LitVertexShader);
    }

    [Fact]
    public void LitFragmentShader_IsNotNullOrEmpty()
    {
        Assert.False(string.IsNullOrEmpty(DefaultShaders.LitFragmentShader));
    }

    [Fact]
    public void LitFragmentShader_ContainsVersion330()
    {
        Assert.Contains("#version 330", DefaultShaders.LitFragmentShader);
    }

    [Fact]
    public void LitFragmentShader_ContainsCameraPositionUniform()
    {
        Assert.Contains("uCameraPosition", DefaultShaders.LitFragmentShader);
    }

    [Fact]
    public void LitFragmentShader_ContainsLightUniforms()
    {
        Assert.Contains("uLightDirection", DefaultShaders.LitFragmentShader);
        Assert.Contains("uLightColor", DefaultShaders.LitFragmentShader);
        Assert.Contains("uLightIntensity", DefaultShaders.LitFragmentShader);
    }

    [Fact]
    public void LitFragmentShader_ContainsEmissiveUniform()
    {
        Assert.Contains("uEmissive", DefaultShaders.LitFragmentShader);
    }

    [Fact]
    public void LitFragmentShader_ImplementsDiffuseLighting()
    {
        // Check for diffuse calculation
        Assert.Contains("diffuse", DefaultShaders.LitFragmentShader);
    }

    [Fact]
    public void LitFragmentShader_ImplementsSpecularLighting()
    {
        // Check for specular calculation (Blinn-Phong)
        Assert.Contains("specular", DefaultShaders.LitFragmentShader);
    }

    [Fact]
    public void LitFragmentShader_ImplementsAmbientLighting()
    {
        // Check for ambient calculation
        Assert.Contains("ambient", DefaultShaders.LitFragmentShader);
    }

    #endregion

    #region Solid Color Shaders

    [Fact]
    public void SolidVertexShader_IsNotNullOrEmpty()
    {
        Assert.False(string.IsNullOrEmpty(DefaultShaders.SolidVertexShader));
    }

    [Fact]
    public void SolidVertexShader_ContainsVersion330()
    {
        Assert.Contains("#version 330", DefaultShaders.SolidVertexShader);
    }

    [Fact]
    public void SolidVertexShader_ContainsMVPTransform()
    {
        Assert.Contains("uModel", DefaultShaders.SolidVertexShader);
        Assert.Contains("uView", DefaultShaders.SolidVertexShader);
        Assert.Contains("uProjection", DefaultShaders.SolidVertexShader);
    }

    [Fact]
    public void SolidVertexShader_OutputsColor()
    {
        Assert.Contains("vColor", DefaultShaders.SolidVertexShader);
    }

    [Fact]
    public void SolidFragmentShader_IsNotNullOrEmpty()
    {
        Assert.False(string.IsNullOrEmpty(DefaultShaders.SolidFragmentShader));
    }

    [Fact]
    public void SolidFragmentShader_ContainsVersion330()
    {
        Assert.Contains("#version 330", DefaultShaders.SolidFragmentShader);
    }

    [Fact]
    public void SolidFragmentShader_DoesNotUseTextures()
    {
        // Solid shader should not reference textures
        Assert.DoesNotContain("texture(", DefaultShaders.SolidFragmentShader);
        Assert.DoesNotContain("sampler2D", DefaultShaders.SolidFragmentShader);
    }

    [Fact]
    public void SolidFragmentShader_MultipliesColors()
    {
        Assert.Contains("vColor", DefaultShaders.SolidFragmentShader);
        Assert.Contains("uColor", DefaultShaders.SolidFragmentShader);
    }

    #endregion

    #region Shader Compatibility

    [Fact]
    public void AllShaders_UseConsistentAttributeLocations()
    {
        // Position at location 0
        Assert.Contains("layout (location = 0) in vec3 aPosition", DefaultShaders.UnlitVertexShader);
        Assert.Contains("layout (location = 0) in vec3 aPosition", DefaultShaders.LitVertexShader);
        Assert.Contains("layout (location = 0) in vec3 aPosition", DefaultShaders.SolidVertexShader);

        // Normal at location 1
        Assert.Contains("layout (location = 1) in vec3 aNormal", DefaultShaders.UnlitVertexShader);
        Assert.Contains("layout (location = 1) in vec3 aNormal", DefaultShaders.LitVertexShader);
        Assert.Contains("layout (location = 1) in vec3 aNormal", DefaultShaders.SolidVertexShader);

        // TexCoord at location 2
        Assert.Contains("layout (location = 2) in vec2 aTexCoord", DefaultShaders.UnlitVertexShader);
        Assert.Contains("layout (location = 2) in vec2 aTexCoord", DefaultShaders.LitVertexShader);
        Assert.Contains("layout (location = 2) in vec2 aTexCoord", DefaultShaders.SolidVertexShader);

        // Color at location 3
        Assert.Contains("layout (location = 3) in vec4 aColor", DefaultShaders.UnlitVertexShader);
        Assert.Contains("layout (location = 3) in vec4 aColor", DefaultShaders.LitVertexShader);
        Assert.Contains("layout (location = 3) in vec4 aColor", DefaultShaders.SolidVertexShader);
    }

    [Fact]
    public void AllFragmentShaders_OutputToFragColor()
    {
        Assert.Contains("out vec4 FragColor", DefaultShaders.UnlitFragmentShader);
        Assert.Contains("out vec4 FragColor", DefaultShaders.LitFragmentShader);
        Assert.Contains("out vec4 FragColor", DefaultShaders.SolidFragmentShader);
    }

    #endregion
}
