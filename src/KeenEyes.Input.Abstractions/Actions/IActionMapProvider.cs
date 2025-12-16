namespace KeenEyes.Input.Abstractions;

/// <summary>
/// Manages multiple input action maps and provides context switching.
/// </summary>
/// <remarks>
/// <para>
/// An <see cref="IActionMapProvider"/> is an optional abstraction for managing
/// multiple <see cref="InputActionMap"/> instances. It provides:
/// </para>
/// <list type="bullet">
/// <item>Centralized storage of all action maps</item>
/// <item>Active map management for context switching</item>
/// <item>Lookup by name</item>
/// </list>
/// <para>
/// This interface can be registered as a world extension if needed, but is
/// entirely optional. Simple games may prefer to manage action maps directly.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register action maps
/// var provider = new ActionMapProvider();
/// provider.AddActionMap(gameplayMap);
/// provider.AddActionMap(menuMap);
///
/// // Switch context
/// provider.SetActiveMap("Menu");
///
/// // Use active map
/// if (provider.ActiveMap?.GetAction("Select")?.IsPressed(input) == true)
///     SelectMenuItem();
/// </code>
/// </example>
public interface IActionMapProvider
{
    /// <summary>
    /// Gets all registered action maps.
    /// </summary>
    IReadOnlyList<InputActionMap> ActionMaps { get; }

    /// <summary>
    /// Gets the currently active action map.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The active map is the one that should be checked for input each frame.
    /// Only one map can be active at a time.
    /// </para>
    /// <para>
    /// May be <c>null</c> if no map has been set as active.
    /// </para>
    /// </remarks>
    InputActionMap? ActiveMap { get; }

    /// <summary>
    /// Adds an action map to this provider.
    /// </summary>
    /// <param name="map">The action map to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="map"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when an action map with the same name already exists.
    /// </exception>
    void AddActionMap(InputActionMap map);

    /// <summary>
    /// Removes an action map by name.
    /// </summary>
    /// <param name="name">The name of the action map to remove.</param>
    /// <returns><c>true</c> if the map was found and removed; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// If the removed map was the active map, <see cref="ActiveMap"/> becomes <c>null</c>.
    /// </remarks>
    bool RemoveActionMap(string name);

    /// <summary>
    /// Sets the active action map by name.
    /// </summary>
    /// <param name="name">The name of the action map to activate.</param>
    /// <remarks>
    /// <para>
    /// This enables the specified map and disables all others.
    /// </para>
    /// <para>
    /// Pass <c>null</c> or an empty string to deactivate all maps.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when no action map with the specified name exists.
    /// </exception>
    void SetActiveMap(string? name);

    /// <summary>
    /// Gets an action map by name.
    /// </summary>
    /// <param name="name">The name of the action map to get.</param>
    /// <returns>The action map if found; otherwise, <c>null</c>.</returns>
    InputActionMap? GetActionMap(string name);

    /// <summary>
    /// Tries to get an action map by name.
    /// </summary>
    /// <param name="name">The name of the action map to get.</param>
    /// <param name="map">When this method returns, contains the map if found; otherwise, <c>null</c>.</param>
    /// <returns><c>true</c> if the map was found; otherwise, <c>false</c>.</returns>
    bool TryGetActionMap(string name, out InputActionMap? map);
}
