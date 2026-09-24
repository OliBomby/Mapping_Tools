namespace Mapping_Tools.Application.Abstractions;

/// <summary>Persists a beatmap while letting the owning client choose its filename.</summary>
public interface IAutomaticBeatmapFilenameStore
{
    /// <summary>Writes a complete beatmap and returns its resulting path.</summary>
    string WriteBeatmap(string path, IEnumerable<string> lines);
}
