using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Application.Settings.Models;

namespace Mapping_Tools.Application.BeatmapEditing;

/// <summary>Determines when a mapping-tool save should refresh the live editor.</summary>
public static class AutomaticEditorReloadPolicy
{
    /// <summary>
    ///     Returns whether the save belongs to a QuickRun mutation of a live
    ///     editor session while automatic reload is enabled.
    /// </summary>
    /// <param name="session">The target beatmap session being saved.</param>
    /// <param name="quickRun">Whether the mutation was started by QuickRun.</param>
    /// <param name="settings">The application preferences controlling auto reload.</param>
    /// <returns><see langword="true" /> only when all automatic reload conditions are met.</returns>
    public static bool ShouldReloadEditor(
        BeatmapEditingSession session,
        bool quickRun,
        ApplicationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(settings);

        return quickRun
               && session.Source == BeatmapEditingSource.LiveEditor
               && settings.AutoReload;
    }
}
