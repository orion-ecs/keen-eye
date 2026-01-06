namespace KeenEyes.Tests;

/// <summary>
/// Collection definition for debugging/profiling tests that should not run in parallel.
/// These tests use timers, stopwatches, and performance tracking which can produce
/// inconsistent results when run concurrently with other timing-sensitive tests.
/// </summary>
[CollectionDefinition("DebuggingTests", DisableParallelization = true)]
public class DebuggingTestsCollection
{
}
