using KeenEyes.Network.Serialization;

namespace KeenEyes.Network.Abstractions.Tests;

/// <summary>
/// Tests for the <see cref="BitReader"/> struct.
/// </summary>
public class BitReaderTests
{
    [Fact]
    public void ReadByte_ReturnsWrittenByte()
    {
        byte[] data = [0x42];
        var reader = new BitReader(data);

        var result = reader.ReadByte();

        Assert.Equal(0x42, result);
    }

    [Fact]
    public void ReadUInt16_ReturnsWrittenValue()
    {
        Span<byte> buffer = stackalloc byte[16];
        var writer = new BitWriter(buffer);
        writer.WriteUInt16(0x1234);

        var reader = new BitReader(writer.GetWrittenSpan());
        var result = reader.ReadUInt16();

        Assert.Equal(0x1234, result);
    }

    [Fact]
    public void ReadUInt32_ReturnsWrittenValue()
    {
        Span<byte> buffer = stackalloc byte[16];
        var writer = new BitWriter(buffer);
        writer.WriteUInt32(0x12345678);

        var reader = new BitReader(writer.GetWrittenSpan());
        var result = reader.ReadUInt32();

        Assert.Equal(0x12345678u, result);
    }

    [Fact]
    public void ReadBits_ReturnsWrittenBits()
    {
        Span<byte> buffer = stackalloc byte[16];
        var writer = new BitWriter(buffer);
        writer.WriteBits(0b101, 3);

        var reader = new BitReader(writer.GetWrittenSpan());
        var result = reader.ReadBits(3);

        Assert.Equal(0b101u, result);
    }

    [Fact]
    public void ReadSignedBits_NegativeValue_ReturnsCorrectly()
    {
        Span<byte> buffer = stackalloc byte[16];
        var writer = new BitWriter(buffer);
        writer.WriteSignedBits(-5, 8);

        var reader = new BitReader(writer.GetWrittenSpan());
        var result = reader.ReadSignedBits(8);

        Assert.Equal(-5, result);
    }

    [Fact]
    public void ReadBool_True_ReturnsTrue()
    {
        Span<byte> buffer = stackalloc byte[16];
        var writer = new BitWriter(buffer);
        writer.WriteBool(true);

        var reader = new BitReader(writer.GetWrittenSpan());
        var result = reader.ReadBool();

        Assert.True(result);
    }

    [Fact]
    public void ReadBool_False_ReturnsFalse()
    {
        Span<byte> buffer = stackalloc byte[16];
        var writer = new BitWriter(buffer);
        writer.WriteBool(false);

        var reader = new BitReader(writer.GetWrittenSpan());
        var result = reader.ReadBool();

        Assert.False(result);
    }

    [Fact]
    public void IsAtEnd_EmptyBuffer_ReturnsTrue()
    {
        var reader = new BitReader(ReadOnlySpan<byte>.Empty);

        Assert.True(reader.IsAtEnd);
    }

    [Fact]
    public void IsAtEnd_AfterReadingAll_ReturnsTrue()
    {
        byte[] data = [0x42];
        var reader = new BitReader(data);

        reader.ReadByte();

        Assert.True(reader.IsAtEnd);
    }

    [Fact]
    public void BitsRemaining_ReturnsCorrectCount()
    {
        byte[] data = [0x42, 0x43];
        var reader = new BitReader(data);

        Assert.Equal(16, reader.BitsRemaining);

        reader.ReadByte();

        Assert.Equal(8, reader.BitsRemaining);
    }

    [Fact]
    public void MultipleValues_RoundTrip()
    {
        Span<byte> buffer = stackalloc byte[64];
        var writer = new BitWriter(buffer);

        writer.WriteByte(0x42);
        writer.WriteUInt16(0x1234);
        writer.WriteUInt32(0xDEADBEEF);
        writer.WriteBool(true);
        writer.WriteSignedBits(-100, 16);

        var reader = new BitReader(writer.GetWrittenSpan());

        Assert.Equal(0x42, reader.ReadByte());
        Assert.Equal(0x1234, reader.ReadUInt16());
        Assert.Equal(0xDEADBEEFu, reader.ReadUInt32());
        Assert.True(reader.ReadBool());
        Assert.Equal(-100, reader.ReadSignedBits(16));
    }
}
