using System.Text.Json;
using System.Text.Json.Serialization;

namespace KeenEyes.Assets;

/// <summary>
/// Represents an asset manifest containing metadata about all project assets.
/// </summary>
/// <remarks>
/// <para>
/// Asset manifests are generated at build time and contain information about all
/// assets in the project, including their paths, types, sizes, and dependencies.
/// </para>
/// <para>
/// This enables runtime asset discovery without filesystem scanning, which is
/// essential for bundled/packaged games, preloading, and validation.
/// </para>
/// </remarks>
public sealed class AssetManifest
{

    private readonly Dictionary<string, AssetInfo> assetsByPath;

    /// <summary>
    /// Gets the manifest format version.
    /// </summary>
    public int Version { get; }

    /// <summary>
    /// Gets the time when the manifest was generated.
    /// </summary>
    public DateTime Generated { get; }

    /// <summary>
    /// Gets the list of all assets in the manifest.
    /// </summary>
    public IReadOnlyList<AssetInfo> Assets { get; }

    /// <summary>
    /// Gets statistics about the assets in the manifest.
    /// </summary>
    public ManifestStatistics Statistics { get; }

    /// <summary>
    /// Creates a new asset manifest.
    /// </summary>
    /// <param name="version">The manifest format version.</param>
    /// <param name="generated">The generation time.</param>
    /// <param name="assets">The list of assets.</param>
    public AssetManifest(int version, DateTime generated, IReadOnlyList<AssetInfo> assets)
    {
        Version = version;
        Generated = generated;
        Assets = assets;

        // Build lookup dictionary
        assetsByPath = new Dictionary<string, AssetInfo>(StringComparer.OrdinalIgnoreCase);
        foreach (var asset in assets)
        {
            assetsByPath[asset.Path] = asset;
        }

        // Calculate statistics
        var byType = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        long totalSize = 0;

        foreach (var asset in assets)
        {
            totalSize += asset.Size;

            if (!byType.TryGetValue(asset.Type, out var count))
            {
                count = 0;
            }

            byType[asset.Type] = count + 1;
        }

        Statistics = new ManifestStatistics(assets.Count, totalSize, byType);
    }

    /// <summary>
    /// Loads an asset manifest from a file path.
    /// </summary>
    /// <param name="path">The path to the manifest file.</param>
    /// <returns>The loaded asset manifest.</returns>
    /// <exception cref="ArgumentNullException">Path is null.</exception>
    /// <exception cref="FileNotFoundException">Manifest file not found.</exception>
    /// <exception cref="InvalidDataException">Manifest file is invalid.</exception>
    public static AssetManifest Load(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Asset manifest not found: {path}", path);
        }

        using var stream = File.OpenRead(path);
        return LoadFromStream(stream);
    }

    /// <summary>
    /// Loads an asset manifest from a stream.
    /// </summary>
    /// <param name="stream">The stream containing the manifest JSON.</param>
    /// <returns>The loaded asset manifest.</returns>
    /// <exception cref="ArgumentNullException">Stream is null.</exception>
    /// <exception cref="InvalidDataException">Manifest data is invalid.</exception>
    public static AssetManifest LoadFromStream(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var json = JsonSerializer.Deserialize(stream, ManifestJsonContext.Default.ManifestJson)
            ?? throw new InvalidDataException("Asset manifest is empty or invalid");

        var assets = json.Assets?
            .Select(a => new AssetInfo(
                a.Path ?? throw new InvalidDataException("Asset path is required"),
                a.Type ?? throw new InvalidDataException("Asset type is required"),
                a.Size,
                a.Hash,
                a.Dependencies?.ToList()))
            .ToList() ?? [];

        return new AssetManifest(
            json.Version,
            json.Generated,
            assets);
    }

    /// <summary>
    /// Checks if an asset exists in the manifest.
    /// </summary>
    /// <param name="path">The asset path to check.</param>
    /// <returns>True if the asset exists in the manifest.</returns>
    public bool Exists(string path)
        => assetsByPath.ContainsKey(path);

    /// <summary>
    /// Gets information about an asset by path.
    /// </summary>
    /// <param name="path">The asset path.</param>
    /// <returns>The asset info, or null if not found.</returns>
    public AssetInfo? GetInfo(string path)
        => assetsByPath.TryGetValue(path, out var info) ? info : null;

    /// <summary>
    /// Gets all assets of a specific type.
    /// </summary>
    /// <param name="type">The asset type to filter by.</param>
    /// <returns>A list of assets matching the type.</returns>
    public IReadOnlyList<AssetInfo> GetAssetsOfType(string type)
        => Assets.Where(a => string.Equals(a.Type, type, StringComparison.OrdinalIgnoreCase)).ToList();

    /// <summary>
    /// Gets the dependencies of an asset.
    /// </summary>
    /// <param name="path">The asset path.</param>
    /// <returns>A list of dependency paths, or an empty list if no dependencies.</returns>
    public IReadOnlyList<string> GetDependencies(string path)
    {
        if (!assetsByPath.TryGetValue(path, out var info))
        {
            return [];
        }

        return info.Dependencies ?? (IReadOnlyList<string>)[];
    }

    /// <summary>
    /// Saves the manifest to a file.
    /// </summary>
    /// <param name="path">The file path to save to.</param>
    public void Save(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        using var stream = File.Create(path);
        SaveToStream(stream);
    }

    /// <summary>
    /// Saves the manifest to a stream.
    /// </summary>
    /// <param name="stream">The stream to write to.</param>
    public void SaveToStream(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var json = new ManifestJson
        {
            Version = Version,
            Generated = Generated,
            Assets = Assets.Select(a => new AssetJson
            {
                Path = a.Path,
                Type = a.Type,
                Size = a.Size,
                Hash = a.Hash,
                Dependencies = a.Dependencies?.ToList()
            }).ToList(),
            Statistics = new StatisticsJson
            {
                TotalAssets = Statistics.TotalAssets,
                TotalSize = Statistics.TotalSize,
                ByType = new Dictionary<string, int>(Statistics.ByType)
            }
        };

        JsonSerializer.Serialize(stream, json, ManifestJsonContext.Default.ManifestJson);
    }

    /// <summary>
    /// Creates a new manifest builder for programmatic manifest construction.
    /// </summary>
    /// <returns>A new manifest builder.</returns>
    public static AssetManifestBuilder CreateBuilder()
        => new();

    #region JSON Models

    internal sealed class ManifestJson
    {
        [JsonPropertyName("version")]
        public int Version { get; set; }

        [JsonPropertyName("generated")]
        public DateTime Generated { get; set; }

        [JsonPropertyName("assets")]
        public List<AssetJson>? Assets { get; set; }

        [JsonPropertyName("statistics")]
        public StatisticsJson? Statistics { get; set; }
    }

    internal sealed class AssetJson
    {
        [JsonPropertyName("path")]
        public string? Path { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("size")]
        public long Size { get; set; }

        [JsonPropertyName("hash")]
        public string? Hash { get; set; }

        [JsonPropertyName("dependencies")]
        public List<string>? Dependencies { get; set; }
    }

    internal sealed class StatisticsJson
    {
        [JsonPropertyName("totalAssets")]
        public int TotalAssets { get; set; }

        [JsonPropertyName("totalSize")]
        public long TotalSize { get; set; }

        [JsonPropertyName("byType")]
        public Dictionary<string, int>? ByType { get; set; }
    }

    #endregion
}

/// <summary>
/// JSON serialization context for asset manifest (AOT compatible).
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = true)]
[JsonSerializable(typeof(AssetManifest.ManifestJson))]
internal partial class ManifestJsonContext : JsonSerializerContext
{
}
