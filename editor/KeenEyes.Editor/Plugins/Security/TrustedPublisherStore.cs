// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace KeenEyes.Editor.Plugins.Security;

/// <summary>
/// Manages the list of trusted plugin publishers.
/// </summary>
internal sealed class TrustedPublisherStore
{
    private readonly string storePath;
    private readonly Dictionary<string, TrustedPublisher> publishers = new(StringComparer.OrdinalIgnoreCase);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Creates a new trusted publisher store.
    /// </summary>
    /// <param name="customPath">Optional custom path for the store file.</param>
    public TrustedPublisherStore(string? customPath = null)
    {
        storePath = customPath ?? GetDefaultStorePath();
    }

    /// <summary>
    /// Gets the default store path for the current platform.
    /// </summary>
    public static string GetDefaultStorePath()
    {
        var configDir = GetConfigDirectory();
        return Path.Combine(configDir, "trusted-publishers.json");
    }

    private static string GetConfigDirectory()
    {
        // Use KeenEyes config directory
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, ".keeneyes");
    }

    /// <summary>
    /// Loads the trusted publishers from disk.
    /// </summary>
    public void Load()
    {
        publishers.Clear();

        if (!File.Exists(storePath))
        {
            return;
        }

        try
        {
            var json = File.ReadAllText(storePath);
            var data = JsonSerializer.Deserialize<TrustedPublisherData>(json, JsonOptions);

            if (data?.Publishers != null)
            {
                foreach (var publisher in data.Publishers)
                {
                    publishers[publisher.PublicKeyToken] = publisher;
                }
            }
        }
        catch (JsonException)
        {
            // Invalid file, start fresh
            publishers.Clear();
        }
    }

    /// <summary>
    /// Saves the trusted publishers to disk.
    /// </summary>
    public void Save()
    {
        var directory = Path.GetDirectoryName(storePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var data = new TrustedPublisherData
        {
            Version = 1,
            Publishers = [.. publishers.Values]
        };

        var json = JsonSerializer.Serialize(data, JsonOptions);
        File.WriteAllText(storePath, json);
    }

    /// <summary>
    /// Checks if a public key token is trusted.
    /// </summary>
    public bool IsTrusted(string publicKeyToken)
    {
        return publishers.ContainsKey(publicKeyToken);
    }

    /// <summary>
    /// Gets a trusted publisher by public key token.
    /// </summary>
    public TrustedPublisher? GetPublisher(string publicKeyToken)
    {
        return publishers.GetValueOrDefault(publicKeyToken);
    }

    /// <summary>
    /// Adds or updates a trusted publisher.
    /// </summary>
    public void AddTrusted(TrustedPublisher publisher)
    {
        publishers[publisher.PublicKeyToken] = publisher;
    }

    /// <summary>
    /// Removes a trusted publisher.
    /// </summary>
    public bool RemoveTrusted(string publicKeyToken)
    {
        return publishers.Remove(publicKeyToken);
    }

    /// <summary>
    /// Gets all trusted publishers.
    /// </summary>
    public IReadOnlyList<TrustedPublisher> GetAll()
    {
        return [.. publishers.Values];
    }

    /// <summary>
    /// Gets the number of trusted publishers.
    /// </summary>
    public int Count => publishers.Count;

    private sealed class TrustedPublisherData
    {
        [JsonPropertyName("version")]
        public int Version { get; set; }

        [JsonPropertyName("publishers")]
        public List<TrustedPublisher> Publishers { get; set; } = [];
    }
}

/// <summary>
/// Represents a trusted plugin publisher.
/// </summary>
public sealed class TrustedPublisher
{
    /// <summary>
    /// Gets or sets the publisher name.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets the public key token (hex string).
    /// </summary>
    [JsonPropertyName("publicKeyToken")]
    public required string PublicKeyToken { get; init; }

    /// <summary>
    /// Gets or sets when this publisher was trusted.
    /// </summary>
    [JsonPropertyName("trustedSince")]
    public required DateTime TrustedSince { get; init; }

    /// <summary>
    /// Gets or sets optional notes about this publisher.
    /// </summary>
    [JsonPropertyName("notes")]
    public string? Notes { get; init; }

    /// <summary>
    /// Gets or sets the full public key (if available).
    /// </summary>
    [JsonPropertyName("publicKey")]
    public string? PublicKey { get; init; }
}
