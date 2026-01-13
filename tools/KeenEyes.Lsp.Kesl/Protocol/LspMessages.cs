using System.Text.Json;
using System.Text.Json.Serialization;

namespace KeenEyes.Lsp.Kesl.Protocol;

/// <summary>
/// An LSP request message.
/// </summary>
public sealed record LspRequest
{
    /// <summary>The JSON-RPC version (always "2.0").</summary>
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; init; } = LspHeaders.JsonRpc;

    /// <summary>The request id.</summary>
    [JsonPropertyName("id")]
    public required object Id { get; init; }

    /// <summary>The method to be invoked.</summary>
    [JsonPropertyName("method")]
    public required string Method { get; init; }

    /// <summary>The method's params.</summary>
    [JsonPropertyName("params")]
    public JsonElement? Params { get; init; }
}

/// <summary>
/// An LSP notification message (no id, no response expected).
/// </summary>
public sealed record LspNotification
{
    /// <summary>The JSON-RPC version (always "2.0").</summary>
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; init; } = LspHeaders.JsonRpc;

    /// <summary>The method to be invoked.</summary>
    [JsonPropertyName("method")]
    public required string Method { get; init; }

    /// <summary>The method's params.</summary>
    [JsonPropertyName("params")]
    public JsonElement? Params { get; init; }
}

/// <summary>
/// An LSP response message.
/// </summary>
public sealed record LspResponse
{
    /// <summary>The JSON-RPC version (always "2.0").</summary>
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; init; } = LspHeaders.JsonRpc;

    /// <summary>The request id.</summary>
    [JsonPropertyName("id")]
    public required object? Id { get; init; }

    /// <summary>The result of a request (mutually exclusive with error).</summary>
    [JsonPropertyName("result")]
    public object? Result { get; init; }

    /// <summary>The error object in case a request fails.</summary>
    [JsonPropertyName("error")]
    public LspError? Error { get; init; }
}

/// <summary>
/// An LSP error object.
/// </summary>
public sealed record LspError
{
    /// <summary>A number indicating the error type that occurred.</summary>
    [JsonPropertyName("code")]
    public required int Code { get; init; }

    /// <summary>A string providing a short description of the error.</summary>
    [JsonPropertyName("message")]
    public required string Message { get; init; }

    /// <summary>A primitive or structured value that contains additional information about the error.</summary>
    [JsonPropertyName("data")]
    public object? Data { get; init; }
}

/// <summary>
/// Standard LSP error codes.
/// </summary>
public static class LspErrorCodes
{
    // JSON-RPC standard errors
    public const int ParseError = -32700;
    public const int InvalidRequest = -32600;
    public const int MethodNotFound = -32601;
    public const int InvalidParams = -32602;
    public const int InternalError = -32603;

    // LSP-specific errors
    public const int ServerNotInitialized = -32002;
    public const int UnknownErrorCode = -32001;
    public const int RequestFailed = -32803;
    public const int ServerCancelled = -32802;
    public const int ContentModified = -32801;
    public const int RequestCancelled = -32800;
}

/// <summary>
/// Parameters for initialize request.
/// </summary>
public sealed record InitializeParams
{
    /// <summary>The process ID of the parent process that started the server.</summary>
    [JsonPropertyName("processId")]
    public int? ProcessId { get; init; }

    /// <summary>The root URI of the workspace.</summary>
    [JsonPropertyName("rootUri")]
    public string? RootUri { get; init; }

    /// <summary>The capabilities provided by the client.</summary>
    [JsonPropertyName("capabilities")]
    public ClientCapabilities? Capabilities { get; init; }
}

/// <summary>
/// Client capabilities.
/// </summary>
public sealed record ClientCapabilities
{
    /// <summary>Text document capabilities.</summary>
    [JsonPropertyName("textDocument")]
    public TextDocumentClientCapabilities? TextDocument { get; init; }
}

/// <summary>
/// Text document client capabilities.
/// </summary>
public sealed record TextDocumentClientCapabilities
{
    /// <summary>Capabilities specific to completion.</summary>
    [JsonPropertyName("completion")]
    public CompletionClientCapabilities? Completion { get; init; }

    /// <summary>Capabilities specific to hover.</summary>
    [JsonPropertyName("hover")]
    public HoverClientCapabilities? Hover { get; init; }
}

/// <summary>
/// Completion client capabilities.
/// </summary>
public sealed record CompletionClientCapabilities
{
    /// <summary>The client supports snippets as insert text.</summary>
    [JsonPropertyName("snippetSupport")]
    public bool SnippetSupport { get; init; }
}

/// <summary>
/// Hover client capabilities.
/// </summary>
public sealed record HoverClientCapabilities
{
    /// <summary>Client supports the follow content formats.</summary>
    [JsonPropertyName("contentFormat")]
    public IReadOnlyList<string>? ContentFormat { get; init; }
}

/// <summary>
/// Result of initialize request.
/// </summary>
public sealed record InitializeResult
{
    /// <summary>The capabilities the server provides.</summary>
    [JsonPropertyName("capabilities")]
    public required ServerCapabilities Capabilities { get; init; }

    /// <summary>Information about the server.</summary>
    [JsonPropertyName("serverInfo")]
    public ServerInfo? ServerInfo { get; init; }
}

/// <summary>
/// Server capabilities.
/// </summary>
public sealed record ServerCapabilities
{
    /// <summary>Defines how text documents are synced.</summary>
    [JsonPropertyName("textDocumentSync")]
    public TextDocumentSyncKind? TextDocumentSync { get; init; }

    /// <summary>The server provides completion support.</summary>
    [JsonPropertyName("completionProvider")]
    public CompletionOptions? CompletionProvider { get; init; }

    /// <summary>The server provides hover support.</summary>
    [JsonPropertyName("hoverProvider")]
    public bool? HoverProvider { get; init; }
}

/// <summary>
/// Completion options.
/// </summary>
public sealed record CompletionOptions
{
    /// <summary>The characters that trigger completion automatically.</summary>
    [JsonPropertyName("triggerCharacters")]
    public IReadOnlyList<string>? TriggerCharacters { get; init; }

    /// <summary>The server provides support to resolve additional information for a completion item.</summary>
    [JsonPropertyName("resolveProvider")]
    public bool? ResolveProvider { get; init; }
}

/// <summary>
/// Server information.
/// </summary>
public sealed record ServerInfo
{
    /// <summary>The name of the server.</summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>The server's version.</summary>
    [JsonPropertyName("version")]
    public string? Version { get; init; }
}

/// <summary>
/// Parameters for textDocument/didOpen notification.
/// </summary>
public sealed record DidOpenTextDocumentParams
{
    /// <summary>The document that was opened.</summary>
    [JsonPropertyName("textDocument")]
    public required TextDocumentItem TextDocument { get; init; }
}

/// <summary>
/// Parameters for textDocument/didChange notification.
/// </summary>
public sealed record DidChangeTextDocumentParams
{
    /// <summary>The document that did change.</summary>
    [JsonPropertyName("textDocument")]
    public required VersionedTextDocumentIdentifier TextDocument { get; init; }

    /// <summary>The actual content changes.</summary>
    [JsonPropertyName("contentChanges")]
    public required IReadOnlyList<TextDocumentContentChangeEvent> ContentChanges { get; init; }
}

/// <summary>
/// An event describing a change to a text document.
/// </summary>
public sealed record TextDocumentContentChangeEvent
{
    /// <summary>The new text of the whole document.</summary>
    [JsonPropertyName("text")]
    public required string Text { get; init; }
}

/// <summary>
/// Parameters for textDocument/didClose notification.
/// </summary>
public sealed record DidCloseTextDocumentParams
{
    /// <summary>The document that was closed.</summary>
    [JsonPropertyName("textDocument")]
    public required TextDocumentIdentifier TextDocument { get; init; }
}

/// <summary>
/// Parameters for textDocument/didSave notification.
/// </summary>
public sealed record DidSaveTextDocumentParams
{
    /// <summary>The document that was saved.</summary>
    [JsonPropertyName("textDocument")]
    public required TextDocumentIdentifier TextDocument { get; init; }

    /// <summary>Optional the content when saved.</summary>
    [JsonPropertyName("text")]
    public string? Text { get; init; }
}

/// <summary>
/// Parameters for textDocument/completion request.
/// </summary>
public sealed record CompletionParams : TextDocumentPositionParams
{
    /// <summary>The completion context.</summary>
    [JsonPropertyName("context")]
    public CompletionContext? Context { get; init; }
}

/// <summary>
/// Completion trigger context.
/// </summary>
public sealed record CompletionContext
{
    /// <summary>How the completion was triggered.</summary>
    [JsonPropertyName("triggerKind")]
    public CompletionTriggerKind TriggerKind { get; init; }

    /// <summary>The trigger character if applicable.</summary>
    [JsonPropertyName("triggerCharacter")]
    public string? TriggerCharacter { get; init; }
}

/// <summary>
/// How a completion was triggered.
/// </summary>
public enum CompletionTriggerKind
{
    /// <summary>Completion was triggered by typing.</summary>
    Invoked = 1,
    /// <summary>Completion was triggered by a trigger character.</summary>
    TriggerCharacter = 2,
    /// <summary>Completion was re-triggered due to cursor movement.</summary>
    TriggerForIncompleteCompletions = 3
}

/// <summary>
/// Parameters for textDocument/hover request.
/// </summary>
public sealed record HoverParams : TextDocumentPositionParams;
