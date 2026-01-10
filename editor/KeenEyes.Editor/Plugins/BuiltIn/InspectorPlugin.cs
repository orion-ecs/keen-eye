// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Capabilities;
using KeenEyes.Editor.Abstractions;
using KeenEyes.Editor.Abstractions.Capabilities;
using KeenEyes.Editor.Application;
using KeenEyes.Editor.Panels;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.UI.Abstractions;
using KeenEyes.UI.Widgets;

namespace KeenEyes.Editor.Plugins.BuiltIn;

/// <summary>
/// Plugin that provides the inspector panel for editing entity properties.
/// </summary>
/// <remarks>
/// <para>
/// The inspector panel displays the components attached to the selected entity
/// and allows editing their properties. Property drawers can be registered
/// to customize the editing experience for specific component types.
/// </para>
/// </remarks>
internal sealed class InspectorPlugin : EditorPluginBase
{
    private const string PanelId = "inspector";

    /// <inheritdoc />
    public override string Name => "Inspector";

    /// <inheritdoc />
    public override string? Description => "Entity inspector panel for editing component properties";

    /// <inheritdoc />
    protected override void OnInitialize(IEditorContext context)
    {
        if (!context.TryGetCapability<IPanelCapability>(out var panels) || panels is null)
        {
            return;
        }

        // Register the inspector panel
        panels.RegisterPanel(
            new PanelDescriptor
            {
                Id = PanelId,
                Title = "Inspector",
                Icon = "inspector",
                DefaultLocation = PanelDockLocation.Right,
                OpenByDefault = true,
                MinWidth = 250,
                MinHeight = 200,
                DefaultWidth = 350,
                DefaultHeight = 500,
                Category = "Scene",
                ToggleShortcut = "Ctrl+Shift+I"
            },
            () => new InspectorPanelImpl());

        // Register shortcut for toggling the inspector panel
        if (context.TryGetCapability<IShortcutCapability>(out var shortcuts) && shortcuts is not null)
        {
            shortcuts.RegisterShortcut(
                "inspector.toggle",
                "Toggle Inspector",
                ShortcutCategories.View,
                "Ctrl+Shift+I",
                () =>
                {
                    if (context.TryGetCapability<IPanelCapability>(out var p) && p is not null)
                    {
                        if (p.IsPanelOpen(PanelId))
                        {
                            p.ClosePanel(PanelId);
                        }
                        else
                        {
                            p.OpenPanel(PanelId);
                        }
                    }
                });
        }
    }
}

/// <summary>
/// Implementation of the inspector panel.
/// </summary>
/// <remarks>
/// This implementation creates the inspector UI and handles selection changes
/// to rebuild the inspector for the currently selected entity.
/// </remarks>
internal sealed class InspectorPanelImpl : IEditorPanel
{
    private Entity rootEntity;
    private Entity contentArea;
    private Entity emptyLabel;
    private IEditorContext? editorContext;
    private IWorld? editorWorld;
    private FontHandle font;
    private EventSubscription? selectionChangedSubscription;

    /// <inheritdoc />
    public Entity RootEntity => rootEntity;

    /// <inheritdoc />
    public void Initialize(PanelContext context)
    {
        editorContext = context.EditorContext;
        editorWorld = context.EditorWorld;
        font = context.Font;

        // Create the inspector panel UI
        rootEntity = CreatePanelUI(context);

        // Subscribe to selection changes to update the inspector
        selectionChangedSubscription = editorContext.OnSelectionChanged(OnSelectionChanged);
    }

    private Entity CreatePanelUI(PanelContext context)
    {
        var world = context.EditorWorld;

        // Create the main panel container
        var panel = WidgetFactory.CreatePanel(world, context.Parent, "InspectorPluginPanel", new PanelConfig(
            Direction: LayoutDirection.Vertical,
            BackgroundColor: EditorColors.DarkPanel
        ));

        ref var panelRect = ref world.Get<UIRect>(panel);
        panelRect.WidthMode = UISizeMode.Fill;
        panelRect.HeightMode = UISizeMode.Fill;

        // Create header
        var header = WidgetFactory.CreatePanel(world, panel, "InspectorPluginHeader", new PanelConfig(
            Height: 28,
            Direction: LayoutDirection.Horizontal,
            MainAxisAlign: LayoutAlign.SpaceBetween,
            CrossAxisAlign: LayoutAlign.Center,
            BackgroundColor: EditorColors.MediumPanel,
            Padding: UIEdges.All(8)
        ));

        ref var headerRect = ref world.Get<UIRect>(header);
        headerRect.WidthMode = UISizeMode.Fill;

        WidgetFactory.CreateLabel(world, header, "InspectorPluginTitle", "Inspector", font, new LabelConfig(
            FontSize: 13,
            TextColor: EditorColors.TextWhite,
            HorizontalAlign: TextAlignH.Left
        ));

        // Create content area
        contentArea = WidgetFactory.CreatePanel(world, panel, "InspectorPluginContent", new PanelConfig(
            Direction: LayoutDirection.Vertical,
            BackgroundColor: new System.Numerics.Vector4(0.10f, 0.10f, 0.13f, 1f),
            Spacing: 2
        ));

        ref var contentRect = ref world.Get<UIRect>(contentArea);
        contentRect.WidthMode = UISizeMode.Fill;
        contentRect.HeightMode = UISizeMode.Fill;

        // Create empty state label
        emptyLabel = WidgetFactory.CreateLabel(world, contentArea, "InspectorPluginEmpty",
            "No entity selected", font, new LabelConfig(
                FontSize: 13,
                TextColor: EditorColors.TextMuted,
                HorizontalAlign: TextAlignH.Center,
                VerticalAlign: TextAlignV.Middle
            ));

        ref var emptyRect = ref world.Get<UIRect>(emptyLabel);
        emptyRect.WidthMode = UISizeMode.Fill;
        emptyRect.HeightMode = UISizeMode.Fill;

        return panel;
    }

    /// <inheritdoc />
    public void Update(float deltaTime)
    {
        // Inspector uses UI events, no per-frame logic needed
    }

    /// <inheritdoc />
    public void Shutdown()
    {
        selectionChangedSubscription?.Dispose();

        if (editorWorld is not null && rootEntity.IsValid && editorWorld.IsAlive(rootEntity))
        {
            editorWorld.Despawn(rootEntity);
        }
    }

    private void OnSelectionChanged(IReadOnlyList<Entity> selectedEntities)
    {
        if (editorWorld is null || editorContext is null || !rootEntity.IsValid)
        {
            return;
        }

        if (selectedEntities.Count > 0)
        {
            // Rebuild inspector for the primary selection
            RefreshInspector(selectedEntities[0]);
        }
        else
        {
            // Clear inspector when nothing is selected
            ClearInspector();
        }
    }

    private void RefreshInspector(Entity selectedEntity)
    {
        if (editorWorld is null || editorContext is null || !contentArea.IsValid)
        {
            return;
        }

        var sceneWorld = editorContext.Worlds.CurrentSceneWorld;

        if (sceneWorld is null || !selectedEntity.IsValid)
        {
            ClearInspector();
            return;
        }

        // Hide empty label
        if (emptyLabel.IsValid && editorWorld.Has<UIElement>(emptyLabel))
        {
            ref var emptyElement = ref editorWorld.Get<UIElement>(emptyLabel);
            emptyElement.Visible = false;
        }

        // Clear existing component displays
        ClearComponentDisplays();

        // Create entity header with name
        CreateEntityHeader(selectedEntity);

        // Display components
        DisplayComponents(sceneWorld, selectedEntity);
    }

    private void ClearInspector()
    {
        if (editorWorld is null || !contentArea.IsValid)
        {
            return;
        }

        // Show empty label
        if (emptyLabel.IsValid && editorWorld.Has<UIElement>(emptyLabel))
        {
            ref var emptyElement = ref editorWorld.Get<UIElement>(emptyLabel);
            emptyElement.Visible = true;
        }

        // Clear component displays
        ClearComponentDisplays();
    }

    private void ClearComponentDisplays()
    {
        if (editorWorld is null || !contentArea.IsValid)
        {
            return;
        }

        var children = editorWorld.GetChildren(contentArea).ToList();
        foreach (var child in children)
        {
            // Don't remove the empty label
            if (child == emptyLabel)
            {
                continue;
            }

            // Remove component display panels
            if (editorWorld.Has<InspectorComponentTag>(child))
            {
                DespawnRecursive(child);
            }
        }
    }

    private void CreateEntityHeader(Entity entity)
    {
        if (editorWorld is null || editorContext is null || !contentArea.IsValid)
        {
            return;
        }

        var entityName = editorContext.Worlds.GetEntityName(entity);

        var headerPanel = WidgetFactory.CreatePanel(editorWorld, contentArea, "EntityHeader", new PanelConfig(
            Height: 32,
            Direction: LayoutDirection.Horizontal,
            MainAxisAlign: LayoutAlign.Start,
            CrossAxisAlign: LayoutAlign.Center,
            BackgroundColor: new System.Numerics.Vector4(0.15f, 0.15f, 0.18f, 1f),
            Padding: UIEdges.All(8)
        ));

        ref var headerRect = ref editorWorld.Get<UIRect>(headerPanel);
        headerRect.WidthMode = UISizeMode.Fill;

        // Entity icon (placeholder)
        _ = WidgetFactory.CreatePanel(editorWorld, headerPanel, "EntityIcon", new PanelConfig(
            Width: 16,
            Height: 16,
            BackgroundColor: new System.Numerics.Vector4(0.3f, 0.5f, 0.8f, 1f)
        ));

        // Entity name label
        WidgetFactory.CreateLabel(editorWorld, headerPanel, "EntityName", entityName, font, new LabelConfig(
            FontSize: 14,
            TextColor: EditorColors.TextWhite,
            HorizontalAlign: TextAlignH.Left
        ));

        editorWorld.Add(headerPanel, new InspectorComponentTag());
    }

    private void DisplayComponents(IWorld sceneWorld, Entity entity)
    {
        if (editorWorld is null || !contentArea.IsValid)
        {
            return;
        }

        if (sceneWorld is ISnapshotCapability snapshot)
        {
            foreach (var (componentType, componentValue) in snapshot.GetComponents(entity))
            {
                CreateComponentSection(componentType, componentValue);
            }
        }
    }

    private void CreateComponentSection(Type componentType, object componentValue)
    {
        if (editorWorld is null || !contentArea.IsValid)
        {
            return;
        }

        // Create component panel
        var componentPanel = WidgetFactory.CreatePanel(editorWorld, contentArea, $"Component_{componentType.Name}", new PanelConfig(
            Direction: LayoutDirection.Vertical,
            BackgroundColor: new System.Numerics.Vector4(0.12f, 0.12f, 0.15f, 1f),
            Spacing: 4
        ));

        ref var panelRect = ref editorWorld.Get<UIRect>(componentPanel);
        panelRect.WidthMode = UISizeMode.Fill;
        panelRect.HeightMode = UISizeMode.FitContent;

        editorWorld.Add(componentPanel, new InspectorComponentTag());

        // Component header
        var headerPanel = WidgetFactory.CreatePanel(editorWorld, componentPanel, $"Header_{componentType.Name}", new PanelConfig(
            Height: 24,
            Direction: LayoutDirection.Horizontal,
            MainAxisAlign: LayoutAlign.Start,
            CrossAxisAlign: LayoutAlign.Center,
            BackgroundColor: new System.Numerics.Vector4(0.18f, 0.18f, 0.22f, 1f),
            Padding: UIEdges.Symmetric(8, 0)
        ));

        ref var headerRect = ref editorWorld.Get<UIRect>(headerPanel);
        headerRect.WidthMode = UISizeMode.Fill;

        // Component type name
        WidgetFactory.CreateLabel(editorWorld, headerPanel, $"Label_{componentType.Name}",
            componentType.Name, font, new LabelConfig(
                FontSize: 12,
                TextColor: EditorColors.TextWhite,
                HorizontalAlign: TextAlignH.Left
            ));
    }

    private void DespawnRecursive(Entity entity)
    {
        if (editorWorld is null)
        {
            return;
        }

        var children = editorWorld.GetChildren(entity).ToList();
        foreach (var child in children)
        {
            DespawnRecursive(child);
        }
        editorWorld.Despawn(entity);
    }
}
