using Mapping_Tools.Domain.Beatmaps.Types;
using Mapping_Tools.Domain.MathUtil;

namespace Mapping_Tools.Domain.Beatmaps.Events;

/// <summary>
/// This represents a storyboarded sound sample for osu! storyboards. These can always be found under the [Events] -> (Storyboard Sound Samples) section.
/// </summary>
/// <example>
/// Sample,56056,0,"soft-hitnormal.wav",30
/// </example>
public class StoryboardSoundSample : Event, IEquatable<StoryboardSoundSample>, IHasStartTime, IHasStoryboardLayer, IComparable<StoryboardSoundSample> {
    public override string EventType { get; set; } = "Sample";

    /// <summary>
    /// The time when this sound event occurs.
    /// </summary>
    public double StartTime { get; set; }

    /// <summary>
    /// The storyboard layer this event belongs to.
    /// </summary>
    public StoryboardLayer Layer { get; set; }

    /// <summary>
    /// The name of the sample file which is the sound of this storyboard sample.
    /// This is a partial path.
    /// </summary>
    public required string FilePath { get; set; }

    /// <summary>
    /// The volume of this sound. Ranges from 0 to 100.
    /// </summary>
    public double Volume { get; set; }

    /// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
    /// <param name="other">An object to compare with this object.</param>
    /// <returns>true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.</returns>
    public bool Equals(StoryboardSoundSample? other) {
        return
            other != null && Math.Abs(StartTime - other.StartTime) < Precision.DoubleEpsilon &&
            Layer == other.Layer &&
            FilePath == other.FilePath &&
            Math.Abs(Volume - other.Volume) < Precision.DoubleEpsilon;
    }

    public int CompareTo(StoryboardSoundSample? other) {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;
        return StartTime.CompareTo(other.StartTime);
    }
}