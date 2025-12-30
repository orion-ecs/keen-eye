using System.Numerics;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.Testing.Graphics;

namespace KeenEyes.Testing.Tests.Graphics;

#region MockGraphicsContext Assertions

public class GraphicsAssertionsContextTests
{
    [Fact]
    public void ShouldHaveDrawnMesh_WhenMeshDrawn_Passes()
    {
        using var context = new MockGraphicsContext();
        var mesh = context.CreateCube();
        context.DrawMesh(mesh);

        var result = context.ShouldHaveDrawnMesh(mesh);

        Assert.Same(context, result);
    }

    [Fact]
    public void ShouldHaveDrawnMesh_WhenMeshNotDrawn_Throws()
    {
        using var context = new MockGraphicsContext();
        var mesh = context.CreateCube();
        // Don't draw it

        var ex = Assert.Throws<AssertionException>(() => context.ShouldHaveDrawnMesh(mesh));
        Assert.Contains("not", ex.Message);
    }

    [Fact]
    public void ShouldHaveDrawnMeshTimes_WhenCountMatches_Passes()
    {
        using var context = new MockGraphicsContext();
        var mesh = context.CreateCube();
        context.DrawMesh(mesh);
        context.DrawMesh(mesh);
        context.DrawMesh(mesh);

        var result = context.ShouldHaveDrawnMeshTimes(mesh, 3);

        Assert.Same(context, result);
    }

    [Fact]
    public void ShouldHaveDrawnMeshTimes_WhenCountDoesNotMatch_Throws()
    {
        using var context = new MockGraphicsContext();
        var mesh = context.CreateCube();
        context.DrawMesh(mesh);

        var ex = Assert.Throws<AssertionException>(() => context.ShouldHaveDrawnMeshTimes(mesh, 5));
        Assert.Contains("5 times", ex.Message);
    }

    [Fact]
    public void ShouldHaveCreatedTexture_WhenTextureCreated_Passes()
    {
        using var context = new MockGraphicsContext();
        context.CreateTexture(32, 32, new byte[32 * 32 * 4]);

        var result = context.ShouldHaveCreatedTexture();

        Assert.Same(context, result);
    }

    [Fact]
    public void ShouldHaveCreatedTexture_WhenNoTextureCreated_Throws()
    {
        using var context = new MockGraphicsContext();
        // Only default WhiteTexture exists

        var ex = Assert.Throws<AssertionException>(() => context.ShouldHaveCreatedTexture());
        Assert.Contains("texture", ex.Message.ToLower());
    }

    [Fact]
    public void ShouldHaveBoundShader_WhenCorrectShaderBound_Passes()
    {
        using var context = new MockGraphicsContext();
        var shader = context.CreateShader("vertex", "fragment");
        context.BindShader(shader);

        var result = context.ShouldHaveBoundShader(shader);

        Assert.Same(context, result);
    }

    [Fact]
    public void ShouldHaveBoundShader_WhenWrongShaderBound_Throws()
    {
        using var context = new MockGraphicsContext();
        var shader1 = context.CreateShader("v1", "f1");
        var shader2 = context.CreateShader("v2", "f2");
        context.BindShader(shader1);

        var ex = Assert.Throws<AssertionException>(() => context.ShouldHaveBoundShader(shader2));
        Assert.Contains("bound", ex.Message);
    }

    [Fact]
    public void ShouldHaveUniform_WhenUniformSet_Passes()
    {
        using var context = new MockGraphicsContext();
        context.SetUniform("time", 1.5f);

        var result = context.ShouldHaveUniform("time", 1.5f);

        Assert.Same(context, result);
    }

    [Fact]
    public void ShouldHaveUniform_WhenUniformNotSet_Throws()
    {
        using var context = new MockGraphicsContext();

        var ex = Assert.Throws<AssertionException>(() => context.ShouldHaveUniform("unknown", 1.0f));
        Assert.Contains("not", ex.Message);
    }

    [Fact]
    public void ShouldHaveUniform_WhenWrongValue_Throws()
    {
        using var context = new MockGraphicsContext();
        context.SetUniform("time", 1.0f);

        var ex = Assert.Throws<AssertionException>(() => context.ShouldHaveUniform("time", 2.0f));
        Assert.Contains("2", ex.Message);
    }
}

#endregion

#region Mock2DRenderer Assertions

public class GraphicsAssertions2DRendererTests
{
    [Fact]
    public void ShouldHaveFilledRect_WhenRectFilled_Passes()
    {
        using var renderer = new Mock2DRenderer();
        renderer.FillRect(new Rectangle(10, 20, 100, 50), new Vector4(1, 0, 0, 1));

        var result = renderer.ShouldHaveFilledRect();

        Assert.Same(renderer, result);
    }

    [Fact]
    public void ShouldHaveFilledRect_WhenNoRectFilled_Throws()
    {
        using var renderer = new Mock2DRenderer();

        var ex = Assert.Throws<AssertionException>(() => renderer.ShouldHaveFilledRect());
        Assert.Contains("FillRect", ex.Message);
    }

    [Fact]
    public void ShouldHaveFilledRect_WithSpecificRect_Passes()
    {
        using var renderer = new Mock2DRenderer();
        var rect = new Rectangle(10, 20, 100, 50);
        renderer.FillRect(rect, new Vector4(1, 0, 0, 1));

        var result = renderer.ShouldHaveFilledRect(rect);

        Assert.Same(renderer, result);
    }

    [Fact]
    public void ShouldHaveFilledRect_WithWrongRect_Throws()
    {
        using var renderer = new Mock2DRenderer();
        renderer.FillRect(new Rectangle(10, 20, 100, 50), new Vector4(1, 0, 0, 1));

        var ex = Assert.Throws<AssertionException>(() =>
            renderer.ShouldHaveFilledRect(new Rectangle(0, 0, 50, 50)));
        Assert.Contains("no matching", ex.Message);
    }

    [Fact]
    public void ShouldHaveDrawnRect_WhenRectDrawn_Passes()
    {
        using var renderer = new Mock2DRenderer();
        renderer.DrawRect(new Rectangle(10, 20, 100, 50), new Vector4(1, 0, 0, 1));

        var result = renderer.ShouldHaveDrawnRect();

        Assert.Same(renderer, result);
    }

    [Fact]
    public void ShouldHaveDrawnRect_WhenNoRectDrawn_Throws()
    {
        using var renderer = new Mock2DRenderer();

        var ex = Assert.Throws<AssertionException>(() => renderer.ShouldHaveDrawnRect());
        Assert.Contains("DrawRect", ex.Message);
    }

    [Fact]
    public void ShouldHaveDrawnLine_WhenLineDrawn_Passes()
    {
        using var renderer = new Mock2DRenderer();
        renderer.DrawLine(0, 0, 100, 100, new Vector4(1, 1, 1, 1));

        var result = renderer.ShouldHaveDrawnLine();

        Assert.Same(renderer, result);
    }

    [Fact]
    public void ShouldHaveDrawnLine_WhenNoLineDrawn_Throws()
    {
        using var renderer = new Mock2DRenderer();

        var ex = Assert.Throws<AssertionException>(() => renderer.ShouldHaveDrawnLine());
        Assert.Contains("DrawLine", ex.Message);
    }

    [Fact]
    public void ShouldHaveFilledCircle_WhenCircleFilled_Passes()
    {
        using var renderer = new Mock2DRenderer();
        renderer.FillCircle(50, 50, 25, new Vector4(1, 0, 0, 1));

        var result = renderer.ShouldHaveFilledCircle();

        Assert.Same(renderer, result);
    }

    [Fact]
    public void ShouldHaveFilledCircle_WhenNoCircleFilled_Throws()
    {
        using var renderer = new Mock2DRenderer();

        var ex = Assert.Throws<AssertionException>(() => renderer.ShouldHaveFilledCircle());
        Assert.Contains("FillCircle", ex.Message);
    }

    [Fact]
    public void ShouldHaveDrawnTexture_WhenTextureDrawn_Passes()
    {
        using var renderer = new Mock2DRenderer();
        var texture = new TextureHandle(1);
        renderer.DrawTexture(texture, 0, 0, 100, 100);

        var result = renderer.ShouldHaveDrawnTexture();

        Assert.Same(renderer, result);
    }

    [Fact]
    public void ShouldHaveDrawnTexture_WhenNoTextureDrawn_Throws()
    {
        using var renderer = new Mock2DRenderer();

        var ex = Assert.Throws<AssertionException>(() => renderer.ShouldHaveDrawnTexture());
        Assert.Contains("DrawTexture", ex.Message);
    }

    [Fact]
    public void ShouldHaveDrawnTexture_WithSpecificTexture_Passes()
    {
        using var renderer = new Mock2DRenderer();
        var texture = new TextureHandle(5);
        renderer.DrawTexture(texture, 0, 0, 100, 100);

        var result = renderer.ShouldHaveDrawnTexture(texture);

        Assert.Same(renderer, result);
    }

    [Fact]
    public void ShouldHaveClipped_WhenClipPushed_Passes()
    {
        using var renderer = new Mock2DRenderer();
        renderer.PushClip(new Rectangle(0, 0, 100, 100));

        var result = renderer.ShouldHaveClipped();

        Assert.Same(renderer, result);
    }

    [Fact]
    public void ShouldHaveClipped_WhenNoClipPushed_Throws()
    {
        using var renderer = new Mock2DRenderer();

        var ex = Assert.Throws<AssertionException>(() => renderer.ShouldHaveClipped());
        Assert.Contains("PushClip", ex.Message);
    }

    [Fact]
    public void ShouldBeInBatch_WhenInBatch_Passes()
    {
        using var renderer = new Mock2DRenderer();
        renderer.Begin();

        var result = renderer.ShouldBeInBatch();

        Assert.Same(renderer, result);
    }

    [Fact]
    public void ShouldBeInBatch_WhenNotInBatch_Throws()
    {
        using var renderer = new Mock2DRenderer();

        var ex = Assert.Throws<AssertionException>(() => renderer.ShouldBeInBatch());
        Assert.Contains("batch", ex.Message);
    }

    [Fact]
    public void ShouldNotBeInBatch_WhenNotInBatch_Passes()
    {
        using var renderer = new Mock2DRenderer();

        var result = renderer.ShouldNotBeInBatch();

        Assert.Same(renderer, result);
    }

    [Fact]
    public void ShouldNotBeInBatch_WhenInBatch_Throws()
    {
        using var renderer = new Mock2DRenderer();
        renderer.Begin();

        var ex = Assert.Throws<AssertionException>(() => renderer.ShouldNotBeInBatch());
        Assert.Contains("batch", ex.Message);
    }
}

#endregion

#region MockTextRenderer Assertions

public class GraphicsAssertionsTextRendererTests
{
    [Fact]
    public void ShouldHaveDrawnText_WhenTextContainsString_Passes()
    {
        using var renderer = new MockTextRenderer();
        var font = new FontHandle(1);
        renderer.DrawText(font, "Hello World", 0, 0, new Vector4(1, 1, 1, 1));

        var result = renderer.ShouldHaveDrawnText("World");

        Assert.Same(renderer, result);
    }

    [Fact]
    public void ShouldHaveDrawnText_WhenNoMatchingText_Throws()
    {
        using var renderer = new MockTextRenderer();
        var font = new FontHandle(1);
        renderer.DrawText(font, "Hello", 0, 0, new Vector4(1, 1, 1, 1));

        var ex = Assert.Throws<AssertionException>(() => renderer.ShouldHaveDrawnText("World"));
        Assert.Contains("World", ex.Message);
    }

    [Fact]
    public void ShouldHaveDrawnTextAt_WhenPositionMatches_Passes()
    {
        using var renderer = new MockTextRenderer();
        var font = new FontHandle(1);
        renderer.DrawText(font, "Test", 100, 200, new Vector4(1, 1, 1, 1));

        var result = renderer.ShouldHaveDrawnTextAt(new Vector2(100, 200));

        Assert.Same(renderer, result);
    }

    [Fact]
    public void ShouldHaveDrawnTextAt_WhenWithinTolerance_Passes()
    {
        using var renderer = new MockTextRenderer();
        var font = new FontHandle(1);
        renderer.DrawText(font, "Test", 100.5f, 200.5f, new Vector4(1, 1, 1, 1));

        var result = renderer.ShouldHaveDrawnTextAt(new Vector2(100, 200), tolerance: 1f);

        Assert.Same(renderer, result);
    }

    [Fact]
    public void ShouldHaveDrawnTextAt_WhenNoMatchingPosition_Throws()
    {
        using var renderer = new MockTextRenderer();
        var font = new FontHandle(1);
        renderer.DrawText(font, "Test", 0, 0, new Vector4(1, 1, 1, 1));

        var ex = Assert.Throws<AssertionException>(() =>
            renderer.ShouldHaveDrawnTextAt(new Vector2(500, 500)));
        Assert.Contains("500", ex.Message);
    }

    [Fact]
    public void ShouldHaveDrawnOutlinedText_WhenOutlinedTextDrawn_Passes()
    {
        using var renderer = new MockTextRenderer();
        var font = new FontHandle(1);
        renderer.DrawTextOutlined(font, "Title", 100, 50, new Vector4(1, 1, 1, 1), new Vector4(0, 0, 0, 1));

        var result = renderer.ShouldHaveDrawnOutlinedText();

        Assert.Same(renderer, result);
    }

    [Fact]
    public void ShouldHaveDrawnOutlinedText_WhenNoOutlinedText_Throws()
    {
        using var renderer = new MockTextRenderer();

        var ex = Assert.Throws<AssertionException>(() => renderer.ShouldHaveDrawnOutlinedText());
        Assert.Contains("outlined", ex.Message);
    }

    [Fact]
    public void ShouldHaveDrawnShadowedText_WhenShadowedTextDrawn_Passes()
    {
        using var renderer = new MockTextRenderer();
        var font = new FontHandle(1);
        renderer.DrawTextShadowed(font, "Title", 100, 50, new Vector4(1, 1, 1, 1), new Vector4(0, 0, 0, 0.5f), new Vector2(2, 2));

        var result = renderer.ShouldHaveDrawnShadowedText();

        Assert.Same(renderer, result);
    }

    [Fact]
    public void ShouldHaveDrawnShadowedText_WhenNoShadowedText_Throws()
    {
        using var renderer = new MockTextRenderer();

        var ex = Assert.Throws<AssertionException>(() => renderer.ShouldHaveDrawnShadowedText());
        Assert.Contains("shadowed", ex.Message);
    }

    [Fact]
    public void ShouldHaveDrawnWrappedText_WhenWrappedTextDrawn_Passes()
    {
        using var renderer = new MockTextRenderer();
        var font = new FontHandle(1);
        renderer.DrawTextWrapped(font, "Long text that wraps", new Rectangle(0, 0, 100, 200), new Vector4(1, 1, 1, 1));

        var result = renderer.ShouldHaveDrawnWrappedText();

        Assert.Same(renderer, result);
    }

    [Fact]
    public void ShouldHaveDrawnWrappedText_WhenNoWrappedText_Throws()
    {
        using var renderer = new MockTextRenderer();

        var ex = Assert.Throws<AssertionException>(() => renderer.ShouldHaveDrawnWrappedText());
        Assert.Contains("wrapped", ex.Message);
    }
}

#endregion

#region MockFontManager Assertions

public class GraphicsAssertionsFontManagerTests
{
    [Fact]
    public void ShouldHaveLoadedFont_WhenFontLoaded_Passes()
    {
        using var fontManager = new MockFontManager();
        fontManager.LoadFont("fonts/test.ttf", 16);

        var result = fontManager.ShouldHaveLoadedFont("fonts/test.ttf");

        Assert.Same(fontManager, result);
    }

    [Fact]
    public void ShouldHaveLoadedFont_WhenFontNotLoaded_Throws()
    {
        using var fontManager = new MockFontManager();

        var ex = Assert.Throws<AssertionException>(() =>
            fontManager.ShouldHaveLoadedFont("fonts/missing.ttf"));
        Assert.Contains("missing.ttf", ex.Message);
    }

    [Fact]
    public void ShouldHaveLoadedAnyFont_WhenFontsLoaded_Passes()
    {
        using var fontManager = new MockFontManager();
        fontManager.LoadFont("test.ttf", 12);

        var result = fontManager.ShouldHaveLoadedAnyFont();

        Assert.Same(fontManager, result);
    }

    [Fact]
    public void ShouldHaveLoadedAnyFont_WhenNoFontsLoaded_Throws()
    {
        using var fontManager = new MockFontManager();

        var ex = Assert.Throws<AssertionException>(() => fontManager.ShouldHaveLoadedAnyFont());
        Assert.Contains("no", ex.Message.ToLower());
    }

    [Fact]
    public void ShouldHaveValidFont_WhenFontIsValid_Passes()
    {
        using var fontManager = new MockFontManager();
        var font = fontManager.LoadFont("test.ttf", 16);

        var result = fontManager.ShouldHaveValidFont(font);

        Assert.Same(fontManager, result);
    }

    [Fact]
    public void ShouldHaveValidFont_WhenFontIsInvalid_Throws()
    {
        using var fontManager = new MockFontManager();
        var invalidFont = new FontHandle(999);

        var ex = Assert.Throws<AssertionException>(() => fontManager.ShouldHaveValidFont(invalidFont));
        Assert.Contains("999", ex.Message);
    }
}

#endregion

#region MockWindow Assertions

public class GraphicsAssertionsWindowTests
{
    [Fact]
    public void ShouldHaveLoaded_WhenWindowLoaded_Passes()
    {
        using var window = new MockWindow();
        window.TriggerLoad();

        var result = window.ShouldHaveLoaded();

        Assert.Same(window, result);
    }

    [Fact]
    public void ShouldHaveLoaded_WhenWindowNotLoaded_Throws()
    {
        using var window = new MockWindow();

        var ex = Assert.Throws<AssertionException>(() => window.ShouldHaveLoaded());
        Assert.Contains("loaded", ex.Message);
    }

    [Fact]
    public void ShouldBeRunning_WhenRunning_Passes()
    {
        using var window = new MockWindow();
        window.Run();

        var result = window.ShouldBeRunning();

        Assert.Same(window, result);
    }

    [Fact]
    public void ShouldBeRunning_WhenNotRunning_Throws()
    {
        using var window = new MockWindow();

        var ex = Assert.Throws<AssertionException>(() => window.ShouldBeRunning());
        Assert.Contains("running", ex.Message);
    }

    [Fact]
    public void ShouldBeClosing_WhenClosing_Passes()
    {
        using var window = new MockWindow();
        window.Run();
        window.Close();

        var result = window.ShouldBeClosing();

        Assert.Same(window, result);
    }

    [Fact]
    public void ShouldBeClosing_WhenNotClosing_Throws()
    {
        using var window = new MockWindow();
        window.Run();

        var ex = Assert.Throws<AssertionException>(() => window.ShouldBeClosing());
        Assert.Contains("closing", ex.Message);
    }

    [Fact]
    public void ShouldHaveSize_WhenSizeMatches_Passes()
    {
        using var window = new MockWindow(1920, 1080);

        var result = window.ShouldHaveSize(1920, 1080);

        Assert.Same(window, result);
    }

    [Fact]
    public void ShouldHaveSize_WhenSizeDoesNotMatch_Throws()
    {
        using var window = new MockWindow(800, 600);

        var ex = Assert.Throws<AssertionException>(() => window.ShouldHaveSize(1920, 1080));
        Assert.Contains("1920x1080", ex.Message);
        Assert.Contains("800x600", ex.Message);
    }
}

#endregion

#region MockGraphicsDevice Assertions

public class GraphicsAssertionsDeviceTests
{
    [Fact]
    public void ShouldHaveDrawn_WhenDrawCallMade_Passes()
    {
        using var device = new MockGraphicsDevice();
        device.DrawElements(PrimitiveType.Triangles, 3, IndexType.UnsignedInt);

        var result = device.ShouldHaveDrawn();

        Assert.Same(device, result);
    }

    [Fact]
    public void ShouldHaveDrawn_WhenNoDrawCalls_Throws()
    {
        using var device = new MockGraphicsDevice();

        var ex = Assert.Throws<AssertionException>(() => device.ShouldHaveDrawn());
        Assert.Contains("draw call", ex.Message);
    }

    [Fact]
    public void ShouldHaveDrawnTimes_WhenCountMatches_Passes()
    {
        using var device = new MockGraphicsDevice();
        device.DrawElements(PrimitiveType.Triangles, 3, IndexType.UnsignedInt);
        device.DrawElements(PrimitiveType.Triangles, 6, IndexType.UnsignedInt);

        var result = device.ShouldHaveDrawnTimes(2);

        Assert.Same(device, result);
    }

    [Fact]
    public void ShouldHaveDrawnTimes_WhenCountDoesNotMatch_Throws()
    {
        using var device = new MockGraphicsDevice();
        device.DrawElements(PrimitiveType.Triangles, 3, IndexType.UnsignedInt);

        var ex = Assert.Throws<AssertionException>(() => device.ShouldHaveDrawnTimes(5));
        Assert.Contains("5", ex.Message);
    }

    [Fact]
    public void ShouldHaveDepthTestEnabled_WhenEnabled_Passes()
    {
        using var device = new MockGraphicsDevice();
        device.Enable(RenderCapability.DepthTest);

        var result = device.ShouldHaveDepthTestEnabled();

        Assert.Same(device, result);
    }

    [Fact]
    public void ShouldHaveDepthTestEnabled_WhenNotEnabled_Throws()
    {
        using var device = new MockGraphicsDevice();

        var ex = Assert.Throws<AssertionException>(() => device.ShouldHaveDepthTestEnabled());
        Assert.Contains("depth", ex.Message.ToLower());
    }

    [Fact]
    public void ShouldHaveBlendingEnabled_WhenEnabled_Passes()
    {
        using var device = new MockGraphicsDevice();
        device.Enable(RenderCapability.Blend);

        var result = device.ShouldHaveBlendingEnabled();

        Assert.Same(device, result);
    }

    [Fact]
    public void ShouldHaveBlendingEnabled_WhenNotEnabled_Throws()
    {
        using var device = new MockGraphicsDevice();

        var ex = Assert.Throws<AssertionException>(() => device.ShouldHaveBlendingEnabled());
        Assert.Contains("blend", ex.Message.ToLower());
    }
}

#endregion
