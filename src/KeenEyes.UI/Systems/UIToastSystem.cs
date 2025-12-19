using System.Numerics;

using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI;

/// <summary>
/// System that handles toast notification lifecycle, auto-dismiss timing, and stacking.
/// </summary>
/// <remarks>
/// <para>
/// This system manages toast notifications including:
/// <list type="bullet">
/// <item>Auto-dismiss timing based on toast duration</item>
/// <item>Click-to-dismiss handling</item>
/// <item>Close button clicks</item>
/// <item>Stacking and positioning within containers</item>
/// </list>
/// </para>
/// <para>
/// This system should run in <see cref="SystemPhase.EarlyUpdate"/> phase to process
/// toast timers and dismissals.
/// </para>
/// </remarks>
public sealed class UIToastSystem : SystemBase
{
    private EventSubscription? clickSubscription;

    /// <inheritdoc />
    protected override void OnInitialize()
    {
        // Subscribe to click events for toast dismissal and close buttons
        clickSubscription = World.Subscribe<UIClickEvent>(OnClick);
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        clickSubscription?.Dispose();
        clickSubscription = null;
        base.Dispose();
    }

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        // Update toast timers and auto-dismiss
        var toastsToRemove = new List<Entity>();

        foreach (var entity in World.Query<UIToast>())
        {
            ref var toast = ref World.Get<UIToast>(entity);

            // Skip toasts that are already closing or have indefinite duration
            if (toast.IsClosing || toast.Duration <= 0)
            {
                continue;
            }

            toast.TimeRemaining -= deltaTime;

            if (toast.TimeRemaining <= 0)
            {
                toast.IsClosing = true;
                toastsToRemove.Add(entity);
            }
        }

        // Remove expired toasts
        foreach (var entity in toastsToRemove)
        {
            DismissToast(entity, wasManual: false);
        }
    }

    private void OnClick(UIClickEvent e)
    {
        // Check if clicking a toast close button
        if (World.Has<UIToastCloseButton>(e.Element))
        {
            HandleCloseButtonClick(e);
            return;
        }

        // Check if clicking directly on a toast
        if (World.Has<UIToast>(e.Element))
        {
            HandleToastClick(e);
        }
    }

    private void HandleCloseButtonClick(UIClickEvent e)
    {
        ref readonly var closeButton = ref World.Get<UIToastCloseButton>(e.Element);
        var toast = closeButton.Toast;

        if (!World.IsAlive(toast) || !World.Has<UIToast>(toast))
        {
            return;
        }

        DismissToast(toast, wasManual: true);
    }

    private void HandleToastClick(UIClickEvent e)
    {
        ref var toast = ref World.Get<UIToast>(e.Element);

        if (!toast.CanDismiss || toast.IsClosing)
        {
            return;
        }

        DismissToast(e.Element, wasManual: true);
    }

    /// <summary>
    /// Shows a toast notification by making it visible and firing events.
    /// </summary>
    /// <param name="toast">The toast entity to show.</param>
    public void ShowToast(Entity toast)
    {
        if (!World.IsAlive(toast) || !World.Has<UIToast>(toast))
        {
            return;
        }

        ref var toastComponent = ref World.Get<UIToast>(toast);

        // Reset the timer
        toastComponent.TimeRemaining = toastComponent.Duration;
        toastComponent.IsClosing = false;

        // Show the toast
        if (World.Has<UIElement>(toast))
        {
            ref var element = ref World.Get<UIElement>(toast);
            element.Visible = true;
        }

        // Remove hidden tag
        if (World.Has<UIHiddenTag>(toast))
        {
            World.Remove<UIHiddenTag>(toast);
        }

        // Fire shown event
        World.Send(new UIToastShownEvent(toast));
    }

    /// <summary>
    /// Dismisses a toast notification.
    /// </summary>
    /// <param name="toast">The toast entity to dismiss.</param>
    /// <param name="wasManual">Whether the dismissal was user-initiated.</param>
    public void DismissToast(Entity toast, bool wasManual)
    {
        if (!World.IsAlive(toast) || !World.Has<UIToast>(toast))
        {
            return;
        }

        ref var toastComponent = ref World.Get<UIToast>(toast);
        toastComponent.IsClosing = true;

        // Hide the toast
        if (World.Has<UIElement>(toast))
        {
            ref var element = ref World.Get<UIElement>(toast);
            element.Visible = false;
        }

        // Add hidden tag
        if (!World.Has<UIHiddenTag>(toast))
        {
            World.Add(toast, new UIHiddenTag());
        }

        // Fire dismissed event
        World.Send(new UIToastDismissedEvent(toast, wasManual));

        // Despawn the toast entity after dismissal
        World.Despawn(toast);
    }

    /// <summary>
    /// Gets the number of currently visible toasts in a container.
    /// </summary>
    /// <param name="container">The container entity.</param>
    /// <returns>The number of visible toasts.</returns>
    public int GetVisibleToastCount(Entity container)
    {
        int count = 0;

        foreach (var entity in World.Query<UIToast>())
        {
            ref readonly var toast = ref World.Get<UIToast>(entity);

            if (toast.Container == container && !toast.IsClosing)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Calculates the Y position for a new toast based on existing toasts.
    /// </summary>
    /// <param name="container">The container entity.</param>
    /// <param name="toastHeight">The height of the new toast.</param>
    /// <returns>The Y offset for the new toast.</returns>
    public float CalculateToastYPosition(Entity container, float toastHeight)
    {
        if (!World.IsAlive(container) || !World.Has<UIToastContainer>(container))
        {
            return 0;
        }

        ref readonly var containerComponent = ref World.Get<UIToastContainer>(container);
        float yOffset = containerComponent.Margin;

        foreach (var entity in World.Query<UIToast, UIElement>())
        {
            ref readonly var toast = ref World.Get<UIToast>(entity);

            if (toast.Container != container || toast.IsClosing)
            {
                continue;
            }

            ref readonly var rect = ref World.Get<UIRect>(entity);
            yOffset += rect.Size.Y + containerComponent.Spacing;
        }

        return yOffset;
    }
}
