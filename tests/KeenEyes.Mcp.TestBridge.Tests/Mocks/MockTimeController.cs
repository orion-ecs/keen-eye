using KeenEyes.TestBridge.Time;

namespace KeenEyes.Mcp.TestBridge.Tests.Mocks;

/// <summary>
/// Mock implementation of ITimeController for testing MCP tools.
/// </summary>
internal sealed class MockTimeController : ITimeController
{
    public bool IsPaused { get; set; }
    public float TimeScale { get; set; } = 1f;
    public int PendingStepFrames { get; set; }
    public long LastModifiedFrame { get; set; }

    private TimeStateSnapshot Snapshot() => new()
    {
        IsPaused = IsPaused,
        TimeScale = TimeScale,
        TotalPausedTime = 0.0,
        LastModifiedFrame = LastModifiedFrame,
        PendingStepFrames = PendingStepFrames
    };

    public Task<TimeStateSnapshot> GetTimeStateAsync() => Task.FromResult(Snapshot());

    public Task<TimeStateSnapshot> PauseAsync()
    {
        IsPaused = true;
        return Task.FromResult(Snapshot());
    }

    public Task<TimeStateSnapshot> ResumeAsync()
    {
        IsPaused = false;
        PendingStepFrames = 0;
        return Task.FromResult(Snapshot());
    }

    public Task<TimeStateSnapshot> SetTimeScaleAsync(float scale)
    {
        TimeScale = scale;
        return Task.FromResult(Snapshot());
    }

    public Task<TimeStateSnapshot> StepFrameAsync(int frames = 1)
    {
        IsPaused = true;
        PendingStepFrames = frames;
        return Task.FromResult(Snapshot());
    }

    public Task<TimeStateSnapshot> TogglePauseAsync()
    {
        IsPaused = !IsPaused;
        return Task.FromResult(Snapshot());
    }
}
