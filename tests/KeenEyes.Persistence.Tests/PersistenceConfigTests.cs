using KeenEyes.Persistence.Encryption;

namespace KeenEyes.Persistence.Tests;

/// <summary>
/// Tests for PersistenceConfig.
/// </summary>
public class PersistenceConfigTests
{
    [Fact]
    public void Default_ReturnsConfigWithNoEncryption()
    {
        var config = PersistenceConfig.Default;

        Assert.NotNull(config);
        Assert.IsType<NoEncryptionProvider>(config.EncryptionProvider);
        Assert.Same(NoEncryptionProvider.Instance, config.EncryptionProvider);
        Assert.Null(config.SaveDirectory);
    }

    [Fact]
    public void Default_IsSingleton()
    {
        var config1 = PersistenceConfig.Default;
        var config2 = PersistenceConfig.Default;

        Assert.Same(config1, config2);
    }

    [Fact]
    public void WithEncryption_CreatesConfigWithAesProvider()
    {
        var config = PersistenceConfig.WithEncryption("testPassword");

        Assert.NotNull(config);
        Assert.IsType<AesEncryptionProvider>(config.EncryptionProvider);
        Assert.True(config.EncryptionProvider.IsEncrypted);
        Assert.Equal("AES-256", config.EncryptionProvider.Name);
        Assert.Null(config.SaveDirectory);
    }

    [Fact]
    public void WithEncryption_WithSaveDirectory_SetsDirectory()
    {
        var saveDir = "custom/save/path";
        var config = PersistenceConfig.WithEncryption("password", saveDir);

        Assert.Equal(saveDir, config.SaveDirectory);
        Assert.IsType<AesEncryptionProvider>(config.EncryptionProvider);
    }

    [Fact]
    public void WithEncryption_WithoutSaveDirectory_SaveDirectoryIsNull()
    {
        var config = PersistenceConfig.WithEncryption("password");

        Assert.Null(config.SaveDirectory);
    }

    [Fact]
    public void Init_CustomEncryptionProvider_SetsProvider()
    {
        var customProvider = new AesEncryptionProvider("custom");
        var config = new PersistenceConfig
        {
            EncryptionProvider = customProvider
        };

        Assert.Same(customProvider, config.EncryptionProvider);
    }

    [Fact]
    public void Init_CustomSaveDirectory_SetsDirectory()
    {
        var config = new PersistenceConfig
        {
            SaveDirectory = "my/custom/path"
        };

        Assert.Equal("my/custom/path", config.SaveDirectory);
    }

    [Fact]
    public void DefaultConstructor_UsesNoEncryption()
    {
        var config = new PersistenceConfig();

        Assert.Same(NoEncryptionProvider.Instance, config.EncryptionProvider);
        Assert.Null(config.SaveDirectory);
    }

    [Fact]
    public void Record_SupportsWithSyntax()
    {
        var config1 = new PersistenceConfig
        {
            EncryptionProvider = new AesEncryptionProvider("password"),
            SaveDirectory = "original"
        };

        var config2 = config1 with { SaveDirectory = "modified" };

        Assert.Equal("original", config1.SaveDirectory);
        Assert.Equal("modified", config2.SaveDirectory);
        Assert.Same(config1.EncryptionProvider, config2.EncryptionProvider);
    }
}
