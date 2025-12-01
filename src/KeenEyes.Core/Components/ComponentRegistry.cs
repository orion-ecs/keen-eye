using System.Runtime.CompilerServices;

namespace KeenEyes;

/// <summary>
/// Registry that tracks component types for a specific World.
/// Each World has its own registry, allowing isolated ECS instances.
/// </summary>
public sealed class ComponentRegistry
{
    private readonly Dictionary<Type, ComponentInfo> byType = [];
    private readonly List<ComponentInfo> all = [];
    private int nextId;

    /// <summary>
    /// All registered component types.
    /// </summary>
    public IReadOnlyList<ComponentInfo> All => all;

    /// <summary>
    /// Number of registered component types.
    /// </summary>
    public int Count => all.Count;

    /// <summary>
    /// Registers a component type and returns its info.
    /// If already registered, returns the existing info.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="isTag">Whether this is a tag component.</param>
    /// <returns>The component info for this type.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ComponentInfo Register<T>(bool isTag = false) where T : struct
    {
        var type = typeof(T);

        if (byType.TryGetValue(type, out var existing))
        {
            return existing;
        }

        var id = new ComponentId(nextId++);
        var size = isTag ? 0 : ComponentMeta<T>.Size;
        var info = new ComponentInfo(id, type, size, isTag);

        byType[type] = info;
        all.Add(info);

        return info;
    }

    /// <summary>
    /// Gets the component info for a type, or null if not registered.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ComponentInfo? Get<T>() where T : struct
    {
        return byType.TryGetValue(typeof(T), out var info) ? info : null;
    }

    /// <summary>
    /// Gets the component info for a type, or null if not registered.
    /// </summary>
    public ComponentInfo? Get(Type type)
    {
        return byType.TryGetValue(type, out var info) ? info : null;
    }

    /// <summary>
    /// Gets or registers a component type.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ComponentInfo GetOrRegister<T>(bool isTag = false) where T : struct
    {
        return Get<T>() ?? Register<T>(isTag);
    }

    /// <summary>
    /// Checks if a component type is registered.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsRegistered<T>() where T : struct
    {
        return byType.ContainsKey(typeof(T));
    }
}
