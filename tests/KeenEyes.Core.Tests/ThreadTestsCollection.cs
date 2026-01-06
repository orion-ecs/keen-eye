namespace KeenEyes.Tests;

/// <summary>
/// Collection definition for thread-based tests that should not run in parallel.
/// Tests in this collection spawn multiple threads and may cause resource contention
/// or thread pool exhaustion when run concurrently with other thread-based tests.
/// </summary>
[CollectionDefinition("ThreadTests", DisableParallelization = true)]
public class ThreadTestsCollection
{
}
