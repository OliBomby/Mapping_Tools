#nullable disable
using System.Collections;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.MathUtil;

namespace Mapping_Tools.Core.BeatmapHelper;

/// <summary>
///     Represents one playable edge expanded from a hit object, with both explicit and timing-inherited sample state.
/// </summary>
public class TimelineObject
{
    // 
    /// <summary>
    ///     Controls whether hitsound-copy operations may use this edge.
    /// </summary>
    /// <remarks>Special for hitsound copier</remarks>
    public bool CanCopy = true;

    /// <summary>
    ///     Generates a new <see cref="TimelineObject" />.
    /// </summary>
    /// <param name="origin"></param>
    /// <param name="time"></param>
    /// <param name="objectType"></param>
    /// <param name="repeat"></param>
    /// <param name="hitsounds"></param>
    /// <param name="sampleset"></param>
    /// <param name="additionset"></param>
    public TimelineObject(HitObject origin, double time, int objectType, int repeat, int hitsounds, SampleSet sampleset, SampleSet additionset)
    {
        Origin = origin;
        Time = time;

        var b = new BitArray(new[] { hitsounds });
        Normal = b[0];
        Whistle = b[1];
        Finish = b[2];
        Clap = b[3];

        SampleSet = sampleset;
        AdditionSet = additionset;

        ObjectType = objectType;

        Repeat = repeat;

        if (IsCircle || IsHoldnoteHead) // Can have custom index/volume/filename
        {
            CustomIndex = origin.CustomIndex;
            SampleVolume = origin.SampleVolume;
            Filename = origin.Filename;
        }
    }

    /// <summary>
    ///     Gets or sets the hit object whose edge this event represents.
    /// </summary>
    public HitObject Origin { get; set; }

    /// <summary>
    ///     Gets or sets the edge time in milliseconds.
    /// </summary>
    public double Time { get; set; }

    /// <summary>
    ///     Gets or sets the edge index: zero for a head, intermediate slider repeats, and the final index for a tail.
    /// </summary>
    public int Repeat { get; set; }

    /// <summary>
    ///     Gets or sets the packed osu! gameplay object type inherited from <see cref="Origin" />.
    /// </summary>
    public int ObjectType { get; set; }

    private BitArray TypeArray => new(new[] { ObjectType });

    /// <summary>
    ///     Indicates that the origin is a circle.
    /// </summary>
    public bool IsCircle => TypeArray[0];

    /// <summary>
    ///     Indicates that the origin is a slider.
    /// </summary>
    public bool IsSlider => TypeArray[1];

    /// <summary>
    ///     Indicates that the origin is a spinner.
    /// </summary>
    public bool IsSpinner => TypeArray[3];

    /// <summary>
    ///     Indicates that the origin is an osu!mania hold note.
    /// </summary>
    public bool IsHoldNote => TypeArray[7];

    /// <summary>
    ///     Indicates that this event is a slider head.
    /// </summary>
    public bool IsSliderHead => IsSlider && Repeat == 0;

    /// <summary>
    ///     Indicates that this event is an intermediate slider repeat edge.
    /// </summary>
    public bool IsSliderRepeat => IsSlider && Repeat != 0 && Repeat != Origin.Repeat;

    /// <summary>
    ///     Indicates that this event is the final slider edge.
    /// </summary>
    public bool IsSliderEnd => IsSlider && Repeat == Origin.Repeat;

    /// <summary>
    ///     Indicates that this event is the silent beginning of a spinner.
    /// </summary>
    public bool IsSpinnerHead => IsSpinner && Repeat == 0;

    /// <summary>
    ///     Indicates that this event is the playable spinner end.
    /// </summary>
    public bool IsSpinnerEnd => IsSpinner && Repeat == 1;

    /// <summary>
    ///     Indicates that this event is the playable beginning of a mania hold note.
    /// </summary>
    public bool IsHoldnoteHead => IsHoldNote && Repeat == 0;

    /// <summary>
    ///     Indicates that this event is the mania hold-note release.
    /// </summary>
    public bool IsHoldnoteEnd => IsHoldNote && Repeat == 1;

    /// <summary>
    ///     Gets or sets the event's explicit normal-layer sample family.
    /// </summary>
    public SampleSet SampleSet { get; set; }

    /// <summary>
    ///     Gets or sets the event's explicit addition-layer sample family.
    /// </summary>
    public SampleSet AdditionSet { get; set; }

    /// <summary>
    ///     Indicates that the normal sample bit is set.
    /// </summary>
    public bool Normal { get; set; }

    /// <summary>
    ///     Indicates that the whistle sample bit is set.
    /// </summary>
    public bool Whistle { get; set; }

    /// <summary>
    ///     Indicates that the finish sample bit is set.
    /// </summary>
    public bool Finish { get; set; }

    /// <summary>
    ///     Indicates that the clap sample bit is set.
    /// </summary>
    public bool Clap { get; set; }

    /// <summary>
    ///     Indicates that this edge type produces a gameplay hitsound.
    /// </summary>
    public bool HasHitsound =>
        IsCircle || IsSliderHead || IsHoldnoteHead || IsSliderEnd || IsSpinnerEnd || IsSliderRepeat;

    /// <summary>
    ///     Indicates that this circle or hold-note head uses an explicit custom filename.
    /// </summary>
    public bool UsesFilename => !string.IsNullOrEmpty(Filename) && (IsCircle || IsHoldnoteHead);

    /// <summary>
    ///     Indicates that this edge type supports object-level custom index, volume, and filename fields.
    /// </summary>
    public bool CanCustoms => IsCircle || IsHoldnoteHead;

    /// <summary>
    ///     Gets or sets the event's explicit custom sample index.
    /// </summary>
    public int CustomIndex { get; set; }

    /// <summary>
    ///     Gets or sets the event's explicit sample volume percentage.
    /// </summary>
    public double SampleVolume { get; set; }

    /// <summary>
    ///     Gets or sets the explicit beatmap-relative custom filename.
    /// </summary>
    public string Filename { get; set; }

    // Special combined with greenline
    /// <summary>
    ///     Gets or sets the timing point active at the exact edge time.
    /// </summary>
    public TimingPoint TimingPoint { get; set; }

    /// <summary>
    ///     Gets or sets the timing point used for hitsound inheritance, including osu!'s five-millisecond lookup offset.
    /// </summary>
    public TimingPoint HitsoundTimingPoint { get; set; }

    /// <summary>
    ///     Gets or sets the uninherited timing point supplying BPM at this edge.
    /// </summary>
    public TimingPoint UninheritedTimingPoint { get; set; }

    /// <summary>
    ///     Gets or sets the fully resolved normal-layer sample family.
    /// </summary>
    public SampleSet FenoSampleSet { get; set; }

    /// <summary>
    ///     Gets or sets the fully resolved addition-layer sample family.
    /// </summary>
    public SampleSet FenoAdditionSet { get; set; }

    /// <summary>
    ///     Gets or sets the fully resolved custom sample index.
    /// </summary>
    public int FenoCustomIndex { get; set; }

    /// <summary>
    ///     Gets or sets the fully resolved sample volume percentage.
    /// </summary>
    public double FenoSampleVolume { get; set; }

    /// <summary>
    ///     Grabs the hitsound from the <see cref="TimelineObject" />
    /// </summary>
    /// <returns></returns>
    public Hitsound GetHitsound()
    {
        if (Normal) return Hitsound.Normal;
        if (Whistle) return Hitsound.Whistle;
        if (Finish) return Hitsound.Finish;
        if (Clap) return Hitsound.Clap;
        return Hitsound.Normal;
    }

    /// <summary>
    ///     Packs the normal, whistle, finish, and clap flags into the osu! hitsound integer.
    /// </summary>
    /// <returns>The packed hitsound bit field.</returns>
    public int GetHitsounds()
    {
        return MathHelper.GetIntFromBitArray(new BitArray(new[] { Normal, Whistle, Finish, Clap }));
    }

    /// <summary>
    ///     Sets the hitsound to the <see cref="TimelineObject" />
    /// </summary>
    /// <param name="hitsound"></param>
    public void SetHitsound(Hitsound hitsound)
    {
        Normal = false;
        Whistle = false;
        Finish = false;
        Clap = false;
        switch (hitsound)
        {
            case Hitsound.Normal:
                Normal = true;
                return;
            case Hitsound.Whistle:
                Whistle = true;
                return;
            case Hitsound.Finish:
                Finish = true;
                return;
            case Hitsound.Clap:
                Clap = true;
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(hitsound), hitsound, null);
        }
    }

    /// <summary>
    ///     Clears resolved sample overrides on this expanded timeline event.
    /// </summary>
    public void ResetHitsounds()
    {
        Normal = false;
        Whistle = false;
        Finish = false;
        Clap = false;
        SampleSet = SampleSet.None;
        AdditionSet = SampleSet.None;
    }

    /// <summary>
    ///     Checks if the selected timeline object does play a normal
    ///     (Only in modes other than Mania)
    /// </summary>
    /// <param name="mode"></param>
    /// <returns></returns>
    public bool PlaysNormal(GameMode mode)
    {
        return mode != GameMode.Mania || Normal || !(Whistle || Finish || Clap);
    }

    /// <summary>
    ///     Lists the resolved sample family, layer, and custom index combinations that play at this edge.
    /// </summary>
    /// <param name="mode"></param>
    /// <returns></returns>
    public List<Tuple<SampleSet, Hitsound, int>> GetPlayingHitsounds(GameMode mode = GameMode.Standard)
    {
        var samples = new List<Tuple<SampleSet, Hitsound, int>>();
        bool normal = mode != GameMode.Mania || Normal || !(Whistle || Finish || Clap);

        if (normal)
            samples.Add(new Tuple<SampleSet, Hitsound, int>(FenoSampleSet, Hitsound.Normal, FenoCustomIndex));
        if (Whistle)
            samples.Add(new Tuple<SampleSet, Hitsound, int>(FenoAdditionSet, Hitsound.Whistle, FenoCustomIndex));
        if (Finish)
            samples.Add(new Tuple<SampleSet, Hitsound, int>(FenoAdditionSet, Hitsound.Finish, FenoCustomIndex));
        if (Clap)
            samples.Add(new Tuple<SampleSet, Hitsound, int>(FenoAdditionSet, Hitsound.Clap, FenoCustomIndex));

        return samples;
    }

    /// <summary>
    ///     Grabs the playing filenames of the <see cref="TimelineObject" />
    /// </summary>
    /// <param name="mode">The osu! <see cref="GameMode" /></param>
    /// <param name="includeDefaults"></param>
    /// <returns></returns>
    public List<string> GetPlayingFilenames(GameMode mode = GameMode.Standard, bool includeDefaults = true)
    {
        var samples = new List<string>();
        bool normal = mode != GameMode.Mania || Normal || !(Whistle || Finish || Clap);
        bool useFilename = !string.IsNullOrEmpty(Filename) && (IsCircle || IsHoldnoteHead);

        if (useFilename)
        {
            samples.Add(Filename);
        }
        else if (includeDefaults || FenoCustomIndex != 0)
        {
            if (normal)
                samples.Add(GetFileName(FenoSampleSet, Hitsound.Normal, FenoCustomIndex, mode));
            if (Whistle)
                samples.Add(GetFileName(FenoAdditionSet, Hitsound.Whistle, FenoCustomIndex, mode));
            if (Finish)
                samples.Add(GetFileName(FenoAdditionSet, Hitsound.Finish, FenoCustomIndex, mode));
            if (Clap)
                samples.Add(GetFileName(FenoAdditionSet, Hitsound.Clap, FenoCustomIndex, mode));
        }

        return samples;
    }

    /// <summary>
    ///     Resolves filenames through a first-identical-sample map so duplicate audio refers to its canonical file.
    /// </summary>
    /// <param name="mode"></param>
    /// <param name="mapDir"></param>
    /// <param name="firstSamples"></param>
    /// <param name="includeDefaults"></param>
    /// <returns></returns>
    public List<string> GetFirstPlayingFilenames(GameMode mode, string mapDir, Dictionary<string, string> firstSamples, bool includeDefaults = true)
    {
        var samples = new List<string>();
        bool normal = mode != GameMode.Mania || Normal || !(Whistle || Finish || Clap);
        bool useFilename = !string.IsNullOrEmpty(Filename) && (IsCircle || IsHoldnoteHead);

        if (useFilename)
        {
            string samplePath = Path.Combine(mapDir, Filename);
            string fullPathExtLess = Path.Combine(
                Path.GetDirectoryName(samplePath) ?? throw new InvalidOperationException(),
                Path.GetFileNameWithoutExtension(samplePath));

            // Get the first occurence of this sound to not get duplicated
            if (firstSamples.Keys.Contains(fullPathExtLess)) samples.Add(Path.GetFileName(firstSamples[fullPathExtLess]));
        }
        else if (includeDefaults || FenoCustomIndex != 0)
        {
            if (normal)
                AddFirstIdenticalFilename(FenoSampleSet, Hitsound.Normal, FenoCustomIndex, samples, mode, false, mapDir, firstSamples, includeDefaults);
            if (Whistle)
                AddFirstIdenticalFilename(FenoAdditionSet, Hitsound.Whistle, FenoCustomIndex, samples, mode, false, mapDir, firstSamples, includeDefaults);
            if (Finish)
                AddFirstIdenticalFilename(FenoAdditionSet, Hitsound.Finish, FenoCustomIndex, samples, mode, false, mapDir, firstSamples, includeDefaults);
            if (Clap)
                AddFirstIdenticalFilename(FenoAdditionSet, Hitsound.Clap, FenoCustomIndex, samples, mode, false, mapDir, firstSamples, includeDefaults);
        }

        return samples;
    }

    private void AddFirstIdenticalFilename(SampleSet sampleSet, Hitsound hitsound, int index, List<string> samples, GameMode mode, bool useFilename, string mapDir,
        Dictionary<string, string> firstSamples, bool includeDefaults)
    {
        string filename = GetFileName(sampleSet, hitsound, index, mode);
        string samplePath = Path.Combine(mapDir, filename);
        string fullPathExtLess = Path.Combine(
            Path.GetDirectoryName(samplePath) ?? throw new InvalidOperationException(),
            Path.GetFileNameWithoutExtension(samplePath));

        // Get the first occurence of this sound to not get duplicated
        if (firstSamples.Keys.Contains(fullPathExtLess))
        {
            if (!useFilename) samples.Add(Path.GetFileName(firstSamples[fullPathExtLess]));
        }
        else
        {
            // Sample doesn't exist
            if (!useFilename && includeDefaults) samples.Add(GetFileName(sampleSet, hitsound, 0, mode));
        }
    }

    /// <summary>
    ///     Writes this expanded edge's sample edits back into the appropriate origin object or slider-edge fields.
    /// </summary>
    public void HitsoundsToOrigin()
    {
        if (Origin.IsCircle || Origin.IsSpinner && Repeat == 1 || Origin.IsHoldNote && Repeat == 0)
        {
            Origin.Hitsounds = GetHitsounds();
            Origin.SampleSet = SampleSet;
            Origin.AdditionSet = AdditionSet;
            Origin.CustomIndex = CustomIndex;
            Origin.SampleVolume = SampleVolume;
            Origin.Filename = Filename;
        }
        else if (Origin.IsSlider)
        {
            Origin.EdgeHitsounds[Repeat] = GetHitsounds();
            Origin.EdgeSampleSets[Repeat] = SampleSet;
            Origin.EdgeAdditionSets[Repeat] = AdditionSet;
        }
    }

    /// <summary>
    ///     Resolves auto sample families, custom index, and volume against the active hitsound timing point.
    /// </summary>
    /// <param name="hstp"></param>
    public void GiveHitsoundTimingPoint(TimingPoint hstp)
    {
        HitsoundTimingPoint = hstp;
        FenoSampleSet = SampleSet == 0 ? hstp.SampleSet : SampleSet;
        FenoAdditionSet = AdditionSet == 0 ? FenoSampleSet : AdditionSet;
        FenoCustomIndex = CustomIndex == 0 ? hstp.SampleIndex : CustomIndex;
        FenoSampleVolume = Math.Abs(SampleVolume) < Precision.DOUBLE_EPSILON ? hstp.Volume : SampleVolume;
    }

    /// <summary>
    ///     Grabs the playing file name of the object.
    /// </summary>
    /// <param name="sampleSet"></param>
    /// <param name="hitsound"></param>
    /// <param name="index"></param>
    /// <param name="mode"></param>
    /// <returns></returns>
    public static string GetFileName(SampleSet sampleSet, Hitsound hitsound, int index, GameMode mode)
    {
        string taiko = mode == GameMode.Taiko ? "taiko-" : "";
        switch (index)
        {
            case 0:
                return $"{taiko}{sampleSet.ToString().ToLower()}-hit{hitsound.ToString().ToLower()}-default";
            case 1:
                return $"{taiko}{sampleSet.ToString().ToLower()}-hit{hitsound.ToString().ToLower()}";
            default:
                return $"{taiko}{sampleSet.ToString().ToLower()}-hit{hitsound.ToString().ToLower()}{index}";
        }
    }

    /// <summary>
    ///     Copies the expanded event while retaining its reference to the originating hit object.
    /// </summary>
    /// <returns>An independently mutable timeline event.</returns>
    public TimelineObject Copy()
    {
        return (TimelineObject)MemberwiseClone();
    }

    /// <summary>
    ///     Formats the event's time, edge role, and resolved sample state for diagnostics.
    /// </summary>
    /// <returns>A diagnostic description rather than osu! file text.</returns>
    public override string ToString()
    {
        return $"{Time}, {ObjectType}, {Repeat}, {FenoSampleVolume}";
    }
}
