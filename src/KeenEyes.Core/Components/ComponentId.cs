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

    /// <inheritdoc />
    public override string ToString() => $"ComponentId({Value})";

    /// <summary>Converts a ComponentId to its underlying integer value.</summary>
    /// <param name="id">The component ID.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator int(ComponentId id) => id.Value;
}
