using KeenEyes.Network.Protocol;

namespace KeenEyes.Network.Tests;

/// <summary>
/// Tests for network message protocol (writer/reader).
/// </summary>
public class NetworkMessageProtocolTests
{
    #region Header Tests

    [Fact]
    public void WriteHeader_ReadHeader_RoundTrip()
    {
        Span<byte> buffer = stackalloc byte[64];
        var writer = new NetworkMessageWriter(buffer);

        writer.WriteHeader(MessageType.EntitySpawn, 12345);

        var reader = new NetworkMessageReader(writer.GetWrittenSpan());
        reader.ReadHeader(out var type, out var tick);

        Assert.Equal(MessageType.EntitySpawn, type);
        Assert.Equal(12345u, tick);
    }

    [Theory]
    [InlineData(MessageType.None, 0u)]
    [InlineData(MessageType.ConnectionRequest, 1u)]
    [InlineData(MessageType.ConnectionAccepted, 100u)]
    [InlineData(MessageType.Ping, uint.MaxValue)]
    [InlineData(MessageType.EntitySpawn, 999999u)]
    public void WriteHeader_ReadHeader_AllMessageTypes(MessageType expectedType, uint expectedTick)
    {
        Span<byte> buffer = stackalloc byte[64];
        var writer = new NetworkMessageWriter(buffer);

        writer.WriteHeader(expectedType, expectedTick);

        var reader = new NetworkMessageReader(writer.GetWrittenSpan());
        reader.ReadHeader(out var type, out var tick);

        Assert.Equal(expectedType, type);
        Assert.Equal(expectedTick, tick);
    }

    #endregion

    [Fact]
    public void WriteEntitySpawn_ReadEntitySpawn_RoundTrip()
    {
        Span<byte> buffer = stackalloc byte[64];
        var writer = new NetworkMessageWriter(buffer);

        writer.WriteHeader(MessageType.EntitySpawn, 100);
        writer.WriteEntitySpawn(42, 5);

        var reader = new NetworkMessageReader(writer.GetWrittenSpan());
        reader.ReadHeader(out _, out _);
        reader.ReadEntitySpawn(out var networkId, out var ownerId);

        Assert.Equal(42u, networkId);
        Assert.Equal(5, ownerId);
    }

    [Fact]
    public void WriteEntityDespawn_ReadEntityDespawn_RoundTrip()
    {
        Span<byte> buffer = stackalloc byte[64];
        var writer = new NetworkMessageWriter(buffer);

        writer.WriteHeader(MessageType.EntityDespawn, 100);
        writer.WriteEntityDespawn(42);

        var reader = new NetworkMessageReader(writer.GetWrittenSpan());
        reader.ReadHeader(out _, out _);
        reader.ReadEntityDespawn(out var networkId);

        Assert.Equal(42u, networkId);
    }

    [Fact]
    public void WriteComponentTypeId_ReadComponentTypeId_RoundTrip()
    {
        Span<byte> buffer = stackalloc byte[64];
        var writer = new NetworkMessageWriter(buffer);

        writer.WriteComponentTypeId(1234);

        var reader = new NetworkMessageReader(writer.GetWrittenSpan());
        var typeId = reader.ReadComponentTypeId();

        Assert.Equal(1234, typeId);
    }

    [Fact]
    public void BytesWritten_ReturnsCorrectCount()
    {
        Span<byte> buffer = stackalloc byte[64];
        var writer = new NetworkMessageWriter(buffer);

        writer.WriteHeader(MessageType.Ping, 0);

        Assert.True(writer.BytesWritten > 0);
    }

    [Fact]
    public void ToArray_ReturnsNewArray()
    {
        Span<byte> buffer = stackalloc byte[64];
        var writer = new NetworkMessageWriter(buffer);

        writer.WriteHeader(MessageType.Pong, 0);

        var array = writer.ToArray();
        Assert.NotNull(array);
        Assert.True(array.Length > 0);
    }

    [Fact]
    public void MessageType_AllTypesHaveDistinctValues()
    {
        var values = Enum.GetValues<MessageType>();
        var distinctValues = values.Distinct().ToList();

        Assert.Equal(values.Length, distinctValues.Count);
    }

    [Fact]
    public void WriteSignedBits_NegativeClientId_RoundTrips()
    {
        Span<byte> buffer = stackalloc byte[64];
        var writer = new NetworkMessageWriter(buffer);

        writer.WriteSignedBits(-1, 16);

        var reader = new NetworkMessageReader(writer.GetWrittenSpan());
        var result = reader.ReadSignedBits(16);

        Assert.Equal(-1, result);
    }

    #region Entity Spawn/Despawn Tests

    [Theory]
    [InlineData(0u, 0)]
    [InlineData(1u, 1)]
    [InlineData(1000u, 5)]
    [InlineData(uint.MaxValue, -1)]
    public void WriteEntitySpawn_ReadEntitySpawn_VariousValues(uint networkId, int ownerId)
    {
        Span<byte> buffer = stackalloc byte[64];
        var writer = new NetworkMessageWriter(buffer);

        writer.WriteHeader(MessageType.EntitySpawn, 100);
        writer.WriteEntitySpawn(networkId, ownerId);

        var reader = new NetworkMessageReader(writer.GetWrittenSpan());
        reader.ReadHeader(out _, out _);
        reader.ReadEntitySpawn(out var actualNetworkId, out var actualOwnerId);

        Assert.Equal(networkId, actualNetworkId);
        Assert.Equal(ownerId, actualOwnerId);
    }

    [Theory]
    [InlineData(0u)]
    [InlineData(1u)]
    [InlineData(1000u)]
    [InlineData(uint.MaxValue)]
    public void WriteEntityDespawn_ReadEntityDespawn_VariousValues(uint networkId)
    {
        Span<byte> buffer = stackalloc byte[64];
        var writer = new NetworkMessageWriter(buffer);

        writer.WriteHeader(MessageType.EntityDespawn, 100);
        writer.WriteEntityDespawn(networkId);

        var reader = new NetworkMessageReader(writer.GetWrittenSpan());
        reader.ReadHeader(out _, out _);
        reader.ReadEntityDespawn(out var actualNetworkId);

        Assert.Equal(networkId, actualNetworkId);
    }

    #endregion

    #region Reader Properties Tests

    [Fact]
    public void IsAtEnd_OnEmptyReader_ReturnsTrue()
    {
        var reader = new NetworkMessageReader(ReadOnlySpan<byte>.Empty);
        Assert.True(reader.IsAtEnd);
    }

    [Fact]
    public void IsAtEnd_AfterReadingAllData_ReturnsTrue()
    {
        Span<byte> buffer = stackalloc byte[16];
        var writer = new NetworkMessageWriter(buffer);
        writer.WriteHeader(MessageType.Ping, 0);

        var reader = new NetworkMessageReader(writer.GetWrittenSpan());
        reader.ReadHeader(out _, out _);

        Assert.True(reader.IsAtEnd);
    }

    [Fact]
    public void IsAtEnd_BeforeReadingAllData_ReturnsFalse()
    {
        Span<byte> buffer = stackalloc byte[64];
        var writer = new NetworkMessageWriter(buffer);
        writer.WriteHeader(MessageType.EntitySpawn, 100);
        writer.WriteEntitySpawn(1, 2);

        var reader = new NetworkMessageReader(writer.GetWrittenSpan());
        reader.ReadHeader(out _, out _);

        Assert.False(reader.IsAtEnd);
    }

    [Fact]
    public void BitsRemaining_ReturnsCorrectValue()
    {
        Span<byte> buffer = stackalloc byte[16];
        var writer = new NetworkMessageWriter(buffer);
        writer.WriteHeader(MessageType.Ping, 0);

        var reader = new NetworkMessageReader(writer.GetWrittenSpan());
        var initialBits = reader.BitsRemaining;

        reader.ReadHeader(out _, out _);

        Assert.True(initialBits > reader.BitsRemaining);
        Assert.Equal(0, reader.BitsRemaining);
    }

    [Fact]
    public void PeekMessageType_ReturnsMessageType()
    {
        Span<byte> buffer = stackalloc byte[16];
        var writer = new NetworkMessageWriter(buffer);
        writer.WriteHeader(MessageType.EntitySpawn, 42);

        var reader = new NetworkMessageReader(writer.GetWrittenSpan());
        var peekedType = reader.PeekMessageType();

        Assert.Equal(MessageType.EntitySpawn, peekedType);
    }

    #endregion

    #region Writer Properties Tests

    [Fact]
    public void BytesWritten_InitiallyZero()
    {
        Span<byte> buffer = stackalloc byte[64];
        var writer = new NetworkMessageWriter(buffer);

        Assert.Equal(0, writer.BytesWritten);
    }

    [Fact]
    public void BytesWritten_IncreasesAfterWriting()
    {
        Span<byte> buffer = stackalloc byte[64];
        var writer = new NetworkMessageWriter(buffer);

        writer.WriteHeader(MessageType.Ping, 0);
        var afterHeader = writer.BytesWritten;

        writer.WriteUInt32(12345);
        var afterUint = writer.BytesWritten;

        Assert.True(afterHeader > 0);
        Assert.True(afterUint > afterHeader);
    }

    [Fact]
    public void GetWrittenSpan_ReturnsCorrectLength()
    {
        Span<byte> buffer = stackalloc byte[64];
        var writer = new NetworkMessageWriter(buffer);

        writer.WriteHeader(MessageType.Pong, 42);

        var span = writer.GetWrittenSpan();
        Assert.Equal(writer.BytesWritten, span.Length);
    }

    [Fact]
    public void ToArray_ReturnsCorrectLength()
    {
        Span<byte> buffer = stackalloc byte[64];
        var writer = new NetworkMessageWriter(buffer);

        writer.WriteHeader(MessageType.Ping, 0);

        var array = writer.ToArray();
        Assert.Equal(writer.BytesWritten, array.Length);
    }

    [Fact]
    public void ToArray_ReturnsCopyOfData()
    {
        Span<byte> buffer = stackalloc byte[64];
        var writer = new NetworkMessageWriter(buffer);

        writer.WriteHeader(MessageType.EntitySpawn, 100);
        writer.WriteEntitySpawn(42, 5);

        var array = writer.ToArray();

        // Verify by reading from the array
        var reader = new NetworkMessageReader(array);
        reader.ReadHeader(out var type, out var tick);
        reader.ReadEntitySpawn(out var networkId, out var ownerId);

        Assert.Equal(MessageType.EntitySpawn, type);
        Assert.Equal(100u, tick);
        Assert.Equal(42u, networkId);
        Assert.Equal(5, ownerId);
    }

    #endregion

    #region WriteUInt32 Tests

    [Theory]
    [InlineData(0u)]
    [InlineData(1u)]
    [InlineData(255u)]
    [InlineData(65535u)]
    [InlineData(uint.MaxValue)]
    public void WriteUInt32_ReadAfterHeader_RoundTrips(uint value)
    {
        Span<byte> buffer = stackalloc byte[64];
        var writer = new NetworkMessageWriter(buffer);

        writer.WriteHeader(MessageType.ComponentUpdate, 0);
        writer.WriteUInt32(value);

        var reader = new NetworkMessageReader(writer.GetWrittenSpan());
        reader.ReadHeader(out _, out _);
        reader.ReadEntityDespawn(out var readValue); // Uses ReadUInt32 internally

        Assert.Equal(value, readValue);
    }

    #endregion

    #region Component Type ID Tests

    [Theory]
    [InlineData((ushort)0)]
    [InlineData((ushort)1)]
    [InlineData((ushort)255)]
    [InlineData((ushort)1234)]
    [InlineData(ushort.MaxValue)]
    public void WriteComponentTypeId_ReadComponentTypeId_RoundTrips(ushort componentTypeId)
    {
        Span<byte> buffer = stackalloc byte[64];
        var writer = new NetworkMessageWriter(buffer);

        writer.WriteComponentTypeId(componentTypeId);

        var reader = new NetworkMessageReader(writer.GetWrittenSpan());
        var result = reader.ReadComponentTypeId();

        Assert.Equal(componentTypeId, result);
    }

    #endregion

    #region Complex Message Tests

    [Fact]
    public void CompleteEntitySpawnMessage_RoundTrips()
    {
        Span<byte> buffer = stackalloc byte[128];
        var writer = new NetworkMessageWriter(buffer);

        // Write a complete entity spawn message
        writer.WriteHeader(MessageType.EntitySpawn, 12345);
        writer.WriteEntitySpawn(999, 3);
        writer.WriteComponentTypeId(42);
        writer.WriteUInt32(100);

        var reader = new NetworkMessageReader(writer.GetWrittenSpan());

        reader.ReadHeader(out var type, out var tick);
        Assert.Equal(MessageType.EntitySpawn, type);
        Assert.Equal(12345u, tick);

        reader.ReadEntitySpawn(out var networkId, out var ownerId);
        Assert.Equal(999u, networkId);
        Assert.Equal(3, ownerId);

        var componentType = reader.ReadComponentTypeId();
        Assert.Equal(42, componentType);

        reader.ReadEntityDespawn(out var extraData);
        Assert.Equal(100u, extraData);

        Assert.True(reader.IsAtEnd);
    }

    [Fact]
    public void MultipleMessages_InSequence_AllRoundTrip()
    {
        Span<byte> buffer = stackalloc byte[256];
        var writer = new NetworkMessageWriter(buffer);

        // Write multiple messages
        writer.WriteHeader(MessageType.Ping, 1);
        var pingEnd = writer.BytesWritten;

        Span<byte> buffer2 = stackalloc byte[256];
        var writer2 = new NetworkMessageWriter(buffer2);
        writer2.WriteHeader(MessageType.EntitySpawn, 2);
        writer2.WriteEntitySpawn(42, 1);

        // Read first message
        var reader1 = new NetworkMessageReader(writer.GetWrittenSpan());
        reader1.ReadHeader(out var type1, out var tick1);
        Assert.Equal(MessageType.Ping, type1);
        Assert.Equal(1u, tick1);

        // Read second message
        var reader2 = new NetworkMessageReader(writer2.GetWrittenSpan());
        reader2.ReadHeader(out var type2, out var tick2);
        reader2.ReadEntitySpawn(out var netId, out var owner);
        Assert.Equal(MessageType.EntitySpawn, type2);
        Assert.Equal(2u, tick2);
        Assert.Equal(42u, netId);
        Assert.Equal(1, owner);
    }

    #endregion

    #region Signed Bits Tests

    [Theory]
    [InlineData(0, 8)]
    [InlineData(127, 8)]
    [InlineData(-128, 8)]
    [InlineData(0, 16)]
    [InlineData(32767, 16)]
    [InlineData(-32768, 16)]
    [InlineData(1000, 16)]
    [InlineData(-1000, 16)]
    public void WriteSignedBits_ReadSignedBits_VariousBitWidths(int value, int bits)
    {
        Span<byte> buffer = stackalloc byte[64];
        var writer = new NetworkMessageWriter(buffer);

        writer.WriteSignedBits(value, bits);

        var reader = new NetworkMessageReader(writer.GetWrittenSpan());
        var result = reader.ReadSignedBits(bits);

        Assert.Equal(value, result);
    }

    #endregion
}
