using System.Diagnostics.CodeAnalysis;

namespace Mapping_Tools.Core.BeatmapHelper.Events;

/// <summary>
///     This represents a storyboarded sound sample for osu! storyboards. These can always be found under the [Events] ->
///     (Storyboard Sound Samples) section.
/// </summary>
/// <example>
///     Sample,56056,0,"soft-hitnormal.wav",30
/// </example>
public class StoryboardSoundSample : Event, IEquatable<StoryboardSoundSample>, IHasStartTime, IHasEndTime, IComparable<StoryboardSoundSample>
{
    /// <summary>
    ///     Creates an empty sound event for property-based construction.
    /// </summary>
    public StoryboardSoundSample() { }

    /// <summary>
    ///     Creates a storyboard sound event from explicit values.
    /// </summary>
    /// <param name="startTime">The playback time in milliseconds.</param>
    /// <param name="layer">The storyboard layer that owns the sound.</param>
    /// <param name="filePath">The sample path relative to the beatmap folder.</param>
    /// <param name="volume">The playback volume from 0 through 100.</param>
    public StoryboardSoundSample(double startTime, StoryboardLayer layer, string filePath, double volume)
    {
        StartTime = startTime;
        Layer = layer;
        FilePath = filePath;
        Volume = volume;
    }


    /// <summary>
    ///     The storyboard layer this event belongs to.
    /// </summary>
    public StoryboardLayer Layer { get; set; }

    /// <summary>
    ///     The name of the sample file which is the sound of this storyboard sample.
    ///     This is a partial path.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    ///     The volume of this sound. Ranges from 0 to 100.
    /// </summary>
    public double Volume { get; set; }

    /// <summary>
    ///     Orders sound events chronologically by <see cref="StartTime" />.
    /// </summary>
    /// <param name="other">The sound event to compare.</param>
    /// <returns>A signed value indicating the relative playback order.</returns>
    public int CompareTo(StoryboardSoundSample? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;
        return StartTime.CompareTo(other.StartTime);
    }

    /// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
    /// <param name="other">An object to compare with this object.</param>
    /// <returns>true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.</returns>
    [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
    public bool Equals(StoryboardSoundSample? other)
    {
        return other != null &&
               StartTime == other.StartTime &&
               Layer == other.Layer &&
               FilePath == other.FilePath &&
               Volume == other.Volume;
    }

    /// <inheritdoc />
    /// <remarks>A sound sample is instantaneous, so changing its end time also changes its start time.</remarks>
    public double EndTime
    {
        get => StartTime;
        set => StartTime = value;
    }

    /// <summary>
    ///     The time when this sound event occurs.
    /// </summary>
    public double StartTime { get; set; }

}
