using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.Testing.Graphics;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI.Tests;

/// <summary>
/// Tests for the UIRenderSystem rendering functionality.
/// </summary>
public class UIRenderSystemTests
{
    #region Initialization and Batch Management

    [Fact]
    public void RenderSystem_WithNoRenderers_DoesNotThrow()
    {
        using var world = new World();
        var renderSystem = new UIRenderSystem();
        world.AddSystem(renderSystem);

        // Should not throw when renderers are not available
        renderSystem.Update(0);
    }

    [Fact]
    public void RenderSystem_WithRenderer_BeginAndEndCalled()
    {
        using var world = new World();
        var renderer2D = new Mock2DRenderer();
        world.SetExtension<I2DRenderer>(renderer2D);

        var renderSystem = new UIRenderSystem();
        world.AddSystem(renderSystem);

        CreateSimpleCanvas(world);

        renderSystem.Update(0);

        Assert.Equal(1, renderer2D.BeginCount);
        Assert.Equal(1, renderer2D.EndCount);
    }

    [Fact]
    public void RenderSystem_WithTextRenderer_BeginAndEndCalled()
    {
        using var world = new World();
        var renderer2D = new Mock2DRenderer();
        var textRenderer = new MockTextRenderer();
        world.SetExtension<I2DRenderer>(renderer2D);
        world.SetExtension<ITextRenderer>(textRenderer);

        var renderSystem = new UIRenderSystem();
        world.AddSystem(renderSystem);

        CreateSimpleCanvas(world);

        renderSystem.Update(0);

        Assert.Equal(1, textRenderer.BeginCount);
        Assert.Equal(1, textRenderer.EndCount);
    }

    #endregion

    #region Root Entity Processing

    [Fact]
    public void RenderSystem_ProcessesAllRootEntities()
    {
        using var world = new World();
        var renderer2D = new Mock2DRenderer();
        world.SetExtension<I2DRenderer>(renderer2D);

        var renderSystem = new UIRenderSystem();
        world.AddSystem(renderSystem);

        // Create two root canvases
        CreateCanvasWithBackground(world, new Vector4(1, 0, 0, 1));
        CreateCanvasWithBackground(world, new Vector4(0, 1, 0, 1));

        renderSystem.Update(0);

        // Should have rendered both backgrounds
        var fillCommands = renderer2D.Commands.OfType<FillRectCommand>().ToList();
        Assert.True(fillCommands.Count >= 2);
    }

    [Fact]
    public void RenderSystem_SkipsInvisibleRootEntities()
    {
        using var world = new World();
        var renderer2D = new Mock2DRenderer();
        world.SetExtension<I2DRenderer>(renderer2D);

        var renderSystem = new UIRenderSystem();
        world.AddSystem(renderSystem);

        var canvas = CreateCanvasWithBackground(world, new Vector4(1, 0, 0, 1));

        // Make invisible
        ref var element = ref world.Get<UIElement>(canvas);
        element.Visible = false;

        renderSystem.Update(0);

        // Should not have rendered the background
        var fillCommands = renderer2D.Commands.OfType<FillRectCommand>().ToList();
        Assert.Empty(fillCommands);
    }

    [Fact]
    public void RenderSystem_SkipsHiddenRootEntities()
    {
        using var world = new World();
        var renderer2D = new Mock2DRenderer();
        world.SetExtension<I2DRenderer>(renderer2D);

        var renderSystem = new UIRenderSystem();
        world.AddSystem(renderSystem);

        var canvas = CreateCanvasWithBackground(world, new Vector4(1, 0, 0, 1));
        world.Add(canvas, new UIHiddenTag());

        renderSystem.Update(0);

        // Should not have rendered the background
        var fillCommands = renderer2D.Commands.OfType<FillRectCommand>().ToList();
        Assert.Empty(fillCommands);
    }

    #endregion

    #region Style Rendering

    [Fact]
    public void RenderSystem_RendersBackgroundColor()
    {
        using var world = new World();
        var renderer2D = new Mock2DRenderer();
        world.SetExtension<I2DRenderer>(renderer2D);

        var renderSystem = new UIRenderSystem();
        world.AddSystem(renderSystem);

        var backgroundColor = new Vector4(0.5f, 0.3f, 0.8f, 1);
        CreateCanvasWithBackground(world, backgroundColor);

        renderSystem.Update(0);

        var fillCommand = renderer2D.Commands.OfType<FillRectCommand>().FirstOrDefault();
        Assert.NotNull(fillCommand);
        Assert.True(fillCommand.Color.X.ApproximatelyEquals(backgroundColor.X));
        Assert.True(fillCommand.Color.Y.ApproximatelyEquals(backgroundColor.Y));
        Assert.True(fillCommand.Color.Z.ApproximatelyEquals(backgroundColor.Z));
    }

    [Fact]
    public void RenderSystem_RendersRoundedBackground()
    {
        using var world = new World();
        var renderer2D = new Mock2DRenderer();
        world.SetExtension<I2DRenderer>(renderer2D);

        var renderSystem = new UIRenderSystem();
        world.AddSystem(renderSystem);

        var canvas = CreateSimpleCanvas(world);
        var element = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(10, 10, 100, 50))
            .With(new UIStyle
            {
                BackgroundColor = new Vector4(1, 0, 0, 1),
                CornerRadius = 10
            })
            .Build();
        world.SetParent(element, canvas);
        SetComputedBounds(world, element, 10, 10, 100, 50);

        renderSystem.Update(0);

        var roundedCommand = renderer2D.Commands.OfType<FillRoundedRectCommand>().FirstOrDefault();
        Assert.NotNull(roundedCommand);
        Assert.True(roundedCommand.Radius.ApproximatelyEquals(10f));
    }

    [Fact]
    public void RenderSystem_RendersBorder()
    {
        using var world = new World();
        var renderer2D = new Mock2DRenderer();
        world.SetExtension<I2DRenderer>(renderer2D);

        var renderSystem = new UIRenderSystem();
        world.AddSystem(renderSystem);

        var canvas = CreateSimpleCanvas(world);
        var element = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(10, 10, 100, 50))
            .With(new UIStyle
            {
                BorderColor = new Vector4(0, 1, 0, 1),
                BorderWidth = 2
            })
            .Build();
        world.SetParent(element, canvas);
        SetComputedBounds(world, element, 10, 10, 100, 50);

        renderSystem.Update(0);

        var borderCommand = renderer2D.Commands.OfType<DrawRectCommand>().FirstOrDefault();
        Assert.NotNull(borderCommand);
        Assert.True(borderCommand.Thickness.ApproximatelyEquals(2f));
        Assert.True(borderCommand.Color.Y.ApproximatelyEquals(1f));
    }

    [Fact]
    public void RenderSystem_RendersRoundedBorder()
    {
        using var world = new World();
        var renderer2D = new Mock2DRenderer();
        world.SetExtension<I2DRenderer>(renderer2D);

        var renderSystem = new UIRenderSystem();
        world.AddSystem(renderSystem);

        var canvas = CreateSimpleCanvas(world);
        var element = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(10, 10, 100, 50))
            .With(new UIStyle
            {
                BorderColor = new Vector4(0, 1, 0, 1),
                BorderWidth = 2,
                CornerRadius = 8
            })
            .Build();
        world.SetParent(element, canvas);
        SetComputedBounds(world, element, 10, 10, 100, 50);

        renderSystem.Update(0);

        var roundedBorderCommand = renderer2D.Commands.OfType<DrawRoundedRectCommand>().FirstOrDefault();
        Assert.NotNull(roundedBorderCommand);
        Assert.True(roundedBorderCommand.Radius.ApproximatelyEquals(8f));
        Assert.True(roundedBorderCommand.Thickness.ApproximatelyEquals(2f));
    }

    [Fact]
    public void RenderSystem_RendersBackgroundTexture()
    {
        using var world = new World();
        var renderer2D = new Mock2DRenderer();
        world.SetExtension<I2DRenderer>(renderer2D);

        var renderSystem = new UIRenderSystem();
        world.AddSystem(renderSystem);

        var canvas = CreateSimpleCanvas(world);
        var texture = new TextureHandle(1);
        var element = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(10, 10, 100, 50))
            .With(new UIStyle
            {
                BackgroundTexture = texture
            })
            .Build();
        world.SetParent(element, canvas);
        SetComputedBounds(world, element, 10, 10, 100, 50);

        renderSystem.Update(0);

        var textureCommand = renderer2D.Commands.OfType<DrawTextureCommand>().FirstOrDefault();
        Assert.NotNull(textureCommand);
        Assert.Equal(texture, textureCommand.Texture);
    }

    #endregion

    #region Image Rendering

    [Fact]
    public void RenderSystem_RendersImage()
    {
        using var world = new World();
        var renderer2D = new Mock2DRenderer();
        world.SetExtension<I2DRenderer>(renderer2D);

        var renderSystem = new UIRenderSystem();
        world.AddSystem(renderSystem);

        var canvas = CreateSimpleCanvas(world);
        var texture = new TextureHandle(2);
        var tint = new Vector4(0.8f, 0.9f, 1f, 1f);
        var element = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(20, 30, 64, 64))
            .With(new UIImage
            {
                Texture = texture,
                Tint = tint
            })
            .Build();
        world.SetParent(element, canvas);
        SetComputedBounds(world, element, 20, 30, 64, 64);

        renderSystem.Update(0);

        var regionCommand = renderer2D.Commands.OfType<DrawTextureRegionCommand>().FirstOrDefault();
        Assert.NotNull(regionCommand);
        Assert.Equal(texture, regionCommand.Texture);
        Assert.True(regionCommand.Tint.X.ApproximatelyEquals(tint.X));
    }

    [Fact]
    public void RenderSystem_RendersImageWithSourceRect()
    {
        using var world = new World();
        var renderer2D = new Mock2DRenderer();
        world.SetExtension<I2DRenderer>(renderer2D);

        var renderSystem = new UIRenderSystem();
        world.AddSystem(renderSystem);

        var canvas = CreateSimpleCanvas(world);
        var texture = new TextureHandle(3);
        var sourceRect = new Rectangle(0.25f, 0.25f, 0.5f, 0.5f);
        var element = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(20, 30, 64, 64))
            .With(new UIImage
            {
                Texture = texture,
                SourceRect = sourceRect
            })
            .Build();
        world.SetParent(element, canvas);
        SetComputedBounds(world, element, 20, 30, 64, 64);

        renderSystem.Update(0);

        var regionCommand = renderer2D.Commands.OfType<DrawTextureRegionCommand>().FirstOrDefault();
        Assert.NotNull(regionCommand);
        Assert.True(regionCommand.SourceRect.X.ApproximatelyEquals(sourceRect.X));
        Assert.True(regionCommand.SourceRect.Width.ApproximatelyEquals(sourceRect.Width));
    }

    [Fact]
    public void RenderSystem_SkipsImageWithInvalidTexture()
    {
        using var world = new World();
        var renderer2D = new Mock2DRenderer();
        world.SetExtension<I2DRenderer>(renderer2D);

        var renderSystem = new UIRenderSystem();
        world.AddSystem(renderSystem);

        var canvas = CreateSimpleCanvas(world);
        var element = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(20, 30, 64, 64))
            .With(new UIImage
            {
                Texture = TextureHandle.Invalid
            })
            .Build();
        world.SetParent(element, canvas);
        SetComputedBounds(world, element, 20, 30, 64, 64);

        renderSystem.Update(0);

        var imageCommands = renderer2D.Commands.OfType<DrawTextureRegionCommand>().ToList();
        Assert.Empty(imageCommands);
    }

    #endregion

    #region Text Rendering

    [Fact]
    public void RenderSystem_RendersText()
    {
        using var world = new World();
        var renderer2D = new Mock2DRenderer();
        var textRenderer = new MockTextRenderer();
        world.SetExtension<I2DRenderer>(renderer2D);
        world.SetExtension<ITextRenderer>(textRenderer);

        var renderSystem = new UIRenderSystem();
        world.AddSystem(renderSystem);

        var canvas = CreateSimpleCanvas(world);
        var font = new FontHandle(1);
        var textContent = "Hello World";
        var textColor = new Vector4(1, 1, 1, 1);
        var element = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(10, 10, 200, 40))
            .With(new UIText
            {
                Content = textContent,
                Font = font,
                Color = textColor,
                HorizontalAlign = TextAlignH.Center,
                VerticalAlign = TextAlignV.Middle
            })
            .Build();
        world.SetParent(element, canvas);
        SetComputedBounds(world, element, 10, 10, 200, 40);

        renderSystem.Update(0);

        var textCommand = textRenderer.Commands.OfType<DrawTextCommand>().FirstOrDefault();
        Assert.NotNull(textCommand);
        Assert.Equal(textContent, textCommand.Text);
        Assert.Equal(font, textCommand.Font);
        Assert.Equal(TextAlignH.Center, textCommand.AlignH);
        Assert.Equal(TextAlignV.Middle, textCommand.AlignV);
    }

    [Fact]
    public void RenderSystem_RendersWrappedText()
    {
        using var world = new World();
        var renderer2D = new Mock2DRenderer();
        var textRenderer = new MockTextRenderer();
        world.SetExtension<I2DRenderer>(renderer2D);
        world.SetExtension<ITextRenderer>(textRenderer);

        var renderSystem = new UIRenderSystem();
        world.AddSystem(renderSystem);

        var canvas = CreateSimpleCanvas(world);
        var font = new FontHandle(1);
        var textContent = "This is a long text that should wrap";
        var element = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(10, 10, 200, 100))
            .With(new UIText
            {
                Content = textContent,
                Font = font,
                Color = new Vector4(1, 1, 1, 1),
                WordWrap = true
            })
            .Build();
        world.SetParent(element, canvas);
        SetComputedBounds(world, element, 10, 10, 200, 100);

        renderSystem.Update(0);

        var wrappedCommand = textRenderer.Commands.OfType<DrawTextWrappedCommand>().FirstOrDefault();
        Assert.NotNull(wrappedCommand);
        Assert.Equal(textContent, wrappedCommand.Text);
    }

    [Fact]
    public void RenderSystem_SkipsEmptyText()
    {
        using var world = new World();
        var renderer2D = new Mock2DRenderer();
        var textRenderer = new MockTextRenderer();
        world.SetExtension<I2DRenderer>(renderer2D);
        world.SetExtension<ITextRenderer>(textRenderer);

        var renderSystem = new UIRenderSystem();
        world.AddSystem(renderSystem);

        var canvas = CreateSimpleCanvas(world);
        var element = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(10, 10, 200, 40))
            .With(new UIText
            {
                Content = "",
                Font = new FontHandle(1),
                Color = new Vector4(1, 1, 1, 1)
            })
            .Build();
        world.SetParent(element, canvas);
        SetComputedBounds(world, element, 10, 10, 200, 40);

        renderSystem.Update(0);

        Assert.Empty(textRenderer.Commands);
    }

    [Fact]
    public void RenderSystem_FlushesBeforeAndAfterText()
    {
        using var world = new World();
        var renderer2D = new Mock2DRenderer();
        var textRenderer = new MockTextRenderer();
        world.SetExtension<I2DRenderer>(renderer2D);
        world.SetExtension<ITextRenderer>(textRenderer);

        var renderSystem = new UIRenderSystem();
        world.AddSystem(renderSystem);

        var canvas = CreateSimpleCanvas(world);
        var element = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(10, 10, 200, 40))
            .With(new UIText
            {
                Content = "Test",
                Font = new FontHandle(1),
                Color = new Vector4(1, 1, 1, 1)
            })
            .Build();
        world.SetParent(element, canvas);
        SetComputedBounds(world, element, 10, 10, 200, 40);

        renderSystem.Update(0);

        // Should have flushed 2D renderer before text, and text renderer after
        Assert.True(renderer2D.FlushCount >= 1);
        Assert.True(textRenderer.FlushCount >= 1);
    }

    #endregion

    #region Interaction State Rendering

    [Fact]
    public void RenderSystem_RendersHoverOverlay()
    {
        using var world = new World();
        var renderer2D = new Mock2DRenderer();
        world.SetExtension<I2DRenderer>(renderer2D);

        var renderSystem = new UIRenderSystem();
        world.AddSystem(renderSystem);

        var canvas = CreateSimpleCanvas(world);
        var element = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(10, 10, 100, 50))
            .With(new UIInteractable { State = UIInteractionState.Hovered })
            .Build();
        world.SetParent(element, canvas);
        SetComputedBounds(world, element, 10, 10, 100, 50);

        renderSystem.Update(0);

        // Should have a light overlay for hover
        var overlays = renderer2D.Commands.OfType<FillRectCommand>()
            .Where(cmd => cmd.Color.W.ApproximatelyEquals(0.1f))
            .ToList();
        Assert.NotEmpty(overlays);
    }

    [Fact]
    public void RenderSystem_RendersPressedOverlay()
    {
        using var world = new World();
        var renderer2D = new Mock2DRenderer();
        world.SetExtension<I2DRenderer>(renderer2D);

        var renderSystem = new UIRenderSystem();
        world.AddSystem(renderSystem);

        var canvas = CreateSimpleCanvas(world);
        var element = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(10, 10, 100, 50))
            .With(new UIInteractable { State = UIInteractionState.Pressed })
            .Build();
        world.SetParent(element, canvas);
        SetComputedBounds(world, element, 10, 10, 100, 50);

        renderSystem.Update(0);

        // Should have a dark overlay for pressed
        var overlays = renderer2D.Commands.OfType<FillRectCommand>()
            .Where(cmd => cmd.Color.W.ApproximatelyEquals(0.2f))
            .ToList();
        Assert.NotEmpty(overlays);
    }

    #endregion

    #region Focus Indicator Rendering

    [Fact]
    public void RenderSystem_RendersFocusIndicator()
    {
        using var world = new World();
        var renderer2D = new Mock2DRenderer();
        world.SetExtension<I2DRenderer>(renderer2D);

        var renderSystem = new UIRenderSystem();
        world.AddSystem(renderSystem);

        var canvas = CreateSimpleCanvas(world);
        var element = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(10, 10, 100, 50))
            .With(new UIFocusedTag())
            .Build();
        world.SetParent(element, canvas);
        SetComputedBounds(world, element, 10, 10, 100, 50);

        renderSystem.Update(0);

        // Should have a focus outline (slightly larger than the element)
        var focusOutlines = renderer2D.Commands.OfType<DrawRectCommand>()
            .Where(cmd => cmd.Thickness.ApproximatelyEquals(2f))
            .ToList();
        Assert.NotEmpty(focusOutlines);
    }

    #endregion

    #region Clipping

    [Fact]
    public void RenderSystem_PushesClipForClippedElement()
    {
        using var world = new World();
        var renderer2D = new Mock2DRenderer();
        world.SetExtension<I2DRenderer>(renderer2D);

        var renderSystem = new UIRenderSystem();
        world.AddSystem(renderSystem);

        var canvas = CreateSimpleCanvas(world);
        var clipParent = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(10, 10, 100, 50))
            .With(new UIClipChildrenTag())
            .Build();
        world.SetParent(clipParent, canvas);
        SetComputedBounds(world, clipParent, 10, 10, 100, 50);

        renderSystem.Update(0);

        var pushClips = renderer2D.Commands.OfType<PushClipCommand>().ToList();
        var popClips = renderer2D.Commands.OfType<PopClipCommand>().ToList();

        Assert.NotEmpty(pushClips);
        Assert.NotEmpty(popClips);
        Assert.Equal(pushClips.Count, popClips.Count);
    }

    [Fact]
    public void RenderSystem_ClipsChildrenCorrectly()
    {
        using var world = new World();
        var renderer2D = new Mock2DRenderer();
        world.SetExtension<I2DRenderer>(renderer2D);

        var renderSystem = new UIRenderSystem();
        world.AddSystem(renderSystem);

        var canvas = CreateSimpleCanvas(world);
        var clipParent = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(10, 10, 100, 50))
            .With(new UIStyle { BackgroundColor = new Vector4(1, 0, 0, 1) })
            .With(new UIClipChildrenTag())
            .Build();
        world.SetParent(clipParent, canvas);
        SetComputedBounds(world, clipParent, 10, 10, 100, 50);

        var child = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(50, 50, 100, 100))
            .With(new UIStyle { BackgroundColor = new Vector4(0, 1, 0, 1) })
            .Build();
        world.SetParent(child, clipParent);
        SetComputedBounds(world, child, 50, 50, 100, 100);

        renderSystem.Update(0);

        var commands = renderer2D.Commands;
        var pushClipIndex = commands.FindIndex(c => c is PushClipCommand);
        var childRenderIndex = commands.FindIndex(pushClipIndex, c => c is FillRectCommand fr && fr.Color.Y.ApproximatelyEquals(1f));
        var popClipIndex = commands.FindIndex(c => c is PopClipCommand);

        // Verify order: PushClip -> Child Render -> PopClip
        Assert.True(pushClipIndex < childRenderIndex);
        Assert.True(childRenderIndex < popClipIndex);
    }

    #endregion

    #region Child Rendering

    [Fact]
    public void RenderSystem_RendersChildrenRecursively()
    {
        using var world = new World();
        var renderer2D = new Mock2DRenderer();
        world.SetExtension<I2DRenderer>(renderer2D);

        var renderSystem = new UIRenderSystem();
        world.AddSystem(renderSystem);

        var canvas = CreateSimpleCanvas(world);
        var parent = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(10, 10, 200, 100))
            .With(new UIStyle { BackgroundColor = new Vector4(1, 0, 0, 1) })
            .Build();
        world.SetParent(parent, canvas);
        SetComputedBounds(world, parent, 10, 10, 200, 100);

        var child = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(20, 20, 50, 50))
            .With(new UIStyle { BackgroundColor = new Vector4(0, 1, 0, 1) })
            .Build();
        world.SetParent(child, parent);
        SetComputedBounds(world, child, 20, 20, 50, 50);

        renderSystem.Update(0);

        var fillCommands = renderer2D.Commands.OfType<FillRectCommand>().ToList();
        Assert.True(fillCommands.Count >= 2); // Parent and child backgrounds
    }

    [Fact]
    public void RenderSystem_SkipsInvisibleChildren()
    {
        using var world = new World();
        var renderer2D = new Mock2DRenderer();
        world.SetExtension<I2DRenderer>(renderer2D);

        var renderSystem = new UIRenderSystem();
        world.AddSystem(renderSystem);

        var canvas = CreateSimpleCanvas(world);
        var parent = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(10, 10, 200, 100))
            .With(new UIStyle { BackgroundColor = new Vector4(1, 0, 0, 1) })
            .Build();
        world.SetParent(parent, canvas);
        SetComputedBounds(world, parent, 10, 10, 200, 100);

        var child = world.Spawn()
            .With(new UIElement { Visible = false })
            .With(UIRect.Fixed(20, 20, 50, 50))
            .With(new UIStyle { BackgroundColor = new Vector4(0, 1, 0, 1) })
            .Build();
        world.SetParent(child, parent);
        SetComputedBounds(world, child, 20, 20, 50, 50);

        renderSystem.Update(0);

        // Should only have parent background (red), not child (green)
        var fillCommands = renderer2D.Commands.OfType<FillRectCommand>().ToList();
        Assert.DoesNotContain(fillCommands, cmd => cmd.Color.Y.ApproximatelyEquals(1f));
    }

    [Fact]
    public void RenderSystem_SkipsHiddenChildren()
    {
        using var world = new World();
        var renderer2D = new Mock2DRenderer();
        world.SetExtension<I2DRenderer>(renderer2D);

        var renderSystem = new UIRenderSystem();
        world.AddSystem(renderSystem);

        var canvas = CreateSimpleCanvas(world);
        var parent = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(10, 10, 200, 100))
            .With(new UIStyle { BackgroundColor = new Vector4(1, 0, 0, 1) })
            .Build();
        world.SetParent(parent, canvas);
        SetComputedBounds(world, parent, 10, 10, 200, 100);

        var child = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(20, 20, 50, 50))
            .With(new UIStyle { BackgroundColor = new Vector4(0, 1, 0, 1) })
            .With(new UIHiddenTag())
            .Build();
        world.SetParent(child, parent);
        SetComputedBounds(world, child, 20, 20, 50, 50);

        renderSystem.Update(0);

        // Should only have parent background (red), not child (green)
        var fillCommands = renderer2D.Commands.OfType<FillRectCommand>().ToList();
        Assert.DoesNotContain(fillCommands, cmd => cmd.Color.Y.ApproximatelyEquals(1f));
    }

    [Fact]
    public void RenderSystem_SkipsChildrenWithoutUIComponents()
    {
        using var world = new World();
        var renderer2D = new Mock2DRenderer();
        world.SetExtension<I2DRenderer>(renderer2D);

        var renderSystem = new UIRenderSystem();
        world.AddSystem(renderSystem);

        var canvas = CreateSimpleCanvas(world);
        var parent = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(10, 10, 200, 100))
            .Build();
        world.SetParent(parent, canvas);
        SetComputedBounds(world, parent, 10, 10, 200, 100);

        // Create a non-UI child entity
        var nonUIChild = world.Spawn().Build();
        world.SetParent(nonUIChild, parent);

        // Should not throw
        renderSystem.Update(0);
    }

    #endregion

    #region Renderer Provider Fallback

    [Fact]
    public void RenderSystem_WithRendererProvider_UsesProvider()
    {
        using var world = new World();
        var renderer2D = new Mock2DRenderer();
        var provider = new MockRendererProvider(renderer2D);
        world.SetExtension<I2DRendererProvider>(provider);

        var renderSystem = new UIRenderSystem();
        world.AddSystem(renderSystem);

        CreateSimpleCanvas(world);

        renderSystem.Update(0);

        Assert.Equal(1, renderer2D.BeginCount);
        Assert.Equal(1, renderer2D.EndCount);
    }

    [Fact]
    public void RenderSystem_WithTextRendererProvider_UsesProvider()
    {
        using var world = new World();
        var renderer2D = new Mock2DRenderer();
        var textRenderer = new MockTextRenderer();
        var textProvider = new MockTextRendererProvider(textRenderer);
        world.SetExtension<I2DRenderer>(renderer2D);
        world.SetExtension<ITextRendererProvider>(textProvider);

        var renderSystem = new UIRenderSystem();
        world.AddSystem(renderSystem);

        var canvas = CreateSimpleCanvas(world);
        var element = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(10, 10, 200, 40))
            .With(new UIText
            {
                Content = "Test",
                Font = new FontHandle(1),
                Color = new Vector4(1, 1, 1, 1)
            })
            .Build();
        world.SetParent(element, canvas);
        SetComputedBounds(world, element, 10, 10, 200, 40);

        renderSystem.Update(0);

        Assert.Equal(1, textRenderer.BeginCount);
    }

    #endregion

    #region Scrollable Elements

    [Fact]
    public void RenderSystem_WithScrollableElement_ReadsScrollPosition()
    {
        using var world = new World();
        var renderer2D = new Mock2DRenderer();
        world.SetExtension<I2DRenderer>(renderer2D);

        var renderSystem = new UIRenderSystem();
        world.AddSystem(renderSystem);

        var canvas = CreateSimpleCanvas(world);
        var scrollable = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(10, 10, 200, 100))
            .With(new UIScrollable { ScrollPosition = new Vector2(50, 100) })
            .With(new UIStyle { BackgroundColor = new Vector4(1, 0, 0, 1) })
            .Build();
        world.SetParent(scrollable, canvas);
        SetComputedBounds(world, scrollable, 10, 10, 200, 100);

        // Should not throw and should render normally
        renderSystem.Update(0);

        var fillCommands = renderer2D.Commands.OfType<FillRectCommand>().ToList();
        Assert.NotEmpty(fillCommands);
    }

    #endregion

    #region Style Edge Cases

    [Fact]
    public void RenderSystem_WithTransparentBackground_SkipsBackgroundRender()
    {
        using var world = new World();
        var renderer2D = new Mock2DRenderer();
        world.SetExtension<I2DRenderer>(renderer2D);

        var renderSystem = new UIRenderSystem();
        world.AddSystem(renderSystem);

        var canvas = CreateSimpleCanvas(world);
        var element = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(10, 10, 100, 50))
            .With(new UIStyle
            {
                BackgroundColor = new Vector4(1, 0, 0, 0) // Alpha = 0
            })
            .Build();
        world.SetParent(element, canvas);
        SetComputedBounds(world, element, 10, 10, 100, 50);

        renderSystem.Update(0);

        // Should not have any fill commands for the transparent element
        var fillCommands = renderer2D.Commands.OfType<FillRectCommand>().ToList();
        Assert.Empty(fillCommands);
    }

    [Fact]
    public void RenderSystem_WithTransparentBorder_SkipsBorderRender()
    {
        using var world = new World();
        var renderer2D = new Mock2DRenderer();
        world.SetExtension<I2DRenderer>(renderer2D);

        var renderSystem = new UIRenderSystem();
        world.AddSystem(renderSystem);

        var canvas = CreateSimpleCanvas(world);
        var element = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(10, 10, 100, 50))
            .With(new UIStyle
            {
                BorderColor = new Vector4(0, 1, 0, 0), // Alpha = 0
                BorderWidth = 2
            })
            .Build();
        world.SetParent(element, canvas);
        SetComputedBounds(world, element, 10, 10, 100, 50);

        renderSystem.Update(0);

        // Should not have any border commands
        var borderCommands = renderer2D.Commands.OfType<DrawRectCommand>().ToList();
        Assert.Empty(borderCommands);
    }

    [Fact]
    public void RenderSystem_WithZeroBorderWidth_SkipsBorderRender()
    {
        using var world = new World();
        var renderer2D = new Mock2DRenderer();
        world.SetExtension<I2DRenderer>(renderer2D);

        var renderSystem = new UIRenderSystem();
        world.AddSystem(renderSystem);

        var canvas = CreateSimpleCanvas(world);
        var element = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(10, 10, 100, 50))
            .With(new UIStyle
            {
                BorderColor = new Vector4(0, 1, 0, 1),
                BorderWidth = 0 // Zero width
            })
            .Build();
        world.SetParent(element, canvas);
        SetComputedBounds(world, element, 10, 10, 100, 50);

        renderSystem.Update(0);

        // Should not have any border commands
        var borderCommands = renderer2D.Commands.OfType<DrawRectCommand>().ToList();
        Assert.Empty(borderCommands);
    }

    #endregion

    #region Image Edge Cases

    [Fact]
    public void RenderSystem_WithImageZeroSourceRect_UsesNormalizedDefault()
    {
        using var world = new World();
        var renderer2D = new Mock2DRenderer();
        world.SetExtension<I2DRenderer>(renderer2D);

        var renderSystem = new UIRenderSystem();
        world.AddSystem(renderSystem);

        var canvas = CreateSimpleCanvas(world);
        var texture = new TextureHandle(2);
        var element = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(20, 30, 64, 64))
            .With(new UIImage
            {
                Texture = texture,
                SourceRect = new Rectangle(0, 0, 0, 0) // Zero dimensions
            })
            .Build();
        world.SetParent(element, canvas);
        SetComputedBounds(world, element, 20, 30, 64, 64);

        renderSystem.Update(0);

        var regionCommand = renderer2D.Commands.OfType<DrawTextureRegionCommand>().FirstOrDefault();
        Assert.NotNull(regionCommand);
        // Should use normalized (0,0,1,1) rect
        Assert.True(regionCommand.SourceRect.Width.ApproximatelyEquals(1f));
        Assert.True(regionCommand.SourceRect.Height.ApproximatelyEquals(1f));
    }

    [Fact]
    public void RenderSystem_WithImageNegativeSourceRect_UsesNormalizedDefault()
    {
        using var world = new World();
        var renderer2D = new Mock2DRenderer();
        world.SetExtension<I2DRenderer>(renderer2D);

        var renderSystem = new UIRenderSystem();
        world.AddSystem(renderSystem);

        var canvas = CreateSimpleCanvas(world);
        var texture = new TextureHandle(2);
        var element = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(20, 30, 64, 64))
            .With(new UIImage
            {
                Texture = texture,
                SourceRect = new Rectangle(0, 0, -10, -10) // Negative dimensions
            })
            .Build();
        world.SetParent(element, canvas);
        SetComputedBounds(world, element, 20, 30, 64, 64);

        renderSystem.Update(0);

        var regionCommand = renderer2D.Commands.OfType<DrawTextureRegionCommand>().FirstOrDefault();
        Assert.NotNull(regionCommand);
        // Should use normalized (0,0,1,1) rect
        Assert.True(regionCommand.SourceRect.Width.ApproximatelyEquals(1f));
        Assert.True(regionCommand.SourceRect.Height.ApproximatelyEquals(1f));
    }

    #endregion

    #region Text Alignment Edge Cases

    [Fact]
    public void RenderSystem_WithTextLeftAlignment_PositionsCorrectly()
    {
        using var world = new World();
        var renderer2D = new Mock2DRenderer();
        var textRenderer = new MockTextRenderer();
        world.SetExtension<I2DRenderer>(renderer2D);
        world.SetExtension<ITextRenderer>(textRenderer);

        var renderSystem = new UIRenderSystem();
        world.AddSystem(renderSystem);

        var canvas = CreateSimpleCanvas(world);
        var element = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(10, 10, 200, 40))
            .With(new UIText
            {
                Content = "Test",
                Font = new FontHandle(1),
                Color = new Vector4(1, 1, 1, 1),
                HorizontalAlign = TextAlignH.Left,
                VerticalAlign = TextAlignV.Top
            })
            .Build();
        world.SetParent(element, canvas);
        SetComputedBounds(world, element, 10, 10, 200, 40);

        renderSystem.Update(0);

        var textCommand = textRenderer.Commands.OfType<DrawTextCommand>().FirstOrDefault();
        Assert.NotNull(textCommand);
        Assert.Equal(TextAlignH.Left, textCommand.AlignH);
        Assert.Equal(TextAlignV.Top, textCommand.AlignV);
        // For left/top alignment, Position should be at bounds origin
        Assert.True(textCommand.Position.X.ApproximatelyEquals(10f));
        Assert.True(textCommand.Position.Y.ApproximatelyEquals(10f));
    }

    [Fact]
    public void RenderSystem_WithTextRightAlignment_PositionsCorrectly()
    {
        using var world = new World();
        var renderer2D = new Mock2DRenderer();
        var textRenderer = new MockTextRenderer();
        world.SetExtension<I2DRenderer>(renderer2D);
        world.SetExtension<ITextRenderer>(textRenderer);

        var renderSystem = new UIRenderSystem();
        world.AddSystem(renderSystem);

        var canvas = CreateSimpleCanvas(world);
        var element = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(10, 10, 200, 40))
            .With(new UIText
            {
                Content = "Test",
                Font = new FontHandle(1),
                Color = new Vector4(1, 1, 1, 1),
                HorizontalAlign = TextAlignH.Right,
                VerticalAlign = TextAlignV.Bottom
            })
            .Build();
        world.SetParent(element, canvas);
        SetComputedBounds(world, element, 10, 10, 200, 40);

        renderSystem.Update(0);

        var textCommand = textRenderer.Commands.OfType<DrawTextCommand>().FirstOrDefault();
        Assert.NotNull(textCommand);
        Assert.Equal(TextAlignH.Right, textCommand.AlignH);
        Assert.Equal(TextAlignV.Bottom, textCommand.AlignV);
        // For right/bottom alignment, Position.X = bounds.X + bounds.Width, Position.Y = bounds.Y + bounds.Height
        Assert.True(textCommand.Position.X.ApproximatelyEquals(210f)); // 10 + 200
        Assert.True(textCommand.Position.Y.ApproximatelyEquals(50f));  // 10 + 40
    }

    [Fact]
    public void RenderSystem_SkipsNullTextContent()
    {
        using var world = new World();
        var renderer2D = new Mock2DRenderer();
        var textRenderer = new MockTextRenderer();
        world.SetExtension<I2DRenderer>(renderer2D);
        world.SetExtension<ITextRenderer>(textRenderer);

        var renderSystem = new UIRenderSystem();
        world.AddSystem(renderSystem);

        var canvas = CreateSimpleCanvas(world);
        var element = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(10, 10, 200, 40))
            .With(new UIText
            {
                Content = null!,
                Font = new FontHandle(1),
                Color = new Vector4(1, 1, 1, 1)
            })
            .Build();
        world.SetParent(element, canvas);
        SetComputedBounds(world, element, 10, 10, 200, 40);

        renderSystem.Update(0);

        Assert.Empty(textRenderer.Commands);
    }

    #endregion

    #region Interaction State Edge Cases

    [Fact]
    public void RenderSystem_WithInteractableNormalState_NoOverlay()
    {
        using var world = new World();
        var renderer2D = new Mock2DRenderer();
        world.SetExtension<I2DRenderer>(renderer2D);

        var renderSystem = new UIRenderSystem();
        world.AddSystem(renderSystem);

        var canvas = CreateSimpleCanvas(world);
        var element = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Fixed(10, 10, 100, 50))
            .With(new UIInteractable { State = UIInteractionState.Normal })
            .Build();
        world.SetParent(element, canvas);
        SetComputedBounds(world, element, 10, 10, 100, 50);

        renderSystem.Update(0);

        // Should not have any overlay (0.1f or 0.2f alpha)
        var overlays = renderer2D.Commands.OfType<FillRectCommand>()
            .Where(cmd => cmd.Color.W.ApproximatelyEquals(0.1f) || cmd.Color.W.ApproximatelyEquals(0.2f))
            .ToList();
        Assert.Empty(overlays);
    }

    #endregion

    #region Multiple Update Cycles

    [Fact]
    public void RenderSystem_MultipleUpdates_ReinitializesOnceOnly()
    {
        using var world = new World();
        var renderer2D = new Mock2DRenderer();
        world.SetExtension<I2DRenderer>(renderer2D);

        var renderSystem = new UIRenderSystem();
        world.AddSystem(renderSystem);

        CreateSimpleCanvas(world);

        // Multiple updates
        renderSystem.Update(0);
        renderSystem.Update(0);
        renderSystem.Update(0);

        // Begin/End should be called each time
        Assert.Equal(3, renderer2D.BeginCount);
        Assert.Equal(3, renderer2D.EndCount);
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

    private static Entity CreateCanvasWithBackground(World world, Vector4 backgroundColor)
    {
        var canvas = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIStyle { BackgroundColor = backgroundColor })
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

/// <summary>
/// Mock renderer provider for testing fallback initialization.
/// </summary>
internal sealed class MockRendererProvider(I2DRenderer renderer) : I2DRendererProvider
{
    public I2DRenderer Get2DRenderer() => renderer;
}

/// <summary>
/// Mock text renderer provider for testing fallback initialization.
/// </summary>
internal sealed class MockTextRendererProvider(ITextRenderer renderer) : ITextRendererProvider
{
    public ITextRenderer GetTextRenderer() => renderer;
}
