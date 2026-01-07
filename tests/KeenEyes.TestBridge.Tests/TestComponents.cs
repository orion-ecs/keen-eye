namespace KeenEyes.TestBridge.Tests;

/// <summary>
/// Test position component.
/// </summary>
public struct TestPosition : IComponent
{
    public float X;
    public float Y;
}

/// <summary>
/// Test velocity component.
/// </summary>
public struct TestVelocity : IComponent
{
    public float X;
    public float Y;
}

/// <summary>
/// Test health component.
/// </summary>
public struct TestHealth : IComponent
{
    public int Current;
    public int Max;
}

/// <summary>
/// Test tag component for marking entities.
/// </summary>
public struct TestTag : ITagComponent;

/// <summary>
/// Test system that counts updates.
/// </summary>
public class TestCountingSystem : SystemBase
{
    public int UpdateCount { get; private set; }
    public float TotalDeltaTime { get; private set; }

    public override void Update(float deltaTime)
    {
        UpdateCount++;
        TotalDeltaTime += deltaTime;
    }
}
