using KeenEyes.Animation.Components;
using KeenEyes.Animation.Events;
using KeenEyes.Animation.Systems;

namespace KeenEyes.Animation;

/// <summary>
/// Plugin that adds animation system support to the ECS world.
/// </summary>
/// <remarks>
/// <para>
/// This plugin provides a complete ECS-native animation system supporting:
/// <list type="bullet">
///   <item><description>Skeletal animation with keyframe clips</description></item>
///   <item><description>Sprite sheet animation for 2D games</description></item>
///   <item><description>Property tweening with easing functions</description></item>
///   <item><description>State machine-based animation control</description></item>
/// </list>
/// </para>
/// <para>
/// After installation, access the animation manager through the world extension:
/// <code>
/// var animations = world.GetExtension&lt;AnimationManager&gt;();
/// var clipId = animations.RegisterClip(myClip);
/// </code>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// using var world = new World();
///
/// // Install with default configuration
/// world.InstallPlugin(new AnimationPlugin());
///
/// // Register animation assets
/// var animations = world.GetExtension&lt;AnimationManager&gt;();
/// var walkClipId = animations.RegisterClip(walkClip);
/// var runSheetId = animations.RegisterSpriteSheet(runSheet);
///
/// // Create animated entity with skeletal animation
/// var character = world.Spawn()
///     .With(Transform3D.Identity)
///     .With(AnimationPlayer.ForClip(walkClipId))
///     .Build();
///
/// // Create animated sprite
/// var sprite = world.Spawn()
///     .With(new Transform2D(position, 0, Vector2.One))
///     .With(SpriteAnimator.ForSheet(runSheetId))
///     .Build();
///
/// // Create tweened property
/// var fading = world.Spawn()
///     .With(TweenFloat.Create(1f, 0f, 2f, EaseType.QuadOut))
///     .Build();
/// </code>
/// </example>
public sealed class AnimationPlugin : IWorldPlugin
{
    private AnimationManager? animationManager;

    /// <summary>
    /// Creates a new animation plugin with default configuration.
    /// </summary>
    public AnimationPlugin()
        : this(AnimationConfig.Default)
    {
    }

    /// <summary>
    /// Creates a new animation plugin with the specified configuration.
    /// </summary>
    /// <param name="config">The animation configuration.</param>
    /// <exception cref="ArgumentException">Thrown if configuration is invalid.</exception>
    public AnimationPlugin(AnimationConfig config)
    {
        var error = config.Validate();
        if (error != null)
        {
            throw new ArgumentException($"Invalid AnimationConfig: {error}", nameof(config));
        }

        Config = config;
    }

    /// <summary>
    /// Gets the configuration for this plugin.
    /// </summary>
    public AnimationConfig Config { get; }

    /// <inheritdoc/>
    public string Name => "Animation";

    /// <inheritdoc/>
    public void Install(IPluginContext context)
    {
        // Register components
        context.RegisterComponent<AnimationPlayer>();
        context.RegisterComponent<SpriteAnimator>();
        context.RegisterComponent<Animator>();
        context.RegisterComponent<BoneReference>();
        context.RegisterComponent<TweenFloat>();
        context.RegisterComponent<TweenVector2>();
        context.RegisterComponent<TweenVector3>();
        context.RegisterComponent<TweenVector4>();

        // Create and expose the animation manager
        animationManager = new AnimationManager();
        context.SetExtension(animationManager);

        // Register systems - order matters for proper updates
        // Animation state updates first
        context.AddSystem<AnimatorSystem>(
            SystemPhase.Update,
            order: 50);

        context.AddSystem<AnimationPlayerSystem>(
            SystemPhase.Update,
            order: 51);

        context.AddSystem<SpriteAnimationSystem>(
            SystemPhase.Update,
            order: 52);

        // Animation events after playback update
        context.AddSystem<AnimationEventSystem>(
            SystemPhase.Update,
            order: 53);

        // Skeleton pose application after all animation updates
        context.AddSystem<SkeletonPoseSystem>(
            SystemPhase.Update,
            order: 55);

        // Tweens update after animation state
        context.AddSystem<TweenSystem>(
            SystemPhase.Update,
            order: 60);
    }

    /// <inheritdoc/>
    public void Uninstall(IPluginContext context)
    {
        // Remove the extension
        context.RemoveExtension<AnimationManager>();

        // Dispose the animation manager
        animationManager?.Dispose();
        animationManager = null;

        // Systems are automatically cleaned up by PluginManager
    }
}
