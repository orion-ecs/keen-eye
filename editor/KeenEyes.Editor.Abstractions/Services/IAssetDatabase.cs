// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

namespace KeenEyes.Editor.Abstractions;

/// <summary>
/// Manages the database of assets in the project.
/// </summary>
public interface IAssetDatabase
{
    /// <summary>
    /// Gets the project root directory.
    /// </summary>
    string ProjectRoot { get; }

    /// <summary>
    /// Gets all known assets.
    /// </summary>
    IReadOnlyDictionary<string, AssetEntry> AllAssets { get; }

    /// <summary>
    /// Raised when an asset is added.
    /// </summary>
    event EventHandler<AssetEventArgs>? AssetAdded;

    /// <summary>
    /// Raised when an asset is removed.
    /// </summary>
    event EventHandler<AssetEventArgs>? AssetRemoved;

    /// <summary>
    /// Raised when an asset is modified.
    /// </summary>
    event EventHandler<AssetEventArgs>? AssetModified;

    /// <summary>
    /// Scans the project directory for assets with the specified extensions.
    /// </summary>
    /// <param name="extensions">File extensions to include (e.g., ".kescene", ".keprefab").</param>
    void Scan(params string[] extensions);

    /// <summary>
    /// Starts watching for file system changes.
    /// </summary>
    void StartWatching();

    /// <summary>
    /// Stops watching for file system changes.
    /// </summary>
    void StopWatching();

    /// <summary>
    /// Gets an asset by its relative path.
    /// </summary>
    /// <param name="relativePath">The path relative to the project root.</param>
    /// <returns>The asset entry, or null if not found.</returns>
    AssetEntry? GetAsset(string relativePath);

    /// <summary>
    /// Gets all assets of a specific type.
    /// </summary>
    /// <param name="assetType">The asset type to filter by.</param>
    /// <returns>An enumerable of matching assets.</returns>
    IEnumerable<AssetEntry> GetAssetsByType(AssetType assetType);

    /// <summary>
    /// Refreshes a specific asset.
    /// </summary>
    /// <param name="relativePath">The path relative to the project root.</param>
    void Refresh(string relativePath);
}

/// <summary>
/// Represents an asset in the project.
/// </summary>
/// <param name="RelativePath">Path relative to the project root.</param>
/// <param name="FullPath">Full absolute path to the asset.</param>
/// <param name="Name">Display name of the asset.</param>
/// <param name="Type">Type of the asset.</param>
/// <param name="LastModified">Last modification time.</param>
public sealed record AssetEntry(
    string RelativePath,
    string FullPath,
    string Name,
    AssetType Type,
    DateTime LastModified);

/// <summary>
/// Types of assets recognized by the editor.
/// </summary>
public enum AssetType
{
    /// <summary>Unknown asset type.</summary>
    Unknown,

    /// <summary>Scene file (.kescene).</summary>
    Scene,

    /// <summary>Prefab file (.keprefab).</summary>
    Prefab,

    /// <summary>World configuration file (.keworld).</summary>
    WorldConfig,

    /// <summary>Shader source file (.kesl).</summary>
    Shader,

    /// <summary>Image file (texture).</summary>
    Texture,

    /// <summary>Audio file.</summary>
    Audio,

    /// <summary>C# script file.</summary>
    Script,

    /// <summary>Data file (JSON, XML, etc.).</summary>
    Data
}

/// <summary>
/// Event arguments for asset events.
/// </summary>
/// <param name="Asset">The asset that triggered the event.</param>
public sealed class AssetEventArgs(AssetEntry Asset) : EventArgs
{
    /// <summary>
    /// Gets the asset that triggered the event.
    /// </summary>
    public AssetEntry Asset { get; } = Asset;
}
