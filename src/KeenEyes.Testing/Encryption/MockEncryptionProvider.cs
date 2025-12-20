using KeenEyes.Persistence.Encryption;

namespace KeenEyes.Testing.Encryption;

/// <summary>
/// Modes for the mock encryption provider.
/// </summary>
public enum MockEncryptionMode
{
    /// <summary>
    /// Data passes through unchanged. IsEncrypted returns false.
    /// </summary>
    PassThrough,

    /// <summary>
    /// Data is XOR'd with a key for reversible transformation. IsEncrypted returns true.
    /// </summary>
    Reversible,

    /// <summary>
    /// Data is stored but returned unchanged. Useful for tracking operations.
    /// </summary>
    Tracking
}

/// <summary>
/// A mock implementation of <see cref="IEncryptionProvider"/> for testing encryption
/// and decryption operations without real cryptographic operations.
/// </summary>
/// <remarks>
/// <para>
/// MockEncryptionProvider supports multiple modes for different testing scenarios:
/// <list type="bullet">
///   <item><description><see cref="MockEncryptionMode.PassThrough"/>: Data unchanged</description></item>
///   <item><description><see cref="MockEncryptionMode.Reversible"/>: XOR-based transformation</description></item>
///   <item><description><see cref="MockEncryptionMode.Tracking"/>: Stores operations for verification</description></item>
/// </list>
/// </para>
/// <para>
/// Use the <see cref="Operations"/> list to verify encryption/decryption was called.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var crypto = new MockEncryptionProvider();
/// crypto.Mode = MockEncryptionMode.Reversible;
///
/// var data = Encoding.UTF8.GetBytes("secret");
/// var encrypted = crypto.Encrypt(data);
/// var decrypted = crypto.Decrypt(encrypted);
///
/// decrypted.Should().BeEquivalentTo(data);
/// crypto.EncryptCount.Should().Be(1);
/// </code>
/// </example>
public sealed class MockEncryptionProvider : IEncryptionProvider
{
    private readonly byte[] xorKey = [0x42, 0x13, 0x37, 0xAB, 0xCD, 0xEF, 0x12, 0x34];

    /// <summary>
    /// Gets or sets the mock encryption mode.
    /// </summary>
    public MockEncryptionMode Mode { get; set; } = MockEncryptionMode.PassThrough;

    /// <summary>
    /// Gets the list of all recorded operations.
    /// </summary>
    public List<EncryptionOperation> Operations { get; } = [];

    /// <summary>
    /// Gets the number of times Encrypt was called.
    /// </summary>
    public int EncryptCount { get; private set; }

    /// <summary>
    /// Gets the number of times Decrypt was called.
    /// </summary>
    public int DecryptCount { get; private set; }

    /// <summary>
    /// Gets or sets whether encryption should fail with an exception.
    /// </summary>
    public bool ShouldFailEncrypt { get; set; }

    /// <summary>
    /// Gets or sets whether decryption should fail with an exception.
    /// </summary>
    public bool ShouldFailDecrypt { get; set; }

    /// <summary>
    /// Gets or sets a custom encryption function.
    /// </summary>
    public Func<byte[], byte[]>? CustomEncrypt { get; set; }

    /// <summary>
    /// Gets or sets a custom decryption function.
    /// </summary>
    public Func<byte[], byte[]>? CustomDecrypt { get; set; }

    #region IEncryptionProvider Implementation

    /// <inheritdoc />
    public string Name => "MockEncryptionProvider";

    /// <inheritdoc />
    public bool IsEncrypted => Mode != MockEncryptionMode.PassThrough;

    /// <inheritdoc />
    public byte[] Encrypt(byte[] data)
    {
        EncryptCount++;
        Operations.Add(new EncryptionOperation(OperationType.Encrypt, data.Length, DateTime.UtcNow));

        if (ShouldFailEncrypt)
        {
            throw new InvalidOperationException("Mock encryption failed (ShouldFailEncrypt = true)");
        }

        if (CustomEncrypt != null)
        {
            return CustomEncrypt(data);
        }

        return Mode switch
        {
            MockEncryptionMode.PassThrough => data,
            MockEncryptionMode.Reversible => XorTransform(data),
            MockEncryptionMode.Tracking => data,
            _ => data
        };
    }

    /// <inheritdoc />
    public byte[] Decrypt(byte[] encryptedData)
    {
        DecryptCount++;
        Operations.Add(new EncryptionOperation(OperationType.Decrypt, encryptedData.Length, DateTime.UtcNow));

        if (ShouldFailDecrypt)
        {
            throw new System.Security.Cryptography.CryptographicException("Mock decryption failed (ShouldFailDecrypt = true)");
        }

        if (CustomDecrypt != null)
        {
            return CustomDecrypt(encryptedData);
        }

        return Mode switch
        {
            MockEncryptionMode.PassThrough => encryptedData,
            MockEncryptionMode.Reversible => XorTransform(encryptedData),
            MockEncryptionMode.Tracking => encryptedData,
            _ => encryptedData
        };
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

    #endregion

    #region Test Control

    /// <summary>
    /// Resets all tracking state.
    /// </summary>
    public void Reset()
    {
        Operations.Clear();
        EncryptCount = 0;
        DecryptCount = 0;
        ShouldFailEncrypt = false;
        ShouldFailDecrypt = false;
        CustomEncrypt = null;
        CustomDecrypt = null;
        Mode = MockEncryptionMode.PassThrough;
    }

    /// <summary>
    /// Clears only the operations list.
    /// </summary>
    public void ClearOperations()
    {
        Operations.Clear();
        EncryptCount = 0;
        DecryptCount = 0;
    }

    private byte[] XorTransform(byte[] data)
    {
        var result = new byte[data.Length];
        for (int i = 0; i < data.Length; i++)
        {
            result[i] = (byte)(data[i] ^ xorKey[i % xorKey.Length]);
        }
        return result;
    }

    #endregion
}

/// <summary>
/// Type of encryption operation.
/// </summary>
public enum OperationType
{
    /// <summary>Encryption operation.</summary>
    Encrypt,

    /// <summary>Decryption operation.</summary>
    Decrypt
}

/// <summary>
/// A recorded encryption operation.
/// </summary>
/// <param name="Type">The operation type.</param>
/// <param name="DataSize">The size of the data in bytes.</param>
/// <param name="Timestamp">When the operation occurred.</param>
public sealed record EncryptionOperation(OperationType Type, int DataSize, DateTime Timestamp);
