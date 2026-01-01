namespace KeenEyes.Editor.Assets;

/// <summary>
/// Manages asset discovery, caching, and hot reload for the editor.
/// </summary>
public sealed class AssetDatabase : IDisposable
{
    private readonly string _projectRoot;
    private readonly Dictionary<string, AssetEntry> _assets = new(StringComparer.OrdinalIgnoreCase);
    private readonly FileSystemWatcher _watcher;
    private readonly Lock _lock = new();
    private bool _isDisposed;

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
        _projectRoot = Path.GetFullPath(projectRoot);

        if (!Directory.Exists(_projectRoot))
        {
            throw new DirectoryNotFoundException($"Project root not found: {_projectRoot}");
        }

        _watcher = new FileSystemWatcher(_projectRoot)
        {
            NotifyFilter = NotifyFilters.FileName
                | NotifyFilters.DirectoryName
                | NotifyFilters.LastWrite,
            IncludeSubdirectories = true
        };

        _watcher.Created += OnFileCreated;
        _watcher.Deleted += OnFileDeleted;
        _watcher.Changed += OnFileChanged;
        _watcher.Renamed += OnFileRenamed;
    }

    /// <summary>
    /// Gets the project root directory.
    /// </summary>
    public string ProjectRoot => _projectRoot;

    /// <summary>
    /// Gets all known assets.
    /// </summary>
    public IEnumerable<AssetEntry> AllAssets
    {
        get
        {
            lock (_lock)
            {
                return _assets.Values.ToList();
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

        lock (_lock)
        {
            _assets.Clear();

            foreach (var file in Directory.EnumerateFiles(_projectRoot, "*.*", SearchOption.AllDirectories))
            {
                var extension = Path.GetExtension(file);

                if (extensions.Length == 0 || extensionSet.Contains(extension))
                {
                    var relativePath = GetRelativePath(file);
                    var entry = CreateAssetEntry(file, relativePath);
                    _assets[relativePath] = entry;
                }
            }
        }
    }

    /// <summary>
    /// Starts watching for file system changes.
    /// </summary>
    public void StartWatching()
    {
        _watcher.EnableRaisingEvents = true;
    }

    /// <summary>
    /// Stops watching for file system changes.
    /// </summary>
    public void StopWatching()
    {
        _watcher.EnableRaisingEvents = false;
    }

    /// <summary>
    /// Gets an asset by its relative path.
    /// </summary>
    /// <param name="relativePath">The relative path from project root.</param>
    /// <returns>The asset entry, or null if not found.</returns>
    public AssetEntry? GetAsset(string relativePath)
    {
        lock (_lock)
        {
            return _assets.TryGetValue(relativePath, out var entry) ? entry : null;
        }
    }

    /// <summary>
    /// Gets assets by type.
    /// </summary>
    /// <param name="assetType">The type of assets to retrieve.</param>
    /// <returns>All assets of the specified type.</returns>
    public IEnumerable<AssetEntry> GetAssetsByType(AssetType assetType)
    {
        lock (_lock)
        {
            return _assets.Values.Where(a => a.Type == assetType).ToList();
        }
    }

    /// <summary>
    /// Refreshes a specific asset's metadata.
    /// </summary>
    /// <param name="relativePath">The relative path of the asset to refresh.</param>
    public void Refresh(string relativePath)
    {
        var fullPath = Path.Combine(_projectRoot, relativePath);

        lock (_lock)
        {
            if (File.Exists(fullPath))
            {
                var entry = CreateAssetEntry(fullPath, relativePath);
                _assets[relativePath] = entry;
                AssetModified?.Invoke(this, new AssetEventArgs(entry));
            }
            else if (_assets.TryGetValue(relativePath, out var removed))
            {
                _assets.Remove(relativePath);
                AssetRemoved?.Invoke(this, new AssetEventArgs(removed));
            }
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _watcher.EnableRaisingEvents = false;
        _watcher.Created -= OnFileCreated;
        _watcher.Deleted -= OnFileDeleted;
        _watcher.Changed -= OnFileChanged;
        _watcher.Renamed -= OnFileRenamed;
        _watcher.Dispose();

        _isDisposed = true;
    }

    private void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        var relativePath = GetRelativePath(e.FullPath);

        lock (_lock)
        {
            if (!_assets.ContainsKey(relativePath))
            {
                var entry = CreateAssetEntry(e.FullPath, relativePath);
                _assets[relativePath] = entry;
                AssetAdded?.Invoke(this, new AssetEventArgs(entry));
            }
        }
    }

    private void OnFileDeleted(object sender, FileSystemEventArgs e)
    {
        var relativePath = GetRelativePath(e.FullPath);

        lock (_lock)
        {
            if (_assets.TryGetValue(relativePath, out var entry))
            {
                _assets.Remove(relativePath);
                AssetRemoved?.Invoke(this, new AssetEventArgs(entry));
            }
        }
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        var relativePath = GetRelativePath(e.FullPath);

        lock (_lock)
        {
            if (_assets.TryGetValue(relativePath, out _))
            {
                var entry = CreateAssetEntry(e.FullPath, relativePath);
                _assets[relativePath] = entry;
                AssetModified?.Invoke(this, new AssetEventArgs(entry));
            }
        }
    }

    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        var oldRelativePath = GetRelativePath(e.OldFullPath);
        var newRelativePath = GetRelativePath(e.FullPath);

        lock (_lock)
        {
            // Remove old entry
            if (_assets.TryGetValue(oldRelativePath, out var oldEntry))
            {
                _assets.Remove(oldRelativePath);
                AssetRemoved?.Invoke(this, new AssetEventArgs(oldEntry));
            }

            // Add new entry
            var newEntry = CreateAssetEntry(e.FullPath, newRelativePath);
            _assets[newRelativePath] = newEntry;
            AssetAdded?.Invoke(this, new AssetEventArgs(newEntry));
        }
    }

    private string GetRelativePath(string fullPath)
    {
        return Path.GetRelativePath(_projectRoot, fullPath);
    }

    private static AssetEntry CreateAssetEntry(string fullPath, string relativePath)
    {
        var extension = Path.GetExtension(fullPath);
        var assetType = GetAssetType(extension);
        var fileInfo = new FileInfo(fullPath);

        return new AssetEntry
        {
            Name = Path.GetFileNameWithoutExtension(fullPath),
            RelativePath = relativePath,
            FullPath = fullPath,
            Extension = extension,
            Type = assetType,
            LastModified = fileInfo.Exists ? fileInfo.LastWriteTimeUtc : DateTime.UtcNow,
            Size = fileInfo.Exists ? fileInfo.Length : 0
        };
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
