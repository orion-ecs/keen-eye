namespace KeenEyes;

public sealed partial class World
{
    #region Plugins

    /// <summary>
    /// Installs a plugin into this world.
    /// </summary>
    /// <typeparam name="T">The plugin type to install.</typeparam>
    /// <returns>This world for method chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a plugin with the same name is already installed.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The plugin's <see cref="IWorldPlugin.Install"/> method is called during installation.
    /// Systems registered by the plugin are tracked and will be automatically removed
    /// when the plugin is uninstalled.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// world.InstallPlugin&lt;PhysicsPlugin&gt;()
    ///      .InstallPlugin&lt;RenderingPlugin&gt;();
    /// </code>
    /// </example>
    public World InstallPlugin<T>() where T : IWorldPlugin, new()
    {
        pluginManager.InstallPlugin<T>();
        return this;
    }

    /// <summary>
    /// Installs a plugin instance into this world.
    /// </summary>
    /// <param name="plugin">The plugin instance to install.</param>
    /// <returns>This world for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when plugin is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a plugin with the same name is already installed.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Use this overload when you need to pass a pre-configured plugin instance.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var physics = new PhysicsPlugin(gravity: -9.81f);
    /// world.InstallPlugin(physics);
    /// </code>
    /// </example>
    public World InstallPlugin(IWorldPlugin plugin)
    {
        pluginManager.InstallPlugin(plugin);
        return this;
    }

    /// <summary>
    /// Uninstalls a plugin from this world.
    /// </summary>
    /// <typeparam name="T">The plugin type to uninstall.</typeparam>
    /// <returns>True if the plugin was found and uninstalled; false otherwise.</returns>
    /// <remarks>
    /// <para>
    /// The plugin's <see cref="IWorldPlugin.Uninstall"/> method is called during uninstallation.
    /// All systems registered by the plugin are automatically removed and disposed.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// world.UninstallPlugin&lt;PhysicsPlugin&gt;();
    /// </code>
    /// </example>
    public bool UninstallPlugin<T>() where T : IWorldPlugin
        => pluginManager.UninstallPlugin<T>();

    /// <summary>
    /// Uninstalls a plugin by name from this world.
    /// </summary>
    /// <param name="name">The name of the plugin to uninstall.</param>
    /// <returns>True if the plugin was found and uninstalled; false otherwise.</returns>
    public bool UninstallPlugin(string name)
        => pluginManager.UninstallPlugin(name);

    /// <summary>
    /// Gets a plugin of the specified type.
    /// </summary>
    /// <typeparam name="T">The plugin type to retrieve.</typeparam>
    /// <returns>The plugin instance, or null if not found.</returns>
    /// <example>
    /// <code>
    /// var physics = world.GetPlugin&lt;PhysicsPlugin&gt;();
    /// if (physics is not null)
    /// {
    ///     Console.WriteLine($"Physics plugin: {physics.Name}");
    /// }
    /// </code>
    /// </example>
    public T? GetPlugin<T>() where T : class, IWorldPlugin
        => pluginManager.GetPlugin<T>();

    /// <summary>
    /// Gets a plugin by name.
    /// </summary>
    /// <param name="name">The name of the plugin to retrieve.</param>
    /// <returns>The plugin instance, or null if not found.</returns>
    public IWorldPlugin? GetPlugin(string name)
        => pluginManager.GetPlugin(name);

    /// <summary>
    /// Checks if a plugin of the specified type is installed.
    /// </summary>
    /// <typeparam name="T">The plugin type to check for.</typeparam>
    /// <returns>True if the plugin is installed; false otherwise.</returns>
    public bool HasPlugin<T>() where T : IWorldPlugin
        => pluginManager.HasPlugin<T>();

    /// <summary>
    /// Checks if a plugin with the specified name is installed.
    /// </summary>
    /// <param name="name">The name of the plugin to check for.</param>
    /// <returns>True if the plugin is installed; false otherwise.</returns>
    public bool HasPlugin(string name)
        => pluginManager.HasPlugin(name);

    /// <summary>
    /// Gets all installed plugins.
    /// </summary>
    /// <returns>An enumerable of all installed plugins.</returns>
    public IEnumerable<IWorldPlugin> GetPlugins()
        => pluginManager.GetPlugins();

    #endregion
}
