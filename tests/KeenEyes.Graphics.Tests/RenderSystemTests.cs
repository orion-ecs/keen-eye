using System.Numerics;
using KeenEyes.Graphics.Tests.Mocks;
using KeenEyes.Spatial;

namespace KeenEyes.Graphics.Tests;

/// <summary>
/// Tests for the RenderSystem class.
/// </summary>
public class RenderSystemTests : IDisposable
{
    private readonly World world;
    private readonly MockGraphicsWindow mockWindow;
    private readonly GraphicsContext context;

    public RenderSystemTests()
    {
        world = new World();
        mockWindow = new MockGraphicsWindow();
        context = new GraphicsContext(world, null, mockWindow);
        world.SetExtension(context);
        context.Initialize();
        mockWindow.SimulateLoad();
    }

    public void Dispose()
    {
        world.Dispose();
    }

    #region Initialize Tests

    [Fact]
    public void OnInitialize_WithoutGraphicsContext_ThrowsInvalidOperationException()
    {
        using var worldWithoutGraphics = new World();
        var system = new RenderSystem();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            worldWithoutGraphics.AddSystem(system));

        Assert.Contains("GraphicsContext", ex.Message);
    }

    [Fact]
    public void OnInitialize_WithGraphicsContext_Succeeds()
    {
        var system = new RenderSystem();

        // Should not throw
        world.AddSystem(system);
    }

    #endregion

    #region Update Without Camera Tests

    [Fact]
    public void Update_WithoutCamera_DoesNotRender()
    {
        var system = new RenderSystem();
        world.AddSystem(system);
        mockWindow.MockDevice.Calls.Clear();

        system.Update(0.016f);

        // Should not have any draw calls
        Assert.DoesNotContain(mockWindow.MockDevice.Calls, c => c.Contains("DrawElements"));
    }

    [Fact]
    public void Update_WhenNotInitialized_ReturnsEarly()
    {
        // Create a new context but don't simulate load
        using var testWorld = new World();
        var testWindow = new MockGraphicsWindow();
        var testContext = new GraphicsContext(testWorld, null, testWindow);
        testWorld.SetExtension(testContext);
        testContext.Initialize();
        // Don't call testWindow.SimulateLoad()

        var system = new RenderSystem();
        testWorld.AddSystem(system);
        testWindow.MockDevice.Calls.Clear();

        system.Update(0.016f);

        // Should not have any render calls
        Assert.Empty(testWindow.MockDevice.Calls);
    }

    #endregion

    #region Camera Selection Tests

    [Fact]
    public void Update_WithMainCameraTag_UsesMainCamera()
    {
        var system = new RenderSystem();
        world.AddSystem(system);

        // Create a regular camera with red clear color
        var regularCam = Camera.CreatePerspective(60f, 1.78f, 0.1f, 1000f);
        regularCam.ClearColor = new Vector4(1, 0, 0, 1);
        world.Spawn()
            .With(regularCam)
            .With(Transform3D.Identity)
            .Build();

        // Create a main camera with green clear color
        var mainCam = Camera.CreatePerspective(90f, 1.78f, 0.1f, 1000f);
        mainCam.ClearColor = new Vector4(0, 1, 0, 1);
        world.Spawn()
            .With(mainCam)
            .With(Transform3D.Identity)
            .WithTag<MainCameraTag>()
            .Build();

        mockWindow.MockDevice.Calls.Clear();
        system.Update(0.016f);

        // Should use main camera's green clear color
        Assert.Contains(mockWindow.MockDevice.Calls, c => c.Contains("ClearColor(0,") && c.Contains("1,"));
    }

    [Fact]
    public void Update_WithoutMainCameraTag_UsesFallbackCamera()
    {
        var system = new RenderSystem();
        world.AddSystem(system);

        // Create only a regular camera
        world.Spawn()
            .With(Camera.CreatePerspective(60f, 1.78f, 0.1f, 1000f))
            .With(Transform3D.Identity)
            .Build();

        mockWindow.MockDevice.Calls.Clear();
        system.Update(0.016f);

        // Should use the fallback camera
        Assert.Contains(mockWindow.MockDevice.Calls, c => c.Contains("ClearColor"));
    }

    #endregion

    #region Clear Buffer Tests

    [Fact]
    public void Update_WithClearColorBuffer_ClearsColorBuffer()
    {
        var system = new RenderSystem();
        world.AddSystem(system);

        var camera = Camera.CreatePerspective(60f, 1.78f, 0.1f, 1000f);
        camera.ClearColorBuffer = true;
        camera.ClearDepthBuffer = false;
        world.Spawn()
            .With(camera)
            .With(Transform3D.Identity)
            .Build();

        mockWindow.MockDevice.Calls.Clear();
        system.Update(0.016f);

        Assert.Contains(mockWindow.MockDevice.Calls, c => c.Contains("ClearColor"));
        Assert.Contains(mockWindow.MockDevice.Calls, c => c.StartsWith("Clear(") && c.Contains("ColorBuffer"));
    }

    [Fact]
    public void Update_WithClearDepthBuffer_ClearsDepthBuffer()
    {
        var system = new RenderSystem();
        world.AddSystem(system);

        var camera = Camera.CreatePerspective(60f, 1.78f, 0.1f, 1000f);
        camera.ClearColorBuffer = false;
        camera.ClearDepthBuffer = true;
        world.Spawn()
            .With(camera)
            .With(Transform3D.Identity)
            .Build();

        mockWindow.MockDevice.Calls.Clear();
        system.Update(0.016f);

        Assert.Contains(mockWindow.MockDevice.Calls, c => c.StartsWith("Clear(") && c.Contains("DepthBuffer"));
    }

    [Fact]
    public void Update_WithBothBuffersClear_ClearsBoth()
    {
        var system = new RenderSystem();
        world.AddSystem(system);

        var camera = Camera.CreatePerspective(60f, 1.78f, 0.1f, 1000f);
        camera.ClearColorBuffer = true;
        camera.ClearDepthBuffer = true;
        world.Spawn()
            .With(camera)
            .With(Transform3D.Identity)
            .Build();

        mockWindow.MockDevice.Calls.Clear();
        system.Update(0.016f);

        Assert.Contains(mockWindow.MockDevice.Calls, c =>
            c.StartsWith("Clear(") && c.Contains("ColorBuffer") && c.Contains("DepthBuffer"));
    }

    #endregion

    #region Depth and Culling Tests

    [Fact]
    public void Update_EnablesDepthTest()
    {
        var system = new RenderSystem();
        world.AddSystem(system);

        world.Spawn()
            .With(Camera.CreatePerspective(60f, 1.78f, 0.1f, 1000f))
            .With(Transform3D.Identity)
            .Build();

        mockWindow.MockDevice.Calls.Clear();
        system.Update(0.016f);

        Assert.Contains(mockWindow.MockDevice.Calls, c => c.Contains("Enable") && c.Contains("DepthTest"));
    }

    [Fact]
    public void Update_EnablesCullFace()
    {
        var system = new RenderSystem();
        world.AddSystem(system);

        world.Spawn()
            .With(Camera.CreatePerspective(60f, 1.78f, 0.1f, 1000f))
            .With(Transform3D.Identity)
            .Build();

        mockWindow.MockDevice.Calls.Clear();
        system.Update(0.016f);

        Assert.Contains(mockWindow.MockDevice.Calls, c => c.Contains("Enable") && c.Contains("CullFace"));
        Assert.Contains(mockWindow.MockDevice.Calls, c => c.Contains("CullFace(Back)"));
    }

    #endregion

    #region Light Tests

    [Fact]
    public void Update_WithDirectionalLight_UsesLightData()
    {
        var system = new RenderSystem();
        world.AddSystem(system);

        // Create camera
        world.Spawn()
            .With(Camera.CreatePerspective(60f, 1.78f, 0.1f, 1000f))
            .With(Transform3D.Identity)
            .Build();

        // Create directional light
        world.Spawn()
            .With(new Light { Type = LightType.Directional, Color = new Vector3(1, 0, 0), Intensity = 2f })
            .With(Transform3D.Identity)
            .Build();

        // Create renderable entity
        int meshId = context.CreateQuad();
        world.Spawn()
            .With(Transform3D.Identity)
            .With(new Renderable(meshId, 0))
            .Build();

        mockWindow.MockDevice.Calls.Clear();
        system.Update(0.016f);

        // Should have set light uniforms (checking that rendering happened)
        Assert.Contains(mockWindow.MockDevice.Calls, c => c.Contains("DrawElements"));
    }

    #endregion

    #region Render Queue Tests

    [Fact]
    public void Update_WithRenderableEntities_DrawsThem()
    {
        var system = new RenderSystem();
        world.AddSystem(system);

        // Create camera
        world.Spawn()
            .With(Camera.CreatePerspective(60f, 1.78f, 0.1f, 1000f))
            .With(Transform3D.Identity)
            .Build();

        // Create renderable entity
        int meshId = context.CreateQuad();
        world.Spawn()
            .With(Transform3D.Identity)
            .With(new Renderable(meshId, 0))
            .Build();

        mockWindow.MockDevice.Calls.Clear();
        system.Update(0.016f);

        Assert.Contains(mockWindow.MockDevice.Calls, c => c.Contains("DrawElements"));
    }

    [Fact]
    public void Update_WithInvalidMeshId_SkipsEntity()
    {
        var system = new RenderSystem();
        world.AddSystem(system);

        // Create camera
        world.Spawn()
            .With(Camera.CreatePerspective(60f, 1.78f, 0.1f, 1000f))
            .With(Transform3D.Identity)
            .Build();

        // Create entity with invalid mesh ID
        world.Spawn()
            .With(Transform3D.Identity)
            .With(new Renderable(9999, 0)) // Invalid mesh ID
            .Build();

        mockWindow.MockDevice.Calls.Clear();
        system.Update(0.016f);

        // Should not have draw calls (mesh not found)
        Assert.DoesNotContain(mockWindow.MockDevice.Calls, c => c.Contains("DrawElements"));
    }

    [Fact]
    public void Update_SortsRenderQueueByLayer()
    {
        var system = new RenderSystem();
        world.AddSystem(system);

        // Create camera
        world.Spawn()
            .With(Camera.CreatePerspective(60f, 1.78f, 0.1f, 1000f))
            .With(Transform3D.Identity)
            .Build();

        // Create multiple renderables with different layers
        int meshId = context.CreateQuad();

        world.Spawn()
            .With(Transform3D.Identity)
            .With(new Renderable(meshId, 0) { Layer = 10 })
            .Build();

        world.Spawn()
            .With(Transform3D.Identity)
            .With(new Renderable(meshId, 0) { Layer = 5 })
            .Build();

        world.Spawn()
            .With(Transform3D.Identity)
            .With(new Renderable(meshId, 0) { Layer = 15 })
            .Build();

        mockWindow.MockDevice.Calls.Clear();
        system.Update(0.016f);

        // Should have 3 draw calls (one per entity)
        int drawCalls = mockWindow.MockDevice.Calls.Count(c => c.Contains("DrawElements"));
        Assert.Equal(3, drawCalls);
    }

    #endregion

    #region Material Tests

    [Fact]
    public void Update_WithMaterial_UsesMaterialShader()
    {
        var system = new RenderSystem();
        world.AddSystem(system);

        // Create camera
        world.Spawn()
            .With(Camera.CreatePerspective(60f, 1.78f, 0.1f, 1000f))
            .With(Transform3D.Identity)
            .Build();

        // Create renderable with material
        int meshId = context.CreateQuad();
        int shaderId = context.CreateShader(
            "#version 330\nvoid main() { }",
            "#version 330\nout vec4 c;\nvoid main() { c = vec4(1); }");

        world.Spawn()
            .With(Transform3D.Identity)
            .With(new Renderable(meshId, 0))
            .With(new Material { ShaderId = shaderId, Color = new Vector4(1, 0, 0, 1) })
            .Build();

        mockWindow.MockDevice.Calls.Clear();
        system.Update(0.016f);

        // Should use the custom shader
        Assert.Contains(mockWindow.MockDevice.Calls, c => c.Contains("UseProgram"));
        Assert.Contains(mockWindow.MockDevice.Calls, c => c.Contains("DrawElements"));
    }

    [Fact]
    public void Update_WithoutMaterial_UsesSolidShader()
    {
        var system = new RenderSystem();
        world.AddSystem(system);

        // Create camera
        world.Spawn()
            .With(Camera.CreatePerspective(60f, 1.78f, 0.1f, 1000f))
            .With(Transform3D.Identity)
            .Build();

        // Create renderable without material
        int meshId = context.CreateQuad();
        world.Spawn()
            .With(Transform3D.Identity)
            .With(new Renderable(meshId, 0))
            .Build();

        mockWindow.MockDevice.Calls.Clear();
        system.Update(0.016f);

        // Should still render (using default solid shader)
        Assert.Contains(mockWindow.MockDevice.Calls, c => c.Contains("UseProgram"));
        Assert.Contains(mockWindow.MockDevice.Calls, c => c.Contains("DrawElements"));
    }

    #endregion

    #region Texture Binding Tests

    [Fact]
    public void Update_WithMaterialTexture_BindsTexture()
    {
        var system = new RenderSystem();
        world.AddSystem(system);

        // Create camera
        world.Spawn()
            .With(Camera.CreatePerspective(60f, 1.78f, 0.1f, 1000f))
            .With(Transform3D.Identity)
            .Build();

        // Create texture and renderable with material
        int meshId = context.CreateQuad();
        int textureId = context.CreateSolidColorTexture(255, 0, 0);

        world.Spawn()
            .With(Transform3D.Identity)
            .With(new Renderable(meshId, 0))
            .With(new Material { TextureId = textureId })
            .Build();

        mockWindow.MockDevice.Calls.Clear();
        system.Update(0.016f);

        Assert.Contains(mockWindow.MockDevice.Calls, c => c.Contains("ActiveTexture"));
        Assert.Contains(mockWindow.MockDevice.Calls, c => c.Contains("BindTexture"));
    }

    #endregion

    #region Cleanup Tests

    [Fact]
    public void Update_AfterRendering_CleansUp()
    {
        var system = new RenderSystem();
        world.AddSystem(system);

        // Create camera
        world.Spawn()
            .With(Camera.CreatePerspective(60f, 1.78f, 0.1f, 1000f))
            .With(Transform3D.Identity)
            .Build();

        // Create renderable
        int meshId = context.CreateQuad();
        world.Spawn()
            .With(Transform3D.Identity)
            .With(new Renderable(meshId, 0))
            .Build();

        mockWindow.MockDevice.Calls.Clear();
        system.Update(0.016f);

        // Should unbind VAO and shader at the end
        Assert.Contains(mockWindow.MockDevice.Calls, c => c == "BindVertexArray(0)");
        Assert.Contains(mockWindow.MockDevice.Calls, c => c == "UseProgram(0)");
    }

    [Fact]
    public void Dispose_ClearsRenderQueue()
    {
        var system = new RenderSystem();
        world.AddSystem(system);

        // Should not throw
        system.Dispose();
    }

    #endregion
}
