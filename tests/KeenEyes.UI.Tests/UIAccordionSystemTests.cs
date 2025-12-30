using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI.Tests;

/// <summary>
/// Tests for the UIAccordionSystem section expand/collapse handling.
/// </summary>
public class UIAccordionSystemTests
{
    #region Expand/Collapse Tests

    [Fact]
    public void AccordionHeader_Click_ExpandsSection()
    {
        using var world = new World();
        var system = new UIAccordionSystem();
        world.AddSystem(system);

        var (accordion, sections, headers) = CreateAccordion(world, 3, allowMultiple: true);

        // Click header to expand
        SimulateClick(world, headers[0]);
        system.Update(0);

        ref readonly var section = ref world.Get<UIAccordionSection>(sections[0]);
        Assert.True(section.IsExpanded);
    }

    [Fact]
    public void AccordionHeader_ClickExpanded_CollapsesSection()
    {
        using var world = new World();
        var system = new UIAccordionSystem();
        world.AddSystem(system);

        var (accordion, sections, headers) = CreateAccordion(world, 3, allowMultiple: true);

        // Expand first
        SimulateClick(world, headers[0]);
        system.Update(0);

        // Click again to collapse
        SimulateClick(world, headers[0]);
        system.Update(0);

        ref readonly var section = ref world.Get<UIAccordionSection>(sections[0]);
        Assert.False(section.IsExpanded);
    }

    [Fact]
    public void AccordionHeader_Click_ShowsContent()
    {
        using var world = new World();
        var system = new UIAccordionSystem();
        world.AddSystem(system);

        var (accordion, sections, headers) = CreateAccordion(world, 3, allowMultiple: true);

        // Click header to expand
        SimulateClick(world, headers[0]);
        system.Update(0);

        var contentContainer = world.Get<UIAccordionSection>(sections[0]).ContentContainer;
        ref readonly var contentElement = ref world.Get<UIElement>(contentContainer);
        Assert.True(contentElement.Visible);
    }

    [Fact]
    public void AccordionHeader_ClickExpanded_HidesContent()
    {
        using var world = new World();
        var system = new UIAccordionSystem();
        world.AddSystem(system);

        var (accordion, sections, headers) = CreateAccordion(world, 3, allowMultiple: true);

        // Expand first
        SimulateClick(world, headers[0]);
        system.Update(0);

        // Collapse
        SimulateClick(world, headers[0]);
        system.Update(0);

        var contentContainer = world.Get<UIAccordionSection>(sections[0]).ContentContainer;
        ref readonly var contentElement = ref world.Get<UIElement>(contentContainer);
        Assert.False(contentElement.Visible);
    }

    [Fact]
    public void AccordionHeader_Click_UpdatesArrowVisual()
    {
        using var world = new World();
        var system = new UIAccordionSystem();
        world.AddSystem(system);

        var (accordion, sections, headers) = CreateAccordion(world, 3, allowMultiple: true);

        // Find arrow element
        Entity arrow = Entity.Null;
        foreach (var child in world.GetChildren(headers[0]))
        {
            if (world.Has<UIAccordionArrowTag>(child))
            {
                arrow = child;
                break;
            }
        }

        // Click to expand
        SimulateClick(world, headers[0]);
        system.Update(0);

        ref readonly var arrowText = ref world.Get<UIText>(arrow);
        Assert.Equal("▼", arrowText.Content);
    }

    [Fact]
    public void AccordionHeader_ClickCollapsed_UpdatesArrowToRight()
    {
        using var world = new World();
        var system = new UIAccordionSystem();
        world.AddSystem(system);

        var (accordion, sections, headers) = CreateAccordion(world, 3, allowMultiple: true);

        // Find arrow element
        Entity arrow = Entity.Null;
        foreach (var child in world.GetChildren(headers[0]))
        {
            if (world.Has<UIAccordionArrowTag>(child))
            {
                arrow = child;
                break;
            }
        }

        // Expand then collapse
        SimulateClick(world, headers[0]);
        system.Update(0);
        SimulateClick(world, headers[0]);
        system.Update(0);

        ref readonly var arrowText = ref world.Get<UIText>(arrow);
        Assert.Equal("▶", arrowText.Content);
    }

    #endregion

    #region Single-Expand Mode Tests

    [Fact]
    public void Accordion_SingleExpandMode_CollapsesOthersWhenExpanding()
    {
        using var world = new World();
        var system = new UIAccordionSystem();
        world.AddSystem(system);

        var (accordion, sections, headers) = CreateAccordion(world, 3, allowMultiple: false);

        // Expand first section
        SimulateClick(world, headers[0]);
        system.Update(0);

        // Expand second section (should collapse first)
        SimulateClick(world, headers[1]);
        system.Update(0);

        ref readonly var section0 = ref world.Get<UIAccordionSection>(sections[0]);
        ref readonly var section1 = ref world.Get<UIAccordionSection>(sections[1]);

        Assert.False(section0.IsExpanded);
        Assert.True(section1.IsExpanded);
    }

    [Fact]
    public void Accordion_MultiExpandMode_AllowsMultipleExpanded()
    {
        using var world = new World();
        var system = new UIAccordionSystem();
        world.AddSystem(system);

        var (accordion, sections, headers) = CreateAccordion(world, 3, allowMultiple: true);

        // Expand first section
        SimulateClick(world, headers[0]);
        system.Update(0);

        // Expand second section (both should be expanded)
        SimulateClick(world, headers[1]);
        system.Update(0);

        ref readonly var section0 = ref world.Get<UIAccordionSection>(sections[0]);
        ref readonly var section1 = ref world.Get<UIAccordionSection>(sections[1]);

        Assert.True(section0.IsExpanded);
        Assert.True(section1.IsExpanded);
    }

    #endregion

    #region Event Tests

    [Fact]
    public void AccordionHeader_ClickToExpand_FiresExpandedEvent()
    {
        using var world = new World();
        var system = new UIAccordionSystem();
        world.AddSystem(system);

        var (accordion, sections, headers) = CreateAccordion(world, 2, allowMultiple: true);

        UIAccordionSectionExpandedEvent? receivedEvent = null;
        world.Subscribe<UIAccordionSectionExpandedEvent>(e => receivedEvent = e);

        SimulateClick(world, headers[0]);
        system.Update(0);

        Assert.NotNull(receivedEvent);
        Assert.Equal(accordion, receivedEvent.Value.Accordion);
        Assert.Equal(sections[0], receivedEvent.Value.Section);
    }

    [Fact]
    public void AccordionHeader_ClickToCollapse_FiresCollapsedEvent()
    {
        using var world = new World();
        var system = new UIAccordionSystem();
        world.AddSystem(system);

        var (accordion, sections, headers) = CreateAccordion(world, 2, allowMultiple: true);

        // Expand first
        SimulateClick(world, headers[0]);
        system.Update(0);

        UIAccordionSectionCollapsedEvent? receivedEvent = null;
        world.Subscribe<UIAccordionSectionCollapsedEvent>(e => receivedEvent = e);

        // Collapse
        SimulateClick(world, headers[0]);
        system.Update(0);

        Assert.NotNull(receivedEvent);
        Assert.Equal(accordion, receivedEvent.Value.Accordion);
        Assert.Equal(sections[0], receivedEvent.Value.Section);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void AccordionHeader_NoParentSection_DoesNothing()
    {
        using var world = new World();
        var system = new UIAccordionSystem();
        world.AddSystem(system);

        // Header without parent section
        var header = world.Spawn()
            .With(UIElement.Default)
            .With(new UIAccordionHeaderTag())
            .With(UIInteractable.Clickable())
            .Build();

        SimulateClick(world, header);

        // Should not throw
        system.Update(0);
    }

    [Fact]
    public void AccordionHeader_InvalidAccordion_DoesNothing()
    {
        using var world = new World();
        var system = new UIAccordionSystem();
        world.AddSystem(system);

        // Create section with invalid accordion reference
        var section = world.Spawn()
            .With(UIElement.Default)
            .With(new UIAccordionSection(Entity.Null, "Section")
            {
                IsExpanded = false
            })
            .Build();

        var header = world.Spawn()
            .With(UIElement.Default)
            .With(new UIAccordionHeaderTag())
            .With(UIInteractable.Clickable())
            .Build();

        world.SetParent(header, section);

        SimulateClick(world, header);

        // Should not throw
        system.Update(0);
    }

    [Fact]
    public void AccordionHeader_NestedInNonSectionParent_FindsSectionAncestor()
    {
        using var world = new World();
        var system = new UIAccordionSystem();
        world.AddSystem(system);

        var accordion = world.Spawn()
            .With(UIElement.Default)
            .With(new UIAccordion(true))
            .Build();

        var contentContainer = world.Spawn()
            .With(new UIElement { Visible = false })
            .Build();

        var section = world.Spawn()
            .With(UIElement.Default)
            .With(new UIAccordionSection(accordion, "Section")
            {
                IsExpanded = false,
                ContentContainer = contentContainer
            })
            .Build();

        // Nested container that is not a section
        var nestedContainer = world.Spawn()
            .With(UIElement.Default)
            .Build();

        var header = world.Spawn()
            .With(UIElement.Default)
            .With(new UIAccordionHeaderTag())
            .With(UIInteractable.Clickable())
            .Build();

        // Header is nested in container, which is in section
        world.SetParent(header, nestedContainer);
        world.SetParent(nestedContainer, section);
        world.SetParent(section, accordion);

        SimulateClick(world, header);
        system.Update(0);

        ref readonly var sectionData = ref world.Get<UIAccordionSection>(section);
        Assert.True(sectionData.IsExpanded);
    }

    [Fact]
    public void AccordionHeader_NoArrowChild_DoesNotThrow()
    {
        using var world = new World();
        var system = new UIAccordionSystem();
        world.AddSystem(system);

        var accordion = world.Spawn()
            .With(UIElement.Default)
            .With(new UIAccordion(true))
            .Build();

        var contentContainer = world.Spawn()
            .With(new UIElement { Visible = false })
            .Build();

        // Header without arrow child
        var header = world.Spawn()
            .With(UIElement.Default)
            .With(new UIAccordionHeaderTag())
            .With(UIInteractable.Clickable())
            .Build();

        var section = world.Spawn()
            .With(UIElement.Default)
            .With(new UIAccordionSection(accordion, "Section")
            {
                IsExpanded = false,
                Header = header,
                ContentContainer = contentContainer
            })
            .Build();

        world.SetParent(header, section);
        world.SetParent(section, accordion);

        SimulateClick(world, header);

        // Should not throw even without arrow element
        system.Update(0);

        ref readonly var sectionData = ref world.Get<UIAccordionSection>(section);
        Assert.True(sectionData.IsExpanded);
    }

    [Fact]
    public void AccordionHeader_ArrowWithoutUIText_DoesNotThrow()
    {
        using var world = new World();
        var system = new UIAccordionSystem();
        world.AddSystem(system);

        var accordion = world.Spawn()
            .With(UIElement.Default)
            .With(new UIAccordion(true))
            .Build();

        var contentContainer = world.Spawn()
            .With(new UIElement { Visible = false })
            .Build();

        // Arrow without UIText
        var arrow = world.Spawn()
            .With(UIElement.Default)
            .With(new UIAccordionArrowTag())
            .Build();

        var header = world.Spawn()
            .With(UIElement.Default)
            .With(new UIAccordionHeaderTag())
            .With(UIInteractable.Clickable())
            .Build();

        world.SetParent(arrow, header);

        var section = world.Spawn()
            .With(UIElement.Default)
            .With(new UIAccordionSection(accordion, "Section")
            {
                IsExpanded = false,
                Header = header,
                ContentContainer = contentContainer
            })
            .Build();

        world.SetParent(header, section);
        world.SetParent(section, accordion);

        SimulateClick(world, header);

        // Should not throw even without UIText on arrow
        system.Update(0);
    }

    [Fact]
    public void Accordion_SingleExpandMode_CollapsesOthersFiresEvents()
    {
        using var world = new World();
        var system = new UIAccordionSystem();
        world.AddSystem(system);

        var (accordion, sections, headers) = CreateAccordion(world, 3, allowMultiple: false);

        // Expand first section
        SimulateClick(world, headers[0]);
        system.Update(0);

        var collapsedEvents = new List<UIAccordionSectionCollapsedEvent>();
        world.Subscribe<UIAccordionSectionCollapsedEvent>(e => collapsedEvents.Add(e));

        // Expand second section (should collapse first and fire event)
        SimulateClick(world, headers[1]);
        system.Update(0);

        Assert.Single(collapsedEvents);
        Assert.Equal(sections[0], collapsedEvents[0].Section);
    }

    [Fact]
    public void Accordion_SingleExpandMode_InvalidHeaderOnOtherSection_StillCollapses()
    {
        using var world = new World();
        var system = new UIAccordionSystem();
        world.AddSystem(system);

        var accordion = world.Spawn()
            .With(UIElement.Default)
            .With(new UIAccordion(false)) // Single expand mode
            .Build();

        // Section 1 with valid header
        var arrow1 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIText { Content = "▶" })
            .With(new UIAccordionArrowTag())
            .Build();

        var header1 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIAccordionHeaderTag())
            .With(UIInteractable.Clickable())
            .Build();

        world.SetParent(arrow1, header1);

        var content1 = world.Spawn()
            .With(new UIElement { Visible = false })
            .Build();

        var section1 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIAccordionSection(accordion, "Section 1")
            {
                Index = 0,
                IsExpanded = false,
                Header = header1,
                ContentContainer = content1
            })
            .Build();

        world.SetParent(header1, section1);
        world.SetParent(section1, accordion);

        // Section 2 with NO header (Entity.Null)
        var content2 = world.Spawn()
            .With(new UIElement { Visible = true })
            .Build();

        var section2 = world.Spawn()
            .With(UIElement.Default)
            .With(new UIAccordionSection(accordion, "Section 2")
            {
                Index = 1,
                IsExpanded = true, // Already expanded
                Header = Entity.Null, // No header
                ContentContainer = content2
            })
            .Build();

        world.SetParent(section2, accordion);

        // Expand section 1 - should collapse section 2 even without header
        SimulateClick(world, header1);
        system.Update(0);

        ref readonly var sec2 = ref world.Get<UIAccordionSection>(section2);
        Assert.False(sec2.IsExpanded);
    }

    [Fact]
    public void AccordionSection_ContentWithoutUIElement_DoesNotThrow()
    {
        using var world = new World();
        var system = new UIAccordionSystem();
        world.AddSystem(system);

        var accordion = world.Spawn()
            .With(UIElement.Default)
            .With(new UIAccordion(true))
            .Build();

        // Content without UIElement component
        var contentContainer = world.Spawn()
            .Build();

        var header = world.Spawn()
            .With(UIElement.Default)
            .With(new UIAccordionHeaderTag())
            .With(UIInteractable.Clickable())
            .Build();

        var section = world.Spawn()
            .With(UIElement.Default)
            .With(new UIAccordionSection(accordion, "Section")
            {
                IsExpanded = false,
                Header = header,
                ContentContainer = contentContainer
            })
            .Build();

        world.SetParent(header, section);
        world.SetParent(section, accordion);

        // Should not throw even without UIElement on content
        SimulateClick(world, header);
        system.Update(0);
    }

    [Fact]
    public void AccordionSection_InvalidContentContainer_DoesNotThrow()
    {
        using var world = new World();
        var system = new UIAccordionSystem();
        world.AddSystem(system);

        var accordion = world.Spawn()
            .With(UIElement.Default)
            .With(new UIAccordion(true))
            .Build();

        var header = world.Spawn()
            .With(UIElement.Default)
            .With(new UIAccordionHeaderTag())
            .With(UIInteractable.Clickable())
            .Build();

        var section = world.Spawn()
            .With(UIElement.Default)
            .With(new UIAccordionSection(accordion, "Section")
            {
                IsExpanded = false,
                Header = header,
                ContentContainer = Entity.Null // Invalid content
            })
            .Build();

        world.SetParent(header, section);
        world.SetParent(section, accordion);

        // Should not throw with invalid content container
        SimulateClick(world, header);
        system.Update(0);
    }

    [Fact]
    public void AccordionHeader_NoClickEvent_IsIgnored()
    {
        using var world = new World();
        var system = new UIAccordionSystem();
        world.AddSystem(system);

        var (accordion, sections, headers) = CreateAccordion(world, 1, allowMultiple: true);

        // Don't simulate click
        system.Update(0);

        ref readonly var section = ref world.Get<UIAccordionSection>(sections[0]);
        Assert.False(section.IsExpanded);
    }

    #endregion

    #region Helper Methods

    private static (Entity Accordion, Entity[] Sections, Entity[] Headers) CreateAccordion(
        World world, int sectionCount, bool allowMultiple)
    {
        // Create accordion container
        var accordion = world.Spawn()
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIAccordion(allowMultiple))
            .Build();

        var sections = new Entity[sectionCount];
        var headers = new Entity[sectionCount];

        for (int i = 0; i < sectionCount; i++)
        {
            // Create arrow
            var arrow = world.Spawn()
                .With(UIElement.Default)
                .With(new UIText { Content = "▶" })
                .With(new UIAccordionArrowTag())
                .Build();

            // Create header
            headers[i] = world.Spawn()
                .With(UIElement.Default)
                .With(UIRect.Fixed(0, i * 40, 300, 40))
                .With(new UIAccordionHeaderTag())
                .With(UIInteractable.Clickable())
                .Build();

            world.SetParent(arrow, headers[i]);

            // Create content container
            var contentContainer = world.Spawn()
                .With(new UIElement { Visible = false })
                .With(UIRect.Fixed(0, 0, 300, 100))
                .Build();

            // Create section
            sections[i] = world.Spawn()
                .With(UIElement.Default)
                .With(UIRect.Fixed(0, 0, 300, 140))
                .With(new UIAccordionSection(accordion, $"Section {i + 1}")
                {
                    Index = i,
                    IsExpanded = false,
                    Header = headers[i],
                    ContentContainer = contentContainer
                })
                .Build();

            world.SetParent(headers[i], sections[i]);
            world.SetParent(contentContainer, sections[i]);
            world.SetParent(sections[i], accordion);
        }

        return (accordion, sections, headers);
    }

    private static void SimulateClick(World world, Entity entity)
    {
        ref var interactable = ref world.Get<UIInteractable>(entity);
        interactable.PendingEvents |= UIEventFlags.Click;
    }

    #endregion
}
