using KeenEyes.Graphics.Silk.Resources;
using KeenEyes.Graphics.Tests.Mocks;

namespace KeenEyes.Graphics.Tests;

/// <summary>
/// Tests for the TextureManager class.
/// </summary>
public class TextureManagerTests : IDisposable
{
    private readonly MockGraphicsDevice device;
    private readonly TextureManager manager;

    public TextureManagerTests()
    {
        device = new MockGraphicsDevice();
        manager = new TextureManager { Device = device };
    }

    public void Dispose()
    {
        manager.Dispose();
        device.Dispose();
    }

    #region CreateTexture Tests

    [Fact]
    public void CreateTexture_WithValidData_ReturnsPositiveId()
    {
        byte[] data = [255, 0, 0, 255]; // 1x1 red pixel

        int textureId = manager.CreateTexture(1, 1, data);

        Assert.True(textureId > 0);
    }

    [Fact]
    public void CreateTexture_GeneratesTexture()
    {
        byte[] data = [255, 0, 0, 255];

        manager.CreateTexture(1, 1, data);

        Assert.Single(device.GeneratedTextures);
    }

    [Fact]
    public void CreateTexture_BindsTexture()
    {
        byte[] data = [255, 0, 0, 255];

        manager.CreateTexture(1, 1, data);

        Assert.Contains(device.Calls, c => c.StartsWith("BindTexture(Texture2D,"));
    }

    [Fact]
    public void CreateTexture_UploadsTextureData()
    {
        byte[] data = [255, 0, 0, 255];

        manager.CreateTexture(1, 1, data);

        Assert.Contains(device.Calls, c => c.Contains("TexImage2D"));
    }

    [Fact]
    public void CreateTexture_SetsFilterParameters()
    {
        byte[] data = [255, 0, 0, 255];

        manager.CreateTexture(1, 1, data);

        var texParamCalls = device.Calls.Count(c => c.StartsWith("TexParameter"));
        Assert.True(texParamCalls >= 4); // Min, Mag, WrapS, WrapT
    }

    [Fact]
    public void CreateTexture_WithNearestFilter_SetsNearestFiltering()
    {
        byte[] data = [255, 0, 0, 255];

        manager.CreateTexture(1, 1, data, TextureFilter.Nearest);

        // Verify MinFilter and MagFilter params are set
        Assert.Contains(device.Calls, c => c.Contains("MinFilter"));
        Assert.Contains(device.Calls, c => c.Contains("MagFilter"));
    }

    [Fact]
    public void CreateTexture_WithLinearFilter_SetsLinearFiltering()
    {
        byte[] data = [255, 0, 0, 255];

        manager.CreateTexture(1, 1, data, TextureFilter.Linear);

        // Verify MinFilter and MagFilter params are set
        Assert.Contains(device.Calls, c => c.Contains("MinFilter"));
        Assert.Contains(device.Calls, c => c.Contains("MagFilter"));
    }

    [Fact]
    public void CreateTexture_WithTrilinearFilter_GeneratesMipmaps()
    {
        byte[] data = [255, 0, 0, 255];

        manager.CreateTexture(1, 1, data, TextureFilter.Trilinear);

        Assert.Contains(device.Calls, c => c.StartsWith("GenerateMipmap"));
    }

    [Fact]
    public void CreateTexture_WithRepeatWrap_SetsRepeatWrapping()
    {
        byte[] data = [255, 0, 0, 255];

        manager.CreateTexture(1, 1, data, wrap: TextureWrap.Repeat);

        // Verify WrapS and WrapT params are set
        Assert.Contains(device.Calls, c => c.Contains("WrapS"));
        Assert.Contains(device.Calls, c => c.Contains("WrapT"));
    }

    [Fact]
    public void CreateTexture_WithClampToEdge_SetsClampWrapping()
    {
        byte[] data = [255, 0, 0, 255];

        manager.CreateTexture(1, 1, data, wrap: TextureWrap.ClampToEdge);

        // Verify WrapS and WrapT params are set
        Assert.Contains(device.Calls, c => c.Contains("WrapS"));
        Assert.Contains(device.Calls, c => c.Contains("WrapT"));
    }

    [Fact]
    public void CreateTexture_UnbindsTextureAfter()
    {
        byte[] data = [255, 0, 0, 255];

        manager.CreateTexture(1, 1, data);

        Assert.Contains(device.Calls, c => c == "BindTexture(Texture2D, 0)");
    }

    [Fact]
    public void CreateTexture_WithoutDevice_ThrowsInvalidOperationException()
    {
        var managerWithoutDevice = new TextureManager();
        byte[] data = [255, 0, 0, 255];

        Assert.Throws<InvalidOperationException>(() =>
            managerWithoutDevice.CreateTexture(1, 1, data));
    }

    [Fact]
    public void CreateTexture_MultipleCalls_ReturnsUniqueIds()
    {
        byte[] data = [255, 0, 0, 255];

        int id1 = manager.CreateTexture(1, 1, data);
        int id2 = manager.CreateTexture(1, 1, data);

        Assert.NotEqual(id1, id2);
    }

    #endregion

    #region CreateSolidColorTexture Tests

    [Fact]
    public void CreateSolidColorTexture_ReturnsPositiveId()
    {
        int textureId = manager.CreateSolidColorTexture(255, 0, 0);

        Assert.True(textureId > 0);
    }

    [Fact]
    public void CreateSolidColorTexture_Creates1x1Texture()
    {
        manager.CreateSolidColorTexture(255, 0, 0);

        Assert.Contains(device.Calls, c => c.Contains("TexImage2D") && c.Contains("1x1"));
    }

    [Fact]
    public void CreateSolidColorTexture_UsesNearestFilter()
    {
        manager.CreateSolidColorTexture(255, 0, 0);

        // Solid color textures should use nearest filtering
        Assert.Contains(device.Calls, c => c.Contains("MinFilter"));
        Assert.Contains(device.Calls, c => c.Contains("MagFilter"));
    }

    [Fact]
    public void CreateSolidColorTexture_WithAlpha_UsesAlphaValue()
    {
        int textureId = manager.CreateSolidColorTexture(255, 0, 0, 128);
        var textureData = manager.GetTexture(textureId);

        Assert.NotNull(textureData);
        Assert.True(textureData.HasAlpha);
    }

    #endregion

    #region GetTexture Tests

    [Fact]
    public void GetTexture_WithValidId_ReturnsTextureData()
    {
        byte[] data = [255, 0, 0, 255];

        int textureId = manager.CreateTexture(1, 1, data);
        var textureData = manager.GetTexture(textureId);

        Assert.NotNull(textureData);
    }

    [Fact]
    public void GetTexture_ReturnsCorrectDimensions()
    {
        byte[] data = new byte[4 * 4 * 4]; // 4x4 RGBA

        int textureId = manager.CreateTexture(4, 4, data);
        var textureData = manager.GetTexture(textureId);

        Assert.NotNull(textureData);
        Assert.Equal(4, textureData.Width);
        Assert.Equal(4, textureData.Height);
    }

    [Fact]
    public void GetTexture_WithInvalidId_ReturnsNull()
    {
        var textureData = manager.GetTexture(999);

        Assert.Null(textureData);
    }

    [Fact]
    public void GetTexture_WithZeroId_ReturnsNull()
    {
        var textureData = manager.GetTexture(0);

        Assert.Null(textureData);
    }

    #endregion

    #region DeleteTexture Tests

    [Fact]
    public void DeleteTexture_WithValidId_ReturnsTrue()
    {
        byte[] data = [255, 0, 0, 255];

        int textureId = manager.CreateTexture(1, 1, data);
        bool deleted = manager.DeleteTexture(textureId);

        Assert.True(deleted);
    }

    [Fact]
    public void DeleteTexture_WithInvalidId_ReturnsFalse()
    {
        bool deleted = manager.DeleteTexture(999);

        Assert.False(deleted);
    }

    [Fact]
    public void DeleteTexture_DeletesGPUTexture()
    {
        byte[] data = [255, 0, 0, 255];

        int textureId = manager.CreateTexture(1, 1, data);
        manager.DeleteTexture(textureId);

        Assert.Single(device.DeletedTextures);
    }

    [Fact]
    public void DeleteTexture_MakesTextureUnavailable()
    {
        byte[] data = [255, 0, 0, 255];

        int textureId = manager.CreateTexture(1, 1, data);
        manager.DeleteTexture(textureId);

        Assert.Null(manager.GetTexture(textureId));
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_DeletesAllTextures()
    {
        byte[] data = [255, 0, 0, 255];

        using var testManager = new TextureManager { Device = device };
        testManager.CreateTexture(1, 1, data);
        testManager.CreateTexture(1, 1, data);
        device.Reset();

        using var testManager2 = new TextureManager { Device = device };
        testManager2.CreateTexture(1, 1, data);
        testManager2.CreateTexture(1, 1, data);
        testManager2.Dispose();

        Assert.Equal(2, device.DeletedTextures.Count);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        byte[] data = [255, 0, 0, 255];

        using var testManager = new TextureManager { Device = device };
        testManager.CreateTexture(1, 1, data);

        // Should not throw
        testManager.Dispose();
        testManager.Dispose();
    }

    #endregion
}
