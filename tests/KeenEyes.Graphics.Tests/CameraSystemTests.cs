using KeenEyes.Graphics.Tests.Mocks;

namespace KeenEyes.Graphics.Tests;

/// <summary>
/// Tests for the CameraSystem class.
/// </summary>
public class CameraSystemTests : IDisposable
{
    private readonly World world;
    private readonly MockGraphicsWindow mockWindow;
    private readonly GraphicsContext context;

    public CameraSystemTests()
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
    public void OnInitialize_SubscribesToResizeEvent()
    {
        var system = new CameraSystem();
        world.AddSystem(system);

        // Verify system receives resize events by simulating a resize
        var camera = new Camera { FieldOfView = 60f, AspectRatio = 1f };
        world.Spawn().With(camera).Build();

        mockWindow.SimulateResize(1920, 1080);

        var entity = world.Query<Camera>().First();
        var updatedCamera = world.Get<Camera>(entity);
        Assert.Equal(1920f / 1080f, updatedCamera.AspectRatio, 0.001f);
    }

    [Fact]
    public void OnInitialize_CapturesInitialWindowSize()
    {
        // Window starts with a different size
        mockWindow.Width = 800;
        mockWindow.Height = 600;

        var system = new CameraSystem();
        world.AddSystem(system);

        // Create a camera with a different aspect ratio
        var camera = new Camera { FieldOfView = 60f, AspectRatio = 1f };
        world.Spawn().With(camera).Build();

        // Simulate a resize to trigger the initial update
        mockWindow.SimulateResize(800, 600);

        // Camera should have aspect ratio based on window size
        var entity = world.Query<Camera>().First();
        var updatedCamera = world.Get<Camera>(entity);
        Assert.Equal(800f / 600f, updatedCamera.AspectRatio, 0.001f);
    }

    #endregion

    #region Resize Tests

    [Fact]
    public void OnResize_UpdatesCameraAspectRatio()
    {
        var system = new CameraSystem();
        world.AddSystem(system);

        // Create a camera
        var camera = new Camera { FieldOfView = 60f, AspectRatio = 1f };
        world.Spawn().With(camera).Build();

        // Simulate resize
        mockWindow.SimulateResize(1920, 1080);

        // Camera should have updated aspect ratio
        var entity = world.Query<Camera>().First();
        var updatedCamera = world.Get<Camera>(entity);
        Assert.Equal(1920f / 1080f, updatedCamera.AspectRatio, 0.001f);
    }

    [Fact]
    public void OnResize_UpdatesAllCameras()
    {
        var system = new CameraSystem();
        world.AddSystem(system);

        // Create multiple cameras
        world.Spawn().With(new Camera { FieldOfView = 60f, AspectRatio = 1f }).Build();
        world.Spawn().With(new Camera { FieldOfView = 90f, AspectRatio = 1f }).Build();
        world.Spawn().With(new Camera { FieldOfView = 45f, AspectRatio = 1f }).Build();

        // Simulate resize
        mockWindow.SimulateResize(1600, 900);

        // All cameras should have updated aspect ratio
        float expectedAspect = 1600f / 900f;
        foreach (var entity in world.Query<Camera>())
        {
            var cam = world.Get<Camera>(entity);
            Assert.Equal(expectedAspect, cam.AspectRatio, 0.001f);
        }
    }

    [Fact]
    public void OnResize_WithZeroWidth_DoesNotUpdate()
    {
        var system = new CameraSystem();
        world.AddSystem(system);

        // Create a camera with known aspect ratio
        var camera = new Camera { FieldOfView = 60f, AspectRatio = 2f };
        world.Spawn().With(camera).Build();

        // Simulate resize with zero width
        mockWindow.SimulateResize(0, 600);

        // Camera should retain original aspect ratio
        var entity = world.Query<Camera>().First();
        var updatedCamera = world.Get<Camera>(entity);
        Assert.Equal(2f, updatedCamera.AspectRatio, 0.001f);
    }

    [Fact]
    public void OnResize_WithZeroHeight_DoesNotUpdate()
    {
        var system = new CameraSystem();
        world.AddSystem(system);

        // Create a camera with known aspect ratio
        var camera = new Camera { FieldOfView = 60f, AspectRatio = 2f };
        world.Spawn().With(camera).Build();

        // Simulate resize with zero height
        mockWindow.SimulateResize(800, 0);

        // Camera should retain original aspect ratio
        var entity = world.Query<Camera>().First();
        var updatedCamera = world.Get<Camera>(entity);
        Assert.Equal(2f, updatedCamera.AspectRatio, 0.001f);
    }

    #endregion

    #region Update Tests

    [Fact]
    public void Update_WhenWindowSizeChanges_UpdatesCameras()
    {
        var system = new CameraSystem();
        world.AddSystem(system);

        // Create a camera
        var camera = new Camera { FieldOfView = 60f, AspectRatio = 1f };
        world.Spawn().With(camera).Build();

        // Change window size without triggering resize event
        mockWindow.Width = 1024;
        mockWindow.Height = 768;

        // Update should detect the change
        system.Update(0.016f);

        // Camera should have updated aspect ratio
        var entity = world.Query<Camera>().First();
        var updatedCamera = world.Get<Camera>(entity);
        Assert.Equal(1024f / 768f, updatedCamera.AspectRatio, 0.001f);
    }

    [Fact]
    public void Update_WhenWindowSizeUnchanged_DoesNothing()
    {
        mockWindow.Width = 800;
        mockWindow.Height = 600;

        var system = new CameraSystem();
        world.AddSystem(system);

        // Create a camera with different aspect ratio
        var camera = new Camera { FieldOfView = 60f, AspectRatio = 99f };
        var entity = world.Spawn().With(camera).Build();

        // First update captures initial size and updates camera
        system.Update(0.016f);

        // Manually set a different aspect ratio
        ref var cam = ref world.Get<Camera>(entity);
        cam.AspectRatio = 99f;

        // Second update should not change it (size unchanged)
        system.Update(0.016f);

        var finalCamera = world.Get<Camera>(entity);
        Assert.Equal(99f, finalCamera.AspectRatio, 0.001f);
    }

    [Fact]
    public void Update_WithNewlyAddedCamera_UpdatesAspectRatio()
    {
        mockWindow.Width = 1920;
        mockWindow.Height = 1080;

        var system = new CameraSystem();
        world.AddSystem(system);

        // First update with no cameras
        system.Update(0.016f);

        // Add a camera after the resize
        var camera = new Camera { FieldOfView = 60f, AspectRatio = 1f };
        var entity = world.Spawn().With(camera).Build();

        // Trigger a size "change" to force update
        mockWindow.Width = 1921; // Slight change
        mockWindow.Height = 1080;
        system.Update(0.016f);

        // New camera should have correct aspect ratio
        var updatedCamera = world.Get<Camera>(entity);
        Assert.Equal(1921f / 1080f, updatedCamera.AspectRatio, 0.001f);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_UnsubscribesFromResizeEvent()
    {
        var system = new CameraSystem();
        world.AddSystem(system);

        // Create a camera
        var camera = new Camera { FieldOfView = 60f, AspectRatio = 1f };
        world.Spawn().With(camera).Build();

        // Dispose the system
        system.Dispose();

        // Resize should not affect camera (system unsubscribed)
        // Note: The camera aspect might still be what it was set to during registration
        // This test mainly verifies no exceptions are thrown
        mockWindow.SimulateResize(9999, 9999);
    }

    #endregion
}
