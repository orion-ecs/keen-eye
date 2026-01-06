using System.Text;
using KeenEyes.Graph.Abstractions;
using KeenEyes.Graph.Kesl.Editing;
using KeenEyes.Shaders.Compiler;
using KeenEyes.Shaders.Compiler.CodeGen;

namespace KeenEyes.Graph.Kesl.Preview;

/// <summary>
/// Panel for displaying generated shader code from a visual graph.
/// </summary>
/// <remarks>
/// <para>
/// The code preview panel shows the KESL and/or GLSL code generated from
/// the current shader graph. It automatically regenerates when the graph
/// changes (with debouncing to avoid excessive regeneration).
/// </para>
/// </remarks>
public sealed class CodePreviewPanel
{
    private readonly KeslGraphExporter exporter = new();
    private readonly KeslCompiler keslCompiler = new();
    private readonly GlslGenerator glslGenerator = new();

    private Entity currentCanvas;
    private IWorld? currentWorld;
    private string cachedKeslSource = "";
    private string cachedGlslSource = "";
    private bool isDirty = true;
    private DateTime lastChangeTime;
    private readonly TimeSpan debounceDelay = TimeSpan.FromMilliseconds(300);

    /// <summary>
    /// Gets or sets the active preview tab.
    /// </summary>
    public PreviewTab ActiveTab { get; set; } = PreviewTab.Kesl;

    /// <summary>
    /// Gets whether there are compilation errors.
    /// </summary>
    public bool HasErrors { get; private set; }

    /// <summary>
    /// Gets the current error messages (if any).
    /// </summary>
    public IReadOnlyList<string> ErrorMessages { get; private set; } = [];

    /// <summary>
    /// Gets the generated KESL source code.
    /// </summary>
    public string KeslSource => cachedKeslSource;

    /// <summary>
    /// Gets the generated GLSL source code.
    /// </summary>
    public string GlslSource => cachedGlslSource;

    /// <summary>
    /// Sets the graph to preview.
    /// </summary>
    /// <param name="canvas">The graph canvas entity.</param>
    /// <param name="world">The world containing the graph.</param>
    public void SetGraph(Entity canvas, IWorld world)
    {
        if (currentCanvas != canvas || currentWorld != world)
        {
            currentCanvas = canvas;
            currentWorld = world;
            MarkDirty();
        }
    }

    /// <summary>
    /// Marks the preview as needing regeneration.
    /// </summary>
    public void MarkDirty()
    {
        isDirty = true;
        lastChangeTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the preview, regenerating code if needed.
    /// </summary>
    public void Update()
    {
        if (!isDirty || currentWorld is null)
        {
            return;
        }

        // Debounce: wait until no changes for debounceDelay
        if (DateTime.UtcNow - lastChangeTime < debounceDelay)
        {
            return;
        }

        Regenerate();
        isDirty = false;
    }

    /// <summary>
    /// Forces immediate regeneration of the preview.
    /// </summary>
    public void Regenerate()
    {
        if (currentWorld is null)
        {
            cachedKeslSource = "";
            cachedGlslSource = "";
            HasErrors = false;
            ErrorMessages = [];
            return;
        }

        var errors = new List<string>();

        // Generate KESL source from graph
        var exportResult = exporter.Export(currentCanvas, currentWorld);
        if (exportResult.IsSuccess && exportResult.Source is not null)
        {
            cachedKeslSource = exportResult.Source;

            // Generate GLSL from KESL
            var compileResult = keslCompiler.Compile(cachedKeslSource);
            if (!compileResult.HasErrors && compileResult.SourceFile is not null)
            {
                var glslBuilder = new StringBuilder();
                foreach (var decl in compileResult.SourceFile.Declarations)
                {
                    if (decl is Shaders.Compiler.Parsing.Ast.ComputeDeclaration compute)
                    {
                        var glsl = glslGenerator.Generate(compute);
                        glslBuilder.AppendLine(glsl);
                    }
                }
                cachedGlslSource = glslBuilder.ToString();
            }
            else
            {
                foreach (var error in compileResult.Errors)
                {
                    errors.Add($"[{error.Location.Line}:{error.Location.Column}] {error.Message}");
                }
                cachedGlslSource = "// Compilation errors - see KESL tab";
            }
        }
        else
        {
            foreach (var error in exportResult.Errors)
            {
                errors.Add(error.Message);
            }
            cachedKeslSource = "// Export failed - see errors";
            cachedGlslSource = "";
        }

        HasErrors = errors.Count > 0;
        ErrorMessages = errors;
    }

    /// <summary>
    /// Gets the current source code based on the active tab.
    /// </summary>
    public string GetActiveSource()
    {
        return ActiveTab switch
        {
            PreviewTab.Kesl => cachedKeslSource,
            PreviewTab.Glsl => cachedGlslSource,
            _ => ""
        };
    }

    /// <summary>
    /// Copies the current source code to clipboard.
    /// </summary>
    /// <returns>The source code that would be copied.</returns>
    public string CopyToClipboard()
    {
        return GetActiveSource();
    }
}

/// <summary>
/// Available preview tabs in the code preview panel.
/// </summary>
public enum PreviewTab
{
    /// <summary>KESL source code.</summary>
    Kesl,

    /// <summary>Generated GLSL code.</summary>
    Glsl
}
