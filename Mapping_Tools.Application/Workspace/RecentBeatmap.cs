namespace Mapping_Tools.ApplicationServices.Workspace;

/// <summary>
/// Identifies a previously selected beatmap and the culture-formatted timestamp
/// shown by the legacy recent-map list.
/// </summary>
/// <param name="Path">The local path recorded when the map was selected.</param>
/// <param name="DisplayDate">
/// The timestamp text exactly as persisted by legacy Mapping Tools. It remains
/// text because old files do not record which culture produced it.
/// </param>
public sealed record RecentBeatmap(string Path, string DisplayDate);
