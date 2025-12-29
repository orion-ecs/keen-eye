using System.Numerics;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.UI.Abstractions;
using KeenEyes.UI.Widgets;

namespace KeenEyes.UI.Tests;

/// <summary>
/// Tests for WidgetFactory accordion widget creation methods.
/// </summary>
public class WidgetFactoryAccordionTests
{
    private static readonly FontHandle testFont = new(1);

    #region CreateAccordion Tests

    [Fact]
    public void CreateAccordion_HasRequiredComponents()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var accordion = WidgetFactory.CreateAccordion(world, parent);

        Assert.True(world.Has<UIElement>(accordion));
        Assert.True(world.Has<UIRect>(accordion));
        Assert.True(world.Has<UIStyle>(accordion));
        Assert.True(world.Has<UILayout>(accordion));
        Assert.True(world.Has<UIAccordion>(accordion));
    }

    [Fact]
    public void CreateAccordion_HasVerticalLayout()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var accordion = WidgetFactory.CreateAccordion(world, parent);

        ref readonly var layout = ref world.Get<UILayout>(accordion);
        Assert.Equal(LayoutDirection.Vertical, layout.Direction);
    }

    [Fact]
    public void CreateAccordion_HasContentContainer()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var accordion = WidgetFactory.CreateAccordion(world, parent);

        ref readonly var accordionData = ref world.Get<UIAccordion>(accordion);
        Assert.NotEqual(Entity.Null, accordionData.ContentContainer);
        Assert.True(world.IsAlive(accordionData.ContentContainer));
    }

    [Fact]
    public void CreateAccordion_ContentContainerIsChild()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var accordion = WidgetFactory.CreateAccordion(world, parent);

        ref readonly var accordionData = ref world.Get<UIAccordion>(accordion);
        Assert.Equal(accordion, world.GetParent(accordionData.ContentContainer));
    }

    [Fact]
    public void CreateAccordion_AppliesConfig()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new AccordionConfig { Width = 400, Spacing = 10f };

        var accordion = WidgetFactory.CreateAccordion(world, parent, config);

        ref readonly var rect = ref world.Get<UIRect>(accordion);
        Assert.Equal(new Vector2(400, 0), rect.Size);
        Assert.Equal(UISizeMode.Fixed, rect.WidthMode);

        ref readonly var layout = ref world.Get<UILayout>(accordion);
        Assert.Equal(10f, layout.Spacing);
    }

    [Fact]
    public void CreateAccordion_AllowsMultipleExpanded_WhenConfigured()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new AccordionConfig { AllowMultipleExpanded = true };

        var accordion = WidgetFactory.CreateAccordion(world, parent, config);

        ref readonly var accordionData = ref world.Get<UIAccordion>(accordion);
        Assert.True(accordionData.AllowMultipleExpanded);
    }

    [Fact]
    public void CreateAccordion_DisallowsMultipleExpanded_ByDefault()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var accordion = WidgetFactory.CreateAccordion(world, parent);

        ref readonly var accordionData = ref world.Get<UIAccordion>(accordion);
        Assert.False(accordionData.AllowMultipleExpanded);
    }

    [Fact]
    public void CreateAccordion_InitializesWithZeroSections()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var accordion = WidgetFactory.CreateAccordion(world, parent);

        ref readonly var accordionData = ref world.Get<UIAccordion>(accordion);
        Assert.Equal(0, accordionData.SectionCount);
    }

    [Fact]
    public void CreateAccordion_HasClipChildrenTag()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var accordion = WidgetFactory.CreateAccordion(world, parent);

        Assert.True(world.Has<UIClipChildrenTag>(accordion));
    }

    [Fact]
    public void CreateAccordion_IsNotRaycastTarget()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var accordion = WidgetFactory.CreateAccordion(world, parent);

        ref readonly var element = ref world.Get<UIElement>(accordion);
        Assert.False(element.RaycastTarget);
    }

    [Fact]
    public void CreateAccordion_HeightMode_FitContent_ByDefault()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var accordion = WidgetFactory.CreateAccordion(world, parent);

        ref readonly var rect = ref world.Get<UIRect>(accordion);
        Assert.Equal(UISizeMode.FitContent, rect.HeightMode);
    }

    [Fact]
    public void CreateAccordion_WithFixedHeight_SetsHeightMode()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new AccordionConfig { Height = 600 };

        var accordion = WidgetFactory.CreateAccordion(world, parent, config);

        ref readonly var rect = ref world.Get<UIRect>(accordion);
        Assert.Equal(600, rect.Size.Y);
        Assert.Equal(UISizeMode.Fixed, rect.HeightMode);
    }

    #endregion

    #region CreateAccordionSection Tests

    [Fact]
    public void CreateAccordionSection_ReturnsSectionAndContent()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var accordion = WidgetFactory.CreateAccordion(world, parent);

        var (section, content) = WidgetFactory.CreateAccordionSection(world, accordion, "Section 1", testFont);

        Assert.True(world.IsAlive(section));
        Assert.True(world.IsAlive(content));
    }

    [Fact]
    public void CreateAccordionSection_SectionIsChildOfContentContainer()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var accordion = WidgetFactory.CreateAccordion(world, parent);

        var (section, _) = WidgetFactory.CreateAccordionSection(world, accordion, "Section 1", testFont);

        ref readonly var accordionData = ref world.Get<UIAccordion>(accordion);
        Assert.Equal(accordionData.ContentContainer, world.GetParent(section));
    }

    [Fact]
    public void CreateAccordionSection_IncrementsSectionCount()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var accordion = WidgetFactory.CreateAccordion(world, parent);

        WidgetFactory.CreateAccordionSection(world, accordion, "Section 1", testFont);

        ref readonly var accordionData = ref world.Get<UIAccordion>(accordion);
        Assert.Equal(1, accordionData.SectionCount);
    }

    [Fact]
    public void CreateAccordionSection_MultipleSections_IncrementsCount()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var accordion = WidgetFactory.CreateAccordion(world, parent);

        WidgetFactory.CreateAccordionSection(world, accordion, "Section 1", testFont);
        WidgetFactory.CreateAccordionSection(world, accordion, "Section 2", testFont);
        WidgetFactory.CreateAccordionSection(world, accordion, "Section 3", testFont);

        ref readonly var accordionData = ref world.Get<UIAccordion>(accordion);
        Assert.Equal(3, accordionData.SectionCount);
    }

    [Fact]
    public void CreateAccordionSection_SectionHasRequiredComponents()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var accordion = WidgetFactory.CreateAccordion(world, parent);

        var (section, _) = WidgetFactory.CreateAccordionSection(world, accordion, "Section 1", testFont);

        Assert.True(world.Has<UIElement>(section));
        Assert.True(world.Has<UIRect>(section));
        Assert.True(world.Has<UIStyle>(section));
        Assert.True(world.Has<UILayout>(section));
        Assert.True(world.Has<UIAccordionSection>(section));
    }

    [Fact]
    public void CreateAccordionSection_ContentHasRequiredComponents()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var accordion = WidgetFactory.CreateAccordion(world, parent);

        var (_, content) = WidgetFactory.CreateAccordionSection(world, accordion, "Section 1", testFont);

        Assert.True(world.Has<UIElement>(content));
        Assert.True(world.Has<UIRect>(content));
        Assert.True(world.Has<UILayout>(content));
    }

    [Fact]
    public void CreateAccordionSection_SectionHasHeader()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var accordion = WidgetFactory.CreateAccordion(world, parent);

        var (section, _) = WidgetFactory.CreateAccordionSection(world, accordion, "Test Title", testFont);

        var children = world.GetChildren(section).ToList();
        Assert.Contains(children, child => world.Has<UIAccordionHeaderTag>(child));

        var header = children.First(child => world.Has<UIAccordionHeaderTag>(child));
        Assert.True(world.Has<UIInteractable>(header));

        // Title is a child of the header
        var headerChildren = world.GetChildren(header).ToList();
        var titleEntity = headerChildren.FirstOrDefault(child => world.Has<UIText>(child) && !world.Has<UIAccordionArrowTag>(child));
        Assert.NotEqual(Entity.Null, titleEntity);
        ref readonly var text = ref world.Get<UIText>(titleEntity);
        Assert.Equal("Test Title", text.Content);
    }

    [Fact]
    public void CreateAccordionSection_InitiallyExpanded_WhenConfigured()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var accordion = WidgetFactory.CreateAccordion(world, parent);

        var (section, content) = WidgetFactory.CreateAccordionSection(world, accordion, "Section", testFont, null, true);

        ref readonly var sectionData = ref world.Get<UIAccordionSection>(section);
        Assert.True(sectionData.IsExpanded);

        ref readonly var contentElement = ref world.Get<UIElement>(content);
        Assert.True(contentElement.Visible);
    }

    [Fact]
    public void CreateAccordionSection_InitiallyCollapsed_ByDefault()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var accordion = WidgetFactory.CreateAccordion(world, parent);

        var (section, content) = WidgetFactory.CreateAccordionSection(world, accordion, "Section", testFont);

        ref readonly var sectionData = ref world.Get<UIAccordionSection>(section);
        Assert.False(sectionData.IsExpanded);

        ref readonly var contentElement = ref world.Get<UIElement>(content);
        Assert.False(contentElement.Visible);
    }

    [Fact]
    public void CreateAccordionSection_AppliesConfig()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var accordion = WidgetFactory.CreateAccordion(world, parent);
        var config = new AccordionConfig { Spacing = 15f };

        var (section, _) = WidgetFactory.CreateAccordionSection(world, accordion, "Section", testFont, config);

        ref readonly var layout = ref world.Get<UILayout>(section);
        Assert.Equal(LayoutDirection.Vertical, layout.Direction);
    }

    [Fact]
    public void CreateAccordionSection_ContentIsChildOfSection()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var accordion = WidgetFactory.CreateAccordion(world, parent);

        var (section, content) = WidgetFactory.CreateAccordionSection(world, accordion, "Section", testFont);

        Assert.Equal(section, world.GetParent(content));
    }

    [Fact]
    public void CreateAccordionSection_HeaderIsInteractable()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var accordion = WidgetFactory.CreateAccordion(world, parent);

        var (section, _) = WidgetFactory.CreateAccordionSection(world, accordion, "Section", testFont);

        var children = world.GetChildren(section).ToList();
        var header = children.First(child => world.Has<UIAccordionHeaderTag>(child));

        Assert.True(world.Has<UIInteractable>(header));
        ref readonly var interactable = ref world.Get<UIInteractable>(header);
        Assert.True(interactable.CanClick);
    }

    #endregion

    #region Helper Methods

    private static Entity CreateRootEntity(World world)
    {
        var root = world.Spawn("Root")
            .With(UIElement.Default)
            .With(UIRect.Stretch())
            .With(new UIRootTag())
            .Build();

        var layout = new UILayoutSystem();
        world.AddSystem(layout);
        layout.Initialize(world);
        layout.Update(0);

        return root;
    }

    #endregion
}
