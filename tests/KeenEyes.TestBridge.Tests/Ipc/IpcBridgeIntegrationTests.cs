using KeenEyes.TestBridge.Client;
using KeenEyes.TestBridge.State;

namespace KeenEyes.TestBridge.Tests.Ipc;

/// <summary>
/// Integration tests for IPC bridge client/server communication.
/// </summary>
public class IpcBridgeIntegrationTests : IAsyncLifetime
{
    private readonly string testPipeName = $"KeenEyes.TestBridge.Tests.{Guid.NewGuid():N}";
    private World? world;
    private InProcessBridge? inProcessBridge;
    private KeenEyes.TestBridge.Ipc.IpcBridgeServer? server;

    public async ValueTask InitializeAsync()
    {
        world = new World();
        inProcessBridge = new InProcessBridge(world);
        var options = new IpcOptions { PipeName = testPipeName };
        server = new KeenEyes.TestBridge.Ipc.IpcBridgeServer(inProcessBridge, options);
        await server.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (server != null)
        {
            await server.StopAsync();
            server.Dispose();
        }

        inProcessBridge?.Dispose();
        world?.Dispose();
    }

    #region Connection

    [Fact]
    public async Task Client_ConnectsToServer()
    {
        var options = new IpcOptions { PipeName = testPipeName };
        await using var client = new TestBridgeClient(options);

        await client.ConnectAsync(TestContext.Current.CancellationToken);

        client.IsConnected.ShouldBeTrue();
    }

    [Fact]
    public async Task Client_DisconnectsFromServer()
    {
        var options = new IpcOptions { PipeName = testPipeName };
        await using var client = new TestBridgeClient(options);
        await client.ConnectAsync(TestContext.Current.CancellationToken);

        await client.DisconnectAsync();

        client.IsConnected.ShouldBeFalse();
    }

    [Fact]
    public async Task Client_RaisesConnectionChangedOnConnect()
    {
        var options = new IpcOptions { PipeName = testPipeName };
        await using var client = new TestBridgeClient(options);
        var connectionEvents = new List<bool>();
        client.ConnectionChanged += isConnected => connectionEvents.Add(isConnected);

        await client.ConnectAsync(TestContext.Current.CancellationToken);

        connectionEvents.ShouldContain(true);
    }

    #endregion

    #region State Controller

    [Fact]
    public async Task State_GetEntityCountAsync_ReturnsCount()
    {
        world!.Spawn().Build();
        world.Spawn().Build();
        world.Spawn().Build();

        var options = new IpcOptions { PipeName = testPipeName };
        await using var client = new TestBridgeClient(options);
        await client.ConnectAsync(TestContext.Current.CancellationToken);

        var count = await client.State.GetEntityCountAsync();

        count.ShouldBe(3);
    }

    [Fact]
    public async Task State_GetWorldStatsAsync_ReturnsStats()
    {
        world!.Spawn().With(new TestPosition { X = 1, Y = 2 }).Build();

        var options = new IpcOptions { PipeName = testPipeName };
        await using var client = new TestBridgeClient(options);
        await client.ConnectAsync(TestContext.Current.CancellationToken);

        var stats = await client.State.GetWorldStatsAsync();

        stats.EntityCount.ShouldBe(1);
        stats.ArchetypeCount.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task State_GetEntityByNameAsync_ReturnsEntity()
    {
        world!.Spawn()
            .With(new TestPosition { X = 10, Y = 20 })
            .WithName("TestEntity")
            .Build();

        var options = new IpcOptions { PipeName = testPipeName };
        await using var client = new TestBridgeClient(options);
        await client.ConnectAsync(TestContext.Current.CancellationToken);

        var entity = await client.State.GetEntityByNameAsync("TestEntity");

        entity.ShouldNotBeNull();
        entity.Name.ShouldBe("TestEntity");
    }

    [Fact]
    public async Task State_GetEntityByNameAsync_ReturnsNullForUnknown()
    {
        var options = new IpcOptions { PipeName = testPipeName };
        await using var client = new TestBridgeClient(options);
        await client.ConnectAsync(TestContext.Current.CancellationToken);

        var entity = await client.State.GetEntityByNameAsync("NonExistent");

        entity.ShouldBeNull();
    }

    [Fact]
    public async Task State_QueryEntitiesAsync_ReturnsMatchingEntities()
    {
        world!.Spawn().With(new TestPosition { X = 1, Y = 2 }).Build();
        world.Spawn().With(new TestPosition { X = 3, Y = 4 }).Build();
        world.Spawn().Build(); // No Position

        var options = new IpcOptions { PipeName = testPipeName };
        await using var client = new TestBridgeClient(options);
        await client.ConnectAsync(TestContext.Current.CancellationToken);

        var query = new EntityQuery
        {
            WithComponents = ["TestPosition"]
        };
        var entities = await client.State.QueryEntitiesAsync(query);

        entities.Count.ShouldBe(2);
    }

    [Fact]
    public async Task State_GetEntitiesWithTagAsync_ReturnsTaggedEntities()
    {
        var entity1 = world!.Spawn().Build();
        var entity2 = world.Spawn().Build();
        world.Spawn().Build(); // No tag

        world.AddTag(entity1, "enemy");
        world.AddTag(entity2, "enemy");

        var options = new IpcOptions { PipeName = testPipeName };
        await using var client = new TestBridgeClient(options);
        await client.ConnectAsync(TestContext.Current.CancellationToken);

        var entityIds = await client.State.GetEntitiesWithTagAsync("enemy");

        entityIds.Count.ShouldBe(2);
        entityIds.ShouldContain(entity1.Id);
        entityIds.ShouldContain(entity2.Id);
    }

    #endregion

    #region Input Controller

    [Fact]
    public async Task Input_KeyPressAsync_TriggersKeyState()
    {
        var options = new IpcOptions { PipeName = testPipeName };
        await using var client = new TestBridgeClient(options);
        await client.ConnectAsync(TestContext.Current.CancellationToken);

        // Key press should not throw
        await Should.NotThrowAsync(async () =>
            await client.Input.KeyPressAsync(KeenEyes.Input.Abstractions.Key.Space));
    }

    [Fact]
    public async Task Input_MouseMoveAsync_UpdatesPosition()
    {
        var options = new IpcOptions { PipeName = testPipeName };
        await using var client = new TestBridgeClient(options);
        await client.ConnectAsync(TestContext.Current.CancellationToken);

        await client.Input.MouseMoveAsync(100, 200);

        var position = client.Input.GetMousePosition();
        position.X.ShouldBe(100);
        position.Y.ShouldBe(200);
    }

    [Fact]
    public async Task Input_ClickAsync_Works()
    {
        var options = new IpcOptions { PipeName = testPipeName };
        await using var client = new TestBridgeClient(options);
        await client.ConnectAsync(TestContext.Current.CancellationToken);

        await Should.NotThrowAsync(async () =>
            await client.Input.ClickAsync(50, 50, KeenEyes.Input.Abstractions.MouseButton.Left));
    }

    [Fact]
    public async Task Input_TypeTextAsync_Works()
    {
        var options = new IpcOptions { PipeName = testPipeName };
        await using var client = new TestBridgeClient(options);
        await client.ConnectAsync(TestContext.Current.CancellationToken);

        await Should.NotThrowAsync(async () =>
            await client.Input.TypeTextAsync("Hello"));
    }

    [Fact]
    public async Task Input_GamepadCount_ReturnsCount()
    {
        var options = new IpcOptions { PipeName = testPipeName };
        await using var client = new TestBridgeClient(options);
        await client.ConnectAsync(TestContext.Current.CancellationToken);

        var count = client.Input.GamepadCount;

        count.ShouldBe(4); // Default gamepad count
    }

    [Fact]
    public async Task Input_ResetAllAsync_Works()
    {
        var options = new IpcOptions { PipeName = testPipeName };
        await using var client = new TestBridgeClient(options);
        await client.ConnectAsync(TestContext.Current.CancellationToken);

        await client.Input.MouseMoveAsync(100, 100);

        await Should.NotThrowAsync(async () =>
            await client.Input.ResetAllAsync());
    }

    #endregion

    #region Capture Controller

    [Fact]
    public async Task Capture_IsAvailable_ReturnsFalseWithoutRenderer()
    {
        var options = new IpcOptions { PipeName = testPipeName };
        await using var client = new TestBridgeClient(options);
        await client.ConnectAsync(TestContext.Current.CancellationToken);

        // Without a renderer, capture should not be available
        client.Capture.IsAvailable.ShouldBeFalse();
    }

    [Fact]
    public async Task Capture_IsRecording_ReturnsFalseInitially()
    {
        var options = new IpcOptions { PipeName = testPipeName };
        await using var client = new TestBridgeClient(options);
        await client.ConnectAsync(TestContext.Current.CancellationToken);

        client.Capture.IsRecording.ShouldBeFalse();
    }

    #endregion

    #region WaitForAsync

    [Fact]
    public async Task WaitForAsync_ConditionMet_ReturnsTrue()
    {
        world!.Spawn().Build();

        var options = new IpcOptions { PipeName = testPipeName };
        await using var client = new TestBridgeClient(options);
        await client.ConnectAsync(TestContext.Current.CancellationToken);

        var result = await client.WaitForAsync(
            state => state.GetEntityCountAsync().Result > 0,
            TimeSpan.FromSeconds(5),
            cancellationToken: TestContext.Current.CancellationToken);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task WaitForAsync_ConditionNotMet_ReturnsFalse()
    {
        var options = new IpcOptions { PipeName = testPipeName };
        await using var client = new TestBridgeClient(options);
        await client.ConnectAsync(TestContext.Current.CancellationToken);

        var result = await client.WaitForAsync(
            state => state.GetEntityCountAsync().Result > 100,
            TimeSpan.FromMilliseconds(100),
            pollInterval: TimeSpan.FromMilliseconds(20),
            cancellationToken: TestContext.Current.CancellationToken);

        result.ShouldBeFalse();
    }

    #endregion

    #region Error Handling

    [Fact]
    public async Task Client_SendingBeforeConnect_Throws()
    {
        var options = new IpcOptions { PipeName = testPipeName };
        await using var client = new TestBridgeClient(options);

        // Not connected, should throw
        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await client.State.GetEntityCountAsync());
    }

    [Fact]
    public async Task Client_DisposedTwice_DoesNotThrow()
    {
        var options = new IpcOptions { PipeName = testPipeName };
        var client = new TestBridgeClient(options);
        await client.ConnectAsync(TestContext.Current.CancellationToken);

        await client.DisposeAsync();
        await Should.NotThrowAsync(async () =>
            await client.DisposeAsync());
    }

    #endregion
}

[Component]
public partial struct TestPosition
{
    public float X;
    public float Y;
}
