using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using KeenEyes.Serialization;

namespace KeenEyes.Replay;

/// <summary>
/// Provides fast checksum calculation for world state validation during replay.
/// </summary>
/// <remarks>
/// <para>
/// WorldChecksum uses FNV-1a hashing algorithm for fast checksum calculation,
/// targeting sub-millisecond performance for typical world states. The checksum
/// captures entity IDs, versions, component data, and singleton state.
/// </para>
/// <para>
/// Checksums are used to detect replay desynchronization by comparing the
/// world state during playback against the recorded checksum. Any difference
/// indicates non-deterministic behavior that may cause replay divergence.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Calculate checksum during recording
/// uint checksum = WorldChecksum.Calculate(world, serializer);
///
/// // Compare during playback
/// uint playbackChecksum = WorldChecksum.Calculate(world, serializer);
/// if (checksum != playbackChecksum)
/// {
///     // Desync detected!
/// }
/// </code>
/// </example>
public static class WorldChecksum
{
    /// <summary>
    /// FNV-1a 32-bit offset basis.
    /// </summary>
    private const uint Fnv32OffsetBasis = 2166136261;

    /// <summary>
    /// FNV-1a 32-bit prime.
    /// </summary>
    private const uint Fnv32Prime = 16777619;

    /// <summary>
    /// Calculates a checksum for the current state of a world.
    /// </summary>
    /// <param name="world">The world to calculate the checksum for.</param>
    /// <param name="serializer">The component serializer for serializing component data.</param>
    /// <returns>A 32-bit unsigned integer checksum representing the world state.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="world"/> or <paramref name="serializer"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The checksum includes:
    /// <list type="bullet">
    /// <item><description>Entity IDs and versions (in deterministic order)</description></item>
    /// <item><description>Component data for each entity</description></item>
    /// <item><description>Singleton data</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Performance target: less than 1ms for typical world states (hundreds of entities).
    /// </para>
    /// </remarks>
    public static uint Calculate(World world, IComponentSerializer serializer)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(serializer);

        var hash = Fnv32OffsetBasis;

        // Hash entities in a deterministic order (by ID)
        var entities = world.GetAllEntities()
            .OrderBy(e => e.Id)
            .ToList();

        // Hash entity count
        hash = HashInt32(hash, entities.Count);

        foreach (var entity in entities)
        {
            // Hash entity ID and version
            hash = HashInt32(hash, entity.Id);
            hash = HashInt32(hash, entity.Version);

            // Hash entity name (if any)
            var name = world.GetName(entity);
            if (name is not null)
            {
                hash = HashString(hash, name);
            }

            // Hash components in a deterministic order (by type name)
            var components = world.GetComponents(entity)
                .OrderBy(c => c.Type.FullName)
                .ToList();

            hash = HashInt32(hash, components.Count);

            foreach (var (type, value) in components)
            {
                // Hash component type name
                var typeName = type.FullName ?? type.Name;
                hash = HashString(hash, typeName);

                // Hash component data using serializer
                var info = world.Components.Get(type);
                var isTag = info?.IsTag ?? false;

                if (!isTag)
                {
                    var jsonData = serializer.Serialize(type, value);
                    if (jsonData.HasValue)
                    {
                        var jsonText = jsonData.Value.GetRawText();
                        hash = HashString(hash, jsonText);
                    }
                }
            }
        }

        // Hash singletons in a deterministic order (by type name)
        var singletons = world.GetAllSingletons()
            .OrderBy(s => s.Type.FullName)
            .ToList();

        hash = HashInt32(hash, singletons.Count);

        foreach (var (type, value) in singletons)
        {
            // Hash singleton type name
            var typeName = type.FullName ?? type.Name;
            hash = HashString(hash, typeName);

            // Hash singleton data using serializer
            var jsonData = serializer.Serialize(type, value);
            if (jsonData.HasValue)
            {
                var jsonText = jsonData.Value.GetRawText();
                hash = HashString(hash, jsonText);
            }
        }

        return hash;
    }

    /// <summary>
    /// Calculates a checksum for a world snapshot.
    /// </summary>
    /// <param name="snapshot">The snapshot to calculate the checksum for.</param>
    /// <returns>A 32-bit unsigned integer checksum representing the snapshot state.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="snapshot"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The checksum includes:
    /// <list type="bullet">
    /// <item><description>Entity IDs (in deterministic order)</description></item>
    /// <item><description>Component data for each entity</description></item>
    /// <item><description>Singleton data</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// This overload is useful for calculating checksums from serialized data
    /// without restoring to a live world.
    /// </para>
    /// </remarks>
    public static uint Calculate(WorldSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var hash = Fnv32OffsetBasis;

        // Hash entities in a deterministic order (by ID)
        var entities = snapshot.Entities
            .OrderBy(e => e.Id)
            .ToList();

        // Hash entity count
        hash = HashInt32(hash, entities.Count);

        foreach (var entity in entities)
        {
            // Hash entity ID
            hash = HashInt32(hash, entity.Id);

            // Hash entity name (if any)
            if (entity.Name is not null)
            {
                hash = HashString(hash, entity.Name);
            }

            // Hash parent ID
            if (entity.ParentId.HasValue)
            {
                hash = HashInt32(hash, entity.ParentId.Value);
            }

            // Hash components in a deterministic order (by type name)
            var components = entity.Components
                .OrderBy(c => c.TypeName)
                .ToList();

            hash = HashInt32(hash, components.Count);

            foreach (var component in components)
            {
                // Hash component type name
                hash = HashString(hash, component.TypeName);

                // Hash component data
                if (!component.IsTag && component.Data.HasValue)
                {
                    var jsonText = component.Data.Value.GetRawText();
                    hash = HashString(hash, jsonText);
                }
            }
        }

        // Hash singletons in a deterministic order (by type name)
        var singletons = snapshot.Singletons
            .OrderBy(s => s.TypeName)
            .ToList();

        hash = HashInt32(hash, singletons.Count);

        foreach (var singleton in singletons)
        {
            // Hash singleton type name
            hash = HashString(hash, singleton.TypeName);

            // Hash singleton data
            var jsonText = singleton.Data.GetRawText();
            hash = HashString(hash, jsonText);
        }

        return hash;
    }

    /// <summary>
    /// Hashes a 32-bit integer into the running hash using FNV-1a.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint HashInt32(uint hash, int value)
    {
        // Hash each byte of the integer
        hash = (hash ^ (byte)(value & 0xFF)) * Fnv32Prime;
        hash = (hash ^ (byte)((value >> 8) & 0xFF)) * Fnv32Prime;
        hash = (hash ^ (byte)((value >> 16) & 0xFF)) * Fnv32Prime;
        hash = (hash ^ (byte)((value >> 24) & 0xFF)) * Fnv32Prime;
        return hash;
    }

    /// <summary>
    /// Hashes a string into the running hash using FNV-1a.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint HashString(uint hash, string value)
    {
        // Use UTF-8 encoding for consistent cross-platform hashing
        var bytes = Encoding.UTF8.GetBytes(value);
        foreach (var b in bytes)
        {
            hash = (hash ^ b) * Fnv32Prime;
        }
        return hash;
    }
}
