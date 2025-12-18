using System.Diagnostics.CodeAnalysis;
using System.Numerics;

using FontStashSharp;

using KeenEyes.Common;
using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Graphics.Silk.Text;

/// <summary>
/// OpenGL implementation of <see cref="ITextRenderer"/> using FontStashSharp.
/// </summary>
/// <remarks>
/// <para>
/// This renderer uses FontStashSharp to handle glyph rendering. Text is drawn
/// through DynamicSpriteFont.DrawText which generates quads that are batched
/// by FontStashRenderer.
/// </para>
/// </remarks>
/// <param name="device">The graphics device.</param>
/// <param name="fontManager">The font manager.</param>
/// <param name="textureManager">The texture manager for FontStashSharp.</param>
/// <param name="screenWidth">Initial screen width.</param>
/// <param name="screenHeight">Initial screen height.</param>
[ExcludeFromCodeCoverage(Justification = "Requires real GPU context")]
internal sealed class SilkTextRenderer(IGraphicsDevice device, SilkFontManager fontManager, FontStashTextureManager textureManager, float screenWidth, float screenHeight) : ITextRenderer
{
    private readonly SilkFontManager fontManager = fontManager ?? throw new ArgumentNullException(nameof(fontManager));
    private readonly FontStashRenderer renderer = new(device, textureManager, screenWidth, screenHeight);
    private bool isBatching;
    private bool disposed;

    /// <summary>
    /// Updates the screen size for projection matrix calculation.
    /// </summary>
    /// <param name="width">The new screen width.</param>
    /// <param name="height">The new screen height.</param>
    public void SetScreenSize(float width, float height)
    {
        renderer.SetScreenSize(width, height);
    }

    #region ITextRenderer Implementation

    /// <inheritdoc />
    public void Begin()
    {
        if (isBatching)
        {
            throw new InvalidOperationException("Begin() called while already batching. Call End() first.");
        }

        isBatching = true;
        renderer.Begin();
    }

    /// <inheritdoc />
    public void Begin(in Matrix4x4 customProjection)
    {
        if (isBatching)
        {
            throw new InvalidOperationException("Begin() called while already batching. Call End() first.");
        }

        isBatching = true;
        renderer.Begin(customProjection);
    }

    /// <inheritdoc />
    public void End()
    {
        if (!isBatching)
        {
            return;
        }

        renderer.End();
        isBatching = false;
    }

    /// <inheritdoc />
    public void Flush()
    {
        renderer.Flush();
    }

    /// <inheritdoc />
    public void DrawText(
        FontHandle font,
        ReadOnlySpan<char> text,
        float x,
        float y,
        Vector4 color,
        TextAlignH alignH = TextAlignH.Left,
        TextAlignV alignV = TextAlignV.Top)
    {
        DrawTextInternal(font, text, x, y, color, 1f, 0f, alignH, alignV);
    }

    /// <inheritdoc />
    public void DrawText(
        FontHandle font,
        ReadOnlySpan<char> text,
        float x,
        float y,
        Vector4 color,
        float scale,
        TextAlignH alignH = TextAlignH.Left,
        TextAlignV alignV = TextAlignV.Top)
    {
        DrawTextInternal(font, text, x, y, color, scale, 0f, alignH, alignV);
    }

    /// <inheritdoc />
    public void DrawTextRotated(
        FontHandle font,
        ReadOnlySpan<char> text,
        float x,
        float y,
        Vector4 color,
        float rotation,
        TextAlignH alignH = TextAlignH.Left,
        TextAlignV alignV = TextAlignV.Top)
    {
        DrawTextInternal(font, text, x, y, color, 1f, rotation, alignH, alignV);
    }

    /// <inheritdoc />
    public void DrawTextWrapped(
        FontHandle font,
        ReadOnlySpan<char> text,
        in Rectangle bounds,
        Vector4 color,
        TextAlignH alignH = TextAlignH.Left,
        TextAlignV alignV = TextAlignV.Top)
    {
        var spriteFont = fontManager.GetSpriteFont(font);
        if (spriteFont is null)
        {
            return;
        }

        var lineHeight = spriteFont.LineHeight;

        // Calculate word wrap breaks
        var breaks = fontManager.CalculateWordWrap(font, text, bounds.Width);

        // Calculate total height for vertical alignment
        int lineCount = breaks.Count + 1;
        float totalHeight = lineCount * lineHeight;

        // Determine starting Y based on vertical alignment
        float startY = alignV switch
        {
            TextAlignV.Top => bounds.Y,
            TextAlignV.Middle => bounds.Y + (bounds.Height - totalHeight) / 2,
            TextAlignV.Bottom => bounds.Y + bounds.Height - totalHeight,
            TextAlignV.Baseline => bounds.Y + lineHeight * 0.8f, // Approximate baseline
            _ => bounds.Y
        };

        // Render each line
        int lineStart = 0;
        float currentY = startY;
        var fsColor = ToFSColor(color);

        for (int i = 0; i <= breaks.Count; i++)
        {
            int lineEnd = i < breaks.Count ? breaks[i] : text.Length;
            var lineText = text[lineStart..lineEnd].ToString();

            // Calculate line width for horizontal alignment
            var lineWidth = spriteFont.MeasureString(lineText).X;
            float lineX = alignH switch
            {
                TextAlignH.Left => bounds.X,
                TextAlignH.Center => bounds.X + (bounds.Width - lineWidth) / 2,
                TextAlignH.Right => bounds.X + bounds.Width - lineWidth,
                _ => bounds.X
            };

            spriteFont.DrawText(renderer, lineText, new System.Numerics.Vector2(lineX, currentY), fsColor);

            currentY += lineHeight;
            lineStart = lineEnd + 1; // Skip the break character
        }
    }

    /// <inheritdoc />
    public void DrawTextOutlined(
        FontHandle font,
        ReadOnlySpan<char> text,
        float x,
        float y,
        Vector4 textColor,
        Vector4 outlineColor,
        float outlineWidth = 1f,
        TextAlignH alignH = TextAlignH.Left,
        TextAlignV alignV = TextAlignV.Top)
    {
        // Draw outline by rendering text at offsets
        for (float ox = -outlineWidth; ox <= outlineWidth; ox += outlineWidth)
        {
            for (float oy = -outlineWidth; oy <= outlineWidth; oy += outlineWidth)
            {
                if (!ox.IsApproximatelyZero() || !oy.IsApproximatelyZero())
                {
                    DrawTextInternal(font, text, x + ox, y + oy, outlineColor, 1f, 0f, alignH, alignV);
                }
            }
        }

        // Draw main text on top
        DrawTextInternal(font, text, x, y, textColor, 1f, 0f, alignH, alignV);
    }

    /// <inheritdoc />
    public void DrawTextShadowed(
        FontHandle font,
        ReadOnlySpan<char> text,
        float x,
        float y,
        Vector4 textColor,
        Vector4 shadowColor,
        Vector2 shadowOffset,
        TextAlignH alignH = TextAlignH.Left,
        TextAlignV alignV = TextAlignV.Top)
    {
        // Draw shadow
        DrawTextInternal(font, text, x + shadowOffset.X, y + shadowOffset.Y, shadowColor, 1f, 0f, alignH, alignV);

        // Draw main text
        DrawTextInternal(font, text, x, y, textColor, 1f, 0f, alignH, alignV);
    }

    #endregion

    private void DrawTextInternal(
        FontHandle font,
        ReadOnlySpan<char> text,
        float x,
        float y,
        Vector4 color,
        float scale,
        float rotation,
        TextAlignH alignH,
        TextAlignV alignV)
    {
        if (text.IsEmpty)
        {
            return;
        }

        var spriteFont = fontManager.GetSpriteFont(font);
        if (spriteFont is null)
        {
            return;
        }

        var textString = text.ToString();
        var textSize = spriteFont.MeasureString(textString);

        // Apply alignment offset
        float offsetX = alignH switch
        {
            TextAlignH.Left => 0,
            TextAlignH.Center => -textSize.X * scale / 2,
            TextAlignH.Right => -textSize.X * scale,
            _ => 0
        };

        float offsetY = alignV switch
        {
            TextAlignV.Top => 0,
            TextAlignV.Middle => -textSize.Y * scale / 2,
            TextAlignV.Bottom => -textSize.Y * scale,
            TextAlignV.Baseline => -spriteFont.LineHeight * 0.8f * scale,
            _ => 0
        };

        var position = new System.Numerics.Vector2(x + offsetX, y + offsetY);
        var fsColor = ToFSColor(color);

        if (!rotation.IsApproximatelyZero())
        {
            // FontStashSharp supports rotation through the overload with origin
            var origin = new System.Numerics.Vector2(-offsetX / scale, -offsetY / scale);
            spriteFont.DrawText(renderer, textString, new System.Numerics.Vector2(x, y), fsColor,
                rotation, origin, new System.Numerics.Vector2(scale, scale));
        }
        else if (!scale.ApproximatelyEquals(1f))
        {
            spriteFont.DrawText(renderer, textString, position, fsColor,
                0f, System.Numerics.Vector2.Zero, new System.Numerics.Vector2(scale, scale));
        }
        else
        {
            spriteFont.DrawText(renderer, textString, position, fsColor);
        }
    }

    private static FSColor ToFSColor(Vector4 color)
    {
        return new FSColor(
            (byte)(color.X * 255),
            (byte)(color.Y * 255),
            (byte)(color.Z * 255),
            (byte)(color.W * 255));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        renderer.Dispose();
    }
}
