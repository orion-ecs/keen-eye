using System.Numerics;

using KeenEyes.Editor.Application;
using KeenEyes.Editor.PlayMode;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.Replay;
using KeenEyes.UI.Abstractions;
using KeenEyes.UI.Widgets;

namespace KeenEyes.Editor.Panels;

/// <summary>
/// The frame inspector panel provides detailed frame-by-frame debugging for replay playback.
/// </summary>
/// <remarks>
/// <para>
/// This panel displays:
/// <list type="bullet">
/// <item><description>Frame metadata (number, time, delta)</description></item>
/// <item><description>Events that occurred during the frame</description></item>
/// <item><description>Entity lifecycle tracking (created/destroyed)</description></item>
/// <item><description>Component diffs with change highlighting</description></item>
/// </list>
/// </para>
/// </remarks>
public static class FrameInspectorPanel
{
    // Event type colors
    private static readonly Vector4 EntityCreatedColor = new(0.4f, 0.8f, 0.4f, 1f);
    private static readonly Vector4 EntityDestroyedColor = new(0.8f, 0.4f, 0.4f, 1f);
    private static readonly Vector4 ComponentAddedColor = new(0.5f, 0.7f, 0.9f, 1f);
    private static readonly Vector4 ComponentRemovedColor = new(0.9f, 0.6f, 0.5f, 1f);
    private static readonly Vector4 ComponentChangedColor = new(0.9f, 0.8f, 0.4f, 1f);
    private static readonly Vector4 SystemEventColor = new(0.7f, 0.7f, 0.75f, 1f);
    private static readonly Vector4 CustomEventColor = new(0.8f, 0.6f, 0.9f, 1f);
    private static readonly Vector4 SnapshotColor = new(0.6f, 0.9f, 0.9f, 1f);

    // Delta colors for numeric changes
    private static readonly Vector4 PositiveDeltaColor = new(0.4f, 0.9f, 0.4f, 1f);
    private static readonly Vector4 NegativeDeltaColor = new(0.9f, 0.4f, 0.4f, 1f);

    /// <summary>
    /// Creates the frame inspector panel.
    /// </summary>
    /// <param name="editorWorld">The editor UI world.</param>
    /// <param name="parent">The parent container entity.</param>
    /// <param name="font">The font to use for text.</param>
    /// <param name="replayPlayback">The replay playback mode to inspect frames from (can be null).</param>
    /// <returns>The created panel entity.</returns>
    public static Entity Create(
        IWorld editorWorld,
        Entity parent,
        FontHandle font,
        ReplayPlaybackMode? replayPlayback)
    {
        // Create the main panel container
        var panel = WidgetFactory.CreatePanel(editorWorld, parent, "FrameInspectorPanel", new PanelConfig(
            Width: 380,
            Direction: LayoutDirection.Vertical,
            BackgroundColor: EditorColors.DarkPanel
        ));

        // Create header with frame metadata
        var header = CreateHeader(editorWorld, panel, font);

        // Create toolbar with filter buttons
        var toolbar = CreateToolbar(editorWorld, panel, font);

        // Create content area (scrollable event list)
        var contentArea = WidgetFactory.CreatePanel(editorWorld, panel, "FrameInspectorContent", new PanelConfig(
            Direction: LayoutDirection.Vertical,
            BackgroundColor: new Vector4(0.08f, 0.08f, 0.10f, 1f),
            Padding: UIEdges.All(4),
            Spacing: 2
        ));

        ref var contentRect = ref editorWorld.Get<UIRect>(contentArea);
        contentRect.WidthMode = UISizeMode.Fill;
        contentRect.HeightMode = UISizeMode.Fill;

        // Create empty state label
        var emptyLabel = WidgetFactory.CreateLabel(editorWorld, contentArea, "FrameInspectorEmpty",
            "No replay loaded", font, new LabelConfig(
                FontSize: 13,
                TextColor: EditorColors.TextMuted,
                HorizontalAlign: TextAlignH.Center,
                VerticalAlign: TextAlignV.Middle
            ));

        ref var emptyRect = ref editorWorld.Get<UIRect>(emptyLabel);
        emptyRect.WidthMode = UISizeMode.Fill;
        emptyRect.HeightMode = UISizeMode.Fill;

        // Store state
        var state = new FrameInspectorPanelState
        {
            Header = header,
            Toolbar = toolbar,
            ContentArea = contentArea,
            EmptyLabel = emptyLabel,
            ReplayPlayback = replayPlayback,
            Font = font,
            ShowEntityEvents = true,
            ShowComponentEvents = true,
            ShowSystemEvents = true,
            ShowCustomEvents = true,
            FilterEntityId = null,
            ExpandedEventIndex = -1
        };

        editorWorld.Add(panel, state);

        // Subscribe to replay playback events if available
        if (replayPlayback is not null)
        {
            replayPlayback.FrameChanged += (_, args) => OnFrameChanged(editorWorld, panel, args);
            replayPlayback.StateChanged += (_, args) => OnStateChanged(editorWorld, panel, args);

            // Initial refresh if replay is already loaded
            if (replayPlayback.IsLoaded)
            {
                RefreshFrameDisplay(editorWorld, panel);
            }
        }

        return panel;
    }

    private static Entity CreateHeader(IWorld world, Entity panel, FontHandle font)
    {
        var header = WidgetFactory.CreatePanel(world, panel, "FrameInspectorHeader", new PanelConfig(
            Height: 64,
            Direction: LayoutDirection.Vertical,
            BackgroundColor: EditorColors.MediumPanel,
            Padding: UIEdges.All(8),
            Spacing: 4
        ));

        ref var headerRect = ref world.Get<UIRect>(header);
        headerRect.WidthMode = UISizeMode.Fill;

        // Title row
        var titleRow = WidgetFactory.CreatePanel(world, header, "TitleRow", new PanelConfig(
            Height: 20,
            Direction: LayoutDirection.Horizontal,
            MainAxisAlign: LayoutAlign.SpaceBetween,
            CrossAxisAlign: LayoutAlign.Center
        ));

        ref var titleRowRect = ref world.Get<UIRect>(titleRow);
        titleRowRect.WidthMode = UISizeMode.Fill;

        WidgetFactory.CreateLabel(world, titleRow, "FrameInspectorTitle", "Frame Inspector", font, new LabelConfig(
            FontSize: 13,
            TextColor: EditorColors.TextWhite,
            HorizontalAlign: TextAlignH.Left
        ));

        // Frame info row
        var frameInfoRow = WidgetFactory.CreatePanel(world, header, "FrameInfoRow", new PanelConfig(
            Height: 28,
            Direction: LayoutDirection.Horizontal,
            MainAxisAlign: LayoutAlign.SpaceBetween,
            CrossAxisAlign: LayoutAlign.Center,
            BackgroundColor: new Vector4(0.12f, 0.12f, 0.16f, 1f),
            Padding: UIEdges.Symmetric(8, 4)
        ));

        ref var frameInfoRect = ref world.Get<UIRect>(frameInfoRow);
        frameInfoRect.WidthMode = UISizeMode.Fill;

        // Frame number label
        var frameLabel = WidgetFactory.CreateLabel(world, frameInfoRow, "FrameNumber",
            "Frame: ---", font, new LabelConfig(
                FontSize: 14,
                TextColor: EditorColors.TextWhite,
                HorizontalAlign: TextAlignH.Left
            ));

        // Delta time label
        var deltaLabel = WidgetFactory.CreateLabel(world, frameInfoRow, "DeltaTime",
            "\u0394t: ---", font, new LabelConfig(
                FontSize: 12,
                TextColor: EditorColors.TextLight,
                HorizontalAlign: TextAlignH.Right
            ));

        // Store header element references
        world.Add(header, new FrameInspectorHeaderState
        {
            FrameLabel = frameLabel,
            DeltaLabel = deltaLabel
        });

        return header;
    }

    private static Entity CreateToolbar(IWorld world, Entity panel, FontHandle font)
    {
        var toolbar = WidgetFactory.CreatePanel(world, panel, "FrameInspectorToolbar", new PanelConfig(
            Height: 32,
            Direction: LayoutDirection.Horizontal,
            MainAxisAlign: LayoutAlign.Start,
            CrossAxisAlign: LayoutAlign.Center,
            BackgroundColor: new Vector4(0.14f, 0.14f, 0.18f, 1f),
            Padding: UIEdges.Symmetric(6, 4),
            Spacing: 4
        ));

        ref var toolbarRect = ref world.Get<UIRect>(toolbar);
        toolbarRect.WidthMode = UISizeMode.Fill;

        // Filter buttons
        var entityBtn = CreateFilterButton(world, toolbar, font, "EntityFilter", "\u25CF", EntityCreatedColor, "Entities");
        var compBtn = CreateFilterButton(world, toolbar, font, "ComponentFilter", "\u25A0", ComponentAddedColor, "Components");
        var sysBtn = CreateFilterButton(world, toolbar, font, "SystemFilter", "\u25B6", SystemEventColor, "Systems");
        var customBtn = CreateFilterButton(world, toolbar, font, "CustomFilter", "\u2605", CustomEventColor, "Custom");

        // Spacer
        var spacer = WidgetFactory.CreatePanel(world, toolbar, "Spacer", new PanelConfig());
        ref var spacerRect = ref world.Get<UIRect>(spacer);
        spacerRect.WidthMode = UISizeMode.Fill;
        spacerRect.HeightMode = UISizeMode.Fill;

        // Event count label
        var countLabel = WidgetFactory.CreateLabel(world, toolbar, "EventCount",
            "0 events", font, new LabelConfig(
                FontSize: 11,
                TextColor: EditorColors.TextMuted,
                HorizontalAlign: TextAlignH.Right
            ));

        // Store toolbar element references
        world.Add(toolbar, new FrameInspectorToolbarState
        {
            EntityFilterButton = entityBtn,
            ComponentFilterButton = compBtn,
            SystemFilterButton = sysBtn,
            CustomFilterButton = customBtn,
            EventCountLabel = countLabel
        });

        return toolbar;
    }

    private static Entity CreateFilterButton(
        IWorld world,
        Entity parent,
        FontHandle font,
        string name,
        string icon,
        Vector4 color,
        string tooltip)
    {
        var button = WidgetFactory.CreateButton(world, parent, name, icon, font, new ButtonConfig(
            Width: 24,
            Height: 24,
            BackgroundColor: new Vector4(0.2f, 0.2f, 0.25f, 1f),
            TextColor: color,
            FontSize: 12
        ));

        // Add tooltip component if available
        world.Add(button, new FrameInspectorFilterButtonTag { FilterType = name });

        return button;
    }

    private static void OnFrameChanged(IWorld editorWorld, Entity panel, FrameChangedEventArgs args)
    {
        RefreshFrameDisplay(editorWorld, panel);
    }

    private static void OnStateChanged(IWorld editorWorld, Entity panel, PlaybackStateChangedEventArgs args)
    {
        // Refresh when playback stops to show final state
        if (args.NewState == PlaybackState.Stopped)
        {
            RefreshFrameDisplay(editorWorld, panel);
        }
    }

    /// <summary>
    /// Refreshes the frame display with current frame data.
    /// </summary>
    private static void RefreshFrameDisplay(IWorld editorWorld, Entity panel)
    {
        if (!editorWorld.Has<FrameInspectorPanelState>(panel))
        {
            return;
        }

        ref readonly var state = ref editorWorld.Get<FrameInspectorPanelState>(panel);
        var replayPlayback = state.ReplayPlayback;

        // Update header
        UpdateHeader(editorWorld, state.Header, replayPlayback);

        // Clear existing events
        ClearEventEntries(editorWorld, state.ContentArea, state.EmptyLabel);

        // Check if replay playback is available and loaded
        if (replayPlayback is null || !replayPlayback.IsLoaded)
        {
            ShowEmptyState(editorWorld, state.EmptyLabel, "No replay loaded");
            UpdateEventCount(editorWorld, state.Toolbar, 0);
            return;
        }

        // Get current frame data
        var frameData = replayPlayback.GetCurrentFrameData();
        if (frameData is null)
        {
            ShowEmptyState(editorWorld, state.EmptyLabel, "No frame data");
            UpdateEventCount(editorWorld, state.Toolbar, 0);
            return;
        }

        // Hide empty label
        HideEmptyState(editorWorld, state.EmptyLabel);

        // Filter and display events
        var filteredEvents = FilterEvents(frameData.Events, state);
        var displayedCount = 0;

        foreach (var evt in filteredEvents)
        {
            AddEventEntry(editorWorld, state.ContentArea, state.Font, evt, displayedCount);
            displayedCount++;
        }

        // Update event count
        UpdateEventCount(editorWorld, state.Toolbar, displayedCount);

        // Show empty state if no events match filter
        if (displayedCount == 0)
        {
            ShowEmptyState(editorWorld, state.EmptyLabel, "No matching events");
        }
    }

    private static void UpdateHeader(IWorld world, Entity header, ReplayPlaybackMode? replayPlayback)
    {
        if (!world.Has<FrameInspectorHeaderState>(header))
        {
            return;
        }

        ref readonly var headerState = ref world.Get<FrameInspectorHeaderState>(header);

        var frameNumber = replayPlayback?.CurrentFrame ?? -1;
        var totalFrames = replayPlayback?.TotalFrames ?? 0;
        var frameData = replayPlayback?.GetCurrentFrameData();

        // Update frame number label
        if (world.Has<UIText>(headerState.FrameLabel))
        {
            ref var frameText = ref world.Get<UIText>(headerState.FrameLabel);
            frameText.Content = frameNumber >= 0
                ? $"Frame: {frameNumber} / {totalFrames - 1}"
                : "Frame: ---";
        }

        // Update delta time label
        if (world.Has<UIText>(headerState.DeltaLabel))
        {
            ref var deltaText = ref world.Get<UIText>(headerState.DeltaLabel);
            if (frameData is not null)
            {
                var deltaMs = frameData.DeltaTime.TotalMilliseconds;
                deltaText.Content = $"\u0394t: {deltaMs:F2}ms";
            }
            else
            {
                deltaText.Content = "\u0394t: ---";
            }
        }
    }

    private static void UpdateEventCount(IWorld world, Entity toolbar, int count)
    {
        if (!world.Has<FrameInspectorToolbarState>(toolbar))
        {
            return;
        }

        ref readonly var toolbarState = ref world.Get<FrameInspectorToolbarState>(toolbar);

        if (world.Has<UIText>(toolbarState.EventCountLabel))
        {
            ref var countText = ref world.Get<UIText>(toolbarState.EventCountLabel);
            countText.Content = count == 1 ? "1 event" : $"{count} events";
        }
    }

    private static IEnumerable<ReplayEvent> FilterEvents(
        IReadOnlyList<ReplayEvent> events,
        FrameInspectorPanelState state)
    {
        foreach (var evt in events)
        {
            // Skip frame start/end markers
            if (evt.Type == ReplayEventType.FrameStart || evt.Type == ReplayEventType.FrameEnd)
            {
                continue;
            }

            // Entity filter
            if (state.FilterEntityId.HasValue && evt.EntityId != state.FilterEntityId)
            {
                continue;
            }

            // Type filters
            var show = evt.Type switch
            {
                ReplayEventType.EntityCreated or ReplayEventType.EntityDestroyed => state.ShowEntityEvents,
                ReplayEventType.ComponentAdded or ReplayEventType.ComponentRemoved or ReplayEventType.ComponentChanged => state.ShowComponentEvents,
                ReplayEventType.SystemStart or ReplayEventType.SystemEnd => state.ShowSystemEvents,
                ReplayEventType.Custom => state.ShowCustomEvents,
                ReplayEventType.Snapshot => state.ShowComponentEvents, // Show snapshots with component events
                _ => true
            };

            if (show)
            {
                yield return evt;
            }
        }
    }

    private static void AddEventEntry(
        IWorld world,
        Entity contentArea,
        FontHandle font,
        ReplayEvent evt,
        int index)
    {
        var (icon, color, label) = GetEventDisplayInfo(evt);

        // Create event row
        var row = WidgetFactory.CreatePanel(world, contentArea, $"Event_{index}", new PanelConfig(
            Height: 22,
            Direction: LayoutDirection.Horizontal,
            CrossAxisAlign: LayoutAlign.Center,
            BackgroundColor: index % 2 == 0
                ? new Vector4(0.10f, 0.10f, 0.13f, 1f)
                : new Vector4(0.11f, 0.11f, 0.14f, 1f),
            Padding: UIEdges.Symmetric(6, 2),
            Spacing: 6
        ));

        ref var rowRect = ref world.Get<UIRect>(row);
        rowRect.WidthMode = UISizeMode.Fill;

        world.Add(row, new FrameInspectorEventTag { EventIndex = index });

        // Icon
        WidgetFactory.CreateLabel(world, row, $"Icon_{index}", icon, font, new LabelConfig(
            FontSize: 10,
            TextColor: color,
            HorizontalAlign: TextAlignH.Left
        ));

        // Event type label
        WidgetFactory.CreateLabel(world, row, $"Type_{index}", label, font, new LabelConfig(
            FontSize: 11,
            TextColor: color,
            HorizontalAlign: TextAlignH.Left
        ));

        // Entity/Component info
        var details = GetEventDetails(evt);
        if (!string.IsNullOrEmpty(details))
        {
            WidgetFactory.CreateLabel(world, row, $"Details_{index}", details, font, new LabelConfig(
                FontSize: 11,
                TextColor: EditorColors.TextLight,
                HorizontalAlign: TextAlignH.Left
            ));
        }

        // Show component diff for ComponentChanged events
        if (evt.Type == ReplayEventType.ComponentChanged && evt.Data is not null)
        {
            CreateComponentDiffRow(world, contentArea, font, evt, index);
        }
    }

    private static (string Icon, Vector4 Color, string Label) GetEventDisplayInfo(ReplayEvent evt)
    {
        return evt.Type switch
        {
            ReplayEventType.EntityCreated => ("\u25CF", EntityCreatedColor, "Created"),
            ReplayEventType.EntityDestroyed => ("\u2716", EntityDestroyedColor, "Destroyed"),
            ReplayEventType.ComponentAdded => ("\u25B2", ComponentAddedColor, "Added"),
            ReplayEventType.ComponentRemoved => ("\u25BC", ComponentRemovedColor, "Removed"),
            ReplayEventType.ComponentChanged => ("\u25C6", ComponentChangedColor, "Changed"),
            ReplayEventType.SystemStart => ("\u25B6", SystemEventColor, "Start"),
            ReplayEventType.SystemEnd => ("\u25A0", SystemEventColor, "End"),
            ReplayEventType.Snapshot => ("\u2B24", SnapshotColor, "Snapshot"),
            ReplayEventType.Custom => ("\u2605", CustomEventColor, evt.CustomType ?? "Custom"),
            _ => ("\u25CB", EditorColors.TextMuted, "Unknown")
        };
    }

    private static string GetEventDetails(ReplayEvent evt)
    {
        return evt.Type switch
        {
            ReplayEventType.EntityCreated or ReplayEventType.EntityDestroyed =>
                evt.EntityId.HasValue ? $"Entity {evt.EntityId}" : string.Empty,

            ReplayEventType.ComponentAdded or ReplayEventType.ComponentRemoved or ReplayEventType.ComponentChanged =>
                FormatComponentEvent(evt),

            ReplayEventType.SystemStart or ReplayEventType.SystemEnd =>
                FormatSystemName(evt.SystemTypeName),

            ReplayEventType.Custom =>
                evt.CustomType ?? string.Empty,

            _ => string.Empty
        };
    }

    private static string FormatComponentEvent(ReplayEvent evt)
    {
        var parts = new List<string>();

        if (evt.EntityId.HasValue)
        {
            parts.Add($"Entity {evt.EntityId}");
        }

        if (!string.IsNullOrEmpty(evt.ComponentTypeName))
        {
            var typeName = FormatTypeName(evt.ComponentTypeName);
            parts.Add($"\u2192 {typeName}");
        }

        return string.Join(" ", parts);
    }

    private static string FormatSystemName(string? typeName)
    {
        if (string.IsNullOrEmpty(typeName))
        {
            return string.Empty;
        }

        // Extract just the class name from fully qualified name
        var lastDot = typeName.LastIndexOf('.');
        return lastDot >= 0 ? typeName[(lastDot + 1)..] : typeName;
    }

    private static string FormatTypeName(string typeName)
    {
        // Extract just the class name from fully qualified name
        var lastDot = typeName.LastIndexOf('.');
        return lastDot >= 0 ? typeName[(lastDot + 1)..] : typeName;
    }

    private static void CreateComponentDiffRow(
        IWorld world,
        Entity contentArea,
        FontHandle font,
        ReplayEvent evt,
        int parentIndex)
    {
        if (evt.Data is null)
        {
            return;
        }

        // Create diff container
        var diffContainer = WidgetFactory.CreatePanel(world, contentArea, $"Diff_{parentIndex}", new PanelConfig(
            Direction: LayoutDirection.Vertical,
            BackgroundColor: new Vector4(0.08f, 0.08f, 0.11f, 1f),
            Padding: UIEdges.All(8),
            Spacing: 2
        ));

        ref var diffRect = ref world.Get<UIRect>(diffContainer);
        diffRect.WidthMode = UISizeMode.Fill;
        diffRect.HeightMode = UISizeMode.FitContent;

        world.Add(diffContainer, new FrameInspectorEventTag { EventIndex = parentIndex });

        // Display each changed field
        var fieldIndex = 0;
        foreach (var (key, value) in evt.Data)
        {
            CreateDiffFieldRow(world, diffContainer, font, key, value, fieldIndex);
            fieldIndex++;
        }
    }

    private static void CreateDiffFieldRow(
        IWorld world,
        Entity container,
        FontHandle font,
        string fieldName,
        object value,
        int index)
    {
        var row = WidgetFactory.CreatePanel(world, container, $"DiffField_{index}", new PanelConfig(
            Height: 18,
            Direction: LayoutDirection.Horizontal,
            MainAxisAlign: LayoutAlign.SpaceBetween,
            CrossAxisAlign: LayoutAlign.Center
        ));

        ref var rowRect = ref world.Get<UIRect>(row);
        rowRect.WidthMode = UISizeMode.Fill;

        // Field name
        WidgetFactory.CreateLabel(world, row, $"DiffName_{index}", fieldName, font, new LabelConfig(
            FontSize: 10,
            TextColor: EditorColors.TextMuted,
            HorizontalAlign: TextAlignH.Left
        ));

        // Field value with formatting
        var (valueText, valueColor) = FormatDiffValue(value);
        WidgetFactory.CreateLabel(world, row, $"DiffValue_{index}", valueText, font, new LabelConfig(
            FontSize: 10,
            TextColor: valueColor,
            HorizontalAlign: TextAlignH.Right
        ));
    }

    private static (string Text, Vector4 Color) FormatDiffValue(object value)
    {
        // Check if this is a delta value dictionary
        if (value is IDictionary<string, object> dict)
        {
            if (dict.TryGetValue("before", out var before) && dict.TryGetValue("after", out var after))
            {
                var beforeStr = FormatValue(before);
                var afterStr = FormatValue(after);

                // Calculate delta for numeric types
                var deltaStr = TryCalculateDelta(before, after);
                if (!string.IsNullOrEmpty(deltaStr))
                {
                    var color = deltaStr.StartsWith('-') ? NegativeDeltaColor : PositiveDeltaColor;
                    return ($"{beforeStr} \u2192 {afterStr} ({deltaStr})", color);
                }

                return ($"{beforeStr} \u2192 {afterStr}", ComponentChangedColor);
            }
        }

        return (FormatValue(value), EditorColors.TextLight);
    }

    private static string FormatValue(object? value)
    {
        return value switch
        {
            null => "null",
            float f => f.ToString("F2"),
            double d => d.ToString("F2"),
            Vector2 v2 => $"({v2.X:F2}, {v2.Y:F2})",
            Vector3 v3 => $"({v3.X:F2}, {v3.Y:F2}, {v3.Z:F2})",
            Vector4 v4 => $"({v4.X:F2}, {v4.Y:F2}, {v4.Z:F2}, {v4.W:F2})",
            bool b => b ? "true" : "false",
            string s => s.Length > 20 ? $"\"{s[..17]}...\"" : $"\"{s}\"",
            _ => value.ToString() ?? "?"
        };
    }

    private static string? TryCalculateDelta(object? before, object? after)
    {
        if (before is null || after is null)
        {
            return null;
        }

        try
        {
            return (before, after) switch
            {
                (int b, int a) => FormatDelta(a - b),
                (long b, long a) => FormatDelta(a - b),
                (float b, float a) => FormatDelta(a - b),
                (double b, double a) => FormatDelta(a - b),
                (decimal b, decimal a) => FormatDelta((double)(a - b)),
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }

    private static string FormatDelta(double delta)
    {
        if (Math.Abs(delta) < 0.0001)
        {
            return string.Empty;
        }

        return delta >= 0 ? $"+{delta:F2}" : $"{delta:F2}";
    }

    private static string FormatDelta(long delta)
    {
        if (delta == 0)
        {
            return string.Empty;
        }

        return delta >= 0 ? $"+{delta}" : $"{delta}";
    }

    private static void ShowEmptyState(IWorld world, Entity emptyLabel, string message)
    {
        if (world.Has<UIElement>(emptyLabel))
        {
            ref var element = ref world.Get<UIElement>(emptyLabel);
            element.Visible = true;
        }

        if (world.Has<UIText>(emptyLabel))
        {
            ref var text = ref world.Get<UIText>(emptyLabel);
            text.Content = message;
        }
    }

    private static void HideEmptyState(IWorld world, Entity emptyLabel)
    {
        if (world.Has<UIElement>(emptyLabel))
        {
            ref var element = ref world.Get<UIElement>(emptyLabel);
            element.Visible = false;
        }
    }

    private static void ClearEventEntries(IWorld world, Entity contentArea, Entity preserveEntity)
    {
        var children = world.GetChildren(contentArea).ToList();
        foreach (var child in children)
        {
            if (child == preserveEntity)
            {
                continue;
            }

            if (world.Has<FrameInspectorEventTag>(child))
            {
                DespawnRecursive(world, child);
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
    /// Sets the filter state for event types.
    /// </summary>
    /// <param name="editorWorld">The editor world.</param>
    /// <param name="panel">The frame inspector panel entity.</param>
    /// <param name="showEntities">Whether to show entity events.</param>
    /// <param name="showComponents">Whether to show component events.</param>
    /// <param name="showSystems">Whether to show system events.</param>
    /// <param name="showCustom">Whether to show custom events.</param>
    public static void SetEventFilter(
        IWorld editorWorld,
        Entity panel,
        bool showEntities,
        bool showComponents,
        bool showSystems,
        bool showCustom)
    {
        if (!editorWorld.Has<FrameInspectorPanelState>(panel))
        {
            return;
        }

        ref var state = ref editorWorld.Get<FrameInspectorPanelState>(panel);
        state.ShowEntityEvents = showEntities;
        state.ShowComponentEvents = showComponents;
        state.ShowSystemEvents = showSystems;
        state.ShowCustomEvents = showCustom;

        RefreshFrameDisplay(editorWorld, panel);
    }

    /// <summary>
    /// Sets the entity filter to focus on a specific entity.
    /// </summary>
    /// <param name="editorWorld">The editor world.</param>
    /// <param name="panel">The frame inspector panel entity.</param>
    /// <param name="entityId">The entity ID to filter by, or null to show all entities.</param>
    public static void SetEntityFilter(IWorld editorWorld, Entity panel, int? entityId)
    {
        if (!editorWorld.Has<FrameInspectorPanelState>(panel))
        {
            return;
        }

        ref var state = ref editorWorld.Get<FrameInspectorPanelState>(panel);
        state.FilterEntityId = entityId;

        RefreshFrameDisplay(editorWorld, panel);
    }

    /// <summary>
    /// Manually triggers a refresh of the frame display.
    /// </summary>
    /// <param name="editorWorld">The editor world.</param>
    /// <param name="panel">The frame inspector panel entity.</param>
    public static void Refresh(IWorld editorWorld, Entity panel)
    {
        RefreshFrameDisplay(editorWorld, panel);
    }
}

/// <summary>
/// Component storing the state of the frame inspector panel.
/// </summary>
internal struct FrameInspectorPanelState : IComponent
{
    public Entity Header;
    public Entity Toolbar;
    public Entity ContentArea;
    public Entity EmptyLabel;
    public ReplayPlaybackMode? ReplayPlayback;
    public FontHandle Font;
    public bool ShowEntityEvents;
    public bool ShowComponentEvents;
    public bool ShowSystemEvents;
    public bool ShowCustomEvents;
    public int? FilterEntityId;
    public int ExpandedEventIndex;
}

/// <summary>
/// Component storing header element references.
/// </summary>
internal struct FrameInspectorHeaderState : IComponent
{
    public Entity FrameLabel;
    public Entity DeltaLabel;
}

/// <summary>
/// Component storing toolbar element references.
/// </summary>
internal struct FrameInspectorToolbarState : IComponent
{
    public Entity EntityFilterButton;
    public Entity ComponentFilterButton;
    public Entity SystemFilterButton;
    public Entity CustomFilterButton;
    public Entity EventCountLabel;
}

/// <summary>
/// Tag component to identify frame inspector event rows.
/// </summary>
internal struct FrameInspectorEventTag : IComponent
{
    public int EventIndex;
}

/// <summary>
/// Tag component to identify filter buttons.
/// </summary>
internal struct FrameInspectorFilterButtonTag : IComponent
{
    public string FilterType;
}
