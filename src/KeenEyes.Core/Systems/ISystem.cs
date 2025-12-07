namespace KeenEyes;

/// <summary>
/// Interface for ECS systems that process entities.
/// </summary>
public interface ISystem : IDisposable
{
    /// <summary>
    /// Gets or sets whether this system is enabled.
    /// Disabled systems are skipped during world updates.
    /// </summary>
    bool Enabled { get; set; }

    /// <summary>
    /// Called when the system is added to a world.
    /// </summary>
    void Initialize(World world);

    /// <summary>
    /// Called each frame/tick to update the system.
    /// </summary>
    void Update(float deltaTime);
}

/// <summary>
/// Base class for ECS systems with common functionality.
/// </summary>
public abstract class SystemBase : ISystem
{
    private World? world;
    private bool enabled = true;

    /// <summary>
    /// The world this system operates on.
    /// </summary>
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
    public virtual void Initialize(World world)
    {
        this.world = world;
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
