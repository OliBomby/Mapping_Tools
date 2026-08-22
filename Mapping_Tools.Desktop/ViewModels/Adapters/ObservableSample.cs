using CommunityToolkit.Mvvm.ComponentModel;
using Mapping_Tools.Core.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Core.Classes.HitsoundStuff;

namespace Mapping_Tools.Desktop.ViewModels.Adapters;

/// <summary>Adapts a plain package sample for Desktop default-sample editing.</summary>
public sealed class ObservableSample : ObservableObject
{
    private readonly Sample model;

    /// <summary>Creates an adapter around the supplied sample.</summary>
    /// <param name="model">The plain sample edited by this adapter.</param>
    public ObservableSample(Sample model)
    {
        this.model = model ?? throw new ArgumentNullException(nameof(model));
        SampleArgs = new ObservableSampleGeneratingArgs(model.SampleArgs);
    }

    /// <summary>Gets the plain sample represented by this adapter.</summary>
    public Sample Model => model;

    /// <summary>Gets the observable source and transformation arguments.</summary>
    public ObservableSampleGeneratingArgs SampleArgs { get; }

    /// <summary>Gets or sets the sample family.</summary>
    public SampleSet SampleSet
    {
        get => model.SampleSet;
        set
        {
            if (model.SampleSet == value) return;
            model.SampleSet = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Gets or sets the hitsound layer.</summary>
    public Hitsound Hitsound
    {
        get => model.Hitsound;
        set
        {
            if (model.Hitsound == value) return;
            model.Hitsound = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Creates an independent plain snapshot for Application services.</summary>
    /// <returns>A copied package sample with copied generation arguments.</returns>
    public Sample Snapshot() => model.Copy();
}
