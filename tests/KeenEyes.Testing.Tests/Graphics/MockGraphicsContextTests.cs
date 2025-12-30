using System.Numerics;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.Testing.Graphics;

namespace KeenEyes.Testing.Tests.Graphics;

public class MockGraphicsContextTests
{
    #region Construction

    [Fact]
    public void Constructor_CreatesDefaultResources()
    {
        using var context = new MockGraphicsContext();

        Assert.True(context.LitShader.Id > 0);
        Assert.True(context.UnlitShader.Id > 0);
        Assert.True(context.SolidShader.Id > 0);
        Assert.True(context.WhiteTexture.Id > 0);
        Assert.Contains(context.LitShader, context.Shaders.Keys);
        Assert.Contains(context.WhiteTexture, context.Textures.Keys);
    }

    [Fact]
    public void Constructor_InitializesEmptyCollections()
    {
        using var context = new MockGraphicsContext();

        Assert.Empty(context.Meshes);
        Assert.Empty(context.MeshDrawCalls);
    }

    [Fact]
    public void Constructor_SetsDefaultDimensions()
    {
        using var context = new MockGraphicsContext();

        Assert.Equal(800, context.Width);
        Assert.Equal(600, context.Height);
        Assert.True(context.IsInitialized);
    }

    #endregion

    #region Mesh Operations

    [Fact]
    public void CreateMesh_CreatesMeshWithData()
    {
        using var context = new MockGraphicsContext();
        var vertices = new byte[] { 0, 0, 0, 1, 0, 0, 0, 1, 0 };
        var indices = new uint[] { 0, 1, 2 };

        var mesh = context.CreateMesh(vertices, 3, indices);

        Assert.True(mesh.Id > 0);
        Assert.Contains(mesh, context.Meshes.Keys);
        Assert.Equal(3, context.Meshes[mesh].VertexCount);
        Assert.Equal(3, context.Meshes[mesh].IndexCount);
    }

    [Fact]
    public void CreateQuad_CreatesQuadMesh()
    {
        using var context = new MockGraphicsContext();

        var mesh = context.CreateQuad();

        Assert.True(mesh.Id > 0);
        Assert.Equal(4, context.Meshes[mesh].VertexCount);
        Assert.Equal(6, context.Meshes[mesh].IndexCount);
        Assert.True(context.Meshes[mesh].IsQuad);
    }

    [Fact]
    public void CreateQuad_WithDimensions_StoresSize()
    {
        using var context = new MockGraphicsContext();

        var mesh = context.CreateQuad(2f, 3f);

        Assert.Equal(2f, context.Meshes[mesh].Width);
        Assert.Equal(3f, context.Meshes[mesh].Height);
    }

    [Fact]
    public void CreateCube_CreatesCubeMesh()
    {
        using var context = new MockGraphicsContext();

        var mesh = context.CreateCube();

        Assert.True(mesh.Id > 0);
        Assert.Equal(24, context.Meshes[mesh].VertexCount);
        Assert.Equal(36, context.Meshes[mesh].IndexCount);
        Assert.True(context.Meshes[mesh].IsCube);
    }

    [Fact]
    public void CreateCube_WithSize_StoresSize()
    {
        using var context = new MockGraphicsContext();

        var mesh = context.CreateCube(2f);

        Assert.Equal(2f, context.Meshes[mesh].Size);
    }

    [Fact]
    public void DeleteMesh_RemovesMesh()
    {
        using var context = new MockGraphicsContext();
        var mesh = context.CreateQuad();

        context.DeleteMesh(mesh);

        Assert.DoesNotContain(mesh, context.Meshes.Keys);
    }

    [Fact]
    public void DeleteMesh_UnbindsIfBound()
    {
        using var context = new MockGraphicsContext();
        var mesh = context.CreateQuad();
        context.BindMesh(mesh);

        context.DeleteMesh(mesh);

        Assert.Equal(default(MeshHandle), context.BoundMesh);
    }

    [Fact]
    public void BindMesh_UpdatesBoundMesh()
    {
        using var context = new MockGraphicsContext();
        var mesh = context.CreateQuad();

        context.BindMesh(mesh);

        Assert.Equal(mesh, context.BoundMesh);
    }

    [Fact]
    public void DrawMesh_RecordsDrawCall()
    {
        using var context = new MockGraphicsContext();
        var mesh = context.CreateQuad();
        var shader = context.CreateShader("v", "f");
        context.BindShader(shader);
        context.BindMesh(mesh);

        context.DrawMesh(mesh);

        Assert.Single(context.MeshDrawCalls);
        Assert.Equal(mesh, context.MeshDrawCalls[0].Mesh);
        Assert.Equal(shader, context.MeshDrawCalls[0].Shader);
    }

    #endregion

    #region Texture Operations

    [Fact]
    public void CreateTexture_CreatesTextureFromData()
    {
        using var context = new MockGraphicsContext();
        var data = new byte[64]; // 4x4 RGBA

        var texture = context.CreateTexture(4, 4, data);

        Assert.True(texture.Id > 0);
        Assert.Contains(texture, context.Textures.Keys);
        Assert.Equal(4, context.Textures[texture].Width);
        Assert.Equal(4, context.Textures[texture].Height);
    }

    [Fact]
    public void LoadTexture_CreatesTextureWithPath()
    {
        using var context = new MockGraphicsContext();

        var texture = context.LoadTexture("test.png");

        Assert.True(texture.Id > 0);
        Assert.Equal("test.png", context.Textures[texture].SourcePath);
    }

    [Fact]
    public void DeleteTexture_RemovesTexture()
    {
        using var context = new MockGraphicsContext();
        var texture = context.CreateTexture(4, 4, new byte[64]);

        context.DeleteTexture(texture);

        Assert.DoesNotContain(texture, context.Textures.Keys);
    }

    [Fact]
    public void DeleteTexture_UnbindsIfBound()
    {
        using var context = new MockGraphicsContext();
        var texture = context.CreateTexture(4, 4, new byte[64]);
        context.BindTexture(texture, 0);

        context.DeleteTexture(texture);

        Assert.Empty(context.BoundTextures);
    }

    [Fact]
    public void BindTexture_UpdatesBoundTextures()
    {
        using var context = new MockGraphicsContext();
        var texture = context.CreateTexture(4, 4, new byte[64]);

        context.BindTexture(texture, 0);

        Assert.Equal(texture, context.BoundTextures[0]);
    }

    [Fact]
    public void BindTexture_MultipleUnits_TracksAll()
    {
        using var context = new MockGraphicsContext();
        var tex1 = context.CreateTexture(4, 4, new byte[64]);
        var tex2 = context.CreateTexture(4, 4, new byte[64]);

        context.BindTexture(tex1, 0);
        context.BindTexture(tex2, 1);

        Assert.Equal(tex1, context.BoundTextures[0]);
        Assert.Equal(tex2, context.BoundTextures[1]);
    }

    [Fact]
    public void BindTexture_WithZeroId_UnbindsUnit()
    {
        using var context = new MockGraphicsContext();
        var texture = context.CreateTexture(4, 4, new byte[64]);
        context.BindTexture(texture, 0);

        context.BindTexture(default, 0);

        Assert.False(context.BoundTextures.ContainsKey(0));
    }

    #endregion

    #region Shader Operations

    [Fact]
    public void CreateShader_CreatesShader()
    {
        using var context = new MockGraphicsContext();

        var shader = context.CreateShader("vertex source", "fragment source");

        Assert.True(shader.Id > 0);
        Assert.Contains(shader, context.Shaders.Keys);
        Assert.Equal("vertex source", context.Shaders[shader].VertexSource);
        Assert.Equal("fragment source", context.Shaders[shader].FragmentSource);
    }

    [Fact]
    public void DeleteShader_RemovesShader()
    {
        using var context = new MockGraphicsContext();
        var shader = context.CreateShader("v", "f");

        context.DeleteShader(shader);

        Assert.DoesNotContain(shader, context.Shaders.Keys);
    }

    [Fact]
    public void DeleteShader_DoesNotRemoveDefaultShaders()
    {
        using var context = new MockGraphicsContext();

        context.DeleteShader(context.LitShader);
        context.DeleteShader(context.UnlitShader);
        context.DeleteShader(context.SolidShader);

        Assert.Contains(context.LitShader, context.Shaders.Keys);
        Assert.Contains(context.UnlitShader, context.Shaders.Keys);
        Assert.Contains(context.SolidShader, context.Shaders.Keys);
    }

    [Fact]
    public void DeleteShader_UnbindsIfBound()
    {
        using var context = new MockGraphicsContext();
        var shader = context.CreateShader("v", "f");
        context.BindShader(shader);

        context.DeleteShader(shader);

        Assert.Equal(default(ShaderHandle), context.BoundShader);
    }

    [Fact]
    public void BindShader_UpdatesBoundShader()
    {
        using var context = new MockGraphicsContext();
        var shader = context.CreateShader("v", "f");

        context.BindShader(shader);

        Assert.Equal(shader, context.BoundShader);
    }

    [Fact]
    public void BindShader_ClearsUniformValues()
    {
        using var context = new MockGraphicsContext();
        var shader1 = context.CreateShader("v", "f");
        context.BindShader(shader1);
        context.SetUniform("test", 1.0f);

        var shader2 = context.CreateShader("v2", "f2");
        context.BindShader(shader2);

        Assert.Empty(context.UniformValues);
    }

    [Fact]
    public void SetUniform_Float_StoresValue()
    {
        using var context = new MockGraphicsContext();
        var shader = context.CreateShader("v", "f");
        context.BindShader(shader);

        context.SetUniform("time", 1.5f);

        Assert.Equal(1.5f, context.UniformValues["time"]);
    }

    [Fact]
    public void SetUniform_Int_StoresValue()
    {
        using var context = new MockGraphicsContext();
        var shader = context.CreateShader("v", "f");
        context.BindShader(shader);

        context.SetUniform("texture", 0);

        Assert.Equal(0, context.UniformValues["texture"]);
    }

    [Fact]
    public void SetUniform_Vector2_StoresValue()
    {
        using var context = new MockGraphicsContext();
        var shader = context.CreateShader("v", "f");
        context.BindShader(shader);
        var value = new Vector2(800, 600);

        context.SetUniform("resolution", value);

        Assert.Equal(value, context.UniformValues["resolution"]);
    }

    [Fact]
    public void SetUniform_Vector3_StoresValue()
    {
        using var context = new MockGraphicsContext();
        var shader = context.CreateShader("v", "f");
        context.BindShader(shader);
        var value = new Vector3(1, 0, 0);

        context.SetUniform("color", value);

        Assert.Equal(value, context.UniformValues["color"]);
    }

    [Fact]
    public void SetUniform_Vector4_StoresValue()
    {
        using var context = new MockGraphicsContext();
        var shader = context.CreateShader("v", "f");
        context.BindShader(shader);
        var value = new Vector4(1, 0, 0, 1);

        context.SetUniform("tint", value);

        Assert.Equal(value, context.UniformValues["tint"]);
    }

    [Fact]
    public void SetUniform_Matrix4x4_StoresValue()
    {
        using var context = new MockGraphicsContext();
        var shader = context.CreateShader("v", "f");
        context.BindShader(shader);
        var value = Matrix4x4.Identity;

        context.SetUniform("mvp", value);

        Assert.Equal(value, context.UniformValues["mvp"]);
    }

    #endregion

    #region Lifecycle

    [Fact]
    public void ProcessEvents_IncrementsCounter()
    {
        using var context = new MockGraphicsContext();

        context.ProcessEvents();
        context.ProcessEvents();

        Assert.Equal(2, context.RenderState.ProcessEventsCount);
    }

    [Fact]
    public void SwapBuffers_IncrementsCounter()
    {
        using var context = new MockGraphicsContext();

        context.SwapBuffers();
        context.SwapBuffers();

        Assert.Equal(2, context.RenderState.SwapBuffersCount);
    }

    #endregion

    #region Render State

    [Fact]
    public void SetClearColor_UpdatesRenderState()
    {
        using var context = new MockGraphicsContext();
        var color = new Vector4(0.2f, 0.3f, 0.4f, 1f);

        context.SetClearColor(color);

        Assert.Equal(color, context.RenderState.ClearColor);
    }

    [Fact]
    public void Clear_IncrementsClearCount()
    {
        using var context = new MockGraphicsContext();

        context.Clear(ClearMask.ColorBuffer);
        context.Clear(ClearMask.DepthBuffer);

        Assert.Equal(2, context.RenderState.ClearCount);
    }

    [Fact]
    public void SetViewport_UpdatesRenderState()
    {
        using var context = new MockGraphicsContext();

        context.SetViewport(0, 0, 1920, 1080);

        Assert.Equal((0, 0, 1920, 1080), context.RenderState.Viewport);
    }

    [Fact]
    public void SetDepthTest_UpdatesRenderState()
    {
        using var context = new MockGraphicsContext();

        context.SetDepthTest(true);

        Assert.True(context.RenderState.DepthTestEnabled);
    }

    #endregion

    #region Reset and Dispose

    [Fact]
    public void Reset_ClearsAllUserState()
    {
        using var context = new MockGraphicsContext();
        context.CreateQuad();
        context.CreateTexture(4, 4, new byte[64]);
        var shader = context.CreateShader("v", "f");
        context.BindShader(shader);
        context.DrawMesh(context.CreateQuad());

        context.Reset();

        Assert.Empty(context.Meshes);
        Assert.Empty(context.MeshDrawCalls);
        Assert.Empty(context.UniformValues);
        // Default resources should be recreated
        Assert.True(context.LitShader.Id > 0);
    }

    [Fact]
    public void ClearDrawCalls_ClearsOnlyDrawCalls()
    {
        using var context = new MockGraphicsContext();
        var mesh = context.CreateQuad();
        context.DrawMesh(mesh);

        context.ClearDrawCalls();

        Assert.Empty(context.MeshDrawCalls);
        Assert.NotEmpty(context.Meshes);
    }

    [Fact]
    public void Dispose_ClearsState()
    {
        var context = new MockGraphicsContext();
        context.CreateQuad();

        context.Dispose();

        Assert.Empty(context.Meshes);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var context = new MockGraphicsContext();

        context.Dispose();
        context.Dispose();

        // Should not throw
    }

    #endregion
}

#region Supporting Type Tests

public class MockMeshInfoTests
{
    [Fact]
    public void MockMeshInfo_StoresVertexAndIndexCount()
    {
        var info = new MockMeshInfo(100, 200, null, null);

        Assert.Equal(100, info.VertexCount);
        Assert.Equal(200, info.IndexCount);
    }

    [Fact]
    public void MockMeshInfo_StoresData()
    {
        var vertices = new byte[] { 1, 2, 3 };
        var indices = new uint[] { 0, 1, 2 };
        var info = new MockMeshInfo(3, 3, vertices, indices);

        Assert.Equal(vertices, info.VertexData);
        Assert.Equal(indices, info.IndexData);
    }
}

public class MockTextureInfoTests
{
    [Fact]
    public void MockTextureInfo_StoresDimensions()
    {
        var info = new MockTextureInfo(256, 128, null);

        Assert.Equal(256, info.Width);
        Assert.Equal(128, info.Height);
    }

    [Fact]
    public void MockTextureInfo_StoresPath()
    {
        var info = new MockTextureInfo(0, 0, "test.png");

        Assert.Equal("test.png", info.SourcePath);
    }
}

public class MockShaderInfoTests
{
    [Fact]
    public void MockShaderInfo_StoresSource()
    {
        var info = new MockShaderInfo("vertex", "fragment");

        Assert.Equal("vertex", info.VertexSource);
        Assert.Equal("fragment", info.FragmentSource);
    }
}

public class MeshDrawCallGraphicsContextTests
{
    [Fact]
    public void MeshDrawCall_StoresAllProperties()
    {
        var mesh = new MeshHandle(1);
        var shader = new ShaderHandle(2);
        var textures = new Dictionary<int, TextureHandle> { [0] = new(3) };
        var uniforms = new Dictionary<string, object> { ["test"] = 1.0f };

        var call = new MeshDrawCall(mesh, shader, textures, uniforms);

        Assert.Equal(mesh, call.Mesh);
        Assert.Equal(shader, call.Shader);
        Assert.Single(call.Textures);
        Assert.Single(call.Uniforms);
    }
}

public class MockContextRenderStateTests
{
    [Fact]
    public void MockContextRenderState_InitializesDefaults()
    {
        var state = new MockContextRenderState();

        Assert.Equal(Vector4.Zero, state.ClearColor);
        Assert.Equal(0, state.ClearCount);
        Assert.False(state.DepthTestEnabled);
    }

    [Fact]
    public void Reset_ClearsState()
    {
        var state = new MockContextRenderState
        {
            ClearColor = Vector4.One,
            ClearCount = 5,
            DepthTestEnabled = true
        };

        state.Reset();

        Assert.Equal(Vector4.Zero, state.ClearColor);
        Assert.Equal(0, state.ClearCount);
        Assert.False(state.DepthTestEnabled);
    }
}

#endregion
