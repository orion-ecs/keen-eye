using System.Numerics;

using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI.Tests;

/// <summary>
/// Tests for the UIPropertyGridSystem category interactions.
/// </summary>
public class UIPropertyGridSystemTests
{
    #region Category Expand Tests

    [Fact]
    public void CategoryHeader_Click_ExpandsCategory()
    {
        using var world = new World();
        var propertyGridSystem = new UIPropertyGridSystem();
        world.AddSystem(propertyGridSystem);

        var propertyGrid = world.Spawn()
            .With(UIElement.Default)
            .Build();

        var contentContainer = world.Spawn()
            .With(new UIElement { Visible = false })
            .Build();

        var category = world.Spawn()
            .With(UIElement.Default)
            .With(new UIPropertyCategory
            {
                IsExpanded = false,
                ContentContainer = contentContainer,
                PropertyGrid = propertyGrid
            })
            .Build();

        var header = world.Spawn()
            .With(UIElement.Default)
            .With(UIInteractable.Clickable())
            .With(new UIPropertyCategoryHeaderTag())
            .Build();
        world.SetParent(header, category);

        // Set click flag on header to trigger expand
        ref var interactable = ref world.Get<UIInteractable>(header);
        interactable.PendingEvents |= UIEventType.Click;

        propertyGridSystem.Update(0);

        ref readonly var updatedCategory = ref world.Get<UIPropertyCategory>(category);
        ref readonly var contentElement = ref world.Get<UIElement>(contentContainer);

        Assert.True(updatedCategory.IsExpanded);
        Assert.True(contentElement.Visible);
    }

    [Fact]
    public void CategoryHeader_ClickWhenExpanded_CollapsesCategory()
    {
        using var world = new World();
        var propertyGridSystem = new UIPropertyGridSystem();
        world.AddSystem(propertyGridSystem);

        var propertyGrid = world.Spawn()
            .With(UIElement.Default)
            .Build();

        var contentContainer = world.Spawn()
            .With(new UIElement { Visible = true })
            .Build();

        var category = world.Spawn()
            .With(UIElement.Default)
            .With(new UIPropertyCategory
            {
                IsExpanded = true,
                ContentContainer = contentContainer,
                PropertyGrid = propertyGrid
            })
            .Build();

        var header = world.Spawn()
            .With(UIElement.Default)
            .With(UIInteractable.Clickable())
            .With(new UIPropertyCategoryHeaderTag())
            .Build();
        world.SetParent(header, category);

        // Set click flag on header to trigger collapse
        ref var interactable = ref world.Get<UIInteractable>(header);
        interactable.PendingEvents |= UIEventType.Click;

        propertyGridSystem.Update(0);

        ref readonly var updatedCategory = ref world.Get<UIPropertyCategory>(category);
        ref readonly var contentElement = ref world.Get<UIElement>(contentContainer);

        Assert.False(updatedCategory.IsExpanded);
        Assert.False(contentElement.Visible);
    }

    #endregion

    #region Event Tests

    [Fact]
    public void CategoryExpand_FiresExpandedEvent()
    {
        using var world = new World();
        var propertyGridSystem = new UIPropertyGridSystem();
        world.AddSystem(propertyGridSystem);

        var propertyGrid = world.Spawn()
            .With(UIElement.Default)
            .Build();

        var contentContainer = world.Spawn()
            .With(new UIElement { Visible = false })
            .Build();

        var category = world.Spawn()
            .With(UIElement.Default)
            .With(new UIPropertyCategory
            {
                IsExpanded = false,
                ContentContainer = contentContainer,
                PropertyGrid = propertyGrid
            })
            .Build();

        var header = world.Spawn()
            .With(UIElement.Default)
            .With(UIInteractable.Clickable())
            .With(new UIPropertyCategoryHeaderTag())
            .Build();
        world.SetParent(header, category);

        bool eventFired = false;
        world.Subscribe<UIPropertyCategoryExpandedEvent>(e =>
        {
            if (e.Category == category)
            {
                eventFired = true;
            }
        });

        // Set click flag on header to trigger expand
        ref var interactable = ref world.Get<UIInteractable>(header);
        interactable.PendingEvents |= UIEventType.Click;

        propertyGridSystem.Update(0);

        Assert.True(eventFired);
    }

    [Fact]
    public void CategoryCollapse_FiresCollapsedEvent()
    {
        using var world = new World();
        var propertyGridSystem = new UIPropertyGridSystem();
        world.AddSystem(propertyGridSystem);

        var propertyGrid = world.Spawn()
            .With(UIElement.Default)
            .Build();

        var contentContainer = world.Spawn()
            .With(new UIElement { Visible = true })
            .Build();

        var category = world.Spawn()
            .With(UIElement.Default)
            .With(new UIPropertyCategory
            {
                IsExpanded = true,
                ContentContainer = contentContainer,
                PropertyGrid = propertyGrid
            })
            .Build();

        var header = world.Spawn()
            .With(UIElement.Default)
            .With(UIInteractable.Clickable())
            .With(new UIPropertyCategoryHeaderTag())
            .Build();
        world.SetParent(header, category);

        bool eventFired = false;
        world.Subscribe<UIPropertyCategoryCollapsedEvent>(e =>
        {
            if (e.Category == category)
            {
                eventFired = true;
            }
        });

        // Set click flag on header to trigger collapse
        ref var interactable = ref world.Get<UIInteractable>(header);
        interactable.PendingEvents |= UIEventType.Click;

        propertyGridSystem.Update(0);

        Assert.True(eventFired);
    }

    #endregion

    #region Visual Update Tests

    [Fact]
    public void CategoryExpand_UpdatesArrowVisual()
    {
        using var world = new World();
        var propertyGridSystem = new UIPropertyGridSystem();
        world.AddSystem(propertyGridSystem);

        var propertyGrid = world.Spawn()
            .With(UIElement.Default)
            .Build();

        var contentContainer = world.Spawn()
            .With(new UIElement { Visible = false })
            .Build();

        var category = world.Spawn()
            .With(UIElement.Default)
            .With(new UIPropertyCategory
            {
                IsExpanded = false,
                ContentContainer = contentContainer,
                PropertyGrid = propertyGrid
            })
            .Build();

        var header = world.Spawn()
            .With(UIElement.Default)
            .With(UIInteractable.Clickable())
            .With(new UIPropertyCategoryHeaderTag())
            .Build();
        world.SetParent(header, category);

        var arrow = world.Spawn()
            .With(UIElement.Default)
            .With(new UIStyle { BackgroundColor = new Vector4(0.7f, 0.7f, 0.7f, 1f) })
            .With(new UIPropertyCategoryArrowTag())
            .Build();
        world.SetParent(arrow, header);

        // Set click flag on header to trigger expand
        ref var interactable = ref world.Get<UIInteractable>(header);
        interactable.PendingEvents |= UIEventType.Click;

        propertyGridSystem.Update(0);

        ref readonly var arrowStyle = ref world.Get<UIStyle>(arrow);

        Assert.True(arrowStyle.BackgroundColor.X < 0.6f);
    }

    #endregion

    #region No-Op Tests

    [Fact]
    public void CategoryHeader_NoClickEvent_DoesNothing()
    {
        using var world = new World();
        var propertyGridSystem = new UIPropertyGridSystem();
        world.AddSystem(propertyGridSystem);

        var propertyGrid = world.Spawn()
            .With(UIElement.Default)
            .Build();

        var contentContainer = world.Spawn()
            .With(new UIElement { Visible = false })
            .Build();

        var category = world.Spawn()
            .With(UIElement.Default)
            .With(new UIPropertyCategory
            {
                IsExpanded = false,
                ContentContainer = contentContainer,
                PropertyGrid = propertyGrid
            })
            .Build();

        var header = world.Spawn()
            .With(UIElement.Default)
            .With(new UIInteractable())
            .With(new UIPropertyCategoryHeaderTag())
            .Build();
        world.SetParent(header, category);

        propertyGridSystem.Update(0);

        ref readonly var updatedCategory = ref world.Get<UIPropertyCategory>(category);

        Assert.False(updatedCategory.IsExpanded);
    }

    [Fact]
    public void NonCategoryHeader_WithClick_DoesNothing()
    {
        using var world = new World();
        var propertyGridSystem = new UIPropertyGridSystem();
        world.AddSystem(propertyGridSystem);

        var nonCategoryElement = world.Spawn()
            .With(UIElement.Default)
            .With(UIInteractable.Clickable())
            .Build();

        propertyGridSystem.Update(0);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void CategoryHeader_WithNoParent_DoesNothing()
    {
        // Test header click when header has no parent (orphaned)
        using var world = new World();
        var propertyGridSystem = new UIPropertyGridSystem();
        world.AddSystem(propertyGridSystem);

        // Header with click but no parent at all
        var header = world.Spawn()
            .With(UIElement.Default)
            .With(UIInteractable.Clickable())
            .With(new UIPropertyCategoryHeaderTag())
            .Build();

        ref var interactable = ref world.Get<UIInteractable>(header);
        interactable.PendingEvents |= UIEventType.Click;

        // Should not throw or crash
        propertyGridSystem.Update(0);
    }

    [Fact]
    public void CategoryHeader_WithNonCategoryParent_DoesNothing()
    {
        // Test header click when parent is not a category
        using var world = new World();
        var propertyGridSystem = new UIPropertyGridSystem();
        world.AddSystem(propertyGridSystem);

        // Create a non-category parent
        var parent = world.Spawn()
            .With(UIElement.Default)
            .Build();

        var header = world.Spawn()
            .With(UIElement.Default)
            .With(UIInteractable.Clickable())
            .With(new UIPropertyCategoryHeaderTag())
            .Build();
        world.SetParent(header, parent);

        ref var interactable = ref world.Get<UIInteractable>(header);
        interactable.PendingEvents |= UIEventType.Click;

        // Should not throw or crash - parent traversal finds no category
        propertyGridSystem.Update(0);
    }

    [Fact]
    public void CategoryHeader_WithNestedNonCategoryParent_FindsCategoryAncestor()
    {
        // Test header click when header is nested under non-category element under category
        using var world = new World();
        var propertyGridSystem = new UIPropertyGridSystem();
        world.AddSystem(propertyGridSystem);

        var propertyGrid = world.Spawn()
            .With(UIElement.Default)
            .Build();

        var contentContainer = world.Spawn()
            .With(new UIElement { Visible = false })
            .Build();

        var category = world.Spawn()
            .With(UIElement.Default)
            .With(new UIPropertyCategory
            {
                IsExpanded = false,
                ContentContainer = contentContainer,
                PropertyGrid = propertyGrid
            })
            .Build();

        // Intermediate non-category parent
        var intermediate = world.Spawn()
            .With(UIElement.Default)
            .Build();
        world.SetParent(intermediate, category);

        var header = world.Spawn()
            .With(UIElement.Default)
            .With(UIInteractable.Clickable())
            .With(new UIPropertyCategoryHeaderTag())
            .Build();
        world.SetParent(header, intermediate);

        ref var interactable = ref world.Get<UIInteractable>(header);
        interactable.PendingEvents |= UIEventType.Click;

        propertyGridSystem.Update(0);

        ref readonly var updatedCategory = ref world.Get<UIPropertyCategory>(category);
        Assert.True(updatedCategory.IsExpanded);
    }

    [Fact]
    public void CategoryExpand_WithInvalidContentContainer_DoesNotCrash()
    {
        // Test expand when ContentContainer is Entity.Null
        using var world = new World();
        var propertyGridSystem = new UIPropertyGridSystem();
        world.AddSystem(propertyGridSystem);

        var propertyGrid = world.Spawn()
            .With(UIElement.Default)
            .Build();

        var category = world.Spawn()
            .With(UIElement.Default)
            .With(new UIPropertyCategory
            {
                IsExpanded = false,
                ContentContainer = Entity.Null,
                PropertyGrid = propertyGrid
            })
            .Build();

        var header = world.Spawn()
            .With(UIElement.Default)
            .With(UIInteractable.Clickable())
            .With(new UIPropertyCategoryHeaderTag())
            .Build();
        world.SetParent(header, category);

        ref var interactable = ref world.Get<UIInteractable>(header);
        interactable.PendingEvents |= UIEventType.Click;

        propertyGridSystem.Update(0);

        ref readonly var updatedCategory = ref world.Get<UIPropertyCategory>(category);
        Assert.True(updatedCategory.IsExpanded);
    }

    [Fact]
    public void CategoryExpand_WithInvalidPropertyGrid_DoesNotFireEvent()
    {
        // Test expand when PropertyGrid is Entity.Null - no event should fire
        using var world = new World();
        var propertyGridSystem = new UIPropertyGridSystem();
        world.AddSystem(propertyGridSystem);

        var contentContainer = world.Spawn()
            .With(new UIElement { Visible = false })
            .Build();

        var category = world.Spawn()
            .With(UIElement.Default)
            .With(new UIPropertyCategory
            {
                IsExpanded = false,
                ContentContainer = contentContainer,
                PropertyGrid = Entity.Null
            })
            .Build();

        var header = world.Spawn()
            .With(UIElement.Default)
            .With(UIInteractable.Clickable())
            .With(new UIPropertyCategoryHeaderTag())
            .Build();
        world.SetParent(header, category);

        bool eventFired = false;
        world.Subscribe<UIPropertyCategoryExpandedEvent>(e => eventFired = true);

        ref var interactable = ref world.Get<UIInteractable>(header);
        interactable.PendingEvents |= UIEventType.Click;

        propertyGridSystem.Update(0);

        ref readonly var updatedCategory = ref world.Get<UIPropertyCategory>(category);
        Assert.True(updatedCategory.IsExpanded);
        Assert.False(eventFired);
    }

    [Fact]
    public void CategoryCollapse_WithInvalidPropertyGrid_DoesNotFireEvent()
    {
        // Test collapse when PropertyGrid is Entity.Null
        using var world = new World();
        var propertyGridSystem = new UIPropertyGridSystem();
        world.AddSystem(propertyGridSystem);

        var contentContainer = world.Spawn()
            .With(new UIElement { Visible = true })
            .Build();

        var category = world.Spawn()
            .With(UIElement.Default)
            .With(new UIPropertyCategory
            {
                IsExpanded = true,
                ContentContainer = contentContainer,
                PropertyGrid = Entity.Null
            })
            .Build();

        var header = world.Spawn()
            .With(UIElement.Default)
            .With(UIInteractable.Clickable())
            .With(new UIPropertyCategoryHeaderTag())
            .Build();
        world.SetParent(header, category);

        bool eventFired = false;
        world.Subscribe<UIPropertyCategoryCollapsedEvent>(e => eventFired = true);

        ref var interactable = ref world.Get<UIInteractable>(header);
        interactable.PendingEvents |= UIEventType.Click;

        propertyGridSystem.Update(0);

        ref readonly var updatedCategory = ref world.Get<UIPropertyCategory>(category);
        Assert.False(updatedCategory.IsExpanded);
        Assert.False(eventFired);
    }

    [Fact]
    public void CategoryCollapse_UpdatesArrowVisual()
    {
        // Test arrow visual when collapsing (color changes to collapsed state)
        using var world = new World();
        var propertyGridSystem = new UIPropertyGridSystem();
        world.AddSystem(propertyGridSystem);

        var propertyGrid = world.Spawn()
            .With(UIElement.Default)
            .Build();

        var contentContainer = world.Spawn()
            .With(new UIElement { Visible = true })
            .Build();

        var category = world.Spawn()
            .With(UIElement.Default)
            .With(new UIPropertyCategory
            {
                IsExpanded = true,
                ContentContainer = contentContainer,
                PropertyGrid = propertyGrid
            })
            .Build();

        var header = world.Spawn()
            .With(UIElement.Default)
            .With(UIInteractable.Clickable())
            .With(new UIPropertyCategoryHeaderTag())
            .Build();
        world.SetParent(header, category);

        // Arrow with expanded state color
        var arrow = world.Spawn()
            .With(UIElement.Default)
            .With(new UIStyle { BackgroundColor = new Vector4(0.5f, 0.5f, 0.5f, 1f) })
            .With(new UIPropertyCategoryArrowTag())
            .Build();
        world.SetParent(arrow, header);

        ref var interactable = ref world.Get<UIInteractable>(header);
        interactable.PendingEvents |= UIEventType.Click;

        propertyGridSystem.Update(0);

        ref readonly var arrowStyle = ref world.Get<UIStyle>(arrow);
        // Collapsed arrow should have higher brightness (0.7f)
        Assert.True(arrowStyle.BackgroundColor.X > 0.6f);
    }

    [Fact]
    public void CategoryExpand_ArrowWithoutStyle_DoesNotCrash()
    {
        // Test when arrow child has the tag but no UIStyle component
        using var world = new World();
        var propertyGridSystem = new UIPropertyGridSystem();
        world.AddSystem(propertyGridSystem);

        var propertyGrid = world.Spawn()
            .With(UIElement.Default)
            .Build();

        var contentContainer = world.Spawn()
            .With(new UIElement { Visible = false })
            .Build();

        var category = world.Spawn()
            .With(UIElement.Default)
            .With(new UIPropertyCategory
            {
                IsExpanded = false,
                ContentContainer = contentContainer,
                PropertyGrid = propertyGrid
            })
            .Build();

        var header = world.Spawn()
            .With(UIElement.Default)
            .With(UIInteractable.Clickable())
            .With(new UIPropertyCategoryHeaderTag())
            .Build();
        world.SetParent(header, category);

        // Arrow without UIStyle
        var arrow = world.Spawn()
            .With(UIElement.Default)
            .With(new UIPropertyCategoryArrowTag())
            .Build();
        world.SetParent(arrow, header);

        ref var interactable = ref world.Get<UIInteractable>(header);
        interactable.PendingEvents |= UIEventType.Click;

        // Should not throw
        propertyGridSystem.Update(0);

        ref readonly var updatedCategory = ref world.Get<UIPropertyCategory>(category);
        Assert.True(updatedCategory.IsExpanded);
    }

    [Fact]
    public void CategoryExpand_WithNoArrowChild_DoesNotCrash()
    {
        // Test expand when header has no arrow child at all
        using var world = new World();
        var propertyGridSystem = new UIPropertyGridSystem();
        world.AddSystem(propertyGridSystem);

        var propertyGrid = world.Spawn()
            .With(UIElement.Default)
            .Build();

        var contentContainer = world.Spawn()
            .With(new UIElement { Visible = false })
            .Build();

        var category = world.Spawn()
            .With(UIElement.Default)
            .With(new UIPropertyCategory
            {
                IsExpanded = false,
                ContentContainer = contentContainer,
                PropertyGrid = propertyGrid
            })
            .Build();

        var header = world.Spawn()
            .With(UIElement.Default)
            .With(UIInteractable.Clickable())
            .With(new UIPropertyCategoryHeaderTag())
            .Build();
        world.SetParent(header, category);

        // No children at all

        ref var interactable = ref world.Get<UIInteractable>(header);
        interactable.PendingEvents |= UIEventType.Click;

        propertyGridSystem.Update(0);

        ref readonly var updatedCategory = ref world.Get<UIPropertyCategory>(category);
        Assert.True(updatedCategory.IsExpanded);
    }

    [Fact]
    public void CategoryExpand_ContentContainerWithoutUIElement_DoesNotCrash()
    {
        // Test when ContentContainer exists but doesn't have UIElement
        using var world = new World();
        var propertyGridSystem = new UIPropertyGridSystem();
        world.AddSystem(propertyGridSystem);

        var propertyGrid = world.Spawn()
            .With(UIElement.Default)
            .Build();

        // Content container without UIElement
        var contentContainer = world.Spawn().Build();

        var category = world.Spawn()
            .With(UIElement.Default)
            .With(new UIPropertyCategory
            {
                IsExpanded = false,
                ContentContainer = contentContainer,
                PropertyGrid = propertyGrid
            })
            .Build();

        var header = world.Spawn()
            .With(UIElement.Default)
            .With(UIInteractable.Clickable())
            .With(new UIPropertyCategoryHeaderTag())
            .Build();
        world.SetParent(header, category);

        ref var interactable = ref world.Get<UIInteractable>(header);
        interactable.PendingEvents |= UIEventType.Click;

        // Should not throw
        propertyGridSystem.Update(0);

        ref readonly var updatedCategory = ref world.Get<UIPropertyCategory>(category);
        Assert.True(updatedCategory.IsExpanded);
    }

    #endregion
}
