using Mapping_Tools.Core.Classes.BeatmapHelper.Enums;

namespace Mapping_Tools.Core.Classes.HitsoundStuff;

/// <summary>
///     Assigns a generated sound to one osu! sample-set and hitsound layer at a package time.
/// </summary>
public class Sample
{
    /// <summary>
    ///     Creates a unity-volume, highest-priority normal sample in the normal set.
    /// </summary>
    public Sample()
    {
        SampleArgs = new SampleGeneratingArgs();
        OutsideVolume = 1;
        Priority = 0;
        SampleSet = SampleSet.Normal;
        Hitsound = Hitsound.Normal;
    }

    /// <summary>
    ///     Creates a fully specified package sample.
    /// </summary>
    /// <param name="sampleSet">The target osu! sample family.</param>
    /// <param name="hitsound">The target sample layer.</param>
    /// <param name="sampleArgs">The source and transformations.</param>
    /// <param name="priority">Conflict priority; lower values take precedence.</param>
    /// <param name="outsideVolume">The event-level volume multiplier.</param>
    public Sample(SampleSet sampleSet, Hitsound hitsound, SampleGeneratingArgs sampleArgs, int priority, double outsideVolume)
    {
        SampleArgs = sampleArgs;
        OutsideVolume = outsideVolume;
        Priority = priority;
        SampleSet = sampleSet;
        Hitsound = hitsound;
    }

    /// <summary>
    ///     Converts an import layer into a package sample while copying its generation arguments.
    /// </summary>
    /// <param name="hl">The imported hitsound layer.</param>
    public Sample(HitsoundLayer hl)
    {
        SampleArgs = hl.SampleArgs.Copy();
        OutsideVolume = 1;
        Priority = hl.Priority;
        SampleSet = hl.SampleSet;
        Hitsound = hl.Hitsound;
    }

    /// <summary>
    ///     Gets or sets the source and transformations used to produce the sound.
    /// </summary>
    public SampleGeneratingArgs SampleArgs { get; set; }

    /// <summary>
    ///     Gets or sets conflict priority; lower values win when deriving sample-set metadata.
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    ///     Gets or sets the event-level volume multiplier applied outside sample generation.
    /// </summary>
    public double OutsideVolume { get; set; }

    /// <summary>
    ///     Gets or sets the osu! normal, soft, or drum family receiving the sample.
    /// </summary>
    public SampleSet SampleSet { get; set; }

    /// <summary>
    ///     Gets or sets the normal, whistle, finish, or clap layer receiving the sample.
    /// </summary>
    public Hitsound Hitsound { get; set; }

    /// <summary>
    ///     Copies the sample and its nested generation arguments.
    /// </summary>
    /// <returns>An independently mutable sample.</returns>
    public Sample Copy()
    {
        return new Sample(SampleSet, Hitsound, SampleArgs.Copy(), Priority, OutsideVolume);
    }

    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
    {
        return $"{SampleArgs}, outside volume: {OutsideVolume}, priority: {Priority}, sampleset: {SampleSet}, hitsound: {Hitsound}";
    }
}
