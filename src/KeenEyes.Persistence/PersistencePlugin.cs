namespace KeenEyes.Persistence;

/// <summary>
/// Plugin that adds encrypted persistence capabilities to a KeenEyes world.
/// </summary>
/// <remarks>
/// <para>
/// The PersistencePlugin extends the World's built-in save functionality with
/// optional AES-256 encryption. When encryption is enabled, all save data is
/// encrypted before being written to disk and decrypted when loaded.
/// </para>
/// <para>
/// The plugin exposes an <see cref="EncryptedPersistenceApi"/> extension that
/// provides encrypted save/load operations.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Install with encryption
/// var config = PersistenceConfig.WithEncryption("myPassword");
/// world.InstallPlugin(new PersistencePlugin(config));
///
/// // Save encrypted
/// var persistence = world.GetExtension&lt;EncryptedPersistenceApi&gt;();
/// persistence.SaveToSlot("slot1", serializer);
///
/// // Load encrypted
/// var (info, entityMap) = persistence.LoadFromSlot("slot1", serializer);
/// </code>
/// </example>
/// <param name="config">Configuration options for the persistence plugin.</param>
public sealed class PersistencePlugin(PersistenceConfig? config = null) : IWorldPlugin
{
    private readonly PersistenceConfig config = config ?? PersistenceConfig.Default;

    /// <summary>
    /// Creates a new persistence plugin with default options (no encryption).
    /// </summary>
    public PersistencePlugin() : this(null)
    {
    }

    /// <inheritdoc />
    public string Name => "Persistence";

    /// <inheritdoc />
    public void Install(IPluginContext context)
    {
        if (context.World is not World world)
        {
            throw new InvalidOperationException("PersistencePlugin requires a concrete World instance");
        }

        // Configure save directory if specified
        if (config.SaveDirectory is not null)
        {
            world.SaveDirectory = config.SaveDirectory;
        }

        // Create and register the encrypted persistence API
        var api = new EncryptedPersistenceApi(world, config);
        context.SetExtension(api);
    }

    /// <inheritdoc />
    public void Uninstall(IPluginContext context)
    {
        context.RemoveExtension<EncryptedPersistenceApi>();
    }
}
