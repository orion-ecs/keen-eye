namespace KeenEyes.Testing.Encryption;

/// <summary>
/// Fluent assertions for encryption verification.
/// </summary>
public static class EncryptionAssertions
{
    /// <summary>
    /// Asserts that encryption was called at least once.
    /// </summary>
    /// <param name="provider">The mock encryption provider.</param>
    /// <returns>The provider for chaining.</returns>
    /// <exception cref="AssertionException">Thrown when no encryption occurred.</exception>
    public static MockEncryptionProvider ShouldHaveEncrypted(this MockEncryptionProvider provider)
    {
        if (provider.EncryptCount == 0)
        {
            throw new AssertionException("Expected at least one encryption operation, but none occurred.");
        }

        return provider;
    }

    /// <summary>
    /// Asserts that encryption was called a specific number of times.
    /// </summary>
    /// <param name="provider">The mock encryption provider.</param>
    /// <param name="times">The expected number of encryptions.</param>
    /// <returns>The provider for chaining.</returns>
    /// <exception cref="AssertionException">Thrown when the count doesn't match.</exception>
    public static MockEncryptionProvider ShouldHaveEncryptedTimes(this MockEncryptionProvider provider, int times)
    {
        if (provider.EncryptCount != times)
        {
            throw new AssertionException($"Expected {times} encryption operations, but {provider.EncryptCount} occurred.");
        }

        return provider;
    }

    /// <summary>
    /// Asserts that decryption was called at least once.
    /// </summary>
    /// <param name="provider">The mock encryption provider.</param>
    /// <returns>The provider for chaining.</returns>
    /// <exception cref="AssertionException">Thrown when no decryption occurred.</exception>
    public static MockEncryptionProvider ShouldHaveDecrypted(this MockEncryptionProvider provider)
    {
        if (provider.DecryptCount == 0)
        {
            throw new AssertionException("Expected at least one decryption operation, but none occurred.");
        }

        return provider;
    }

    /// <summary>
    /// Asserts that decryption was called a specific number of times.
    /// </summary>
    /// <param name="provider">The mock encryption provider.</param>
    /// <param name="times">The expected number of decryptions.</param>
    /// <returns>The provider for chaining.</returns>
    /// <exception cref="AssertionException">Thrown when the count doesn't match.</exception>
    public static MockEncryptionProvider ShouldHaveDecryptedTimes(this MockEncryptionProvider provider, int times)
    {
        if (provider.DecryptCount != times)
        {
            throw new AssertionException($"Expected {times} decryption operations, but {provider.DecryptCount} occurred.");
        }

        return provider;
    }

    /// <summary>
    /// Asserts that data of at least the specified size was encrypted.
    /// </summary>
    /// <param name="provider">The mock encryption provider.</param>
    /// <param name="minSize">The minimum data size in bytes.</param>
    /// <returns>The provider for chaining.</returns>
    /// <exception cref="AssertionException">Thrown when no matching operation was found.</exception>
    public static MockEncryptionProvider ShouldHaveEncryptedDataOfSize(this MockEncryptionProvider provider, int minSize)
    {
        var encryptOps = provider.Operations.Where(o => o.Type == OperationType.Encrypt).ToList();
        if (!encryptOps.Any(o => o.DataSize >= minSize))
        {
            var maxFound = encryptOps.Count > 0 ? encryptOps.Max(o => o.DataSize) : 0;
            throw new AssertionException($"Expected encryption of data at least {minSize} bytes, but maximum was {maxFound} bytes.");
        }

        return provider;
    }

    /// <summary>
    /// Asserts that data of at least the specified size was decrypted.
    /// </summary>
    /// <param name="provider">The mock encryption provider.</param>
    /// <param name="minSize">The minimum data size in bytes.</param>
    /// <returns>The provider for chaining.</returns>
    /// <exception cref="AssertionException">Thrown when no matching operation was found.</exception>
    public static MockEncryptionProvider ShouldHaveDecryptedDataOfSize(this MockEncryptionProvider provider, int minSize)
    {
        var decryptOps = provider.Operations.Where(o => o.Type == OperationType.Decrypt).ToList();
        if (!decryptOps.Any(o => o.DataSize >= minSize))
        {
            var maxFound = decryptOps.Count > 0 ? decryptOps.Max(o => o.DataSize) : 0;
            throw new AssertionException($"Expected decryption of data at least {minSize} bytes, but maximum was {maxFound} bytes.");
        }

        return provider;
    }

    /// <summary>
    /// Asserts that no encryption operations occurred.
    /// </summary>
    /// <param name="provider">The mock encryption provider.</param>
    /// <returns>The provider for chaining.</returns>
    /// <exception cref="AssertionException">Thrown when encryption occurred.</exception>
    public static MockEncryptionProvider ShouldNotHaveEncrypted(this MockEncryptionProvider provider)
    {
        if (provider.EncryptCount > 0)
        {
            throw new AssertionException($"Expected no encryption operations, but {provider.EncryptCount} occurred.");
        }

        return provider;
    }

    /// <summary>
    /// Asserts that no decryption operations occurred.
    /// </summary>
    /// <param name="provider">The mock encryption provider.</param>
    /// <returns>The provider for chaining.</returns>
    /// <exception cref="AssertionException">Thrown when decryption occurred.</exception>
    public static MockEncryptionProvider ShouldNotHaveDecrypted(this MockEncryptionProvider provider)
    {
        if (provider.DecryptCount > 0)
        {
            throw new AssertionException($"Expected no decryption operations, but {provider.DecryptCount} occurred.");
        }

        return provider;
    }

    /// <summary>
    /// Asserts that the provider is in encrypted mode.
    /// </summary>
    /// <param name="provider">The mock encryption provider.</param>
    /// <returns>The provider for chaining.</returns>
    /// <exception cref="AssertionException">Thrown when not in encrypted mode.</exception>
    public static MockEncryptionProvider ShouldBeEncrypting(this MockEncryptionProvider provider)
    {
        if (!provider.IsEncrypted)
        {
            throw new AssertionException("Expected provider to be in encrypted mode, but it was in pass-through mode.");
        }

        return provider;
    }

    /// <summary>
    /// Asserts that the total number of operations (encrypt + decrypt) matches.
    /// </summary>
    /// <param name="provider">The mock encryption provider.</param>
    /// <param name="count">The expected total operation count.</param>
    /// <returns>The provider for chaining.</returns>
    /// <exception cref="AssertionException">Thrown when the count doesn't match.</exception>
    public static MockEncryptionProvider ShouldHaveTotalOperations(this MockEncryptionProvider provider, int count)
    {
        var total = provider.Operations.Count;
        if (total != count)
        {
            throw new AssertionException($"Expected {count} total operations, but {total} occurred.");
        }

        return provider;
    }
}
