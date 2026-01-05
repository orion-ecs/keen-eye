namespace KeenEyes.Graph.Abstractions;

/// <summary>
/// Identifies the data type of a graph port.
/// </summary>
/// <remarks>
/// Port types determine connection compatibility. Implicit widening conversions
/// are allowed (e.g., float to float2) but narrowing conversions are not.
/// </remarks>
public enum PortTypeId
{
    /// <summary>Accepts any port type.</summary>
    Any = 0,

    /// <summary>Single-precision floating point.</summary>
    Float = 1,

    /// <summary>Two-component float vector.</summary>
    Float2 = 2,

    /// <summary>Three-component float vector.</summary>
    Float3 = 3,

    /// <summary>Four-component float vector.</summary>
    Float4 = 4,

    /// <summary>32-bit signed integer.</summary>
    Int = 10,

    /// <summary>Two-component integer vector.</summary>
    Int2 = 11,

    /// <summary>Three-component integer vector.</summary>
    Int3 = 12,

    /// <summary>Four-component integer vector.</summary>
    Int4 = 13,

    /// <summary>Boolean value.</summary>
    Bool = 20,

    /// <summary>Entity reference.</summary>
    Entity = 30,

    /// <summary>Execution flow (for control flow nodes).</summary>
    Flow = 100
}
