using System.Text;
using System.Text.Json;

namespace KeenEyes.Lsp.Kesl.Protocol;

/// <summary>
/// Handles LSP message framing (Content-Length headers) over streams.
/// </summary>
/// <param name="inputStream">The input stream to read messages from.</param>
/// <param name="outputStream">The output stream to write messages to.</param>
public sealed class MessageFraming(Stream inputStream, Stream outputStream)
{
    private readonly Lock writeLock = new();

    /// <summary>
    /// Reads the next LSP message from the input stream.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The JSON content of the message, or null if the stream ended.</returns>
    public async Task<string?> ReadMessageAsync(CancellationToken cancellationToken = default)
    {
        // Read headers until we find Content-Length
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var headerLine = await ReadLineAsync(cancellationToken);

        while (!string.IsNullOrEmpty(headerLine))
        {
            var colonIndex = headerLine.IndexOf(':');
            if (colonIndex > 0)
            {
                var key = headerLine[..colonIndex].Trim();
                var value = headerLine[(colonIndex + 1)..].Trim();
                headers[key] = value;
            }

            headerLine = await ReadLineAsync(cancellationToken);
        }

        // Check for Content-Length header
        if (!headers.TryGetValue(LspHeaders.ContentLength, out var contentLengthStr) ||
            !int.TryParse(contentLengthStr, out var contentLength))
        {
            return null;
        }

        // Read the content
        var buffer = new byte[contentLength];
        var totalRead = 0;
        while (totalRead < contentLength)
        {
            var bytesRead = await inputStream.ReadAsync(buffer.AsMemory(totalRead, contentLength - totalRead), cancellationToken);
            if (bytesRead == 0)
            {
                return null; // Stream ended unexpectedly
            }
            totalRead += bytesRead;
        }

        return Encoding.UTF8.GetString(buffer);
    }

    /// <summary>
    /// Reads a line from the input stream (terminated by \r\n).
    /// </summary>
    private async Task<string> ReadLineAsync(CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        var buffer = new byte[1];
        var lastChar = '\0';

        while (true)
        {
            var bytesRead = await inputStream.ReadAsync(buffer, cancellationToken);
            if (bytesRead == 0)
            {
                return sb.ToString();
            }

            var c = (char)buffer[0];

            // Check for \r\n line ending
            if (lastChar == '\r' && c == '\n')
            {
                // Remove the \r we added
                if (sb.Length > 0)
                {
                    sb.Length--;
                }
                return sb.ToString();
            }

            sb.Append(c);
            lastChar = c;
        }
    }

    /// <summary>
    /// Writes an LSP message to the output stream.
    /// </summary>
    /// <param name="content">The JSON content of the message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task WriteMessageAsync(string content, CancellationToken cancellationToken = default)
    {
        var contentBytes = Encoding.UTF8.GetBytes(content);
        var header = $"{LspHeaders.ContentLength}: {contentBytes.Length}\r\n\r\n";
        var headerBytes = Encoding.ASCII.GetBytes(header);

        lock (writeLock)
        {
            outputStream.Write(headerBytes);
            outputStream.Write(contentBytes);
            outputStream.Flush();
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Writes an LSP response to the output stream.
    /// </summary>
    /// <param name="response">The response to write.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task WriteResponseAsync(LspResponse response, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(response, LspJsonContext.Default.LspResponse);
        await WriteMessageAsync(json, cancellationToken);
    }

    /// <summary>
    /// Writes an LSP notification to the output stream.
    /// </summary>
    /// <param name="method">The notification method.</param>
    /// <param name="params">The notification parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task WriteNotificationAsync(string method, PublishDiagnosticsParams @params, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(new LspNotificationMessage<PublishDiagnosticsParams>
        {
            Method = method,
            Params = @params
        }, LspJsonContext.Default.LspNotificationMessagePublishDiagnosticsParams);

        await WriteMessageAsync(json, cancellationToken);
    }
}

/// <summary>
/// A typed notification message for AOT-compatible serialization.
/// </summary>
/// <typeparam name="T">The type of the params.</typeparam>
public sealed record LspNotificationMessage<T>
{
    /// <summary>The JSON-RPC version.</summary>
    [System.Text.Json.Serialization.JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; init; } = LspHeaders.JsonRpc;

    /// <summary>The method name.</summary>
    [System.Text.Json.Serialization.JsonPropertyName("method")]
    public required string Method { get; init; }

    /// <summary>The params.</summary>
    [System.Text.Json.Serialization.JsonPropertyName("params")]
    public required T Params { get; init; }
}
