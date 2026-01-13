using KeenEyes.TestBridge.Time;

namespace KeenEyes.TestBridge.Client;

/// <summary>
/// Remote implementation of <see cref="ITimeController"/> that communicates over IPC.
/// </summary>
internal sealed class RemoteTimeController(TestBridgeClient client) : ITimeController
{
    /// <inheritdoc />
    public async Task<TimeStateSnapshot> GetTimeStateAsync()
    {
        var result = await client.SendRequestAsync<TimeStateSnapshot>("time.getState", null, CancellationToken.None);
        return result ?? CreateDefaultSnapshot();
    }

    /// <inheritdoc />
    public async Task<TimeStateSnapshot> PauseAsync()
    {
        var result = await client.SendRequestAsync<TimeStateSnapshot>("time.pause", null, CancellationToken.None);
        return result ?? CreateDefaultSnapshot();
    }

    /// <inheritdoc />
    public async Task<TimeStateSnapshot> ResumeAsync()
    {
        var result = await client.SendRequestAsync<TimeStateSnapshot>("time.resume", null, CancellationToken.None);
        return result ?? CreateDefaultSnapshot();
    }

    /// <inheritdoc />
    public async Task<TimeStateSnapshot> SetTimeScaleAsync(float scale)
    {
        var result = await client.SendRequestAsync<TimeStateSnapshot>(
            "time.setScale",
            new { scale },
            CancellationToken.None);
        return result ?? CreateDefaultSnapshot();
    }

    /// <inheritdoc />
    public async Task<TimeStateSnapshot> StepFrameAsync(int frames = 1)
    {
        var result = await client.SendRequestAsync<TimeStateSnapshot>(
            "time.stepFrame",
            new { frames },
            CancellationToken.None);
        return result ?? CreateDefaultSnapshot();
    }

    /// <inheritdoc />
    public async Task<TimeStateSnapshot> TogglePauseAsync()
    {
        var result = await client.SendRequestAsync<TimeStateSnapshot>("time.togglePause", null, CancellationToken.None);
        return result ?? CreateDefaultSnapshot();
    }

    private static TimeStateSnapshot CreateDefaultSnapshot() => new()
    {
        IsPaused = false,
        TimeScale = 1.0f,
        TotalPausedTime = 0.0,
        LastModifiedFrame = 0,
        PendingStepFrames = 0
    };
}
