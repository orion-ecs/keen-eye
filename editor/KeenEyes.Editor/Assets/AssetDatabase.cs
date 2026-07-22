using Abstractions = KeenEyes.Editor.Abstractions;

namespace KeenEyes.Editor.Assets;

/// <summary>
/// Manages asset discovery, caching, and hot reload for the editor.
/// </summary>
public sealed class AssetDatabase : Abstractions.IAssetDatabase, IDisposable
{
    private readonly string _projectRoot;
    private readonly Dictionary<string, AssetEntry> _assets = new(StringComparer.OrdinalIgnoreCase);
    private readonly FileSystemWatcher _watcher;
    private readonly Lock _lock = new();
    private bool _isDisposed;

    // Backing delegates for the IAssetDatabase events, which use the abstraction's
    // asset model. They are bridged from the concrete events in the constructor.
    private EventHandler<Abstractions.AssetEventArgs>? _abstractionAssetAdded;
    private EventHandler<Abstractions.AssetEventArgs>? _abstractionAssetRemoved;
    private EventHandler<Abstractions.AssetEventArgs>? _abstractionAssetModified;

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

        // Bridge the concrete asset events to the abstraction events so consumers
        // that observe this database through IAssetDatabase receive notifications.
        AssetAdded += (_, e) => _abstractionAssetAdded?.Invoke(this, new Abstractions.AssetEventArgs(ToAbstraction(e.Asset)));
        AssetRemoved += (_, e) => _abstractionAssetRemoved?.Invoke(this, new Abstractions.AssetEventArgs(ToAbstraction(e.Asset)));
        AssetModified += (_, e) => _abstractionAssetModified?.Invoke(this, new Abstractions.AssetEventArgs(ToAbstraction(e.Asset)));
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

    #region IAssetDatabase (abstraction) implementation

    /// <inheritdoc />
    IReadOnlyDictionary<string, Abstractions.AssetEntry> Abstractions.IAssetDatabase.AllAssets
    {
        get
        {
            lock (_lock)
            {
                var result = new Dictionary<string, Abstractions.AssetEntry>(_assets.Count, StringComparer.OrdinalIgnoreCase);
                foreach (var pair in _assets)
                {
                    result[pair.Key] = ToAbstraction(pair.Value);
                }

                return result;
            }
        }
    }

    /// <inheritdoc />
    event EventHandler<Abstractions.AssetEventArgs>? Abstractions.IAssetDatabase.AssetAdded
    {
        add => _abstractionAssetAdded += value;
        remove => _abstractionAssetAdded -= value;
    }

    /// <inheritdoc />
    event EventHandler<Abstractions.AssetEventArgs>? Abstractions.IAssetDatabase.AssetRemoved
    {
        add => _abstractionAssetRemoved += value;
        remove => _abstractionAssetRemoved -= value;
    }

    /// <inheritdoc />
    event EventHandler<Abstractions.AssetEventArgs>? Abstractions.IAssetDatabase.AssetModified
    {
        add => _abstractionAssetModified += value;
        remove => _abstractionAssetModified -= value;
    }

    /// <inheritdoc />
    Abstractions.AssetEntry? Abstractions.IAssetDatabase.GetAsset(string relativePath)
    {
        var entry = GetAsset(relativePath);
        return entry is null ? null : ToAbstraction(entry);
    }

    /// <inheritdoc />
    IEnumerable<Abstractions.AssetEntry> Abstractions.IAssetDatabase.GetAssetsByType(Abstractions.AssetType assetType)
    {
        return GetAssetsByType(ToConcrete(assetType)).Select(ToAbstraction);
    }

    private static Abstractions.AssetEntry ToAbstraction(AssetEntry entry)
    {
        return new Abstractions.AssetEntry(
            entry.RelativePath,
            entry.FullPath,
            entry.Name,
            ToAbstraction(entry.Type),
            entry.LastModified);
    }

    private static Abstractions.AssetType ToAbstraction(AssetType type) => type switch
    {
        AssetType.Scene => Abstractions.AssetType.Scene,
        AssetType.Prefab => Abstractions.AssetType.Prefab,
        AssetType.WorldConfig => Abstractions.AssetType.WorldConfig,
        AssetType.Shader => Abstractions.AssetType.Shader,
        AssetType.Texture => Abstractions.AssetType.Texture,
        AssetType.Audio => Abstractions.AssetType.Audio,
        AssetType.Script => Abstractions.AssetType.Script,
        AssetType.Data => Abstractions.AssetType.Data,
        _ => Abstractions.AssetType.Unknown
    };

    private static AssetType ToConcrete(Abstractions.AssetType type) => type switch
    {
        Abstractions.AssetType.Scene => AssetType.Scene,
        Abstractions.AssetType.Prefab => AssetType.Prefab,
        Abstractions.AssetType.WorldConfig => AssetType.WorldConfig,
        Abstractions.AssetType.Shader => AssetType.Shader,
        Abstractions.AssetType.Texture => AssetType.Texture,
        Abstractions.AssetType.Audio => AssetType.Audio,
        Abstractions.AssetType.Script => AssetType.Script,
        Abstractions.AssetType.Data => AssetType.Data,
        _ => AssetType.Unknown
    };

    #endregion

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
