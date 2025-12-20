using System.Numerics;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.Testing.Graphics;

namespace KeenEyes.Testing.Tests.Graphics;

public class MockGraphicsContextTests
{
    #region Default Resources

    [Fact]
    public void Constructor_CreatesDefaultShaders()
    {
        using var context = new MockGraphicsContext();

        context.LitShader.Id.ShouldNotBe(0);
        context.UnlitShader.Id.ShouldNotBe(0);
        context.SolidShader.Id.ShouldNotBe(0);
    }

    [Fact]
    public void Constructor_CreatesWhiteTexture()
    {
        using var context = new MockGraphicsContext();

        context.WhiteTexture.Id.ShouldNotBe(0);
        context.Textures.ShouldContainKey(context.WhiteTexture);
    }

    #endregion

    #region Mesh Operations

    [Fact]
    public void CreateMesh_CreatesMesh()
    {
        using var context = new MockGraphicsContext();
        var vertices = new byte[100];
        var indices = new uint[] { 0, 1, 2 };

        var mesh = context.CreateMesh(vertices, 10, indices);

        mesh.Id.ShouldNotBe(0);
        context.Meshes.ShouldContainKey(mesh);
        context.Meshes[mesh].VertexCount.ShouldBe(10);
        context.Meshes[mesh].IndexCount.ShouldBe(3);
    }

    [Fact]
    public void CreateCube_CreatesCubePrimitive()
    {
        using var context = new MockGraphicsContext();

        var mesh = context.CreateCube(2f);

        context.Meshes[mesh].IsCube.ShouldBeTrue();
        context.Meshes[mesh].Size.ShouldBe(2f);
    }

    [Fact]
    public void CreateQuad_CreatesQuadPrimitive()
    {
        using var context = new MockGraphicsContext();

        var mesh = context.CreateQuad(100f, 50f);

        context.Meshes[mesh].IsQuad.ShouldBeTrue();
        context.Meshes[mesh].Width.ShouldBe(100f);
        context.Meshes[mesh].Height.ShouldBe(50f);
    }

    [Fact]
    public void DeleteMesh_RemovesMesh()
    {
        using var context = new MockGraphicsContext();
        var mesh = context.CreateCube();

        context.DeleteMesh(mesh);

        context.Meshes.ShouldNotContainKey(mesh);
    }

    [Fact]
    public void BindMesh_SetsBoundMesh()
    {
        using var context = new MockGraphicsContext();
        var mesh = context.CreateCube();

        context.BindMesh(mesh);

        context.BoundMesh.ShouldBe(mesh);
    }

    [Fact]
    public void DrawMesh_RecordsDrawCall()
    {
        using var context = new MockGraphicsContext();
        var mesh = context.CreateCube();
        context.BindShader(context.LitShader);

        context.DrawMesh(mesh);

        context.MeshDrawCalls.Count.ShouldBe(1);
        context.MeshDrawCalls[0].Mesh.ShouldBe(mesh);
        context.MeshDrawCalls[0].Shader.ShouldBe(context.LitShader);
    }

    #endregion

    #region Texture Operations

    [Fact]
    public void CreateTexture_CreatesTexture()
    {
        using var context = new MockGraphicsContext();
        var pixels = new byte[16];

        var texture = context.CreateTexture(4, 4, pixels);

        texture.Id.ShouldNotBe(0);
        context.Textures.ShouldContainKey(texture);
        context.Textures[texture].Width.ShouldBe(4);
        context.Textures[texture].Height.ShouldBe(4);
    }

    [Fact]
    public void LoadTexture_TracksPath()
    {
        using var context = new MockGraphicsContext();

        var texture = context.LoadTexture("assets/test.png");

        context.Textures[texture].SourcePath.ShouldBe("assets/test.png");
    }

    [Fact]
    public void DeleteTexture_RemovesTexture()
    {
        using var context = new MockGraphicsContext();
        var texture = context.CreateTexture(4, 4, new byte[16]);

        context.DeleteTexture(texture);

        context.Textures.ShouldNotContainKey(texture);
    }

    [Fact]
    public void BindTexture_SetsBoundTexture()
    {
        using var context = new MockGraphicsContext();
        var texture = context.CreateTexture(4, 4, new byte[16]);

        context.BindTexture(texture, 0);

        context.BoundTextures[0].ShouldBe(texture);
    }

    [Fact]
    public void BindTexture_ZeroUnbinds()
    {
        using var context = new MockGraphicsContext();
        var texture = context.CreateTexture(4, 4, new byte[16]);
        context.BindTexture(texture, 0);

        context.BindTexture(new TextureHandle(0), 0);

        context.BoundTextures.ShouldNotContainKey(0);
    }

    #endregion

    #region Shader Operations

    [Fact]
    public void CreateShader_CreatesShader()
    {
        using var context = new MockGraphicsContext();

        var shader = context.CreateShader("vertex", "fragment");

        shader.Id.ShouldNotBe(0);
        context.Shaders.ShouldContainKey(shader);
        context.Shaders[shader].VertexSource.ShouldBe("vertex");
        context.Shaders[shader].FragmentSource.ShouldBe("fragment");
    }

    [Fact]
    public void DeleteShader_RemovesShader()
    {
        using var context = new MockGraphicsContext();
        var shader = context.CreateShader("vertex", "fragment");

        context.DeleteShader(shader);

        context.Shaders.ShouldNotContainKey(shader);
    }

    [Fact]
    public void DeleteShader_DoesNotDeleteDefaultShaders()
    {
        using var context = new MockGraphicsContext();

        context.DeleteShader(context.LitShader);
        context.DeleteShader(context.UnlitShader);
        context.DeleteShader(context.SolidShader);

        context.Shaders.ShouldContainKey(context.LitShader);
        context.Shaders.ShouldContainKey(context.UnlitShader);
        context.Shaders.ShouldContainKey(context.SolidShader);
    }

    [Fact]
    public void BindShader_SetsBoundShader()
    {
        using var context = new MockGraphicsContext();

        context.BindShader(context.LitShader);

        context.BoundShader.ShouldBe(context.LitShader);
    }

    [Fact]
    public void BindShader_ClearsUniformValues()
    {
        using var context = new MockGraphicsContext();
        context.BindShader(context.LitShader);
        context.SetUniform("color", 1f);

        context.BindShader(context.UnlitShader);

        context.UniformValues.ShouldBeEmpty();
    }

    #endregion

    #region Uniform Operations

    [Fact]
    public void SetUniform_Float_StoresValue()
    {
        using var context = new MockGraphicsContext();
        context.BindShader(context.LitShader);

        context.SetUniform("alpha", 0.5f);

        context.UniformValues["alpha"].ShouldBe(0.5f);
    }

    [Fact]
    public void SetUniform_Int_StoresValue()
    {
        using var context = new MockGraphicsContext();
        context.BindShader(context.LitShader);

        context.SetUniform("count", 42);

        context.UniformValues["count"].ShouldBe(42);
    }

    [Fact]
    public void SetUniform_Vector2_StoresValue()
    {
        using var context = new MockGraphicsContext();
        context.BindShader(context.LitShader);

        context.SetUniform("offset", new Vector2(10, 20));

        context.UniformValues["offset"].ShouldBe(new Vector2(10, 20));
    }

    [Fact]
    public void SetUniform_Vector4_StoresValue()
    {
        using var context = new MockGraphicsContext();
        context.BindShader(context.LitShader);

        context.SetUniform("color", new Vector4(1, 0, 0, 1));

        context.UniformValues["color"].ShouldBe(new Vector4(1, 0, 0, 1));
    }

    [Fact]
    public void SetUniform_Matrix4x4_StoresValue()
    {
        using var context = new MockGraphicsContext();
        context.BindShader(context.LitShader);
        var matrix = Matrix4x4.CreateTranslation(1, 2, 3);

        context.SetUniform("model", matrix);

        context.UniformValues["model"].ShouldBe(matrix);
    }

    #endregion

    #region Render State

    [Fact]
    public void SetClearColor_SetsColor()
    {
        using var context = new MockGraphicsContext();

        context.SetClearColor(new Vector4(1, 0, 0, 1));

        context.RenderState.ClearColor.ShouldBe(new Vector4(1, 0, 0, 1));
    }

    [Fact]
    public void Clear_IncrementsCount()
    {
        using var context = new MockGraphicsContext();

        context.Clear(ClearMask.ColorBuffer);

        context.RenderState.ClearCount.ShouldBe(1);
        context.RenderState.LastClearMask.ShouldBe(ClearMask.ColorBuffer);
    }

    [Fact]
    public void SetViewport_SetsViewport()
    {
        using var context = new MockGraphicsContext();

        context.SetViewport(0, 0, 1920, 1080);

        context.RenderState.Viewport.ShouldBe((0, 0, 1920, 1080));
    }

    [Fact]
    public void SetDepthTest_SetsState()
    {
        using var context = new MockGraphicsContext();

        context.SetDepthTest(true);

        context.RenderState.DepthTestEnabled.ShouldBeTrue();
    }

    [Fact]
    public void SetBlending_SetsState()
    {
        using var context = new MockGraphicsContext();

        context.SetBlending(true);

        context.RenderState.BlendingEnabled.ShouldBeTrue();
    }

    [Fact]
    public void SetCulling_SetsState()
    {
        using var context = new MockGraphicsContext();

        context.SetCulling(true, CullFaceMode.Front);

        context.RenderState.CullingEnabled.ShouldBeTrue();
        context.RenderState.CullFaceMode.ShouldBe(CullFaceMode.Front);
    }

    #endregion

    #region Lifecycle

    [Fact]
    public void ProcessEvents_IncrementsCount()
    {
        using var context = new MockGraphicsContext();

        context.ProcessEvents();
        context.ProcessEvents();

        context.RenderState.ProcessEventsCount.ShouldBe(2);
    }

    [Fact]
    public void SwapBuffers_IncrementsCount()
    {
        using var context = new MockGraphicsContext();

        context.SwapBuffers();
        context.SwapBuffers();

        context.RenderState.SwapBuffersCount.ShouldBe(2);
    }

    #endregion

    #region Reset Tests

    [Fact]
    public void Reset_ClearsNonDefaultResources()
    {
        using var context = new MockGraphicsContext();
        var mesh = context.CreateCube();
        var texture = context.CreateTexture(4, 4, new byte[16]);
        var shader = context.CreateShader("v", "f");
        context.DrawMesh(mesh);

        context.Reset();

        context.Meshes.ShouldBeEmpty();
        context.MeshDrawCalls.ShouldBeEmpty();
        context.Shaders.Count.ShouldBe(3); // Only default shaders remain
        context.Textures.Count.ShouldBe(1); // Only white texture remains
    }

    [Fact]
    public void ClearDrawCalls_ClearsOnlyDrawCalls()
    {
        using var context = new MockGraphicsContext();
        var mesh = context.CreateCube();
        context.DrawMesh(mesh);

        context.ClearDrawCalls();

        context.MeshDrawCalls.ShouldBeEmpty();
        context.Meshes.ShouldNotBeEmpty();
    }

    #endregion

    #region Properties

    [Fact]
    public void IsInitialized_DefaultsToTrue()
    {
        using var context = new MockGraphicsContext();

        context.IsInitialized.ShouldBeTrue();
    }

    [Fact]
    public void Width_DefaultsTo800()
    {
        using var context = new MockGraphicsContext();

        context.Width.ShouldBe(800);
    }

    [Fact]
    public void Height_DefaultsTo600()
    {
        using var context = new MockGraphicsContext();

        context.Height.ShouldBe(600);
    }

    #endregion
}
