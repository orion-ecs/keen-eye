// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

namespace KeenEyes.Editor.Abstractions;

/// <summary>
/// Marks a class as an editor extension that should generate a typed accessor property on IEditorContext.
/// </summary>
/// <remarks>
/// <para>
/// When applied to a class, the source generator will create a C# 13 extension member
/// that provides direct property access instead of using IEditorContext.GetExtension.
/// </para>
/// <para>
/// The generated extension allows code like <c>context.MyExtension</c> instead of
/// <c>context.GetExtension&lt;MyExtensionClass&gt;()</c>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Mark the extension class
/// [EditorExtension("SceneAnalyzer")]
/// public sealed class SceneAnalyzerExtension
/// {
///     public IEnumerable&lt;Entity&gt; FindOrphanedEntities() { ... }
/// }
///
/// // Usage in plugin code becomes:
/// var orphans = context.SceneAnalyzer.FindOrphanedEntities();
/// </code>
/// </example>
/// <param name="propertyName">The name of the property to generate on IEditorContext.</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
[ExcludeFromCodeCoverage]
public sealed class EditorExtensionAttribute(string propertyName) : Attribute
{
    /// <summary>
    /// Gets the property name to generate on IEditorContext.
    /// </summary>
    public string PropertyName { get; } = propertyName;

    /// <summary>
    /// Gets or sets whether the extension property is nullable.
    /// When true, the property returns null if the extension is not registered.
    /// When false (default), accessing the property throws if the extension is not registered.
    /// </summary>
    public bool Nullable { get; set; }
}
