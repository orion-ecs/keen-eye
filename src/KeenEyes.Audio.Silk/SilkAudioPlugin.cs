using KeenEyes.Audio.Abstractions;
using KeenEyes.Platform.Silk;

namespace KeenEyes.Audio.Silk;

/// <summary>
/// Plugin that provides Silk.NET OpenAL audio capabilities to a KeenEyes world.
/// </summary>
/// <remarks>
/// <para>
/// This plugin requires <c>SilkWindowPlugin</c> from KeenEyes.Platform.Silk to be installed first.
/// It will throw an <see cref="InvalidOperationException"/> if the window plugin is not available.
/// </para>
/// <para>
/// The plugin creates a <see cref="SilkAudioContext"/> extension that provides
/// audio clip loading and one-shot playback functionality.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Install window plugin first (required)
/// world.InstallPlugin(new SilkWindowPlugin(new WindowConfig
/// {
///     Title = "My Game",
///     Width = 1920,
///     Height = 1080
/// }));
///
/// // Then install audio plugin
/// world.InstallPlugin(new SilkAudioPlugin(new SilkAudioConfig
/// {
///     MaxOneShotSources = 64,
///     InitialMasterVolume = 0.8f
/// }));
///
/// // Access audio context
/// var audio = world.GetExtension&lt;IAudioContext&gt;();
/// var clip = audio.LoadClip("assets/sounds/explosion.wav");
/// audio.Play(clip);
/// </code>
/// </example>
/// <param name="config">The audio configuration.</param>
public sealed class SilkAudioPlugin(SilkAudioConfig config) : IWorldPlugin
{
    private SilkAudioContext? audioContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="SilkAudioPlugin"/> class with default configuration.
    /// </summary>
    public SilkAudioPlugin()
        : this(new SilkAudioConfig())
    {
    }

    /// <inheritdoc />
    public string Name => "SilkAudio";

    /// <inheritdoc />
    public void Install(IPluginContext context)
    {
        // Explicit dependency - fail loudly if window plugin not installed
        if (!context.TryGetExtension<ISilkWindowProvider>(out var windowProvider) || windowProvider is null)
        {
            throw new InvalidOperationException(
                $"{nameof(SilkAudioPlugin)} requires SilkWindowPlugin to be installed first. " +
                $"Install SilkWindowPlugin before installing {nameof(SilkAudioPlugin)}.");
        }

        // Create audio context using the shared window provider
        audioContext = new SilkAudioContext(windowProvider, config);

        // Register as both interface and concrete type for flexibility
        context.SetExtension<IAudioContext>(audioContext);
        context.SetExtension(audioContext);

        // Register backend-agnostic audio update system
        context.AddSystem<AudioUpdateSystem>(SystemPhase.Update);
    }

    /// <inheritdoc />
    public void Uninstall(IPluginContext context)
    {
        context.RemoveExtension<IAudioContext>();
        context.RemoveExtension<SilkAudioContext>();
        audioContext?.Dispose();
        audioContext = null;
    }
}
