using Mapping_Tools.Application.BeatmapEditing.Models;

namespace Mapping_Tools.Application.Tools.GeometryDashboard.Models;

/// <summary>
///     Contains only the editor state needed by the Geometry Dashboard application.
/// </summary>
public sealed class GeometryDashboardRuntimeSnapshot
{
    /// <summary>
    ///     Creates a snapshot after Infrastructure has established editor availability.
    /// </summary>
    /// <param name="editor">The validated live editor state.</param>
    /// <param name="isEditorActive">Whether osu! was the foreground editor window at read time.</param>
    public GeometryDashboardRuntimeSnapshot(
        LiveBeatmapSnapshot editor,
        bool isEditorActive)
    {
        Editor = editor ?? throw new ArgumentNullException(nameof(editor));
        IsEditorActive = isEditorActive;
    }

    /// <summary>Gets the validated live editor-memory snapshot.</summary>
    public LiveBeatmapSnapshot Editor { get; }

    /// <summary>Gets whether osu! was the foreground editor window when this snapshot was read.</summary>
    public bool IsEditorActive { get; }
}
