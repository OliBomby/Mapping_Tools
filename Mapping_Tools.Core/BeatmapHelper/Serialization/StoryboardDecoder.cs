using Mapping_Tools.Core.BeatmapHelper.Events;
using static Mapping_Tools.Core.BeatmapHelper.FileFormatHelper;

namespace Mapping_Tools.Core.BeatmapHelper.Serialization;

/// <summary>Decodes osu! event sections into storyboard models.</summary>
public sealed class StoryboardDecoder : IStoryboardDecoder
{
    /// <inheritdoc />
    public StoryBoard Decode(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        string[] lines = SplitLines(text);
        StoryBoard storyboard = new();

        // Decode each event category separately so nested commands can be rebuilt as trees.
        var eventLines = GetCategoryLines(lines, "[Events]").ToList();
        var backgroundVideo = new List<string>();
        var breaks = new List<string>();
        var background = new List<string>();
        var fail = new List<string>();
        var pass = new List<string>();
        var foreground = new List<string>();
        var overlay = new List<string>();
        var samples = new List<string>();

        string[] backgroundVideoIdentifiers = ["0", "1", "Video"];
        string[] breakIdentifiers = ["2", "Break"];
        string[] sampleIdentifiers = ["5", "Sample"];
        string[] categories =
        [
            "//Storyboard Layer 0 (Background)",
            "//Storyboard Layer 1 (Fail)",
            "//Storyboard Layer 2 (Pass)",
            "//Storyboard Layer 3 (Foreground)",
            "//Storyboard Layer 4 (Overlay)",
        ];
        string lastCategory = categories[0];

        foreach (string line in eventLines)
        {
            if (backgroundVideoIdentifiers.Any(line.StartsWith))
            {
                backgroundVideo.Add(line);
            }
            else if (breakIdentifiers.Any(line.StartsWith))
            {
                breaks.Add(line);
            }
            else if (sampleIdentifiers.Any(line.StartsWith))
            {
                samples.Add(line);
            }
            else if (categories.Any(line.StartsWith)) lastCategory = line;
            else if (!line.StartsWith("//"))
            {
                switch (lastCategory)
                {
                    case "//Storyboard Layer 0 (Background)":
                        background.Add(line);
                        break;
                    case "//Storyboard Layer 1 (Fail)":
                        fail.Add(line);
                        break;
                    case "//Storyboard Layer 2 (Pass)":
                        pass.Add(line);
                        break;
                    case "//Storyboard Layer 3 (Foreground)":
                        foreground.Add(line);
                        break;
                    case "//Storyboard Layer 4 (Overlay)":
                        overlay.Add(line);
                        break;
                }
            }
        }

        storyboard.ForceAddOverlayLayer = CategoryExists(eventLines, "//Storyboard Layer 4 (Overlay)");
        storyboard.BackgroundAndVideoEvents.AddRange(backgroundVideo.Select(EventTextCodec.DecodeLine));
        storyboard.BreakPeriods.AddRange(breaks.Select(line => (Break)EventTextCodec.DecodeLine(line)));

        storyboard.StoryboardLayerBackground.AddRange(EventTextCodec.DecodeTree(background));
        storyboard.StoryboardLayerFail.AddRange(EventTextCodec.DecodeTree(fail));
        storyboard.StoryboardLayerPass.AddRange(EventTextCodec.DecodeTree(pass));
        storyboard.StoryboardLayerForeground.AddRange(EventTextCodec.DecodeTree(foreground));
        storyboard.StoryboardLayerOverlay.AddRange(EventTextCodec.DecodeTree(overlay));

        storyboard.StoryboardSoundSamples.AddRange(
            samples.Select(line => (StoryboardSoundSample)EventTextCodec.DecodeLine(line)));
        return storyboard;
    }

    private static string[] SplitLines(string text) => text.Split(["\r\n", "\n", "\r"], StringSplitOptions.None);
}
