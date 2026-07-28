using System.Numerics;

using KeenEyes.Common;
using KeenEyes.Testing.Graphics;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI.Tests;

/// <summary>
/// Pins that UI layout is driven by the graphics context's logical size, never its framebuffer
/// size.
/// </summary>
/// <remarks>
/// Part of the fix for #1352. The renderer's viewport is sized in framebuffer pixels, but layout
/// must stay in logical points, because that is the space pointer positions are reported in. If
/// layout ever switched to pixels, everything would still render crisply while every click landed
/// in the wrong place - so the split is worth a test of its own.
/// </remarks>
public class UILayoutHiDpiTests
{
    /// <summary>
    /// A 2x display: 800x600 points of window backed by a 1600x1200 pixel framebuffer.
    /// </summary>
    private static MockGraphicsContext CreateRetinaGraphics() => new()
    {
        Width = 800,
        Height = 600,
        FramebufferWidth = 1600,
        FramebufferHeight = 1200,
    };

    [Fact]
    public void SetScreenSize_FedFromGraphicsContext_UsesLogicalPointsNotFramebufferPixels()
    {
        using var world = new World();
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var graphics = CreateRetinaGraphics();

        // This is the call every host makes (samples, editor): Width/Height, not the
        // Framebuffer* pair.
        layoutSystem.SetScreenSize(graphics.Width, graphics.Height);
        layoutSystem.Update(0);

        ref readonly var rect = ref world.Get<UIRect>(canvas);

        Assert.True(rect.ComputedBounds.Width.ApproximatelyEquals(800f));
        Assert.True(rect.ComputedBounds.Height.ApproximatelyEquals(600f));
    }

    [Fact]
    public void SetScreenSize_WhenFedFramebufferPixels_MisplacesLayoutRelativeToPointerSpace()
    {
        using var world = new World();
        var layoutSystem = new UILayoutSystem();
        world.AddSystem(layoutSystem);

        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        // A bottom-right anchored element is where the mismatch shows up: its position is
        // derived from the root size, so feeding pixels moves it off the visible window.
        var corner = world.Spawn()
            .With(UIElement.Default)
            .With(new UIRect
            {
                AnchorMin = Vector2.One,
                AnchorMax = Vector2.One,
                Pivot = Vector2.One,
                Size = new Vector2(100f, 40f),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fixed,
            })
            .Build();

        world.SetParent(corner, canvas);

        var graphics = CreateRetinaGraphics();

        layoutSystem.SetScreenSize(graphics.FramebufferWidth, graphics.FramebufferHeight);
        layoutSystem.Update(0);

        ref readonly var wrong = ref world.Get<UIRect>(corner);
        var wrongRight = wrong.ComputedBounds.X + wrong.ComputedBounds.Width;

        // Pointer coordinates only ever reach 800, so an element sitting at x=1600 is
        // unclickable. Documents why SetScreenSize must be fed logical points.
        Assert.True(wrongRight > graphics.Width);

        layoutSystem.SetScreenSize(graphics.Width, graphics.Height);
        layoutSystem.Update(0);

        ref readonly var right = ref world.Get<UIRect>(corner);

        Assert.True((right.ComputedBounds.X + right.ComputedBounds.Width).ApproximatelyEquals(800f));
    }
}
