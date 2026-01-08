using System.Numerics;

using KeenEyes.Editor.Application;
using KeenEyes.Editor.Logging;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.Logging;
using KeenEyes.UI.Abstractions;
using KeenEyes.UI.Widgets;

namespace KeenEyes.Editor.Panels;

/// <summary>
/// The console panel displays log messages, warnings, and errors.
/// </summary>
public static class ConsolePanel
{
    private static readonly Vector4 InfoColor = new(0.7f, 0.7f, 0.75f, 1f);
    private static readonly Vector4 WarningColor = new(1f, 0.8f, 0.2f, 1f);
    private static readonly Vector4 ErrorColor = new(1f, 0.3f, 0.3f, 1f);
    private static readonly Vector4 DebugColor = new(0.5f, 0.5f, 0.55f, 1f);

    /// <summary>
    /// Creates the console panel.
    /// </summary>
    /// <param name="editorWorld">The editor UI world.</param>
    /// <param name="parent">The parent container entity.</param>
    /// <param name="font">The font to use for text.</param>
    /// <param name="logProvider">The log provider to display logs from.</param>
    /// <returns>The created panel entity.</returns>
    public static Entity Create(
        IWorld editorWorld,
        Entity parent,
        FontHandle font,
        EditorLogProvider logProvider)
    {
        // Create the main panel container
        var panel = WidgetFactory.CreatePanel(editorWorld, parent, "ConsolePanel", new PanelConfig(
            Height: 200,
            Direction: LayoutDirection.Vertical,
            BackgroundColor: EditorColors.DarkPanel
        ));

        ref var panelRect = ref editorWorld.Get<UIRect>(panel);
        panelRect.WidthMode = UISizeMode.Fill;

        // Create toolbar with filter buttons
        var toolbar = CreateToolbar(editorWorld, panel, font, logProvider);

        // Create content area (scrollable log list)
        var contentArea = WidgetFactory.CreatePanel(editorWorld, panel, "ConsoleContent", new PanelConfig(
            Direction: LayoutDirection.Vertical,
            BackgroundColor: new Vector4(0.08f, 0.08f, 0.10f, 1f),
            Padding: UIEdges.All(4),
            Spacing: 1
        ));

        ref var contentRect = ref editorWorld.Get<UIRect>(contentArea);
        contentRect.WidthMode = UISizeMode.Fill;
        contentRect.HeightMode = UISizeMode.Fill;

        // Store state
        var state = new ConsolePanelState
        {
            ContentArea = contentArea,
            Toolbar = toolbar,
            LogProvider = logProvider,
            Font = font,
            ShowInfo = true,
            ShowWarnings = true,
            ShowErrors = true,
            SearchText = null
        };

        editorWorld.Add(panel, state);

        // Subscribe to log events
        logProvider.LogAdded += entry => OnLogAdded(editorWorld, panel, entry);
        logProvider.LogsCleared += () => ClearLogDisplay(editorWorld, panel);

        // Populate with existing logs
        RefreshLogDisplay(editorWorld, panel);

        return panel;
    }

    private static Entity CreateToolbar(
        IWorld editorWorld,
        Entity panel,
        FontHandle font,
        EditorLogProvider logProvider)
    {
        var toolbar = WidgetFactory.CreatePanel(editorWorld, panel, "ConsoleToolbar", new PanelConfig(
            Height: 28,
            Direction: LayoutDirection.Horizontal,
            MainAxisAlign: LayoutAlign.SpaceBetween,
            CrossAxisAlign: LayoutAlign.Center,
            BackgroundColor: EditorColors.MediumPanel,
            Padding: UIEdges.Symmetric(8, 4),
            Spacing: 8
        ));

        ref var toolbarRect = ref editorWorld.Get<UIRect>(toolbar);
        toolbarRect.WidthMode = UISizeMode.Fill;

        // Left side: Clear button and title
        var leftGroup = WidgetFactory.CreatePanel(editorWorld, toolbar, "ConsoleLeft", new PanelConfig(
            Direction: LayoutDirection.Horizontal,
            CrossAxisAlign: LayoutAlign.Center,
            Spacing: 8
        ));

        ref var leftRect = ref editorWorld.Get<UIRect>(leftGroup);
        leftRect.HeightMode = UISizeMode.Fill;

        // Clear button
        var clearButton = WidgetFactory.CreateButton(editorWorld, leftGroup, "ClearButton", "Clear", font, new ButtonConfig(
            Width: 50,
            Height: 20,
            BackgroundColor: new Vector4(0.25f, 0.25f, 0.30f, 1f),
            TextColor: EditorColors.TextLight,
            FontSize: 11
        ));

        // Console title
        WidgetFactory.CreateLabel(editorWorld, leftGroup, "ConsoleTitle", "Console", font, new LabelConfig(
            FontSize: 13,
            TextColor: EditorColors.TextWhite,
            HorizontalAlign: TextAlignH.Left
        ));

        // Right side: Filter toggles with counts
        var rightGroup = WidgetFactory.CreatePanel(editorWorld, toolbar, "ConsoleRight", new PanelConfig(
            Direction: LayoutDirection.Horizontal,
            CrossAxisAlign: LayoutAlign.Center,
            Spacing: 12
        ));

        ref var rightRect = ref editorWorld.Get<UIRect>(rightGroup);
        rightRect.HeightMode = UISizeMode.Fill;

        // Info count
        var infoLabel = WidgetFactory.CreateLabel(editorWorld, rightGroup, "InfoCount",
            $"\u25CF {logProvider.InfoCount}", font, new LabelConfig(
                FontSize: 12,
                TextColor: InfoColor,
                HorizontalAlign: TextAlignH.Right
            ));

        // Warning count
        var warningLabel = WidgetFactory.CreateLabel(editorWorld, rightGroup, "WarningCount",
            $"\u26A0 {logProvider.WarningCount}", font, new LabelConfig(
                FontSize: 12,
                TextColor: WarningColor,
                HorizontalAlign: TextAlignH.Right
            ));

        // Error count
        var errorLabel = WidgetFactory.CreateLabel(editorWorld, rightGroup, "ErrorCount",
            $"\u2716 {logProvider.ErrorCount}", font, new LabelConfig(
                FontSize: 12,
                TextColor: ErrorColor,
                HorizontalAlign: TextAlignH.Right
            ));

        // Store count label references
        editorWorld.Add(toolbar, new ConsoleToolbarState
        {
            InfoLabel = infoLabel,
            WarningLabel = warningLabel,
            ErrorLabel = errorLabel,
            ClearButton = clearButton
        });

        // Handle clear button click
        editorWorld.Subscribe<UIClickEvent>(e =>
        {
            if (e.Element == clearButton)
            {
                logProvider.Clear();
            }
        });

        return toolbar;
    }

    private static void OnLogAdded(IWorld editorWorld, Entity panel, LogEntry entry)
    {
        if (!editorWorld.Has<ConsolePanelState>(panel))
        {
            return;
        }

        ref readonly var state = ref editorWorld.Get<ConsolePanelState>(panel);

        // Check if this entry passes the filter
        if (!ShouldShowEntry(entry, state.ShowInfo, state.ShowWarnings, state.ShowErrors, state.SearchText))
        {
            return;
        }

        // Add the log entry to the display
        AddLogEntryUI(editorWorld, state.ContentArea, state.Font, entry);

        // Update counts in toolbar
        UpdateToolbarCounts(editorWorld, state.Toolbar, state.LogProvider);
    }

    private static bool ShouldShowEntry(LogEntry entry, bool showInfo, bool showWarnings, bool showErrors, string? searchText)
    {
        // Level filter
        if (entry.Level >= LogLevel.Error && !showErrors)
        {
            return false;
        }

        if (entry.Level == LogLevel.Warning && !showWarnings)
        {
            return false;
        }

        if (entry.Level < LogLevel.Warning && !showInfo)
        {
            return false;
        }

        // Search filter
        if (!string.IsNullOrWhiteSpace(searchText) &&
            !entry.Message.Contains(searchText, StringComparison.OrdinalIgnoreCase) &&
            !entry.Category.Contains(searchText, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private static void AddLogEntryUI(IWorld editorWorld, Entity contentArea, FontHandle font, LogEntry entry)
    {
        var color = GetLogLevelColor(entry.Level);
        var icon = GetLogLevelIcon(entry.Level);

        // Create log entry row
        var row = WidgetFactory.CreatePanel(editorWorld, contentArea, $"Log_{entry.Timestamp.Ticks}", new PanelConfig(
            Height: 18,
            Direction: LayoutDirection.Horizontal,
            CrossAxisAlign: LayoutAlign.Center,
            Spacing: 6,
            Padding: UIEdges.Symmetric(4, 0)
        ));

        ref var rowRect = ref editorWorld.Get<UIRect>(row);
        rowRect.WidthMode = UISizeMode.Fill;

        editorWorld.Add(row, new ConsoleLogEntryTag());

        // Icon/Level indicator
        WidgetFactory.CreateLabel(editorWorld, row, $"Icon_{entry.Timestamp.Ticks}",
            icon, font, new LabelConfig(
                FontSize: 10,
                TextColor: color,
                HorizontalAlign: TextAlignH.Left
            ));

        // Timestamp
        WidgetFactory.CreateLabel(editorWorld, row, $"Time_{entry.Timestamp.Ticks}",
            entry.GetFormattedTime(), font, new LabelConfig(
                FontSize: 10,
                TextColor: EditorColors.TextMuted,
                HorizontalAlign: TextAlignH.Left
            ));

        // Category (if not empty)
        if (!string.IsNullOrWhiteSpace(entry.Category))
        {
            WidgetFactory.CreateLabel(editorWorld, row, $"Category_{entry.Timestamp.Ticks}",
                $"[{entry.Category}]", font, new LabelConfig(
                    FontSize: 10,
                    TextColor: new Vector4(0.5f, 0.6f, 0.7f, 1f),
                    HorizontalAlign: TextAlignH.Left
                ));
        }

        // Message
        WidgetFactory.CreateLabel(editorWorld, row, $"Message_{entry.Timestamp.Ticks}",
            entry.Message, font, new LabelConfig(
                FontSize: 11,
                TextColor: color,
                HorizontalAlign: TextAlignH.Left
            ));
    }

    private static Vector4 GetLogLevelColor(LogLevel level)
    {
        return level switch
        {
            LogLevel.Trace => DebugColor,
            LogLevel.Debug => DebugColor,
            LogLevel.Info => InfoColor,
            LogLevel.Warning => WarningColor,
            LogLevel.Error => ErrorColor,
            LogLevel.Fatal => ErrorColor,
            _ => InfoColor
        };
    }

    private static string GetLogLevelIcon(LogLevel level)
    {
        return level switch
        {
            LogLevel.Trace => "\u25CB",    // ○
            LogLevel.Debug => "\u25CB",    // ○
            LogLevel.Info => "\u25CF",     // ●
            LogLevel.Warning => "\u26A0",  // ⚠
            LogLevel.Error => "\u2716",    // ✖
            LogLevel.Fatal => "\u2716",    // ✖
            _ => "\u25CF"
        };
    }

    private static void UpdateToolbarCounts(IWorld editorWorld, Entity toolbar, EditorLogProvider logProvider)
    {
        if (!editorWorld.Has<ConsoleToolbarState>(toolbar))
        {
            return;
        }

        ref readonly var toolbarState = ref editorWorld.Get<ConsoleToolbarState>(toolbar);

        // Update info count
        if (editorWorld.Has<UIText>(toolbarState.InfoLabel))
        {
            ref var infoText = ref editorWorld.Get<UIText>(toolbarState.InfoLabel);
            infoText.Content = $"\u25CF {logProvider.InfoCount}";
        }

        // Update warning count
        if (editorWorld.Has<UIText>(toolbarState.WarningLabel))
        {
            ref var warningText = ref editorWorld.Get<UIText>(toolbarState.WarningLabel);
            warningText.Content = $"\u26A0 {logProvider.WarningCount}";
        }

        // Update error count
        if (editorWorld.Has<UIText>(toolbarState.ErrorLabel))
        {
            ref var errorText = ref editorWorld.Get<UIText>(toolbarState.ErrorLabel);
            errorText.Content = $"\u2716 {logProvider.ErrorCount}";
        }
    }

    private static void RefreshLogDisplay(IWorld editorWorld, Entity panel)
    {
        if (!editorWorld.Has<ConsolePanelState>(panel))
        {
            return;
        }

        ref readonly var state = ref editorWorld.Get<ConsolePanelState>(panel);

        // Clear existing display
        ClearLogEntries(editorWorld, state.ContentArea);

        // Add all filtered entries
        foreach (var entry in state.LogProvider.GetFilteredEntries(
            state.ShowInfo, state.ShowWarnings, state.ShowErrors, state.SearchText))
        {
            AddLogEntryUI(editorWorld, state.ContentArea, state.Font, entry);
        }

        // Update toolbar counts
        UpdateToolbarCounts(editorWorld, state.Toolbar, state.LogProvider);
    }

    private static void ClearLogDisplay(IWorld editorWorld, Entity panel)
    {
        if (!editorWorld.Has<ConsolePanelState>(panel))
        {
            return;
        }

        ref readonly var state = ref editorWorld.Get<ConsolePanelState>(panel);

        ClearLogEntries(editorWorld, state.ContentArea);
        UpdateToolbarCounts(editorWorld, state.Toolbar, state.LogProvider);
    }

    private static void ClearLogEntries(IWorld editorWorld, Entity contentArea)
    {
        var children = editorWorld.GetChildren(contentArea).ToList();
        foreach (var child in children)
        {
            if (editorWorld.Has<ConsoleLogEntryTag>(child))
            {
                DespawnRecursive(editorWorld, child);
            }
        }
    }

    private static void DespawnRecursive(IWorld world, Entity entity)
    {
        var children = world.GetChildren(entity).ToList();
        foreach (var child in children)
        {
            DespawnRecursive(world, child);
        }
        world.Despawn(entity);
    }

    /// <summary>
    /// Sets the filter state for the console panel.
    /// </summary>
    /// <param name="editorWorld">The editor world.</param>
    /// <param name="panel">The console panel entity.</param>
    /// <param name="showInfo">Whether to show info messages.</param>
    /// <param name="showWarnings">Whether to show warnings.</param>
    /// <param name="showErrors">Whether to show errors.</param>
    public static void SetFilter(IWorld editorWorld, Entity panel, bool showInfo, bool showWarnings, bool showErrors)
    {
        if (!editorWorld.Has<ConsolePanelState>(panel))
        {
            return;
        }

        ref var state = ref editorWorld.Get<ConsolePanelState>(panel);
        state.ShowInfo = showInfo;
        state.ShowWarnings = showWarnings;
        state.ShowErrors = showErrors;

        RefreshLogDisplay(editorWorld, panel);
    }

    /// <summary>
    /// Sets the search text filter for the console panel.
    /// </summary>
    /// <param name="editorWorld">The editor world.</param>
    /// <param name="panel">The console panel entity.</param>
    /// <param name="searchText">The text to search for, or null to clear search.</param>
    public static void SetSearchText(IWorld editorWorld, Entity panel, string? searchText)
    {
        if (!editorWorld.Has<ConsolePanelState>(panel))
        {
            return;
        }

        ref var state = ref editorWorld.Get<ConsolePanelState>(panel);
        state.SearchText = searchText;

        RefreshLogDisplay(editorWorld, panel);
    }
}

/// <summary>
/// Component storing the state of the console panel.
/// </summary>
internal struct ConsolePanelState : IComponent
{
    public Entity ContentArea;
    public Entity Toolbar;
    public EditorLogProvider LogProvider;
    public FontHandle Font;
    public bool ShowInfo;
    public bool ShowWarnings;
    public bool ShowErrors;
    public string? SearchText;
}

/// <summary>
/// Component storing toolbar element references.
/// </summary>
internal struct ConsoleToolbarState : IComponent
{
    public Entity InfoLabel;
    public Entity WarningLabel;
    public Entity ErrorLabel;
    public Entity ClearButton;
}

/// <summary>
/// Tag component to identify console log entry rows.
/// </summary>
internal struct ConsoleLogEntryTag : IComponent;
