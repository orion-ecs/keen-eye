namespace KeenEyes.Benchmarks;

/// <summary>
/// 2D position component (8 bytes).
/// </summary>
public struct Position : IComponent
{
    public float X;
    public float Y;
}

/// <summary>
/// 2D velocity component (8 bytes).
/// </summary>
public struct Velocity : IComponent
{
    public float X;
    public float Y;
}

/// <summary>
/// Health component (8 bytes).
/// </summary>
public struct Health : IComponent
{
    public int Current;
    public int Max;
}

/// <summary>
/// Rotation component (4 bytes).
/// </summary>
public struct Rotation : IComponent
{
    public float Angle;
}

/// <summary>
/// Small component for minimal overhead testing (4 bytes).
/// </summary>
public struct SmallComponent : IComponent
{
    public int Value;
}

/// <summary>
/// Medium-sized component (64 bytes) to test memory layout effects.
/// </summary>
public struct MediumComponent : IComponent
{
    public long A, B, C, D, E, F, G, H;
}

/// <summary>
/// Large component (256 bytes) to test larger data transfers.
/// </summary>
public struct LargeComponent : IComponent
{
    public long A0, A1, A2, A3, A4, A5, A6, A7;
    public long B0, B1, B2, B3, B4, B5, B6, B7;
    public long C0, C1, C2, C3, C4, C5, C6, C7;
    public long D0, D1, D2, D3, D4, D5, D6, D7;
}

/// <summary>
/// Tag component for filtering tests.
/// </summary>
public struct ActiveTag : ITagComponent;

/// <summary>
/// Tag component for exclusion filter tests.
/// </summary>
public struct FrozenTag : ITagComponent;

/// <summary>
/// Tag component for complex filter tests.
/// </summary>
public struct PlayerTag : ITagComponent;

/// <summary>
/// Singleton for time tracking.
/// </summary>
public struct GameTime
{
    public float DeltaTime;
    public float TotalTime;
}

/// <summary>
/// Singleton for game configuration.
/// </summary>
public struct GameConfig
{
    public int MaxEntities;
    public float Gravity;
}
