namespace KeenEyes.Capabilities;

/// <summary>
/// Capability interface for configuring world persistence settings.
/// </summary>
/// <remarks>
/// <para>
/// This capability provides access to persistence configuration settings
/// such as the save directory. Plugins that need to configure persistence
/// should request this capability via <see cref="IPluginContext.GetCapability{T}"/>
/// rather than casting to the concrete World type.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public void Install(IPluginContext context)
/// {
///     if (context.TryGetCapability&lt;IPersistenceCapability&gt;(out var persistence))
///     {
///         if (config.SaveDirectory is not null)
///         {
///             persistence.SaveDirectory = config.SaveDirectory;
///         }
///     }
/// }
/// </code>
/// </example>
public interface IPersistenceCapability
{
    /// <summary>
    /// Gets or sets the directory used for save files.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Defaults to a "saves" subdirectory in the current working directory.
    /// Setting this property allows plugins to customize where save data is stored.
    /// </para>
    /// </remarks>
    string SaveDirectory { get; set; }
}
