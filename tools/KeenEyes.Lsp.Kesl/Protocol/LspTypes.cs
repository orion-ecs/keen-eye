using System.Text.Json.Serialization;

namespace KeenEyes.Lsp.Kesl.Protocol;

/// <summary>
/// LSP message header constants.
/// </summary>
public static class LspHeaders
{
    public const string ContentLength = "Content-Length";
    public const string ContentType = "Content-Type";
    public const string JsonRpc = "2.0";
}

/// <summary>
/// A position in a text document expressed as zero-based line and character offset.
/// </summary>
public sealed record LspPosition
{
    /// <summary>Line position in a document (zero-based).</summary>
    [JsonPropertyName("line")]
    public required int Line { get; init; }

    /// <summary>Character offset on a line in a document (zero-based).</summary>
    [JsonPropertyName("character")]
    public required int Character { get; init; }
}

/// <summary>
/// A range in a text document expressed as start and end positions.
/// </summary>
public sealed record LspRange
{
    /// <summary>The range's start position.</summary>
    [JsonPropertyName("start")]
    public required LspPosition Start { get; init; }

    /// <summary>The range's end position.</summary>
    [JsonPropertyName("end")]
    public required LspPosition End { get; init; }
}

/// <summary>
/// A location inside a resource.
/// </summary>
public sealed record Location
{
    /// <summary>The text document's URI.</summary>
    [JsonPropertyName("uri")]
    public required string Uri { get; init; }

    /// <summary>The location's range.</summary>
    [JsonPropertyName("range")]
    public required LspRange Range { get; init; }
}

/// <summary>
/// Identifies a text document.
/// </summary>
public record TextDocumentIdentifier
{
    /// <summary>The text document's URI.</summary>
    [JsonPropertyName("uri")]
    public required string Uri { get; init; }
}

/// <summary>
/// Identifies a versioned text document.
/// </summary>
public sealed record VersionedTextDocumentIdentifier : TextDocumentIdentifier
{
    /// <summary>The version number of this document.</summary>
    [JsonPropertyName("version")]
    public required int Version { get; init; }
}

/// <summary>
/// An item to transfer a text document from the client to the server.
/// </summary>
public sealed record TextDocumentItem
{
    /// <summary>The text document's URI.</summary>
    [JsonPropertyName("uri")]
    public required string Uri { get; init; }

    /// <summary>The text document's language identifier.</summary>
    [JsonPropertyName("languageId")]
    public required string LanguageId { get; init; }

    /// <summary>The version number of this document.</summary>
    [JsonPropertyName("version")]
    public required int Version { get; init; }

    /// <summary>The content of the opened text document.</summary>
    [JsonPropertyName("text")]
    public required string Text { get; init; }
}

/// <summary>
/// A text document position parameters.
/// </summary>
public record TextDocumentPositionParams
{
    /// <summary>The text document.</summary>
    [JsonPropertyName("textDocument")]
    public required TextDocumentIdentifier TextDocument { get; init; }

    /// <summary>The position inside the text document.</summary>
    [JsonPropertyName("position")]
    public required LspPosition Position { get; init; }
}

/// <summary>
/// Diagnostic severity levels.
/// </summary>
public enum LspDiagnosticSeverity
{
    Error = 1,
    Warning = 2,
    Information = 3,
    Hint = 4
}

/// <summary>
/// Represents a diagnostic, such as a compiler error or warning.
/// </summary>
public sealed record LspDiagnostic
{
    /// <summary>The range at which the message applies.</summary>
    [JsonPropertyName("range")]
    public required LspRange Range { get; init; }

    /// <summary>The diagnostic's severity.</summary>
    [JsonPropertyName("severity")]
    public LspDiagnosticSeverity? Severity { get; init; }

    /// <summary>The diagnostic's code.</summary>
    [JsonPropertyName("code")]
    public string? Code { get; init; }

    /// <summary>A human-readable string describing the source of this diagnostic.</summary>
    [JsonPropertyName("source")]
    public string? Source { get; init; }

    /// <summary>The diagnostic's message.</summary>
    [JsonPropertyName("message")]
    public required string Message { get; init; }

    /// <summary>An array of related information.</summary>
    [JsonPropertyName("relatedInformation")]
    public IReadOnlyList<DiagnosticRelatedInformation>? RelatedInformation { get; init; }
}

/// <summary>
/// Related diagnostic information.
/// </summary>
public sealed record DiagnosticRelatedInformation
{
    /// <summary>The location of this related diagnostic information.</summary>
    [JsonPropertyName("location")]
    public required Location Location { get; init; }

    /// <summary>The message of this related diagnostic information.</summary>
    [JsonPropertyName("message")]
    public required string Message { get; init; }
}

/// <summary>
/// Parameters for textDocument/publishDiagnostics notification.
/// </summary>
public sealed record PublishDiagnosticsParams
{
    /// <summary>The URI for which diagnostic information is reported.</summary>
    [JsonPropertyName("uri")]
    public required string Uri { get; init; }

    /// <summary>The version number of the document the diagnostics are published for.</summary>
    [JsonPropertyName("version")]
    public int? Version { get; init; }

    /// <summary>An array of diagnostic information items.</summary>
    [JsonPropertyName("diagnostics")]
    public required IReadOnlyList<LspDiagnostic> Diagnostics { get; init; }
}

/// <summary>
/// Defines how the host (editor) should sync document changes to the language server.
/// </summary>
public enum TextDocumentSyncKind
{
    /// <summary>Documents should not be synced at all.</summary>
    None = 0,
    /// <summary>Documents are synced by always sending the full content.</summary>
    Full = 1,
    /// <summary>Documents are synced by sending incremental updates.</summary>
    Incremental = 2
}

/// <summary>
/// Completion item kinds.
/// </summary>
public enum CompletionItemKind
{
    Text = 1,
    Method = 2,
    Function = 3,
    Constructor = 4,
    Field = 5,
    Variable = 6,
    Class = 7,
    Interface = 8,
    Module = 9,
    Property = 10,
    Unit = 11,
    Value = 12,
    Enum = 13,
    Keyword = 14,
    Snippet = 15,
    Color = 16,
    File = 17,
    Reference = 18,
    Folder = 19,
    EnumMember = 20,
    Constant = 21,
    Struct = 22,
    Event = 23,
    Operator = 24,
    TypeParameter = 25
}

/// <summary>
/// A completion item.
/// </summary>
public sealed record CompletionItem
{
    /// <summary>The label of this completion item.</summary>
    [JsonPropertyName("label")]
    public required string Label { get; init; }

    /// <summary>The kind of this completion item.</summary>
    [JsonPropertyName("kind")]
    public CompletionItemKind? Kind { get; init; }

    /// <summary>A human-readable string with additional information about this item.</summary>
    [JsonPropertyName("detail")]
    public string? Detail { get; init; }

    /// <summary>A human-readable string that represents a doc-comment.</summary>
    [JsonPropertyName("documentation")]
    public string? Documentation { get; init; }

    /// <summary>A string that should be inserted into a document when selecting this completion.</summary>
    [JsonPropertyName("insertText")]
    public string? InsertText { get; init; }
}

/// <summary>
/// Represents a collection of completion items.
/// </summary>
public sealed record CompletionList
{
    /// <summary>This list is not complete. Further typing should result in recomputing this list.</summary>
    [JsonPropertyName("isIncomplete")]
    public bool IsIncomplete { get; init; }

    /// <summary>The completion items.</summary>
    [JsonPropertyName("items")]
    public required IReadOnlyList<CompletionItem> Items { get; init; }
}

/// <summary>
/// Hover content.
/// </summary>
public sealed record Hover
{
    /// <summary>The hover's content.</summary>
    [JsonPropertyName("contents")]
    public required MarkupContent Contents { get; init; }

    /// <summary>An optional range is a range inside the text document that is used to visualize the hover.</summary>
    [JsonPropertyName("range")]
    public LspRange? Range { get; init; }
}

/// <summary>
/// A MarkupContent literal represents a string value which content is interpreted based on its kind flag.
/// </summary>
public sealed record MarkupContent
{
    /// <summary>The type of the Markup.</summary>
    [JsonPropertyName("kind")]
    public required string Kind { get; init; }

    /// <summary>The content itself.</summary>
    [JsonPropertyName("value")]
    public required string Value { get; init; }

    public static MarkupContent PlainText(string text) => new() { Kind = "plaintext", Value = text };
    public static MarkupContent Markdown(string markdown) => new() { Kind = "markdown", Value = markdown };
}
