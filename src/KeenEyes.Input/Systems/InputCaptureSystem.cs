using KeenEyes.Input.Abstractions;

namespace KeenEyes.Input;

/// <summary>
/// System that captures input at the beginning of each frame.
/// </summary>
/// <remarks>
/// <para>
/// This system runs very early in the EarlyUpdate phase (order: -1000) to ensure
/// input state is captured before other systems process it.
/// </para>
/// <para>
/// Input events are processed through the underlying backend's event loop.
/// This system calls <see cref="IInputContext.Update"/> for any frame-boundary
/// processing needed by the input backend.
/// </para>
/// <para>
/// This system is backend-agnostic and works with any <see cref="IInputContext"/>
/// implementation registered as a world extension.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Typically added by the input plugin, but can be added manually:
/// world.AddSystem&lt;InputCaptureSystem&gt;(SystemPhase.EarlyUpdate, order: -1000);
/// </code>
/// </example>
public sealed class InputCaptureSystem : ISystem
{
    private IWorld? world;
    private IInputContext? inputContext;

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
        inputContext ??= world?.TryGetExtension<IInputContext>(out var ctx) == true ? ctx : null;

        // Call update to process any queued input
        inputContext?.Update();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // No resources to dispose
    }
}
