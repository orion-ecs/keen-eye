using KeenEyes.Capabilities;

namespace KeenEyes.Testing.Systems;

/// <summary>
/// Records system execution calls for testing and verification.
/// </summary>
/// <remarks>
/// <para>
/// SystemRecorder uses system hooks to automatically record all system
/// executions, allowing tests to verify that systems were called in
/// the expected order and with the expected parameters.
/// </para>
/// <para>
/// The recorder maintains a complete history of all system calls,
/// including timestamps and delta times, enabling detailed analysis
/// of system execution patterns.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// using var testWorld = new TestWorldBuilder()
///     .WithSystemRecording()
///     .WithSystem&lt;MovementSystem&gt;()
///     .Build();
///
/// testWorld.Step();
///
/// var recorder = testWorld.SystemRecorder!;
/// recorder.ShouldHaveCalledSystem&lt;MovementSystem&gt;();
/// Assert.Equal(1, recorder.GetCallCount&lt;MovementSystem&gt;());
/// </code>
/// </example>
public sealed class SystemRecorder : IDisposable
{
    private readonly List<SystemCall> calls = [];
    private readonly Lock callsLock = new();
    private EventSubscription? hookSubscription;
    private bool disposed;

    /// <summary>
    /// Gets all recorded system calls.
    /// </summary>
    public IReadOnlyList<SystemCall> Calls
    {
        get
        {
            lock (callsLock)
            {
                return [.. calls];
            }
        }
    }

    /// <summary>
    /// Gets the total number of recorded system calls.
    /// </summary>
    public int TotalCallCount
    {
        get
        {
            lock (callsLock)
            {
                return calls.Count;
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemRecorder"/> class.
    /// </summary>
    public SystemRecorder()
    {
    }

    /// <summary>
    /// Attaches the recorder to a world using system hooks.
    /// </summary>
    /// <param name="hookCapability">The system hook capability to use.</param>
    /// <param name="phase">Optional phase filter. If null, all phases are recorded.</param>
    /// <exception cref="ArgumentNullException">Thrown when hookCapability is null.</exception>
    public void AttachTo(ISystemHookCapability hookCapability, SystemPhase? phase = null)
    {
        ArgumentNullException.ThrowIfNull(hookCapability);

        hookSubscription?.Dispose();
        hookSubscription = hookCapability.AddSystemHook(
            beforeHook: null,
            afterHook: (system, deltaTime) => RecordCall(system, deltaTime),
            phase: phase
        );
    }

    /// <summary>
    /// Records a system execution call.
    /// </summary>
    /// <param name="system">The system that was executed.</param>
    /// <param name="deltaTime">The delta time passed to the system.</param>
    internal void RecordCall(ISystem system, float deltaTime)
    {
        var call = new SystemCall(
            SystemType: system.GetType(),
            SystemName: system.GetType().Name,
            DeltaTime: deltaTime,
            Timestamp: DateTime.UtcNow
        );

        lock (callsLock)
        {
            calls.Add(call);
        }
    }

    /// <summary>
    /// Manually records a system call (for testing without hooks).
    /// </summary>
    /// <typeparam name="TSystem">The system type to record.</typeparam>
    /// <param name="deltaTime">The delta time to record.</param>
    public void RecordCall<TSystem>(float deltaTime) where TSystem : ISystem
    {
        var call = new SystemCall(
            SystemType: typeof(TSystem),
            SystemName: typeof(TSystem).Name,
            DeltaTime: deltaTime,
            Timestamp: DateTime.UtcNow
        );

        lock (callsLock)
        {
            calls.Add(call);
        }
    }

    /// <summary>
    /// Checks if a system of the specified type was called.
    /// </summary>
    /// <typeparam name="TSystem">The system type to check.</typeparam>
    /// <returns>True if the system was called at least once; otherwise, false.</returns>
    public bool WasCalled<TSystem>() where TSystem : ISystem
    {
        lock (callsLock)
        {
            return calls.Any(c => c.SystemType == typeof(TSystem));
        }
    }

    /// <summary>
    /// Checks if a system with the specified name was called.
    /// </summary>
    /// <param name="systemName">The name of the system to check.</param>
    /// <returns>True if the system was called at least once; otherwise, false.</returns>
    public bool WasCalled(string systemName)
    {
        lock (callsLock)
        {
            return calls.Any(c => c.SystemName == systemName);
        }
    }

    /// <summary>
    /// Gets the number of times a system was called.
    /// </summary>
    /// <typeparam name="TSystem">The system type to count.</typeparam>
    /// <returns>The number of times the system was called.</returns>
    public int GetCallCount<TSystem>() where TSystem : ISystem
    {
        lock (callsLock)
        {
            return calls.Count(c => c.SystemType == typeof(TSystem));
        }
    }

    /// <summary>
    /// Gets the number of times a system with the specified name was called.
    /// </summary>
    /// <param name="systemName">The name of the system to count.</param>
    /// <returns>The number of times the system was called.</returns>
    public int GetCallCount(string systemName)
    {
        lock (callsLock)
        {
            return calls.Count(c => c.SystemName == systemName);
        }
    }

    /// <summary>
    /// Gets all calls for a specific system type.
    /// </summary>
    /// <typeparam name="TSystem">The system type to get calls for.</typeparam>
    /// <returns>A list of all calls for the specified system.</returns>
    public IReadOnlyList<SystemCall> GetCalls<TSystem>() where TSystem : ISystem
    {
        lock (callsLock)
        {
            return calls.Where(c => c.SystemType == typeof(TSystem)).ToList();
        }
    }

    /// <summary>
    /// Gets the last call for a specific system type.
    /// </summary>
    /// <typeparam name="TSystem">The system type to get the last call for.</typeparam>
    /// <returns>The last call for the system, or null if never called.</returns>
    public SystemCall? GetLastCall<TSystem>() where TSystem : ISystem
    {
        lock (callsLock)
        {
            var systemCalls = calls.Where(c => c.SystemType == typeof(TSystem)).ToList();
            return systemCalls.Count > 0 ? systemCalls[^1] : null;
        }
    }

    /// <summary>
    /// Gets the total delta time accumulated for a specific system.
    /// </summary>
    /// <typeparam name="TSystem">The system type to get accumulated time for.</typeparam>
    /// <returns>The total delta time passed to the system across all calls.</returns>
    public float GetTotalDeltaTime<TSystem>() where TSystem : ISystem
    {
        lock (callsLock)
        {
            return calls.Where(c => c.SystemType == typeof(TSystem)).Sum(c => c.DeltaTime);
        }
    }

    /// <summary>
    /// Clears all recorded calls.
    /// </summary>
    public void Clear()
    {
        lock (callsLock)
        {
            calls.Clear();
        }
    }

    /// <summary>
    /// Disposes the recorder and removes any attached hooks.
    /// </summary>
    public void Dispose()
    {
        if (!disposed)
        {
            hookSubscription?.Dispose();
            disposed = true;
        }
    }
}

/// <summary>
/// Represents a recorded system execution call.
/// </summary>
/// <param name="SystemType">The type of the system that was executed.</param>
/// <param name="SystemName">The name of the system that was executed.</param>
/// <param name="DeltaTime">The delta time passed to the system.</param>
/// <param name="Timestamp">The UTC timestamp when the system was executed.</param>
public readonly record struct SystemCall(
    Type SystemType,
    string SystemName,
    float DeltaTime,
    DateTime Timestamp
);
