using System.Numerics;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI.Tests;

/// <summary>
/// Additional comprehensive tests for UI.Abstractions components to improve coverage.
/// </summary>
public class UIAbstractionsAdditionalComponentTests
{
    #region UIDataGrid Event Tests

    [Fact]
    public void UIGridRowDoubleClickEvent_Constructor_SetsAllProperties()
    {
        var dataGrid = new Entity(100, 1);
        var row = new Entity(101, 1);
        var evt = new UIGridRowDoubleClickEvent(dataGrid, row, 5);

        Assert.Equal(dataGrid, evt.DataGrid);
        Assert.Equal(row, evt.Row);
        Assert.Equal(5, evt.RowIndex);
    }

    [Fact]
    public void UIGridRowDoubleClickEvent_WithDifferentIndices_StoresCorrectValues()
    {
        var dataGrid = new Entity(200, 1);
        var row = new Entity(201, 1);
        var evt1 = new UIGridRowDoubleClickEvent(dataGrid, row, 0);
        var evt2 = new UIGridRowDoubleClickEvent(dataGrid, row, 999);

        Assert.Equal(0, evt1.RowIndex);
        Assert.Equal(999, evt2.RowIndex);
    }

    #endregion

    #region UIDock Tests

    [Fact]
    public void UIDockZone_Constructor_SetsZone()
    {
        var dockZone = new UIDockZone(DockZone.Left);

        Assert.Equal(DockZone.Left, dockZone.Zone);
        Assert.Equal(200f, dockZone.Size);
        Assert.Equal(100f, dockZone.MinSize);
        Assert.False(dockZone.IsCollapsed);
        Assert.Equal(new Entity(0, 0), dockZone.TabGroup);
        Assert.Equal(new Entity(0, 0), dockZone.Container);
    }

    [Fact]
    public void UIDockZone_WithRightZone_SetsCorrectly()
    {
        var dockZone = new UIDockZone(DockZone.Right);

        Assert.Equal(DockZone.Right, dockZone.Zone);
    }

    [Fact]
    public void UIDockZone_AllProperties_CanBeModified()
    {
        var container = new Entity(500, 1);
        var tabGroup = new Entity(501, 1);
        var dockZone = new UIDockZone(DockZone.Top)
        {
            Size = 300f,
            MinSize = 150f,
            IsCollapsed = true,
            TabGroup = tabGroup,
            Container = container
        };

        Assert.Equal(DockZone.Top, dockZone.Zone);
        Assert.Equal(300f, dockZone.Size);
        Assert.Equal(150f, dockZone.MinSize);
        Assert.True(dockZone.IsCollapsed);
        Assert.Equal(tabGroup, dockZone.TabGroup);
        Assert.Equal(container, dockZone.Container);
    }

    [Fact]
    public void UIDockPanel_Constructor_SetsTitle()
    {
        var panel = new UIDockPanel("Inspector");

        Assert.Equal("Inspector", panel.Title);
        Assert.Equal(DockState.Floating, panel.State);
        Assert.Equal(DockZone.None, panel.CurrentZone);
        Assert.Equal(Entity.Null, panel.DockContainer);
        Assert.True(panel.CanClose);
        Assert.True(panel.CanFloat);
        Assert.True(panel.CanDock);
        Assert.Equal(DockZone.All, panel.AllowedZones);
        Assert.Equal(Vector2.Zero, panel.FloatingPosition);
        Assert.Equal(new Vector2(300, 200), panel.FloatingSize);
    }

    [Fact]
    public void UIDockPanel_AllProperties_CanBeModified()
    {
        var container = new Entity(600, 1);
        var panel = new UIDockPanel("Properties")
        {
            State = DockState.Docked,
            CurrentZone = DockZone.Right,
            DockContainer = container,
            CanClose = false,
            CanFloat = false,
            CanDock = false,
            AllowedZones = DockZone.Left | DockZone.Right,
            FloatingPosition = new Vector2(100, 100),
            FloatingSize = new Vector2(400, 300)
        };

        Assert.Equal("Properties", panel.Title);
        Assert.Equal(DockState.Docked, panel.State);
        Assert.Equal(DockZone.Right, panel.CurrentZone);
        Assert.Equal(container, panel.DockContainer);
        Assert.False(panel.CanClose);
        Assert.False(panel.CanFloat);
        Assert.False(panel.CanDock);
        Assert.Equal(DockZone.Left | DockZone.Right, panel.AllowedZones);
        Assert.Equal(new Vector2(100, 100), panel.FloatingPosition);
        Assert.Equal(new Vector2(400, 300), panel.FloatingSize);
    }

    [Fact]
    public void UIDockPanel_WithEmptyTitle_AllowsEmpty()
    {
        var panel = new UIDockPanel("");

        Assert.Equal("", panel.Title);
    }

    #endregion

    #region UIMenu Tests

    [Fact]
    public void UIMenuShortcut_Constructor_SetsShortcut()
    {
        var shortcut = new UIMenuShortcut("Ctrl+S");

        Assert.Equal("Ctrl+S", shortcut.Shortcut);
    }

    [Fact]
    public void UIMenuShortcut_WithDifferentShortcuts_StoresCorrectly()
    {
        var shortcut1 = new UIMenuShortcut("Ctrl+C");
        var shortcut2 = new UIMenuShortcut("Alt+F4");
        var shortcut3 = new UIMenuShortcut("Shift+Delete");

        Assert.Equal("Ctrl+C", shortcut1.Shortcut);
        Assert.Equal("Alt+F4", shortcut2.Shortcut);
        Assert.Equal("Shift+Delete", shortcut3.Shortcut);
    }

    [Fact]
    public void UIMenuShortcut_WithEmptyString_AllowsEmpty()
    {
        var shortcut = new UIMenuShortcut("");

        Assert.Equal("", shortcut.Shortcut);
    }

    #endregion

    #region UIPropertyGrid Tests

    [Fact]
    public void UIPropertyGrid_DefaultConstructor_HasDefaultLabelWidth()
    {
        var grid = new UIPropertyGrid();

        // Default constructor without parameters sets fields to default values (0)
        Assert.Equal(0f, grid.LabelWidthRatio);
        Assert.Equal(new Entity(0, 0), grid.ContentContainer);
        Assert.Equal(0, grid.CategoryCount);
        Assert.Equal(0, grid.RowCount);
    }

    [Fact]
    public void UIPropertyGrid_ConstructorWithRatio_SetsRatio()
    {
        var grid = new UIPropertyGrid(0.6f);

        Assert.Equal(0.6f, grid.LabelWidthRatio);
        Assert.Equal(Entity.Null, grid.ContentContainer);
        Assert.Equal(0, grid.CategoryCount);
        Assert.Equal(0, grid.RowCount);
    }

    [Fact]
    public void UIPropertyGrid_AllProperties_CanBeModified()
    {
        var container = new Entity(700, 1);
        var grid = new UIPropertyGrid(0.5f)
        {
            ContentContainer = container,
            CategoryCount = 5,
            RowCount = 20
        };

        Assert.Equal(0.5f, grid.LabelWidthRatio);
        Assert.Equal(container, grid.ContentContainer);
        Assert.Equal(5, grid.CategoryCount);
        Assert.Equal(20, grid.RowCount);
    }

    [Fact]
    public void UIPropertyCategory_Constructor_SetsProperties()
    {
        var propertyGrid = new Entity(800, 1);
        var category = new UIPropertyCategory(propertyGrid, "Transform");

        Assert.Equal(propertyGrid, category.PropertyGrid);
        Assert.Equal("Transform", category.Name);
        Assert.True(category.IsExpanded);
        Assert.Equal(Entity.Null, category.ContentContainer);
        Assert.Equal(0, category.Index);
    }

    [Fact]
    public void UIPropertyCategory_AllProperties_CanBeModified()
    {
        var propertyGrid = new Entity(801, 1);
        var container = new Entity(802, 1);
        var category = new UIPropertyCategory(propertyGrid, "Physics")
        {
            IsExpanded = false,
            ContentContainer = container,
            Index = 3
        };

        Assert.Equal(propertyGrid, category.PropertyGrid);
        Assert.Equal("Physics", category.Name);
        Assert.False(category.IsExpanded);
        Assert.Equal(container, category.ContentContainer);
        Assert.Equal(3, category.Index);
    }

    [Fact]
    public void UIPropertyCategory_WithEmptyName_AllowsEmpty()
    {
        var propertyGrid = new Entity(803, 1);
        var category = new UIPropertyCategory(propertyGrid, "");

        Assert.Equal("", category.Name);
    }

    [Fact]
    public void UIPropertyRow_Constructor_SetsProperties()
    {
        var propertyGrid = new Entity(900, 1);
        var row = new UIPropertyRow(propertyGrid, "position");

        Assert.Equal(propertyGrid, row.PropertyGrid);
        Assert.Equal("position", row.PropertyName);
        Assert.Equal("position", row.Label);
        Assert.Equal(PropertyType.String, row.Type);
        Assert.Equal(Entity.Null, row.Category);
        Assert.Equal(Entity.Null, row.EditorEntity);
        Assert.Equal(0, row.Index);
        Assert.False(row.IsReadOnly);
    }

    [Fact]
    public void UIPropertyRow_AllProperties_CanBeModified()
    {
        var propertyGrid = new Entity(901, 1);
        var category = new Entity(902, 1);
        var editor = new Entity(903, 1);
        var row = new UIPropertyRow(propertyGrid, "velocity")
        {
            Label = "Velocity (m/s)",
            Type = PropertyType.Vector3,
            Category = category,
            EditorEntity = editor,
            Index = 7,
            IsReadOnly = true
        };

        Assert.Equal(propertyGrid, row.PropertyGrid);
        Assert.Equal("velocity", row.PropertyName);
        Assert.Equal("Velocity (m/s)", row.Label);
        Assert.Equal(PropertyType.Vector3, row.Type);
        Assert.Equal(category, row.Category);
        Assert.Equal(editor, row.EditorEntity);
        Assert.Equal(7, row.Index);
        Assert.True(row.IsReadOnly);
    }

    [Fact]
    public void UIPropertyRow_AllPropertyTypes_AreValid()
    {
        var propertyGrid = new Entity(904, 1);

        var rowString = new UIPropertyRow(propertyGrid, "name") { Type = PropertyType.String };
        var rowInt = new UIPropertyRow(propertyGrid, "count") { Type = PropertyType.Int };
        var rowFloat = new UIPropertyRow(propertyGrid, "speed") { Type = PropertyType.Float };
        var rowBool = new UIPropertyRow(propertyGrid, "enabled") { Type = PropertyType.Bool };
        var rowColor = new UIPropertyRow(propertyGrid, "tint") { Type = PropertyType.Color };
        var rowVector2 = new UIPropertyRow(propertyGrid, "position2d") { Type = PropertyType.Vector2 };
        var rowVector3 = new UIPropertyRow(propertyGrid, "position3d") { Type = PropertyType.Vector3 };
        var rowVector4 = new UIPropertyRow(propertyGrid, "rotation") { Type = PropertyType.Vector4 };
        var rowEnum = new UIPropertyRow(propertyGrid, "state") { Type = PropertyType.Enum };
        var rowMultiLine = new UIPropertyRow(propertyGrid, "description") { Type = PropertyType.MultiLineString };
        var rowCustom = new UIPropertyRow(propertyGrid, "custom") { Type = PropertyType.Custom };

        Assert.Equal(PropertyType.String, rowString.Type);
        Assert.Equal(PropertyType.Int, rowInt.Type);
        Assert.Equal(PropertyType.Float, rowFloat.Type);
        Assert.Equal(PropertyType.Bool, rowBool.Type);
        Assert.Equal(PropertyType.Color, rowColor.Type);
        Assert.Equal(PropertyType.Vector2, rowVector2.Type);
        Assert.Equal(PropertyType.Vector3, rowVector3.Type);
        Assert.Equal(PropertyType.Vector4, rowVector4.Type);
        Assert.Equal(PropertyType.Enum, rowEnum.Type);
        Assert.Equal(PropertyType.MultiLineString, rowMultiLine.Type);
        Assert.Equal(PropertyType.Custom, rowCustom.Type);
    }

    #endregion

    #region UIRadialMenu Tests

    [Fact]
    public void UIRadialMenu_Constructor_SetsSliceCount()
    {
        var menu = new UIRadialMenu(8);

        Assert.False(menu.IsOpen);
        Assert.Equal(-1, menu.SelectedIndex);
        Assert.Equal(40f, menu.InnerRadius);
        Assert.Equal(120f, menu.OuterRadius);
        Assert.Equal(8, menu.SliceCount);
        Assert.Equal(0f, menu.OpenProgress);
        Assert.Equal(-MathF.PI / 2, menu.StartAngle);
        Assert.Equal(Vector2.Zero, menu.CenterPosition);
    }

    [Fact]
    public void UIRadialMenu_AllProperties_CanBeModified()
    {
        var menu = new UIRadialMenu(6)
        {
            IsOpen = true,
            SelectedIndex = 2,
            InnerRadius = 50f,
            OuterRadius = 150f,
            OpenProgress = 0.75f,
            StartAngle = 0f,
            CenterPosition = new Vector2(100, 100)
        };

        Assert.True(menu.IsOpen);
        Assert.Equal(2, menu.SelectedIndex);
        Assert.Equal(50f, menu.InnerRadius);
        Assert.Equal(150f, menu.OuterRadius);
        Assert.Equal(6, menu.SliceCount);
        Assert.Equal(0.75f, menu.OpenProgress);
        Assert.Equal(0f, menu.StartAngle);
        Assert.Equal(new Vector2(100, 100), menu.CenterPosition);
    }

    [Fact]
    public void UIRadialMenu_WithDifferentSliceCounts_StoresCorrectly()
    {
        var menu4 = new UIRadialMenu(4);
        var menu12 = new UIRadialMenu(12);

        Assert.Equal(4, menu4.SliceCount);
        Assert.Equal(12, menu12.SliceCount);
    }

    [Fact]
    public void UIRadialSlice_Constructor_SetsProperties()
    {
        var radialMenu = new Entity(1000, 1);
        var slice = new UIRadialSlice(radialMenu, 3);

        Assert.Equal(radialMenu, slice.RadialMenu);
        Assert.Equal(3, slice.Index);
        Assert.Equal(0, slice.StartAngle);
        Assert.Equal(0, slice.EndAngle);
        Assert.False(slice.HasSubmenu);
        Assert.True(slice.IsEnabled);
        Assert.Equal("", slice.Label);
        Assert.Equal("", slice.ItemId);
        Assert.Equal(Entity.Null, slice.Submenu);
    }

    [Fact]
    public void UIRadialSlice_AllProperties_CanBeModified()
    {
        var radialMenu = new Entity(1001, 1);
        var submenu = new Entity(1002, 1);
        var slice = new UIRadialSlice(radialMenu, 5)
        {
            StartAngle = 0f,
            EndAngle = MathF.PI / 4,
            HasSubmenu = true,
            IsEnabled = false,
            Label = "Weapons",
            ItemId = "weapons-menu",
            Submenu = submenu
        };

        Assert.Equal(radialMenu, slice.RadialMenu);
        Assert.Equal(5, slice.Index);
        Assert.Equal(0f, slice.StartAngle);
        Assert.Equal(MathF.PI / 4, slice.EndAngle);
        Assert.True(slice.HasSubmenu);
        Assert.False(slice.IsEnabled);
        Assert.Equal("Weapons", slice.Label);
        Assert.Equal("weapons-menu", slice.ItemId);
        Assert.Equal(submenu, slice.Submenu);
    }

    #endregion

    #region UISplitter Tests

    [Fact]
    public void UISplitter_Constructor_SetsProperties()
    {
        var splitter = new UISplitter(LayoutDirection.Horizontal);

        Assert.Equal(LayoutDirection.Horizontal, splitter.Orientation);
        Assert.Equal(0.5f, splitter.SplitRatio);
        Assert.Equal(50f, splitter.MinFirstPane);
        Assert.Equal(50f, splitter.MinSecondPane);
        Assert.Equal(4f, splitter.HandleSize);
    }

    [Fact]
    public void UISplitter_ConstructorWithRatio_SetsRatio()
    {
        var splitter = new UISplitter(LayoutDirection.Vertical, 0.3f);

        Assert.Equal(LayoutDirection.Vertical, splitter.Orientation);
        Assert.Equal(0.3f, splitter.SplitRatio);
        Assert.Equal(50f, splitter.MinFirstPane);
        Assert.Equal(50f, splitter.MinSecondPane);
        Assert.Equal(4f, splitter.HandleSize);
    }

    [Fact]
    public void UISplitter_AllProperties_CanBeModified()
    {
        var splitter = new UISplitter(LayoutDirection.Horizontal, 0.6f)
        {
            MinFirstPane = 100f,
            MinSecondPane = 150f,
            HandleSize = 8f
        };

        Assert.Equal(LayoutDirection.Horizontal, splitter.Orientation);
        Assert.Equal(0.6f, splitter.SplitRatio);
        Assert.Equal(100f, splitter.MinFirstPane);
        Assert.Equal(150f, splitter.MinSecondPane);
        Assert.Equal(8f, splitter.HandleSize);
    }

    [Fact]
    public void UISplitterFirstPane_Constructor_SetsSplitterContainer()
    {
        var splitterContainer = new Entity(1100, 1);
        var firstPane = new UISplitterFirstPane(splitterContainer);

        Assert.Equal(splitterContainer, firstPane.SplitterContainer);
    }

    [Fact]
    public void UISplitterSecondPane_Constructor_SetsSplitterContainer()
    {
        var splitterContainer = new Entity(1200, 1);
        var secondPane = new UISplitterSecondPane(splitterContainer);

        Assert.Equal(splitterContainer, secondPane.SplitterContainer);
    }

    [Fact]
    public void UISplitterFirstPane_WithDifferentEntities_StoresCorrectly()
    {
        var container1 = new Entity(1101, 1);
        var container2 = new Entity(1102, 1);
        var pane1 = new UISplitterFirstPane(container1);
        var pane2 = new UISplitterFirstPane(container2);

        Assert.Equal(container1, pane1.SplitterContainer);
        Assert.Equal(container2, pane2.SplitterContainer);
    }

    [Fact]
    public void UISplitterSecondPane_WithDifferentEntities_StoresCorrectly()
    {
        var container1 = new Entity(1201, 1);
        var container2 = new Entity(1202, 1);
        var pane1 = new UISplitterSecondPane(container1);
        var pane2 = new UISplitterSecondPane(container2);

        Assert.Equal(container1, pane1.SplitterContainer);
        Assert.Equal(container2, pane2.SplitterContainer);
    }

    #endregion

    #region UITextInput Event Tests

    [Fact]
    public void UITextChangedEvent_Constructor_SetsAllProperties()
    {
        var element = new Entity(1300, 1);
        var evt = new UITextChangedEvent(element, "old text", "new text");

        Assert.Equal(element, evt.Element);
        Assert.Equal("old text", evt.OldText);
        Assert.Equal("new text", evt.NewText);
    }

    [Fact]
    public void UITextChangedEvent_WithEmptyStrings_AllowsEmpty()
    {
        var element = new Entity(1301, 1);
        var evt = new UITextChangedEvent(element, "", "");

        Assert.Equal(element, evt.Element);
        Assert.Equal("", evt.OldText);
        Assert.Equal("", evt.NewText);
    }

    [Fact]
    public void UITextChangedEvent_WithDifferentText_StoresCorrectly()
    {
        var element = new Entity(1302, 1);
        var evt1 = new UITextChangedEvent(element, "Hello", "World");
        var evt2 = new UITextChangedEvent(element, "A", "B");

        Assert.Equal("Hello", evt1.OldText);
        Assert.Equal("World", evt1.NewText);
        Assert.Equal("A", evt2.OldText);
        Assert.Equal("B", evt2.NewText);
    }

    #endregion

    #region UIToolbar Tests

    [Fact]
    public void UIToolbar_Constructor_SetsOrientation()
    {
        var toolbar = new UIToolbar(LayoutDirection.Horizontal);

        Assert.Equal(LayoutDirection.Horizontal, toolbar.Orientation);
        Assert.Equal(0, toolbar.ButtonCount);
    }

    [Fact]
    public void UIToolbar_ConstructorWithVertical_SetsVertical()
    {
        var toolbar = new UIToolbar(LayoutDirection.Vertical);

        Assert.Equal(LayoutDirection.Vertical, toolbar.Orientation);
        Assert.Equal(0, toolbar.ButtonCount);
    }

    [Fact]
    public void UIToolbar_ButtonCount_CanBeModified()
    {
        var toolbar = new UIToolbar(LayoutDirection.Horizontal)
        {
            ButtonCount = 5
        };

        Assert.Equal(5, toolbar.ButtonCount);
    }

    [Fact]
    public void UIToolbarButton_Constructor_SetsToolbar()
    {
        var toolbar = new Entity(1400, 1);
        var button = new UIToolbarButton(toolbar);

        Assert.Equal(toolbar, button.Toolbar);
        Assert.False(button.IsToggle);
        Assert.False(button.IsPressed);
        Assert.Equal(string.Empty, button.TooltipText);
        Assert.Equal(0, button.Index);
    }

    [Fact]
    public void UIToolbarButton_AllProperties_CanBeModified()
    {
        var toolbar = new Entity(1401, 1);
        var button = new UIToolbarButton(toolbar)
        {
            IsToggle = true,
            IsPressed = true,
            TooltipText = "Save File",
            Index = 3
        };

        Assert.Equal(toolbar, button.Toolbar);
        Assert.True(button.IsToggle);
        Assert.True(button.IsPressed);
        Assert.Equal("Save File", button.TooltipText);
        Assert.Equal(3, button.Index);
    }

    [Fact]
    public void UIToolbarButton_WithEmptyTooltip_AllowsEmpty()
    {
        var toolbar = new Entity(1402, 1);
        var button = new UIToolbarButton(toolbar)
        {
            TooltipText = ""
        };

        Assert.Equal("", button.TooltipText);
    }

    [Fact]
    public void UIToolbarSeparator_Constructor_SetsToolbar()
    {
        var toolbar = new Entity(1500, 1);
        var separator = new UIToolbarSeparator(toolbar);

        Assert.Equal(toolbar, separator.Toolbar);
        Assert.Equal(0, separator.Index);
    }

    [Fact]
    public void UIToolbarSeparator_Index_CanBeModified()
    {
        var toolbar = new Entity(1501, 1);
        var separator = new UIToolbarSeparator(toolbar)
        {
            Index = 7
        };

        Assert.Equal(toolbar, separator.Toolbar);
        Assert.Equal(7, separator.Index);
    }

    [Fact]
    public void UIToolbarSeparator_WithDifferentToolbars_StoresCorrectly()
    {
        var toolbar1 = new Entity(1502, 1);
        var toolbar2 = new Entity(1503, 1);
        var sep1 = new UIToolbarSeparator(toolbar1);
        var sep2 = new UIToolbarSeparator(toolbar2);

        Assert.Equal(toolbar1, sep1.Toolbar);
        Assert.Equal(toolbar2, sep2.Toolbar);
    }

    #endregion

    #region PropertyType Enum Tests

    [Fact]
    public void PropertyType_AllValues_HaveCorrectNumericValues()
    {
        Assert.Equal(0, (byte)PropertyType.String);
        Assert.Equal(1, (byte)PropertyType.Int);
        Assert.Equal(2, (byte)PropertyType.Float);
        Assert.Equal(3, (byte)PropertyType.Bool);
        Assert.Equal(4, (byte)PropertyType.Color);
        Assert.Equal(5, (byte)PropertyType.Vector2);
        Assert.Equal(6, (byte)PropertyType.Vector3);
        Assert.Equal(7, (byte)PropertyType.Vector4);
        Assert.Equal(8, (byte)PropertyType.Enum);
        Assert.Equal(9, (byte)PropertyType.MultiLineString);
        Assert.Equal(10, (byte)PropertyType.Custom);
    }

    #endregion
}
