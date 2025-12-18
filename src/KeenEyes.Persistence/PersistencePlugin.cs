using KeenEyes.Capabilities;

// TODO: Remove this suppression after refactoring to use IWorld interface
#pragma warning disable KEEN050 // IWorld to World cast - legacy code pending refactoring

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
/// <para>
/// <b>Note:</b> This plugin currently requires a concrete World instance because
/// the snapshot serialization APIs are deeply integrated with World internals.
/// Future versions may support custom IWorld implementations.
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
        // Configure save directory via capability if available
        if (config.SaveDirectory is not null &&
            context.TryGetCapability<IPersistenceCapability>(out var persistenceCapability) &&
            persistenceCapability is not null)
        {
            persistenceCapability.SaveDirectory = config.SaveDirectory;
        }

        // EncryptedPersistenceApi requires concrete World for snapshot operations
        // TODO: Abstract snapshot functionality to support custom IWorld implementations
        if (context.World is not World world)
        {
            throw new InvalidOperationException(
                "PersistencePlugin currently requires a concrete World instance. " +
                "Custom IWorld implementations are not yet supported for persistence.");
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
