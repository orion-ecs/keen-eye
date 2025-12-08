namespace KeenEyes.Testing.Tests;

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
/// Test tag component for marking entities as active.
/// </summary>
public struct ActiveTag : ITagComponent;

/// <summary>
/// Test tag component for marking entities as frozen.
/// </summary>
public struct FrozenTag : ITagComponent;

/// <summary>
/// Simple test plugin for testing.
/// </summary>
public class TestPlugin : IWorldPlugin
{
    public string Name => "TestPlugin";
    public bool WasInstalled { get; private set; }
    public bool WasUninstalled { get; private set; }

    public void Install(PluginContext context)
    {
        WasInstalled = true;
    }

    public void Uninstall(PluginContext context)
    {
        WasUninstalled = true;
    }
}

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
