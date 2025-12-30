using KeenEyes.Testing.Network;

namespace KeenEyes.Testing.Tests.Network;

public class MockNetworkContextTests
{
    #region Connection Tests

    [Fact]
    public void Connect_SetsConnectingState()
    {
        using var network = new MockNetworkContext();

        network.Connect("server:7777");

        network.ConnectionState.ShouldBe(NetworkConnectionState.Connecting);
        network.ConnectedEndpoint.ShouldBe("server:7777");
    }

    [Fact]
    public void Connect_IncrementsConnectAttemptCount()
    {
        using var network = new MockNetworkContext();

        network.Connect("server:7777");

        network.ConnectAttemptCount.ShouldBe(1);
    }

    [Fact]
    public void Connect_WhenAlreadyConnecting_Throws()
    {
        using var network = new MockNetworkContext();
        network.Connect("server:7777");

        Should.Throw<InvalidOperationException>(() => network.Connect("server:8888"));
    }

    [Fact]
    public void Connect_WithAutoConnect_ConnectsImmediately()
    {
        using var network = new MockNetworkContext();
        network.AutoConnect = true;

        network.Connect("server:7777");

        network.IsConnected.ShouldBeTrue();
        network.ConnectionState.ShouldBe(NetworkConnectionState.Connected);
    }

    [Fact]
    public void Connect_WithAutoFail_FailsImmediately()
    {
        using var network = new MockNetworkContext();
        network.AutoFail = true;
        var eventFired = false;
        network.OnConnectionFailed += _ => eventFired = true;

        network.Connect("server:7777");

        network.ConnectionState.ShouldBe(NetworkConnectionState.Disconnected);
        eventFired.ShouldBeTrue();
    }

    #endregion

    #region SimulateConnect Tests

    [Fact]
    public void SimulateConnect_SetsConnectedState()
    {
        using var network = new MockNetworkContext();
        network.Connect("server:7777");

        network.SimulateConnect();

        network.IsConnected.ShouldBeTrue();
        network.ConnectionState.ShouldBe(NetworkConnectionState.Connected);
    }

    [Fact]
    public void SimulateConnect_FiresOnConnectedEvent()
    {
        using var network = new MockNetworkContext();
        network.Connect("server:7777");
        NetworkConnectionEventArgs? receivedArgs = null;
        network.OnConnected += args => receivedArgs = args;

        network.SimulateConnect();

        receivedArgs.ShouldNotBeNull();
        receivedArgs.Value.Endpoint.ShouldBe("server:7777");
    }

    [Fact]
    public void SimulateConnect_WhenNotConnecting_Throws()
    {
        using var network = new MockNetworkContext();

        Should.Throw<InvalidOperationException>(() => network.SimulateConnect());
    }

    #endregion

    #region Disconnect Tests

    [Fact]
    public void Disconnect_SetsDisconnectedState()
    {
        using var network = new MockNetworkContext();
        network.Connect("server:7777");
        network.SimulateConnect();

        network.Disconnect("User requested");

        network.ConnectionState.ShouldBe(NetworkConnectionState.Disconnected);
        network.IsConnected.ShouldBeFalse();
    }

    [Fact]
    public void Disconnect_IncrementsDisconnectCount()
    {
        using var network = new MockNetworkContext();
        network.Connect("server:7777");
        network.SimulateConnect();

        network.Disconnect();

        network.DisconnectCount.ShouldBe(1);
    }

    [Fact]
    public void Disconnect_FiresOnDisconnectedEvent()
    {
        using var network = new MockNetworkContext();
        network.Connect("server:7777");
        network.SimulateConnect();
        NetworkConnectionEventArgs? receivedArgs = null;
        network.OnDisconnected += args => receivedArgs = args;

        network.Disconnect("Test reason");

        receivedArgs.ShouldNotBeNull();
        receivedArgs.Value.Reason.ShouldBe("Test reason");
    }

    [Fact]
    public void Disconnect_WhenAlreadyDisconnected_DoesNothing()
    {
        using var network = new MockNetworkContext();

        network.Disconnect(); // Should not throw

        network.DisconnectCount.ShouldBe(0);
    }

    #endregion

    #region Send Tests

    [Fact]
    public void Send_WhenConnected_RecordsMessage()
    {
        using var network = new MockNetworkContext();
        network.Connect("server:7777");
        network.SimulateConnect();
        var message = new TestMessage { Value = 42 };

        network.Send(message);

        network.SentMessages.Count.ShouldBe(1);
        network.SentMessages[0].Data.ShouldBe(message);
        network.SentMessages[0].Reliable.ShouldBeTrue();
    }

    [Fact]
    public void Send_WhenNotConnected_Throws()
    {
        using var network = new MockNetworkContext();

        Should.Throw<InvalidOperationException>(() => network.Send(new TestMessage()));
    }

    [Fact]
    public void Send_Unreliable_RecordsReliableFalse()
    {
        using var network = new MockNetworkContext();
        network.Connect("server:7777");
        network.SimulateConnect();

        network.Send(new TestMessage(), reliable: false);

        network.SentMessages[0].Reliable.ShouldBeFalse();
    }

    [Fact]
    public void Send_WithChannel_RecordsChannel()
    {
        using var network = new MockNetworkContext();
        network.Connect("server:7777");
        network.SimulateConnect();

        network.Send(new TestMessage(), channel: 5);

        network.SentMessages[0].Channel.ShouldBe(5);
    }

    [Fact]
    public void SendTo_RecordsEndpoint()
    {
        using var network = new MockNetworkContext();

        network.SendTo("client:1234", new TestMessage());

        network.SentMessages.Count.ShouldBe(1);
        network.SentMessages[0].Endpoint.ShouldBe("client:1234");
    }

    #endregion

    #region Receive Tests

    [Fact]
    public void SimulateReceive_AddsToQueue()
    {
        using var network = new MockNetworkContext();
        var message = new TestMessage { Value = 123 };

        network.SimulateReceive(message, "peer:1234");

        network.ReceiveQueueCount.ShouldBe(1);
    }

    [Fact]
    public void SimulateReceive_FiresOnDataReceived()
    {
        using var network = new MockNetworkContext();
        NetworkDataReceivedEventArgs? receivedArgs = null;
        network.OnDataReceived += args => receivedArgs = args;
        var message = new TestMessage { Value = 42 };

        network.SimulateReceive(message, "peer:1234", channel: 3);

        receivedArgs.ShouldNotBeNull();
        receivedArgs.Value.Data.ShouldBe(message);
        receivedArgs.Value.SenderEndpoint.ShouldBe("peer:1234");
        receivedArgs.Value.Channel.ShouldBe(3);
    }

    [Fact]
    public void TryReceive_WithMatchingType_ReturnsTrue()
    {
        using var network = new MockNetworkContext();
        network.SimulateReceive(new TestMessage { Value = 42 }, "peer:1234");

        var result = network.TryReceive<TestMessage>(out var message);

        result.ShouldBeTrue();
        message.ShouldNotBeNull();
        message!.Value.ShouldBe(42);
    }

    [Fact]
    public void TryReceive_WithNoMatchingType_ReturnsFalse()
    {
        using var network = new MockNetworkContext();
        network.SimulateReceive(new TestMessage { Value = 42 }, "peer:1234");

        var result = network.TryReceive<OtherMessage>(out var message);

        result.ShouldBeFalse();
        message.ShouldBeNull();
    }

    [Fact]
    public void TryReceive_EmptyQueue_ReturnsFalse()
    {
        using var network = new MockNetworkContext();

        var result = network.TryReceive<TestMessage>(out var message);

        result.ShouldBeFalse();
        message.ShouldBeNull();
    }

    [Fact]
    public void TryReceiveAny_ReturnsFirstMessage()
    {
        using var network = new MockNetworkContext();
        network.SimulateReceive(new TestMessage { Value = 42 }, "peer:1234");

        var result = network.TryReceiveAny(out var data, out var sender);

        result.ShouldBeTrue();
        data.ShouldBeOfType<TestMessage>();
        sender.ShouldBe("peer:1234");
    }

    #endregion

    #region Latency and Packet Loss

    [Fact]
    public void SimulateLatency_SetsLatency()
    {
        using var network = new MockNetworkContext();

        network.SimulateLatency(100f);

        network.Latency.ShouldBe(100f);
        network.Options.SimulatedLatency.ShouldBe(100f);
    }

    [Fact]
    public void SimulatePacketLoss_SetsPacketLoss()
    {
        using var network = new MockNetworkContext();

        network.SimulatePacketLoss(0.1f);

        network.PacketLoss.ShouldBe(0.1f);
        network.Options.SimulatedPacketLoss.ShouldBe(0.1f);
    }

    [Fact]
    public void SimulatePacketLoss_ClampsBetweenZeroAndOne()
    {
        using var network = new MockNetworkContext();

        network.SimulatePacketLoss(1.5f);
        network.PacketLoss.ShouldBe(1f);

        network.SimulatePacketLoss(-0.5f);
        network.PacketLoss.ShouldBe(0f);
    }

    [Fact]
    public void SendUnreliable_WithPacketLoss_DropsMessages()
    {
        using var network = new MockNetworkContext();
        network.SimulatePacketLoss(0.5f); // 50% loss

        // Send 10 unreliable messages
        for (int i = 0; i < 10; i++)
        {
            network.SendTo("peer", new TestMessage(), reliable: false);
        }

        // With 50% loss and deterministic behavior, every other message should be dropped
        network.SentMessages.Count.ShouldBeLessThan(10);
    }

    [Fact]
    public void SendReliable_WithPacketLoss_DoesNotDropMessages()
    {
        using var network = new MockNetworkContext();
        network.SimulatePacketLoss(0.5f);

        for (int i = 0; i < 10; i++)
        {
            network.SendTo("peer", new TestMessage(), reliable: true);
        }

        network.SentMessages.Count.ShouldBe(10);
    }

    #endregion

    #region Update Tests

    [Fact]
    public void Update_IncrementsUpdateCount()
    {
        using var network = new MockNetworkContext();

        network.Update();
        network.Update();

        network.UpdateCount.ShouldBe(2);
    }

    #endregion

    #region Reset Tests

    [Fact]
    public void Reset_ClearsAllState()
    {
        using var network = new MockNetworkContext();
        network.Connect("server:7777");
        network.SimulateConnect();
        network.Send(new TestMessage());
        network.SimulateReceive(new TestMessage(), "peer");
        network.SimulateLatency(100f);

        network.Reset();

        network.ConnectionState.ShouldBe(NetworkConnectionState.Disconnected);
        network.SentMessages.ShouldBeEmpty();
        network.ReceiveQueueCount.ShouldBe(0);
        network.ConnectAttemptCount.ShouldBe(0);
        network.Latency.ShouldBe(0);
    }

    [Fact]
    public void ClearSentMessages_ClearsOnlyMessages()
    {
        using var network = new MockNetworkContext();
        network.Connect("server:7777");
        network.SimulateConnect();
        network.Send(new TestMessage());

        network.ClearSentMessages();

        network.SentMessages.ShouldBeEmpty();
        network.IsConnected.ShouldBeTrue(); // Connection preserved
    }

    [Fact]
    public void ClearReceiveQueue_ClearsOnlyQueue()
    {
        using var network = new MockNetworkContext();
        network.SimulateReceive(new TestMessage(), "peer");

        network.ClearReceiveQueue();

        network.ReceiveQueueCount.ShouldBe(0);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_ResetsState()
    {
        var network = new MockNetworkContext();
        network.Connect("server:7777");
        network.SimulateConnect();

        network.Dispose();

        network.ConnectionState.ShouldBe(NetworkConnectionState.Disconnected);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var network = new MockNetworkContext();

        Should.NotThrow(() =>
        {
            network.Dispose();
            network.Dispose();
        });
    }

    #endregion

    #region NetworkOptions Tests

    [Fact]
    public void NetworkOptions_DefaultValues()
    {
        var options = new NetworkOptions();

        options.SimulatedLatency.ShouldBe(0f);
        options.SimulatedLatencyVariance.ShouldBe(0f);
        options.SimulatedPacketLoss.ShouldBe(0f);
        options.SimulatedPacketDuplication.ShouldBe(0f);
        options.SimulatedPacketReordering.ShouldBe(0f);
        options.ConnectionTimeout.ShouldBe(5000f);
        options.AutoReconnect.ShouldBeFalse();
        options.ReconnectDelay.ShouldBe(1000f);
        options.MaxReconnectAttempts.ShouldBe(5);
    }

    [Fact]
    public void NetworkOptions_CanSetAllProperties()
    {
        var options = new NetworkOptions
        {
            SimulatedLatency = 100f,
            SimulatedLatencyVariance = 20f,
            SimulatedPacketLoss = 0.1f,
            SimulatedPacketDuplication = 0.05f,
            SimulatedPacketReordering = 0.02f,
            ConnectionTimeout = 10000f,
            AutoReconnect = true,
            ReconnectDelay = 2000f,
            MaxReconnectAttempts = 10
        };

        options.SimulatedLatency.ShouldBe(100f);
        options.SimulatedLatencyVariance.ShouldBe(20f);
        options.SimulatedPacketLoss.ShouldBe(0.1f);
        options.SimulatedPacketDuplication.ShouldBe(0.05f);
        options.SimulatedPacketReordering.ShouldBe(0.02f);
        options.ConnectionTimeout.ShouldBe(10000f);
        options.AutoReconnect.ShouldBeTrue();
        options.ReconnectDelay.ShouldBe(2000f);
        options.MaxReconnectAttempts.ShouldBe(10);
    }

    [Fact]
    public void MockNetworkContext_Options_IsAccessible()
    {
        using var network = new MockNetworkContext();

        network.Options.ShouldNotBeNull();
        network.Options.ShouldBeOfType<NetworkOptions>();
    }

    #endregion

    #region SentMessage Tests

    [Fact]
    public void SentMessage_StoresAllProperties()
    {
        using var network = new MockNetworkContext();
        network.Connect("server:7777");
        network.SimulateConnect();
        var message = new TestMessage { Value = 42 };

        network.Send(message, reliable: false, channel: 3);

        var sent = network.SentMessages[0];
        sent.Endpoint.ShouldBe("server:7777");
        sent.Data.ShouldBe(message);
        sent.DataType.ShouldBe(typeof(TestMessage));
        sent.Reliable.ShouldBeFalse();
        sent.Channel.ShouldBe(3);
        sent.Timestamp.ShouldNotBe(default);
    }

    [Fact]
    public void SentMessage_RecordEquality()
    {
        var timestamp = DateTime.UtcNow;
        var data = new TestMessage { Value = 1 };

        var msg1 = new SentMessage("endpoint", data, typeof(TestMessage), true, 0, timestamp);
        var msg2 = new SentMessage("endpoint", data, typeof(TestMessage), true, 0, timestamp);

        msg1.ShouldBe(msg2);
        msg1.GetHashCode().ShouldBe(msg2.GetHashCode());
    }

    [Fact]
    public void SentMessage_DifferentEndpoint_NotEqual()
    {
        var timestamp = DateTime.UtcNow;
        var data = new TestMessage { Value = 1 };

        var msg1 = new SentMessage("endpoint1", data, typeof(TestMessage), true, 0, timestamp);
        var msg2 = new SentMessage("endpoint2", data, typeof(TestMessage), true, 0, timestamp);

        msg1.ShouldNotBe(msg2);
    }

    #endregion

    #region Test Helper Types

    private sealed class TestMessage
    {
        public int Value { get; set; }
    }

    private sealed class OtherMessage
    {
        public string? Text { get; set; }
    }

    #endregion
}
