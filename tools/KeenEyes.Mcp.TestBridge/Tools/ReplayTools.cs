using System.ComponentModel;
using KeenEyes.Mcp.TestBridge.Connection;
using KeenEyes.TestBridge.Replay;
using ModelContextProtocol.Server;

namespace KeenEyes.Mcp.TestBridge.Tools;

/// <summary>
/// MCP tools for replay recording and playback.
/// </summary>
/// <remarks>
/// <para>
/// These tools allow recording gameplay sessions, saving/loading replay files,
/// controlling playback with pause/seek/step operations, and validating replays.
/// </para>
/// <para>
/// Recording requires the ReplayPlugin to be installed on the game world.
/// Playback can be done independently using the built-in ReplayPlayer.
/// </para>
/// </remarks>
[McpServerToolType]
public sealed class ReplayTools(BridgeConnectionManager connection)
{
    #region Recording Control

    [McpServerTool(Name = "replay_start_recording")]
    [Description("Start recording gameplay. Requires ReplayPlugin to be installed on the world.")]
    public async Task<ReplayOperationResultMcp> StartRecording(
        [Description("Optional name for this recording")]
        string? name = null,
        [Description("Maximum frames to record (default: 36000 = 10 minutes at 60fps)")]
        int maxFrames = 36000,
        [Description("Interval in milliseconds between world state snapshots (default: 5000 = 5 seconds)")]
        int snapshotIntervalMs = 5000)
    {
        var bridge = connection.GetBridge();
        var result = await bridge.Replay.StartRecordingAsync(name, maxFrames, snapshotIntervalMs);
        return ReplayOperationResultMcp.FromResult(result);
    }

    [McpServerTool(Name = "replay_stop_recording")]
    [Description("Stop recording and keep the recording data in memory. Use replay_save to persist.")]
    public async Task<ReplayOperationResultMcp> StopRecording()
    {
        var bridge = connection.GetBridge();
        var result = await bridge.Replay.StopRecordingAsync();
        return ReplayOperationResultMcp.FromResult(result);
    }

    [McpServerTool(Name = "replay_cancel_recording")]
    [Description("Cancel recording and discard all recorded data.")]
    public async Task<ReplayOperationResultMcp> CancelRecording()
    {
        var bridge = connection.GetBridge();
        var result = await bridge.Replay.CancelRecordingAsync();
        return ReplayOperationResultMcp.FromResult(result);
    }

    [McpServerTool(Name = "replay_is_recording")]
    [Description("Check if recording is currently in progress.")]
    public async Task<RecordingStatusResult> IsRecording()
    {
        var bridge = connection.GetBridge();
        var isRecording = await bridge.Replay.IsRecordingAsync();
        var info = await bridge.Replay.GetRecordingInfoAsync();

        return new RecordingStatusResult
        {
            IsRecording = isRecording,
            Info = info != null ? RecordingInfoResult.FromSnapshot(info) : null
        };
    }

    [McpServerTool(Name = "replay_force_snapshot")]
    [Description("Force a world state snapshot during recording.")]
    public async Task<ReplayOperationResultMcp> ForceSnapshot()
    {
        var bridge = connection.GetBridge();
        var result = await bridge.Replay.ForceSnapshotAsync();
        return ReplayOperationResultMcp.FromResult(result);
    }

    #endregion

    #region Recording Management

    [McpServerTool(Name = "replay_save")]
    [Description("Save the current recording to a file.")]
    public async Task<ReplayOperationResultMcp> Save(
        [Description("File path to save the replay to (with .kreplay extension)")]
        string path)
    {
        var bridge = connection.GetBridge();
        var result = await bridge.Replay.SaveAsync(path);
        return ReplayOperationResultMcp.FromResult(result);
    }

    [McpServerTool(Name = "replay_load")]
    [Description("Load a replay file for playback.")]
    public async Task<ReplayOperationResultMcp> Load(
        [Description("File path of the replay to load")]
        string path)
    {
        var bridge = connection.GetBridge();
        var result = await bridge.Replay.LoadAsync(path);
        return ReplayOperationResultMcp.FromResult(result);
    }

    [McpServerTool(Name = "replay_list")]
    [Description("List all replay files in a directory.")]
    public async Task<ReplayFileListResult> List(
        [Description("Directory to search for replay files (null = current directory)")]
        string? directory = null)
    {
        var bridge = connection.GetBridge();
        var files = await bridge.Replay.ListAsync(directory);
        return new ReplayFileListResult
        {
            Count = files.Count,
            Directory = directory ?? Environment.CurrentDirectory,
            Files = files.Select(ReplayFileResult.FromSnapshot).ToList()
        };
    }

    [McpServerTool(Name = "replay_delete")]
    [Description("Delete a replay file.")]
    public async Task<ReplayDeleteResult> Delete(
        [Description("File path of the replay to delete")]
        string path)
    {
        var bridge = connection.GetBridge();
        var deleted = await bridge.Replay.DeleteAsync(path);
        return new ReplayDeleteResult
        {
            Success = deleted,
            Path = path,
            Message = deleted ? $"Replay '{path}' deleted" : $"Replay '{path}' not found or could not be deleted"
        };
    }

    [McpServerTool(Name = "replay_get_metadata")]
    [Description("Get metadata from a replay file without loading it for playback.")]
    public async Task<ReplayMetadataResult?> GetMetadata(
        [Description("File path of the replay")]
        string path)
    {
        var bridge = connection.GetBridge();
        var metadata = await bridge.Replay.GetMetadataAsync(path);
        return metadata != null ? ReplayMetadataResult.FromSnapshot(metadata) : null;
    }

    #endregion

    #region Playback Control

    [McpServerTool(Name = "replay_play")]
    [Description("Start or resume playback of the loaded replay.")]
    public async Task<ReplayOperationResultMcp> Play()
    {
        var bridge = connection.GetBridge();
        var result = await bridge.Replay.PlayAsync();
        return ReplayOperationResultMcp.FromResult(result);
    }

    [McpServerTool(Name = "replay_pause")]
    [Description("Pause playback.")]
    public async Task<ReplayOperationResultMcp> Pause()
    {
        var bridge = connection.GetBridge();
        var result = await bridge.Replay.PauseAsync();
        return ReplayOperationResultMcp.FromResult(result);
    }

    [McpServerTool(Name = "replay_stop")]
    [Description("Stop playback and reset to the beginning.")]
    public async Task<ReplayOperationResultMcp> Stop()
    {
        var bridge = connection.GetBridge();
        var result = await bridge.Replay.StopPlaybackAsync();
        return ReplayOperationResultMcp.FromResult(result);
    }

    [McpServerTool(Name = "replay_get_playback_state")]
    [Description("Get the current playback state.")]
    public async Task<PlaybackStateResult> GetPlaybackState()
    {
        var bridge = connection.GetBridge();
        var state = await bridge.Replay.GetPlaybackStateAsync();
        return PlaybackStateResult.FromSnapshot(state);
    }

    [McpServerTool(Name = "replay_set_speed")]
    [Description("Set the playback speed (0.25x to 4.0x).")]
    public async Task<ReplayOperationResultMcp> SetSpeed(
        [Description("Playback speed multiplier (0.25 to 4.0)")]
        float speed)
    {
        var bridge = connection.GetBridge();
        var result = await bridge.Replay.SetSpeedAsync(speed);
        return ReplayOperationResultMcp.FromResult(result);
    }

    #endregion

    #region Playback Navigation

    [McpServerTool(Name = "replay_seek_frame")]
    [Description("Seek to a specific frame in the replay.")]
    public async Task<ReplayOperationResultMcp> SeekFrame(
        [Description("Frame number to seek to")]
        int frame)
    {
        var bridge = connection.GetBridge();
        var result = await bridge.Replay.SeekFrameAsync(frame);
        return ReplayOperationResultMcp.FromResult(result);
    }

    [McpServerTool(Name = "replay_seek_time")]
    [Description("Seek to a specific time in the replay.")]
    public async Task<ReplayOperationResultMcp> SeekTime(
        [Description("Time in seconds to seek to")]
        float seconds)
    {
        var bridge = connection.GetBridge();
        var result = await bridge.Replay.SeekTimeAsync(seconds);
        return ReplayOperationResultMcp.FromResult(result);
    }

    [McpServerTool(Name = "replay_step_forward")]
    [Description("Step forward by a number of frames (pauses if playing).")]
    public async Task<ReplayOperationResultMcp> StepForward(
        [Description("Number of frames to step forward")]
        int frames = 1)
    {
        var bridge = connection.GetBridge();
        var result = await bridge.Replay.StepForwardAsync(frames);
        return ReplayOperationResultMcp.FromResult(result);
    }

    [McpServerTool(Name = "replay_step_backward")]
    [Description("Step backward by a number of frames (pauses if playing).")]
    public async Task<ReplayOperationResultMcp> StepBackward(
        [Description("Number of frames to step backward")]
        int frames = 1)
    {
        var bridge = connection.GetBridge();
        var result = await bridge.Replay.StepBackwardAsync(frames);
        return ReplayOperationResultMcp.FromResult(result);
    }

    #endregion

    #region Frame Inspection

    [McpServerTool(Name = "replay_get_frame")]
    [Description("Get details about a specific frame.")]
    public async Task<ReplayFrameResult?> GetFrame(
        [Description("Frame number to inspect")]
        int frame)
    {
        var bridge = connection.GetBridge();
        var frameData = await bridge.Replay.GetFrameAsync(frame);
        return frameData != null ? ReplayFrameResult.FromSnapshot(frameData) : null;
    }

    [McpServerTool(Name = "replay_get_frame_range")]
    [Description("Get details about a range of frames.")]
    public async Task<FrameRangeResult> GetFrameRange(
        [Description("Starting frame number")]
        int startFrame,
        [Description("Number of frames to retrieve")]
        int count)
    {
        var bridge = connection.GetBridge();
        var frames = await bridge.Replay.GetFrameRangeAsync(startFrame, count);
        return new FrameRangeResult
        {
            StartFrame = startFrame,
            Count = frames.Count,
            Frames = frames.Select(ReplayFrameResult.FromSnapshot).ToList()
        };
    }

    [McpServerTool(Name = "replay_get_inputs")]
    [Description("Get input events for a range of frames.")]
    public async Task<InputEventsResult> GetInputs(
        [Description("Starting frame number")]
        int startFrame,
        [Description("Ending frame number")]
        int endFrame)
    {
        var bridge = connection.GetBridge();
        var inputs = await bridge.Replay.GetInputsAsync(startFrame, endFrame);
        return new InputEventsResult
        {
            StartFrame = startFrame,
            EndFrame = endFrame,
            Count = inputs.Count,
            Inputs = inputs.Select(InputEventResult.FromSnapshot).ToList()
        };
    }

    [McpServerTool(Name = "replay_get_events")]
    [Description("Get replay events (entity spawns, despawns, etc.) for a range of frames.")]
    public async Task<ReplayEventsResult> GetEvents(
        [Description("Starting frame number")]
        int startFrame,
        [Description("Ending frame number")]
        int endFrame)
    {
        var bridge = connection.GetBridge();
        var events = await bridge.Replay.GetEventsAsync(startFrame, endFrame);
        return new ReplayEventsResult
        {
            StartFrame = startFrame,
            EndFrame = endFrame,
            Count = events.Count,
            Events = events.Select(ReplayEventResult.FromSnapshot).ToList()
        };
    }

    [McpServerTool(Name = "replay_get_snapshots")]
    [Description("Get a list of all world state snapshots in the loaded replay.")]
    public async Task<SnapshotMarkersResult> GetSnapshots()
    {
        var bridge = connection.GetBridge();
        var snapshots = await bridge.Replay.GetSnapshotsAsync();
        return new SnapshotMarkersResult
        {
            Count = snapshots.Count,
            Snapshots = snapshots.Select(SnapshotMarkerResult.FromSnapshot).ToList()
        };
    }

    #endregion

    #region Validation

    [McpServerTool(Name = "replay_validate")]
    [Description("Validate a replay file for integrity and compatibility.")]
    public async Task<ValidationResult> Validate(
        [Description("File path of the replay to validate")]
        string path)
    {
        var bridge = connection.GetBridge();
        var result = await bridge.Replay.ValidateAsync(path);
        return ValidationResult.FromSnapshot(result);
    }

    [McpServerTool(Name = "replay_check_determinism")]
    [Description("Check if the loaded replay has determinism issues (checksum mismatches).")]
    public async Task<DeterminismResult> CheckDeterminism()
    {
        var bridge = connection.GetBridge();
        var result = await bridge.Replay.CheckDeterminismAsync();
        return DeterminismResult.FromSnapshot(result);
    }

    #endregion
}

#region Result Records

/// <summary>
/// Result of a replay operation.
/// </summary>
public sealed record ReplayOperationResultMcp
{
    /// <summary>
    /// Gets whether the operation was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets an error message if the operation failed.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Gets recording info if available.
    /// </summary>
    public RecordingInfoResult? RecordingInfo { get; init; }

    /// <summary>
    /// Gets playback state if available.
    /// </summary>
    public PlaybackStateResult? PlaybackState { get; init; }

    /// <summary>
    /// Gets metadata if available.
    /// </summary>
    public ReplayMetadataResult? Metadata { get; init; }

    /// <summary>
    /// Creates from a ReplayOperationResult.
    /// </summary>
    public static ReplayOperationResultMcp FromResult(ReplayOperationResult result)
    {
        return new ReplayOperationResultMcp
        {
            Success = result.Success,
            Error = result.Error,
            RecordingInfo = result.RecordingInfo != null
                ? RecordingInfoResult.FromSnapshot(result.RecordingInfo)
                : null,
            PlaybackState = result.PlaybackState != null
                ? PlaybackStateResult.FromSnapshot(result.PlaybackState)
                : null,
            Metadata = result.Metadata != null
                ? ReplayMetadataResult.FromSnapshot(result.Metadata)
                : null
        };
    }
}

/// <summary>
/// Recording status information.
/// </summary>
public sealed record RecordingStatusResult
{
    /// <summary>
    /// Gets whether recording is in progress.
    /// </summary>
    public required bool IsRecording { get; init; }

    /// <summary>
    /// Gets recording info if recording.
    /// </summary>
    public RecordingInfoResult? Info { get; init; }
}

/// <summary>
/// Recording information for MCP results.
/// </summary>
public sealed record RecordingInfoResult
{
    /// <summary>
    /// Gets whether recording is in progress.
    /// </summary>
    public required bool IsRecording { get; init; }

    /// <summary>
    /// Gets the recording name.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets the number of frames recorded.
    /// </summary>
    public required int FrameCount { get; init; }

    /// <summary>
    /// Gets the recording duration in seconds.
    /// </summary>
    public required float DurationSeconds { get; init; }

    /// <summary>
    /// Gets the number of snapshots taken.
    /// </summary>
    public required int SnapshotCount { get; init; }

    /// <summary>
    /// Gets when recording started.
    /// </summary>
    public DateTimeOffset? StartedAt { get; init; }

    /// <summary>
    /// Creates from a RecordingInfoSnapshot.
    /// </summary>
    public static RecordingInfoResult FromSnapshot(RecordingInfoSnapshot snapshot)
    {
        return new RecordingInfoResult
        {
            IsRecording = snapshot.IsRecording,
            Name = snapshot.Name,
            FrameCount = snapshot.FrameCount,
            DurationSeconds = snapshot.DurationSeconds,
            SnapshotCount = snapshot.SnapshotCount,
            StartedAt = snapshot.StartedAt
        };
    }
}

/// <summary>
/// Playback state for MCP results.
/// </summary>
public sealed record PlaybackStateResult
{
    /// <summary>
    /// Gets whether a replay is loaded.
    /// </summary>
    public required bool IsLoaded { get; init; }

    /// <summary>
    /// Gets whether playback is active.
    /// </summary>
    public required bool IsPlaying { get; init; }

    /// <summary>
    /// Gets whether playback is paused.
    /// </summary>
    public required bool IsPaused { get; init; }

    /// <summary>
    /// Gets whether playback is stopped.
    /// </summary>
    public required bool IsStopped { get; init; }

    /// <summary>
    /// Gets the current frame number.
    /// </summary>
    public required int CurrentFrame { get; init; }

    /// <summary>
    /// Gets the total number of frames.
    /// </summary>
    public required int TotalFrames { get; init; }

    /// <summary>
    /// Gets the current time in seconds.
    /// </summary>
    public required float CurrentTimeSeconds { get; init; }

    /// <summary>
    /// Gets the total time in seconds.
    /// </summary>
    public required float TotalTimeSeconds { get; init; }

    /// <summary>
    /// Gets the playback speed multiplier.
    /// </summary>
    public required float PlaybackSpeed { get; init; }

    /// <summary>
    /// Gets the name of the loaded replay.
    /// </summary>
    public string? ReplayName { get; init; }

    /// <summary>
    /// Creates from a PlaybackStateSnapshot.
    /// </summary>
    public static PlaybackStateResult FromSnapshot(PlaybackStateSnapshot snapshot)
    {
        return new PlaybackStateResult
        {
            IsLoaded = snapshot.IsLoaded,
            IsPlaying = snapshot.IsPlaying,
            IsPaused = snapshot.IsPaused,
            IsStopped = snapshot.IsStopped,
            CurrentFrame = snapshot.CurrentFrame,
            TotalFrames = snapshot.TotalFrames,
            CurrentTimeSeconds = snapshot.CurrentTimeSeconds,
            TotalTimeSeconds = snapshot.TotalTimeSeconds,
            PlaybackSpeed = snapshot.PlaybackSpeed,
            ReplayName = snapshot.ReplayName
        };
    }
}

/// <summary>
/// Replay file list result.
/// </summary>
public sealed record ReplayFileListResult
{
    /// <summary>
    /// Gets the number of files found.
    /// </summary>
    public required int Count { get; init; }

    /// <summary>
    /// Gets the directory that was searched.
    /// </summary>
    public required string Directory { get; init; }

    /// <summary>
    /// Gets the list of files.
    /// </summary>
    public required List<ReplayFileResult> Files { get; init; }
}

/// <summary>
/// Replay file information for MCP results.
/// </summary>
public sealed record ReplayFileResult
{
    /// <summary>
    /// Gets the file path.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Gets the file name.
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    /// Gets the file size in bytes.
    /// </summary>
    public required long SizeBytes { get; init; }

    /// <summary>
    /// Gets when the file was last modified.
    /// </summary>
    public required DateTimeOffset LastModified { get; init; }

    /// <summary>
    /// Gets the replay metadata, if available.
    /// </summary>
    public ReplayMetadataResult? Metadata { get; init; }

    /// <summary>
    /// Gets any validation error.
    /// </summary>
    public string? ValidationError { get; init; }

    /// <summary>
    /// Creates from a ReplayFileSnapshot.
    /// </summary>
    public static ReplayFileResult FromSnapshot(ReplayFileSnapshot snapshot)
    {
        return new ReplayFileResult
        {
            Path = snapshot.Path,
            FileName = snapshot.FileName,
            SizeBytes = snapshot.SizeBytes,
            LastModified = snapshot.LastModified,
            Metadata = snapshot.Metadata != null
                ? ReplayMetadataResult.FromSnapshot(snapshot.Metadata)
                : null,
            ValidationError = snapshot.ValidationError
        };
    }
}

/// <summary>
/// Replay metadata for MCP results.
/// </summary>
public sealed record ReplayMetadataResult
{
    /// <summary>
    /// Gets the replay name.
    /// </summary>
    public required string? Name { get; init; }

    /// <summary>
    /// Gets the replay description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets when recording started.
    /// </summary>
    public DateTimeOffset? RecordingStarted { get; init; }

    /// <summary>
    /// Gets when recording ended.
    /// </summary>
    public DateTimeOffset? RecordingEnded { get; init; }

    /// <summary>
    /// Gets the duration in seconds.
    /// </summary>
    public required float DurationSeconds { get; init; }

    /// <summary>
    /// Gets the total frame count.
    /// </summary>
    public required int FrameCount { get; init; }

    /// <summary>
    /// Gets the snapshot count.
    /// </summary>
    public required int SnapshotCount { get; init; }

    /// <summary>
    /// Gets the average frame rate.
    /// </summary>
    public required float AverageFrameRate { get; init; }

    /// <summary>
    /// Gets the data version.
    /// </summary>
    public int? DataVersion { get; init; }

    /// <summary>
    /// Gets the file size in bytes.
    /// </summary>
    public long? FileSizeBytes { get; init; }

    /// <summary>
    /// Gets whether the file has a checksum.
    /// </summary>
    public bool? HasChecksum { get; init; }

    /// <summary>
    /// Creates from a ReplayMetadataSnapshot.
    /// </summary>
    public static ReplayMetadataResult FromSnapshot(ReplayMetadataSnapshot snapshot)
    {
        return new ReplayMetadataResult
        {
            Name = snapshot.Name,
            Description = snapshot.Description,
            RecordingStarted = snapshot.RecordingStarted,
            RecordingEnded = snapshot.RecordingEnded,
            DurationSeconds = snapshot.DurationSeconds,
            FrameCount = snapshot.FrameCount,
            SnapshotCount = snapshot.SnapshotCount,
            AverageFrameRate = snapshot.AverageFrameRate,
            DataVersion = snapshot.DataVersion,
            FileSizeBytes = snapshot.FileSizeBytes,
            HasChecksum = snapshot.HasChecksum
        };
    }
}

/// <summary>
/// Replay delete result.
/// </summary>
public sealed record ReplayDeleteResult
{
    /// <summary>
    /// Gets whether the delete was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the path that was deleted.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Gets a message describing the result.
    /// </summary>
    public required string Message { get; init; }
}

/// <summary>
/// Replay frame information for MCP results.
/// </summary>
public sealed record ReplayFrameResult
{
    /// <summary>
    /// Gets the frame number.
    /// </summary>
    public required int FrameNumber { get; init; }

    /// <summary>
    /// Gets the delta time in seconds.
    /// </summary>
    public required float DeltaTimeSeconds { get; init; }

    /// <summary>
    /// Gets the elapsed time in seconds.
    /// </summary>
    public required float ElapsedTimeSeconds { get; init; }

    /// <summary>
    /// Gets the number of input events in this frame.
    /// </summary>
    public required int InputEventCount { get; init; }

    /// <summary>
    /// Gets the number of game events in this frame.
    /// </summary>
    public required int EventCount { get; init; }

    /// <summary>
    /// Gets whether this frame has a preceding snapshot.
    /// </summary>
    public required bool HasSnapshot { get; init; }

    /// <summary>
    /// Gets the frame checksum, if available.
    /// </summary>
    public uint? Checksum { get; init; }

    /// <summary>
    /// Creates from a ReplayFrameSnapshot.
    /// </summary>
    public static ReplayFrameResult FromSnapshot(ReplayFrameSnapshot snapshot)
    {
        return new ReplayFrameResult
        {
            FrameNumber = snapshot.FrameNumber,
            DeltaTimeSeconds = snapshot.DeltaTimeSeconds,
            ElapsedTimeSeconds = snapshot.ElapsedTimeSeconds,
            InputEventCount = snapshot.InputEventCount,
            EventCount = snapshot.EventCount,
            HasSnapshot = snapshot.HasSnapshot,
            Checksum = snapshot.Checksum
        };
    }
}

/// <summary>
/// Frame range result.
/// </summary>
public sealed record FrameRangeResult
{
    /// <summary>
    /// Gets the starting frame.
    /// </summary>
    public required int StartFrame { get; init; }

    /// <summary>
    /// Gets the number of frames returned.
    /// </summary>
    public required int Count { get; init; }

    /// <summary>
    /// Gets the frame data.
    /// </summary>
    public required List<ReplayFrameResult> Frames { get; init; }
}

/// <summary>
/// Input event for MCP results.
/// </summary>
public sealed record InputEventResult
{
    /// <summary>
    /// Gets the input type.
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Gets the frame number.
    /// </summary>
    public required int Frame { get; init; }

    /// <summary>
    /// Gets the key name, if applicable.
    /// </summary>
    public string? Key { get; init; }

    /// <summary>
    /// Gets the value, if applicable.
    /// </summary>
    public float? Value { get; init; }

    /// <summary>
    /// Gets the X position, if applicable.
    /// </summary>
    public float? PositionX { get; init; }

    /// <summary>
    /// Gets the Y position, if applicable.
    /// </summary>
    public float? PositionY { get; init; }

    /// <summary>
    /// Gets the custom type name, if applicable.
    /// </summary>
    public string? CustomType { get; init; }

    /// <summary>
    /// Gets the timestamp in milliseconds.
    /// </summary>
    public float? TimestampMs { get; init; }

    /// <summary>
    /// Creates from an InputEventSnapshot.
    /// </summary>
    public static InputEventResult FromSnapshot(InputEventSnapshot snapshot)
    {
        return new InputEventResult
        {
            Type = snapshot.Type,
            Frame = snapshot.Frame,
            Key = snapshot.Key,
            Value = snapshot.Value,
            PositionX = snapshot.PositionX,
            PositionY = snapshot.PositionY,
            CustomType = snapshot.CustomType,
            TimestampMs = snapshot.TimestampMs
        };
    }
}

/// <summary>
/// Input events result.
/// </summary>
public sealed record InputEventsResult
{
    /// <summary>
    /// Gets the starting frame.
    /// </summary>
    public required int StartFrame { get; init; }

    /// <summary>
    /// Gets the ending frame.
    /// </summary>
    public required int EndFrame { get; init; }

    /// <summary>
    /// Gets the number of input events.
    /// </summary>
    public required int Count { get; init; }

    /// <summary>
    /// Gets the input events.
    /// </summary>
    public required List<InputEventResult> Inputs { get; init; }
}

/// <summary>
/// Replay event for MCP results.
/// </summary>
public sealed record ReplayEventResult
{
    /// <summary>
    /// Gets the event type.
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Gets the frame number.
    /// </summary>
    public required int Frame { get; init; }

    /// <summary>
    /// Gets the timestamp in milliseconds.
    /// </summary>
    public required float TimestampMs { get; init; }

    /// <summary>
    /// Gets the entity ID, if applicable.
    /// </summary>
    public int? EntityId { get; init; }

    /// <summary>
    /// Gets the component type name, if applicable.
    /// </summary>
    public string? ComponentTypeName { get; init; }

    /// <summary>
    /// Gets the system type name, if applicable.
    /// </summary>
    public string? SystemTypeName { get; init; }

    /// <summary>
    /// Gets the custom type name, if applicable.
    /// </summary>
    public string? CustomType { get; init; }

    /// <summary>
    /// Creates from a ReplayEventSnapshot.
    /// </summary>
    public static ReplayEventResult FromSnapshot(ReplayEventSnapshot snapshot)
    {
        return new ReplayEventResult
        {
            Type = snapshot.Type,
            Frame = snapshot.Frame,
            TimestampMs = snapshot.TimestampMs,
            EntityId = snapshot.EntityId,
            ComponentTypeName = snapshot.ComponentTypeName,
            SystemTypeName = snapshot.SystemTypeName,
            CustomType = snapshot.CustomType
        };
    }
}

/// <summary>
/// Replay events result.
/// </summary>
public sealed record ReplayEventsResult
{
    /// <summary>
    /// Gets the starting frame.
    /// </summary>
    public required int StartFrame { get; init; }

    /// <summary>
    /// Gets the ending frame.
    /// </summary>
    public required int EndFrame { get; init; }

    /// <summary>
    /// Gets the number of events.
    /// </summary>
    public required int Count { get; init; }

    /// <summary>
    /// Gets the events.
    /// </summary>
    public required List<ReplayEventResult> Events { get; init; }
}

/// <summary>
/// Snapshot marker for MCP results.
/// </summary>
public sealed record SnapshotMarkerResult
{
    /// <summary>
    /// Gets the frame number where the snapshot was taken.
    /// </summary>
    public required int FrameNumber { get; init; }

    /// <summary>
    /// Gets the elapsed time in seconds.
    /// </summary>
    public required float ElapsedTimeSeconds { get; init; }

    /// <summary>
    /// Gets the snapshot checksum, if available.
    /// </summary>
    public uint? Checksum { get; init; }

    /// <summary>
    /// Gets the snapshot index.
    /// </summary>
    public required int Index { get; init; }

    /// <summary>
    /// Creates from a SnapshotMarkerSnapshot.
    /// </summary>
    public static SnapshotMarkerResult FromSnapshot(SnapshotMarkerSnapshot snapshot)
    {
        return new SnapshotMarkerResult
        {
            FrameNumber = snapshot.FrameNumber,
            ElapsedTimeSeconds = snapshot.ElapsedTimeSeconds,
            Checksum = snapshot.Checksum,
            Index = snapshot.Index
        };
    }
}

/// <summary>
/// Snapshot markers result.
/// </summary>
public sealed record SnapshotMarkersResult
{
    /// <summary>
    /// Gets the number of snapshots.
    /// </summary>
    public required int Count { get; init; }

    /// <summary>
    /// Gets the snapshot markers.
    /// </summary>
    public required List<SnapshotMarkerResult> Snapshots { get; init; }
}

/// <summary>
/// Validation result for MCP.
/// </summary>
public sealed record ValidationResult
{
    /// <summary>
    /// Gets whether the file is valid.
    /// </summary>
    public required bool IsValid { get; init; }

    /// <summary>
    /// Gets the file path that was validated.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Gets the total number of frames.
    /// </summary>
    public int? TotalFrames { get; init; }

    /// <summary>
    /// Gets the number of snapshots.
    /// </summary>
    public int? SnapshotCount { get; init; }

    /// <summary>
    /// Gets the data version.
    /// </summary>
    public int? DataVersion { get; init; }

    /// <summary>
    /// Gets any validation errors.
    /// </summary>
    public List<string>? Errors { get; init; }

    /// <summary>
    /// Creates from a ValidationResultSnapshot.
    /// </summary>
    public static ValidationResult FromSnapshot(ValidationResultSnapshot snapshot)
    {
        return new ValidationResult
        {
            IsValid = snapshot.IsValid,
            Path = snapshot.Path,
            TotalFrames = snapshot.TotalFrames,
            SnapshotCount = snapshot.SnapshotCount,
            DataVersion = snapshot.DataVersion,
            Errors = snapshot.Errors?.ToList()
        };
    }
}

/// <summary>
/// Determinism check result for MCP.
/// </summary>
public sealed record DeterminismResult
{
    /// <summary>
    /// Gets whether the replay is deterministic.
    /// </summary>
    public required bool IsDeterministic { get; init; }

    /// <summary>
    /// Gets the total frames checked.
    /// </summary>
    public required int TotalFramesChecked { get; init; }

    /// <summary>
    /// Gets the number of frames with checksums.
    /// </summary>
    public required int FramesWithChecksums { get; init; }

    /// <summary>
    /// Gets the first frame where desync was detected.
    /// </summary>
    public int? FirstDesyncFrame { get; init; }

    /// <summary>
    /// Gets desync details, if any.
    /// </summary>
    public string? DesyncDetails { get; init; }

    /// <summary>
    /// Creates from a DeterminismResultSnapshot.
    /// </summary>
    public static DeterminismResult FromSnapshot(DeterminismResultSnapshot snapshot)
    {
        return new DeterminismResult
        {
            IsDeterministic = snapshot.IsDeterministic,
            TotalFramesChecked = snapshot.TotalFramesChecked,
            FramesWithChecksums = snapshot.FramesWithChecksums,
            FirstDesyncFrame = snapshot.FirstDesyncFrame,
            DesyncDetails = snapshot.DesyncDetails
        };
    }
}

#endregion
