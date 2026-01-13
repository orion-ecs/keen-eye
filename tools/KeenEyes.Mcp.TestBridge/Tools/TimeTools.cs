using System.ComponentModel;
using KeenEyes.Mcp.TestBridge.Connection;
using KeenEyes.TestBridge.Time;
using ModelContextProtocol.Server;

namespace KeenEyes.Mcp.TestBridge.Tools;

/// <summary>
/// MCP tools for game time control.
/// </summary>
/// <remarks>
/// <para>
/// These tools allow pausing, resuming, and controlling the time scale
/// of a running KeenEyes application. Note that time control is cooperative -
/// games must check the TimeState singleton and honor its values.
/// </para>
/// </remarks>
[McpServerToolType]
public sealed class TimeTools(BridgeConnectionManager connection)
{
    #region State

    [McpServerTool(Name = "time_get_state")]
    [Description("Get the current time control state including pause status, time scale, and frame information.")]
    public async Task<TimeStateResult> GetState()
    {
        var bridge = connection.GetBridge();
        var state = await bridge.Time.GetTimeStateAsync();
        return TimeStateResult.FromSnapshot(state);
    }

    #endregion

    #region Pause Control

    [McpServerTool(Name = "time_pause")]
    [Description("Pause game execution. The game will stop updating until resumed.")]
    public async Task<TimeStateResult> Pause()
    {
        var bridge = connection.GetBridge();
        var state = await bridge.Time.PauseAsync();
        return TimeStateResult.FromSnapshot(state);
    }

    [McpServerTool(Name = "time_resume")]
    [Description("Resume game execution after being paused.")]
    public async Task<TimeStateResult> Resume()
    {
        var bridge = connection.GetBridge();
        var state = await bridge.Time.ResumeAsync();
        return TimeStateResult.FromSnapshot(state);
    }

    [McpServerTool(Name = "time_toggle_pause")]
    [Description("Toggle between paused and running states.")]
    public async Task<TimeStateResult> TogglePause()
    {
        var bridge = connection.GetBridge();
        var state = await bridge.Time.TogglePauseAsync();
        return TimeStateResult.FromSnapshot(state);
    }

    #endregion

    #region Time Scale

    [McpServerTool(Name = "time_set_scale")]
    [Description("Set the time scale multiplier. Use 1.0 for normal speed, <1.0 for slow motion, >1.0 for fast forward.")]
    public async Task<TimeStateResult> SetScale(
        [Description("Time scale multiplier (0.0 = frozen, 0.5 = half speed, 1.0 = normal, 2.0 = double speed)")]
        float scale)
    {
        if (scale < 0)
        {
            return new TimeStateResult
            {
                Success = false,
                Error = "Time scale cannot be negative"
            };
        }

        var bridge = connection.GetBridge();
        var state = await bridge.Time.SetTimeScaleAsync(scale);
        return TimeStateResult.FromSnapshot(state);
    }

    #endregion

    #region Frame Stepping

    [McpServerTool(Name = "time_step_frame")]
    [Description("Step forward a specified number of frames while paused. Useful for frame-by-frame debugging.")]
    public async Task<TimeStateResult> StepFrame(
        [Description("Number of frames to step forward (default: 1)")]
        int frames = 1)
    {
        if (frames < 1)
        {
            return new TimeStateResult
            {
                Success = false,
                Error = "Frames must be at least 1"
            };
        }

        var bridge = connection.GetBridge();
        var state = await bridge.Time.StepFrameAsync(frames);
        return TimeStateResult.FromSnapshot(state);
    }

    #endregion
}

#region Result Records

/// <summary>
/// Result of a time control operation.
/// </summary>
public sealed record TimeStateResult
{
    /// <summary>
    /// Gets whether the operation was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets whether the game is currently paused.
    /// </summary>
    public bool IsPaused { get; init; }

    /// <summary>
    /// Gets the current time scale multiplier.
    /// </summary>
    public float TimeScale { get; init; }

    /// <summary>
    /// Gets the total time spent paused, in seconds.
    /// </summary>
    public double TotalPausedTime { get; init; }

    /// <summary>
    /// Gets the frame number at which time control was last modified.
    /// </summary>
    public long LastModifiedFrame { get; init; }

    /// <summary>
    /// Gets the number of pending step frames remaining.
    /// </summary>
    public int PendingStepFrames { get; init; }

    /// <summary>
    /// Gets an error message if the operation failed.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Creates a successful result from a TimeStateSnapshot.
    /// </summary>
    public static TimeStateResult FromSnapshot(TimeStateSnapshot snapshot)
    {
        return new TimeStateResult
        {
            Success = true,
            IsPaused = snapshot.IsPaused,
            TimeScale = snapshot.TimeScale,
            TotalPausedTime = snapshot.TotalPausedTime,
            LastModifiedFrame = snapshot.LastModifiedFrame,
            PendingStepFrames = snapshot.PendingStepFrames
        };
    }
}

#endregion
