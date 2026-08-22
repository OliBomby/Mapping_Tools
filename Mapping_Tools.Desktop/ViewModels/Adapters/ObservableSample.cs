using CommunityToolkit.Mvvm.ComponentModel;
using Mapping_Tools.Core.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Core.Classes.HitsoundStuff;

namespace Mapping_Tools.Desktop.ViewModels.Adapters;

/// <summary>Adapts a plain package sample for Desktop default-sample editing.</summary>
public sealed partial class ObservableSample : ObservableObject
{
    private readonly Sample model;

    /// <summary>Creates an adapter around the supplied sample.</summary>
    /// <param name="model">The plain sample edited by this adapter.</param>
    public ObservableSample(Sample model)
    {
        this.model = model ?? throw new ArgumentNullException(nameof(model));
        SampleSet = model.SampleSet;
        Hitsound = model.Hitsound;
        SampleArgs = new ObservableSampleGeneratingArgs(model.SampleArgs);
    }

    /// <summary>Gets the plain sample represented by this adapter.</summary>
    public Sample Model => model;

    /// <summary>Gets the observable source and transformation arguments.</summary>
    public ObservableSampleGeneratingArgs SampleArgs { get; }

    /// <summary>Gets or sets the sample family.</summary>
    [ObservableProperty]
    public partial SampleSet SampleSet { get; set; }

    /// <summary>Gets or sets the hitsound layer.</summary>
    [ObservableProperty]
    public partial Hitsound Hitsound { get; set; }

    /// <summary>Creates an independent plain snapshot for Application services.</summary>
    /// <returns>A copied package sample with copied generation arguments.</returns>
    public Sample Snapshot() => model.Copy();

    partial void OnSampleSetChanged(SampleSet value) => model.SampleSet = value;
    partial void OnHitsoundChanged(Hitsound value) => model.Hitsound = value;
}
