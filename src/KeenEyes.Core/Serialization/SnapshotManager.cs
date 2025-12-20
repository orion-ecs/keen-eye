using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using KeenEyes.Capabilities;

namespace KeenEyes.Serialization;

/// <summary>
/// Provides functionality for creating, serializing, and restoring world snapshots.
/// </summary>
/// <remarks>
/// <para>
/// The SnapshotManager is the central handler for world persistence. It captures
/// the complete state of a world including all entities, components, hierarchy
/// relationships, and singletons.
/// </para>
/// <para>
/// Snapshots can be serialized to JSON format for storage and later restored.
/// The serialization uses <see cref="System.Text.Json"/> for efficient processing.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create a snapshot of the current world state
/// var snapshot = SnapshotManager.CreateSnapshot(world);
///
/// // Serialize to JSON
/// var json = SnapshotManager.ToJson(snapshot);
///
/// // Save to file
/// File.WriteAllText("save.json", json);
///
/// // Later, load and restore
/// var loadedJson = File.ReadAllText("save.json");
/// var loadedSnapshot = SnapshotManager.FromJson(loadedJson);
/// SnapshotManager.RestoreSnapshot(world, loadedSnapshot, typeResolver);
/// </code>
/// </example>
public static class SnapshotManager
{
    /// <summary>
    /// Creates a snapshot of the current world state using AOT-compatible serialization.
    /// </summary>
    /// <param name="world">The world to capture.</param>
    /// <param name="serializer">
    /// Component serializer for AOT-compatible serialization. Pass an instance of
    /// the generated <c>ComponentSerializationRegistry</c> which implements this interface
    /// for components marked with <c>[Component(Serializable = true)]</c>.
    /// </param>
    /// <param name="metadata">Optional metadata to include in the snapshot.</param>
    /// <returns>A snapshot containing all entities, components, hierarchy, and singletons.</returns>
    /// <remarks>
    /// <para>
    /// The snapshot captures:
    /// <list type="bullet">
    /// <item><description>All entities and their IDs</description></item>
    /// <item><description>All components attached to each entity</description></item>
    /// <item><description>Entity names (if assigned)</description></item>
    /// <item><description>Parent-child hierarchy relationships</description></item>
    /// <item><description>All world singletons</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Component and singleton data is pre-serialized to JSON using the provided serializer
    /// for Native AOT compatibility. This eliminates the need for reflection during JSON serialization.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="world"/> or <paramref name="serializer"/> is null.
    /// </exception>
    public static WorldSnapshot CreateSnapshot(
        World world,
        IComponentSerializer serializer,
        IReadOnlyDictionary<string, object>? metadata = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(serializer);

        var entities = new List<SerializedEntity>();

        // Collect all entities with their components and hierarchy info
        foreach (var entity in world.GetAllEntities())
        {
            var components = new List<SerializedComponent>();

            foreach (var (type, value) in world.GetComponents(entity))
            {
                var info = world.Components.Get(type);
                var typeName = type.AssemblyQualifiedName ?? type.FullName ?? type.Name;
                var isTag = info?.IsTag ?? false;

                // Serialize component data using IComponentSerializer for AOT compatibility
                var jsonData = isTag ? null : serializer.Serialize(type, value);

                components.Add(new SerializedComponent
                {
                    TypeName = typeName,
                    Data = jsonData,
                    IsTag = isTag
                });
            }

            var parent = world.GetParent(entity);

            entities.Add(new SerializedEntity
            {
                Id = entity.Id,
                Name = world.GetName(entity),
                Components = components,
                ParentId = parent.IsValid ? parent.Id : null
            });
        }

        // Collect singletons
        var singletons = new List<SerializedSingleton>();
        foreach (var (type, value) in world.GetAllSingletons())
        {
            var typeName = type.AssemblyQualifiedName ?? type.FullName ?? type.Name;

            // Serialize singleton data using IComponentSerializer for AOT compatibility
            var jsonData = serializer.Serialize(type, value)
                ?? throw new InvalidOperationException(
                    $"Failed to serialize singleton of type '{typeName}'. " +
                    $"Ensure the type is marked with [Component(Serializable = true)].");

            singletons.Add(new SerializedSingleton
            {
                TypeName = typeName,
                Data = jsonData
            });
        }

        return new WorldSnapshot
        {
            Timestamp = DateTimeOffset.UtcNow,
            Entities = entities,
            Singletons = singletons,
            Metadata = metadata
        };
    }

    /// <summary>
    /// Restores a world from a snapshot using AOT-compatible deserialization.
    /// </summary>
    /// <param name="world">The world to restore into. Will be cleared before restoration.</param>
    /// <param name="snapshot">The snapshot to restore from.</param>
    /// <param name="serializer">
    /// Component serializer for AOT-compatible deserialization. Pass an instance of
    /// the generated <c>ComponentSerializationRegistry</c> which implements this interface
    /// for components marked with <c>[Component(Serializable = true)]</c>.
    /// </param>
    /// <returns>
    /// A dictionary mapping original entity IDs from the snapshot to newly created entities.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method first clears the world using <see cref="World.Clear"/>, then
    /// recreates all entities with their components. Entity IDs in the restored
    /// world may differ from the original IDs in the snapshot.
    /// </para>
    /// <para>
    /// Hierarchy relationships are reconstructed after all entities are created.
    /// </para>
    /// <para>
    /// The source generator creates <c>ComponentSerializationRegistry</c> which implements
    /// <see cref="IComponentSerializer"/> for components marked with <c>[Component(Serializable = true)]</c>.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="world"/>, <paramref name="snapshot"/>, or <paramref name="serializer"/> is null.
    /// </exception>
    public static Dictionary<int, Entity> RestoreSnapshot(
        World world,
        WorldSnapshot snapshot,
        IComponentSerializer serializer)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(serializer);

        // Use serializer's type resolver
        Func<string, Type?> resolveType = typeName =>
        {
            return serializer.GetType(typeName);
        };

        // Clear the world before restoration
        world.Clear();

        // Map from snapshot entity ID to new entity
        var entityMap = new Dictionary<int, Entity>();

        // First pass: Create all entities with their components
        foreach (var serializedEntity in snapshot.Entities)
        {
            var builder = world.Spawn(serializedEntity.Name);

            foreach (var component in serializedEntity.Components)
            {
                var type = resolveType(component.TypeName);
                if (type is null)
                {
                    // Type not found - skip this component
                    continue;
                }

                // Ensure type is registered
                var info = world.Components.Get(type)
                    ?? RegisterComponent(world, type, component.TypeName, component.IsTag, serializer);

                // Convert the data to the correct type if needed
                var value = ConvertComponentData(component.Data, type, serializer);
                if (value is not null)
                {
                    builder.WithBoxed(info, value);
                }
            }

            var entity = builder.Build();
            entityMap[serializedEntity.Id] = entity;
        }

        // Second pass: Restore hierarchy relationships
        foreach (var serializedEntity in snapshot.Entities.Where(e => e.ParentId.HasValue))
        {
            if (entityMap.TryGetValue(serializedEntity.Id, out var child) &&
                entityMap.TryGetValue(serializedEntity.ParentId!.Value, out var parent))
            {
                world.SetParent(child, parent);
            }
        }

        // Restore singletons
        foreach (var singleton in snapshot.Singletons)
        {
            var type = resolveType(singleton.TypeName);
            if (type is null)
            {
                continue;
            }

            // Deserialize singleton data from JSON
            var typeName = type.AssemblyQualifiedName ?? type.FullName ?? type.Name;
            var value = serializer.Deserialize(typeName, singleton.Data)
                ?? (type.FullName is not null ? serializer.Deserialize(type.FullName, singleton.Data) : null);

            if (value is not null)
            {
                SetSingleton(world, type, singleton.TypeName, value, serializer);
            }
        }

        return entityMap;
    }

    /// <summary>
    /// Serializes a snapshot to JSON format using AOT-compatible source generation.
    /// </summary>
    /// <param name="snapshot">The snapshot to serialize.</param>
    /// <returns>A JSON string representing the snapshot.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="snapshot"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// This method serializes the WorldSnapshot envelope (metadata, entity list, etc.), not component data.
    /// Component data serialization is handled by IComponentSerializer for AOT compatibility.
    /// </para>
    /// <para>
    /// Uses source-generated JSON serialization which is fully Native AOT compatible.
    /// The serialization uses camelCase naming, includes fields, and omits null values.
    /// </para>
    /// </remarks>
    public static string ToJson(WorldSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return JsonSerializer.Serialize(snapshot, SnapshotJsonContext.Default.WorldSnapshot);
    }

    /// <summary>
    /// Deserializes a snapshot from JSON format using AOT-compatible source generation.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized snapshot.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is invalid.</exception>
    /// <remarks>
    /// <para>
    /// This method deserializes the WorldSnapshot envelope (metadata, entity list, etc.), not component data.
    /// Component data deserialization is handled by IComponentSerializer for AOT compatibility.
    /// </para>
    /// <para>
    /// Uses source-generated JSON serialization which is fully Native AOT compatible.
    /// The deserialization expects camelCase naming and supports fields.
    /// </para>
    /// </remarks>
    public static WorldSnapshot? FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        return JsonSerializer.Deserialize(json, SnapshotJsonContext.Default.WorldSnapshot);
    }

    /// <summary>
    /// Binary format magic bytes identifying KeenEyes snapshot files.
    /// </summary>
    private static ReadOnlySpan<byte> BinaryMagic => "KEEN"u8;

    /// <summary>
    /// Current binary format version.
    /// </summary>
    private const ushort BinaryFormatVersion = 1;

    /// <summary>
    /// Binary format flags.
    /// </summary>
    [Flags]
    private enum BinaryFlags : ushort
    {
        None = 0,
        HasMetadata = 1 << 0,
        HasStringTable = 1 << 1,
        // Reserved for future: Compressed = 1 << 2,
    }

    /// <summary>
    /// Serializes a snapshot to binary format using AOT-compatible serialization.
    /// </summary>
    /// <param name="snapshot">The snapshot to serialize.</param>
    /// <param name="serializer">
    /// Component serializer that implements both <see cref="IComponentSerializer"/> and
    /// <see cref="IBinaryComponentSerializer"/>. Pass an instance of the generated
    /// <c>ComponentSerializer</c> which implements both interfaces for components
    /// marked with <c>[Component(Serializable = true)]</c>.
    /// </param>
    /// <returns>A byte array containing the serialized snapshot.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="snapshot"/> or <paramref name="serializer"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Binary format provides significant benefits over JSON:
    /// <list type="bullet">
    /// <item><description>Smaller file sizes (typically 50-80% reduction)</description></item>
    /// <item><description>Faster serialization/deserialization</description></item>
    /// <item><description>No string parsing overhead</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// This method uses native binary serialization for component data, avoiding the
    /// overhead of JSON string parsing. The serializer must implement both
    /// <see cref="IComponentSerializer"/> (to deserialize JsonElement data) and
    /// <see cref="IBinaryComponentSerializer"/> (to write native binary).
    /// </para>
    /// <para>
    /// The binary format is versioned to support future evolution while maintaining
    /// backwards compatibility.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var snapshot = SnapshotManager.CreateSnapshot(world, serializer);
    /// var binary = SnapshotManager.ToBinary(snapshot, serializer);
    /// File.WriteAllBytes("save.bin", binary);
    /// </code>
    /// </example>
    public static byte[] ToBinary<TSerializer>(WorldSnapshot snapshot, TSerializer serializer)
        where TSerializer : IComponentSerializer, IBinaryComponentSerializer
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(serializer);

        using var stream = new MemoryStream();
        ToBinaryStream(snapshot, serializer, stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Serializes a snapshot to a stream in binary format using native binary serialization.
    /// </summary>
    /// <param name="snapshot">The snapshot to serialize.</param>
    /// <param name="serializer">
    /// Component serializer that implements both <see cref="IComponentSerializer"/> and
    /// <see cref="IBinaryComponentSerializer"/>.
    /// </param>
    /// <param name="stream">The stream to write to.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Use this overload for streaming scenarios or when writing directly to files
    /// to avoid intermediate byte array allocations.
    /// </para>
    /// <para>
    /// This method uses native binary serialization for component data. The JsonElement
    /// data in the snapshot is first deserialized using <see cref="IComponentSerializer"/>,
    /// then serialized to binary using <see cref="IBinaryComponentSerializer"/>.
    /// </para>
    /// </remarks>
    public static void ToBinaryStream<TSerializer>(WorldSnapshot snapshot, TSerializer serializer, Stream stream)
        where TSerializer : IComponentSerializer, IBinaryComponentSerializer
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(serializer);
        ArgumentNullException.ThrowIfNull(stream);

        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

        // Build string table for type names (saves space when many entities share types)
        var stringTable = new List<string>();
        var stringIndex = new Dictionary<string, ushort>();

        ushort GetOrAddString(string s)
        {
            if (stringIndex.TryGetValue(s, out var idx))
            {
                return idx;
            }

            idx = (ushort)stringTable.Count;
            stringTable.Add(s);
            stringIndex[s] = idx;
            return idx;
        }

        // Pre-populate string table with all type names
        foreach (var entity in snapshot.Entities)
        {
            foreach (var component in entity.Components)
            {
                GetOrAddString(component.TypeName);
            }
        }
        foreach (var singleton in snapshot.Singletons)
        {
            GetOrAddString(singleton.TypeName);
        }

        // Write header
        writer.Write(BinaryMagic);
        writer.Write(BinaryFormatVersion);

        var flags = BinaryFlags.HasStringTable; // Always use string table in v1
        if (snapshot.Metadata is not null && snapshot.Metadata.Count > 0)
        {
            flags |= BinaryFlags.HasMetadata;
        }
        writer.Write((ushort)flags);

        writer.Write(snapshot.Entities.Count);
        writer.Write(snapshot.Singletons.Count);

        // Write timestamp as Unix milliseconds
        writer.Write(snapshot.Timestamp.ToUnixTimeMilliseconds());

        // Write snapshot version
        writer.Write(snapshot.Version);

        // Write string table
        writer.Write((ushort)stringTable.Count);
        foreach (var s in stringTable)
        {
            writer.Write(s);
        }

        // Write entities
        foreach (var entity in snapshot.Entities)
        {
            writer.Write(entity.Id);
            writer.Write(entity.ParentId ?? -1);

            // Write name (empty string if null)
            writer.Write(entity.Name ?? string.Empty);

            // Write components
            writer.Write((ushort)entity.Components.Count);
            foreach (var component in entity.Components)
            {
                // Write type name as string table index
                writer.Write(stringIndex[component.TypeName]);
                writer.Write(component.IsTag);

                if (component.IsTag)
                {
                    continue; // Tags have no data
                }

                // Serialize component data to native binary
                if (component.Data.HasValue)
                {
                    // Deserialize from JsonElement to get the component value
                    var componentValue = serializer.Deserialize(component.TypeName, component.Data.Value);
                    if (componentValue is not null)
                    {
                        var type = serializer.GetType(component.TypeName);
                        if (type is not null)
                        {
                            // Use a temporary stream to capture the binary data
                            using var tempStream = new MemoryStream();
                            using var tempWriter = new BinaryWriter(tempStream, Encoding.UTF8, leaveOpen: true);

                            if (serializer.WriteTo(type, componentValue, tempWriter))
                            {
                                // Write the length-prefixed binary data
                                var binaryData = tempStream.ToArray();
                                writer.Write(binaryData.Length);
                                writer.Write(binaryData);
                                continue;
                            }
                        }
                    }

                    // Fallback: serialize as JSON bytes if native binary fails
                    var jsonBytes = Encoding.UTF8.GetBytes(component.Data.Value.GetRawText());
                    writer.Write(-jsonBytes.Length); // Negative length indicates JSON fallback
                    writer.Write(jsonBytes);
                }
                else
                {
                    writer.Write(0);
                }
            }
        }

        // Write singletons
        foreach (var singleton in snapshot.Singletons)
        {
            // Write type name as string table index
            writer.Write(stringIndex[singleton.TypeName]);

            // Deserialize from JsonElement to get the singleton value
            var singletonValue = serializer.Deserialize(singleton.TypeName, singleton.Data);
            if (singletonValue is not null)
            {
                var type = serializer.GetType(singleton.TypeName);
                if (type is not null)
                {
                    // Use a temporary stream to capture the binary data
                    using var tempStream = new MemoryStream();
                    using var tempWriter = new BinaryWriter(tempStream, Encoding.UTF8, leaveOpen: true);

                    if (serializer.WriteTo(type, singletonValue, tempWriter))
                    {
                        // Write the length-prefixed binary data
                        var binaryData = tempStream.ToArray();
                        writer.Write(binaryData.Length);
                        writer.Write(binaryData);
                        continue;
                    }
                }
            }

            // Fallback: serialize as JSON bytes if native binary fails
            var jsonBytes = Encoding.UTF8.GetBytes(singleton.Data.GetRawText());
            writer.Write(-jsonBytes.Length); // Negative length indicates JSON fallback
            writer.Write(jsonBytes);
        }

        // Write metadata as JSON if present
        if ((flags & BinaryFlags.HasMetadata) != 0)
        {
            var metadataJson = JsonSerializer.Serialize(
                snapshot.Metadata,
                SnapshotJsonContext.Default.IReadOnlyDictionaryStringObject);
            writer.Write(metadataJson);
        }
    }

    /// <summary>
    /// Deserializes a snapshot from binary format.
    /// </summary>
    /// <param name="data">The binary data to deserialize.</param>
    /// <param name="serializer">
    /// Component serializer that implements both <see cref="IComponentSerializer"/> and
    /// <see cref="IBinaryComponentSerializer"/>.
    /// </param>
    /// <returns>The deserialized snapshot, or null if the data is invalid.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="data"/> or <paramref name="serializer"/> is null.
    /// </exception>
    /// <exception cref="InvalidDataException">
    /// Thrown when the binary data is invalid or corrupted.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method validates the binary header before attempting deserialization.
    /// If the magic bytes or version are invalid, an exception is thrown.
    /// </para>
    /// <para>
    /// Component data is read using native binary deserialization via
    /// <see cref="IBinaryComponentSerializer.ReadFrom"/>, then converted to JsonElement
    /// using <see cref="IComponentSerializer.Serialize"/> for storage in the snapshot.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var binary = File.ReadAllBytes("save.bin");
    /// var snapshot = SnapshotManager.FromBinary(binary, serializer);
    /// SnapshotManager.RestoreSnapshot(world, snapshot, componentSerializer);
    /// </code>
    /// </example>
    public static WorldSnapshot FromBinary<TSerializer>(byte[] data, TSerializer serializer)
        where TSerializer : IComponentSerializer, IBinaryComponentSerializer
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(serializer);

        using var stream = new MemoryStream(data);
        return FromBinaryStream(stream, serializer);
    }

    /// <summary>
    /// Deserializes a snapshot from a stream in binary format using native binary deserialization.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="serializer">
    /// Component serializer that implements both <see cref="IComponentSerializer"/> and
    /// <see cref="IBinaryComponentSerializer"/>.
    /// </param>
    /// <returns>The deserialized snapshot.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is null.
    /// </exception>
    /// <exception cref="InvalidDataException">
    /// Thrown when the binary data is invalid or corrupted.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Use this overload for streaming scenarios or when reading directly from files
    /// to avoid intermediate byte array allocations.
    /// </para>
    /// <para>
    /// Component data is read using native binary deserialization via
    /// <see cref="IBinaryComponentSerializer.ReadFrom"/>, then converted to JsonElement
    /// using <see cref="IComponentSerializer.Serialize"/> for storage in the snapshot.
    /// </para>
    /// </remarks>
    public static WorldSnapshot FromBinaryStream<TSerializer>(Stream stream, TSerializer serializer)
        where TSerializer : IComponentSerializer, IBinaryComponentSerializer
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(serializer);

        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

        // Read and validate header
        Span<byte> magic = stackalloc byte[4];
        if (reader.Read(magic) != 4 || !magic.SequenceEqual(BinaryMagic))
        {
            throw new InvalidDataException("Invalid binary snapshot: missing or incorrect magic bytes.");
        }

        var version = reader.ReadUInt16();
        if (version > BinaryFormatVersion)
        {
            throw new InvalidDataException(
                $"Binary snapshot version {version} is not supported. Maximum supported version is {BinaryFormatVersion}.");
        }

        var flags = (BinaryFlags)reader.ReadUInt16();
        var entityCount = reader.ReadInt32();
        var singletonCount = reader.ReadInt32();

        // Read timestamp
        var unixMs = reader.ReadInt64();
        var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(unixMs);

        // Read snapshot version
        var snapshotVersion = reader.ReadInt32();

        // Read string table if present
        string[] stringTable;
        if ((flags & BinaryFlags.HasStringTable) != 0)
        {
            var stringCount = reader.ReadUInt16();
            stringTable = new string[stringCount];
            for (var i = 0; i < stringCount; i++)
            {
                stringTable[i] = reader.ReadString();
            }
        }
        else
        {
            stringTable = [];
        }

        var hasStringTable = (flags & BinaryFlags.HasStringTable) != 0;

        // Read entities
        var entities = new List<SerializedEntity>(entityCount);
        for (var i = 0; i < entityCount; i++)
        {
            var id = reader.ReadInt32();
            var parentId = reader.ReadInt32();
            var name = reader.ReadString();
            if (string.IsNullOrEmpty(name))
            {
                name = null;
            }

            var componentCount = reader.ReadUInt16();
            var components = new List<SerializedComponent>(componentCount);

            for (var j = 0; j < componentCount; j++)
            {
                // Read type name (from string table if enabled, otherwise inline)
                string typeName;
                if (hasStringTable)
                {
                    var typeIndex = reader.ReadUInt16();
                    typeName = stringTable[typeIndex];
                }
                else
                {
                    typeName = reader.ReadString();
                }

                var isTag = reader.ReadBoolean();

                JsonElement? data = null;
                if (!isTag)
                {
                    var dataLength = reader.ReadInt32();
                    if (dataLength != 0)
                    {
                        // Negative length indicates JSON fallback
                        if (dataLength < 0)
                        {
                            var jsonBytes = reader.ReadBytes(-dataLength);
                            var jsonString = Encoding.UTF8.GetString(jsonBytes);
                            data = JsonDocument.Parse(jsonString).RootElement.Clone();
                        }
                        else
                        {
                            // Read native binary data
                            var binaryData = reader.ReadBytes(dataLength);
                            using var tempStream = new MemoryStream(binaryData);
                            using var tempReader = new BinaryReader(tempStream, Encoding.UTF8, leaveOpen: true);

                            var componentValue = serializer.ReadFrom(typeName, tempReader);
                            if (componentValue is not null)
                            {
                                var type = serializer.GetType(typeName);
                                if (type is not null)
                                {
                                    // Convert to JsonElement for storage in snapshot
                                    data = serializer.Serialize(type, componentValue);
                                }
                            }
                        }
                    }
                }

                components.Add(new SerializedComponent
                {
                    TypeName = typeName,
                    IsTag = isTag,
                    Data = data
                });
            }

            entities.Add(new SerializedEntity
            {
                Id = id,
                ParentId = parentId == -1 ? null : parentId,
                Name = name,
                Components = components
            });
        }

        // Read singletons
        var singletons = new List<SerializedSingleton>(singletonCount);
        for (var i = 0; i < singletonCount; i++)
        {
            // Read type name (from string table if enabled, otherwise inline)
            string typeName;
            if (hasStringTable)
            {
                var typeIndex = reader.ReadUInt16();
                typeName = stringTable[typeIndex];
            }
            else
            {
                typeName = reader.ReadString();
            }

            var dataLength = reader.ReadInt32();
            JsonElement data;

            // Negative length indicates JSON fallback
            if (dataLength < 0)
            {
                var jsonBytes = reader.ReadBytes(-dataLength);
                var jsonString = Encoding.UTF8.GetString(jsonBytes);
                data = JsonDocument.Parse(jsonString).RootElement.Clone();
            }
            else
            {
                // Read native binary data
                var binaryData = reader.ReadBytes(dataLength);
                using var tempStream = new MemoryStream(binaryData);
                using var tempReader = new BinaryReader(tempStream, Encoding.UTF8, leaveOpen: true);

                var singletonValue = serializer.ReadFrom(typeName, tempReader);
                if (singletonValue is not null)
                {
                    var type = serializer.GetType(typeName);
                    if (type is not null)
                    {
                        // Convert to JsonElement for storage in snapshot
                        var jsonElement = serializer.Serialize(type, singletonValue);
                        data = jsonElement ?? throw new InvalidDataException(
                            $"Failed to serialize singleton '{typeName}' to JSON.");
                    }
                    else
                    {
                        throw new InvalidDataException($"Unknown singleton type: {typeName}");
                    }
                }
                else
                {
                    throw new InvalidDataException($"Failed to deserialize singleton: {typeName}");
                }
            }

            singletons.Add(new SerializedSingleton
            {
                TypeName = typeName,
                Data = data
            });
        }

        // Read metadata if present
        IReadOnlyDictionary<string, object>? metadata = null;
        if ((flags & BinaryFlags.HasMetadata) != 0)
        {
            var metadataJson = reader.ReadString();
            metadata = JsonSerializer.Deserialize(
                metadataJson,
                SnapshotJsonContext.Default.IReadOnlyDictionaryStringObject);
        }

        return new WorldSnapshot
        {
            Version = snapshotVersion,
            Timestamp = timestamp,
            Entities = entities,
            Singletons = singletons,
            Metadata = metadata
        };
    }

    /// <summary>
    /// Registers a component type using the serializer's AOT-compatible method.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the serializer cannot register the component type.
    /// </exception>
    private static ComponentInfo RegisterComponent(ISerializationCapability serialization, Type type, string typeName, bool isTag, IComponentSerializer serializer)
    {
        var info = serializer.RegisterComponent(serialization, typeName, isTag);
        if (info is not null)
        {
            return info;
        }

        // Also try with full name
        if (type.FullName is not null)
        {
            info = serializer.RegisterComponent(serialization, type.FullName, isTag);
            if (info is not null)
            {
                return info;
            }
        }

        throw new InvalidOperationException(
            $"Component type '{typeName}' is not registered in the serializer. " +
            $"Ensure all component types are marked with [Component(Serializable = true)].");
    }

    /// <summary>
    /// Sets a singleton value using the serializer's AOT-compatible method.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the serializer cannot set the singleton value.
    /// </exception>
    private static void SetSingleton(ISerializationCapability serialization, Type type, string typeName, object value, IComponentSerializer serializer)
    {
        if (serializer.SetSingleton(serialization, typeName, value))
        {
            return;
        }

        // Also try with full name
        if (type.FullName is not null && serializer.SetSingleton(serialization, type.FullName, value))
        {
            return;
        }

        throw new InvalidOperationException(
            $"Singleton type '{typeName}' is not registered in the serializer. " +
            $"Ensure all singleton types are marked with [Component(Serializable = true)].");
    }

    /// <summary>
    /// Converts component data from JSON to the target type using AOT-compatible deserialization.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the serializer cannot deserialize the component data.
    /// </exception>
    private static object? ConvertComponentData(JsonElement? data, Type targetType, IComponentSerializer serializer)
    {
        // Null data (tag components)
        if (data is null)
        {
            return null;
        }

        var jsonElement = data.Value;
        var typeName = targetType.AssemblyQualifiedName ?? targetType.FullName ?? targetType.Name;
        var result = serializer.Deserialize(typeName, jsonElement);
        if (result is not null)
        {
            return result;
        }

        // Also try with full name
        if (targetType.FullName is not null)
        {
            result = serializer.Deserialize(targetType.FullName, jsonElement);
            if (result is not null)
            {
                return result;
            }
        }

        throw new InvalidOperationException(
            $"Cannot deserialize component type '{typeName}'. " +
            $"Ensure the type is marked with [Component(Serializable = true)].");
    }
}
