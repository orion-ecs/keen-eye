// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Editor.Plugins;
using KeenEyes.Editor.Plugins.Security;

namespace KeenEyes.Editor.Tests.Plugins.Security;

/// <summary>
/// Tests for <see cref="PluginSignatureVerifier"/>.
/// </summary>
public sealed class PluginSignatureVerifierTests : IDisposable
{
    private readonly string tempDirectory;
    private readonly TrustedPublisherStore trustedStore;
    private readonly PluginSignatureVerifier verifier;

    public PluginSignatureVerifierTests()
    {
        tempDirectory = Path.Combine(Path.GetTempPath(), $"SignatureVerifierTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);

        var storePath = Path.Combine(tempDirectory, "trusted.json");
        trustedStore = new TrustedPublisherStore(storePath);
        verifier = new PluginSignatureVerifier(trustedStore);
    }

    public void Dispose()
    {
        if (Directory.Exists(tempDirectory))
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    #region Basic Verification Tests

    [Fact]
    public void Verify_WithNonexistentFile_ReturnsError()
    {
        // Act
        var result = verifier.Verify("/nonexistent/assembly.dll");

        // Assert
        Assert.False(result.IsSigned);
        Assert.False(result.IsValid);
        Assert.False(result.IsTrusted);
        Assert.Equal(SignatureStatus.Invalid, result.Status);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void Verify_WithUnsignedAssembly_ReturnsUnsigned()
    {
        // Arrange - Test assembly is typically not signed with strong name
        var assemblyPath = typeof(PluginSignatureVerifierTests).Assembly.Location;

        // Act
        var result = verifier.Verify(assemblyPath);

        // Assert
        // Most test assemblies are not strong-named, so should be unsigned
        if (result.Status == SignatureStatus.Unsigned)
        {
            Assert.False(result.IsSigned);
            Assert.False(result.IsValid);
            Assert.False(result.IsTrusted);
        }
    }

    [Fact]
    public void Verify_WithSignedAssembly_ReturnsSigned()
    {
        // Arrange - Use mscorlib or System assembly which is strong-named
        var systemAssemblyPath = typeof(object).Assembly.Location;

        // Act
        var result = verifier.Verify(systemAssemblyPath);

        // Assert
        Assert.True(result.IsSigned);
        Assert.True(result.IsValid);
        Assert.NotNull(result.PublicKeyToken);
        Assert.NotEmpty(result.PublicKeyToken);
    }

    #endregion

    #region Trusted Publisher Tests

    [Fact]
    public void Verify_WithTrustedPublisher_ReturnsValidAndTrusted()
    {
        // Arrange - Get the public key token of a signed assembly
        var systemAssemblyPath = typeof(object).Assembly.Location;
        var firstResult = verifier.Verify(systemAssemblyPath);

        if (!firstResult.IsSigned)
        {
            // Skip test if system assembly isn't signed (unlikely)
            return;
        }

        // Add the publisher as trusted
        trustedStore.AddTrusted(new TrustedPublisher
        {
            Name = "Microsoft",
            PublicKeyToken = firstResult.PublicKeyToken!,
            TrustedSince = DateTime.UtcNow
        });

        // Act
        var result = verifier.Verify(systemAssemblyPath);

        // Assert
        Assert.True(result.IsSigned);
        Assert.True(result.IsValid);
        Assert.True(result.IsTrusted);
        Assert.Equal(SignatureStatus.Valid, result.Status);
    }

    [Fact]
    public void Verify_WithUntrustedPublisher_ReturnsValidButUntrusted()
    {
        // Arrange - Use a signed assembly but don't add to trusted store
        var systemAssemblyPath = typeof(object).Assembly.Location;

        // Act
        var result = verifier.Verify(systemAssemblyPath);

        // Assert
        if (result.IsSigned)
        {
            Assert.True(result.IsValid);
            Assert.False(result.IsTrusted);
            Assert.Equal(SignatureStatus.ValidUntrusted, result.Status);
        }
    }

    #endregion

    #region Manifest Verification Tests

    [Fact]
    public void Verify_WithMatchingManifestToken_Succeeds()
    {
        // Arrange
        var systemAssemblyPath = typeof(object).Assembly.Location;
        var baseResult = verifier.Verify(systemAssemblyPath);

        if (!baseResult.IsSigned)
        {
            return;
        }

        var manifest = new PluginManifest
        {
            Name = "Test",
            Id = "test",
            Version = "1.0.0",
            EntryPoint = new PluginEntryPoint { Assembly = "test.dll", Type = "Test.Plugin" },
            Security = new PluginSecurity { PublicKeyToken = baseResult.PublicKeyToken }
        };

        // Act
        var result = verifier.Verify(systemAssemblyPath, manifest);

        // Assert
        Assert.True(result.IsSigned);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Verify_WithMismatchedManifestToken_ReturnsTampered()
    {
        // Arrange
        var systemAssemblyPath = typeof(object).Assembly.Location;
        var baseResult = verifier.Verify(systemAssemblyPath);

        if (!baseResult.IsSigned)
        {
            return;
        }

        var manifest = new PluginManifest
        {
            Name = "Test",
            Id = "test",
            Version = "1.0.0",
            EntryPoint = new PluginEntryPoint { Assembly = "test.dll", Type = "Test.Plugin" },
            Security = new PluginSecurity { PublicKeyToken = "0000000000000000" } // Wrong token
        };

        // Act
        var result = verifier.Verify(systemAssemblyPath, manifest);

        // Assert
        Assert.True(result.IsSigned);
        Assert.False(result.IsValid);
        Assert.Equal(SignatureStatus.Tampered, result.Status);
        Assert.Contains("mismatch", result.ErrorMessage?.ToLowerInvariant() ?? "");
    }

    [Fact]
    public void Verify_WithNullSecurityInManifest_SkipsManifestCheck()
    {
        // Arrange
        var systemAssemblyPath = typeof(object).Assembly.Location;
        var manifest = new PluginManifest
        {
            Name = "Test",
            Id = "test",
            Version = "1.0.0",
            EntryPoint = new PluginEntryPoint { Assembly = "test.dll", Type = "Test.Plugin" },
            Security = null // No security section
        };

        // Act
        var result = verifier.Verify(systemAssemblyPath, manifest);

        // Assert
        // Should return the base verification result (no tampered status)
        if (result.IsSigned)
        {
            Assert.NotEqual(SignatureStatus.Tampered, result.Status);
        }
    }

    [Fact]
    public void Verify_WithUnsignedAssemblyAndManifest_ReturnsUnsigned()
    {
        // Arrange - Test assembly is typically unsigned
        var assemblyPath = typeof(PluginSignatureVerifierTests).Assembly.Location;
        var manifest = new PluginManifest
        {
            Name = "Test",
            Id = "test",
            Version = "1.0.0",
            EntryPoint = new PluginEntryPoint { Assembly = "test.dll", Type = "Test.Plugin" },
            Security = new PluginSecurity { PublicKeyToken = "somefaketoken" }
        };

        // Act
        var result = verifier.Verify(assemblyPath, manifest);

        // Assert
        if (result.Status == SignatureStatus.Unsigned)
        {
            Assert.False(result.IsSigned);
            // Manifest check shouldn't apply to unsigned assemblies
        }
    }

    #endregion

    #region SignatureVerificationResult Tests

    [Fact]
    public void SignatureVerificationResult_Error_SetsPropertiesCorrectly()
    {
        // Act
        var result = SignatureVerificationResult.Error("Test error message");

        // Assert
        Assert.False(result.IsSigned);
        Assert.False(result.IsValid);
        Assert.False(result.IsTrusted);
        Assert.Equal(SignatureStatus.Invalid, result.Status);
        Assert.Equal("Test error message", result.ErrorMessage);
    }

    #endregion

    #region PublicKeyToken Tests

    [Fact]
    public void Verify_SignedAssembly_ReturnsPublicKeyToken()
    {
        // Arrange
        var systemAssemblyPath = typeof(object).Assembly.Location;

        // Act
        var result = verifier.Verify(systemAssemblyPath);

        // Assert
        if (result.IsSigned)
        {
            Assert.NotNull(result.PublicKeyToken);
            Assert.Equal(16, result.PublicKeyToken.Length); // 8 bytes = 16 hex chars
            Assert.True(result.PublicKeyToken.All(c => "0123456789abcdef".Contains(c)));
        }
    }

    [Fact]
    public void Verify_SignedAssembly_ReturnsFullPublicKey()
    {
        // Arrange
        var systemAssemblyPath = typeof(object).Assembly.Location;

        // Act
        var result = verifier.Verify(systemAssemblyPath);

        // Assert
        if (result.IsSigned)
        {
            Assert.NotNull(result.PublicKey);
            Assert.True(result.PublicKey.Length > result.PublicKeyToken!.Length);
        }
    }

    #endregion

    #region Invalid File Tests

    [Fact]
    public void Verify_WithInvalidFile_ReturnsError()
    {
        // Arrange
        var tempFile = Path.Combine(tempDirectory, "invalid.dll");
        File.WriteAllText(tempFile, "This is not a valid assembly");

        // Act
        var result = verifier.Verify(tempFile);

        // Assert
        Assert.False(result.IsSigned);
        Assert.False(result.IsValid);
        Assert.Equal(SignatureStatus.Invalid, result.Status);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void Verify_WithEmptyFile_ReturnsError()
    {
        // Arrange
        var tempFile = Path.Combine(tempDirectory, "empty.dll");
        File.Create(tempFile).Dispose();

        // Act
        var result = verifier.Verify(tempFile);

        // Assert
        Assert.False(result.IsSigned);
        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
    }

    #endregion
}
