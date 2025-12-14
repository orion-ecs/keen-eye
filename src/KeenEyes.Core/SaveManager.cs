using System.IO.Compression;
using KeenEyes.Serialization;

namespace KeenEyes;

/// <summary>
/// Manages save slots and world persistence.
/// </summary>
/// <remarks>
/// <para>
/// This is an internal manager class that handles all save/load operations.
/// The public API is exposed through <see cref="World"/>.
/// </para>
/// <para>
/// SaveManager integrates with <see cref="SnapshotManager"/> for world serialization
/// and <see cref="SaveFileFormat"/> for file I/O with compression and checksums.
/// </para>
/// </remarks>
internal sealed class SaveManager
{
    private readonly World world;
    private string saveDirectory;

    /// <summary>
    /// Creates a new SaveManager for the specified world.
    /// </summary>
    /// <param name="world">The world this manager belongs to.</param>
    /// <param name="saveDirectory">The directory to store save files. Defaults to "./saves".</param>
    internal SaveManager(World world, string? saveDirectory = null)
    {
        this.world = world;
        this.saveDirectory = saveDirectory ?? Path.Combine(Environment.CurrentDirectory, "saves");
    }

    /// <summary>
    /// Gets or sets the directory where save files are stored.
    /// </summary>
    internal string SaveDirectory
    {
        get => saveDirectory;
        set => saveDirectory = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Saves the current world state to a slot.
    /// </summary>
    /// <typeparam name="TSerializer">The serializer type that implements both interfaces.</typeparam>
    /// <param name="slotName">The name of the save slot.</param>
    /// <param name="serializer">The component serializer for AOT-compatible serialization.</param>
    /// <param name="options">Optional save options. Uses default if not specified.</param>
    /// <returns>The save slot info with updated metadata.</returns>
    internal SaveSlotInfo Save<TSerializer>(
        string slotName,
        TSerializer serializer,
        SaveSlotOptions? options = null)
        where TSerializer : IComponentSerializer, IBinaryComponentSerializer
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slotName);
        ArgumentNullException.ThrowIfNull(serializer);

        options ??= SaveSlotOptions.Default;

        // Create snapshot
        var snapshot = SnapshotManager.CreateSnapshot(world, serializer, options.CustomMetadata);

        // Serialize to binary or JSON
        byte[] snapshotData;
        if (options.Format == SaveFormat.Binary)
        {
            snapshotData = SnapshotManager.ToBinary(snapshot, serializer);
        }
        else
        {
            var json = SnapshotManager.ToJson(snapshot);
            snapshotData = System.Text.Encoding.UTF8.GetBytes(json);
        }

        // Check if slot exists (for save count and created timestamp)
        var existingInfo = TryGetSlotInfo(slotName);
        var now = DateTimeOffset.UtcNow;

        // Create slot info
        var slotInfo = new SaveSlotInfo
        {
            SlotName = slotName,
            DisplayName = options.DisplayName,
            CreatedAt = existingInfo?.CreatedAt ?? now,
            ModifiedAt = now,
            PlayTime = options.PlayTime,
            SaveCount = (existingInfo?.SaveCount ?? 0) + 1,
            Format = options.Format,
            EntityCount = snapshot.Entities.Count,
            ThumbnailBase64 = options.ThumbnailData is not null
                ? Convert.ToBase64String(options.ThumbnailData)
                : null,
            ThumbnailMimeType = options.ThumbnailMimeType,
            CustomMetadata = options.CustomMetadata,
            AppVersion = options.AppVersion,
            FormatVersion = 1
        };

        // Ensure directory exists
        EnsureSaveDirectoryExists();

        // Write save file
        var filePath = GetSlotFilePath(slotName);
        using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            SaveFileFormat.Write(fileStream, slotInfo, snapshotData, options);
        }

        // Return the updated slot info (with sizes filled in)
        return SaveFileFormat.ReadMetadata(File.ReadAllBytes(filePath));
    }

    /// <summary>
    /// Loads a world state from a save slot.
    /// </summary>
    /// <typeparam name="TSerializer">The serializer type that implements both interfaces.</typeparam>
    /// <param name="slotName">The name of the save slot to load.</param>
    /// <param name="serializer">The component serializer for AOT-compatible deserialization.</param>
    /// <param name="validateChecksum">Whether to validate the checksum if present.</param>
    /// <returns>
    /// A tuple containing the save slot info and a mapping from original entity IDs to new entities.
    /// </returns>
    /// <exception cref="FileNotFoundException">Thrown when the save slot doesn't exist.</exception>
    /// <exception cref="InvalidDataException">Thrown when the save file is corrupted.</exception>
    internal (SaveSlotInfo SlotInfo, Dictionary<int, Entity> EntityMap) Load<TSerializer>(
        string slotName,
        TSerializer serializer,
        bool validateChecksum = true)
        where TSerializer : IComponentSerializer, IBinaryComponentSerializer
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slotName);
        ArgumentNullException.ThrowIfNull(serializer);

        var filePath = GetSlotFilePath(slotName);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Save slot '{slotName}' not found.", filePath);
        }

        // Read save file
        var fileData = File.ReadAllBytes(filePath);
        var (slotInfo, snapshotData) = SaveFileFormat.Read(fileData, validateChecksum);

        // Deserialize snapshot
        WorldSnapshot snapshot;
        if (slotInfo.Format == SaveFormat.Binary)
        {
            snapshot = SnapshotManager.FromBinary(snapshotData, serializer);
        }
        else
        {
            var json = System.Text.Encoding.UTF8.GetString(snapshotData);
            snapshot = SnapshotManager.FromJson(json)
                ?? throw new InvalidDataException("Failed to deserialize JSON snapshot.");
        }

        // Restore snapshot
        var entityMap = SnapshotManager.RestoreSnapshot(world, snapshot, serializer);

        return (slotInfo, entityMap);
    }

    /// <summary>
    /// Gets information about a save slot without loading it.
    /// </summary>
    /// <param name="slotName">The name of the save slot.</param>
    /// <returns>The save slot info, or null if the slot doesn't exist.</returns>
    internal SaveSlotInfo? TryGetSlotInfo(string slotName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slotName);

        var filePath = GetSlotFilePath(slotName);
        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            var fileData = File.ReadAllBytes(filePath);
            return SaveFileFormat.ReadMetadata(fileData);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets information about a save slot, throwing if it doesn't exist.
    /// </summary>
    /// <param name="slotName">The name of the save slot.</param>
    /// <returns>The save slot info.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the save slot doesn't exist.</exception>
    internal SaveSlotInfo GetSlotInfo(string slotName)
    {
        return TryGetSlotInfo(slotName)
            ?? throw new FileNotFoundException($"Save slot '{slotName}' not found.", GetSlotFilePath(slotName));
    }

    /// <summary>
    /// Lists all available save slots.
    /// </summary>
    /// <returns>An enumerable of save slot info for all valid save files.</returns>
    internal IEnumerable<SaveSlotInfo> ListSlots()
    {
        if (!Directory.Exists(saveDirectory))
        {
            yield break;
        }

        foreach (var filePath in Directory.EnumerateFiles(saveDirectory, $"*{SaveFileFormat.Extension}"))
        {
            SaveSlotInfo? slotInfo = null;
            try
            {
                var fileData = File.ReadAllBytes(filePath);
                if (SaveFileFormat.IsValidFormat(fileData))
                {
                    slotInfo = SaveFileFormat.ReadMetadata(fileData);
                }
            }
            catch
            {
                // Skip invalid files
            }

            if (slotInfo is not null)
            {
                yield return slotInfo;
            }
        }
    }

    /// <summary>
    /// Checks if a save slot exists.
    /// </summary>
    /// <param name="slotName">The name of the save slot.</param>
    /// <returns>True if the slot exists and is valid.</returns>
    internal bool SlotExists(string slotName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slotName);

        var filePath = GetSlotFilePath(slotName);
        if (!File.Exists(filePath))
        {
            return false;
        }

        try
        {
            var fileData = File.ReadAllBytes(filePath);
            return SaveFileFormat.IsValidFormat(fileData);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Deletes a save slot.
    /// </summary>
    /// <param name="slotName">The name of the save slot to delete.</param>
    /// <returns>True if the slot was deleted; false if it didn't exist.</returns>
    internal bool DeleteSlot(string slotName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slotName);

        var filePath = GetSlotFilePath(slotName);
        if (!File.Exists(filePath))
        {
            return false;
        }

        File.Delete(filePath);
        return true;
    }

    /// <summary>
    /// Copies a save slot to a new name.
    /// </summary>
    /// <param name="sourceSlotName">The source slot name.</param>
    /// <param name="destinationSlotName">The destination slot name.</param>
    /// <param name="overwrite">Whether to overwrite if destination exists.</param>
    /// <returns>The save slot info for the copied slot.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the source slot doesn't exist.</exception>
    /// <exception cref="IOException">Thrown when destination exists and overwrite is false.</exception>
    internal SaveSlotInfo CopySlot(string sourceSlotName, string destinationSlotName, bool overwrite = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceSlotName);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationSlotName);

        var sourcePath = GetSlotFilePath(sourceSlotName);
        var destinationPath = GetSlotFilePath(destinationSlotName);

        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException($"Source save slot '{sourceSlotName}' not found.", sourcePath);
        }

        if (!overwrite && File.Exists(destinationPath))
        {
            throw new IOException($"Destination save slot '{destinationSlotName}' already exists.");
        }

        EnsureSaveDirectoryExists();
        File.Copy(sourcePath, destinationPath, overwrite);

        // Update the slot name in the copied file
        var fileData = File.ReadAllBytes(destinationPath);
        var (slotInfo, snapshotData) = SaveFileFormat.Read(fileData, validateChecksum: false);

        var updatedSlotInfo = slotInfo with
        {
            SlotName = destinationSlotName,
            ModifiedAt = DateTimeOffset.UtcNow
        };

        var updatedFileData = SaveFileFormat.Write(updatedSlotInfo, snapshotData, new SaveSlotOptions
        {
            Format = slotInfo.Format,
            Compression = slotInfo.Compression,
            IncludeChecksum = slotInfo.Checksum is not null
        });

        File.WriteAllBytes(destinationPath, updatedFileData);

        return updatedSlotInfo;
    }

    /// <summary>
    /// Renames a save slot.
    /// </summary>
    /// <param name="oldSlotName">The current slot name.</param>
    /// <param name="newSlotName">The new slot name.</param>
    /// <returns>The save slot info with the new name.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the source slot doesn't exist.</exception>
    /// <exception cref="IOException">Thrown when the new name already exists.</exception>
    internal SaveSlotInfo RenameSlot(string oldSlotName, string newSlotName)
    {
        var result = CopySlot(oldSlotName, newSlotName, overwrite: false);
        DeleteSlot(oldSlotName);
        return result;
    }

    /// <summary>
    /// Validates a save slot's integrity.
    /// </summary>
    /// <param name="slotName">The name of the save slot to validate.</param>
    /// <returns>The validation result with any errors.</returns>
    internal SaveSlotInfo? ValidateSlot(string slotName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slotName);

        var filePath = GetSlotFilePath(slotName);
        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            var fileData = File.ReadAllBytes(filePath);
            return SaveFileFormat.Validate(fileData);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the file path for a save slot.
    /// </summary>
    /// <param name="slotName">The slot name.</param>
    /// <returns>The full file path.</returns>
    internal string GetSlotFilePath(string slotName)
    {
        return Path.Combine(saveDirectory, $"{slotName}{SaveFileFormat.Extension}");
    }

    /// <summary>
    /// Ensures the save directory exists.
    /// </summary>
    private void EnsureSaveDirectoryExists()
    {
        if (!Directory.Exists(saveDirectory))
        {
            Directory.CreateDirectory(saveDirectory);
        }
    }

    #region Delta Save Operations

    /// <summary>
    /// Saves a delta snapshot to a slot.
    /// </summary>
    /// <typeparam name="TSerializer">The serializer type that implements both interfaces.</typeparam>
    /// <param name="slotName">The name of the save slot.</param>
    /// <param name="delta">The delta snapshot to save.</param>
    /// <param name="serializer">The component serializer for AOT-compatible serialization.</param>
    /// <param name="options">Optional save options. Uses default if not specified.</param>
    /// <returns>The save slot info with updated metadata.</returns>
    internal SaveSlotInfo SaveDelta<TSerializer>(
        string slotName,
        DeltaSnapshot delta,
        TSerializer serializer,
        SaveSlotOptions? options = null)
        where TSerializer : IComponentSerializer, IBinaryComponentSerializer
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slotName);
        ArgumentNullException.ThrowIfNull(delta);
        ArgumentNullException.ThrowIfNull(serializer);

        options ??= SaveSlotOptions.Default;

        // Serialize delta to JSON
        var json = System.Text.Json.JsonSerializer.Serialize(delta, SnapshotJsonContext.Default.DeltaSnapshot);
        var snapshotData = System.Text.Encoding.UTF8.GetBytes(json);

        // Check if slot exists (for save count and created timestamp)
        var existingInfo = TryGetSlotInfo(slotName);
        var now = DateTimeOffset.UtcNow;

        // Create slot info for delta
        var slotInfo = new SaveSlotInfo
        {
            SlotName = slotName,
            DisplayName = options.DisplayName ?? $"Delta #{delta.SequenceNumber}",
            CreatedAt = existingInfo?.CreatedAt ?? now,
            ModifiedAt = now,
            PlayTime = options.PlayTime,
            SaveCount = (existingInfo?.SaveCount ?? 0) + 1,
            Format = SaveFormat.Json, // Deltas always use JSON for simplicity
            EntityCount = delta.CreatedEntities.Count + delta.ModifiedEntities.Count,
            CustomMetadata = new Dictionary<string, object>
            {
                ["isDelta"] = true,
                ["baselineSlotName"] = delta.BaselineSlotName,
                ["sequenceNumber"] = delta.SequenceNumber,
                ["changeCount"] = delta.ChangeCount
            },
            AppVersion = options.AppVersion,
            FormatVersion = 1
        };

        // Ensure directory exists
        EnsureSaveDirectoryExists();

        // Write save file
        var filePath = GetSlotFilePath(slotName);
        using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            SaveFileFormat.Write(fileStream, slotInfo, snapshotData, options with { Format = SaveFormat.Json });
        }

        // Return the updated slot info (with sizes filled in)
        return SaveFileFormat.ReadMetadata(File.ReadAllBytes(filePath));
    }

    /// <summary>
    /// Loads a delta snapshot from a slot.
    /// </summary>
    /// <param name="slotName">The name of the save slot to load.</param>
    /// <param name="validateChecksum">Whether to validate the checksum if present.</param>
    /// <returns>The delta snapshot.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the save slot doesn't exist.</exception>
    /// <exception cref="InvalidDataException">Thrown when the save file is corrupted or not a delta.</exception>
    internal DeltaSnapshot LoadDelta(string slotName, bool validateChecksum = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slotName);

        var filePath = GetSlotFilePath(slotName);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Save slot '{slotName}' not found.", filePath);
        }

        // Read save file
        var fileData = File.ReadAllBytes(filePath);
        var (slotInfo, snapshotData) = SaveFileFormat.Read(fileData, validateChecksum);

        // Check if this is a delta
        if (slotInfo.CustomMetadata is null ||
            !slotInfo.CustomMetadata.TryGetValue("isDelta", out var isDeltatObj) ||
            isDeltatObj is not bool isDelta ||
            !isDelta)
        {
            throw new InvalidDataException($"Save slot '{slotName}' is not a delta snapshot.");
        }

        // Deserialize delta
        var json = System.Text.Encoding.UTF8.GetString(snapshotData);
        var delta = System.Text.Json.JsonSerializer.Deserialize(json, SnapshotJsonContext.Default.DeltaSnapshot)
            ?? throw new InvalidDataException("Failed to deserialize delta snapshot.");

        return delta;
    }

    #endregion

    #region Async Operations

    /// <summary>
    /// Saves the current world state to a slot asynchronously.
    /// </summary>
    /// <typeparam name="TSerializer">The serializer type that implements both interfaces.</typeparam>
    /// <param name="slotName">The name of the save slot.</param>
    /// <param name="serializer">The component serializer for AOT-compatible serialization.</param>
    /// <param name="options">Optional save options. Uses default if not specified.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The save slot info with updated metadata.</returns>
    internal async Task<SaveSlotInfo> SaveAsync<TSerializer>(
        string slotName,
        TSerializer serializer,
        SaveSlotOptions? options = null,
        CancellationToken cancellationToken = default)
        where TSerializer : IComponentSerializer, IBinaryComponentSerializer
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slotName);
        ArgumentNullException.ThrowIfNull(serializer);

        options ??= SaveSlotOptions.Default;

        // Create snapshot (synchronous - CPU bound)
        var snapshot = SnapshotManager.CreateSnapshot(world, serializer, options.CustomMetadata);

        // Serialize to binary or JSON (synchronous - CPU bound)
        byte[] snapshotData;
        if (options.Format == SaveFormat.Binary)
        {
            snapshotData = SnapshotManager.ToBinary(snapshot, serializer);
        }
        else
        {
            var json = SnapshotManager.ToJson(snapshot);
            snapshotData = System.Text.Encoding.UTF8.GetBytes(json);
        }

        // Check if slot exists (for save count and created timestamp)
        var existingInfo = await TryGetSlotInfoAsync(slotName, cancellationToken).ConfigureAwait(false);
        var now = DateTimeOffset.UtcNow;

        // Create slot info
        var slotInfo = new SaveSlotInfo
        {
            SlotName = slotName,
            DisplayName = options.DisplayName,
            CreatedAt = existingInfo?.CreatedAt ?? now,
            ModifiedAt = now,
            PlayTime = options.PlayTime,
            SaveCount = (existingInfo?.SaveCount ?? 0) + 1,
            Format = options.Format,
            EntityCount = snapshot.Entities.Count,
            ThumbnailBase64 = options.ThumbnailData is not null
                ? Convert.ToBase64String(options.ThumbnailData)
                : null,
            ThumbnailMimeType = options.ThumbnailMimeType,
            CustomMetadata = options.CustomMetadata,
            AppVersion = options.AppVersion,
            FormatVersion = 1
        };

        // Ensure directory exists
        EnsureSaveDirectoryExists();

        // Write save file asynchronously
        var filePath = GetSlotFilePath(slotName);
        var saveData = SaveFileFormat.Write(slotInfo, snapshotData, options);
        await File.WriteAllBytesAsync(filePath, saveData, cancellationToken).ConfigureAwait(false);

        // Return the updated slot info (with sizes filled in)
        var resultData = await File.ReadAllBytesAsync(filePath, cancellationToken).ConfigureAwait(false);
        return SaveFileFormat.ReadMetadata(resultData);
    }

    /// <summary>
    /// Loads a world state from a save slot asynchronously.
    /// </summary>
    /// <typeparam name="TSerializer">The serializer type that implements both interfaces.</typeparam>
    /// <param name="slotName">The name of the save slot to load.</param>
    /// <param name="serializer">The component serializer for AOT-compatible deserialization.</param>
    /// <param name="validateChecksum">Whether to validate the checksum if present.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>
    /// A tuple containing the save slot info and a mapping from original entity IDs to new entities.
    /// </returns>
    /// <exception cref="FileNotFoundException">Thrown when the save slot doesn't exist.</exception>
    /// <exception cref="InvalidDataException">Thrown when the save file is corrupted.</exception>
    internal async Task<(SaveSlotInfo SlotInfo, Dictionary<int, Entity> EntityMap)> LoadAsync<TSerializer>(
        string slotName,
        TSerializer serializer,
        bool validateChecksum = true,
        CancellationToken cancellationToken = default)
        where TSerializer : IComponentSerializer, IBinaryComponentSerializer
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slotName);
        ArgumentNullException.ThrowIfNull(serializer);

        var filePath = GetSlotFilePath(slotName);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Save slot '{slotName}' not found.", filePath);
        }

        // Read save file asynchronously
        var fileData = await File.ReadAllBytesAsync(filePath, cancellationToken).ConfigureAwait(false);
        var (slotInfo, snapshotData) = SaveFileFormat.Read(fileData, validateChecksum);

        // Deserialize snapshot (synchronous - CPU bound)
        WorldSnapshot snapshot;
        if (slotInfo.Format == SaveFormat.Binary)
        {
            snapshot = SnapshotManager.FromBinary(snapshotData, serializer);
        }
        else
        {
            var json = System.Text.Encoding.UTF8.GetString(snapshotData);
            snapshot = SnapshotManager.FromJson(json)
                ?? throw new InvalidDataException("Failed to deserialize JSON snapshot.");
        }

        // Restore snapshot (synchronous - CPU bound)
        var entityMap = SnapshotManager.RestoreSnapshot(world, snapshot, serializer);

        return (slotInfo, entityMap);
    }

    /// <summary>
    /// Gets information about a save slot without loading it asynchronously.
    /// </summary>
    /// <param name="slotName">The name of the save slot.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The save slot info, or null if the slot doesn't exist.</returns>
    internal async Task<SaveSlotInfo?> TryGetSlotInfoAsync(string slotName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slotName);

        var filePath = GetSlotFilePath(slotName);
        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            var fileData = await File.ReadAllBytesAsync(filePath, cancellationToken).ConfigureAwait(false);
            return SaveFileFormat.ReadMetadata(fileData);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Lists all available save slots asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A list of save slot info for all valid save files.</returns>
    internal async Task<IReadOnlyList<SaveSlotInfo>> ListSlotsAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(saveDirectory))
        {
            return [];
        }

        var result = new List<SaveSlotInfo>();
        var files = Directory.EnumerateFiles(saveDirectory, $"*{SaveFileFormat.Extension}");

        foreach (var filePath in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var fileData = await File.ReadAllBytesAsync(filePath, cancellationToken).ConfigureAwait(false);
                if (SaveFileFormat.IsValidFormat(fileData))
                {
                    var slotInfo = SaveFileFormat.ReadMetadata(fileData);
                    result.Add(slotInfo);
                }
            }
            catch
            {
                // Skip invalid files
            }
        }

        return result;
    }

    /// <summary>
    /// Deletes a save slot asynchronously.
    /// </summary>
    /// <param name="slotName">The name of the save slot to delete.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>True if the slot was deleted; false if it didn't exist.</returns>
    internal Task<bool> DeleteSlotAsync(string slotName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slotName);
        cancellationToken.ThrowIfCancellationRequested();

        var filePath = GetSlotFilePath(slotName);
        if (!File.Exists(filePath))
        {
            return Task.FromResult(false);
        }

        File.Delete(filePath);
        return Task.FromResult(true);
    }

    /// <summary>
    /// Copies a save slot to a new name asynchronously.
    /// </summary>
    /// <param name="sourceSlotName">The source slot name.</param>
    /// <param name="destinationSlotName">The destination slot name.</param>
    /// <param name="overwrite">Whether to overwrite if destination exists.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The save slot info for the copied slot.</returns>
    internal async Task<SaveSlotInfo> CopySlotAsync(
        string sourceSlotName,
        string destinationSlotName,
        bool overwrite = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceSlotName);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationSlotName);

        var sourcePath = GetSlotFilePath(sourceSlotName);
        var destinationPath = GetSlotFilePath(destinationSlotName);

        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException($"Source save slot '{sourceSlotName}' not found.", sourcePath);
        }

        if (!overwrite && File.Exists(destinationPath))
        {
            throw new IOException($"Destination save slot '{destinationSlotName}' already exists.");
        }

        EnsureSaveDirectoryExists();

        // Read source file
        var fileData = await File.ReadAllBytesAsync(sourcePath, cancellationToken).ConfigureAwait(false);
        var (slotInfo, snapshotData) = SaveFileFormat.Read(fileData, validateChecksum: false);

        // Update slot info
        var updatedSlotInfo = slotInfo with
        {
            SlotName = destinationSlotName,
            ModifiedAt = DateTimeOffset.UtcNow
        };

        // Write to destination
        var updatedFileData = SaveFileFormat.Write(updatedSlotInfo, snapshotData, new SaveSlotOptions
        {
            Format = slotInfo.Format,
            Compression = slotInfo.Compression,
            IncludeChecksum = slotInfo.Checksum is not null
        });

        await File.WriteAllBytesAsync(destinationPath, updatedFileData, cancellationToken).ConfigureAwait(false);

        return updatedSlotInfo;
    }

    /// <summary>
    /// Validates a save slot's integrity asynchronously.
    /// </summary>
    /// <param name="slotName">The name of the save slot to validate.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The validation result with any errors.</returns>
    internal async Task<SaveSlotInfo?> ValidateSlotAsync(string slotName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slotName);

        var filePath = GetSlotFilePath(slotName);
        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            var fileData = await File.ReadAllBytesAsync(filePath, cancellationToken).ConfigureAwait(false);
            return SaveFileFormat.Validate(fileData);
        }
        catch
        {
            return null;
        }
    }

    #endregion
}
