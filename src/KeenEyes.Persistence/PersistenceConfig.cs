using KeenEyes.Persistence.Encryption;

namespace KeenEyes.Persistence;

/// <summary>
/// Configuration options for the persistence plugin.
/// </summary>
/// <remarks>
/// <para>
/// Configure encryption, save directory, and other persistence settings.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Enable encryption with password
/// var config = new PersistenceConfig
/// {
///     EncryptionProvider = new AesEncryptionProvider("secret"),
///     SaveDirectory = "encrypted_saves"
/// };
/// world.InstallPlugin(new PersistencePlugin(config));
/// </code>
/// </example>
public sealed record PersistenceConfig
{
    /// <summary>
    /// Gets or sets the encryption provider.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="NoEncryptionProvider.Instance"/> (no encryption).
    /// </remarks>
    public IEncryptionProvider EncryptionProvider { get; init; } = NoEncryptionProvider.Instance;

    /// <summary>
    /// Gets or sets the directory where encrypted saves are stored.
    /// </summary>
    /// <remarks>
    /// If null, uses the world's default <see cref="World.SaveDirectory"/>.
    /// </remarks>
    public string? SaveDirectory { get; init; }

    /// <summary>
    /// Gets the default configuration (no encryption).
    /// </summary>
    public static PersistenceConfig Default { get; } = new();

    /// <summary>
    /// Creates a configuration with AES encryption using the specified password.
    /// </summary>
    /// <param name="password">The encryption password.</param>
    /// <param name="saveDirectory">Optional save directory override.</param>
    /// <returns>A configuration with AES-256 encryption enabled.</returns>
    public static PersistenceConfig WithEncryption(string password, string? saveDirectory = null)
    {
        return new PersistenceConfig
        {
            EncryptionProvider = new AesEncryptionProvider(password),
            SaveDirectory = saveDirectory
        };
    }
}
