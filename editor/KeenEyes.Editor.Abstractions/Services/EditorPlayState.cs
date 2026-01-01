// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

namespace KeenEyes.Editor.Abstractions;

/// <summary>
/// Represents the current play mode state of the editor.
/// </summary>
public enum EditorPlayState
{
    /// <summary>
    /// The editor is in edit mode. Scene changes are persisted.
    /// </summary>
    Editing,

    /// <summary>
    /// The game is running. Scene changes are temporary.
    /// </summary>
    Playing,

    /// <summary>
    /// The game is paused. Can be resumed or stopped.
    /// </summary>
    Paused
}
