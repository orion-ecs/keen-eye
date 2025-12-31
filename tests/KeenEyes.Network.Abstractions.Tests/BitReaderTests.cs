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

    [Fact]
    public void ReadFloat_ReturnsWrittenValue()
    {
        Span<byte> buffer = stackalloc byte[16];
        var writer = new BitWriter(buffer);
        writer.WriteFloat(3.14159f);

        var reader = new BitReader(writer.GetWrittenSpan());
        var result = reader.ReadFloat();

        Assert.Equal(3.14159f, result);
    }

    [Fact]
    public void ReadQuantized_ReturnsApproximateValue()
    {
        Span<byte> buffer = stackalloc byte[16];
        var writer = new BitWriter(buffer);
        writer.WriteQuantized(50.5f, 0f, 100f, 0.1f);

        var reader = new BitReader(writer.GetWrittenSpan());
        var result = reader.ReadQuantized(0f, 100f, 0.1f);

        Assert.InRange(result, 50.4f, 50.6f);
    }

    [Fact]
    public void ReadBytes_IntoSpan_FillsCorrectly()
    {
        Span<byte> buffer = stackalloc byte[16];
        var writer = new BitWriter(buffer);
        writer.WriteByte(0x11);
        writer.WriteByte(0x22);
        writer.WriteByte(0x33);

        var reader = new BitReader(writer.GetWrittenSpan());
        Span<byte> result = stackalloc byte[3];
        reader.ReadBytes(result);

        Assert.Equal(0x11, result[0]);
        Assert.Equal(0x22, result[1]);
        Assert.Equal(0x33, result[2]);
    }

    [Fact]
    public void ReadBytes_ReturnsNewArray()
    {
        Span<byte> buffer = stackalloc byte[16];
        var writer = new BitWriter(buffer);
        writer.WriteByte(0xAA);
        writer.WriteByte(0xBB);

        var reader = new BitReader(writer.GetWrittenSpan());
        var result = reader.ReadBytes(2);

        Assert.Equal(2, result.Length);
        Assert.Equal(0xAA, result[0]);
        Assert.Equal(0xBB, result[1]);
    }

    [Fact]
    public void SkipBits_AdvancesPosition()
    {
        Span<byte> buffer = stackalloc byte[16];
        var writer = new BitWriter(buffer);
        writer.WriteByte(0xFF);
        writer.WriteByte(0x42);

        var reader = new BitReader(writer.GetWrittenSpan());
        reader.SkipBits(8);
        var result = reader.ReadByte();

        Assert.Equal(0x42, result);
    }

    [Fact]
    public void BytePosition_AfterReading_ReturnsCorrectValue()
    {
        byte[] data = [0x11, 0x22, 0x33];
        var reader = new BitReader(data);

        Assert.Equal(0, reader.BytePosition);

        reader.ReadByte();
        Assert.Equal(1, reader.BytePosition);

        reader.ReadByte();
        Assert.Equal(2, reader.BytePosition);
    }

    [Fact]
    public void BitOffset_AfterPartialRead_ReturnsCorrectValue()
    {
        byte[] data = [0xFF, 0xFF];
        var reader = new BitReader(data);

        Assert.Equal(0, reader.BitOffset);

        reader.ReadBits(3);
        Assert.Equal(3, reader.BitOffset);

        reader.ReadBits(5);
        Assert.Equal(0, reader.BitOffset); // Wrapped to next byte
    }

    [Fact]
    public void TotalBitsRead_TracksCorrectly()
    {
        byte[] data = [0xFF, 0xFF, 0xFF];
        var reader = new BitReader(data);

        Assert.Equal(0, reader.TotalBitsRead);

        reader.ReadBits(5);
        Assert.Equal(5, reader.TotalBitsRead);

        reader.ReadByte();
        Assert.Equal(13, reader.TotalBitsRead);
    }

    [Fact]
    public void ReadBits_ZeroBits_ThrowsArgumentOutOfRange()
    {
        byte[] data = [0xFF];
        var reader = new BitReader(data);

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => ReadBitsHelper(data, 0));
        Assert.Equal("bits", ex.ParamName);
    }

    [Fact]
    public void ReadBits_TooManyBits_ThrowsArgumentOutOfRange()
    {
        byte[] data = [0xFF];

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => ReadBitsHelper(data, 33));
        Assert.Equal("bits", ex.ParamName);
    }

    private static uint ReadBitsHelper(byte[] data, int bits)
    {
        var reader = new BitReader(data);
        return reader.ReadBits(bits);
    }

    [Fact]
    public void ReadSignedBits_PositiveValue_ReturnsCorrectly()
    {
        Span<byte> buffer = stackalloc byte[16];
        var writer = new BitWriter(buffer);
        writer.WriteSignedBits(42, 8);

        var reader = new BitReader(writer.GetWrittenSpan());
        var result = reader.ReadSignedBits(8);

        Assert.Equal(42, result);
    }

    [Fact]
    public void ReadQuantized_MinValue_ReturnsMin()
    {
        Span<byte> buffer = stackalloc byte[16];
        var writer = new BitWriter(buffer);
        writer.WriteQuantized(-100f, -100f, 100f, 1f);

        var reader = new BitReader(writer.GetWrittenSpan());
        var result = reader.ReadQuantized(-100f, 100f, 1f);

        Assert.Equal(-100f, result);
    }

    [Fact]
    public void ReadQuantized_MaxValue_ReturnsApproximateMax()
    {
        Span<byte> buffer = stackalloc byte[16];
        var writer = new BitWriter(buffer);
        writer.WriteQuantized(100f, -100f, 100f, 1f);

        var reader = new BitReader(writer.GetWrittenSpan());
        var result = reader.ReadQuantized(-100f, 100f, 1f);

        Assert.InRange(result, 99f, 101f);
    }
}
