namespace KeenEyes.Platform.Silk;

/// <summary>
/// Plugin that creates and manages a Silk.NET window and main loop.
/// </summary>
/// <remarks>
/// <para>
/// This plugin must be installed before <c>SilkGraphicsPlugin</c> or <c>SilkInputPlugin</c>,
/// as both depend on the <see cref="ISilkWindowProvider"/> extension this plugin provides.
/// </para>
/// <para>
/// The plugin registers both <see cref="ISilkWindowProvider"/> for direct window access
/// and <see cref="ILoopProvider"/> for the main application loop. Use
/// <c>WorldRunnerBuilder</c> to easily set up the main loop.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create and install the window plugin
/// world.InstallPlugin(new SilkWindowPlugin(new WindowConfig
/// {
///     Title = "My Game",
///     Width = 1920,
///     Height = 1080,
///     VSync = true
/// }));
///
/// // Install other plugins (graphics, input)
/// world.InstallPlugin(new SilkGraphicsPlugin(graphicsConfig));
/// world.InstallPlugin(new SilkInputPlugin(inputConfig));
///
/// // Run with WorldRunnerBuilder (recommended)
/// world.CreateRunner()
///     .OnReady(() =&gt; CreateScene(world))
///     .Run();
/// </code>
/// </example>
/// <param name="config">The window configuration.</param>
public sealed class SilkWindowPlugin(WindowConfig config) : IWorldPlugin
{
    private SilkWindowProvider? provider;

    /// <summary>
    /// Initializes a new instance of the <see cref="SilkWindowPlugin"/> class with default configuration.
    /// </summary>
    public SilkWindowPlugin()
        : this(new WindowConfig())
    {
    }

    /// <inheritdoc />
    public string Name => "SilkWindow";

    /// <inheritdoc />
    public void Install(IPluginContext context)
    {
        provider = new SilkWindowProvider(config);
        var loopProvider = new SilkLoopProvider(provider);

        context.SetExtension<ISilkWindowProvider>(provider);
        context.SetExtension<ILoopProvider>(loopProvider);
    }

    /// <inheritdoc />
    public void Uninstall(IPluginContext context)
    {
        context.RemoveExtension<ILoopProvider>();
        context.RemoveExtension<ISilkWindowProvider>();
        provider?.Dispose();
        provider = null;
    }
}
