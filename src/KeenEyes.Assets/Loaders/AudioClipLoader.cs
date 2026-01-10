using KeenEyes.Audio.Abstractions;
using NLayer;
using NVorbis;

namespace KeenEyes.Assets;

/// <summary>
/// Loader for audio clip assets supporting WAV, OGG, MP3, and FLAC formats.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="AudioClipLoader"/> loads audio files in WAV, OGG Vorbis, MP3, and FLAC formats.
/// WAV files are parsed directly, OGG files are decoded using NVorbis, MP3 files are
/// decoded using NLayer, and FLAC files use a built-in pure C# decoder.
/// </para>
/// <para>
/// FLAC support includes 16-bit and 24-bit files, with 24-bit automatically downsampled
/// to 16-bit for playback. FLAC provides lossless audio compression.
/// </para>
/// <para>
/// Loaded audio data is submitted to the audio context and wrapped in an
/// <see cref="AudioClipAsset"/> containing the audio handle and metadata.
/// </para>
/// </remarks>
public sealed class AudioClipLoader : IAssetLoader<AudioClipAsset>
{
    private readonly IAudioContext audio;

    /// <inheritdoc />
    public IReadOnlyList<string> Extensions => [".wav", ".ogg", ".mp3", ".flac"];

    /// <summary>
    /// Creates a new audio clip loader.
    /// </summary>
    /// <param name="audio">The audio context for audio buffer creation.</param>
    /// <exception cref="ArgumentNullException">Audio context is null.</exception>
    public AudioClipLoader(IAudioContext audio)
    {
        ArgumentNullException.ThrowIfNull(audio);
        this.audio = audio;
    }

    /// <inheritdoc />
    public AudioClipAsset Load(Stream stream, AssetLoadContext context)
    {
        var extension = Path.GetExtension(context.Path).ToLowerInvariant();

        return extension switch
        {
            ".ogg" => LoadOgg(stream, context.Path),
            ".wav" => LoadWav(stream, context.Path),
            ".mp3" => LoadMp3(stream, context.Path),
            ".flac" => LoadFlac(stream, context.Path),
            _ => throw new NotSupportedException($"Audio format not supported: {extension}")
        };
    }

    /// <inheritdoc />
    public async Task<AudioClipAsset> LoadAsync(
        Stream stream,
        AssetLoadContext context,
        CancellationToken cancellationToken = default)
    {
        // Audio decoding is CPU-bound, so run on thread pool
        return await Task.Run(() => Load(stream, context), cancellationToken);
    }

    /// <inheritdoc />
    public long EstimateSize(AudioClipAsset asset)
        => asset.SizeBytes;

    private AudioClipAsset LoadOgg(Stream stream, string path)
    {
        using var vorbis = new VorbisReader(stream);

        var sampleRate = vorbis.SampleRate;
        var channels = vorbis.Channels;
        var totalSamples = vorbis.TotalSamples;

        // Decode to float samples
        var samples = new float[totalSamples * channels];
        var samplesRead = 0;
        var buffer = new float[4096];

        while (samplesRead < samples.Length)
        {
            var count = vorbis.ReadSamples(buffer, 0, Math.Min(buffer.Length, samples.Length - samplesRead));
            if (count == 0)
            {
                break;
            }

            Array.Copy(buffer, 0, samples, samplesRead, count);
            samplesRead += count;
        }

        // Convert to 16-bit PCM for audio context
        var pcmData = ConvertToInt16(samples);
        var format = channels == 1 ? AudioFormat.Mono16 : AudioFormat.Stereo16;

        // Create audio clip using the context
        var handle = audio.CreateClip(pcmData, format, sampleRate);
        var duration = TimeSpan.FromSeconds((double)totalSamples / sampleRate);

        return new AudioClipAsset(handle, duration, channels, sampleRate, 16, audio);
    }

    private AudioClipAsset LoadWav(Stream stream, string path)
    {
        using var reader = new BinaryReader(stream);

        // Read RIFF header
        var riff = new string(reader.ReadChars(4));
        if (riff != "RIFF")
        {
            throw new InvalidDataException($"Invalid WAV file: missing RIFF header in {path}");
        }

        reader.ReadInt32(); // File size
        var wave = new string(reader.ReadChars(4));
        if (wave != "WAVE")
        {
            throw new InvalidDataException($"Invalid WAV file: missing WAVE format in {path}");
        }

        // Read fmt chunk
        int sampleRate = 0;
        short channels = 0;
        short bitsPerSample = 0;

        while (stream.Position < stream.Length)
        {
            var chunkId = new string(reader.ReadChars(4));
            var chunkSize = reader.ReadInt32();

            if (chunkId == "fmt ")
            {
                var audioFormat = reader.ReadInt16();
                if (audioFormat != 1) // PCM
                {
                    throw new InvalidDataException($"Unsupported WAV format: only PCM supported in {path}");
                }

                channels = reader.ReadInt16();
                sampleRate = reader.ReadInt32();
                reader.ReadInt32(); // Byte rate
                reader.ReadInt16(); // Block align
                bitsPerSample = reader.ReadInt16();

                // Skip any extra format bytes
                if (chunkSize > 16)
                {
                    reader.ReadBytes(chunkSize - 16);
                }
            }
            else if (chunkId == "data")
            {
                var data = reader.ReadBytes(chunkSize);

                var format = (channels, bitsPerSample) switch
                {
                    (1, 8) => AudioFormat.Mono8,
                    (1, 16) => AudioFormat.Mono16,
                    (2, 8) => AudioFormat.Stereo8,
                    (2, 16) => AudioFormat.Stereo16,
                    _ => throw new InvalidDataException($"Unsupported WAV format: {channels} channels, {bitsPerSample} bits in {path}")
                };

                var handle = audio.CreateClip(data, format, sampleRate);
                var totalSamples = data.Length / (channels * (bitsPerSample / 8));
                var duration = TimeSpan.FromSeconds((double)totalSamples / sampleRate);

                return new AudioClipAsset(handle, duration, channels, sampleRate, bitsPerSample, audio);
            }
            else
            {
                // Skip unknown chunks
                reader.ReadBytes(chunkSize);
            }
        }

        throw new InvalidDataException($"Invalid WAV file: missing data chunk in {path}");
    }

    private AudioClipAsset LoadMp3(Stream stream, string path)
    {
        using var mpegFile = new MpegFile(stream);
        var sampleRate = mpegFile.SampleRate;
        var channels = mpegFile.Channels;

        // Decode all samples to float buffer
        var samples = new List<float>();
        var buffer = new float[4096];
        int read;
        while ((read = mpegFile.ReadSamples(buffer, 0, buffer.Length)) > 0)
        {
            for (var i = 0; i < read; i++)
            {
                samples.Add(buffer[i]);
            }
        }

        // Convert to 16-bit PCM
        var pcmData = ConvertToInt16(samples.ToArray());
        var format = channels == 1 ? AudioFormat.Mono16 : AudioFormat.Stereo16;

        var handle = audio.CreateClip(pcmData, format, sampleRate);
        var totalSamples = samples.Count / channels;
        var duration = TimeSpan.FromSeconds((double)totalSamples / sampleRate);

        return new AudioClipAsset(handle, duration, channels, sampleRate, 16, audio);
    }

    private AudioClipAsset LoadFlac(Stream stream, string path)
    {
        // Use SimpleFlac decoder with byte output enabled
        var options = new FlacDecoder.Options { ConvertOutputToBytes = true };
        using var decoder = new FlacDecoder(stream, options);

        var sampleRate = decoder.SampleRate;
        var channels = decoder.ChannelCount;
        var bitsPerSample = decoder.BitsPerSample;

        // Collect all decoded bytes
        using var output = new MemoryStream();
        while (decoder.DecodeFrame())
        {
            output.Write(decoder.BufferBytes, 0, decoder.BufferByteCount);
        }

        var pcmData = output.ToArray();

        // Determine audio format based on bits per sample and channels
        var format = (channels, bitsPerSample) switch
        {
            (1, 8) => AudioFormat.Mono8,
            (1, 16) => AudioFormat.Mono16,
            (2, 8) => AudioFormat.Stereo8,
            (2, 16) => AudioFormat.Stereo16,
            // For 24-bit FLAC, we need to convert to 16-bit
            (1, 24) => AudioFormat.Mono16,
            (2, 24) => AudioFormat.Stereo16,
            _ => throw new InvalidDataException(
                $"Unsupported FLAC format: {channels} channels, {bitsPerSample} bits in {path}")
        };

        // Convert 24-bit to 16-bit if needed
        if (bitsPerSample == 24)
        {
            pcmData = Convert24BitTo16Bit(pcmData);
        }

        var handle = audio.CreateClip(pcmData, format, sampleRate);
        var bytesPerSample = bitsPerSample == 24 ? 2 : bitsPerSample / 8; // After conversion
        var totalSamples = pcmData.Length / (channels * bytesPerSample);
        var duration = TimeSpan.FromSeconds((double)totalSamples / sampleRate);

        return new AudioClipAsset(handle, duration, channels, sampleRate, bitsPerSample == 24 ? 16 : bitsPerSample, audio);
    }

    private static byte[] Convert24BitTo16Bit(byte[] data24)
    {
        // 24-bit samples are 3 bytes each, convert to 16-bit (2 bytes)
        var sampleCount = data24.Length / 3;
        var data16 = new byte[sampleCount * 2];

        for (var i = 0; i < sampleCount; i++)
        {
            // Read 24-bit sample (little-endian, signed)
            var b0 = data24[(i * 3) + 0];
            var b1 = data24[(i * 3) + 1];
            var b2 = data24[(i * 3) + 2];

            // Convert to 32-bit signed integer and shift to get upper 16 bits
            var sample24 = (b2 << 16) | (b1 << 8) | b0;
            if ((sample24 & 0x800000) != 0)
            {
                sample24 |= unchecked((int)0xFF000000); // Sign extend
            }

            // Take upper 16 bits of 24-bit sample
            var sample16 = (short)(sample24 >> 8);

            // Write 16-bit sample (little-endian)
            data16[(i * 2) + 0] = (byte)(sample16 & 0xFF);
            data16[(i * 2) + 1] = (byte)((sample16 >> 8) & 0xFF);
        }

        return data16;
    }

    private static byte[] ConvertToInt16(float[] samples)
    {
        var bytes = new byte[samples.Length * 2];
        for (var i = 0; i < samples.Length; i++)
        {
            // Clamp to -1 to 1 range and convert to 16-bit
            var sample = Math.Clamp(samples[i], -1f, 1f);
            var value = (short)(sample * short.MaxValue);
            bytes[i * 2] = (byte)(value & 0xFF);
            bytes[(i * 2) + 1] = (byte)((value >> 8) & 0xFF);
        }

        return bytes;
    }
}
