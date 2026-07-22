using KeenEyes.TestBridge.Replay;

namespace KeenEyes.Mcp.TestBridge.Tests.Mocks;

/// <summary>
/// Mock implementation of IReplayController for testing MCP tools.
/// </summary>
internal sealed class MockReplayController : IReplayController
{
    public Task<ReplayOperationResult> StartRecordingAsync(
        string? name = null,
        int maxFrames = 36000,
        int snapshotIntervalMs = 5000)
        => Task.FromResult(ReplayOperationResult.Ok());

    public Task<ReplayOperationResult> StopRecordingAsync() => Task.FromResult(ReplayOperationResult.Ok());

    public Task<ReplayOperationResult> CancelRecordingAsync() => Task.FromResult(ReplayOperationResult.Ok());

    public Task<bool> IsRecordingAsync() => Task.FromResult(false);

    public Task<RecordingInfoSnapshot?> GetRecordingInfoAsync()
        => Task.FromResult<RecordingInfoSnapshot?>(null);

    public Task<ReplayOperationResult> ForceSnapshotAsync() => Task.FromResult(ReplayOperationResult.Ok());

    public Task<ReplayOperationResult> SaveAsync(string path) => Task.FromResult(ReplayOperationResult.Ok(path));

    public Task<ReplayOperationResult> LoadAsync(string path) => Task.FromResult(ReplayOperationResult.Ok(path));

    public Task<bool> DeleteAsync(string path) => Task.FromResult(true);

    public Task<ReplayMetadataSnapshot?> GetMetadataAsync(string path)
        => Task.FromResult<ReplayMetadataSnapshot?>(null);

    public Task<ReplayOperationResult> PlayAsync() => Task.FromResult(ReplayOperationResult.Ok());

    public Task<ReplayOperationResult> PauseAsync() => Task.FromResult(ReplayOperationResult.Ok());

    public Task<ReplayOperationResult> StopPlaybackAsync() => Task.FromResult(ReplayOperationResult.Ok());

    public Task<PlaybackStateSnapshot> GetPlaybackStateAsync()
        => Task.FromResult(new PlaybackStateSnapshot
        {
            IsLoaded = false,
            IsPlaying = false,
            IsPaused = false,
            IsStopped = true,
            CurrentFrame = 0,
            TotalFrames = 0,
            CurrentTimeSeconds = 0f,
            TotalTimeSeconds = 0f,
            PlaybackSpeed = 1f
        });

    public Task<ReplayOperationResult> SetSpeedAsync(float speed) => Task.FromResult(ReplayOperationResult.Ok());

    public Task<ReplayOperationResult> SeekFrameAsync(int frame) => Task.FromResult(ReplayOperationResult.Ok());

    public Task<ReplayOperationResult> SeekTimeAsync(float seconds) => Task.FromResult(ReplayOperationResult.Ok());

    public Task<ReplayOperationResult> StepForwardAsync(int frames = 1) => Task.FromResult(ReplayOperationResult.Ok());

    public Task<ReplayOperationResult> StepBackwardAsync(int frames = 1) => Task.FromResult(ReplayOperationResult.Ok());

    public Task<ReplayFrameSnapshot?> GetFrameAsync(int frame) => Task.FromResult<ReplayFrameSnapshot?>(null);

    public Task<ValidationResultSnapshot> ValidateAsync(string path)
        => Task.FromResult(new ValidationResultSnapshot { IsValid = true, Path = path });

    public Task<DeterminismResultSnapshot> CheckDeterminismAsync()
        => Task.FromResult(new DeterminismResultSnapshot
        {
            IsDeterministic = true,
            TotalFramesChecked = 0,
            FramesWithChecksums = 0
        });

    public Task<IReadOnlyList<ReplayFileSnapshot>> ListAsync(string? directory = null)
        => Task.FromResult<IReadOnlyList<ReplayFileSnapshot>>([]);

    public Task<IReadOnlyList<ReplayFrameSnapshot>> GetFrameRangeAsync(int startFrame, int count)
        => Task.FromResult<IReadOnlyList<ReplayFrameSnapshot>>([]);

    public Task<IReadOnlyList<InputEventSnapshot>> GetInputsAsync(int startFrame, int endFrame)
        => Task.FromResult<IReadOnlyList<InputEventSnapshot>>([]);

    public Task<IReadOnlyList<ReplayEventSnapshot>> GetEventsAsync(int startFrame, int endFrame)
        => Task.FromResult<IReadOnlyList<ReplayEventSnapshot>>([]);

    public Task<IReadOnlyList<SnapshotMarkerSnapshot>> GetSnapshotsAsync()
        => Task.FromResult<IReadOnlyList<SnapshotMarkerSnapshot>>([]);
}
