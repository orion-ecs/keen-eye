// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Editor.Plugins.Installation;

namespace KeenEyes.Cli.Tests.Installation;

public sealed class InstallationPlanTests
{
    [Fact]
    public void Invalid_CreatesInvalidPlan()
    {
        var plan = InstallationPlan.Invalid("Package not found");

        Assert.False(plan.IsValid);
        Assert.Equal("Package not found", plan.ErrorMessage);
        Assert.Empty(plan.PackagesToInstall);
    }

    [Fact]
    public void Valid_CreatesValidPlan()
    {
        var packages = new List<PackageToInstall>
        {
            new()
            {
                PackageId = "Dependency.Package",
                Version = "1.0.0",
                IsPrimary = false
            },
            new()
            {
                PackageId = "Main.Package",
                Version = "2.0.0",
                IsPrimary = true
            }
        };

        var plan = InstallationPlan.Valid(packages);

        Assert.True(plan.IsValid);
        Assert.Null(plan.ErrorMessage);
        Assert.Equal(2, plan.PackagesToInstall.Count);
    }

    [Fact]
    public void Valid_PackagesInDependencyOrder()
    {
        var packages = new List<PackageToInstall>
        {
            new() { PackageId = "Dep1", Version = "1.0.0", IsPrimary = false },
            new() { PackageId = "Dep2", Version = "1.0.0", IsPrimary = false },
            new() { PackageId = "Main", Version = "1.0.0", IsPrimary = true }
        };

        var plan = InstallationPlan.Valid(packages);

        // Dependencies should come before primary
        var primaryIndex = plan.PackagesToInstall
            .Select((p, i) => (p, i))
            .First(x => x.p.IsPrimary).i;

        var dependencyIndices = plan.PackagesToInstall
            .Select((p, i) => (p, i))
            .Where(x => !x.p.IsPrimary)
            .Select(x => x.i);

        Assert.All(dependencyIndices, depIndex => Assert.True(depIndex < primaryIndex));
    }
}
