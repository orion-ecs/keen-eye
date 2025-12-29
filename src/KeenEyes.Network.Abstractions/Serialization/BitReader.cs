using System.Runtime.CompilerServices;

namespace KeenEyes.Network.Serialization;

/// <summary>
/// A high-performance bit-level reader for network deserialization.
/// </summary>
/// <remarks>
/// <para>
/// Complements <see cref="BitWriter"/> by reading bit-packed data. Both must use
/// the same bit ordering and encoding for data to round-trip correctly.
/// </para>
/// <para>
/// This type is a ref struct to avoid heap allocations in hot paths.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var reader = new BitReader(receivedData);
///
/// var id = reader.ReadBits(6);           // 6 bits for value 0-63
/// var flag = reader.ReadBool();          // 1 bit
/// var pos = reader.ReadQuantized(-1000f, 1000f, 0.01f);  // 18 bits
/// </code>
/// </example>
public ref struct BitReader
{
    private readonly ReadOnlySpan<byte> buffer;
    private int bytePosition;
    private int bitOffset;

    /// <summary>
    /// Initializes a new instance of the <see cref="BitReader"/> struct.
    /// </summary>
    /// <param name="buffer">The buffer to read from.</param>
    public BitReader(ReadOnlySpan<byte> buffer)
    {
        this.buffer = buffer;
        bytePosition = 0;
        bitOffset = 0;
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
    /// Gets the total number of bits read.
    /// </summary>
    public readonly int TotalBitsRead => bytePosition * 8 + bitOffset;

    /// <summary>
    /// Gets whether the reader has reached the end of the buffer.
    /// </summary>
    public readonly bool IsAtEnd => bytePosition >= buffer.Length;

    /// <summary>
    /// Gets the number of bits remaining in the buffer.
    /// </summary>
    public readonly int BitsRemaining => (buffer.Length * 8) - TotalBitsRead;

    /// <summary>
    /// Reads a single bit as a boolean.
    /// </summary>
    /// <returns>True if the bit is 1; false if 0.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ReadBool()
    {
        var value = (buffer[bytePosition] >> bitOffset) & 1;
        AdvanceBits(1);
        return value == 1;
    }

    /// <summary>
    /// Reads an unsigned integer using the specified number of bits.
    /// </summary>
    /// <param name="bits">The number of bits to read (1-32).</param>
    /// <returns>The unsigned integer value.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when bits is less than 1 or greater than 32.
    /// </exception>
    public uint ReadBits(int bits)
    {
        if (bits < 1 || bits > 32)
        {
            throw new ArgumentOutOfRangeException(nameof(bits), "Bits must be between 1 and 32.");
        }

        uint value = 0;
        var bitsRead = 0;

        while (bitsRead < bits)
        {
            var bitsAvailable = 8 - bitOffset;
            var bitsToRead = Math.Min(bitsAvailable, bits - bitsRead);

            // Extract the bits from current byte
            var mask = (1u << bitsToRead) - 1;
            var fragment = (buffer[bytePosition] >> bitOffset) & (byte)mask;

            value |= (uint)fragment << bitsRead;
            bitsRead += bitsToRead;
            AdvanceBits(bitsToRead);
        }

        return value;
    }

    /// <summary>
    /// Reads a signed integer using the specified number of bits.
    /// </summary>
    /// <param name="bits">The number of bits to read (1-32).</param>
    /// <returns>The signed integer value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadSignedBits(int bits)
    {
        var unsigned = ReadBits(bits);

        // Sign extend if necessary
        var signBit = 1u << (bits - 1);
        if ((unsigned & signBit) != 0)
        {
            // Extend sign bits
            var mask = uint.MaxValue << bits;
            return unchecked((int)(unsigned | mask));
        }

        return unchecked((int)unsigned);
    }

    /// <summary>
    /// Reads a byte (8 bits).
    /// </summary>
    /// <returns>The byte value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte ReadByte()
    {
        return (byte)ReadBits(8);
    }

    /// <summary>
    /// Reads an unsigned 16-bit integer.
    /// </summary>
    /// <returns>The value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort ReadUInt16()
    {
        return (ushort)ReadBits(16);
    }

    /// <summary>
    /// Reads an unsigned 32-bit integer.
    /// </summary>
    /// <returns>The value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadUInt32()
    {
        return ReadBits(32);
    }

    /// <summary>
    /// Reads a 32-bit float without dequantization.
    /// </summary>
    /// <returns>The float value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float ReadFloat()
    {
        return BitConverter.UInt32BitsToSingle(ReadBits(32));
    }

    /// <summary>
    /// Reads a quantized float value.
    /// </summary>
    /// <param name="min">The minimum value of the range.</param>
    /// <param name="max">The maximum value of the range.</param>
    /// <param name="resolution">The precision/step size.</param>
    /// <returns>The dequantized float value.</returns>
    public float ReadQuantized(float min, float max, float resolution)
    {
        // Calculate bits needed (must match writer)
        var range = max - min;
        var maxSteps = (uint)Math.Ceiling(range / resolution);
        var bits = (int)Math.Ceiling(Math.Log2(maxSteps + 1));

        var quantized = ReadBits(bits);
        return min + (quantized * resolution);
    }

    /// <summary>
    /// Reads bytes into a span.
    /// </summary>
    /// <param name="destination">The span to read into.</param>
    public void ReadBytes(Span<byte> destination)
    {
        for (var i = 0; i < destination.Length; i++)
        {
            destination[i] = ReadByte();
        }
    }

    /// <summary>
    /// Reads bytes into a new array.
    /// </summary>
    /// <param name="count">The number of bytes to read.</param>
    /// <returns>A new byte array containing the read data.</returns>
    public byte[] ReadBytes(int count)
    {
        var result = new byte[count];
        ReadBytes(result);
        return result;
    }

    /// <summary>
    /// Skips the specified number of bits.
    /// </summary>
    /// <param name="bits">The number of bits to skip.</param>
    public void SkipBits(int bits)
    {
        AdvanceBits(bits);
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
