namespace Mapping_Tools.Core.BeatmapHelper.Serialization;

/// <summary>Encodes beatmap models as complete osu! beatmap text.</summary>
public interface IBeatmapEncoder
{
    /// <summary>Encodes a beatmap using the format version stored on the beatmap.</summary>
    /// <param name="beatmap">The beatmap to encode.</param>
    /// <returns>The complete osu! beatmap document, using CRLF line endings.</returns>
    string Encode(Beatmap beatmap);
}
