namespace KeenEyes.Network.Tests;

/// <summary>
/// Tests for network configuration classes.
/// </summary>
public class NetworkConfigTests
{
    #region NetworkPluginConfig Tests

    [Fact]
    public void NetworkPluginConfig_DefaultValues_AreCorrect()
    {
        var config = new NetworkPluginConfig();

        Assert.Equal(30, config.TickRate);
        Assert.Equal(100f, config.InterpolationDelayMs);
        Assert.Equal(10, config.MaxPredictionTicks);
        Assert.Equal(32, config.SnapshotBufferSize);
        Assert.True(config.EnableBandwidthLimiting);
        Assert.Equal(64 * 1024, config.MaxBandwidthBytesPerSecond);
        Assert.Null(config.Serializer);
        Assert.Null(config.Interpolator);
    }

    [Fact]
    public void NetworkPluginConfig_PropertiesCanBeSet()
    {
        var config = new NetworkPluginConfig
        {
            TickRate = 60,
            InterpolationDelayMs = 50f,
            MaxPredictionTicks = 20,
            SnapshotBufferSize = 64,
            EnableBandwidthLimiting = false,
            MaxBandwidthBytesPerSecond = 128 * 1024
        };

        Assert.Equal(60, config.TickRate);
        Assert.Equal(50f, config.InterpolationDelayMs);
        Assert.Equal(20, config.MaxPredictionTicks);
        Assert.Equal(64, config.SnapshotBufferSize);
        Assert.False(config.EnableBandwidthLimiting);
        Assert.Equal(128 * 1024, config.MaxBandwidthBytesPerSecond);
    }

    [Fact]
    public void NetworkPluginConfig_RecordEquality_Works()
    {
        var config1 = new NetworkPluginConfig { TickRate = 30 };
        var config2 = new NetworkPluginConfig { TickRate = 30 };
        var config3 = new NetworkPluginConfig { TickRate = 60 };

        Assert.Equal(config1, config2);
        Assert.NotEqual(config1, config3);
    }

    #endregion

    #region ServerNetworkConfig Tests

    [Fact]
    public void ServerNetworkConfig_DefaultValues_AreCorrect()
    {
        var config = new ServerNetworkConfig();

        // Inherited defaults
        Assert.Equal(30, config.TickRate);
        Assert.Equal(100f, config.InterpolationDelayMs);

        // Server-specific defaults
        Assert.Equal(7777, config.Port);
        Assert.Equal(16, config.MaxClients);
    }

    [Fact]
    public void ServerNetworkConfig_PropertiesCanBeSet()
    {
        var config = new ServerNetworkConfig
        {
            Port = 8080,
            MaxClients = 32,
            TickRate = 60
        };

        Assert.Equal(8080, config.Port);
        Assert.Equal(32, config.MaxClients);
        Assert.Equal(60, config.TickRate);
    }

    [Fact]
    public void ServerNetworkConfig_InheritsFromNetworkPluginConfig()
    {
        var config = new ServerNetworkConfig();

        Assert.IsAssignableFrom<NetworkPluginConfig>(config);
    }

    #endregion

    #region ClientNetworkConfig Tests

    [Fact]
    public void ClientNetworkConfig_DefaultValues_AreCorrect()
    {
        var config = new ClientNetworkConfig();

        // Inherited defaults
        Assert.Equal(30, config.TickRate);
        Assert.Equal(100f, config.InterpolationDelayMs);

        // Client-specific defaults
        Assert.Equal("127.0.0.1", config.ServerAddress);
        Assert.Equal(7777, config.ServerPort);
        Assert.True(config.EnablePrediction);
        Assert.Null(config.InputSerializer);
        Assert.Equal(64, config.InputBufferSize);
        Assert.Equal(0.01f, config.MispredictionThreshold);
        Assert.Null(config.InputApplicator);
    }

    [Fact]
    public void ClientNetworkConfig_PropertiesCanBeSet()
    {
        var config = new ClientNetworkConfig
        {
            ServerAddress = "192.168.1.1",
            ServerPort = 8080,
            EnablePrediction = false,
            InputBufferSize = 128,
            MispredictionThreshold = 0.1f,
            TickRate = 60
        };

        Assert.Equal("192.168.1.1", config.ServerAddress);
        Assert.Equal(8080, config.ServerPort);
        Assert.False(config.EnablePrediction);
        Assert.Equal(128, config.InputBufferSize);
        Assert.Equal(0.1f, config.MispredictionThreshold);
        Assert.Equal(60, config.TickRate);
    }

    [Fact]
    public void ClientNetworkConfig_InputApplicator_CanBeSet()
    {
        var appliedInputs = new List<(Entity, object)>();

        var config = new ClientNetworkConfig
        {
            InputApplicator = (entity, input) => appliedInputs.Add((entity, input))
        };

        Assert.NotNull(config.InputApplicator);

        // Invoke the applicator
        var testEntity = new Entity(1, 1);
        config.InputApplicator(testEntity, "test input");

        Assert.Single(appliedInputs);
        Assert.Equal(testEntity, appliedInputs[0].Item1);
        Assert.Equal("test input", appliedInputs[0].Item2);
    }

    [Fact]
    public void ClientNetworkConfig_InheritsFromNetworkPluginConfig()
    {
        var config = new ClientNetworkConfig();

        Assert.IsAssignableFrom<NetworkPluginConfig>(config);
    }

    #endregion
}
