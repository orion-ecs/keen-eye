namespace KeenEyes.Persistence.Encryption;

/// <summary>
/// Provides encryption and decryption operations for save data.
/// </summary>
/// <remarks>
/// <para>
/// Implementations must be thread-safe for use in async save/load operations.
/// </para>
/// </remarks>
public interface IEncryptionProvider
{
    /// <summary>
    /// Gets the name of this encryption provider.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets whether this provider actually encrypts data.
    /// </summary>
    /// <remarks>
    /// Returns false for pass-through providers like <see cref="NoEncryptionProvider"/>.
    /// </remarks>
    bool IsEncrypted { get; }

    /// <summary>
    /// Encrypts the specified data.
    /// </summary>
    /// <param name="data">The plaintext data to encrypt.</param>
    /// <returns>The encrypted data with any necessary headers (IV, salt, etc.).</returns>
    byte[] Encrypt(byte[] data);

    /// <summary>
    /// Decrypts the specified data.
    /// </summary>
    /// <param name="encryptedData">The encrypted data including headers.</param>
    /// <returns>The decrypted plaintext data.</returns>
    /// <exception cref="System.Security.Cryptography.CryptographicException">
    /// Thrown when decryption fails (wrong password, corrupted data, etc.).
    /// </exception>
    byte[] Decrypt(byte[] encryptedData);

    /// <summary>
    /// Encrypts data asynchronously.
    /// </summary>
    /// <param name="data">The plaintext data to encrypt.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The encrypted data with any necessary headers.</returns>
    Task<byte[]> EncryptAsync(byte[] data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Decrypts data asynchronously.
    /// </summary>
    /// <param name="encryptedData">The encrypted data including headers.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The decrypted plaintext data.</returns>
    Task<byte[]> DecryptAsync(byte[] encryptedData, CancellationToken cancellationToken = default);
}
