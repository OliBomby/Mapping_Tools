namespace Mapping_Tools.Core.BeatmapHelper.Serialization;

/// <summary>Encodes storyboard models as complete osu! events sections.</summary>
public sealed class StoryboardEncoder : IStoryboardEncoder
{
    /// <inheritdoc />
    public string Encode(StoryBoard storyboard, int targetVersion = 128)
    {
        ArgumentNullException.ThrowIfNull(storyboard);
        return string.Join("\r\n", EncodeLines(storyboard, targetVersion, includeComments: true)) + "\r\n";
    }

    internal static List<string> EncodeLines(
        StoryBoard storyboard,
        int targetVersion,
        bool includeComments,
        bool includeEmptyOverlayHeader = false)
    {
        var lines = new List<string> { "[Events]" };

        // Keep category comments for legacy files; they define the grouping of storyboard records.
        AddComment(lines, "//Background and Video events", includeComments);
        lines.AddRange(
            storyboard.BackgroundAndVideoEvents.Select(value => EventTextCodec.EncodeLine(value, targetVersion)));

        AddComment(lines, "//Storyboard Layer 0 (Background)", includeComments);
        lines.AddRange(EventTextCodec.EncodeTree(storyboard.StoryboardLayerBackground, targetVersion));

        AddComment(lines, "//Storyboard Layer 1 (Fail)", includeComments);
        lines.AddRange(EventTextCodec.EncodeTree(storyboard.StoryboardLayerFail, targetVersion));

        AddComment(lines, "//Storyboard Layer 2 (Pass)", includeComments);
        lines.AddRange(EventTextCodec.EncodeTree(storyboard.StoryboardLayerPass, targetVersion));

        AddComment(lines, "//Storyboard Layer 3 (Foreground)", includeComments);
        lines.AddRange(EventTextCodec.EncodeTree(storyboard.StoryboardLayerForeground, targetVersion));

        // Preserve an explicitly requested empty overlay section when editing an existing document.
        if (includeEmptyOverlayHeader || storyboard.ForceAddOverlayLayer || storyboard.StoryboardLayerOverlay.Count > 0)
        {
            AddComment(lines, "//Storyboard Layer 4 (Overlay)", includeComments);
            lines.AddRange(EventTextCodec.EncodeTree(storyboard.StoryboardLayerOverlay, targetVersion));
        }

        AddComment(lines, "//Storyboard Sound Samples", includeComments);
        lines.AddRange(
            storyboard.StoryboardSoundSamples.Select(value => EventTextCodec.EncodeLine(value, targetVersion)));

        lines.Add("");
        return lines;
    }

    private static void AddComment(List<string> lines, string comment, bool includeComments)
    {
        if (includeComments)
            lines.Add(comment);
    }
}
