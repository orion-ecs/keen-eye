namespace KeenEyes.Persistence.Encryption;

/// <summary>
/// A pass-through encryption provider that does not encrypt data.
/// </summary>
/// <remarks>
/// <para>
/// Use this provider when encryption is not needed. Data is returned as-is.
/// </para>
/// </remarks>
public sealed class NoEncryptionProvider : IEncryptionProvider
{
    /// <summary>
    /// Gets the singleton instance of the no-encryption provider.
    /// </summary>
    public static NoEncryptionProvider Instance { get; } = new();

    private NoEncryptionProvider()
    {
    }

    /// <inheritdoc />
    public string Name => "None";

    /// <inheritdoc />
    public bool IsEncrypted => false;

    /// <inheritdoc />
    public byte[] Encrypt(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return data;
    }

    /// <inheritdoc />
    public byte[] Decrypt(byte[] encryptedData)
    {
        ArgumentNullException.ThrowIfNull(encryptedData);
        return encryptedData;
    }

    /// <inheritdoc />
    public Task<byte[]> EncryptAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);
        return Task.FromResult(data);
    }

    /// <inheritdoc />
    public Task<byte[]> DecryptAsync(byte[] encryptedData, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(encryptedData);
        return Task.FromResult(encryptedData);
    }
}
