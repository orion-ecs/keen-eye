namespace KeenEyes;

/// <summary>
/// Interface for ECS systems that process entities.
/// </summary>
public interface ISystem : IDisposable
{
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

    /// <summary>
    /// The world this system operates on.
    /// </summary>
    protected World World => world ?? throw new InvalidOperationException("System not initialized");

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

    /// <inheritdoc />
    public abstract void Update(float deltaTime);

    /// <inheritdoc />
    public virtual void Dispose() { }
}
