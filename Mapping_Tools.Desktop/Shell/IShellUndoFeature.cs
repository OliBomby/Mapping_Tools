using Mapping_Tools.Desktop.Services.Undo;

namespace Mapping_Tools.Desktop.Shell;

/// <summary>Exposes a feature's edit history to the shell's undo and redo actions.</summary>
public interface IShellUndoFeature
{
    /// <summary>Gets the feature's in-memory edit history.</summary>
    IProjectUndoHistory? UndoHistory { get; }
}
