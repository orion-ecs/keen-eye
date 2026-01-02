using System.Buffers;

namespace KeenEyes;

/// <summary>
/// Manages global system hooks for before/after system execution callbacks.
/// </summary>
/// <remarks>
/// <para>
/// This is an internal manager class that handles system hook registration and invocation.
/// The public API is exposed through <see cref="World"/>.
/// </para>
/// <para>
/// Hooks are stored as tuples of before/after callbacks, allowing independent registration
/// of multiple hooks without interference. Hooks execute in registration order.
/// </para>
/// <para>
/// Performance optimization: When no hooks are registered, invocation methods return immediately
/// with minimal overhead (empty check only).
/// </para>
/// <para>
/// This class is thread-safe: hook registration, unregistration, and invocation can be called
/// concurrently from multiple threads. Invocation uses a snapshot pattern for iteration.
/// </para>
/// </remarks>
internal sealed class SystemHookManager
{
    private readonly Lock syncRoot = new();
    private readonly List<HookEntry> hooks = [];

    /// <summary>
    /// Adds a system hook with optional before and after callbacks.
    /// </summary>
    /// <param name="beforeHook">Optional callback to invoke before system execution.</param>
    /// <param name="afterHook">Optional callback to invoke after system execution.</param>
    /// <param name="phase">Optional phase filter - hook only executes for systems in this phase.</param>
    /// <returns>A subscription that can be disposed to unregister the hook.</returns>
    /// <remarks>
    /// At least one of <paramref name="beforeHook"/> or <paramref name="afterHook"/> must be non-null.
    /// If both are null, an <see cref="ArgumentException"/> is thrown.
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when both beforeHook and afterHook are null.</exception>
    internal EventSubscription AddHook(SystemHook? beforeHook, SystemHook? afterHook, SystemPhase? phase)
    {
        if (beforeHook is null && afterHook is null)
        {
            throw new ArgumentException("At least one of beforeHook or afterHook must be non-null.");
        }

        var entry = new HookEntry(beforeHook, afterHook, phase);
        lock (syncRoot)
        {
            hooks.Add(entry);
        }

        return new EventSubscription(() =>
        {
            lock (syncRoot)
            {
                hooks.Remove(entry);
            }
        });
    }

    /// <summary>
    /// Invokes all registered before-hooks for the specified system.
    /// </summary>
    /// <param name="system">The system about to be executed.</param>
    /// <param name="deltaTime">The time elapsed since the last update.</param>
    /// <param name="phase">The phase the system is executing in.</param>
    /// <remarks>
    /// This method has minimal overhead when no hooks are registered (empty check only).
    /// </remarks>
    internal void InvokeBeforeHooks(ISystem system, float deltaTime, SystemPhase phase)
    {
        // Get a snapshot of hooks under lock using pooled array
        int count;
        HookEntry[] rentedArray;
        lock (syncRoot)
        {
            count = hooks.Count;
            if (count == 0)
            {
                return;
            }
            rentedArray = ArrayPool<HookEntry>.Shared.Rent(count);
            hooks.CopyTo(rentedArray);
        }

        try
        {
            for (int i = 0; i < count; i++)
            {
                var entry = rentedArray[i];
                // Skip if phase filter doesn't match
                if (entry.Phase.HasValue && entry.Phase.Value != phase)
                {
                    continue;
                }

                entry.BeforeHook?.Invoke(system, deltaTime);
            }
        }
        finally
        {
            ArrayPool<HookEntry>.Shared.Return(rentedArray);
        }
    }

    /// <summary>
    /// Invokes all registered after-hooks for the specified system.
    /// </summary>
    /// <param name="system">The system that just executed.</param>
    /// <param name="deltaTime">The time elapsed since the last update.</param>
    /// <param name="phase">The phase the system executed in.</param>
    /// <remarks>
    /// This method has minimal overhead when no hooks are registered (empty check only).
    /// </remarks>
    internal void InvokeAfterHooks(ISystem system, float deltaTime, SystemPhase phase)
    {
        // Get a snapshot of hooks under lock using pooled array
        int count;
        HookEntry[] rentedArray;
        lock (syncRoot)
        {
            count = hooks.Count;
            if (count == 0)
            {
                return;
            }
            rentedArray = ArrayPool<HookEntry>.Shared.Rent(count);
            hooks.CopyTo(rentedArray);
        }

        try
        {
            for (int i = 0; i < count; i++)
            {
                var entry = rentedArray[i];
                // Skip if phase filter doesn't match
                if (entry.Phase.HasValue && entry.Phase.Value != phase)
                {
                    continue;
                }

                entry.AfterHook?.Invoke(system, deltaTime);
            }
        }
        finally
        {
            ArrayPool<HookEntry>.Shared.Return(rentedArray);
        }
    }

    /// <summary>
    /// Clears all registered hooks.
    /// </summary>
    /// <remarks>
    /// This is called during <see cref="World.Dispose()"/> to ensure proper cleanup.
    /// After calling this method, existing <see cref="EventSubscription"/> objects become no-ops when disposed.
    /// </remarks>
    internal void Clear()
    {
        lock (syncRoot)
        {
            hooks.Clear();
        }
    }

    /// <summary>
    /// Internal record for storing hook callbacks with optional phase filter.
    /// </summary>
    private sealed record HookEntry(
        SystemHook? BeforeHook,
        SystemHook? AfterHook,
        SystemPhase? Phase);
}
