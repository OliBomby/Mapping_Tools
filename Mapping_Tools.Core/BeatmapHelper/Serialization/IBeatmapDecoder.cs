namespace Mapping_Tools.Core.BeatmapHelper.Serialization;

/// <summary>Decodes complete osu! beatmap text into a beatmap model.</summary>
public interface IBeatmapDecoder
{
    /// <summary>Decodes a complete osu! beatmap document.</summary>
    /// <param name="text">The complete contents of an osu! beatmap file.</param>
    /// <returns>The decoded beatmap.</returns>
    Beatmap Decode(string text);
}
