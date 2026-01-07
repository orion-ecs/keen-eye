// TODO: Remove this suppression after refactoring to use IWorld interface
#pragma warning disable KEEN050 // IWorld to World cast - test code pending refactoring

namespace KeenEyes.Tests;

/// <summary>
/// Test extension class for plugin tests.
/// </summary>
public class TestPhysicsWorld
{
    public float Gravity { get; set; } = -9.81f;

    public bool Raycast(float x, float y) => true;
}

/// <summary>
/// Test extension class for plugin tests.
/// </summary>
public class TestRenderer
{
    public int DrawCalls { get; set; }
}

/// <summary>
/// Test system for plugin tests.
/// </summary>
public class TestPluginSystem : SystemBase
{
    public int InitCount { get; private set; }
    public int UpdateCount { get; private set; }
    public bool Disposed { get; private set; }

    protected override void OnInitialize()
    {
        InitCount++;
    }

    public override void Update(float deltaTime)
    {
        UpdateCount++;
    }

    protected override void Dispose(bool disposing)
    {
        Disposed = true;
        base.Dispose(disposing);
    }
}

/// <summary>
/// Another test system for plugin tests.
/// </summary>
public class TestSecondPluginSystem : SystemBase
{
    public int UpdateCount { get; private set; }
    public bool Disposed { get; private set; }

    public override void Update(float deltaTime)
    {
        UpdateCount++;
    }

    protected override void Dispose(bool disposing)
    {
        Disposed = true;
        base.Dispose(disposing);
    }
}

/// <summary>
/// Simple test plugin for plugin tests.
/// </summary>
public class TestSimplePlugin : IWorldPlugin
{
    public string Name => "TestSimple";

    public bool InstallCalled { get; private set; }
    public bool UninstallCalled { get; private set; }
    public IPluginContext? LastContext { get; private set; }

    public void Install(IPluginContext context)
    {
        InstallCalled = true;
        LastContext = context;
    }

    public void Uninstall(IPluginContext context)
    {
        UninstallCalled = true;
    }
}

/// <summary>
/// Test plugin that registers systems.
/// </summary>
public class TestSystemPlugin : IWorldPlugin
{
    public string Name => "TestSystem";

    public TestPluginSystem? System1 { get; private set; }
    public TestSecondPluginSystem? System2 { get; private set; }

    public void Install(IPluginContext context)
    {
        System1 = context.AddSystem<TestPluginSystem>(SystemPhase.Update, order: 0);
        System2 = context.AddSystem<TestSecondPluginSystem>(SystemPhase.Update, order: 10);
    }

    public void Uninstall(IPluginContext context)
    {
    }
}

/// <summary>
/// Test plugin that exposes extensions.
/// </summary>
public class TestExtensionPlugin : IWorldPlugin
{
    public string Name => "TestExtension";

    public TestPhysicsWorld? Physics { get; private set; }

    public void Install(IPluginContext context)
    {
        Physics = new TestPhysicsWorld { Gravity = -10.0f };
        context.SetExtension(Physics);
    }

    public void Uninstall(IPluginContext context)
    {
        context.RemoveExtension<TestPhysicsWorld>();
    }
}

/// <summary>
/// Test plugin with configurable name.
/// </summary>
/// <param name="name">The plugin name.</param>
public class TestConfigurablePlugin(string name) : IWorldPlugin
{
    private readonly string name = name;

    public string Name => name;

    public void Install(IPluginContext context)
    {
    }

    public void Uninstall(IPluginContext context)
    {
    }
}

/// <summary>
/// Helper system for tracking update order.
/// </summary>
/// <param name="name">The system name.</param>
/// <param name="order">The list to track execution order.</param>
public class TestOrderTrackingSystem(string name, List<string> order) : SystemBase
{
    private readonly string name = name;
    private readonly List<string> order = order;

    public override void Update(float deltaTime)
    {
        order.Add(name);
    }
}

/// <summary>
/// Test plugin that registers systems in specific order.
/// </summary>
/// <param name="system1">The first system to register.</param>
/// <param name="system2">The second system to register.</param>
public class TestOrderedPlugin(ISystem system1, ISystem system2) : IWorldPlugin
{
    private readonly ISystem system1 = system1;
    private readonly ISystem system2 = system2;

    public string Name => "TestOrdered";

    public void Install(IPluginContext context)
    {
        context.AddSystem(system1, SystemPhase.Update, order: 0);
        context.AddSystem(system2, SystemPhase.Update, order: 10);
    }

    public void Uninstall(IPluginContext context)
    {
    }
}

/// <summary>
/// Test plugin that registers a system group.
/// </summary>
/// <param name="group">The system group to register.</param>
public class TestGroupPlugin(SystemGroup group) : IWorldPlugin
{
    private readonly SystemGroup group = group;

    public string Name => "TestGroup";

    public void Install(IPluginContext context)
    {
        context.AddSystemGroup(group, SystemPhase.Update);
    }

    public void Uninstall(IPluginContext context)
    {
    }
}

/// <summary>
/// Test plugin for tracking install order.
/// </summary>
/// <param name="name">The plugin name.</param>
/// <param name="installOrder">The list to track install order.</param>
public class TestTrackingPlugin(string name, List<string> installOrder) : IWorldPlugin
{
    private readonly string name = name;
    private readonly List<string> installOrder = installOrder;

    public string Name => name;

    public void Install(IPluginContext context)
    {
        installOrder.Add(name);
    }

    public void Uninstall(IPluginContext context)
    {
    }
}

/// <summary>
/// Test plugin that accesses the world during install.
/// </summary>
public class TestWorldAccessPlugin : IWorldPlugin
{
    public string Name => "TestWorldAccess";

    public void Install(IPluginContext context)
    {
        var world = (World)context.World;
        world.SetSingleton(new TestGameConfig { Difficulty = 5 });
    }

    public void Uninstall(IPluginContext context)
    {
    }
}

/// <summary>
/// Test plugin that uses AddSystem with dependency constraints.
/// </summary>
public class TestDependencyPlugin : IWorldPlugin
{
    public string Name => "TestDependency";

    public TestPluginSystem? System { get; private set; }

    public void Install(IPluginContext context)
    {
        System = context.AddSystem<TestPluginSystem>(
            SystemPhase.Update,
            order: 0,
            runsBefore: [typeof(TestSecondPluginSystem)],
            runsAfter: []);
    }

    public void Uninstall(IPluginContext context)
    {
    }
}

/// <summary>
/// Test plugin that uses AddSystem instance overload with dependency constraints.
/// </summary>
public class TestInstanceDependencyPlugin : IWorldPlugin
{
    public string Name => "TestInstanceDependency";

    public TestPluginSystem? System { get; private set; }

    public void Install(IPluginContext context)
    {
        System = new TestPluginSystem();
        context.AddSystem(
            System,
            SystemPhase.Update,
            order: 0,
            runsBefore: [typeof(TestSecondPluginSystem)],
            runsAfter: []);
    }

    public void Uninstall(IPluginContext context)
    {
    }
}
