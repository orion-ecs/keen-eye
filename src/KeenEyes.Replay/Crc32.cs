namespace KeenEyes.Replay;

/// <summary>
/// Provides CRC32 checksum computation.
/// </summary>
/// <remarks>
/// Uses the standard CRC-32 polynomial (0xEDB88320) which is compatible with
/// gzip, PNG, and other common formats.
/// </remarks>
internal static class Crc32
{
    private static readonly uint[] crcTable = GenerateTable();

    /// <summary>
    /// Generates the CRC32 lookup table.
    /// </summary>
    private static uint[] GenerateTable()
    {
        const uint polynomial = 0xEDB88320;
        var table = new uint[256];

        for (uint i = 0; i < 256; i++)
        {
            var crc = i;
            for (var j = 0; j < 8; j++)
            {
                crc = (crc & 1) != 0 ? (crc >> 1) ^ polynomial : crc >> 1;
            }
            table[i] = crc;
        }

        return table;
    }

    /// <summary>
    /// Computes the CRC32 checksum of the specified data.
    /// </summary>
    /// <param name="data">The data to compute the checksum for.</param>
    /// <returns>The CRC32 checksum as a 32-bit unsigned integer.</returns>
    public static uint HashToUInt32(ReadOnlySpan<byte> data)
    {
        var crc = 0xFFFFFFFF;

        foreach (var b in data)
        {
            crc = crcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
        }

        return crc ^ 0xFFFFFFFF;
    }

    /// <summary>
    /// Computes the CRC32 checksum of the specified data.
    /// </summary>
    /// <param name="data">The data to compute the checksum for.</param>
    /// <returns>The CRC32 checksum as a 32-bit unsigned integer.</returns>
    public static uint HashToUInt32(byte[] data)
    {
        return HashToUInt32(data.AsSpan());
    }
}
