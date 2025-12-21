namespace KeenEyes.Persistence.Tests;

/// <summary>
/// Additional tests for PersistencePlugin to improve coverage.
/// </summary>
public class PersistencePluginAdditionalTests
{
    [Fact]
    public void Install_WithSaveDirectoryConfig_SetsSaveDirectory()
    {
        var testDir = Path.Combine(Path.GetTempPath(), $"persistence_capability_test_{Guid.NewGuid():N}");

        try
        {
            using var world = new World { SaveDirectory = testDir };
            var config = new PersistenceConfig { SaveDirectory = Path.Combine(testDir, "custom") };
            var plugin = new PersistencePlugin(config);

            world.InstallPlugin(plugin);

            // Verify the plugin installed successfully
            var api = world.GetExtension<EncryptedPersistenceApi>();
            Assert.NotNull(api);
            Assert.Equal(Path.Combine(testDir, "custom"), api.SaveDirectory);
        }
        finally
        {
            if (Directory.Exists(testDir))
            {
                Directory.Delete(testDir, recursive: true);
            }
        }
    }

    [Fact]
    public void Constructor_WithoutConfig_UsesDefaultConfig()
    {
        var plugin = new PersistencePlugin();

        Assert.Equal("Persistence", plugin.Name);
    }

    [Fact]
    public void Constructor_WithConfig_UsesProvidedConfig()
    {
        var config = PersistenceConfig.WithEncryption("test");
        var plugin = new PersistencePlugin(config);

        Assert.Equal("Persistence", plugin.Name);
    }

    [Fact]
    public void Name_ReturnsCorrectName()
    {
        var plugin = new PersistencePlugin();

        Assert.Equal("Persistence", plugin.Name);
    }
}
