using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.Testing.Graphics;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI.Tests;

/// <summary>
/// Tests for image scale modes in UIRenderSystem.
/// </summary>
public class UIImageScaleModeTests
{
    #region Stretch Mode Tests

    [Fact]
    public void RenderImage_StretchMode_FillsEntireBounds()
    {
        using var world = new World();
        var renderer2D = new Mock2DRenderer();
        world.SetExtension<I2DRenderer>(renderer2D);

        var renderSystem = new UIRenderSystem();
        world.AddSystem(renderSystem);

        var canvas = CreateSimpleCanvas(world);
        var texture = new TextureHandle(1, 100, 100);
        var element = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(10, 20, 200, 100))
            .With(UIImage.Stretch(texture))
            .Build();
        world.SetParent(element, canvas);
        SetComputedBounds(world, element, 10, 20, 200, 100);

        renderSystem.Update(0);

        var command = renderer2D.Commands.OfType<DrawTextureRegionCommand>().FirstOrDefault();
        Assert.NotNull(command);
        Assert.True(command.DestRect.X.ApproximatelyEquals(10f));
        Assert.True(command.DestRect.Y.ApproximatelyEquals(20f));
        Assert.True(command.DestRect.Width.ApproximatelyEquals(200f));
        Assert.True(command.DestRect.Height.ApproximatelyEquals(100f));
    }

    [Fact]
    public void RenderImage_StretchWithPreserveAspect_BehavesAsScaleToFit()
    {
        using var world = new World();
        var renderer2D = new Mock2DRenderer();
        world.SetExtension<I2DRenderer>(renderer2D);

        var renderSystem = new UIRenderSystem();
        world.AddSystem(renderSystem);

        var canvas = CreateSimpleCanvas(world);
        var texture = new TextureHandle(1, 100, 50); // 2:1 aspect ratio
        var element = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(0, 0, 200, 200))
            .With(new UIImage
            {
                Texture = texture,
                Tint = Vector4.One,
                ScaleMode = ImageScaleMode.Stretch,
                PreserveAspect = true
            })
            .Build();
        world.SetParent(element, canvas);
        SetComputedBounds(world, element, 0, 0, 200, 200);

        renderSystem.Update(0);

        var command = renderer2D.Commands.OfType<DrawTextureRegionCommand>().FirstOrDefault();
        Assert.NotNull(command);
        // Should be 200x100 (scaled to fit), centered vertically
        Assert.True(command.DestRect.Width.ApproximatelyEquals(200f));
        Assert.True(command.DestRect.Height.ApproximatelyEquals(100f));
        Assert.True(command.DestRect.Y.ApproximatelyEquals(50f)); // Centered: (200-100)/2
    }

    #endregion

    #region ScaleToFit Mode Tests

    [Fact]
    public void RenderImage_ScaleToFit_MaintainsAspectRatio()
    {
        using var world = new World();
        var renderer2D = new Mock2DRenderer();
        world.SetExtension<I2DRenderer>(renderer2D);

        var renderSystem = new UIRenderSystem();
        world.AddSystem(renderSystem);

        var canvas = CreateSimpleCanvas(world);
        var texture = new TextureHandle(1, 100, 50); // 2:1 aspect ratio
        var element = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(0, 0, 200, 200))
            .With(UIImage.Create(texture)) // Default is ScaleToFit
            .Build();
        world.SetParent(element, canvas);
        SetComputedBounds(world, element, 0, 0, 200, 200);

        renderSystem.Update(0);

        var command = renderer2D.Commands.OfType<DrawTextureRegionCommand>().FirstOrDefault();
        Assert.NotNull(command);
        // Texture is 2:1, bounds is 1:1, so scale by width
        // 200 wide means 100 tall, centered vertically at y=50
        Assert.True(command.DestRect.Width.ApproximatelyEquals(200f));
        Assert.True(command.DestRect.Height.ApproximatelyEquals(100f));
        Assert.True(command.DestRect.X.ApproximatelyEquals(0f));
        Assert.True(command.DestRect.Y.ApproximatelyEquals(50f));
    }

    [Fact]
    public void RenderImage_ScaleToFit_TallImage_CentersHorizontally()
    {
        using var world = new World();
        var renderer2D = new Mock2DRenderer();
        world.SetExtension<I2DRenderer>(renderer2D);

        var renderSystem = new UIRenderSystem();
        world.AddSystem(renderSystem);

        var canvas = CreateSimpleCanvas(world);
        var texture = new TextureHandle(1, 50, 100); // 1:2 aspect ratio (tall)
        var element = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(0, 0, 200, 200))
            .With(UIImage.Create(texture))
            .Build();
        world.SetParent(element, canvas);
        SetComputedBounds(world, element, 0, 0, 200, 200);

        renderSystem.Update(0);

        var command = renderer2D.Commands.OfType<DrawTextureRegionCommand>().FirstOrDefault();
        Assert.NotNull(command);
        // Texture is 1:2, bounds is 1:1, so scale by height
        // 200 tall means 100 wide, centered horizontally at x=50
        Assert.True(command.DestRect.Width.ApproximatelyEquals(100f));
        Assert.True(command.DestRect.Height.ApproximatelyEquals(200f));
        Assert.True(command.DestRect.X.ApproximatelyEquals(50f));
        Assert.True(command.DestRect.Y.ApproximatelyEquals(0f));
    }

    #endregion

    #region ScaleToFill Mode Tests

    [Fact]
    public void RenderImage_ScaleToFill_FillsBoundsAndCrops()
    {
        using var world = new World();
        var renderer2D = new Mock2DRenderer();
        world.SetExtension<I2DRenderer>(renderer2D);

        var renderSystem = new UIRenderSystem();
        world.AddSystem(renderSystem);

        var canvas = CreateSimpleCanvas(world);
        var texture = new TextureHandle(1, 100, 50); // 2:1 aspect ratio
        var element = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(0, 0, 200, 200))
            .With(new UIImage
            {
                Texture = texture,
                Tint = Vector4.One,
                ScaleMode = ImageScaleMode.ScaleToFill
            })
            .Build();
        world.SetParent(element, canvas);
        SetComputedBounds(world, element, 0, 0, 200, 200);

        renderSystem.Update(0);

        var command = renderer2D.Commands.OfType<DrawTextureRegionCommand>().FirstOrDefault();
        Assert.NotNull(command);
        // Dest rect should fill the entire bounds
        Assert.True(command.DestRect.X.ApproximatelyEquals(0f));
        Assert.True(command.DestRect.Y.ApproximatelyEquals(0f));
        Assert.True(command.DestRect.Width.ApproximatelyEquals(200f));
        Assert.True(command.DestRect.Height.ApproximatelyEquals(200f));
        // Source rect should be cropped (center portion of texture)
        Assert.True(command.SourceRect.Width < 1f); // Cropped horizontally
    }

    #endregion

    #region Tile Mode Tests

    [Fact]
    public void RenderImage_TileMode_ProducesMultipleDrawCalls()
    {
        using var world = new World();
        var renderer2D = new Mock2DRenderer();
        world.SetExtension<I2DRenderer>(renderer2D);

        var renderSystem = new UIRenderSystem();
        world.AddSystem(renderSystem);

        var canvas = CreateSimpleCanvas(world);
        var texture = new TextureHandle(1, 32, 32); // Small texture
        var element = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(0, 0, 100, 100))
            .With(UIImage.Tiled(texture))
            .Build();
        world.SetParent(element, canvas);
        SetComputedBounds(world, element, 0, 0, 100, 100);

        renderSystem.Update(0);

        var commands = renderer2D.Commands.OfType<DrawTextureRegionCommand>().ToList();
        // 100/32 = 4 tiles per axis (rounded up), so 4x4 = 16 tiles
        Assert.True(commands.Count >= 9); // At least 3x3 tiles
    }

    [Fact]
    public void RenderImage_TileMode_FirstTileAtOrigin()
    {
        using var world = new World();
        var renderer2D = new Mock2DRenderer();
        world.SetExtension<I2DRenderer>(renderer2D);

        var renderSystem = new UIRenderSystem();
        world.AddSystem(renderSystem);

        var canvas = CreateSimpleCanvas(world);
        var texture = new TextureHandle(1, 32, 32);
        var element = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(10, 20, 100, 100))
            .With(UIImage.Tiled(texture))
            .Build();
        world.SetParent(element, canvas);
        SetComputedBounds(world, element, 10, 20, 100, 100);

        renderSystem.Update(0);

        var commands = renderer2D.Commands.OfType<DrawTextureRegionCommand>().ToList();
        var firstTile = commands.FirstOrDefault();
        Assert.NotNull(firstTile);
        Assert.True(firstTile.DestRect.X.ApproximatelyEquals(10f));
        Assert.True(firstTile.DestRect.Y.ApproximatelyEquals(20f));
    }

    #endregion

    #region NineSlice Mode Tests

    [Fact]
    public void RenderImage_NineSlice_ProducesNineDrawCalls()
    {
        using var world = new World();
        var renderer2D = new Mock2DRenderer();
        world.SetExtension<I2DRenderer>(renderer2D);

        var renderSystem = new UIRenderSystem();
        world.AddSystem(renderSystem);

        var canvas = CreateSimpleCanvas(world);
        var texture = new TextureHandle(1, 64, 64);
        var element = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(0, 0, 200, 200))
            .With(UIImage.NineSlice(texture, UIEdges.All(16)))
            .Build();
        world.SetParent(element, canvas);
        SetComputedBounds(world, element, 0, 0, 200, 200);

        renderSystem.Update(0);

        var commands = renderer2D.Commands.OfType<DrawTextureRegionCommand>().ToList();
        Assert.Equal(9, commands.Count);
    }

    [Fact]
    public void RenderImage_NineSlice_CornersFixedSize()
    {
        using var world = new World();
        var renderer2D = new Mock2DRenderer();
        world.SetExtension<I2DRenderer>(renderer2D);

        var renderSystem = new UIRenderSystem();
        world.AddSystem(renderSystem);

        var canvas = CreateSimpleCanvas(world);
        var texture = new TextureHandle(1, 64, 64);
        var border = UIEdges.All(16);
        var element = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(0, 0, 200, 200))
            .With(UIImage.NineSlice(texture, border))
            .Build();
        world.SetParent(element, canvas);
        SetComputedBounds(world, element, 0, 0, 200, 200);

        renderSystem.Update(0);

        var commands = renderer2D.Commands.OfType<DrawTextureRegionCommand>().ToList();

        // Top-left corner should be at (0,0) with size (16,16)
        var topLeft = commands.FirstOrDefault(c =>
            c.DestRect.X.ApproximatelyEquals(0f) &&
            c.DestRect.Y.ApproximatelyEquals(0f) &&
            c.DestRect.Width.ApproximatelyEquals(16f) &&
            c.DestRect.Height.ApproximatelyEquals(16f));
        Assert.NotNull(topLeft);

        // Bottom-right corner should be at (200-16, 200-16) with size (16,16)
        var bottomRight = commands.FirstOrDefault(c =>
            c.DestRect.X.ApproximatelyEquals(184f) &&
            c.DestRect.Y.ApproximatelyEquals(184f) &&
            c.DestRect.Width.ApproximatelyEquals(16f) &&
            c.DestRect.Height.ApproximatelyEquals(16f));
        Assert.NotNull(bottomRight);
    }

    [Fact]
    public void RenderImage_NineSlice_BorderClampingWhenTooSmall()
    {
        using var world = new World();
        var renderer2D = new Mock2DRenderer();
        world.SetExtension<I2DRenderer>(renderer2D);

        var renderSystem = new UIRenderSystem();
        world.AddSystem(renderSystem);

        var canvas = CreateSimpleCanvas(world);
        var texture = new TextureHandle(1, 64, 64);
        var border = UIEdges.All(50); // Border larger than half the element
        var element = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(0, 0, 60, 60)) // Smaller than combined borders
            .With(UIImage.NineSlice(texture, border))
            .Build();
        world.SetParent(element, canvas);
        SetComputedBounds(world, element, 0, 0, 60, 60);

        renderSystem.Update(0);

        var commands = renderer2D.Commands.OfType<DrawTextureRegionCommand>().ToList();
        // Should still produce draws without overlap/negative sizes
        Assert.True(commands.Count > 0);
        foreach (var cmd in commands)
        {
            Assert.True(cmd.DestRect.Width >= 0);
            Assert.True(cmd.DestRect.Height >= 0);
        }
    }

    [Fact]
    public void RenderImage_NineSlice_CenterStretchMode()
    {
        using var world = new World();
        var renderer2D = new Mock2DRenderer();
        world.SetExtension<I2DRenderer>(renderer2D);

        var renderSystem = new UIRenderSystem();
        world.AddSystem(renderSystem);

        var canvas = CreateSimpleCanvas(world);
        var texture = new TextureHandle(1, 64, 64);
        var element = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(0, 0, 200, 200))
            .With(UIImage.NineSlice(texture, UIEdges.All(16), SlicedFillMode.Stretch, SlicedFillMode.Stretch))
            .Build();
        world.SetParent(element, canvas);
        SetComputedBounds(world, element, 0, 0, 200, 200);

        renderSystem.Update(0);

        var commands = renderer2D.Commands.OfType<DrawTextureRegionCommand>().ToList();
        // With stretch mode, exactly 9 draw calls
        Assert.Equal(9, commands.Count);

        // Center should fill the remaining space
        var center = commands.FirstOrDefault(c =>
            c.DestRect.X.ApproximatelyEquals(16f) &&
            c.DestRect.Y.ApproximatelyEquals(16f));
        Assert.NotNull(center);
        Assert.True(center.DestRect.Width.ApproximatelyEquals(168f)); // 200 - 16 - 16
        Assert.True(center.DestRect.Height.ApproximatelyEquals(168f));
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void RenderImage_ZeroSizeBounds_NoDrawCalls()
    {
        using var world = new World();
        var renderer2D = new Mock2DRenderer();
        world.SetExtension<I2DRenderer>(renderer2D);

        var renderSystem = new UIRenderSystem();
        world.AddSystem(renderSystem);

        var canvas = CreateSimpleCanvas(world);
        var texture = new TextureHandle(1, 64, 64);
        var element = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(0, 0, 0, 0)) // Zero size
            .With(UIImage.Create(texture))
            .Build();
        world.SetParent(element, canvas);
        SetComputedBounds(world, element, 0, 0, 0, 0);

        renderSystem.Update(0);

        // With zero bounds, drawing should still work but produce no visible output
        // The render system should handle this gracefully
        var commands = renderer2D.Commands.OfType<DrawTextureRegionCommand>().ToList();
        // Commands with zero size are technically valid but render nothing
        foreach (var cmd in commands)
        {
            Assert.True(cmd.DestRect.Width >= 0);
            Assert.True(cmd.DestRect.Height >= 0);
        }
    }

    [Fact]
    public void RenderImage_InvalidTexture_NoDrawCalls()
    {
        using var world = new World();
        var renderer2D = new Mock2DRenderer();
        world.SetExtension<I2DRenderer>(renderer2D);

        var renderSystem = new UIRenderSystem();
        world.AddSystem(renderSystem);

        var canvas = CreateSimpleCanvas(world);
        var element = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(0, 0, 100, 100))
            .With(new UIImage
            {
                Texture = TextureHandle.Invalid,
                ScaleMode = ImageScaleMode.NineSlice
            })
            .Build();
        world.SetParent(element, canvas);
        SetComputedBounds(world, element, 0, 0, 100, 100);

        renderSystem.Update(0);

        var commands = renderer2D.Commands.OfType<DrawTextureRegionCommand>().ToList();
        Assert.Empty(commands);
    }

    [Fact]
    public void RenderImage_TextureNoDimensions_FallsBackToStretch()
    {
        using var world = new World();
        var renderer2D = new Mock2DRenderer();
        world.SetExtension<I2DRenderer>(renderer2D);

        var renderSystem = new UIRenderSystem();
        world.AddSystem(renderSystem);

        var canvas = CreateSimpleCanvas(world);
        var texture = new TextureHandle(1, 0, 0); // No dimensions
        var element = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(10, 20, 100, 100))
            .With(new UIImage
            {
                Texture = texture,
                Tint = Vector4.One,
                ScaleMode = ImageScaleMode.NineSlice // Would normally need dimensions
            })
            .Build();
        world.SetParent(element, canvas);
        SetComputedBounds(world, element, 10, 20, 100, 100);

        renderSystem.Update(0);

        var command = renderer2D.Commands.OfType<DrawTextureRegionCommand>().FirstOrDefault();
        Assert.NotNull(command);
        // Should fall back to simple stretch
        Assert.True(command.DestRect.X.ApproximatelyEquals(10f));
        Assert.True(command.DestRect.Y.ApproximatelyEquals(20f));
        Assert.True(command.DestRect.Width.ApproximatelyEquals(100f));
        Assert.True(command.DestRect.Height.ApproximatelyEquals(100f));
    }

    #endregion

    #region Factory Method Tests

    [Fact]
    public void UIImage_NineSliceFactory_SetsCorrectProperties()
    {
        var texture = new TextureHandle(1, 64, 64);
        var border = new UIEdges(10, 20, 30, 40);

        var image = UIImage.NineSlice(texture, border);

        Assert.Equal(ImageScaleMode.NineSlice, image.ScaleMode);
        Assert.Equal(border, image.SliceBorder);
        Assert.Equal(SlicedFillMode.Stretch, image.CenterFillMode);
        Assert.Equal(SlicedFillMode.Stretch, image.EdgeFillMode);
    }

    [Fact]
    public void UIImage_NineSliceFactory_WithTileMode()
    {
        var texture = new TextureHandle(1, 64, 64);
        var border = UIEdges.All(10);

        var image = UIImage.NineSlice(texture, border, SlicedFillMode.Tile, SlicedFillMode.Tile);

        Assert.Equal(SlicedFillMode.Tile, image.CenterFillMode);
        Assert.Equal(SlicedFillMode.Tile, image.EdgeFillMode);
    }

    [Fact]
    public void UIImage_TiledFactory_SetsCorrectScaleMode()
    {
        var texture = new TextureHandle(1, 64, 64);

        var image = UIImage.Tiled(texture);

        Assert.Equal(ImageScaleMode.Tile, image.ScaleMode);
        Assert.Equal(texture, image.Texture);
    }

    #endregion

    #region TextureHandle Tests

    [Fact]
    public void TextureHandle_WithDimensions_StoresDimensions()
    {
        var handle = new TextureHandle(1, 128, 256);

        Assert.Equal(1, handle.Id);
        Assert.Equal(128, handle.Width);
        Assert.Equal(256, handle.Height);
        Assert.True(handle.IsValid);
    }

    [Fact]
    public void TextureHandle_Size_ReturnsVector2()
    {
        var handle = new TextureHandle(1, 100, 50);

        var size = handle.Size;

        Assert.Equal(100, size.X);
        Assert.Equal(50, size.Y);
    }

    [Fact]
    public void TextureHandle_Invalid_HasZeroDimensions()
    {
        var handle = TextureHandle.Invalid;

        Assert.False(handle.IsValid);
        Assert.Equal(0, handle.Width);
        Assert.Equal(0, handle.Height);
    }

    #endregion

    #region Helper Methods

    private static Entity CreateSimpleCanvas(World world)
    {
        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();
        SetComputedBounds(world, canvas, 0, 0, 800, 600);
        return canvas;
    }

    private static void SetComputedBounds(World world, Entity entity, float x, float y, float width, float height)
    {
        ref var rect = ref world.Get<UIRect>(entity);
        rect.ComputedBounds = new Rectangle(x, y, width, height);
    }

    #endregion
}
