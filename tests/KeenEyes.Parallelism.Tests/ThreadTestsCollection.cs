namespace KeenEyes.Tests;

/// <summary>
/// Collection definition for parallelism tests that should not run in parallel with other tests.
/// These tests heavily use ThreadPool and JobScheduler, causing resource contention
/// when run concurrently with other thread-heavy tests.
/// </summary>
[CollectionDefinition("ParallelismTests", DisableParallelization = true)]
public class ParallelismTestsCollection
{
}
