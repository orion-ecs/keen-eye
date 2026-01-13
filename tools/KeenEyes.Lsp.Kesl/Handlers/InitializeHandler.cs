using KeenEyes.Lsp.Kesl.Protocol;

namespace KeenEyes.Lsp.Kesl.Handlers;

/// <summary>
/// Handles the initialize request.
/// </summary>
public sealed class InitializeHandler
{
    /// <summary>
    /// Handles the initialize request.
    /// </summary>
    /// <param name="params">The initialize parameters.</param>
    /// <returns>The initialize result.</returns>
    public InitializeResult Handle(InitializeParams @params)
    {
        return new InitializeResult
        {
            Capabilities = new ServerCapabilities
            {
                TextDocumentSync = TextDocumentSyncKind.Full,
                CompletionProvider = new CompletionOptions
                {
                    TriggerCharacters = [".", "@", ":"],
                    ResolveProvider = false
                },
                HoverProvider = true
            },
            ServerInfo = new ServerInfo
            {
                Name = "KESL Language Server",
                Version = "0.1.0"
            }
        };
    }
}
