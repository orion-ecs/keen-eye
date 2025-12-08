using KeenEyes.Graphics.Tests.Mocks;

namespace KeenEyes.Graphics.Tests;

/// <summary>
/// Tests for the GraphicsContext class.
/// </summary>
public class GraphicsContextTests : IDisposable
{
    private readonly World world;
    private readonly MockGraphicsWindow mockWindow;
    private readonly GraphicsContext context;

    public GraphicsContextTests()
    {
        world = new World();
        mockWindow = new MockGraphicsWindow();
        context = new GraphicsContext(world, null, mockWindow);
    }

    public void Dispose()
    {
        context.Dispose();
        world.Dispose();
    }

    #region Initialize Tests

    [Fact]
    public void Initialize_SetsUpWindowEvents()
    {
        context.Initialize();

        // Window should be assigned
        Assert.NotNull(context.Window);
    }

    [Fact]
    public void Initialize_CalledTwice_OnlyInitializesOnce()
    {
        context.Initialize();
        context.Initialize();

        // Should not throw or cause issues
        Assert.NotNull(context.Window);
    }

    [Fact]
    public void Initialize_ThenLoad_SetsIsInitializedTrue()
    {
        context.Initialize();
        mockWindow.SimulateLoad();

        Assert.True(context.IsInitialized);
    }

    [Fact]
    public void Initialize_ThenLoad_CreatesDevice()
    {
        context.Initialize();
        mockWindow.SimulateLoad();

        Assert.NotNull(context.Device);
    }

    [Fact]
    public void Initialize_ThenLoad_CreatesBuiltInShaders()
    {
        context.Initialize();
        mockWindow.SimulateLoad();

        Assert.True(context.UnlitShaderId > 0);
        Assert.True(context.LitShaderId > 0);
        Assert.True(context.SolidShaderId > 0);
    }

    [Fact]
    public void Initialize_ThenLoad_CreatesWhiteTexture()
    {
        context.Initialize();
        mockWindow.SimulateLoad();

        Assert.True(context.WhiteTextureId > 0);
    }

    [Fact]
    public void Initialize_ThenLoad_RaisesOnLoadEvent()
    {
        bool loadRaised = false;
        context.OnLoad += () => loadRaised = true;

        context.Initialize();
        mockWindow.SimulateLoad();

        Assert.True(loadRaised);
    }

    #endregion

    #region Resize Tests

    [Fact]
    public void Resize_RaisesOnResizeEvent()
    {
        int receivedWidth = 0;
        int receivedHeight = 0;
        context.OnResize += (w, h) =>
        {
            receivedWidth = w;
            receivedHeight = h;
        };

        context.Initialize();
        mockWindow.SimulateLoad();
        mockWindow.SimulateResize(1920, 1080);

        Assert.Equal(1920, receivedWidth);
        Assert.Equal(1080, receivedHeight);
    }

    [Fact]
    public void Resize_UpdatesViewport()
    {
        context.Initialize();
        mockWindow.SimulateLoad();
        mockWindow.MockDevice.Calls.Clear();

        mockWindow.SimulateResize(800, 600);

        Assert.Contains(mockWindow.MockDevice.Calls, c => c.Contains("Viewport") && c.Contains("800") && c.Contains("600"));
    }

    #endregion

    #region Closing Tests

    [Fact]
    public void Closing_RaisesOnClosingEvent()
    {
        bool closingRaised = false;
        context.OnClosing += () => closingRaised = true;

        context.Initialize();
        mockWindow.SimulateClosing();

        Assert.True(closingRaised);
    }

    [Fact]
    public void ShouldClose_ReturnsTrueWhenWindowClosing()
    {
        context.Initialize();

        Assert.False(context.ShouldClose);

        mockWindow.SimulateClosing();

        Assert.True(context.ShouldClose);
    }

    #endregion

    #region Mesh API Tests

    [Fact]
    public void CreateMesh_ReturnsValidId()
    {
        context.Initialize();
        mockWindow.SimulateLoad();

        Vertex[] vertices =
        [
            new() { Position = new System.Numerics.Vector3(0, 0, 0) },
            new() { Position = new System.Numerics.Vector3(1, 0, 0) },
            new() { Position = new System.Numerics.Vector3(0, 1, 0) }
        ];
        uint[] indices = [0, 1, 2];

        int meshId = context.CreateMesh(vertices, indices);

        Assert.True(meshId > 0);
    }

    [Fact]
    public void CreateQuad_ReturnsValidId()
    {
        context.Initialize();
        mockWindow.SimulateLoad();

        int meshId = context.CreateQuad(2f, 2f);

        Assert.True(meshId > 0);
    }

    [Fact]
    public void CreateCube_ReturnsValidId()
    {
        context.Initialize();
        mockWindow.SimulateLoad();

        int meshId = context.CreateCube(1f);

        Assert.True(meshId > 0);
    }

    [Fact]
    public void DeleteMesh_WithValidId_ReturnsTrue()
    {
        context.Initialize();
        mockWindow.SimulateLoad();

        int meshId = context.CreateQuad();
        bool deleted = context.DeleteMesh(meshId);

        Assert.True(deleted);
    }

    [Fact]
    public void DeleteMesh_WithInvalidId_ReturnsFalse()
    {
        context.Initialize();
        mockWindow.SimulateLoad();

        bool deleted = context.DeleteMesh(999);

        Assert.False(deleted);
    }

    #endregion

    #region Texture API Tests

    [Fact]
    public void CreateTexture_ReturnsValidId()
    {
        context.Initialize();
        mockWindow.SimulateLoad();

        byte[] data = [255, 0, 0, 255];
        int textureId = context.CreateTexture(1, 1, data);

        Assert.True(textureId > 0);
    }

    [Fact]
    public void CreateSolidColorTexture_ReturnsValidId()
    {
        context.Initialize();
        mockWindow.SimulateLoad();

        int textureId = context.CreateSolidColorTexture(255, 128, 0);

        Assert.True(textureId > 0);
    }

    [Fact]
    public void DeleteTexture_WithValidId_ReturnsTrue()
    {
        context.Initialize();
        mockWindow.SimulateLoad();

        int textureId = context.CreateSolidColorTexture(255, 255, 255);
        bool deleted = context.DeleteTexture(textureId);

        Assert.True(deleted);
    }

    #endregion

    #region Shader API Tests

    [Fact]
    public void CreateShader_ReturnsValidId()
    {
        context.Initialize();
        mockWindow.SimulateLoad();

        int shaderId = context.CreateShader(
            "#version 330\nvoid main() { }",
            "#version 330\nout vec4 c;\nvoid main() { c = vec4(1); }");

        Assert.True(shaderId > 0);
    }

    [Fact]
    public void DeleteShader_WithValidId_ReturnsTrue()
    {
        context.Initialize();
        mockWindow.SimulateLoad();

        int shaderId = context.CreateShader(
            "#version 330\nvoid main() { }",
            "#version 330\nout vec4 c;\nvoid main() { c = vec4(1); }");
        bool deleted = context.DeleteShader(shaderId);

        Assert.True(deleted);
    }

    #endregion

    #region Rendering API Tests

    [Fact]
    public void Clear_WithColor_SetsClearColorAndClears()
    {
        context.Initialize();
        mockWindow.SimulateLoad();
        mockWindow.MockDevice.Calls.Clear();

        context.Clear(new System.Numerics.Vector4(1f, 0f, 0f, 1f));

        Assert.Contains(mockWindow.MockDevice.Calls, c => c.StartsWith("ClearColor"));
        Assert.Contains(mockWindow.MockDevice.Calls, c => c.StartsWith("Clear"));
    }

    [Fact]
    public void Clear_WithoutArgs_UsesDefaultClearColor()
    {
        context.Initialize();
        mockWindow.SimulateLoad();
        mockWindow.MockDevice.Calls.Clear();

        context.Clear();

        Assert.Contains(mockWindow.MockDevice.Calls, c => c.StartsWith("Clear"));
    }

    [Fact]
    public void EnableDepthTest_CallsDeviceEnable()
    {
        context.Initialize();
        mockWindow.SimulateLoad();
        mockWindow.MockDevice.Calls.Clear();

        context.EnableDepthTest();

        Assert.Contains(mockWindow.MockDevice.Calls, c => c.Contains("Enable") && c.Contains("DepthTest"));
    }

    [Fact]
    public void DisableDepthTest_CallsDeviceDisable()
    {
        context.Initialize();
        mockWindow.SimulateLoad();
        mockWindow.MockDevice.Calls.Clear();

        context.DisableDepthTest();

        Assert.Contains(mockWindow.MockDevice.Calls, c => c.Contains("Disable") && c.Contains("DepthTest"));
    }

    [Fact]
    public void EnableCulling_CallsDeviceEnable()
    {
        context.Initialize();
        mockWindow.SimulateLoad();
        mockWindow.MockDevice.Calls.Clear();

        context.EnableCulling();

        Assert.Contains(mockWindow.MockDevice.Calls, c => c.Contains("Enable") && c.Contains("CullFace"));
    }

    [Fact]
    public void DisableCulling_CallsDeviceDisable()
    {
        context.Initialize();
        mockWindow.SimulateLoad();
        mockWindow.MockDevice.Calls.Clear();

        context.DisableCulling();

        Assert.Contains(mockWindow.MockDevice.Calls, c => c.Contains("Disable") && c.Contains("CullFace"));
    }

    [Fact]
    public void SetViewport_CallsDeviceViewport()
    {
        context.Initialize();
        mockWindow.SimulateLoad();
        mockWindow.MockDevice.Calls.Clear();

        context.SetViewport(0, 0, 1920, 1080);

        Assert.Contains(mockWindow.MockDevice.Calls, c => c.Contains("Viewport") && c.Contains("1920") && c.Contains("1080"));
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        context.Initialize();
        mockWindow.SimulateLoad();

        // Should not throw
        context.Dispose();
        context.Dispose();
    }

    #endregion
}
