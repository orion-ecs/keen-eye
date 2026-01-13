using KeenEyes.Lsp.Kesl.Protocol;
using KeenEyes.Shaders.Compiler;
using KeenEyes.Shaders.Compiler.Diagnostics;

namespace KeenEyes.Lsp.Kesl.Services;

/// <summary>
/// Publishes diagnostics to the LSP client.
/// </summary>
/// <param name="framing">The message framing for output.</param>
/// <param name="documentManager">The document manager.</param>
public sealed class DiagnosticPublisher(MessageFraming framing, DocumentManager documentManager)
{

    /// <summary>
    /// Compiles a document and publishes diagnostics.
    /// </summary>
    /// <param name="uri">The document URI.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task PublishDiagnosticsAsync(string uri, CancellationToken cancellationToken = default)
    {
        var document = documentManager.GetDocument(uri);
        if (document == null)
        {
            return;
        }

        var text = document.Text;
        var result = KeslCompiler.Compile(text, uri);

        var lspDiagnostics = ConvertDiagnostics(result.Diagnostics, document);

        var @params = new PublishDiagnosticsParams
        {
            Uri = uri,
            Version = document.Version,
            Diagnostics = lspDiagnostics
        };

        await framing.WriteNotificationAsync("textDocument/publishDiagnostics", @params, cancellationToken);
    }

    /// <summary>
    /// Clears diagnostics for a document.
    /// </summary>
    /// <param name="uri">The document URI.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task ClearDiagnosticsAsync(string uri, CancellationToken cancellationToken = default)
    {
        var @params = new PublishDiagnosticsParams
        {
            Uri = uri,
            Diagnostics = []
        };

        await framing.WriteNotificationAsync("textDocument/publishDiagnostics", @params, cancellationToken);
    }

    /// <summary>
    /// Converts KESL diagnostics to LSP diagnostics.
    /// </summary>
    private static List<LspDiagnostic> ConvertDiagnostics(IReadOnlyList<Diagnostic> diagnostics, DocumentState document)
    {
        var lspDiagnostics = new List<LspDiagnostic>();

        foreach (var diagnostic in diagnostics)
        {
            var message = FormatMessage(diagnostic);

            var startLine = Math.Max(0, diagnostic.Span.Start.Line - 1); // Convert to 0-based
            var startChar = Math.Max(0, diagnostic.Span.Start.Column - 1);
            var endLine = Math.Max(0, diagnostic.Span.End.Line - 1);
            var endChar = Math.Max(0, diagnostic.Span.End.Column - 1);

            lspDiagnostics.Add(new LspDiagnostic
            {
                Range = new LspRange
                {
                    Start = new LspPosition { Line = startLine, Character = startChar },
                    End = new LspPosition { Line = endLine, Character = endChar }
                },
                Severity = diagnostic.Severity switch
                {
                    DiagnosticSeverity.Error => LspDiagnosticSeverity.Error,
                    DiagnosticSeverity.Warning => LspDiagnosticSeverity.Warning,
                    _ => LspDiagnosticSeverity.Information
                },
                Code = diagnostic.Code,
                Source = "kesl",
                Message = message
            });
        }

        return lspDiagnostics;
    }

    /// <summary>
    /// Formats the diagnostic message, including suggestions if present.
    /// </summary>
    private static string FormatMessage(Diagnostic diagnostic)
    {
        var message = diagnostic.Message;

        if (diagnostic.Suggestions is { Count: > 0 })
        {
            if (diagnostic.Suggestions.Count == 1)
            {
                message += $" Did you mean '{diagnostic.Suggestions[0]}'?";
            }
            else
            {
                message += $" Did you mean: {string.Join(", ", diagnostic.Suggestions)}?";
            }
        }

        return message;
    }
}
