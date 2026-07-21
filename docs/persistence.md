# Persistence & Encryption

The `KeenEyes.Persistence` library adds password-based AES-256 encryption on top of the World's built-in save-slot system, so save files on disk cannot be read or tampered with without the correct password.

## Overview

`World` already exposes save-slot functionality directly (`world.SaveToSlot`, `world.LoadFromSlot`, `world.SaveDirectory`) built on the snapshot and serialization mechanism described in [Serialization and Snapshots](serialization.md). `KeenEyes.Persistence` does not replace that mechanism — it wraps it with an encryption step:

1. A `WorldSnapshot` is created and serialized (binary or JSON), exactly as it would be for an unencrypted save.
2. The serialized bytes are passed through an `IEncryptionProvider` before being written to the `.ksave` container.
3. On load, the container is read, the payload is decrypted, and the result is handed back to the same snapshot deserialization path.

Because the encryption step sits *around* the existing snapshot pipeline, everything documented in [Serialization and Snapshots](serialization.md) about what gets captured, entity ID mapping, and AOT-compatible serializers still applies unchanged.

## Quick Start

### Installation

```csharp
using KeenEyes.Persistence;

using var world = new World();

// Install with no encryption (pass-through)
world.InstallPlugin(new PersistencePlugin());
```

Installing `PersistencePlugin` registers an `EncryptedPersistenceApi` extension on the world. Retrieve it with `world.GetExtension<EncryptedPersistenceApi>()`.

### Enabling Encryption

Use `PersistenceConfig.WithEncryption` to configure AES-256 encryption with a password:

```csharp
using KeenEyes.Persistence;

var config = PersistenceConfig.WithEncryption("myPassword");
world.InstallPlugin(new PersistencePlugin(config));

var persistence = world.GetExtension<EncryptedPersistenceApi>();

// Save (creates a snapshot, serializes it, then encrypts it before writing)
var serializer = new ComponentSerializer(); // generated serializer
var slotInfo = persistence.SaveToSlot("slot1", serializer);

// Load (reads the file, decrypts it, then restores the snapshot into the world)
var (info, entityMap) = persistence.LoadFromSlot("slot1", serializer);
```

`PersistenceConfig` is a record, so the same result can be built explicitly:

```csharp
using KeenEyes.Persistence;
using KeenEyes.Persistence.Encryption;

var config = new PersistenceConfig
{
    EncryptionProvider = new AesEncryptionProvider("myPassword"),
    SaveDirectory = "encrypted_saves"
};
world.InstallPlugin(new PersistencePlugin(config));
```

**Note:** When using a custom `IWorld` implementation (not the standard `World` class), `PersistenceConfig.SaveDirectory` must be set explicitly. `EncryptedPersistenceApi` falls back to `World.SaveDirectory` only when the world is a concrete `World` instance; otherwise it throws an `ArgumentException` during construction.

## Core Concepts

### PersistencePlugin

`PersistencePlugin` is an `IWorldPlugin`. On `Install`, it:

- Applies `PersistenceConfig.SaveDirectory` to the world via the `IPersistenceCapability` capability, if one is available and a directory was configured.
- Constructs an `EncryptedPersistenceApi` for the world and registers it with `context.SetExtension(api)`.

On `Uninstall`, it removes the extension with `context.RemoveExtension<EncryptedPersistenceApi>()`.

```csharp
public sealed class PersistencePlugin(PersistenceConfig? config = null) : IWorldPlugin
{
    public string Name => "Persistence";
    // Install / Uninstall as described above
}
```

### PersistenceConfig

`PersistenceConfig` is an immutable record with two settings:

| Property | Description |
|----------|--------------|
| `EncryptionProvider` | An `IEncryptionProvider`. Defaults to `NoEncryptionProvider.Instance` (no encryption). |
| `SaveDirectory` | Optional override for where `.ksave` files are stored. Falls back to `World.SaveDirectory` when null (concrete `World` only). |

`PersistenceConfig.Default` returns the no-encryption configuration. `PersistenceConfig.WithEncryption(password, saveDirectory?)` is a convenience factory that sets `EncryptionProvider` to a new `AesEncryptionProvider(password)`.

### EncryptedPersistenceApi

`EncryptedPersistenceApi` is the world extension installed by `PersistencePlugin`. It mirrors the shape of `World`'s own save-slot methods, but routes the serialized snapshot bytes through the configured `IEncryptionProvider` before writing, and through decryption before restoring:

| Member | Description |
|--------|-------------|
| `IsEncryptionEnabled` | `true` when the configured provider actually encrypts (`IEncryptionProvider.IsEncrypted`). |
| `EncryptionProviderName` | The provider's `Name` (`"AES-256"` or `"None"`). |
| `SaveDirectory` | The resolved save directory for this API instance. |
| `SaveToSlot<TSerializer>(slotName, serializer, options?)` | Creates a snapshot, serializes it, encrypts it, and writes it to a `.ksave` file. Returns a `SaveSlotInfo`. |
| `LoadFromSlot<TSerializer>(slotName, serializer, validateChecksum = true)` | Reads a `.ksave` file, decrypts it, deserializes the snapshot, and restores it into the world. Returns the `SaveSlotInfo` and the old-ID-to-new-`Entity` map. |
| `SaveToSlotAsync` / `LoadFromSlotAsync` | Async equivalents that use the provider's `EncryptAsync`/`DecryptAsync`. |
| `GetSlotInfo(slotName)` | Reads only the `SaveSlotInfo` metadata, without decrypting the payload. |
| `ListSlots()` | Enumerates `SaveSlotInfo` for every `.ksave` file in the save directory. |
| `SlotExists(slotName)` / `DeleteSlot(slotName)` | Slot existence check and deletion. |
| `GetSlotFilePath(slotName)` | Resolves the full path for a slot name, validated by `SlotNameValidator`. |

`TSerializer` must implement both `IComponentSerializer` and `IBinaryComponentSerializer` — the same constraint used by `World.SaveToSlot`/`World.LoadFromSlot` and by `SnapshotManager.ToBinary`/`FromBinary`.

The returned `SaveSlotInfo` (and the `SaveSlotOptions` accepted by `SaveToSlot`) are the same types used by the World's unencrypted save API — see [Serialization and Snapshots](serialization.md) for the full snapshot lifecycle they describe. `GetSlotInfo` and `ListSlots` can still enumerate and inspect encrypted saves without a password, because the encryption only covers the snapshot payload, not the `SaveSlotInfo` metadata stored alongside it in the `.ksave` container.

### Encryption Providers

`IEncryptionProvider` is the extension point for encryption. It exposes `Name`, `IsEncrypted`, and sync/async `Encrypt`/`Decrypt` pairs operating on `byte[]`.

Two implementations ship with the library:

- **`NoEncryptionProvider`** — a pass-through singleton (`NoEncryptionProvider.Instance`) with `IsEncrypted => false`. This is `PersistenceConfig.Default`'s provider.
- **`AesEncryptionProvider`** — AES-256 in CBC mode with PKCS7 padding. The key is derived from the supplied password with PBKDF2 (SHA-256, 100,000 iterations) and a random 16-byte salt generated per encrypt call. A random 16-byte IV is also generated per call. The output layout is `[Salt (16 bytes)][IV (16 bytes)][Ciphertext]`, so each encrypted payload is fully self-describing and needs only the password to decrypt.

```csharp
using KeenEyes.Persistence.Encryption;

var provider = new AesEncryptionProvider("MySecretPassword");
byte[] encrypted = provider.Encrypt(plaintextBytes);
byte[] decrypted = provider.Decrypt(encrypted);
```

Because the salt and IV are regenerated on every `Encrypt` call, encrypting the same snapshot twice produces different ciphertext — this is expected and does not affect round-tripping through `Decrypt`.

A custom `IEncryptionProvider` can be supplied instead of `AesEncryptionProvider` (for example, to integrate with a platform key vault) by setting `PersistenceConfig.EncryptionProvider` directly.

## Performance Considerations

- `SaveToSlot` encrypts the serialized snapshot bytes *before* handing them to `SaveFileFormat.Write`, which performs the actual GZip/Brotli compression configured via `SaveSlotOptions.Compression`. Because encrypted data is high-entropy, it compresses far less effectively than the original plaintext snapshot — expect encrypted `.ksave` files to be noticeably larger than unencrypted ones using the same `SaveSlotOptions`.
- `AesEncryptionProvider` derives its key with 100,000 PBKDF2 iterations on every `Encrypt`/`Decrypt` call. This is deliberate (it makes brute-forcing the password expensive) but means each save/load pays a fixed key-derivation cost in addition to serialization and compression time.
- Use `SaveToSlotAsync`/`LoadFromSlotAsync` to keep encryption and file I/O off the main thread; `AesEncryptionProvider`'s async methods currently wrap the synchronous implementation in `Task.FromResult`, so the CPU-bound work still runs on the calling thread unless the caller offloads it (e.g. with `Task.Run`).

## Next Steps

- [Serialization and Snapshots](serialization.md) — the snapshot creation, binary/JSON serialization, and restore mechanism that `EncryptedPersistenceApi` builds on.
- [Plugins](plugins.md) — how `IWorldPlugin`, `IPluginContext`, extensions, and capabilities work in general.
- [Change Tracking](change-tracking.md) — for scenarios that need incremental save data rather than full snapshots.
