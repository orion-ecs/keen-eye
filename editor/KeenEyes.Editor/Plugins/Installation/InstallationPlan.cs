// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

namespace KeenEyes.Editor.Plugins.Installation;

/// <summary>
/// Represents a planned plugin installation.
/// </summary>
public sealed class InstallationPlan
{
    /// <summary>
    /// Gets or sets whether the plan is valid and can be executed.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Gets or sets the error message if the plan is invalid.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets or sets the packages to install (in dependency order).
    /// </summary>
    public IReadOnlyList<PackageToInstall> PackagesToInstall { get; init; } = [];

    /// <summary>
    /// Gets or sets the total download size in bytes (if known).
    /// </summary>
    public long? TotalDownloadSize { get; init; }

    /// <summary>
    /// Creates an invalid plan with an error message.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>An invalid installation plan.</returns>
    public static InstallationPlan Invalid(string errorMessage)
    {
        return new InstallationPlan
        {
            IsValid = false,
            ErrorMessage = errorMessage
        };
    }

    /// <summary>
    /// Creates a valid plan with packages to install.
    /// </summary>
    /// <param name="packages">The packages to install.</param>
    /// <returns>A valid installation plan.</returns>
    public static InstallationPlan Valid(IReadOnlyList<PackageToInstall> packages)
    {
        return new InstallationPlan
        {
            IsValid = true,
            PackagesToInstall = packages
        };
    }
}
