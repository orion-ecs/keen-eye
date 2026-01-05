using System.Numerics;

namespace KeenEyes.Graph.Abstractions;

/// <summary>
/// Component for animating view pan/zoom transitions.
/// </summary>
public struct GraphViewAnimation : IComponent
{
    /// <summary>The starting pan position.</summary>
    public Vector2 StartPan;

    /// <summary>The target pan position.</summary>
    public Vector2 TargetPan;

    /// <summary>The starting zoom level.</summary>
    public float StartZoom;

    /// <summary>The target zoom level.</summary>
    public float TargetZoom;

    /// <summary>The total duration of the animation in seconds.</summary>
    public float Duration;

    /// <summary>The elapsed time since the animation started.</summary>
    public float ElapsedTime;

    /// <summary>
    /// Gets whether the animation has completed.
    /// </summary>
    public readonly bool IsComplete => ElapsedTime >= Duration;

    /// <summary>
    /// Gets the current progress (0-1) with easing applied.
    /// </summary>
    public readonly float Progress
    {
        get
        {
            if (Duration <= 0)
            {
                return 1f;
            }

            var t = Math.Clamp(ElapsedTime / Duration, 0f, 1f);
            // EaseOutCubic
            return 1f - MathF.Pow(1f - t, 3f);
        }
    }

    /// <summary>
    /// Gets the current interpolated pan position.
    /// </summary>
    public readonly Vector2 CurrentPan => Vector2.Lerp(StartPan, TargetPan, Progress);

    /// <summary>
    /// Gets the current interpolated zoom level.
    /// </summary>
    public readonly float CurrentZoom => float.Lerp(StartZoom, TargetZoom, Progress);
}
