using KeenEyes.Audio.Abstractions;

namespace KeenEyes.Audio.Abstractions.Tests;

/// <summary>
/// Tests for audio exception types.
/// </summary>
public class AudioExceptionTests
{
    #region AudioException Tests

    [Fact]
    public void AudioException_WithMessage_SetsMessage()
    {
        var exception = new AudioException("Test error");

        Assert.Equal("Test error", exception.Message);
    }

    [Fact]
    public void AudioException_WithMessageAndInner_SetsBoth()
    {
        var inner = new InvalidOperationException("Inner error");
        var exception = new AudioException("Outer error", inner);

        Assert.Equal("Outer error", exception.Message);
        Assert.Same(inner, exception.InnerException);
    }

    [Fact]
    public void AudioException_IsException()
    {
        var exception = new AudioException("Test");

        Assert.IsAssignableFrom<Exception>(exception);
    }

    #endregion

    #region AudioInitializationException Tests

    [Fact]
    public void AudioInitializationException_WithMessage_SetsMessage()
    {
        var exception = new AudioInitializationException("Device not found");

        Assert.Equal("Device not found", exception.Message);
    }

    [Fact]
    public void AudioInitializationException_IsAudioException()
    {
        var exception = new AudioInitializationException("Test");

        Assert.IsAssignableFrom<AudioException>(exception);
    }

    #endregion

    #region AudioLoadException Tests

    [Fact]
    public void AudioLoadException_WithMessage_SetsMessage()
    {
        var exception = new AudioLoadException("Failed to load file.wav");

        Assert.Equal("Failed to load file.wav", exception.Message);
    }

    [Fact]
    public void AudioLoadException_WithMessageAndInner_SetsBoth()
    {
        var inner = new FileNotFoundException("File not found");
        var exception = new AudioLoadException("Failed to load audio", inner);

        Assert.Equal("Failed to load audio", exception.Message);
        Assert.Same(inner, exception.InnerException);
    }

    [Fact]
    public void AudioLoadException_IsAudioException()
    {
        var exception = new AudioLoadException("Test");

        Assert.IsAssignableFrom<AudioException>(exception);
    }

    #endregion
}
