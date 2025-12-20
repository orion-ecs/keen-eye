using System.Text;
using KeenEyes.Testing.Encryption;

namespace KeenEyes.Testing.Tests.Encryption;

public class MockEncryptionProviderTests
{
    #region Properties

    [Fact]
    public void Name_ReturnsMockEncryptionProvider()
    {
        var provider = new MockEncryptionProvider();

        provider.Name.ShouldBe("MockEncryptionProvider");
    }

    [Fact]
    public void IsEncrypted_PassThrough_ReturnsFalse()
    {
        var provider = new MockEncryptionProvider { Mode = MockEncryptionMode.PassThrough };

        provider.IsEncrypted.ShouldBeFalse();
    }

    [Fact]
    public void IsEncrypted_Reversible_ReturnsTrue()
    {
        var provider = new MockEncryptionProvider { Mode = MockEncryptionMode.Reversible };

        provider.IsEncrypted.ShouldBeTrue();
    }

    [Fact]
    public void IsEncrypted_Tracking_ReturnsTrue()
    {
        var provider = new MockEncryptionProvider { Mode = MockEncryptionMode.Tracking };

        provider.IsEncrypted.ShouldBeTrue();
    }

    #endregion

    #region PassThrough Mode

    [Fact]
    public void Encrypt_PassThrough_ReturnsUnchangedData()
    {
        var provider = new MockEncryptionProvider { Mode = MockEncryptionMode.PassThrough };
        var data = Encoding.UTF8.GetBytes("secret");

        var result = provider.Encrypt(data);

        result.ShouldBe(data);
    }

    [Fact]
    public void Decrypt_PassThrough_ReturnsUnchangedData()
    {
        var provider = new MockEncryptionProvider { Mode = MockEncryptionMode.PassThrough };
        var data = Encoding.UTF8.GetBytes("secret");

        var result = provider.Decrypt(data);

        result.ShouldBe(data);
    }

    #endregion

    #region Reversible Mode

    [Fact]
    public void Encrypt_Reversible_TransformsData()
    {
        var provider = new MockEncryptionProvider { Mode = MockEncryptionMode.Reversible };
        var data = Encoding.UTF8.GetBytes("secret");

        var encrypted = provider.Encrypt(data);

        encrypted.ShouldNotBe(data);
    }

    [Fact]
    public void Decrypt_Reversible_ReversesEncryption()
    {
        var provider = new MockEncryptionProvider { Mode = MockEncryptionMode.Reversible };
        var original = Encoding.UTF8.GetBytes("secret message");

        var encrypted = provider.Encrypt(original);
        var decrypted = provider.Decrypt(encrypted);

        decrypted.ShouldBe(original);
    }

    [Fact]
    public void RoundTrip_Reversible_PreservesData()
    {
        var provider = new MockEncryptionProvider { Mode = MockEncryptionMode.Reversible };
        var original = "The quick brown fox jumps over the lazy dog";
        var data = Encoding.UTF8.GetBytes(original);

        var encrypted = provider.Encrypt(data);
        var decrypted = provider.Decrypt(encrypted);

        Encoding.UTF8.GetString(decrypted).ShouldBe(original);
    }

    #endregion

    #region Tracking Mode

    [Fact]
    public void Encrypt_Tracking_ReturnsUnchangedData()
    {
        var provider = new MockEncryptionProvider { Mode = MockEncryptionMode.Tracking };
        var data = Encoding.UTF8.GetBytes("secret");

        var result = provider.Encrypt(data);

        result.ShouldBe(data);
    }

    [Fact]
    public void Decrypt_Tracking_ReturnsUnchangedData()
    {
        var provider = new MockEncryptionProvider { Mode = MockEncryptionMode.Tracking };
        var data = Encoding.UTF8.GetBytes("secret");

        var result = provider.Decrypt(data);

        result.ShouldBe(data);
    }

    #endregion

    #region Operation Tracking

    [Fact]
    public void Encrypt_IncrementsEncryptCount()
    {
        var provider = new MockEncryptionProvider();

        provider.Encrypt([1, 2, 3]);
        provider.Encrypt([4, 5, 6]);

        provider.EncryptCount.ShouldBe(2);
    }

    [Fact]
    public void Decrypt_IncrementsDecryptCount()
    {
        var provider = new MockEncryptionProvider();

        provider.Decrypt([1, 2, 3]);
        provider.Decrypt([4, 5, 6]);

        provider.DecryptCount.ShouldBe(2);
    }

    [Fact]
    public void Encrypt_RecordsOperation()
    {
        var provider = new MockEncryptionProvider();
        var data = new byte[] { 1, 2, 3, 4, 5 };

        provider.Encrypt(data);

        provider.Operations.Count.ShouldBe(1);
        provider.Operations[0].Type.ShouldBe(OperationType.Encrypt);
        provider.Operations[0].DataSize.ShouldBe(5);
    }

    [Fact]
    public void Decrypt_RecordsOperation()
    {
        var provider = new MockEncryptionProvider();
        var data = new byte[] { 1, 2, 3 };

        provider.Decrypt(data);

        provider.Operations.Count.ShouldBe(1);
        provider.Operations[0].Type.ShouldBe(OperationType.Decrypt);
        provider.Operations[0].DataSize.ShouldBe(3);
    }

    [Fact]
    public void Operations_RecordsTimestamp()
    {
        var provider = new MockEncryptionProvider();
        var before = DateTime.UtcNow;

        provider.Encrypt([1, 2, 3]);

        var after = DateTime.UtcNow;
        provider.Operations[0].Timestamp.ShouldBeGreaterThanOrEqualTo(before);
        provider.Operations[0].Timestamp.ShouldBeLessThanOrEqualTo(after);
    }

    #endregion

    #region Failure Simulation

    [Fact]
    public void Encrypt_WhenShouldFail_Throws()
    {
        var provider = new MockEncryptionProvider { ShouldFailEncrypt = true };

        Should.Throw<InvalidOperationException>(() => provider.Encrypt([1, 2, 3]));
    }

    [Fact]
    public void Decrypt_WhenShouldFail_ThrowsCryptographicException()
    {
        var provider = new MockEncryptionProvider { ShouldFailDecrypt = true };

        Should.Throw<System.Security.Cryptography.CryptographicException>(() => provider.Decrypt([1, 2, 3]));
    }

    [Fact]
    public void Encrypt_WhenShouldFail_StillIncrementsCount()
    {
        var provider = new MockEncryptionProvider { ShouldFailEncrypt = true };

        try { provider.Encrypt([1, 2, 3]); } catch { }

        provider.EncryptCount.ShouldBe(1);
    }

    #endregion

    #region Custom Functions

    [Fact]
    public void Encrypt_WithCustomFunction_UsesCustom()
    {
        var provider = new MockEncryptionProvider { CustomEncrypt = data => data.Reverse().ToArray() };

        var result = provider.Encrypt([1, 2, 3]);

        result.ShouldBe(new byte[] { 3, 2, 1 });
    }

    [Fact]
    public void Decrypt_WithCustomFunction_UsesCustom()
    {
        var provider = new MockEncryptionProvider { CustomDecrypt = data => data.Reverse().ToArray() };

        var result = provider.Decrypt([1, 2, 3]);

        result.ShouldBe(new byte[] { 3, 2, 1 });
    }

    #endregion

    #region Async Methods

    [Fact]
    public async Task EncryptAsync_ReturnsEncryptedData()
    {
        var provider = new MockEncryptionProvider { Mode = MockEncryptionMode.Reversible };
        var data = Encoding.UTF8.GetBytes("secret");

        var encrypted = await provider.EncryptAsync(data, TestContext.Current.CancellationToken);

        encrypted.ShouldNotBe(data);
        provider.EncryptCount.ShouldBe(1);
    }

    [Fact]
    public async Task DecryptAsync_ReturnsDecryptedData()
    {
        var provider = new MockEncryptionProvider { Mode = MockEncryptionMode.Reversible };
        var original = Encoding.UTF8.GetBytes("secret");
        var encrypted = provider.Encrypt(original);

        var decrypted = await provider.DecryptAsync(encrypted, TestContext.Current.CancellationToken);

        decrypted.ShouldBe(original);
    }

    [Fact]
    public async Task EncryptAsync_RespectsCancellation()
    {
        var provider = new MockEncryptionProvider();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(
            () => provider.EncryptAsync([1, 2, 3], cts.Token));
    }

    [Fact]
    public async Task DecryptAsync_RespectsCancellation()
    {
        var provider = new MockEncryptionProvider();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(
            () => provider.DecryptAsync([1, 2, 3], cts.Token));
    }

    #endregion

    #region Reset Tests

    [Fact]
    public void Reset_ClearsAllState()
    {
        var provider = new MockEncryptionProvider
        {
            Mode = MockEncryptionMode.Reversible,
            CustomEncrypt = _ => []
        };
        provider.Encrypt([1, 2, 3]);
        provider.ShouldFailEncrypt = true; // Set after Encrypt to test it gets reset

        provider.Reset();

        provider.Mode.ShouldBe(MockEncryptionMode.PassThrough);
        provider.ShouldFailEncrypt.ShouldBeFalse();
        provider.ShouldFailDecrypt.ShouldBeFalse();
        provider.CustomEncrypt.ShouldBeNull();
        provider.CustomDecrypt.ShouldBeNull();
        provider.Operations.ShouldBeEmpty();
        provider.EncryptCount.ShouldBe(0);
        provider.DecryptCount.ShouldBe(0);
    }

    [Fact]
    public void ClearOperations_ClearsOnlyOperations()
    {
        var provider = new MockEncryptionProvider { Mode = MockEncryptionMode.Reversible };
        provider.Encrypt([1, 2, 3]);

        provider.ClearOperations();

        provider.Operations.ShouldBeEmpty();
        provider.EncryptCount.ShouldBe(0);
        provider.Mode.ShouldBe(MockEncryptionMode.Reversible); // Mode preserved
    }

    #endregion
}
