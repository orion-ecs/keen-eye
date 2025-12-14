using System.Security.Cryptography;
using KeenEyes.Persistence.Encryption;

namespace KeenEyes.Persistence.Tests;

/// <summary>
/// Tests for the encryption providers.
/// </summary>
public class EncryptionProviderTests
{
    #region NoEncryptionProvider Tests

    [Fact]
    public void NoEncryption_Name_ReturnsNone()
    {
        Assert.Equal("None", NoEncryptionProvider.Instance.Name);
    }

    [Fact]
    public void NoEncryption_IsEncrypted_ReturnsFalse()
    {
        Assert.False(NoEncryptionProvider.Instance.IsEncrypted);
    }

    [Fact]
    public void NoEncryption_Encrypt_ReturnsOriginalData()
    {
        var data = new byte[] { 1, 2, 3, 4, 5 };

        var result = NoEncryptionProvider.Instance.Encrypt(data);

        Assert.Same(data, result);
    }

    [Fact]
    public void NoEncryption_Decrypt_ReturnsOriginalData()
    {
        var data = new byte[] { 1, 2, 3, 4, 5 };

        var result = NoEncryptionProvider.Instance.Decrypt(data);

        Assert.Same(data, result);
    }

    [Fact]
    public async Task NoEncryption_EncryptAsync_ReturnsOriginalData()
    {
        var data = new byte[] { 1, 2, 3, 4, 5 };

        var result = await NoEncryptionProvider.Instance.EncryptAsync(data, TestContext.Current.CancellationToken);

        Assert.Same(data, result);
    }

    [Fact]
    public void NoEncryption_Encrypt_ThrowsOnNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            NoEncryptionProvider.Instance.Encrypt(null!));
    }

    #endregion

    #region AesEncryptionProvider Tests

    [Fact]
    public void Aes_Name_ReturnsAes256()
    {
        var provider = new AesEncryptionProvider("password");
        Assert.Equal("AES-256", provider.Name);
    }

    [Fact]
    public void Aes_IsEncrypted_ReturnsTrue()
    {
        var provider = new AesEncryptionProvider("password");
        Assert.True(provider.IsEncrypted);
    }

    [Fact]
    public void Aes_Constructor_ThrowsOnNullPassword()
    {
        Assert.ThrowsAny<ArgumentException>(() => new AesEncryptionProvider(null!));
    }

    [Fact]
    public void Aes_Constructor_ThrowsOnEmptyPassword()
    {
        Assert.Throws<ArgumentException>(() => new AesEncryptionProvider(""));
    }

    [Fact]
    public void Aes_RoundTrip_SamePassword_ReturnsOriginalData()
    {
        var provider = new AesEncryptionProvider("testPassword123");
        var originalData = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

        var encrypted = provider.Encrypt(originalData);
        var decrypted = provider.Decrypt(encrypted);

        Assert.Equal(originalData, decrypted);
    }

    [Fact]
    public void Aes_Encrypt_ProducesDataLargerThanInput()
    {
        var provider = new AesEncryptionProvider("password");
        var data = new byte[] { 1, 2, 3, 4, 5 };

        var encrypted = provider.Encrypt(data);

        // Encrypted data includes: salt (16) + IV (16) + padded ciphertext
        Assert.True(encrypted.Length > data.Length);
        Assert.True(encrypted.Length >= 32); // At minimum salt + IV
    }

    [Fact]
    public void Aes_EncryptTwice_ProducesDifferentCiphertext()
    {
        var provider = new AesEncryptionProvider("password");
        var data = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

        var encrypted1 = provider.Encrypt(data);
        var encrypted2 = provider.Encrypt(data);

        // Different salt and IV each time means different ciphertext
        Assert.NotEqual(encrypted1, encrypted2);
    }

    [Fact]
    public void Aes_Decrypt_WrongPassword_ThrowsCryptographicException()
    {
        var provider1 = new AesEncryptionProvider("correctPassword");
        var provider2 = new AesEncryptionProvider("wrongPassword");
        var data = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

        var encrypted = provider1.Encrypt(data);

        Assert.Throws<CryptographicException>(() => provider2.Decrypt(encrypted));
    }

    [Fact]
    public void Aes_Decrypt_TruncatedData_ThrowsCryptographicException()
    {
        var provider = new AesEncryptionProvider("password");

        // Data too short (less than salt + IV = 32 bytes)
        var truncatedData = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

        Assert.Throws<CryptographicException>(() => provider.Decrypt(truncatedData));
    }

    [Fact]
    public void Aes_Decrypt_CorruptedData_ThrowsCryptographicException()
    {
        var provider = new AesEncryptionProvider("password");
        var data = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

        var encrypted = provider.Encrypt(data);

        // Corrupt some bytes in the ciphertext portion (after salt + IV)
        encrypted[40] ^= 0xFF;
        encrypted[41] ^= 0xFF;

        Assert.Throws<CryptographicException>(() => provider.Decrypt(encrypted));
    }

    [Fact]
    public void Aes_Encrypt_ThrowsOnNull()
    {
        var provider = new AesEncryptionProvider("password");
        Assert.Throws<ArgumentNullException>(() => provider.Encrypt(null!));
    }

    [Fact]
    public void Aes_Decrypt_ThrowsOnNull()
    {
        var provider = new AesEncryptionProvider("password");
        Assert.Throws<ArgumentNullException>(() => provider.Decrypt(null!));
    }

    [Fact]
    public void Aes_RoundTrip_LargeData_ReturnsOriginalData()
    {
        var provider = new AesEncryptionProvider("testPassword");
        var largeData = new byte[10000];
        Random.Shared.NextBytes(largeData);

        var encrypted = provider.Encrypt(largeData);
        var decrypted = provider.Decrypt(encrypted);

        Assert.Equal(largeData, decrypted);
    }

    [Fact]
    public void Aes_RoundTrip_EmptyData_ReturnsEmptyData()
    {
        var provider = new AesEncryptionProvider("password");
        var emptyData = Array.Empty<byte>();

        var encrypted = provider.Encrypt(emptyData);
        var decrypted = provider.Decrypt(encrypted);

        Assert.Empty(decrypted);
    }

    [Fact]
    public async Task Aes_RoundTripAsync_ReturnsOriginalData()
    {
        var provider = new AesEncryptionProvider("asyncTest");
        var data = new byte[] { 10, 20, 30, 40, 50 };

        var encrypted = await provider.EncryptAsync(data, TestContext.Current.CancellationToken);
        var decrypted = await provider.DecryptAsync(encrypted, TestContext.Current.CancellationToken);

        Assert.Equal(data, decrypted);
    }

    [Fact]
    public void Aes_RoundTrip_UnicodePassword_ReturnsOriginalData()
    {
        var provider = new AesEncryptionProvider("p@$$w0rd!");
        var data = new byte[] { 1, 2, 3, 4, 5 };

        var encrypted = provider.Encrypt(data);
        var decrypted = provider.Decrypt(encrypted);

        Assert.Equal(data, decrypted);
    }

    [Fact]
    public void Aes_RoundTrip_LongPassword_ReturnsOriginalData()
    {
        var longPassword = new string('x', 1000);
        var provider = new AesEncryptionProvider(longPassword);
        var data = new byte[] { 1, 2, 3, 4, 5 };

        var encrypted = provider.Encrypt(data);
        var decrypted = provider.Decrypt(encrypted);

        Assert.Equal(data, decrypted);
    }

    [Fact]
    public void Aes_DifferentPasswords_ProduceDifferentCiphertext()
    {
        var provider1 = new AesEncryptionProvider("password1");
        var provider2 = new AesEncryptionProvider("password2");
        var data = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

        var encrypted1 = provider1.Encrypt(data);
        var encrypted2 = provider2.Encrypt(data);

        // Even with same plaintext, different passwords produce different ciphertext
        // (also different salts, but ignoring that)
        Assert.NotEqual(encrypted1, encrypted2);
    }

    #endregion
}
