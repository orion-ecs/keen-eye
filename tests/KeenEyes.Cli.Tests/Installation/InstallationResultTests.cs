// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Editor.Plugins.Installation;

namespace KeenEyes.Cli.Tests.Installation;

public sealed class InstallationResultTests
{
    [Fact]
    public void Succeeded_CreatesSuccessResult()
    {
        var result = InstallationResult.Succeeded(
            "1.2.3",
            "/path/to/package",
            ["Package1", "Package2"]);

        Assert.True(result.Success);
        Assert.Null(result.ErrorMessage);
        Assert.Equal("1.2.3", result.InstalledVersion);
        Assert.Equal("/path/to/package", result.InstallPath);
        Assert.Equal(2, result.InstalledPackages.Count);
    }

    [Fact]
    public void Succeeded_WithoutPackages_DefaultsToEmptyList()
    {
        var result = InstallationResult.Succeeded("1.0.0", "/path");

        Assert.True(result.Success);
        Assert.Empty(result.InstalledPackages);
    }

    [Fact]
    public void Failed_CreatesFailedResult()
    {
        var result = InstallationResult.Failed("Download failed");

        Assert.False(result.Success);
        Assert.Equal("Download failed", result.ErrorMessage);
        Assert.Null(result.InstalledVersion);
        Assert.Null(result.InstallPath);
    }
}
