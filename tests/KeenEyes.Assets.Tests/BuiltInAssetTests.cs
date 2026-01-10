using System.Numerics;
using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Assets.Tests;

/// <summary>
/// Tests for built-in asset types.
/// </summary>
public class BuiltInAssetTests
{
    #region RawAsset Tests

    [Fact]
    public void RawAsset_StoresData()
    {
        var data = new byte[] { 1, 2, 3, 4, 5 };
        using var asset = new RawAsset(data);

        Assert.Equal(data, asset.Data);
    }

    [Fact]
    public void RawAsset_SizeBytes_ReturnsCorrectValue()
    {
        var data = new byte[] { 1, 2, 3, 4, 5 };
        using var asset = new RawAsset(data);

        Assert.Equal(5, asset.SizeBytes);
    }

    [Fact]
    public void RawAsset_AsSpan_ReturnsSpan()
    {
        var data = new byte[] { 1, 2, 3 };
        using var asset = new RawAsset(data);

        var span = asset.AsSpan();

        Assert.Equal(3, span.Length);
        Assert.Equal(1, span[0]);
        Assert.Equal(2, span[1]);
        Assert.Equal(3, span[2]);
    }

    [Fact]
    public void RawAsset_AsMemory_ReturnsMemory()
    {
        var data = new byte[] { 1, 2, 3 };
        using var asset = new RawAsset(data);

        var memory = asset.AsMemory();

        Assert.Equal(3, memory.Length);
    }

    [Fact]
    public void RawAsset_CreateStream_ReturnsReadableStream()
    {
        var data = new byte[] { 1, 2, 3 };
        using var asset = new RawAsset(data);

        using var stream = asset.CreateStream();

        Assert.Equal(3, stream.Length);
        Assert.Equal(1, stream.ReadByte());
    }

    [Fact]
    public void RawAsset_Dispose_IsIdempotent()
    {
        var asset = new RawAsset([1, 2, 3]);
        asset.Dispose();
        asset.Dispose(); // Should not throw
    }

    #endregion

    #region TextureAsset Tests

    [Fact]
    public void TextureAsset_StoresProperties()
    {
        var handle = new TextureHandle(42);
        var mockGraphics = new MockGraphicsContext();

        using var asset = new TextureAsset(handle, 256, 128, TextureFormat.Rgba8, mockGraphics);

        Assert.Equal(handle, asset.Handle);
        Assert.Equal(256, asset.Width);
        Assert.Equal(128, asset.Height);
        Assert.Equal(TextureFormat.Rgba8, asset.Format);
    }

    [Fact]
    public void TextureAsset_SizeBytes_CalculatesCorrectly()
    {
        var handle = new TextureHandle(42);
        var mockGraphics = new MockGraphicsContext();

        using var asset = new TextureAsset(handle, 256, 128, TextureFormat.Rgba8, mockGraphics);

        // 256 * 128 * 4 bytes per pixel (RGBA8)
        Assert.Equal(256 * 128 * 4, asset.SizeBytes);
    }

    [Fact]
    public void TextureAsset_Dispose_DisposesHandle()
    {
        var handle = new TextureHandle(42);
        var mockGraphics = new MockGraphicsContext();

        var asset = new TextureAsset(handle, 256, 128, TextureFormat.Rgba8, mockGraphics);
        asset.Dispose();

        Assert.Contains(handle, mockGraphics.DisposedTextures);
    }

    [Fact]
    public void TextureAsset_Dispose_IsIdempotent()
    {
        var handle = new TextureHandle(42);
        var mockGraphics = new MockGraphicsContext();

        var asset = new TextureAsset(handle, 256, 128, TextureFormat.Rgba8, mockGraphics);
        asset.Dispose();
        asset.Dispose(); // Should not throw
    }

    [Fact]
    public void TextureAsset_WithNullGraphics_DisposeSafely()
    {
        var handle = new TextureHandle(42);

        var asset = new TextureAsset(handle, 256, 128, TextureFormat.Rgba8, null);
        asset.Dispose(); // Should not throw
    }

    #endregion

    #region MeshAsset Tests

    [Fact]
    public void MeshAsset_StoresVerticesAndIndices()
    {
        var vertices = new MeshVertex[]
        {
            MeshVertex.CreateBasic(new Vector3(0, 0, 0), Vector3.UnitY, Vector2.Zero),
            MeshVertex.CreateBasic(new Vector3(1, 1, 1), Vector3.UnitY, Vector2.One)
        };
        var indices = new uint[] { 0, 1, 0 };
        var submeshes = new[] { new Submesh(0, 3, -1) };

        using var asset = new MeshAsset("test", vertices, indices, submeshes, Vector3.Zero, Vector3.One);

        Assert.Equal("test", asset.Name);
        Assert.Equal(vertices, asset.Vertices);
        Assert.Equal(indices, asset.Indices);
        Assert.Equal(submeshes, asset.Submeshes);
        Assert.Equal(Vector3.Zero, asset.BoundsMin);
        Assert.Equal(Vector3.One, asset.BoundsMax);
    }

    [Fact]
    public void MeshAsset_Create_ComputesBounds()
    {
        var vertices = new MeshVertex[]
        {
            MeshVertex.CreateBasic(new Vector3(-1, 0, 0), Vector3.UnitY, Vector2.Zero),
            MeshVertex.CreateBasic(new Vector3(1, 2, 3), Vector3.UnitY, Vector2.One)
        };
        var indices = new uint[] { 0, 1 };

        using var asset = MeshAsset.Create("test", vertices, indices);

        Assert.Equal(new Vector3(-1, 0, 0), asset.BoundsMin);
        Assert.Equal(new Vector3(1, 2, 3), asset.BoundsMax);
    }

    [Fact]
    public void MeshAsset_SizeBytes_CalculatesCorrectly()
    {
        var vertices = new MeshVertex[]
        {
            MeshVertex.CreateBasic(Vector3.Zero, Vector3.UnitY, Vector2.Zero),
            MeshVertex.CreateBasic(Vector3.One, Vector3.UnitY, Vector2.One)
        };
        var indices = new uint[] { 0, 1, 0 };
        var submeshes = new[] { new Submesh(0, 3, -1) };

        using var asset = new MeshAsset("test", vertices, indices, submeshes, Vector3.Zero, Vector3.One);

        // Each vertex: (3+3+2+4+4+4) floats = 20 floats * 4 bytes = 80 bytes
        // Plus joints: 4 ushorts * 2 bytes = 8 bytes per vertex
        // 2 vertices = (80 + 8) * 2 = 176 bytes
        // 3 indices * 4 bytes = 12 bytes
        // 1 submesh * 3 ints * 4 bytes = 12 bytes
        // Total = 176 + 12 + 12 = 200 bytes
        Assert.Equal(200, asset.SizeBytes);
    }

    [Fact]
    public void MeshAsset_Dispose_IsIdempotent()
    {
        var asset = new MeshAsset("test", [], [], [], Vector3.Zero, Vector3.Zero);
        asset.Dispose();
        asset.Dispose(); // Should not throw
    }

    #endregion

    #region CacheStats Tests

    [Fact]
    public void CacheStats_HitRatio_CalculatesCorrectly()
    {
        var stats = new CacheStats(
            TotalAssets: 10,
            LoadedAssets: 8,
            PendingAssets: 1,
            FailedAssets: 1,
            TotalSizeBytes: 1000,
            MaxSizeBytes: 2000,
            CacheHits: 75,
            CacheMisses: 25
        );

        Assert.Equal(0.75, stats.HitRatio);
    }

    [Fact]
    public void CacheStats_HitRatio_WithZeroTotal_ReturnsZero()
    {
        var stats = new CacheStats(
            TotalAssets: 0,
            LoadedAssets: 0,
            PendingAssets: 0,
            FailedAssets: 0,
            TotalSizeBytes: 0,
            MaxSizeBytes: 1000,
            CacheHits: 0,
            CacheMisses: 0
        );

        Assert.Equal(0.0, stats.HitRatio);
    }

    #endregion

    #region LoadPriority Tests

    [Fact]
    public void LoadPriority_Values_AreInOrder()
    {
        Assert.True(LoadPriority.Immediate < LoadPriority.High);
        Assert.True(LoadPriority.High < LoadPriority.Normal);
        Assert.True(LoadPriority.Normal < LoadPriority.Low);
        Assert.True(LoadPriority.Low < LoadPriority.Streaming);
    }

    #endregion

    #region AssetState Tests

    [Fact]
    public void AssetState_HasExpectedValues()
    {
        Assert.Equal(0, (int)AssetState.Invalid);
        Assert.Equal(1, (int)AssetState.Pending);
        Assert.Equal(2, (int)AssetState.Loading);
        Assert.Equal(3, (int)AssetState.Loaded);
        Assert.Equal(4, (int)AssetState.Failed);
        Assert.Equal(5, (int)AssetState.Unloaded);
    }

    #endregion
}

/// <summary>
/// Mock graphics context for testing.
/// </summary>
file sealed class MockGraphicsContext : IGraphicsContext
{
    public List<TextureHandle> DisposedTextures { get; } = [];

    // IGraphicsContext State properties
    public IWindow? Window => null;
    public IGraphicsDevice? Device => null;
    public bool IsInitialized => true;
    public int Width => 800;
    public int Height => 600;

    // Default resources
    public ShaderHandle LitShader => new(1);
    public ShaderHandle UnlitShader => new(2);
    public ShaderHandle SolidShader => new(3);
    public ShaderHandle PbrShader => new(4);
    public TextureHandle WhiteTexture => new(1);

    // Lifecycle
    public void ProcessEvents() { }
    public void SwapBuffers() { }

    // Mesh operations
    public MeshHandle CreateMesh(ReadOnlySpan<byte> vertices, int vertexCount, ReadOnlySpan<uint> indices) => new(1);
    public MeshHandle CreateCube(float size = 1f) => new(1);
    public MeshHandle CreateQuad(float width = 1f, float height = 1f) => new(1);
    public void DeleteMesh(MeshHandle handle) { }
    public void BindMesh(MeshHandle handle) { }
    public void DrawMesh(MeshHandle handle) { }

    // Texture operations
    public TextureHandle CreateTexture(int width, int height, ReadOnlySpan<byte> pixels) => new(1);
    public TextureHandle LoadTexture(string path) => new(1);
    public void DeleteTexture(TextureHandle handle) => DisposedTextures.Add(handle);
    public void BindTexture(TextureHandle handle, int unit = 0) { }

    // Shader operations
    public ShaderHandle CreateShader(string vertexSource, string fragmentSource) => new(1);
    public void DeleteShader(ShaderHandle handle) { }
    public void BindShader(ShaderHandle handle) { }
    public void SetUniform(string name, float value) { }
    public void SetUniform(string name, int value) { }
    public void SetUniform(string name, Vector2 value) { }
    public void SetUniform(string name, Vector3 value) { }
    public void SetUniform(string name, Vector4 value) { }
    public void SetUniform(string name, in Matrix4x4 value) { }

    // Render state
    public void SetClearColor(Vector4 color) { }
    public void Clear(ClearMask mask) { }
    public void SetViewport(int x, int y, int width, int height) { }
    public void SetDepthTest(bool enabled) { }
    public void SetBlending(bool enabled) { }
    public void SetCulling(bool enabled, CullFaceMode mode = CullFaceMode.Back) { }

    public void Dispose() { }
}
