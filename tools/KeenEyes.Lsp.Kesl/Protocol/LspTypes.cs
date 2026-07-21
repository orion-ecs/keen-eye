using System.Text.Json.Serialization;

namespace KeenEyes.Lsp.Kesl.Protocol;

/// <summary>
/// LSP message header constants.
/// </summary>
public static class LspHeaders
{
    /// <summary>The name of the header that specifies the length, in bytes, of the message content.</summary>
    public const string ContentLength = "Content-Length";
    /// <summary>The name of the header that specifies the MIME type and encoding of the message content.</summary>
    public const string ContentType = "Content-Type";
    /// <summary>The JSON-RPC protocol version used by LSP messages.</summary>
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
    /// <summary>Reports an error.</summary>
    Error = 1,
    /// <summary>Reports a warning.</summary>
    Warning = 2,
    /// <summary>Reports an informational message.</summary>
    Information = 3,
    /// <summary>Reports a hint.</summary>
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
    /// <summary>A plain text completion item.</summary>
    Text = 1,
    /// <summary>A method completion item.</summary>
    Method = 2,
    /// <summary>A function completion item.</summary>
    Function = 3,
    /// <summary>A constructor completion item.</summary>
    Constructor = 4,
    /// <summary>A field completion item.</summary>
    Field = 5,
    /// <summary>A variable completion item.</summary>
    Variable = 6,
    /// <summary>A class completion item.</summary>
    Class = 7,
    /// <summary>An interface completion item.</summary>
    Interface = 8,
    /// <summary>A module completion item.</summary>
    Module = 9,
    /// <summary>A property completion item.</summary>
    Property = 10,
    /// <summary>A unit completion item.</summary>
    Unit = 11,
    /// <summary>A value completion item.</summary>
    Value = 12,
    /// <summary>An enum completion item.</summary>
    Enum = 13,
    /// <summary>A keyword completion item.</summary>
    Keyword = 14,
    /// <summary>A snippet completion item.</summary>
    Snippet = 15,
    /// <summary>A color completion item.</summary>
    Color = 16,
    /// <summary>A file completion item.</summary>
    File = 17,
    /// <summary>A reference completion item.</summary>
    Reference = 18,
    /// <summary>A folder completion item.</summary>
    Folder = 19,
    /// <summary>An enum member completion item.</summary>
    EnumMember = 20,
    /// <summary>A constant completion item.</summary>
    Constant = 21,
    /// <summary>A struct completion item.</summary>
    Struct = 22,
    /// <summary>An event completion item.</summary>
    Event = 23,
    /// <summary>An operator completion item.</summary>
    Operator = 24,
    /// <summary>A type parameter completion item.</summary>
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

    /// <summary>Creates a <see cref="MarkupContent"/> whose value is interpreted as plain text.</summary>
    /// <param name="text">The plain text content.</param>
    /// <returns>A <see cref="MarkupContent"/> with kind set to "plaintext".</returns>
    public static MarkupContent PlainText(string text) => new() { Kind = "plaintext", Value = text };
    /// <summary>Creates a <see cref="MarkupContent"/> whose value is interpreted as Markdown.</summary>
    /// <param name="markdown">The Markdown content.</param>
    /// <returns>A <see cref="MarkupContent"/> with kind set to "markdown".</returns>
    public static MarkupContent Markdown(string markdown) => new() { Kind = "markdown", Value = markdown };
}
