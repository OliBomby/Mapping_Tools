using CommunityToolkit.Mvvm.ComponentModel;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.HitsoundStuff;

namespace Mapping_Tools.Desktop.Tools.HitsoundStudio.ViewModels.Adapters;

/// <summary>Adapts a plain hitsound layer for Desktop list display and editing.</summary>
public sealed partial class ObservableHitsoundLayer : ObservableObject
{
    /// <summary>Creates an adapter around the supplied layer.</summary>
    /// <param name="model">The plain layer edited by this adapter.</param>
    public ObservableHitsoundLayer(HitsoundLayer model)
    {
        Model = model ?? throw new ArgumentNullException(nameof(model));
        Name = model.Name;
        SampleSet = model.SampleSet;
        Hitsound = model.Hitsound;
        Priority = model.Priority;
        Times = model.Times;
        SampleArgs = new ObservableSampleGeneratingArgs(model.SampleArgs);
    }

    /// <summary>Gets the plain layer represented by this adapter.</summary>
    public HitsoundLayer Model { get; }

    /// <summary>Gets or sets the display name.</summary>
    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the sample family.</summary>
    [ObservableProperty]
    public partial SampleSet SampleSet { get; set; }

    /// <summary>Gets or sets the hitsound layer.</summary>
    [ObservableProperty]
    public partial Hitsound Hitsound { get; set; }

    /// <summary>Gets or sets the export priority.</summary>
    [ObservableProperty]
    public partial int Priority { get; set; }

    /// <summary>Gets the persisted import settings.</summary>
    public LayerImportArgs ImportArgs => Model.ImportArgs;

    /// <summary>Gets the observable sample-generation settings.</summary>
    public ObservableSampleGeneratingArgs SampleArgs { get; }

    /// <summary>Gets or sets the sorted timestamps assigned to this layer.</summary>
    [ObservableProperty]
    public partial List<double> Times { get; set; } = [];

    /// <summary>Creates a detached plain snapshot of this layer.</summary>
    /// <returns>A layer containing independent import, sample, and timing data.</returns>
    public HitsoundLayer Snapshot()
    {
        return new HitsoundLayer(
            Model.Name,
            Model.SampleSet,
            Model.Hitsound,
            Model.Priority,
            CloneImportArgs(Model.ImportArgs),
            SampleArgs.Snapshot(),
            Model.Times.ToList());
    }

    private static LayerImportArgs CloneImportArgs(LayerImportArgs source)
    {
        return new LayerImportArgs(source.ImportType)
        {
            Path = source.Path,
            X = source.X,
            Y = source.Y,
            SamplePath = source.SamplePath,
            Volume = source.Volume,
            DiscriminateVolumes = source.DiscriminateVolumes,
            DetectDuplicateSamples = source.DetectDuplicateSamples,
            RemoveDuplicates = source.RemoveDuplicates,
            Bank = source.Bank,
            Patch = source.Patch,
            Key = source.Key,
            Length = source.Length,
            LengthRoughness = source.LengthRoughness,
            Velocity = source.Velocity,
            VelocityRoughness = source.VelocityRoughness,
            Offset = source.Offset,
        };
    }

    partial void OnNameChanged(string value)
    {
        Model.Name = value;
    }

    partial void OnSampleSetChanged(SampleSet value)
    {
        Model.SampleSet = value;
    }

    partial void OnHitsoundChanged(Hitsound value)
    {
        Model.Hitsound = value;
    }

    partial void OnPriorityChanged(int value)
    {
        Model.Priority = value;
    }

    partial void OnTimesChanged(List<double> value)
    {
        Model.Times = value;
    }
}
