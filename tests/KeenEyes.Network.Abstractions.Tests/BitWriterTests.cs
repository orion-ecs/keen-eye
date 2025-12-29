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
}
