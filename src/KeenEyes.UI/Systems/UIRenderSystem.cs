using System.Numerics;
using KeenEyes;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI;

/// <summary>
/// System that renders UI elements using 2D rendering primitives.
/// </summary>
/// <remarks>
/// <para>
/// The render system traverses UI hierarchies depth-first starting from <see cref="UIRootTag"/>
/// entities and renders visible elements using <see cref="I2DRenderer"/> and
/// <see cref="ITextRenderer"/>.
/// </para>
/// <para>
/// Render order is determined by hierarchy depth multiplied by 10000 plus the element's
/// <see cref="UIRect.LocalZIndex"/>. This ensures children render on top of parents.
/// </para>
/// <para>
/// This system should run in <see cref="SystemPhase.Render"/> phase with a high order
/// to ensure UI renders on top of the game world.
/// </para>
/// </remarks>
public sealed class UIRenderSystem : SystemBase
{
    /// <summary>
    /// Elements with LocalZIndex at or above this threshold are rendered in a second pass
    /// on top of all other elements. Used for menus, tooltips, modals, etc.
    /// </summary>
    private const short OverlayZIndexThreshold = 1000;

    private I2DRenderer? renderer2D;
    private ITextRenderer? textRenderer;
    private List<(Entity Entity, int DepthScore)>? deferredOverlays;

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {

        // Lazy initialization of renderers - keep trying until we find them
        // (renderer may not be available until window loads)
        if (renderer2D is null)
        {
            TryInitializeRenderers();
            if (renderer2D is null)
            {
                return;
            }
        }

        renderer2D.Begin();
        textRenderer?.Begin();

        try
        {
            // Initialize deferred overlay list for two-pass rendering
            deferredOverlays = [];

            // FIRST PASS: Render normal elements, collect overlay elements for later
            foreach (var rootEntity in World.Query<UIElement, UIRect, UIRootTag>())
            {
                ref readonly var element = ref World.Get<UIElement>(rootEntity);
                if (!element.Visible)
                {
                    continue;
                }

                if (World.Has<UIHiddenTag>(rootEntity))
                {
                    continue;
                }

                // Render root and children recursively
                RenderElement(rootEntity, 0);
            }

            // SECOND PASS: Render overlay elements (menus, tooltips, modals) on top
            if (deferredOverlays.Count > 0)
            {
                // Clear any active clipping so overlays can extend beyond parent bounds
                renderer2D.ClearClip();

                // Sort overlays by depth score to ensure correct stacking order
                // (parent overlays render before child overlays)
                deferredOverlays.Sort((a, b) => a.DepthScore.CompareTo(b.DepthScore));

                foreach (var (entity, _) in deferredOverlays)
                {
                    RenderOverlayElement(entity);
                }
            }
        }
        finally
        {
            deferredOverlays = null;
            textRenderer?.End();
            renderer2D.End();
        }
    }

    private void TryInitializeRenderers()
    {
        // Try getting I2DRenderer directly as extension first, fall back to provider
        if (!World.TryGetExtension(out renderer2D) &&
            World.TryGetExtension<I2DRendererProvider>(out var provider) &&
            provider is not null)
        {
            renderer2D = provider.Get2DRenderer();
        }

        // Try getting ITextRenderer directly as extension first, fall back to provider
        if (!World.TryGetExtension(out textRenderer) &&
            World.TryGetExtension<ITextRendererProvider>(out var textProvider) &&
            textProvider is not null)
        {
            textRenderer = textProvider.GetTextRenderer();
        }
    }

    private void RenderElement(Entity entity, int depth)
    {
        if (renderer2D is null)
        {
            return;
        }

        ref readonly var element = ref World.Get<UIElement>(entity);
        if (!element.Visible)
        {
            return;
        }

        ref readonly var rect = ref World.Get<UIRect>(entity);

        // Check if this element should be deferred to overlay pass
        if (rect.LocalZIndex >= OverlayZIndexThreshold)
        {
            int depthScore = depth * 10000 + rect.LocalZIndex;
            deferredOverlays?.Add((entity, depthScore));
            CollectOverlayChildren(entity, depth + 1);
            return; // Don't render now - will be rendered in overlay pass
        }

        var bounds = rect.ComputedBounds;

        // Push clip if element has clipping
        bool hasClip = World.Has<UIClipChildrenTag>(entity);
        if (hasClip)
        {
            renderer2D.PushClip(bounds);
        }

        // TODO: Implement scroll offset for scrollable containers
        // The scroll position would be used to translate child rendering
        _ = World.Has<UIScrollable>(entity)
            ? World.Get<UIScrollable>(entity).ScrollPosition
            : Vector2.Zero;

        // Render this element's visuals
        RenderElementVisuals(entity, bounds);

        // Render children - using IWorld.GetChildren
        var children = World.GetChildren(entity);
        foreach (var child in children)
        {
            if (!World.Has<UIElement>(child) || !World.Has<UIRect>(child))
            {
                continue;
            }

            if (World.Has<UIHiddenTag>(child))
            {
                continue;
            }

            RenderElement(child, depth + 1);
        }

        // Pop clip
        if (hasClip)
        {
            renderer2D.PopClip();
        }
    }

    /// <summary>
    /// Recursively collects all visible children of an overlay element.
    /// Each child is added with its own depth score for proper stacking order.
    /// </summary>
    private void CollectOverlayChildren(Entity parent, int depth)
    {
        var children = World.GetChildren(parent);
        foreach (var child in children)
        {
            if (!World.Has<UIElement>(child) || !World.Has<UIRect>(child))
            {
                continue;
            }

            if (World.Has<UIHiddenTag>(child))
            {
                continue;
            }

            ref readonly var element = ref World.Get<UIElement>(child);
            if (!element.Visible)
            {
                continue;
            }

            ref readonly var rect = ref World.Get<UIRect>(child);
            int depthScore = depth * 10000 + rect.LocalZIndex;
            deferredOverlays?.Add((child, depthScore));

            // Continue collecting grandchildren
            CollectOverlayChildren(child, depth + 1);
        }
    }

    /// <summary>
    /// Renders a single overlay element without clipping constraints.
    /// Called during the overlay pass after all normal elements have been rendered.
    /// </summary>
    private void RenderOverlayElement(Entity entity)
    {
        if (renderer2D is null)
        {
            return;
        }

        ref readonly var rect = ref World.Get<UIRect>(entity);
        var bounds = rect.ComputedBounds;

        RenderElementVisuals(entity, bounds);
    }

    private void RenderElementVisuals(Entity entity, Rectangle bounds)
    {
        if (renderer2D is null)
        {
            return;
        }

        // Render background style
        if (World.Has<UIStyle>(entity))
        {
            ref readonly var style = ref World.Get<UIStyle>(entity);
            RenderStyle(bounds, style);
        }

        // Render image
        if (World.Has<UIImage>(entity))
        {
            ref readonly var image = ref World.Get<UIImage>(entity);
            RenderImage(bounds, image);
        }

        // Render text
        if (World.Has<UIText>(entity) && textRenderer is not null)
        {
            ref readonly var text = ref World.Get<UIText>(entity);
            RenderText(bounds, text);
        }

        // Render interaction state overlay (hover/pressed effects)
        if (World.Has<UIInteractable>(entity))
        {
            ref readonly var interactable = ref World.Get<UIInteractable>(entity);
            RenderInteractionState(bounds, interactable);
        }

        // Render focus indicator
        if (World.Has<UIFocusedTag>(entity))
        {
            RenderFocusIndicator(bounds);
        }
    }

    private void RenderStyle(Rectangle bounds, in UIStyle style)
    {
        if (renderer2D is null)
        {
            return;
        }

        // Draw background
        if (style.BackgroundColor.W > 0)
        {
            if (style.CornerRadius > 0)
            {
                renderer2D.FillRoundedRect(
                    bounds.X, bounds.Y, bounds.Width, bounds.Height,
                    style.CornerRadius, style.BackgroundColor);
            }
            else
            {
                renderer2D.FillRect(bounds, style.BackgroundColor);
            }
        }

        // Draw background texture
        if (style.BackgroundTexture.IsValid)
        {
            renderer2D.DrawTexture(style.BackgroundTexture, bounds.X, bounds.Y, bounds.Width, bounds.Height);
        }

        // Draw border
        if (style.BorderWidth > 0 && style.BorderColor.W > 0)
        {
            if (style.CornerRadius > 0)
            {
                renderer2D.DrawRoundedRect(
                    bounds.X, bounds.Y, bounds.Width, bounds.Height,
                    style.CornerRadius, style.BorderColor, style.BorderWidth);
            }
            else
            {
                renderer2D.DrawRect(bounds, style.BorderColor, style.BorderWidth);
            }
        }
    }

    private void RenderImage(Rectangle bounds, in UIImage image)
    {
        if (renderer2D is null || !image.Texture.IsValid)
        {
            return;
        }

        // Get texture dimensions
        int texWidth = image.Texture.Width;
        int texHeight = image.Texture.Height;

        // If texture has no dimensions, fall back to simple stretch
        if (texWidth <= 0 || texHeight <= 0)
        {
            RenderImageStretched(bounds, image);
            return;
        }

        // Get source region dimensions (either from SourceRect or full texture)
        var sourceRect = image.SourceRect;
        float srcWidth, srcHeight;

        if (sourceRect.Width > 0 && sourceRect.Height > 0)
        {
            // SourceRect is in normalized coordinates (0-1)
            srcWidth = sourceRect.Width * texWidth;
            srcHeight = sourceRect.Height * texHeight;
        }
        else
        {
            srcWidth = texWidth;
            srcHeight = texHeight;
        }

        // Apply scale mode
        switch (image.ScaleMode)
        {
            case ImageScaleMode.Stretch:
                if (image.PreserveAspect)
                {
                    RenderImageScaleToFit(bounds, image, srcWidth, srcHeight);
                }
                else
                {
                    RenderImageStretched(bounds, image);
                }
                break;

            case ImageScaleMode.ScaleToFit:
                RenderImageScaleToFit(bounds, image, srcWidth, srcHeight);
                break;

            case ImageScaleMode.ScaleToFill:
                RenderImageScaleToFill(bounds, image, srcWidth, srcHeight);
                break;

            case ImageScaleMode.Tile:
                RenderImageTiled(bounds, image, srcWidth, srcHeight);
                break;

            case ImageScaleMode.NineSlice:
                RenderImageNineSlice(bounds, image);
                break;

            default:
                RenderImageStretched(bounds, image);
                break;
        }
    }

    private void RenderImageStretched(Rectangle bounds, in UIImage image)
    {
        var sourceRect = image.SourceRect;
        if (sourceRect.Width <= 0 || sourceRect.Height <= 0)
        {
            sourceRect = new Rectangle(0, 0, 1, 1); // Normalized coordinates for full texture
        }

        renderer2D!.DrawTextureRegion(image.Texture, bounds, sourceRect, image.Tint);
    }

    private void RenderImageScaleToFit(Rectangle bounds, in UIImage image, float srcWidth, float srcHeight)
    {
        // Calculate uniform scale to fit within bounds (letterbox)
        float scaleX = bounds.Width / srcWidth;
        float scaleY = bounds.Height / srcHeight;
        float scale = Math.Min(scaleX, scaleY);

        float destWidth = srcWidth * scale;
        float destHeight = srcHeight * scale;

        // Center within bounds
        float destX = bounds.X + (bounds.Width - destWidth) / 2;
        float destY = bounds.Y + (bounds.Height - destHeight) / 2;

        var destRect = new Rectangle(destX, destY, destWidth, destHeight);
        var sourceRect = image.SourceRect;
        if (sourceRect.Width <= 0 || sourceRect.Height <= 0)
        {
            sourceRect = new Rectangle(0, 0, 1, 1);
        }

        renderer2D!.DrawTextureRegion(image.Texture, destRect, sourceRect, image.Tint);
    }

    private void RenderImageScaleToFill(Rectangle bounds, in UIImage image, float srcWidth, float srcHeight)
    {
        // Calculate uniform scale to fill bounds (may crop)
        float scaleX = bounds.Width / srcWidth;
        float scaleY = bounds.Height / srcHeight;
        float scale = Math.Max(scaleX, scaleY);

        // Calculate cropped source region (in normalized coordinates)
        float visibleWidth = bounds.Width / scale;
        float visibleHeight = bounds.Height / scale;

        // Center the crop
        float cropX = (srcWidth - visibleWidth) / 2;
        float cropY = (srcHeight - visibleHeight) / 2;

        // Convert to normalized coordinates
        int texWidth = image.Texture.Width;
        int texHeight = image.Texture.Height;

        Rectangle baseSource = image.SourceRect;
        if (baseSource.Width <= 0 || baseSource.Height <= 0)
        {
            baseSource = new Rectangle(0, 0, 1, 1);
        }

        // Apply crop within the source rect
        float sourceU = baseSource.X + (cropX / texWidth);
        float sourceV = baseSource.Y + (cropY / texHeight);
        float sourceW = visibleWidth / texWidth;
        float sourceH = visibleHeight / texHeight;

        var croppedSource = new Rectangle(sourceU, sourceV, sourceW, sourceH);
        renderer2D!.DrawTextureRegion(image.Texture, bounds, croppedSource, image.Tint);
    }

    private void RenderImageTiled(Rectangle bounds, in UIImage image, float srcWidth, float srcHeight)
    {
        var sourceRect = image.SourceRect;
        if (sourceRect.Width <= 0 || sourceRect.Height <= 0)
        {
            sourceRect = new Rectangle(0, 0, 1, 1);
        }

        // Calculate how many tiles we need
        int tilesX = (int)Math.Ceiling(bounds.Width / srcWidth);
        int tilesY = (int)Math.Ceiling(bounds.Height / srcHeight);

        for (int ty = 0; ty < tilesY; ty++)
        {
            for (int tx = 0; tx < tilesX; tx++)
            {
                float tileX = bounds.X + tx * srcWidth;
                float tileY = bounds.Y + ty * srcHeight;
                float tileW = srcWidth;
                float tileH = srcHeight;
                var tileSrc = sourceRect;

                // Clip the last column if it extends beyond bounds
                if (tileX + tileW > bounds.Right)
                {
                    float excess = (tileX + tileW) - bounds.Right;
                    float ratio = 1 - (excess / srcWidth);
                    tileW = bounds.Right - tileX;
                    tileSrc = new Rectangle(sourceRect.X, sourceRect.Y, sourceRect.Width * ratio, sourceRect.Height);
                }

                // Clip the last row if it extends beyond bounds
                if (tileY + tileH > bounds.Bottom)
                {
                    float excess = (tileY + tileH) - bounds.Bottom;
                    float ratio = 1 - (excess / srcHeight);
                    tileH = bounds.Bottom - tileY;
                    tileSrc = new Rectangle(tileSrc.X, tileSrc.Y, tileSrc.Width, sourceRect.Height * ratio);
                }

                var destRect = new Rectangle(tileX, tileY, tileW, tileH);
                renderer2D!.DrawTextureRegion(image.Texture, destRect, tileSrc, image.Tint);
            }
        }
    }

    private void RenderImageNineSlice(Rectangle bounds, in UIImage image)
    {
        var tex = image.Texture;
        var border = image.SliceBorder;
        float texW = tex.Width;
        float texH = tex.Height;

        // Handle case where texture has no dimensions
        if (texW <= 0 || texH <= 0)
        {
            RenderImageStretched(bounds, image);
            return;
        }

        // Destination border sizes (in screen pixels)
        float dLeft = border.Left;
        float dRight = border.Right;
        float dTop = border.Top;
        float dBottom = border.Bottom;

        // Clamp borders if destination is smaller than combined borders
        float totalHorizontal = dLeft + dRight;
        float totalVertical = dTop + dBottom;

        if (totalHorizontal > bounds.Width && totalHorizontal > 0)
        {
            float scale = bounds.Width / totalHorizontal;
            dLeft *= scale;
            dRight *= scale;
        }

        if (totalVertical > bounds.Height && totalVertical > 0)
        {
            float scale = bounds.Height / totalVertical;
            dTop *= scale;
            dBottom *= scale;
        }

        // Calculate destination positions
        float x0 = bounds.X;
        float x1 = bounds.X + dLeft;
        float x2 = bounds.Right - dRight;

        float y0 = bounds.Y;
        float y1 = bounds.Y + dTop;
        float y2 = bounds.Bottom - dBottom;

        // Normalized UV border sizes
        float uLeft = border.Left / texW;
        float uRight = border.Right / texW;
        float vTop = border.Top / texH;
        float vBottom = border.Bottom / texH;

        // UV coordinates
        float u0 = 0, u1 = uLeft, u2 = 1 - uRight, u3 = 1;
        float v0 = 0, v1 = vTop, v2 = 1 - vBottom, v3 = 1;

        // Define destination rectangles for all 9 regions
        var destRects = new Rectangle[9];
        var srcRects = new Rectangle[9];

        // Corners (fixed size, no stretch/tile)
        destRects[0] = new Rectangle(x0, y0, dLeft, dTop);           // Top-left
        srcRects[0] = new Rectangle(u0, v0, u1 - u0, v1 - v0);

        destRects[2] = new Rectangle(x2, y0, dRight, dTop);          // Top-right
        srcRects[2] = new Rectangle(u2, v0, u3 - u2, v1 - v0);

        destRects[6] = new Rectangle(x0, y2, dLeft, dBottom);        // Bottom-left
        srcRects[6] = new Rectangle(u0, v2, u1 - u0, v3 - v2);

        destRects[8] = new Rectangle(x2, y2, dRight, dBottom);       // Bottom-right
        srcRects[8] = new Rectangle(u2, v2, u3 - u2, v3 - v2);

        // Edges (stretch in one axis)
        destRects[1] = new Rectangle(x1, y0, x2 - x1, dTop);         // Top edge
        srcRects[1] = new Rectangle(u1, v0, u2 - u1, v1 - v0);

        destRects[7] = new Rectangle(x1, y2, x2 - x1, dBottom);      // Bottom edge
        srcRects[7] = new Rectangle(u1, v2, u2 - u1, v3 - v2);

        destRects[3] = new Rectangle(x0, y1, dLeft, y2 - y1);        // Left edge
        srcRects[3] = new Rectangle(u0, v1, u1 - u0, v2 - v1);

        destRects[5] = new Rectangle(x2, y1, dRight, y2 - y1);       // Right edge
        srcRects[5] = new Rectangle(u2, v1, u3 - u2, v2 - v1);

        // Center (stretch in both axes)
        destRects[4] = new Rectangle(x1, y1, x2 - x1, y2 - y1);      // Center
        srcRects[4] = new Rectangle(u1, v1, u2 - u1, v2 - v1);

        // Draw corners (always stretched, they're fixed size)
        int[] corners = [0, 2, 6, 8];
        foreach (int i in corners)
        {
            if (destRects[i].Width > 0 && destRects[i].Height > 0)
            {
                renderer2D!.DrawTextureRegion(image.Texture, destRects[i], srcRects[i], image.Tint);
            }
        }

        // Draw edges based on EdgeFillMode
        int[] edges = [1, 3, 5, 7];
        foreach (int i in edges)
        {
            if (destRects[i].Width > 0 && destRects[i].Height > 0)
            {
                if (image.EdgeFillMode == SlicedFillMode.Tile)
                {
                    RenderTiledRegion(destRects[i], srcRects[i], image, texW, texH, i);
                }
                else
                {
                    renderer2D!.DrawTextureRegion(image.Texture, destRects[i], srcRects[i], image.Tint);
                }
            }
        }

        // Draw center based on CenterFillMode
        if (destRects[4].Width > 0 && destRects[4].Height > 0)
        {
            if (image.CenterFillMode == SlicedFillMode.Tile)
            {
                RenderTiledRegion(destRects[4], srcRects[4], image, texW, texH, 4);
            }
            else
            {
                renderer2D!.DrawTextureRegion(image.Texture, destRects[4], srcRects[4], image.Tint);
            }
        }
    }

    private void RenderTiledRegion(Rectangle destRect, Rectangle srcRect, in UIImage image, float texW, float texH, int regionIndex)
    {
        // Calculate source region size in pixels
        float srcPixelW = srcRect.Width * texW;
        float srcPixelH = srcRect.Height * texH;

        if (srcPixelW <= 0 || srcPixelH <= 0)
        {
            return;
        }

        // For edge regions, only tile in one direction
        bool isHorizontalEdge = regionIndex == 1 || regionIndex == 7; // Top/bottom edges
        bool isVerticalEdge = regionIndex == 3 || regionIndex == 5;   // Left/right edges

        int tilesX = isVerticalEdge ? 1 : (int)Math.Ceiling(destRect.Width / srcPixelW);
        int tilesY = isHorizontalEdge ? 1 : (int)Math.Ceiling(destRect.Height / srcPixelH);

        for (int ty = 0; ty < tilesY; ty++)
        {
            for (int tx = 0; tx < tilesX; tx++)
            {
                float tileX = destRect.X + tx * srcPixelW;
                float tileY = destRect.Y + ty * srcPixelH;
                float tileW = isVerticalEdge ? destRect.Width : srcPixelW;
                float tileH = isHorizontalEdge ? destRect.Height : srcPixelH;
                var tileSrc = srcRect;

                // Clip if extends beyond dest rect
                if (tileX + tileW > destRect.Right)
                {
                    float excess = (tileX + tileW) - destRect.Right;
                    float ratio = 1 - (excess / srcPixelW);
                    tileW = destRect.Right - tileX;
                    tileSrc = new Rectangle(srcRect.X, srcRect.Y, srcRect.Width * ratio, srcRect.Height);
                }

                if (tileY + tileH > destRect.Bottom)
                {
                    float excess = (tileY + tileH) - destRect.Bottom;
                    float ratio = 1 - (excess / srcPixelH);
                    tileH = destRect.Bottom - tileY;
                    tileSrc = new Rectangle(tileSrc.X, tileSrc.Y, tileSrc.Width, srcRect.Height * ratio);
                }

                var dest = new Rectangle(tileX, tileY, tileW, tileH);
                renderer2D!.DrawTextureRegion(image.Texture, dest, tileSrc, image.Tint);
            }
        }
    }

    private void RenderText(Rectangle bounds, in UIText text)
    {
        if (textRenderer is null || string.IsNullOrEmpty(text.Content))
        {
            return;
        }

        // Flush 2D batch first to ensure backgrounds are drawn before text
        renderer2D?.Flush();

        // Calculate text position based on alignment
        float x = text.HorizontalAlign switch
        {
            TextAlignH.Center => bounds.X + bounds.Width / 2,
            TextAlignH.Right => bounds.X + bounds.Width,
            _ => bounds.X
        };

        float y = text.VerticalAlign switch
        {
            TextAlignV.Middle => bounds.Y + bounds.Height / 2,
            TextAlignV.Bottom => bounds.Y + bounds.Height,
            _ => bounds.Y
        };

        // Use word wrap or simple text rendering
        if (text.WordWrap)
        {
            textRenderer.DrawTextWrapped(
                text.Font, text.Content.AsSpan(), bounds, text.Color,
                text.HorizontalAlign, text.VerticalAlign);
        }
        else
        {
            textRenderer.DrawText(
                text.Font, text.Content.AsSpan(), x, y, text.Color,
                text.HorizontalAlign, text.VerticalAlign);
        }

        // Flush text immediately so it renders on top of background
        textRenderer.Flush();
    }

    private void RenderInteractionState(Rectangle bounds, in UIInteractable interactable)
    {
        if (renderer2D is null)
        {
            return;
        }

        // Draw subtle overlay for hover/pressed states
        if (interactable.IsPressed)
        {
            // Dark overlay when pressed
            renderer2D.FillRect(bounds, new Vector4(0, 0, 0, 0.2f));
        }
        else if (interactable.IsHovered)
        {
            // Light overlay when hovered
            renderer2D.FillRect(bounds, new Vector4(1, 1, 1, 0.1f));
        }
    }

    private void RenderFocusIndicator(Rectangle bounds)
    {
        if (renderer2D is null)
        {
            return;
        }

        // Draw focus outline
        const float focusOutlineWidth = 2f;
        var focusColor = new Vector4(0.3f, 0.6f, 1f, 0.8f); // Blue focus ring

        renderer2D.DrawRect(
            bounds.X - focusOutlineWidth,
            bounds.Y - focusOutlineWidth,
            bounds.Width + focusOutlineWidth * 2,
            bounds.Height + focusOutlineWidth * 2,
            focusColor,
            focusOutlineWidth);
    }
}
