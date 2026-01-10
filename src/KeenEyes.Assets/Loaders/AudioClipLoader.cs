using KeenEyes.Audio.Abstractions;
using NLayer;
using NVorbis;

namespace KeenEyes.Assets;

/// <summary>
/// Loader for audio clip assets supporting WAV and OGG formats.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="AudioClipLoader"/> loads audio files in WAV and OGG Vorbis formats.
/// WAV files are parsed directly, while OGG files are decoded using NVorbis.
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
    public IReadOnlyList<string> Extensions => [".wav", ".ogg", ".mp3"];

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
