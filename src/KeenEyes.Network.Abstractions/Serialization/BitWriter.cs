using System.Buffers;
using System.Runtime.CompilerServices;

namespace KeenEyes.Network.Serialization;

/// <summary>
/// A high-performance bit-level writer for network serialization.
/// </summary>
/// <remarks>
/// <para>
/// Unlike byte-aligned writers, BitWriter packs data at the bit level for maximum
/// bandwidth efficiency. This is critical for network code where every bit counts.
/// </para>
/// <para>
/// This type is a ref struct to avoid heap allocations in hot paths.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var buffer = new byte[256];
/// var writer = new BitWriter(buffer);
///
/// writer.WriteBits(42, 6);        // 6 bits for value 0-63
/// writer.WriteBool(true);          // 1 bit
/// writer.WriteQuantized(123.45f, -1000f, 1000f, 0.01f);  // 18 bits
///
/// var bytesWritten = writer.BytePosition + (writer.BitOffset > 0 ? 1 : 0);
/// </code>
/// </example>
public ref struct BitWriter
{
    private readonly Span<byte> buffer;
    private int bytePosition;
    private int bitOffset;

    /// <summary>
    /// Initializes a new instance of the <see cref="BitWriter"/> struct.
    /// </summary>
    /// <param name="buffer">The buffer to write to.</param>
    public BitWriter(Span<byte> buffer)
    {
        this.buffer = buffer;
        bytePosition = 0;
        bitOffset = 0;

        // Clear the buffer to ensure clean bit operations
        buffer.Clear();
    }

    /// <summary>
    /// Gets the current byte position in the buffer.
    /// </summary>
    public readonly int BytePosition => bytePosition;

    /// <summary>
    /// Gets the current bit offset within the current byte (0-7).
    /// </summary>
    public readonly int BitOffset => bitOffset;

    /// <summary>
    /// Gets the total number of bits written.
    /// </summary>
    public readonly int TotalBitsWritten => bytePosition * 8 + bitOffset;

    /// <summary>
    /// Gets the number of bytes required to hold all written bits.
    /// </summary>
    public readonly int BytesRequired => bytePosition + (bitOffset > 0 ? 1 : 0);

    /// <summary>
    /// Writes a single bit.
    /// </summary>
    /// <param name="value">The bit value (true = 1, false = 0).</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteBool(bool value)
    {
        if (value)
        {
            buffer[bytePosition] |= (byte)(1 << bitOffset);
        }

        AdvanceBits(1);
    }

    /// <summary>
    /// Writes an unsigned integer using the specified number of bits.
    /// </summary>
    /// <param name="value">The value to write.</param>
    /// <param name="bits">The number of bits to use (1-32).</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when bits is less than 1 or greater than 32.
    /// </exception>
    public void WriteBits(uint value, int bits)
    {
        if (bits < 1 || bits > 32)
        {
            throw new ArgumentOutOfRangeException(nameof(bits), "Bits must be between 1 and 32.");
        }

        // Mask to ensure we only write the specified bits
        var mask = bits == 32 ? uint.MaxValue : (1u << bits) - 1;
        value &= mask;

        var bitsRemaining = bits;
        while (bitsRemaining > 0)
        {
            var bitsAvailable = 8 - bitOffset;
            var bitsToWrite = Math.Min(bitsAvailable, bitsRemaining);

            // Extract the bits we're writing this iteration
            var fragment = (byte)(value & ((1u << bitsToWrite) - 1));
            buffer[bytePosition] |= (byte)(fragment << bitOffset);

            value >>= bitsToWrite;
            bitsRemaining -= bitsToWrite;
            AdvanceBits(bitsToWrite);
        }
    }

    /// <summary>
    /// Writes a signed integer using the specified number of bits.
    /// </summary>
    /// <param name="value">The value to write.</param>
    /// <param name="bits">The number of bits to use (1-32).</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteSignedBits(int value, int bits)
    {
        WriteBits(unchecked((uint)value), bits);
    }

    /// <summary>
    /// Writes a byte (8 bits).
    /// </summary>
    /// <param name="value">The byte value.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteByte(byte value)
    {
        WriteBits(value, 8);
    }

    /// <summary>
    /// Writes an unsigned 16-bit integer.
    /// </summary>
    /// <param name="value">The value to write.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUInt16(ushort value)
    {
        WriteBits(value, 16);
    }

    /// <summary>
    /// Writes an unsigned 32-bit integer.
    /// </summary>
    /// <param name="value">The value to write.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUInt32(uint value)
    {
        WriteBits(value, 32);
    }

    /// <summary>
    /// Writes a 32-bit float without quantization.
    /// </summary>
    /// <param name="value">The float value.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteFloat(float value)
    {
        WriteBits(BitConverter.SingleToUInt32Bits(value), 32);
    }

    /// <summary>
    /// Writes a quantized float value using minimal bits.
    /// </summary>
    /// <param name="value">The value to write.</param>
    /// <param name="min">The minimum value of the range.</param>
    /// <param name="max">The maximum value of the range.</param>
    /// <param name="resolution">The precision/step size.</param>
    /// <returns>The number of bits written.</returns>
    public int WriteQuantized(float value, float min, float max, float resolution)
    {
        // Clamp to range
        value = Math.Clamp(value, min, max);

        // Convert to integer steps
        var normalizedValue = (value - min) / resolution;
        var quantized = (uint)Math.Round(normalizedValue);

        // Calculate bits needed
        var range = max - min;
        var maxSteps = (uint)Math.Ceiling(range / resolution);
        var bits = (int)Math.Ceiling(Math.Log2(maxSteps + 1));

        WriteBits(quantized, bits);
        return bits;
    }

    /// <summary>
    /// Writes bytes from a span.
    /// </summary>
    /// <param name="data">The bytes to write.</param>
    public void WriteBytes(ReadOnlySpan<byte> data)
    {
        foreach (var b in data)
        {
            WriteByte(b);
        }
    }

    /// <summary>
    /// Gets the written data as a span.
    /// </summary>
    /// <returns>A span containing all written bytes.</returns>
    public readonly ReadOnlySpan<byte> GetWrittenSpan()
    {
        return buffer[..BytesRequired];
    }

    /// <summary>
    /// Copies the written data to a new byte array.
    /// </summary>
    /// <returns>A new byte array containing the written data.</returns>
    public readonly byte[] ToArray()
    {
        return GetWrittenSpan().ToArray();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AdvanceBits(int bits)
    {
        bitOffset += bits;
        while (bitOffset >= 8)
        {
            bitOffset -= 8;
            bytePosition++;
        }
    }
}
