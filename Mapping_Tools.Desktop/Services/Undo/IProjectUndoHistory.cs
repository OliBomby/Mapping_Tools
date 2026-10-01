namespace Mapping_Tools.Desktop.Services.Undo;

/// <summary>Provides one project's in-memory edit history.</summary>
public interface IProjectUndoHistory
{
    /// <summary>Raised when the available undo or redo actions change.</summary>
    event EventHandler? Changed;

    /// <summary>Gets whether a previous project state can be restored.</summary>
    bool CanUndo { get; }

    /// <summary>Gets whether an undone project state can be restored.</summary>
    bool CanRedo { get; }

    /// <summary>Gets whether a previous state is currently being installed.</summary>
    bool IsRestoring { get; }

    /// <summary>Groups changes in a possibly nested operation, blocking undo and redo until it completes.</summary>
    /// <returns>A scope that commits the outermost edit when disposed.</returns>
    IDisposable BeginEdit();

    /// <summary>Groups pointer, keyboard, or text input changes while allowing undo and redo before the scope ends.</summary>
    /// <returns>A scope that commits pending changes when the outermost edit or gesture ends.</returns>
    IDisposable BeginGesture();

    /// <summary>Ignores changes made while refreshing view-only presentation fields.</summary>
    IDisposable SuspendRecording();

    /// <summary>Records the current project state if it differs from the last state.</summary>
    void Capture();

    /// <summary>Adds an external change to the current edit transaction.</summary>
    /// <param name="change">The change to replay alongside the project state.</param>
    void AddExternalChange(IProjectUndoExternalChange change);

    /// <summary>Restores the preceding project state.</summary>
    /// <remarks>Captures pending gesture changes first. Requests are ignored while an operation edit is open.</remarks>
    void Undo();

    /// <summary>Restores the next project state.</summary>
    /// <remarks>Captures pending gesture changes first. Requests are ignored while an operation edit is open.</remarks>
    void Redo();
}
