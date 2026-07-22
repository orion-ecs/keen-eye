using KeenEyes.Editor.Abstractions;

namespace KeenEyes.Editor.Assets;

/// <summary>
/// Manages asset discovery, caching, and hot reload for the editor.
/// </summary>
public sealed class AssetDatabase : IAssetDatabase, IDisposable
{
    private readonly string projectRoot;
    private readonly Dictionary<string, AssetEntry> assets = [with(StringComparer.OrdinalIgnoreCase)];
    private readonly FileSystemWatcher watcher;
    private readonly Lock gate = new();
    private bool isDisposed;

    /// <summary>
    /// Raised when an asset is added to the database.
    /// </summary>
    public event EventHandler<AssetEventArgs>? AssetAdded;

    /// <summary>
    /// Raised when an asset is removed from the database.
    /// </summary>
    public event EventHandler<AssetEventArgs>? AssetRemoved;

    /// <summary>
    /// Raised when an asset is modified.
    /// </summary>
    public event EventHandler<AssetEventArgs>? AssetModified;

    /// <summary>
    /// Creates a new asset database for the specified project root.
    /// </summary>
    /// <param name="projectRoot">The root directory to monitor for assets.</param>
    public AssetDatabase(string projectRoot)
    {
        this.projectRoot = Path.GetFullPath(projectRoot);

        if (!Directory.Exists(this.projectRoot))
        {
            throw new DirectoryNotFoundException($"Project root not found: {this.projectRoot}");
        }

        watcher = new FileSystemWatcher(this.projectRoot)
        {
            NotifyFilter = NotifyFilters.FileName
                | NotifyFilters.DirectoryName
                | NotifyFilters.LastWrite,
            IncludeSubdirectories = true
        };

        watcher.Created += OnFileCreated;
        watcher.Deleted += OnFileDeleted;
        watcher.Changed += OnFileChanged;
        watcher.Renamed += OnFileRenamed;
    }

    /// <summary>
    /// Gets the project root directory.
    /// </summary>
    public string ProjectRoot => projectRoot;

    /// <summary>
    /// Gets all known assets keyed by their relative path.
    /// </summary>
    public IReadOnlyDictionary<string, AssetEntry> AllAssets
    {
        get
        {
            lock (gate)
            {
                return new Dictionary<string, AssetEntry>(assets, StringComparer.OrdinalIgnoreCase);
            }
        }
    }

    /// <summary>
    /// Performs an initial scan of the project directory.
    /// </summary>
    /// <param name="extensions">File extensions to include (e.g., ".kescene", ".keprefab").</param>
    public void Scan(params string[] extensions)
    {
        var extensionSet = new HashSet<string>(extensions, StringComparer.OrdinalIgnoreCase);

        lock (gate)
        {
            assets.Clear();

            foreach (var file in Directory.EnumerateFiles(projectRoot, "*.*", SearchOption.AllDirectories))
            {
                var extension = Path.GetExtension(file);

                if (extensions.Length == 0 || extensionSet.Contains(extension))
                {
                    var relativePath = GetRelativePath(file);
                    var entry = CreateAssetEntry(file, relativePath);
                    assets[relativePath] = entry;
                }
            }
        }
    }

    /// <summary>
    /// Starts watching for file system changes.
    /// </summary>
    public void StartWatching()
    {
        watcher.EnableRaisingEvents = true;
    }

    /// <summary>
    /// Stops watching for file system changes.
    /// </summary>
    public void StopWatching()
    {
        watcher.EnableRaisingEvents = false;
    }

    /// <summary>
    /// Gets an asset by its relative path.
    /// </summary>
    /// <param name="relativePath">The relative path from project root.</param>
    /// <returns>The asset entry, or null if not found.</returns>
    public AssetEntry? GetAsset(string relativePath)
    {
        lock (gate)
        {
            return assets.TryGetValue(relativePath, out var entry) ? entry : null;
        }
    }

    /// <summary>
    /// Gets assets by type.
    /// </summary>
    /// <param name="assetType">The type of assets to retrieve.</param>
    /// <returns>All assets of the specified type.</returns>
    public IEnumerable<AssetEntry> GetAssetsByType(AssetType assetType)
    {
        lock (gate)
        {
            return assets.Values.Where(a => a.Type == assetType).ToList();
        }
    }

    /// <summary>
    /// Refreshes a specific asset's metadata.
    /// </summary>
    /// <param name="relativePath">The relative path of the asset to refresh.</param>
    public void Refresh(string relativePath)
    {
        var fullPath = Path.Combine(projectRoot, relativePath);

        lock (gate)
        {
            if (File.Exists(fullPath))
            {
                var entry = CreateAssetEntry(fullPath, relativePath);
                assets[relativePath] = entry;
                AssetModified?.Invoke(this, new AssetEventArgs(entry));
            }
            else if (assets.TryGetValue(relativePath, out var removed))
            {
                assets.Remove(relativePath);
                AssetRemoved?.Invoke(this, new AssetEventArgs(removed));
            }
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (isDisposed)
        {
            return;
        }

        watcher.EnableRaisingEvents = false;
        watcher.Created -= OnFileCreated;
        watcher.Deleted -= OnFileDeleted;
        watcher.Changed -= OnFileChanged;
        watcher.Renamed -= OnFileRenamed;
        watcher.Dispose();

        isDisposed = true;
    }

    private void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        var relativePath = GetRelativePath(e.FullPath);

        lock (gate)
        {
            if (!assets.ContainsKey(relativePath))
            {
                var entry = CreateAssetEntry(e.FullPath, relativePath);
                assets[relativePath] = entry;
                AssetAdded?.Invoke(this, new AssetEventArgs(entry));
            }
        }
    }

    private void OnFileDeleted(object sender, FileSystemEventArgs e)
    {
        var relativePath = GetRelativePath(e.FullPath);

        lock (gate)
        {
            if (assets.TryGetValue(relativePath, out var entry))
            {
                assets.Remove(relativePath);
                AssetRemoved?.Invoke(this, new AssetEventArgs(entry));
            }
        }
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        var relativePath = GetRelativePath(e.FullPath);

        lock (gate)
        {
            if (assets.ContainsKey(relativePath))
            {
                var entry = CreateAssetEntry(e.FullPath, relativePath);
                assets[relativePath] = entry;
                AssetModified?.Invoke(this, new AssetEventArgs(entry));
            }
        }
    }

    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        var oldRelativePath = GetRelativePath(e.OldFullPath);
        var newRelativePath = GetRelativePath(e.FullPath);

        lock (gate)
        {
            // Remove old entry
            if (assets.TryGetValue(oldRelativePath, out var oldEntry))
            {
                assets.Remove(oldRelativePath);
                AssetRemoved?.Invoke(this, new AssetEventArgs(oldEntry));
            }

            // Add new entry
            var newEntry = CreateAssetEntry(e.FullPath, newRelativePath);
            assets[newRelativePath] = newEntry;
            AssetAdded?.Invoke(this, new AssetEventArgs(newEntry));
        }
    }

    private string GetRelativePath(string fullPath)
    {
        return Path.GetRelativePath(projectRoot, fullPath);
    }

    private static AssetEntry CreateAssetEntry(string fullPath, string relativePath)
    {
        var extension = Path.GetExtension(fullPath);
        var assetType = GetAssetType(extension);
        var fileInfo = new FileInfo(fullPath);

        return new AssetEntry(
            relativePath,
            fullPath,
            Path.GetFileNameWithoutExtension(fullPath),
            assetType,
            fileInfo.Exists ? fileInfo.LastWriteTimeUtc : DateTime.UtcNow);
    }

    private static AssetType GetAssetType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".kescene" => AssetType.Scene,
            ".keprefab" => AssetType.Prefab,
            ".keworld" => AssetType.WorldConfig,
            ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif" => AssetType.Texture,
            ".kesl" => AssetType.Shader,
            ".wav" or ".ogg" or ".mp3" => AssetType.Audio,
            ".cs" => AssetType.Script,
            ".json" => AssetType.Data,
            _ => AssetType.Unknown
        };
    }
}
