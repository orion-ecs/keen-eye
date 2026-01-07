using KeenEyes.Input.Abstractions;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI;

/// <summary>
/// System that handles keyboard navigation between focusable UI elements.
/// </summary>
/// <remarks>
/// <para>
/// The focus system processes Tab and Shift+Tab key presses to navigate between
/// <see cref="UIInteractable"/> elements with <see cref="UIInteractable.CanFocus"/> enabled.
/// </para>
/// <para>
/// Focus order is determined by <see cref="UIInteractable.TabIndex"/> - lower values
/// are focused first. Elements with the same TabIndex are ordered by their position
/// in the hierarchy.
/// </para>
/// <para>
/// This system should run in <see cref="SystemPhase.EarlyUpdate"/> phase after
/// <see cref="UIInputSystem"/>.
/// </para>
/// </remarks>
public sealed class UIFocusSystem : SystemBase
{
    private UIContext? uiContext;
    private IInputContext? inputContext;
    private bool tabWasDown;
    private bool escapeWasDown;

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        // Lazy initialization
        if (inputContext is null && !World.TryGetExtension(out inputContext))
        {
            return;
        }

        if (uiContext is null && !World.TryGetExtension(out uiContext))
        {
            return;
        }

        // At this point both contexts are guaranteed non-null
        var input = inputContext!;
        var ui = uiContext!;

        var keyboard = input.Keyboard;

        // Handle Tab navigation
        bool tabIsDown = keyboard.IsKeyDown(Key.Tab);
        if (tabIsDown && !tabWasDown)
        {
            bool isShiftDown = (keyboard.Modifiers & KeyModifiers.Shift) != 0;
            NavigateFocus(isShiftDown);
        }
        tabWasDown = tabIsDown;

        // Handle Escape to clear focus
        bool escapeIsDown = keyboard.IsKeyDown(Key.Escape);
        if (escapeIsDown && !escapeWasDown)
        {
            ui.ClearFocus();
        }
        escapeWasDown = escapeIsDown;

        // Handle Enter/Space on focused element
        if (ui.HasFocus)
        {
            var focused = ui.FocusedEntity;
            if (World.IsAlive(focused) && World.Has<UIInteractable>(focused))
            {
                bool enterPressed = keyboard.IsKeyDown(Key.Enter) || keyboard.IsKeyDown(Key.KeypadEnter);
                bool spacePressed = keyboard.IsKeyDown(Key.Space);

                if (enterPressed || spacePressed)
                {
                    ref var interactable = ref World.Get<UIInteractable>(focused);

                    if (enterPressed)
                    {
                        interactable.PendingEvents |= UIEventType.Submit;
                        World.Send(new UISubmitEvent(focused));
                    }

                    if (spacePressed && interactable.CanClick)
                    {
                        interactable.PendingEvents |= UIEventType.Click;
                        var rect = World.Get<UIRect>(focused);
                        World.Send(new UIClickEvent(focused, rect.ComputedBounds.Center, MouseButton.Left));
                    }
                }
            }
        }
    }

    private void NavigateFocus(bool reverse)
    {
        if (uiContext is null)
        {
            return;
        }

        // Get all focusable elements sorted by tab index
        var focusableElements = new List<(Entity Entity, int TabIndex, int Order)>();
        int order = 0;

        foreach (var entity in World.Query<UIInteractable, UIElement, UIRect>())
        {
            if (World.Has<UIHiddenTag>(entity) || World.Has<UIDisabledTag>(entity))
            {
                continue;
            }

            ref readonly var element = ref World.Get<UIElement>(entity);
            if (!element.Visible)
            {
                continue;
            }

            ref readonly var interactable = ref World.Get<UIInteractable>(entity);
            if (!interactable.CanFocus)
            {
                continue;
            }

            focusableElements.Add((entity, interactable.TabIndex, order++));
        }

        if (focusableElements.Count == 0)
        {
            return;
        }

        // Sort by tab index, then by order
        focusableElements.Sort((a, b) =>
        {
            int cmp = a.TabIndex.CompareTo(b.TabIndex);
            return cmp != 0 ? cmp : a.Order.CompareTo(b.Order);
        });

        // Find current focused element
        int currentIndex = -1;
        if (uiContext.HasFocus)
        {
            for (int i = 0; i < focusableElements.Count; i++)
            {
                if (focusableElements[i].Entity == uiContext.FocusedEntity)
                {
                    currentIndex = i;
                    break;
                }
            }
        }

        // Calculate next index
        int nextIndex;
        if (reverse)
        {
            nextIndex = currentIndex <= 0
                ? focusableElements.Count - 1
                : currentIndex - 1;
        }
        else
        {
            nextIndex = currentIndex >= focusableElements.Count - 1
                ? 0
                : currentIndex + 1;
        }

        // Focus the next element
        uiContext.RequestFocus(focusableElements[nextIndex].Entity);
    }
}
