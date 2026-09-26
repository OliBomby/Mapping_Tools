namespace Mapping_Tools.Core.BeatmapHelper.Serialization;

/// <summary>Decodes storyboard event text into a storyboard model.</summary>
public interface IStoryboardDecoder
{
    /// <summary>Decodes the events section of an osu! document.</summary>
    /// <param name="text">The complete text containing the events section.</param>
    /// <returns>The decoded storyboard.</returns>
    StoryBoard Decode(string text);
}
