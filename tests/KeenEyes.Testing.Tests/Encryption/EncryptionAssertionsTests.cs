using KeenEyes.Testing.Encryption;

namespace KeenEyes.Testing.Tests.Encryption;

public class EncryptionAssertionsTests
{
    #region ShouldHaveEncrypted

    [Fact]
    public void ShouldHaveEncrypted_WhenEncrypted_Passes()
    {
        var provider = new MockEncryptionProvider();
        provider.Encrypt([1, 2, 3]);

        var result = provider.ShouldHaveEncrypted();

        Assert.Same(provider, result);
    }

    [Fact]
    public void ShouldHaveEncrypted_WhenNoEncryption_Throws()
    {
        var provider = new MockEncryptionProvider();

        var ex = Assert.Throws<AssertionException>(() => provider.ShouldHaveEncrypted());
        Assert.Contains("no", ex.Message.ToLower());
    }

    #endregion

    #region ShouldHaveEncryptedTimes

    [Fact]
    public void ShouldHaveEncryptedTimes_WhenCountMatches_Passes()
    {
        var provider = new MockEncryptionProvider();
        provider.Encrypt([1, 2, 3]);
        provider.Encrypt([4, 5, 6]);
        provider.Encrypt([7, 8, 9]);

        var result = provider.ShouldHaveEncryptedTimes(3);

        Assert.Same(provider, result);
    }

    [Fact]
    public void ShouldHaveEncryptedTimes_WhenCountDoesNotMatch_Throws()
    {
        var provider = new MockEncryptionProvider();
        provider.Encrypt([1, 2, 3]);

        var ex = Assert.Throws<AssertionException>(() => provider.ShouldHaveEncryptedTimes(5));
        Assert.Contains("5", ex.Message);
    }

    #endregion

    #region ShouldHaveDecrypted

    [Fact]
    public void ShouldHaveDecrypted_WhenDecrypted_Passes()
    {
        var provider = new MockEncryptionProvider();
        provider.Decrypt([1, 2, 3]);

        var result = provider.ShouldHaveDecrypted();

        Assert.Same(provider, result);
    }

    [Fact]
    public void ShouldHaveDecrypted_WhenNoDecryption_Throws()
    {
        var provider = new MockEncryptionProvider();

        var ex = Assert.Throws<AssertionException>(() => provider.ShouldHaveDecrypted());
        Assert.Contains("no", ex.Message.ToLower());
    }

    #endregion

    #region ShouldHaveDecryptedTimes

    [Fact]
    public void ShouldHaveDecryptedTimes_WhenCountMatches_Passes()
    {
        var provider = new MockEncryptionProvider();
        provider.Decrypt([1, 2, 3]);
        provider.Decrypt([4, 5, 6]);

        var result = provider.ShouldHaveDecryptedTimes(2);

        Assert.Same(provider, result);
    }

    [Fact]
    public void ShouldHaveDecryptedTimes_WhenCountDoesNotMatch_Throws()
    {
        var provider = new MockEncryptionProvider();
        provider.Decrypt([1, 2, 3]);

        var ex = Assert.Throws<AssertionException>(() => provider.ShouldHaveDecryptedTimes(3));
        Assert.Contains("3", ex.Message);
    }

    #endregion

    #region ShouldHaveEncryptedDataOfSize

    [Fact]
    public void ShouldHaveEncryptedDataOfSize_WhenLargeEnough_Passes()
    {
        var provider = new MockEncryptionProvider();
        var data = new byte[100];
        provider.Encrypt(data);

        var result = provider.ShouldHaveEncryptedDataOfSize(50);

        Assert.Same(provider, result);
    }

    [Fact]
    public void ShouldHaveEncryptedDataOfSize_WhenExactSize_Passes()
    {
        var provider = new MockEncryptionProvider();
        var data = new byte[100];
        provider.Encrypt(data);

        var result = provider.ShouldHaveEncryptedDataOfSize(100);

        Assert.Same(provider, result);
    }

    [Fact]
    public void ShouldHaveEncryptedDataOfSize_WhenTooSmall_Throws()
    {
        var provider = new MockEncryptionProvider();
        var data = new byte[50];
        provider.Encrypt(data);

        var ex = Assert.Throws<AssertionException>(() => provider.ShouldHaveEncryptedDataOfSize(100));
        Assert.Contains("100", ex.Message);
        Assert.Contains("50", ex.Message);
    }

    [Fact]
    public void ShouldHaveEncryptedDataOfSize_WhenNoData_Throws()
    {
        var provider = new MockEncryptionProvider();

        var ex = Assert.Throws<AssertionException>(() => provider.ShouldHaveEncryptedDataOfSize(10));
        Assert.Contains("0", ex.Message);
    }

    #endregion

    #region ShouldHaveDecryptedDataOfSize

    [Fact]
    public void ShouldHaveDecryptedDataOfSize_WhenLargeEnough_Passes()
    {
        var provider = new MockEncryptionProvider();
        var data = new byte[100];
        provider.Decrypt(data);

        var result = provider.ShouldHaveDecryptedDataOfSize(50);

        Assert.Same(provider, result);
    }

    [Fact]
    public void ShouldHaveDecryptedDataOfSize_WhenTooSmall_Throws()
    {
        var provider = new MockEncryptionProvider();
        var data = new byte[50];
        provider.Decrypt(data);

        var ex = Assert.Throws<AssertionException>(() => provider.ShouldHaveDecryptedDataOfSize(100));
        Assert.Contains("100", ex.Message);
    }

    [Fact]
    public void ShouldHaveDecryptedDataOfSize_WhenNoData_Throws()
    {
        var provider = new MockEncryptionProvider();

        var ex = Assert.Throws<AssertionException>(() => provider.ShouldHaveDecryptedDataOfSize(10));
        Assert.Contains("0", ex.Message);
    }

    #endregion

    #region ShouldNotHaveEncrypted

    [Fact]
    public void ShouldNotHaveEncrypted_WhenNoEncryption_Passes()
    {
        var provider = new MockEncryptionProvider();

        var result = provider.ShouldNotHaveEncrypted();

        Assert.Same(provider, result);
    }

    [Fact]
    public void ShouldNotHaveEncrypted_WhenEncrypted_Throws()
    {
        var provider = new MockEncryptionProvider();
        provider.Encrypt([1, 2, 3]);

        var ex = Assert.Throws<AssertionException>(() => provider.ShouldNotHaveEncrypted());
        Assert.Contains("1", ex.Message);
    }

    #endregion

    #region ShouldNotHaveDecrypted

    [Fact]
    public void ShouldNotHaveDecrypted_WhenNoDecryption_Passes()
    {
        var provider = new MockEncryptionProvider();

        var result = provider.ShouldNotHaveDecrypted();

        Assert.Same(provider, result);
    }

    [Fact]
    public void ShouldNotHaveDecrypted_WhenDecrypted_Throws()
    {
        var provider = new MockEncryptionProvider();
        provider.Decrypt([1, 2, 3]);

        var ex = Assert.Throws<AssertionException>(() => provider.ShouldNotHaveDecrypted());
        Assert.Contains("1", ex.Message);
    }

    #endregion

    #region ShouldBeEncrypting

    [Fact]
    public void ShouldBeEncrypting_WhenIsEncryptedTrue_Passes()
    {
        var provider = new MockEncryptionProvider { Mode = MockEncryptionMode.Reversible };

        var result = provider.ShouldBeEncrypting();

        Assert.Same(provider, result);
    }

    [Fact]
    public void ShouldBeEncrypting_WhenIsEncryptedFalse_Throws()
    {
        var provider = new MockEncryptionProvider { Mode = MockEncryptionMode.PassThrough };

        var ex = Assert.Throws<AssertionException>(() => provider.ShouldBeEncrypting());
        Assert.Contains("pass-through", ex.Message);
    }

    #endregion

    #region ShouldHaveTotalOperations

    [Fact]
    public void ShouldHaveTotalOperations_WhenCountMatches_Passes()
    {
        var provider = new MockEncryptionProvider();
        provider.Encrypt([1, 2, 3]);
        provider.Decrypt([4, 5, 6]);
        provider.Encrypt([7, 8, 9]);

        var result = provider.ShouldHaveTotalOperations(3);

        Assert.Same(provider, result);
    }

    [Fact]
    public void ShouldHaveTotalOperations_WhenCountDoesNotMatch_Throws()
    {
        var provider = new MockEncryptionProvider();
        provider.Encrypt([1, 2, 3]);

        var ex = Assert.Throws<AssertionException>(() => provider.ShouldHaveTotalOperations(5));
        Assert.Contains("5", ex.Message);
        Assert.Contains("1", ex.Message);
    }

    #endregion
}
