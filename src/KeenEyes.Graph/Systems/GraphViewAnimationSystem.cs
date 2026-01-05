using KeenEyes.Graph.Abstractions;

namespace KeenEyes.Graph;

/// <summary>
/// System that processes view animation for smooth pan/zoom transitions.
/// </summary>
/// <remarks>
/// <para>
/// This system updates canvases that have a <see cref="GraphViewAnimation"/> component,
/// interpolating the pan and zoom values each frame until the animation completes.
/// </para>
/// <para>
/// Animations are typically triggered by operations like "Frame Selection" (F key).
/// </para>
/// </remarks>
public sealed class GraphViewAnimationSystem : SystemBase
{
    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        // Process all canvases with active view animations
        foreach (var canvas in World.Query<GraphCanvas, GraphViewAnimation, GraphCanvasTag>())
        {
            ref var canvasData = ref World.Get<GraphCanvas>(canvas);
            ref var animation = ref World.Get<GraphViewAnimation>(canvas);

            // Update elapsed time
            animation.ElapsedTime += deltaTime;

            if (animation.IsComplete)
            {
                // Animation finished - set final values and remove component
                canvasData.Pan = animation.TargetPan;
                canvasData.Zoom = animation.TargetZoom;
                World.Remove<GraphViewAnimation>(canvas);
            }
            else
            {
                // Interpolate current values
                canvasData.Pan = animation.CurrentPan;
                canvasData.Zoom = animation.CurrentZoom;
            }
        }
    }
}
