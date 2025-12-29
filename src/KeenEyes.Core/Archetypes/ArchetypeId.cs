using System.Collections.Immutable;

namespace KeenEyes;

/// <summary>
/// Unique identifier for an archetype based on its component type combination.
/// Two archetypes with the same set of component types will have the same ArchetypeId.
/// </summary>
/// <remarks>
/// <para>
/// The ArchetypeId is computed by sorting component types by their full name and
/// combining their hash codes. This ensures a stable, consistent identifier regardless
/// of the order in which components were added.
/// </para>
/// <para>
/// ArchetypeId is immutable and implements value equality semantics.
/// </para>
/// </remarks>
public readonly struct ArchetypeId : IEquatable<ArchetypeId>
{
    private readonly int hashCode;
    private readonly ImmutableArray<Type> componentTypes;

    /// <summary>
    /// Gets the component types that make up this archetype.
    /// </summary>
    public ImmutableArray<Type> ComponentTypes => componentTypes;

    /// <summary>
    /// Gets the number of component types in this archetype.
    /// </summary>
    public int ComponentCount => componentTypes.IsDefault ? 0 : componentTypes.Length;

    /// <summary>
    /// Creates an ArchetypeId from a collection of component types.
    /// </summary>
    /// <param name="types">The component types for this archetype.</param>
    public ArchetypeId(IEnumerable<Type> types)
    {
        // Sort types by full name using ordinal comparison for consistent ordering
        // Must use ordinal to match the binary search in Archetype.Has() which uses CompareOrdinal
        componentTypes = [.. types.OrderBy(t => t.FullName, StringComparer.Ordinal)];

        // Compute hash using HashCode.Combine for stability
        var hash = new HashCode();
        foreach (var type in componentTypes)
        {
            hash.Add(type);
        }
        hashCode = hash.ToHashCode();
    }

    /// <summary>
    /// Creates an ArchetypeId from sorted component types (internal fast path).
    /// </summary>
    internal ArchetypeId(ImmutableArray<Type> sortedTypes, int precomputedHash)
    {
        componentTypes = sortedTypes;
        hashCode = precomputedHash;
    }

    /// <summary>
    /// Checks if this archetype contains a specific component type.
    /// </summary>
    /// <typeparam name="T">The component type to check for.</typeparam>
    /// <returns>True if this archetype contains the component type.</returns>
    public bool Has<T>() where T : struct, IComponent
    {
        return Has(typeof(T));
    }

    /// <summary>
    /// Checks if this archetype contains a specific component type.
    /// </summary>
    /// <param name="type">The component type to check for.</param>
    /// <returns>True if this archetype contains the component type.</returns>
    public bool Has(Type type)
    {
        if (componentTypes.IsDefault)
        {
            return false;
        }

        // Binary search since types are sorted
        var left = 0;
        var right = componentTypes.Length - 1;
        var targetName = type.FullName;

        while (left <= right)
        {
            var mid = left + (right - left) / 2;
            var comparison = string.CompareOrdinal(componentTypes[mid].FullName, targetName);

            if (comparison == 0)
            {
                return true;
            }

            if (comparison < 0)
            {
                left = mid + 1;
            }
            else
            {
                right = mid - 1;
            }
        }

        return false;
    }

    /// <summary>
    /// Creates a new ArchetypeId with an additional component type.
    /// </summary>
    /// <typeparam name="T">The component type to add.</typeparam>
    /// <returns>A new ArchetypeId with the added component type.</returns>
    public ArchetypeId With<T>() where T : struct, IComponent
    {
        return With(typeof(T));
    }

    /// <summary>
    /// Creates a new ArchetypeId with an additional component type.
    /// </summary>
    /// <param name="type">The component type to add.</param>
    /// <returns>A new ArchetypeId with the added component type.</returns>
    public ArchetypeId With(Type type)
    {
        if (Has(type))
        {
            return this;
        }

        var newTypes = componentTypes.IsDefault
            ? [type]
            : componentTypes.Add(type);

        return new ArchetypeId(newTypes);
    }

    /// <summary>
    /// Creates a new ArchetypeId without the specified component type.
    /// </summary>
    /// <typeparam name="T">The component type to remove.</typeparam>
    /// <returns>A new ArchetypeId without the component type.</returns>
    public ArchetypeId Without<T>() where T : struct, IComponent
    {
        return Without(typeof(T));
    }

    /// <summary>
    /// Creates a new ArchetypeId without the specified component type.
    /// </summary>
    /// <param name="type">The component type to remove.</param>
    /// <returns>A new ArchetypeId without the component type.</returns>
    public ArchetypeId Without(Type type)
    {
        if (componentTypes.IsDefault || !Has(type))
        {
            return this;
        }

        var newTypes = componentTypes.Remove(type);
        return new ArchetypeId(newTypes);
    }

    /// <inheritdoc />
    public bool Equals(ArchetypeId other)
    {
        if (hashCode != other.hashCode)
        {
            return false;
        }

        if (componentTypes.IsDefault && other.componentTypes.IsDefault)
        {
            return true;
        }

        if (componentTypes.IsDefault || other.componentTypes.IsDefault)
        {
            return false;
        }

        if (componentTypes.Length != other.componentTypes.Length)
        {
            return false;
        }

        for (var i = 0; i < componentTypes.Length; i++)
        {
            if (componentTypes[i] != other.componentTypes[i])
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is ArchetypeId other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode() => hashCode;

    /// <inheritdoc />
    public override string ToString()
    {
        if (componentTypes.IsDefault || componentTypes.Length == 0)
        {
            return "ArchetypeId()";
        }

        var names = string.Join(", ", componentTypes.Select(t => t.Name));
        return $"ArchetypeId({names})";
    }

    /// <summary>
    /// Equality operator.
    /// </summary>
    public static bool operator ==(ArchetypeId left, ArchetypeId right) => left.Equals(right);

    /// <summary>
    /// Inequality operator.
    /// </summary>
    public static bool operator !=(ArchetypeId left, ArchetypeId right) => !left.Equals(right);
}
