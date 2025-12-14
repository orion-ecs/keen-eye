using System.Security.Cryptography;

namespace KeenEyes.Persistence.Encryption;

/// <summary>
/// Provides AES-256 encryption for save data using password-based key derivation.
/// </summary>
/// <remarks>
/// <para>
/// Uses AES-256 in CBC mode with PKCS7 padding. Keys are derived from passwords
/// using PBKDF2 with a random salt. The salt and IV are prepended to the encrypted
/// data for self-contained decryption.
/// </para>
/// <para>
/// File format: [Salt (16 bytes)][IV (16 bytes)][Encrypted Data]
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var provider = new AesEncryptionProvider("MySecretPassword");
/// var encrypted = provider.Encrypt(data);
/// var decrypted = provider.Decrypt(encrypted);
/// </code>
/// </example>
public sealed class AesEncryptionProvider : IEncryptionProvider
{
    private const int SaltSize = 16;
    private const int IvSize = 16;
    private const int KeySize = 32; // 256 bits
    private const int Iterations = 100000;
    private static readonly HashAlgorithmName HashAlgorithm = HashAlgorithmName.SHA256;

    private readonly byte[] passwordBytes;

    /// <summary>
    /// Creates a new AES encryption provider with the specified password.
    /// </summary>
    /// <param name="password">The password used to derive the encryption key.</param>
    /// <exception cref="ArgumentException">Thrown when password is null or empty.</exception>
    public AesEncryptionProvider(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);
        passwordBytes = System.Text.Encoding.UTF8.GetBytes(password);
    }

    /// <inheritdoc />
    public string Name => "AES-256";

    /// <inheritdoc />
    public bool IsEncrypted => true;

    /// <inheritdoc />
    public byte[] Encrypt(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        // Generate random salt and IV
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var iv = RandomNumberGenerator.GetBytes(IvSize);

        // Derive key from password
        var key = DeriveKey(salt);

        // Encrypt data
        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        var encryptedData = encryptor.TransformFinalBlock(data, 0, data.Length);

        // Combine salt + IV + encrypted data
        var result = new byte[SaltSize + IvSize + encryptedData.Length];
        Buffer.BlockCopy(salt, 0, result, 0, SaltSize);
        Buffer.BlockCopy(iv, 0, result, SaltSize, IvSize);
        Buffer.BlockCopy(encryptedData, 0, result, SaltSize + IvSize, encryptedData.Length);

        return result;
    }

    /// <inheritdoc />
    public byte[] Decrypt(byte[] encryptedData)
    {
        ArgumentNullException.ThrowIfNull(encryptedData);

        if (encryptedData.Length < SaltSize + IvSize)
        {
            throw new CryptographicException("Encrypted data is too short to contain required headers.");
        }

        // Extract salt, IV, and encrypted content
        var salt = new byte[SaltSize];
        var iv = new byte[IvSize];
        var ciphertext = new byte[encryptedData.Length - SaltSize - IvSize];

        Buffer.BlockCopy(encryptedData, 0, salt, 0, SaltSize);
        Buffer.BlockCopy(encryptedData, SaltSize, iv, 0, IvSize);
        Buffer.BlockCopy(encryptedData, SaltSize + IvSize, ciphertext, 0, ciphertext.Length);

        // Derive key from password
        var key = DeriveKey(salt);

        // Decrypt data
        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
    }

    /// <inheritdoc />
    public Task<byte[]> EncryptAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Encrypt(data));
    }

    /// <inheritdoc />
    public Task<byte[]> DecryptAsync(byte[] encryptedData, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Decrypt(encryptedData));
    }

    /// <summary>
    /// Derives a 256-bit key from the password using PBKDF2.
    /// </summary>
    private byte[] DeriveKey(byte[] salt)
    {
        return Rfc2898DeriveBytes.Pbkdf2(passwordBytes, salt, Iterations, HashAlgorithm, KeySize);
    }
}
