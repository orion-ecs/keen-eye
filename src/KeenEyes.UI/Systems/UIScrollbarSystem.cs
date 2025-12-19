using System.Numerics;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI;

/// <summary>
/// System that handles scrollbar thumb drag interactions.
/// </summary>
/// <remarks>
/// <para>
/// This system listens for drag events on scrollbar thumbs and updates
/// the parent ScrollView's scroll position accordingly.
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
    public override void Dispose()
    {
        dragSubscription?.Dispose();
        base.Dispose();
    }

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        // Event-driven via subscriptions
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

        // Get the ScrollView's computed bounds to know the viewport size
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
