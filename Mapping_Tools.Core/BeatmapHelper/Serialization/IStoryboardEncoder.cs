namespace Mapping_Tools.Core.BeatmapHelper.Serialization;

/// <summary>Encodes storyboard models as osu! events-section text.</summary>
public interface IStoryboardEncoder
{
    /// <summary>Encodes the complete events section using the requested format version.</summary>
    /// <param name="storyboard">The storyboard to encode.</param>
    /// <param name="targetVersion">The osu! format version that determines storyboard time precision.</param>
    /// <returns>The complete events section, using CRLF line endings.</returns>
    string Encode(StoryBoard storyboard, int targetVersion = 128);
}
