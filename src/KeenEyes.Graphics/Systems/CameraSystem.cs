namespace KeenEyes.Graphics;

/// <summary>
/// System that updates camera aspect ratios when the window is resized.
/// </summary>
/// <remarks>
/// <para>
/// This system listens for window resize events and updates all camera components
/// with the new aspect ratio. It runs in the <see cref="SystemPhase.EarlyUpdate"/> phase.
/// </para>
/// </remarks>
public sealed class CameraSystem : SystemBase
{
    private GraphicsContext? graphics;
    private int lastWidth;
    private int lastHeight;

    /// <inheritdoc />
    protected override void OnInitialize()
    {
        if (World.TryGetExtension<GraphicsContext>(out graphics))
        {
            graphics.OnResize += HandleResize;
            if (graphics.Window is not null)
            {
                lastWidth = graphics.Window.Size.X;
                lastHeight = graphics.Window.Size.Y;
            }
        }
    }

    private void HandleResize(int width, int height)
    {
        lastWidth = width;
        lastHeight = height;
        UpdateCameraAspectRatios();
    }

    private void UpdateCameraAspectRatios()
    {
        if (lastWidth <= 0 || lastHeight <= 0)
        {
            return;
        }

        float aspectRatio = (float)lastWidth / lastHeight;

        foreach (var entity in World.Query<Camera>())
        {
            ref var camera = ref World.Get<Camera>(entity);
            camera.AspectRatio = aspectRatio;
        }
    }

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        // Most work is done in resize callback
        // This update ensures cameras added after resize are also updated
        if (graphics?.Window is not null)
        {
            int currentWidth = graphics.Window.Size.X;
            int currentHeight = graphics.Window.Size.Y;

            if (currentWidth != lastWidth || currentHeight != lastHeight)
            {
                lastWidth = currentWidth;
                lastHeight = currentHeight;
                UpdateCameraAspectRatios();
            }
        }
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        if (graphics is not null)
        {
            graphics.OnResize -= HandleResize;
        }
        base.Dispose();
    }
}
