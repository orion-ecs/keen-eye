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
/// audio clip loading, one-shot playback, and 3D spatial audio functionality.
/// </para>
/// <para>
/// ECS components <see cref="AudioSource"/> and <see cref="AudioListener"/> are registered
/// automatically, along with systems to synchronize them with the OpenAL backend.
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
/// // Access audio context for one-shot playback
/// var audio = world.GetExtension&lt;IAudioContext&gt;();
/// var clip = audio.LoadClip("assets/sounds/explosion.wav");
/// audio.Play(clip);
///
/// // Or use ECS components for 3D spatial audio
/// var enemy = world.Spawn()
///     .With(new Transform3D(new Vector3(10, 0, 0), Quaternion.Identity, Vector3.One))
///     .With(AudioSource.Default with { Clip = clip, Spatial = true, PlayOnAwake = true })
///     .Build();
/// </code>
/// </example>
/// <param name="config">The audio configuration.</param>
public sealed class SilkAudioPlugin(SilkAudioConfig config) : IWorldPlugin
{
    private SilkAudioContext? audioContext;
    private AudioSourceSystem? audioSourceSystem;
    private EventSubscription? audioSourceRemovedSubscription;
    private EventSubscription? entityDestroyedSubscription;

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

        // Register ECS components
        context.RegisterComponent<AudioSource>();
        context.RegisterComponent<AudioListener>();

        // Register systems
        // AudioUpdateSystem: Recycles finished sources in the pool
        context.AddSystem<AudioUpdateSystem>(SystemPhase.Update, order: 0);

        // AudioListenerSystem: Syncs listener position/orientation (runs before source system)
        context.AddSystem<AudioListenerSystem>(SystemPhase.Update, order: 5);

        // AudioSourceSystem: Syncs source positions and playback state
        audioSourceSystem = new AudioSourceSystem();
        context.AddSystem(audioSourceSystem, SystemPhase.Update, order: 10);

        // Subscribe to component lifecycle events
        audioSourceRemovedSubscription = context.World.OnComponentRemoved<AudioSource>(OnAudioSourceRemoved);
        entityDestroyedSubscription = context.World.OnEntityDestroyed(OnEntityDestroyed);
    }

    /// <inheritdoc />
    public void Uninstall(IPluginContext context)
    {
        // Unsubscribe from events
        audioSourceRemovedSubscription?.Dispose();
        entityDestroyedSubscription?.Dispose();
        audioSourceRemovedSubscription = null;
        entityDestroyedSubscription = null;

        // Remove extensions
        context.RemoveExtension<IAudioContext>();
        context.RemoveExtension<SilkAudioContext>();

        // Dispose resources
        audioContext?.Dispose();
        audioContext = null;
        audioSourceSystem = null;

        // Systems are automatically cleaned up by PluginManager
    }

    private void OnAudioSourceRemoved(Entity entity)
    {
        audioSourceSystem?.OnAudioSourceRemoved(entity);
    }

    private void OnEntityDestroyed(Entity entity)
    {
        audioSourceSystem?.OnEntityDestroyed(entity);
    }
}
