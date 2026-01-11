using System.Text.Json;
using KeenEyes.TestBridge.Ipc.Handlers;
using KeenEyes.TestBridge.Ipc.Protocol;
using KeenEyes.TestBridge.Window;

namespace KeenEyes.TestBridge.Tests.Ipc;

public class InputCommandHandlerTests : IDisposable
{
    private readonly World world;
    private readonly InProcessBridge bridge;
    private readonly InputCommandHandler handler;

    public InputCommandHandlerTests()
    {
        world = new World();
        bridge = new InProcessBridge(world);
        handler = new InputCommandHandler(bridge.Input);
    }

    public void Dispose()
    {
        bridge.Dispose();
        world.Dispose();
    }

    [Fact]
    public void Prefix_ReturnsInput()
    {
        handler.Prefix.ShouldBe("input");
    }

    [Fact]
    public async Task HandleAsync_KeyDown_Works()
    {
        var args = JsonSerializer.SerializeToElement(new { key = "Space" });

        var result = await handler.HandleAsync("keyDown", args, TestContext.Current.CancellationToken);

        result.ShouldBeNull(); // Void command returns null
    }

    [Fact]
    public async Task HandleAsync_KeyPress_WithModifiers_Works()
    {
        var args = JsonSerializer.SerializeToElement(new
        {
            key = "A",
            modifiers = "Control"
        });

        var result = await handler.HandleAsync("keyPress", args, TestContext.Current.CancellationToken);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task HandleAsync_MouseMove_UpdatesPosition()
    {
        var args = JsonSerializer.SerializeToElement(new { x = 100.0, y = 200.0 });

        await handler.HandleAsync("mouseMove", args, TestContext.Current.CancellationToken);

        var position = bridge.Input.GetMousePosition();
        position.X.ShouldBe(100);
        position.Y.ShouldBe(200);
    }

    [Fact]
    public async Task HandleAsync_Click_Works()
    {
        var args = JsonSerializer.SerializeToElement(new { x = 50.0, y = 50.0, button = "Left" });

        var result = await handler.HandleAsync("click", args, TestContext.Current.CancellationToken);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task HandleAsync_GetMousePosition_ReturnsPosition()
    {
        await bridge.Input.MouseMoveAsync(123, 456);

        var result = await handler.HandleAsync("getMousePosition", null, TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task HandleAsync_GamepadCount_ReturnsCount()
    {
        var result = await handler.HandleAsync("gamepadCount", null, TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        ((int)result!).ShouldBe(4);
    }

    [Fact]
    public async Task HandleAsync_UnknownCommand_Throws()
    {
        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await handler.HandleAsync("unknownCommand", null, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task HandleAsync_MissingRequiredArg_Throws()
    {
        // keyDown requires 'key' argument
        await Should.ThrowAsync<ArgumentException>(async () =>
            await handler.HandleAsync("keyDown", null, TestContext.Current.CancellationToken));
    }
}

public class StateCommandHandlerTests : IDisposable
{
    private readonly World world;
    private readonly InProcessBridge bridge;
    private readonly StateCommandHandler handler;

    public StateCommandHandlerTests()
    {
        world = new World();
        bridge = new InProcessBridge(world);
        handler = new StateCommandHandler(bridge.State);
    }

    public void Dispose()
    {
        bridge.Dispose();
        world.Dispose();
    }

    [Fact]
    public void Prefix_ReturnsState()
    {
        handler.Prefix.ShouldBe("state");
    }

    [Fact]
    public async Task HandleAsync_GetEntityCount_ReturnsCount()
    {
        world.Spawn().Build();
        world.Spawn().Build();

        var result = await handler.HandleAsync("getEntityCount", null, TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        ((int)result!).ShouldBe(2);
    }

    [Fact]
    public async Task HandleAsync_GetWorldStats_ReturnsStats()
    {
        world.Spawn().Build();

        var result = await handler.HandleAsync("getWorldStats", null, TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task HandleAsync_GetEntityByName_ReturnsEntity()
    {
        world.Spawn().WithName("TestEntity").Build();
        var args = JsonSerializer.SerializeToElement(new { name = "TestEntity" });

        var result = await handler.HandleAsync("getEntityByName", args, TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task HandleAsync_GetEntityByName_ReturnsNullForUnknown()
    {
        var args = JsonSerializer.SerializeToElement(new { name = "NonExistent" });

        var result = await handler.HandleAsync("getEntityByName", args, TestContext.Current.CancellationToken);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task HandleAsync_GetEntitiesWithTag_ReturnsTaggedEntities()
    {
        var entity = world.Spawn().Build();
        world.AddTag(entity, "enemy");
        var args = JsonSerializer.SerializeToElement(new { tag = "enemy" });

        var result = await handler.HandleAsync("getEntitiesWithTag", args, TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
    }
}

public class CaptureCommandHandlerTests : IDisposable
{
    private readonly World world;
    private readonly InProcessBridge bridge;
    private readonly CaptureCommandHandler handler;

    public CaptureCommandHandlerTests()
    {
        world = new World();
        bridge = new InProcessBridge(world);
        handler = new CaptureCommandHandler(bridge.Capture);
    }

    public void Dispose()
    {
        bridge.Dispose();
        world.Dispose();
    }

    [Fact]
    public void Prefix_ReturnsCapture()
    {
        handler.Prefix.ShouldBe("capture");
    }

    [Fact]
    public async Task HandleAsync_IsAvailable_ReturnsBool()
    {
        var result = await handler.HandleAsync("isAvailable", null, TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        // Without renderer, should be false
        ((bool)result!).ShouldBeFalse();
    }

    [Fact]
    public async Task HandleAsync_IsRecording_ReturnsBool()
    {
        var result = await handler.HandleAsync("isRecording", null, TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        ((bool)result!).ShouldBeFalse();
    }

    [Fact]
    public async Task HandleAsync_RecordedFrameCount_ReturnsInt()
    {
        var result = await handler.HandleAsync("recordedFrameCount", null, TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        ((int)result!).ShouldBe(0);
    }
}

public class WindowCommandHandlerTests : IDisposable
{
    private readonly World world;
    private readonly InProcessBridge bridge;
    private readonly WindowCommandHandler handler;

    public WindowCommandHandlerTests()
    {
        world = new World();
        bridge = new InProcessBridge(world);
        handler = new WindowCommandHandler(bridge.Window);
    }

    public void Dispose()
    {
        bridge.Dispose();
        world.Dispose();
    }

    [Fact]
    public void Prefix_ReturnsWindow()
    {
        handler.Prefix.ShouldBe("window");
    }

    [Fact]
    public async Task HandleAsync_IsAvailable_ReturnsBool()
    {
        var result = await handler.HandleAsync("isAvailable", null, TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        // Without window, should be false
        ((bool)result!).ShouldBeFalse();
    }

    [Fact]
    public async Task HandleAsync_GetState_ReturnsSnapshot()
    {
        var result = await handler.HandleAsync("getState", null, TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result.ShouldBeOfType<WindowStateSnapshot>();
    }

    [Fact]
    public async Task HandleAsync_GetSize_ReturnsSizeResult()
    {
        var result = await handler.HandleAsync("getSize", null, TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result.ShouldBeOfType<WindowSizeResult>();
    }

    [Fact]
    public async Task HandleAsync_GetTitle_ReturnsString()
    {
        var result = await handler.HandleAsync("getTitle", null, TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result.ShouldBeOfType<string>();
    }

    [Fact]
    public async Task HandleAsync_IsClosing_ReturnsBool()
    {
        var result = await handler.HandleAsync("isClosing", null, TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        ((bool)result!).ShouldBeFalse();
    }

    [Fact]
    public async Task HandleAsync_IsFocused_ReturnsBool()
    {
        var result = await handler.HandleAsync("isFocused", null, TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        ((bool)result!).ShouldBeFalse();
    }

    [Fact]
    public async Task HandleAsync_GetAspectRatio_ReturnsFloat()
    {
        var result = await handler.HandleAsync("getAspectRatio", null, TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        ((float)result!).ShouldBe(0f);
    }

    [Fact]
    public async Task HandleAsync_UnknownCommand_Throws()
    {
        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await handler.HandleAsync("unknownCommand", null, TestContext.Current.CancellationToken));
    }
}
