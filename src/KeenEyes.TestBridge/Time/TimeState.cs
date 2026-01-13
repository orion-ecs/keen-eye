namespace KeenEyes.TestBridge.Time;

/// <summary>
/// Singleton component representing the current time control state.
/// </summary>
/// <remarks>
/// <para>
/// This struct is stored as a singleton in the World and can be modified
/// via the TestBridge to control game execution. Games must check this
/// state in their update loops to honor pause/time scale settings.
/// </para>
/// <para>
/// The TestBridge provides this as a cooperative mechanism - it does not
/// directly control the game loop. Applications integrate by checking
/// <see cref="IsPaused"/> and applying <see cref="TimeScale"/> to their
/// delta time calculations.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In your game loop
/// var timeState = world.TryGetSingleton&lt;TimeState&gt;(out var state) ? state : default;
/// if (timeState.IsPaused)
/// {
///     return; // Skip update when paused
/// }
///
/// var scaledDeltaTime = deltaTime * timeState.TimeScale;
/// // Use scaledDeltaTime for physics, animations, etc.
/// </code>
/// </example>
public struct TimeState
{
    /// <summary>
    /// Gets or sets whether the game is paused.
    /// </summary>
    /// <remarks>
    /// When true, game systems should skip their update logic.
    /// UI systems may still want to update while paused.
    /// </remarks>
    public bool IsPaused { get; set; }

    /// <summary>
    /// Gets or sets the time scale multiplier.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A value of 1.0 means normal speed. Values less than 1.0 slow
    /// down time, values greater than 1.0 speed up time.
    /// </para>
    /// <para>
    /// Common values:
    /// </para>
    /// <list type="bullet">
    /// <item><description>0.0 - Effectively paused</description></item>
    /// <item><description>0.5 - Half speed (slow motion)</description></item>
    /// <item><description>1.0 - Normal speed</description></item>
    /// <item><description>2.0 - Double speed (fast forward)</description></item>
    /// </list>
    /// </remarks>
    public float TimeScale { get; set; }

    /// <summary>
    /// Gets or sets the total time spent in paused state.
    /// </summary>
    /// <remarks>
    /// This is updated by the TestBridge when transitioning between
    /// paused and unpaused states. Games can use this for analytics
    /// or to exclude paused time from playtime tracking.
    /// </remarks>
    public double TotalPausedTime { get; set; }

    /// <summary>
    /// Gets or sets the frame count at which time control was last modified.
    /// </summary>
    /// <remarks>
    /// This can be used to detect when time state changes and trigger
    /// appropriate responses (e.g., audio fade out on pause).
    /// </remarks>
    public long LastModifiedFrame { get; set; }

    /// <summary>
    /// Gets or sets the number of frames to step when single-stepping.
    /// </summary>
    /// <remarks>
    /// When greater than zero, the game should execute this many frames
    /// even if paused, then decrement this counter. This enables
    /// frame-by-frame debugging.
    /// </remarks>
    public int PendingStepFrames { get; set; }

    /// <summary>
    /// Creates a default time state with normal speed and not paused.
    /// </summary>
    /// <returns>A new TimeState with default values.</returns>
    public static TimeState CreateDefault() => new()
    {
        IsPaused = false,
        TimeScale = 1.0f,
        TotalPausedTime = 0.0,
        LastModifiedFrame = 0,
        PendingStepFrames = 0
    };
}
