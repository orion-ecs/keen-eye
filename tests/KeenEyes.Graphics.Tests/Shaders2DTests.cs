using KeenEyes.Graphics.Silk.Rendering2D;

namespace KeenEyes.Graphics.Tests;

/// <summary>
/// Tests for <see cref="Shaders2D"/> string constants.
/// </summary>
public sealed class Shaders2DTests
{
    #region Vertex Shader

    [Fact]
    public void VertexShader_IsNotNullOrEmpty()
    {
        Assert.False(string.IsNullOrEmpty(Shaders2D.VertexShader));
    }

    [Fact]
    public void VertexShader_ContainsVersion330()
    {
        Assert.Contains("#version 330", Shaders2D.VertexShader);
    }

    [Fact]
    public void VertexShader_ContainsPositionAttribute()
    {
        Assert.Contains("layout (location = 0) in vec2 aPosition", Shaders2D.VertexShader);
    }

    [Fact]
    public void VertexShader_ContainsTexCoordAttribute()
    {
        Assert.Contains("layout (location = 1) in vec2 aTexCoord", Shaders2D.VertexShader);
    }

    [Fact]
    public void VertexShader_ContainsColorAttribute()
    {
        Assert.Contains("layout (location = 2) in vec4 aColor", Shaders2D.VertexShader);
    }

    [Fact]
    public void VertexShader_ContainsProjectionUniform()
    {
        Assert.Contains("uniform mat4 uProjection", Shaders2D.VertexShader);
    }

    [Fact]
    public void VertexShader_OutputsTexCoord()
    {
        Assert.Contains("out vec2 vTexCoord", Shaders2D.VertexShader);
    }

    [Fact]
    public void VertexShader_OutputsColor()
    {
        Assert.Contains("out vec4 vColor", Shaders2D.VertexShader);
    }

    [Fact]
    public void VertexShader_ContainsMainFunction()
    {
        Assert.Contains("void main()", Shaders2D.VertexShader);
    }

    [Fact]
    public void VertexShader_AppliesProjection()
    {
        Assert.Contains("gl_Position", Shaders2D.VertexShader);
        Assert.Contains("uProjection", Shaders2D.VertexShader);
        Assert.Contains("vec4(aPosition, 0.0, 1.0)", Shaders2D.VertexShader);
    }

    [Fact]
    public void VertexShader_PassesThroughTexCoord()
    {
        Assert.Contains("vTexCoord = aTexCoord", Shaders2D.VertexShader);
    }

    [Fact]
    public void VertexShader_PassesThroughColor()
    {
        Assert.Contains("vColor = aColor", Shaders2D.VertexShader);
    }

    #endregion

    #region Fragment Shader

    [Fact]
    public void FragmentShader_IsNotNullOrEmpty()
    {
        Assert.False(string.IsNullOrEmpty(Shaders2D.FragmentShader));
    }

    [Fact]
    public void FragmentShader_ContainsVersion330()
    {
        Assert.Contains("#version 330", Shaders2D.FragmentShader);
    }

    [Fact]
    public void FragmentShader_InputsTexCoord()
    {
        Assert.Contains("in vec2 vTexCoord", Shaders2D.FragmentShader);
    }

    [Fact]
    public void FragmentShader_InputsColor()
    {
        Assert.Contains("in vec4 vColor", Shaders2D.FragmentShader);
    }

    [Fact]
    public void FragmentShader_ContainsTextureUniform()
    {
        Assert.Contains("uniform sampler2D uTexture", Shaders2D.FragmentShader);
    }

    [Fact]
    public void FragmentShader_OutputsFragColor()
    {
        Assert.Contains("out vec4 FragColor", Shaders2D.FragmentShader);
    }

    [Fact]
    public void FragmentShader_ContainsMainFunction()
    {
        Assert.Contains("void main()", Shaders2D.FragmentShader);
    }

    [Fact]
    public void FragmentShader_SamplesTexture()
    {
        Assert.Contains("texture(uTexture, vTexCoord)", Shaders2D.FragmentShader);
    }

    [Fact]
    public void FragmentShader_MultipliesTextureAndColor()
    {
        Assert.Contains("texColor * vColor", Shaders2D.FragmentShader);
    }

    #endregion

    #region Shader Compatibility

    [Fact]
    public void VertexAndFragmentShaders_HaveMatchingVaryings()
    {
        // vTexCoord should be output from vertex and input to fragment
        Assert.Contains("out vec2 vTexCoord", Shaders2D.VertexShader);
        Assert.Contains("in vec2 vTexCoord", Shaders2D.FragmentShader);

        // vColor should be output from vertex and input to fragment
        Assert.Contains("out vec4 vColor", Shaders2D.VertexShader);
        Assert.Contains("in vec4 vColor", Shaders2D.FragmentShader);
    }

    [Fact]
    public void Shaders_UseDifferentPositionTypes()
    {
        // 2D shaders use vec2 for positions (not vec3 like 3D shaders)
        Assert.Contains("vec2 aPosition", Shaders2D.VertexShader);
    }

    [Fact]
    public void Shaders_UseZeroForDepth()
    {
        // 2D shaders should set Z to 0.0 for flat rendering
        Assert.Contains("0.0", Shaders2D.VertexShader);
    }

    #endregion

    #region Attribute Layout

    [Fact]
    public void VertexShader_UsesCorrectAttributeLocations()
    {
        // Location 0: Position (vec2)
        Assert.Contains("layout (location = 0) in vec2 aPosition", Shaders2D.VertexShader);

        // Location 1: TexCoord (vec2)
        Assert.Contains("layout (location = 1) in vec2 aTexCoord", Shaders2D.VertexShader);

        // Location 2: Color (vec4)
        Assert.Contains("layout (location = 2) in vec4 aColor", Shaders2D.VertexShader);
    }

    #endregion
}
