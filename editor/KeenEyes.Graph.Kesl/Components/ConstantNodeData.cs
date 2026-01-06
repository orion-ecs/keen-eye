using System.Numerics;

namespace KeenEyes.Graph.Kesl.Components;

/// <summary>
/// Component data for float constant nodes.
/// </summary>
public struct FloatConstantData : IComponent
{
    /// <summary>The constant value.</summary>
    public float Value;

    /// <summary>Creates default data with value 0.</summary>
    public static FloatConstantData Default => new() { Value = 0f };
}

/// <summary>
/// Component data for integer constant nodes.
/// </summary>
public struct IntConstantData : IComponent
{
    /// <summary>The constant value.</summary>
    public int Value;

    /// <summary>Creates default data with value 0.</summary>
    public static IntConstantData Default => new() { Value = 0 };
}

/// <summary>
/// Component data for boolean constant nodes.
/// </summary>
public struct BoolConstantData : IComponent
{
    /// <summary>The constant value.</summary>
    public bool Value;

    /// <summary>Creates default data with value false.</summary>
    public static BoolConstantData Default => new() { Value = false };
}

/// <summary>
/// Component data for Vector2 constant nodes.
/// </summary>
public struct Vector2ConstantData : IComponent
{
    /// <summary>The constant value.</summary>
    public Vector2 Value;

    /// <summary>Creates default data with value (0, 0).</summary>
    public static Vector2ConstantData Default => new() { Value = Vector2.Zero };
}

/// <summary>
/// Component data for Vector3 constant nodes.
/// </summary>
public struct Vector3ConstantData : IComponent
{
    /// <summary>The constant value.</summary>
    public Vector3 Value;

    /// <summary>Creates default data with value (0, 0, 0).</summary>
    public static Vector3ConstantData Default => new() { Value = Vector3.Zero };
}

/// <summary>
/// Component data for Vector4 constant nodes.
/// </summary>
public struct Vector4ConstantData : IComponent
{
    /// <summary>The constant value.</summary>
    public Vector4 Value;

    /// <summary>Creates default data with value (0, 0, 0, 0).</summary>
    public static Vector4ConstantData Default => new() { Value = Vector4.Zero };
}
