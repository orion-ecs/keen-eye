// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using System.Numerics;
using KeenEyes.Editor.Abstractions;
using KeenEyes.Editor.Abstractions.Capabilities;
using KeenEyes.Editor.Application;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.Input.Abstractions;
using KeenEyes.UI.Abstractions;
using KeenEyes.UI.Widgets;

namespace KeenEyes.Editor.Plugins.BuiltIn;

/// <summary>
/// Plugin that provides the plugin manager panel for viewing and managing plugins.
/// </summary>
/// <remarks>
/// <para>
/// The plugin manager panel displays all discovered plugins with their status,
/// allows enabling/disabling plugins that support it, and hot reloading plugins
/// that support hot reload.
/// </para>
/// </remarks>
internal sealed class PluginManagerPlugin : EditorPluginBase
{
    private const string PanelId = "plugins";

    /// <inheritdoc />
    public override string Name => "Plugin Manager";

    /// <inheritdoc />
    public override string? Description => "Plugin manager panel for viewing and managing plugins";

    /// <inheritdoc />
    protected override void OnInitialize(IEditorContext context)
    {
        if (!context.TryGetCapability<IPanelCapability>(out var panels) || panels is null)
        {
            return;
        }

        // Register the plugin manager panel
        panels.RegisterPanel(
            new PanelDescriptor
            {
                Id = PanelId,
                Title = "Plugins",
                Icon = "plugins",
                DefaultLocation = PanelDockLocation.Right,
                OpenByDefault = false,
                MinWidth = 400,
                MinHeight = 300,
                DefaultWidth = 500,
                DefaultHeight = 400,
                Category = "Tools",
                ToggleShortcut = "Ctrl+Shift+P"
            },
            () => new PluginManagerPanelImpl());

        // Register shortcut for toggling the plugin manager panel
        if (context.TryGetCapability<IShortcutCapability>(out var shortcuts) && shortcuts is not null)
        {
            shortcuts.RegisterShortcut(
                "plugins.toggle",
                "Toggle Plugin Manager",
                ShortcutCategories.View,
                "Ctrl+Shift+P",
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
/// Implementation of the plugin manager panel.
/// </summary>
internal sealed class PluginManagerPanelImpl : IEditorPanel
{
    private const float RefreshInterval = 1.0f;

    private Entity rootEntity;
    private IWorld? editorWorld;
    private IEditorContext? editorContext;
    private EditorPluginManager? pluginManager;
    private FontHandle font;
    private float refreshTimer;

    /// <inheritdoc />
    public Entity RootEntity => rootEntity;

    /// <inheritdoc />
    public void Initialize(PanelContext panelContext)
    {
        editorWorld = panelContext.EditorWorld;
        editorContext = panelContext.EditorContext;
        rootEntity = panelContext.Parent;

        // Get the plugin manager from the editor context
        // The plugin manager is stored as a global extension
        if (editorContext.TryGetExtension<EditorPluginManager>(out var manager))
        {
            pluginManager = manager;
        }

        // TODO: Get font from panel context or resource manager
        font = default;

        CreatePanelUI();
        SubscribeToEvents();
        RefreshPluginList();
    }

    private void SubscribeToEvents()
    {
        if (editorWorld is null)
        {
            return;
        }

        // Subscribe to click events
        editorWorld.Subscribe<UIClickEvent>(OnClick);
    }

    private void OnClick(UIClickEvent e)
    {
        if (editorWorld is null || pluginManager is null)
        {
            return;
        }

        var clickedEntity = e.Element;

        // Check if a plugin list item was clicked
        if (editorWorld.Has<PluginListItemData>(clickedEntity))
        {
            ref readonly var itemData = ref editorWorld.Get<PluginListItemData>(clickedEntity);
            SelectPlugin(itemData.PluginId);
            return;
        }

        // Check if a control button was clicked
        if (editorWorld.Has<PluginControlButtonData>(clickedEntity))
        {
            ref readonly var buttonData = ref editorWorld.Get<PluginControlButtonData>(clickedEntity);
            ExecuteButtonAction(buttonData.PluginId, buttonData.Action);
            return;
        }
    }

    private void ExecuteButtonAction(string pluginId, PluginButtonAction action)
    {
        if (pluginManager is null)
        {
            return;
        }

        switch (action)
        {
            case PluginButtonAction.Enable:
                pluginManager.EnableDynamicPlugin(pluginId);
                break;
            case PluginButtonAction.Disable:
                pluginManager.DisableDynamicPlugin(pluginId);
                break;
            case PluginButtonAction.Reload:
                pluginManager.ReloadDynamicPlugin(pluginId);
                break;
        }

        // Refresh the UI after action
        RefreshPluginList();

        // Re-select the current plugin to update the details view
        if (editorWorld is not null && editorWorld.Has<PluginManagerPanelState>(rootEntity))
        {
            ref readonly var state = ref editorWorld.Get<PluginManagerPanelState>(rootEntity);
            if (state.SelectedPluginId is not null)
            {
                var plugin = pluginManager.GetDynamicPlugin(state.SelectedPluginId);
                if (plugin is not null)
                {
                    ShowPluginDetails(plugin);
                }
            }
        }
    }

    /// <inheritdoc />
    public void Update(float deltaTime)
    {
        refreshTimer += deltaTime;

        if (refreshTimer >= RefreshInterval)
        {
            refreshTimer = 0f;
            RefreshPluginList();
        }
    }

    /// <inheritdoc />
    public void Shutdown()
    {
        // Panel cleanup is handled by the dock system
    }

    private void CreatePanelUI()
    {
        if (editorWorld is null)
        {
            return;
        }

        // Create the main split container (horizontal)
        var splitContainer = WidgetFactory.CreatePanel(
            editorWorld,
            rootEntity,
            "PluginSplitContainer",
            new PanelConfig(
                Direction: LayoutDirection.Horizontal,
                BackgroundColor: EditorColors.DarkPanel
            ));

        ref var splitRect = ref editorWorld.Get<UIRect>(splitContainer);
        splitRect.WidthMode = UISizeMode.Fill;
        splitRect.HeightMode = UISizeMode.Fill;

        // Create left panel (plugin list)
        var leftPanel = CreatePluginListPanel(splitContainer);

        // Create divider
        WidgetFactory.CreateDivider(editorWorld, splitContainer, "PluginDivider", new DividerConfig(
            Orientation: LayoutDirection.Vertical,
            Thickness: 1,
            Color: new Vector4(0.3f, 0.3f, 0.35f, 1f)
        ));

        // Create right panel (details)
        var rightPanel = CreateDetailsPanel(splitContainer);

        // Store panel state
        editorWorld.Add(rootEntity, new PluginManagerPanelState
        {
            PluginListContainer = leftPanel,
            DetailsContainer = rightPanel,
            SelectedPluginId = null
        });
    }

    private Entity CreatePluginListPanel(Entity parent)
    {
        if (editorWorld is null)
        {
            return Entity.Null;
        }

        // Create list container
        var listPanel = WidgetFactory.CreatePanel(
            editorWorld,
            parent,
            "PluginListPanel",
            new PanelConfig(
                Width: 180,
                Direction: LayoutDirection.Vertical,
                BackgroundColor: EditorColors.MediumPanel
            ));

        ref var listRect = ref editorWorld.Get<UIRect>(listPanel);
        listRect.HeightMode = UISizeMode.Fill;

        // Create header
        var header = WidgetFactory.CreatePanel(
            editorWorld,
            listPanel,
            "PluginListHeader",
            new PanelConfig(
                Height: 28,
                Direction: LayoutDirection.Horizontal,
                MainAxisAlign: LayoutAlign.Start,
                CrossAxisAlign: LayoutAlign.Center,
                BackgroundColor: EditorColors.DarkPanel,
                Padding: UIEdges.All(8)
            ));

        ref var headerRect = ref editorWorld.Get<UIRect>(header);
        headerRect.WidthMode = UISizeMode.Fill;

        WidgetFactory.CreateLabel(
            editorWorld,
            header,
            "PluginListTitle",
            "PLUGINS",
            font,
            new LabelConfig(
                FontSize: 11,
                TextColor: EditorColors.TextMuted,
                HorizontalAlign: TextAlignH.Left
            ));

        // Create scrollable list container
        var listContainer = WidgetFactory.CreatePanel(
            editorWorld,
            listPanel,
            "PluginListItems",
            new PanelConfig(
                Direction: LayoutDirection.Vertical,
                BackgroundColor: EditorColors.MediumPanel,
                Padding: UIEdges.All(4)
            ));

        ref var containerRect = ref editorWorld.Get<UIRect>(listContainer);
        containerRect.WidthMode = UISizeMode.Fill;
        containerRect.HeightMode = UISizeMode.Fill;

        return listContainer;
    }

    private Entity CreateDetailsPanel(Entity parent)
    {
        if (editorWorld is null)
        {
            return Entity.Null;
        }

        // Create details container
        var detailsPanel = WidgetFactory.CreatePanel(
            editorWorld,
            parent,
            "PluginDetailsPanel",
            new PanelConfig(
                Direction: LayoutDirection.Vertical,
                BackgroundColor: EditorColors.DarkPanel,
                Padding: UIEdges.All(12)
            ));

        ref var detailsRect = ref editorWorld.Get<UIRect>(detailsPanel);
        detailsRect.WidthMode = UISizeMode.Fill;
        detailsRect.HeightMode = UISizeMode.Fill;

        // Create "Select a plugin" placeholder
        WidgetFactory.CreateLabel(
            editorWorld,
            detailsPanel,
            "PluginDetailsPlaceholder",
            "Select a plugin to view details",
            font,
            new LabelConfig(
                FontSize: 13,
                TextColor: EditorColors.TextMuted,
                HorizontalAlign: TextAlignH.Center,
                VerticalAlign: TextAlignV.Middle
            ));

        return detailsPanel;
    }

    private void RefreshPluginList()
    {
        if (editorWorld is null || pluginManager is null)
        {
            return;
        }

        if (!editorWorld.Has<PluginManagerPanelState>(rootEntity))
        {
            return;
        }

        ref readonly var state = ref editorWorld.Get<PluginManagerPanelState>(rootEntity);
        var listContainer = state.PluginListContainer;

        if (!listContainer.IsValid)
        {
            return;
        }

        // Clear existing plugin items
        ClearChildren(listContainer);

        // Add plugin items
        foreach (var plugin in pluginManager.GetDynamicPlugins())
        {
            CreatePluginListItem(listContainer, plugin);
        }
    }

    private void CreatePluginListItem(Entity parent, LoadedPlugin plugin)
    {
        if (editorWorld is null)
        {
            return;
        }

        var itemId = $"PluginItem_{plugin.Manifest.Id}";

        // Create item container
        var item = WidgetFactory.CreatePanel(
            editorWorld,
            parent,
            itemId,
            new PanelConfig(
                Height: 26,
                Direction: LayoutDirection.Horizontal,
                MainAxisAlign: LayoutAlign.Start,
                CrossAxisAlign: LayoutAlign.Center,
                BackgroundColor: Vector4.Zero,
                Padding: UIEdges.Symmetric(8, 4)
            ));

        ref var itemRect = ref editorWorld.Get<UIRect>(item);
        itemRect.WidthMode = UISizeMode.Fill;

        // Make item clickable
        editorWorld.Add(item, new UIInteractable
        {
            CanClick = true,
            CanFocus = true
        });

        // Store plugin reference
        editorWorld.Add(item, new PluginListItemData
        {
            PluginId = plugin.Manifest.Id
        });

        // Create status indicator (colored dot)
        var statusColor = GetStateColor(plugin.State);
        var statusBadge = editorWorld.Spawn($"{itemId}_Status")
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                Size = new Vector2(8, 8),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIStyle
            {
                BackgroundColor = statusColor,
                CornerRadius = 4
            })
            .Build();
        editorWorld.SetParent(statusBadge, item);

        // Create spacer
        var spacer = editorWorld.Spawn($"{itemId}_Spacer")
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                Size = new Vector2(8, 0),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fixed
            })
            .Build();
        editorWorld.SetParent(spacer, item);

        // Create plugin name label
        WidgetFactory.CreateLabel(
            editorWorld,
            item,
            $"{itemId}_Name",
            plugin.Manifest.Name,
            font,
            new LabelConfig(
                FontSize: 12,
                TextColor: plugin.State == PluginState.Failed ? new Vector4(0.9f, 0.4f, 0.4f, 1f) : EditorColors.TextLight,
                HorizontalAlign: TextAlignH.Left
            ));
    }

    private void ClearChildren(Entity parent)
    {
        if (editorWorld is null || !parent.IsValid)
        {
            return;
        }

        var children = editorWorld.GetChildren(parent).ToList();
        foreach (var child in children)
        {
            DespawnRecursive(child);
        }
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

    /// <summary>
    /// Gets the color for a plugin state.
    /// </summary>
    internal static Vector4 GetStateColor(PluginState state)
    {
        return state switch
        {
            PluginState.Enabled => new Vector4(0.3f, 0.8f, 0.3f, 1f),    // Green
            PluginState.Disabled => new Vector4(0.6f, 0.6f, 0.6f, 1f),   // Gray
            PluginState.Failed => new Vector4(0.9f, 0.3f, 0.3f, 1f),     // Red
            PluginState.Loaded => new Vector4(0.8f, 0.7f, 0.2f, 1f),     // Yellow
            PluginState.Discovered => new Vector4(0.5f, 0.5f, 0.7f, 1f), // Blue-gray
            PluginState.Unloading => new Vector4(0.7f, 0.5f, 0.2f, 1f),  // Orange
            _ => new Vector4(0.5f, 0.5f, 0.5f, 1f)
        };
    }

    /// <summary>
    /// Selects a plugin and shows its details.
    /// </summary>
    internal void SelectPlugin(string pluginId)
    {
        if (editorWorld is null || pluginManager is null)
        {
            return;
        }

        if (!editorWorld.Has<PluginManagerPanelState>(rootEntity))
        {
            return;
        }

        ref var state = ref editorWorld.Get<PluginManagerPanelState>(rootEntity);
        state.SelectedPluginId = pluginId;

        var plugin = pluginManager.GetDynamicPlugin(pluginId);
        if (plugin is null)
        {
            return;
        }

        ShowPluginDetails(plugin);
    }

    private void ShowPluginDetails(LoadedPlugin plugin)
    {
        if (editorWorld is null)
        {
            return;
        }

        if (!editorWorld.Has<PluginManagerPanelState>(rootEntity))
        {
            return;
        }

        ref readonly var state = ref editorWorld.Get<PluginManagerPanelState>(rootEntity);
        var detailsContainer = state.DetailsContainer;

        if (!detailsContainer.IsValid)
        {
            return;
        }

        // Clear existing details
        ClearChildren(detailsContainer);

        // Create header with name and version
        var header = WidgetFactory.CreatePanel(
            editorWorld,
            detailsContainer,
            "PluginDetailsHeader",
            new PanelConfig(
                Height: 60,
                Direction: LayoutDirection.Vertical,
                MainAxisAlign: LayoutAlign.Start,
                BackgroundColor: Vector4.Zero
            ));

        ref var headerRect = ref editorWorld.Get<UIRect>(header);
        headerRect.WidthMode = UISizeMode.Fill;

        // Plugin name
        WidgetFactory.CreateLabel(
            editorWorld,
            header,
            "PluginName",
            plugin.Manifest.Name,
            font,
            new LabelConfig(
                FontSize: 18,
                TextColor: EditorColors.TextWhite,
                HorizontalAlign: TextAlignH.Left
            ));

        // Version and author
        var subtitle = $"v{plugin.Manifest.Version}";
        if (!string.IsNullOrEmpty(plugin.Manifest.Author))
        {
            subtitle += $" by {plugin.Manifest.Author}";
        }

        WidgetFactory.CreateLabel(
            editorWorld,
            header,
            "PluginSubtitle",
            subtitle,
            font,
            new LabelConfig(
                FontSize: 12,
                TextColor: EditorColors.TextMuted,
                HorizontalAlign: TextAlignH.Left
            ));

        // Description
        if (!string.IsNullOrEmpty(plugin.Manifest.Description))
        {
            WidgetFactory.CreateLabel(
                editorWorld,
                detailsContainer,
                "PluginDescription",
                plugin.Manifest.Description,
                font,
                new LabelConfig(
                    FontSize: 13,
                    TextColor: EditorColors.TextLight,
                    HorizontalAlign: TextAlignH.Left
                ));
        }

        // Divider
        WidgetFactory.CreateDivider(editorWorld, detailsContainer, "DetailsDivider", new DividerConfig(
            Orientation: LayoutDirection.Horizontal,
            Thickness: 1,
            Margin: 8,
            Color: new Vector4(0.3f, 0.3f, 0.35f, 1f)
        ));

        // Dependencies section
        CreateDependenciesSection(detailsContainer, plugin);

        // Permissions section
        CreatePermissionsSection(detailsContainer, plugin);

        // Control buttons
        CreateControlButtons(detailsContainer, plugin);

        // Error section (if failed)
        if (plugin.State == PluginState.Failed && !string.IsNullOrEmpty(plugin.ErrorMessage))
        {
            CreateErrorSection(detailsContainer, plugin);
        }
    }

    private void CreateDependenciesSection(Entity parent, LoadedPlugin plugin)
    {
        if (editorWorld is null)
        {
            return;
        }

        var deps = plugin.Manifest.Dependencies;
        var depsText = deps is null || deps.Count == 0
            ? "None"
            : string.Join(", ", deps.Keys);

        WidgetFactory.CreateLabel(
            editorWorld,
            parent,
            "PluginDependencies",
            $"Dependencies: {depsText}",
            font,
            new LabelConfig(
                FontSize: 12,
                TextColor: EditorColors.TextMuted,
                HorizontalAlign: TextAlignH.Left
            ));
    }

    private void CreatePermissionsSection(Entity parent, LoadedPlugin plugin)
    {
        if (editorWorld is null)
        {
            return;
        }

        var permissions = plugin.Manifest.Permissions;
        var permList = new List<string>();

        if (permissions?.Required is not null)
        {
            permList.AddRange(permissions.Required);
        }

        var permsText = permList.Count == 0 ? "None" : string.Join(", ", permList);

        WidgetFactory.CreateLabel(
            editorWorld,
            parent,
            "PluginPermissions",
            $"Permissions: {permsText}",
            font,
            new LabelConfig(
                FontSize: 12,
                TextColor: EditorColors.TextMuted,
                HorizontalAlign: TextAlignH.Left
            ));
    }

    private void CreateControlButtons(Entity parent, LoadedPlugin plugin)
    {
        if (editorWorld is null)
        {
            return;
        }

        // Create button container
        var buttonContainer = WidgetFactory.CreatePanel(
            editorWorld,
            parent,
            "PluginControlButtons",
            new PanelConfig(
                Height: 40,
                Direction: LayoutDirection.Horizontal,
                MainAxisAlign: LayoutAlign.Start,
                CrossAxisAlign: LayoutAlign.Center,
                BackgroundColor: Vector4.Zero,
                Spacing: 8
            ));

        ref var containerRect = ref editorWorld.Get<UIRect>(buttonContainer);
        containerRect.WidthMode = UISizeMode.Fill;

        // Enable/Disable button
        if (plugin.SupportsDisable)
        {
            var isEnabled = plugin.State == PluginState.Enabled;
            var buttonText = isEnabled ? "Disable" : "Enable";

            var enableButton = WidgetFactory.CreateButton(
                editorWorld,
                buttonContainer,
                "PluginEnableButton",
                buttonText,
                font,
                new ButtonConfig(
                    Width: 80,
                    Height: 28,
                    BackgroundColor: EditorColors.Primary
                ));

            editorWorld.Add(enableButton, new PluginControlButtonData
            {
                PluginId = plugin.Manifest.Id,
                Action = isEnabled ? PluginButtonAction.Disable : PluginButtonAction.Enable
            });
        }
        else
        {
            // Show "Restart Required" label for plugins that don't support disable
            WidgetFactory.CreateLabel(
                editorWorld,
                buttonContainer,
                "PluginRestartRequired",
                "Restart Required",
                font,
                new LabelConfig(
                    FontSize: 11,
                    TextColor: new Vector4(0.8f, 0.7f, 0.3f, 1f),
                    HorizontalAlign: TextAlignH.Left
                ));
        }

        // Reload button (for hot-reload capable plugins)
        if (plugin.SupportsHotReload && plugin.State == PluginState.Enabled)
        {
            var reloadButton = WidgetFactory.CreateButton(
                editorWorld,
                buttonContainer,
                "PluginReloadButton",
                "Reload",
                font,
                new ButtonConfig(
                    Width: 70,
                    Height: 28,
                    BackgroundColor: EditorColors.MediumPanel
                ));

            editorWorld.Add(reloadButton, new PluginControlButtonData
            {
                PluginId = plugin.Manifest.Id,
                Action = PluginButtonAction.Reload
            });
        }
    }

    private void CreateErrorSection(Entity parent, LoadedPlugin plugin)
    {
        if (editorWorld is null || string.IsNullOrEmpty(plugin.ErrorMessage))
        {
            return;
        }

        // Create error card
        var (_, content) = WidgetFactory.CreateCard(
            editorWorld,
            parent,
            "Error",
            font,
            new CardConfig(
                TitleBarColor: new Vector4(0.6f, 0.2f, 0.2f, 1f),
                ContentColor: new Vector4(0.3f, 0.15f, 0.15f, 1f),
                BorderColor: new Vector4(0.7f, 0.3f, 0.3f, 1f),
                BorderWidth: 1
            ));

        // Add error message
        WidgetFactory.CreateLabel(
            editorWorld,
            content,
            "PluginErrorMessage",
            plugin.ErrorMessage,
            font,
            new LabelConfig(
                FontSize: 12,
                TextColor: new Vector4(1f, 0.8f, 0.8f, 1f),
                HorizontalAlign: TextAlignH.Left
            ));
    }
}

/// <summary>
/// Component storing the state of the plugin manager panel.
/// </summary>
internal struct PluginManagerPanelState : IComponent
{
    /// <summary>
    /// The container entity for the plugin list.
    /// </summary>
    public Entity PluginListContainer;

    /// <summary>
    /// The container entity for the plugin details.
    /// </summary>
    public Entity DetailsContainer;

    /// <summary>
    /// The ID of the currently selected plugin, if any.
    /// </summary>
    public string? SelectedPluginId;
}

/// <summary>
/// Component storing data for a plugin list item.
/// </summary>
internal struct PluginListItemData : IComponent
{
    /// <summary>
    /// The plugin ID this item represents.
    /// </summary>
    public string PluginId;
}

/// <summary>
/// Component storing data for a plugin control button.
/// </summary>
internal struct PluginControlButtonData : IComponent
{
    /// <summary>
    /// The plugin ID this button controls.
    /// </summary>
    public string PluginId;

    /// <summary>
    /// The action this button performs.
    /// </summary>
    public PluginButtonAction Action;
}

/// <summary>
/// Actions available for plugin control buttons.
/// </summary>
internal enum PluginButtonAction
{
    /// <summary>Enable the plugin.</summary>
    Enable,

    /// <summary>Disable the plugin.</summary>
    Disable,

    /// <summary>Reload the plugin (hot reload).</summary>
    Reload
}
