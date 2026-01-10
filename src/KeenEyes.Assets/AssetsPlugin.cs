using KeenEyes.Audio.Abstractions;
using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Assets;

/// <summary>
/// Plugin that integrates the asset management system with a KeenEyes world.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="AssetsPlugin"/> creates and configures an <see cref="AssetManager"/>,
/// registers built-in loaders based on available dependencies (graphics, audio),
/// and sets up the asset resolution system for ECS integration.
/// </para>
/// <para>
/// Built-in loaders are registered automatically if their dependencies are available:
/// <list type="bullet">
/// <item><see cref="TextureLoader"/> - requires <see cref="IGraphicsContext"/></item>
/// <item><see cref="SpriteAtlasLoader"/> - requires <see cref="IGraphicsContext"/></item>
/// <item><see cref="AnimationLoader"/> - requires <see cref="IGraphicsContext"/></item>
/// <item><see cref="FontLoader"/> - requires <see cref="IFontManagerProvider"/></item>
/// <item><see cref="AudioClipLoader"/> - requires <see cref="IAudioContext"/></item>
/// <item><see cref="MeshLoader"/> - no dependencies</item>
/// <item><see cref="RawLoader"/> - no dependencies</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create world with asset management
/// using var world = new World();
///
/// // Install graphics and audio first (optional)
/// world.InstallPlugin(new SilkGraphicsPlugin(config));
/// world.InstallPlugin(new SilkAudioPlugin());
///
/// // Install assets plugin
/// world.InstallPlugin(new AssetsPlugin(new AssetsConfig
/// {
///     RootPath = "Assets",
///     EnableHotReload = true  // Development mode
/// }));
///
/// // Now load assets
/// var assets = world.GetExtension&lt;AssetManager&gt;();
/// using var texture = assets.Load&lt;TextureAsset&gt;("textures/player.png");
/// </code>
/// </example>
/// <param name="config">Asset configuration, or null for defaults.</param>
public sealed class AssetsPlugin(AssetsConfig? config = null) : IWorldPlugin
{
    private readonly AssetsConfig resolvedConfig = config ?? AssetsConfig.Default;
    private AssetManager? assetManager;
    private ReloadManager? reloadManager;

    /// <inheritdoc />
    public string Name => "Assets";

    /// <inheritdoc />
    public void Install(IPluginContext context)
    {
        // Create asset manager
        assetManager = new AssetManager(resolvedConfig);

        // Register built-in loaders based on available dependencies

        // TextureLoader, SpriteAtlasLoader, and AnimationLoader require IGraphicsContext
        if (context.TryGetExtension<IGraphicsContext>(out var graphics) && graphics != null)
        {
            assetManager.RegisterLoader(new TextureLoader(graphics));
            assetManager.RegisterLoader(new SpriteAtlasLoader());
            assetManager.RegisterLoader(new AnimationLoader());

            // FontLoader requires IFontManager (provided by IFontManagerProvider)
            if (graphics is IFontManagerProvider fontProvider)
            {
                var fontManager = fontProvider.GetFontManager();
                if (fontManager != null)
                {
                    assetManager.RegisterLoader(new FontLoader(fontManager));
                }
            }
        }

        // AudioClipLoader requires IAudioContext
        if (context.TryGetExtension<IAudioContext>(out var audio) && audio != null)
        {
            assetManager.RegisterLoader(new AudioClipLoader(audio));
        }

        // These loaders have no external dependencies
        assetManager.RegisterLoader(new MeshLoader());
        assetManager.RegisterLoader(new RawLoader());

        // Register as world extension
        context.SetExtension(assetManager);

        // Register the asset resolution system
        context.AddSystem<AssetResolutionSystem>(SystemPhase.EarlyUpdate, order: -100);

        // Set up hot reload if enabled
        if (resolvedConfig.EnableHotReload && Directory.Exists(resolvedConfig.RootPath))
        {
            reloadManager = new ReloadManager(resolvedConfig.RootPath, assetManager);
            reloadManager.OnAssetReloaded += path =>
            {
                // Forward to asset manager event
                // Note: AssetManager.OnAssetReloaded is raised in ReloadAsync
            };
        }
    }

    /// <inheritdoc />
    public void Uninstall(IPluginContext context)
    {
        // Dispose hot reload manager first
        reloadManager?.Dispose();
        reloadManager = null;

        // Remove extension
        context.RemoveExtension<AssetManager>();

        // Dispose asset manager (unloads all assets)
        assetManager?.Dispose();
        assetManager = null;
    }
}
