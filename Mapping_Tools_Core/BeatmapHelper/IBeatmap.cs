using System.Collections.Generic;
using JetBrains.Annotations;
using Mapping_Tools_Core.BeatmapHelper.ComboColours;
using Mapping_Tools_Core.BeatmapHelper.Events;
using Mapping_Tools_Core.BeatmapHelper.TimingStuff;

namespace Mapping_Tools_Core.BeatmapHelper {
    public interface IBeatmap : IComboColourCollection {
        /// <summary>
        /// Contains all the values in the [General] section of a .osu file. The key is the variable name and the value is the value.
        /// This section typically contains:
        /// AudioFilename,
        /// AudioLeadIn,
        /// PreviewTime,
        /// Countdown,
        /// SampleSet,
        /// StackLeniency,
        /// Mode,
        /// LetterboxInBreaks,
        /// StoryFireInFront,
        /// SkinPreference,
        /// EpilepsyWarning,
        /// CountdownOffset,
        /// SpecialStyle,
        /// WidescreenStoryboard,
        /// SamplesMatchPlaybackRate
        /// </summary>
        [NotNull]
        Dictionary<string, TValue> General { get; }

        /// <summary>
        /// Contains all the values in the [Editor] section of a .osu file. The key is the variable name and the value is the value.
        /// This section typically contains:
        /// Bookmarks,
        /// DistanceSpacing,
        /// BeatDivisor,
        /// GridSize,
        /// TimelineZoom
        /// </summary>
        [NotNull]
        Dictionary<string, TValue> Editor { get; }

        /// <summary>
        /// Contains all the values in the [Metadata] section of a .osu file. The key is the variable name and the value is the value.
        /// This section typically contains:
        /// Title,
        /// TitleUnicode,
        /// Artist,
        /// ArtistUnicode,
        /// Creator,
        /// Version,
        /// Source,
        /// Tags,
        /// BeatmapID,
        /// BeatmapSetID
        /// </summary>
        [NotNull]
        Dictionary<string, TValue> Metadata { get; }

        /// <summary>
        /// Contains all the values in the [Difficulty] section of a .osu file. The key is the variable name and the value is the value.
        /// This section typically contains:
        /// HPDrainRate,
        /// CircleSize,
        /// OverallDifficulty,
        /// ApproachRate,
        /// GlobalSliderMultiplier,
        /// SliderTickRate
        /// </summary>
        [NotNull]
        Dictionary<string, TValue> Difficulty { get; }

        /// <summary>
        /// Contains all the basic combo colours. The order of this list is the same as how they are numbered in the .osu.
        /// There can not be more than 8 combo colours.
        /// <c>Combo1 : 245,222,139</c>
        /// </summary>
        [NotNull]
        List<IComboColour> ComboColoursList { get; }

        /// <summary>
        /// Contains all the special colours. These include the colours of slider bodies or slider outlines.
        /// The key is the name of the special colour and the value is the actual colour.
        /// </summary>
        [NotNull]
        Dictionary<string, IComboColour> SpecialColours { get; }

        /// <summary>
        /// The timing of this beatmap. This objects contains all the timing points (data from the [TimingPoints] section) plus the global slider multiplier.
        /// It also has a number of helper methods to fetch data from the timing points.
        /// With this object you can always calculate the slider velocity at any time.
        /// Any changes to the slider multiplier property in this object will not be serialized. Change the value in <see cref="Difficulty"/> instead.
        /// </summary>
        [NotNull]
        Timing BeatmapTiming { get; }

        /// <summary>
        /// The storyboard of the Beatmap. Stores everything under the [Events] section.
        /// </summary>
        [NotNull]
        IStoryboard StoryBoard { get; }

        /// <summary>
        /// List of all the hit objects in this beatmap.
        /// </summary>
        [NotNull]
        IReadOnlyList<HitObject> HitObjects { get; }

        /// <summary>
        /// Gets the bookmarks of this beatmap. This returns a clone of the real bookmarks which are stored in the <see cref="Editor"/> property.
        /// The bookmarks are represented with just a double which is the time of the bookmark.
        /// </summary>
        [NotNull]
        List<double> GetBookmarks();

        /// <summary>
        /// Sets the bookmarks of this beatmap. This replaces the bookmarks which are stored in the <see cref="Editor"/> property.
        /// The bookmarks are represented with just a double which is the time of the bookmark.
        /// </summary>
        void SetBookmarks([NotNull] List<double> value);

        /// <summary>
        /// Grabs the specified file name of beatmap file.
        /// with format of:
        /// <c>Artist - Title (Host) [Difficulty].osu</c>
        /// </summary>
        /// <returns>String of file name.</returns>
        string GetFileName();

        /// <summary>
        /// Creates a deep-clone of this beatmap and returns it.
        /// </summary>
        /// <returns>The deep-cloned beatmap</returns>
        IBeatmap DeepClone();

        /// <summary>
        /// Creates a shallow-clone of this beatmap and returns it.
        /// </summary>
        /// <returns>The shallow-cloned beatmap</returns>
        IBeatmap Clone();
    }
}