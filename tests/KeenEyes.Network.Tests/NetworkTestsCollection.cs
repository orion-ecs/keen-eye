namespace KeenEyes.Tests;

/// <summary>
/// Collection definition for network transport tests that should not run in parallel.
/// These tests use sockets and network resources that can cause port conflicts
/// and resource contention when run concurrently.
/// </summary>
[CollectionDefinition("NetworkTests", DisableParallelization = true)]
public class NetworkTestsCollection
{
}
