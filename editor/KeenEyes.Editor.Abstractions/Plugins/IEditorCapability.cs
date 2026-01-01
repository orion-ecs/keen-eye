// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

namespace KeenEyes.Editor.Abstractions;

/// <summary>
/// Marker interface for editor capabilities.
/// </summary>
/// <remarks>
/// <para>
/// All editor capability interfaces must implement this interface.
/// This enables type-safe capability access through <see cref="IEditorContext.GetCapability{T}"/>.
/// </para>
/// <para>
/// Editor capabilities provide extension points for plugins to add functionality:
/// <list type="bullet">
/// <item><description>Inspector capabilities for property drawers and component inspectors</description></item>
/// <item><description>Menu capabilities for menu items and toolbar buttons</description></item>
/// <item><description>Panel capabilities for dockable panels</description></item>
/// <item><description>Viewport capabilities for gizmos and overlays</description></item>
/// <item><description>Tool capabilities for viewport tools</description></item>
/// <item><description>Shortcut capabilities for keyboard shortcuts</description></item>
/// <item><description>Asset capabilities for importers and thumbnails</description></item>
/// </list>
/// </para>
/// </remarks>
/// <seealso cref="IEditorContext"/>
/// <seealso cref="IEditorPlugin"/>
public interface IEditorCapability;
