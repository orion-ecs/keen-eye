namespace KeenEyes.Graphics.Silk;

/// <summary>
/// System that renders all visible entities each frame.
/// </summary>
/// <remarks>
/// This system runs in the Render phase and draws all entities with
/// Renderable and Transform3D components.
/// </remarks>
internal sealed class SilkRenderSystem : ISystem
{
#pragma warning disable IDE0052 // Remove unread private members - will be used when implementing render logic
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
        // TODO: Migrate rendering logic from KeenEyes.Graphics.RenderSystem
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // No resources to dispose
    }
}
