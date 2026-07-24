using System.Buffers.Binary;
using System.Text;
using KeenEyes.Audio.Abstractions;
using KeenEyes.Audio.Silk.Decoders;

namespace KeenEyes.Audio.Silk.Tests;

/// <summary>
/// Tests for <see cref="WavDecoder"/> input validation and decoding.
/// </summary>
public sealed class WavDecoderTests
{
    /// <summary>
    /// Builds a PCM WAV byte array. The declared <paramref name="declaredDataSize"/>
    /// may deliberately differ from the number of <paramref name="data"/> bytes present
    /// in order to exercise malformed-input handling.
    /// </summary>
    private static byte[] BuildWav(short channels, int sampleRate, short bitsPerSample,
        int declaredDataSize, byte[] data)
    {
        var buffer = new byte[44 + data.Length];
        var span = buffer.AsSpan();

        Encoding.ASCII.GetBytes("RIFF").CopyTo(span[..4]);
        BinaryPrimitives.WriteInt32LittleEndian(span.Slice(4, 4), 36 + data.Length);
        Encoding.ASCII.GetBytes("WAVE").CopyTo(span.Slice(8, 4));

        Encoding.ASCII.GetBytes("fmt ").CopyTo(span.Slice(12, 4));
        BinaryPrimitives.WriteInt32LittleEndian(span.Slice(16, 4), 16);
        BinaryPrimitives.WriteInt16LittleEndian(span.Slice(20, 2), 1); // PCM
        BinaryPrimitives.WriteInt16LittleEndian(span.Slice(22, 2), channels);
        BinaryPrimitives.WriteInt32LittleEndian(span.Slice(24, 4), sampleRate);
        int byteRate = sampleRate * channels * (bitsPerSample / 8);
        BinaryPrimitives.WriteInt32LittleEndian(span.Slice(28, 4), byteRate);
        BinaryPrimitives.WriteInt16LittleEndian(span.Slice(32, 2), (short)(channels * (bitsPerSample / 8)));
        BinaryPrimitives.WriteInt16LittleEndian(span.Slice(34, 2), bitsPerSample);

        Encoding.ASCII.GetBytes("data").CopyTo(span.Slice(36, 4));
        BinaryPrimitives.WriteInt32LittleEndian(span.Slice(40, 4), declaredDataSize);
        data.CopyTo(span[44..]);

        return buffer;
    }

    [Fact]
    public void Decode_ValidPcmWav_ReturnsDecodedData()
    {
        var samples = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var wav = BuildWav(channels: 1, sampleRate: 8000, bitsPerSample: 16,
            declaredDataSize: samples.Length, data: samples);

        var result = WavDecoder.Decode(wav);

        Assert.Equal(AudioFormat.Mono16, result.Format);
        Assert.Equal(1, result.Channels);
        Assert.Equal(8000, result.SampleRate);
        Assert.Equal(16, result.BitsPerSample);
        Assert.Equal(samples, result.Data);
    }

    [Fact]
    public void Decode_DataChunkLargerThanRemaining_ThrowsAudioLoadException()
    {
        // Declares 200000 data bytes but only supplies 4.
        var wav = BuildWav(channels: 1, sampleRate: 8000, bitsPerSample: 16,
            declaredDataSize: 200_000, data: new byte[] { 0x01, 0x02, 0x03, 0x04 });

        Assert.Throws<AudioLoadException>(() => WavDecoder.Decode(wav));
    }

    [Fact]
    public void Decode_NegativeChunkSize_ThrowsAudioLoadException()
    {
        // A negative chunk size would otherwise slice out of range (or, for a
        // non-data chunk, spin the parse loop forever).
        var wav = BuildWav(channels: 1, sampleRate: 8000, bitsPerSample: 16,
            declaredDataSize: -8, data: new byte[] { 0x01, 0x02, 0x03, 0x04 });

        Assert.Throws<AudioLoadException>(() => WavDecoder.Decode(wav));
    }
}
