using System.Numerics;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI;

/// <summary>
/// System that handles property grid interactions: category expand/collapse.
/// </summary>
/// <remarks>
/// <para>
/// This system processes interactions with property grid widgets including:
/// <list type="bullet">
/// <item>Expand/collapse categories via header clicks</item>
/// <item>Property value change events</item>
/// </list>
/// </para>
/// </remarks>
public sealed class UIPropertyGridSystem : SystemBase
{
    /// <inheritdoc/>
    public override void Update(float deltaTime)
    {
        // Handle category header clicks for expand/collapse
        ProcessCategoryHeaderClicks();
    }

    private void ProcessCategoryHeaderClicks()
    {
        // Find category headers that were clicked
        foreach (var headerEntity in World.Query<UIPropertyCategoryHeaderTag, UIInteractable>())
        {
            ref readonly var interactable = ref World.Get<UIInteractable>(headerEntity);

            if (!interactable.HasEvent(UIEventType.Click))
            {
                continue;
            }

            // Find the parent category
            var parent = World.GetParent(headerEntity);
            while (parent.IsValid && !World.Has<UIPropertyCategory>(parent))
            {
                parent = World.GetParent(parent);
            }

            if (!parent.IsValid || !World.Has<UIPropertyCategory>(parent))
            {
                continue;
            }

            ref var category = ref World.Get<UIPropertyCategory>(parent);

            // Toggle expansion
            category.IsExpanded = !category.IsExpanded;

            // Update visibility of content container
            if (category.ContentContainer.IsValid && World.Has<UIElement>(category.ContentContainer))
            {
                ref var contentElement = ref World.Get<UIElement>(category.ContentContainer);
                contentElement.Visible = category.IsExpanded;
            }

            // Update arrow visual
            UpdateCategoryArrowVisual(headerEntity, category.IsExpanded);

            // Fire expand/collapse event
            if (category.PropertyGrid.IsValid)
            {
                if (category.IsExpanded)
                {
                    World.Send(new UIPropertyCategoryExpandedEvent(category.PropertyGrid, parent));
                }
                else
                {
                    World.Send(new UIPropertyCategoryCollapsedEvent(category.PropertyGrid, parent));
                }
            }
        }
    }

    private void UpdateCategoryArrowVisual(Entity headerEntity, bool isExpanded)
    {
        // Find the arrow element within the header
        foreach (var child in World.GetChildren(headerEntity))
        {
            if (World.Has<UIPropertyCategoryArrowTag>(child) && World.Has<UIStyle>(child))
            {
                ref var style = ref World.Get<UIStyle>(child);
                // Visual feedback - change color to indicate state
                style.BackgroundColor = isExpanded
                    ? new Vector4(0.5f, 0.5f, 0.5f, 1f)
                    : new Vector4(0.7f, 0.7f, 0.7f, 1f);
                break;
            }
        }
    }
}
