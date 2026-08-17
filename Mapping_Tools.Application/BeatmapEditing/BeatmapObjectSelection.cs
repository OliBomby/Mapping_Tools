using Mapping_Tools.Core.Classes.BeatmapHelper;

namespace Mapping_Tools.Application.BeatmapEditing;

/// <summary>
/// Selects beatmap objects using the four import modes shared by slider tools.
/// </summary>
internal static class BeatmapObjectSelection
{
    internal static IReadOnlyList<HitObject> Select<TImportMode>(
        BeatmapEditingSession session,
        TImportMode importMode,
        TImportMode selected,
        TImportMode bookmarked,
        TImportMode time,
        TImportMode everything,
        string? timeCode)
        where TImportMode : struct, Enum
    {
        ArgumentNullException.ThrowIfNull(session);

        if (EqualityComparer<TImportMode>.Default.Equals(importMode, selected))
        {
            return session.SelectedHitObjects;
        }

        if (EqualityComparer<TImportMode>.Default.Equals(importMode, bookmarked))
        {
            return session.Editor.Beatmap.GetBookmarkedObjects();
        }

        if (EqualityComparer<TImportMode>.Default.Equals(importMode, time))
        {
            return session.Editor.Beatmap.QueryTimeCode(timeCode ?? string.Empty).ToList();
        }

        if (EqualityComparer<TImportMode>.Default.Equals(importMode, everything))
        {
            return session.Editor.Beatmap.HitObjects;
        }

        throw new ArgumentException("Unexpected beatmap import mode.", nameof(importMode));
    }
}
