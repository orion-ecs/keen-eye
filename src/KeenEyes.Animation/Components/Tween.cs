using System.Numerics;
using KeenEyes.Animation.Tweening;

namespace KeenEyes.Animation.Components;

/// <summary>
/// Component for tweening a float value over time.
/// </summary>
/// <remarks>
/// <para>
/// Tween components interpolate values over a duration with optional easing.
/// The tweened value is stored in <see cref="CurrentValue"/> and can be
/// applied to other components by systems.
/// </para>
/// <para>
/// For property-specific tweening (position, scale, color, etc.), consider
/// using specialized components or attaching metadata to identify the target.
/// </para>
/// </remarks>
[Component]
public partial struct TweenFloat
{
    /// <summary>
    /// The starting value.
    /// </summary>
    public float StartValue;

    /// <summary>
    /// The ending value.
    /// </summary>
    public float EndValue;

    /// <summary>
    /// The duration of the tween in seconds.
    /// </summary>
    public float Duration;

    /// <summary>
    /// The elapsed time in seconds.
    /// </summary>
    [BuilderIgnore]
    public float ElapsedTime;

    /// <summary>
    /// The current interpolated value.
    /// </summary>
    [BuilderIgnore]
    public float CurrentValue;

    /// <summary>
    /// The easing function to use.
    /// </summary>
    public EaseType EaseType;

    /// <summary>
    /// Whether the tween is currently playing.
    /// </summary>
    public bool IsPlaying;

    /// <summary>
    /// Whether the tween has completed.
    /// </summary>
    [BuilderIgnore]
    public bool IsComplete;

    /// <summary>
    /// Whether to loop the tween.
    /// </summary>
    public bool Loop;

    /// <summary>
    /// Whether to ping-pong (reverse direction each cycle).
    /// </summary>
    public bool PingPong;

    /// <summary>
    /// Creates a tween from start to end over the specified duration.
    /// </summary>
    public static TweenFloat Create(float start, float end, float duration, EaseType ease = EaseType.Linear) => new()
    {
        StartValue = start,
        EndValue = end,
        Duration = duration,
        ElapsedTime = 0f,
        CurrentValue = start,
        EaseType = ease,
        IsPlaying = true,
        IsComplete = false,
        Loop = false,
        PingPong = false
    };
}

/// <summary>
/// Component for tweening a Vector2 value over time.
/// </summary>
[Component]
public partial struct TweenVector2
{
    /// <summary>
    /// The starting value.
    /// </summary>
    public Vector2 StartValue;

    /// <summary>
    /// The ending value.
    /// </summary>
    public Vector2 EndValue;

    /// <summary>
    /// The duration of the tween in seconds.
    /// </summary>
    public float Duration;

    /// <summary>
    /// The elapsed time in seconds.
    /// </summary>
    [BuilderIgnore]
    public float ElapsedTime;

    /// <summary>
    /// The current interpolated value.
    /// </summary>
    [BuilderIgnore]
    public Vector2 CurrentValue;

    /// <summary>
    /// The easing function to use.
    /// </summary>
    public EaseType EaseType;

    /// <summary>
    /// Whether the tween is currently playing.
    /// </summary>
    public bool IsPlaying;

    /// <summary>
    /// Whether the tween has completed.
    /// </summary>
    [BuilderIgnore]
    public bool IsComplete;

    /// <summary>
    /// Whether to loop the tween.
    /// </summary>
    public bool Loop;

    /// <summary>
    /// Whether to ping-pong (reverse direction each cycle).
    /// </summary>
    public bool PingPong;

    /// <summary>
    /// Creates a tween from start to end over the specified duration.
    /// </summary>
    public static TweenVector2 Create(Vector2 start, Vector2 end, float duration, EaseType ease = EaseType.Linear) => new()
    {
        StartValue = start,
        EndValue = end,
        Duration = duration,
        ElapsedTime = 0f,
        CurrentValue = start,
        EaseType = ease,
        IsPlaying = true,
        IsComplete = false,
        Loop = false,
        PingPong = false
    };
}

/// <summary>
/// Component for tweening a Vector3 value over time.
/// </summary>
[Component]
public partial struct TweenVector3
{
    /// <summary>
    /// The starting value.
    /// </summary>
    public Vector3 StartValue;

    /// <summary>
    /// The ending value.
    /// </summary>
    public Vector3 EndValue;

    /// <summary>
    /// The duration of the tween in seconds.
    /// </summary>
    public float Duration;

    /// <summary>
    /// The elapsed time in seconds.
    /// </summary>
    [BuilderIgnore]
    public float ElapsedTime;

    /// <summary>
    /// The current interpolated value.
    /// </summary>
    [BuilderIgnore]
    public Vector3 CurrentValue;

    /// <summary>
    /// The easing function to use.
    /// </summary>
    public EaseType EaseType;

    /// <summary>
    /// Whether the tween is currently playing.
    /// </summary>
    public bool IsPlaying;

    /// <summary>
    /// Whether the tween has completed.
    /// </summary>
    [BuilderIgnore]
    public bool IsComplete;

    /// <summary>
    /// Whether to loop the tween.
    /// </summary>
    public bool Loop;

    /// <summary>
    /// Whether to ping-pong (reverse direction each cycle).
    /// </summary>
    public bool PingPong;

    /// <summary>
    /// Creates a tween from start to end over the specified duration.
    /// </summary>
    public static TweenVector3 Create(Vector3 start, Vector3 end, float duration, EaseType ease = EaseType.Linear) => new()
    {
        StartValue = start,
        EndValue = end,
        Duration = duration,
        ElapsedTime = 0f,
        CurrentValue = start,
        EaseType = ease,
        IsPlaying = true,
        IsComplete = false,
        Loop = false,
        PingPong = false
    };
}

/// <summary>
/// Component for tweening a Vector4 (color) value over time.
/// </summary>
[Component]
public partial struct TweenVector4
{
    /// <summary>
    /// The starting value.
    /// </summary>
    public Vector4 StartValue;

    /// <summary>
    /// The ending value.
    /// </summary>
    public Vector4 EndValue;

    /// <summary>
    /// The duration of the tween in seconds.
    /// </summary>
    public float Duration;

    /// <summary>
    /// The elapsed time in seconds.
    /// </summary>
    [BuilderIgnore]
    public float ElapsedTime;

    /// <summary>
    /// The current interpolated value.
    /// </summary>
    [BuilderIgnore]
    public Vector4 CurrentValue;

    /// <summary>
    /// The easing function to use.
    /// </summary>
    public EaseType EaseType;

    /// <summary>
    /// Whether the tween is currently playing.
    /// </summary>
    public bool IsPlaying;

    /// <summary>
    /// Whether the tween has completed.
    /// </summary>
    [BuilderIgnore]
    public bool IsComplete;

    /// <summary>
    /// Whether to loop the tween.
    /// </summary>
    public bool Loop;

    /// <summary>
    /// Whether to ping-pong (reverse direction each cycle).
    /// </summary>
    public bool PingPong;

    /// <summary>
    /// Creates a tween from start to end over the specified duration.
    /// </summary>
    public static TweenVector4 Create(Vector4 start, Vector4 end, float duration, EaseType ease = EaseType.Linear) => new()
    {
        StartValue = start,
        EndValue = end,
        Duration = duration,
        ElapsedTime = 0f,
        CurrentValue = start,
        EaseType = ease,
        IsPlaying = true,
        IsComplete = false,
        Loop = false,
        PingPong = false
    };
}
