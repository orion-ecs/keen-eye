using System.Numerics;
using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI;

/// <summary>
/// System that manages dockable panels, dock zones, and drag-to-dock functionality.
/// </summary>
/// <remarks>
/// <para>
/// This system handles:
/// <list type="bullet">
/// <item>Panel dragging from title bars</item>
/// <item>Dock zone detection during drag</item>
/// <item>Preview overlay display showing where panel will dock</item>
/// <item>Docking/undocking operations</item>
/// <item>Tab switching in dock zones</item>
/// <item>Zone resizing via splitters</item>
/// </list>
/// </para>
/// </remarks>
public sealed class UIDockSystem : SystemBase
{
    private EventSubscription? dockRequestSubscription;
    private EventSubscription? floatRequestSubscription;
    private EventSubscription? dragStartSubscription;
    private EventSubscription? dragSubscription;
    private EventSubscription? dragEndSubscription;
    private EventSubscription? clickSubscription;

    /// <inheritdoc />
    protected override void OnInitialize()
    {
        dockRequestSubscription = World.Subscribe<UIDockRequestEvent>(OnDockRequest);
        floatRequestSubscription = World.Subscribe<UIFloatRequestEvent>(OnFloatRequest);
        dragStartSubscription = World.Subscribe<UIDragStartEvent>(OnDragStart);
        dragSubscription = World.Subscribe<UIDragEvent>(OnDrag);
        dragEndSubscription = World.Subscribe<UIDragEndEvent>(OnDragEnd);
        clickSubscription = World.Subscribe<UIClickEvent>(OnClick);
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        dockRequestSubscription?.Dispose();
        floatRequestSubscription?.Dispose();
        dragStartSubscription?.Dispose();
        dragSubscription?.Dispose();
        dragEndSubscription?.Dispose();
        clickSubscription?.Dispose();
        base.Dispose();
    }

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        // Update zone layouts when panels change
        // Most work is event-driven
    }

    private void OnDockRequest(UIDockRequestEvent e)
    {
        DockPanel(e.Panel, e.Zone, e.Container);
    }

    private void OnFloatRequest(UIFloatRequestEvent e)
    {
        FloatPanel(e.Panel, e.Position);
    }

    private void OnDragStart(UIDragStartEvent e)
    {
        // Check if dragging a dock panel's title bar
        if (!World.Has<UIWindowTitleBar>(e.Element))
        {
            return;
        }

        ref readonly var titleBar = ref World.Get<UIWindowTitleBar>(e.Element);
        if (!World.Has<UIDockPanel>(titleBar.Window))
        {
            return;
        }

        ref var panel = ref World.Get<UIDockPanel>(titleBar.Window);

        if (!panel.CanFloat && panel.State == DockState.Docked)
        {
            return;  // Can't undock if floating is not allowed
        }

        // Mark panel as being dragged
        if (!World.Has<UIDockDraggingTag>(titleBar.Window))
        {
            World.Add(titleBar.Window, new UIDockDraggingTag());
        }

        // Find the dock container and set dragging panel
        if (panel.DockContainer.IsValid && World.Has<UIDockContainer>(panel.DockContainer))
        {
            ref var container = ref World.Get<UIDockContainer>(panel.DockContainer);
            container.DraggingPanel = titleBar.Window;

            // Show preview overlay
            ShowPreviewOverlay(panel.DockContainer, true);
        }
    }

    private void OnDrag(UIDragEvent e)
    {
        // Check if this element is related to a dragging dock panel
        if (!World.Has<UIWindowTitleBar>(e.Element))
        {
            return;
        }

        ref readonly var titleBar = ref World.Get<UIWindowTitleBar>(e.Element);
        if (!World.Has<UIDockDraggingTag>(titleBar.Window))
        {
            return;
        }

        if (!World.Has<UIDockPanel>(titleBar.Window))
        {
            return;
        }

        ref readonly var panel = ref World.Get<UIDockPanel>(titleBar.Window);

        // Find which zone the cursor is over
        if (panel.DockContainer.IsValid)
        {
            var detectedZone = DetectDockZone(panel.DockContainer, e.Position);
            UpdatePreviewOverlay(panel.DockContainer, detectedZone, e.Position);
        }

        // Move floating panel if it's floating
        if (panel.State == DockState.Floating && World.Has<UIRect>(titleBar.Window))
        {
            ref var rect = ref World.Get<UIRect>(titleBar.Window);
            rect.Offset = new UIEdges(
                rect.Offset.Left + e.Delta.X,
                rect.Offset.Top + e.Delta.Y,
                0, 0);
        }
    }

    private void OnDragEnd(UIDragEndEvent e)
    {
        if (!World.Has<UIWindowTitleBar>(e.Element))
        {
            return;
        }

        ref readonly var titleBar = ref World.Get<UIWindowTitleBar>(e.Element);
        if (!World.Has<UIDockDraggingTag>(titleBar.Window))
        {
            return;
        }

        if (!World.Has<UIDockPanel>(titleBar.Window))
        {
            return;
        }

        ref var panel = ref World.Get<UIDockPanel>(titleBar.Window);

        // Determine final dock zone
        if (panel.DockContainer.IsValid)
        {
            var targetZone = DetectDockZone(panel.DockContainer, e.EndPosition);

            if (targetZone != DockZone.None && panel.CanDock && (panel.AllowedZones & targetZone) != 0)
            {
                // Dock to the zone
                DockPanel(titleBar.Window, targetZone, panel.DockContainer);
            }
            else if (panel.State == DockState.Docked && panel.CanFloat)
            {
                // Undock to floating
                FloatPanel(titleBar.Window, e.EndPosition);
            }

            // Hide preview and clear dragging state
            ShowPreviewOverlay(panel.DockContainer, false);

            ref var container = ref World.Get<UIDockContainer>(panel.DockContainer);
            container.DraggingPanel = Entity.Null;
        }

        // Remove dragging tag
        if (World.Has<UIDockDraggingTag>(titleBar.Window))
        {
            World.Remove<UIDockDraggingTag>(titleBar.Window);
        }
    }

    private void OnClick(UIClickEvent e)
    {
        // Handle tab clicks
        if (World.Has<UIDockTab>(e.Element))
        {
            ref readonly var tab = ref World.Get<UIDockTab>(e.Element);
            SelectTab(tab.TabGroup, tab.Index);
        }
    }

    /// <summary>
    /// Docks a panel to a specific zone.
    /// </summary>
    public void DockPanel(Entity panelEntity, DockZone zone, Entity containerEntity)
    {
        if (!World.Has<UIDockPanel>(panelEntity) || !World.Has<UIDockContainer>(containerEntity))
        {
            return;
        }

        ref var panel = ref World.Get<UIDockPanel>(panelEntity);
        ref var container = ref World.Get<UIDockContainer>(containerEntity);

        var previousState = panel.State;

        // If already docked somewhere, remove from that zone first
        if (panel.State == DockState.Docked && panel.CurrentZone != DockZone.None)
        {
            RemovePanelFromZone(panelEntity, ref panel, containerEntity);
        }

        // Find the target zone entity
        Entity targetZoneEntity = zone switch
        {
            DockZone.Left => container.LeftZone,
            DockZone.Right => container.RightZone,
            DockZone.Top => container.TopZone,
            DockZone.Bottom => container.BottomZone,
            DockZone.Center => container.CenterZone,
            _ => Entity.Null
        };

        if (!targetZoneEntity.IsValid)
        {
            return;
        }

        // Add panel to zone
        AddPanelToZone(panelEntity, ref panel, targetZoneEntity, zone);

        // Update panel state
        panel.State = DockState.Docked;
        panel.CurrentZone = zone;
        panel.DockContainer = containerEntity;

        // Fire events
        if (previousState != DockState.Docked)
        {
            World.Send(new UIDockStateChangedEvent(panelEntity, previousState, DockState.Docked));
        }

        World.Send(new UIDockPanelDockedEvent(panelEntity, zone, containerEntity));
    }

    /// <summary>
    /// Floats (undocks) a panel.
    /// </summary>
    public void FloatPanel(Entity panelEntity, Vector2 position)
    {
        if (!World.Has<UIDockPanel>(panelEntity))
        {
            return;
        }

        ref var panel = ref World.Get<UIDockPanel>(panelEntity);

        if (!panel.CanFloat)
        {
            return;
        }

        var previousState = panel.State;
        var previousZone = panel.CurrentZone;

        // Remove from current zone if docked
        if (panel.State == DockState.Docked && panel.DockContainer.IsValid)
        {
            RemovePanelFromZone(panelEntity, ref panel, panel.DockContainer);
        }

        // Update state
        panel.State = DockState.Floating;
        panel.CurrentZone = DockZone.None;
        panel.FloatingPosition = position;

        // Position the panel window
        if (World.Has<UIRect>(panelEntity))
        {
            ref var rect = ref World.Get<UIRect>(panelEntity);
            rect.Offset = new UIEdges(position.X, position.Y, 0, 0);
            rect.Size = panel.FloatingSize;
        }

        // Make visible
        if (World.Has<UIElement>(panelEntity))
        {
            ref var element = ref World.Get<UIElement>(panelEntity);
            element.Visible = true;
        }

        if (World.Has<UIHiddenTag>(panelEntity))
        {
            World.Remove<UIHiddenTag>(panelEntity);
        }

        // Fire events
        if (previousState != DockState.Floating)
        {
            World.Send(new UIDockStateChangedEvent(panelEntity, previousState, DockState.Floating));
        }

        if (previousZone != DockZone.None)
        {
            World.Send(new UIDockPanelUndockedEvent(panelEntity, previousZone));
        }
    }

    /// <summary>
    /// Selects a tab in a dock tab group.
    /// </summary>
    public void SelectTab(Entity tabGroupEntity, int index)
    {
        if (!World.Has<UIDockTabGroup>(tabGroupEntity))
        {
            return;
        }

        ref var tabGroup = ref World.Get<UIDockTabGroup>(tabGroupEntity);
        var previousIndex = tabGroup.SelectedIndex;

        if (index < 0 || index >= tabGroup.TabCount || index == previousIndex)
        {
            return;
        }

        tabGroup.SelectedIndex = index;

        // Update tab visibility
        foreach (var tabEntity in World.Query<UIDockTab>())
        {
            ref readonly var tab = ref World.Get<UIDockTab>(tabEntity);
            if (tab.TabGroup != tabGroupEntity)
            {
                continue;
            }

            bool isSelected = tab.Index == index;

            // Update panel visibility
            if (tab.Panel.IsValid && World.Has<UIElement>(tab.Panel))
            {
                ref var element = ref World.Get<UIElement>(tab.Panel);
                element.Visible = isSelected;

                if (isSelected && World.Has<UIHiddenTag>(tab.Panel))
                {
                    World.Remove<UIHiddenTag>(tab.Panel);
                }
                else if (!isSelected && !World.Has<UIHiddenTag>(tab.Panel))
                {
                    World.Add(tab.Panel, new UIHiddenTag());
                }
            }
        }

        World.Send(new UIDockTabChangedEvent(tabGroupEntity, previousIndex, index));
    }

    private DockZone DetectDockZone(Entity containerEntity, Vector2 position)
    {
        if (!World.Has<UIDockContainer>(containerEntity) || !World.Has<UIRect>(containerEntity))
        {
            return DockZone.None;
        }

        ref readonly var containerRect = ref World.Get<UIRect>(containerEntity);
        var bounds = containerRect.ComputedBounds;

        // Check if position is within the container
        if (position.X < bounds.X || position.X > bounds.X + bounds.Width ||
            position.Y < bounds.Y || position.Y > bounds.Y + bounds.Height)
        {
            return DockZone.None;
        }

        // Calculate relative position within container
        var relX = (position.X - bounds.X) / bounds.Width;
        var relY = (position.Y - bounds.Y) / bounds.Height;

        // Define edge zones (outer 25%)
        const float edgeThreshold = 0.25f;

        if (relX < edgeThreshold)
        {
            return DockZone.Left;
        }

        if (relX > 1f - edgeThreshold)
        {
            return DockZone.Right;
        }

        if (relY < edgeThreshold)
        {
            return DockZone.Top;
        }

        if (relY > 1f - edgeThreshold)
        {
            return DockZone.Bottom;
        }

        return DockZone.Center;
    }

    private void ShowPreviewOverlay(Entity containerEntity, bool show)
    {
        if (!World.Has<UIDockContainer>(containerEntity))
        {
            return;
        }

        ref readonly var container = ref World.Get<UIDockContainer>(containerEntity);

        if (!container.PreviewOverlay.IsValid || !World.IsAlive(container.PreviewOverlay))
        {
            return;
        }

        if (World.Has<UIElement>(container.PreviewOverlay))
        {
            ref var element = ref World.Get<UIElement>(container.PreviewOverlay);
            element.Visible = show;
        }

        if (show && World.Has<UIHiddenTag>(container.PreviewOverlay))
        {
            World.Remove<UIHiddenTag>(container.PreviewOverlay);
        }
        else if (!show && !World.Has<UIHiddenTag>(container.PreviewOverlay))
        {
            World.Add(container.PreviewOverlay, new UIHiddenTag());
        }
    }

    private void UpdatePreviewOverlay(Entity containerEntity, DockZone zone, Vector2 position)
    {
        if (!World.Has<UIDockContainer>(containerEntity) || !World.Has<UIRect>(containerEntity))
        {
            return;
        }

        ref readonly var container = ref World.Get<UIDockContainer>(containerEntity);
        if (!container.PreviewOverlay.IsValid || !World.Has<UIRect>(container.PreviewOverlay))
        {
            return;
        }

        ref readonly var containerRect = ref World.Get<UIRect>(containerEntity);
        ref var previewRect = ref World.Get<UIRect>(container.PreviewOverlay);

        var bounds = containerRect.ComputedBounds;
        var halfWidth = bounds.Width / 2;
        var halfHeight = bounds.Height / 2;

        // Position and size preview based on target zone
        (float x, float y, float w, float h) = zone switch
        {
            DockZone.Left => (bounds.X, bounds.Y, halfWidth, bounds.Height),
            DockZone.Right => (bounds.X + halfWidth, bounds.Y, halfWidth, bounds.Height),
            DockZone.Top => (bounds.X, bounds.Y, bounds.Width, halfHeight),
            DockZone.Bottom => (bounds.X, bounds.Y + halfHeight, bounds.Width, halfHeight),
            DockZone.Center => (bounds.X + bounds.Width / 4, bounds.Y + bounds.Height / 4, halfWidth, halfHeight),
            _ => (0, 0, 0, 0)
        };

        previewRect.Offset = new UIEdges(x, y, 0, 0);
        previewRect.Size = new Vector2(w, h);
    }

    private void AddPanelToZone(Entity panelEntity, ref UIDockPanel panel, Entity zoneEntity, DockZone zone)
    {
        if (!World.Has<UIDockZone>(zoneEntity))
        {
            return;
        }

        ref var dockZone = ref World.Get<UIDockZone>(zoneEntity);

        // Parent the panel to the zone
        World.SetParent(panelEntity, zoneEntity);

        // If zone has a tab group, add a tab
        if (dockZone.TabGroup.IsValid && World.Has<UIDockTabGroup>(dockZone.TabGroup))
        {
            ref var tabGroup = ref World.Get<UIDockTabGroup>(dockZone.TabGroup);

            // Create a tab for this panel
            var tab = World.Spawn($"Tab_{panel.Title}")
                .With(new UIElement { Visible = true, RaycastTarget = true })
                .With(new UIDockTab(panelEntity, dockZone.TabGroup) { Index = tabGroup.TabCount })
                .Build();

            World.SetParent(tab, dockZone.TabGroup);
            tabGroup.TabCount++;

            // Select this tab if it's the first or only one
            if (tabGroup.TabCount == 1)
            {
                SelectTab(dockZone.TabGroup, 0);
            }
        }

        // Make sure zone is not collapsed
        dockZone.IsCollapsed = false;
    }

    private void RemovePanelFromZone(Entity panelEntity, ref UIDockPanel panel, Entity containerEntity)
    {
        // Remove tab if exists
        Entity tabToRemove = Entity.Null;
        foreach (var tabEntity in World.Query<UIDockTab>())
        {
            ref readonly var tab = ref World.Get<UIDockTab>(tabEntity);
            if (tab.Panel == panelEntity)
            {
                tabToRemove = tabEntity;
                break;
            }
        }

        if (tabToRemove.IsValid)
        {
            if (World.Has<UIDockTab>(tabToRemove))
            {
                ref readonly var tab = ref World.Get<UIDockTab>(tabToRemove);
                if (World.Has<UIDockTabGroup>(tab.TabGroup))
                {
                    ref var tabGroup = ref World.Get<UIDockTabGroup>(tab.TabGroup);
                    tabGroup.TabCount--;

                    // Adjust selected index if needed
                    if (tabGroup.SelectedIndex >= tabGroup.TabCount && tabGroup.TabCount > 0)
                    {
                        SelectTab(tab.TabGroup, tabGroup.TabCount - 1);
                    }
                }
            }

            World.Despawn(tabToRemove);
        }

        // Unparent the panel
        World.SetParent(panelEntity, Entity.Null);
    }
}
