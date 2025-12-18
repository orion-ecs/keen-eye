using System.Numerics;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI;

/// <summary>
/// System that handles slider widget interactions.
/// </summary>
/// <remarks>
/// <para>
/// This system handles both click-to-set and drag interactions on slider widgets,
/// updating the slider value and visual elements accordingly.
/// </para>
/// </remarks>
public sealed class UISliderSystem : SystemBase
{
    private EventSubscription? dragSubscription;
    private EventSubscription? clickSubscription;

    /// <inheritdoc />
    protected override void OnInitialize()
    {
        dragSubscription = World.Subscribe<UIDragEvent>(OnDrag);
        clickSubscription = World.Subscribe<UIClickEvent>(OnClick);
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        dragSubscription?.Dispose();
        clickSubscription?.Dispose();
        base.Dispose();
    }

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        // Event-driven via subscriptions
    }

    private void OnDrag(UIDragEvent e)
    {
        var entity = e.Element;

        if (!World.IsAlive(entity) || !World.Has<UISlider>(entity))
        {
            return;
        }

        ref var slider = ref World.Get<UISlider>(entity);
        ref readonly var rect = ref World.Get<UIRect>(entity);

        // Calculate normalized value from mouse X position relative to slider bounds
        var bounds = rect.ComputedBounds;
        if (bounds.Width > 0)
        {
            var normalizedX = (e.Position.X - bounds.X) / bounds.Width;
            normalizedX = Math.Clamp(normalizedX, 0f, 1f);

            // Convert to actual value
            slider.Value = slider.MinValue + normalizedX * (slider.MaxValue - slider.MinValue);

            UpdateSliderVisual(entity, ref slider, in rect);
        }
    }

    private void OnClick(UIClickEvent e)
    {
        var entity = e.Element;

        if (!World.IsAlive(entity) || !World.Has<UISlider>(entity))
        {
            return;
        }

        ref var slider = ref World.Get<UISlider>(entity);
        ref readonly var rect = ref World.Get<UIRect>(entity);

        // Calculate normalized value from click position
        var bounds = rect.ComputedBounds;
        if (bounds.Width > 0)
        {
            var normalizedX = (e.Position.X - bounds.X) / bounds.Width;
            normalizedX = Math.Clamp(normalizedX, 0f, 1f);

            // Convert to actual value
            slider.Value = slider.MinValue + normalizedX * (slider.MaxValue - slider.MinValue);

            UpdateSliderVisual(entity, ref slider, in rect);
        }
    }

    private void UpdateSliderVisual(Entity sliderEntity, ref UISlider slider, in UIRect sliderRect)
    {
        // Calculate normalized value
        var range = slider.MaxValue - slider.MinValue;
        var normalizedValue = range > 0 ? (slider.Value - slider.MinValue) / range : 0f;
        normalizedValue = Math.Clamp(normalizedValue, 0f, 1f);

        // Update fill
        if (slider.FillEntity.IsValid && World.IsAlive(slider.FillEntity) && World.Has<UIRect>(slider.FillEntity))
        {
            ref var fillRect = ref World.Get<UIRect>(slider.FillEntity);
            fillRect.AnchorMax = new Vector2(normalizedValue, 0.5f);

            MarkLayoutDirty(slider.FillEntity);
        }

        // Update thumb position
        if (slider.ThumbEntity.IsValid && World.IsAlive(slider.ThumbEntity) && World.Has<UIRect>(slider.ThumbEntity))
        {
            ref var thumbRect = ref World.Get<UIRect>(slider.ThumbEntity);
            thumbRect.AnchorMin = new Vector2(normalizedValue, 0.5f);
            thumbRect.AnchorMax = new Vector2(normalizedValue, 0.5f);

            MarkLayoutDirty(slider.ThumbEntity);
        }

        // Also update UIScrollable if present (for backwards compatibility)
        if (World.Has<UIScrollable>(sliderEntity))
        {
            ref var scrollable = ref World.Get<UIScrollable>(sliderEntity);
            scrollable.ScrollPosition = new Vector2(normalizedValue, 0);
        }
    }

    private void MarkLayoutDirty(Entity entity)
    {
        if (!World.Has<UILayoutDirtyTag>(entity))
        {
            World.Add(entity, new UILayoutDirtyTag());
        }
    }
}
