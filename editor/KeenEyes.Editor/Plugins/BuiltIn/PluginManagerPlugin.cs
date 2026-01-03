// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using System.Numerics;
using KeenEyes.Editor.Abstractions;
using KeenEyes.Editor.Abstractions.Capabilities;
using KeenEyes.Editor.Application;
using KeenEyes.Editor.Plugins.Installation;
using KeenEyes.Editor.Plugins.NuGet;
using KeenEyes.Editor.Plugins.Registry;
using KeenEyes.Editor.Plugins.Settings;
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
/// The plugin manager panel has three tabs:
/// <list type="bullet">
/// <item><b>Installed</b> - View and manage installed plugins</item>
/// <item><b>Browse</b> - Search and install new plugins from NuGet sources</item>
/// <item><b>Settings</b> - Configure plugin paths, sources, and options</item>
/// </list>
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
                MinWidth = 500,
                MinHeight = 400,
                DefaultWidth = 600,
                DefaultHeight = 500,
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
/// Implementation of the plugin manager panel with tabs.
/// </summary>
internal sealed class PluginManagerPanelImpl : IEditorPanel
{
    private const float RefreshInterval = 1.0f;
    private const float SearchDebounceTime = 0.3f;

    private Entity rootEntity;
    private IWorld? editorWorld;
    private IEditorContext? editorContext;
    private EditorPluginManager? pluginManager;
    private FontHandle font;
    private float refreshTimer;

    // Services
    private INuGetClient? nugetClient;
    private PluginRegistry? registry;
    private PluginInstaller? installer;
    private PluginUninstaller? uninstaller;
    private GlobalPluginSettings? settings;

    /// <inheritdoc />
    public Entity RootEntity => rootEntity;

    /// <inheritdoc />
    public void Initialize(PanelContext panelContext)
    {
        editorWorld = panelContext.EditorWorld;
        editorContext = panelContext.EditorContext;
        rootEntity = panelContext.Parent;

        // Get the plugin manager from the editor context
        if (editorContext.TryGetExtension<EditorPluginManager>(out var manager))
        {
            pluginManager = manager;
        }

        // Get services
        if (editorContext.TryGetExtension<INuGetClient>(out var nuget))
        {
            nugetClient = nuget;
        }

        if (editorContext.TryGetExtension<PluginRegistry>(out var reg))
        {
            registry = reg;
        }

        // Create installer and uninstaller if we have the required services
        if (nugetClient is not null && registry is not null)
        {
            installer = new PluginInstaller(nugetClient, registry);
            uninstaller = new PluginUninstaller(registry);
        }

        // Load settings
        settings = new GlobalPluginSettings();
        settings.Load();

        // TODO: Get font from panel context or resource manager
        font = default;

        CreatePanelUI();
        SubscribeToEvents();
        RefreshInstalledPluginList();
    }

    private void SubscribeToEvents()
    {
        if (editorWorld is null)
        {
            return;
        }

        // Subscribe to click events
        editorWorld.Subscribe<UIClickEvent>(OnClick);
        editorWorld.Subscribe<UITextChangedEvent>(OnTextChanged);
        editorWorld.Subscribe<UIModalResultEvent>(OnModalResult);
    }

    private void OnClick(UIClickEvent e)
    {
        if (editorWorld is null || pluginManager is null)
        {
            return;
        }

        var clickedEntity = e.Element;

        // Check if a plugin list item was clicked (Installed tab)
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

        // Check if a browse result item was clicked
        if (editorWorld.Has<BrowseResultItemData>(clickedEntity))
        {
            ref readonly var browseData = ref editorWorld.Get<BrowseResultItemData>(clickedEntity);
            SelectBrowseResult(browseData.PackageId);
            return;
        }

        // Check if an install button was clicked
        if (editorWorld.Has<InstallButtonData>(clickedEntity))
        {
            ref readonly var installData = ref editorWorld.Get<InstallButtonData>(clickedEntity);
            StartInstallPackage(installData.PackageId, installData.Version);
            return;
        }

        // Check if a settings control was clicked
        if (editorWorld.Has<SettingsControlData>(clickedEntity))
        {
            ref readonly var settingsData = ref editorWorld.Get<SettingsControlData>(clickedEntity);
            ExecuteSettingsAction(settingsData);
            return;
        }
    }

    private void OnTextChanged(UITextChangedEvent e)
    {
        if (editorWorld is null)
        {
            return;
        }

        // Check if the search box changed
        if (editorWorld.Has<SearchBoxData>(e.Element))
        {
            if (!editorWorld.Has<PluginManagerPanelState>(rootEntity))
            {
                return;
            }

            ref var state = ref editorWorld.Get<PluginManagerPanelState>(rootEntity);
            state.SearchQuery = e.NewText;
            state.SearchDebounceTimer = SearchDebounceTime;
        }
    }

    private void OnModalResult(UIModalResultEvent e)
    {
        if (editorWorld is null || settings is null)
        {
            return;
        }

        if (!editorWorld.Has<PluginManagerPanelState>(rootEntity))
        {
            return;
        }

        ref readonly var state = ref editorWorld.Get<PluginManagerPanelState>(rootEntity);

        // Check if this is the Add Source dialog
        if (e.Modal == state.AddSourceDialog)
        {
            HandleAddSourceDialogResult(e.Modal, e.Result);
            return;
        }

        // Check if this is the Add Path dialog
        if (e.Modal == state.AddPathDialog)
        {
            HandleAddPathDialogResult(e.Modal, e.Result);
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
            case PluginButtonAction.Uninstall:
                StartUninstallPlugin(pluginId);
                break;
        }

        // Refresh the UI after action
        RefreshInstalledPluginList();

        // Re-select the current plugin to update the details view
        if (editorWorld is not null && editorWorld.Has<PluginManagerPanelState>(rootEntity))
        {
            ref readonly var state = ref editorWorld.Get<PluginManagerPanelState>(rootEntity);
            if (state.SelectedPluginId is not null)
            {
                var plugin = pluginManager.GetDynamicPlugin(state.SelectedPluginId);
                if (plugin is not null)
                {
                    ShowInstalledPluginDetails(plugin);
                }
            }
        }
    }

    private void ExecuteSettingsAction(SettingsControlData data)
    {
        if (settings is null)
        {
            return;
        }

        switch (data.Action)
        {
            case SettingsAction.ToggleHotReload:
                settings.SetHotReloadEnabled(!settings.HotReload.Enabled);
                settings.Save();
                RefreshSettingsTab();
                break;
            case SettingsAction.ToggleDeveloperMode:
                settings.SetDeveloperModeEnabled(!settings.Developer.Enabled);
                settings.Save();
                RefreshSettingsTab();
                break;
            case SettingsAction.ToggleVerboseLogging:
                settings.SetVerboseLoggingEnabled(!settings.Developer.VerboseLogging);
                settings.Save();
                RefreshSettingsTab();
                break;
            case SettingsAction.ToggleCodeSigning:
                settings.SetCodeSigningRequired(!settings.Security.RequireCodeSigning);
                settings.Save();
                RefreshSettingsTab();
                break;
            case SettingsAction.TogglePermissionSystem:
                settings.SetPermissionSystemEnabled(!settings.Security.EnablePermissionSystem);
                settings.Save();
                RefreshSettingsTab();
                break;
            case SettingsAction.AddSource:
                ShowAddSourceDialog();
                break;
            case SettingsAction.RemoveSource:
                if (!string.IsNullOrEmpty(data.TargetId))
                {
                    settings.RemoveSource(data.TargetId);
                    settings.Save();
                    RefreshSettingsTab();
                }
                break;
            case SettingsAction.AddSearchPath:
                ShowAddPathDialog();
                break;
            case SettingsAction.RemoveSearchPath:
                if (!string.IsNullOrEmpty(data.TargetId))
                {
                    settings.RemoveSearchPath(data.TargetId);
                    settings.Save();
                    RefreshSettingsTab();
                }
                break;
        }
    }

    /// <inheritdoc />
    public void Update(float deltaTime)
    {
        if (editorWorld is null)
        {
            return;
        }

        if (!editorWorld.Has<PluginManagerPanelState>(rootEntity))
        {
            return;
        }

        ref var state = ref editorWorld.Get<PluginManagerPanelState>(rootEntity);

        // Handle periodic refresh for installed tab
        refreshTimer += deltaTime;
        if (refreshTimer >= RefreshInterval)
        {
            refreshTimer = 0f;
            RefreshInstalledPluginList();
        }

        // Handle search debounce
        if (state.SearchDebounceTimer > 0)
        {
            state.SearchDebounceTimer -= deltaTime;
            if (state.SearchDebounceTimer <= 0)
            {
                StartSearch(state.SearchQuery ?? string.Empty);
            }
        }

        // Handle async operation updates
        UpdateAsyncOperation(ref state);
    }

    private void UpdateAsyncOperation(ref PluginManagerPanelState state)
    {
        if (state.AsyncTask is null || !state.AsyncTask.IsCompleted)
        {
            return;
        }

        if (state.AsyncTask.IsFaulted)
        {
            state.AsyncStatus = AsyncOperationStatus.Failed;
            state.AsyncErrorMessage = state.AsyncTask.Exception?.InnerException?.Message ?? "Unknown error";
        }
        else if (state.AsyncTask.IsCanceled)
        {
            state.AsyncStatus = AsyncOperationStatus.Idle;
        }
        else
        {
            state.AsyncStatus = AsyncOperationStatus.Completed;
            HandleAsyncCompletion(state.AsyncType, state.AsyncTask);
        }

        state.AsyncTask = null;
        state.AsyncType = AsyncOperationType.None;

        // Update UI to reflect completion
        RefreshBrowseTab();
    }

    private void HandleAsyncCompletion(AsyncOperationType type, Task task)
    {
        switch (type)
        {
            case AsyncOperationType.Search:
                if (task is Task<IReadOnlyList<PackageSearchResult>> searchTask)
                {
                    UpdateSearchResults(searchTask.Result);
                }
                break;
            case AsyncOperationType.Install:
                if (task is Task<InstallationResult> installTask && installTask.Result.Success)
                {
                    registry?.Load();
                    RefreshInstalledPluginList();
                }
                break;
            case AsyncOperationType.Uninstall:
                if (task is Task<UninstallResult> uninstallTask && uninstallTask.Result.Success)
                {
                    RefreshInstalledPluginList();
                }
                break;
        }
    }

    /// <inheritdoc />
    public void Shutdown()
    {
        // Clean up any open dialogs
        CleanupAddSourceDialog();
        CleanupAddPathDialog();

        // Panel cleanup is handled by the dock system
    }

    private void CreatePanelUI()
    {
        if (editorWorld is null)
        {
            return;
        }

        // Create tab view with three tabs
        var tabs = new[]
        {
            new TabConfig("Installed", MinWidth: 80),
            new TabConfig("Browse", MinWidth: 70),
            new TabConfig("Settings", MinWidth: 70)
        };

        var tabViewConfig = new TabViewConfig
        {
            TabBarHeight = 32,
            TabSpacing = 4
        };

        var (tabView, contentPanels) = WidgetFactory.CreateTabView(
            editorWorld,
            rootEntity,
            "PluginManagerTabs",
            tabs,
            font,
            tabViewConfig);

        ref var tabRect = ref editorWorld.Get<UIRect>(tabView);
        tabRect.WidthMode = UISizeMode.Fill;
        tabRect.HeightMode = UISizeMode.Fill;

        // Store panel state
        editorWorld.Add(rootEntity, new PluginManagerPanelState
        {
            TabView = tabView,
            InstalledTabContent = contentPanels[0],
            BrowseTabContent = contentPanels[1],
            SettingsTabContent = contentPanels[2],
            SelectedPluginId = null,
            SearchResults = []
        });

        // Build each tab's content
        BuildInstalledTabContent(contentPanels[0]);
        BuildBrowseTabContent(contentPanels[1]);
        BuildSettingsTabContent(contentPanels[2]);
    }

    #region Installed Tab

    private void BuildInstalledTabContent(Entity parent)
    {
        if (editorWorld is null)
        {
            return;
        }

        // Create the main split container (horizontal)
        var splitContainer = WidgetFactory.CreatePanel(
            editorWorld,
            parent,
            "InstalledSplitContainer",
            new PanelConfig(
                Direction: LayoutDirection.Horizontal,
                BackgroundColor: EditorColors.DarkPanel
            ));

        ref var splitRect = ref editorWorld.Get<UIRect>(splitContainer);
        splitRect.WidthMode = UISizeMode.Fill;
        splitRect.HeightMode = UISizeMode.Fill;

        // Create left panel (plugin list)
        var leftPanel = CreateInstalledPluginListPanel(splitContainer);

        // Create divider
        WidgetFactory.CreateDivider(editorWorld, splitContainer, "InstalledDivider", new DividerConfig(
            Orientation: LayoutDirection.Vertical,
            Thickness: 1,
            Color: new Vector4(0.3f, 0.3f, 0.35f, 1f)
        ));

        // Create right panel (details)
        var rightPanel = CreateInstalledDetailsPanel(splitContainer);

        // Update state with containers
        if (editorWorld.Has<PluginManagerPanelState>(rootEntity))
        {
            ref var state = ref editorWorld.Get<PluginManagerPanelState>(rootEntity);
            state.InstalledListContainer = leftPanel;
            state.InstalledDetailsContainer = rightPanel;
        }
    }

    private Entity CreateInstalledPluginListPanel(Entity parent)
    {
        if (editorWorld is null)
        {
            return Entity.Null;
        }

        // Create list container
        var listPanel = WidgetFactory.CreatePanel(
            editorWorld,
            parent,
            "InstalledListPanel",
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
            "InstalledListHeader",
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
            "InstalledListTitle",
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
            "InstalledListItems",
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

    private Entity CreateInstalledDetailsPanel(Entity parent)
    {
        if (editorWorld is null)
        {
            return Entity.Null;
        }

        // Create details container
        var detailsPanel = WidgetFactory.CreatePanel(
            editorWorld,
            parent,
            "InstalledDetailsPanel",
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
            "InstalledDetailsPlaceholder",
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

    private void RefreshInstalledPluginList()
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
        var listContainer = state.InstalledListContainer;

        if (!listContainer.IsValid)
        {
            return;
        }

        // Clear existing plugin items
        ClearChildren(listContainer);

        // Add plugin items
        foreach (var plugin in pluginManager.GetDynamicPlugins())
        {
            CreateInstalledPluginListItem(listContainer, plugin);
        }
    }

    private void CreateInstalledPluginListItem(Entity parent, LoadedPlugin plugin)
    {
        if (editorWorld is null)
        {
            return;
        }

        var itemId = $"InstalledItem_{plugin.Manifest.Id}";

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

        ShowInstalledPluginDetails(plugin);
    }

    private void ShowInstalledPluginDetails(LoadedPlugin plugin)
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
        var detailsContainer = state.InstalledDetailsContainer;

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
            "InstalledDetailsHeader",
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
            "InstalledPluginName",
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
            "InstalledPluginSubtitle",
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
                "InstalledPluginDescription",
                plugin.Manifest.Description,
                font,
                new LabelConfig(
                    FontSize: 13,
                    TextColor: EditorColors.TextLight,
                    HorizontalAlign: TextAlignH.Left
                ));
        }

        // Divider
        WidgetFactory.CreateDivider(editorWorld, detailsContainer, "InstalledDetailsDivider", new DividerConfig(
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
            "InstalledPluginDependencies",
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
            "InstalledPluginPermissions",
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
            "InstalledControlButtons",
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
                "InstalledEnableButton",
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
                "InstalledRestartRequired",
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
                "InstalledReloadButton",
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

        // Uninstall button
        var uninstallButton = WidgetFactory.CreateButton(
            editorWorld,
            buttonContainer,
            "InstalledUninstallButton",
            "Uninstall",
            font,
            new ButtonConfig(
                Width: 80,
                Height: 28,
                BackgroundColor: new Vector4(0.6f, 0.2f, 0.2f, 1f)
            ));

        editorWorld.Add(uninstallButton, new PluginControlButtonData
        {
            PluginId = plugin.Manifest.Id,
            Action = PluginButtonAction.Uninstall
        });
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
            "InstalledErrorMessage",
            plugin.ErrorMessage,
            font,
            new LabelConfig(
                FontSize: 12,
                TextColor: new Vector4(1f, 0.8f, 0.8f, 1f),
                HorizontalAlign: TextAlignH.Left
            ));
    }

    private void StartUninstallPlugin(string pluginId)
    {
        if (uninstaller is null || editorWorld is null)
        {
            return;
        }

        if (!editorWorld.Has<PluginManagerPanelState>(rootEntity))
        {
            return;
        }

        // For now, do synchronous uninstall (it's fast, just registry operations)
        var result = uninstaller.Uninstall(pluginId, force: false);

        if (!result.Success)
        {
            // TODO: Show error toast or dialog
            return;
        }

        RefreshInstalledPluginList();
    }

    #endregion

    #region Browse Tab

    private void BuildBrowseTabContent(Entity parent)
    {
        if (editorWorld is null)
        {
            return;
        }

        // Add layout to parent
        editorWorld.Add(parent, new UILayout
        {
            Direction = LayoutDirection.Vertical,
            MainAxisAlign = LayoutAlign.Start,
            CrossAxisAlign = LayoutAlign.Start,
            Spacing = 0
        });

        // Create search bar
        _ = CreateSearchBar(parent);

        // Create main content split (results list | details)
        var splitContainer = WidgetFactory.CreatePanel(
            editorWorld,
            parent,
            "BrowseSplitContainer",
            new PanelConfig(
                Direction: LayoutDirection.Horizontal,
                BackgroundColor: EditorColors.DarkPanel
            ));

        ref var splitRect = ref editorWorld.Get<UIRect>(splitContainer);
        splitRect.WidthMode = UISizeMode.Fill;
        splitRect.HeightMode = UISizeMode.Fill;

        // Create results list panel
        var resultsPanel = CreateBrowseResultsPanel(splitContainer);

        // Create divider
        WidgetFactory.CreateDivider(editorWorld, splitContainer, "BrowseDivider", new DividerConfig(
            Orientation: LayoutDirection.Vertical,
            Thickness: 1,
            Color: new Vector4(0.3f, 0.3f, 0.35f, 1f)
        ));

        // Create details panel
        var detailsPanel = CreateBrowseDetailsPanel(splitContainer);

        // Update state
        if (editorWorld.Has<PluginManagerPanelState>(rootEntity))
        {
            ref var state = ref editorWorld.Get<PluginManagerPanelState>(rootEntity);
            state.BrowseResultsContainer = resultsPanel;
            state.BrowseDetailsContainer = detailsPanel;
        }
    }

    private Entity CreateSearchBar(Entity parent)
    {
        if (editorWorld is null)
        {
            return Entity.Null;
        }

        var searchContainer = WidgetFactory.CreatePanel(
            editorWorld,
            parent,
            "BrowseSearchBar",
            new PanelConfig(
                Height: 40,
                Direction: LayoutDirection.Horizontal,
                MainAxisAlign: LayoutAlign.Start,
                CrossAxisAlign: LayoutAlign.Center,
                BackgroundColor: EditorColors.MediumPanel,
                Padding: UIEdges.All(8),
                Spacing: 8
            ));

        ref var containerRect = ref editorWorld.Get<UIRect>(searchContainer);
        containerRect.WidthMode = UISizeMode.Fill;

        // Search label
        WidgetFactory.CreateLabel(
            editorWorld,
            searchContainer,
            "BrowseSearchLabel",
            "Search:",
            font,
            new LabelConfig(
                FontSize: 12,
                TextColor: EditorColors.TextLight,
                HorizontalAlign: TextAlignH.Left
            ));

        // Search input
        var searchInput = WidgetFactory.CreateTextField(
            editorWorld,
            searchContainer,
            "BrowseSearchInput",
            font,
            new TextFieldConfig(
                Width: 300,
                Height: 24,
                PlaceholderText: "Search plugins..."
            ));

        editorWorld.Add(searchInput, new SearchBoxData());

        // Refresh button
        _ = WidgetFactory.CreateButton(
            editorWorld,
            searchContainer,
            "BrowseRefreshButton",
            "Refresh",
            font,
            new ButtonConfig(
                Width: 70,
                Height: 24,
                BackgroundColor: EditorColors.MediumPanel
            ));

        return searchContainer;
    }

    private Entity CreateBrowseResultsPanel(Entity parent)
    {
        if (editorWorld is null)
        {
            return Entity.Null;
        }

        var resultsPanel = WidgetFactory.CreatePanel(
            editorWorld,
            parent,
            "BrowseResultsPanel",
            new PanelConfig(
                Width: 200,
                Direction: LayoutDirection.Vertical,
                BackgroundColor: EditorColors.MediumPanel
            ));

        ref var resultsRect = ref editorWorld.Get<UIRect>(resultsPanel);
        resultsRect.HeightMode = UISizeMode.Fill;

        // Header
        var header = WidgetFactory.CreatePanel(
            editorWorld,
            resultsPanel,
            "BrowseResultsHeader",
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
            "BrowseResultsTitle",
            "AVAILABLE",
            font,
            new LabelConfig(
                FontSize: 11,
                TextColor: EditorColors.TextMuted,
                HorizontalAlign: TextAlignH.Left
            ));

        // Results list container
        var listContainer = WidgetFactory.CreatePanel(
            editorWorld,
            resultsPanel,
            "BrowseResultsItems",
            new PanelConfig(
                Direction: LayoutDirection.Vertical,
                BackgroundColor: EditorColors.MediumPanel,
                Padding: UIEdges.All(4)
            ));

        ref var listRect = ref editorWorld.Get<UIRect>(listContainer);
        listRect.WidthMode = UISizeMode.Fill;
        listRect.HeightMode = UISizeMode.Fill;

        return listContainer;
    }

    private Entity CreateBrowseDetailsPanel(Entity parent)
    {
        if (editorWorld is null)
        {
            return Entity.Null;
        }

        var detailsPanel = WidgetFactory.CreatePanel(
            editorWorld,
            parent,
            "BrowseDetailsPanel",
            new PanelConfig(
                Direction: LayoutDirection.Vertical,
                BackgroundColor: EditorColors.DarkPanel,
                Padding: UIEdges.All(12)
            ));

        ref var detailsRect = ref editorWorld.Get<UIRect>(detailsPanel);
        detailsRect.WidthMode = UISizeMode.Fill;
        detailsRect.HeightMode = UISizeMode.Fill;

        // Placeholder
        WidgetFactory.CreateLabel(
            editorWorld,
            detailsPanel,
            "BrowseDetailsPlaceholder",
            "Search for plugins to browse available packages",
            font,
            new LabelConfig(
                FontSize: 13,
                TextColor: EditorColors.TextMuted,
                HorizontalAlign: TextAlignH.Center,
                VerticalAlign: TextAlignV.Middle
            ));

        return detailsPanel;
    }

    private void StartSearch(string query)
    {
        if (nugetClient is null || editorWorld is null || string.IsNullOrWhiteSpace(query))
        {
            return;
        }

        if (!editorWorld.Has<PluginManagerPanelState>(rootEntity))
        {
            return;
        }

        ref var state = ref editorWorld.Get<PluginManagerPanelState>(rootEntity);

        // Start async search
        state.AsyncType = AsyncOperationType.Search;
        state.AsyncStatus = AsyncOperationStatus.Running;
        state.AsyncTask = nugetClient.SearchAsync(query, settings?.GetDefaultSourceUrl(), take: 50);
    }

    private void UpdateSearchResults(IReadOnlyList<PackageSearchResult> results)
    {
        if (editorWorld is null)
        {
            return;
        }

        if (!editorWorld.Has<PluginManagerPanelState>(rootEntity))
        {
            return;
        }

        ref var state = ref editorWorld.Get<PluginManagerPanelState>(rootEntity);
        state.SearchResults = results.ToList();

        RefreshBrowseTab();
    }

    private void RefreshBrowseTab()
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
        var resultsContainer = state.BrowseResultsContainer;

        if (!resultsContainer.IsValid)
        {
            return;
        }

        // Clear existing results
        ClearChildren(resultsContainer);

        // Add result items
        foreach (var result in state.SearchResults)
        {
            CreateBrowseResultItem(resultsContainer, result);
        }
    }

    private void CreateBrowseResultItem(Entity parent, PackageSearchResult result)
    {
        if (editorWorld is null)
        {
            return;
        }

        var itemId = $"BrowseItem_{result.PackageId}";

        var item = WidgetFactory.CreatePanel(
            editorWorld,
            parent,
            itemId,
            new PanelConfig(
                Height: 40,
                Direction: LayoutDirection.Vertical,
                MainAxisAlign: LayoutAlign.Center,
                BackgroundColor: Vector4.Zero,
                Padding: UIEdges.Symmetric(8, 4)
            ));

        ref var itemRect = ref editorWorld.Get<UIRect>(item);
        itemRect.WidthMode = UISizeMode.Fill;

        editorWorld.Add(item, new UIInteractable
        {
            CanClick = true,
            CanFocus = true
        });

        editorWorld.Add(item, new BrowseResultItemData
        {
            PackageId = result.PackageId
        });

        // Package name
        WidgetFactory.CreateLabel(
            editorWorld,
            item,
            $"{itemId}_Name",
            result.PackageId,
            font,
            new LabelConfig(
                FontSize: 12,
                TextColor: EditorColors.TextLight,
                HorizontalAlign: TextAlignH.Left
            ));

        // Version and downloads
        var downloads = result.DownloadCount.HasValue
            ? FormatDownloadCount(result.DownloadCount.Value)
            : "";
        var info = $"v{result.LatestVersion}";
        if (!string.IsNullOrEmpty(downloads))
        {
            info += $" | {downloads}";
        }

        WidgetFactory.CreateLabel(
            editorWorld,
            item,
            $"{itemId}_Info",
            info,
            font,
            new LabelConfig(
                FontSize: 10,
                TextColor: EditorColors.TextMuted,
                HorizontalAlign: TextAlignH.Left
            ));
    }

    private static string FormatDownloadCount(long count)
    {
        return count switch
        {
            >= 1_000_000_000 => $"{count / 1_000_000_000.0:F1}B",
            >= 1_000_000 => $"{count / 1_000_000.0:F1}M",
            >= 1_000 => $"{count / 1_000.0:F1}K",
            _ => count.ToString()
        };
    }

    private void SelectBrowseResult(string packageId)
    {
        if (editorWorld is null)
        {
            return;
        }

        if (!editorWorld.Has<PluginManagerPanelState>(rootEntity))
        {
            return;
        }

        ref var state = ref editorWorld.Get<PluginManagerPanelState>(rootEntity);
        state.SelectedBrowsePackageId = packageId;

        var result = state.SearchResults.FirstOrDefault(r => r.PackageId == packageId);
        if (result is not null)
        {
            ShowBrowseDetails(result);
        }
    }

    private void ShowBrowseDetails(PackageSearchResult result)
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
        var detailsContainer = state.BrowseDetailsContainer;

        if (!detailsContainer.IsValid)
        {
            return;
        }

        ClearChildren(detailsContainer);

        // Header
        var header = WidgetFactory.CreatePanel(
            editorWorld,
            detailsContainer,
            "BrowseDetailsHeader",
            new PanelConfig(
                Height: 60,
                Direction: LayoutDirection.Vertical,
                MainAxisAlign: LayoutAlign.Start,
                BackgroundColor: Vector4.Zero
            ));

        ref var headerRect = ref editorWorld.Get<UIRect>(header);
        headerRect.WidthMode = UISizeMode.Fill;

        // Package name
        WidgetFactory.CreateLabel(
            editorWorld,
            header,
            "BrowsePackageName",
            result.PackageId,
            font,
            new LabelConfig(
                FontSize: 18,
                TextColor: EditorColors.TextWhite,
                HorizontalAlign: TextAlignH.Left
            ));

        // Version and author
        var subtitle = $"v{result.LatestVersion}";
        if (!string.IsNullOrEmpty(result.Authors))
        {
            subtitle += $" by {result.Authors}";
        }

        WidgetFactory.CreateLabel(
            editorWorld,
            header,
            "BrowsePackageSubtitle",
            subtitle,
            font,
            new LabelConfig(
                FontSize: 12,
                TextColor: EditorColors.TextMuted,
                HorizontalAlign: TextAlignH.Left
            ));

        // Description
        if (!string.IsNullOrEmpty(result.Description))
        {
            WidgetFactory.CreateLabel(
                editorWorld,
                detailsContainer,
                "BrowsePackageDescription",
                result.Description,
                font,
                new LabelConfig(
                    FontSize: 13,
                    TextColor: EditorColors.TextLight,
                    HorizontalAlign: TextAlignH.Left
                ));
        }

        // Install button
        var buttonContainer = WidgetFactory.CreatePanel(
            editorWorld,
            detailsContainer,
            "BrowseInstallButtonContainer",
            new PanelConfig(
                Height: 40,
                Direction: LayoutDirection.Horizontal,
                MainAxisAlign: LayoutAlign.Start,
                CrossAxisAlign: LayoutAlign.Center,
                BackgroundColor: Vector4.Zero,
                Spacing: 8
            ));

        ref var buttonRect = ref editorWorld.Get<UIRect>(buttonContainer);
        buttonRect.WidthMode = UISizeMode.Fill;

        // Check if already installed
        var isInstalled = registry?.IsInstalled(result.PackageId) ?? false;

        var installButton = WidgetFactory.CreateButton(
            editorWorld,
            buttonContainer,
            "BrowseInstallButton",
            isInstalled ? "Installed" : "Install",
            font,
            new ButtonConfig(
                Width: 80,
                Height: 28,
                BackgroundColor: isInstalled ? EditorColors.MediumPanel : EditorColors.Primary
            ));

        if (!isInstalled)
        {
            editorWorld.Add(installButton, new InstallButtonData
            {
                PackageId = result.PackageId,
                Version = result.LatestVersion
            });
        }
    }

    private void StartInstallPackage(string packageId, string version)
    {
        if (installer is null || editorWorld is null)
        {
            return;
        }

        if (!editorWorld.Has<PluginManagerPanelState>(rootEntity))
        {
            return;
        }

        ref var state = ref editorWorld.Get<PluginManagerPanelState>(rootEntity);

        // Start async install
        var source = settings?.GetDefaultSourceUrl();
        var installTask = Task.Run(async () =>
        {
            var plan = await installer.CreatePlanAsync(packageId, version, source);
            if (!plan.IsValid)
            {
                return InstallationResult.Failed(plan.ErrorMessage ?? "Failed to create installation plan");
            }

            return await installer.ExecuteAsync(plan, null);
        });

        state.AsyncType = AsyncOperationType.Install;
        state.AsyncStatus = AsyncOperationStatus.Running;
        state.AsyncTask = installTask;
    }

    #endregion

    #region Settings Tab

    private void BuildSettingsTabContent(Entity parent)
    {
        if (editorWorld is null || settings is null)
        {
            return;
        }

        // Add layout to parent and make it scrollable
        editorWorld.Add(parent, new UILayout
        {
            Direction = LayoutDirection.Vertical,
            MainAxisAlign = LayoutAlign.Start,
            CrossAxisAlign = LayoutAlign.Start,
            Spacing = 12
        });

        editorWorld.Add(parent, new UIStyle
        {
            Padding = UIEdges.All(12)
        });

        // Create settings sections
        CreateSourcesSection(parent);
        CreateSearchPathsSection(parent);
        CreateHotReloadSection(parent);
        CreateDeveloperSection(parent);
        CreateSecuritySection(parent);

        // Store reference
        if (editorWorld.Has<PluginManagerPanelState>(rootEntity))
        {
            ref var state = ref editorWorld.Get<PluginManagerPanelState>(rootEntity);
            state.SettingsContent = parent;
        }
    }

    private void CreateSourcesSection(Entity parent)
    {
        if (editorWorld is null || settings is null)
        {
            return;
        }

        // Section header
        WidgetFactory.CreateLabel(
            editorWorld,
            parent,
            "SettingsSourcesHeader",
            "PACKAGE SOURCES",
            font,
            new LabelConfig(
                FontSize: 11,
                TextColor: EditorColors.TextMuted,
                HorizontalAlign: TextAlignH.Left
            ));

        // Sources list
        foreach (var source in settings.GetSources())
        {
            CreateSourceRow(parent, source);
        }

        // Add source button
        var addButton = WidgetFactory.CreateButton(
            editorWorld,
            parent,
            "SettingsAddSourceButton",
            "+ Add Source",
            font,
            new ButtonConfig(
                Width: 100,
                Height: 24,
                BackgroundColor: EditorColors.MediumPanel
            ));

        editorWorld.Add(addButton, new SettingsControlData
        {
            Action = SettingsAction.AddSource
        });
    }

    private void CreateSourceRow(Entity parent, PluginSourceSettings source)
    {
        if (editorWorld is null)
        {
            return;
        }

        var row = WidgetFactory.CreatePanel(
            editorWorld,
            parent,
            $"SettingsSource_{source.Name}",
            new PanelConfig(
                Height: 24,
                Direction: LayoutDirection.Horizontal,
                MainAxisAlign: LayoutAlign.Start,
                CrossAxisAlign: LayoutAlign.Center,
                BackgroundColor: Vector4.Zero,
                Spacing: 8
            ));

        ref var rowRect = ref editorWorld.Get<UIRect>(row);
        rowRect.WidthMode = UISizeMode.Fill;

        // Checkbox
        var checkColor = source.IsEnabled
            ? new Vector4(0.3f, 0.7f, 0.3f, 1f)
            : new Vector4(0.5f, 0.5f, 0.5f, 1f);

        WidgetFactory.CreateLabel(
            editorWorld,
            row,
            $"SettingsSource_{source.Name}_Check",
            source.IsEnabled ? "[x]" : "[ ]",
            font,
            new LabelConfig(
                FontSize: 12,
                TextColor: checkColor,
                HorizontalAlign: TextAlignH.Left
            ));

        // Name
        var nameText = source.Name;
        if (source.IsDefault)
        {
            nameText += " (default)";
        }

        WidgetFactory.CreateLabel(
            editorWorld,
            row,
            $"SettingsSource_{source.Name}_Name",
            nameText,
            font,
            new LabelConfig(
                FontSize: 12,
                TextColor: EditorColors.TextLight,
                HorizontalAlign: TextAlignH.Left
            ));

        // Remove button (if not default)
        if (!source.IsDefault)
        {
            var removeButton = WidgetFactory.CreateButton(
                editorWorld,
                row,
                $"SettingsSource_{source.Name}_Remove",
                "Remove",
                font,
                new ButtonConfig(
                    Width: 60,
                    Height: 20,
                    BackgroundColor: new Vector4(0.5f, 0.2f, 0.2f, 1f)
                ));

            editorWorld.Add(removeButton, new SettingsControlData
            {
                Action = SettingsAction.RemoveSource,
                TargetId = source.Name
            });
        }
    }

    private void CreateSearchPathsSection(Entity parent)
    {
        if (editorWorld is null || settings is null)
        {
            return;
        }

        // Section header
        WidgetFactory.CreateLabel(
            editorWorld,
            parent,
            "SettingsPathsHeader",
            "SEARCH PATHS",
            font,
            new LabelConfig(
                FontSize: 11,
                TextColor: EditorColors.TextMuted,
                HorizontalAlign: TextAlignH.Left
            ));

        // Paths list
        var paths = settings.GetSearchPaths();
        if (paths.Count == 0)
        {
            WidgetFactory.CreateLabel(
                editorWorld,
                parent,
                "SettingsPathsEmpty",
                "No custom search paths configured",
                font,
                new LabelConfig(
                    FontSize: 12,
                    TextColor: EditorColors.TextMuted,
                    HorizontalAlign: TextAlignH.Left
                ));
        }
        else
        {
            foreach (var path in paths)
            {
                CreateSearchPathRow(parent, path);
            }
        }

        // Add path button
        var addButton = WidgetFactory.CreateButton(
            editorWorld,
            parent,
            "SettingsAddPathButton",
            "+ Add Path",
            font,
            new ButtonConfig(
                Width: 100,
                Height: 24,
                BackgroundColor: EditorColors.MediumPanel
            ));

        editorWorld.Add(addButton, new SettingsControlData
        {
            Action = SettingsAction.AddSearchPath
        });
    }

    private void CreateSearchPathRow(Entity parent, PluginSearchPath path)
    {
        if (editorWorld is null)
        {
            return;
        }

        var row = WidgetFactory.CreatePanel(
            editorWorld,
            parent,
            $"SettingsPath_{path.Path.GetHashCode()}",
            new PanelConfig(
                Height: 24,
                Direction: LayoutDirection.Horizontal,
                MainAxisAlign: LayoutAlign.Start,
                CrossAxisAlign: LayoutAlign.Center,
                BackgroundColor: Vector4.Zero,
                Spacing: 8
            ));

        ref var rowRect = ref editorWorld.Get<UIRect>(row);
        rowRect.WidthMode = UISizeMode.Fill;

        // Path label
        WidgetFactory.CreateLabel(
            editorWorld,
            row,
            $"SettingsPath_{path.Path.GetHashCode()}_Label",
            path.Path,
            font,
            new LabelConfig(
                FontSize: 12,
                TextColor: EditorColors.TextLight,
                HorizontalAlign: TextAlignH.Left
            ));

        // Remove button
        var removeButton = WidgetFactory.CreateButton(
            editorWorld,
            row,
            $"SettingsPath_{path.Path.GetHashCode()}_Remove",
            "X",
            font,
            new ButtonConfig(
                Width: 24,
                Height: 20,
                BackgroundColor: new Vector4(0.5f, 0.2f, 0.2f, 1f)
            ));

        editorWorld.Add(removeButton, new SettingsControlData
        {
            Action = SettingsAction.RemoveSearchPath,
            TargetId = path.Path
        });
    }

    private void CreateHotReloadSection(Entity parent)
    {
        if (editorWorld is null || settings is null)
        {
            return;
        }

        WidgetFactory.CreateLabel(
            editorWorld,
            parent,
            "SettingsHotReloadHeader",
            "HOT RELOAD",
            font,
            new LabelConfig(
                FontSize: 11,
                TextColor: EditorColors.TextMuted,
                HorizontalAlign: TextAlignH.Left
            ));

        CreateToggleRow(
            parent,
            "HotReload",
            "Enable hot reload for plugins",
            settings.HotReload.Enabled,
            SettingsAction.ToggleHotReload);
    }

    private void CreateDeveloperSection(Entity parent)
    {
        if (editorWorld is null || settings is null)
        {
            return;
        }

        WidgetFactory.CreateLabel(
            editorWorld,
            parent,
            "SettingsDeveloperHeader",
            "DEVELOPER OPTIONS",
            font,
            new LabelConfig(
                FontSize: 11,
                TextColor: EditorColors.TextMuted,
                HorizontalAlign: TextAlignH.Left
            ));

        CreateToggleRow(
            parent,
            "DeveloperMode",
            "Developer mode",
            settings.Developer.Enabled,
            SettingsAction.ToggleDeveloperMode);

        CreateToggleRow(
            parent,
            "VerboseLogging",
            "Verbose logging",
            settings.Developer.VerboseLogging,
            SettingsAction.ToggleVerboseLogging);
    }

    private void CreateSecuritySection(Entity parent)
    {
        if (editorWorld is null || settings is null)
        {
            return;
        }

        WidgetFactory.CreateLabel(
            editorWorld,
            parent,
            "SettingsSecurityHeader",
            "SECURITY",
            font,
            new LabelConfig(
                FontSize: 11,
                TextColor: EditorColors.TextMuted,
                HorizontalAlign: TextAlignH.Left
            ));

        CreateToggleRow(
            parent,
            "CodeSigning",
            "Require code signing",
            settings.Security.RequireCodeSigning,
            SettingsAction.ToggleCodeSigning);

        CreateToggleRow(
            parent,
            "PermissionSystem",
            "Enable permission system",
            settings.Security.EnablePermissionSystem,
            SettingsAction.TogglePermissionSystem);
    }

    private void CreateToggleRow(Entity parent, string id, string label, bool isEnabled, SettingsAction action)
    {
        if (editorWorld is null)
        {
            return;
        }

        var row = WidgetFactory.CreatePanel(
            editorWorld,
            parent,
            $"SettingsToggle_{id}",
            new PanelConfig(
                Height: 24,
                Direction: LayoutDirection.Horizontal,
                MainAxisAlign: LayoutAlign.Start,
                CrossAxisAlign: LayoutAlign.Center,
                BackgroundColor: Vector4.Zero,
                Spacing: 8
            ));

        ref var rowRect = ref editorWorld.Get<UIRect>(row);
        rowRect.WidthMode = UISizeMode.Fill;

        // Make row clickable
        editorWorld.Add(row, new UIInteractable
        {
            CanClick = true,
            CanFocus = true
        });

        editorWorld.Add(row, new SettingsControlData
        {
            Action = action
        });

        // Checkbox
        var checkColor = isEnabled
            ? new Vector4(0.3f, 0.7f, 0.3f, 1f)
            : new Vector4(0.5f, 0.5f, 0.5f, 1f);

        WidgetFactory.CreateLabel(
            editorWorld,
            row,
            $"SettingsToggle_{id}_Check",
            isEnabled ? "[x]" : "[ ]",
            font,
            new LabelConfig(
                FontSize: 12,
                TextColor: checkColor,
                HorizontalAlign: TextAlignH.Left
            ));

        // Label
        WidgetFactory.CreateLabel(
            editorWorld,
            row,
            $"SettingsToggle_{id}_Label",
            label,
            font,
            new LabelConfig(
                FontSize: 12,
                TextColor: EditorColors.TextLight,
                HorizontalAlign: TextAlignH.Left
            ));
    }

    private void RefreshSettingsTab()
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
        var settingsContent = state.SettingsContent;

        if (!settingsContent.IsValid)
        {
            return;
        }

        // Rebuild settings content
        ClearChildren(settingsContent);
        CreateSourcesSection(settingsContent);
        CreateSearchPathsSection(settingsContent);
        CreateHotReloadSection(settingsContent);
        CreateDeveloperSection(settingsContent);
        CreateSecuritySection(settingsContent);
    }

    #endregion

    #region Dialogs

    private void ShowAddSourceDialog()
    {
        if (editorWorld is null)
        {
            return;
        }

        var modalConfig = new ModalConfig(
            Width: 450,
            Title: "Add Package Source",
            CloseOnBackdropClick: false,
            CloseOnEscape: true,
            ShowCloseButton: true);

        var buttons = new[]
        {
            new ModalButtonDef("Cancel", ModalResult.Cancel, IsPrimary: false, Width: 80),
            new ModalButtonDef("Add", ModalResult.OK, IsPrimary: true, Width: 80)
        };

        var (modal, backdrop, contentPanel) = WidgetFactory.CreateModal(
            editorWorld,
            rootEntity,
            "AddSourceDialog",
            font,
            modalConfig,
            buttons);

        // Create form fields
        // Name label
        WidgetFactory.CreateLabel(
            editorWorld,
            contentPanel,
            "Name:",
            font,
            new LabelConfig(Height: 20, FontSize: 14));

        // Name input
        var nameInput = WidgetFactory.CreateTextField(
            editorWorld,
            contentPanel,
            "AddSourceDialog_Name",
            font,
            new TextFieldConfig(Width: 400, Height: 32, PlaceholderText: "Source name (e.g., My Private Feed)"));

        // URL label
        WidgetFactory.CreateLabel(
            editorWorld,
            contentPanel,
            "URL:",
            font,
            new LabelConfig(Height: 20, FontSize: 14));

        // URL input
        var urlInput = WidgetFactory.CreateTextField(
            editorWorld,
            contentPanel,
            "AddSourceDialog_Url",
            font,
            new TextFieldConfig(Width: 400, Height: 32, PlaceholderText: "https://api.nuget.org/v3/index.json"));

        // Make Default checkbox
        var makeDefaultCheckbox = WidgetFactory.CreateCheckbox(
            editorWorld,
            contentPanel,
            "AddSourceDialog_MakeDefault",
            "Make this the default source",
            font,
            new CheckboxConfig(IsChecked: false, Size: 18, FontSize: 14));

        // Add dialog data component to modal
        editorWorld.Add(modal, new AddSourceDialogData
        {
            NameInput = nameInput,
            UrlInput = urlInput,
            MakeDefaultCheckbox = makeDefaultCheckbox
        });

        // Store reference in panel state
        if (editorWorld.Has<PluginManagerPanelState>(rootEntity))
        {
            ref var state = ref editorWorld.Get<PluginManagerPanelState>(rootEntity);
            state.AddSourceDialog = modal;
        }

        // Open the modal
        OpenModalDialog(modal, backdrop);
    }

    private void ShowAddPathDialog()
    {
        if (editorWorld is null)
        {
            return;
        }

        var modalConfig = new ModalConfig(
            Width: 450,
            Title: "Add Search Path",
            CloseOnBackdropClick: false,
            CloseOnEscape: true,
            ShowCloseButton: true);

        var buttons = new[]
        {
            new ModalButtonDef("Cancel", ModalResult.Cancel, IsPrimary: false, Width: 80),
            new ModalButtonDef("Add", ModalResult.OK, IsPrimary: true, Width: 80)
        };

        var (modal, backdrop, contentPanel) = WidgetFactory.CreateModal(
            editorWorld,
            rootEntity,
            "AddPathDialog",
            font,
            modalConfig,
            buttons);

        // Path label
        WidgetFactory.CreateLabel(
            editorWorld,
            contentPanel,
            "Path:",
            font,
            new LabelConfig(Height: 20, FontSize: 14));

        // Path input
        var pathInput = WidgetFactory.CreateTextField(
            editorWorld,
            contentPanel,
            "AddPathDialog_Path",
            font,
            new TextFieldConfig(Width: 400, Height: 32, PlaceholderText: "C:\\Plugins or /home/user/plugins"));

        // Description label
        WidgetFactory.CreateLabel(
            editorWorld,
            contentPanel,
            "Description (optional):",
            font,
            new LabelConfig(Height: 20, FontSize: 14));

        // Description input
        var descriptionInput = WidgetFactory.CreateTextField(
            editorWorld,
            contentPanel,
            "AddPathDialog_Description",
            font,
            new TextFieldConfig(Width: 400, Height: 32, PlaceholderText: "Brief description"));

        // Recursive checkbox
        var recursiveCheckbox = WidgetFactory.CreateCheckbox(
            editorWorld,
            contentPanel,
            "AddPathDialog_Recursive",
            "Search subdirectories recursively",
            font,
            new CheckboxConfig(IsChecked: true, Size: 18, FontSize: 14));

        // Add dialog data component to modal
        editorWorld.Add(modal, new AddPathDialogData
        {
            PathInput = pathInput,
            DescriptionInput = descriptionInput,
            RecursiveCheckbox = recursiveCheckbox
        });

        // Store reference in panel state
        if (editorWorld.Has<PluginManagerPanelState>(rootEntity))
        {
            ref var state = ref editorWorld.Get<PluginManagerPanelState>(rootEntity);
            state.AddPathDialog = modal;
        }

        // Open the modal
        OpenModalDialog(modal, backdrop);
    }

    private void HandleAddSourceDialogResult(Entity modal, ModalResult result)
    {
        if (editorWorld is null || settings is null)
        {
            return;
        }

        if (result == ModalResult.OK && editorWorld.Has<AddSourceDialogData>(modal))
        {
            ref readonly var dialogData = ref editorWorld.Get<AddSourceDialogData>(modal);

            // Read the text values
            string name = string.Empty;
            string url = string.Empty;
            bool makeDefault = false;

            if (editorWorld.IsAlive(dialogData.NameInput) && editorWorld.Has<UIText>(dialogData.NameInput))
            {
                var text = editorWorld.Get<UIText>(dialogData.NameInput).Content;
                // Check if showing placeholder
                if (editorWorld.Has<UITextInput>(dialogData.NameInput))
                {
                    var textInput = editorWorld.Get<UITextInput>(dialogData.NameInput);
                    if (!textInput.ShowingPlaceholder)
                    {
                        name = text;
                    }
                }
            }

            if (editorWorld.IsAlive(dialogData.UrlInput) && editorWorld.Has<UIText>(dialogData.UrlInput))
            {
                var text = editorWorld.Get<UIText>(dialogData.UrlInput).Content;
                if (editorWorld.Has<UITextInput>(dialogData.UrlInput))
                {
                    var textInput = editorWorld.Get<UITextInput>(dialogData.UrlInput);
                    if (!textInput.ShowingPlaceholder)
                    {
                        url = text;
                    }
                }
            }

            if (editorWorld.IsAlive(dialogData.MakeDefaultCheckbox) && editorWorld.Has<UICheckbox>(dialogData.MakeDefaultCheckbox))
            {
                makeDefault = editorWorld.Get<UICheckbox>(dialogData.MakeDefaultCheckbox).IsChecked;
            }

            // Validate and save
            if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(url))
            {
                settings.AddSource(name.Trim(), url.Trim(), makeDefault);
                settings.Save();
                RefreshSettingsTab();
            }
        }

        // Clean up dialog
        CleanupAddSourceDialog();
    }

    private void HandleAddPathDialogResult(Entity modal, ModalResult result)
    {
        if (editorWorld is null || settings is null)
        {
            return;
        }

        if (result == ModalResult.OK && editorWorld.Has<AddPathDialogData>(modal))
        {
            ref readonly var dialogData = ref editorWorld.Get<AddPathDialogData>(modal);

            // Read the values
            string path = string.Empty;
            string? description = null;
            bool recursive = true;

            if (editorWorld.IsAlive(dialogData.PathInput) && editorWorld.Has<UIText>(dialogData.PathInput))
            {
                var text = editorWorld.Get<UIText>(dialogData.PathInput).Content;
                if (editorWorld.Has<UITextInput>(dialogData.PathInput))
                {
                    var textInput = editorWorld.Get<UITextInput>(dialogData.PathInput);
                    if (!textInput.ShowingPlaceholder)
                    {
                        path = text;
                    }
                }
            }

            if (editorWorld.IsAlive(dialogData.DescriptionInput) && editorWorld.Has<UIText>(dialogData.DescriptionInput))
            {
                var text = editorWorld.Get<UIText>(dialogData.DescriptionInput).Content;
                if (editorWorld.Has<UITextInput>(dialogData.DescriptionInput))
                {
                    var textInput = editorWorld.Get<UITextInput>(dialogData.DescriptionInput);
                    if (!textInput.ShowingPlaceholder)
                    {
                        description = string.IsNullOrWhiteSpace(text) ? null : text.Trim();
                    }
                }
            }

            if (editorWorld.IsAlive(dialogData.RecursiveCheckbox) && editorWorld.Has<UICheckbox>(dialogData.RecursiveCheckbox))
            {
                recursive = editorWorld.Get<UICheckbox>(dialogData.RecursiveCheckbox).IsChecked;
            }

            // Validate and save
            if (!string.IsNullOrWhiteSpace(path))
            {
                settings.AddSearchPath(path.Trim(), description, recursive);
                settings.Save();
                RefreshSettingsTab();
            }
        }

        // Clean up dialog
        CleanupAddPathDialog();
    }

    private void OpenModalDialog(Entity modal, Entity backdrop)
    {
        if (editorWorld is null)
        {
            return;
        }

        // Show the modal by setting visibility
        if (editorWorld.Has<UIElement>(modal))
        {
            ref var element = ref editorWorld.Get<UIElement>(modal);
            element.Visible = true;
        }

        if (editorWorld.Has<UIHiddenTag>(modal))
        {
            editorWorld.Remove<UIHiddenTag>(modal);
        }

        // Show the backdrop
        if (backdrop.IsValid && editorWorld.IsAlive(backdrop))
        {
            if (editorWorld.Has<UIElement>(backdrop))
            {
                ref var backdropElement = ref editorWorld.Get<UIElement>(backdrop);
                backdropElement.Visible = true;
            }

            if (editorWorld.Has<UIHiddenTag>(backdrop))
            {
                editorWorld.Remove<UIHiddenTag>(backdrop);
            }
        }

        // Update modal state
        if (editorWorld.Has<UIModal>(modal))
        {
            ref var modalComponent = ref editorWorld.Get<UIModal>(modal);
            modalComponent.IsOpen = true;
        }

        // Mark layout dirty
        if (!editorWorld.Has<UILayoutDirtyTag>(modal))
        {
            editorWorld.Add(modal, new UILayoutDirtyTag());
        }
    }

    private void CleanupAddSourceDialog()
    {
        if (editorWorld is null)
        {
            return;
        }

        if (!editorWorld.Has<PluginManagerPanelState>(rootEntity))
        {
            return;
        }

        ref var state = ref editorWorld.Get<PluginManagerPanelState>(rootEntity);

        if (state.AddSourceDialog.IsValid && editorWorld.IsAlive(state.AddSourceDialog))
        {
            // Get backdrop before despawning modal
            Entity backdrop = Entity.Null;
            if (editorWorld.Has<UIModal>(state.AddSourceDialog))
            {
                backdrop = editorWorld.Get<UIModal>(state.AddSourceDialog).Backdrop;
            }

            // Despawn modal (children are despawned automatically via hierarchy)
            DespawnRecursive(state.AddSourceDialog);

            // Despawn backdrop
            if (backdrop.IsValid && editorWorld.IsAlive(backdrop))
            {
                editorWorld.Despawn(backdrop);
            }
        }

        state.AddSourceDialog = Entity.Null;
    }

    private void CleanupAddPathDialog()
    {
        if (editorWorld is null)
        {
            return;
        }

        if (!editorWorld.Has<PluginManagerPanelState>(rootEntity))
        {
            return;
        }

        ref var state = ref editorWorld.Get<PluginManagerPanelState>(rootEntity);

        if (state.AddPathDialog.IsValid && editorWorld.IsAlive(state.AddPathDialog))
        {
            // Get backdrop before despawning modal
            Entity backdrop = Entity.Null;
            if (editorWorld.Has<UIModal>(state.AddPathDialog))
            {
                backdrop = editorWorld.Get<UIModal>(state.AddPathDialog).Backdrop;
            }

            // Despawn modal (children are despawned automatically via hierarchy)
            DespawnRecursive(state.AddPathDialog);

            // Despawn backdrop
            if (backdrop.IsValid && editorWorld.IsAlive(backdrop))
            {
                editorWorld.Despawn(backdrop);
            }
        }

        state.AddPathDialog = Entity.Null;
    }

    #endregion

    #region Helpers

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

    #endregion
}

#region Components and Enums

/// <summary>
/// Component storing the state of the plugin manager panel.
/// </summary>
internal struct PluginManagerPanelState : IComponent
{
    /// <summary>The tab view container entity.</summary>
    public Entity TabView;

    /// <summary>The Installed tab content container.</summary>
    public Entity InstalledTabContent;

    /// <summary>The Browse tab content container.</summary>
    public Entity BrowseTabContent;

    /// <summary>The Settings tab content container.</summary>
    public Entity SettingsTabContent;

    /// <summary>The installed plugins list container.</summary>
    public Entity InstalledListContainer;

    /// <summary>The installed plugin details container.</summary>
    public Entity InstalledDetailsContainer;

    /// <summary>The browse results list container.</summary>
    public Entity BrowseResultsContainer;

    /// <summary>The browse details container.</summary>
    public Entity BrowseDetailsContainer;

    /// <summary>The settings content container.</summary>
    public Entity SettingsContent;

    /// <summary>The ID of the currently selected installed plugin.</summary>
    public string? SelectedPluginId;

    /// <summary>The ID of the currently selected browse package.</summary>
    public string? SelectedBrowsePackageId;

    /// <summary>The current search query.</summary>
    public string? SearchQuery;

    /// <summary>The search debounce timer.</summary>
    public float SearchDebounceTimer;

    /// <summary>The current search results.</summary>
    public List<PackageSearchResult> SearchResults;

    /// <summary>The current async operation type.</summary>
    public AsyncOperationType AsyncType;

    /// <summary>The current async operation status.</summary>
    public AsyncOperationStatus AsyncStatus;

    /// <summary>The current async task.</summary>
    public Task? AsyncTask;

    /// <summary>Error message from async operation.</summary>
    public string? AsyncErrorMessage;

    /// <summary>The currently active Add Source dialog modal entity (if any).</summary>
    public Entity AddSourceDialog;

    /// <summary>The currently active Add Path dialog modal entity (if any).</summary>
    public Entity AddPathDialog;
}

/// <summary>
/// Component storing data for a plugin list item.
/// </summary>
internal struct PluginListItemData : IComponent
{
    /// <summary>The plugin ID this item represents.</summary>
    public string PluginId;
}

/// <summary>
/// Component storing data for a plugin control button.
/// </summary>
internal struct PluginControlButtonData : IComponent
{
    /// <summary>The plugin ID this button controls.</summary>
    public string PluginId;

    /// <summary>The action this button performs.</summary>
    public PluginButtonAction Action;
}

/// <summary>
/// Component storing data for a browse result item.
/// </summary>
internal struct BrowseResultItemData : IComponent
{
    /// <summary>The package ID this item represents.</summary>
    public string PackageId;
}

/// <summary>
/// Component storing data for an install button.
/// </summary>
internal struct InstallButtonData : IComponent
{
    /// <summary>The package ID to install.</summary>
    public string PackageId;

    /// <summary>The version to install.</summary>
    public string Version;
}

/// <summary>
/// Component storing data for a search box.
/// </summary>
internal struct SearchBoxData : IComponent;

/// <summary>
/// Component identifying an active Add Source dialog.
/// </summary>
internal struct AddSourceDialogData : IComponent
{
    /// <summary>The text input entity for the source name.</summary>
    public Entity NameInput;

    /// <summary>The text input entity for the source URL.</summary>
    public Entity UrlInput;

    /// <summary>The checkbox entity for "Make Default".</summary>
    public Entity MakeDefaultCheckbox;
}

/// <summary>
/// Component identifying an active Add Path dialog.
/// </summary>
internal struct AddPathDialogData : IComponent
{
    /// <summary>The text input entity for the path.</summary>
    public Entity PathInput;

    /// <summary>The text input entity for the description.</summary>
    public Entity DescriptionInput;

    /// <summary>The checkbox entity for "Recursive".</summary>
    public Entity RecursiveCheckbox;
}

/// <summary>
/// Component storing data for a settings control.
/// </summary>
internal struct SettingsControlData : IComponent
{
    /// <summary>The settings action to perform.</summary>
    public SettingsAction Action;

    /// <summary>Optional target ID (e.g., source name, path).</summary>
    public string? TargetId;
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
    Reload,

    /// <summary>Uninstall the plugin.</summary>
    Uninstall
}

/// <summary>
/// Types of async operations.
/// </summary>
internal enum AsyncOperationType
{
    /// <summary>No operation.</summary>
    None,

    /// <summary>Search operation.</summary>
    Search,

    /// <summary>Install operation.</summary>
    Install,

    /// <summary>Uninstall operation.</summary>
    Uninstall
}

/// <summary>
/// Status of async operations.
/// </summary>
internal enum AsyncOperationStatus
{
    /// <summary>Idle, no operation in progress.</summary>
    Idle,

    /// <summary>Operation is running.</summary>
    Running,

    /// <summary>Operation completed successfully.</summary>
    Completed,

    /// <summary>Operation failed.</summary>
    Failed
}

/// <summary>
/// Settings panel actions.
/// </summary>
internal enum SettingsAction
{
    /// <summary>Toggle hot reload.</summary>
    ToggleHotReload,

    /// <summary>Toggle developer mode.</summary>
    ToggleDeveloperMode,

    /// <summary>Toggle verbose logging.</summary>
    ToggleVerboseLogging,

    /// <summary>Toggle code signing requirement.</summary>
    ToggleCodeSigning,

    /// <summary>Toggle permission system.</summary>
    TogglePermissionSystem,

    /// <summary>Add a package source.</summary>
    AddSource,

    /// <summary>Remove a package source.</summary>
    RemoveSource,

    /// <summary>Add a search path.</summary>
    AddSearchPath,

    /// <summary>Remove a search path.</summary>
    RemoveSearchPath
}

#endregion
