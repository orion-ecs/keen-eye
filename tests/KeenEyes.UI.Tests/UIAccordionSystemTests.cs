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
        system.Initialize(world);

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
        system.Initialize(world);

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
        system.Initialize(world);

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
        system.Initialize(world);

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
        system.Initialize(world);

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
        system.Initialize(world);

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
        system.Initialize(world);

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
        system.Initialize(world);

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
        system.Initialize(world);

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
        system.Initialize(world);

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
