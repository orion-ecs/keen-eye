using KeenEyes.Persistence.Encryption;
using KeenEyes.Serialization;

namespace KeenEyes.Persistence;

/// <summary>
/// Provides encrypted save/load operations for the world.
/// </summary>
/// <remarks>
/// <para>
/// This API wraps the World's built-in save functionality and adds an encryption
/// layer on top. The encryption is applied after compression but before writing
/// to disk, so the encrypted file cannot be read without the correct password.
/// </para>
/// <para>
/// Access this API through the world extensions:
/// </para>
/// <code>
/// var persistence = world.GetExtension&lt;EncryptedPersistenceApi&gt;();
/// persistence.SaveToSlot("slot1", serializer);
/// </code>
/// </remarks>
public sealed class EncryptedPersistenceApi
{
    private readonly IWorld world;
    private readonly IEncryptionProvider encryptionProvider;
    private readonly string saveDirectory;

    /// <summary>
    /// Creates a new encrypted persistence API.
    /// </summary>
    /// <param name="world">The world to save/load.</param>
    /// <param name="config">The persistence configuration.</param>
    internal EncryptedPersistenceApi(IWorld world, PersistenceConfig config)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(config);

        this.world = world;
        encryptionProvider = config.EncryptionProvider;

        // Use configured directory, or fall back to World's default if available
        if (config.SaveDirectory is not null)
        {
            saveDirectory = config.SaveDirectory;
        }
        else if (world is World concreteWorld)
        {
            saveDirectory = concreteWorld.SaveDirectory;
        }
        else
        {
            throw new ArgumentException(
                "SaveDirectory must be specified in PersistenceConfig when using an IWorld implementation that doesn't provide a default save directory.",
                nameof(config));
        }
    }

    /// <summary>
    /// Gets whether encryption is enabled.
    /// </summary>
    public bool IsEncryptionEnabled => encryptionProvider.IsEncrypted;

    /// <summary>
    /// Gets the name of the encryption provider.
    /// </summary>
    public string EncryptionProviderName => encryptionProvider.Name;

    /// <summary>
    /// Gets the save directory for encrypted saves.
    /// </summary>
    public string SaveDirectory => saveDirectory;

    /// <summary>
    /// Saves the world state to a slot with encryption.
    /// </summary>
    /// <typeparam name="TSerializer">The serializer type.</typeparam>
    /// <param name="slotName">The name of the save slot.</param>
    /// <param name="serializer">The component serializer.</param>
    /// <param name="options">Optional save options.</param>
    /// <returns>The save slot info.</returns>
    public SaveSlotInfo SaveToSlot<TSerializer>(
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

        // Serialize to binary
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

        // Encrypt the snapshot data
        var encryptedData = encryptionProvider.Encrypt(snapshotData);

        // Get existing info for save count and created timestamp
        var existingInfo = GetSlotInfo(slotName);
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

        // Write file using SaveFileFormat (handles compression and checksum)
        var filePath = GetSlotFilePath(slotName);
        EnsureSaveDirectory();

        var fileData = SaveFileFormat.Write(slotInfo, encryptedData, options);
        File.WriteAllBytes(filePath, fileData);

        // Update slot info with sizes
        return slotInfo with
        {
            UncompressedSize = snapshotData.Length,
            CompressedSize = encryptedData.Length,
            Compression = options.Compression
        };
    }

    /// <summary>
    /// Loads a world state from an encrypted slot.
    /// </summary>
    /// <typeparam name="TSerializer">The serializer type.</typeparam>
    /// <param name="slotName">The name of the save slot to load.</param>
    /// <param name="serializer">The component serializer.</param>
    /// <param name="validateChecksum">Whether to validate the checksum.</param>
    /// <returns>The save slot info and entity mapping.</returns>
    public (SaveSlotInfo SlotInfo, Dictionary<int, Entity> EntityMap) LoadFromSlot<TSerializer>(
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

        // Read file
        var fileData = File.ReadAllBytes(filePath);
        var (slotInfo, encryptedData) = SaveFileFormat.Read(fileData, validateChecksum);

        // Decrypt the data
        var snapshotData = encryptionProvider.Decrypt(encryptedData);

        // Deserialize snapshot
        WorldSnapshot? snapshot;
        if (slotInfo.Format == SaveFormat.Binary)
        {
            snapshot = SnapshotManager.FromBinary(snapshotData, serializer);
        }
        else
        {
            var json = System.Text.Encoding.UTF8.GetString(snapshotData);
            snapshot = SnapshotManager.FromJson(json);
        }

        if (snapshot is null)
        {
            throw new InvalidDataException($"Failed to deserialize save slot '{slotName}'.");
        }

        // Restore world state
        var entityMap = SnapshotManager.RestoreSnapshot(world, snapshot, serializer);

        return (slotInfo, entityMap);
    }

    /// <summary>
    /// Saves the world state asynchronously with encryption.
    /// </summary>
    public async Task<SaveSlotInfo> SaveToSlotAsync<TSerializer>(
        string slotName,
        TSerializer serializer,
        SaveSlotOptions? options = null,
        CancellationToken cancellationToken = default)
        where TSerializer : IComponentSerializer, IBinaryComponentSerializer
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slotName);
        ArgumentNullException.ThrowIfNull(serializer);

        options ??= SaveSlotOptions.Default;

        // Create snapshot (sync - CPU bound)
        var snapshot = SnapshotManager.CreateSnapshot(world, serializer, options.CustomMetadata);

        // Serialize to binary
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

        // Encrypt the snapshot data
        var encryptedData = await encryptionProvider.EncryptAsync(snapshotData, cancellationToken)
            .ConfigureAwait(false);

        // Get existing info
        var existingInfo = GetSlotInfo(slotName);
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

        // Write file
        var filePath = GetSlotFilePath(slotName);
        EnsureSaveDirectory();

        var fileData = SaveFileFormat.Write(slotInfo, encryptedData, options);
        await File.WriteAllBytesAsync(filePath, fileData, cancellationToken).ConfigureAwait(false);

        return slotInfo with
        {
            UncompressedSize = snapshotData.Length,
            CompressedSize = encryptedData.Length,
            Compression = options.Compression
        };
    }

    /// <summary>
    /// Loads a world state asynchronously from an encrypted slot.
    /// </summary>
    public async Task<(SaveSlotInfo SlotInfo, Dictionary<int, Entity> EntityMap)> LoadFromSlotAsync<TSerializer>(
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

        // Read file
        var fileData = await File.ReadAllBytesAsync(filePath, cancellationToken).ConfigureAwait(false);
        var (slotInfo, encryptedData) = SaveFileFormat.Read(fileData, validateChecksum);

        // Decrypt the data
        var snapshotData = await encryptionProvider.DecryptAsync(encryptedData, cancellationToken)
            .ConfigureAwait(false);

        // Deserialize snapshot (sync - CPU bound)
        WorldSnapshot? snapshot;
        if (slotInfo.Format == SaveFormat.Binary)
        {
            snapshot = SnapshotManager.FromBinary(snapshotData, serializer);
        }
        else
        {
            var json = System.Text.Encoding.UTF8.GetString(snapshotData);
            snapshot = SnapshotManager.FromJson(json);
        }

        if (snapshot is null)
        {
            throw new InvalidDataException($"Failed to deserialize save slot '{slotName}'.");
        }

        // Restore world state
        var entityMap = SnapshotManager.RestoreSnapshot(world, snapshot, serializer);

        return (slotInfo, entityMap);
    }

    /// <summary>
    /// Gets information about an encrypted save slot.
    /// </summary>
    /// <param name="slotName">The slot name.</param>
    /// <returns>The slot info, or null if it doesn't exist.</returns>
    public SaveSlotInfo? GetSlotInfo(string slotName)
    {
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
    /// Lists all encrypted save slots.
    /// </summary>
    /// <returns>An enumerable of save slot info.</returns>
    public IEnumerable<SaveSlotInfo> ListSlots()
    {
        if (!Directory.Exists(saveDirectory))
        {
            yield break;
        }

        var files = Directory.GetFiles(saveDirectory, "*.ksave");
        foreach (var file in files)
        {
            SaveSlotInfo? info = null;
            try
            {
                var fileData = File.ReadAllBytes(file);
                info = SaveFileFormat.ReadMetadata(fileData);
            }
            catch
            {
                // Skip invalid files
            }

            if (info is not null)
            {
                yield return info;
            }
        }
    }

    /// <summary>
    /// Checks if an encrypted save slot exists.
    /// </summary>
    /// <param name="slotName">The slot name.</param>
    /// <returns>True if the slot exists.</returns>
    public bool SlotExists(string slotName)
    {
        return File.Exists(GetSlotFilePath(slotName));
    }

    /// <summary>
    /// Deletes an encrypted save slot.
    /// </summary>
    /// <param name="slotName">The slot name.</param>
    /// <returns>True if the slot was deleted.</returns>
    public bool DeleteSlot(string slotName)
    {
        var filePath = GetSlotFilePath(slotName);
        if (!File.Exists(filePath))
        {
            return false;
        }

        File.Delete(filePath);
        return true;
    }

    /// <summary>
    /// Gets the file path for a slot.
    /// </summary>
    /// <param name="slotName">The slot name.</param>
    /// <returns>The full file path.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the slot name contains path separators or other unsafe characters.
    /// </exception>
    public string GetSlotFilePath(string slotName)
    {
        SlotNameValidator.Validate(slotName);
        return Path.Combine(saveDirectory, $"{slotName}.ksave");
    }

    private void EnsureSaveDirectory()
    {
        if (!Directory.Exists(saveDirectory))
        {
            Directory.CreateDirectory(saveDirectory);
        }
    }
}
