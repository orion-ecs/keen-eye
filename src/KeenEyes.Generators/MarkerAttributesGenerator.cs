using Microsoft.CodeAnalysis;

namespace KeenEyes.Generators;

/// <summary>
/// Generates marker attributes using the [Embedded] pattern for .NET 10+ compatibility.
/// This eliminates the need for a separate Generators.Attributes assembly reference.
/// </summary>
[Generator]
public class MarkerAttributesGenerator : IIncrementalGenerator
{
    /// <summary>
    /// Initializes the generator and registers post-initialization output for embedded attributes.
    /// </summary>
    /// <param name="context">The initialization context.</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx =>
        {
            // Mark all generated attributes as embedded to prevent CS0436 conflicts
            ctx.AddEmbeddedAttributeDefinition();

            // Generate ComponentAttribute
            ctx.AddSource("ComponentAttribute.g.cs", """
                #nullable enable
                using System;
                using System.Diagnostics.CodeAnalysis;

                namespace KeenEyes;

                /// <summary>
                /// Marks a struct as an ECS component.
                /// </summary>
                [global::Microsoft.CodeAnalysis.EmbeddedAttribute]
                [AttributeUsage(AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
                [ExcludeFromCodeCoverage]
                internal sealed class ComponentAttribute : Attribute
                {
                    /// <summary>
                    /// Whether this component should be serializable.
                    /// </summary>
                    public bool Serializable { get; set; }
                }
                """);

            // Generate TagComponentAttribute
            ctx.AddSource("TagComponentAttribute.g.cs", """
                #nullable enable
                using System;
                using System.Diagnostics.CodeAnalysis;

                namespace KeenEyes;

                /// <summary>
                /// Marks a struct as a tag component (no data, just a marker).
                /// </summary>
                [global::Microsoft.CodeAnalysis.EmbeddedAttribute]
                [AttributeUsage(AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
                [ExcludeFromCodeCoverage]
                internal sealed class TagComponentAttribute : Attribute
                {
                }
                """);

            // Generate SystemAttribute
            ctx.AddSource("SystemAttribute.g.cs", """
                #nullable enable
                using System;
                using System.Diagnostics.CodeAnalysis;

                namespace KeenEyes;

                /// <summary>
                /// Marks a class as an ECS system for auto-discovery and registration.
                /// </summary>
                [global::Microsoft.CodeAnalysis.EmbeddedAttribute]
                [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
                [ExcludeFromCodeCoverage]
                internal sealed class SystemAttribute : Attribute
                {
                    /// <summary>
                    /// The phase in which this system runs (e.g., Update, FixedUpdate, Render).
                    /// </summary>
                    public SystemPhase Phase { get; set; } = SystemPhase.Update;

                    /// <summary>
                    /// Execution order within the phase. Lower values run first.
                    /// </summary>
                    public int Order { get; set; }

                    /// <summary>
                    /// Optional group name for organizing related systems.
                    /// </summary>
                    public string? Group { get; set; }
                }
                """);

            // Generate RunBeforeAttribute
            ctx.AddSource("RunBeforeAttribute.g.cs", """
                #nullable enable
                using System;
                using System.Diagnostics.CodeAnalysis;

                namespace KeenEyes;

                /// <summary>
                /// Specifies that this system must run before another system.
                /// </summary>
                [global::Microsoft.CodeAnalysis.EmbeddedAttribute]
                [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
                [ExcludeFromCodeCoverage]
                internal sealed class RunBeforeAttribute(Type targetSystem) : Attribute
                {
                    /// <summary>
                    /// Gets the type of the system that this system must run before.
                    /// </summary>
                    public Type TargetSystem { get; } = targetSystem ?? throw new ArgumentNullException(nameof(targetSystem));
                }
                """);

            // Generate RunAfterAttribute
            ctx.AddSource("RunAfterAttribute.g.cs", """
                #nullable enable
                using System;
                using System.Diagnostics.CodeAnalysis;

                namespace KeenEyes;

                /// <summary>
                /// Specifies that this system must run after another system.
                /// </summary>
                [global::Microsoft.CodeAnalysis.EmbeddedAttribute]
                [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
                [ExcludeFromCodeCoverage]
                internal sealed class RunAfterAttribute(Type targetSystem) : Attribute
                {
                    /// <summary>
                    /// Gets the type of the system that this system must run after.
                    /// </summary>
                    public Type TargetSystem { get; } = targetSystem ?? throw new ArgumentNullException(nameof(targetSystem));
                }
                """);

            // Generate QueryAttribute
            ctx.AddSource("QueryAttribute.g.cs", """
                #nullable enable
                using System;
                using System.Diagnostics.CodeAnalysis;

                namespace KeenEyes;

                /// <summary>
                /// Marks a method as defining a query for batch operations.
                /// </summary>
                [global::Microsoft.CodeAnalysis.EmbeddedAttribute]
                [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
                [ExcludeFromCodeCoverage]
                internal sealed class QueryAttribute : Attribute
                {
                }
                """);

            // Generate BundleAttribute
            ctx.AddSource("BundleAttribute.g.cs", """
                #nullable enable
                using System;
                using System.Diagnostics.CodeAnalysis;

                namespace KeenEyes;

                /// <summary>
                /// Marks a struct as a component bundle (collection of components).
                /// </summary>
                [global::Microsoft.CodeAnalysis.EmbeddedAttribute]
                [AttributeUsage(AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
                [ExcludeFromCodeCoverage]
                internal sealed class BundleAttribute : Attribute
                {
                }
                """);

            // Generate MixinAttribute
            ctx.AddSource("MixinAttribute.g.cs", """
                #nullable enable
                using System;
                using System.Diagnostics.CodeAnalysis;

                namespace KeenEyes;

                /// <summary>
                /// Marks a struct as a mixin that adds methods to entity builders.
                /// </summary>
                [global::Microsoft.CodeAnalysis.EmbeddedAttribute]
                [AttributeUsage(AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
                [ExcludeFromCodeCoverage]
                internal sealed class MixinAttribute : Attribute
                {
                }
                """);

            // Generate RequiresComponentAttribute
            ctx.AddSource("RequiresComponentAttribute.g.cs", """
                #nullable enable
                using System;
                using System.Diagnostics.CodeAnalysis;

                namespace KeenEyes;

                /// <summary>
                /// Specifies that a component requires another component to be present on the same entity.
                /// </summary>
                [global::Microsoft.CodeAnalysis.EmbeddedAttribute]
                [AttributeUsage(AttributeTargets.Struct, Inherited = false, AllowMultiple = true)]
                [ExcludeFromCodeCoverage]
                internal sealed class RequiresComponentAttribute(Type requiredType) : Attribute
                {
                    /// <summary>
                    /// Gets the type of the required component.
                    /// </summary>
                    public Type RequiredType { get; } = requiredType ?? throw new ArgumentNullException(nameof(requiredType));
                }
                """);

            // Generate ConflictsWithAttribute
            ctx.AddSource("ConflictsWithAttribute.g.cs", """
                #nullable enable
                using System;
                using System.Diagnostics.CodeAnalysis;

                namespace KeenEyes;

                /// <summary>
                /// Specifies that a component conflicts with another component and cannot coexist on the same entity.
                /// </summary>
                [global::Microsoft.CodeAnalysis.EmbeddedAttribute]
                [AttributeUsage(AttributeTargets.Struct, Inherited = false, AllowMultiple = true)]
                [ExcludeFromCodeCoverage]
                internal sealed class ConflictsWithAttribute(Type conflictingType) : Attribute
                {
                    /// <summary>
                    /// Gets the type of the conflicting component.
                    /// </summary>
                    public Type ConflictingType { get; } = conflictingType ?? throw new ArgumentNullException(nameof(conflictingType));
                }
                """);

            // Generate PluginExtensionAttribute
            ctx.AddSource("PluginExtensionAttribute.g.cs", """
                #nullable enable
                using System;
                using System.Diagnostics.CodeAnalysis;

                namespace KeenEyes;

                /// <summary>
                /// Marks a class as a plugin extension that should generate a typed accessor property on World.
                /// </summary>
                /// <param name="propertyName">The name of the property to generate on World.</param>
                [global::Microsoft.CodeAnalysis.EmbeddedAttribute]
                [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
                [ExcludeFromCodeCoverage]
                internal sealed class PluginExtensionAttribute(string propertyName) : Attribute
                {
                    /// <summary>
                    /// Gets the property name to generate on World.
                    /// </summary>
                    public string PropertyName { get; } = propertyName;

                    /// <summary>
                    /// Gets or sets whether the extension property is nullable.
                    /// When true, the property returns null if the extension is not registered.
                    /// When false (default), accessing the property throws if the extension is not registered.
                    /// </summary>
                    public bool Nullable { get; set; }
                }
                """);

            // Generate DefaultValueAttribute
            ctx.AddSource("DefaultValueAttribute.g.cs", """
                #nullable enable
                using System;
                using System.Diagnostics.CodeAnalysis;

                namespace KeenEyes;

                /// <summary>
                /// Specifies a default value for a component field in generated builder methods.
                /// </summary>
                /// <param name="value">The default value to use in generated builder methods.</param>
                [global::Microsoft.CodeAnalysis.EmbeddedAttribute]
                [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
                [ExcludeFromCodeCoverage]
                internal sealed class DefaultValueAttribute(object? value) : Attribute
                {
                    /// <summary>
                    /// Gets the default value for the field or property.
                    /// </summary>
                    public object? Value { get; } = value;
                }
                """);

            // Generate BuilderIgnoreAttribute
            ctx.AddSource("BuilderIgnoreAttribute.g.cs", """
                #nullable enable
                using System;
                using System.Diagnostics.CodeAnalysis;

                namespace KeenEyes;

                /// <summary>
                /// Excludes a field from the generated fluent builder method parameters.
                /// The field will use its default value.
                /// </summary>
                [global::Microsoft.CodeAnalysis.EmbeddedAttribute]
                [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
                [ExcludeFromCodeCoverage]
                internal sealed class BuilderIgnoreAttribute : Attribute;
                """);
        });
    }
}
