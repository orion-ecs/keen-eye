namespace KeenEyes.Sample.Racing;

/// <summary>
/// Accumulates elapsed time for the current lap and records the final lap time.
/// </summary>
/// <remarks>
/// Pure data only. <see cref="LapTrackingSystem"/> advances the timer and marks
/// the lap finished once the car crosses the start/finish line.
/// </remarks>
[Component(Serializable = true)]
public partial struct LapTimer
{
    /// <summary>
    /// Time elapsed since the lap started, in seconds.
    /// </summary>
    public float ElapsedSeconds;

    /// <summary>
    /// Whether the lap has been completed.
    /// </summary>
    public bool Finished;

    /// <summary>
    /// The final lap time in seconds, valid once <see cref="Finished"/> is true.
    /// </summary>
    public float FinishedSeconds;
}
