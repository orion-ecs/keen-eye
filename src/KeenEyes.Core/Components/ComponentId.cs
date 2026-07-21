using System.Runtime.CompilerServices;

namespace KeenEyes;

/// <summary>
/// Unique identifier for a component type.
/// </summary>
/// <param name="Value">The underlying integer identifier.</param>
public readonly record struct ComponentId(int Value) : IComparable<ComponentId>
{
    /// <summary>An invalid/unassigned component ID.</summary>
    public static readonly ComponentId None = new(-1);

    /// <summary>Whether this ID is valid.</summary>
    public bool IsValid => Value >= 0;

    /// <inheritdoc />
    public int CompareTo(ComponentId other) => Value.CompareTo(other.Value);

    /// <summary>Determines whether one component ID is less than another.</summary>
    /// <param name="left">The first component ID.</param>
    /// <param name="right">The second component ID.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is less than <paramref name="right"/>.</returns>
    public static bool operator <(ComponentId left, ComponentId right) => left.Value < right.Value;

    /// <summary>Determines whether one component ID is less than or equal to another.</summary>
    /// <param name="left">The first component ID.</param>
    /// <param name="right">The second component ID.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is less than or equal to <paramref name="right"/>.</returns>
    public static bool operator <=(ComponentId left, ComponentId right) => left.Value <= right.Value;

    /// <summary>Determines whether one component ID is greater than another.</summary>
    /// <param name="left">The first component ID.</param>
    /// <param name="right">The second component ID.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is greater than <paramref name="right"/>.</returns>
    public static bool operator >(ComponentId left, ComponentId right) => left.Value > right.Value;

    /// <summary>Determines whether one component ID is greater than or equal to another.</summary>
    /// <param name="left">The first component ID.</param>
    /// <param name="right">The second component ID.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is greater than or equal to <paramref name="right"/>.</returns>
    public static bool operator >=(ComponentId left, ComponentId right) => left.Value >= right.Value;

    /// <inheritdoc />
    public override string ToString() => $"ComponentId({Value})";

    /// <summary>Converts a ComponentId to its underlying integer value.</summary>
    /// <param name="id">The component ID.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator int(ComponentId id) => id.Value;
}
