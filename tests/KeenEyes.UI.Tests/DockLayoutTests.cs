using System.Numerics;
using KeenEyes.UI.Abstractions;
using KeenEyes.UI.Docking;

namespace KeenEyes.UI.Tests;

/// <summary>
/// Tests for DockLayout serialization and layout management.
/// </summary>
public sealed class DockLayoutTests
{
    [Fact]
    public void CaptureLayout_WithNoDockContainer_ReturnsEmptyLayout()
    {
        using var world = new World();
        var entity = world.Spawn().Build();

        var layout = DockLayout.CaptureLayout(world, entity);

        Assert.NotNull(layout);
        Assert.Empty(layout.Zones);
        Assert.Empty(layout.FloatingPanels);
    }

    [Fact]
    public void CaptureLayout_WithEmptyContainer_ReturnsLayoutWithEmptyZones()
    {
        using var world = new World();
        world.InstallPlugin(new UIPlugin());

        var container = CreateDockContainer(world);

        var layout = DockLayout.CaptureLayout(world, container);

        Assert.NotNull(layout);
        Assert.Equal(1, layout.Version);
    }

    [Fact]
    public void CaptureLayout_WithDockedPanels_CapturesZoneData()
    {
        using var world = new World();
        world.InstallPlugin(new UIPlugin());

        var container = CreateDockContainer(world);
        ref readonly var dockContainer = ref world.Get<UIDockContainer>(container);

        // Create a panel docked to the left zone
        var panel = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIRect())
            .With(new UIDockPanel
            {
                Title = "TestPanel",
                State = DockState.Docked,
                CurrentZone = DockZone.Left,
                DockContainer = container,
                FloatingPosition = new Vector2(100, 100),
                FloatingSize = new Vector2(300, 400)
            })
            .Build();

        var layout = DockLayout.CaptureLayout(world, container);

        Assert.NotNull(layout);
        Assert.True(layout.Zones.ContainsKey(DockZone.Left));
        var leftZone = layout.Zones[DockZone.Left];
        Assert.Single(leftZone.Panels);
        Assert.Equal("TestPanel", leftZone.Panels[0].PanelId);
    }

    [Fact]
    public void CaptureLayout_WithFloatingPanels_CapturesFloatingData()
    {
        using var world = new World();
        world.InstallPlugin(new UIPlugin());

        var container = CreateDockContainer(world);

        // Create a floating panel
        var panel = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIRect())
            .With(new UIDockPanel
            {
                Title = "FloatingPanel",
                State = DockState.Floating,
                CurrentZone = DockZone.None,
                DockContainer = container,
                FloatingPosition = new Vector2(200, 150),
                FloatingSize = new Vector2(400, 300)
            })
            .Build();

        var layout = DockLayout.CaptureLayout(world, container);

        Assert.NotNull(layout);
        Assert.Single(layout.FloatingPanels);
        var floatingPanel = layout.FloatingPanels[0];
        Assert.Equal("FloatingPanel", floatingPanel.PanelId);
        Assert.Equal(200, floatingPanel.PositionX);
        Assert.Equal(150, floatingPanel.PositionY);
        Assert.Equal(400, floatingPanel.SizeX);
        Assert.Equal(300, floatingPanel.SizeY);
    }

    [Fact]
    public void CaptureLayout_WithCustomPanelIdResolver_UsesCustomIds()
    {
        using var world = new World();
        world.InstallPlugin(new UIPlugin());

        var container = CreateDockContainer(world);

        var panel = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIRect())
            .With(new UIDockPanel
            {
                Title = "TestPanel",
                State = DockState.Docked,
                CurrentZone = DockZone.Left,
                DockContainer = container
            })
            .Build();

        var layout = DockLayout.CaptureLayout(world, container, entity => $"custom_{entity.Id}");

        Assert.NotNull(layout);
        Assert.True(layout.Zones.ContainsKey(DockZone.Left));
        var leftZone = layout.Zones[DockZone.Left];
        Assert.Single(leftZone.Panels);
        Assert.StartsWith("custom_", leftZone.Panels[0].PanelId);
    }

    [Fact]
    public void ApplyLayout_WithNoDockContainer_DoesNotThrow()
    {
        using var world = new World();
        var entity = world.Spawn().Build();
        var layout = new DockLayout();

        var exception = Record.Exception(() =>
            DockLayout.ApplyLayout(world, entity, layout, _ => Entity.Null));

        Assert.Null(exception);
    }

    [Fact]
    public void ApplyLayout_WithValidLayout_RestoresPanelStates()
    {
        using var world = new World();
        world.InstallPlugin(new UIPlugin());

        var container = CreateDockContainer(world);

        // Create a panel
        var panel = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIRect())
            .With(new UIDockPanel
            {
                Title = "TestPanel",
                State = DockState.Floating,
                CurrentZone = DockZone.None,
                DockContainer = container
            })
            .Build();

        // Create a layout that docks the panel to the left zone
        var layout = new DockLayout
        {
            Zones = new Dictionary<DockZone, DockZoneLayout>
            {
                [DockZone.Left] = new DockZoneLayout
                {
                    Size = 250f,
                    IsCollapsed = false,
                    SelectedTabIndex = 0,
                    Panels =
                    [
                        new DockPanelLayout
                        {
                            PanelId = "TestPanel",
                            PositionX = 100,
                            PositionY = 100,
                            SizeX = 300,
                            SizeY = 400
                        }
                    ]
                }
            }
        };

        DockLayout.ApplyLayout(world, container, layout, id =>
        {
            if (id == "TestPanel")
            {
                return panel;
            }
            return Entity.Null;
        });

        ref readonly var updatedPanel = ref world.Get<UIDockPanel>(panel);
        Assert.Equal(DockState.Docked, updatedPanel.State);
        Assert.Equal(DockZone.Left, updatedPanel.CurrentZone);
        Assert.Equal(new Vector2(100, 100), updatedPanel.FloatingPosition);
        Assert.Equal(new Vector2(300, 400), updatedPanel.FloatingSize);
    }

    [Fact]
    public void ApplyLayout_WithFloatingPanels_RestoresFloatingState()
    {
        using var world = new World();
        world.InstallPlugin(new UIPlugin());

        var container = CreateDockContainer(world);

        // Create a panel
        var panel = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIRect())
            .With(new UIDockPanel
            {
                Title = "FloatingPanel",
                State = DockState.Docked,
                CurrentZone = DockZone.Left,
                DockContainer = container
            })
            .Build();

        // Create a layout with a floating panel
        var layout = new DockLayout
        {
            FloatingPanels =
            [
                new DockPanelLayout
                {
                    PanelId = "FloatingPanel",
                    PositionX = 500,
                    PositionY = 300,
                    SizeX = 350,
                    SizeY = 450
                }
            ]
        };

        DockLayout.ApplyLayout(world, container, layout, id =>
        {
            if (id == "FloatingPanel")
            {
                return panel;
            }
            return Entity.Null;
        });

        ref readonly var updatedPanel = ref world.Get<UIDockPanel>(panel);
        Assert.Equal(DockState.Floating, updatedPanel.State);
        Assert.Equal(DockZone.None, updatedPanel.CurrentZone);
        Assert.Equal(new Vector2(500, 300), updatedPanel.FloatingPosition);
        Assert.Equal(new Vector2(350, 450), updatedPanel.FloatingSize);
    }

    [Fact]
    public void ApplyLayout_WithInvalidPanelResolver_SkipsMissingPanels()
    {
        using var world = new World();
        world.InstallPlugin(new UIPlugin());

        var container = CreateDockContainer(world);

        var layout = new DockLayout
        {
            Zones = new Dictionary<DockZone, DockZoneLayout>
            {
                [DockZone.Left] = new DockZoneLayout
                {
                    Panels =
                    [
                        new DockPanelLayout { PanelId = "NonExistent" }
                    ]
                }
            }
        };

        // Should not throw even though panel doesn't exist
        var exception = Record.Exception(() =>
            DockLayout.ApplyLayout(world, container, layout, _ => Entity.Null));

        Assert.Null(exception);
    }

    [Fact]
    public void ToJson_SerializesLayout()
    {
        var layout = new DockLayout
        {
            Version = 1,
            Zones = new Dictionary<DockZone, DockZoneLayout>
            {
                [DockZone.Left] = new DockZoneLayout
                {
                    Size = 200f,
                    IsCollapsed = false,
                    SelectedTabIndex = 0,
                    Panels =
                    [
                        new DockPanelLayout
                        {
                            PanelId = "Panel1",
                            PositionX = 10,
                            PositionY = 20,
                            SizeX = 300,
                            SizeY = 400
                        }
                    ]
                }
            },
            FloatingPanels =
            [
                new DockPanelLayout
                {
                    PanelId = "FloatingPanel",
                    PositionX = 100,
                    PositionY = 150,
                    SizeX = 250,
                    SizeY = 350
                }
            ]
        };

        var json = layout.ToJson();

        Assert.NotNull(json);
        Assert.Contains("\"version\"", json);
        Assert.Contains("\"zones\"", json);
        Assert.Contains("\"floatingPanels\"", json);
        Assert.Contains("Panel1", json);
        Assert.Contains("FloatingPanel", json);
    }

    [Fact]
    public void FromJson_DeserializesLayout()
    {
        var json = """
        {
          "version": 1,
          "zones": {
            "left": {
              "size": 200,
              "isCollapsed": false,
              "selectedTabIndex": 0,
              "panels": [
                {
                  "panelId": "Panel1",
                  "positionX": 10,
                  "positionY": 20,
                  "sizeX": 300,
                  "sizeY": 400
                }
              ]
            }
          },
          "floatingPanels": [
            {
              "panelId": "FloatingPanel",
              "positionX": 100,
              "positionY": 150,
              "sizeX": 250,
              "sizeY": 350
            }
          ]
        }
        """;

        var layout = DockLayout.FromJson(json);

        Assert.NotNull(layout);
        Assert.Equal(1, layout.Version);
        Assert.Single(layout.Zones);
        Assert.True(layout.Zones.ContainsKey(DockZone.Left));
        Assert.Single(layout.FloatingPanels);
        Assert.Equal("FloatingPanel", layout.FloatingPanels[0].PanelId);
    }

    [Fact]
    public void FromJson_WithInvalidJson_ReturnsEmptyLayout()
    {
        var invalidJson = "{ invalid json }";

        var layout = DockLayout.FromJson(invalidJson);

        Assert.NotNull(layout);
        Assert.Empty(layout.Zones);
        Assert.Empty(layout.FloatingPanels);
        Assert.Equal(1, layout.Version);
    }

    [Fact]
    public void FromJson_WithEmptyString_ReturnsEmptyLayout()
    {
        var layout = DockLayout.FromJson("");

        Assert.NotNull(layout);
        Assert.Empty(layout.Zones);
        Assert.Empty(layout.FloatingPanels);
    }

    [Fact]
    public void RoundTrip_PreservesLayoutData()
    {
        using var world = new World();
        world.InstallPlugin(new UIPlugin());

        var container = CreateDockContainer(world);

        // Create panels in different states
        var dockedPanel = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIRect())
            .With(new UIDockPanel
            {
                Title = "DockedPanel",
                State = DockState.Docked,
                CurrentZone = DockZone.Right,
                DockContainer = container,
                FloatingPosition = new Vector2(50, 75),
                FloatingSize = new Vector2(200, 300)
            })
            .Build();

        var floatingPanel = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIRect())
            .With(new UIDockPanel
            {
                Title = "FloatingPanel",
                State = DockState.Floating,
                CurrentZone = DockZone.None,
                DockContainer = container,
                FloatingPosition = new Vector2(400, 300),
                FloatingSize = new Vector2(500, 400)
            })
            .Build();

        // Capture layout
        var originalLayout = DockLayout.CaptureLayout(world, container);

        // Serialize and deserialize
        var json = originalLayout.ToJson();
        var restoredLayout = DockLayout.FromJson(json);

        // Verify data is preserved
        Assert.Equal(originalLayout.Version, restoredLayout.Version);
        Assert.Equal(originalLayout.FloatingPanels.Count, restoredLayout.FloatingPanels.Count);

        if (restoredLayout.FloatingPanels.Count > 0)
        {
            Assert.Equal("FloatingPanel", restoredLayout.FloatingPanels[0].PanelId);
            Assert.Equal(400, restoredLayout.FloatingPanels[0].PositionX);
            Assert.Equal(300, restoredLayout.FloatingPanels[0].PositionY);
        }
    }

    [Fact]
    public void CaptureLayout_WithMultiplePanelsInZone_CapturesAll()
    {
        using var world = new World();
        world.InstallPlugin(new UIPlugin());

        var container = CreateDockContainer(world);

        // Create multiple panels in the same zone
        for (int i = 0; i < 3; i++)
        {
            world.Spawn()
                .With(new UIElement { Visible = true })
                .With(new UIRect())
                .With(new UIDockPanel
                {
                    Title = $"Panel{i}",
                    State = DockState.Docked,
                    CurrentZone = DockZone.Center,
                    DockContainer = container
                })
                .Build();
        }

        var layout = DockLayout.CaptureLayout(world, container);

        Assert.NotNull(layout);
        Assert.True(layout.Zones.ContainsKey(DockZone.Center));
        Assert.Equal(3, layout.Zones[DockZone.Center].Panels.Count);
    }

    [Fact]
    public void ApplyLayout_UpdatesZoneSize()
    {
        using var world = new World();
        world.InstallPlugin(new UIPlugin());

        var container = CreateDockContainer(world);
        ref readonly var dockContainer = ref world.Get<UIDockContainer>(container);

        var layout = new DockLayout
        {
            Zones = new Dictionary<DockZone, DockZoneLayout>
            {
                [DockZone.Left] = new DockZoneLayout
                {
                    Size = 350f,
                    IsCollapsed = true,
                    SelectedTabIndex = 2
                }
            }
        };

        DockLayout.ApplyLayout(world, container, layout, _ => Entity.Null);

        if (dockContainer.LeftZone.IsValid && world.Has<UIDockZone>(dockContainer.LeftZone))
        {
            ref readonly var leftZone = ref world.Get<UIDockZone>(dockContainer.LeftZone);
            Assert.Equal(350f, leftZone.Size);
            Assert.True(leftZone.IsCollapsed);
        }
    }

    [Fact]
    public void DockZoneLayout_DefaultValues()
    {
        var zoneLayout = new DockZoneLayout();

        Assert.Equal(200f, zoneLayout.Size);
        Assert.False(zoneLayout.IsCollapsed);
        Assert.Equal(0, zoneLayout.SelectedTabIndex);
        Assert.NotNull(zoneLayout.Panels);
        Assert.Empty(zoneLayout.Panels);
    }

    [Fact]
    public void DockPanelLayout_DefaultValues()
    {
        var panelLayout = new DockPanelLayout();

        Assert.Equal(string.Empty, panelLayout.PanelId);
        Assert.Equal(0f, panelLayout.PositionX);
        Assert.Equal(0f, panelLayout.PositionY);
        Assert.Equal(0f, panelLayout.SizeX);
        Assert.Equal(0f, panelLayout.SizeY);
    }

    private static Entity CreateDockContainer(World world)
    {
        var leftZone = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIRect())
            .With(new UIDockZone { Zone = DockZone.Left, Size = 200f })
            .Build();

        var rightZone = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIRect())
            .With(new UIDockZone { Zone = DockZone.Right, Size = 200f })
            .Build();

        var topZone = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIRect())
            .With(new UIDockZone { Zone = DockZone.Top, Size = 100f })
            .Build();

        var bottomZone = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIRect())
            .With(new UIDockZone { Zone = DockZone.Bottom, Size = 100f })
            .Build();

        var centerZone = world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIRect())
            .With(new UIDockZone { Zone = DockZone.Center })
            .Build();

        return world.Spawn()
            .With(new UIElement { Visible = true })
            .With(new UIRect())
            .With(new UIDockContainer
            {
                LeftZone = leftZone,
                RightZone = rightZone,
                TopZone = topZone,
                BottomZone = bottomZone,
                CenterZone = centerZone
            })
            .Build();
    }
}
