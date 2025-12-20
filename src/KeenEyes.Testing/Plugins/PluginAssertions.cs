namespace KeenEyes.Testing.Plugins;

/// <summary>
/// Provides fluent assertion methods for verifying plugin behavior.
/// </summary>
/// <remarks>
/// <para>
/// These extension methods provide a fluent interface for asserting on
/// <see cref="MockPlugin"/> and <see cref="MockPluginContext"/> instances.
/// All assertions throw <see cref="PluginAssertionException"/> on failure.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var plugin = new MockPlugin("TestPlugin");
/// world.InstallPlugin(plugin);
///
/// plugin
///     .ShouldBeInstalled()
///     .ShouldHaveBeenInstalledTimes(1);
///
/// var context = new MockPluginContext(plugin);
/// plugin.Install(context);
///
/// context
///     .ShouldHaveRegisteredSystem&lt;PhysicsSystem&gt;()
///     .ShouldHaveSetExtension&lt;PhysicsWorld&gt;();
/// </code>
/// </example>
public static class PluginAssertions
{
    #region MockPlugin Assertions

    /// <summary>
    /// Asserts that the plugin has been installed at least once.
    /// </summary>
    /// <param name="plugin">The mock plugin to check.</param>
    /// <returns>The plugin for method chaining.</returns>
    /// <exception cref="PluginAssertionException">Thrown when the plugin was not installed.</exception>
    public static MockPlugin ShouldBeInstalled(this MockPlugin plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);

        if (!plugin.WasInstalled)
        {
            throw new PluginAssertionException(
                $"Expected plugin '{plugin.Name}' to have been installed, but it was not.");
        }

        return plugin;
    }

    /// <summary>
    /// Asserts that the plugin has not been installed.
    /// </summary>
    /// <param name="plugin">The mock plugin to check.</param>
    /// <returns>The plugin for method chaining.</returns>
    /// <exception cref="PluginAssertionException">Thrown when the plugin was installed.</exception>
    public static MockPlugin ShouldNotBeInstalled(this MockPlugin plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);

        if (plugin.WasInstalled)
        {
            throw new PluginAssertionException(
                $"Expected plugin '{plugin.Name}' to not have been installed, but it was installed {plugin.InstallCount} time(s).");
        }

        return plugin;
    }

    /// <summary>
    /// Asserts that the plugin has been uninstalled at least once.
    /// </summary>
    /// <param name="plugin">The mock plugin to check.</param>
    /// <returns>The plugin for method chaining.</returns>
    /// <exception cref="PluginAssertionException">Thrown when the plugin was not uninstalled.</exception>
    public static MockPlugin ShouldBeUninstalled(this MockPlugin plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);

        if (!plugin.WasUninstalled)
        {
            throw new PluginAssertionException(
                $"Expected plugin '{plugin.Name}' to have been uninstalled, but it was not.");
        }

        return plugin;
    }

    /// <summary>
    /// Asserts that the plugin has not been uninstalled.
    /// </summary>
    /// <param name="plugin">The mock plugin to check.</param>
    /// <returns>The plugin for method chaining.</returns>
    /// <exception cref="PluginAssertionException">Thrown when the plugin was uninstalled.</exception>
    public static MockPlugin ShouldNotBeUninstalled(this MockPlugin plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);

        if (plugin.WasUninstalled)
        {
            throw new PluginAssertionException(
                $"Expected plugin '{plugin.Name}' to not have been uninstalled, but it was uninstalled {plugin.UninstallCount} time(s).");
        }

        return plugin;
    }

    /// <summary>
    /// Asserts that the plugin has been installed exactly the specified number of times.
    /// </summary>
    /// <param name="plugin">The mock plugin to check.</param>
    /// <param name="expectedCount">The expected install count.</param>
    /// <returns>The plugin for method chaining.</returns>
    /// <exception cref="PluginAssertionException">Thrown when the install count doesn't match.</exception>
    public static MockPlugin ShouldHaveBeenInstalledTimes(this MockPlugin plugin, int expectedCount)
    {
        ArgumentNullException.ThrowIfNull(plugin);

        if (plugin.InstallCount != expectedCount)
        {
            throw new PluginAssertionException(
                $"Expected plugin '{plugin.Name}' to have been installed {expectedCount} time(s), but it was installed {plugin.InstallCount} time(s).");
        }

        return plugin;
    }

    /// <summary>
    /// Asserts that the plugin has been uninstalled exactly the specified number of times.
    /// </summary>
    /// <param name="plugin">The mock plugin to check.</param>
    /// <param name="expectedCount">The expected uninstall count.</param>
    /// <returns>The plugin for method chaining.</returns>
    /// <exception cref="PluginAssertionException">Thrown when the uninstall count doesn't match.</exception>
    public static MockPlugin ShouldHaveBeenUninstalledTimes(this MockPlugin plugin, int expectedCount)
    {
        ArgumentNullException.ThrowIfNull(plugin);

        if (plugin.UninstallCount != expectedCount)
        {
            throw new PluginAssertionException(
                $"Expected plugin '{plugin.Name}' to have been uninstalled {expectedCount} time(s), but it was uninstalled {plugin.UninstallCount} time(s).");
        }

        return plugin;
    }

    /// <summary>
    /// Asserts that the plugin is currently in an installed state.
    /// </summary>
    /// <param name="plugin">The mock plugin to check.</param>
    /// <returns>The plugin for method chaining.</returns>
    /// <exception cref="PluginAssertionException">Thrown when the plugin is not currently installed.</exception>
    public static MockPlugin ShouldBeCurrentlyInstalled(this MockPlugin plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);

        if (!plugin.IsInstalled)
        {
            throw new PluginAssertionException(
                $"Expected plugin '{plugin.Name}' to be currently installed, but it is not (installed: {plugin.InstallCount}, uninstalled: {plugin.UninstallCount}).");
        }

        return plugin;
    }

    #endregion

    #region MockPluginContext Assertions

    /// <summary>
    /// Asserts that a system of the specified type was registered.
    /// </summary>
    /// <typeparam name="T">The system type to check.</typeparam>
    /// <param name="context">The mock plugin context to check.</param>
    /// <returns>The context for method chaining.</returns>
    /// <exception cref="PluginAssertionException">Thrown when the system was not registered.</exception>
    public static MockPluginContext ShouldHaveRegisteredSystem<T>(this MockPluginContext context)
        where T : ISystem
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.WasSystemRegistered<T>())
        {
            throw new PluginAssertionException(
                $"Expected system '{typeof(T).Name}' to have been registered, but it was not.");
        }

        return context;
    }

    /// <summary>
    /// Asserts that a system of the specified type was not registered.
    /// </summary>
    /// <typeparam name="T">The system type to check.</typeparam>
    /// <param name="context">The mock plugin context to check.</param>
    /// <returns>The context for method chaining.</returns>
    /// <exception cref="PluginAssertionException">Thrown when the system was registered.</exception>
    public static MockPluginContext ShouldNotHaveRegisteredSystem<T>(this MockPluginContext context)
        where T : ISystem
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.WasSystemRegistered<T>())
        {
            throw new PluginAssertionException(
                $"Expected system '{typeof(T).Name}' to not have been registered, but it was.");
        }

        return context;
    }

    /// <summary>
    /// Asserts that a system of the specified type was registered at a specific phase.
    /// </summary>
    /// <typeparam name="T">The system type to check.</typeparam>
    /// <param name="context">The mock plugin context to check.</param>
    /// <param name="expectedPhase">The expected phase.</param>
    /// <returns>The context for method chaining.</returns>
    /// <exception cref="PluginAssertionException">Thrown when the system was not registered at the expected phase.</exception>
    public static MockPluginContext ShouldHaveRegisteredSystemAtPhase<T>(
        this MockPluginContext context,
        SystemPhase expectedPhase)
        where T : ISystem
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.WasSystemRegisteredAtPhase<T>(expectedPhase))
        {
            var registration = context.GetSystemRegistration<T>();
            if (registration is null)
            {
                throw new PluginAssertionException(
                    $"Expected system '{typeof(T).Name}' to have been registered at phase {expectedPhase}, but it was not registered at all.");
            }

            throw new PluginAssertionException(
                $"Expected system '{typeof(T).Name}' to have been registered at phase {expectedPhase}, but it was registered at phase {registration.Value.Phase}.");
        }

        return context;
    }

    /// <summary>
    /// Asserts that an extension of the specified type was set.
    /// </summary>
    /// <typeparam name="T">The extension type to check.</typeparam>
    /// <param name="context">The mock plugin context to check.</param>
    /// <returns>The context for method chaining.</returns>
    /// <exception cref="PluginAssertionException">Thrown when the extension was not set.</exception>
    public static MockPluginContext ShouldHaveSetExtension<T>(this MockPluginContext context)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.WasExtensionSet<T>())
        {
            throw new PluginAssertionException(
                $"Expected extension '{typeof(T).Name}' to have been set, but it was not.");
        }

        return context;
    }

    /// <summary>
    /// Asserts that an extension of the specified type was not set.
    /// </summary>
    /// <typeparam name="T">The extension type to check.</typeparam>
    /// <param name="context">The mock plugin context to check.</param>
    /// <returns>The context for method chaining.</returns>
    /// <exception cref="PluginAssertionException">Thrown when the extension was set.</exception>
    public static MockPluginContext ShouldNotHaveSetExtension<T>(this MockPluginContext context)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.WasExtensionSet<T>())
        {
            throw new PluginAssertionException(
                $"Expected extension '{typeof(T).Name}' to not have been set, but it was.");
        }

        return context;
    }

    /// <summary>
    /// Asserts that a component of the specified type was registered.
    /// </summary>
    /// <typeparam name="T">The component type to check.</typeparam>
    /// <param name="context">The mock plugin context to check.</param>
    /// <returns>The context for method chaining.</returns>
    /// <exception cref="PluginAssertionException">Thrown when the component was not registered.</exception>
    public static MockPluginContext ShouldHaveRegisteredComponent<T>(this MockPluginContext context)
        where T : struct, IComponent
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.WasComponentRegistered<T>())
        {
            throw new PluginAssertionException(
                $"Expected component '{typeof(T).Name}' to have been registered, but it was not.");
        }

        return context;
    }

    /// <summary>
    /// Asserts that exactly the specified number of systems were registered.
    /// </summary>
    /// <param name="context">The mock plugin context to check.</param>
    /// <param name="expectedCount">The expected number of systems.</param>
    /// <returns>The context for method chaining.</returns>
    /// <exception cref="PluginAssertionException">Thrown when the count doesn't match.</exception>
    public static MockPluginContext ShouldHaveRegisteredSystemCount(
        this MockPluginContext context,
        int expectedCount)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.RegisteredSystems.Count != expectedCount)
        {
            throw new PluginAssertionException(
                $"Expected {expectedCount} system(s) to have been registered, but {context.RegisteredSystems.Count} were.");
        }

        return context;
    }

    /// <summary>
    /// Asserts that exactly the specified number of extensions were set.
    /// </summary>
    /// <param name="context">The mock plugin context to check.</param>
    /// <param name="expectedCount">The expected number of extensions.</param>
    /// <returns>The context for method chaining.</returns>
    /// <exception cref="PluginAssertionException">Thrown when the count doesn't match.</exception>
    public static MockPluginContext ShouldHaveSetExtensionCount(
        this MockPluginContext context,
        int expectedCount)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.RegisteredExtensions.Count != expectedCount)
        {
            throw new PluginAssertionException(
                $"Expected {expectedCount} extension(s) to have been set, but {context.RegisteredExtensions.Count} were.");
        }

        return context;
    }

    #endregion
}

/// <summary>
/// Exception thrown when a plugin assertion fails.
/// </summary>
public class PluginAssertionException : Exception
{
    /// <summary>
    /// Creates a new plugin assertion exception with the specified message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public PluginAssertionException(string message) : base(message)
    {
    }

    /// <summary>
    /// Creates a new plugin assertion exception with the specified message and inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public PluginAssertionException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
