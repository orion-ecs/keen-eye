using System.Numerics;

using KeenEyes.Graphics.Silk.Resources;

namespace KeenEyes.Graphics.Tests;

/// <summary>
/// Tests for <see cref="Vertex"/> struct.
/// </summary>
public sealed class VertexTests
{
    #region Constructor

    [Fact]
    public void Constructor_WithAllParameters_SetsProperties()
    {
        var position = new Vector3(1f, 2f, 3f);
        var normal = new Vector3(0f, 1f, 0f);
        var texCoord = new Vector2(0.5f, 0.5f);
        var color = new Vector4(1f, 0f, 0f, 1f);

        var vertex = new Vertex(position, normal, texCoord, color);

        Assert.Equal(position, vertex.Position);
        Assert.Equal(normal, vertex.Normal);
        Assert.Equal(texCoord, vertex.TexCoord);
        Assert.Equal(color, vertex.Color);
    }

    [Fact]
    public void Constructor_WithoutColor_UsesWhiteDefault()
    {
        var position = new Vector3(1f, 2f, 3f);
        var normal = new Vector3(0f, 1f, 0f);
        var texCoord = new Vector2(0.5f, 0.5f);

        var vertex = new Vertex(position, normal, texCoord);

        Assert.Equal(Vector4.One, vertex.Color);
    }

    [Fact]
    public void Constructor_WithDefaultColor_UsesWhiteDefault()
    {
        var position = new Vector3(1f, 2f, 3f);
        var normal = new Vector3(0f, 1f, 0f);
        var texCoord = new Vector2(0.5f, 0.5f);

        var vertex = new Vertex(position, normal, texCoord, default);

        Assert.Equal(Vector4.One, vertex.Color);
    }

    #endregion

    #region Size

    [Fact]
    public void SizeInBytes_ReturnsCorrectSize()
    {
        // Vertex has: Vector3 Position (12 bytes) + Vector3 Normal (12 bytes) +
        // Vector2 TexCoord (8 bytes) + Vector4 Color (16 bytes) = 48 bytes
        Assert.Equal(48, Vertex.SizeInBytes);
    }

    [Fact]
    public void SizeInBytes_Matches12Floats()
    {
        Assert.Equal(sizeof(float) * 12, Vertex.SizeInBytes);
    }

    #endregion

    #region Field Mutation

    [Fact]
    public void Position_CanBeMutated()
    {
        var vertex = new Vertex(Vector3.Zero, Vector3.UnitY, Vector2.Zero);
        var newPosition = new Vector3(10f, 20f, 30f);

        vertex.Position = newPosition;

        Assert.Equal(newPosition, vertex.Position);
    }

    [Fact]
    public void Normal_CanBeMutated()
    {
        var vertex = new Vertex(Vector3.Zero, Vector3.UnitY, Vector2.Zero);
        var newNormal = new Vector3(1f, 0f, 0f);

        vertex.Normal = newNormal;

        Assert.Equal(newNormal, vertex.Normal);
    }

    [Fact]
    public void TexCoord_CanBeMutated()
    {
        var vertex = new Vertex(Vector3.Zero, Vector3.UnitY, Vector2.Zero);
        var newTexCoord = new Vector2(1f, 1f);

        vertex.TexCoord = newTexCoord;

        Assert.Equal(newTexCoord, vertex.TexCoord);
    }

    [Fact]
    public void Color_CanBeMutated()
    {
        var vertex = new Vertex(Vector3.Zero, Vector3.UnitY, Vector2.Zero);
        var newColor = new Vector4(0f, 1f, 0f, 0.5f);

        vertex.Color = newColor;

        Assert.Equal(newColor, vertex.Color);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Constructor_WithZeroVectors_Works()
    {
        var vertex = new Vertex(Vector3.Zero, Vector3.Zero, Vector2.Zero, Vector4.Zero);

        Assert.Equal(Vector3.Zero, vertex.Position);
        Assert.Equal(Vector3.Zero, vertex.Normal);
        Assert.Equal(Vector2.Zero, vertex.TexCoord);
        // Color defaults to white when zero is passed (see Vertex constructor)
        Assert.Equal(Vector4.One, vertex.Color);
    }

    [Fact]
    public void Constructor_WithNegativeValues_Works()
    {
        var vertex = new Vertex(
            new Vector3(-1f, -2f, -3f),
            new Vector3(-1f, 0f, 0f),
            new Vector2(-0.5f, -0.5f),
            new Vector4(-1f, -1f, -1f, -1f));

        Assert.Equal(new Vector3(-1f, -2f, -3f), vertex.Position);
        Assert.Equal(new Vector3(-1f, 0f, 0f), vertex.Normal);
        Assert.Equal(new Vector2(-0.5f, -0.5f), vertex.TexCoord);
        Assert.Equal(new Vector4(-1f, -1f, -1f, -1f), vertex.Color);
    }

    [Fact]
    public void Constructor_WithExtremeValues_Works()
    {
        var vertex = new Vertex(
            new Vector3(float.MaxValue, float.MinValue, 0f),
            Vector3.UnitY,
            new Vector2(float.MaxValue, float.MinValue),
            new Vector4(float.MaxValue, 0f, 0f, 1f));

        Assert.Equal(float.MaxValue, vertex.Position.X);
        Assert.Equal(float.MinValue, vertex.Position.Y);
        Assert.Equal(float.MaxValue, vertex.TexCoord.X);
        Assert.Equal(float.MinValue, vertex.TexCoord.Y);
    }

    #endregion

    #region Common Vertex Patterns

    [Fact]
    public void Constructor_ForQuadVertex_TopLeft()
    {
        var vertex = new Vertex(
            new Vector3(-0.5f, 0.5f, 0f),
            Vector3.UnitZ,
            new Vector2(0f, 1f));

        Assert.Equal(new Vector3(-0.5f, 0.5f, 0f), vertex.Position);
        Assert.Equal(Vector3.UnitZ, vertex.Normal);
        Assert.Equal(new Vector2(0f, 1f), vertex.TexCoord);
        Assert.Equal(Vector4.One, vertex.Color);
    }

    [Fact]
    public void Constructor_ForCubeVertex_WithNormal()
    {
        var vertex = new Vertex(
            new Vector3(0.5f, -0.5f, 0.5f),
            new Vector3(0f, 0f, 1f),
            new Vector2(1f, 0f),
            Vector4.One);

        Assert.Equal(new Vector3(0.5f, -0.5f, 0.5f), vertex.Position);
        Assert.Equal(new Vector3(0f, 0f, 1f), vertex.Normal);
    }

    #endregion
}
