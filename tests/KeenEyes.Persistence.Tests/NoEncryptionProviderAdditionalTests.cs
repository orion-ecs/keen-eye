using KeenEyes.Persistence.Encryption;

namespace KeenEyes.Persistence.Tests;

/// <summary>
/// Additional tests for NoEncryptionProvider to improve coverage.
/// </summary>
public class NoEncryptionProviderAdditionalTests
{
    [Fact]
    public async Task NoEncryption_DecryptAsync_ReturnsOriginalData()
    {
        var data = new byte[] { 1, 2, 3, 4, 5 };

        var result = await NoEncryptionProvider.Instance.DecryptAsync(data, TestContext.Current.CancellationToken);

        Assert.Same(data, result);
    }

    [Fact]
    public void NoEncryption_Decrypt_ThrowsOnNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            NoEncryptionProvider.Instance.Decrypt(null!));
    }

    [Fact]
    public async Task NoEncryption_EncryptAsync_ThrowsOnNull()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await NoEncryptionProvider.Instance.EncryptAsync(null!, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task NoEncryption_DecryptAsync_ThrowsOnNull()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await NoEncryptionProvider.Instance.DecryptAsync(null!, TestContext.Current.CancellationToken));
    }
}
