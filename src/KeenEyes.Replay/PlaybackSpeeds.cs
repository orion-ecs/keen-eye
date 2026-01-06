namespace KeenEyes.Replay;

/// <summary>
/// Provides predefined playback speed constants for replay playback.
/// </summary>
/// <remarks>
/// <para>
/// These constants define common playback speeds ranging from slow motion (0.25x)
/// to fast forward (4x). Use these with <see cref="ReplayPlayer.PlaybackSpeed"/>
/// for consistent playback control.
/// </para>
/// <para>
/// The valid range for playback speed is from <see cref="MinSpeed"/> (0.25x)
/// to <see cref="MaxSpeed"/> (4x).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var player = new ReplayPlayer();
/// player.LoadReplay("recording.kreplay");
///
/// // Set to slow motion
/// player.PlaybackSpeed = PlaybackSpeeds.QuarterSpeed;
///
/// // Set to fast forward
/// player.PlaybackSpeed = PlaybackSpeeds.QuadrupleSpeed;
/// </code>
/// </example>
public static class PlaybackSpeeds
{
    /// <summary>
    /// The minimum allowed playback speed (0.25x).
    /// </summary>
    public const float MinSpeed = 0.25f;

    /// <summary>
    /// The maximum allowed playback speed (4.0x).
    /// </summary>
    public const float MaxSpeed = 4.0f;

    /// <summary>
    /// Quarter speed (0.25x) - extreme slow motion.
    /// </summary>
    public const float QuarterSpeed = 0.25f;

    /// <summary>
    /// Half speed (0.5x) - slow motion.
    /// </summary>
    public const float HalfSpeed = 0.5f;

    /// <summary>
    /// Normal speed (1.0x) - plays at recorded speed.
    /// </summary>
    public const float NormalSpeed = 1.0f;

    /// <summary>
    /// Double speed (2.0x) - fast forward.
    /// </summary>
    public const float DoubleSpeed = 2.0f;

    /// <summary>
    /// Quadruple speed (4.0x) - fast forward.
    /// </summary>
    public const float QuadrupleSpeed = 4.0f;
}
