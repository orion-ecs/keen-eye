using KeenEyes.Lsp.Kesl.Protocol;
using KeenEyes.Lsp.Kesl.Services;
using KeenEyes.Shaders.Compiler;
using KeenEyes.Shaders.Compiler.Parsing.Ast;

namespace KeenEyes.Lsp.Kesl.Handlers;

/// <summary>
/// Handles textDocument/definition requests.
/// </summary>
public sealed class DefinitionHandler(DocumentManager documentManager)
{
    /// <summary>
    /// Handles a definition request.
    /// </summary>
    /// <param name="params">The definition parameters.</param>
    /// <returns>The location of the definition, or null if not found.</returns>
    public Location? Handle(DefinitionParams @params)
    {
        var document = documentManager.GetDocument(@params.TextDocument.Uri);
        if (document == null)
        {
            return null;
        }

        // Get the word at the cursor position
        var word = document.GetWordAtPosition(
            @params.Position.Line,
            @params.Position.Character);

        if (string.IsNullOrEmpty(word))
        {
            return null;
        }

        // Parse the document to get AST
        var text = document.Text;
        var result = KeslCompiler.Compile(text, @params.TextDocument.Uri);
        if (result.SourceFile == null)
        {
            return null;
        }

        // Find component declaration matching the word
        var declaration = FindComponentDeclaration(result.SourceFile, word);
        if (declaration == null)
        {
            return null;
        }

        // Convert 1-based source location to 0-based LSP position
        return new Location
        {
            Uri = @params.TextDocument.Uri,
            Range = new LspRange
            {
                Start = new LspPosition
                {
                    Line = declaration.Location.Line - 1,
                    Character = declaration.Location.Column - 1
                },
                End = new LspPosition
                {
                    Line = declaration.Location.Line - 1,
                    Character = declaration.Location.Column - 1 + declaration.Name.Length
                }
            }
        };
    }

    private static ComponentDeclaration? FindComponentDeclaration(
        SourceFile sourceFile,
        string componentName)
    {
        foreach (var decl in sourceFile.Declarations)
        {
            if (decl is ComponentDeclaration component &&
                string.Equals(component.Name, componentName, StringComparison.Ordinal))
            {
                return component;
            }
        }

        return null;
    }
}
