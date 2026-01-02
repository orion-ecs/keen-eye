// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Cli.Commands.Sources;
using KeenEyes.Editor.Plugins.Registry;
using Xunit;

namespace KeenEyes.Cli.Tests.Commands;

[Collection("PluginRegistry")]
public sealed class SourcesCommandTests : IDisposable
{
    private readonly string testDir;
    private readonly TestConsoleOutput output;

    public SourcesCommandTests()
    {
        testDir = Path.Combine(Path.GetTempPath(), "keeneyes-test-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(testDir);
        PluginRegistryPaths.SetTestOverride(testDir);

        output = new TestConsoleOutput();
    }

    public void Dispose()
    {
        PluginRegistryPaths.SetTestOverride(null);

        if (Directory.Exists(testDir))
        {
            try
            {
                Directory.Delete(testDir, recursive: true);
            }
            catch
            {
                // Ignore cleanup failures
            }
        }
    }

    [Fact]
    public async Task SourcesList_ShowsConfiguredSources()
    {
        var command = new SourcesListCommand();
        var result = await command.ExecuteAsync([], output, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Contains(output.Lines, l => l.Contains("nuget.org"));
    }

    [Fact]
    public async Task SourcesAdd_AddsNewSource()
    {
        var command = new SourcesAddCommand();
        var result = await command.ExecuteAsync(
            ["mycompany", "https://pkgs.example.com/v3/index.json"],
            output,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Contains(output.Successes, s => s.Contains("mycompany"));

        // Verify it was added
        var registry = new PluginRegistry();
        registry.Load();
        Assert.Contains(registry.GetSources(), s => s.Name == "mycompany");
    }

    [Fact]
    public async Task SourcesAdd_WithInvalidUrl_ReturnsError()
    {
        var command = new SourcesAddCommand();
        var result = await command.ExecuteAsync(
            ["mycompany", "not-a-url"],
            output,
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(2, result.ExitCode);
    }

    [Fact]
    public async Task SourcesAdd_WithMissingArgs_ReturnsError()
    {
        var command = new SourcesAddCommand();
        var result = await command.ExecuteAsync(["mycompany"], output, TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(2, result.ExitCode);
    }

    [Fact]
    public async Task SourcesAdd_WithDuplicateName_ReturnsError()
    {
        var addCommand = new SourcesAddCommand();

        // Add first
        await addCommand.ExecuteAsync(
            ["mycompany", "https://pkgs.example.com/v3/index.json"],
            output,
            TestContext.Current.CancellationToken);

        output.Clear();

        // Try to add duplicate
        var result = await addCommand.ExecuteAsync(
            ["mycompany", "https://other.example.com/v3/index.json"],
            output,
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task SourcesRemove_RemovesSource()
    {
        // Setup: Add a source first
        var registry = new PluginRegistry();
        registry.Load();
        registry.AddSource("mycompany", "https://pkgs.example.com/v3/index.json");
        registry.Save();

        var command = new SourcesRemoveCommand();
        var result = await command.ExecuteAsync(["mycompany"], output, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);

        // Verify it was removed
        var registry2 = new PluginRegistry();
        registry2.Load();
        Assert.DoesNotContain(registry2.GetSources(), s => s.Name == "mycompany");
    }

    [Fact]
    public async Task SourcesRemove_WithNonExistentSource_ReturnsError()
    {
        var command = new SourcesRemoveCommand();
        var result = await command.ExecuteAsync(["nonexistent"], output, TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task SourcesCommandGroup_RoutesToSubcommands()
    {
        var group = new SourcesCommandGroup();
        var result = await group.ExecuteAsync(["list"], output, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Contains(output.Lines, l => l.Contains("nuget.org"));
    }

    [Fact]
    public async Task SourcesCommandGroup_WithUnknownCommand_ReturnsError()
    {
        var group = new SourcesCommandGroup();
        var result = await group.ExecuteAsync(["unknown"], output, TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(2, result.ExitCode);
    }
}
