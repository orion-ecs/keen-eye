namespace KeenEyes.Input.Abstractions;

/// <summary>
/// Default implementation of <see cref="IActionMapProvider"/>.
/// </summary>
/// <remarks>
/// <para>
/// Provides simple storage and management of action maps with context switching.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var provider = new ActionMapProvider();
///
/// // Add action maps
/// var gameplayMap = new InputActionMap("Gameplay");
/// gameplayMap.AddAction("Jump", InputBinding.FromKey(Key.Space));
/// provider.AddActionMap(gameplayMap);
///
/// var menuMap = new InputActionMap("Menu");
/// menuMap.AddAction("Back", InputBinding.FromKey(Key.Escape));
/// provider.AddActionMap(menuMap);
///
/// // Activate gameplay context
/// provider.SetActiveMap("Gameplay");
/// </code>
/// </example>
public sealed class ActionMapProvider : IActionMapProvider
{
    private readonly Dictionary<string, InputActionMap> maps = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<InputActionMap> mapList = [];
    private InputActionMap? activeMap;

    /// <inheritdoc />
    public IReadOnlyList<InputActionMap> ActionMaps => mapList;

    /// <inheritdoc />
    public InputActionMap? ActiveMap => activeMap;

    /// <inheritdoc />
    public void AddActionMap(InputActionMap map)
    {
        ArgumentNullException.ThrowIfNull(map);

        if (maps.ContainsKey(map.Name))
        {
            throw new ArgumentException($"An action map with the name '{map.Name}' already exists.", nameof(map));
        }

        maps[map.Name] = map;
        mapList.Add(map);
    }

    /// <inheritdoc />
    public bool RemoveActionMap(string name)
    {
        if (!maps.TryGetValue(name, out var map))
        {
            return false;
        }

        maps.Remove(name);
        mapList.Remove(map);

        if (activeMap == map)
        {
            activeMap = null;
        }

        return true;
    }

    /// <inheritdoc />
    public void SetActiveMap(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            // Deactivate all maps
            foreach (var map in mapList)
            {
                map.Enabled = false;
            }

            activeMap = null;
            return;
        }

        if (!maps.TryGetValue(name, out var targetMap))
        {
            throw new ArgumentException($"No action map with the name '{name}' exists.", nameof(name));
        }

        // Disable all maps, then enable the target
        foreach (var map in mapList)
        {
            map.Enabled = false;
        }

        targetMap.Enabled = true;
        activeMap = targetMap;
    }

    /// <inheritdoc />
    public InputActionMap? GetActionMap(string name)
    {
        return maps.GetValueOrDefault(name);
    }

    /// <inheritdoc />
    public bool TryGetActionMap(string name, out InputActionMap? map)
    {
        return maps.TryGetValue(name, out map);
    }

    /// <summary>
    /// Removes all action maps from this provider.
    /// </summary>
    public void Clear()
    {
        maps.Clear();
        mapList.Clear();
        activeMap = null;
    }
}
