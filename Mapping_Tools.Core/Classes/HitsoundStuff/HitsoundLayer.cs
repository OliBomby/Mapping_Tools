using Mapping_Tools.Core.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Core.Classes.MathUtil;

namespace Mapping_Tools.Core.Classes.HitsoundStuff;

/// <summary>
///     Represents a single hitsound and every time that hitsound has to be played.
///     It is also directly connected to the source of the data.
/// </summary>
public class HitsoundLayer
{
    /// <inheritdoc />
    public HitsoundLayer() : this(string.Empty, SampleSet.Normal, Hitsound.Normal, int.MaxValue,
        new LayerImportArgs(), new SampleGeneratingArgs(), new List<double>())
    {
    }

    /// <inheritdoc />
    public HitsoundLayer(string name, ImportType importType, SampleSet sampleSet, Hitsound hitsound, string samplePath) :
        this(name, sampleSet, hitsound, int.MaxValue, new LayerImportArgs(importType), new SampleGeneratingArgs(samplePath), new List<double>())
    {
    }

    /// <inheritdoc />
    public HitsoundLayer(string name, SampleSet sampleSet, Hitsound hitsound, SampleGeneratingArgs sampleArgs, LayerImportArgs importArgs) :
        this(name, sampleSet, hitsound, int.MaxValue, importArgs, sampleArgs, new List<double>())
    {
    }

    /// <inheritdoc />
    public HitsoundLayer(string name, SampleSet sampleSet, Hitsound hitsound, int priority, LayerImportArgs importArgs, SampleGeneratingArgs sampleArgs, List<double> times)
    {
        Name = name;
        SampleSet = sampleSet;
        Hitsound = hitsound;
        Priority = priority;
        ImportArgs = importArgs;
        SampleArgs = sampleArgs;
        Times = times;
    }

    /// <summary>
    ///     The name of this hitsound layer. This is only for the convenience of the user and not an unique identifier.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    ///     The sample set that this sample should play on.
    /// </summary>
    public SampleSet SampleSet { get; set; }

    /// <summary>
    ///     The hitsound that this sample should play on.
    /// </summary>
    public Hitsound Hitsound { get; set; }

    /// <summary>
    ///     The priority of this hitsound layer. When mixing multiple <see cref="Sample" />,
    ///     the sampleset of the one with the lowest priority will be taken.
    ///     This priority value is equal to the index in the hitsound layers list.
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    ///     Contains all the information about how this hitsound layer was generated, so it can be reloaded.
    /// </summary>
    public LayerImportArgs ImportArgs { get; set; }

    /// <summary>
    ///     Contains all the information about how the sound of this hitsound should be generated.
    /// </summary>
    public SampleGeneratingArgs SampleArgs { get; set; }

    /// <summary>
    ///     Contains all the times that this hitsound should play.
    ///     This list is usually sorted.
    /// </summary>
    public List<double> Times { get; set; }

    /// <summary>
    ///     Reloads this hitsound layer with times from a list of hitsound layers that could be relevant to this one.
    /// </summary>
    /// <param name="layers"></param>
    public void Reload(List<HitsoundLayer> layers)
    {
        var sameLayer = layers.FindAll(o => ImportArgs.ReloadCompatible(o.ImportArgs));

        Times.Clear();
        foreach (var hsl in sameLayer) Times.AddRange(hsl.Times);
        Times.Sort();
    }

    /// <summary>
    ///     Removes duplicate values from the <see cref="Times" /> list.
    /// </summary>
    public void RemoveDuplicates()
    {
        if (Times.Count < 2) return;

        for (int i = 1; i < Times.Count; i++)
            if (Math.Abs(Times[i] - Times[i - 1]) < Precision.DOUBLE_EPSILON)
            {
                Times.RemoveAt(i);
                i--;
            }
    }
}
