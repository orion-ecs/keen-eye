namespace KeenEyes.Graph.Kesl.Preview;

/// <summary>
/// Panel for displaying real-time shader execution preview.
/// </summary>
/// <remarks>
/// <para>
/// The shader preview panel shows before/after component values for sample entities
/// after executing a compiled shader. This provides visual feedback about shader
/// behavior without requiring actual GPU execution.
/// </para>
/// <para>
/// The panel automatically regenerates when the graph changes (with debouncing to
/// avoid excessive regeneration). Each update:
/// 1. Compiles the graph to AST
/// 2. Rebuilds preview entities based on query bindings
/// 3. Captures "before" state
/// 4. Executes shader interpretation
/// 5. Captures "after" state
/// </para>
/// </remarks>
public sealed class ShaderPreviewPanel
{
    private readonly PreviewEntityManager entityManager = new();
    private readonly ShaderExecutor executor = new();

    private Entity currentCanvas;
    private IWorld? currentWorld;

    private List<PreviewEntity> beforeState = [];
    private List<PreviewEntity> afterState = [];

    private bool isDirty = true;
    private DateTime lastChangeTime;
    private readonly TimeSpan debounceDelay = TimeSpan.FromMilliseconds(300);

    /// <summary>
    /// Gets or sets the delta time value used for shader execution.
    /// </summary>
    /// <remarks>
    /// Default is 0.016f (~60fps). This value is available to the shader
    /// as the <c>deltaTime</c> parameter.
    /// </remarks>
    public float DeltaTime { get; set; } = 0.016f;

    /// <summary>
    /// Gets or sets the number of preview entities to create.
    /// </summary>
    /// <remarks>
    /// Changing this value will trigger a regeneration on the next update.
    /// </remarks>
    public int EntityCount
    {
        get => entityManager.EntityCount;
        set
        {
            if (entityManager.EntityCount != value)
            {
                entityManager.EntityCount = value;
                MarkDirty();
            }
        }
    }

    /// <summary>
    /// Gets the entity component values before shader execution.
    /// </summary>
    public IReadOnlyList<PreviewEntity> BeforeState => beforeState;

    /// <summary>
    /// Gets the entity component values after shader execution.
    /// </summary>
    public IReadOnlyList<PreviewEntity> AfterState => afterState;

    /// <summary>
    /// Gets the last compilation error message.
    /// </summary>
    public string CompilationError => executor.LastError;

    /// <summary>
    /// Gets whether there was a compilation error.
    /// </summary>
    public bool HasError => executor.HasError;

    /// <summary>
    /// Sets the graph canvas to preview.
    /// </summary>
    /// <param name="canvas">The graph canvas entity.</param>
    /// <param name="world">The world containing the graph.</param>
    public void SetCanvas(Entity canvas, IWorld world)
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
    /// <remarks>
    /// Call this when the graph changes. The actual regeneration will happen
    /// on the next <see cref="Update"/> call after the debounce delay.
    /// </remarks>
    public void MarkDirty()
    {
        isDirty = true;
        lastChangeTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the preview, regenerating if needed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This should be called each frame from the update loop. Regeneration
    /// only occurs after the debounce delay has passed since the last change.
    /// </para>
    /// </remarks>
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
    /// Forces immediate regeneration, bypassing the debounce delay.
    /// </summary>
    public void ForceRegenerate()
    {
        isDirty = false;
        Regenerate();
    }

    /// <summary>
    /// Resets preview entities to their initial values and re-executes.
    /// </summary>
    /// <remarks>
    /// Call this to re-run the shader without recompiling.
    /// </remarks>
    public void ResetAndExecute()
    {
        if (currentWorld is null)
        {
            return;
        }

        // Reset entities to initial values
        entityManager.Reset();

        // Capture before state
        beforeState = entityManager.CloneCurrentState();

        // Execute shader
        executor.Execute(entityManager, DeltaTime);

        // Capture after state
        afterState = entityManager.CloneCurrentState();
    }

    private void Regenerate()
    {
        if (currentWorld is null)
        {
            beforeState = [];
            afterState = [];
            return;
        }

        // Step 1: Compile shader and get query bindings
        if (!executor.Compile(currentCanvas, currentWorld))
        {
            // Compilation failed - clear state
            beforeState = [];
            afterState = [];
            return;
        }

        // Step 2: Rebuild preview entities based on bindings
        entityManager.RebuildFromBindings(executor.CurrentBindings);

        // Step 3: Capture before state
        beforeState = entityManager.CloneCurrentState();

        // Step 4: Execute shader
        executor.Execute(entityManager, DeltaTime);

        // Step 5: Capture after state
        afterState = entityManager.CloneCurrentState();
    }

    /// <summary>
    /// Gets formatted text showing before/after values for display.
    /// </summary>
    /// <returns>A formatted string representation of the preview state.</returns>
    public string GetFormattedOutput()
    {
        if (HasError)
        {
            return $"Error: {CompilationError}";
        }

        if (beforeState.Count == 0 || beforeState.All(e => e.Components.Count == 0))
        {
            return "No preview available. Add query bindings to the shader graph.";
        }

        var lines = new List<string>();

        for (int i = 0; i < beforeState.Count; i++)
        {
            var before = beforeState[i];
            var after = afterState[i];

            lines.Add($"Entity {before.Index}:");

            foreach (var (componentName, beforeComp) in before.Components)
            {
                if (!after.Components.TryGetValue(componentName, out var afterComp))
                {
                    continue;
                }

                lines.Add($"  {componentName}:");

                foreach (var (fieldName, beforeValue) in beforeComp.Fields)
                {
                    if (!afterComp.Fields.TryGetValue(fieldName, out var afterValue))
                    {
                        continue;
                    }

                    var delta = afterValue - beforeValue;
                    var deltaStr = delta >= 0 ? $"+{delta:F3}" : $"{delta:F3}";

                    lines.Add($"    {fieldName}: {beforeValue:F3} -> {afterValue:F3} ({deltaStr})");
                }
            }

            lines.Add("");
        }

        return string.Join("\n", lines);
    }
}
