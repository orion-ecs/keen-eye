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

        // Get scroll offset for scrollable containers
        Vector2 scrollOffset = World.Has<UIScrollable>(entity)
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

            // Apply scroll offset to child's computed bounds for rendering
            if (scrollOffset != Vector2.Zero)
            {
                ref var childRect = ref World.Get<UIRect>(child);
                var originalBounds = childRect.ComputedBounds;

                // Offset the computed bounds by the scroll position
                childRect.ComputedBounds = new Rectangle(
                    originalBounds.X - scrollOffset.X,
                    originalBounds.Y - scrollOffset.Y,
                    originalBounds.Width,
                    originalBounds.Height);

                RenderElement(child, depth + 1);

                // Restore original bounds
                childRect.ComputedBounds = originalBounds;
            }
            else
            {
                RenderElement(child, depth + 1);
            }
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

        // Render spinner
        if (World.Has<UISpinner>(entity))
        {
            ref readonly var spinner = ref World.Get<UISpinner>(entity);
            RenderSpinner(bounds, spinner);
        }

        // Render progress bar
        if (World.Has<UIProgressBar>(entity))
        {
            ref readonly var progressBar = ref World.Get<UIProgressBar>(entity);
            RenderProgressBar(bounds, progressBar);
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

        var destRect = bounds;
        var sourceRect = image.SourceRect;

        // If no source rect specified, use whole texture
        if (sourceRect.Width <= 0 || sourceRect.Height <= 0)
        {
            sourceRect = new Rectangle(0, 0, 1, 1); // Normalized coordinates
        }

        // Apply scale mode
        // For now, just draw the texture - more sophisticated scale modes would
        // require knowing the texture dimensions
        renderer2D.DrawTextureRegion(image.Texture, destRect, sourceRect, image.Tint);
    }

    private void RenderText(Rectangle bounds, in UIText text)
    {
        if (textRenderer is null || string.IsNullOrEmpty(text.Content))
        {
            return;
        }

        // Flush 2D batch first to ensure backgrounds are drawn before text
        renderer2D?.Flush();

        // Apply clipping for overflow modes (Hidden or Ellipsis)
        bool shouldClip = text.Overflow == TextOverflow.Hidden || text.Overflow == TextOverflow.Ellipsis;
        if (shouldClip && renderer2D is not null)
        {
            renderer2D.PushClip(bounds);
        }

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

        // Pop clip after rendering
        if (shouldClip && renderer2D is not null)
        {
            renderer2D.PopClip();
        }
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

    private void RenderSpinner(Rectangle bounds, in UISpinner spinner)
    {
        if (renderer2D is null)
        {
            return;
        }

        var centerX = bounds.X + bounds.Width / 2;
        var centerY = bounds.Y + bounds.Height / 2;
        var radius = Math.Min(bounds.Width, bounds.Height) / 2 - spinner.Thickness;

        switch (spinner.Style)
        {
            case SpinnerStyle.Circular:
                RenderCircularSpinner(centerX, centerY, radius, spinner);
                break;
            case SpinnerStyle.Dots:
                RenderDotsSpinner(centerX, centerY, radius, spinner);
                break;
            case SpinnerStyle.Bar:
                RenderBarSpinner(bounds, spinner);
                break;
        }
    }

    private void RenderCircularSpinner(float centerX, float centerY, float radius, in UISpinner spinner)
    {
        if (renderer2D is null)
        {
            return;
        }

        // Draw an arc by drawing line segments
        const int segments = 32;
        float arcAngle = spinner.ArcLength * MathF.PI * 2;
        float startAngle = spinner.CurrentAngle;
        float step = arcAngle / segments;

        for (int i = 0; i < segments; i++)
        {
            float angle1 = startAngle + i * step;
            float angle2 = startAngle + (i + 1) * step;

            float x1 = centerX + MathF.Cos(angle1) * radius;
            float y1 = centerY + MathF.Sin(angle1) * radius;
            float x2 = centerX + MathF.Cos(angle2) * radius;
            float y2 = centerY + MathF.Sin(angle2) * radius;

            renderer2D.DrawLine(x1, y1, x2, y2, spinner.Color, spinner.Thickness);
        }
    }

    private void RenderDotsSpinner(float centerX, float centerY, float radius, in UISpinner spinner)
    {
        if (renderer2D is null)
        {
            return;
        }

        int dotCount = Math.Max(3, spinner.ElementCount);
        float dotRadius = spinner.Thickness;
        float angleStep = MathF.PI * 2 / dotCount;

        // Find which dot is "active" based on current angle
        float normalizedAngle = spinner.CurrentAngle / (MathF.PI * 2);
        float activePosition = normalizedAngle * dotCount;

        for (int i = 0; i < dotCount; i++)
        {
            float angle = i * angleStep - MathF.PI / 2; // Start at top
            float x = centerX + MathF.Cos(angle) * radius;
            float y = centerY + MathF.Sin(angle) * radius;

            // Calculate opacity based on distance from active position
            float distance = Math.Min(
                Math.Abs(i - activePosition),
                Math.Min(Math.Abs(i - activePosition + dotCount), Math.Abs(i - activePosition - dotCount)));
            float normalizedDistance = distance / (dotCount / 2f);
            float opacity = Math.Clamp(1f - normalizedDistance * 0.7f, 0.3f, 1f);

            var color = spinner.Color with { W = spinner.Color.W * opacity };
            renderer2D.FillCircle(x, y, dotRadius, color);
        }
    }

    private void RenderBarSpinner(Rectangle bounds, in UISpinner spinner)
    {
        if (renderer2D is null || bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        // Oscillating bar - use 30% of width but clamp to reasonable bounds
        float barWidth = bounds.Width * 0.3f;
        barWidth = Math.Max(barWidth, 4f); // Minimum 4 pixels
        barWidth = Math.Min(barWidth, bounds.Width); // Can't exceed container

        float oscillation = (MathF.Sin(spinner.CurrentAngle) + 1f) / 2f; // 0 to 1
        float maxTravel = bounds.Width - barWidth;
        float barX = bounds.X + maxTravel * oscillation;

        // Clamp bar position to container bounds
        barX = Math.Clamp(barX, bounds.X, bounds.X + bounds.Width - barWidth);

        renderer2D.FillRect(barX, bounds.Y, barWidth, bounds.Height, spinner.Color);
    }

    private void RenderProgressBar(Rectangle bounds, in UIProgressBar progressBar)
    {
        if (renderer2D is null)
        {
            return;
        }

        // Draw background
        renderer2D.FillRect(bounds, progressBar.BackgroundColor);

        // Draw fill
        float fillWidth = bounds.Width * progressBar.AnimatedValue;
        if (fillWidth > 0)
        {
            renderer2D.FillRect(bounds.X, bounds.Y, fillWidth, bounds.Height, progressBar.FillColor);
        }

        // Draw text if enabled
        if (progressBar.ShowText && textRenderer is not null)
        {
            renderer2D.Flush();
            string text = $"{(int)(progressBar.AnimatedValue * 100)}%";
            textRenderer.DrawText(
                default,
                text.AsSpan(),
                bounds.X + bounds.Width / 2,
                bounds.Y + bounds.Height / 2,
                new Vector4(1, 1, 1, 1),
                TextAlignH.Center,
                TextAlignV.Middle);
            textRenderer.Flush();
        }
    }
}
