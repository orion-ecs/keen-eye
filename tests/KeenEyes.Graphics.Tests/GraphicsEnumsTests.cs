using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Graphics.Tests;

/// <summary>
/// Tests to verify that enum values match their OpenGL constant equivalents.
/// </summary>
/// <remarks>
/// These tests are critical because some enums are cast to int and passed directly
/// to OpenGL without conversion. If the values don't match OpenGL constants,
/// textures may not render correctly (e.g., black textures, missing filtering).
/// </remarks>
public class GraphicsEnumsTests
{
    #region TextureMinFilter Tests

    [Fact]
    public void TextureMinFilter_Nearest_MatchesOpenGLConstant()
    {
        // GL_NEAREST = 0x2600
        Assert.Equal(0x2600, (int)TextureMinFilter.Nearest);
    }

    [Fact]
    public void TextureMinFilter_Linear_MatchesOpenGLConstant()
    {
        // GL_LINEAR = 0x2601
        Assert.Equal(0x2601, (int)TextureMinFilter.Linear);
    }

    [Fact]
    public void TextureMinFilter_NearestMipmapNearest_MatchesOpenGLConstant()
    {
        // GL_NEAREST_MIPMAP_NEAREST = 0x2700
        Assert.Equal(0x2700, (int)TextureMinFilter.NearestMipmapNearest);
    }

    [Fact]
    public void TextureMinFilter_LinearMipmapNearest_MatchesOpenGLConstant()
    {
        // GL_LINEAR_MIPMAP_NEAREST = 0x2701
        Assert.Equal(0x2701, (int)TextureMinFilter.LinearMipmapNearest);
    }

    [Fact]
    public void TextureMinFilter_NearestMipmapLinear_MatchesOpenGLConstant()
    {
        // GL_NEAREST_MIPMAP_LINEAR = 0x2702
        Assert.Equal(0x2702, (int)TextureMinFilter.NearestMipmapLinear);
    }

    [Fact]
    public void TextureMinFilter_LinearMipmapLinear_MatchesOpenGLConstant()
    {
        // GL_LINEAR_MIPMAP_LINEAR = 0x2703
        Assert.Equal(0x2703, (int)TextureMinFilter.LinearMipmapLinear);
    }

    #endregion

    #region TextureMagFilter Tests

    [Fact]
    public void TextureMagFilter_Nearest_MatchesOpenGLConstant()
    {
        // GL_NEAREST = 0x2600
        Assert.Equal(0x2600, (int)TextureMagFilter.Nearest);
    }

    [Fact]
    public void TextureMagFilter_Linear_MatchesOpenGLConstant()
    {
        // GL_LINEAR = 0x2601
        Assert.Equal(0x2601, (int)TextureMagFilter.Linear);
    }

    #endregion

    #region TextureWrapMode Tests

    [Fact]
    public void TextureWrapMode_Repeat_MatchesOpenGLConstant()
    {
        // GL_REPEAT = 0x2901
        Assert.Equal(0x2901, (int)TextureWrapMode.Repeat);
    }

    [Fact]
    public void TextureWrapMode_MirroredRepeat_MatchesOpenGLConstant()
    {
        // GL_MIRRORED_REPEAT = 0x8370
        Assert.Equal(0x8370, (int)TextureWrapMode.MirroredRepeat);
    }

    [Fact]
    public void TextureWrapMode_ClampToEdge_MatchesOpenGLConstant()
    {
        // GL_CLAMP_TO_EDGE = 0x812F
        Assert.Equal(0x812F, (int)TextureWrapMode.ClampToEdge);
    }

    [Fact]
    public void TextureWrapMode_ClampToBorder_MatchesOpenGLConstant()
    {
        // GL_CLAMP_TO_BORDER = 0x812D
        Assert.Equal(0x812D, (int)TextureWrapMode.ClampToBorder);
    }

    #endregion
}
