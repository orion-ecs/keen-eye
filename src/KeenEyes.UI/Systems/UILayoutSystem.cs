using System.Numerics;
using KeenEyes;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.Localization;
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
    private IFontManager? fontManager;
    private bool fontManagerInitialized;
    private ILocalization? localization;
    private bool localizationInitialized;
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

    private void TryInitializeFontManager()
    {
        if (fontManagerInitialized)
        {
            return;
        }

        fontManagerInitialized = true;

        if (World.TryGetExtension<IFontManagerProvider>(out var provider) && provider is not null)
        {
            fontManager = provider.GetFontManager();
        }
    }

    private void TryInitializeLocalization()
    {
        if (localizationInitialized)
        {
            return;
        }

        localizationInitialized = true;

        if (World.TryGetExtension<ILocalization>(out var loc) && loc is not null)
        {
            localization = loc;
        }
    }

    /// <summary>
    /// Gets the current text direction from the localization system.
    /// </summary>
    private TextDirection CurrentTextDirection =>
        localization?.CurrentTextDirection ?? TextDirection.LeftToRight;

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        // Lazy initialize dependencies
        TryInitializeFontManager();
        TryInitializeLocalization();

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
        // Using IWorld.GetChildren for hierarchy access
        var children = World.GetChildren(parent).ToList();
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
        bool isRtl = CurrentTextDirection == TextDirection.RightToLeft;

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

            // Measure FitContent dimensions before calculating anchored bounds
            // This is needed because CalculateAnchoredBounds uses Size.X/Y directly
            // when anchors match, and FitContent elements have Size=0 initially
            float measuredWidth = childRect.Size.X;
            float measuredHeight = childRect.Size.Y;

            if (childRect.WidthMode == UISizeMode.FitContent)
            {
                // For FitContent, use the larger of measured content width and Size.X (minimum width)
                float contentWidth = MeasureContentWidth(child, parentBounds);
                measuredWidth = Math.Max(childRect.Size.X, contentWidth);
            }

            if (childRect.HeightMode == UISizeMode.FitContent)
            {
                // For FitContent, use the larger of measured content height and Size.Y (minimum height)
                float contentHeight = MeasureContentHeight(child, parentBounds, measuredWidth);
                measuredHeight = Math.Max(childRect.Size.Y, contentHeight);
            }

            // Create a modified rect with measured sizes for calculation
            var rectForCalc = childRect;
            rectForCalc.Size = new System.Numerics.Vector2(measuredWidth, measuredHeight);

            bool shouldMirror = isRtl && childRect.MirrorForRtl;
            childRect.ComputedBounds = CalculateAnchoredBounds(rectForCalc, parentBounds, padding, shouldMirror);

            // Process this child's children
            ProcessChildren(child, childRect.ComputedBounds);
        }
    }

    private void ProcessFlexboxLayout(Entity parent, Rectangle parentBounds, List<Entity> children)
    {
        ref readonly var layout = ref World.Get<UILayout>(parent);
        ref readonly var parentRect = ref World.Get<UIRect>(parent);

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

        // Determine if we should reverse for RTL
        // For horizontal layouts with MirrorForRtl enabled, reverse the order when in RTL mode
        bool isRtl = CurrentTextDirection == TextDirection.RightToLeft;
        bool shouldMirrorForRtl = parentRect.MirrorForRtl && isRtl &&
                                   layout.Direction == LayoutDirection.Horizontal;

        // Reverse order logic:
        // - If ReverseOrder is true and no RTL mirroring: reverse
        // - If ReverseOrder is false and RTL mirroring: reverse
        // - If both or neither: don't reverse (they cancel out)
        bool shouldReverse = layout.ReverseOrder != shouldMirrorForRtl;

        if (shouldReverse)
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
        float availableMainSize = direction == LayoutDirection.Horizontal
            ? contentArea.Width
            : contentArea.Height;
        float totalSpacing = layoutSpacing * (layoutChildren.Count - 1);

        // Distribute remaining space to Fill children
        // First, identify Fill children and calculate non-Fill total
        var fillChildIndices = new List<int>();
        float nonFillMainSize = 0;

        for (int i = 0; i < layoutChildren.Count; i++)
        {
            var child = layoutChildren[i];
            ref readonly var childRect = ref World.Get<UIRect>(child);
            var size = childSizes[i];
            float mainSize = direction == LayoutDirection.Horizontal ? size.X : size.Y;

            bool isFillInMainAxis = direction == LayoutDirection.Horizontal
                ? childRect.WidthMode == UISizeMode.Fill
                : childRect.HeightMode == UISizeMode.Fill;

            if (isFillInMainAxis)
            {
                fillChildIndices.Add(i);
            }
            else
            {
                nonFillMainSize += mainSize;
            }
        }

        // Calculate size for Fill children
        float fillChildSize = 0;
        if (fillChildIndices.Count > 0)
        {
            float remainingSpace = availableMainSize - nonFillMainSize - totalSpacing;
            fillChildSize = Math.Max(0, remainingSpace / fillChildIndices.Count);

            // Update sizes for Fill children
            foreach (var idx in fillChildIndices)
            {
                var size = childSizes[idx];
                if (direction == LayoutDirection.Horizontal)
                {
                    childSizes[idx] = new Vector2(fillChildSize, size.Y);
                }
                else
                {
                    childSizes[idx] = new Vector2(size.X, fillChildSize);
                }
            }
        }

        // Recalculate total size after Fill distribution
        float totalMainSize = childSizes.Sum(s => direction == LayoutDirection.Horizontal ? s.X : s.Y);

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
                UISizeMode.FitContent => MeasureContentWidth(child, contentArea),
                UISizeMode.Fill => direction == LayoutDirection.Horizontal
                    ? 0 // Will be calculated later based on available space
                    : contentArea.Width,
                UISizeMode.Percentage => contentArea.Width * (childRect.Size.X / 100f),
                _ => childRect.Size.X
            };

            float height = childRect.HeightMode switch
            {
                UISizeMode.Fixed => childRect.Size.Y,
                UISizeMode.FitContent => MeasureContentHeight(child, contentArea, width),
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

    private float MeasureContentWidth(Entity entity, Rectangle availableArea)
    {
        // Get padding from style if present
        var padding = World.Has<UIStyle>(entity)
            ? World.Get<UIStyle>(entity).Padding
            : UIEdges.Zero;

        // Get layout info if present
        var hasLayout = World.Has<UILayout>(entity);
        var layout = hasLayout ? World.Get<UILayout>(entity) : default;

        // Get visible children (exclude menus - they're positioned absolutely and shouldn't affect parent layout)
        var children = World.GetChildren(entity)
            .Where(c => World.Has<UIRect>(c) && World.Has<UIElement>(c))
            .Where(c => World.Get<UIElement>(c).Visible && !World.Has<UIHiddenTag>(c))
            .Where(c => !World.Has<UIMenu>(c))
            .ToList();

        if (children.Count == 0)
        {
            // No children - measure text content if present
            if (World.Has<UIText>(entity) && fontManager is not null)
            {
                ref readonly var text = ref World.Get<UIText>(entity);
                if (!string.IsNullOrEmpty(text.Content) && text.Font.IsValid)
                {
                    var textWidth = fontManager.MeasureTextWidth(text.Font, text.Content);
                    return textWidth + padding.HorizontalSize;
                }
            }
            return padding.HorizontalSize;
        }

        // Measure children recursively
        float totalWidth = 0;
        float maxWidth = 0;
        var isHorizontal = hasLayout && layout.Direction == LayoutDirection.Horizontal;
        var spacing = hasLayout ? layout.Spacing : 0;

        foreach (var child in children)
        {
            ref readonly var childRect = ref World.Get<UIRect>(child);
            float childWidth = childRect.WidthMode switch
            {
                UISizeMode.Fixed => childRect.Size.X,
                UISizeMode.FitContent => MeasureContentWidth(child, availableArea),
                UISizeMode.Percentage => availableArea.Width * (childRect.Size.X / 100f),
                _ => childRect.Size.X
            };

            if (isHorizontal)
            {
                totalWidth += childWidth;
            }
            maxWidth = Math.Max(maxWidth, childWidth);
        }

        if (isHorizontal)
        {
            return totalWidth + spacing * (children.Count - 1) + padding.HorizontalSize;
        }
        return maxWidth + padding.HorizontalSize;
    }

    private float MeasureContentHeight(Entity entity, Rectangle availableArea, float resolvedWidth)
    {
        // Get padding from style if present
        var padding = World.Has<UIStyle>(entity)
            ? World.Get<UIStyle>(entity).Padding
            : UIEdges.Zero;

        // Get layout info if present
        var hasLayout = World.Has<UILayout>(entity);
        var layout = hasLayout ? World.Get<UILayout>(entity) : default;

        // Get visible children (exclude menus - they're positioned absolutely and shouldn't affect parent layout)
        var children = World.GetChildren(entity)
            .Where(c => World.Has<UIRect>(c) && World.Has<UIElement>(c))
            .Where(c => World.Get<UIElement>(c).Visible && !World.Has<UIHiddenTag>(c))
            .Where(c => !World.Has<UIMenu>(c))
            .ToList();

        if (children.Count == 0)
        {
            // No children - measure text content if present
            if (World.Has<UIText>(entity) && fontManager is not null)
            {
                ref readonly var text = ref World.Get<UIText>(entity);
                if (!string.IsNullOrEmpty(text.Content) && text.Font.IsValid)
                {
                    var textSize = fontManager.MeasureText(text.Font, text.Content);
                    return textSize.Y + padding.VerticalSize;
                }
            }
            return padding.VerticalSize;
        }

        // Measure children recursively
        float totalHeight = 0;
        float maxHeight = 0;
        var isVertical = !hasLayout || layout.Direction == LayoutDirection.Vertical;
        var spacing = hasLayout ? layout.Spacing : 0;

        // Create a working area with the resolved width
        var childArea = new Rectangle(availableArea.X, availableArea.Y, resolvedWidth, availableArea.Height);

        foreach (var child in children)
        {
            ref readonly var childRect = ref World.Get<UIRect>(child);

            // First resolve width for this child
            float childWidth = childRect.WidthMode switch
            {
                UISizeMode.Fixed => childRect.Size.X,
                UISizeMode.FitContent => MeasureContentWidth(child, childArea),
                UISizeMode.Fill => resolvedWidth - padding.HorizontalSize,
                UISizeMode.Percentage => resolvedWidth * (childRect.Size.X / 100f),
                _ => childRect.Size.X
            };

            // Then measure height
            float childHeight = childRect.HeightMode switch
            {
                UISizeMode.Fixed => childRect.Size.Y,
                UISizeMode.FitContent => MeasureContentHeight(child, childArea, childWidth),
                UISizeMode.Percentage => availableArea.Height * (childRect.Size.Y / 100f),
                _ => childRect.Size.Y
            };

            if (isVertical)
            {
                totalHeight += childHeight;
            }
            maxHeight = Math.Max(maxHeight, childHeight);
        }

        if (isVertical)
        {
            return totalHeight + spacing * (children.Count - 1) + padding.VerticalSize;
        }
        return maxHeight + padding.VerticalSize;
    }

    private static Rectangle CalculateAnchoredBounds(in UIRect rect, Rectangle parentBounds, UIEdges padding, bool mirrorHorizontal = false)
    {
        // For RTL mirroring, we invert horizontal anchor positions and swap left/right offsets
        float anchorMinX = mirrorHorizontal ? 1f - rect.AnchorMax.X : rect.AnchorMin.X;
        float anchorMaxX = mirrorHorizontal ? 1f - rect.AnchorMin.X : rect.AnchorMax.X;
        float offsetLeft = mirrorHorizontal ? rect.Offset.Right : rect.Offset.Left;
        float offsetRight = mirrorHorizontal ? rect.Offset.Left : rect.Offset.Right;
        float pivotX = mirrorHorizontal ? 1f - rect.Pivot.X : rect.Pivot.X;

        // Calculate anchor points in parent space
        float anchorLeft = parentBounds.X + padding.Left + anchorMinX * (parentBounds.Width - padding.HorizontalSize);
        float anchorRight = parentBounds.X + padding.Left + anchorMaxX * (parentBounds.Width - padding.HorizontalSize);
        float anchorTop = parentBounds.Y + padding.Top + rect.AnchorMin.Y * (parentBounds.Height - padding.VerticalSize);
        float anchorBottom = parentBounds.Y + padding.Top + rect.AnchorMax.Y * (parentBounds.Height - padding.VerticalSize);

        // Calculate bounds based on anchor spread
        float x, y, width, height;

        // Horizontal
        if (Math.Abs(anchorMinX - anchorMaxX) < 0.0001f)
        {
            // Anchors match - use Size for width
            // Note: For FitContent mode, caller must pre-measure and set Size.X
            width = rect.Size.X;
            x = anchorLeft + offsetLeft - pivotX * width;
        }
        else
        {
            // Anchors differ - stretch between anchors
            x = anchorLeft + offsetLeft;
            width = anchorRight - anchorLeft - offsetLeft - offsetRight;
        }

        // Vertical
        if (Math.Abs(rect.AnchorMin.Y - rect.AnchorMax.Y) < 0.0001f)
        {
            // Anchors match - use Size for height
            // Note: For FitContent mode, caller must pre-measure and set Size.Y
            height = rect.Size.Y;
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
