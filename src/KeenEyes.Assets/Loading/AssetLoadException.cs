namespace KeenEyes.Assets;

/// <summary>
/// Exception thrown when an asset fails to load.
/// </summary>
/// <param name="assetPath">The path of the asset that failed to load.</param>
/// <param name="assetType">The type of asset that was being loaded.</param>
/// <param name="message">The error message.</param>
/// <param name="innerException">The inner exception that caused the failure.</param>
public sealed class AssetLoadException(
    string assetPath,
    Type assetType,
    string message,
    Exception? innerException = null)
    : Exception(message, innerException)
{
    /// <summary>
    /// Gets the path of the asset that failed to load.
    /// </summary>
    public string AssetPath { get; } = assetPath;

    /// <summary>
    /// Gets the type of asset that was being loaded.
    /// </summary>
    public Type AssetType { get; } = assetType;

    /// <summary>
    /// Creates a new asset load exception for a missing file.
    /// </summary>
    /// <param name="assetPath">The path that was not found.</param>
    /// <param name="assetType">The type of asset that was being loaded.</param>
    /// <returns>A new exception instance.</returns>
    public static AssetLoadException FileNotFound(string assetPath, Type assetType)
        => new(assetPath, assetType, $"Asset file not found: {assetPath}");

    /// <summary>
    /// Creates a new asset load exception for an unsupported format.
    /// </summary>
    /// <param name="assetPath">The path of the unsupported asset.</param>
    /// <param name="assetType">The type of asset that was being loaded.</param>
    /// <param name="extension">The unsupported file extension.</param>
    /// <returns>A new exception instance.</returns>
    public static AssetLoadException UnsupportedFormat(string assetPath, Type assetType, string extension)
        => new(assetPath, assetType, $"No loader registered for extension '{extension}' when loading {assetType.Name}");

    /// <summary>
    /// Creates a new asset load exception for a parse error.
    /// </summary>
    /// <param name="assetPath">The path of the asset.</param>
    /// <param name="assetType">The type of asset that was being loaded.</param>
    /// <param name="innerException">The exception from the parser.</param>
    /// <returns>A new exception instance.</returns>
    public static AssetLoadException ParseError(string assetPath, Type assetType, Exception innerException)
        => new(assetPath, assetType, $"Failed to parse asset: {assetPath}", innerException);
}
