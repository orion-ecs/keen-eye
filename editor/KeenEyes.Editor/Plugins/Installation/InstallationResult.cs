// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

namespace KeenEyes.Editor.Plugins.Installation;

/// <summary>
/// Represents the result of a plugin installation.
/// </summary>
public sealed class InstallationResult
{
    /// <summary>
    /// Gets or sets whether the installation succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets or sets the error message if installation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets or sets the installed version.
    /// </summary>
    public string? InstalledVersion { get; init; }

    /// <summary>
    /// Gets or sets the installation path.
    /// </summary>
    public string? InstallPath { get; init; }

    /// <summary>
    /// Gets or sets the packages that were installed.
    /// </summary>
    public IReadOnlyList<string> InstalledPackages { get; init; } = [];

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="version">The installed version.</param>
    /// <param name="installPath">The installation path.</param>
    /// <param name="installedPackages">List of installed package IDs.</param>
    /// <returns>A successful result.</returns>
    public static InstallationResult Succeeded(
        string version,
        string installPath,
        IReadOnlyList<string>? installedPackages = null)
    {
        return new InstallationResult
        {
            Success = true,
            InstalledVersion = version,
            InstallPath = installPath,
            InstalledPackages = installedPackages ?? []
        };
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>A failed result.</returns>
    public static InstallationResult Failed(string errorMessage)
    {
        return new InstallationResult
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}
