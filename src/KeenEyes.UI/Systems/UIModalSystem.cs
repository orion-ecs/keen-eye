using KeenEyes.Input.Abstractions;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI;

/// <summary>
/// System that handles modal dialog behavior including backdrop clicks, Escape key, and button actions.
/// </summary>
/// <remarks>
/// <para>
/// This system manages modal dialogs which are overlay windows that block interaction with the
/// rest of the UI until dismissed. It handles:
/// <list type="bullet">
/// <item>Backdrop click to close (if enabled)</item>
/// <item>Escape key to close (if enabled)</item>
/// <item>Action button clicks (OK, Cancel, etc.)</item>
/// <item>Close button clicks</item>
/// </list>
/// </para>
/// <para>
/// This system should run in <see cref="SystemPhase.EarlyUpdate"/> phase after
/// <see cref="UIWindowSystem"/> to handle modal-specific behavior.
/// </para>
/// </remarks>
public sealed class UIModalSystem : SystemBase
{
    private EventSubscription? clickSubscription;
    private IInputContext? inputContext;
    private bool escapeWasDown;

    /// <inheritdoc />
    protected override void OnInitialize()
    {
        // Subscribe to click events for backdrop, close buttons, and action buttons
        clickSubscription = World.Subscribe<UIClickEvent>(OnClick);

        // Try to get input context for keyboard handling
        World.TryGetExtension(out inputContext);
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
        // Handle Escape key for open modals
        if (inputContext == null)
        {
            World.TryGetExtension(out inputContext);
            if (inputContext == null)
            {
                return;
            }
        }

        var keyboard = inputContext.Keyboard;

        // Check for Escape key press (transition from up to down)
        bool escapeIsDown = keyboard.IsKeyDown(Key.Escape);
        if (escapeIsDown && !escapeWasDown)
        {
            HandleEscapeKey();
        }
        escapeWasDown = escapeIsDown;
    }

    private void HandleEscapeKey()
    {
        // Find the topmost open modal that allows Escape to close
        foreach (var entity in World.Query<UIModal>())
        {
            ref readonly var modal = ref World.Get<UIModal>(entity);

            if (modal.IsOpen && modal.CloseOnEscape)
            {
                CloseModal(entity, ModalResult.Cancel);
                break; // Only close the topmost modal
            }
        }
    }

    private void OnClick(UIClickEvent e)
    {
        // Check if clicking a modal backdrop
        if (World.Has<UIModalBackdrop>(e.Element))
        {
            HandleBackdropClick(e);
            return;
        }

        // Check if clicking a modal close button
        if (World.Has<UIModalCloseButton>(e.Element))
        {
            HandleCloseButtonClick(e);
            return;
        }

        // Check if clicking a modal action button
        if (World.Has<UIModalButton>(e.Element))
        {
            HandleModalButtonClick(e);
        }
    }

    private void HandleBackdropClick(UIClickEvent e)
    {
        ref readonly var backdrop = ref World.Get<UIModalBackdrop>(e.Element);
        var modal = backdrop.Modal;

        if (!World.IsAlive(modal) || !World.Has<UIModal>(modal))
        {
            return;
        }

        ref readonly var modalComponent = ref World.Get<UIModal>(modal);
        if (!modalComponent.CloseOnBackdropClick)
        {
            return;
        }

        CloseModal(modal, ModalResult.None);
    }

    private void HandleCloseButtonClick(UIClickEvent e)
    {
        ref readonly var closeButton = ref World.Get<UIModalCloseButton>(e.Element);
        var modal = closeButton.Modal;

        if (!World.IsAlive(modal) || !World.Has<UIModal>(modal))
        {
            return;
        }

        CloseModal(modal, ModalResult.None);
    }

    private void HandleModalButtonClick(UIClickEvent e)
    {
        ref readonly var button = ref World.Get<UIModalButton>(e.Element);
        var modal = button.Modal;

        if (!World.IsAlive(modal) || !World.Has<UIModal>(modal))
        {
            return;
        }

        // Fire the result event before closing
        World.Send(new UIModalResultEvent(modal, e.Element, button.Result));

        CloseModal(modal, button.Result);
    }

    /// <summary>
    /// Opens a modal dialog.
    /// </summary>
    /// <param name="modal">The modal entity to open.</param>
    public void OpenModal(Entity modal)
    {
        if (!World.IsAlive(modal) || !World.Has<UIModal>(modal))
        {
            return;
        }

        ref var modalComponent = ref World.Get<UIModal>(modal);
        if (modalComponent.IsOpen)
        {
            return; // Already open
        }

        // Capture backdrop before modifying the entity (removing/adding components can invalidate refs)
        var backdrop = modalComponent.Backdrop;

        modalComponent.IsOpen = true;

        // Show the modal
        if (World.Has<UIElement>(modal))
        {
            ref var element = ref World.Get<UIElement>(modal);
            element.Visible = true;
        }

        // Remove hidden tag
        if (World.Has<UIHiddenTag>(modal))
        {
            World.Remove<UIHiddenTag>(modal);
        }

        // Show the backdrop (using captured value since ref may be invalidated)
        if (backdrop.IsValid && World.IsAlive(backdrop))
        {
            if (World.Has<UIElement>(backdrop))
            {
                ref var backdropElement = ref World.Get<UIElement>(backdrop);
                backdropElement.Visible = true;
            }

            if (World.Has<UIHiddenTag>(backdrop))
            {
                World.Remove<UIHiddenTag>(backdrop);
            }
        }

        // Mark layout dirty
        if (!World.Has<UILayoutDirtyTag>(modal))
        {
            World.Add(modal, new UILayoutDirtyTag());
        }

        // Fire opened event
        World.Send(new UIModalOpenedEvent(modal));
    }

    /// <summary>
    /// Closes a modal dialog with the specified result.
    /// </summary>
    /// <param name="modal">The modal entity to close.</param>
    /// <param name="result">The result to report when closing.</param>
    public void CloseModal(Entity modal, ModalResult result)
    {
        if (!World.IsAlive(modal) || !World.Has<UIModal>(modal))
        {
            return;
        }

        ref var modalComponent = ref World.Get<UIModal>(modal);
        if (!modalComponent.IsOpen)
        {
            return; // Already closed
        }

        // Capture backdrop before modifying the entity (adding components can invalidate refs)
        var backdrop = modalComponent.Backdrop;

        modalComponent.IsOpen = false;

        // Hide the modal
        if (World.Has<UIElement>(modal))
        {
            ref var element = ref World.Get<UIElement>(modal);
            element.Visible = false;
        }

        // Add hidden tag
        if (!World.Has<UIHiddenTag>(modal))
        {
            World.Add(modal, new UIHiddenTag());
        }

        // Hide the backdrop (using captured value since ref may be invalidated)
        if (backdrop.IsValid && World.IsAlive(backdrop))
        {
            if (World.Has<UIElement>(backdrop))
            {
                ref var backdropElement = ref World.Get<UIElement>(backdrop);
                backdropElement.Visible = false;
            }

            if (!World.Has<UIHiddenTag>(backdrop))
            {
                World.Add(backdrop, new UIHiddenTag());
            }
        }

        // Fire closed event
        World.Send(new UIModalClosedEvent(modal, result));
    }
}
