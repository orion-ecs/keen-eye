using KeenEyes.Network.Components;
using KeenEyes.Network.Protocol;
using KeenEyes.Network.Serialization;
using KeenEyes.Network.Transport;

namespace KeenEyes.Network.Tests;

// LocalTransport completes synchronously, so Wait() is safe
#pragma warning disable xUnit1031 // Do not use blocking task operations
#pragma warning disable xUnit1051 // Use TestContext.Current.CancellationToken

/// <summary>
/// Test component for delta sync tests.
/// </summary>
public struct TestPosition : INetworkSerializable, INetworkDeltaSerializable<TestPosition>
{
    public float X;
    public float Y;
    public float Z;

    public readonly void NetworkSerialize(ref BitWriter writer)
    {
        writer.WriteFloat(X);
        writer.WriteFloat(Y);
        writer.WriteFloat(Z);
    }

    public void NetworkDeserialize(ref BitReader reader)
    {
        X = reader.ReadFloat();
        Y = reader.ReadFloat();
        Z = reader.ReadFloat();
    }

    public readonly uint GetDirtyMask(in TestPosition baseline)
    {
        uint mask = 0;
        if (MathF.Abs(X - baseline.X) > 0.0001f)
        {
            mask |= 1;
        }

        if (MathF.Abs(Y - baseline.Y) > 0.0001f)
        {
            mask |= 2;
        }

        if (MathF.Abs(Z - baseline.Z) > 0.0001f)
        {
            mask |= 4;
        }

        return mask;
    }

    public readonly void NetworkSerializeDelta(ref BitWriter writer, in TestPosition baseline, uint dirtyMask)
    {
        if ((dirtyMask & 1) != 0)
        {
            writer.WriteFloat(X);
        }

        if ((dirtyMask & 2) != 0)
        {
            writer.WriteFloat(Y);
        }

        if ((dirtyMask & 4) != 0)
        {
            writer.WriteFloat(Z);
        }
    }

    public readonly void NetworkDeserializeDelta(ref BitReader reader, ref TestPosition baseline, uint dirtyMask)
    {
        if ((dirtyMask & 1) != 0)
        {
            baseline.X = reader.ReadFloat();
        }

        if ((dirtyMask & 2) != 0)
        {
            baseline.Y = reader.ReadFloat();
        }

        if ((dirtyMask & 4) != 0)
        {
            baseline.Z = reader.ReadFloat();
        }
    }
}

/// <summary>
/// Test component for velocity.
/// </summary>
public struct TestVelocity : INetworkSerializable, INetworkDeltaSerializable<TestVelocity>
{
    public float VX;
    public float VY;

    public readonly void NetworkSerialize(ref BitWriter writer)
    {
        writer.WriteFloat(VX);
        writer.WriteFloat(VY);
    }

    public void NetworkDeserialize(ref BitReader reader)
    {
        VX = reader.ReadFloat();
        VY = reader.ReadFloat();
    }

    public readonly uint GetDirtyMask(in TestVelocity baseline)
    {
        uint mask = 0;
        if (MathF.Abs(VX - baseline.VX) > 0.0001f)
        {
            mask |= 1;
        }

        if (MathF.Abs(VY - baseline.VY) > 0.0001f)
        {
            mask |= 2;
        }

        return mask;
    }

    public readonly void NetworkSerializeDelta(ref BitWriter writer, in TestVelocity baseline, uint dirtyMask)
    {
        if ((dirtyMask & 1) != 0)
        {
            writer.WriteFloat(VX);
        }

        if ((dirtyMask & 2) != 0)
        {
            writer.WriteFloat(VY);
        }
    }

    public readonly void NetworkDeserializeDelta(ref BitReader reader, ref TestVelocity baseline, uint dirtyMask)
    {
        if ((dirtyMask & 1) != 0)
        {
            baseline.VX = reader.ReadFloat();
        }

        if ((dirtyMask & 2) != 0)
        {
            baseline.VY = reader.ReadFloat();
        }
    }
}

/// <summary>
/// Mock network serializer for testing delta sync.
/// </summary>
public sealed class MockNetworkSerializer : INetworkSerializer
{
    private readonly Dictionary<Type, ushort> typeToId = new()
    {
        [typeof(TestPosition)] = 1,
        [typeof(TestVelocity)] = 2,
    };

    private readonly Dictionary<ushort, Type> idToType = new()
    {
        [1] = typeof(TestPosition),
        [2] = typeof(TestVelocity),
    };

    public bool IsNetworkSerializable(Type type) => typeToId.ContainsKey(type);

    public ushort? GetNetworkTypeId(Type type) =>
        typeToId.TryGetValue(type, out var id) ? id : null;

    public Type? GetTypeFromNetworkId(ushort networkTypeId) =>
        idToType.TryGetValue(networkTypeId, out var type) ? type : null;

    public bool Serialize(Type type, object value, ref BitWriter writer)
    {
        if (!typeToId.ContainsKey(type))
        {
            return false;
        }

        if (type == typeof(TestPosition))
        {
            ((TestPosition)value).NetworkSerialize(ref writer);
            return true;
        }

        if (type == typeof(TestVelocity))
        {
            ((TestVelocity)value).NetworkSerialize(ref writer);
            return true;
        }

        return false;
    }

    public object? Deserialize(ushort networkTypeId, ref BitReader reader)
    {
        switch (networkTypeId)
        {
            case 1:
                var pos = new TestPosition();
                pos.NetworkDeserialize(ref reader);
                return pos;
            case 2:
                var vel = new TestVelocity();
                vel.NetworkDeserialize(ref reader);
                return vel;
            default:
                return null;
        }
    }

    public IEnumerable<Type> GetRegisteredTypes() => typeToId.Keys;

    public IEnumerable<NetworkComponentInfo> GetRegisteredComponentInfo()
    {
        yield return new NetworkComponentInfo
        {
            Type = typeof(TestPosition),
            NetworkTypeId = 1,
            Strategy = SyncStrategy.Authoritative,
            Frequency = 0,
            Priority = 128,
            SupportsInterpolation = true,
            SupportsPrediction = false,
            SupportsDelta = true,
        };
        yield return new NetworkComponentInfo
        {
            Type = typeof(TestVelocity),
            NetworkTypeId = 2,
            Strategy = SyncStrategy.Authoritative,
            Frequency = 0,
            Priority = 128,
            SupportsInterpolation = false,
            SupportsPrediction = false,
            SupportsDelta = true,
        };
    }

    public bool SupportsDelta(Type type) => type == typeof(TestPosition) || type == typeof(TestVelocity);

    public uint GetDirtyMask(Type type, object current, object baseline)
    {
        if (type == typeof(TestPosition))
        {
            return ((TestPosition)current).GetDirtyMask((TestPosition)baseline);
        }

        if (type == typeof(TestVelocity))
        {
            return ((TestVelocity)current).GetDirtyMask((TestVelocity)baseline);
        }

        return 0;
    }

    public bool SerializeDelta(Type type, object current, object baseline, ref BitWriter writer)
    {
        if (type == typeof(TestPosition))
        {
            var c = (TestPosition)current;
            var b = (TestPosition)baseline;
            var mask = c.GetDirtyMask(b);
            writer.WriteUInt32(mask);
            if (mask != 0)
            {
                c.NetworkSerializeDelta(ref writer, b, mask);
            }

            return true;
        }

        if (type == typeof(TestVelocity))
        {
            var c = (TestVelocity)current;
            var b = (TestVelocity)baseline;
            var mask = c.GetDirtyMask(b);
            writer.WriteUInt32(mask);
            if (mask != 0)
            {
                c.NetworkSerializeDelta(ref writer, b, mask);
            }

            return true;
        }

        return false;
    }

    public object? DeserializeDelta(ushort networkTypeId, ref BitReader reader, object baseline)
    {
        var mask = reader.ReadUInt32();
        if (mask == 0)
        {
            return baseline;
        }

        switch (networkTypeId)
        {
            case 1:
            {
                var b = (TestPosition)baseline;
                new TestPosition().NetworkDeserializeDelta(ref reader, ref b, mask);
                return b;
            }
            case 2:
            {
                var b = (TestVelocity)baseline;
                new TestVelocity().NetworkDeserializeDelta(ref reader, ref b, mask);
                return b;
            }
            default:
                return baseline;
        }
    }
}

/// <summary>
/// Tests for delta sync encoding and decoding.
/// </summary>
public sealed class DeltaSyncTests
{
    #region GetDirtyMask Tests

    [Fact]
    public void GetDirtyMask_NoChanges_ReturnsZero()
    {
        var current = new TestPosition { X = 1.0f, Y = 2.0f, Z = 3.0f };
        var baseline = new TestPosition { X = 1.0f, Y = 2.0f, Z = 3.0f };

        var mask = current.GetDirtyMask(baseline);

        Assert.Equal(0u, mask);
    }

    [Fact]
    public void GetDirtyMask_SingleFieldChanged_ReturnsBitForThatField()
    {
        var baseline = new TestPosition { X = 1.0f, Y = 2.0f, Z = 3.0f };

        // Only X changed
        var current1 = new TestPosition { X = 5.0f, Y = 2.0f, Z = 3.0f };
        Assert.Equal(1u, current1.GetDirtyMask(baseline));

        // Only Y changed
        var current2 = new TestPosition { X = 1.0f, Y = 10.0f, Z = 3.0f };
        Assert.Equal(2u, current2.GetDirtyMask(baseline));

        // Only Z changed
        var current3 = new TestPosition { X = 1.0f, Y = 2.0f, Z = 99.0f };
        Assert.Equal(4u, current3.GetDirtyMask(baseline));
    }

    [Fact]
    public void GetDirtyMask_MultipleFieldsChanged_ReturnsCombinedBits()
    {
        var baseline = new TestPosition { X = 0.0f, Y = 0.0f, Z = 0.0f };
        var current = new TestPosition { X = 1.0f, Y = 1.0f, Z = 0.0f }; // X and Y changed

        var mask = current.GetDirtyMask(baseline);

        Assert.Equal(3u, mask); // 0b011
    }

    [Fact]
    public void GetDirtyMask_AllFieldsChanged_ReturnsAllBits()
    {
        var baseline = new TestPosition { X = 0.0f, Y = 0.0f, Z = 0.0f };
        var current = new TestPosition { X = 1.0f, Y = 2.0f, Z = 3.0f };

        var mask = current.GetDirtyMask(baseline);

        Assert.Equal(7u, mask); // 0b111
    }

    [Fact]
    public void GetDirtyMask_TinyChange_IgnoredByEpsilon()
    {
        var baseline = new TestPosition { X = 1.0f, Y = 2.0f, Z = 3.0f };
        var current = new TestPosition { X = 1.00001f, Y = 2.0f, Z = 3.0f }; // Change less than epsilon

        var mask = current.GetDirtyMask(baseline);

        Assert.Equal(0u, mask); // Should ignore tiny changes
    }

    #endregion

    #region Delta Serialization Round-Trip Tests

    [Fact]
    public void SerializeDelta_DeserializeDelta_RoundTrip_SingleField()
    {
        var serializer = new MockNetworkSerializer();
        var baseline = new TestPosition { X = 1.0f, Y = 2.0f, Z = 3.0f };
        var current = new TestPosition { X = 10.0f, Y = 2.0f, Z = 3.0f }; // Only X changed

        // Serialize delta
        Span<byte> buffer = stackalloc byte[64];
        var writer = new BitWriter(buffer);
        var serialized = serializer.SerializeDelta(typeof(TestPosition), current, baseline, ref writer);
        Assert.True(serialized);

        // Deserialize delta
        var reader = new BitReader(writer.GetWrittenSpan());
        var result = serializer.DeserializeDelta(1, ref reader, baseline);

        Assert.NotNull(result);
        var deserialized = (TestPosition)result;
        Assert.Equal(10.0f, deserialized.X, 0.001f); // Changed value
        Assert.Equal(2.0f, deserialized.Y, 0.001f); // Unchanged
        Assert.Equal(3.0f, deserialized.Z, 0.001f); // Unchanged
    }

    [Fact]
    public void SerializeDelta_DeserializeDelta_RoundTrip_MultipleFields()
    {
        var serializer = new MockNetworkSerializer();
        var baseline = new TestPosition { X = 0.0f, Y = 0.0f, Z = 0.0f };
        var current = new TestPosition { X = 100.0f, Y = 200.0f, Z = 0.0f }; // X and Y changed

        // Serialize delta
        Span<byte> buffer = stackalloc byte[64];
        var writer = new BitWriter(buffer);
        serializer.SerializeDelta(typeof(TestPosition), current, baseline, ref writer);

        // Deserialize delta
        var reader = new BitReader(writer.GetWrittenSpan());
        var result = (TestPosition)serializer.DeserializeDelta(1, ref reader, baseline)!;

        Assert.Equal(100.0f, result.X, 0.001f);
        Assert.Equal(200.0f, result.Y, 0.001f);
        Assert.Equal(0.0f, result.Z, 0.001f);
    }

    [Fact]
    public void SerializeDelta_NoChanges_ReturnsBaseline()
    {
        var serializer = new MockNetworkSerializer();
        var baseline = new TestPosition { X = 5.0f, Y = 10.0f, Z = 15.0f };
        var current = new TestPosition { X = 5.0f, Y = 10.0f, Z = 15.0f }; // No changes

        // Serialize delta (mask = 0)
        Span<byte> buffer = stackalloc byte[64];
        var writer = new BitWriter(buffer);
        serializer.SerializeDelta(typeof(TestPosition), current, baseline, ref writer);

        // Deserialize delta
        var reader = new BitReader(writer.GetWrittenSpan());
        var result = serializer.DeserializeDelta(1, ref reader, baseline);

        // Should return baseline unchanged (boxed reference will be same object)
        Assert.NotNull(result);
        var pos = (TestPosition)result;
        Assert.Equal(baseline.X, pos.X);
        Assert.Equal(baseline.Y, pos.Y);
        Assert.Equal(baseline.Z, pos.Z);
    }

    [Fact]
    public void SerializeDelta_SmallerThanFullSerialization()
    {
        var serializer = new MockNetworkSerializer();
        var baseline = new TestPosition { X = 1.0f, Y = 2.0f, Z = 3.0f };
        var current = new TestPosition { X = 10.0f, Y = 2.0f, Z = 3.0f }; // Only X changed

        // Full serialization
        Span<byte> fullBuffer = stackalloc byte[64];
        var fullWriter = new BitWriter(fullBuffer);
        serializer.Serialize(typeof(TestPosition), current, ref fullWriter);
        var fullSize = fullWriter.BytesRequired;

        // Delta serialization
        Span<byte> deltaBuffer = stackalloc byte[64];
        var deltaWriter = new BitWriter(deltaBuffer);
        serializer.SerializeDelta(typeof(TestPosition), current, baseline, ref deltaWriter);
        var deltaSize = deltaWriter.BytesRequired;

        // Delta should be smaller (4 bytes mask + 4 bytes float vs 12 bytes for 3 floats)
        Assert.True(deltaSize < fullSize,
            $"Delta size ({deltaSize}) should be smaller than full size ({fullSize})");
    }

    #endregion

    #region NetworkMessageWriter/Reader Delta Tests

    [Fact]
    public void WriteComponentDelta_ReadComponentDelta_RoundTrip()
    {
        var serializer = new MockNetworkSerializer();
        var baseline = new TestPosition { X = 0.0f, Y = 0.0f, Z = 0.0f };
        var current = new TestPosition { X = 50.0f, Y = 0.0f, Z = 100.0f }; // X and Z changed

        // Write delta using NetworkMessageWriter
        Span<byte> buffer = stackalloc byte[128];
        var writer = new NetworkMessageWriter(buffer);
        writer.WriteHeader(MessageType.ComponentDelta, 42);
        writer.WriteUInt32(123); // network ID
        writer.WriteComponentCount(1);
        var written = writer.WriteComponentDelta(serializer, typeof(TestPosition), current, baseline);
        Assert.True(written);

        // Read delta using NetworkMessageReader
        var reader = new NetworkMessageReader(writer.GetWrittenSpan());
        reader.ReadHeader(out var msgType, out var tick);
        Assert.Equal(MessageType.ComponentDelta, msgType);
        Assert.Equal(42u, tick);

        var networkId = reader.ReadNetworkId();
        Assert.Equal(123u, networkId);

        var componentCount = reader.ReadComponentCount();
        Assert.Equal(1, componentCount);

        var result = reader.ReadComponentDelta(serializer, baseline, out var componentType);
        Assert.NotNull(result);
        Assert.Equal(typeof(TestPosition), componentType);

        var deserialized = (TestPosition)result;
        Assert.Equal(50.0f, deserialized.X, 0.001f);
        Assert.Equal(0.0f, deserialized.Y, 0.001f);
        Assert.Equal(100.0f, deserialized.Z, 0.001f);
    }

    [Fact]
    public void WriteComponentDelta_MultipleComponents_RoundTrip()
    {
        var serializer = new MockNetworkSerializer();

        var posBaseline = new TestPosition { X = 0.0f, Y = 0.0f, Z = 0.0f };
        var posCurrent = new TestPosition { X = 10.0f, Y = 0.0f, Z = 0.0f };

        var velBaseline = new TestVelocity { VX = 0.0f, VY = 0.0f };
        var velCurrent = new TestVelocity { VX = 5.0f, VY = 0.0f };

        // Write both components
        Span<byte> buffer = stackalloc byte[256];
        var writer = new NetworkMessageWriter(buffer);
        writer.WriteHeader(MessageType.ComponentDelta, 100);
        writer.WriteUInt32(1);
        writer.WriteComponentCount(2);
        writer.WriteComponentDelta(serializer, typeof(TestPosition), posCurrent, posBaseline);
        writer.WriteComponentDelta(serializer, typeof(TestVelocity), velCurrent, velBaseline);

        // Read both components
        var reader = new NetworkMessageReader(writer.GetWrittenSpan());
        reader.ReadHeader(out _, out _);
        reader.ReadNetworkId();
        var count = reader.ReadComponentCount();
        Assert.Equal(2, count);

        var pos = (TestPosition)reader.ReadComponentDelta(serializer, posBaseline, out _)!;
        Assert.Equal(10.0f, pos.X, 0.001f);

        var vel = (TestVelocity)reader.ReadComponentDelta(serializer, velBaseline, out _)!;
        Assert.Equal(5.0f, vel.VX, 0.001f);
    }

    #endregion
}

/// <summary>
/// Tests for hierarchy replication.
/// </summary>
public sealed class HierarchyReplicationTests
{
    #region WriteHierarchyChange/ReadHierarchyChange Tests

    [Fact]
    public void WriteHierarchyChange_ReadHierarchyChange_RoundTrip()
    {
        Span<byte> buffer = stackalloc byte[64];
        var writer = new NetworkMessageWriter(buffer);

        writer.WriteHeader(MessageType.HierarchyChange, 100);
        writer.WriteHierarchyChange(childNetworkId: 5, parentNetworkId: 3);

        var reader = new NetworkMessageReader(writer.GetWrittenSpan());
        reader.ReadHeader(out var msgType, out var tick);
        Assert.Equal(MessageType.HierarchyChange, msgType);
        Assert.Equal(100u, tick);

        reader.ReadHierarchyChange(out var childId, out var parentId);
        Assert.Equal(5u, childId);
        Assert.Equal(3u, parentId);
    }

    [Fact]
    public void WriteHierarchyChange_NoParent_UsesZero()
    {
        Span<byte> buffer = stackalloc byte[64];
        var writer = new NetworkMessageWriter(buffer);

        writer.WriteHeader(MessageType.HierarchyChange, 1);
        writer.WriteHierarchyChange(childNetworkId: 10, parentNetworkId: 0); // 0 = no parent

        var reader = new NetworkMessageReader(writer.GetWrittenSpan());
        reader.ReadHeader(out _, out _);
        reader.ReadHierarchyChange(out var childId, out var parentId);

        Assert.Equal(10u, childId);
        Assert.Equal(0u, parentId);
    }

    [Theory]
    [InlineData(1u, 2u)]
    [InlineData(uint.MaxValue, 0u)]
    [InlineData(100u, 99u)]
    [InlineData(0u, 1u)] // Edge case: network ID 0 as child (unusual but valid)
    public void WriteHierarchyChange_VariousIds_RoundTrip(uint childId, uint parentId)
    {
        Span<byte> buffer = stackalloc byte[64];
        var writer = new NetworkMessageWriter(buffer);

        writer.WriteHeader(MessageType.HierarchyChange, 0);
        writer.WriteHierarchyChange(childId, parentId);

        var reader = new NetworkMessageReader(writer.GetWrittenSpan());
        reader.ReadHeader(out _, out _);
        reader.ReadHierarchyChange(out var readChildId, out var readParentId);

        Assert.Equal(childId, readChildId);
        Assert.Equal(parentId, readParentId);
    }

    #endregion

    #region Server-Client Hierarchy Integration Tests

    [Fact]
    public async Task Server_SendHierarchyChange_Client_AppliesParent()
    {
        var (server, client) = LocalTransport.CreatePair();
        using var serverWorld = new World();
        using var clientWorld = new World();

        // Set up server
        var serverPlugin = new NetworkServerPlugin(server, new ServerNetworkConfig { TickRate = 60 });
        serverWorld.InstallPlugin(serverPlugin);

        // Set up client
        var clientPlugin = new NetworkClientPlugin(client);
        clientWorld.InstallPlugin(clientPlugin);

        // Connect
        await server.ListenAsync(7777);
        await client.ConnectAsync("localhost", 7777);
        server.Update();
        client.Update();

        // Create parent entity on server
        var serverParent = serverWorld.Spawn().Build();
        var parentNetId = serverPlugin.RegisterNetworkedEntity(serverParent);

        // Create child entity on server
        var serverChild = serverWorld.Spawn().Build();
        var childNetId = serverPlugin.RegisterNetworkedEntity(serverChild);

        // Run server tick to send spawn messages
        serverWorld.Update(0.02f);
        client.Update();

        // Spawn entities on client side
        Span<byte> spawnBuf1 = stackalloc byte[32];
        var sw1 = new NetworkMessageWriter(spawnBuf1);
        sw1.WriteHeader(MessageType.EntitySpawn, 1);
        sw1.WriteEntitySpawn(parentNetId.Value, 0);
        sw1.WriteComponentCount(0);
        server.SendToAll(sw1.GetWrittenSpan(), DeliveryMode.ReliableOrdered);

        Span<byte> spawnBuf2 = stackalloc byte[32];
        var sw2 = new NetworkMessageWriter(spawnBuf2);
        sw2.WriteHeader(MessageType.EntitySpawn, 2);
        sw2.WriteEntitySpawn(childNetId.Value, 0);
        sw2.WriteComponentCount(0);
        server.SendToAll(sw2.GetWrittenSpan(), DeliveryMode.ReliableOrdered);
        server.Update();
        client.Update();

        // Verify entities exist on client
        var clientParentExists = clientPlugin.NetworkIds.TryGetLocalEntity(parentNetId.Value, out var clientParent);
        var clientChildExists = clientPlugin.NetworkIds.TryGetLocalEntity(childNetId.Value, out var clientChild);
        Assert.True(clientParentExists, "Parent entity should exist on client");
        Assert.True(clientChildExists, "Child entity should exist on client");

        // Set parent on server
        serverWorld.SetParent(serverChild, serverParent);

        // Send hierarchy change message
        Span<byte> buffer = stackalloc byte[32];
        var writer = new NetworkMessageWriter(buffer);
        writer.WriteHeader(MessageType.HierarchyChange, 3);
        writer.WriteHierarchyChange(childNetId.Value, parentNetId.Value);
        server.SendToAll(writer.GetWrittenSpan(), DeliveryMode.ReliableOrdered);
        server.Update();
        client.Update();

        // Verify parent was applied on client
        var actualParent = clientWorld.GetParent(clientChild);
        Assert.Equal(clientParent, actualParent);

        // Clean up
        serverWorld.UninstallPlugin("NetworkServer");
        clientWorld.UninstallPlugin("NetworkClient");
        client.Dispose();
        server.Dispose();
    }

    [Fact]
    public async Task Server_SendHierarchyChange_ClearParent_Client_Unparents()
    {
        var (server, client) = LocalTransport.CreatePair();
        using var serverWorld = new World();
        using var clientWorld = new World();

        // Set up plugins
        var serverPlugin = new NetworkServerPlugin(server, new ServerNetworkConfig { TickRate = 60 });
        serverWorld.InstallPlugin(serverPlugin);
        var clientPlugin = new NetworkClientPlugin(client);
        clientWorld.InstallPlugin(clientPlugin);

        // Connect
        await server.ListenAsync(7777);
        await client.ConnectAsync("localhost", 7777);
        server.Update();
        client.Update();

        // Create entities on server
        var serverParent = serverWorld.Spawn().Build();
        var parentNetId = serverPlugin.RegisterNetworkedEntity(serverParent);
        var serverChild = serverWorld.Spawn().Build();
        var childNetId = serverPlugin.RegisterNetworkedEntity(serverChild);
        serverWorld.SetParent(serverChild, serverParent);

        serverWorld.Update(0.02f);

        // Spawn entities on client by sending EntitySpawn messages (matching the passing test pattern)
        Span<byte> spawnBuf1 = stackalloc byte[32];
        var sw1 = new NetworkMessageWriter(spawnBuf1);
        sw1.WriteHeader(MessageType.EntitySpawn, 1);
        sw1.WriteEntitySpawn(parentNetId.Value, 0);
        sw1.WriteComponentCount(0);
        server.SendToAll(sw1.GetWrittenSpan(), DeliveryMode.ReliableOrdered);

        Span<byte> spawnBuf2 = stackalloc byte[32];
        var sw2 = new NetworkMessageWriter(spawnBuf2);
        sw2.WriteHeader(MessageType.EntitySpawn, 2);
        sw2.WriteEntitySpawn(childNetId.Value, 0);
        sw2.WriteComponentCount(0);
        server.SendToAll(sw2.GetWrittenSpan(), DeliveryMode.ReliableOrdered);
        server.Update();
        client.Update();

        // Verify entities exist on client
        var clientParentExists = clientPlugin.NetworkIds.TryGetLocalEntity(parentNetId.Value, out var clientParent);
        var clientChildExists = clientPlugin.NetworkIds.TryGetLocalEntity(childNetId.Value, out var clientChild);
        Assert.True(clientParentExists, "Parent entity should exist on client");
        Assert.True(clientChildExists, "Child entity should exist on client");

        // Set parent via hierarchy message
        Span<byte> setParentBuffer = stackalloc byte[32];
        var setParentWriter = new NetworkMessageWriter(setParentBuffer);
        setParentWriter.WriteHeader(MessageType.HierarchyChange, 3);
        setParentWriter.WriteHierarchyChange(childNetId.Value, parentNetId.Value);
        server.SendToAll(setParentWriter.GetWrittenSpan(), DeliveryMode.ReliableOrdered);
        server.Update();
        client.Update();

        Assert.Equal(clientParent, clientWorld.GetParent(clientChild));

        // Clear parent on server and send
        serverWorld.SetParent(serverChild, Entity.Null);

        Span<byte> buffer = stackalloc byte[32];
        var writer = new NetworkMessageWriter(buffer);
        writer.WriteHeader(MessageType.HierarchyChange, 4);
        writer.WriteHierarchyChange(childNetId.Value, 0); // 0 = no parent
        server.SendToAll(writer.GetWrittenSpan(), DeliveryMode.ReliableOrdered);
        server.Update();
        client.Update();

        // Verify parent was cleared on client
        var actualParent = clientWorld.GetParent(clientChild);
        Assert.Equal(Entity.Null, actualParent);

        // Clean up
        serverWorld.UninstallPlugin("NetworkServer");
        clientWorld.UninstallPlugin("NetworkClient");
        client.Dispose();
        server.Dispose();
    }

    #endregion

    #region SendHierarchySnapshot Tests

    [Fact]
    public async Task SendFullSnapshot_IncludesHierarchyChanges()
    {
        var (server, client) = LocalTransport.CreatePair();
        using var serverWorld = new World();

        // Set up server with serializer
        var serializer = new MockNetworkSerializer();
        var serverPlugin = new NetworkServerPlugin(server, new ServerNetworkConfig
        {
            TickRate = 60,
            Serializer = serializer,
        });
        serverWorld.InstallPlugin(serverPlugin);

        // Create parent/child hierarchy before client connects
        var serverParent = serverWorld.Spawn().Build();
        var parentNetId = serverPlugin.RegisterNetworkedEntity(serverParent);
        var serverChild = serverWorld.Spawn().Build();
        var childNetId = serverPlugin.RegisterNetworkedEntity(serverChild);
        serverWorld.SetParent(serverChild, serverParent);

        // Track received hierarchy messages
        var receivedHierarchyChanges = new List<(uint childId, uint parentId)>();

        client.DataReceived += (_, data) =>
        {
            var reader = new NetworkMessageReader(data);
            reader.ReadHeader(out var msgType, out uint _);
            if (msgType == MessageType.HierarchyChange)
            {
                reader.ReadHierarchyChange(out var childId, out var parentId);
                receivedHierarchyChanges.Add((childId, parentId));
            }
        };

        // Connect and trigger full snapshot
        await server.ListenAsync(7777);
        await client.ConnectAsync("localhost", 7777);
        server.Update();
        client.Update();

        // Run server tick to send full snapshot (client marked as needing full snapshot)
        serverWorld.Update(0.02f);
        server.Update();
        client.Update();

        // Verify we received the hierarchy change
        Assert.Contains(receivedHierarchyChanges, h => h.childId == childNetId.Value && h.parentId == parentNetId.Value);

        serverWorld.UninstallPlugin("NetworkServer");
        server.Dispose();
        client.Dispose();
    }

    #endregion
}

/// <summary>
/// Tests for the MockNetworkSerializer itself.
/// </summary>
public sealed class MockNetworkSerializerTests
{
    [Fact]
    public void IsNetworkSerializable_RegisteredType_ReturnsTrue()
    {
        var serializer = new MockNetworkSerializer();

        Assert.True(serializer.IsNetworkSerializable(typeof(TestPosition)));
        Assert.True(serializer.IsNetworkSerializable(typeof(TestVelocity)));
    }

    [Fact]
    public void IsNetworkSerializable_UnregisteredType_ReturnsFalse()
    {
        var serializer = new MockNetworkSerializer();

        Assert.False(serializer.IsNetworkSerializable(typeof(int)));
        Assert.False(serializer.IsNetworkSerializable(typeof(string)));
    }

    [Fact]
    public void GetNetworkTypeId_RegisteredType_ReturnsId()
    {
        var serializer = new MockNetworkSerializer();

        Assert.Equal((ushort)1, serializer.GetNetworkTypeId(typeof(TestPosition)));
        Assert.Equal((ushort)2, serializer.GetNetworkTypeId(typeof(TestVelocity)));
    }

    [Fact]
    public void GetNetworkTypeId_UnregisteredType_ReturnsNull()
    {
        var serializer = new MockNetworkSerializer();

        Assert.Null(serializer.GetNetworkTypeId(typeof(int)));
    }

    [Fact]
    public void GetTypeFromNetworkId_ValidId_ReturnsType()
    {
        var serializer = new MockNetworkSerializer();

        Assert.Equal(typeof(TestPosition), serializer.GetTypeFromNetworkId(1));
        Assert.Equal(typeof(TestVelocity), serializer.GetTypeFromNetworkId(2));
    }

    [Fact]
    public void GetTypeFromNetworkId_InvalidId_ReturnsNull()
    {
        var serializer = new MockNetworkSerializer();

        Assert.Null(serializer.GetTypeFromNetworkId(99));
    }

    [Fact]
    public void Serialize_Deserialize_FullRoundTrip()
    {
        var serializer = new MockNetworkSerializer();
        var original = new TestPosition { X = 1.5f, Y = 2.5f, Z = 3.5f };

        Span<byte> buffer = stackalloc byte[64];
        var writer = new BitWriter(buffer);
        var result = serializer.Serialize(typeof(TestPosition), original, ref writer);
        Assert.True(result);

        var reader = new BitReader(writer.GetWrittenSpan());
        var deserialized = serializer.Deserialize(1, ref reader);

        Assert.NotNull(deserialized);
        var pos = (TestPosition)deserialized;
        Assert.Equal(1.5f, pos.X, 0.001f);
        Assert.Equal(2.5f, pos.Y, 0.001f);
        Assert.Equal(3.5f, pos.Z, 0.001f);
    }

    [Fact]
    public void SupportsDelta_DeltaType_ReturnsTrue()
    {
        var serializer = new MockNetworkSerializer();

        Assert.True(serializer.SupportsDelta(typeof(TestPosition)));
        Assert.True(serializer.SupportsDelta(typeof(TestVelocity)));
    }
}
