using KeenEyes.Capabilities;

namespace KeenEyes.Testing.Capabilities;

/// <summary>
/// Records information about a system hook that was added.
/// </summary>
/// <param name="BeforeHook">The before hook callback, or null.</param>
/// <param name="AfterHook">The after hook callback, or null.</param>
/// <param name="Phase">The optional phase filter.</param>
public readonly record struct RegisteredHookInfo(
    SystemHook? BeforeHook,
    SystemHook? AfterHook,
    SystemPhase? Phase);

/// <summary>
/// Mock implementation of <see cref="ISystemHookCapability"/> for testing.
/// </summary>
/// <remarks>
/// <para>
/// This mock tracks all system hooks that are registered and provides methods
/// to verify hook behavior in tests. Hooks can be optionally invoked by
/// calling <see cref="SimulateSystemExecution"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var mockHooks = new MockSystemHookCapability();
/// mockContext.SetCapability&lt;ISystemHookCapability&gt;(mockHooks);
///
/// plugin.Install(mockContext);
///
/// // Verify hooks were registered
/// Assert.True(mockHooks.WasHookAdded);
/// Assert.Equal(1, mockHooks.HookCount);
///
/// // Simulate system execution to invoke hooks
/// mockHooks.SimulateSystemExecution(mockSystem, 0.016f);
/// </code>
/// </example>
public sealed class MockSystemHookCapability : ISystemHookCapability
{
    private readonly List<RegisteredHookInfo> registeredHooks = [];
    private readonly List<EventSubscription> subscriptions = [];

    /// <summary>
    /// Gets all hooks that were registered.
    /// </summary>
    public IReadOnlyList<RegisteredHookInfo> RegisteredHooks => registeredHooks;

    /// <summary>
    /// Gets the number of hooks registered.
    /// </summary>
    public int HookCount => registeredHooks.Count;

    /// <summary>
    /// Gets whether any hooks were added.
    /// </summary>
    public bool WasHookAdded => registeredHooks.Count > 0;

    /// <summary>
    /// Gets or sets whether hooks should actually be invoked when
    /// <see cref="SimulateSystemExecution"/> is called.
    /// </summary>
    public bool EnableHookExecution { get; set; } = true;

    /// <inheritdoc />
    public EventSubscription AddSystemHook(
        SystemHook? beforeHook = null,
        SystemHook? afterHook = null,
        SystemPhase? phase = null)
    {
        if (beforeHook is null && afterHook is null)
        {
            throw new ArgumentException("At least one of beforeHook or afterHook must be non-null.");
        }

        var hookInfo = new RegisteredHookInfo(beforeHook, afterHook, phase);
        registeredHooks.Add(hookInfo);

        var subscription = new EventSubscription(() =>
        {
            registeredHooks.Remove(hookInfo);
        });

        subscriptions.Add(subscription);
        return subscription;
    }

    /// <summary>
    /// Simulates system execution, invoking all registered hooks.
    /// </summary>
    /// <param name="system">The system being executed.</param>
    /// <param name="deltaTime">The delta time for the update.</param>
    /// <param name="phase">The phase of the system. If null, all hooks are invoked.</param>
    public void SimulateSystemExecution(ISystem system, float deltaTime, SystemPhase? phase = null)
    {
        if (!EnableHookExecution)
        {
            return;
        }

        foreach (var hook in registeredHooks)
        {
            // Check phase filter
            if (hook.Phase.HasValue && phase.HasValue && hook.Phase.Value != phase.Value)
            {
                continue;
            }

            hook.BeforeHook?.Invoke(system, deltaTime);
            hook.AfterHook?.Invoke(system, deltaTime);
        }
    }

    /// <summary>
    /// Clears all registered hooks.
    /// </summary>
    public void Clear()
    {
        foreach (var sub in subscriptions)
        {
            sub.Dispose();
        }

        subscriptions.Clear();
        registeredHooks.Clear();
    }

    /// <summary>
    /// Gets the hook registered at the specified index.
    /// </summary>
    /// <param name="index">The index of the hook.</param>
    /// <returns>The hook info.</returns>
    public RegisteredHookInfo GetHook(int index) => registeredHooks[index];

    /// <summary>
    /// Checks if a hook with a before callback was registered.
    /// </summary>
    public bool HasBeforeHook => registeredHooks.Any(h => h.BeforeHook is not null);

    /// <summary>
    /// Checks if a hook with an after callback was registered.
    /// </summary>
    public bool HasAfterHook => registeredHooks.Any(h => h.AfterHook is not null);

    /// <summary>
    /// Checks if any hook was registered for the specified phase.
    /// </summary>
    /// <param name="phase">The phase to check.</param>
    /// <returns>True if a hook was registered for this phase; false otherwise.</returns>
    public bool HasHookForPhase(SystemPhase phase)
    {
        return registeredHooks.Any(h => h.Phase == phase);
    }
}
