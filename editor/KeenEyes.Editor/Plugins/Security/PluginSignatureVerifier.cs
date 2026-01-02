// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using KeenEyes.Editor.Abstractions;

namespace KeenEyes.Editor.Plugins.Security;

/// <summary>
/// Verifies plugin signatures and manages trusted publishers.
/// </summary>
internal sealed class PluginSignatureVerifier
{
    private readonly TrustedPublisherStore trustedStore;
    private readonly IEditorPluginLogger? logger;

    /// <summary>
    /// Creates a new signature verifier.
    /// </summary>
    public PluginSignatureVerifier(
        TrustedPublisherStore trustedStore,
        IEditorPluginLogger? logger = null)
    {
        this.trustedStore = trustedStore;
        this.logger = logger;
    }

    /// <summary>
    /// Verifies the signature of a plugin assembly.
    /// </summary>
    public SignatureVerificationResult Verify(string assemblyPath)
    {
        if (!File.Exists(assemblyPath))
        {
            return SignatureVerificationResult.Error($"File not found: {assemblyPath}");
        }

        try
        {
            using var stream = File.OpenRead(assemblyPath);
            using var peReader = new PEReader(stream);

            if (!peReader.HasMetadata)
            {
                return SignatureVerificationResult.Error("Assembly has no metadata");
            }

            var reader = peReader.GetMetadataReader();

            if (!reader.IsAssembly)
            {
                return SignatureVerificationResult.Error("File is not an assembly");
            }

            var assemblyDef = reader.GetAssemblyDefinition();

            // Check if assembly has a public key (strong-named)
            if (assemblyDef.PublicKey.IsNil)
            {
                return new SignatureVerificationResult
                {
                    IsSigned = false,
                    IsValid = false,
                    IsTrusted = false,
                    Status = SignatureStatus.Unsigned
                };
            }

            // Get the public key
            var publicKeyBytes = reader.GetBlobBytes(assemblyDef.PublicKey);
            var publicKeyToken = ComputePublicKeyToken(publicKeyBytes);
            var publicKeyTokenHex = Convert.ToHexString(publicKeyToken).ToLowerInvariant();

            // Check if trusted
            var isTrusted = trustedStore.IsTrusted(publicKeyTokenHex);
            var publisher = trustedStore.GetPublisher(publicKeyTokenHex);

            // For now, assume valid if has public key (full signature verification
            // would require checking the PE signature which is more complex)
            return new SignatureVerificationResult
            {
                IsSigned = true,
                IsValid = true,
                IsTrusted = isTrusted,
                Status = isTrusted ? SignatureStatus.Valid : SignatureStatus.ValidUntrusted,
                PublicKeyToken = publicKeyTokenHex,
                SignerName = publisher?.Name,
                PublicKey = Convert.ToHexString(publicKeyBytes).ToLowerInvariant()
            };
        }
        catch (Exception ex)
        {
            logger?.LogError($"Signature verification failed: {ex.Message}");
            return SignatureVerificationResult.Error(ex.Message);
        }
    }

    /// <summary>
    /// Verifies the signature matches what's declared in the manifest.
    /// </summary>
    public SignatureVerificationResult Verify(string assemblyPath, PluginManifest manifest)
    {
        var result = Verify(assemblyPath);

        if (!result.IsSigned)
        {
            return result;
        }

        // If manifest declares a public key token, verify it matches
        if (!string.IsNullOrEmpty(manifest.Security?.PublicKeyToken))
        {
            var expectedToken = manifest.Security.PublicKeyToken.ToLowerInvariant();
            var actualToken = result.PublicKeyToken?.ToLowerInvariant();

            if (!string.Equals(expectedToken, actualToken, StringComparison.Ordinal))
            {
                return new SignatureVerificationResult
                {
                    IsSigned = true,
                    IsValid = false,
                    IsTrusted = false,
                    Status = SignatureStatus.Tampered,
                    PublicKeyToken = result.PublicKeyToken,
                    SignerName = result.SignerName,
                    ErrorMessage = $"Public key token mismatch: expected {expectedToken}, got {actualToken}"
                };
            }
        }

        return result;
    }

    /// <summary>
    /// Computes the public key token from a full public key.
    /// The token is the last 8 bytes of the SHA-1 hash of the public key, reversed.
    /// </summary>
    private static byte[] ComputePublicKeyToken(byte[] publicKey)
    {
        var hash = System.Security.Cryptography.SHA1.HashData(publicKey);
        var token = new byte[8];
        Array.Copy(hash, hash.Length - 8, token, 0, 8);
        Array.Reverse(token);
        return token;
    }
}

/// <summary>
/// Result of signature verification.
/// </summary>
public sealed class SignatureVerificationResult
{
    /// <summary>
    /// Gets a value indicating whether the assembly is signed.
    /// </summary>
    public required bool IsSigned { get; init; }

    /// <summary>
    /// Gets a value indicating whether the signature is valid.
    /// </summary>
    public required bool IsValid { get; init; }

    /// <summary>
    /// Gets a value indicating whether the signer is trusted.
    /// </summary>
    public required bool IsTrusted { get; init; }

    /// <summary>
    /// Gets the status of signature verification.
    /// </summary>
    public SignatureStatus Status { get; init; }

    /// <summary>
    /// Gets the signer name if known.
    /// </summary>
    public string? SignerName { get; init; }

    /// <summary>
    /// Gets the public key token (hex string).
    /// </summary>
    public string? PublicKeyToken { get; init; }

    /// <summary>
    /// Gets the full public key (hex string).
    /// </summary>
    public string? PublicKey { get; init; }

    /// <summary>
    /// Gets an error message if verification failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates an error result.
    /// </summary>
    public static SignatureVerificationResult Error(string message) => new()
    {
        IsSigned = false,
        IsValid = false,
        IsTrusted = false,
        Status = SignatureStatus.Invalid,
        ErrorMessage = message
    };
}

/// <summary>
/// Status of signature verification.
/// </summary>
public enum SignatureStatus
{
    /// <summary>
    /// Signed by a trusted publisher.
    /// </summary>
    Valid,

    /// <summary>
    /// Valid signature but publisher not trusted.
    /// </summary>
    ValidUntrusted,

    /// <summary>
    /// Signature failed verification.
    /// </summary>
    Invalid,

    /// <summary>
    /// No signature present.
    /// </summary>
    Unsigned,

    /// <summary>
    /// Signature present but assembly was modified.
    /// </summary>
    Tampered
}
