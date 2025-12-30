using System.Numerics;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.UI.Abstractions;
using KeenEyes.UI.Widgets;

namespace KeenEyes.UI.Tests;

/// <summary>
/// Tests for WidgetFactory DataGrid and Dock widget creation methods.
/// </summary>
public class WidgetFactoryDataGridDockTests
{
    private static readonly FontHandle testFont = new(1);

    #region DataGrid Tests

    [Fact]
    public void CreateDataGrid_WithConfig_HasRequiredComponents()
    {
        using var world = new World();
        var config = new DataGridConfig(
            Columns: [new DataGridColumnDef("Name", 100), new DataGridColumnDef("Value", 100)]);

        var grid = WidgetFactory.CreateDataGrid(world, config);

        Assert.True(world.Has<UIElement>(grid));
        Assert.True(world.Has<UIRect>(grid));
        Assert.True(world.Has<UIStyle>(grid));
        Assert.True(world.Has<UILayout>(grid));
        Assert.True(world.Has<UIDataGrid>(grid));
    }

    [Fact]
    public void CreateDataGrid_WithHeaders_CreatesSimpleGrid()
    {
        using var world = new World();

        var grid = WidgetFactory.CreateDataGrid(world, "Column1", "Column2", "Column3");

        Assert.True(world.Has<UIDataGrid>(grid));
        ref readonly var gridData = ref world.Get<UIDataGrid>(grid);
        Assert.NotEqual(Entity.Null, gridData.HeaderRow);
    }

    [Fact]
    public void CreateDataGrid_HasHeaderRow()
    {
        using var world = new World();
        var config = new DataGridConfig(
            Columns: [new DataGridColumnDef("Name", 100), new DataGridColumnDef("Value", 100)]);

        var grid = WidgetFactory.CreateDataGrid(world, config);

        ref readonly var gridData = ref world.Get<UIDataGrid>(grid);
        Assert.True(world.IsAlive(gridData.HeaderRow));
    }

    [Fact]
    public void CreateDataGrid_HasBodyContainer()
    {
        using var world = new World();
        var config = new DataGridConfig(
            Columns: [new DataGridColumnDef("Name", 100)]);

        var grid = WidgetFactory.CreateDataGrid(world, config);

        ref readonly var gridData = ref world.Get<UIDataGrid>(grid);
        Assert.True(world.IsAlive(gridData.BodyContainer));
    }

    [Fact]
    public void CreateDataGrid_AppliesConfig()
    {
        using var world = new World();
        var config = new DataGridConfig(
            Columns: [new DataGridColumnDef("Name", 100)],
            Size: new Vector2(500, 400),
            RowHeight: 30f,
            HeaderHeight: 40f,
            AlternatingRowColors: true);

        var grid = WidgetFactory.CreateDataGrid(world, config);

        ref readonly var rect = ref world.Get<UIRect>(grid);
        Assert.Equal(new Vector2(500, 400), rect.Size);

        ref readonly var gridData = ref world.Get<UIDataGrid>(grid);
        Assert.Equal(30f, gridData.RowHeight);
        Assert.Equal(40f, gridData.HeaderHeight);
        Assert.True(gridData.AlternatingRowColors);
    }

    [Fact]
    public void CreateDataGrid_CreatesColumns()
    {
        using var world = new World();
        var config = new DataGridConfig(
            Columns:
            [
                new DataGridColumnDef("Name", 150),
                new DataGridColumnDef("Age", 80),
                new DataGridColumnDef("City", 120)
            ]);

        var grid = WidgetFactory.CreateDataGrid(world, config);

        // Verify columns were created
        var columnCount = 0;
        foreach (var entity in world.Query<UIDataGridColumn>())
        {
            ref readonly var col = ref world.Get<UIDataGridColumn>(entity);
            if (col.DataGrid == grid)
            {
                columnCount++;
            }
        }
        Assert.Equal(3, columnCount);
    }

    [Fact]
    public void CreateDataGrid_HasClipChildrenTag()
    {
        using var world = new World();
        var config = new DataGridConfig(
            Columns: [new DataGridColumnDef("Name", 100)]);

        var grid = WidgetFactory.CreateDataGrid(world, config);

        Assert.True(world.Has<UIClipChildrenTag>(grid));
    }

    [Fact]
    public void AddDataGridRow_AddsRowToGrid()
    {
        using var world = new World();
        var config = new DataGridConfig(
            Columns:
            [
                new DataGridColumnDef("Name", 100),
                new DataGridColumnDef("Value", 100)
            ]);
        var grid = WidgetFactory.CreateDataGrid(world, config);

        var row = WidgetFactory.AddDataGridRow(world, grid, ["Item1", "100"]);

        Assert.True(world.IsAlive(row));
        Assert.True(world.Has<UIDataGridRow>(row));
    }

    [Fact]
    public void AddDataGridRow_IncrementsTotalRowCount()
    {
        using var world = new World();
        var config = new DataGridConfig(
            Columns: [new DataGridColumnDef("Name", 100)]);
        var grid = WidgetFactory.CreateDataGrid(world, config);

        WidgetFactory.AddDataGridRow(world, grid, ["Item1"]);
        WidgetFactory.AddDataGridRow(world, grid, ["Item2"]);
        WidgetFactory.AddDataGridRow(world, grid, ["Item3"]);

        ref readonly var gridData = ref world.Get<UIDataGrid>(grid);
        Assert.Equal(3, gridData.TotalRowCount);
    }

    [Fact]
    public void AddDataGridRow_WithRowData_StoresData()
    {
        using var world = new World();
        var config = new DataGridConfig(
            Columns: [new DataGridColumnDef("Name", 100)]);
        var grid = WidgetFactory.CreateDataGrid(world, config);
        var userData = new { Id = 1, Name = "Test" };

        var row = WidgetFactory.AddDataGridRow(world, grid, ["Item1"], userData);

        ref readonly var rowData = ref world.Get<UIDataGridRow>(row);
        Assert.Equal(userData, rowData.RowData);
    }

    [Fact]
    public void AddDataGridRow_WithInvalidGrid_ReturnsNull()
    {
        using var world = new World();

        var result = WidgetFactory.AddDataGridRow(world, Entity.Null, ["Item"]);

        Assert.Equal(Entity.Null, result);
    }

    [Fact]
    public void ClearDataGridRows_RemovesAllRows()
    {
        using var world = new World();
        var config = new DataGridConfig(
            Columns: [new DataGridColumnDef("Name", 100)]);
        var grid = WidgetFactory.CreateDataGrid(world, config);
        WidgetFactory.AddDataGridRow(world, grid, ["Item1"]);
        WidgetFactory.AddDataGridRow(world, grid, ["Item2"]);

        WidgetFactory.ClearDataGridRows(world, grid);

        ref readonly var gridData = ref world.Get<UIDataGrid>(grid);
        Assert.Equal(0, gridData.TotalRowCount);
    }

    [Fact]
    public void ClearDataGridRows_WithInvalidGrid_DoesNotThrow()
    {
        using var world = new World();

        // Should not throw
        WidgetFactory.ClearDataGridRows(world, Entity.Null);
    }

    [Fact]
    public void SetDataGridCellValue_UpdatesCell()
    {
        using var world = new World();
        var config = new DataGridConfig(
            Columns: [new DataGridColumnDef("Name", 100)]);
        var grid = WidgetFactory.CreateDataGrid(world, config);
        WidgetFactory.AddDataGridRow(world, grid, ["Original"]);

        WidgetFactory.SetDataGridCellValue(world, grid, 0, 0, "Updated");

        // Verify cell was updated
        foreach (var cellEntity in world.Query<UIDataGridCell>())
        {
            ref readonly var cell = ref world.Get<UIDataGridCell>(cellEntity);
            if (cell.DataGrid == grid && cell.RowIndex == 0 && cell.ColumnIndex == 0)
            {
                Assert.Equal("Updated", cell.Value);
                break;
            }
        }
    }

    #endregion

    #region DockContainer Tests

    [Fact]
    public void CreateDockContainer_HasRequiredComponents()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var container = WidgetFactory.CreateDockContainer(world, parent);

        Assert.True(world.Has<UIElement>(container));
        Assert.True(world.Has<UIRect>(container));
        Assert.True(world.Has<UILayout>(container));
        Assert.True(world.Has<UIDockContainer>(container));
    }

    [Fact]
    public void CreateDockContainer_CreatesDockZones()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var container = WidgetFactory.CreateDockContainer(world, parent);

        ref readonly var dockData = ref world.Get<UIDockContainer>(container);
        Assert.True(world.IsAlive(dockData.LeftZone));
        Assert.True(world.IsAlive(dockData.RightZone));
        Assert.True(world.IsAlive(dockData.TopZone));
        Assert.True(world.IsAlive(dockData.BottomZone));
        Assert.True(world.IsAlive(dockData.CenterZone));
    }

    [Fact]
    public void CreateDockContainer_CreatesPreviewOverlay()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var container = WidgetFactory.CreateDockContainer(world, parent);

        ref readonly var dockData = ref world.Get<UIDockContainer>(container);
        Assert.True(world.IsAlive(dockData.PreviewOverlay));
        Assert.True(world.Has<UIDockPreviewTag>(dockData.PreviewOverlay));
    }

    [Fact]
    public void CreateDockContainer_AppliesConfig()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new DockContainerConfig
        {
            LeftZoneSize = 250f,
            RightZoneSize = 200f,
            ShowLeftZone = true,
            ShowRightZone = false
        };

        var container = WidgetFactory.CreateDockContainer(world, parent, config);

        ref readonly var dockData = ref world.Get<UIDockContainer>(container);
        ref readonly var leftZone = ref world.Get<UIDockZone>(dockData.LeftZone);
        Assert.Equal(250f, leftZone.Size);
    }

    [Fact]
    public void CreateDockContainer_WithName_SetsEntityName()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var container = WidgetFactory.CreateDockContainer(world, parent, "MyDockContainer");

        Assert.Equal("MyDockContainer", world.GetName(container));
    }

    [Fact]
    public void CreateDockContainer_ZonesHaveTabGroups()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var container = WidgetFactory.CreateDockContainer(world, parent);

        ref readonly var dockData = ref world.Get<UIDockContainer>(container);
        ref readonly var leftZone = ref world.Get<UIDockZone>(dockData.LeftZone);
        Assert.True(world.IsAlive(leftZone.TabGroup));
        Assert.True(world.Has<UIDockTabGroup>(leftZone.TabGroup));
    }

    #endregion

    #region DockPanel Tests

    [Fact]
    public void CreateDockPanel_ReturnsPanel_AndContentPanel()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var (panel, contentPanel) = WidgetFactory.CreateDockPanel(world, parent, "Test Panel", testFont);

        Assert.True(world.IsAlive(panel));
        Assert.True(world.IsAlive(contentPanel));
    }

    [Fact]
    public void CreateDockPanel_PanelHasRequiredComponents()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var (panel, _) = WidgetFactory.CreateDockPanel(world, parent, "Test Panel", testFont);

        Assert.True(world.Has<UIElement>(panel));
        Assert.True(world.Has<UIRect>(panel));
        Assert.True(world.Has<UIStyle>(panel));
        Assert.True(world.Has<UILayout>(panel));
        Assert.True(world.Has<UIDockPanel>(panel));
    }

    [Fact]
    public void CreateDockPanel_HasTitleBar()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var (panel, _) = WidgetFactory.CreateDockPanel(world, parent, "Test Panel", testFont);

        var children = world.GetChildren(panel).ToList();
        Assert.True(children.Count >= 2); // Title bar and content
    }

    [Fact]
    public void CreateDockPanel_AppliesConfig()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new DockPanelConfig
        {
            Width = 400,
            Height = 300,
            CanClose = true,
            CanFloat = true,
            CanDock = true
        };

        var (panel, _) = WidgetFactory.CreateDockPanel(world, parent, "Test Panel", testFont, config);

        ref readonly var rect = ref world.Get<UIRect>(panel);
        Assert.Equal(new Vector2(400, 300), rect.Size);

        ref readonly var panelData = ref world.Get<UIDockPanel>(panel);
        Assert.True(panelData.CanClose);
        Assert.True(panelData.CanFloat);
        Assert.True(panelData.CanDock);
    }

    [Fact]
    public void CreateDockPanel_WithName_SetsEntityName()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var (panel, _) = WidgetFactory.CreateDockPanel(world, parent, "PropertiesPanel", "Properties", testFont);

        Assert.Equal("PropertiesPanel", world.GetName(panel));
    }

    [Fact]
    public void CreateDockPanel_StartsFloating()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);

        var (panel, _) = WidgetFactory.CreateDockPanel(world, parent, "Test Panel", testFont);

        ref readonly var panelData = ref world.Get<UIDockPanel>(panel);
        Assert.Equal(DockState.Floating, panelData.State);
        Assert.Equal(DockZone.None, panelData.CurrentZone);
    }

    [Fact]
    public void CreateDockPanel_WithCanClose_HasCloseButton()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var config = new DockPanelConfig { CanClose = true };

        var (panel, _) = WidgetFactory.CreateDockPanel(world, parent, "Test Panel", testFont, config);

        // Find title bar and check for close button
        var children = world.GetChildren(panel).ToList();
        var titleBar = children[0];
        var titleBarChildren = world.GetChildren(titleBar).ToList();

        // Should have title label and close button
        Assert.True(titleBarChildren.Count >= 2);
    }

    [Fact]
    public void DockPanel_DocksToZone()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var container = WidgetFactory.CreateDockContainer(world, parent);
        var (panel, _) = WidgetFactory.CreateDockPanel(world, parent, "Test Panel", testFont);

        WidgetFactory.DockPanel(world, panel, container, DockZone.Left);

        ref readonly var panelData = ref world.Get<UIDockPanel>(panel);
        Assert.Equal(DockState.Docked, panelData.State);
        Assert.Equal(DockZone.Left, panelData.CurrentZone);
        Assert.Equal(container, panelData.DockContainer);
    }

    [Fact]
    public void DockPanel_WithInvalidPanel_DoesNothing()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var container = WidgetFactory.CreateDockContainer(world, parent);

        // Should not throw
        WidgetFactory.DockPanel(world, Entity.Null, container, DockZone.Left);
    }

    [Fact]
    public void DockPanel_WithInvalidContainer_DoesNothing()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var (panel, _) = WidgetFactory.CreateDockPanel(world, parent, "Test Panel", testFont);

        // Should not throw
        WidgetFactory.DockPanel(world, panel, Entity.Null, DockZone.Left);

        ref readonly var panelData = ref world.Get<UIDockPanel>(panel);
        Assert.Equal(DockState.Floating, panelData.State);
    }

    [Fact]
    public void UndockPanel_MakesPanelFloating()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var container = WidgetFactory.CreateDockContainer(world, parent);
        var (panel, _) = WidgetFactory.CreateDockPanel(world, parent, "Test Panel", testFont);
        WidgetFactory.DockPanel(world, panel, container, DockZone.Left);

        WidgetFactory.UndockPanel(world, panel, new Vector2(100, 100));

        ref readonly var panelData = ref world.Get<UIDockPanel>(panel);
        Assert.Equal(DockState.Floating, panelData.State);
        Assert.Equal(DockZone.None, panelData.CurrentZone);
        Assert.Equal(Entity.Null, panelData.DockContainer);
    }

    [Fact]
    public void UndockPanel_WhenAlreadyFloating_DoesNothing()
    {
        using var world = new World();
        var parent = CreateRootEntity(world);
        var (panel, _) = WidgetFactory.CreateDockPanel(world, parent, "Test Panel", testFont);

        // Should not throw
        WidgetFactory.UndockPanel(world, panel, new Vector2(100, 100));

        ref readonly var panelData = ref world.Get<UIDockPanel>(panel);
        Assert.Equal(DockState.Floating, panelData.State);
    }

    [Fact]
    public void UndockPanel_WithInvalidPanel_DoesNothing()
    {
        using var world = new World();

        // Should not throw
        WidgetFactory.UndockPanel(world, Entity.Null, new Vector2(100, 100));
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
