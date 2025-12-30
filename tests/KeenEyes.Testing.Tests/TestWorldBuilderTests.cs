namespace KeenEyes.Testing.Tests;

public class TestWorldBuilderTests
{
    #region Build Tests

    [Fact]
    public void Build_CreatesValidWorld()
    {
        using var testWorld = new TestWorldBuilder().Build();

        testWorld.World.ShouldNotBeNull();
    }

    [Fact]
    public void Build_CanBeReused()
    {
        var builder = new TestWorldBuilder();

        using var world1 = builder.Build();
        using var world2 = builder.Build();

        world1.World.ShouldNotBe(world2.World);
    }

    #endregion

    #region WithDeterministicIds Tests

    [Fact]
    public void WithDeterministicIds_SetsFlag()
    {
        using var testWorld = new TestWorldBuilder()
            .WithDeterministicIds()
            .Build();

        testWorld.HasDeterministicIds.ShouldBeTrue();
    }

    [Fact]
    public void WithDeterministicIds_EntityIdsAreSequential()
    {
        using var testWorld = new TestWorldBuilder()
            .WithDeterministicIds()
            .Build();

        var entity1 = testWorld.World.Spawn().Build();
        var entity2 = testWorld.World.Spawn().Build();
        var entity3 = testWorld.World.Spawn().Build();

        entity1.Id.ShouldBe(0);
        entity2.Id.ShouldBe(1);
        entity3.Id.ShouldBe(2);
    }

    [Fact]
    public void WithoutDeterministicIds_FlagIsFalse()
    {
        using var testWorld = new TestWorldBuilder().Build();

        testWorld.HasDeterministicIds.ShouldBeFalse();
    }

    #endregion

    #region WithManualTime Tests

    [Fact]
    public void WithManualTime_CreatesTestClock()
    {
        using var testWorld = new TestWorldBuilder()
            .WithManualTime()
            .Build();

        testWorld.Clock.ShouldNotBeNull();
        testWorld.HasManualTime.ShouldBeTrue();
    }

    [Fact]
    public void WithManualTime_CustomFps_SetsCorrectly()
    {
        using var testWorld = new TestWorldBuilder()
            .WithManualTime(fps: 30f)
            .Build();

        testWorld.Clock!.Fps.ShouldBe(30f);
    }

    [Fact]
    public void WithoutManualTime_ClockIsNull()
    {
        using var testWorld = new TestWorldBuilder().Build();

        testWorld.Clock.ShouldBeNull();
        testWorld.HasManualTime.ShouldBeFalse();
    }

    #endregion

    #region WithPlugin Tests

    [Fact]
    public void WithPlugin_Generic_InstallsPlugin()
    {
        using var testWorld = new TestWorldBuilder()
            .WithPlugin<TestPlugin>()
            .Build();

        testWorld.World.HasPlugin<TestPlugin>().ShouldBeTrue();
    }

    [Fact]
    public void WithPlugin_Instance_InstallsPlugin()
    {
        var plugin = new TestPlugin();

        using var testWorld = new TestWorldBuilder()
            .WithPlugin(plugin)
            .Build();

        plugin.WasInstalled.ShouldBeTrue();
    }

    [Fact]
    public void WithPlugin_NullPlugin_Throws()
    {
        var builder = new TestWorldBuilder();

        Should.Throw<ArgumentNullException>(() => builder.WithPlugin(null!));
    }

    #endregion

    #region WithSystem Tests

    [Fact]
    public void WithSystem_Generic_RegistersSystem()
    {
        using var testWorld = new TestWorldBuilder()
            .WithSystem<TestCountingSystem>()
            .WithManualTime()
            .Build();

        // Step should trigger the system
        testWorld.Step();

        // System should have been updated - we can verify by checking the world updates
        testWorld.Clock!.FrameCount.ShouldBe(1);
    }

    [Fact]
    public void WithSystem_Instance_RegistersSystem()
    {
        var system = new TestCountingSystem();

        using var testWorld = new TestWorldBuilder()
            .WithSystem(system)
            .WithManualTime()
            .Build();

        testWorld.Step();

        system.UpdateCount.ShouldBe(1);
    }

    [Fact]
    public void WithSystem_CustomPhaseAndOrder_AppliesCorrectly()
    {
        using var testWorld = new TestWorldBuilder()
            .WithSystem<TestCountingSystem>(SystemPhase.EarlyUpdate, order: 10)
            .Build();

        // Should not throw - just verify registration worked
        testWorld.World.ShouldNotBeNull();
    }

    [Fact]
    public void WithSystem_NullSystem_Throws()
    {
        var builder = new TestWorldBuilder();

        Should.Throw<ArgumentNullException>(() => builder.WithSystem(null!));
    }

    #endregion

    #region WithMockNetwork Tests

    [Fact]
    public void WithMockNetwork_CreatesMockNetworkContext()
    {
        using var testWorld = new TestWorldBuilder()
            .WithMockNetwork()
            .Build();

        testWorld.MockNetwork.ShouldNotBeNull();
    }

    [Fact]
    public void WithMockNetwork_WithOptions_AppliesOptions()
    {
        var options = new Testing.Network.NetworkOptions
        {
            SimulatedLatency = 50f,
            SimulatedLatencyVariance = 10f,
            SimulatedPacketLoss = 0.01f
        };

        using var testWorld = new TestWorldBuilder()
            .WithMockNetwork(options)
            .Build();

        testWorld.MockNetwork.ShouldNotBeNull();
        testWorld.MockNetwork!.Options.SimulatedLatency.ShouldBe(50f);
        testWorld.MockNetwork!.Options.SimulatedLatencyVariance.ShouldBe(10f);
        testWorld.MockNetwork!.Options.SimulatedPacketLoss.ShouldBe(0.01f);
    }

    [Fact]
    public void WithMockNetwork_WithNullOptions_UsesDefaults()
    {
        using var testWorld = new TestWorldBuilder()
            .WithMockNetwork(null)
            .Build();

        testWorld.MockNetwork.ShouldNotBeNull();
        testWorld.MockNetwork!.Options.ShouldNotBeNull();
    }

    #endregion

    #region WithMockInput Tests

    [Fact]
    public void WithMockInput_CreatesMockInputContext()
    {
        using var testWorld = new TestWorldBuilder()
            .WithMockInput()
            .Build();

        testWorld.MockInput.ShouldNotBeNull();
    }

    [Fact]
    public void WithMockInput_CustomGamepadCount_CreatesCorrectNumberOfGamepads()
    {
        using var testWorld = new TestWorldBuilder()
            .WithMockInput(gamepadCount: 2)
            .Build();

        testWorld.MockInput.ShouldNotBeNull();
        testWorld.MockInput!.Gamepads.Length.ShouldBe(2);
    }

    #endregion

    #region WithMockLoopProvider Tests

    [Fact]
    public void WithMockLoopProvider_CreatesMockLoopProvider()
    {
        using var testWorld = new TestWorldBuilder()
            .WithMockLoopProvider()
            .Build();

        testWorld.MockLoopProvider.ShouldNotBeNull();
    }

    #endregion

    #region WithMockWindow Tests

    [Fact]
    public void WithMockWindow_CreatesMockWindow()
    {
        using var testWorld = new TestWorldBuilder()
            .WithMockWindow()
            .Build();

        testWorld.MockWindow.ShouldNotBeNull();
    }

    [Fact]
    public void WithMockWindow_CustomDimensions_SetsWindowSize()
    {
        using var testWorld = new TestWorldBuilder()
            .WithMockWindow(width: 1024, height: 768)
            .Build();

        testWorld.MockWindow.ShouldNotBeNull();
        testWorld.MockWindow!.Width.ShouldBe(1024);
        testWorld.MockWindow!.Height.ShouldBe(768);
    }

    #endregion

    #region WithMockGraphics Tests

    [Fact]
    public void WithMockGraphics_CreatesMockGraphicsContextAndDevice()
    {
        using var testWorld = new TestWorldBuilder()
            .WithMockGraphics()
            .Build();

        testWorld.MockGraphicsContext.ShouldNotBeNull();
        testWorld.MockGraphicsDevice.ShouldNotBeNull();
    }

    #endregion

    #region WithMock2DRenderer Tests

    [Fact]
    public void WithMock2DRenderer_CreatesMock2DRenderer()
    {
        using var testWorld = new TestWorldBuilder()
            .WithMock2DRenderer()
            .Build();

        testWorld.Mock2DRenderer.ShouldNotBeNull();
    }

    #endregion

    #region WithMockTextRenderer Tests

    [Fact]
    public void WithMockTextRenderer_CreatesMockTextRenderer()
    {
        using var testWorld = new TestWorldBuilder()
            .WithMockTextRenderer()
            .Build();

        testWorld.MockTextRenderer.ShouldNotBeNull();
    }

    #endregion

    #region WithMockFontManager Tests

    [Fact]
    public void WithMockFontManager_CreatesMockFontManager()
    {
        using var testWorld = new TestWorldBuilder()
            .WithMockFontManager()
            .Build();

        testWorld.MockFontManager.ShouldNotBeNull();
    }

    #endregion

    #region WithMockLogging Tests

    [Fact]
    public void WithMockLogging_CreatesMockLogProvider()
    {
        using var testWorld = new TestWorldBuilder()
            .WithMockLogging()
            .Build();

        testWorld.MockLogProvider.ShouldNotBeNull();
    }

    #endregion

    #region WithMockEncryption Tests

    [Fact]
    public void WithMockEncryption_CreatesMockEncryption()
    {
        using var testWorld = new TestWorldBuilder()
            .WithMockEncryption()
            .Build();

        testWorld.MockEncryption.ShouldNotBeNull();
    }

    #endregion

    #region WithEventRecording Tests

    [Fact]
    public void WithEventRecording_CreatesEventRecorder()
    {
        using var testWorld = new TestWorldBuilder()
            .WithManualTime()
            .WithEventRecording<TestEvent>()
            .Build();

        var recorder = testWorld.GetEventRecorder<TestEvent>();
        recorder.ShouldNotBeNull();
    }

    private readonly record struct TestEvent(int Value);

    #endregion

    #region Chaining Tests

    [Fact]
    public void Builder_SupportsFluenChaining()
    {
        using var testWorld = new TestWorldBuilder()
            .WithDeterministicIds()
            .WithManualTime(fps: 60f)
            .WithPlugin<TestPlugin>()
            .WithSystem<TestCountingSystem>()
            .Build();

        testWorld.HasDeterministicIds.ShouldBeTrue();
        testWorld.HasManualTime.ShouldBeTrue();
        testWorld.World.HasPlugin<TestPlugin>().ShouldBeTrue();
    }

    #endregion
}
