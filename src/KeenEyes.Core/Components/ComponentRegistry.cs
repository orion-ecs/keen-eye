using System.Runtime.CompilerServices;
using KeenEyes.Capabilities;

namespace KeenEyes;

/// <summary>
/// Registry that tracks component types for a specific World.
/// Each World has its own registry, allowing isolated ECS instances.
/// </summary>
/// <remarks>
/// <para>
/// This class is thread-safe: component registration and lookup can be called
/// concurrently from multiple threads.
/// </para>
/// </remarks>
public sealed class ComponentRegistry : IComponentRegistry
{
    private readonly Lock syncRoot = new();
    private readonly Dictionary<Type, ComponentInfo> byType = [];
    private readonly List<ComponentInfo> all = [];
    private int nextId;

    /// <summary>
    /// All registered component types.
    /// </summary>
    /// <remarks>
    /// Returns a snapshot to allow safe iteration while other threads may register components.
    /// </remarks>
    public IReadOnlyList<ComponentInfo> All
    {
        get
        {
            lock (syncRoot)
            {
                return [.. all];
            }
        }
    }

    /// <summary>
    /// Number of registered component types.
    /// </summary>
    public int Count
    {
        get
        {
            lock (syncRoot)
            {
                return all.Count;
            }
        }
    }

    /// <summary>
    /// Registers a component type and returns its info.
    /// If already registered, returns the existing info.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="isTag">Whether this is a tag component.</param>
    /// <returns>The component info for this type.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ComponentInfo Register<T>(bool isTag = false) where T : struct, IComponent
    {
        var type = typeof(T);

        lock (syncRoot)
        {
            if (byType.TryGetValue(type, out var existing))
            {
                return existing;
            }

            // Auto-detect if type implements ITagComponent (AOT-compatible type check)
            var actuallyTag = isTag || typeof(ITagComponent).IsAssignableFrom(type);

            var id = new ComponentId(nextId++);
            var size = actuallyTag ? 0 : ComponentMeta<T>.Size;

            // Pre-create default value for tags at registration time (AOT-compatible)
            object? defaultValue = actuallyTag ? (object)default(T)! : null;

            ComponentInfo? infoRef = null;
            var info = new ComponentInfo(id, type, size, actuallyTag)
            {
                // Store setup function that knows how to create the dispatcher for this component type
                // This avoids reflection during entity creation (cold path setup, hot path usage)
                SetupDispatcher = (self, handlers) =>
                {
                    self.FireAddedBoxed = (h, e, obj) => h.FireAdded(e, (T)obj);
                },
                // Factory for creating typed component arrays without reflection (AOT-compatible)
                CreateComponentArray = capacity => new FixedComponentArray<T>(capacity),
                // Applicator for adding this component to an EntityBuilder without reflection
                ApplyToBuilder = (builder, boxedValue) => builder.With((T)boxedValue),
                // Validator invoker for calling typed validators without reflection
                InvokeValidator = (world, entity, data, validator) =>
                {
                    var typedValidator = (ComponentValidator<T>)validator;
                    var component = (T)data;
                    return typedValidator(world, entity, component);
                }
            };

            // Set up tag applicator after info is created (needs to capture info reference)
            // Uses WithBoxed with pre-created default value to avoid reflection
            infoRef = info;
            if (actuallyTag)
            {
                info.ApplyTagToBuilder = builder => builder.WithBoxed(infoRef, defaultValue!);
            }

            byType[type] = info;
            all.Add(info);

            return info;
        }
    }

    /// <summary>
    /// Gets the component info for a type, or null if not registered.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ComponentInfo? Get<T>() where T : struct
    {
        lock (syncRoot)
        {
            return byType.TryGetValue(typeof(T), out var info) ? info : null;
        }
    }

    /// <summary>
    /// Gets the component info for a type, or null if not registered.
    /// </summary>
    public ComponentInfo? Get(Type type)
    {
        lock (syncRoot)
        {
            return byType.TryGetValue(type, out var info) ? info : null;
        }
    }

    /// <summary>
    /// Gets or registers a component type.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ComponentInfo GetOrRegister<T>(bool isTag = false) where T : struct, IComponent
    {
        // Register already handles locking and check-then-add atomically
        return Register<T>(isTag);
    }

    /// <summary>
    /// Checks if a component type is registered.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsRegistered<T>() where T : struct
    {
        lock (syncRoot)
        {
            return byType.ContainsKey(typeof(T));
        }
    }

    /// <summary>
    /// Gets the component info by its ID, or null if the ID is invalid.
    /// </summary>
    /// <param name="id">The component ID to look up.</param>
    /// <returns>The component info, or null if the ID is out of range.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ComponentInfo? GetById(ComponentId id)
    {
        lock (syncRoot)
        {
            var index = id.Value;
            if (index < 0 || index >= all.Count)
            {
                return null;
            }
            return all[index];
        }
    }
}
