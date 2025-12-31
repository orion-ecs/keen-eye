using KeenEyes.Network.Serialization;

namespace KeenEyes.Network.Abstractions.Tests;

/// <summary>
/// Tests for the <see cref="BitWriter"/> struct.
/// </summary>
public class BitWriterTests
{
    [Fact]
    public void WriteByte_WritesSingleByte()
    {
        Span<byte> buffer = stackalloc byte[16];
        var writer = new BitWriter(buffer);

        writer.WriteByte(0x42);

        Assert.Equal(1, writer.BytesRequired);
        var span = writer.GetWrittenSpan();
        Assert.Equal(0x42, span[0]);
    }

    [Fact]
    public void WriteUInt16_WritesTwoBytes()
    {
        Span<byte> buffer = stackalloc byte[16];
        var writer = new BitWriter(buffer);

        writer.WriteUInt16(0x1234);

        Assert.Equal(2, writer.BytesRequired);
    }

    [Fact]
    public void WriteUInt32_WritesFourBytes()
    {
        Span<byte> buffer = stackalloc byte[16];
        var writer = new BitWriter(buffer);

        writer.WriteUInt32(0x12345678);

        Assert.Equal(4, writer.BytesRequired);
    }

    [Fact]
    public void WriteBits_WritesPartialByte()
    {
        Span<byte> buffer = stackalloc byte[16];
        var writer = new BitWriter(buffer);

        writer.WriteBits(0b101, 3);

        Assert.Equal(1, writer.BytesRequired);
    }

    [Fact]
    public void WriteBits_MultipleCalls_PacksCorrectly()
    {
        Span<byte> buffer = stackalloc byte[16];
        var writer = new BitWriter(buffer);

        writer.WriteBits(0b1, 1);
        writer.WriteBits(0b0, 1);
        writer.WriteBits(0b1, 1);

        Assert.Equal(1, writer.BytesRequired);
    }

    [Fact]
    public void WriteSignedBits_NegativeValue_EncodesCorrectly()
    {
        Span<byte> buffer = stackalloc byte[16];
        var writer = new BitWriter(buffer);

        writer.WriteSignedBits(-1, 8);

        Assert.Equal(1, writer.BytesRequired);
    }

    [Fact]
    public void WriteBool_True_WritesSingleBit()
    {
        Span<byte> buffer = stackalloc byte[16];
        var writer = new BitWriter(buffer);

        writer.WriteBool(true);

        Assert.Equal(1, writer.BytesRequired);
    }

    [Fact]
    public void WriteBool_False_WritesSingleBit()
    {
        Span<byte> buffer = stackalloc byte[16];
        var writer = new BitWriter(buffer);

        writer.WriteBool(false);

        Assert.Equal(1, writer.BytesRequired);
    }

    [Fact]
    public void GetWrittenSpan_ReturnsCorrectLength()
    {
        Span<byte> buffer = stackalloc byte[16];
        var writer = new BitWriter(buffer);

        writer.WriteByte(1);
        writer.WriteByte(2);
        writer.WriteByte(3);

        var span = writer.GetWrittenSpan();
        Assert.Equal(3, span.Length);
    }

    [Fact]
    public void ToArray_ReturnsNewArray()
    {
        Span<byte> buffer = stackalloc byte[16];
        var writer = new BitWriter(buffer);

        writer.WriteUInt32(0x12345678);

        var array = writer.ToArray();
        Assert.Equal(4, array.Length);
    }

    [Fact]
    public void WriteFloat_WritesFullPrecision()
    {
        Span<byte> buffer = stackalloc byte[16];
        var writer = new BitWriter(buffer);

        writer.WriteFloat(3.14159f);

        Assert.Equal(4, writer.BytesRequired);
    }

    [Fact]
    public void WriteQuantized_ReturnsCorrectBitCount()
    {
        Span<byte> buffer = stackalloc byte[16];
        var writer = new BitWriter(buffer);

        // Range 0-100 with resolution 1 = 101 values = 7 bits
        var bitsWritten = writer.WriteQuantized(50f, 0f, 100f, 1f);

        Assert.Equal(7, bitsWritten);
    }

    [Fact]
    public void WriteQuantized_ClampsToRange()
    {
        Span<byte> buffer = stackalloc byte[16];
        var writer = new BitWriter(buffer);

        // Value outside range should be clamped
        writer.WriteQuantized(200f, 0f, 100f, 1f);

        var reader = new BitReader(writer.GetWrittenSpan());
        var result = reader.ReadQuantized(0f, 100f, 1f);

        Assert.InRange(result, 99f, 101f);
    }

    [Fact]
    public void WriteBytes_WritesAllBytes()
    {
        Span<byte> buffer = stackalloc byte[16];
        var writer = new BitWriter(buffer);

        byte[] data = [0x11, 0x22, 0x33, 0x44];
        writer.WriteBytes(data);

        Assert.Equal(4, writer.BytesRequired);
        var span = writer.GetWrittenSpan();
        Assert.Equal(0x11, span[0]);
        Assert.Equal(0x22, span[1]);
        Assert.Equal(0x33, span[2]);
        Assert.Equal(0x44, span[3]);
    }

    [Fact]
    public void BytePosition_TracksCorrectly()
    {
        Span<byte> buffer = stackalloc byte[16];
        var writer = new BitWriter(buffer);

        Assert.Equal(0, writer.BytePosition);

        writer.WriteByte(0x42);
        Assert.Equal(1, writer.BytePosition);

        writer.WriteByte(0x43);
        Assert.Equal(2, writer.BytePosition);
    }

    [Fact]
    public void BitOffset_TracksPartialBytes()
    {
        Span<byte> buffer = stackalloc byte[16];
        var writer = new BitWriter(buffer);

        Assert.Equal(0, writer.BitOffset);

        writer.WriteBits(0b111, 3);
        Assert.Equal(3, writer.BitOffset);

        writer.WriteBits(0b11111, 5);
        Assert.Equal(0, writer.BitOffset); // Wrapped to next byte
    }

    [Fact]
    public void TotalBitsWritten_TracksCorrectly()
    {
        Span<byte> buffer = stackalloc byte[16];
        var writer = new BitWriter(buffer);

        Assert.Equal(0, writer.TotalBitsWritten);

        writer.WriteBits(0b101, 3);
        Assert.Equal(3, writer.TotalBitsWritten);

        writer.WriteByte(0xFF);
        Assert.Equal(11, writer.TotalBitsWritten);
    }

    [Fact]
    public void WriteBits_ZeroBits_ThrowsArgumentOutOfRange()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => WriteBitsHelper(0, 0));
        Assert.Equal("bits", ex.ParamName);
    }

    [Fact]
    public void WriteBits_TooManyBits_ThrowsArgumentOutOfRange()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => WriteBitsHelper(0, 33));
        Assert.Equal("bits", ex.ParamName);
    }

    private static void WriteBitsHelper(uint value, int bits)
    {
        Span<byte> buffer = stackalloc byte[16];
        var writer = new BitWriter(buffer);
        writer.WriteBits(value, bits);
    }

    [Fact]
    public void BytesRequired_WithPartialByte_RoundsUp()
    {
        Span<byte> buffer = stackalloc byte[16];
        var writer = new BitWriter(buffer);

        writer.WriteBits(0b1, 1);

        Assert.Equal(1, writer.BytesRequired);
        Assert.Equal(0, writer.BytePosition);
        Assert.Equal(1, writer.BitOffset);
    }

    [Fact]
    public void WriteBits_32Bits_WritesCorrectly()
    {
        Span<byte> buffer = stackalloc byte[16];
        var writer = new BitWriter(buffer);

        writer.WriteBits(0xFFFFFFFF, 32);

        Assert.Equal(4, writer.BytesRequired);
        var span = writer.GetWrittenSpan();
        Assert.Equal(0xFF, span[0]);
        Assert.Equal(0xFF, span[1]);
        Assert.Equal(0xFF, span[2]);
        Assert.Equal(0xFF, span[3]);
    }

    [Fact]
    public void Constructor_ClearsBuffer()
    {
        Span<byte> buffer = stackalloc byte[4];
        buffer[0] = 0xFF;
        buffer[1] = 0xFF;

        var writer = new BitWriter(buffer);
        writer.WriteBits(0, 8);

        var span = writer.GetWrittenSpan();
        Assert.Equal(0, span[0]);
    }
}
