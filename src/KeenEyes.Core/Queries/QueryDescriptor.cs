using System.Collections.Immutable;

namespace KeenEyes;

/// <summary>
/// Immutable descriptor for a query, used as a cache key.
/// Two queries with the same With and Without types produce the same descriptor.
/// </summary>
/// <remarks>
/// <para>
/// QueryDescriptor uses value equality based on the sorted component types
/// in both the With and Without sets. This ensures consistent cache lookup
/// regardless of the order components were specified.
/// </para>
/// <para>
/// Use <see cref="QueryDescriptor.FromDescription(QueryDescription)"/> to
/// create a descriptor from an existing QueryDescription.
/// </para>
/// </remarks>
public readonly struct QueryDescriptor : IEquatable<QueryDescriptor>
{
    private readonly ImmutableArray<Type> with;
    private readonly ImmutableArray<Type> without;
    private readonly int hashCode;

    /// <summary>
    /// Gets the component types that must be present.
    /// </summary>
    public ImmutableArray<Type> With => with;

    /// <summary>
    /// Gets the component types that must NOT be present.
    /// </summary>
    public ImmutableArray<Type> Without => without;

    /// <summary>
    /// Creates a QueryDescriptor with the specified component requirements.
    /// </summary>
    /// <param name="with">Component types that must be present.</param>
    /// <param name="without">Component types that must NOT be present.</param>
    public QueryDescriptor(IEnumerable<Type> with, IEnumerable<Type> without)
    {
        // Sort for consistent equality comparison
        this.with = [.. with.OrderBy(t => t.FullName)];
        this.without = [.. without.OrderBy(t => t.FullName)];

        // Precompute hash
        var hash = new HashCode();
        hash.Add(1); // Discriminator for 'with' types
        foreach (var type in this.with)
        {
            hash.Add(type);
        }
        hash.Add(2); // Discriminator for 'without' types
        foreach (var type in this.without)
        {
            hash.Add(type);
        }
        hashCode = hash.ToHashCode();
    }

    /// <summary>
    /// Creates a QueryDescriptor from a QueryDescription.
    /// </summary>
    /// <param name="description">The query description.</param>
    /// <returns>An immutable descriptor for cache lookup.</returns>
    public static QueryDescriptor FromDescription(QueryDescription description)
    {
        return new QueryDescriptor(description.AllRequired, description.Without);
    }

    /// <summary>
    /// Checks if an archetype matches this query descriptor.
    /// </summary>
    /// <param name="archetype">The archetype to check.</param>
    /// <returns>True if the archetype matches.</returns>
    public bool Matches(Archetype archetype)
    {
        // Check all required components are present
        foreach (var required in with)
        {
            if (!archetype.Has(required))
            {
                return false;
            }
        }

        // Check no excluded components are present
        foreach (var excluded in without)
        {
            if (archetype.Has(excluded))
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public bool Equals(QueryDescriptor other)
    {
        if (hashCode != other.hashCode)
        {
            return false;
        }

        if (with.Length != other.with.Length || without.Length != other.without.Length)
        {
            return false;
        }

        for (var i = 0; i < with.Length; i++)
        {
            if (with[i] != other.with[i])
            {
                return false;
            }
        }

        for (var i = 0; i < without.Length; i++)
        {
            if (without[i] != other.without[i])
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is QueryDescriptor other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode() => hashCode;

    /// <inheritdoc />
    public override string ToString()
    {
        var withNames = with.IsDefault ? "" : string.Join(", ", with.Select(t => t.Name));
        var withoutNames = without.IsDefault ? "" : string.Join(", ", without.Select(t => t.Name));

        if (string.IsNullOrEmpty(withoutNames))
        {
            return $"Query({withNames})";
        }

        return $"Query({withNames}) Without({withoutNames})";
    }

    /// <summary>
    /// Equality operator.
    /// </summary>
    public static bool operator ==(QueryDescriptor left, QueryDescriptor right) => left.Equals(right);

    /// <summary>
    /// Inequality operator.
    /// </summary>
    public static bool operator !=(QueryDescriptor left, QueryDescriptor right) => !left.Equals(right);
}
