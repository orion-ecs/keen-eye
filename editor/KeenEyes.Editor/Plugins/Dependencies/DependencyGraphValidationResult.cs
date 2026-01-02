// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

namespace KeenEyes.Editor.Plugins.Dependencies;

/// <summary>
/// Represents the result of dependency graph validation.
/// </summary>
/// <remarks>
/// Contains either a valid load order (plugins sorted by dependencies)
/// or a list of errors that prevented resolution.
/// </remarks>
internal sealed class DependencyGraphValidationResult
{
    private DependencyGraphValidationResult(
        bool isValid,
        IReadOnlyList<string>? loadOrder,
        IReadOnlyList<DependencyError>? errors)
    {
        IsValid = isValid;
        LoadOrder = loadOrder ?? [];
        Errors = errors ?? [];
    }

    /// <summary>
    /// Gets a value indicating whether the dependency graph is valid.
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// Gets the plugin IDs in dependency order (load dependencies first).
    /// </summary>
    /// <remarks>
    /// Empty if validation failed. When valid, plugins should be loaded
    /// in this order to ensure dependencies are available.
    /// </remarks>
    public IReadOnlyList<string> LoadOrder { get; }

    /// <summary>
    /// Gets the list of dependency errors, if any.
    /// </summary>
    public IReadOnlyList<DependencyError> Errors { get; }

    /// <summary>
    /// Creates a successful validation result with the given load order.
    /// </summary>
    /// <param name="loadOrder">The plugin IDs in dependency order.</param>
    /// <returns>A successful validation result.</returns>
    public static DependencyGraphValidationResult Success(IReadOnlyList<string> loadOrder)
    {
        return new DependencyGraphValidationResult(true, loadOrder, null);
    }

    /// <summary>
    /// Creates a failed validation result with the given errors.
    /// </summary>
    /// <param name="errors">The dependency errors that caused validation to fail.</param>
    /// <returns>A failed validation result.</returns>
    public static DependencyGraphValidationResult Failure(IReadOnlyList<DependencyError> errors)
    {
        return new DependencyGraphValidationResult(false, null, errors);
    }

    /// <summary>
    /// Creates a failed validation result with a single error.
    /// </summary>
    /// <param name="error">The dependency error that caused validation to fail.</param>
    /// <returns>A failed validation result.</returns>
    public static DependencyGraphValidationResult Failure(DependencyError error)
    {
        return new DependencyGraphValidationResult(false, null, [error]);
    }
}
