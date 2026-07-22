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
    /// Gets or sets whether the ghost should render a motion trail.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When true, games should read the ghost's recent path from
    /// <see cref="GhostPlayer.GetTrailPoints(System.Span{Vector3})"/> and draw a
    /// fading trail behind the ghost. When false (the default), no trail is drawn
    /// and the trail provider is never consulted, so there is zero behavior change.
    /// </para>
    /// <para>
    /// The ghost system does not render the trail itself; these settings are hints
    /// for the game's rendering code.
    /// </para>
    /// </remarks>
    public bool ShowTrail { get; init; }

    /// <summary>
    /// Gets or sets the maximum number of recent frames included in the trail.
    /// </summary>
    /// <value>
    /// A positive frame count. Default is 60 (roughly one second at 60 fps).
    /// </value>
    /// <remarks>
    /// This bounds both the visual length of the trail and the buffer a renderer
    /// needs to request from
    /// <see cref="GhostPlayer.GetTrailPoints(System.Span{Vector3})"/>. Longer trails
    /// use more memory in the caller's buffer but do not affect stored ghost data.
    /// </remarks>
    public int TrailLength { get; init; } = 60;

    /// <summary>
    /// Gets or sets the opacity at the oldest (tail) end of the trail.
    /// </summary>
    /// <value>
    /// A value between 0.0 (fully transparent tail) and 1.0 (no fade). Default is 0.5.
    /// </value>
    /// <remarks>
    /// The trail fades from this opacity at the tail toward full opacity at the head
    /// (the ghost's current position). Renderers should interpolate opacity across the
    /// trail points using this as the starting value.
    /// </remarks>
    public float TrailFadeStart { get; init; } = 0.5f;

    /// <summary>
    /// Gets or sets the width of the trail, in world units.
    /// </summary>
    /// <value>
    /// A positive width. Default is 0.1.
    /// </value>
    /// <remarks>
    /// Renderers that draw the trail as a line or ribbon should use this as the line
    /// thickness. Renderers that draw discrete markers may ignore it.
    /// </remarks>
    public float TrailWidth { get; init; } = 0.1f;

    /// <summary>
    /// Gets or sets the style used to draw the trail.
    /// </summary>
    /// <remarks>
    /// Default is <see cref="TrailStyle.Line"/>. See <see cref="TrailStyle"/> for the
    /// available styles and the <see cref="TrailStyle.Ribbon"/> fallback note.
    /// </remarks>
    public TrailStyle TrailStyle { get; init; } = TrailStyle.Line;

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
