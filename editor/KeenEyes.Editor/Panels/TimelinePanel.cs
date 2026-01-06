using System.Numerics;

using KeenEyes.Editor.Application;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.Input.Abstractions;
using KeenEyes.Replay;
using KeenEyes.UI.Abstractions;
using KeenEyes.UI.Widgets;

namespace KeenEyes.Editor.Panels;

/// <summary>
/// Visual timeline panel for replay playback with scrubber, markers, and transport controls.
/// </summary>
/// <remarks>
/// <para>
/// The TimelinePanel provides a visual interface for controlling replay playback including:
/// </para>
/// <list type="bullet">
/// <item><description>Transport controls (play/pause/stop/step forward/step backward)</description></item>
/// <item><description>Playback speed selector (0.25x to 4x)</description></item>
/// <item><description>Draggable timeline scrubber for seeking</description></item>
/// <item><description>Snapshot markers showing fast seek points</description></item>
/// <item><description>Event markers with color coding by event type</description></item>
/// <item><description>Frame and time display</description></item>
/// </list>
/// </remarks>
public static class TimelinePanel
{
    // Event marker colors based on issue specification
    private static readonly Vector4 EntityCreatedColor = new(0.2f, 0.8f, 0.2f, 1f);   // Green
    private static readonly Vector4 EntityDestroyedColor = new(0.9f, 0.2f, 0.2f, 1f); // Red
    private static readonly Vector4 ComponentAddedColor = new(0.3f, 0.5f, 0.9f, 1f);  // Blue
    private static readonly Vector4 ComponentRemovedColor = new(0.9f, 0.8f, 0.2f, 1f);// Yellow
    private static readonly Vector4 SnapshotMarkerColor = new(0.7f, 0.7f, 0.7f, 1f);  // Gray

    // Speed options
    private static readonly string[] SpeedLabels = ["0.25x", "0.5x", "1x", "2x", "4x"];
    private static readonly float[] SpeedValues = [0.25f, 0.5f, 1f, 2f, 4f];

    /// <summary>
    /// Creates the timeline panel.
    /// </summary>
    /// <param name="editorWorld">The editor UI world.</param>
    /// <param name="parent">The parent container entity.</param>
    /// <param name="font">The font to use for text.</param>
    /// <param name="replayPlayer">The replay player to control.</param>
    /// <returns>The created panel entity.</returns>
    public static Entity Create(
        IWorld editorWorld,
        Entity parent,
        FontHandle font,
        ReplayPlayer? replayPlayer)
    {
        // Create the main panel container
        var panel = WidgetFactory.CreatePanel(editorWorld, parent, "TimelinePanel", new PanelConfig(
            Height: 120,
            Direction: LayoutDirection.Vertical,
            BackgroundColor: EditorColors.DarkPanel,
            Padding: UIEdges.All(4),
            Spacing: 4
        ));

        ref var panelRect = ref editorWorld.Get<UIRect>(panel);
        panelRect.WidthMode = UISizeMode.Fill;

        // Create transport controls and info bar
        var controlsBar = CreateControlsBar(editorWorld, panel, font, replayPlayer);

        // Create timeline area with scrubber
        var timelineArea = CreateTimelineArea(editorWorld, panel, font, replayPlayer);

        // Create marker tracks area
        var markerTracks = CreateMarkerTracks(editorWorld, panel, font, replayPlayer);

        // Store state
        var state = new TimelinePanelState
        {
            ControlsBar = controlsBar.Container,
            TimelineArea = timelineArea.Container,
            MarkerTracks = markerTracks,
            PlayButton = controlsBar.PlayButton,
            PauseButton = controlsBar.PauseButton,
            StopButton = controlsBar.StopButton,
            StepBackButton = controlsBar.StepBackButton,
            StepForwardButton = controlsBar.StepForwardButton,
            SpeedDropdown = controlsBar.SpeedDropdown,
            FrameLabel = controlsBar.FrameLabel,
            TimeLabel = controlsBar.TimeLabel,
            Scrubber = timelineArea.Scrubber,
            ScrubberThumb = timelineArea.ScrubberThumb,
            ScrubberFill = timelineArea.ScrubberFill,
            TimeStartLabel = timelineArea.TimeStartLabel,
            TimeCurrentLabel = timelineArea.TimeCurrentLabel,
            TimeEndLabel = timelineArea.TimeEndLabel,
            SnapshotTrack = markerTracks,
            EventTrack = markerTracks,
            ReplayPlayer = replayPlayer,
            Font = font,
            CurrentSpeedIndex = 2 // 1x speed
        };

        editorWorld.Add(panel, state);

        // Subscribe to player events if available
        if (replayPlayer != null)
        {
            SubscribeToPlayerEvents(editorWorld, panel, replayPlayer);
        }

        // Subscribe to UI events for interaction
        SubscribeToUIEvents(editorWorld, panel);

        return panel;
    }

    private static (Entity Container, Entity PlayButton, Entity PauseButton, Entity StopButton,
        Entity StepBackButton, Entity StepForwardButton, Entity SpeedDropdown,
        Entity FrameLabel, Entity TimeLabel) CreateControlsBar(
        IWorld editorWorld,
        Entity parent,
        FontHandle font,
        ReplayPlayer? player)
    {
        // Create toolbar container
        var toolbar = WidgetFactory.CreatePanel(editorWorld, parent, "TimelineControls", new PanelConfig(
            Height: 32,
            Direction: LayoutDirection.Horizontal,
            MainAxisAlign: LayoutAlign.SpaceBetween,
            CrossAxisAlign: LayoutAlign.Center,
            BackgroundColor: EditorColors.MediumPanel,
            Padding: UIEdges.Symmetric(8, 4),
            Spacing: 4
        ));

        ref var toolbarRect = ref editorWorld.Get<UIRect>(toolbar);
        toolbarRect.WidthMode = UISizeMode.Fill;

        // Left side: Transport controls
        var leftGroup = WidgetFactory.CreatePanel(editorWorld, toolbar, "TransportControls", new PanelConfig(
            Direction: LayoutDirection.Horizontal,
            CrossAxisAlign: LayoutAlign.Center,
            Spacing: 4
        ));

        ref var leftRect = ref editorWorld.Get<UIRect>(leftGroup);
        leftRect.HeightMode = UISizeMode.Fill;

        // Step backward button
        var stepBackButton = CreateTransportButton(editorWorld, leftGroup, "StepBack", "\u23EA", font); // ⏪

        // Play button
        var playButton = CreateTransportButton(editorWorld, leftGroup, "Play", "\u25B6", font); // ▶

        // Pause button
        var pauseButton = CreateTransportButton(editorWorld, leftGroup, "Pause", "\u23F8", font); // ⏸

        // Stop button
        var stopButton = CreateTransportButton(editorWorld, leftGroup, "Stop", "\u25A0", font); // ■

        // Step forward button
        var stepForwardButton = CreateTransportButton(editorWorld, leftGroup, "StepForward", "\u23E9", font); // ⏩

        // Speed selector
        var speedDropdown = WidgetFactory.CreateDropdown(editorWorld, leftGroup, "SpeedSelector",
            SpeedLabels, font, new DropdownConfig(
                Width: 70,
                Height: 24,
                SelectedIndex: 2, // 1x speed
                BackgroundColor: new Vector4(0.2f, 0.2f, 0.25f, 1f),
                TextColor: EditorColors.TextLight,
                FontSize: 11
            ));

        // Right side: Frame and time info
        var rightGroup = WidgetFactory.CreatePanel(editorWorld, toolbar, "TimeInfo", new PanelConfig(
            Direction: LayoutDirection.Horizontal,
            CrossAxisAlign: LayoutAlign.Center,
            Spacing: 16
        ));

        ref var rightRect = ref editorWorld.Get<UIRect>(rightGroup);
        rightRect.HeightMode = UISizeMode.Fill;

        // Frame counter
        var currentFrame = player?.CurrentFrame ?? 0;
        var totalFrames = player?.TotalFrames ?? 0;
        var frameLabel = WidgetFactory.CreateLabel(editorWorld, rightGroup, "FrameLabel",
            $"Frame: {currentFrame} / {totalFrames}", font, new LabelConfig(
                FontSize: 12,
                TextColor: EditorColors.TextLight,
                HorizontalAlign: TextAlignH.Right
            ));

        // Time display
        var currentTime = player?.CurrentTime ?? TimeSpan.Zero;
        var totalDuration = player?.TotalDuration ?? TimeSpan.Zero;
        var timeLabel = WidgetFactory.CreateLabel(editorWorld, rightGroup, "TimeLabel",
            $"{FormatTime(currentTime)} / {FormatTime(totalDuration)}", font, new LabelConfig(
                FontSize: 12,
                TextColor: EditorColors.TextMuted,
                HorizontalAlign: TextAlignH.Right
            ));

        return (toolbar, playButton, pauseButton, stopButton, stepBackButton, stepForwardButton,
            speedDropdown, frameLabel, timeLabel);
    }

    private static Entity CreateTransportButton(IWorld world, Entity parent, string name, string icon, FontHandle font)
    {
        return WidgetFactory.CreateButton(world, parent, name, icon, font, new ButtonConfig(
            Width: 28,
            Height: 24,
            BackgroundColor: new Vector4(0.25f, 0.25f, 0.30f, 1f),
            TextColor: EditorColors.TextLight,
            FontSize: 14,
            CornerRadius: 3
        ));
    }

    private static (Entity Container, Entity Scrubber, Entity ScrubberThumb, Entity ScrubberFill,
        Entity TimeStartLabel, Entity TimeCurrentLabel, Entity TimeEndLabel) CreateTimelineArea(
        IWorld editorWorld,
        Entity parent,
        FontHandle font,
        ReplayPlayer? player)
    {
        // Create timeline container
        var container = WidgetFactory.CreatePanel(editorWorld, parent, "TimelineArea", new PanelConfig(
            Height: 40,
            Direction: LayoutDirection.Vertical,
            BackgroundColor: new Vector4(0.08f, 0.08f, 0.10f, 1f),
            Padding: UIEdges.Symmetric(8, 4),
            Spacing: 2
        ));

        ref var containerRect = ref editorWorld.Get<UIRect>(container);
        containerRect.WidthMode = UISizeMode.Fill;

        // Create scrubber track
        var scrubberTrack = WidgetFactory.CreatePanel(editorWorld, container, "ScrubberTrack", new PanelConfig(
            Height: 16,
            BackgroundColor: new Vector4(0.15f, 0.15f, 0.18f, 1f)
        ));

        ref var trackRect = ref editorWorld.Get<UIRect>(scrubberTrack);
        trackRect.WidthMode = UISizeMode.Fill;

        // Calculate initial position
        float normalizedPosition = 0f;
        if (player is { TotalFrames: > 0 })
        {
            normalizedPosition = (float)player.CurrentFrame / player.TotalFrames;
        }

        // Create fill bar (shows progress)
        var fillBar = editorWorld.Spawn("ScrubberFill")
            .With(new UIElement { Visible = true, RaycastTarget = false })
            .With(new UIRect
            {
                AnchorMin = Vector2.Zero,
                AnchorMax = new Vector2(normalizedPosition, 1f),
                Pivot = Vector2.Zero,
                Size = Vector2.Zero,
                WidthMode = UISizeMode.Fill,
                HeightMode = UISizeMode.Fill
            })
            .With(new UIStyle
            {
                BackgroundColor = new Vector4(0.3f, 0.5f, 0.8f, 0.5f),
                CornerRadius = 2
            })
            .Build();

        editorWorld.SetParent(fillBar, scrubberTrack);

        // Create scrubber thumb (draggable handle)
        var thumbSize = 14f;
        var thumb = editorWorld.Spawn("ScrubberThumb")
            .With(new UIElement { Visible = true, RaycastTarget = true })
            .With(new UIRect
            {
                AnchorMin = new Vector2(normalizedPosition, 0.5f),
                AnchorMax = new Vector2(normalizedPosition, 0.5f),
                Pivot = new Vector2(0.5f, 0.5f),
                Size = new Vector2(thumbSize, thumbSize),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIStyle
            {
                BackgroundColor = new Vector4(0.9f, 0.9f, 0.95f, 1f),
                CornerRadius = thumbSize / 2
            })
            .With(new UIInteractable
            {
                CanClick = true,
                CanDrag = true
            })
            .Build();

        editorWorld.SetParent(thumb, scrubberTrack);

        // Add tag to identify the scrubber for click handling
        editorWorld.Add(scrubberTrack, new TimelineScrubberTag());
        editorWorld.Add(scrubberTrack, new UIInteractable { CanClick = true });

        // Create time labels row
        var timeLabelsRow = WidgetFactory.CreatePanel(editorWorld, container, "TimeLabels", new PanelConfig(
            Height: 14,
            Direction: LayoutDirection.Horizontal,
            MainAxisAlign: LayoutAlign.SpaceBetween,
            CrossAxisAlign: LayoutAlign.Center
        ));

        ref var labelsRect = ref editorWorld.Get<UIRect>(timeLabelsRow);
        labelsRect.WidthMode = UISizeMode.Fill;

        var totalDuration = player?.TotalDuration ?? TimeSpan.Zero;
        var currentTime = player?.CurrentTime ?? TimeSpan.Zero;

        // Start time
        var startLabel = WidgetFactory.CreateLabel(editorWorld, timeLabelsRow, "TimeStart",
            "0:00", font, new LabelConfig(
                FontSize: 10,
                TextColor: EditorColors.TextMuted,
                HorizontalAlign: TextAlignH.Left
            ));

        // Current time indicator
        var currentLabel = WidgetFactory.CreateLabel(editorWorld, timeLabelsRow, "TimeCurrent",
            $"\u2191 {FormatTime(currentTime)}", font, new LabelConfig(
                FontSize: 10,
                TextColor: EditorColors.TextLight,
                HorizontalAlign: TextAlignH.Center
            ));

        // End time
        var endLabel = WidgetFactory.CreateLabel(editorWorld, timeLabelsRow, "TimeEnd",
            FormatTime(totalDuration), font, new LabelConfig(
                FontSize: 10,
                TextColor: EditorColors.TextMuted,
                HorizontalAlign: TextAlignH.Right
            ));

        return (container, scrubberTrack, thumb, fillBar, startLabel, currentLabel, endLabel);
    }

    private static Entity CreateMarkerTracks(
        IWorld editorWorld,
        Entity parent,
        FontHandle font,
        ReplayPlayer? player)
    {
        // Create marker tracks container
        var container = WidgetFactory.CreatePanel(editorWorld, parent, "MarkerTracks", new PanelConfig(
            Height: 36,
            Direction: LayoutDirection.Vertical,
            BackgroundColor: new Vector4(0.06f, 0.06f, 0.08f, 1f),
            Padding: UIEdges.Symmetric(8, 2),
            Spacing: 2
        ));

        ref var containerRect = ref editorWorld.Get<UIRect>(container);
        containerRect.WidthMode = UISizeMode.Fill;

        // Snapshots track row
        var snapshotRow = CreateMarkerTrackRow(editorWorld, container, "Snapshots:", font);
        editorWorld.Add(snapshotRow.Track, new TimelineSnapshotTrackTag());

        // Events track row
        var eventRow = CreateMarkerTrackRow(editorWorld, container, "Events:", font);
        editorWorld.Add(eventRow.Track, new TimelineEventTrackTag());

        // Populate markers if player has data
        if (player?.LoadedReplay != null)
        {
            PopulateSnapshotMarkers(editorWorld, snapshotRow.Track, player.LoadedReplay);
            PopulateEventMarkers(editorWorld, eventRow.Track, player.LoadedReplay);
        }

        return container;
    }

    private static (Entity Row, Entity Track) CreateMarkerTrackRow(
        IWorld editorWorld,
        Entity parent,
        string label,
        FontHandle font)
    {
        var row = WidgetFactory.CreatePanel(editorWorld, parent, $"{label}Row", new PanelConfig(
            Height: 14,
            Direction: LayoutDirection.Horizontal,
            CrossAxisAlign: LayoutAlign.Center,
            Spacing: 8
        ));

        ref var rowRect = ref editorWorld.Get<UIRect>(row);
        rowRect.WidthMode = UISizeMode.Fill;

        // Label
        WidgetFactory.CreateLabel(editorWorld, row, $"{label}Label", label, font, new LabelConfig(
            Width: 70,
            FontSize: 10,
            TextColor: EditorColors.TextMuted,
            HorizontalAlign: TextAlignH.Left
        ));

        // Track area for markers
        var track = WidgetFactory.CreatePanel(editorWorld, row, $"{label}Track", new PanelConfig(
            Height: 12,
            BackgroundColor: new Vector4(0.1f, 0.1f, 0.12f, 1f)
        ));

        ref var trackRect = ref editorWorld.Get<UIRect>(track);
        trackRect.WidthMode = UISizeMode.Fill;

        return (row, track);
    }

    private static void PopulateSnapshotMarkers(IWorld world, Entity track, ReplayData replayData)
    {
        if (replayData.FrameCount == 0)
        {
            return;
        }

        foreach (var snapshot in replayData.Snapshots)
        {
            float normalizedPos = (float)snapshot.FrameNumber / replayData.FrameCount;
            CreateMarker(world, track, normalizedPos, SnapshotMarkerColor, "\u25CF", snapshot.FrameNumber);
        }
    }

    private static void PopulateEventMarkers(IWorld world, Entity track, ReplayData replayData)
    {
        if (replayData.FrameCount == 0)
        {
            return;
        }

        foreach (var frame in replayData.Frames)
        {
            foreach (var evt in frame.Events)
            {
                var color = GetEventColor(evt.Type);
                if (color.HasValue)
                {
                    float normalizedPos = (float)frame.FrameNumber / replayData.FrameCount;
                    var icon = GetEventIcon(evt.Type);
                    CreateMarker(world, track, normalizedPos, color.Value, icon, frame.FrameNumber);
                }
            }
        }
    }

    private static void CreateMarker(IWorld world, Entity track, float normalizedPosition, Vector4 color, string icon, int frameNumber)
    {
        var marker = world.Spawn($"Marker_{frameNumber}")
            .With(new UIElement { Visible = true, RaycastTarget = true })
            .With(new UIRect
            {
                AnchorMin = new Vector2(normalizedPosition, 0.5f),
                AnchorMax = new Vector2(normalizedPosition, 0.5f),
                Pivot = new Vector2(0.5f, 0.5f),
                Size = new Vector2(8, 12),
                WidthMode = UISizeMode.Fixed,
                HeightMode = UISizeMode.Fixed
            })
            .With(new UIText
            {
                Content = icon,
                Font = default, // Will use default font
                Color = color,
                FontSize = 10,
                HorizontalAlign = TextAlignH.Center,
                VerticalAlign = TextAlignV.Middle
            })
            .With(new UIInteractable { CanClick = true })
            .With(new TimelineMarkerTag { FrameNumber = frameNumber })
            .Build();

        world.SetParent(marker, track);
    }

    private static Vector4? GetEventColor(ReplayEventType eventType)
    {
        return eventType switch
        {
            ReplayEventType.EntityCreated => EntityCreatedColor,
            ReplayEventType.EntityDestroyed => EntityDestroyedColor,
            ReplayEventType.ComponentAdded => ComponentAddedColor,
            ReplayEventType.ComponentRemoved => ComponentRemovedColor,
            _ => null // Don't show markers for other event types
        };
    }

    private static string GetEventIcon(ReplayEventType eventType)
    {
        return eventType switch
        {
            ReplayEventType.EntityCreated => "\u25B2",    // ▲
            ReplayEventType.EntityDestroyed => "\u25BC",  // ▼
            ReplayEventType.ComponentAdded => "\u25CF",   // ●
            ReplayEventType.ComponentRemoved => "\u25CB", // ○
            _ => "\u25CF"
        };
    }

    private static void SubscribeToPlayerEvents(IWorld world, Entity panel, ReplayPlayer player)
    {
        player.FrameChanged += frameIndex => OnFrameChanged(world, panel, frameIndex);
        player.PlaybackStarted += () => OnPlaybackStateChanged(world, panel, PlaybackState.Playing);
        player.PlaybackPaused += () => OnPlaybackStateChanged(world, panel, PlaybackState.Paused);
        player.PlaybackStopped += () => OnPlaybackStateChanged(world, panel, PlaybackState.Stopped);
        player.PlaybackEnded += () => OnPlaybackStateChanged(world, panel, PlaybackState.Stopped);
    }

    private static void SubscribeToUIEvents(IWorld world, Entity panel)
    {
        // Subscribe to click events for transport controls
        world.Subscribe<UIClickEvent>(e => OnUIClick(world, panel, e));

        // Subscribe to drag events for scrubber
        world.Subscribe<UIDragEvent>(e => OnUIDrag(world, panel, e));
    }

    private static void OnUIClick(IWorld world, Entity panel, UIClickEvent e)
    {
        if (!world.Has<TimelinePanelState>(panel))
        {
            return;
        }

        ref var state = ref world.Get<TimelinePanelState>(panel);
        var player = state.ReplayPlayer;

        if (player == null || !player.IsLoaded)
        {
            return;
        }

        // Check transport buttons
        if (e.Element == state.PlayButton)
        {
            player.Play();
        }
        else if (e.Element == state.PauseButton)
        {
            player.Pause();
        }
        else if (e.Element == state.StopButton)
        {
            player.Stop();
        }
        else if (e.Element == state.StepBackButton)
        {
            player.Step(-1);
        }
        else if (e.Element == state.StepForwardButton)
        {
            player.Step(1);
        }
        else if (e.Element == state.Scrubber)
        {
            // Click on scrubber track to seek
            SeekToPosition(world, panel, e.Position);
        }
        else if (world.Has<TimelineMarkerTag>(e.Element))
        {
            // Click on marker to jump to that frame
            ref readonly var marker = ref world.Get<TimelineMarkerTag>(e.Element);
            player.SeekToFrame(marker.FrameNumber);
        }
    }

    private static void OnUIDrag(IWorld world, Entity panel, UIDragEvent e)
    {
        if (!world.Has<TimelinePanelState>(panel))
        {
            return;
        }

        ref readonly var state = ref world.Get<TimelinePanelState>(panel);

        // Check if dragging the scrubber thumb
        if (e.Element == state.ScrubberThumb)
        {
            SeekToPosition(world, panel, e.Position);
        }
    }

    private static void SeekToPosition(IWorld world, Entity panel, Vector2 position)
    {
        if (!world.Has<TimelinePanelState>(panel))
        {
            return;
        }

        ref readonly var state = ref world.Get<TimelinePanelState>(panel);
        var player = state.ReplayPlayer;

        if (player == null || !player.IsLoaded || player.TotalFrames == 0)
        {
            return;
        }

        // Get scrubber bounds
        if (!world.Has<UIRect>(state.Scrubber))
        {
            return;
        }

        ref readonly var scrubberRect = ref world.Get<UIRect>(state.Scrubber);
        var bounds = scrubberRect.ComputedBounds;

        if (bounds.Width <= 0)
        {
            return;
        }

        // Calculate normalized position
        var normalizedX = (position.X - bounds.X) / bounds.Width;
        normalizedX = Math.Clamp(normalizedX, 0f, 1f);

        // Convert to frame number and seek
        var targetFrame = (int)(normalizedX * (player.TotalFrames - 1));
        player.SeekToFrame(targetFrame);
    }

    private static void OnFrameChanged(IWorld world, Entity panel, int frameIndex)
    {
        if (!world.IsAlive(panel) || !world.Has<TimelinePanelState>(panel))
        {
            return;
        }

        UpdateScrubberPosition(world, panel);
        UpdateTimeLabels(world, panel);
    }

    private static void OnPlaybackStateChanged(IWorld world, Entity panel, PlaybackState newState)
    {
        if (!world.IsAlive(panel) || !world.Has<TimelinePanelState>(panel))
        {
            return;
        }

        UpdateTransportButtonStates(world, panel, newState);
    }

    private static void UpdateScrubberPosition(IWorld world, Entity panel)
    {
        ref readonly var state = ref world.Get<TimelinePanelState>(panel);
        var player = state.ReplayPlayer;

        if (player == null || player.TotalFrames == 0)
        {
            return;
        }

        float normalizedPos = (float)player.CurrentFrame / player.TotalFrames;
        normalizedPos = Math.Clamp(normalizedPos, 0f, 1f);

        // Update thumb position
        if (state.ScrubberThumb.IsValid && world.Has<UIRect>(state.ScrubberThumb))
        {
            ref var thumbRect = ref world.Get<UIRect>(state.ScrubberThumb);
            thumbRect.AnchorMin = new Vector2(normalizedPos, 0.5f);
            thumbRect.AnchorMax = new Vector2(normalizedPos, 0.5f);

            if (!world.Has<UILayoutDirtyTag>(state.ScrubberThumb))
            {
                world.Add(state.ScrubberThumb, new UILayoutDirtyTag());
            }
        }

        // Update fill bar
        if (state.ScrubberFill.IsValid && world.Has<UIRect>(state.ScrubberFill))
        {
            ref var fillRect = ref world.Get<UIRect>(state.ScrubberFill);
            fillRect.AnchorMax = new Vector2(normalizedPos, 1f);

            if (!world.Has<UILayoutDirtyTag>(state.ScrubberFill))
            {
                world.Add(state.ScrubberFill, new UILayoutDirtyTag());
            }
        }
    }

    private static void UpdateTimeLabels(IWorld world, Entity panel)
    {
        ref readonly var state = ref world.Get<TimelinePanelState>(panel);
        var player = state.ReplayPlayer;

        if (player == null)
        {
            return;
        }

        // Update frame label
        if (state.FrameLabel.IsValid && world.Has<UIText>(state.FrameLabel))
        {
            ref var frameText = ref world.Get<UIText>(state.FrameLabel);
            frameText.Content = $"Frame: {player.CurrentFrame} / {player.TotalFrames}";
        }

        // Update time label
        if (state.TimeLabel.IsValid && world.Has<UIText>(state.TimeLabel))
        {
            ref var timeText = ref world.Get<UIText>(state.TimeLabel);
            timeText.Content = $"{FormatTime(player.CurrentTime)} / {FormatTime(player.TotalDuration)}";
        }

        // Update current time indicator
        if (state.TimeCurrentLabel.IsValid && world.Has<UIText>(state.TimeCurrentLabel))
        {
            ref var currentText = ref world.Get<UIText>(state.TimeCurrentLabel);
            currentText.Content = $"\u2191 {FormatTime(player.CurrentTime)}";
        }
    }

    private static void UpdateTransportButtonStates(IWorld world, Entity panel, PlaybackState state)
    {
        // Update button visual states based on playback state
        // (Visual feedback for which state is active)
        ref readonly var panelState = ref world.Get<TimelinePanelState>(panel);

        // Highlight play button when playing
        if (panelState.PlayButton.IsValid && world.Has<UIStyle>(panelState.PlayButton))
        {
            ref var playStyle = ref world.Get<UIStyle>(panelState.PlayButton);
            playStyle.BackgroundColor = state == PlaybackState.Playing
                ? new Vector4(0.3f, 0.5f, 0.3f, 1f)
                : new Vector4(0.25f, 0.25f, 0.30f, 1f);
        }

        // Highlight pause button when paused
        if (panelState.PauseButton.IsValid && world.Has<UIStyle>(panelState.PauseButton))
        {
            ref var pauseStyle = ref world.Get<UIStyle>(panelState.PauseButton);
            pauseStyle.BackgroundColor = state == PlaybackState.Paused
                ? new Vector4(0.5f, 0.5f, 0.3f, 1f)
                : new Vector4(0.25f, 0.25f, 0.30f, 1f);
        }
    }

    /// <summary>
    /// Updates the timeline panel with a new replay player.
    /// </summary>
    /// <param name="world">The editor world.</param>
    /// <param name="panel">The timeline panel entity.</param>
    /// <param name="player">The new replay player.</param>
    public static void SetReplayPlayer(IWorld world, Entity panel, ReplayPlayer? player)
    {
        if (!world.Has<TimelinePanelState>(panel))
        {
            return;
        }

        ref var state = ref world.Get<TimelinePanelState>(panel);

        // Update player reference
        state.ReplayPlayer = player;

        // Subscribe to new player events
        if (player != null)
        {
            SubscribeToPlayerEvents(world, panel, player);

            // Refresh markers if replay is loaded
            if (player.LoadedReplay != null)
            {
                RefreshMarkers(world, panel, player.LoadedReplay);
            }
        }

        // Update UI
        UpdateScrubberPosition(world, panel);
        UpdateTimeLabels(world, panel);
    }

    /// <summary>
    /// Refreshes the marker tracks with data from the loaded replay.
    /// </summary>
    /// <param name="world">The editor world.</param>
    /// <param name="panel">The timeline panel entity.</param>
    /// <param name="replayData">The replay data to use for markers.</param>
    public static void RefreshMarkers(IWorld world, Entity panel, ReplayData replayData)
    {
        if (!world.Has<TimelinePanelState>(panel))
        {
            return;
        }

        ref readonly var state = ref world.Get<TimelinePanelState>(panel);

        // Clear existing markers and repopulate
        // Find snapshot and event tracks
        foreach (var child in world.GetChildren(state.MarkerTracks))
        {
            if (world.Has<TimelineSnapshotTrackTag>(child))
            {
                ClearMarkers(world, child);
                PopulateSnapshotMarkers(world, child, replayData);
            }
            else if (world.Has<TimelineEventTrackTag>(child))
            {
                ClearMarkers(world, child);
                PopulateEventMarkers(world, child, replayData);
            }
        }
    }

    private static void ClearMarkers(IWorld world, Entity track)
    {
        var children = world.GetChildren(track).ToList();
        foreach (var child in children)
        {
            if (world.Has<TimelineMarkerTag>(child))
            {
                world.Despawn(child);
            }
        }
    }

    /// <summary>
    /// Handles keyboard input for timeline navigation.
    /// </summary>
    /// <param name="world">The editor world.</param>
    /// <param name="panel">The timeline panel entity.</param>
    /// <param name="key">The key that was pressed.</param>
    /// <returns>True if the key was handled, false otherwise.</returns>
    public static bool HandleKeyDown(IWorld world, Entity panel, Key key)
    {
        if (!world.Has<TimelinePanelState>(panel))
        {
            return false;
        }

        ref readonly var state = ref world.Get<TimelinePanelState>(panel);
        var player = state.ReplayPlayer;

        if (player == null || !player.IsLoaded)
        {
            return false;
        }

        switch (key)
        {
            case Key.Space:
                // Toggle play/pause
                if (player.State == PlaybackState.Playing)
                {
                    player.Pause();
                }
                else
                {
                    player.Play();
                }
                return true;

            case Key.Left:
                // Step backward
                player.Step(-1);
                return true;

            case Key.Right:
                // Step forward
                player.Step(1);
                return true;

            case Key.Home:
                // Jump to beginning
                player.SeekToFrame(0);
                return true;

            case Key.End:
                // Jump to end
                if (player.TotalFrames > 0)
                {
                    player.SeekToFrame(player.TotalFrames - 1);
                }
                return true;

            default:
                return false;
        }
    }

    private static string FormatTime(TimeSpan time)
    {
        if (time.TotalHours >= 1)
        {
            return $"{(int)time.TotalHours}:{time.Minutes:D2}:{time.Seconds:D2}";
        }
        return $"{time.Minutes}:{time.Seconds:D2}";
    }

    /// <summary>
    /// Gets the playback speed value for a given speed index.
    /// </summary>
    /// <param name="speedIndex">The index into the speed options (0-4).</param>
    /// <returns>The playback speed multiplier (0.25, 0.5, 1, 2, or 4).</returns>
    public static float GetSpeedValue(int speedIndex)
    {
        if (speedIndex < 0 || speedIndex >= SpeedValues.Length)
        {
            return 1f; // Default to normal speed
        }
        return SpeedValues[speedIndex];
    }
}

/// <summary>
/// Component storing the state of the timeline panel.
/// </summary>
internal struct TimelinePanelState : IComponent
{
    public Entity ControlsBar;
    public Entity TimelineArea;
    public Entity MarkerTracks;
    public Entity PlayButton;
    public Entity PauseButton;
    public Entity StopButton;
    public Entity StepBackButton;
    public Entity StepForwardButton;
    public Entity SpeedDropdown;
    public Entity FrameLabel;
    public Entity TimeLabel;
    public Entity Scrubber;
    public Entity ScrubberThumb;
    public Entity ScrubberFill;
    public Entity TimeStartLabel;
    public Entity TimeCurrentLabel;
    public Entity TimeEndLabel;
    public Entity SnapshotTrack;
    public Entity EventTrack;
    public ReplayPlayer? ReplayPlayer;
    public FontHandle Font;
    public int CurrentSpeedIndex;
}

/// <summary>
/// Tag component to identify the scrubber track for click handling.
/// </summary>
internal struct TimelineScrubberTag : IComponent;

/// <summary>
/// Tag component to identify the snapshot marker track.
/// </summary>
internal struct TimelineSnapshotTrackTag : IComponent;

/// <summary>
/// Tag component to identify the event marker track.
/// </summary>
internal struct TimelineEventTrackTag : IComponent;

/// <summary>
/// Tag component for timeline markers with frame information.
/// </summary>
internal struct TimelineMarkerTag : IComponent
{
    public int FrameNumber;
}
