namespace KeenEyes.Tests;

/// <summary>
/// Constants used throughout the test suite to replace magic numbers
/// and provide meaningful context for test values.
/// </summary>
public static class TestConstants
{
    /// <summary>
    /// An entity ID that is guaranteed to not exist in any test world.
    /// Used to test operations against non-existent entities.
    /// </summary>
    public const int InvalidEntityId = 999;

    /// <summary>
    /// Default version number for entities, typically used when creating
    /// Entity instances that represent non-existent or dead entities.
    /// </summary>
    public const int DefaultEntityVersion = 1;

    /// <summary>
    /// A ComponentId value that is guaranteed to not exist in any test registry.
    /// Used to test operations against non-existent component types.
    /// </summary>
    public const int InvalidComponentId = 999;

    /// <summary>
    /// High ID offset used in concurrent tests to avoid ID conflicts
    /// between threads allocating from different ranges.
    /// </summary>
    public const int ConcurrentTestHighIdOffset = 1000;

    /// <summary>
    /// Number of entities to create for standard batch operations in tests.
    /// Used when testing iteration, bulk operations, or moderate scale.
    /// </summary>
    public const int StandardBatchSize = 100;

    /// <summary>
    /// Number of entities for small-scale tests or quick iterations.
    /// </summary>
    public const int SmallBatchSize = 10;

    /// <summary>
    /// Number of entities for large-scale stress tests or performance scenarios.
    /// </summary>
    public const int LargeBatchSize = 1000;

    /// <summary>
    /// Short delay in milliseconds for thread synchronization in tests.
    /// </summary>
    public const int ThreadSleepShortMs = 1;

    /// <summary>
    /// Medium delay in milliseconds for thread synchronization in tests.
    /// </summary>
    public const int ThreadSleepMediumMs = 10;

    /// <summary>
    /// Longer delay in milliseconds for ensuring operations complete in tests.
    /// </summary>
    public const int ThreadSleepLongMs = 50;

    /// <summary>
    /// Number of iterations per thread in concurrent tests.
    /// </summary>
    public const int ConcurrentIterationsPerThread = 1000;

    /// <summary>
    /// Timeout in seconds for thread.Join() operations in tests.
    /// Prevents tests from hanging indefinitely if a thread deadlocks.
    /// </summary>
    public const int ThreadJoinTimeoutSeconds = 30;

    /// <summary>
    /// Timeout as TimeSpan for thread.Join() operations.
    /// </summary>
    public static readonly TimeSpan ThreadJoinTimeout = TimeSpan.FromSeconds(ThreadJoinTimeoutSeconds);
}
