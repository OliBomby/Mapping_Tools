using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;

namespace Mapping_Tools.Application.BeatmapEditing;

/// <summary>Resolves a shared hit-object selection mode against an editing session.</summary>
internal static class BeatmapObjectSelection
{
    /// <summary>Returns the hit objects selected by <paramref name="mode" />.</summary>
    /// <param name="session">The editing session containing editor and beatmap state.</param>
    /// <param name="mode">The source from which hit objects should be selected.</param>
    /// <param name="timeCode">The legacy osu! time-code expression used by Time mode.</param>
    /// <returns>The selected hit objects in their source order.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="session" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="mode" /> is not defined.</exception>
    internal static IReadOnlyList<HitObject> Select(
        BeatmapEditingSession session,
        HitObjectSelectionMode mode,
        string? timeCode)
    {
        ArgumentNullException.ThrowIfNull(session);

        return mode switch
        {
            HitObjectSelectionMode.Selected => session.SelectedHitObjects,
            HitObjectSelectionMode.Bookmarked => session.Editor.Beatmap.GetBookmarkedObjects(),
            HitObjectSelectionMode.Time => session.Editor.Beatmap.QueryTimeCode(timeCode ?? string.Empty).ToList(),
            HitObjectSelectionMode.Everything => session.Editor.Beatmap.HitObjects,
            _ => throw new ArgumentException("Unknown hit-object selection mode.", nameof(mode)),
        };
    }
}
