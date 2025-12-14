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
/// </remarks>
public static class SlotNameValidator
{
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

        // Check for invalid filename characters (excluding what we already checked)
        var invalidChars = Path.GetInvalidFileNameChars();
        foreach (var c in slotName)
        {
            if (Array.IndexOf(invalidChars, c) >= 0)
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

        var invalidChars = Path.GetInvalidFileNameChars();
        foreach (var c in slotName)
        {
            if (Array.IndexOf(invalidChars, c) >= 0)
            {
                errorMessage = $"Slot name contains invalid character '{c}'.";
                return false;
            }
        }

        return true;
    }
}
