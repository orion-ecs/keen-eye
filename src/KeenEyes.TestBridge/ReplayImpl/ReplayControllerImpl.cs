using System.Diagnostics.CodeAnalysis;
using KeenEyes.Replay;
using KeenEyes.Serialization;
using KeenEyes.TestBridge.Replay;

namespace KeenEyes.TestBridge.ReplayImpl;

/// <summary>
/// Implementation of <see cref="IReplayController"/> that wraps the existing Replay infrastructure.
/// </summary>
/// <remarks>
/// <para>
/// This controller provides TestBridge access to replay recording and playback functionality.
/// Recording requires the ReplayPlugin to be installed on the world; playback can be done
/// independently using the ReplayPlayer.
/// </para>
/// </remarks>
internal sealed class ReplayControllerImpl(World world) : IReplayController
{
    private readonly ReplayPlayer player = new();
    private readonly Lock syncLock = new();
    private ReplayData? lastRecordingData;

    #region Recording Control

    /// <inheritdoc/>
    public Task<ReplayOperationResult> StartRecordingAsync(
        string? name = null,
        int maxFrames = 36000,
        int snapshotIntervalMs = 5000)
    {
        try
        {
            if (!world.TryGetExtension<ReplayRecorder>(out var recorder))
            {
                return Task.FromResult(ReplayOperationResult.Fail(
                    "ReplayPlugin is not installed. Install the ReplayPlugin on the world to enable recording."));
            }

            if (recorder.IsRecording)
            {
                return Task.FromResult(ReplayOperationResult.Fail("Recording is already in progress."));
            }

            // Note: The recorder uses options set at plugin install time.
            // MCP tools can't dynamically change maxFrames/snapshotInterval without reinstalling the plugin.
            // For now, we start with the configured options.
            recorder.StartRecording(name);

            return Task.FromResult(ReplayOperationResult.Ok(CreateRecordingInfo(recorder)));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ReplayOperationResult.Fail(ex.Message));
        }
    }

    /// <inheritdoc/>
    public Task<ReplayOperationResult> StopRecordingAsync()
    {
        try
        {
            if (!world.TryGetExtension<ReplayRecorder>(out var recorder))
            {
                return Task.FromResult(ReplayOperationResult.Fail("ReplayPlugin is not installed."));
            }

            if (!recorder.IsRecording)
            {
                return Task.FromResult(ReplayOperationResult.Fail("No recording is in progress."));
            }

            lastRecordingData = recorder.StopRecording();
            if (lastRecordingData is null)
            {
                return Task.FromResult(ReplayOperationResult.Fail("Failed to stop recording."));
            }

            return Task.FromResult(ReplayOperationResult.Ok(new RecordingInfoSnapshot
            {
                IsRecording = false,
                Name = lastRecordingData.Name,
                FrameCount = lastRecordingData.FrameCount,
                DurationSeconds = (float)lastRecordingData.Duration.TotalSeconds,
                SnapshotCount = lastRecordingData.Snapshots.Count,
                StartedAt = lastRecordingData.RecordingStarted
            }));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ReplayOperationResult.Fail(ex.Message));
        }
    }

    /// <inheritdoc/>
    public Task<ReplayOperationResult> CancelRecordingAsync()
    {
        try
        {
            if (!world.TryGetExtension<ReplayRecorder>(out var recorder))
            {
                return Task.FromResult(ReplayOperationResult.Fail("ReplayPlugin is not installed."));
            }

            recorder.CancelRecording();
            return Task.FromResult(ReplayOperationResult.Ok());
        }
        catch (Exception ex)
        {
            return Task.FromResult(ReplayOperationResult.Fail(ex.Message));
        }
    }

    /// <inheritdoc/>
    public Task<bool> IsRecordingAsync()
    {
        if (!world.TryGetExtension<ReplayRecorder>(out var recorder))
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(recorder.IsRecording);
    }

    /// <inheritdoc/>
    public Task<RecordingInfoSnapshot?> GetRecordingInfoAsync()
    {
        if (!world.TryGetExtension<ReplayRecorder>(out var recorder) || !recorder.IsRecording)
        {
            return Task.FromResult<RecordingInfoSnapshot?>(null);
        }

        return Task.FromResult<RecordingInfoSnapshot?>(CreateRecordingInfo(recorder));
    }

    /// <inheritdoc/>
    public Task<ReplayOperationResult> ForceSnapshotAsync()
    {
        try
        {
            if (!world.TryGetExtension<ReplayRecorder>(out var recorder))
            {
                return Task.FromResult(ReplayOperationResult.Fail("ReplayPlugin is not installed."));
            }

            if (!recorder.IsRecording)
            {
                return Task.FromResult(ReplayOperationResult.Fail("No recording is in progress."));
            }

            recorder.CaptureSnapshot();
            return Task.FromResult(ReplayOperationResult.Ok(CreateRecordingInfo(recorder)));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ReplayOperationResult.Fail(ex.Message));
        }
    }

    #endregion

    #region Recording Management

    /// <inheritdoc/>
    public Task<ReplayOperationResult> SaveAsync(string path)
    {
        try
        {
            // Try to stop current recording if no data is available
            if (lastRecordingData is null
                && world.TryGetExtension<ReplayRecorder>(out var recorder)
                && recorder.IsRecording)
            {
                lastRecordingData = recorder.StopRecording();
            }

            if (lastRecordingData is null)
            {
                return Task.FromResult(ReplayOperationResult.Fail("No recording data available to save."));
            }

            ReplayFileFormat.WriteToFile(path, lastRecordingData);
            return Task.FromResult(ReplayOperationResult.Ok(path));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ReplayOperationResult.Fail(ex.Message));
        }
    }

    /// <inheritdoc/>
    public Task<ReplayOperationResult> LoadAsync(string path)
    {
        try
        {
            lock (syncLock)
            {
                player.LoadReplay(path);
            }

            var metadata = CreateMetadataFromPlayer();
            if (metadata is null)
            {
                return Task.FromResult(ReplayOperationResult.Fail("Failed to load replay metadata."));
            }

            return Task.FromResult(ReplayOperationResult.Ok(metadata));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ReplayOperationResult.Fail(ex.Message));
        }
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<ReplayFileSnapshot>> ListAsync(string? directory = null)
    {
        try
        {
            var dir = directory ?? Environment.CurrentDirectory;
            var files = Directory.GetFiles(dir, "*" + ReplayFileFormat.Extension);

            var results = new List<ReplayFileSnapshot>();
            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                ReplayMetadataSnapshot? metadata = null;
                string? error = null;

                try
                {
                    var replayInfo = ReplayFileFormat.ReadMetadataFromFile(file);
                    metadata = CreateMetadataFromFileInfo(replayInfo);
                    if (replayInfo.ValidationError is not null)
                    {
                        error = replayInfo.ValidationError;
                    }
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                }

                results.Add(new ReplayFileSnapshot
                {
                    Path = file,
                    FileName = fileInfo.Name,
                    SizeBytes = fileInfo.Length,
                    LastModified = new DateTimeOffset(fileInfo.LastWriteTimeUtc, TimeSpan.Zero),
                    Metadata = metadata,
                    ValidationError = error
                });
            }

            return Task.FromResult<IReadOnlyList<ReplayFileSnapshot>>(results);
        }
        catch
        {
            return Task.FromResult<IReadOnlyList<ReplayFileSnapshot>>([]);
        }
    }

    /// <inheritdoc/>
    public Task<bool> DeleteAsync(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    /// <inheritdoc/>
    public Task<ReplayMetadataSnapshot?> GetMetadataAsync(string path)
    {
        try
        {
            var info = ReplayFileFormat.ReadMetadataFromFile(path);
            return Task.FromResult<ReplayMetadataSnapshot?>(CreateMetadataFromFileInfo(info));
        }
        catch
        {
            return Task.FromResult<ReplayMetadataSnapshot?>(null);
        }
    }

    #endregion

    #region Playback Control

    /// <inheritdoc/>
    public Task<ReplayOperationResult> PlayAsync()
    {
        try
        {
            lock (syncLock)
            {
                if (!player.IsLoaded)
                {
                    return Task.FromResult(ReplayOperationResult.Fail("No replay is loaded."));
                }

                player.Play();
            }

            return Task.FromResult(ReplayOperationResult.Ok(CreatePlaybackState()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ReplayOperationResult.Fail(ex.Message));
        }
    }

    /// <inheritdoc/>
    public Task<ReplayOperationResult> PauseAsync()
    {
        try
        {
            lock (syncLock)
            {
                player.Pause();
            }

            return Task.FromResult(ReplayOperationResult.Ok(CreatePlaybackState()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ReplayOperationResult.Fail(ex.Message));
        }
    }

    /// <inheritdoc/>
    public Task<ReplayOperationResult> StopPlaybackAsync()
    {
        try
        {
            lock (syncLock)
            {
                player.Stop();
            }

            return Task.FromResult(ReplayOperationResult.Ok(CreatePlaybackState()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ReplayOperationResult.Fail(ex.Message));
        }
    }

    /// <inheritdoc/>
    public Task<PlaybackStateSnapshot> GetPlaybackStateAsync()
    {
        return Task.FromResult(CreatePlaybackState());
    }

    /// <inheritdoc/>
    public Task<ReplayOperationResult> SetSpeedAsync(float speed)
    {
        try
        {
            if (speed < 0.25f || speed > 4.0f)
            {
                return Task.FromResult(ReplayOperationResult.Fail("Speed must be between 0.25 and 4.0."));
            }

            lock (syncLock)
            {
                player.PlaybackSpeed = speed;
            }

            return Task.FromResult(ReplayOperationResult.Ok(CreatePlaybackState()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ReplayOperationResult.Fail(ex.Message));
        }
    }

    #endregion

    #region Playback Navigation

    /// <inheritdoc/>
    public Task<ReplayOperationResult> SeekFrameAsync(int frame)
    {
        try
        {
            lock (syncLock)
            {
                if (!player.IsLoaded)
                {
                    return Task.FromResult(ReplayOperationResult.Fail("No replay is loaded."));
                }

                player.SeekToFrame(frame);
            }

            return Task.FromResult(ReplayOperationResult.Ok(CreatePlaybackState()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ReplayOperationResult.Fail(ex.Message));
        }
    }

    /// <inheritdoc/>
    public Task<ReplayOperationResult> SeekTimeAsync(float seconds)
    {
        try
        {
            lock (syncLock)
            {
                if (!player.IsLoaded)
                {
                    return Task.FromResult(ReplayOperationResult.Fail("No replay is loaded."));
                }

                player.SeekToTime(TimeSpan.FromSeconds(seconds));
            }

            return Task.FromResult(ReplayOperationResult.Ok(CreatePlaybackState()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ReplayOperationResult.Fail(ex.Message));
        }
    }

    /// <inheritdoc/>
    public Task<ReplayOperationResult> StepForwardAsync(int frames = 1)
    {
        try
        {
            lock (syncLock)
            {
                if (!player.IsLoaded)
                {
                    return Task.FromResult(ReplayOperationResult.Fail("No replay is loaded."));
                }

                player.Step(frames);
            }

            return Task.FromResult(ReplayOperationResult.Ok(CreatePlaybackState()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ReplayOperationResult.Fail(ex.Message));
        }
    }

    /// <inheritdoc/>
    public Task<ReplayOperationResult> StepBackwardAsync(int frames = 1)
    {
        try
        {
            lock (syncLock)
            {
                if (!player.IsLoaded)
                {
                    return Task.FromResult(ReplayOperationResult.Fail("No replay is loaded."));
                }

                player.Step(-frames);
            }

            return Task.FromResult(ReplayOperationResult.Ok(CreatePlaybackState()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ReplayOperationResult.Fail(ex.Message));
        }
    }

    #endregion

    #region Frame Inspection

    /// <inheritdoc/>
    public Task<ReplayFrameSnapshot?> GetFrameAsync(int frame)
    {
        try
        {
            lock (syncLock)
            {
                if (!player.IsLoaded)
                {
                    return Task.FromResult<ReplayFrameSnapshot?>(null);
                }

                var replayFrame = player.GetFrame(frame);
                if (replayFrame is null)
                {
                    return Task.FromResult<ReplayFrameSnapshot?>(null);
                }

                return Task.FromResult<ReplayFrameSnapshot?>(CreateFrameSnapshot(replayFrame));
            }
        }
        catch
        {
            return Task.FromResult<ReplayFrameSnapshot?>(null);
        }
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<ReplayFrameSnapshot>> GetFrameRangeAsync(int startFrame, int count)
    {
        try
        {
            var results = new List<ReplayFrameSnapshot>();

            lock (syncLock)
            {
                if (!player.IsLoaded)
                {
                    return Task.FromResult<IReadOnlyList<ReplayFrameSnapshot>>(results);
                }

                for (var i = 0; i < count; i++)
                {
                    var frame = player.GetFrame(startFrame + i);
                    if (frame is not null)
                    {
                        results.Add(CreateFrameSnapshot(frame));
                    }
                }
            }

            return Task.FromResult<IReadOnlyList<ReplayFrameSnapshot>>(results);
        }
        catch
        {
            return Task.FromResult<IReadOnlyList<ReplayFrameSnapshot>>([]);
        }
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<InputEventSnapshot>> GetInputsAsync(int startFrame, int endFrame)
    {
        try
        {
            var results = new List<InputEventSnapshot>();

            lock (syncLock)
            {
                if (!player.IsLoaded)
                {
                    return Task.FromResult<IReadOnlyList<InputEventSnapshot>>(results);
                }

                for (var i = startFrame; i <= endFrame; i++)
                {
                    var frame = player.GetFrame(i);
                    if (frame is null)
                    {
                        continue;
                    }

                    foreach (var input in frame.InputEvents)
                    {
                        results.Add(CreateInputEventSnapshot(input, i));
                    }
                }
            }

            return Task.FromResult<IReadOnlyList<InputEventSnapshot>>(results);
        }
        catch
        {
            return Task.FromResult<IReadOnlyList<InputEventSnapshot>>([]);
        }
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<ReplayEventSnapshot>> GetEventsAsync(int startFrame, int endFrame)
    {
        try
        {
            var results = new List<ReplayEventSnapshot>();

            lock (syncLock)
            {
                if (!player.IsLoaded)
                {
                    return Task.FromResult<IReadOnlyList<ReplayEventSnapshot>>(results);
                }

                for (var i = startFrame; i <= endFrame; i++)
                {
                    var frame = player.GetFrame(i);
                    if (frame is null)
                    {
                        continue;
                    }

                    foreach (var evt in frame.Events)
                    {
                        results.Add(CreateReplayEventSnapshot(evt, i));
                    }
                }
            }

            return Task.FromResult<IReadOnlyList<ReplayEventSnapshot>>(results);
        }
        catch
        {
            return Task.FromResult<IReadOnlyList<ReplayEventSnapshot>>([]);
        }
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<SnapshotMarkerSnapshot>> GetSnapshotsAsync()
    {
        try
        {
            var results = new List<SnapshotMarkerSnapshot>();

            lock (syncLock)
            {
                if (!player.IsLoaded)
                {
                    return Task.FromResult<IReadOnlyList<SnapshotMarkerSnapshot>>(results);
                }

                var data = player.LoadedReplay;
                if (data is null)
                {
                    return Task.FromResult<IReadOnlyList<SnapshotMarkerSnapshot>>(results);
                }

                for (var i = 0; i < data.Snapshots.Count; i++)
                {
                    var snapshot = data.Snapshots[i];
                    results.Add(new SnapshotMarkerSnapshot
                    {
                        FrameNumber = snapshot.FrameNumber,
                        ElapsedTimeSeconds = (float)snapshot.ElapsedTime.TotalSeconds,
                        Checksum = snapshot.Checksum,
                        Index = i
                    });
                }
            }

            return Task.FromResult<IReadOnlyList<SnapshotMarkerSnapshot>>(results);
        }
        catch
        {
            return Task.FromResult<IReadOnlyList<SnapshotMarkerSnapshot>>([]);
        }
    }

    #endregion

    #region Validation

    /// <inheritdoc/>
    public Task<ValidationResultSnapshot> ValidateAsync(string path)
    {
        try
        {
            var fileInfo = ReplayFileFormat.ValidateFile(path);
            if (fileInfo is null)
            {
                return Task.FromResult(new ValidationResultSnapshot
                {
                    IsValid = false,
                    Path = path,
                    Errors = ["File is not a valid replay file or does not exist."]
                });
            }

            var errors = new List<string>();
            if (fileInfo.ValidationError is not null)
            {
                errors.Add(fileInfo.ValidationError);
            }

            return Task.FromResult(new ValidationResultSnapshot
            {
                IsValid = errors.Count == 0,
                Path = path,
                TotalFrames = fileInfo.FrameCount,
                SnapshotCount = fileInfo.SnapshotCount,
                DataVersion = fileInfo.DataVersion,
                Errors = errors.Count > 0 ? errors : null
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new ValidationResultSnapshot
            {
                IsValid = false,
                Path = path,
                Errors = [ex.Message]
            });
        }
    }

    /// <inheritdoc/>
    public Task<DeterminismResultSnapshot> CheckDeterminismAsync()
    {
        try
        {
            lock (syncLock)
            {
                if (!player.IsLoaded)
                {
                    return Task.FromResult(new DeterminismResultSnapshot
                    {
                        IsDeterministic = false,
                        TotalFramesChecked = 0,
                        FramesWithChecksums = 0,
                        DesyncDetails = "No replay is loaded."
                    });
                }

                // For now, we just check if checksums are consistent
                // Full determinism check would require replaying with world
                var data = player.LoadedReplay;
                if (data is null)
                {
                    return Task.FromResult(new DeterminismResultSnapshot
                    {
                        IsDeterministic = false,
                        TotalFramesChecked = 0,
                        FramesWithChecksums = 0,
                        DesyncDetails = "Failed to access replay data."
                    });
                }

                var framesWithChecksums = data.Frames.Count(f => f.Checksum.HasValue);

                return Task.FromResult(new DeterminismResultSnapshot
                {
                    IsDeterministic = true, // We can't validate without replaying
                    TotalFramesChecked = data.FrameCount,
                    FramesWithChecksums = framesWithChecksums,
                    DesyncDetails = framesWithChecksums == 0
                        ? "No checksums recorded in replay."
                        : null
                });
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(new DeterminismResultSnapshot
            {
                IsDeterministic = false,
                TotalFramesChecked = 0,
                FramesWithChecksums = 0,
                DesyncDetails = ex.Message
            });
        }
    }

    #endregion

    #region Helper Methods

    private RecordingInfoSnapshot CreateRecordingInfo(ReplayRecorder recorder)
    {
        return new RecordingInfoSnapshot
        {
            IsRecording = recorder.IsRecording,
            Name = null, // Not accessible from ReplayRecorder
            FrameCount = recorder.RecordedFrameCount,
            DurationSeconds = (float)recorder.ElapsedTime.TotalSeconds,
            SnapshotCount = recorder.SnapshotCount,
            StartedAt = null // Not accessible from ReplayRecorder
        };
    }

    private PlaybackStateSnapshot CreatePlaybackState()
    {
        lock (syncLock)
        {
            var isLoaded = player.IsLoaded;
            var totalFrames = isLoaded ? player.TotalFrames : 0;
            var totalTime = isLoaded ? player.TotalDuration : TimeSpan.Zero;

            return new PlaybackStateSnapshot
            {
                IsLoaded = isLoaded,
                IsPlaying = player.State == PlaybackState.Playing,
                IsPaused = player.State == PlaybackState.Paused,
                IsStopped = player.State == PlaybackState.Stopped,
                CurrentFrame = player.CurrentFrame,
                TotalFrames = totalFrames,
                CurrentTimeSeconds = (float)player.CurrentTime.TotalSeconds,
                TotalTimeSeconds = (float)totalTime.TotalSeconds,
                PlaybackSpeed = player.PlaybackSpeed,
                ReplayName = player.LoadedReplay?.Name
            };
        }
    }

    private ReplayMetadataSnapshot? CreateMetadataFromPlayer()
    {
        lock (syncLock)
        {
            var data = player.LoadedReplay;
            if (data is null)
            {
                return null;
            }

            return new ReplayMetadataSnapshot
            {
                Name = data.Name,
                Description = data.Description,
                RecordingStarted = data.RecordingStarted,
                RecordingEnded = data.RecordingEnded,
                DurationSeconds = (float)data.Duration.TotalSeconds,
                FrameCount = data.FrameCount,
                SnapshotCount = data.Snapshots.Count,
                AverageFrameRate = (float)data.AverageFrameRate,
                DataVersion = data.Version
            };
        }
    }

    private static ReplayMetadataSnapshot CreateMetadataFromFileInfo(ReplayFileInfo info)
    {
        return new ReplayMetadataSnapshot
        {
            Name = info.Name,
            Description = info.Description,
            RecordingStarted = info.RecordingStarted,
            RecordingEnded = info.RecordingEnded,
            DurationSeconds = (float)info.Duration.TotalSeconds,
            FrameCount = info.FrameCount,
            SnapshotCount = info.SnapshotCount,
            AverageFrameRate = info.Duration.TotalSeconds > 0
                ? (float)(info.FrameCount / info.Duration.TotalSeconds)
                : 0f,
            DataVersion = info.DataVersion,
            FileSizeBytes = info.CompressedSize,
            HasChecksum = info.Checksum is not null
        };
    }

    private static ReplayFrameSnapshot CreateFrameSnapshot(ReplayFrame frame)
    {
        return new ReplayFrameSnapshot
        {
            FrameNumber = frame.FrameNumber,
            DeltaTimeSeconds = (float)frame.DeltaTime.TotalSeconds,
            ElapsedTimeSeconds = (float)frame.ElapsedTime.TotalSeconds,
            InputEventCount = frame.InputEvents.Count,
            EventCount = frame.Events.Count,
            HasSnapshot = frame.PrecedingSnapshotIndex.HasValue,
            Checksum = frame.Checksum
        };
    }

    private static InputEventSnapshot CreateInputEventSnapshot(InputEvent input, int frame)
    {
        return new InputEventSnapshot
        {
            Type = input.Type.ToString(),
            Frame = frame,
            Key = input.Key,
            Value = input.Value,
            PositionX = input.Position.X,
            PositionY = input.Position.Y,
            CustomType = input.CustomType,
            TimestampMs = (float)input.Timestamp.TotalMilliseconds
        };
    }

    private static ReplayEventSnapshot CreateReplayEventSnapshot(ReplayEvent evt, int frame)
    {
        return new ReplayEventSnapshot
        {
            Type = evt.Type.ToString(),
            Frame = frame,
            TimestampMs = (float)evt.Timestamp.TotalMilliseconds,
            EntityId = evt.EntityId,
            ComponentTypeName = evt.ComponentTypeName,
            SystemTypeName = evt.SystemTypeName,
            CustomType = evt.CustomType
        };
    }

    #endregion
}
