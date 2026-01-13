using System.Collections.Concurrent;

namespace KeenEyes.Shaders.HotReload;

/// <summary>
/// Registry for tracking hot-reloadable shaders.
/// </summary>
public sealed class ShaderRegistry
{
    private readonly ConcurrentDictionary<string, IHotReloadable> shaders = new();

    /// <summary>
    /// Event raised when a shader is updated.
    /// </summary>
    public event Action<string, IHotReloadable>? OnShaderUpdated;

    /// <summary>
    /// Event raised when a shader is registered.
    /// </summary>
    public event Action<string, IHotReloadable>? OnShaderRegistered;

    /// <summary>
    /// Event raised when a shader is unregistered.
    /// </summary>
    public event Action<string>? OnShaderUnregistered;

    /// <summary>
    /// Gets the number of registered shaders.
    /// </summary>
    public int Count => shaders.Count;

    /// <summary>
    /// Gets all registered shader names.
    /// </summary>
    public IEnumerable<string> ShaderNames => shaders.Keys;

    /// <summary>
    /// Registers a hot-reloadable shader.
    /// </summary>
    /// <param name="shader">The shader to register.</param>
    /// <returns>True if the shader was registered, false if a shader with the same name already exists.</returns>
    public bool Register(IHotReloadable shader)
    {
        if (shaders.TryAdd(shader.Name, shader))
        {
            OnShaderRegistered?.Invoke(shader.Name, shader);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Unregisters a shader by name.
    /// </summary>
    /// <param name="name">The shader name.</param>
    /// <returns>True if the shader was unregistered, false if it wasn't found.</returns>
    public bool Unregister(string name)
    {
        if (shaders.TryRemove(name, out _))
        {
            OnShaderUnregistered?.Invoke(name);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets a shader by name.
    /// </summary>
    /// <param name="name">The shader name.</param>
    /// <returns>The shader, or null if not found.</returns>
    public IHotReloadable? Get(string name)
    {
        shaders.TryGetValue(name, out var shader);
        return shader;
    }

    /// <summary>
    /// Checks if a shader is registered.
    /// </summary>
    /// <param name="name">The shader name.</param>
    /// <returns>True if the shader is registered.</returns>
    public bool Contains(string name) => shaders.ContainsKey(name);

    /// <summary>
    /// Updates a shader with new source code.
    /// </summary>
    /// <param name="name">The shader name.</param>
    /// <param name="newSource">The new shader source code.</param>
    /// <param name="backend">The shader backend to use.</param>
    /// <returns>True if the shader was found and updated, false otherwise.</returns>
    public bool Update(string name, string newSource, ShaderBackend backend)
    {
        if (shaders.TryGetValue(name, out var shader))
        {
            shader.Reload(newSource, backend);
            OnShaderUpdated?.Invoke(name, shader);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Clears all registered shaders.
    /// </summary>
    public void Clear()
    {
        var names = shaders.Keys.ToList();
        shaders.Clear();

        foreach (var name in names)
        {
            OnShaderUnregistered?.Invoke(name);
        }
    }
}
