using System.Numerics;

namespace KeenEyes.Replay.Ghost;

/// <summary>
/// Configuration for ghost visual appearance.
/// </summary>
/// <remarks>
/// <para>
/// This configuration controls how a ghost appears when rendered in the game.
/// Games can use these settings to render ghosts with different colors and
/// transparency levels to distinguish between multiple ghosts or indicate
/// their meaning (e.g., personal best vs. world record).
/// </para>
/// <para>
/// The visual settings are purely informational - the ghost system does not
/// perform rendering. Games should read these settings and apply them to
/// their ghost rendering logic.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create config for a personal best ghost
/// var personalBestConfig = new GhostVisualConfig
/// {
///     TintColor = new Vector4(0f, 1f, 0f, 1f),  // Green
///     Opacity = 0.5f,
///     Label = "Personal Best"
/// };
///
/// // Create config for a world record ghost
/// var worldRecordConfig = new GhostVisualConfig
/// {
///     TintColor = new Vector4(1f, 0.84f, 0f, 1f),  // Gold
///     Opacity = 0.6f,
///     Label = "World Record"
/// };
/// </code>
/// </example>
public sealed record GhostVisualConfig
{
    /// <summary>
    /// Gets the default visual configuration.
    /// </summary>
    /// <remarks>
    /// Default settings: white tint, 50% opacity, no label.
    /// </remarks>
    public static GhostVisualConfig Default { get; } = new();

    /// <summary>
    /// Gets or sets the tint color for the ghost.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The tint color is represented as an RGBA <see cref="Vector4"/> where
    /// each component ranges from 0.0 to 1.0. Games should multiply this
    /// color with the ghost's base color/texture when rendering.
    /// </para>
    /// <para>
    /// Default value is white (1, 1, 1, 1), meaning no tint is applied.
    /// </para>
    /// </remarks>
    public Vector4 TintColor { get; init; } = Vector4.One;

    /// <summary>
    /// Gets or sets the opacity of the ghost.
    /// </summary>
    /// <value>
    /// A value between 0.0 (fully transparent) and 1.0 (fully opaque).
    /// Default is 0.5 (50% transparent).
    /// </value>
    /// <remarks>
    /// <para>
    /// This value is separate from the alpha component of <see cref="TintColor"/>
    /// for convenience. Games may choose to combine them (multiply) or use
    /// them separately.
    /// </para>
    /// <para>
    /// A value of 0.5 provides good visibility while clearly distinguishing
    /// the ghost from real entities.
    /// </para>
    /// </remarks>
    public float Opacity { get; init; } = 0.5f;

    /// <summary>
    /// Gets or sets an optional label for the ghost.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The label can be displayed above or near the ghost to identify it.
    /// Common uses include:
    /// </para>
    /// <list type="bullet">
    /// <item><description>"Personal Best"</description></item>
    /// <item><description>"World Record"</description></item>
    /// <item><description>"Friend: PlayerName"</description></item>
    /// <item><description>"Previous Attempt"</description></item>
    /// </list>
    /// <para>
    /// Default is null (no label).
    /// </para>
    /// </remarks>
    public string? Label { get; init; }

    /// <summary>
    /// Gets or sets whether the ghost should cast shadows.
    /// </summary>
    /// <remarks>
    /// When true, the ghost should cast shadows like a real entity.
    /// When false (default), the ghost should not affect lighting.
    /// </remarks>
    public bool CastsShadows { get; init; }

    /// <summary>
    /// Gets or sets whether the ghost should receive shadows.
    /// </summary>
    /// <remarks>
    /// When true, shadows from other objects appear on the ghost.
    /// When false (default), the ghost ignores shadows for a more ethereal look.
    /// </remarks>
    public bool ReceivesShadows { get; init; }

    /// <summary>
    /// Gets or sets whether the ghost should render with an outline.
    /// </summary>
    /// <remarks>
    /// When true, games should render an outline around the ghost for
    /// better visibility, especially at high opacity levels.
    /// </remarks>
    public bool ShowOutline { get; init; }

    /// <summary>
    /// Gets or sets the outline color when <see cref="ShowOutline"/> is true.
    /// </summary>
    /// <remarks>
    /// Default is the same as <see cref="TintColor"/>. Set to a contrasting
    /// color for better visibility.
    /// </remarks>
    public Vector4? OutlineColor { get; init; }

    /// <summary>
    /// Creates a new <see cref="GhostVisualConfig"/> with the specified opacity.
    /// </summary>
    /// <param name="opacity">The opacity value (0.0 to 1.0).</param>
    /// <returns>A new configuration with the specified opacity.</returns>
    public GhostVisualConfig WithOpacity(float opacity)
        => this with { Opacity = Math.Clamp(opacity, 0f, 1f) };

    /// <summary>
    /// Creates a new <see cref="GhostVisualConfig"/> with the specified tint color.
    /// </summary>
    /// <param name="r">Red component (0.0 to 1.0).</param>
    /// <param name="g">Green component (0.0 to 1.0).</param>
    /// <param name="b">Blue component (0.0 to 1.0).</param>
    /// <returns>A new configuration with the specified tint color.</returns>
    public GhostVisualConfig WithTint(float r, float g, float b)
        => this with { TintColor = new Vector4(r, g, b, 1f) };

    /// <summary>
    /// Creates a new <see cref="GhostVisualConfig"/> with the specified label.
    /// </summary>
    /// <param name="label">The label to display.</param>
    /// <returns>A new configuration with the specified label.</returns>
    public GhostVisualConfig WithLabel(string? label)
        => this with { Label = label };
}
