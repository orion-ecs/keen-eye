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
    private I2DRenderer? renderer2D;
    private ITextRenderer? textRenderer;

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
            // Process all root canvases
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
        }
        finally
        {
            textRenderer?.End();
            renderer2D.End();
        }
    }

    private void TryInitializeRenderers()
    {
        // Try getting I2DRenderer directly as extension first
        if (!World.TryGetExtension(out renderer2D))
        {
            // Fall back to getting it from a 2D renderer provider (e.g., SilkGraphicsContext)
            if (World.TryGetExtension<I2DRendererProvider>(out var provider) && provider is not null)
            {
                renderer2D = provider.Get2DRenderer();
            }
        }

        // Try getting ITextRenderer directly as extension first
        if (!World.TryGetExtension(out textRenderer))
        {
            // Fall back to getting it from a text renderer provider (e.g., SilkGraphicsContext)
            if (World.TryGetExtension<ITextRendererProvider>(out var textProvider) && textProvider is not null)
            {
                textRenderer = textProvider.GetTextRenderer();
            }
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
