namespace KeenEyes;

/// <summary>
/// Interface for ECS systems that process entities.
/// </summary>
/// <remarks>
/// <para>
/// Systems contain the logic that operates on entities and their components.
/// They are registered with a world and executed during the world's update cycle.
/// </para>
/// <para>
/// For a convenient base implementation, see <c>SystemBase</c> in KeenEyes.Core.
/// </para>
/// </remarks>
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
    /// <param name="world">The world this system operates on.</param>
    void Initialize(IWorld world);

    /// <summary>
    /// Called each frame/tick to update the system.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since the last update in seconds.</param>
    void Update(float deltaTime);
}
