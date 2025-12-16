namespace KeenEyes.Platform.Silk;

/// <summary>
/// Plugin that creates and manages a Silk.NET window.
/// </summary>
/// <remarks>
/// <para>
/// This plugin must be installed before <c>SilkGraphicsPlugin</c> or <c>SilkInputPlugin</c>,
/// as both depend on the <see cref="ISilkWindowProvider"/> extension this plugin provides.
/// </para>
/// <para>
/// The window is created during installation but does not start running until
/// <see cref="ISilkWindowProvider.Window"/>'s <c>Run()</c> method is called.
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
/// // Get the window provider to access the window
/// var windowProvider = world.GetExtension&lt;ISilkWindowProvider&gt;();
///
/// // Hook up world updates to window events
/// windowProvider.Window.OnUpdate += deltaTime =&gt; world.Update((float)deltaTime);
/// windowProvider.Window.OnRender += deltaTime =&gt; world.Render((float)deltaTime);
///
/// // Start the window loop (blocks until window closes)
/// windowProvider.Window.Run();
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
        context.SetExtension<ISilkWindowProvider>(provider);
    }

    /// <inheritdoc />
    public void Uninstall(IPluginContext context)
    {
        context.RemoveExtension<ISilkWindowProvider>();
        provider?.Dispose();
        provider = null;
    }
}
