namespace KeenEyes.Graph.Kesl.Validation;

/// <summary>
/// Result of validating a KESL shader graph.
/// </summary>
public sealed class ValidationResult
{
    private readonly List<ValidationMessage> messages = [];

    /// <summary>
    /// Gets whether the validation passed with no errors.
    /// </summary>
    public bool IsValid => !messages.Any(m => m.Severity == ValidationSeverity.Error);

    /// <summary>
    /// Gets all validation messages.
    /// </summary>
    public IReadOnlyList<ValidationMessage> Messages => messages;

    /// <summary>
    /// Gets only error messages.
    /// </summary>
    public IEnumerable<ValidationMessage> Errors => messages.Where(m => m.Severity == ValidationSeverity.Error);

    /// <summary>
    /// Gets only warning messages.
    /// </summary>
    public IEnumerable<ValidationMessage> Warnings => messages.Where(m => m.Severity == ValidationSeverity.Warning);

    /// <summary>
    /// Adds an error message.
    /// </summary>
    /// <param name="code">The error code (e.g., "KESL001").</param>
    /// <param name="message">The error message.</param>
    /// <param name="node">The node that caused the error (optional).</param>
    /// <param name="portIndex">The specific port index (optional).</param>
    public void AddError(string code, string message, Entity node = default, int? portIndex = null)
    {
        messages.Add(new ValidationMessage(ValidationSeverity.Error, code, message, node, portIndex));
    }

    /// <summary>
    /// Adds a warning message.
    /// </summary>
    /// <param name="code">The warning code.</param>
    /// <param name="message">The warning message.</param>
    /// <param name="node">The node that caused the warning (optional).</param>
    /// <param name="portIndex">The specific port index (optional).</param>
    public void AddWarning(string code, string message, Entity node = default, int? portIndex = null)
    {
        messages.Add(new ValidationMessage(ValidationSeverity.Warning, code, message, node, portIndex));
    }

    /// <summary>
    /// Merges another validation result into this one.
    /// </summary>
    /// <param name="other">The other validation result.</param>
    public void Merge(ValidationResult other)
    {
        messages.AddRange(other.messages);
    }
}

/// <summary>
/// Severity level of a validation message.
/// </summary>
public enum ValidationSeverity
{
    /// <summary>Warning that doesn't prevent compilation.</summary>
    Warning,

    /// <summary>Error that prevents compilation.</summary>
    Error
}

/// <summary>
/// A single validation message.
/// </summary>
/// <param name="Severity">The severity level.</param>
/// <param name="Code">The error/warning code.</param>
/// <param name="Message">The human-readable message.</param>
/// <param name="Node">The node entity if applicable.</param>
/// <param name="PortIndex">The specific port if applicable.</param>
public readonly record struct ValidationMessage(
    ValidationSeverity Severity,
    string Code,
    string Message,
    Entity Node,
    int? PortIndex);
