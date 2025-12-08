namespace KeenEyes;

/// <summary>
/// Base class for ECS systems with common functionality.
/// </summary>
/// <remarks>
/// <para>
/// SystemBase provides a convenient implementation of <see cref="ISystem"/> with
/// lifecycle hooks for initialization, enabling/disabling, and update phases.
/// </para>
/// <para>
/// Override <see cref="OnInitialize"/> to set up queries and resources,
/// <see cref="OnBeforeUpdate"/> and <see cref="OnAfterUpdate"/> for pre/post update logic,
/// and <see cref="Update"/> for the main processing logic.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class MovementSystem : SystemBase
/// {
///     protected override void OnInitialize()
///     {
///         // Set up queries and resources
///     }
///
///     public override void Update(float deltaTime)
///     {
///         foreach (var entity in World.Query&lt;Position, Velocity&gt;())
///         {
///             ref var pos = ref World.Get&lt;Position&gt;(entity);
///             ref readonly var vel = ref World.Get&lt;Velocity&gt;(entity);
///             pos.X += vel.X * deltaTime;
///             pos.Y += vel.Y * deltaTime;
///         }
///     }
/// }
/// </code>
/// </example>
public abstract class SystemBase : ISystem
{
    private World? world;
    private bool enabled = true;

    /// <summary>
    /// Gets the world this system operates on.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This property returns the concrete <see cref="World"/> type, providing full
    /// access to all world operations including advanced features not available
    /// through the <see cref="IWorld"/> interface.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when accessed before initialization.</exception>
    protected World World => world ?? throw new InvalidOperationException("System not initialized");

    /// <inheritdoc />
    public bool Enabled
    {
        get => enabled;
        set
        {
            if (enabled == value)
            {
                return;
            }
            enabled = value;
            if (enabled)
            {
                OnEnabled();
            }
            else
            {
                OnDisabled();
            }
        }
    }

    /// <inheritdoc />
    public virtual void Initialize(IWorld world)
    {
        this.world = (World)world;
        OnInitialize();
    }

    /// <summary>
    /// Called after the system is initialized with a world.
    /// Override to set up queries and resources.
    /// </summary>
    protected virtual void OnInitialize() { }

    /// <summary>
    /// Called before entity processing in <see cref="Update"/>.
    /// Override to perform pre-update logic such as accumulating time or resetting state.
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last update.</param>
    protected virtual void OnBeforeUpdate(float deltaTime) { }

    /// <summary>
    /// Called after entity processing in <see cref="Update"/>.
    /// Override to perform post-update logic such as cleanup or statistics collection.
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last update.</param>
    protected virtual void OnAfterUpdate(float deltaTime) { }

    /// <summary>
    /// Called when the system is enabled.
    /// Override to perform initialization that should happen each time the system is activated.
    /// </summary>
    protected virtual void OnEnabled() { }

    /// <summary>
    /// Called when the system is disabled.
    /// Override to perform cleanup that should happen each time the system is deactivated.
    /// </summary>
    protected virtual void OnDisabled() { }

    /// <summary>
    /// Invokes the <see cref="OnBeforeUpdate"/> lifecycle hook.
    /// Called by <see cref="World.Update"/> before the main update.
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last update.</param>
    internal void InvokeBeforeUpdate(float deltaTime) => OnBeforeUpdate(deltaTime);

    /// <summary>
    /// Invokes the <see cref="OnAfterUpdate"/> lifecycle hook.
    /// Called by <see cref="World.Update"/> after the main update.
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last update.</param>
    internal void InvokeAfterUpdate(float deltaTime) => OnAfterUpdate(deltaTime);

    /// <inheritdoc />
    public abstract void Update(float deltaTime);

    /// <inheritdoc />
    public virtual void Dispose() { }
}
