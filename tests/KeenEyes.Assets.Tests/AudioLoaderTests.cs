using System.Numerics;
using KeenEyes.Audio.Abstractions;

namespace KeenEyes.Assets.Tests;

/// <summary>
/// Tests for the audio clip loader.
/// </summary>
public class AudioLoaderTests
{
    [Fact]
    public void AudioClipLoader_Extensions_ContainsWav()
    {
        var audio = new MockAudioContext();
        var loader = new AudioClipLoader(audio);

        Assert.Contains(".wav", loader.Extensions);
    }

    [Fact]
    public void AudioClipLoader_Extensions_ContainsOgg()
    {
        var audio = new MockAudioContext();
        var loader = new AudioClipLoader(audio);

        Assert.Contains(".ogg", loader.Extensions);
    }

    [Fact]
    public void AudioClipLoader_Extensions_ContainsMp3()
    {
        var audio = new MockAudioContext();
        var loader = new AudioClipLoader(audio);

        Assert.Contains(".mp3", loader.Extensions);
    }

    [Fact]
    public void AudioClipLoader_Extensions_HasThreeFormats()
    {
        var audio = new MockAudioContext();
        var loader = new AudioClipLoader(audio);

        Assert.Equal(3, loader.Extensions.Count);
    }

    [Fact]
    public void AudioClipLoader_Constructor_ThrowsOnNullAudioContext()
    {
        Assert.Throws<ArgumentNullException>(() => new AudioClipLoader(null!));
    }
}

/// <summary>
/// Mock audio context for testing.
/// </summary>
file sealed class MockAudioContext : IAudioContext
{
    public List<AudioClipHandle> CreatedClips { get; } = [];
    public List<AudioClipHandle> UnloadedClips { get; } = [];

    public IAudioDevice? Device => null;
    public bool IsInitialized => true;
    public float MasterVolume { get; set; } = 1f;

    public AudioClipHandle LoadClip(string path)
    {
        var handle = new AudioClipHandle(CreatedClips.Count + 1);
        CreatedClips.Add(handle);
        return handle;
    }

    public AudioClipHandle CreateClip(ReadOnlySpan<byte> data, AudioFormat format, int sampleRate)
    {
        var handle = new AudioClipHandle(CreatedClips.Count + 1);
        CreatedClips.Add(handle);
        return handle;
    }

    public AudioClipInfo? GetClipInfo(AudioClipHandle handle) => null;

    public void UnloadClip(AudioClipHandle handle) => UnloadedClips.Add(handle);

    public uint GetBufferId(AudioClipHandle handle) => 0;

    public SoundHandle Play(AudioClipHandle clip, float volume = 1f) => new(1);

    public SoundHandle Play(AudioClipHandle clip, PlaybackOptions options) => new(1);

    public SoundHandle PlayAt(AudioClipHandle clip, Vector3 position, float volume = 1f) => new(1);

    public SoundHandle PlayAt(AudioClipHandle clip, Vector3 position, PlaybackOptions options) => new(1);

    public void Stop(SoundHandle sound) { }

    public void Pause(SoundHandle sound) { }

    public void Resume(SoundHandle sound) { }

    public void SetVolume(SoundHandle sound, float volume) { }

    public void SetPitch(SoundHandle sound, float pitch) { }

    public void SetPosition(SoundHandle sound, Vector3 position) { }

    public bool IsPlaying(SoundHandle sound) => false;

    public void StopAll() { }

    public void PauseAll() { }

    public void ResumeAll() { }

    public float GetChannelVolume(AudioChannel channel) => 1f;

    public void SetChannelVolume(AudioChannel channel, float volume) { }

    public void SetListenerPosition(Vector3 position) { }

    public void SetListenerOrientation(Vector3 forward, Vector3 up) { }

    public void SetListenerVelocity(Vector3 velocity) { }

    public void Update() { }

    public void Dispose() { }
}
