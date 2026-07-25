using KeenEyes.Audio.Abstractions;
using KeenEyes.Audio.Silk.Backend;

namespace KeenEyes.Audio.Silk.Tests;

/// <summary>
/// Tests that an unavailable audio device is reported as a typed, actionable failure.
/// </summary>
/// <remarks>
/// Regression coverage for #1256. Opening the backend can fail two ways - the native OpenAL
/// runtime is missing, or no output device can be opened - and both used to surface as
/// something the caller could not act on: a bare Silk.NET <see cref="Exception"/> reading
/// "Could not load from any of the possible library names!", or a message that named neither
/// the cause nor a remedy. Both now arrive as <see cref="AudioInitializationException"/> with a
/// message that says what is missing.
/// </remarks>
public class OpenALDeviceTests
{
    [Fact]
    public void Constructor_WithUnknownDeviceName_ThrowsAudioInitializationException()
    {
        var exception = Assert.Throws<AudioInitializationException>(
            () => new OpenALDevice("KeenEyes.Tests.NoSuchAudioDevice"));

        // Whichever way the backend is unavailable on the test machine, the message has to
        // name the failure and point at the fix rather than leaking a loader detail.
        Assert.Contains("OpenAL", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "Could not load from any of the possible library names",
            exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_WhenBackendUnavailable_ExplainsHowToFixIt()
    {
        var exception = Assert.Throws<AudioInitializationException>(
            () => new OpenALDevice("KeenEyes.Tests.NoSuchAudioDevice"));

        // A missing native runtime names the package that ships it; a missing device names the
        // device instead. Either way the message is actionable.
        var namesTheRuntime = exception.Message.Contains(
            "Silk.NET.OpenAL.Soft.Native", StringComparison.Ordinal);
        var namesTheDevice = exception.Message.Contains(
            "KeenEyes.Tests.NoSuchAudioDevice", StringComparison.Ordinal);

        Assert.True(
            namesTheRuntime || namesTheDevice,
            $"Message explained neither the missing runtime nor the missing device: {exception.Message}");
    }
}
