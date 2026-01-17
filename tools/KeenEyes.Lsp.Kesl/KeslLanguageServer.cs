using System.Text.Json;
using KeenEyes.Lsp.Kesl.Handlers;
using KeenEyes.Lsp.Kesl.Protocol;
using KeenEyes.Lsp.Kesl.Services;

namespace KeenEyes.Lsp.Kesl;

/// <summary>
/// The KESL Language Server implementation.
/// </summary>
public sealed class KeslLanguageServer
{
    private readonly MessageFraming framing;
    private readonly DocumentManager documentManager;
    private readonly DiagnosticPublisher diagnosticPublisher;
    private readonly InitializeHandler initializeHandler;
    private readonly CompletionHandler completionHandler;
    private readonly HoverHandler hoverHandler;
    private readonly DefinitionHandler definitionHandler;

    private bool shutdown;

    public KeslLanguageServer(Stream input, Stream output)
    {
        framing = new MessageFraming(input, output);
        documentManager = new DocumentManager();
        diagnosticPublisher = new DiagnosticPublisher(framing, documentManager);
        initializeHandler = new InitializeHandler();
        completionHandler = new CompletionHandler(documentManager);
        hoverHandler = new HoverHandler(documentManager);
        definitionHandler = new DefinitionHandler(documentManager);
    }

    /// <summary>
    /// Runs the language server until exit is requested.
    /// </summary>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var message = await framing.ReadMessageAsync(cancellationToken);
                if (message == null)
                {
                    break; // End of input
                }

                await HandleMessageAsync(message, cancellationToken);

                if (shutdown)
                {
                    break;
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                // Log error but continue processing
                Console.Error.WriteLine($"Error processing message: {ex.Message}");
            }
        }
    }

    private async Task HandleMessageAsync(string message, CancellationToken cancellationToken)
    {
        // Try to parse as a request (has id)
        using var doc = JsonDocument.Parse(message);
        var root = doc.RootElement;

        if (!root.TryGetProperty("method", out var methodElement))
        {
            return; // Invalid message
        }

        var method = methodElement.GetString();
        if (method == null)
        {
            return;
        }

        // Check if this is a request (has id) or notification (no id)
        var hasId = root.TryGetProperty("id", out var idElement);
        object? id = null;

        if (hasId)
        {
            id = idElement.ValueKind switch
            {
                JsonValueKind.String => idElement.GetString(),
                JsonValueKind.Number => idElement.GetInt32(),
                _ => null
            };
        }

        JsonElement? @params = null;
        if (root.TryGetProperty("params", out var paramsElement))
        {
            @params = paramsElement;
        }

        if (hasId)
        {
            // Request - needs response
            await HandleRequestAsync(method, id!, @params, cancellationToken);
        }
        else
        {
            // Notification - no response
            await HandleNotificationAsync(method, @params, cancellationToken);
        }
    }

    private async Task HandleRequestAsync(string method, object id, JsonElement? @params, CancellationToken cancellationToken)
    {
        object? result = null;
        LspError? error = null;

        try
        {
            result = method switch
            {
                "initialize" => HandleInitialize(@params),
                "shutdown" => HandleShutdown(),
                "textDocument/completion" => HandleCompletion(@params),
                "textDocument/hover" => HandleHover(@params),
                "textDocument/definition" => HandleDefinition(@params),
                _ => throw new NotSupportedException($"Method '{method}' is not supported")
            };
        }
        catch (NotSupportedException ex)
        {
            error = new LspError
            {
                Code = LspErrorCodes.MethodNotFound,
                Message = ex.Message
            };
        }
        catch (Exception ex)
        {
            error = new LspError
            {
                Code = LspErrorCodes.InternalError,
                Message = ex.Message
            };
        }

        var response = new LspResponse
        {
            Id = id,
            Result = result,
            Error = error
        };

        await framing.WriteResponseAsync(response, cancellationToken);
    }

    private async Task HandleNotificationAsync(string method, JsonElement? @params, CancellationToken cancellationToken)
    {
        switch (method)
        {
            case "initialized":
                // Client signals ready, no action needed
                break;

            case "exit":
                shutdown = true;
                break;

            case "textDocument/didOpen":
                await HandleDidOpenAsync(@params, cancellationToken);
                break;

            case "textDocument/didChange":
                await HandleDidChangeAsync(@params, cancellationToken);
                break;

            case "textDocument/didClose":
                HandleDidClose(@params);
                break;

            case "textDocument/didSave":
                await HandleDidSaveAsync(@params, cancellationToken);
                break;
        }
    }

    private InitializeResult HandleInitialize(JsonElement? @params)
    {
        var initParams = @params.HasValue
            ? JsonSerializer.Deserialize(@params.Value, LspJsonContext.Default.InitializeParams)
            : null;

        return initializeHandler.Handle(initParams ?? new InitializeParams());
    }

    private object? HandleShutdown()
    {
        shutdown = true;
        return null;
    }

    private CompletionList HandleCompletion(JsonElement? @params)
    {
        if (!@params.HasValue)
        {
            return new CompletionList { Items = [] };
        }

        var completionParams = JsonSerializer.Deserialize(@params.Value, LspJsonContext.Default.CompletionParams);
        if (completionParams == null)
        {
            return new CompletionList { Items = [] };
        }

        return completionHandler.Handle(completionParams);
    }

    private Hover? HandleHover(JsonElement? @params)
    {
        if (!@params.HasValue)
        {
            return null;
        }

        var hoverParams = JsonSerializer.Deserialize(@params.Value, LspJsonContext.Default.HoverParams);
        if (hoverParams == null)
        {
            return null;
        }

        return hoverHandler.Handle(hoverParams);
    }

    private Location? HandleDefinition(JsonElement? @params)
    {
        if (!@params.HasValue)
        {
            return null;
        }

        var defParams = JsonSerializer.Deserialize(@params.Value, LspJsonContext.Default.DefinitionParams);
        if (defParams == null)
        {
            return null;
        }

        return definitionHandler.Handle(defParams);
    }

    private async Task HandleDidOpenAsync(JsonElement? @params, CancellationToken cancellationToken)
    {
        if (!@params.HasValue)
        {
            return;
        }

        var openParams = JsonSerializer.Deserialize(@params.Value, LspJsonContext.Default.DidOpenTextDocumentParams);
        if (openParams == null)
        {
            return;
        }

        var textDoc = openParams.TextDocument;
        documentManager.OpenDocument(textDoc.Uri, textDoc.LanguageId, textDoc.Text, textDoc.Version);

        // Publish initial diagnostics
        await diagnosticPublisher.PublishDiagnosticsAsync(textDoc.Uri, cancellationToken);
    }

    private async Task HandleDidChangeAsync(JsonElement? @params, CancellationToken cancellationToken)
    {
        if (!@params.HasValue)
        {
            return;
        }

        var changeParams = JsonSerializer.Deserialize(@params.Value, LspJsonContext.Default.DidChangeTextDocumentParams);
        if (changeParams == null || changeParams.ContentChanges.Count == 0)
        {
            return;
        }

        var uri = changeParams.TextDocument.Uri;
        var version = changeParams.TextDocument.Version;

        // We use full sync, so take the last content change
        var text = changeParams.ContentChanges[^1].Text;
        documentManager.UpdateDocument(uri, text, version);

        // Publish updated diagnostics
        await diagnosticPublisher.PublishDiagnosticsAsync(uri, cancellationToken);
    }

    private void HandleDidClose(JsonElement? @params)
    {
        if (!@params.HasValue)
        {
            return;
        }

        var closeParams = JsonSerializer.Deserialize(@params.Value, LspJsonContext.Default.DidCloseTextDocumentParams);
        if (closeParams == null)
        {
            return;
        }

        documentManager.CloseDocument(closeParams.TextDocument.Uri);
    }

    private async Task HandleDidSaveAsync(JsonElement? @params, CancellationToken cancellationToken)
    {
        if (!@params.HasValue)
        {
            return;
        }

        var saveParams = JsonSerializer.Deserialize(@params.Value, LspJsonContext.Default.DidSaveTextDocumentParams);
        if (saveParams == null)
        {
            return;
        }

        // Re-publish diagnostics on save
        await diagnosticPublisher.PublishDiagnosticsAsync(saveParams.TextDocument.Uri, cancellationToken);
    }
}
