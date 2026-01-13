using System.Text.Json;
using System.Text.Json.Serialization;

namespace KeenEyes.Lsp.Kesl.Protocol;

/// <summary>
/// AOT-compatible JSON serialization context for LSP types.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(LspRequest))]
[JsonSerializable(typeof(LspNotification))]
[JsonSerializable(typeof(LspResponse))]
[JsonSerializable(typeof(LspError))]
[JsonSerializable(typeof(InitializeParams))]
[JsonSerializable(typeof(InitializeResult))]
[JsonSerializable(typeof(ServerCapabilities))]
[JsonSerializable(typeof(ServerInfo))]
[JsonSerializable(typeof(ClientCapabilities))]
[JsonSerializable(typeof(TextDocumentClientCapabilities))]
[JsonSerializable(typeof(CompletionClientCapabilities))]
[JsonSerializable(typeof(HoverClientCapabilities))]
[JsonSerializable(typeof(CompletionOptions))]
[JsonSerializable(typeof(DidOpenTextDocumentParams))]
[JsonSerializable(typeof(DidChangeTextDocumentParams))]
[JsonSerializable(typeof(DidCloseTextDocumentParams))]
[JsonSerializable(typeof(DidSaveTextDocumentParams))]
[JsonSerializable(typeof(TextDocumentContentChangeEvent))]
[JsonSerializable(typeof(TextDocumentItem))]
[JsonSerializable(typeof(TextDocumentIdentifier))]
[JsonSerializable(typeof(VersionedTextDocumentIdentifier))]
[JsonSerializable(typeof(TextDocumentPositionParams))]
[JsonSerializable(typeof(CompletionParams))]
[JsonSerializable(typeof(CompletionContext))]
[JsonSerializable(typeof(HoverParams))]
[JsonSerializable(typeof(PublishDiagnosticsParams))]
[JsonSerializable(typeof(LspDiagnostic))]
[JsonSerializable(typeof(DiagnosticRelatedInformation))]
[JsonSerializable(typeof(LspPosition))]
[JsonSerializable(typeof(LspRange))]
[JsonSerializable(typeof(Location))]
[JsonSerializable(typeof(CompletionList))]
[JsonSerializable(typeof(CompletionItem))]
[JsonSerializable(typeof(Hover))]
[JsonSerializable(typeof(MarkupContent))]
[JsonSerializable(typeof(IReadOnlyList<LspDiagnostic>))]
[JsonSerializable(typeof(IReadOnlyList<CompletionItem>))]
[JsonSerializable(typeof(IReadOnlyList<TextDocumentContentChangeEvent>))]
[JsonSerializable(typeof(IReadOnlyList<string>))]
[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(LspNotificationMessage<PublishDiagnosticsParams>))]
internal sealed partial class LspJsonContext : JsonSerializerContext
{
}
