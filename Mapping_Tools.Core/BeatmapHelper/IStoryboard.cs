using JetBrains.Annotations;
using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Core.BeatmapHelper.Events;

namespace Mapping_Tools.Core.BeatmapHelper;

public interface IStoryboard {
    /// <summary>
    /// A list of all Events under the [Events] -> (Background and Video events) section.
    /// </summary>
    [NotNull]
    List<Event> BackgroundAndVideoEvents { get; set; }

    /// <summary>
    /// A list of all Breaks under the [Events] -> (Break Periods) section.
    /// </summary>
    [NotNull]
    List<Break> BreakPeriods { get; set; }

    /// <summary>
    /// A list of all Events under the [Events] -> (Storyboard Layer 0 (Background)) section.
    /// </summary>
    [NotNull]
    List<Event> StoryboardLayerBackground { get; set; }

    /// <summary>
    /// A list of all Events under the [Events] -> (Storyboard Layer 1 (Fail)) section.
    /// </summary>
    [NotNull]
    List<Event> StoryboardLayerFail { get; set; }

    /// <summary>
    /// A list of all Events under the [Events] -> (Storyboard Layer 2 (Pass)) section.
    /// </summary>
    [NotNull]
    List<Event> StoryboardLayerPass { get; set; }

    /// <summary>
    /// A list of all Events under the [Events] -> (Storyboard Layer 3 (Foreground)) section.
    /// </summary>
    [NotNull]
    List<Event> StoryboardLayerForeground { get; set; }

    /// <summary>
    /// A list of all Events under the [Events] -> (Storyboard Layer 4 (Overlay)) section.
    /// </summary>
    [NotNull]
    List<Event> StoryboardLayerOverlay { get; set; }

    /// <summary>
    /// A list of all storyboarded sound sample events under the [Events] -> (Storyboard Sound Samples) section.
    /// </summary>
    [NotNull]
    List<StoryboardSoundSample> StoryboardSoundSamples { get; set; }

    /// <summary>
    /// A list of all background colour transformation events under the [Events] -> (Background Colour Transformations) section.
    /// </summary>
    [NotNull]
    List<BackgroundColourTransformation> BackgroundColourTransformations { get; set; }
}

public static class IStoryboardExtensions {
    public static IEnumerable<Event> EnumerateAllEvents(this IStoryboard sb) {
        return sb.BackgroundAndVideoEvents.Concat(sb.BreakPeriods).Concat(sb.StoryboardSoundSamples)
            .Concat(sb.StoryboardLayerFail).Concat(sb.StoryboardLayerPass).Concat(sb.StoryboardLayerBackground)
            .Concat(sb.StoryboardLayerForeground).Concat(sb.StoryboardLayerOverlay).Concat(sb.BackgroundColourTransformations);
    }
}