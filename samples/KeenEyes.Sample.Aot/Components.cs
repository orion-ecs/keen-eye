namespace KeenEyes.Sample.Aot;

// ============================================================================
// Component Definitions (AOT-compatible)
// ============================================================================
// Note: There are TWO ways to define components in KeenEyes:
//
// 1. [Component] attribute (RECOMMENDED):
//    [Component]
//    public partial struct Position { public float X, Y; }
//
//    The source generator will implement IComponent automatically and
//    generate fluent builder methods like WithPosition().
//
// 2. Explicit IComponent (shown below):
//    public struct Position : IComponent { public float X, Y; }
//
//    This is also AOT-compatible and demonstrates that no reflection is
//    used. Useful when you want full control or minimal generated code.
//
// Both approaches are equally AOT-compatible. The [Component] attribute is
// recommended for most use cases as it generates helpful extension methods.
// ============================================================================

/// <summary>Position component for 2D coordinates.</summary>
public struct Position : IComponent
{
    /// <summary>X coordinate.</summary>
    public float X;

    /// <summary>Y coordinate.</summary>
    public float Y;
}

/// <summary>Velocity component for movement speed.</summary>
public struct Velocity : IComponent
{
    /// <summary>Horizontal velocity.</summary>
    public float Dx;

    /// <summary>Vertical velocity.</summary>
    public float Dy;
}

/// <summary>Health component for entity health tracking.</summary>
public struct Health : IComponent
{
    /// <summary>Current health value.</summary>
    public int Current;

    /// <summary>Maximum health value.</summary>
    public int Max;
}

/// <summary>Tag component marking enemy entities.</summary>
public struct EnemyTag : ITagComponent;

/// <summary>Singleton for game-wide settings.</summary>
public struct GameSettings
{
    /// <summary>Time scale multiplier.</summary>
    public float TimeScale;
}
