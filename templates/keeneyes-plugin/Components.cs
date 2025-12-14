namespace KeenEyesPlugin;

/// <summary>
/// Example component for the plugin.
/// </summary>
/// <remarks>
/// Components should be pure data with no logic.
/// Use the [Component] attribute to generate fluent builder methods.
/// </remarks>
[Component]
public partial struct ExampleComponent
{
    /// <summary>Example value.</summary>
    public int Value;
}

/// <summary>
/// Example tag component for filtering entities.
/// </summary>
/// <remarks>
/// Tag components are zero-size markers used for filtering queries.
/// They generate parameterless builder methods.
/// </remarks>
[TagComponent]
public partial struct ExampleTag;
