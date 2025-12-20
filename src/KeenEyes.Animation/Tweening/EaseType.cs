namespace KeenEyes.Animation.Tweening;

/// <summary>
/// Defines the type of easing function to apply to a tween.
/// </summary>
public enum EaseType
{
    /// <summary>Linear interpolation (no easing).</summary>
    Linear,

    /// <summary>Quadratic ease in (accelerating from zero velocity).</summary>
    QuadIn,

    /// <summary>Quadratic ease out (decelerating to zero velocity).</summary>
    QuadOut,

    /// <summary>Quadratic ease in and out.</summary>
    QuadInOut,

    /// <summary>Cubic ease in.</summary>
    CubicIn,

    /// <summary>Cubic ease out.</summary>
    CubicOut,

    /// <summary>Cubic ease in and out.</summary>
    CubicInOut,

    /// <summary>Quartic ease in.</summary>
    QuartIn,

    /// <summary>Quartic ease out.</summary>
    QuartOut,

    /// <summary>Quartic ease in and out.</summary>
    QuartInOut,

    /// <summary>Quintic ease in.</summary>
    QuintIn,

    /// <summary>Quintic ease out.</summary>
    QuintOut,

    /// <summary>Quintic ease in and out.</summary>
    QuintInOut,

    /// <summary>Sinusoidal ease in.</summary>
    SineIn,

    /// <summary>Sinusoidal ease out.</summary>
    SineOut,

    /// <summary>Sinusoidal ease in and out.</summary>
    SineInOut,

    /// <summary>Exponential ease in.</summary>
    ExpoIn,

    /// <summary>Exponential ease out.</summary>
    ExpoOut,

    /// <summary>Exponential ease in and out.</summary>
    ExpoInOut,

    /// <summary>Circular ease in.</summary>
    CircIn,

    /// <summary>Circular ease out.</summary>
    CircOut,

    /// <summary>Circular ease in and out.</summary>
    CircInOut,

    /// <summary>Elastic ease in (spring-like).</summary>
    ElasticIn,

    /// <summary>Elastic ease out.</summary>
    ElasticOut,

    /// <summary>Elastic ease in and out.</summary>
    ElasticInOut,

    /// <summary>Back ease in (overshoots then returns).</summary>
    BackIn,

    /// <summary>Back ease out.</summary>
    BackOut,

    /// <summary>Back ease in and out.</summary>
    BackInOut,

    /// <summary>Bounce ease in.</summary>
    BounceIn,

    /// <summary>Bounce ease out.</summary>
    BounceOut,

    /// <summary>Bounce ease in and out.</summary>
    BounceInOut
}
