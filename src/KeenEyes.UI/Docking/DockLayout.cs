using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI.Docking;

/// <summary>
/// Serializable layout state for dock containers, enabling save/restore of layouts.
/// </summary>
/// <remarks>
/// <para>
/// Use <see cref="CaptureLayout"/> to create a snapshot of the current dock configuration,
/// and <see cref="ApplyLayout"/> to restore a previously saved configuration.
/// </para>
/// <para>
/// The layout can be serialized to JSON using <see cref="ToJson"/> and deserialized
/// using <see cref="FromJson"/> for persistent storage.
/// </para>
/// </remarks>
public sealed class DockLayout
{
    /// <summary>
    /// Layout information for each dock zone.
    /// </summary>
    public Dictionary<DockZone, DockZoneLayout> Zones { get; set; } = [];

    /// <summary>
    /// Layout information for floating panels.
    /// </summary>
    public List<DockPanelLayout> FloatingPanels { get; set; } = [];

    /// <summary>
    /// Version number for layout compatibility.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Captures the current layout state from a dock container.
    /// </summary>
    /// <param name="world">The world containing the dock entities.</param>
    /// <param name="container">The dock container entity to capture.</param>
    /// <param name="panelIdResolver">
    /// Optional function to get a panel ID from an entity. If not provided, uses the panel's Title.
    /// </param>
    /// <returns>A layout snapshot that can be serialized and restored.</returns>
    public static DockLayout CaptureLayout(
        IWorld world,
        Entity container,
        Func<Entity, string>? panelIdResolver = null)
    {
        var layout = new DockLayout();

        if (!world.Has<UIDockContainer>(container))
        {
            return layout;
        }

        panelIdResolver ??= (entity) =>
        {
            if (world.Has<UIDockPanel>(entity))
            {
                ref readonly var panel = ref world.Get<UIDockPanel>(entity);
                return panel.Title;
            }
            return $"entity_{entity.Id}";
        };

        ref readonly var dockContainer = ref world.Get<UIDockContainer>(container);

        // Capture each zone
        CaptureZone(world, dockContainer.LeftZone, DockZone.Left, layout, panelIdResolver);
        CaptureZone(world, dockContainer.RightZone, DockZone.Right, layout, panelIdResolver);
        CaptureZone(world, dockContainer.TopZone, DockZone.Top, layout, panelIdResolver);
        CaptureZone(world, dockContainer.BottomZone, DockZone.Bottom, layout, panelIdResolver);
        CaptureZone(world, dockContainer.CenterZone, DockZone.Center, layout, panelIdResolver);

        // Capture floating panels by querying all panels with DockState.Floating
        foreach (var entity in world.Query<UIDockPanel>())
        {
            ref readonly var panel = ref world.Get<UIDockPanel>(entity);
            if (panel.State == DockState.Floating && panel.DockContainer == container)
            {
                var panelLayout = new DockPanelLayout
                {
                    PanelId = panelIdResolver(entity),
                    PositionX = panel.FloatingPosition.X,
                    PositionY = panel.FloatingPosition.Y,
                    SizeX = panel.FloatingSize.X,
                    SizeY = panel.FloatingSize.Y
                };
                layout.FloatingPanels.Add(panelLayout);
            }
        }

        return layout;
    }

    /// <summary>
    /// Applies a previously captured layout to a dock container.
    /// </summary>
    /// <param name="world">The world containing the dock entities.</param>
    /// <param name="container">The dock container entity to apply the layout to.</param>
    /// <param name="layout">The layout to apply.</param>
    /// <param name="panelResolver">
    /// Function to resolve panel IDs to entities. Takes a panel ID string and returns the corresponding entity.
    /// </param>
    public static void ApplyLayout(
        IWorld world,
        Entity container,
        DockLayout layout,
        Func<string, Entity> panelResolver)
    {
        if (!world.Has<UIDockContainer>(container))
        {
            return;
        }

        ref readonly var dockContainer = ref world.Get<UIDockContainer>(container);

        // Apply zone layouts
        ApplyZoneLayout(world, dockContainer.LeftZone, DockZone.Left, layout, panelResolver);
        ApplyZoneLayout(world, dockContainer.RightZone, DockZone.Right, layout, panelResolver);
        ApplyZoneLayout(world, dockContainer.TopZone, DockZone.Top, layout, panelResolver);
        ApplyZoneLayout(world, dockContainer.BottomZone, DockZone.Bottom, layout, panelResolver);
        ApplyZoneLayout(world, dockContainer.CenterZone, DockZone.Center, layout, panelResolver);

        // Apply floating panel layouts
        foreach (var panelLayout in layout.FloatingPanels)
        {
            var panelEntity = panelResolver(panelLayout.PanelId);
            if (!panelEntity.IsValid || !world.Has<UIDockPanel>(panelEntity))
            {
                continue;
            }

            ref var panel = ref world.Get<UIDockPanel>(panelEntity);
            panel.State = DockState.Floating;
            panel.CurrentZone = DockZone.None;
            panel.FloatingPosition = new Vector2(panelLayout.PositionX, panelLayout.PositionY);
            panel.FloatingSize = new Vector2(panelLayout.SizeX, panelLayout.SizeY);
        }
    }

    /// <summary>
    /// Serializes the layout to a JSON string.
    /// </summary>
    /// <returns>The JSON representation of the layout.</returns>
    public string ToJson()
    {
        return JsonSerializer.Serialize(this, DockLayoutJsonContext.Default.DockLayout);
    }

    /// <summary>
    /// Deserializes a layout from a JSON string.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized layout, or an empty layout if deserialization fails.</returns>
    public static DockLayout FromJson(string json)
    {
        try
        {
            return JsonSerializer.Deserialize(json, DockLayoutJsonContext.Default.DockLayout)
                ?? new DockLayout();
        }
        catch (JsonException)
        {
            return new DockLayout();
        }
    }

    private static void CaptureZone(
        IWorld world,
        Entity zoneEntity,
        DockZone zone,
        DockLayout layout,
        Func<Entity, string> panelIdResolver)
    {
        if (!zoneEntity.IsValid || !world.Has<UIDockZone>(zoneEntity))
        {
            return;
        }

        ref readonly var dockZone = ref world.Get<UIDockZone>(zoneEntity);

        var zoneLayout = new DockZoneLayout
        {
            Size = dockZone.Size,
            IsCollapsed = dockZone.IsCollapsed,
            SelectedTabIndex = 0,
            Panels = []
        };

        // Get selected tab index from tab group
        if (dockZone.TabGroup.IsValid && world.Has<UIDockTabGroup>(dockZone.TabGroup))
        {
            ref readonly var tabGroup = ref world.Get<UIDockTabGroup>(dockZone.TabGroup);
            zoneLayout.SelectedTabIndex = tabGroup.SelectedIndex;
        }

        // Collect panels in this zone
        foreach (var entity in world.Query<UIDockPanel>())
        {
            ref readonly var panel = ref world.Get<UIDockPanel>(entity);
            if (panel.CurrentZone == zone && panel.State == DockState.Docked)
            {
                var panelLayout = new DockPanelLayout
                {
                    PanelId = panelIdResolver(entity),
                    PositionX = panel.FloatingPosition.X,
                    PositionY = panel.FloatingPosition.Y,
                    SizeX = panel.FloatingSize.X,
                    SizeY = panel.FloatingSize.Y
                };
                zoneLayout.Panels.Add(panelLayout);
            }
        }

        layout.Zones[zone] = zoneLayout;
    }

    private static void ApplyZoneLayout(
        IWorld world,
        Entity zoneEntity,
        DockZone zone,
        DockLayout layout,
        Func<string, Entity> panelResolver)
    {
        if (!zoneEntity.IsValid || !world.Has<UIDockZone>(zoneEntity))
        {
            return;
        }

        if (!layout.Zones.TryGetValue(zone, out var zoneLayout))
        {
            return;
        }

        ref var dockZone = ref world.Get<UIDockZone>(zoneEntity);
        dockZone.Size = zoneLayout.Size;
        dockZone.IsCollapsed = zoneLayout.IsCollapsed;

        // Update tab group selection
        if (dockZone.TabGroup.IsValid && world.Has<UIDockTabGroup>(dockZone.TabGroup))
        {
            ref var tabGroup = ref world.Get<UIDockTabGroup>(dockZone.TabGroup);
            tabGroup.SelectedIndex = zoneLayout.SelectedTabIndex;
        }

        // Dock panels to this zone
        foreach (var panelLayout in zoneLayout.Panels)
        {
            var panelEntity = panelResolver(panelLayout.PanelId);
            if (!panelEntity.IsValid || !world.Has<UIDockPanel>(panelEntity))
            {
                continue;
            }

            ref var panel = ref world.Get<UIDockPanel>(panelEntity);
            panel.State = DockState.Docked;
            panel.CurrentZone = zone;
            panel.FloatingPosition = new Vector2(panelLayout.PositionX, panelLayout.PositionY);
            panel.FloatingSize = new Vector2(panelLayout.SizeX, panelLayout.SizeY);
        }
    }
}

/// <summary>
/// Layout information for a dock zone.
/// </summary>
public sealed class DockZoneLayout
{
    /// <summary>
    /// Size of the zone in pixels.
    /// </summary>
    public float Size { get; set; } = 200f;

    /// <summary>
    /// Whether the zone is collapsed.
    /// </summary>
    public bool IsCollapsed { get; set; }

    /// <summary>
    /// Index of the selected tab in this zone.
    /// </summary>
    public int SelectedTabIndex { get; set; }

    /// <summary>
    /// Panels docked in this zone, in tab order.
    /// </summary>
    public List<DockPanelLayout> Panels { get; set; } = [];
}

/// <summary>
/// Layout information for a dock panel.
/// </summary>
public sealed class DockPanelLayout
{
    /// <summary>
    /// Identifier for the panel (typically the panel's title).
    /// </summary>
    public string PanelId { get; set; } = string.Empty;

    /// <summary>
    /// Floating position X coordinate (preserved for when panel is undocked).
    /// </summary>
    public float PositionX { get; set; }

    /// <summary>
    /// Floating position Y coordinate (preserved for when panel is undocked).
    /// </summary>
    public float PositionY { get; set; }

    /// <summary>
    /// Floating size width (preserved for when panel is undocked).
    /// </summary>
    public float SizeX { get; set; }

    /// <summary>
    /// Floating size height (preserved for when panel is undocked).
    /// </summary>
    public float SizeY { get; set; }
}

/// <summary>
/// Source-generated JSON serializer context for AOT-compatible serialization.
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(DockLayout))]
[JsonSerializable(typeof(DockZoneLayout))]
[JsonSerializable(typeof(DockPanelLayout))]
[JsonSerializable(typeof(Dictionary<DockZone, DockZoneLayout>))]
[JsonSerializable(typeof(List<DockPanelLayout>))]
internal sealed partial class DockLayoutJsonContext : JsonSerializerContext;
