namespace KeenEyes.Graphics.Silk;

/// <summary>
/// System that updates camera matrices each frame.
/// </summary>
/// <remarks>
/// This system runs in the EarlyUpdate phase before other systems that may
/// depend on camera data (such as culling or rendering systems).
/// </remarks>
internal sealed class SilkCameraSystem : ISystem
{
#pragma warning disable IDE0052 // Remove unread private members - will be used when implementing camera logic
    private IWorld? world;
#pragma warning restore IDE0052

    /// <inheritdoc />
    public bool Enabled { get; set; } = true;

    /// <inheritdoc />
    public void Initialize(IWorld world)
    {
        this.world = world;
    }

    /// <inheritdoc />
    public void Update(float deltaTime)
    {
        // TODO: Migrate camera matrix calculations from KeenEyes.Graphics.CameraSystem
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // No resources to dispose
    }
}
