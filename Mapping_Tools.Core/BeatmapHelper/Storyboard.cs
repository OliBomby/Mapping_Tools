using System.Text.RegularExpressions;
using Mapping_Tools.Core.BeatmapHelper.Events;

namespace Mapping_Tools.Core.BeatmapHelper;

/// <summary>
///     Models event collections shared by <c>.osu</c> and <c>.osb</c> files.
/// </summary>
public class StoryBoard
{
    /// <summary>
    ///     Initializes an empty storyboard.
    /// </summary>
    public StoryBoard()
    {
        BackgroundAndVideoEvents = [];
        BreakPeriods = [];
        StoryboardLayerBackground = [];
        StoryboardLayerPass = [];
        StoryboardLayerFail = [];
        StoryboardLayerForeground = [];
        StoryboardLayerOverlay = [];
        StoryboardSoundSamples = [];
    }

    /// <summary>
    ///     A list of all Events under the [Events] -> (Background and Video events) section.
    /// </summary>
    public List<Event> BackgroundAndVideoEvents { get; set; }

    /// <summary>
    ///     A list of all Breaks under the [Events] -> (Break Periods) section.
    /// </summary>
    public List<Break> BreakPeriods { get; set; }

    /// <summary>
    ///     A list of all Events under the [Events] -> (Storyboard Layer 0 (Background)) section.
    /// </summary>
    public List<Event> StoryboardLayerBackground { get; set; }

    /// <summary>
    ///     A list of all Events under the [Events] -> (Storyboard Layer 1 (Fail)) section.
    /// </summary>
    public List<Event> StoryboardLayerFail { get; set; }

    /// <summary>
    ///     A list of all Events under the [Events] -> (Storyboard Layer 2 (Pass)) section.
    /// </summary>
    public List<Event> StoryboardLayerPass { get; set; }

    /// <summary>
    ///     A list of all Events under the [Events] -> (Storyboard Layer 3 (Foreground)) section.
    /// </summary>
    public List<Event> StoryboardLayerForeground { get; set; }

    /// <summary>
    ///     A list of all Events under the [Events] -> (Storyboard Layer 4 (Overlay)) section.
    /// </summary>
    public List<Event> StoryboardLayerOverlay { get; set; }

    /// <summary>
    ///     A list of all storyboarded sound sample events under the [Events] -> (Storyboard Sound Samples) section.
    /// </summary>
    public List<StoryboardSoundSample> StoryboardSoundSamples { get; set; }

    /// <summary>
    ///     Whether to add the overlay layer header even if there are no events in that layer.
    /// </summary>
    public bool ForceAddOverlayLayer { get; set; }

    /// <summary>
    ///     Grabs the specified file name of storyboard file.
    ///     with format of:
    ///     <c>Artist - Title (Host).osb</c>
    /// </summary>
    /// <returns>String of file name.</returns>
    public string GetFileName(string artist, string title, string creator)
    {
        string fileName = $"{artist} - {title} ({creator}).osb";

        string regexSearch = new(Path.GetInvalidFileNameChars());
        var r = new Regex($"[{Regex.Escape(regexSearch)}]");
        fileName = r.Replace(fileName, "");
        return fileName;
    }
}
