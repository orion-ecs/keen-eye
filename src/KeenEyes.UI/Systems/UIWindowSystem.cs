using System.Numerics;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI;

/// <summary>
/// System that handles floating window behavior including dragging, closing, and z-order.
/// </summary>
/// <remarks>
/// <para>
/// This system listens for:
/// <list type="bullet">
/// <item>Drag events on title bars (<see cref="UIWindowTitleBar"/>) to move windows</item>
/// <item>Click events on close buttons (<see cref="UIWindowCloseButton"/>) to hide windows</item>
/// <item>Click events on windows to bring them to front (z-order management)</item>
/// <item>Drag events on resize handles (<see cref="UIWindowResizeHandle"/>) to resize windows</item>
/// </list>
/// </para>
/// <para>
/// This system should run in <see cref="SystemPhase.EarlyUpdate"/> phase after
/// <see cref="UIInputSystem"/> and <see cref="UITabSystem"/>.
/// </para>
/// </remarks>
public sealed class UIWindowSystem : SystemBase
{
    private EventSubscription? dragSubscription;
    private EventSubscription? clickSubscription;
    private int nextZOrder = 1;

    /// <inheritdoc />
    protected override void OnInitialize()
    {
        // Subscribe to drag events for window movement and resizing
        dragSubscription = World.Subscribe<UIDragEvent>(OnDrag);

        // Subscribe to click events for close buttons and z-order
        clickSubscription = World.Subscribe<UIClickEvent>(OnClick);
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        dragSubscription?.Dispose();
        clickSubscription?.Dispose();
        dragSubscription = null;
        clickSubscription = null;
        base.Dispose();
    }

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        // Window behavior is event-driven, no per-frame work needed
    }

    private void OnDrag(UIDragEvent e)
    {
        // Check if dragging a title bar
        if (World.Has<UIWindowTitleBar>(e.Element))
        {
            HandleTitleBarDrag(e);
            return;
        }

        // Check if dragging a resize handle
        if (World.Has<UIWindowResizeHandle>(e.Element))
        {
            HandleResizeDrag(e);
        }
    }

    private void HandleTitleBarDrag(UIDragEvent e)
    {
        ref readonly var titleBar = ref World.Get<UIWindowTitleBar>(e.Element);
        var window = titleBar.Window;

        if (!World.IsAlive(window) || !World.Has<UIWindow>(window))
        {
            return;
        }

        ref readonly var windowComponent = ref World.Get<UIWindow>(window);
        if (!windowComponent.CanDrag)
        {
            return;
        }

        // Update the window position
        ref var rect = ref World.Get<UIRect>(window);
        rect.Offset = new UIEdges(
            rect.Offset.Left + e.Delta.X,
            rect.Offset.Top + e.Delta.Y,
            rect.Offset.Right,
            rect.Offset.Bottom
        );

        // Mark layout dirty
        if (!World.Has<UILayoutDirtyTag>(window))
        {
            World.Add(window, new UILayoutDirtyTag());
        }
    }

    private void HandleResizeDrag(UIDragEvent e)
    {
        ref readonly var handle = ref World.Get<UIWindowResizeHandle>(e.Element);
        var window = handle.Window;

        if (!World.IsAlive(window) || !World.Has<UIWindow>(window))
        {
            return;
        }

        ref readonly var windowComponent = ref World.Get<UIWindow>(window);
        if (!windowComponent.CanResize)
        {
            return;
        }

        ref var rect = ref World.Get<UIRect>(window);
        var newSize = rect.Size;
        var newOffset = rect.Offset;

        // Apply delta based on which edge(s) are being dragged
        if ((handle.Edge & ResizeEdge.Right) != 0)
        {
            newSize.X += e.Delta.X;
        }

        if ((handle.Edge & ResizeEdge.Left) != 0)
        {
            newSize.X -= e.Delta.X;
            newOffset = new UIEdges(newOffset.Left + e.Delta.X, newOffset.Top, newOffset.Right, newOffset.Bottom);
        }

        if ((handle.Edge & ResizeEdge.Bottom) != 0)
        {
            newSize.Y += e.Delta.Y;
        }

        if ((handle.Edge & ResizeEdge.Top) != 0)
        {
            newSize.Y -= e.Delta.Y;
            newOffset = new UIEdges(newOffset.Left, newOffset.Top + e.Delta.Y, newOffset.Right, newOffset.Bottom);
        }

        // Clamp to min/max size
        newSize.X = Math.Max(newSize.X, windowComponent.MinSize.X);
        newSize.Y = Math.Max(newSize.Y, windowComponent.MinSize.Y);

        if (windowComponent.MaxSize.X > 0)
        {
            newSize.X = Math.Min(newSize.X, windowComponent.MaxSize.X);
        }

        if (windowComponent.MaxSize.Y > 0)
        {
            newSize.Y = Math.Min(newSize.Y, windowComponent.MaxSize.Y);
        }

        rect.Size = newSize;
        rect.Offset = newOffset;

        // Mark layout dirty
        if (!World.Has<UILayoutDirtyTag>(window))
        {
            World.Add(window, new UILayoutDirtyTag());
        }
    }

    private void OnClick(UIClickEvent e)
    {
        // Check if clicking a close button
        if (World.Has<UIWindowCloseButton>(e.Element))
        {
            HandleCloseClick(e);
            return;
        }

        // Check if clicking anywhere in a window (for z-order)
        var clickedWindow = FindParentWindow(e.Element);
        if (clickedWindow.IsValid)
        {
            BringToFront(clickedWindow);
        }
    }

    private void HandleCloseClick(UIClickEvent e)
    {
        ref readonly var closeButton = ref World.Get<UIWindowCloseButton>(e.Element);
        var window = closeButton.Window;

        if (!World.IsAlive(window) || !World.Has<UIWindow>(window))
        {
            return;
        }

        ref readonly var windowComponent = ref World.Get<UIWindow>(window);
        if (!windowComponent.CanClose)
        {
            return;
        }

        // Hide the window
        if (World.Has<UIElement>(window))
        {
            ref var element = ref World.Get<UIElement>(window);
            element.Visible = false;
        }

        // Add hidden tag so layout skips it
        if (!World.Has<UIHiddenTag>(window))
        {
            World.Add(window, new UIHiddenTag());
        }

        // Fire window closed event
        World.Send(new UIWindowClosedEvent(window));
    }

    private Entity FindParentWindow(Entity entity)
    {
        var current = entity;

        while (current.IsValid)
        {
            if (World.Has<UIWindow>(current))
            {
                return current;
            }

            current = World.GetParent(current);
        }

        return Entity.Null;
    }

    private void BringToFront(Entity window)
    {
        if (!World.Has<UIWindow>(window))
        {
            return;
        }

        ref var windowComponent = ref World.Get<UIWindow>(window);

        // Only update if not already at the front
        if (windowComponent.ZOrder < nextZOrder - 1)
        {
            windowComponent.ZOrder = nextZOrder++;

            // Update the UIRect LocalZIndex to match
            if (World.Has<UIRect>(window))
            {
                ref var rect = ref World.Get<UIRect>(window);
                rect.LocalZIndex = (short)Math.Min(windowComponent.ZOrder, short.MaxValue);
            }
        }
    }

    /// <summary>
    /// Shows a window that was previously hidden.
    /// </summary>
    /// <param name="window">The window entity to show.</param>
    public void ShowWindow(Entity window)
    {
        if (!World.IsAlive(window) || !World.Has<UIWindow>(window))
        {
            return;
        }

        // Show the window
        if (World.Has<UIElement>(window))
        {
            ref var element = ref World.Get<UIElement>(window);
            element.Visible = true;
        }

        // Remove hidden tag
        if (World.Has<UIHiddenTag>(window))
        {
            World.Remove<UIHiddenTag>(window);
        }

        // Bring to front
        BringToFront(window);

        // Mark layout dirty
        if (!World.Has<UILayoutDirtyTag>(window))
        {
            World.Add(window, new UILayoutDirtyTag());
        }
    }
}
