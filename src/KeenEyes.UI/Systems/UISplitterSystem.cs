using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI;

/// <summary>
/// System that handles splitter drag operations to resize panes.
/// </summary>
/// <remarks>
/// <para>
/// This system listens for drag events on splitter handles (<see cref="UISplitterHandle"/>)
/// and updates the parent splitter's <see cref="UISplitter.SplitRatio"/> accordingly.
/// </para>
/// <para>
/// The system respects minimum pane sizes defined in the splitter component and
/// fires <see cref="UISplitterChangedEvent"/> when the ratio changes.
/// </para>
/// <para>
/// This system should run in <see cref="SystemPhase.EarlyUpdate"/> phase after
/// <see cref="UIWindowSystem"/>.
/// </para>
/// </remarks>
public sealed class UISplitterSystem : SystemBase
{
    private EventSubscription? dragSubscription;

    /// <inheritdoc />
    protected override void OnInitialize()
    {
        // Subscribe to drag events for splitter handle movement
        dragSubscription = World.Subscribe<UIDragEvent>(OnDrag);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            dragSubscription?.Dispose();
            dragSubscription = null;
        }

        base.Dispose(disposing);
    }

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        // Splitter behavior is event-driven, no per-frame work needed
    }

    private void OnDrag(UIDragEvent e)
    {
        // Check if dragging a splitter handle
        if (!World.Has<UISplitterHandle>(e.Element))
        {
            return;
        }

        HandleSplitterDrag(e);
    }

    private void HandleSplitterDrag(UIDragEvent e)
    {
        ref readonly var handle = ref World.Get<UISplitterHandle>(e.Element);
        var splitterContainer = handle.SplitterContainer;

        if (!World.IsAlive(splitterContainer) || !World.Has<UISplitter>(splitterContainer))
        {
            return;
        }

        ref var splitter = ref World.Get<UISplitter>(splitterContainer);

        // Get the splitter's computed bounds to calculate ratio
        if (!World.Has<UIRect>(splitterContainer))
        {
            return;
        }

        ref readonly var rect = ref World.Get<UIRect>(splitterContainer);
        var bounds = rect.ComputedBounds;

        // Calculate the new ratio based on drag delta
        float oldRatio = splitter.SplitRatio;
        float newRatio;

        if (splitter.Orientation == LayoutDirection.Horizontal)
        {
            // Horizontal orientation: panes are side by side, drag changes X
            float totalWidth = bounds.Width - splitter.HandleSize;
            if (totalWidth <= 0)
            {
                return;
            }

            float firstPaneWidth = totalWidth * splitter.SplitRatio;
            firstPaneWidth += e.Delta.X;

            // Clamp to minimum sizes
            firstPaneWidth = Math.Max(firstPaneWidth, splitter.MinFirstPane);
            firstPaneWidth = Math.Min(firstPaneWidth, totalWidth - splitter.MinSecondPane);

            newRatio = firstPaneWidth / totalWidth;
        }
        else
        {
            // Vertical orientation: panes are stacked, drag changes Y
            float totalHeight = bounds.Height - splitter.HandleSize;
            if (totalHeight <= 0)
            {
                return;
            }

            float firstPaneHeight = totalHeight * splitter.SplitRatio;
            firstPaneHeight += e.Delta.Y;

            // Clamp to minimum sizes
            firstPaneHeight = Math.Max(firstPaneHeight, splitter.MinFirstPane);
            firstPaneHeight = Math.Min(firstPaneHeight, totalHeight - splitter.MinSecondPane);

            newRatio = firstPaneHeight / totalHeight;
        }

        // Clamp ratio to valid range
        newRatio = Math.Clamp(newRatio, 0.0f, 1.0f);

        // Only update if changed significantly (larger epsilon reduces jitter)
        if (Math.Abs(newRatio - oldRatio) > 0.001f)
        {
            splitter.SplitRatio = newRatio;

            // Update the first pane's size to reflect the new ratio
            UpdatePaneSizes(splitterContainer, splitter);

            // Mark layout dirty
            if (!World.Has<UILayoutDirtyTag>(splitterContainer))
            {
                World.Add(splitterContainer, new UILayoutDirtyTag());
            }

            // Fire changed event
            World.Send(new UISplitterChangedEvent(splitterContainer, oldRatio, newRatio));
        }
    }

    private void UpdatePaneSizes(Entity splitterContainer, in UISplitter splitter)
    {
        // Find and update the first pane's percentage size
        foreach (var child in World.GetChildren(splitterContainer))
        {
            if (World.Has<UISplitterFirstPane>(child) && World.Has<UIRect>(child))
            {
                ref var paneRect = ref World.Get<UIRect>(child);

                // Convert ratio to percentage (0-100)
                float percentage = splitter.SplitRatio * 100f;

                if (splitter.Orientation == LayoutDirection.Horizontal)
                {
                    paneRect.Size = new System.Numerics.Vector2(percentage, paneRect.Size.Y);
                }
                else
                {
                    paneRect.Size = new System.Numerics.Vector2(paneRect.Size.X, percentage);
                }

                // Mark this pane dirty too
                if (!World.Has<UILayoutDirtyTag>(child))
                {
                    World.Add(child, new UILayoutDirtyTag());
                }

                break;
            }
        }
    }
}
