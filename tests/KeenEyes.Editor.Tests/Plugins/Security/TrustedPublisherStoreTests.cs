// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Editor.Plugins.Security;

namespace KeenEyes.Editor.Tests.Plugins.Security;

/// <summary>
/// Tests for <see cref="TrustedPublisherStore"/>.
/// </summary>
public sealed class TrustedPublisherStoreTests : IDisposable
{
    private readonly string tempDirectory;
    private readonly string storePath;

    public TrustedPublisherStoreTests()
    {
        tempDirectory = Path.Combine(Path.GetTempPath(), $"TrustedPublisherStoreTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);
        storePath = Path.Combine(tempDirectory, "trusted-publishers.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(tempDirectory))
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithCustomPath_UsesCustomPath()
    {
        // Act
        var store = new TrustedPublisherStore(storePath);
        store.AddTrusted(CreateTestPublisher("test1"));
        store.Save();

        // Assert
        Assert.True(File.Exists(storePath));
    }

    [Fact]
    public void GetDefaultStorePath_ReturnsPathInUserProfile()
    {
        // Act
        var path = TrustedPublisherStore.GetDefaultStorePath();

        // Assert
        Assert.Contains(".keeneyes", path);
        Assert.Contains("trusted-publishers.json", path);
    }

    #endregion

    #region Load Tests

    [Fact]
    public void Load_WithNoFile_StartsEmpty()
    {
        // Arrange
        var nonExistentPath = Path.Combine(tempDirectory, "nonexistent.json");
        var store = new TrustedPublisherStore(nonExistentPath);

        // Act
        store.Load();

        // Assert
        Assert.Equal(0, store.Count);
    }

    [Fact]
    public void Load_WithValidFile_LoadsPublishers()
    {
        // Arrange
        var json = """
            {
              "version": 1,
              "publishers": [
                {
                  "name": "Microsoft",
                  "publicKeyToken": "b03f5f7f11d50a3a",
                  "trustedSince": "2024-01-01T00:00:00Z"
                }
              ]
            }
            """;
        File.WriteAllText(storePath, json);

        var store = new TrustedPublisherStore(storePath);

        // Act
        store.Load();

        // Assert
        Assert.Equal(1, store.Count);
        Assert.True(store.IsTrusted("b03f5f7f11d50a3a"));
    }

    [Fact]
    public void Load_WithInvalidJson_StartsEmpty()
    {
        // Arrange
        File.WriteAllText(storePath, "{ invalid json }");
        var store = new TrustedPublisherStore(storePath);

        // Act
        store.Load();

        // Assert
        Assert.Equal(0, store.Count);
    }

    [Fact]
    public void Load_ClearsExistingData()
    {
        // Arrange
        var store = new TrustedPublisherStore(storePath);
        store.AddTrusted(CreateTestPublisher("existing"));

        // Save empty file
        File.WriteAllText(storePath, """{"version":1,"publishers":[]}""");

        // Act
        store.Load();

        // Assert
        Assert.Equal(0, store.Count);
    }

    #endregion

    #region Save Tests

    [Fact]
    public void Save_CreatesFile()
    {
        // Arrange
        var store = new TrustedPublisherStore(storePath);
        store.AddTrusted(CreateTestPublisher("test1"));

        // Act
        store.Save();

        // Assert
        Assert.True(File.Exists(storePath));
        var json = File.ReadAllText(storePath);
        Assert.Contains("test1", json);
    }

    [Fact]
    public void Save_CreatesDirectoryIfNeeded()
    {
        // Arrange
        var deepPath = Path.Combine(tempDirectory, "nested", "dir", "publishers.json");
        var store = new TrustedPublisherStore(deepPath);
        store.AddTrusted(CreateTestPublisher("test1"));

        // Act
        store.Save();

        // Assert
        Assert.True(File.Exists(deepPath));
    }

    [Fact]
    public void SaveAndLoad_RoundTrips()
    {
        // Arrange
        var store1 = new TrustedPublisherStore(storePath);
        store1.AddTrusted(new TrustedPublisher
        {
            Name = "Test Publisher",
            PublicKeyToken = "1234567890abcdef",
            TrustedSince = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc),
            Notes = "Test notes",
            PublicKey = "fullpublickey"
        });
        store1.Save();

        // Act
        var store2 = new TrustedPublisherStore(storePath);
        store2.Load();
        var publisher = store2.GetPublisher("1234567890abcdef");

        // Assert
        Assert.NotNull(publisher);
        Assert.Equal("Test Publisher", publisher.Name);
        Assert.Equal("Test notes", publisher.Notes);
        Assert.Equal("fullpublickey", publisher.PublicKey);
    }

    #endregion

    #region IsTrusted Tests

    [Fact]
    public void IsTrusted_WithTrustedToken_ReturnsTrue()
    {
        // Arrange
        var store = new TrustedPublisherStore(storePath);
        store.AddTrusted(CreateTestPublisher("abc123"));

        // Act & Assert
        Assert.True(store.IsTrusted("abc123"));
    }

    [Fact]
    public void IsTrusted_WithUntrustedToken_ReturnsFalse()
    {
        // Arrange
        var store = new TrustedPublisherStore(storePath);
        store.AddTrusted(CreateTestPublisher("abc123"));

        // Act & Assert
        Assert.False(store.IsTrusted("xyz789"));
    }

    [Fact]
    public void IsTrusted_IsCaseInsensitive()
    {
        // Arrange
        var store = new TrustedPublisherStore(storePath);
        store.AddTrusted(CreateTestPublisher("AbCdEf"));

        // Act & Assert
        Assert.True(store.IsTrusted("abcdef"));
        Assert.True(store.IsTrusted("ABCDEF"));
        Assert.True(store.IsTrusted("AbCdEf"));
    }

    #endregion

    #region GetPublisher Tests

    [Fact]
    public void GetPublisher_WithExistingToken_ReturnsPublisher()
    {
        // Arrange
        var store = new TrustedPublisherStore(storePath);
        var expected = CreateTestPublisher("test123", "Test Publisher");
        store.AddTrusted(expected);

        // Act
        var actual = store.GetPublisher("test123");

        // Assert
        Assert.NotNull(actual);
        Assert.Equal("Test Publisher", actual.Name);
    }

    [Fact]
    public void GetPublisher_WithNonexistentToken_ReturnsNull()
    {
        // Arrange
        var store = new TrustedPublisherStore(storePath);

        // Act
        var result = store.GetPublisher("nonexistent");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region AddTrusted Tests

    [Fact]
    public void AddTrusted_AddsNewPublisher()
    {
        // Arrange
        var store = new TrustedPublisherStore(storePath);

        // Act
        store.AddTrusted(CreateTestPublisher("new123"));

        // Assert
        Assert.Equal(1, store.Count);
        Assert.True(store.IsTrusted("new123"));
    }

    [Fact]
    public void AddTrusted_UpdatesExistingPublisher()
    {
        // Arrange
        var store = new TrustedPublisherStore(storePath);
        store.AddTrusted(CreateTestPublisher("token123", "Original Name"));

        // Act
        store.AddTrusted(CreateTestPublisher("token123", "Updated Name"));

        // Assert
        Assert.Equal(1, store.Count);
        var publisher = store.GetPublisher("token123");
        Assert.Equal("Updated Name", publisher?.Name);
    }

    #endregion

    #region RemoveTrusted Tests

    [Fact]
    public void RemoveTrusted_WithExistingToken_ReturnsTrue()
    {
        // Arrange
        var store = new TrustedPublisherStore(storePath);
        store.AddTrusted(CreateTestPublisher("toremove"));

        // Act
        var result = store.RemoveTrusted("toremove");

        // Assert
        Assert.True(result);
        Assert.False(store.IsTrusted("toremove"));
        Assert.Equal(0, store.Count);
    }

    [Fact]
    public void RemoveTrusted_WithNonexistentToken_ReturnsFalse()
    {
        // Arrange
        var store = new TrustedPublisherStore(storePath);

        // Act
        var result = store.RemoveTrusted("nonexistent");

        // Assert
        Assert.False(result);
    }

    #endregion

    #region GetAll Tests

    [Fact]
    public void GetAll_ReturnsAllPublishers()
    {
        // Arrange
        var store = new TrustedPublisherStore(storePath);
        store.AddTrusted(CreateTestPublisher("token1", "Publisher 1"));
        store.AddTrusted(CreateTestPublisher("token2", "Publisher 2"));
        store.AddTrusted(CreateTestPublisher("token3", "Publisher 3"));

        // Act
        var all = store.GetAll();

        // Assert
        Assert.Equal(3, all.Count);
        Assert.Contains(all, p => p.Name == "Publisher 1");
        Assert.Contains(all, p => p.Name == "Publisher 2");
        Assert.Contains(all, p => p.Name == "Publisher 3");
    }

    [Fact]
    public void GetAll_WithNoPublishers_ReturnsEmptyList()
    {
        // Arrange
        var store = new TrustedPublisherStore(storePath);

        // Act
        var all = store.GetAll();

        // Assert
        Assert.Empty(all);
    }

    #endregion

    #region Helper Methods

    private static TrustedPublisher CreateTestPublisher(string token, string? name = null)
    {
        return new TrustedPublisher
        {
            Name = name ?? $"Test Publisher {token}",
            PublicKeyToken = token,
            TrustedSince = DateTime.UtcNow
        };
    }

    #endregion
}
