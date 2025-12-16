namespace KeenEyes.Input.Silk;

/// <summary>
/// System that captures input at the beginning of each frame.
/// </summary>
/// <remarks>
/// <para>
/// This system runs very early in the EarlyUpdate phase (order: -1000) to ensure
/// input state is captured before other systems process it.
/// </para>
/// <para>
/// For Silk.NET, input events are processed through the window's event loop.
/// This system primarily exists to call <see cref="Abstractions.IInputContext.Update"/> for
/// any frame-boundary processing needed.
/// </para>
/// </remarks>
internal sealed class SilkInputCaptureSystem : ISystem
{
    private IWorld? world;
    private SilkInputContext? inputContext;

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
        // Lazy initialization - get input context on first update
        inputContext ??= world?.TryGetExtension<SilkInputContext>(out var ctx) == true ? ctx : null;

        // Call update to process any queued input
        inputContext?.Update();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // No resources to dispose
    }
}
