using CommunityToolkit.Mvvm.ComponentModel;
using Mapping_Tools.Core.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Core.Classes.HitsoundStuff;

namespace Mapping_Tools.Desktop.ViewModels.Adapters;

/// <summary>Adapts a plain hitsound layer for Desktop list display and editing.</summary>
public sealed class ObservableHitsoundLayer : ObservableObject
{
    private readonly HitsoundLayer model;

    /// <summary>Creates an adapter around the supplied layer.</summary>
    /// <param name="model">The plain layer edited by this adapter.</param>
    public ObservableHitsoundLayer(HitsoundLayer model)
    {
        this.model = model ?? throw new ArgumentNullException(nameof(model));
        SampleArgs = new ObservableSampleGeneratingArgs(model.SampleArgs);
    }

    /// <summary>Gets the plain layer represented by this adapter.</summary>
    public HitsoundLayer Model => model;

    /// <summary>Gets or sets the display name.</summary>
    public string Name
    {
        get => model.Name;
        set => SetValue(model.Name, value, next => model.Name = next);
    }

    /// <summary>Gets or sets the sample family.</summary>
    public SampleSet SampleSet
    {
        get => model.SampleSet;
        set => SetValue(model.SampleSet, value, next => model.SampleSet = next);
    }

    /// <summary>Gets or sets the hitsound layer.</summary>
    public Hitsound Hitsound
    {
        get => model.Hitsound;
        set => SetValue(model.Hitsound, value, next => model.Hitsound = next);
    }

    /// <summary>Gets or sets the export priority.</summary>
    public int Priority
    {
        get => model.Priority;
        set => SetValue(model.Priority, value, next => model.Priority = next);
    }

    /// <summary>Gets the persisted import settings.</summary>
    public LayerImportArgs ImportArgs => model.ImportArgs;

    /// <summary>Gets the observable sample-generation settings.</summary>
    public ObservableSampleGeneratingArgs SampleArgs { get; }

    /// <summary>Gets or sets the sorted timestamps assigned to this layer.</summary>
    public List<double> Times
    {
        get => model.Times;
        set
        {
            model.Times = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Creates a detached plain snapshot of this layer.</summary>
    /// <returns>A layer containing independent import, sample, and timing data.</returns>
    public HitsoundLayer Snapshot() => new(
        model.Name,
        model.SampleSet,
        model.Hitsound,
        model.Priority,
        CloneImportArgs(model.ImportArgs),
        SampleArgs.Snapshot(),
        model.Times.ToList());

    private static LayerImportArgs CloneImportArgs(LayerImportArgs source) => new(source.ImportType)
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
        Offset = source.Offset
    };

    private void SetValue<T>(T current, T value, Action<T> setter, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(current, value)) return;
        setter(value);
        OnPropertyChanged(propertyName);
    }
}
