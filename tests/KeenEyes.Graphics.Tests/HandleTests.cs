using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Graphics.Tests;

/// <summary>
/// Tests for graphics resource handle types.
/// </summary>
public class HandleTests
{
    #region FontHandle Tests

    [Fact]
    public void FontHandle_WithPositiveId_IsValid()
    {
        var handle = new FontHandle(0);
        Assert.True(handle.IsValid);

        handle = new FontHandle(42);
        Assert.True(handle.IsValid);
    }

    [Fact]
    public void FontHandle_WithNegativeId_IsInvalid()
    {
        var handle = new FontHandle(-1);
        Assert.False(handle.IsValid);

        handle = new FontHandle(-100);
        Assert.False(handle.IsValid);
    }

    [Fact]
    public void FontHandle_Invalid_HasNegativeOneId()
    {
        Assert.Equal(-1, FontHandle.Invalid.Id);
        Assert.False(FontHandle.Invalid.IsValid);
    }

    [Fact]
    public void FontHandle_ToString_ValidHandle_ShowsId()
    {
        var handle = new FontHandle(42);
        Assert.Equal("Font(42)", handle.ToString());
    }

    [Fact]
    public void FontHandle_ToString_InvalidHandle_ShowsInvalid()
    {
        Assert.Equal("Font(Invalid)", FontHandle.Invalid.ToString());
    }

    [Fact]
    public void FontHandle_Equality_SameId_AreEqual()
    {
        var handle1 = new FontHandle(5);
        var handle2 = new FontHandle(5);
        Assert.Equal(handle1, handle2);
    }

    [Fact]
    public void FontHandle_Equality_DifferentId_AreNotEqual()
    {
        var handle1 = new FontHandle(5);
        var handle2 = new FontHandle(6);
        Assert.NotEqual(handle1, handle2);
    }

    #endregion

    #region TextureHandle Tests

    [Fact]
    public void TextureHandle_WithPositiveId_IsValid()
    {
        var handle = new TextureHandle(0);
        Assert.True(handle.IsValid);

        handle = new TextureHandle(42);
        Assert.True(handle.IsValid);
    }

    [Fact]
    public void TextureHandle_WithNegativeId_IsInvalid()
    {
        var handle = new TextureHandle(-1);
        Assert.False(handle.IsValid);
    }

    [Fact]
    public void TextureHandle_Invalid_HasNegativeOneId()
    {
        Assert.Equal(-1, TextureHandle.Invalid.Id);
        Assert.False(TextureHandle.Invalid.IsValid);
    }

    [Fact]
    public void TextureHandle_ToString_ValidHandle_ShowsId()
    {
        var handle = new TextureHandle(42);
        Assert.Equal("Texture(42, 0x0)", handle.ToString());
    }

    [Fact]
    public void TextureHandle_ToString_ValidHandleWithDimensions_ShowsDimensions()
    {
        var handle = new TextureHandle(42, 128, 256);
        Assert.Equal("Texture(42, 128x256)", handle.ToString());
    }

    [Fact]
    public void TextureHandle_ToString_InvalidHandle_ShowsInvalid()
    {
        Assert.Equal("Texture(Invalid)", TextureHandle.Invalid.ToString());
    }

    #endregion

    #region MeshHandle Tests

    [Fact]
    public void MeshHandle_WithPositiveId_IsValid()
    {
        var handle = new MeshHandle(0);
        Assert.True(handle.IsValid);

        handle = new MeshHandle(42);
        Assert.True(handle.IsValid);
    }

    [Fact]
    public void MeshHandle_WithNegativeId_IsInvalid()
    {
        var handle = new MeshHandle(-1);
        Assert.False(handle.IsValid);
    }

    [Fact]
    public void MeshHandle_Invalid_HasNegativeOneId()
    {
        Assert.Equal(-1, MeshHandle.Invalid.Id);
        Assert.False(MeshHandle.Invalid.IsValid);
    }

    [Fact]
    public void MeshHandle_ToString_ValidHandle_ShowsId()
    {
        var handle = new MeshHandle(42);
        Assert.Equal("Mesh(42)", handle.ToString());
    }

    [Fact]
    public void MeshHandle_ToString_InvalidHandle_ShowsInvalid()
    {
        Assert.Equal("Mesh(Invalid)", MeshHandle.Invalid.ToString());
    }

    #endregion

    #region ShaderHandle Tests

    [Fact]
    public void ShaderHandle_WithPositiveId_IsValid()
    {
        var handle = new ShaderHandle(0);
        Assert.True(handle.IsValid);

        handle = new ShaderHandle(42);
        Assert.True(handle.IsValid);
    }

    [Fact]
    public void ShaderHandle_WithNegativeId_IsInvalid()
    {
        var handle = new ShaderHandle(-1);
        Assert.False(handle.IsValid);
    }

    [Fact]
    public void ShaderHandle_Invalid_HasNegativeOneId()
    {
        Assert.Equal(-1, ShaderHandle.Invalid.Id);
        Assert.False(ShaderHandle.Invalid.IsValid);
    }

    [Fact]
    public void ShaderHandle_ToString_ValidHandle_ShowsId()
    {
        var handle = new ShaderHandle(42);
        Assert.Equal("Shader(42)", handle.ToString());
    }

    [Fact]
    public void ShaderHandle_ToString_InvalidHandle_ShowsInvalid()
    {
        Assert.Equal("Shader(Invalid)", ShaderHandle.Invalid.ToString());
    }

    #endregion
}
