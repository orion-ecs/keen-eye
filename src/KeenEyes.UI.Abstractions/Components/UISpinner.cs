using System.Numerics;

namespace KeenEyes.UI.Abstractions;

/// <summary>
/// Specifies the style of spinner animation.
/// </summary>
public enum SpinnerStyle : byte
{
    /// <summary>
    /// Circular spinner that rotates continuously.
    /// </summary>
    Circular = 0,

    /// <summary>
    /// Dots that pulse or bounce in sequence.
    /// </summary>
    Dots = 1,

    /// <summary>
    /// Bar that moves back and forth.
    /// </summary>
    Bar = 2
}

/// <summary>
/// Component for spinner/loading indicator widgets.
/// </summary>
/// <remarks>
/// <para>
/// The UISpinner component creates animated loading indicators. The system handles
/// rotation animation based on <see cref="Speed"/> and updates <see cref="CurrentAngle"/>
/// each frame.
/// </para>
/// <para>
/// Different styles affect how the spinner is rendered:
/// <list type="bullet">
/// <item><see cref="SpinnerStyle.Circular"/> - A rotating arc or ring</item>
/// <item><see cref="SpinnerStyle.Dots"/> - Pulsing dots in sequence</item>
/// <item><see cref="SpinnerStyle.Bar"/> - A bar moving back and forth</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var spinner = world.Spawn()
///     .With(new UIElement { Visible = true })
///     .With(new UIRect { Size = new Vector2(40, 40) })
///     .With(new UISpinner(MathF.PI * 2)
///     {
///         Style = SpinnerStyle.Circular,
///         Color = new Vector4(0.3f, 0.6f, 1f, 1f)
///     })
///     .Build();
/// </code>
/// </example>
/// <param name="speed">The rotation speed in radians per second.</param>
public struct UISpinner(float speed = MathF.PI * 2) : IComponent
{
    /// <summary>
    /// The spinner animation style.
    /// </summary>
    public SpinnerStyle Style = SpinnerStyle.Circular;

    /// <summary>
    /// The rotation speed in radians per second.
    /// </summary>
    /// <remarks>
    /// For <see cref="SpinnerStyle.Circular"/>, this controls rotation speed.
    /// For <see cref="SpinnerStyle.Dots"/>, this controls pulse frequency.
    /// For <see cref="SpinnerStyle.Bar"/>, this controls oscillation speed.
    /// </remarks>
    public float Speed = speed;

    /// <summary>
    /// The spinner color (RGBA, 0-1 range).
    /// </summary>
    public Vector4 Color = new(0.3f, 0.6f, 1f, 1f);

    /// <summary>
    /// The current rotation angle in radians.
    /// </summary>
    /// <remarks>
    /// This value is automatically updated by the UISpinnerSystem.
    /// </remarks>
    public float CurrentAngle = 0;

    /// <summary>
    /// The thickness of the spinner stroke.
    /// </summary>
    /// <remarks>
    /// For <see cref="SpinnerStyle.Circular"/>, this is the ring thickness.
    /// For other styles, this affects element sizing.
    /// </remarks>
    public float Thickness = 3f;

    /// <summary>
    /// The arc length for circular spinners (0 to 1, where 1 is full circle).
    /// </summary>
    /// <remarks>
    /// Only applicable for <see cref="SpinnerStyle.Circular"/>.
    /// A value of 0.25 means a quarter circle arc.
    /// </remarks>
    public float ArcLength = 0.75f;

    /// <summary>
    /// Number of elements for multi-element spinners.
    /// </summary>
    /// <remarks>
    /// For <see cref="SpinnerStyle.Dots"/>, this is the number of dots.
    /// </remarks>
    public int ElementCount = 8;
}

/// <summary>
/// Component for progress bar indicators.
/// </summary>
/// <remarks>
/// <para>
/// Unlike spinners, progress bars show determinate progress from 0 to 1.
/// The <see cref="Value"/> property can be animated smoothly using
/// <see cref="AnimatedValue"/>.
/// </para>
/// </remarks>
/// <param name="value">The initial progress value (0 to 1).</param>
public struct UIProgressBar(float value = 0f) : IComponent
{
    /// <summary>
    /// The current progress value (0 to 1).
    /// </summary>
    public float Value = Math.Clamp(value, 0f, 1f);

    /// <summary>
    /// The animated display value (for smooth transitions).
    /// </summary>
    /// <remarks>
    /// The system smoothly interpolates this toward <see cref="Value"/>.
    /// </remarks>
    public float AnimatedValue = Math.Clamp(value, 0f, 1f);

    /// <summary>
    /// The animation speed for value transitions.
    /// </summary>
    public float AnimationSpeed = 5f;

    /// <summary>
    /// The fill color for the progress portion.
    /// </summary>
    public Vector4 FillColor = new(0.3f, 0.6f, 1f, 1f);

    /// <summary>
    /// The background color for the unfilled portion.
    /// </summary>
    public Vector4 BackgroundColor = new(0.2f, 0.2f, 0.2f, 1f);

    /// <summary>
    /// Whether to show the percentage text.
    /// </summary>
    public bool ShowText = false;
}
