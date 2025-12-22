namespace KeenEyes.Serialization;

/// <summary>
/// Validates save slot names to prevent path traversal attacks.
/// </summary>
/// <remarks>
/// <para>
/// Slot names are used to construct file paths. Without validation, a malicious
/// slot name could contain path separators or ".." sequences to write files
/// outside the intended save directory.
/// </para>
/// <para>
/// This validator ensures slot names contain only safe characters and cannot
/// be used to escape the save directory.
/// </para>
/// <para>
/// For cross-platform compatibility, this validator always rejects characters
/// that are invalid on Windows, even when running on Linux/macOS. This ensures
/// saves created on any platform can be opened on any other platform.
/// </para>
/// </remarks>
public static class SlotNameValidator
{
    // Cross-platform invalid filename characters (Windows superset)
    // We always use these rather than Path.GetInvalidFileNameChars() which varies by OS.
    // This ensures saves created on Linux can be opened on Windows.
    private static readonly char[] invalidFileNameChars =
    [
        '\0', '\x01', '\x02', '\x03', '\x04', '\x05', '\x06', '\x07',
        '\x08', '\x09', '\x0A', '\x0B', '\x0C', '\x0D', '\x0E', '\x0F',
        '\x10', '\x11', '\x12', '\x13', '\x14', '\x15', '\x16', '\x17',
        '\x18', '\x19', '\x1A', '\x1B', '\x1C', '\x1D', '\x1E', '\x1F',
        '"', '<', '>', '|', ':', '*', '?', '\\', '/'
    ];

    /// <summary>
    /// Validates a slot name and throws if it contains unsafe characters.
    /// </summary>
    /// <param name="slotName">The slot name to validate.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when the slot name contains path separators, ".." sequences,
    /// or other characters that could enable path traversal.
    /// </exception>
    public static void Validate(string slotName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slotName);

        // Check for path separators
        if (slotName.Contains(Path.DirectorySeparatorChar))
        {
            throw new ArgumentException(
                $"Slot name cannot contain the directory separator character '{Path.DirectorySeparatorChar}'.",
                nameof(slotName));
        }

        if (slotName.Contains(Path.AltDirectorySeparatorChar))
        {
            throw new ArgumentException(
                $"Slot name cannot contain the alternate directory separator character '{Path.AltDirectorySeparatorChar}'.",
                nameof(slotName));
        }

        // Check for parent directory traversal
        if (slotName.Contains(".."))
        {
            throw new ArgumentException(
                "Slot name cannot contain '..' (parent directory reference).",
                nameof(slotName));
        }

        // Check for invalid filename characters (cross-platform)
        foreach (var c in slotName)
        {
            if (Array.IndexOf(invalidFileNameChars, c) >= 0)
            {
                throw new ArgumentException(
                    $"Slot name contains invalid character '{c}'.",
                    nameof(slotName));
            }
        }
    }

    /// <summary>
    /// Tries to validate a slot name without throwing.
    /// </summary>
    /// <param name="slotName">The slot name to validate.</param>
    /// <param name="errorMessage">The error message if validation fails.</param>
    /// <returns>True if the slot name is valid; otherwise, false.</returns>
    public static bool TryValidate(string slotName, out string? errorMessage)
    {
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(slotName))
        {
            errorMessage = "Slot name cannot be null or whitespace.";
            return false;
        }

        if (slotName.Contains(Path.DirectorySeparatorChar))
        {
            errorMessage = $"Slot name cannot contain the directory separator character '{Path.DirectorySeparatorChar}'.";
            return false;
        }

        if (slotName.Contains(Path.AltDirectorySeparatorChar))
        {
            errorMessage = $"Slot name cannot contain the alternate directory separator character '{Path.AltDirectorySeparatorChar}'.";
            return false;
        }

        if (slotName.Contains(".."))
        {
            errorMessage = "Slot name cannot contain '..' (parent directory reference).";
            return false;
        }

        foreach (var c in slotName)
        {
            if (Array.IndexOf(invalidFileNameChars, c) >= 0)
            {
                errorMessage = $"Slot name contains invalid character '{c}'.";
                return false;
            }
        }

        return true;
    }
}
