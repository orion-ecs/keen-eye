using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Graphics;

/// <summary>
/// System that updates camera aspect ratios when the window is resized.
/// </summary>
/// <remarks>
/// <para>
/// This system listens for window resize events and updates all camera components
/// with the new aspect ratio. It runs in the EarlyUpdate phase.
/// </para>
/// <para>
/// This system requires an <see cref="IGraphicsContext"/> and <see cref="ILoopProvider"/>
/// extension to be present on the world.
/// </para>
/// </remarks>
public sealed class CameraSystem : ISystem
{
    private IWorld? world;
    private IGraphicsContext? graphics;
    private ILoopProvider? loopProvider;
    private int lastWidth;
    private int lastHeight;

    /// <inheritdoc />
    public bool Enabled { get; set; } = true;

    /// <inheritdoc />
    public void Initialize(IWorld world)
    {
        this.world = world;

        world.TryGetExtension(out graphics);

        if (world.TryGetExtension<ILoopProvider>(out loopProvider) && loopProvider is not null)
        {
            loopProvider.OnResize += HandleResize;
        }

        if (graphics is not null)
        {
            lastWidth = graphics.Width;
            lastHeight = graphics.Height;
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
        if (world is null || lastWidth <= 0 || lastHeight <= 0)
        {
            return;
        }

        float aspectRatio = (float)lastWidth / lastHeight;

        foreach (var entity in world.Query<Camera>())
        {
            ref var camera = ref world.Get<Camera>(entity);
            camera.AspectRatio = aspectRatio;
        }
    }

    /// <inheritdoc />
    public void Update(float deltaTime)
    {
        if (world is null || graphics is null)
        {
            return;
        }

        // This update ensures cameras added after resize are also updated
        int currentWidth = graphics.Width;
        int currentHeight = graphics.Height;

        if (currentWidth != lastWidth || currentHeight != lastHeight)
        {
            lastWidth = currentWidth;
            lastHeight = currentHeight;
            UpdateCameraAspectRatios();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (loopProvider is not null)
        {
            loopProvider.OnResize -= HandleResize;
        }
    }
}
