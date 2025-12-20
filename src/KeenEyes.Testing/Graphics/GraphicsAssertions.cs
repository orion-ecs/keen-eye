using System.Numerics;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.Testing.Platform;

namespace KeenEyes.Testing.Graphics;

/// <summary>
/// Fluent assertions for graphics mock verification.
/// </summary>
public static class GraphicsAssertions
{
    #region MockGraphicsContext Assertions

    /// <summary>
    /// Asserts that a mesh was drawn at least once.
    /// </summary>
    /// <param name="context">The mock graphics context.</param>
    /// <param name="mesh">The mesh handle to check.</param>
    /// <returns>The context for chaining.</returns>
    /// <exception cref="AssertionException">Thrown when the mesh was not drawn.</exception>
    public static MockGraphicsContext ShouldHaveDrawnMesh(this MockGraphicsContext context, MeshHandle mesh)
    {
        if (!context.MeshDrawCalls.Any(c => c.Mesh == mesh))
        {
            throw new AssertionException($"Expected mesh {mesh.Id} to be drawn, but it was not.");
        }

        return context;
    }

    /// <summary>
    /// Asserts that a mesh was drawn a specific number of times.
    /// </summary>
    /// <param name="context">The mock graphics context.</param>
    /// <param name="mesh">The mesh handle to check.</param>
    /// <param name="times">The expected number of draw calls.</param>
    /// <returns>The context for chaining.</returns>
    /// <exception cref="AssertionException">Thrown when the count doesn't match.</exception>
    public static MockGraphicsContext ShouldHaveDrawnMeshTimes(this MockGraphicsContext context, MeshHandle mesh, int times)
    {
        var count = context.MeshDrawCalls.Count(c => c.Mesh == mesh);
        if (count != times)
        {
            throw new AssertionException($"Expected mesh {mesh.Id} to be drawn {times} times, but was drawn {count} times.");
        }

        return context;
    }

    /// <summary>
    /// Asserts that a texture was created.
    /// </summary>
    /// <param name="context">The mock graphics context.</param>
    /// <returns>The context for chaining.</returns>
    /// <exception cref="AssertionException">Thrown when no texture was created.</exception>
    public static MockGraphicsContext ShouldHaveCreatedTexture(this MockGraphicsContext context)
    {
        // Subtract 1 for the default WhiteTexture
        if (context.Textures.Count <= 1)
        {
            throw new AssertionException("Expected at least one texture to be created, but none were.");
        }

        return context;
    }

    /// <summary>
    /// Asserts that a shader is currently bound.
    /// </summary>
    /// <param name="context">The mock graphics context.</param>
    /// <param name="shader">The expected bound shader.</param>
    /// <returns>The context for chaining.</returns>
    /// <exception cref="AssertionException">Thrown when the shader is not bound.</exception>
    public static MockGraphicsContext ShouldHaveBoundShader(this MockGraphicsContext context, ShaderHandle shader)
    {
        if (context.BoundShader != shader)
        {
            throw new AssertionException($"Expected shader {shader.Id} to be bound, but {context.BoundShader.Id} was bound.");
        }

        return context;
    }

    /// <summary>
    /// Asserts that a uniform was set with a specific value.
    /// </summary>
    /// <param name="context">The mock graphics context.</param>
    /// <param name="name">The uniform name.</param>
    /// <param name="expectedValue">The expected value.</param>
    /// <returns>The context for chaining.</returns>
    /// <exception cref="AssertionException">Thrown when the uniform doesn't match.</exception>
    public static MockGraphicsContext ShouldHaveUniform(this MockGraphicsContext context, string name, object expectedValue)
    {
        if (!context.UniformValues.TryGetValue(name, out var value))
        {
            throw new AssertionException($"Expected uniform '{name}' to be set, but it was not.");
        }

        if (!Equals(value, expectedValue))
        {
            throw new AssertionException($"Expected uniform '{name}' to be {expectedValue}, but was {value}.");
        }

        return context;
    }

    #endregion

    #region Mock2DRenderer Assertions

    /// <summary>
    /// Asserts that a rectangle was filled.
    /// </summary>
    /// <param name="renderer">The mock 2D renderer.</param>
    /// <param name="rect">The expected rectangle (optional).</param>
    /// <returns>The renderer for chaining.</returns>
    /// <exception cref="AssertionException">Thrown when no matching fill rect was found.</exception>
    public static Mock2DRenderer ShouldHaveFilledRect(this Mock2DRenderer renderer, Rectangle? rect = null)
    {
        var fillRects = renderer.Commands.OfType<FillRectCommand>().ToList();
        if (fillRects.Count == 0)
        {
            throw new AssertionException("Expected at least one FillRect command, but none were recorded.");
        }

        if (rect.HasValue && !fillRects.Any(c => c.Rect == rect.Value))
        {
            throw new AssertionException($"Expected FillRect at {rect.Value}, but no matching command was found.");
        }

        return renderer;
    }

    /// <summary>
    /// Asserts that a rectangle outline was drawn.
    /// </summary>
    /// <param name="renderer">The mock 2D renderer.</param>
    /// <param name="rect">The expected rectangle (optional).</param>
    /// <returns>The renderer for chaining.</returns>
    /// <exception cref="AssertionException">Thrown when no matching draw rect was found.</exception>
    public static Mock2DRenderer ShouldHaveDrawnRect(this Mock2DRenderer renderer, Rectangle? rect = null)
    {
        var drawRects = renderer.Commands.OfType<DrawRectCommand>().ToList();
        if (drawRects.Count == 0)
        {
            throw new AssertionException("Expected at least one DrawRect command, but none were recorded.");
        }

        if (rect.HasValue && !drawRects.Any(c => c.Rect == rect.Value))
        {
            throw new AssertionException($"Expected DrawRect at {rect.Value}, but no matching command was found.");
        }

        return renderer;
    }

    /// <summary>
    /// Asserts that a line was drawn.
    /// </summary>
    /// <param name="renderer">The mock 2D renderer.</param>
    /// <returns>The renderer for chaining.</returns>
    /// <exception cref="AssertionException">Thrown when no line was drawn.</exception>
    public static Mock2DRenderer ShouldHaveDrawnLine(this Mock2DRenderer renderer)
    {
        if (!renderer.Commands.OfType<DrawLineCommand>().Any())
        {
            throw new AssertionException("Expected at least one DrawLine command, but none were recorded.");
        }

        return renderer;
    }

    /// <summary>
    /// Asserts that a circle was filled.
    /// </summary>
    /// <param name="renderer">The mock 2D renderer.</param>
    /// <returns>The renderer for chaining.</returns>
    /// <exception cref="AssertionException">Thrown when no circle was filled.</exception>
    public static Mock2DRenderer ShouldHaveFilledCircle(this Mock2DRenderer renderer)
    {
        if (!renderer.Commands.OfType<FillCircleCommand>().Any())
        {
            throw new AssertionException("Expected at least one FillCircle command, but none were recorded.");
        }

        return renderer;
    }

    /// <summary>
    /// Asserts that a texture was drawn.
    /// </summary>
    /// <param name="renderer">The mock 2D renderer.</param>
    /// <param name="texture">The expected texture handle (optional).</param>
    /// <returns>The renderer for chaining.</returns>
    /// <exception cref="AssertionException">Thrown when no matching texture draw was found.</exception>
    public static Mock2DRenderer ShouldHaveDrawnTexture(this Mock2DRenderer renderer, TextureHandle? texture = null)
    {
        var textureCommands = renderer.Commands.OfType<DrawTextureCommand>().ToList();
        if (textureCommands.Count == 0)
        {
            throw new AssertionException("Expected at least one DrawTexture command, but none were recorded.");
        }

        if (texture.HasValue && !textureCommands.Any(c => c.Texture == texture.Value))
        {
            throw new AssertionException($"Expected texture {texture.Value.Id} to be drawn, but it was not.");
        }

        return renderer;
    }

    /// <summary>
    /// Asserts that a clip was pushed.
    /// </summary>
    /// <param name="renderer">The mock 2D renderer.</param>
    /// <param name="rect">The expected clip rectangle (optional).</param>
    /// <returns>The renderer for chaining.</returns>
    /// <exception cref="AssertionException">Thrown when no matching clip was found.</exception>
    public static Mock2DRenderer ShouldHaveClipped(this Mock2DRenderer renderer, Rectangle? rect = null)
    {
        var clips = renderer.Commands.OfType<PushClipCommand>().ToList();
        if (clips.Count == 0)
        {
            throw new AssertionException("Expected at least one PushClip command, but none were recorded.");
        }

        if (rect.HasValue && !clips.Any(c => c.RequestedClip == rect.Value))
        {
            throw new AssertionException($"Expected clip at {rect.Value}, but no matching command was found.");
        }

        return renderer;
    }

    /// <summary>
    /// Asserts the renderer is in a batch.
    /// </summary>
    /// <param name="renderer">The mock 2D renderer.</param>
    /// <returns>The renderer for chaining.</returns>
    /// <exception cref="AssertionException">Thrown when not in a batch.</exception>
    public static Mock2DRenderer ShouldBeInBatch(this Mock2DRenderer renderer)
    {
        if (!renderer.IsInBatch)
        {
            throw new AssertionException("Expected renderer to be in a batch, but it was not.");
        }

        return renderer;
    }

    /// <summary>
    /// Asserts the renderer is not in a batch.
    /// </summary>
    /// <param name="renderer">The mock 2D renderer.</param>
    /// <returns>The renderer for chaining.</returns>
    /// <exception cref="AssertionException">Thrown when in a batch.</exception>
    public static Mock2DRenderer ShouldNotBeInBatch(this Mock2DRenderer renderer)
    {
        if (renderer.IsInBatch)
        {
            throw new AssertionException("Expected renderer to not be in a batch, but it was.");
        }

        return renderer;
    }

    #endregion

    #region MockTextRenderer Assertions

    /// <summary>
    /// Asserts that text containing the specified string was drawn.
    /// </summary>
    /// <param name="renderer">The mock text renderer.</param>
    /// <param name="containing">The substring to search for.</param>
    /// <returns>The renderer for chaining.</returns>
    /// <exception cref="AssertionException">Thrown when no matching text was found.</exception>
    public static MockTextRenderer ShouldHaveDrawnText(this MockTextRenderer renderer, string containing)
    {
        if (!renderer.Commands.Any(c => c.Text.Contains(containing, StringComparison.OrdinalIgnoreCase)))
        {
            throw new AssertionException($"Expected text containing '{containing}' to be drawn, but no matching command was found.");
        }

        return renderer;
    }

    /// <summary>
    /// Asserts that text was drawn at approximately the specified position.
    /// </summary>
    /// <param name="renderer">The mock text renderer.</param>
    /// <param name="position">The expected position.</param>
    /// <param name="tolerance">The position tolerance (default 1.0).</param>
    /// <returns>The renderer for chaining.</returns>
    /// <exception cref="AssertionException">Thrown when no text was drawn at the position.</exception>
    public static MockTextRenderer ShouldHaveDrawnTextAt(this MockTextRenderer renderer, Vector2 position, float tolerance = 1f)
    {
        var textCommands = renderer.Commands.OfType<DrawTextCommand>().ToList();
        if (!textCommands.Any(c => Vector2.Distance(c.Position, position) <= tolerance))
        {
            throw new AssertionException($"Expected text to be drawn at approximately {position}, but no matching command was found.");
        }

        return renderer;
    }

    /// <summary>
    /// Asserts that outlined text was drawn.
    /// </summary>
    /// <param name="renderer">The mock text renderer.</param>
    /// <returns>The renderer for chaining.</returns>
    /// <exception cref="AssertionException">Thrown when no outlined text was drawn.</exception>
    public static MockTextRenderer ShouldHaveDrawnOutlinedText(this MockTextRenderer renderer)
    {
        if (!renderer.Commands.OfType<DrawTextOutlinedCommand>().Any())
        {
            throw new AssertionException("Expected outlined text to be drawn, but none was recorded.");
        }

        return renderer;
    }

    /// <summary>
    /// Asserts that shadowed text was drawn.
    /// </summary>
    /// <param name="renderer">The mock text renderer.</param>
    /// <returns>The renderer for chaining.</returns>
    /// <exception cref="AssertionException">Thrown when no shadowed text was drawn.</exception>
    public static MockTextRenderer ShouldHaveDrawnShadowedText(this MockTextRenderer renderer)
    {
        if (!renderer.Commands.OfType<DrawTextShadowedCommand>().Any())
        {
            throw new AssertionException("Expected shadowed text to be drawn, but none was recorded.");
        }

        return renderer;
    }

    /// <summary>
    /// Asserts that wrapped text was drawn.
    /// </summary>
    /// <param name="renderer">The mock text renderer.</param>
    /// <returns>The renderer for chaining.</returns>
    /// <exception cref="AssertionException">Thrown when no wrapped text was drawn.</exception>
    public static MockTextRenderer ShouldHaveDrawnWrappedText(this MockTextRenderer renderer)
    {
        if (!renderer.Commands.OfType<DrawTextWrappedCommand>().Any())
        {
            throw new AssertionException("Expected wrapped text to be drawn, but none was recorded.");
        }

        return renderer;
    }

    #endregion

    #region MockFontManager Assertions

    /// <summary>
    /// Asserts that a font was loaded from the specified path.
    /// </summary>
    /// <param name="fontManager">The mock font manager.</param>
    /// <param name="path">The expected font path.</param>
    /// <returns>The font manager for chaining.</returns>
    /// <exception cref="AssertionException">Thrown when the font was not loaded.</exception>
    public static MockFontManager ShouldHaveLoadedFont(this MockFontManager fontManager, string path)
    {
        if (!fontManager.LoadedFontPaths.Contains(path))
        {
            throw new AssertionException($"Expected font '{path}' to be loaded, but it was not.");
        }

        return fontManager;
    }

    /// <summary>
    /// Asserts that at least one font was loaded.
    /// </summary>
    /// <param name="fontManager">The mock font manager.</param>
    /// <returns>The font manager for chaining.</returns>
    /// <exception cref="AssertionException">Thrown when no fonts were loaded.</exception>
    public static MockFontManager ShouldHaveLoadedAnyFont(this MockFontManager fontManager)
    {
        if (fontManager.Fonts.Count == 0)
        {
            throw new AssertionException("Expected at least one font to be loaded, but none were.");
        }

        return fontManager;
    }

    /// <summary>
    /// Asserts that a font handle is valid.
    /// </summary>
    /// <param name="fontManager">The mock font manager.</param>
    /// <param name="font">The font handle to check.</param>
    /// <returns>The font manager for chaining.</returns>
    /// <exception cref="AssertionException">Thrown when the font is invalid.</exception>
    public static MockFontManager ShouldHaveValidFont(this MockFontManager fontManager, FontHandle font)
    {
        if (!fontManager.IsValid(font))
        {
            throw new AssertionException($"Expected font handle {font.Id} to be valid, but it was not.");
        }

        return fontManager;
    }

    #endregion

    #region MockWindow Assertions

    /// <summary>
    /// Asserts that the window has been loaded.
    /// </summary>
    /// <param name="window">The mock window.</param>
    /// <returns>The window for chaining.</returns>
    /// <exception cref="AssertionException">Thrown when the window was not loaded.</exception>
    public static MockWindow ShouldHaveLoaded(this MockWindow window)
    {
        if (window.LoadCount == 0)
        {
            throw new AssertionException("Expected window to have been loaded, but it was not.");
        }

        return window;
    }

    /// <summary>
    /// Asserts that the window is running.
    /// </summary>
    /// <param name="window">The mock window.</param>
    /// <returns>The window for chaining.</returns>
    /// <exception cref="AssertionException">Thrown when the window is not running.</exception>
    public static MockWindow ShouldBeRunning(this MockWindow window)
    {
        if (!window.IsRunning)
        {
            throw new AssertionException("Expected window to be running, but it was not.");
        }

        return window;
    }

    /// <summary>
    /// Asserts that the window is closing or closed.
    /// </summary>
    /// <param name="window">The mock window.</param>
    /// <returns>The window for chaining.</returns>
    /// <exception cref="AssertionException">Thrown when the window is not closing.</exception>
    public static MockWindow ShouldBeClosing(this MockWindow window)
    {
        if (!window.IsClosing)
        {
            throw new AssertionException("Expected window to be closing, but it was not.");
        }

        return window;
    }

    /// <summary>
    /// Asserts that the window has the expected size.
    /// </summary>
    /// <param name="window">The mock window.</param>
    /// <param name="width">The expected width.</param>
    /// <param name="height">The expected height.</param>
    /// <returns>The window for chaining.</returns>
    /// <exception cref="AssertionException">Thrown when the size doesn't match.</exception>
    public static MockWindow ShouldHaveSize(this MockWindow window, int width, int height)
    {
        if (window.Width != width || window.Height != height)
        {
            throw new AssertionException($"Expected window size to be {width}x{height}, but was {window.Width}x{window.Height}.");
        }

        return window;
    }

    #endregion

    #region MockGraphicsDevice Assertions

    /// <summary>
    /// Asserts that a draw call was made.
    /// </summary>
    /// <param name="device">The mock graphics device.</param>
    /// <returns>The device for chaining.</returns>
    /// <exception cref="AssertionException">Thrown when no draw calls were made.</exception>
    public static MockGraphicsDevice ShouldHaveDrawn(this MockGraphicsDevice device)
    {
        if (device.DrawCalls.Count == 0)
        {
            throw new AssertionException("Expected at least one draw call, but none were recorded.");
        }

        return device;
    }

    /// <summary>
    /// Asserts that a specific number of draw calls were made.
    /// </summary>
    /// <param name="device">The mock graphics device.</param>
    /// <param name="count">The expected number of draw calls.</param>
    /// <returns>The device for chaining.</returns>
    /// <exception cref="AssertionException">Thrown when the count doesn't match.</exception>
    public static MockGraphicsDevice ShouldHaveDrawnTimes(this MockGraphicsDevice device, int count)
    {
        if (device.DrawCalls.Count != count)
        {
            throw new AssertionException($"Expected {count} draw calls, but {device.DrawCalls.Count} were recorded.");
        }

        return device;
    }

    /// <summary>
    /// Asserts that depth testing is enabled.
    /// </summary>
    /// <param name="device">The mock graphics device.</param>
    /// <returns>The device for chaining.</returns>
    /// <exception cref="AssertionException">Thrown when depth testing is disabled.</exception>
    public static MockGraphicsDevice ShouldHaveDepthTestEnabled(this MockGraphicsDevice device)
    {
        if (!device.RenderState.EnabledCapabilities.Contains(RenderCapability.DepthTest))
        {
            throw new AssertionException("Expected depth testing to be enabled, but it was disabled.");
        }

        return device;
    }

    /// <summary>
    /// Asserts that blending is enabled.
    /// </summary>
    /// <param name="device">The mock graphics device.</param>
    /// <returns>The device for chaining.</returns>
    /// <exception cref="AssertionException">Thrown when blending is disabled.</exception>
    public static MockGraphicsDevice ShouldHaveBlendingEnabled(this MockGraphicsDevice device)
    {
        if (!device.RenderState.EnabledCapabilities.Contains(RenderCapability.Blend))
        {
            throw new AssertionException("Expected blending to be enabled, but it was disabled.");
        }

        return device;
    }

    #endregion
}
