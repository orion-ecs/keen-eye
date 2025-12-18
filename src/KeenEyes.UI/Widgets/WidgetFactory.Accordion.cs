using System.Numerics;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI.Widgets;

/// <summary>
/// Factory methods for creating accordion widgets.
/// </summary>
public static partial class WidgetFactory
{
    /// <summary>
    /// Creates an accordion widget with expandable/collapsible sections.
    /// </summary>
    /// <param name="world">The world to create the widget in.</param>
    /// <param name="parent">The parent entity.</param>
    /// <param name="config">Optional configuration.</param>
    /// <returns>The accordion entity.</returns>
    public static Entity CreateAccordion(
        IWorld world,
        Entity parent,
        AccordionConfig? config = null)
    {
        config ??= AccordionConfig.Default;

        // Create the main container
        var accordionEntity = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = Vector2.Zero,
                Size = new Vector2(config.Width ?? 0, config.Height ?? 0),
                WidthMode = config.Width.HasValue ? UISizeMode.Fixed : UISizeMode.Fill,
                HeightMode = config.Height.HasValue ? UISizeMode.Fixed : UISizeMode.FitContent
            })
            .With(new UIStyle
            {
                BackgroundColor = config.GetBackgroundColor()
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Vertical,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Start,
                Spacing = config.Spacing
            })
            .With(new UIAccordion(config.AllowMultipleExpanded)
            {
                ContentContainer = Entity.Null,
                SectionCount = 0
            })
            .WithTag<UIClipChildrenTag>()
            .Build();

        world.SetParent(accordionEntity, parent);

        // Create content container
        var contentContainer = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.One,
                Pivot = Vector2.Zero,
                Size = Vector2.Zero,
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.FitContent
            })
            .With(new UIStyle())
            .With(new UILayout
            {
                Direction = LayoutDirection.Vertical,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Start,
                Spacing = config.Spacing
            })
            .Build();

        world.SetParent(contentContainer, accordionEntity);

        // Update accordion reference
        ref var accordion = ref world.Get<UIAccordion>(accordionEntity);
        accordion.ContentContainer = contentContainer;

        return accordionEntity;
    }

    /// <summary>
    /// Creates an accordion section within an accordion.
    /// </summary>
    /// <param name="world">The world to create the widget in.</param>
    /// <param name="accordion">The parent accordion entity.</param>
    /// <param name="title">The section title.</param>
    /// <param name="font">The font for the title text.</param>
    /// <param name="config">Optional configuration.</param>
    /// <param name="isExpanded">Initial expanded state.</param>
    /// <returns>A tuple containing the section entity and content container entity.</returns>
    public static (Entity Section, Entity ContentContainer) CreateAccordionSection(
        IWorld world,
        Entity accordion,
        string title,
        FontHandle font,
        AccordionConfig? config = null,
        bool isExpanded = false)
    {
        config ??= AccordionConfig.Default;

        ref var accordionData = ref world.Get<UIAccordion>(accordion);
        var sectionIndex = accordionData.SectionCount;
        accordionData.SectionCount++;

        // Create section container
        var sectionEntity = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = new Vector2(1, 0),
                Pivot = Vector2.Zero,
                Size = Vector2.Zero,
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.FitContent
            })
            .With(new UIStyle
            {
                BorderColor = config.GetBorderColor(),
                BorderWidth = 1
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Vertical,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Start,
                Spacing = 0
            })
            .With(new UIAccordionSection(accordion, title)
            {
                IsExpanded = isExpanded,
                Index = sectionIndex,
                Header = Entity.Null,
                ContentContainer = Entity.Null
            })
            .Build();

        world.SetParent(sectionEntity, accordionData.ContentContainer);

        // Create header
        var headerEntity = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = true })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = new Vector2(1, 0),
                Pivot = Vector2.Zero,
                Size = new Vector2(0, config.HeaderHeight),
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIStyle
            {
                BackgroundColor = config.GetHeaderColor()
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Horizontal,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Center,
                Spacing = 8
            })
            .With(UIInteractable.Clickable())
            .Build();

        world.SetParent(headerEntity, sectionEntity);
        world.Add(headerEntity, new UIAccordionHeaderTag());

        // Create expand arrow
        var arrowEntity = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.Zero,
                Pivot = new Vector2(0.5f, 0.5f),
                Size = new Vector2(16, 16),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIStyle
            {
                BackgroundColor = isExpanded
                    ? new Vector4(0.5f, 0.5f, 0.5f, 1f)
                    : config.GetArrowColor()
            })
            .Build();

        world.SetParent(arrowEntity, headerEntity);
        world.Add(arrowEntity, new UIAccordionArrowTag());

        // Create title label
        var titleEntity = world.Spawn()
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = Vector2.Zero,
                Pivot = Vector2.Zero,
                Size = Vector2.Zero,
                WidthMode = UISizeMode.FitContent,
                HeightMode = UISizeMode.FitContent
            })
            .With(new UIStyle())
            .With(new UIText
            {
                Content = title,
                Font = font,
                FontSize = config.FontSize,
                Color = config.GetHeaderTextColor(),
                HorizontalAlign = TextAlignH.Left,
                VerticalAlign = TextAlignV.Middle
            })
            .Build();

        world.SetParent(titleEntity, headerEntity);

        // Create content container
        var contentContainer = world.Spawn()
            .With(new UIElement { Visible = isExpanded, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = new Vector2(1, 0),
                Pivot = Vector2.Zero,
                Size = Vector2.Zero,
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.FitContent
            })
            .With(new UIStyle
            {
                BackgroundColor = config.GetContentColor()
            })
            .With(new UILayout
            {
                Direction = LayoutDirection.Vertical,
                MainAxisAlign = LayoutAlign.Start,
                CrossAxisAlign = LayoutAlign.Start,
                Spacing = 0
            })
            .Build();

        world.SetParent(contentContainer, sectionEntity);
        world.Add(contentContainer, new UIAccordionContentTag());

        // Update section reference
        ref var section = ref world.Get<UIAccordionSection>(sectionEntity);
        section.Header = headerEntity;
        section.ContentContainer = contentContainer;

        return (sectionEntity, contentContainer);
    }

    /// <summary>
    /// Creates an accordion with sections from definitions.
    /// </summary>
    /// <param name="world">The world to create the widget in.</param>
    /// <param name="parent">The parent entity.</param>
    /// <param name="font">The font for section titles.</param>
    /// <param name="sections">The section definitions.</param>
    /// <param name="config">Optional configuration.</param>
    /// <returns>A tuple containing the accordion entity and an array of section entities.</returns>
    public static (Entity Accordion, Entity[] Sections) CreateAccordionWithSections(
        IWorld world,
        Entity parent,
        FontHandle font,
        IEnumerable<AccordionSectionDef> sections,
        AccordionConfig? config = null)
    {
        var accordionEntity = CreateAccordion(world, parent, config);
        var sectionEntities = new List<Entity>();

        foreach (var sectionDef in sections)
        {
            var (sectionEntity, _) = CreateAccordionSection(
                world, accordionEntity, sectionDef.Title, font, config, sectionDef.IsExpanded);
            sectionEntities.Add(sectionEntity);
        }

        return (accordionEntity, sectionEntities.ToArray());
    }

    /// <summary>
    /// Expands or collapses an accordion section.
    /// </summary>
    /// <param name="world">The world containing the section.</param>
    /// <param name="sectionEntity">The section entity.</param>
    /// <param name="isExpanded">The new expanded state.</param>
    /// <param name="collapseOthers">Whether to collapse other sections in single-expand mode.</param>
    public static void SetAccordionSectionExpanded(
        IWorld world,
        Entity sectionEntity,
        bool isExpanded,
        bool collapseOthers = true)
    {
        ref var section = ref world.Get<UIAccordionSection>(sectionEntity);

        if (section.IsExpanded == isExpanded)
        {
            return;
        }

        section.IsExpanded = isExpanded;

        // Update content visibility
        if (section.ContentContainer.IsValid && world.Has<UIElement>(section.ContentContainer))
        {
            ref var contentElement = ref world.Get<UIElement>(section.ContentContainer);
            contentElement.Visible = isExpanded;
        }

        // Update arrow visual
        if (section.Header.IsValid)
        {
            foreach (var child in world.GetChildren(section.Header))
            {
                if (world.Has<UIAccordionArrowTag>(child) && world.Has<UIStyle>(child))
                {
                    ref var style = ref world.Get<UIStyle>(child);
                    style.BackgroundColor = isExpanded
                        ? new Vector4(0.5f, 0.5f, 0.5f, 1f)
                        : new Vector4(0.7f, 0.7f, 0.7f, 1f);
                    break;
                }
            }
        }

        // Handle single-expand mode
        if (isExpanded && collapseOthers && section.Accordion.IsValid && world.Has<UIAccordion>(section.Accordion))
        {
            ref var accordion = ref world.Get<UIAccordion>(section.Accordion);
            if (!accordion.AllowMultipleExpanded)
            {
                // Collapse other sections
                foreach (var otherSectionEntity in world.Query<UIAccordionSection>())
                {
                    if (otherSectionEntity == sectionEntity)
                    {
                        continue;
                    }

                    ref var otherSection = ref world.Get<UIAccordionSection>(otherSectionEntity);
                    if (otherSection.Accordion == section.Accordion && otherSection.IsExpanded)
                    {
                        SetAccordionSectionExpanded(world, otherSectionEntity, false, false);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Expands all sections in an accordion.
    /// </summary>
    /// <param name="world">The world containing the accordion.</param>
    /// <param name="accordionEntity">The accordion entity.</param>
    /// <remarks>
    /// This only works if the accordion has AllowMultipleExpanded set to true.
    /// </remarks>
    public static void ExpandAllAccordionSections(IWorld world, Entity accordionEntity)
    {
        if (!world.Has<UIAccordion>(accordionEntity))
        {
            return;
        }

        ref var accordion = ref world.Get<UIAccordion>(accordionEntity);
        if (!accordion.AllowMultipleExpanded)
        {
            return;
        }

        foreach (var sectionEntity in world.Query<UIAccordionSection>())
        {
            ref var section = ref world.Get<UIAccordionSection>(sectionEntity);
            if (section.Accordion == accordionEntity && !section.IsExpanded)
            {
                SetAccordionSectionExpanded(world, sectionEntity, true, false);
            }
        }
    }

    /// <summary>
    /// Collapses all sections in an accordion.
    /// </summary>
    /// <param name="world">The world containing the accordion.</param>
    /// <param name="accordionEntity">The accordion entity.</param>
    public static void CollapseAllAccordionSections(IWorld world, Entity accordionEntity)
    {
        foreach (var sectionEntity in world.Query<UIAccordionSection>())
        {
            ref var section = ref world.Get<UIAccordionSection>(sectionEntity);
            if (section.Accordion == accordionEntity && section.IsExpanded)
            {
                SetAccordionSectionExpanded(world, sectionEntity, false, false);
            }
        }
    }
}
