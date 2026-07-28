using System.Numerics;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.Graphics.Silk;
using KeenEyes.Graphics.Tests.Mocks;
using KeenEyes.Platform.Silk;

// This project carries a local mock of the same name; the shared testing double is the one that
// records render state.
using MockGraphicsDevice = KeenEyes.Testing.Graphics.MockGraphicsDevice;

namespace KeenEyes.Graphics.Tests;

/// <summary>
/// Pins the mapping between the two coordinate spaces the renderer straddles: framebuffer
/// pixels (the viewport, scissor rectangles) and logical points (projections, layout, pointer
/// input).
/// </summary>
/// <remarks>
/// <para>
/// Regression coverage for #1352. The viewport used to be sized from the window's logical size,
/// so on any display where the framebuffer is larger than the window - every Retina Mac, Windows
/// at 150%, Linux with fractional scaling - it covered a fraction of the window and the whole
/// frame rendered small in one corner.
/// </para>
/// <para>
/// These tests simulate a 2x display through <see cref="MockSilkWindowProvider"/>, which is the
/// only way to cover the bug on 1x CI hardware.
/// </para>
/// </remarks>
public class HiDpiViewportTests
{
    private const int LogicalWidth = 800;
    private const int LogicalHeight = 600;

    /// <summary>
    /// Builds a graphics context whose window reports the given logical and framebuffer sizes,
    /// with its GPU resources created against a recording mock device.
    /// </summary>
    private static (World World, SilkGraphicsContext Context, MockGraphicsDevice Device, MockSilkWindowProvider Provider) CreateGraphics(
        int logicalWidth,
        int logicalHeight,
        int framebufferWidth,
        int framebufferHeight)
    {
        var world = new World();
        var provider = new MockSilkWindowProvider
        {
            Width = logicalWidth,
            Height = logicalHeight,
            FramebufferWidth = framebufferWidth,
            FramebufferHeight = framebufferHeight,
        };

        world.SetExtension<ISilkWindowProvider>(provider);
        world.InstallPlugin(new SilkGraphicsPlugin());

        var context = world.GetExtension<SilkGraphicsContext>();
        var device = new MockGraphicsDevice();
        context.InitializeResources(device);

        return (world, context, device, provider);
    }

    /// <summary>
    /// A 2x display: 800x600 points of window backed by a 1600x1200 pixel framebuffer.
    /// </summary>
    private static (World World, SilkGraphicsContext Context, MockGraphicsDevice Device, MockSilkWindowProvider Provider) CreateRetinaGraphics()
        => CreateGraphics(LogicalWidth, LogicalHeight, LogicalWidth * 2, LogicalHeight * 2);

    /// <summary>
    /// Reads back the orthographic projection the 2D renderer handed to the GPU.
    /// </summary>
    private static Matrix4x4 GetRecordedProjection(MockGraphicsDevice device)
    {
        var recorded = device.Programs.Values
            .Where(program => program.UniformLocations.TryGetValue("uProjection", out var location)
                && program.UniformValues.ContainsKey(location))
            .Select(program => program.UniformValues[program.UniformLocations["uProjection"]])
            .OfType<Matrix4x4>()
            .ToList();

        return Assert.Single(recorded);
    }

    #region Viewport is sized in framebuffer pixels

    [Fact]
    public void InitializeResources_WhenFramebufferExceedsLogicalSize_SizesViewportInPixels()
    {
        var (world, _, device, _) = CreateRetinaGraphics();
        using (world)
        {
            // The whole framebuffer, not the 800x600 logical rectangle that used to be passed.
            Assert.Equal((0, 0, 1600, 1200), device.RenderState.Viewport);
        }
    }

    [Fact]
    public void UnbindRenderTarget_WhenFramebufferExceedsLogicalSize_RestoresViewportInPixels()
    {
        var (world, context, device, _) = CreateRetinaGraphics();
        using (world)
        {
            // Rendering to an offscreen target retargets the viewport to the target's texels...
            var target = context.CreateRenderTarget(256, 128, RenderTargetFormat.RGBA8Depth24);
            context.BindRenderTarget(target);
            Assert.Equal((0, 0, 256, 128), device.RenderState.Viewport);

            // ...so coming back must restore the framebuffer's pixel size, not the logical one.
            context.UnbindRenderTarget();

            Assert.Equal((0, 0, 1600, 1200), device.RenderState.Viewport);
        }
    }

    #endregion

    #region Projection and reported sizes stay in logical points

    [Fact]
    public void Width_ReportsLogicalPoints_WhileFramebufferWidthReportsPixels()
    {
        var (world, context, _, _) = CreateRetinaGraphics();
        using (world)
        {
            // UI layout and pointer handling consume Width/Height, so they must stay logical.
            Assert.Equal(LogicalWidth, context.Width);
            Assert.Equal(LogicalHeight, context.Height);

            Assert.Equal(LogicalWidth * 2, context.FramebufferWidth);
            Assert.Equal(LogicalHeight * 2, context.FramebufferHeight);
        }
    }

    [Fact]
    public void Begin_WhenFramebufferExceedsLogicalSize_Builds2DProjectionFromLogicalPoints()
    {
        var (world, context, device, _) = CreateRetinaGraphics();
        using (world)
        {
            context.Renderer2D!.Begin();

            // A projection in logical points keeps drawing coordinates aligned with the pointer
            // positions Silk reports; combined with the pixel viewport, the result is a
            // full-window frame rendered at native resolution.
            var expected = Matrix4x4.CreateOrthographicOffCenter(0, LogicalWidth, LogicalHeight, 0, -1, 1);

            Assert.Equal(expected, GetRecordedProjection(device));
        }
    }

    [Fact]
    public void PushClip_WhenFramebufferExceedsLogicalSize_ConvertsScissorRectToPixels()
    {
        var (world, context, device, _) = CreateRetinaGraphics();
        using (world)
        {
            context.Renderer2D!.Begin();

            // A clip rectangle in logical points, as UI code supplies it.
            context.Renderer2D.PushClip(new Rectangle(100, 50, 200, 100));

            // OpenGL reads scissor rectangles in device pixels with a bottom-left origin, so
            // every component doubles: x 100 -> 200, and y (600 - 50 - 100) = 450 -> 900.
            Assert.Equal((200, 900, 400, 200), device.RenderState.ScissorRect);
        }
    }

    #endregion

    #region Which event drives which space

    [Fact]
    public void FramebufferResize_UpdatesViewport()
    {
        var (world, _, device, provider) = CreateRetinaGraphics();
        using (world)
        {
            provider.SimulateFramebufferResize(2048, 1024);

            Assert.Equal((0, 0, 2048, 1024), device.RenderState.Viewport);
        }
    }

    [Fact]
    public void Resize_UpdatesProjectionButLeavesViewportToFramebufferResize()
    {
        var (world, context, device, provider) = CreateRetinaGraphics();
        using (world)
        {
            // A logical resize alone says nothing about the pixel size of the framebuffer, so
            // it must not touch the viewport - the paired FramebufferResize event does that.
            provider.SimulateResize(1000, 500);
            context.Renderer2D!.Begin();

            Assert.Equal((0, 0, 1600, 1200), device.RenderState.Viewport);
            Assert.Equal(
                Matrix4x4.CreateOrthographicOffCenter(0, 1000, 500, 0, -1, 1),
                GetRecordedProjection(device));
        }
    }

    [Fact]
    public void FramebufferResize_DoesNotChangeTheLogicalProjection()
    {
        var (world, context, device, provider) = CreateRetinaGraphics();
        using (world)
        {
            provider.SimulateFramebufferResize(2048, 1024);
            context.Renderer2D!.Begin();

            // Rendering at a different pixel density must not move any UI element.
            Assert.Equal(
                Matrix4x4.CreateOrthographicOffCenter(0, LogicalWidth, LogicalHeight, 0, -1, 1),
                GetRecordedProjection(device));
        }
    }

    #endregion

    #region 1x displays are unaffected

    [Fact]
    public void InitializeResources_AtOneToOneScaling_SizesViewportFromTheSharedSize()
    {
        var (world, context, device, _) = CreateGraphics(LogicalWidth, LogicalHeight, LogicalWidth, LogicalHeight);
        using (world)
        {
            Assert.Equal((0, 0, LogicalWidth, LogicalHeight), device.RenderState.Viewport);
            Assert.Equal(context.Width, context.FramebufferWidth);
            Assert.Equal(context.Height, context.FramebufferHeight);
        }
    }

    [Fact]
    public void PushClip_AtOneToOneScaling_PassesTheClipRectStraightThrough()
    {
        var (world, context, device, _) = CreateGraphics(LogicalWidth, LogicalHeight, LogicalWidth, LogicalHeight);
        using (world)
        {
            context.Renderer2D!.Begin();
            context.Renderer2D.PushClip(new Rectangle(100, 50, 200, 100));

            // Unscaled, exactly as before the HiDPI fix: only the Y flip is applied.
            Assert.Equal((100, 450, 200, 100), device.RenderState.ScissorRect);
        }
    }

    [Fact]
    public void Resize_AtOneToOneScaling_KeepsViewportAndProjectionInStep()
    {
        var (world, context, device, provider) = CreateGraphics(LogicalWidth, LogicalHeight, LogicalWidth, LogicalHeight);
        using (world)
        {
            // A 1x window resizes both spaces together, so the two events agree.
            provider.SimulateResize(1024, 768);
            provider.SimulateFramebufferResize(1024, 768);
            context.Renderer2D!.Begin();

            Assert.Equal((0, 0, 1024, 768), device.RenderState.Viewport);
            Assert.Equal(
                Matrix4x4.CreateOrthographicOffCenter(0, 1024, 768, 0, -1, 1),
                GetRecordedProjection(device));
        }
    }

    #endregion
}
