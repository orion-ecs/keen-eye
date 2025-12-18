using System.Numerics;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI;

/// <summary>
/// System that handles accordion section expand/collapse interactions.
/// </summary>
/// <remarks>
/// <para>
/// This system processes interactions with accordion widgets including:
/// <list type="bullet">
/// <item>Toggle expansion via header clicks</item>
/// <item>Single-expand mode (collapse others when one expands)</item>
/// </list>
/// </para>
/// </remarks>
public sealed class UIAccordionSystem : SystemBase
{
    /// <inheritdoc/>
    public override void Update(float deltaTime)
    {
        ProcessHeaderClicks();
    }

    private void ProcessHeaderClicks()
    {
        // Find accordion headers that were clicked
        foreach (var headerEntity in World.Query<UIAccordionHeaderTag, UIInteractable>())
        {
            ref readonly var interactable = ref World.Get<UIInteractable>(headerEntity);

            if (!interactable.HasEvent(UIEventFlags.Click))
            {
                continue;
            }

            // Find the parent section
            var parent = World.GetParent(headerEntity);
            while (parent.IsValid && !World.Has<UIAccordionSection>(parent))
            {
                parent = World.GetParent(parent);
            }

            if (!parent.IsValid || !World.Has<UIAccordionSection>(parent))
            {
                continue;
            }

            ref var section = ref World.Get<UIAccordionSection>(parent);
            var accordion = section.Accordion;

            if (!accordion.IsValid || !World.Has<UIAccordion>(accordion))
            {
                continue;
            }

            ref var accordionData = ref World.Get<UIAccordion>(accordion);

            // Toggle expansion
            section.IsExpanded = !section.IsExpanded;

            // If single-expand mode and we're expanding, collapse other sections
            if (!accordionData.AllowMultipleExpanded && section.IsExpanded)
            {
                CollapseOtherSections(accordion, parent);
            }

            // Update content visibility
            UpdateSectionVisibility(parent, section.IsExpanded);

            // Update arrow visual
            UpdateArrowVisual(headerEntity, section.IsExpanded);

            // Fire expand/collapse event
            if (section.IsExpanded)
            {
                World.Send(new UIAccordionSectionExpandedEvent(accordion, parent));
            }
            else
            {
                World.Send(new UIAccordionSectionCollapsedEvent(accordion, parent));
            }
        }
    }

    private void CollapseOtherSections(Entity accordion, Entity expandedSection)
    {
        foreach (var sectionEntity in World.Query<UIAccordionSection>())
        {
            if (sectionEntity == expandedSection)
            {
                continue;
            }

            ref var section = ref World.Get<UIAccordionSection>(sectionEntity);

            if (section.Accordion != accordion || !section.IsExpanded)
            {
                continue;
            }

            // Collapse this section
            section.IsExpanded = false;
            UpdateSectionVisibility(sectionEntity, false);

            // Update arrow visual
            if (section.Header.IsValid)
            {
                UpdateArrowVisual(section.Header, false);
            }

            // Fire collapse event
            World.Send(new UIAccordionSectionCollapsedEvent(accordion, sectionEntity));
        }
    }

    private void UpdateSectionVisibility(Entity sectionEntity, bool isExpanded)
    {
        ref var section = ref World.Get<UIAccordionSection>(sectionEntity);

        if (section.ContentContainer.IsValid && World.Has<UIElement>(section.ContentContainer))
        {
            ref var contentElement = ref World.Get<UIElement>(section.ContentContainer);
            contentElement.Visible = isExpanded;
        }
    }

    private void UpdateArrowVisual(Entity headerEntity, bool isExpanded)
    {
        // Find the arrow element within the header
        foreach (var child in World.GetChildren(headerEntity))
        {
            if (World.Has<UIAccordionArrowTag>(child) && World.Has<UIStyle>(child))
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
