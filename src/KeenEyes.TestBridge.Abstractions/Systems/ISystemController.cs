namespace KeenEyes.TestBridge.Systems;

/// <summary>
/// Controller for managing ECS systems in the world.
/// </summary>
/// <remarks>
/// <para>
/// The system controller provides methods to query, enable, disable, and
/// modify system execution order. This is useful for debugging (disabling
/// specific systems to isolate issues) and testing (controlling which
/// systems run during a test).
/// </para>
/// <para>
/// Systems are identified by their type name. When multiple systems share
/// a name, operations apply to the first match found.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // List all systems
/// var systems = await systemController.GetSystemsAsync();
/// foreach (var system in systems)
/// {
///     Console.WriteLine($"{system.Name}: {(system.Enabled ? "enabled" : "disabled")}");
/// }
///
/// // Disable a system for debugging
/// await systemController.DisableSystemAsync("PhysicsSystem");
///
/// // Re-enable it
/// await systemController.EnableSystemAsync("PhysicsSystem");
/// </code>
/// </example>
public interface ISystemController
{
    /// <summary>
    /// Gets a snapshot of all registered systems.
    /// </summary>
    /// <returns>A list of system snapshots with their current state.</returns>
    Task<IReadOnlyList<SystemSnapshot>> GetSystemsAsync();

    /// <summary>
    /// Gets the count of registered systems.
    /// </summary>
    /// <returns>The number of systems registered with the world.</returns>
    Task<int> GetCountAsync();

    /// <summary>
    /// Gets a system by its type name.
    /// </summary>
    /// <param name="name">The simple type name of the system (e.g., "MovementSystem").</param>
    /// <returns>The system snapshot, or null if not found.</returns>
    Task<SystemSnapshot?> GetSystemAsync(string name);

    /// <summary>
    /// Enables a system by its type name.
    /// </summary>
    /// <param name="name">The simple type name of the system to enable.</param>
    /// <returns>The updated system snapshot.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if no system with the given name exists.</exception>
    /// <remarks>
    /// If the system is already enabled, this is a no-op and returns the current state.
    /// </remarks>
    Task<SystemSnapshot> EnableSystemAsync(string name);

    /// <summary>
    /// Disables a system by its type name.
    /// </summary>
    /// <param name="name">The simple type name of the system to disable.</param>
    /// <returns>The updated system snapshot.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if no system with the given name exists.</exception>
    /// <remarks>
    /// If the system is already disabled, this is a no-op and returns the current state.
    /// </remarks>
    Task<SystemSnapshot> DisableSystemAsync(string name);

    /// <summary>
    /// Toggles the enabled state of a system.
    /// </summary>
    /// <param name="name">The simple type name of the system to toggle.</param>
    /// <returns>The updated system snapshot with the new enabled state.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if no system with the given name exists.</exception>
    Task<SystemSnapshot> ToggleSystemAsync(string name);

    /// <summary>
    /// Gets all systems in a specific phase.
    /// </summary>
    /// <param name="phase">The phase name (e.g., "Update", "FixedUpdate").</param>
    /// <returns>A list of system snapshots in the specified phase.</returns>
    Task<IReadOnlyList<SystemSnapshot>> GetSystemsByPhaseAsync(string phase);

    /// <summary>
    /// Gets all enabled systems.
    /// </summary>
    /// <returns>A list of system snapshots for all enabled systems.</returns>
    Task<IReadOnlyList<SystemSnapshot>> GetEnabledSystemsAsync();

    /// <summary>
    /// Gets all disabled systems.
    /// </summary>
    /// <returns>A list of system snapshots for all disabled systems.</returns>
    Task<IReadOnlyList<SystemSnapshot>> GetDisabledSystemsAsync();
}
