using KeenEyes.Network.Protocol;

namespace KeenEyes.Network.Tests;

/// <summary>
/// Tests for network message protocol (writer/reader).
/// </summary>
public class NetworkMessageProtocolTests
{
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
}
