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

    #region CreateAccordionWithSections Tests

    [Fact]
    public void CreateAccordionWithSections_ReturnsAccordionAndSections()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var sections = new[]
        {
            new AccordionSectionDef("Section 1", false),
            new AccordionSectionDef("Section 2", true),
            new AccordionSectionDef("Section 3", false)
        };

        var (accordion, sectionEntities) = WidgetFactory.CreateAccordionWithSections(world, parent, testFont, sections);

        Assert.True(world.IsAlive(accordion));
        Assert.Equal(3, sectionEntities.Length);
        foreach (var section in sectionEntities)
        {
            Assert.True(world.IsAlive(section));
        }
    }

    [Fact]
    public void CreateAccordionWithSections_SetsExpandedStates()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var sections = new[]
        {
            new AccordionSectionDef("Section 1", false),
            new AccordionSectionDef("Section 2", true)
        };

        var (_, sectionEntities) = WidgetFactory.CreateAccordionWithSections(world, parent, testFont, sections);

        ref readonly var section1 = ref world.Get<UIAccordionSection>(sectionEntities[0]);
        Assert.False(section1.IsExpanded);

        ref readonly var section2 = ref world.Get<UIAccordionSection>(sectionEntities[1]);
        Assert.True(section2.IsExpanded);
    }

    [Fact]
    public void CreateAccordionWithSections_SetsSectionTitles()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var sections = new[]
        {
            new AccordionSectionDef("First", false),
            new AccordionSectionDef("Second", false)
        };

        var (_, sectionEntities) = WidgetFactory.CreateAccordionWithSections(world, parent, testFont, sections);

        ref readonly var section1 = ref world.Get<UIAccordionSection>(sectionEntities[0]);
        Assert.Equal("First", section1.Title);

        ref readonly var section2 = ref world.Get<UIAccordionSection>(sectionEntities[1]);
        Assert.Equal("Second", section2.Title);
    }

    [Fact]
    public void CreateAccordionWithSections_AppliesConfig()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var sections = new[] { new AccordionSectionDef("Section", false) };
        var config = new AccordionConfig { Width = 500 };

        var (accordion, _) = WidgetFactory.CreateAccordionWithSections(world, parent, testFont, sections, config);

        ref readonly var rect = ref world.Get<UIRect>(accordion);
        Assert.Equal(500, rect.Size.X);
    }

    #endregion

    #region SetAccordionSectionExpanded Tests

    [Fact]
    public void SetAccordionSectionExpanded_ExpandsCollapsedSection()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var accordion = WidgetFactory.CreateAccordion(world, parent);
        var (section, content) = WidgetFactory.CreateAccordionSection(world, accordion, "Section", testFont, null, false);

        WidgetFactory.SetAccordionSectionExpanded(world, section, true);

        ref readonly var sectionData = ref world.Get<UIAccordionSection>(section);
        Assert.True(sectionData.IsExpanded);

        ref readonly var contentElement = ref world.Get<UIElement>(content);
        Assert.True(contentElement.Visible);
    }

    [Fact]
    public void SetAccordionSectionExpanded_CollapsesExpandedSection()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var accordion = WidgetFactory.CreateAccordion(world, parent);
        var (section, content) = WidgetFactory.CreateAccordionSection(world, accordion, "Section", testFont, null, true);

        WidgetFactory.SetAccordionSectionExpanded(world, section, false);

        ref readonly var sectionData = ref world.Get<UIAccordionSection>(section);
        Assert.False(sectionData.IsExpanded);

        ref readonly var contentElement = ref world.Get<UIElement>(content);
        Assert.False(contentElement.Visible);
    }

    [Fact]
    public void SetAccordionSectionExpanded_DoesNothingIfAlreadyInState()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var accordion = WidgetFactory.CreateAccordion(world, parent);
        var (section, _) = WidgetFactory.CreateAccordionSection(world, accordion, "Section", testFont, null, true);

        // Should return early without issues
        WidgetFactory.SetAccordionSectionExpanded(world, section, true);

        ref readonly var sectionData = ref world.Get<UIAccordionSection>(section);
        Assert.True(sectionData.IsExpanded);
    }

    [Fact]
    public void SetAccordionSectionExpanded_UpdatesArrowSymbol()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var accordion = WidgetFactory.CreateAccordion(world, parent);
        var (section, _) = WidgetFactory.CreateAccordionSection(world, accordion, "Section", testFont, null, false);

        // Find arrow before expansion
        ref readonly var sectionData = ref world.Get<UIAccordionSection>(section);
        Entity? arrowEntity = null;
        foreach (var child in world.GetChildren(sectionData.Header))
        {
            if (world.Has<UIAccordionArrowTag>(child) && world.Has<UIText>(child))
            {
                arrowEntity = child;
                break;
            }
        }
        Assert.NotNull(arrowEntity);

        ref readonly var arrowTextBefore = ref world.Get<UIText>(arrowEntity.Value);
        Assert.Equal("▶", arrowTextBefore.Content); // Collapsed arrow

        WidgetFactory.SetAccordionSectionExpanded(world, section, true);

        ref readonly var arrowTextAfter = ref world.Get<UIText>(arrowEntity.Value);
        Assert.Equal("▼", arrowTextAfter.Content); // Expanded arrow
    }

    [Fact]
    public void SetAccordionSectionExpanded_SingleMode_CollapsesOtherSections()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new AccordionConfig { AllowMultipleExpanded = false };
        var accordion = WidgetFactory.CreateAccordion(world, parent, config);
        var (section1, _) = WidgetFactory.CreateAccordionSection(world, accordion, "Section 1", testFont, config, true);
        var (section2, _) = WidgetFactory.CreateAccordionSection(world, accordion, "Section 2", testFont, config, false);

        // Expand section2 - should collapse section1
        WidgetFactory.SetAccordionSectionExpanded(world, section2, true);

        ref readonly var section1Data = ref world.Get<UIAccordionSection>(section1);
        ref readonly var section2Data = ref world.Get<UIAccordionSection>(section2);

        Assert.False(section1Data.IsExpanded);
        Assert.True(section2Data.IsExpanded);
    }

    [Fact]
    public void SetAccordionSectionExpanded_MultipleMode_DoesNotCollapseOtherSections()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new AccordionConfig { AllowMultipleExpanded = true };
        var accordion = WidgetFactory.CreateAccordion(world, parent, config);
        var (section1, _) = WidgetFactory.CreateAccordionSection(world, accordion, "Section 1", testFont, config, true);
        var (section2, _) = WidgetFactory.CreateAccordionSection(world, accordion, "Section 2", testFont, config, false);

        // Expand section2 - should NOT collapse section1
        WidgetFactory.SetAccordionSectionExpanded(world, section2, true);

        ref readonly var section1Data = ref world.Get<UIAccordionSection>(section1);
        ref readonly var section2Data = ref world.Get<UIAccordionSection>(section2);

        Assert.True(section1Data.IsExpanded);
        Assert.True(section2Data.IsExpanded);
    }

    #endregion

    #region ExpandAllAccordionSections Tests

    [Fact]
    public void ExpandAllAccordionSections_ExpandsAllSections()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new AccordionConfig { AllowMultipleExpanded = true };
        var accordion = WidgetFactory.CreateAccordion(world, parent, config);
        var (section1, _) = WidgetFactory.CreateAccordionSection(world, accordion, "Section 1", testFont, config, false);
        var (section2, _) = WidgetFactory.CreateAccordionSection(world, accordion, "Section 2", testFont, config, false);
        var (section3, _) = WidgetFactory.CreateAccordionSection(world, accordion, "Section 3", testFont, config, false);

        WidgetFactory.ExpandAllAccordionSections(world, accordion);

        ref readonly var s1 = ref world.Get<UIAccordionSection>(section1);
        ref readonly var s2 = ref world.Get<UIAccordionSection>(section2);
        ref readonly var s3 = ref world.Get<UIAccordionSection>(section3);

        Assert.True(s1.IsExpanded);
        Assert.True(s2.IsExpanded);
        Assert.True(s3.IsExpanded);
    }

    [Fact]
    public void ExpandAllAccordionSections_DoesNothingInSingleMode()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new AccordionConfig { AllowMultipleExpanded = false };
        var accordion = WidgetFactory.CreateAccordion(world, parent, config);
        var (section1, _) = WidgetFactory.CreateAccordionSection(world, accordion, "Section 1", testFont, config, false);
        var (section2, _) = WidgetFactory.CreateAccordionSection(world, accordion, "Section 2", testFont, config, false);

        WidgetFactory.ExpandAllAccordionSections(world, accordion);

        ref readonly var s1 = ref world.Get<UIAccordionSection>(section1);
        ref readonly var s2 = ref world.Get<UIAccordionSection>(section2);

        // Should remain collapsed due to single-mode restriction
        Assert.False(s1.IsExpanded);
        Assert.False(s2.IsExpanded);
    }

    [Fact]
    public void ExpandAllAccordionSections_InvalidEntity_DoesNotThrow()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        // Should not throw
        var exception = Record.Exception(() => WidgetFactory.ExpandAllAccordionSections(world, parent));
        Assert.Null(exception);
    }

    [Fact]
    public void ExpandAllAccordionSections_SkipsAlreadyExpandedSections()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new AccordionConfig { AllowMultipleExpanded = true };
        var accordion = WidgetFactory.CreateAccordion(world, parent, config);
        var (section1, _) = WidgetFactory.CreateAccordionSection(world, accordion, "Section 1", testFont, config, true);
        var (section2, _) = WidgetFactory.CreateAccordionSection(world, accordion, "Section 2", testFont, config, false);

        WidgetFactory.ExpandAllAccordionSections(world, accordion);

        ref readonly var s1 = ref world.Get<UIAccordionSection>(section1);
        ref readonly var s2 = ref world.Get<UIAccordionSection>(section2);

        Assert.True(s1.IsExpanded);
        Assert.True(s2.IsExpanded);
    }

    #endregion

    #region CollapseAllAccordionSections Tests

    [Fact]
    public void CollapseAllAccordionSections_CollapsesAllSections()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new AccordionConfig { AllowMultipleExpanded = true };
        var accordion = WidgetFactory.CreateAccordion(world, parent, config);
        var (section1, _) = WidgetFactory.CreateAccordionSection(world, accordion, "Section 1", testFont, config, true);
        var (section2, _) = WidgetFactory.CreateAccordionSection(world, accordion, "Section 2", testFont, config, true);
        var (section3, _) = WidgetFactory.CreateAccordionSection(world, accordion, "Section 3", testFont, config, true);

        WidgetFactory.CollapseAllAccordionSections(world, accordion);

        ref readonly var s1 = ref world.Get<UIAccordionSection>(section1);
        ref readonly var s2 = ref world.Get<UIAccordionSection>(section2);
        ref readonly var s3 = ref world.Get<UIAccordionSection>(section3);

        Assert.False(s1.IsExpanded);
        Assert.False(s2.IsExpanded);
        Assert.False(s3.IsExpanded);
    }

    [Fact]
    public void CollapseAllAccordionSections_SkipsAlreadyCollapsedSections()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var accordion = WidgetFactory.CreateAccordion(world, parent);
        var (section1, _) = WidgetFactory.CreateAccordionSection(world, accordion, "Section 1", testFont, null, false);
        var (section2, _) = WidgetFactory.CreateAccordionSection(world, accordion, "Section 2", testFont, null, true);

        WidgetFactory.CollapseAllAccordionSections(world, accordion);

        ref readonly var s1 = ref world.Get<UIAccordionSection>(section1);
        ref readonly var s2 = ref world.Get<UIAccordionSection>(section2);

        Assert.False(s1.IsExpanded);
        Assert.False(s2.IsExpanded);
    }

    [Fact]
    public void CollapseAllAccordionSections_DoesNotAffectOtherAccordions()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new AccordionConfig { AllowMultipleExpanded = true };
        var accordion1 = WidgetFactory.CreateAccordion(world, parent, config);
        var accordion2 = WidgetFactory.CreateAccordion(world, parent, config);
        var (section1, _) = WidgetFactory.CreateAccordionSection(world, accordion1, "Accordion1 Section", testFont, config, true);
        var (section2, _) = WidgetFactory.CreateAccordionSection(world, accordion2, "Accordion2 Section", testFont, config, true);

        // Collapse only accordion1
        WidgetFactory.CollapseAllAccordionSections(world, accordion1);

        ref readonly var s1 = ref world.Get<UIAccordionSection>(section1);
        ref readonly var s2 = ref world.Get<UIAccordionSection>(section2);

        Assert.False(s1.IsExpanded);
        Assert.True(s2.IsExpanded); // Should remain expanded
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
