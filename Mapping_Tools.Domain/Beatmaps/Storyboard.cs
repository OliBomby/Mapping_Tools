using System.Text.RegularExpressions;
using Mapping_Tools.Domain.Beatmaps.Events;

namespace Mapping_Tools.Domain.Beatmaps;

/// <summary>
/// Stores everything under the [Events] section of an osu beatmap or storyboard.
/// </summary>
public class Storyboard {
    public List<Event> BackgroundAndVideoEvents { get; set; }

    public List<Break> BreakPeriods { get; set; }

    public List<Event> StoryboardLayerBackground { get; set; }

    public List<Event> StoryboardLayerFail { get; set; }

    public List<Event> StoryboardLayerPass { get; set; }

    public List<Event> StoryboardLayerForeground { get; set; }

    public List<Event> StoryboardLayerOverlay { get; set; }

    public List<StoryboardSoundSample> StoryboardSoundSamples { get; set; }

    public List<BackgroundColourTransformation> BackgroundColourTransformations { get; set; }

    /// <summary>
    /// Initializes an empty storyboard.
    /// </summary>
    public Storyboard() {
        BackgroundAndVideoEvents = new List<Event>();
        BreakPeriods = new List<Break>();
        StoryboardLayerBackground = new List<Event>();
        StoryboardLayerPass = new List<Event>();
        StoryboardLayerFail = new List<Event>();
        StoryboardLayerForeground = new List<Event>();
        StoryboardLayerOverlay = new List<Event>();
        StoryboardSoundSamples = new List<StoryboardSoundSample>();
        BackgroundColourTransformations = new List<BackgroundColourTransformation>();
    }

    /// <summary>
    /// Grabs the specified file name of storyboard file.
    /// with format of:
    /// <c>Artist - Title (Host).osb</c>
    /// </summary>
    /// <returns>String of file name.</returns>
    public static string GetFileName(string artist, string title, string creator) {
        string fileName = $"{artist} - {title} ({creator}).osb";

        string regexSearch = new string(Path.GetInvalidFileNameChars());
        Regex r = new Regex($"[{Regex.Escape(regexSearch)}]");
        fileName = r.Replace(fileName, "");
        return fileName;
    }
}