using System.Buffers.Binary;
using System.Text;
using KeenEyes.Audio.Abstractions;

namespace KeenEyes.Audio.Silk.Decoders;

/// <summary>
/// Decodes WAV audio files.
/// </summary>
internal static class WavDecoder
{
    /// <summary>
    /// Decodes a WAV file from a byte array.
    /// </summary>
    /// <param name="data">The WAV file data.</param>
    /// <returns>The decoded audio data.</returns>
    /// <exception cref="AudioLoadException">Thrown when the WAV file is invalid or unsupported.</exception>
    public static WavData Decode(ReadOnlySpan<byte> data)
    {
        if (data.Length < 44)
        {
            throw new AudioLoadException("WAV file too small for header");
        }

        // RIFF header
        var riff = Encoding.ASCII.GetString(data[..4]);
        if (riff != "RIFF")
        {
            throw new AudioLoadException("Invalid WAV file: missing RIFF header");
        }

        // Skip file size (bytes 4-7)

        var wave = Encoding.ASCII.GetString(data.Slice(8, 4));
        if (wave != "WAVE")
        {
            throw new AudioLoadException("Invalid WAV file: missing WAVE format");
        }

        // Find fmt and data chunks
        int offset = 12;
        int channels = 0;
        int sampleRate = 0;
        int bitsPerSample = 0;

        while (offset < data.Length - 8)
        {
            var chunkId = Encoding.ASCII.GetString(data.Slice(offset, 4));
            var chunkSize = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(offset + 4, 4));

            if (chunkId == "fmt ")
            {
                var formatTag = BinaryPrimitives.ReadInt16LittleEndian(data.Slice(offset + 8, 2));
                if (formatTag != 1) // PCM
                {
                    throw new AudioLoadException($"Unsupported WAV format: {formatTag} (only PCM supported)");
                }

                channels = BinaryPrimitives.ReadInt16LittleEndian(data.Slice(offset + 10, 2));
                sampleRate = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(offset + 12, 4));
                // Skip byte rate (offset + 16) and block align (offset + 20)
                bitsPerSample = BinaryPrimitives.ReadInt16LittleEndian(data.Slice(offset + 22, 2));
            }
            else if (chunkId == "data")
            {
                if (channels == 0 || sampleRate == 0 || bitsPerSample == 0)
                {
                    throw new AudioLoadException("Invalid WAV file: data chunk before fmt chunk");
                }

                var audioData = data.Slice(offset + 8, chunkSize).ToArray();
                var format = GetAudioFormat(channels, bitsPerSample);
                var duration = (float)audioData.Length / (sampleRate * channels * (bitsPerSample / 8));

                return new WavData
                {
                    Data = audioData,
                    Format = format,
                    SampleRate = sampleRate,
                    Channels = channels,
                    BitsPerSample = bitsPerSample,
                    Duration = duration
                };
            }

            offset += 8 + chunkSize;
            // Align to word boundary
            if (chunkSize % 2 == 1)
            {
                offset++;
            }
        }

        throw new AudioLoadException("Invalid WAV file: missing data chunk");
    }

    private static AudioFormat GetAudioFormat(int channels, int bitsPerSample)
    {
        return (channels, bitsPerSample) switch
        {
            (1, 8) => AudioFormat.Mono8,
            (1, 16) => AudioFormat.Mono16,
            (2, 8) => AudioFormat.Stereo8,
            (2, 16) => AudioFormat.Stereo16,
            _ => throw new AudioLoadException($"Unsupported audio format: {channels} channels, {bitsPerSample} bits")
        };
    }
}

/// <summary>
/// Decoded WAV audio data.
/// </summary>
internal sealed class WavData
{
    /// <summary>
    /// Gets the raw audio sample data.
    /// </summary>
    public required byte[] Data { get; init; }

    /// <summary>
    /// Gets the audio format.
    /// </summary>
    public required AudioFormat Format { get; init; }

    /// <summary>
    /// Gets the sample rate in Hz.
    /// </summary>
    public required int SampleRate { get; init; }

    /// <summary>
    /// Gets the number of channels.
    /// </summary>
    public required int Channels { get; init; }

    /// <summary>
    /// Gets the bits per sample.
    /// </summary>
    public required int BitsPerSample { get; init; }

    /// <summary>
    /// Gets the duration in seconds.
    /// </summary>
    public required float Duration { get; init; }
}
