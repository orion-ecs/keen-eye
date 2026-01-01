namespace KeenEyes.Editor.Shortcuts;

/// <summary>
/// Defines all standard editor shortcuts and provides action ID constants.
/// </summary>
public static class EditorShortcuts
{
    #region Action IDs

    // File operations
    /// <summary>Create a new scene.</summary>
    public const string NewScene = "file.new_scene";
    /// <summary>Open an existing scene.</summary>
    public const string OpenScene = "file.open_scene";
    /// <summary>Save the current scene.</summary>
    public const string SaveScene = "file.save_scene";
    /// <summary>Save the current scene with a new name.</summary>
    public const string SaveSceneAs = "file.save_scene_as";
    /// <summary>Create a new project.</summary>
    public const string NewProject = "file.new_project";
    /// <summary>Open an existing project.</summary>
    public const string OpenProject = "file.open_project";
    /// <summary>Exit the editor.</summary>
    public const string Exit = "file.exit";

    // Edit operations
    /// <summary>Undo the last action.</summary>
    public const string Undo = "edit.undo";
    /// <summary>Redo the last undone action.</summary>
    public const string Redo = "edit.redo";
    /// <summary>Cut selection to clipboard.</summary>
    public const string Cut = "edit.cut";
    /// <summary>Copy selection to clipboard.</summary>
    public const string Copy = "edit.copy";
    /// <summary>Paste from clipboard.</summary>
    public const string Paste = "edit.paste";
    /// <summary>Delete selection.</summary>
    public const string Delete = "edit.delete";
    /// <summary>Select all entities.</summary>
    public const string SelectAll = "edit.select_all";
    /// <summary>Duplicate selection.</summary>
    public const string Duplicate = "edit.duplicate";
    /// <summary>Rename selected entity.</summary>
    public const string Rename = "edit.rename";
    /// <summary>Open settings/preferences.</summary>
    public const string Settings = "edit.settings";

    // Entity operations
    /// <summary>Create an empty entity.</summary>
    public const string CreateEmpty = "entity.create_empty";
    /// <summary>Create a child entity.</summary>
    public const string CreateChild = "entity.create_child";

    // View operations
    /// <summary>Focus on the selected entity.</summary>
    public const string FocusSelection = "view.focus_selection";
    /// <summary>Toggle grid visibility.</summary>
    public const string ToggleGrid = "view.toggle_grid";
    /// <summary>Toggle wireframe mode.</summary>
    public const string ToggleWireframe = "view.toggle_wireframe";

    // Transform modes
    /// <summary>Switch to translate mode.</summary>
    public const string TranslateMode = "transform.translate";
    /// <summary>Switch to rotate mode.</summary>
    public const string RotateMode = "transform.rotate";
    /// <summary>Switch to scale mode.</summary>
    public const string ScaleMode = "transform.scale";
    /// <summary>Toggle local/world space.</summary>
    public const string ToggleSpace = "transform.toggle_space";

    // Play mode
    /// <summary>Toggle play mode.</summary>
    public const string PlayStop = "play.play_stop";
    /// <summary>Pause/resume play mode.</summary>
    public const string Pause = "play.pause";
    /// <summary>Step one frame forward.</summary>
    public const string StepFrame = "play.step_frame";

    // Window operations
    /// <summary>Show hierarchy panel.</summary>
    public const string ShowHierarchy = "window.hierarchy";
    /// <summary>Show inspector panel.</summary>
    public const string ShowInspector = "window.inspector";
    /// <summary>Show project panel.</summary>
    public const string ShowProject = "window.project";
    /// <summary>Show console panel.</summary>
    public const string ShowConsole = "window.console";
    /// <summary>Reset window layout.</summary>
    public const string ResetLayout = "window.reset_layout";

    // Help
    /// <summary>Open documentation.</summary>
    public const string Documentation = "help.documentation";
    /// <summary>Show about dialog.</summary>
    public const string About = "help.about";

    #endregion

    #region Categories

    /// <summary>File operations category.</summary>
    public const string CategoryFile = "File";
    /// <summary>Edit operations category.</summary>
    public const string CategoryEdit = "Edit";
    /// <summary>Entity operations category.</summary>
    public const string CategoryEntity = "Entity";
    /// <summary>View operations category.</summary>
    public const string CategoryView = "View";
    /// <summary>Transform operations category.</summary>
    public const string CategoryTransform = "Transform";
    /// <summary>Play mode category.</summary>
    public const string CategoryPlay = "Play Mode";
    /// <summary>Window operations category.</summary>
    public const string CategoryWindow = "Window";
    /// <summary>Help category.</summary>
    public const string CategoryHelp = "Help";

    #endregion

    /// <summary>
    /// Registers all default editor shortcuts with the provided manager.
    /// </summary>
    /// <param name="manager">The shortcut manager to register with.</param>
    /// <param name="actions">Delegate that provides the action implementations.</param>
    public static void RegisterDefaults(ShortcutManager manager, IEditorShortcutActions actions)
    {
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentNullException.ThrowIfNull(actions);

        // File operations
        manager.Register(NewScene, "New Scene", CategoryFile, "Ctrl+N", actions.NewScene);
        manager.Register(OpenScene, "Open Scene", CategoryFile, "Ctrl+O", actions.OpenScene);
        manager.Register(SaveScene, "Save Scene", CategoryFile, "Ctrl+S", actions.SaveScene);
        manager.Register(SaveSceneAs, "Save Scene As...", CategoryFile, "Ctrl+Shift+S", actions.SaveSceneAs);
        manager.Register(Exit, "Exit", CategoryFile, "Alt+F4", actions.Exit);

        // Edit operations
        manager.Register(Undo, "Undo", CategoryEdit, "Ctrl+Z", actions.Undo);
        manager.Register(Redo, "Redo", CategoryEdit, "Ctrl+Y", actions.Redo);
        manager.Register(Cut, "Cut", CategoryEdit, "Ctrl+X", actions.Cut);
        manager.Register(Copy, "Copy", CategoryEdit, "Ctrl+C", actions.Copy);
        manager.Register(Paste, "Paste", CategoryEdit, "Ctrl+V", actions.Paste);
        manager.Register(Delete, "Delete", CategoryEdit, "Del", actions.Delete);
        manager.Register(SelectAll, "Select All", CategoryEdit, "Ctrl+A", actions.SelectAll);
        manager.Register(Duplicate, "Duplicate", CategoryEdit, "Ctrl+D", actions.Duplicate);
        manager.Register(Rename, "Rename", CategoryEdit, "F2", actions.Rename);
        manager.Register(Settings, "Settings", CategoryEdit, "Ctrl+,", actions.Settings);

        // Entity operations
        manager.Register(CreateEmpty, "Create Empty Entity", CategoryEntity, "Ctrl+Shift+N", actions.CreateEmpty);
        manager.Register(CreateChild, "Create Child Entity", CategoryEntity, KeyCombination.None, actions.CreateChild);

        // View operations
        manager.Register(FocusSelection, "Focus Selection", CategoryView, "F", actions.FocusSelection);
        manager.Register(ToggleGrid, "Toggle Grid", CategoryView, "G", actions.ToggleGrid);
        manager.Register(ToggleWireframe, "Toggle Wireframe", CategoryView, "Z", actions.ToggleWireframe);

        // Transform modes
        manager.Register(TranslateMode, "Translate Mode", CategoryTransform, "W", actions.TranslateMode);
        manager.Register(RotateMode, "Rotate Mode", CategoryTransform, "E", actions.RotateMode);
        manager.Register(ScaleMode, "Scale Mode", CategoryTransform, "R", actions.ScaleMode);
        manager.Register(ToggleSpace, "Toggle Local/World", CategoryTransform, "X", actions.ToggleSpace);

        // Play mode
        manager.Register(PlayStop, "Play/Stop", CategoryPlay, "Ctrl+P", actions.PlayStop);
        manager.Register(Pause, "Pause", CategoryPlay, "Ctrl+Shift+P", actions.Pause);
        manager.Register(StepFrame, "Step Frame", CategoryPlay, "Ctrl+Alt+P", actions.StepFrame);

        // Window operations
        manager.Register(ShowHierarchy, "Hierarchy", CategoryWindow, KeyCombination.None, actions.ShowHierarchy);
        manager.Register(ShowInspector, "Inspector", CategoryWindow, KeyCombination.None, actions.ShowInspector);
        manager.Register(ShowProject, "Project", CategoryWindow, KeyCombination.None, actions.ShowProject);
        manager.Register(ShowConsole, "Console", CategoryWindow, KeyCombination.None, actions.ShowConsole);
        manager.Register(ResetLayout, "Reset Layout", CategoryWindow, KeyCombination.None, actions.ResetLayout);

        // Help
        manager.Register(Documentation, "Documentation", CategoryHelp, "F1", actions.Documentation);
        manager.Register(About, "About KeenEyes", CategoryHelp, KeyCombination.None, actions.About);
    }
}

/// <summary>
/// Interface for providing implementations of editor shortcut actions.
/// </summary>
public interface IEditorShortcutActions
{
    // File operations
    /// <summary>Called when New Scene shortcut is triggered.</summary>
    void NewScene();
    /// <summary>Called when Open Scene shortcut is triggered.</summary>
    void OpenScene();
    /// <summary>Called when Save Scene shortcut is triggered.</summary>
    void SaveScene();
    /// <summary>Called when Save Scene As shortcut is triggered.</summary>
    void SaveSceneAs();
    /// <summary>Called when Exit shortcut is triggered.</summary>
    void Exit();

    // Edit operations
    /// <summary>Called when Undo shortcut is triggered.</summary>
    void Undo();
    /// <summary>Called when Redo shortcut is triggered.</summary>
    void Redo();
    /// <summary>Called when Cut shortcut is triggered.</summary>
    void Cut();
    /// <summary>Called when Copy shortcut is triggered.</summary>
    void Copy();
    /// <summary>Called when Paste shortcut is triggered.</summary>
    void Paste();
    /// <summary>Called when Delete shortcut is triggered.</summary>
    void Delete();
    /// <summary>Called when Select All shortcut is triggered.</summary>
    void SelectAll();
    /// <summary>Called when Duplicate shortcut is triggered.</summary>
    void Duplicate();
    /// <summary>Called when Rename shortcut is triggered.</summary>
    void Rename();
    /// <summary>Called when Settings shortcut is triggered.</summary>
    void Settings();

    // Entity operations
    /// <summary>Called when Create Empty shortcut is triggered.</summary>
    void CreateEmpty();
    /// <summary>Called when Create Child shortcut is triggered.</summary>
    void CreateChild();

    // View operations
    /// <summary>Called when Focus Selection shortcut is triggered.</summary>
    void FocusSelection();
    /// <summary>Called when Toggle Grid shortcut is triggered.</summary>
    void ToggleGrid();
    /// <summary>Called when Toggle Wireframe shortcut is triggered.</summary>
    void ToggleWireframe();

    // Transform modes
    /// <summary>Called when Translate Mode shortcut is triggered.</summary>
    void TranslateMode();
    /// <summary>Called when Rotate Mode shortcut is triggered.</summary>
    void RotateMode();
    /// <summary>Called when Scale Mode shortcut is triggered.</summary>
    void ScaleMode();
    /// <summary>Called when Toggle Space shortcut is triggered.</summary>
    void ToggleSpace();

    // Play mode
    /// <summary>Called when Play/Stop shortcut is triggered.</summary>
    void PlayStop();
    /// <summary>Called when Pause shortcut is triggered.</summary>
    void Pause();
    /// <summary>Called when Step Frame shortcut is triggered.</summary>
    void StepFrame();

    // Window operations
    /// <summary>Called when Show Hierarchy shortcut is triggered.</summary>
    void ShowHierarchy();
    /// <summary>Called when Show Inspector shortcut is triggered.</summary>
    void ShowInspector();
    /// <summary>Called when Show Project shortcut is triggered.</summary>
    void ShowProject();
    /// <summary>Called when Show Console shortcut is triggered.</summary>
    void ShowConsole();
    /// <summary>Called when Reset Layout shortcut is triggered.</summary>
    void ResetLayout();

    // Help
    /// <summary>Called when Documentation shortcut is triggered.</summary>
    void Documentation();
    /// <summary>Called when About shortcut is triggered.</summary>
    void About();
}
