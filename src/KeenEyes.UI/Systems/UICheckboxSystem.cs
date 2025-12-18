using System.Numerics;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI;

/// <summary>
/// System that handles checkbox and toggle widget interactions.
/// </summary>
/// <remarks>
/// <para>
/// This system listens for click events on checkbox and toggle widgets
/// and updates their visual state accordingly.
/// </para>
/// </remarks>
public sealed class UICheckboxSystem : SystemBase
{
    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        // Process checkboxes
        foreach (var entity in World.Query<UICheckbox, UIInteractable>())
        {
            ref var checkbox = ref World.Get<UICheckbox>(entity);
            ref readonly var interactable = ref World.Get<UIInteractable>(entity);

            // Check if clicked
            if ((interactable.PendingEvents & UIEventFlags.Click) != 0)
            {
                // Toggle state
                checkbox.IsChecked = !checkbox.IsChecked;

                // Update visual
                UpdateCheckboxVisual(checkbox);
            }
        }

        // Process toggles
        foreach (var entity in World.Query<UIToggle, UIInteractable>())
        {
            ref var toggle = ref World.Get<UIToggle>(entity);
            ref readonly var interactable = ref World.Get<UIInteractable>(entity);

            // Check if clicked
            if ((interactable.PendingEvents & UIEventFlags.Click) != 0)
            {
                // Toggle state
                toggle.IsOn = !toggle.IsOn;

                // Update visual
                UpdateToggleVisual(toggle);
            }
        }
    }

    private void UpdateCheckboxVisual(in UICheckbox checkbox)
    {
        if (!checkbox.BoxEntity.IsValid || !World.IsAlive(checkbox.BoxEntity))
        {
            return;
        }

        if (!World.Has<UIStyle>(checkbox.BoxEntity))
        {
            return;
        }

        ref var style = ref World.Get<UIStyle>(checkbox.BoxEntity);

        // Update background color based on checked state
        style.BackgroundColor = checkbox.IsChecked
            ? new Vector4(0.2f, 0.6f, 1f, 1f)  // Checked color (blue)
            : new Vector4(0.2f, 0.2f, 0.2f, 1f); // Unchecked color (dark gray)
    }

    private void UpdateToggleVisual(in UIToggle toggle)
    {
        // Update track color
        if (toggle.TrackEntity.IsValid && World.IsAlive(toggle.TrackEntity) && World.Has<UIStyle>(toggle.TrackEntity))
        {
            ref var trackStyle = ref World.Get<UIStyle>(toggle.TrackEntity);
            trackStyle.BackgroundColor = toggle.IsOn
                ? new Vector4(0.2f, 0.8f, 0.4f, 1f)  // On color (green)
                : new Vector4(0.4f, 0.4f, 0.4f, 1f); // Off color (gray)
        }

        // Update thumb position
        if (toggle.ThumbEntity.IsValid && World.IsAlive(toggle.ThumbEntity) && World.Has<UIRect>(toggle.ThumbEntity))
        {
            ref var thumbRect = ref World.Get<UIRect>(toggle.ThumbEntity);

            // Get track dimensions to calculate thumb position
            if (toggle.TrackEntity.IsValid && World.Has<UIRect>(toggle.TrackEntity))
            {
                ref readonly var trackRect = ref World.Get<UIRect>(toggle.TrackEntity);
                var trackWidth = trackRect.Size.X;
                var thumbSize = thumbRect.Size.X;
                var thumbPadding = 2f;

                var thumbOffset = toggle.IsOn
                    ? trackWidth - thumbSize - thumbPadding
                    : thumbPadding;

                thumbRect.Offset = new UIEdges(thumbOffset, 0, 0, 0);
            }

            // Mark layout dirty
            if (World.Has<UILayoutDirtyTag>(toggle.ThumbEntity))
            {
                // Already dirty
            }
            else
            {
                World.Add(toggle.ThumbEntity, new UILayoutDirtyTag());
            }
        }
    }
}
