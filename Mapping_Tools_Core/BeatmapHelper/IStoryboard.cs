using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Mapping_Tools_Core.BeatmapHelper.Events;

namespace Mapping_Tools_Core.BeatmapHelper {
    public interface IStoryboard {
        /// <summary>
        /// A list of all Events under the [Events] -> (Background and Video events) section.
        /// </summary>
        [NotNull]
        List<Event> BackgroundAndVideoEvents { get; }

        /// <summary>
        /// A list of all Breaks under the [Events] -> (Break Periods) section.
        /// </summary>
        [NotNull]
        List<Break> BreakPeriods { get; }

        /// <summary>
        /// A list of all Events under the [Events] -> (Storyboard Layer 0 (Background)) section.
        /// </summary>
        [NotNull]
        List<Event> StoryboardLayerBackground { get; }

        /// <summary>
        /// A list of all Events under the [Events] -> (Storyboard Layer 1 (Fail)) section.
        /// </summary>
        [NotNull]
        List<Event> StoryboardLayerFail { get; }

        /// <summary>
        /// A list of all Events under the [Events] -> (Storyboard Layer 2 (Pass)) section.
        /// </summary>
        [NotNull]
        List<Event> StoryboardLayerPass { get; }

        /// <summary>
        /// A list of all Events under the [Events] -> (Storyboard Layer 3 (Foreground)) section.
        /// </summary>
        [NotNull]
        List<Event> StoryboardLayerForeground { get; }

        /// <summary>
        /// A list of all Events under the [Events] -> (Storyboard Layer 4 (Overlay)) section.
        /// </summary>
        [NotNull]
        List<Event> StoryboardLayerOverlay { get; }

        /// <summary>
        /// A list of all storyboarded sound sample events under the [Events] -> (Storyboard Sound Samples) section.
        /// </summary>
        [NotNull]
        List<StoryboardSoundSample> StoryboardSoundSamples { get; }
    }
}