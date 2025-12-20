namespace KeenEyes.Testing.Plugins;

/// <summary>
/// A configurable mock plugin for testing plugin-dependent code.
/// </summary>
/// <remarks>
/// <para>
/// MockPlugin provides a flexible way to test code that depends on plugins. It tracks
/// install/uninstall calls and allows custom installation behavior via callbacks.
/// </para>
/// <para>
/// Use this when testing:
/// </para>
/// <list type="bullet">
/// <item>Plugin installation order</item>
/// <item>Plugin dependency checking</item>
/// <item>World lifecycle with plugins</item>
/// <item>Extension registration patterns</item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Basic usage - track installation
/// var plugin = new MockPlugin("TestPlugin");
/// world.InstallPlugin(plugin);
///
/// Assert.True(plugin.WasInstalled);
/// Assert.Equal(1, plugin.InstallCount);
///
/// // With custom behavior
/// var plugin = new MockPlugin("MyPlugin", context =>
/// {
///     context.SetExtension(new MyApi());
/// });
/// </code>
/// </example>
public class MockPlugin : IWorldPlugin
{
    private readonly Action<IPluginContext>? installAction;
    private readonly Action<IPluginContext>? uninstallAction;

    /// <summary>
    /// Creates a new mock plugin with the specified name.
    /// </summary>
    /// <param name="name">The unique name of this plugin.</param>
    /// <param name="installAction">Optional action to execute during installation.</param>
    /// <param name="uninstallAction">Optional action to execute during uninstallation.</param>
    public MockPlugin(
        string name,
        Action<IPluginContext>? installAction = null,
        Action<IPluginContext>? uninstallAction = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        Name = name;
        this.installAction = installAction;
        this.uninstallAction = uninstallAction;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <summary>
    /// Gets whether the plugin has been installed at least once.
    /// </summary>
    public bool WasInstalled { get; private set; }

    /// <summary>
    /// Gets whether the plugin has been uninstalled at least once.
    /// </summary>
    public bool WasUninstalled { get; private set; }

    /// <summary>
    /// Gets the number of times Install has been called.
    /// </summary>
    public int InstallCount { get; private set; }

    /// <summary>
    /// Gets the number of times Uninstall has been called.
    /// </summary>
    public int UninstallCount { get; private set; }

    /// <summary>
    /// Gets whether the plugin is currently installed (installed more times than uninstalled).
    /// </summary>
    public bool IsInstalled => InstallCount > UninstallCount;

    /// <summary>
    /// Gets the last context passed to Install, if any.
    /// </summary>
    public IPluginContext? LastInstallContext { get; private set; }

    /// <summary>
    /// Gets the last context passed to Uninstall, if any.
    /// </summary>
    public IPluginContext? LastUninstallContext { get; private set; }

    /// <inheritdoc />
    public virtual void Install(IPluginContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        InstallCount++;
        WasInstalled = true;
        LastInstallContext = context;

        installAction?.Invoke(context);
    }

    /// <inheritdoc />
    public virtual void Uninstall(IPluginContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        UninstallCount++;
        WasUninstalled = true;
        LastUninstallContext = context;

        uninstallAction?.Invoke(context);
    }

    /// <summary>
    /// Resets all tracking state.
    /// </summary>
    public void Reset()
    {
        WasInstalled = false;
        WasUninstalled = false;
        InstallCount = 0;
        UninstallCount = 0;
        LastInstallContext = null;
        LastUninstallContext = null;
    }
}

/// <summary>
/// A mock plugin that provides a typed extension during installation.
/// </summary>
/// <typeparam name="TExtension">The type of extension to provide.</typeparam>
/// <remarks>
/// <para>
/// Use this when you need a plugin that automatically registers an extension
/// of a specific type. This is useful for testing code that depends on plugin-provided
/// extensions.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create a mock plugin that provides a PhysicsApi extension
/// var physicsApi = new MockPhysicsApi();
/// var plugin = new MockPlugin&lt;PhysicsApi&gt;("Physics", physicsApi);
///
/// world.InstallPlugin(plugin);
///
/// var api = world.GetExtension&lt;PhysicsApi&gt;();
/// Assert.Same(physicsApi, api);
/// </code>
/// </example>
public class MockPlugin<TExtension> : MockPlugin where TExtension : class
{
    /// <summary>
    /// Creates a new mock plugin that provides the specified extension.
    /// </summary>
    /// <param name="name">The unique name of this plugin.</param>
    /// <param name="extension">The extension instance to register during installation.</param>
    /// <param name="additionalInstallAction">Optional additional action to execute during installation.</param>
    /// <param name="uninstallAction">Optional action to execute during uninstallation.</param>
    public MockPlugin(
        string name,
        TExtension extension,
        Action<IPluginContext>? additionalInstallAction = null,
        Action<IPluginContext>? uninstallAction = null)
        : base(name, null, uninstallAction)
    {
        ArgumentNullException.ThrowIfNull(extension);
        Extension = extension;
        AdditionalInstallAction = additionalInstallAction;
    }

    /// <summary>
    /// Gets the extension instance that this plugin provides.
    /// </summary>
    public TExtension Extension { get; }

    /// <summary>
    /// Gets or sets an additional action to execute during installation.
    /// </summary>
    public Action<IPluginContext>? AdditionalInstallAction { get; set; }

    /// <inheritdoc />
    public override void Install(IPluginContext context)
    {
        base.Install(context);
        context.SetExtension(Extension);
        AdditionalInstallAction?.Invoke(context);
    }
}

/// <summary>
/// A mock plugin that can throw exceptions for testing error handling.
/// </summary>
/// <remarks>
/// <para>
/// Use this when testing how code handles plugin installation/uninstallation failures.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var failingPlugin = new FailingMockPlugin("BadPlugin", new InvalidOperationException("Test error"));
/// Assert.Throws&lt;InvalidOperationException&gt;(() => world.InstallPlugin(failingPlugin));
/// </code>
/// </example>
/// <param name="name">The unique name of this plugin.</param>
/// <param name="installException">Exception to throw during installation, or null.</param>
/// <param name="uninstallException">Exception to throw during uninstallation, or null.</param>
public class FailingMockPlugin(
    string name,
    Exception? installException = null,
    Exception? uninstallException = null) : MockPlugin(name)
{
    /// <inheritdoc />
    public override void Install(IPluginContext context)
    {
        base.Install(context);

        if (installException is not null)
        {
            throw installException;
        }
    }

    /// <inheritdoc />
    public override void Uninstall(IPluginContext context)
    {
        base.Uninstall(context);

        if (uninstallException is not null)
        {
            throw uninstallException;
        }
    }
}
