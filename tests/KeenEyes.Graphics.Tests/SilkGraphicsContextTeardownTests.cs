using KeenEyes.Graphics.Abstractions;
using KeenEyes.Graphics.Silk;
using KeenEyes.Graphics.Tests.Mocks;
using KeenEyes.Platform.Silk;

// This project carries a local mock of the same name; the shared testing double is the one
// that can simulate a lost context.
using MockGraphicsDevice = KeenEyes.Testing.Graphics.MockGraphicsDevice;

namespace KeenEyes.Graphics.Tests;

/// <summary>
/// Tests that graphics teardown never throws, even when the underlying graphics context is
/// already gone.
/// </summary>
/// <remarks>
/// Regression coverage for #1256. A window whose load aborted never raises Closing, so the GPU
/// resources are still alive when the world is disposed - and by then the OpenGL context may be
/// destroyed, which makes every GPU delete throw from the driver binding. Those exceptions
/// escaped <c>World.Dispose()</c> and crashed the process after the application had already
/// reported and handled the original failure.
/// </remarks>
public class SilkGraphicsContextTeardownTests
{
    /// <summary>
    /// Builds a world with the graphics plugin installed and its GPU resources created against
    /// a mock device, which is the state a real game reaches once its window has loaded.
    /// </summary>
    private static (World World, SilkGraphicsContext Context, MockGraphicsDevice Device) CreateLoadedGraphics()
    {
        var world = new World();
        world.SetExtension<ISilkWindowProvider>(new MockSilkWindowProvider());
        world.InstallPlugin(new SilkGraphicsPlugin());

        var context = world.GetExtension<SilkGraphicsContext>();
        var device = new MockGraphicsDevice();
        context.InitializeResources(device);

        return (world, context, device);
    }

    [Fact]
    public void Dispose_WhenGraphicsContextIsLost_DoesNotThrow()
    {
        var (world, context, device) = CreateLoadedGraphics();
        using (world)
        {
            // The window died and took the OpenGL context with it.
            device.ThrowOnDelete = true;

            var exception = Record.Exception(context.Dispose);

            Assert.Null(exception);
        }
    }

    [Fact]
    public void UninstallPlugin_WhenGraphicsContextIsLost_DoesNotThrow()
    {
        var (world, _, device) = CreateLoadedGraphics();
        using (world)
        {
            device.ThrowOnDelete = true;

            var exception = Record.Exception(() => world.UninstallPlugin<SilkGraphicsPlugin>());

            Assert.Null(exception);
        }
    }

    [Fact]
    public void WorldDispose_WhenGraphicsContextIsLost_DoesNotThrow()
    {
        var (world, _, device) = CreateLoadedGraphics();
        device.ThrowOnDelete = true;

        var exception = Record.Exception(world.Dispose);

        Assert.Null(exception);
    }

    [Fact]
    public void Dispose_WhenGraphicsContextIsLost_StillRemovesExtensions()
    {
        var (world, _, device) = CreateLoadedGraphics();
        using (world)
        {
            device.ThrowOnDelete = true;

            world.UninstallPlugin<SilkGraphicsPlugin>();

            // A failed GPU delete must not abort the rest of the teardown.
            Assert.False(world.TryGetExtension<IGraphicsContext>(out _));
            Assert.False(world.TryGetExtension<SilkGraphicsContext>(out _));
        }
    }

    [Fact]
    public void Dispose_WithUsableGraphicsContext_DeletesGpuResources()
    {
        var (world, context, device) = CreateLoadedGraphics();
        using (world)
        {
            Assert.NotEmpty(device.Programs);

            context.Dispose();

            // Tolerating failures must not turn into skipping the work: with a live context
            // every shader program the context created is deleted.
            Assert.Empty(device.Programs);
        }
    }
}
