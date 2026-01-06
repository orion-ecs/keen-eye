using KeenEyes;

namespace KeenEyes.AotVsJit.Benchmarks;

/// <summary>
/// Components used in AOT vs JIT benchmarks.
/// </summary>
[Component]
public partial struct Position
{
    public float X;
    public float Y;
    public float Z;
}

[Component]
public partial struct Velocity
{
    public float X;
    public float Y;
    public float Z;
}

[Component]
public partial struct Health
{
    public int Current;
    public int Max;
}

[Component]
public partial struct Damage
{
    public int Amount;
}

[TagComponent]
public partial struct EnemyTag { }

[TagComponent]
public partial struct PlayerTag { }
