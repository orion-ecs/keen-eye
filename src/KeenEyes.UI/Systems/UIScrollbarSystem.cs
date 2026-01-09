using System.Numerics;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI;

/// <summary>
/// System that handles scrollbar thumb drag interactions and content size updates.
/// </summary>
/// <remarks>
/// <para>
/// This system listens for drag events on scrollbar thumbs and updates
/// the parent ScrollView's scroll position accordingly. It also dynamically
/// calculates ContentSize from the content panel computed bounds and
/// syncs scrollbar thumb position/size with scroll state.
/// </para>
/// </remarks>
public sealed class UIScrollbarSystem : SystemBase
{
    private EventSubscription? dragSubscription;

    /// <inheritdoc />
    protected override void OnInitialize()
    {
        dragSubscription = World.Subscribe<UIDragEvent>(OnDrag);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            dragSubscription?.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        // First, update ContentSize for all scrollable entities based on content panel bounds
        UpdateContentSizes();

        // Sync scrollbar thumb position and size with scroll state
        foreach (var thumbEntity in World.Query<UIScrollbarThumb, UIRect>())
        {
            ref readonly var thumb = ref World.Get<UIScrollbarThumb>(thumbEntity);

            if (!World.IsAlive(thumb.ScrollView) || !World.Has<UIScrollable>(thumb.ScrollView))
            {
                continue;
            }

            ref readonly var scrollable = ref World.Get<UIScrollable>(thumb.ScrollView);

            if (!World.Has<UIRect>(thumb.ScrollView))
            {
                continue;
            }

            ref readonly var scrollViewRect = ref World.Get<UIRect>(thumb.ScrollView);
            var viewportSize = scrollViewRect.ComputedBounds.Size;

            ref var thumbRect = ref World.Get<UIRect>(thumbEntity);

            if (thumb.IsVertical && scrollable.VerticalScroll)
            {
                UpdateVerticalThumb(ref thumbRect, scrollable, viewportSize);
            }
            else if (!thumb.IsVertical && scrollable.HorizontalScroll)
            {
                UpdateHorizontalThumb(ref thumbRect, scrollable, viewportSize);
            }
        }
    }

    private void UpdateContentSizes()
    {
        // Update ContentSize for all UIScrollable entities based on their content panel
        foreach (var scrollViewEntity in World.Query<UIScrollable, UIRect>())
        {
            ref var scrollable = ref World.Get<UIScrollable>(scrollViewEntity);

            // Find the content panel (first child that is not a scrollbar track)
            Entity contentPanel = Entity.Null;
            foreach (var child in World.GetChildren(scrollViewEntity))
            {
                // Skip scrollbar tracks - they have UIScrollbarThumb children
                var hasScrollbarChild = false;
                foreach (var grandchild in World.GetChildren(child))
                {
                    if (World.Has<UIScrollbarThumb>(grandchild))
                    {
                        hasScrollbarChild = true;
                        break;
                    }
                }

                if (!hasScrollbarChild && World.Has<UIRect>(child))
                {
                    contentPanel = child;
                    break;
                }
            }

            if (!contentPanel.IsValid || !World.Has<UIRect>(contentPanel))
            {
                continue;
            }

            ref readonly var contentRect = ref World.Get<UIRect>(contentPanel);
            var contentSize = contentRect.ComputedBounds.Size;

            // Only update if content size is valid and different
            if (contentSize.X > 0 || contentSize.Y > 0)
            {
                scrollable.ContentSize = contentSize;
            }
        }
    }

    private static void UpdateVerticalThumb(ref UIRect thumbRect, in UIScrollable scrollable, Vector2 viewportSize)
    {
        if (scrollable.ContentSize.Y <= 0 || viewportSize.Y <= 0)
        {
            return;
        }

        // Calculate thumb size ratio (viewport / content), clamped to 0-1
        float thumbRatio = Math.Min(1f, viewportSize.Y / scrollable.ContentSize.Y);

        // Calculate thumb position ratio based on scroll position
        var maxScroll = scrollable.GetMaxScroll(viewportSize);
        float positionRatio = maxScroll.Y > 0
            ? scrollable.ScrollPosition.Y / maxScroll.Y
            : 0;

        // Update anchors to position and size the thumb
        float thumbSize = thumbRatio;
        float thumbPos = positionRatio * (1f - thumbSize);

        thumbRect.AnchorMin = new Vector2(0, thumbPos);
        thumbRect.AnchorMax = new Vector2(1, thumbPos + thumbSize);
    }

    private static void UpdateHorizontalThumb(ref UIRect thumbRect, in UIScrollable scrollable, Vector2 viewportSize)
    {
        if (scrollable.ContentSize.X <= 0 || viewportSize.X <= 0)
        {
            return;
        }

        // Calculate thumb size ratio (viewport / content), clamped to 0-1
        float thumbRatio = Math.Min(1f, viewportSize.X / scrollable.ContentSize.X);

        // Calculate thumb position ratio based on scroll position
        var maxScroll = scrollable.GetMaxScroll(viewportSize);
        float positionRatio = maxScroll.X > 0
            ? scrollable.ScrollPosition.X / maxScroll.X
            : 0;

        // Update anchors to position and size the thumb
        float thumbSize = thumbRatio;
        float thumbPos = positionRatio * (1f - thumbSize);

        thumbRect.AnchorMin = new Vector2(thumbPos, 0);
        thumbRect.AnchorMax = new Vector2(thumbPos + thumbSize, 1);
    }

    private void OnDrag(UIDragEvent e)
    {
        var thumbEntity = e.Element;

        if (!World.IsAlive(thumbEntity) || !World.Has<UIScrollbarThumb>(thumbEntity))
        {
            return;
        }

        ref readonly var thumb = ref World.Get<UIScrollbarThumb>(thumbEntity);
        var scrollViewEntity = thumb.ScrollView;

        if (!World.IsAlive(scrollViewEntity) || !World.Has<UIScrollable>(scrollViewEntity))
        {
            return;
        }

        ref var scrollable = ref World.Get<UIScrollable>(scrollViewEntity);

        // Get the ScrollView computed bounds to know the viewport size
        if (!World.Has<UIRect>(scrollViewEntity))
        {
            return;
        }

        ref readonly var scrollViewRect = ref World.Get<UIRect>(scrollViewEntity);
        var viewportSize = scrollViewRect.ComputedBounds.Size;

        // Calculate the scrollable range
        var maxScroll = scrollable.GetMaxScroll(viewportSize);

        if (thumb.IsVertical)
        {
            // For vertical scrollbar, drag delta Y affects scroll Y
            // The ratio of drag movement to scroll movement depends on
            // the ratio of viewport to content size
            if (maxScroll.Y > 0 && viewportSize.Y > 0)
            {
                // Calculate scroll delta based on drag delta
                // drag in viewport space -> scroll in content space
                var trackHeight = viewportSize.Y;
                var scrollRatio = scrollable.ContentSize.Y / trackHeight;
                var scrollDelta = e.Delta.Y * scrollRatio;

                scrollable.ScrollPosition = new Vector2(
                    scrollable.ScrollPosition.X,
                    Math.Clamp(scrollable.ScrollPosition.Y + scrollDelta, 0, maxScroll.Y));
            }
        }
        else
        {
            // For horizontal scrollbar, drag delta X affects scroll X
            if (maxScroll.X > 0 && viewportSize.X > 0)
            {
                var trackWidth = viewportSize.X;
                var scrollRatio = scrollable.ContentSize.X / trackWidth;
                var scrollDelta = e.Delta.X * scrollRatio;

                scrollable.ScrollPosition = new Vector2(
                    Math.Clamp(scrollable.ScrollPosition.X + scrollDelta, 0, maxScroll.X),
                    scrollable.ScrollPosition.Y);
            }
        }
    }
}
