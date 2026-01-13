namespace KeenEyes.TestBridge.Time;

/// <summary>
/// Controller for managing game time control state.
/// </summary>
/// <remarks>
/// <para>
/// The time controller provides methods to pause, resume, and modify
/// the time scale of the running game. These controls are cooperative -
/// the game must check the <see cref="TimeStateSnapshot"/> and honor
/// the settings in its update loop.
/// </para>
/// <para>
/// Time control is useful for debugging (pause to inspect state),
/// performance testing (slow motion to observe behavior), and
/// automated testing (fast forward to reduce test duration).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Pause the game
/// var state = await timeController.PauseAsync();
/// Console.WriteLine($"Game paused: {state.IsPaused}");
///
/// // Step forward one frame
/// state = await timeController.StepFrameAsync(1);
///
/// // Resume at half speed
/// await timeController.SetTimeScaleAsync(0.5f);
/// await timeController.ResumeAsync();
/// </code>
/// </example>
public interface ITimeController
{
    /// <summary>
    /// Gets the current time control state.
    /// </summary>
    /// <returns>A snapshot of the current time state.</returns>
    Task<TimeStateSnapshot> GetTimeStateAsync();

    /// <summary>
    /// Pauses game execution.
    /// </summary>
    /// <returns>The updated time state with <see cref="TimeStateSnapshot.IsPaused"/> set to true.</returns>
    /// <remarks>
    /// If already paused, this is a no-op and returns the current state.
    /// The total paused time counter starts incrementing from this point.
    /// </remarks>
    Task<TimeStateSnapshot> PauseAsync();

    /// <summary>
    /// Resumes game execution.
    /// </summary>
    /// <returns>The updated time state with <see cref="TimeStateSnapshot.IsPaused"/> set to false.</returns>
    /// <remarks>
    /// If already running, this is a no-op and returns the current state.
    /// Any pending step frames are cleared when resuming.
    /// </remarks>
    Task<TimeStateSnapshot> ResumeAsync();

    /// <summary>
    /// Sets the time scale multiplier.
    /// </summary>
    /// <param name="scale">The time scale (1.0 = normal, &lt;1.0 = slow motion, &gt;1.0 = fast forward).</param>
    /// <returns>The updated time state with the new scale.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if scale is negative.</exception>
    /// <remarks>
    /// The time scale affects how delta time is calculated in the game loop.
    /// A scale of 0.0 effectively pauses time but does not set the paused flag.
    /// </remarks>
    Task<TimeStateSnapshot> SetTimeScaleAsync(float scale);

    /// <summary>
    /// Steps forward the specified number of frames while paused.
    /// </summary>
    /// <param name="frames">The number of frames to step (default: 1).</param>
    /// <returns>The updated time state with pending step frames set.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if frames is less than 1.</exception>
    /// <remarks>
    /// <para>
    /// This allows frame-by-frame debugging. The game should check
    /// <see cref="TimeStateSnapshot.PendingStepFrames"/> and execute
    /// that many frames even while paused, decrementing the counter
    /// after each frame.
    /// </para>
    /// <para>
    /// If the game is not paused, this pauses it first and then sets
    /// the pending step frames.
    /// </para>
    /// </remarks>
    Task<TimeStateSnapshot> StepFrameAsync(int frames = 1);

    /// <summary>
    /// Toggles the paused state.
    /// </summary>
    /// <returns>The updated time state with the paused flag toggled.</returns>
    /// <remarks>
    /// Convenience method equivalent to calling <see cref="PauseAsync"/>
    /// if currently running, or <see cref="ResumeAsync"/> if currently paused.
    /// </remarks>
    Task<TimeStateSnapshot> TogglePauseAsync();
}
