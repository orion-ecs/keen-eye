namespace KeenEyes.Input.Abstractions;

/// <summary>
/// A collection of related input actions for a specific context.
/// </summary>
/// <remarks>
/// <para>
/// An <see cref="InputActionMap"/> groups related actions together, typically for a
/// specific game state or context. For example, you might have separate action maps
/// for "Gameplay", "Menu", and "Vehicle" contexts.
/// </para>
/// <para>
/// Disabling an action map disables all its actions at once, making it easy to
/// switch input contexts (e.g., when opening a menu).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create action maps for different contexts
/// var gameplayMap = new InputActionMap("Gameplay");
/// gameplayMap.AddAction("Jump", InputBinding.FromKey(Key.Space));
/// gameplayMap.AddAction("Fire", InputBinding.FromMouseButton(MouseButton.Left));
///
/// var menuMap = new InputActionMap("Menu");
/// menuMap.AddAction("Select", InputBinding.FromKey(Key.Enter));
/// menuMap.AddAction("Back", InputBinding.FromKey(Key.Escape));
///
/// // Switch contexts
/// gameplayMap.Enabled = false;
/// menuMap.Enabled = true;
/// </code>
/// </example>
public sealed class InputActionMap
{
    private readonly Dictionary<string, InputAction> actions = new(StringComparer.OrdinalIgnoreCase);
    private bool enabled = true;

    /// <summary>
    /// Gets the name of this action map.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets or sets whether this action map is enabled.
    /// </summary>
    /// <remarks>
    /// When disabled, all actions in this map are also disabled.
    /// </remarks>
    public bool Enabled
    {
        get => enabled;
        set
        {
            enabled = value;
            foreach (var action in actions.Values)
            {
                action.Enabled = value;
            }
        }
    }

    /// <summary>
    /// Gets all actions in this map.
    /// </summary>
    public IReadOnlyDictionary<string, InputAction> Actions => actions;

    /// <summary>
    /// Initializes a new instance of the <see cref="InputActionMap"/> class.
    /// </summary>
    /// <param name="name">The name of the action map.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty or whitespace.</exception>
    public InputActionMap(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }

    /// <summary>
    /// Adds a new action to this map.
    /// </summary>
    /// <param name="name">The name of the action.</param>
    /// <param name="bindings">The initial bindings for the action.</param>
    /// <returns>The created <see cref="InputAction"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> is empty, whitespace, or already exists in this map.
    /// </exception>
    public InputAction AddAction(string name, params InputBinding[] bindings)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (actions.ContainsKey(name))
        {
            throw new ArgumentException($"An action with the name '{name}' already exists in this map.", nameof(name));
        }

        var action = new InputAction(name, bindings)
        {
            Enabled = enabled
        };
        actions[name] = action;
        return action;
    }

    /// <summary>
    /// Gets an action by name.
    /// </summary>
    /// <param name="name">The name of the action to get.</param>
    /// <returns>The action if found; otherwise, <c>null</c>.</returns>
    public InputAction? GetAction(string name)
    {
        return actions.GetValueOrDefault(name);
    }

    /// <summary>
    /// Tries to get an action by name.
    /// </summary>
    /// <param name="name">The name of the action to get.</param>
    /// <param name="action">When this method returns, contains the action if found; otherwise, <c>null</c>.</param>
    /// <returns><c>true</c> if the action was found; otherwise, <c>false</c>.</returns>
    public bool TryGetAction(string name, out InputAction? action)
    {
        return actions.TryGetValue(name, out action);
    }

    /// <summary>
    /// Removes an action from this map.
    /// </summary>
    /// <param name="name">The name of the action to remove.</param>
    /// <returns><c>true</c> if the action was found and removed; otherwise, <c>false</c>.</returns>
    public bool RemoveAction(string name)
    {
        return actions.Remove(name);
    }

    /// <summary>
    /// Removes all actions from this map.
    /// </summary>
    public void Clear()
    {
        actions.Clear();
    }

    /// <summary>
    /// Checks if this map contains an action with the specified name.
    /// </summary>
    /// <param name="name">The name of the action to check for.</param>
    /// <returns><c>true</c> if an action with the name exists; otherwise, <c>false</c>.</returns>
    public bool ContainsAction(string name)
    {
        return actions.ContainsKey(name);
    }
}
