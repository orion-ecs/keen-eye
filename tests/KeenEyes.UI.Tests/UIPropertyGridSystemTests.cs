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
        interactable.PendingEvents |= UIEventFlags.Click;

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
        interactable.PendingEvents |= UIEventFlags.Click;

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
        interactable.PendingEvents |= UIEventFlags.Click;

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
        interactable.PendingEvents |= UIEventFlags.Click;

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
        interactable.PendingEvents |= UIEventFlags.Click;

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
}
