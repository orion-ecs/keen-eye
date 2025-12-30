using System.Numerics;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.UI.Abstractions;
using KeenEyes.UI.Widgets;

namespace KeenEyes.UI.Tests;

/// <summary>
/// Tests for WidgetFactory property grid widget creation methods.
/// </summary>
public class WidgetFactoryPropertyGridTests
{
    private static readonly FontHandle testFont = new(1);

    #region CreatePropertyGrid Tests

    [Fact]
    public void CreatePropertyGrid_HasRequiredComponents()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var grid = WidgetFactory.CreatePropertyGrid(world, parent, testFont);

        Assert.True(world.Has<UIElement>(grid));
        Assert.True(world.Has<UIRect>(grid));
        Assert.True(world.Has<UIStyle>(grid));
        Assert.True(world.Has<UILayout>(grid));
        Assert.True(world.Has<UIPropertyGrid>(grid));
    }

    [Fact]
    public void CreatePropertyGrid_HasVerticalLayout()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var grid = WidgetFactory.CreatePropertyGrid(world, parent, testFont);

        ref readonly var layout = ref world.Get<UILayout>(grid);
        Assert.Equal(LayoutDirection.Vertical, layout.Direction);
    }

    [Fact]
    public void CreatePropertyGrid_HasContentContainer()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var grid = WidgetFactory.CreatePropertyGrid(world, parent, testFont);

        ref readonly var gridData = ref world.Get<UIPropertyGrid>(grid);
        Assert.True(world.IsAlive(gridData.ContentContainer));
    }

    [Fact]
    public void CreatePropertyGrid_ContentContainerIsChild()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var grid = WidgetFactory.CreatePropertyGrid(world, parent, testFont);

        ref readonly var gridData = ref world.Get<UIPropertyGrid>(grid);
        Assert.Equal(grid, world.GetParent(gridData.ContentContainer));
    }

    [Fact]
    public void CreatePropertyGrid_InitializesWithZeroCounts()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var grid = WidgetFactory.CreatePropertyGrid(world, parent, testFont);

        ref readonly var gridData = ref world.Get<UIPropertyGrid>(grid);
        Assert.Equal(0, gridData.CategoryCount);
        Assert.Equal(0, gridData.RowCount);
    }

    [Fact]
    public void CreatePropertyGrid_AppliesConfig()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new PropertyGridConfig(Width: 400, Height: 600, LabelWidthRatio: 0.5f);

        var grid = WidgetFactory.CreatePropertyGrid(world, parent, testFont, config);

        ref readonly var rect = ref world.Get<UIRect>(grid);
        Assert.Equal(new Vector2(400, 600), rect.Size);
        Assert.Equal(UISizeMode.Fixed, rect.WidthMode);
        Assert.Equal(UISizeMode.Fixed, rect.HeightMode);

        ref readonly var gridData = ref world.Get<UIPropertyGrid>(grid);
        Assert.Equal(0.5f, gridData.LabelWidthRatio);
    }

    [Fact]
    public void CreatePropertyGrid_WithDefaultConfig_UsesFillWidth()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var grid = WidgetFactory.CreatePropertyGrid(world, parent, testFont);

        ref readonly var rect = ref world.Get<UIRect>(grid);
        Assert.Equal(UISizeMode.Fill, rect.WidthMode);
        Assert.Equal(UISizeMode.Fill, rect.HeightMode);
    }

    #endregion

    #region CreatePropertyCategory Tests

    [Fact]
    public void CreatePropertyCategory_HasRequiredComponents()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var grid = WidgetFactory.CreatePropertyGrid(world, parent, testFont);

        var category = WidgetFactory.CreatePropertyCategory(world, grid, "Transform", testFont);

        Assert.True(world.Has<UIElement>(category));
        Assert.True(world.Has<UIRect>(category));
        Assert.True(world.Has<UILayout>(category));
        Assert.True(world.Has<UIPropertyCategory>(category));
    }

    [Fact]
    public void CreatePropertyCategory_IncrementsCategoryCount()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var grid = WidgetFactory.CreatePropertyGrid(world, parent, testFont);

        WidgetFactory.CreatePropertyCategory(world, grid, "Category 1", testFont);
        WidgetFactory.CreatePropertyCategory(world, grid, "Category 2", testFont);

        ref readonly var gridData = ref world.Get<UIPropertyGrid>(grid);
        Assert.Equal(2, gridData.CategoryCount);
    }

    [Fact]
    public void CreatePropertyCategory_IsChildOfContentContainer()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var grid = WidgetFactory.CreatePropertyGrid(world, parent, testFont);

        var category = WidgetFactory.CreatePropertyCategory(world, grid, "Transform", testFont);

        ref readonly var gridData = ref world.Get<UIPropertyGrid>(grid);
        Assert.Equal(gridData.ContentContainer, world.GetParent(category));
    }

    [Fact]
    public void CreatePropertyCategory_StoresCategoryName()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var grid = WidgetFactory.CreatePropertyGrid(world, parent, testFont);

        var category = WidgetFactory.CreatePropertyCategory(world, grid, "Transform", testFont);

        ref readonly var categoryData = ref world.Get<UIPropertyCategory>(category);
        Assert.Equal("Transform", categoryData.Name);
    }

    [Fact]
    public void CreatePropertyCategory_InitiallyExpanded_ByDefault()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var grid = WidgetFactory.CreatePropertyGrid(world, parent, testFont);

        var category = WidgetFactory.CreatePropertyCategory(world, grid, "Transform", testFont);

        ref readonly var categoryData = ref world.Get<UIPropertyCategory>(category);
        Assert.True(categoryData.IsExpanded);
    }

    [Fact]
    public void CreatePropertyCategory_CanBeCollapsed()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var grid = WidgetFactory.CreatePropertyGrid(world, parent, testFont);

        var category = WidgetFactory.CreatePropertyCategory(world, grid, "Transform", testFont, isExpanded: false);

        ref readonly var categoryData = ref world.Get<UIPropertyCategory>(category);
        Assert.False(categoryData.IsExpanded);
    }

    [Fact]
    public void CreatePropertyCategory_HasContentContainer()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var grid = WidgetFactory.CreatePropertyGrid(world, parent, testFont);

        var category = WidgetFactory.CreatePropertyCategory(world, grid, "Transform", testFont);

        ref readonly var categoryData = ref world.Get<UIPropertyCategory>(category);
        Assert.True(world.IsAlive(categoryData.ContentContainer));
    }

    [Fact]
    public void CreatePropertyCategory_HasClickableHeader()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var grid = WidgetFactory.CreatePropertyGrid(world, parent, testFont);

        var category = WidgetFactory.CreatePropertyCategory(world, grid, "Transform", testFont);

        var children = world.GetChildren(category).ToList();
        var header = children.FirstOrDefault(c => world.Has<UIPropertyCategoryHeaderTag>(c));
        Assert.NotEqual(Entity.Null, header);
        Assert.True(world.Has<UIInteractable>(header));
    }

    [Fact]
    public void CreatePropertyCategory_HeaderHasExpandArrow()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var grid = WidgetFactory.CreatePropertyGrid(world, parent, testFont);

        var category = WidgetFactory.CreatePropertyCategory(world, grid, "Transform", testFont);

        var children = world.GetChildren(category).ToList();
        var header = children.FirstOrDefault(c => world.Has<UIPropertyCategoryHeaderTag>(c));
        var headerChildren = world.GetChildren(header).ToList();
        var arrow = headerChildren.FirstOrDefault(c => world.Has<UIPropertyCategoryArrowTag>(c));
        Assert.NotEqual(Entity.Null, arrow);
    }

    #endregion

    #region CreatePropertyRow Tests

    [Fact]
    public void CreatePropertyRow_HasRequiredComponents()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var grid = WidgetFactory.CreatePropertyGrid(world, parent, testFont);
        ref var gridData = ref world.Get<UIPropertyGrid>(grid);

        var row = WidgetFactory.CreatePropertyRow(world, grid, gridData.ContentContainer,
            "position", "Position", PropertyType.String, testFont);

        Assert.True(world.Has<UIElement>(row));
        Assert.True(world.Has<UIRect>(row));
        Assert.True(world.Has<UILayout>(row));
        Assert.True(world.Has<UIPropertyRow>(row));
    }

    [Fact]
    public void CreatePropertyRow_IncrementsRowCount()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var grid = WidgetFactory.CreatePropertyGrid(world, parent, testFont);
        ref var gridData = ref world.Get<UIPropertyGrid>(grid);

        WidgetFactory.CreatePropertyRow(world, grid, gridData.ContentContainer,
            "prop1", "Property 1", PropertyType.String, testFont);
        WidgetFactory.CreatePropertyRow(world, grid, gridData.ContentContainer,
            "prop2", "Property 2", PropertyType.Int, testFont);

        ref readonly var gridDataAfter = ref world.Get<UIPropertyGrid>(grid);
        Assert.Equal(2, gridDataAfter.RowCount);
    }

    [Fact]
    public void CreatePropertyRow_StoresPropertyInfo()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var grid = WidgetFactory.CreatePropertyGrid(world, parent, testFont);
        ref var gridData = ref world.Get<UIPropertyGrid>(grid);

        var row = WidgetFactory.CreatePropertyRow(world, grid, gridData.ContentContainer,
            "myProperty", "My Property", PropertyType.Float, testFont);

        ref readonly var rowData = ref world.Get<UIPropertyRow>(row);
        Assert.Equal("myProperty", rowData.PropertyName);
        Assert.Equal("My Property", rowData.Label);
        Assert.Equal(PropertyType.Float, rowData.Type);
        Assert.Equal(grid, rowData.PropertyGrid);
    }

    [Fact]
    public void CreatePropertyRow_HasEditorEntity()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var grid = WidgetFactory.CreatePropertyGrid(world, parent, testFont);
        ref var gridData = ref world.Get<UIPropertyGrid>(grid);

        var row = WidgetFactory.CreatePropertyRow(world, grid, gridData.ContentContainer,
            "prop", "Property", PropertyType.String, testFont);

        ref readonly var rowData = ref world.Get<UIPropertyRow>(row);
        Assert.True(world.IsAlive(rowData.EditorEntity));
    }

    [Fact]
    public void CreatePropertyRow_ReadOnly_SetsIsReadOnlyFlag()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var grid = WidgetFactory.CreatePropertyGrid(world, parent, testFont);
        ref var gridData = ref world.Get<UIPropertyGrid>(grid);

        var row = WidgetFactory.CreatePropertyRow(world, grid, gridData.ContentContainer,
            "prop", "Property", PropertyType.String, testFont, isReadOnly: true);

        ref readonly var rowData = ref world.Get<UIPropertyRow>(row);
        Assert.True(rowData.IsReadOnly);
    }

    [Theory]
    [InlineData(PropertyType.String)]
    [InlineData(PropertyType.Int)]
    [InlineData(PropertyType.Float)]
    [InlineData(PropertyType.Bool)]
    [InlineData(PropertyType.Color)]
    [InlineData(PropertyType.Vector2)]
    [InlineData(PropertyType.Vector3)]
    [InlineData(PropertyType.Vector4)]
    [InlineData(PropertyType.Enum)]
    public void CreatePropertyRow_WithDifferentTypes_CreatesAppropriateEditor(PropertyType type)
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var grid = WidgetFactory.CreatePropertyGrid(world, parent, testFont);
        ref var gridData = ref world.Get<UIPropertyGrid>(grid);

        var row = WidgetFactory.CreatePropertyRow(world, grid, gridData.ContentContainer,
            "prop", "Property", type, testFont);

        ref readonly var rowData = ref world.Get<UIPropertyRow>(row);
        Assert.True(world.IsAlive(rowData.EditorEntity));
    }

    [Fact]
    public void CreatePropertyRow_InCategory_IsChildOfCategoryContent()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var grid = WidgetFactory.CreatePropertyGrid(world, parent, testFont);
        var category = WidgetFactory.CreatePropertyCategory(world, grid, "Transform", testFont);
        ref var categoryData = ref world.Get<UIPropertyCategory>(category);

        var row = WidgetFactory.CreatePropertyRow(world, grid, categoryData.ContentContainer,
            "x", "X", PropertyType.Float, testFont, category: category);

        Assert.Equal(categoryData.ContentContainer, world.GetParent(row));
    }

    #endregion

    #region CreatePropertyGridWithCategories Tests

    [Fact]
    public void CreatePropertyGridWithCategories_CreatesGridWithCategories()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var categories = new[]
        {
            new PropertyCategoryDef("Transform", true, new[]
            {
                new PropertyDef("x", "X", PropertyType.Float),
                new PropertyDef("y", "Y", PropertyType.Float)
            }),
            new PropertyCategoryDef("Appearance", true, new[]
            {
                new PropertyDef("color", "Color", PropertyType.Color)
            })
        };

        var grid = WidgetFactory.CreatePropertyGridWithCategories(world, parent, testFont, categories);

        Assert.True(world.Has<UIPropertyGrid>(grid));
        ref readonly var gridData = ref world.Get<UIPropertyGrid>(grid);
        Assert.Equal(2, gridData.CategoryCount);
        Assert.Equal(3, gridData.RowCount);
    }

    [Fact]
    public void CreatePropertyGridWithCategories_AppliesExpandedState()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var categories = new[]
        {
            new PropertyCategoryDef("Expanded", true, new[]
            {
                new PropertyDef("x", "X", PropertyType.Float)
            }),
            new PropertyCategoryDef("Collapsed", false, new[]
            {
                new PropertyDef("y", "Y", PropertyType.Float)
            })
        };

        var grid = WidgetFactory.CreatePropertyGridWithCategories(world, parent, testFont, categories);

        // Find categories and verify expanded state
        var categoryCount = 0;
        foreach (var entity in world.Query<UIPropertyCategory>())
        {
            ref readonly var cat = ref world.Get<UIPropertyCategory>(entity);
            if (cat.Name == "Expanded")
            {
                Assert.True(cat.IsExpanded);
                categoryCount++;
            }
            else if (cat.Name == "Collapsed")
            {
                Assert.False(cat.IsExpanded);
                categoryCount++;
            }
        }
        Assert.Equal(2, categoryCount);
    }

    #endregion

    #region CreatePropertyGridFlat Tests

    [Fact]
    public void CreatePropertyGridFlat_CreatesGridWithoutCategories()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var properties = new[]
        {
            new PropertyDef("name", "Name", PropertyType.String),
            new PropertyDef("age", "Age", PropertyType.Int),
            new PropertyDef("active", "Active", PropertyType.Bool)
        };

        var grid = WidgetFactory.CreatePropertyGridFlat(world, parent, testFont, properties);

        Assert.True(world.Has<UIPropertyGrid>(grid));
        ref readonly var gridData = ref world.Get<UIPropertyGrid>(grid);
        Assert.Equal(0, gridData.CategoryCount);
        Assert.Equal(3, gridData.RowCount);
    }

    [Fact]
    public void CreatePropertyGridFlat_AppliesReadOnlyFlag()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var properties = new[]
        {
            new PropertyDef("id", "ID", PropertyType.String, IsReadOnly: true)
        };

        var grid = WidgetFactory.CreatePropertyGridFlat(world, parent, testFont, properties);

        foreach (var entity in world.Query<UIPropertyRow>())
        {
            ref readonly var row = ref world.Get<UIPropertyRow>(entity);
            if (row.PropertyName == "id")
            {
                Assert.True(row.IsReadOnly);
            }
        }
    }

    #endregion

    #region SetPropertyValue Tests

    [Fact]
    public void SetPropertyValue_String_UpdatesTextContent()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var grid = WidgetFactory.CreatePropertyGrid(world, parent, testFont);
        ref var gridData = ref world.Get<UIPropertyGrid>(grid);
        var row = WidgetFactory.CreatePropertyRow(world, grid, gridData.ContentContainer,
            "name", "Name", PropertyType.String, testFont);

        WidgetFactory.SetPropertyValue(world, row, "Hello World");

        ref readonly var rowData = ref world.Get<UIPropertyRow>(row);
        ref readonly var text = ref world.Get<UIText>(rowData.EditorEntity);
        Assert.Equal("Hello World", text.Content);
    }

    [Fact]
    public void SetPropertyValue_Int_UpdatesTextContent()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var grid = WidgetFactory.CreatePropertyGrid(world, parent, testFont);
        ref var gridData = ref world.Get<UIPropertyGrid>(grid);
        var row = WidgetFactory.CreatePropertyRow(world, grid, gridData.ContentContainer,
            "count", "Count", PropertyType.Int, testFont);

        WidgetFactory.SetPropertyValue(world, row, 42);

        ref readonly var rowData = ref world.Get<UIPropertyRow>(row);
        ref readonly var text = ref world.Get<UIText>(rowData.EditorEntity);
        Assert.Equal("42", text.Content);
    }

    [Fact]
    public void SetPropertyValue_Bool_UpdatesStyle()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var grid = WidgetFactory.CreatePropertyGrid(world, parent, testFont);
        ref var gridData = ref world.Get<UIPropertyGrid>(grid);
        var row = WidgetFactory.CreatePropertyRow(world, grid, gridData.ContentContainer,
            "active", "Active", PropertyType.Bool, testFont);

        WidgetFactory.SetPropertyValue(world, row, true);

        ref readonly var rowData = ref world.Get<UIPropertyRow>(row);
        ref readonly var style = ref world.Get<UIStyle>(rowData.EditorEntity);
        // When checked, background should be a blue-ish color
        Assert.True(style.BackgroundColor.X > 0.2f); // Some blue component
    }

    [Fact]
    public void SetPropertyValue_Color_UpdatesBackgroundColor()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var grid = WidgetFactory.CreatePropertyGrid(world, parent, testFont);
        ref var gridData = ref world.Get<UIPropertyGrid>(grid);
        var row = WidgetFactory.CreatePropertyRow(world, grid, gridData.ContentContainer,
            "tint", "Tint", PropertyType.Color, testFont);
        var redColor = new Vector4(1f, 0f, 0f, 1f);

        WidgetFactory.SetPropertyValue(world, row, redColor);

        ref readonly var rowData = ref world.Get<UIPropertyRow>(row);
        ref readonly var style = ref world.Get<UIStyle>(rowData.EditorEntity);
        Assert.Equal(redColor, style.BackgroundColor);
    }

    [Fact]
    public void SetPropertyValue_NullValue_DoesNotThrow()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var grid = WidgetFactory.CreatePropertyGrid(world, parent, testFont);
        ref var gridData = ref world.Get<UIPropertyGrid>(grid);
        var row = WidgetFactory.CreatePropertyRow(world, grid, gridData.ContentContainer,
            "name", "Name", PropertyType.String, testFont);

        // Should not throw
        WidgetFactory.SetPropertyValue(world, row, null);

        ref readonly var rowData = ref world.Get<UIPropertyRow>(row);
        ref readonly var text = ref world.Get<UIText>(rowData.EditorEntity);
        Assert.Equal("", text.Content);
    }

    #endregion

    #region SetPropertyCategoryExpanded Tests

    [Fact]
    public void SetPropertyCategoryExpanded_UpdatesIsExpanded()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var grid = WidgetFactory.CreatePropertyGrid(world, parent, testFont);
        var category = WidgetFactory.CreatePropertyCategory(world, grid, "Transform", testFont, isExpanded: true);

        WidgetFactory.SetPropertyCategoryExpanded(world, category, false);

        ref readonly var categoryData = ref world.Get<UIPropertyCategory>(category);
        Assert.False(categoryData.IsExpanded);
    }

    [Fact]
    public void SetPropertyCategoryExpanded_UpdatesContentVisibility()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var grid = WidgetFactory.CreatePropertyGrid(world, parent, testFont);
        var category = WidgetFactory.CreatePropertyCategory(world, grid, "Transform", testFont, isExpanded: true);
        ref readonly var categoryData = ref world.Get<UIPropertyCategory>(category);

        WidgetFactory.SetPropertyCategoryExpanded(world, category, false);

        ref readonly var contentElement = ref world.Get<UIElement>(categoryData.ContentContainer);
        Assert.False(contentElement.Visible);
    }

    [Fact]
    public void SetPropertyCategoryExpanded_Expand_ShowsContent()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var grid = WidgetFactory.CreatePropertyGrid(world, parent, testFont);
        var category = WidgetFactory.CreatePropertyCategory(world, grid, "Transform", testFont, isExpanded: false);
        ref readonly var categoryData = ref world.Get<UIPropertyCategory>(category);

        WidgetFactory.SetPropertyCategoryExpanded(world, category, true);

        ref readonly var contentElement = ref world.Get<UIElement>(categoryData.ContentContainer);
        Assert.True(contentElement.Visible);
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
