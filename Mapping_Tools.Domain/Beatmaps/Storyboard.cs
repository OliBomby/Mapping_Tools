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
        BackgroundAndVideoEvents = [];
        BreakPeriods = [];
        StoryboardLayerBackground = [];
        StoryboardLayerPass = [];
        StoryboardLayerFail = [];
        StoryboardLayerForeground = [];
        StoryboardLayerOverlay = [];
        StoryboardSoundSamples = [];
        BackgroundColourTransformations = [];
    }

    /// <summary>
    /// Grabs the specified file name of storyboard file.
    /// with format of:
    /// <c>Artist - Title (Host).osb</c>
    /// </summary>
    /// <returns>String of file name.</returns>
    public static string GetFileName(string artist, string title, string creator) {
        string fileName = $"{artist} - {title} ({creator}).osb";

        string regexSearch = new(Path.GetInvalidFileNameChars());
        Regex r = new Regex($"[{Regex.Escape(regexSearch)}]");
        fileName = r.Replace(fileName, "");
        return fileName;
    }
}

public static class StoryboardExtensions {
    public static IEnumerable<Event> EnumerateAllEvents(this Storyboard sb) {
        return sb.BackgroundAndVideoEvents.Concat(sb.BreakPeriods).Concat(sb.StoryboardSoundSamples)
            .Concat(sb.StoryboardLayerFail).Concat(sb.StoryboardLayerPass).Concat(sb.StoryboardLayerBackground)
            .Concat(sb.StoryboardLayerForeground).Concat(sb.StoryboardLayerOverlay).Concat(sb.BackgroundColourTransformations);
    }
}