using System.Runtime.CompilerServices;

namespace KeenEye;

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

    public int CompareTo(ComponentId other) => Value.CompareTo(other.Value);

    public override string ToString() => $"ComponentId({Value})";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator int(ComponentId id) => id.Value;
}
