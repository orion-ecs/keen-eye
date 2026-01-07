using System.Numerics;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI;

/// <summary>
/// System that manages radial (pie) menus for gamepad-friendly selection.
/// </summary>
/// <remarks>
/// <para>
/// This system handles:
/// <list type="bullet">
/// <item>Opening radial menus at specified positions</item>
/// <item>Calculating selected slice from input direction</item>
/// <item>Highlighting the appropriate slice based on input</item>
/// <item>Triggering selection when input is confirmed</item>
/// <item>Animation of menu open/close</item>
/// <item>Nested submenu support</item>
/// </list>
/// </para>
/// </remarks>
public sealed class UIRadialMenuSystem : SystemBase
{
    private EventSubscription? openRequestSubscription;
    private const float OpenAnimationDuration = 0.15f;
    private const float CloseAnimationDuration = 0.1f;

    /// <inheritdoc />
    protected override void OnInitialize()
    {
        openRequestSubscription = World.Subscribe<UIRadialMenuRequestEvent>(OnOpenRequest);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            openRequestSubscription?.Dispose();
            openRequestSubscription = null;
        }

        base.Dispose(disposing);
    }

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        // Update animations and input for open radial menus
        foreach (var entity in World.Query<UIRadialMenu, UIRadialMenuInputState>())
        {
            ref var menu = ref World.Get<UIRadialMenu>(entity);
            ref var inputState = ref World.Get<UIRadialMenuInputState>(entity);

            if (menu.IsOpen)
            {
                // Update open animation
                if (menu.OpenProgress < 1f)
                {
                    menu.OpenProgress = MathF.Min(1f, menu.OpenProgress + deltaTime / OpenAnimationDuration);
                }

                inputState.OpenTime += deltaTime;

                // Calculate selected slice from input direction
                UpdateSelectedSlice(entity, ref menu, ref inputState);
            }
            else if (menu.OpenProgress > 0f)
            {
                // Close animation
                menu.OpenProgress = MathF.Max(0f, menu.OpenProgress - deltaTime / CloseAnimationDuration);
            }
        }
    }

    private void OnOpenRequest(UIRadialMenuRequestEvent e)
    {
        if (!World.Has<UIRadialMenu>(e.Menu))
        {
            return;
        }

        // Close any other open radial menus
        foreach (var entity in World.Query<UIRadialMenu>())
        {
            if (entity != e.Menu)
            {
                ref var otherMenu = ref World.Get<UIRadialMenu>(entity);
                if (otherMenu.IsOpen)
                {
                    CloseRadialMenu(entity, ref otherMenu, wasCancelled: true);
                }
            }
        }

        OpenRadialMenu(e.Menu, e.Position);
    }

    /// <summary>
    /// Opens a radial menu at the specified position.
    /// </summary>
    public void OpenRadialMenu(Entity menuEntity, Vector2 position)
    {
        if (!World.Has<UIRadialMenu>(menuEntity))
        {
            return;
        }

        ref var menu = ref World.Get<UIRadialMenu>(menuEntity);

        menu.IsOpen = true;
        menu.CenterPosition = position;
        menu.OpenProgress = 0f;
        menu.SelectedIndex = -1;

        // Position the menu element
        if (World.Has<UIRect>(menuEntity))
        {
            ref var rect = ref World.Get<UIRect>(menuEntity);
            rect.Offset = new UIEdges(
                position.X - menu.OuterRadius,
                position.Y - menu.OuterRadius,
                0, 0);
        }

        // Make visible
        if (World.Has<UIElement>(menuEntity))
        {
            ref var element = ref World.Get<UIElement>(menuEntity);
            element.Visible = true;
        }

        if (World.Has<UIHiddenTag>(menuEntity))
        {
            World.Remove<UIHiddenTag>(menuEntity);
        }

        if (!World.Has<UIRadialMenuOpenTag>(menuEntity))
        {
            World.Add(menuEntity, new UIRadialMenuOpenTag());
        }

        // Initialize input state
        if (!World.Has<UIRadialMenuInputState>(menuEntity))
        {
            World.Add(menuEntity, new UIRadialMenuInputState());
        }
        else
        {
            ref var inputState = ref World.Get<UIRadialMenuInputState>(menuEntity);
            inputState.InputDirection = Vector2.Zero;
            inputState.InputMagnitude = 0f;
            inputState.IsTriggerHeld = true;
            inputState.OpenTime = 0f;
        }

        World.Send(new UIRadialMenuOpenedEvent(menuEntity, position));
    }

    /// <summary>
    /// Closes a radial menu.
    /// </summary>
    /// <param name="menuEntity">The menu entity.</param>
    /// <param name="wasCancelled">Whether the close was a cancellation (no selection).</param>
    public void CloseRadialMenu(Entity menuEntity, bool wasCancelled = false)
    {
        if (!World.Has<UIRadialMenu>(menuEntity))
        {
            return;
        }

        ref var menu = ref World.Get<UIRadialMenu>(menuEntity);
        CloseRadialMenu(menuEntity, ref menu, wasCancelled);
    }

    private void CloseRadialMenu(Entity menuEntity, ref UIRadialMenu menu, bool wasCancelled)
    {
        menu.IsOpen = false;
        menu.SelectedIndex = -1;

        // Hide
        if (World.Has<UIElement>(menuEntity))
        {
            ref var element = ref World.Get<UIElement>(menuEntity);
            element.Visible = false;
        }

        if (!World.Has<UIHiddenTag>(menuEntity))
        {
            World.Add(menuEntity, new UIHiddenTag());
        }

        if (World.Has<UIRadialMenuOpenTag>(menuEntity))
        {
            World.Remove<UIRadialMenuOpenTag>(menuEntity);
        }

        // Clear selected slice tags
        foreach (var sliceEntity in World.Query<UIRadialSlice>())
        {
            ref readonly var slice = ref World.Get<UIRadialSlice>(sliceEntity);
            if (slice.RadialMenu == menuEntity && World.Has<UIRadialSliceSelectedTag>(sliceEntity))
            {
                World.Remove<UIRadialSliceSelectedTag>(sliceEntity);
            }
        }

        World.Send(new UIRadialMenuClosedEvent(menuEntity, wasCancelled));
    }

    /// <summary>
    /// Updates the input state for a radial menu.
    /// Call this with thumbstick/mouse direction input.
    /// </summary>
    /// <param name="menuEntity">The radial menu entity.</param>
    /// <param name="direction">Normalized input direction.</param>
    /// <param name="magnitude">Input magnitude (0-1).</param>
    public void UpdateInput(Entity menuEntity, Vector2 direction, float magnitude)
    {
        if (!World.Has<UIRadialMenuInputState>(menuEntity))
        {
            return;
        }

        ref var inputState = ref World.Get<UIRadialMenuInputState>(menuEntity);
        inputState.InputDirection = direction;
        inputState.InputMagnitude = magnitude;
    }

    /// <summary>
    /// Confirms the current selection in a radial menu.
    /// Call this when the trigger is released or selection button is pressed.
    /// </summary>
    public void ConfirmSelection(Entity menuEntity)
    {
        if (!World.Has<UIRadialMenu>(menuEntity))
        {
            return;
        }

        ref var menu = ref World.Get<UIRadialMenu>(menuEntity);

        if (!menu.IsOpen || menu.SelectedIndex < 0)
        {
            CloseRadialMenu(menuEntity, ref menu, wasCancelled: true);
            return;
        }

        // Find the selected slice
        Entity selectedSlice = Entity.Null;
        foreach (var sliceEntity in World.Query<UIRadialSlice>())
        {
            ref readonly var slice = ref World.Get<UIRadialSlice>(sliceEntity);
            if (slice.RadialMenu == menuEntity && slice.Index == menu.SelectedIndex)
            {
                selectedSlice = sliceEntity;
                break;
            }
        }

        if (selectedSlice.IsValid)
        {
            ref readonly var slice = ref World.Get<UIRadialSlice>(selectedSlice);

            if (!slice.IsEnabled)
            {
                // Cannot select disabled slice
                return;
            }

            if (slice.HasSubmenu && slice.Submenu.IsValid)
            {
                // Open submenu at the edge of this slice
                var sliceAngle = (slice.StartAngle + slice.EndAngle) / 2;
                var submenuPos = menu.CenterPosition + new Vector2(
                    MathF.Cos(sliceAngle) * menu.OuterRadius,
                    MathF.Sin(sliceAngle) * menu.OuterRadius
                );

                CloseRadialMenu(menuEntity, ref menu, wasCancelled: false);
                World.Send(new UIRadialMenuRequestEvent(slice.Submenu, submenuPos));
            }
            else
            {
                // Fire selection event
                World.Send(new UIRadialSliceSelectedEvent(selectedSlice, menuEntity, slice.ItemId, slice.Index));
                CloseRadialMenu(menuEntity, ref menu, wasCancelled: false);
            }
        }
        else
        {
            CloseRadialMenu(menuEntity, ref menu, wasCancelled: true);
        }
    }

    /// <summary>
    /// Cancels the radial menu without making a selection.
    /// </summary>
    public void Cancel(Entity menuEntity)
    {
        CloseRadialMenu(menuEntity, wasCancelled: true);
    }

    private void UpdateSelectedSlice(Entity menuEntity, ref UIRadialMenu menu, ref UIRadialMenuInputState inputState)
    {
        var previousIndex = menu.SelectedIndex;

        // Check if input is within the active zone
        if (inputState.InputMagnitude < 0.3f)  // Dead zone
        {
            menu.SelectedIndex = -1;
        }
        else
        {
            // Calculate angle from input direction
            var angle = MathF.Atan2(inputState.InputDirection.Y, inputState.InputDirection.X);

            // Normalize angle relative to start angle
            var relativeAngle = angle - menu.StartAngle;
            while (relativeAngle < 0)
            {
                relativeAngle += MathF.Tau;
            }

            while (relativeAngle >= MathF.Tau)
            {
                relativeAngle -= MathF.Tau;
            }

            // Calculate which slice this corresponds to
            var sliceAngle = MathF.Tau / menu.SliceCount;
            menu.SelectedIndex = (int)(relativeAngle / sliceAngle) % menu.SliceCount;
        }

        // Update slice tags and fire event if selection changed
        if (menu.SelectedIndex != previousIndex)
        {
            foreach (var sliceEntity in World.Query<UIRadialSlice>())
            {
                ref readonly var slice = ref World.Get<UIRadialSlice>(sliceEntity);
                if (slice.RadialMenu != menuEntity)
                {
                    continue;
                }

                bool shouldBeSelected = slice.Index == menu.SelectedIndex;
                bool isSelected = World.Has<UIRadialSliceSelectedTag>(sliceEntity);

                if (shouldBeSelected && !isSelected)
                {
                    World.Add(sliceEntity, new UIRadialSliceSelectedTag());
                }
                else if (!shouldBeSelected && isSelected)
                {
                    World.Remove<UIRadialSliceSelectedTag>(sliceEntity);
                }
            }

            World.Send(new UIRadialSliceChangedEvent(menuEntity, previousIndex, menu.SelectedIndex));
        }
    }
}
