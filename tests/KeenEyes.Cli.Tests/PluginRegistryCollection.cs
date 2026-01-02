// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using Xunit;

namespace KeenEyes.Cli.Tests;

/// <summary>
/// Collection definition for tests that access the plugin registry.
/// Tests in this collection run sequentially to avoid file locking conflicts.
/// </summary>
[CollectionDefinition("PluginRegistry", DisableParallelization = true)]
public class PluginRegistryCollection
{
}
