using System.Collections.Concurrent;

namespace KeenEyes.Lsp.Kesl.Services;

/// <summary>
/// Manages open text documents in the language server.
/// </summary>
public sealed class DocumentManager
{
    private readonly ConcurrentDictionary<string, DocumentState> documents = new();

    /// <summary>
    /// Gets or creates the document state for a URI.
    /// </summary>
    /// <param name="uri">The document URI.</param>
    /// <returns>The document state.</returns>
    public DocumentState GetOrCreate(string uri)
    {
        return documents.GetOrAdd(uri, key => new DocumentState(key));
    }

    /// <summary>
    /// Updates the content of a document.
    /// </summary>
    /// <param name="uri">The document URI.</param>
    /// <param name="text">The new document text.</param>
    /// <param name="version">The document version.</param>
    public void UpdateDocument(string uri, string text, int version)
    {
        var state = GetOrCreate(uri);
        state.Update(text, version);
    }

    /// <summary>
    /// Opens a document.
    /// </summary>
    /// <param name="uri">The document URI.</param>
    /// <param name="languageId">The language identifier.</param>
    /// <param name="text">The document text.</param>
    /// <param name="version">The document version.</param>
    public void OpenDocument(string uri, string languageId, string text, int version)
    {
        var state = GetOrCreate(uri);
        state.Open(languageId, text, version);
    }

    /// <summary>
    /// Closes a document.
    /// </summary>
    /// <param name="uri">The document URI.</param>
    public void CloseDocument(string uri)
    {
        documents.TryRemove(uri, out _);
    }

    /// <summary>
    /// Gets the text of a document.
    /// </summary>
    /// <param name="uri">The document URI.</param>
    /// <returns>The document text, or null if not found.</returns>
    public string? GetText(string uri)
    {
        return documents.TryGetValue(uri, out var state) ? state.Text : null;
    }

    /// <summary>
    /// Gets the document state.
    /// </summary>
    /// <param name="uri">The document URI.</param>
    /// <returns>The document state, or null if not found.</returns>
    public DocumentState? GetDocument(string uri)
    {
        return documents.TryGetValue(uri, out var state) ? state : null;
    }

    /// <summary>
    /// Gets all open document URIs.
    /// </summary>
    public IEnumerable<string> GetOpenDocuments() => documents.Keys;
}

/// <summary>
/// Represents the state of an open document.
/// </summary>
/// <param name="uri">The document URI.</param>
public sealed class DocumentState(string uri)
{
    private string text = string.Empty;
    private string[] lines = [];
    private readonly Lock updateLock = new();

    /// <summary>
    /// The document URI.
    /// </summary>
    public string Uri { get; } = uri;

    /// <summary>
    /// The language identifier.
    /// </summary>
    public string LanguageId { get; private set; } = "kesl";

    /// <summary>
    /// The document version.
    /// </summary>
    public int Version { get; private set; }

    /// <summary>
    /// The document text.
    /// </summary>
    public string Text
    {
        get
        {
            lock (updateLock)
            {
                return text;
            }
        }
    }

    /// <summary>
    /// The document lines (cached for position calculations).
    /// </summary>
    public string[] Lines
    {
        get
        {
            lock (updateLock)
            {
                return lines;
            }
        }
    }

    /// <summary>
    /// Whether the document is open.
    /// </summary>
    public bool IsOpen { get; private set; }

    /// <summary>
    /// Opens the document with initial content.
    /// </summary>
    public void Open(string languageId, string content, int version)
    {
        lock (updateLock)
        {
            LanguageId = languageId;
            text = content;
            lines = content.Split('\n');
            Version = version;
            IsOpen = true;
        }
    }

    /// <summary>
    /// Updates the document content.
    /// </summary>
    public void Update(string content, int version)
    {
        lock (updateLock)
        {
            text = content;
            lines = content.Split('\n');
            Version = version;
        }
    }

    /// <summary>
    /// Converts a zero-based line and character position to an offset in the text.
    /// </summary>
    /// <param name="line">Zero-based line number.</param>
    /// <param name="character">Zero-based character offset.</param>
    /// <returns>The offset in the text.</returns>
    public int PositionToOffset(int line, int character)
    {
        lock (updateLock)
        {
            var offset = 0;
            for (var i = 0; i < line && i < lines.Length; i++)
            {
                offset += lines[i].Length + 1; // +1 for newline
            }

            if (line < lines.Length)
            {
                offset += Math.Min(character, lines[line].Length);
            }

            return Math.Min(offset, text.Length);
        }
    }

    /// <summary>
    /// Converts an offset in the text to a zero-based line and character position.
    /// </summary>
    /// <param name="offset">The offset in the text.</param>
    /// <returns>The line and character position.</returns>
    public (int Line, int Character) OffsetToPosition(int offset)
    {
        lock (updateLock)
        {
            var currentOffset = 0;
            for (var line = 0; line < lines.Length; line++)
            {
                var lineLength = lines[line].Length + 1; // +1 for newline
                if (currentOffset + lineLength > offset)
                {
                    return (line, offset - currentOffset);
                }
                currentOffset += lineLength;
            }

            // Past end of document
            return (lines.Length > 0 ? lines.Length - 1 : 0, lines.Length > 0 ? lines[^1].Length : 0);
        }
    }

    /// <summary>
    /// Gets the word at the given position.
    /// </summary>
    /// <param name="line">Zero-based line number.</param>
    /// <param name="character">Zero-based character offset.</param>
    /// <returns>The word at the position, or null if no word found.</returns>
    public string? GetWordAtPosition(int line, int character)
    {
        lock (updateLock)
        {
            if (line < 0 || line >= lines.Length)
            {
                return null;
            }

            var lineText = lines[line].TrimEnd('\r');
            if (character < 0 || character > lineText.Length)
            {
                return null;
            }

            // Find word boundaries
            var start = character;
            var end = character;

            // Expand left
            while (start > 0 && IsWordChar(lineText[start - 1]))
            {
                start--;
            }

            // Expand right
            while (end < lineText.Length && IsWordChar(lineText[end]))
            {
                end++;
            }

            if (start == end)
            {
                return null;
            }

            return lineText[start..end];
        }
    }

    private static bool IsWordChar(char c) =>
        char.IsLetterOrDigit(c) || c == '_';
}
