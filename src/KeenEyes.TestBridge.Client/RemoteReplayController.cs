using KeenEyes.TestBridge.Replay;

namespace KeenEyes.TestBridge.Client;

/// <summary>
/// Remote implementation of <see cref="IReplayController"/> that communicates over IPC.
/// </summary>
internal sealed class RemoteReplayController(TestBridgeClient client) : IReplayController
{
    #region Recording Control

    /// <inheritdoc />
    public async Task<ReplayOperationResult> StartRecordingAsync(
        string? name = null,
        int maxFrames = 36000,
        int snapshotIntervalMs = 5000)
    {
        var result = await client.SendRequestAsync<ReplayOperationResult>(
            "replay.startRecording",
            new { name, maxFrames, snapshotIntervalMs },
            CancellationToken.None);
        return result ?? ReplayOperationResult.Fail("No response from server");
    }

    /// <inheritdoc />
    public async Task<ReplayOperationResult> StopRecordingAsync()
    {
        var result = await client.SendRequestAsync<ReplayOperationResult>(
            "replay.stopRecording",
            null,
            CancellationToken.None);
        return result ?? ReplayOperationResult.Fail("No response from server");
    }

    /// <inheritdoc />
    public async Task<ReplayOperationResult> CancelRecordingAsync()
    {
        var result = await client.SendRequestAsync<ReplayOperationResult>(
            "replay.cancelRecording",
            null,
            CancellationToken.None);
        return result ?? ReplayOperationResult.Fail("No response from server");
    }

    /// <inheritdoc />
    public async Task<bool> IsRecordingAsync()
    {
        var result = await client.SendRequestAsync<bool>(
            "replay.isRecording",
            null,
            CancellationToken.None);
        return result;
    }

    /// <inheritdoc />
    public async Task<RecordingInfoSnapshot?> GetRecordingInfoAsync()
    {
        return await client.SendRequestAsync<RecordingInfoSnapshot>(
            "replay.getRecordingInfo",
            null,
            CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task<ReplayOperationResult> ForceSnapshotAsync()
    {
        var result = await client.SendRequestAsync<ReplayOperationResult>(
            "replay.forceSnapshot",
            null,
            CancellationToken.None);
        return result ?? ReplayOperationResult.Fail("No response from server");
    }

    #endregion

    #region Recording Management

    /// <inheritdoc />
    public async Task<ReplayOperationResult> SaveAsync(string path)
    {
        var result = await client.SendRequestAsync<ReplayOperationResult>(
            "replay.save",
            new { path },
            CancellationToken.None);
        return result ?? ReplayOperationResult.Fail("No response from server");
    }

    /// <inheritdoc />
    public async Task<ReplayOperationResult> LoadAsync(string path)
    {
        var result = await client.SendRequestAsync<ReplayOperationResult>(
            "replay.load",
            new { path },
            CancellationToken.None);
        return result ?? ReplayOperationResult.Fail("No response from server");
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ReplayFileSnapshot>> ListAsync(string? directory = null)
    {
        var result = await client.SendRequestAsync<List<ReplayFileSnapshot>>(
            "replay.list",
            new { directory },
            CancellationToken.None);
        return result ?? [];
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string path)
    {
        var result = await client.SendRequestAsync<bool>(
            "replay.delete",
            new { path },
            CancellationToken.None);
        return result;
    }

    /// <inheritdoc />
    public async Task<ReplayMetadataSnapshot?> GetMetadataAsync(string path)
    {
        return await client.SendRequestAsync<ReplayMetadataSnapshot>(
            "replay.getMetadata",
            new { path },
            CancellationToken.None);
    }

    #endregion

    #region Playback Control

    /// <inheritdoc />
    public async Task<ReplayOperationResult> PlayAsync()
    {
        var result = await client.SendRequestAsync<ReplayOperationResult>(
            "replay.play",
            null,
            CancellationToken.None);
        return result ?? ReplayOperationResult.Fail("No response from server");
    }

    /// <inheritdoc />
    public async Task<ReplayOperationResult> PauseAsync()
    {
        var result = await client.SendRequestAsync<ReplayOperationResult>(
            "replay.pause",
            null,
            CancellationToken.None);
        return result ?? ReplayOperationResult.Fail("No response from server");
    }

    /// <inheritdoc />
    public async Task<ReplayOperationResult> StopPlaybackAsync()
    {
        var result = await client.SendRequestAsync<ReplayOperationResult>(
            "replay.stopPlayback",
            null,
            CancellationToken.None);
        return result ?? ReplayOperationResult.Fail("No response from server");
    }

    /// <inheritdoc />
    public async Task<PlaybackStateSnapshot> GetPlaybackStateAsync()
    {
        var result = await client.SendRequestAsync<PlaybackStateSnapshot>(
            "replay.getPlaybackState",
            null,
            CancellationToken.None);
        return result ?? CreateEmptyPlaybackState();
    }

    /// <inheritdoc />
    public async Task<ReplayOperationResult> SetSpeedAsync(float speed)
    {
        var result = await client.SendRequestAsync<ReplayOperationResult>(
            "replay.setSpeed",
            new { speed },
            CancellationToken.None);
        return result ?? ReplayOperationResult.Fail("No response from server");
    }

    #endregion

    #region Playback Navigation

    /// <inheritdoc />
    public async Task<ReplayOperationResult> SeekFrameAsync(int frame)
    {
        var result = await client.SendRequestAsync<ReplayOperationResult>(
            "replay.seekFrame",
            new { frame },
            CancellationToken.None);
        return result ?? ReplayOperationResult.Fail("No response from server");
    }

    /// <inheritdoc />
    public async Task<ReplayOperationResult> SeekTimeAsync(float seconds)
    {
        var result = await client.SendRequestAsync<ReplayOperationResult>(
            "replay.seekTime",
            new { seconds },
            CancellationToken.None);
        return result ?? ReplayOperationResult.Fail("No response from server");
    }

    /// <inheritdoc />
    public async Task<ReplayOperationResult> StepForwardAsync(int frames = 1)
    {
        var result = await client.SendRequestAsync<ReplayOperationResult>(
            "replay.stepForward",
            new { frames },
            CancellationToken.None);
        return result ?? ReplayOperationResult.Fail("No response from server");
    }

    /// <inheritdoc />
    public async Task<ReplayOperationResult> StepBackwardAsync(int frames = 1)
    {
        var result = await client.SendRequestAsync<ReplayOperationResult>(
            "replay.stepBackward",
            new { frames },
            CancellationToken.None);
        return result ?? ReplayOperationResult.Fail("No response from server");
    }

    #endregion

    #region Frame Inspection

    /// <inheritdoc />
    public async Task<ReplayFrameSnapshot?> GetFrameAsync(int frame)
    {
        return await client.SendRequestAsync<ReplayFrameSnapshot>(
            "replay.getFrame",
            new { frame },
            CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ReplayFrameSnapshot>> GetFrameRangeAsync(int startFrame, int count)
    {
        var result = await client.SendRequestAsync<List<ReplayFrameSnapshot>>(
            "replay.getFrameRange",
            new { startFrame, count },
            CancellationToken.None);
        return result ?? [];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<InputEventSnapshot>> GetInputsAsync(int startFrame, int endFrame)
    {
        var result = await client.SendRequestAsync<List<InputEventSnapshot>>(
            "replay.getInputs",
            new { startFrame, endFrame },
            CancellationToken.None);
        return result ?? [];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ReplayEventSnapshot>> GetEventsAsync(int startFrame, int endFrame)
    {
        var result = await client.SendRequestAsync<List<ReplayEventSnapshot>>(
            "replay.getEvents",
            new { startFrame, endFrame },
            CancellationToken.None);
        return result ?? [];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SnapshotMarkerSnapshot>> GetSnapshotsAsync()
    {
        var result = await client.SendRequestAsync<List<SnapshotMarkerSnapshot>>(
            "replay.getSnapshots",
            null,
            CancellationToken.None);
        return result ?? [];
    }

    #endregion

    #region Validation

    /// <inheritdoc />
    public async Task<ValidationResultSnapshot> ValidateAsync(string path)
    {
        var result = await client.SendRequestAsync<ValidationResultSnapshot>(
            "replay.validate",
            new { path },
            CancellationToken.None);
        return result ?? CreateErrorValidationResult(path, "No response from server");
    }

    /// <inheritdoc />
    public async Task<DeterminismResultSnapshot> CheckDeterminismAsync()
    {
        var result = await client.SendRequestAsync<DeterminismResultSnapshot>(
            "replay.checkDeterminism",
            null,
            CancellationToken.None);
        return result ?? CreateErrorDeterminismResult("No response from server");
    }

    #endregion

    #region Helper Methods

    private static PlaybackStateSnapshot CreateEmptyPlaybackState() => new()
    {
        IsLoaded = false,
        IsPlaying = false,
        IsPaused = false,
        IsStopped = true,
        CurrentFrame = 0,
        TotalFrames = 0,
        CurrentTimeSeconds = 0f,
        TotalTimeSeconds = 0f,
        PlaybackSpeed = 1.0f,
        ReplayName = null
    };

    private static ValidationResultSnapshot CreateErrorValidationResult(string path, string error) => new()
    {
        Path = path,
        IsValid = false,
        Errors = [error]
    };

    private static DeterminismResultSnapshot CreateErrorDeterminismResult(string error) => new()
    {
        IsDeterministic = false,
        TotalFramesChecked = 0,
        FramesWithChecksums = 0,
        DesyncDetails = error
    };

    #endregion
}
