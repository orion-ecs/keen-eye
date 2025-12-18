using System.Numerics;
using KeenEyes;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI;

/// <summary>
/// System that calculates layout bounds for UI elements.
/// </summary>
/// <remarks>
/// <para>
/// The layout system processes UI elements in hierarchy order (root to leaf) and calculates
/// the <see cref="UIRect.ComputedBounds"/> for each element based on its anchor settings
/// and parent bounds.
/// </para>
/// <para>
/// For elements with <see cref="UILayout"/> components, child elements are arranged
/// according to flexbox-style rules.
/// </para>
/// <para>
/// This system should run in <see cref="SystemPhase.LateUpdate"/> with a negative order
/// to ensure layout is calculated before rendering.
/// </para>
/// </remarks>
public sealed class UILayoutSystem : SystemBase
{
    private World? concreteWorld;
    private Vector2 screenSize = new(1280, 720); // Default, should be updated from window

    /// <summary>
    /// Sets the screen size used for root canvas layout.
    /// </summary>
    /// <param name="width">The screen width in pixels.</param>
    /// <param name="height">The screen height in pixels.</param>
    public void SetScreenSize(float width, float height)
    {
        screenSize = new Vector2(width, height);
    }

    /// <inheritdoc />
    protected override void OnInitialize()
    {
        concreteWorld = World as World;
    }

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        if (concreteWorld is null)
        {
            return;
        }

        // Process all root canvases
        foreach (var rootEntity in World.Query<UIElement, UIRect, UIRootTag>())
        {
            if (World.Has<UIHiddenTag>(rootEntity))
            {
                continue;
            }

            // Root canvas gets full screen bounds
            ref var rootRect = ref World.Get<UIRect>(rootEntity);
            rootRect.ComputedBounds = new Rectangle(0, 0, screenSize.X, screenSize.Y);

            // Process children recursively
            ProcessChildren(rootEntity, rootRect.ComputedBounds);
        }

        // Clear dirty tags
        foreach (var entity in World.Query<UILayoutDirtyTag>())
        {
            World.Remove<UILayoutDirtyTag>(entity);
        }
    }

    private void ProcessChildren(Entity parent, Rectangle parentBounds)
    {
        if (concreteWorld is null)
        {
            return;
        }

        var children = concreteWorld.GetChildren(parent).ToList();
        if (children.Count == 0)
        {
            return;
        }

        // Check if parent has layout component
        bool hasLayout = World.Has<UILayout>(parent);

        if (hasLayout)
        {
            ProcessFlexboxLayout(parent, parentBounds, children);
        }
        else
        {
            ProcessAnchorLayout(parentBounds, children);
        }
    }

    private void ProcessAnchorLayout(Rectangle parentBounds, List<Entity> children)
    {
        // Get padding from parent style if available
        var padding = UIEdges.Zero;

        foreach (var child in children)
        {
            if (!World.Has<UIRect>(child))
            {
                continue;
            }

            if (World.Has<UIHiddenTag>(child))
            {
                continue;
            }

            ref var childRect = ref World.Get<UIRect>(child);
            childRect.ComputedBounds = CalculateAnchoredBounds(childRect, parentBounds, padding);

            // Process this child's children
            ProcessChildren(child, childRect.ComputedBounds);
        }
    }

    private void ProcessFlexboxLayout(Entity parent, Rectangle parentBounds, List<Entity> children)
    {
        ref readonly var layout = ref World.Get<UILayout>(parent);

        // Get padding from parent style
        var padding = UIEdges.Zero;
        if (World.Has<UIStyle>(parent))
        {
            padding = World.Get<UIStyle>(parent).Padding;
        }

        // Calculate content area
        var contentArea = new Rectangle(
            parentBounds.X + padding.Left,
            parentBounds.Y + padding.Top,
            parentBounds.Width - padding.HorizontalSize,
            parentBounds.Height - padding.VerticalSize
        );

        // Filter to visible children with UIRect
        var layoutChildren = children
            .Where(c => World.Has<UIRect>(c) && !World.Has<UIHiddenTag>(c))
            .ToList();

        if (layoutChildren.Count == 0)
        {
            return;
        }

        // Reverse order if specified
        if (layout.ReverseOrder)
        {
            layoutChildren.Reverse();
        }

        // Capture values from ref struct for use in lambda
        var direction = layout.Direction;
        var mainAxisAlign = layout.MainAxisAlign;
        var crossAxisAlign = layout.CrossAxisAlign;
        var layoutSpacing = layout.Spacing;
        var wrap = layout.Wrap;

        // Measure children
        var childSizes = MeasureChildren(layoutChildren, contentArea, direction);

        // If wrapping is enabled, use the wrapping layout algorithm
        if (wrap)
        {
            ProcessWrappingLayout(layoutChildren, childSizes, contentArea, direction, mainAxisAlign, crossAxisAlign, layoutSpacing);
            return;
        }

        // Non-wrapping layout (original algorithm)
        ProcessSingleLineLayout(layoutChildren, childSizes, contentArea, direction, mainAxisAlign, crossAxisAlign, layoutSpacing);
    }

    private void ProcessSingleLineLayout(
        List<Entity> layoutChildren,
        List<Vector2> childSizes,
        Rectangle contentArea,
        LayoutDirection direction,
        LayoutAlign mainAxisAlign,
        LayoutAlign crossAxisAlign,
        float layoutSpacing)
    {
        // Calculate total size and spacing
        float totalMainSize = childSizes.Sum(s => direction == LayoutDirection.Horizontal ? s.X : s.Y);
        float totalSpacing = layoutSpacing * (layoutChildren.Count - 1);
        float availableMainSize = direction == LayoutDirection.Horizontal
            ? contentArea.Width
            : contentArea.Height;

        // Calculate starting position and spacing based on alignment
        float mainStart = direction == LayoutDirection.Horizontal ? contentArea.X : contentArea.Y;
        float spacing = layoutSpacing;

        switch (mainAxisAlign)
        {
            case LayoutAlign.Center:
                mainStart += (availableMainSize - totalMainSize - totalSpacing) / 2;
                break;

            case LayoutAlign.End:
                mainStart += availableMainSize - totalMainSize - totalSpacing;
                break;

            case LayoutAlign.SpaceBetween:
                if (layoutChildren.Count > 1)
                {
                    spacing = (availableMainSize - totalMainSize) / (layoutChildren.Count - 1);
                }
                break;

            case LayoutAlign.SpaceAround:
                if (layoutChildren.Count > 0)
                {
                    float totalSpace = availableMainSize - totalMainSize;
                    spacing = totalSpace / layoutChildren.Count;
                    mainStart += spacing / 2;
                }
                break;

            case LayoutAlign.SpaceEvenly:
                if (layoutChildren.Count > 0)
                {
                    spacing = (availableMainSize - totalMainSize) / (layoutChildren.Count + 1);
                    mainStart += spacing;
                }
                break;
        }

        // Position children
        float currentPos = mainStart;
        for (int i = 0; i < layoutChildren.Count; i++)
        {
            var child = layoutChildren[i];
            ref var childRect = ref World.Get<UIRect>(child);
            var childSize = childSizes[i];

            // Calculate cross axis position
            float crossStart = direction == LayoutDirection.Horizontal ? contentArea.Y : contentArea.X;
            float crossSize = direction == LayoutDirection.Horizontal ? contentArea.Height : contentArea.Width;
            float childCrossSize = direction == LayoutDirection.Horizontal ? childSize.Y : childSize.X;

            float crossPos = crossAxisAlign switch
            {
                LayoutAlign.Center => crossStart + (crossSize - childCrossSize) / 2,
                LayoutAlign.End => crossStart + crossSize - childCrossSize,
                _ => crossStart
            };

            // Set computed bounds
            if (direction == LayoutDirection.Horizontal)
            {
                childRect.ComputedBounds = new Rectangle(currentPos, crossPos, childSize.X, childSize.Y);
                currentPos += childSize.X + spacing;
            }
            else
            {
                childRect.ComputedBounds = new Rectangle(crossPos, currentPos, childSize.X, childSize.Y);
                currentPos += childSize.Y + spacing;
            }

            // Process this child's children
            ProcessChildren(child, childRect.ComputedBounds);
        }
    }

    private void ProcessWrappingLayout(
        List<Entity> layoutChildren,
        List<Vector2> childSizes,
        Rectangle contentArea,
        LayoutDirection direction,
        LayoutAlign mainAxisAlign,
        LayoutAlign crossAxisAlign,
        float spacing)
    {
        float availableMainSize = direction == LayoutDirection.Horizontal
            ? contentArea.Width
            : contentArea.Height;

        // Group children into lines
        var lines = new List<List<(Entity entity, Vector2 size)>>();
        var currentLine = new List<(Entity entity, Vector2 size)>();
        float currentLineMainSize = 0;

        for (int i = 0; i < layoutChildren.Count; i++)
        {
            var child = layoutChildren[i];
            var size = childSizes[i];
            float childMainSize = direction == LayoutDirection.Horizontal ? size.X : size.Y;

            // Check if adding this child would overflow the line
            float projectedSize = currentLineMainSize + childMainSize;
            if (currentLine.Count > 0)
            {
                projectedSize += spacing; // Add spacing between items
            }

            if (currentLine.Count > 0 && projectedSize > availableMainSize)
            {
                // Start a new line
                lines.Add(currentLine);
                currentLine = [];
                currentLineMainSize = 0;
            }

            currentLine.Add((child, size));
            currentLineMainSize += childMainSize + (currentLine.Count > 1 ? spacing : 0);
        }

        // Add the last line
        if (currentLine.Count > 0)
        {
            lines.Add(currentLine);
        }

        // Calculate line heights (max cross size in each line)
        var lineHeights = new List<float>();
        foreach (var line in lines)
        {
            float maxCrossSize = 0;
            foreach (var (_, size) in line)
            {
                float crossSize = direction == LayoutDirection.Horizontal ? size.Y : size.X;
                maxCrossSize = Math.Max(maxCrossSize, crossSize);
            }
            lineHeights.Add(maxCrossSize);
        }

        // Calculate total cross size needed
        float totalCrossSize = lineHeights.Sum() + spacing * (lines.Count - 1);
        float availableCrossSize = direction == LayoutDirection.Horizontal
            ? contentArea.Height
            : contentArea.Width;

        // Calculate cross axis starting position based on alignment
        float crossStart = direction == LayoutDirection.Horizontal ? contentArea.Y : contentArea.X;

        // Note: For wrap layouts, crossAxisAlign aligns content within each line,
        // while the lines themselves start from the top/left

        // Position each line
        float currentCrossPos = crossStart;
        for (int lineIndex = 0; lineIndex < lines.Count; lineIndex++)
        {
            var line = lines[lineIndex];
            float lineHeight = lineHeights[lineIndex];

            // Calculate main axis positions for this line
            float lineMainSize = 0;
            foreach (var (_, size) in line)
            {
                lineMainSize += direction == LayoutDirection.Horizontal ? size.X : size.Y;
            }
            lineMainSize += spacing * (line.Count - 1);

            float mainStart = direction == LayoutDirection.Horizontal ? contentArea.X : contentArea.Y;
            float lineSpacing = spacing;

            // Apply main axis alignment for this line
            switch (mainAxisAlign)
            {
                case LayoutAlign.Center:
                    mainStart += (availableMainSize - lineMainSize) / 2;
                    break;

                case LayoutAlign.End:
                    mainStart += availableMainSize - lineMainSize;
                    break;

                case LayoutAlign.SpaceBetween:
                    if (line.Count > 1)
                    {
                        float totalItemSize = line.Sum(item =>
                            direction == LayoutDirection.Horizontal ? item.size.X : item.size.Y);
                        lineSpacing = (availableMainSize - totalItemSize) / (line.Count - 1);
                    }
                    break;

                case LayoutAlign.SpaceAround:
                    if (line.Count > 0)
                    {
                        float totalItemSize = line.Sum(item =>
                            direction == LayoutDirection.Horizontal ? item.size.X : item.size.Y);
                        float totalSpace = availableMainSize - totalItemSize;
                        lineSpacing = totalSpace / line.Count;
                        mainStart += lineSpacing / 2;
                    }
                    break;

                case LayoutAlign.SpaceEvenly:
                    if (line.Count > 0)
                    {
                        float totalItemSize = line.Sum(item =>
                            direction == LayoutDirection.Horizontal ? item.size.X : item.size.Y);
                        lineSpacing = (availableMainSize - totalItemSize) / (line.Count + 1);
                        mainStart += lineSpacing;
                    }
                    break;
            }

            // Position items in this line
            float currentMainPos = mainStart;
            foreach (var (child, size) in line)
            {
                ref var childRect = ref World.Get<UIRect>(child);

                // Calculate cross position within line based on alignment
                float childCrossSize = direction == LayoutDirection.Horizontal ? size.Y : size.X;
                float itemCrossPos = crossAxisAlign switch
                {
                    LayoutAlign.Center => currentCrossPos + (lineHeight - childCrossSize) / 2,
                    LayoutAlign.End => currentCrossPos + lineHeight - childCrossSize,
                    _ => currentCrossPos
                };

                // Set computed bounds
                if (direction == LayoutDirection.Horizontal)
                {
                    childRect.ComputedBounds = new Rectangle(currentMainPos, itemCrossPos, size.X, size.Y);
                    currentMainPos += size.X + lineSpacing;
                }
                else
                {
                    childRect.ComputedBounds = new Rectangle(itemCrossPos, currentMainPos, size.X, size.Y);
                    currentMainPos += size.Y + lineSpacing;
                }

                // Process this child's children
                ProcessChildren(child, childRect.ComputedBounds);
            }

            // Move to next line
            currentCrossPos += lineHeight + spacing;
        }
    }

    private List<Vector2> MeasureChildren(List<Entity> children, Rectangle contentArea, LayoutDirection direction)
    {
        var sizes = new List<Vector2>();

        foreach (var child in children)
        {
            ref readonly var childRect = ref World.Get<UIRect>(child);

            float width = childRect.WidthMode switch
            {
                UISizeMode.Fixed => childRect.Size.X,
                UISizeMode.Fill => direction == LayoutDirection.Horizontal
                    ? 0 // Will be calculated later based on available space
                    : contentArea.Width,
                UISizeMode.Percentage => contentArea.Width * (childRect.Size.X / 100f),
                _ => childRect.Size.X
            };

            float height = childRect.HeightMode switch
            {
                UISizeMode.Fixed => childRect.Size.Y,
                UISizeMode.Fill => direction == LayoutDirection.Vertical
                    ? 0 // Will be calculated later based on available space
                    : contentArea.Height,
                UISizeMode.Percentage => contentArea.Height * (childRect.Size.Y / 100f),
                _ => childRect.Size.Y
            };

            sizes.Add(new Vector2(width, height));
        }

        return sizes;
    }

    private static Rectangle CalculateAnchoredBounds(in UIRect rect, Rectangle parentBounds, UIEdges padding)
    {
        // Calculate anchor points in parent space
        float anchorLeft = parentBounds.X + padding.Left + rect.AnchorMin.X * (parentBounds.Width - padding.HorizontalSize);
        float anchorRight = parentBounds.X + padding.Left + rect.AnchorMax.X * (parentBounds.Width - padding.HorizontalSize);
        float anchorTop = parentBounds.Y + padding.Top + rect.AnchorMin.Y * (parentBounds.Height - padding.VerticalSize);
        float anchorBottom = parentBounds.Y + padding.Top + rect.AnchorMax.Y * (parentBounds.Height - padding.VerticalSize);

        // Calculate bounds based on anchor spread
        float x, y, width, height;

        // Horizontal
        if (Math.Abs(rect.AnchorMin.X - rect.AnchorMax.X) < 0.0001f)
        {
            // Anchors match - use Size for width
            width = rect.WidthMode == UISizeMode.Fixed ? rect.Size.X : rect.Size.X;
            x = anchorLeft + rect.Offset.Left - rect.Pivot.X * width;
        }
        else
        {
            // Anchors differ - stretch between anchors
            x = anchorLeft + rect.Offset.Left;
            width = anchorRight - anchorLeft - rect.Offset.Left - rect.Offset.Right;
        }

        // Vertical
        if (Math.Abs(rect.AnchorMin.Y - rect.AnchorMax.Y) < 0.0001f)
        {
            // Anchors match - use Size for height
            height = rect.HeightMode == UISizeMode.Fixed ? rect.Size.Y : rect.Size.Y;
            y = anchorTop + rect.Offset.Top - rect.Pivot.Y * height;
        }
        else
        {
            // Anchors differ - stretch between anchors
            y = anchorTop + rect.Offset.Top;
            height = anchorBottom - anchorTop - rect.Offset.Top - rect.Offset.Bottom;
        }

        return new Rectangle(x, y, Math.Max(0, width), Math.Max(0, height));
    }
}
