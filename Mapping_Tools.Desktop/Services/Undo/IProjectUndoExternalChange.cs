namespace Mapping_Tools.Desktop.Services.Undo;

/// <summary>Reverses files or other external state changed by one project edit.</summary>
public interface IProjectUndoExternalChange
{
    /// <summary>Approximate bytes retained by this change in the undo history.</summary>
    long EstimatedBytes => 0;

    /// <summary>Restores the state that existed before the edit.</summary>
    void Undo();

    /// <summary>Restores the state that existed after the edit.</summary>
    void Redo();
}
